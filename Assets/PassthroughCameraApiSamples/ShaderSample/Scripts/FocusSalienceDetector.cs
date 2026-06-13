// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using Unity.InferenceEngine;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Slim YOLO runner for the FocusVignette effect. Runs the bundled Sentis model on the
    /// passthrough camera frames and feeds the bounding boxes of "important" object classes
    /// (people, stop signs, vehicles, ...) to the FocusVignette shader as normalized image-space
    /// rectangles. The shader uses them to locally remove the peripheral blur/desaturation and add a
    /// brightness/contrast boost, so safety-relevant objects pop out of the dimmed periphery.
    ///
    /// Unlike the MultiObjectDetection sample's SentisInferenceRunManager, this does NOT use spatial
    /// anchors, environment raycasting, or the detection-menu graph — we only need 2D boxes.
    /// Place this on the same GameObject as the FocusVignette MeshRenderer.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class FocusSalienceDetector : MonoBehaviour
    {
        private const int MaxBoxes = 16;

        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private MeshRenderer m_renderer;

        [Header("Sentis model")]
        [SerializeField] private ModelAsset m_sentisModel;
        [SerializeField] private TextAsset m_labelsAsset;
        [SerializeField] private BackendType m_backend = BackendType.CPU;
        [SerializeField, Range(0, 1)] private float m_iouThreshold = 0.6f;
        [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.35f;

        [Tooltip("Seconds to wait between inferences. YOLO is heavy; running it continuously starves the render thread. ~0.15s (≈6-7 Hz) is plenty for highlighting.")]
        [SerializeField] private float m_detectionInterval = 0.15f;

        [Tooltip("COCO class names that should be highlighted in the periphery. Must match the labels file exactly.")]
        [SerializeField]
        private List<string> m_highlightClasses = new List<string>
        {
            "person", "bicycle", "car", "motorbike", "bus", "truck", "traffic light", "stop sign", "dog", "cat"
        };

        private Worker m_engine;
        private Vector2Int m_inputSize;
        private string[] m_labels;
        private HashSet<int> m_highlightIds;
        private Material m_material;
        private readonly Vector4[] m_boxes = new Vector4[MaxBoxes];

        private static readonly int s_boxesId = Shader.PropertyToID("_SalienceBoxes");
        private static readonly int s_countId = Shader.PropertyToID("_SalienceCount");

        private void Awake()
        {
            var model = ModelLoader.Load(m_sentisModel);
            var inputShape = model.inputs[0].shape;
            m_inputSize = new Vector2Int(inputShape.Get(2), inputShape.Get(3));
            m_engine = new Worker(model, m_backend);

            m_labels = m_labelsAsset.text.Split('\n');
            m_highlightIds = new HashSet<int>();
            for (int i = 0; i < m_labels.Length; i++)
            {
                if (m_highlightClasses.Contains(m_labels[i].Trim()))
                {
                    m_highlightIds.Add(i);
                }
            }
        }

        private IEnumerator Start()
        {
            m_material = m_renderer.material;
            while (true)
            {
                yield return RunInference();
                if (m_detectionInterval > 0f)
                {
                    yield return new WaitForSeconds(m_detectionInterval);
                }
            }
        }

        private void OnDestroy()
        {
            m_engine?.Dispose();
        }

        private IEnumerator RunInference()
        {
            if (m_cameraAccess == null || !m_cameraAccess.IsPlaying || m_material == null)
            {
                yield return null;
                yield break;
            }

            Texture targetTexture = m_cameraAccess.GetTexture();
            var textureTransform = new TextureTransform().SetDimensions(targetTexture.width, targetTexture.height, 3);
            using var input = new Tensor<float>(new TensorShape(1, 3, m_inputSize.x, m_inputSize.y));
            TextureConverter.ToTensor(targetTexture, input, textureTransform);

            m_engine.Schedule(input);

            var boxesAwaiter = (m_engine.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
            while (!boxesAwaiter.IsCompleted)
            {
                yield return null;
            }
            using var boxes = boxesAwaiter.GetResult();
            if (boxes.shape[0] == 0)
            {
                SetCount(0);
                yield break;
            }

            var classAwaiter = (m_engine.PeekOutput(1) as Tensor<int>).ReadbackAndCloneAsync().GetAwaiter();
            while (!classAwaiter.IsCompleted)
            {
                yield return null;
            }
            using var classIDs = classAwaiter.GetResult();

            var scoreAwaiter = (m_engine.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
            while (!scoreAwaiter.IsCompleted)
            {
                yield return null;
            }
            using var scores = scoreAwaiter.GetResult();

            BuildSalienceBoxes(boxes, classIDs, scores);
        }

        private void BuildSalienceBoxes(Tensor<float> boxes, Tensor<int> classIDs, Tensor<float> scores)
        {
            // Gather highlight-class detections above the score threshold.
            var picked = new List<(Vector4 box, float score)>();
            var scoreArr = scores.AsReadOnlyNativeArray();
            for (int i = 0; i < scoreArr.Length; i++)
            {
                if (scoreArr[i] < m_scoreThreshold)
                {
                    continue;
                }
                if (!m_highlightIds.Contains(classIDs[i]))
                {
                    continue;
                }
                picked.Add((new Vector4(boxes[i, 0], boxes[i, 1], boxes[i, 2], boxes[i, 3]), scoreArr[i]));
            }

            // Sort by score and apply non-max suppression.
            picked.Sort((a, b) => b.score.CompareTo(a.score));
            var kept = new List<Vector4>();
            for (int i = 0; i < picked.Count && kept.Count < MaxBoxes; i++)
            {
                bool suppress = false;
                foreach (var k in kept)
                {
                    if (IoU(picked[i].box, k) > m_iouThreshold)
                    {
                        suppress = true;
                        break;
                    }
                }
                if (!suppress)
                {
                    kept.Add(picked[i].box);
                }
            }

            // Convert (x1,y1,x2,y2) in input pixels -> normalized center + half-size (v top-down).
            int n = 0;
            foreach (var b in kept)
            {
                float x1 = b.x / m_inputSize.x;
                float y1 = b.y / m_inputSize.y;
                float x2 = b.z / m_inputSize.x;
                float y2 = b.w / m_inputSize.y;
                m_boxes[n++] = new Vector4(
                    (x1 + x2) * 0.5f,
                    (y1 + y2) * 0.5f,
                    Mathf.Abs(x2 - x1) * 0.5f,
                    Mathf.Abs(y2 - y1) * 0.5f);
            }
            for (int i = n; i < MaxBoxes; i++)
            {
                m_boxes[i] = Vector4.zero;
            }

            m_material.SetVectorArray(s_boxesId, m_boxes);
            m_material.SetInt(s_countId, n);
        }

        private void SetCount(int n)
        {
            if (m_material != null)
            {
                m_material.SetInt(s_countId, n);
            }
        }

        private static float IoU(Vector4 a, Vector4 b)
        {
            float x1 = Mathf.Max(a.x, b.x);
            float y1 = Mathf.Max(a.y, b.y);
            float x2 = Mathf.Min(a.z, b.z);
            float y2 = Mathf.Min(a.w, b.w);
            float inter = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
            float areaA = (a.z - a.x) * (a.w - a.y);
            float areaB = (b.z - b.x) * (b.w - b.y);
            float union = areaA + areaB - inter;
            return union <= 0 ? 0 : inter / union;
        }
    }
}
