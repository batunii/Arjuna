// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Play-mode bake tool: steps through StreamingAssets/DebugVideo.mp4, runs YOLO on each
// time bucket, and writes StreamingAssets/DebugVideo.detections.json. Enable BakeOnPlay
// in the Inspector, press Play in the editor, wait for the DONE log, then disable it.
// VideoTestSceneManager picks the JSON up automatically on the next run.
//
// All classes are baked; class filtering happens at playback time.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace PassthroughCameraSamples.ShaderSample
{
    public class VideoDetectionBaker : MonoBehaviour
    {
        [SerializeField] private YoloRunner m_yolo;
        [Tooltip("Enable, press Play in the editor, wait for the DONE log.")]
        [SerializeField] private bool m_bakeOnPlay = false;
        [SerializeField, Range(0.1f, 2f)] private float m_interval = 0.25f;
        [Tooltip("Bake only the first N seconds (0 = full video).")]
        [SerializeField] private float m_maxSeconds = 0f;

        public bool BakeOnPlay => m_bakeOnPlay;

        private IEnumerator Start()
        {
            if (!m_bakeOnPlay) yield break;
            if (m_yolo == null) m_yolo = GetComponent<YoloRunner>();
            if (m_yolo == null || !m_yolo.HasEngine)
            {
                Debug.LogError("[VideoDetectionBaker] No YoloRunner with a loaded model.");
                yield break;
            }

            var rt = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32);
            rt.Create();

            var go = new GameObject("BakeVideoPlayer");
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake     = false;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.targetTexture   = rt;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.url = Path.Combine(Application.streamingAssetsPath, "DebugVideo.mp4");
            vp.Prepare();
            while (!vp.isPrepared) yield return null;

            double len = vp.length;
            if (m_maxSeconds > 0f) len = Math.Min(len, m_maxSeconds);
            Debug.Log($"[VideoDetectionBaker] Baking {len:F1}s at {m_interval}s intervals…");

            bool seekDone = false;
            vp.seekCompleted += _ => seekDone = true;

            var track   = new VideoDetectionTrack { interval = m_interval };
            var results = new List<(int classId, Vector4 box)>();
            var inSize  = m_yolo.InputSize;

            int steps = 0;
            for (double t = 0; t < len; t += m_interval)
            {
                seekDone = false;
                vp.time = t;
                vp.Play();
                vp.Pause();
                float waited = 0f;
                while (!seekDone && waited < 2f) { waited += Time.unscaledDeltaTime; yield return null; }
                yield return null; // let the decoded frame land in the RT

                yield return m_yolo.InferOnce(rt, results);

                var sample = new BakedSample { t = (float)t };
                foreach (var (classId, box) in results)
                    sample.d.Add(new BakedDetection
                    {
                        c  = classId,
                        x1 = box.x / inSize.x, y1 = box.y / inSize.y,
                        x2 = box.z / inSize.x, y2 = box.w / inSize.y
                    });
                track.samples.Add(sample);

                if (++steps % 10 == 0)
                    Debug.Log($"[VideoDetectionBaker] {t:F1}/{len:F1}s…");
            }

            string outPath = Path.Combine(Application.streamingAssetsPath, "DebugVideo.detections.json");
            File.WriteAllText(outPath, JsonUtility.ToJson(track));
            Debug.Log($"[VideoDetectionBaker] DONE — {track.samples.Count} samples → {outPath}. " +
                      "Disable BakeOnPlay and re-enter Play mode to use the track.");

            Destroy(go);
            rt.Release();
            Destroy(rt);
        }
    }
}
