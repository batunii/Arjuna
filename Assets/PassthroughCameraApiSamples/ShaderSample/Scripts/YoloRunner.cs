// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Lightweight YOLOv9 inference runner — no UI dependencies.
// Runs inference on the passthrough camera every N frames and fires
// OnDetectionsReady with the filtered bounding box list.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample
{
    public class YoloRunner : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private ModelAsset              m_sentisModel;

        [Header("Inference")]
        [SerializeField, Range(1, 10)]  private int   m_runEveryNFrames = 4;
        [SerializeField, Range(0, 1)]   private float m_scoreThreshold  = 0.30f;
        [SerializeField, Range(0, 1)]   private float m_iouThreshold    = 0.50f;

        // (classId, box=(x1,y1,x2,y2) in model pixel space), plus the input dimensions
        public event Action<IReadOnlyList<(int classId, Vector4 box)>, Vector2Int> OnDetectionsReady;

        private Worker    m_engine;
        private Vector2Int m_inputSize;
        private readonly List<(int classId, Vector4 box)> m_detections = new();

        private void Awake()
        {
            if (m_sentisModel == null || m_cameraAccess == null) return;
            var model      = ModelLoader.Load(m_sentisModel);
            var shape      = model.inputs[0].shape;
            m_inputSize    = new Vector2Int(shape.Get(2), shape.Get(3));
            m_engine       = new Worker(model, BackendType.CPU);
        }

        private IEnumerator Start()
        {
            if (m_engine == null) yield break;
            while (!m_cameraAccess.IsPlaying) yield return null;

            int frame = 0;
            while (true)
            {
                if (frame++ % m_runEveryNFrames == 0)
                    yield return RunInference();
                else
                    yield return null;
            }
        }

        private void OnDestroy() => m_engine?.Dispose();

        private IEnumerator RunInference()
        {
            if (!m_cameraAccess.IsPlaying) yield break;

            var tex       = m_cameraAccess.GetTexture();
            var tfm       = new TextureTransform().SetDimensions(tex.width, tex.height, 3);
            using var inp = new Tensor<float>(new TensorShape(1, 3, m_inputSize.x, m_inputSize.y));
            TextureConverter.ToTensor(tex, inp, tfm);
            m_engine.Schedule(inp);

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
            OnDetectionsReady?.Invoke(m_detections, m_inputSize);
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
