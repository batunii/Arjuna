// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Lightweight YOLOv9 inference runner — no UI dependencies.
// Runs inference on the passthrough camera every N frames and fires
// OnDetectionsReady with the filtered bounding box list.
//
// Video mode pipeline (stutter-free):
//   1. Graphics.Blit: full-res RT → small scratch RT  (GPU, fast, every N frames only)
//   2. AsyncGPUReadback: scratch RT → CPU NativeArray  (async, no pipeline stall)
//   3. CPU inference via Burst/Jobs                    (no GPU, non-blocking)
//   Avoids the synchronous GPU→CPU flush that TextureConverter.ToTensor causes on RenderTextures.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Rendering;

namespace PassthroughCameraSamples.ShaderSample
{
    public class YoloRunner : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private ModelAsset              m_sentisModel;

        [Header("Inference")]
        [SerializeField, Range(1, 60)]  private int   m_runEveryNFrames = 20;
        [SerializeField, Range(0, 1)]   private float m_scoreThreshold  = 0.30f;
        [SerializeField, Range(0, 1)]   private float m_iouThreshold    = 0.50f;

        [Tooltip("CPU (Burst) avoids competing with VR rendering; GPUCompute may be faster when the GPU has headroom — A/B on device.")]
        [SerializeField] private BackendType m_backend = BackendType.CPU;
        [Tooltip("Layers executed per frame via ScheduleIterable — spreads one inference over several frames to avoid hitches. 0 = schedule all at once.")]
        [SerializeField, Range(0, 64)] private int m_layersPerFrame = 8;
        [Tooltip("Alternate full-frame passes with zoomed-crop passes around recent detections — recovers small-object recall lost to downscaling.")]
        [SerializeField] private bool m_roiSecondPass = false;
        [Tooltip("Padding around the detection union for the ROI crop, as a fraction of the box size.")]
        [SerializeField, Range(0f, 1f)] private float m_roiPad = 0.5f;

        // (classId, box=(x1,y1,x2,y2) in model pixel space), plus the input dimensions
        public event Action<IReadOnlyList<(int classId, Vector4 box)>, Vector2Int> OnDetectionsReady;

        private Worker     m_engine;
        private Vector2Int m_inputSize;
        private readonly List<(int classId, Vector4 box)> m_detections = new();

        private RenderTexture m_overrideRT;
        private RenderTexture m_blitSource;
        private Texture2D     m_cpuTex;    // CPU-side copy of scratch RT — avoids GPU sync in ToTensor

        // ROI second-pass state (normalized full-frame coords)
        private int     m_inferenceIndex;
        private bool    m_roiValid;
        private Vector2 m_roiScale  = Vector2.one;
        private Vector2 m_roiOffset = Vector2.zero;

        public void SetOverrideRT(RenderTexture rt)  => m_overrideRT = rt;
        public void SetBlitSource(RenderTexture src) => m_blitSource = src;

        public bool HasEngine => m_engine != null;
        public Vector2Int InputSize => m_inputSize;

        private void Awake()
        {
            if (m_sentisModel == null) return;
            var model   = ModelLoader.Load(m_sentisModel);
            var shape   = model.inputs[0].shape;
            m_inputSize = new Vector2Int(shape.Get(2), shape.Get(3));
            m_engine    = new Worker(model, m_backend);
        }

        private IEnumerator Start()
        {
            if (m_engine == null) yield break;
            if (m_cameraAccess != null)
                while (!m_cameraAccess.IsPlaying && m_overrideRT == null) yield return null;
            else
                while (m_overrideRT == null) yield return null;

            int frame = 0;
            while (true)
            {
                if (frame++ % m_runEveryNFrames == 0)
                    yield return RunInference();
                else
                    yield return null;
            }
        }

        private void OnDestroy()
        {
            m_engine?.Dispose();
            if (m_cpuTex != null) Destroy(m_cpuTex);
        }

        private IEnumerator RunInference()
        {
            Texture inputTex;
            Vector2 cropScale  = Vector2.one;
            Vector2 cropOffset = Vector2.zero;

            if (m_overrideRT != null)
            {
                // ROI second pass: alternate full-frame passes with a zoomed crop around the
                // union of recent detections — small objects get ~4x more pixels.
                bool roiPass = m_roiSecondPass && m_roiValid && (m_inferenceIndex % 2 == 1);
                if (roiPass) { cropScale = m_roiScale; cropOffset = m_roiOffset; }
                m_inferenceIndex++;

                // Step 1: blit large source down to small scratch RT (every N frames only).
                // scale/offset sample only the crop window when this is an ROI pass.
                if (m_blitSource != null) Graphics.Blit(m_blitSource, m_overrideRT, cropScale, cropOffset);

                if (m_backend == BackendType.CPU)
                {
                    // Step 2: async GPU readback — yields each frame, zero pipeline stall
                    var req = AsyncGPUReadback.Request(m_overrideRT, 0, TextureFormat.RGBA32);
                    while (!req.done) yield return null;
                    if (req.hasError) yield break;

                    // Step 3: put data into a CPU-side Texture2D so ToTensor reads CPU memory
                    if (m_cpuTex == null)
                        m_cpuTex = new Texture2D(m_overrideRT.width, m_overrideRT.height,
                                                 TextureFormat.RGBA32, mipChain: false, linear: true);
                    m_cpuTex.SetPixelData(req.GetData<byte>(), 0);
                    m_cpuTex.Apply(false); // upload minimal GPU copy so ToTensor can sample it

                    inputTex = m_cpuTex;
                }
                else
                {
                    // GPU backend consumes the RT directly — no CPU round-trip needed.
                    inputTex = m_overrideRT;
                }
            }
            else
            {
                if (!m_cameraAccess.IsPlaying) yield break;
                inputTex = m_cameraAccess.GetTexture();
            }

            yield return InferCore(inputTex, cropScale, cropOffset, fireEvent: true);
        }

        // Shared inference core. Boxes are mapped back to full-frame model pixel space when
        // cropScale/cropOffset describe an ROI pass, so subscribers never see crop coords.
        private IEnumerator InferCore(Texture inputTex, Vector2 cropScale, Vector2 cropOffset, bool fireEvent)
        {
            var tfm = new TextureTransform().SetDimensions(inputTex.width, inputTex.height, 3);
            using var inp = new Tensor<float>(new TensorShape(1, 3, m_inputSize.x, m_inputSize.y));
            TextureConverter.ToTensor(inputTex, inp, tfm);

            if (m_layersPerFrame > 0)
            {
                // Spread layer execution over frames — caps per-frame cost instead of one spike.
                // Some model/backend combos NRE inside iterable execution (Conv layer on the
                // CPU backend here) — degrade permanently to plain Schedule() when that happens.
                var sched = m_engine.ScheduleIterable(inp);
                int n = 0;
                bool failed = false;
                while (true)
                {
                    bool moved;
                    try { moved = sched.MoveNext(); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[YoloRunner] ScheduleIterable failed ({e.GetType().Name}) " +
                                         "— falling back to Schedule() for this session.");
                        m_layersPerFrame = 0;
                        failed = true;
                        break;
                    }
                    if (!moved) break;
                    if (++n % m_layersPerFrame == 0) yield return null;
                }
                if (failed) m_engine.Schedule(inp);
            }
            else
            {
                m_engine.Schedule(inp);
            }

            var bAw = (m_engine.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
            while (!bAw.IsCompleted) yield return null;
            using var boxes = bAw.GetResult();
            if (boxes.shape[0] == 0) yield break;

            var cAw = (m_engine.PeekOutput(1) as Tensor<int>).ReadbackAndCloneAsync().GetAwaiter();
            while (!cAw.IsCompleted) yield return null;
            using var classIDs = cAw.GetResult();

            var sAw = (m_engine.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
            while (!sAw.IsCompleted) yield return null;
            using var scores = sAw.GetResult();

            RunNMS(boxes, classIDs, scores);

            bool isCrop = cropScale != Vector2.one || cropOffset != Vector2.zero;
            if (isCrop)
            {
                for (int i = 0; i < m_detections.Count; i++)
                {
                    var (c, b) = m_detections[i];
                    m_detections[i] = (c, new Vector4(
                        (b.x / m_inputSize.x * cropScale.x + cropOffset.x) * m_inputSize.x,
                        (b.y / m_inputSize.y * cropScale.y + cropOffset.y) * m_inputSize.y,
                        (b.z / m_inputSize.x * cropScale.x + cropOffset.x) * m_inputSize.x,
                        (b.w / m_inputSize.y * cropScale.y + cropOffset.y) * m_inputSize.y));
                }
            }
            else
            {
                UpdateRoiFromDetections();
            }

            if (fireEvent) OnDetectionsReady?.Invoke(m_detections, m_inputSize);
        }

        // Run one inference on an arbitrary texture (e.g. the offline detection baker).
        // Results are copied into `results`; the OnDetectionsReady event is NOT fired.
        public IEnumerator InferOnce(Texture input, List<(int classId, Vector4 box)> results)
        {
            if (m_engine == null) yield break;

            // The CPU backend needs CPU-resident texture data — feeding a RenderTexture
            // directly makes Conv.Execute NRE on the missing CPUTensorData.
            Texture feed = input;
            if (m_backend == BackendType.CPU && input is RenderTexture rtIn)
            {
                var req = AsyncGPUReadback.Request(rtIn, 0, TextureFormat.RGBA32);
                while (!req.done) yield return null;
                if (req.hasError) yield break;
                if (m_cpuTex == null || m_cpuTex.width != rtIn.width || m_cpuTex.height != rtIn.height)
                    m_cpuTex = new Texture2D(rtIn.width, rtIn.height,
                                             TextureFormat.RGBA32, mipChain: false, linear: true);
                m_cpuTex.SetPixelData(req.GetData<byte>(), 0);
                m_cpuTex.Apply(false);
                feed = m_cpuTex;
            }

            yield return InferCore(feed, Vector2.one, Vector2.zero, fireEvent: false);
            results.Clear();
            results.AddRange(m_detections);
        }

        // Crop window = padded union of the latest full-frame detections, min 25% of frame.
        private void UpdateRoiFromDetections()
        {
            if (m_detections.Count == 0) { m_roiValid = false; return; }

            float x1 = 1f, y1 = 1f, x2 = 0f, y2 = 0f;
            foreach (var (_, b) in m_detections)
            {
                float pw = (b.z - b.x) / m_inputSize.x * m_roiPad;
                float ph = (b.w - b.y) / m_inputSize.y * m_roiPad;
                x1 = Mathf.Min(x1, b.x / m_inputSize.x - pw);
                y1 = Mathf.Min(y1, b.y / m_inputSize.y - ph);
                x2 = Mathf.Max(x2, b.z / m_inputSize.x + pw);
                y2 = Mathf.Max(y2, b.w / m_inputSize.y + ph);
            }
            x1 = Mathf.Clamp01(x1); y1 = Mathf.Clamp01(y1);
            x2 = Mathf.Clamp01(x2); y2 = Mathf.Clamp01(y2);

            float w = Mathf.Max(x2 - x1, 0.25f);
            float h = Mathf.Max(y2 - y1, 0.25f);
            x1 = Mathf.Min(x1, 1f - w);
            y1 = Mathf.Min(y1, 1f - h);

            m_roiScale  = new Vector2(w, h);
            m_roiOffset = new Vector2(x1, y1);
            m_roiValid  = true;
        }

        private void RunNMS(Tensor<float> boxes, Tensor<int> classIDs, Tensor<float> scores)
        {
            m_detections.Clear();

            NativeArray<float>.ReadOnly sc = scores.AsReadOnlyNativeArray();
            var filtered = new List<int>();
            for (int i = 0; i < sc.Length; i++)
                if (sc[i] >= m_scoreThreshold) filtered.Add(i);

            if (filtered.Count == 0) return;

            filtered.Sort((a, b) => sc[b].CompareTo(sc[a]));

            var suppressed = new bool[filtered.Count];
            for (int i = 0; i < filtered.Count; i++)
            {
                if (suppressed[i]) continue;
                int idx = filtered[i];
                var box = Box(idx);
                m_detections.Add((classIDs[idx], box));

                for (int j = i + 1; j < filtered.Count; j++)
                {
                    if (!suppressed[j] && IoU(box, Box(filtered[j])) > m_iouThreshold)
                        suppressed[j] = true;
                }
            }

            Vector4 Box(int i) => new(boxes[i, 0], boxes[i, 1], boxes[i, 2], boxes[i, 3]);
        }

        private static float IoU(Vector4 a, Vector4 b)
        {
            float x1 = Mathf.Max(a.x, b.x), y1 = Mathf.Max(a.y, b.y);
            float x2 = Mathf.Min(a.z, b.z), y2 = Mathf.Min(a.w, b.w);
            float inter = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
            float union = (a.z - a.x) * (a.w - a.y) + (b.z - b.x) * (b.w - b.y) - inter;
            return union > 0 ? inter / union : 0;
        }
    }
}
