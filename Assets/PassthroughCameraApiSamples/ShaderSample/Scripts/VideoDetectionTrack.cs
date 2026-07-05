// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Baked detection track for VideoTestScene: per-time-bucket YOLO boxes precomputed
// offline by VideoDetectionBaker, so runtime highlighting needs no live inference
// and coordinates are stable. Boxes are stored normalized [0,1] in video-frame space.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PassthroughCameraSamples.ShaderSample
{
    [Serializable]
    public class BakedDetection
    {
        public int c;              // COCO class id
        public float x1, y1, x2, y2; // normalized [0,1], video-frame space
    }

    [Serializable]
    public class BakedSample
    {
        public float t;            // video time (seconds)
        public List<BakedDetection> d = new();
    }

    [Serializable]
    public class VideoDetectionTrack
    {
        public float interval = 0.25f;
        public List<BakedSample> samples = new();

        // Nearest sample at or before videoTime; null if the track has gone stale there.
        public BakedSample Lookup(double videoTime)
        {
            if (samples == null || samples.Count == 0) return null;
            int lo = 0, hi = samples.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (samples[mid].t <= videoTime) lo = mid; else hi = mid - 1;
            }
            var s = samples[lo];
            return (videoTime - s.t) <= interval * 1.5f ? s : null;
        }

        // On Android, StreamingAssets lives inside the APK (jar: URL) — needs UnityWebRequest.
        public static IEnumerator Load(string path, Action<VideoDetectionTrack> onDone)
        {
            string json = null;
            if (path.Contains("://"))
            {
                using var req = UnityWebRequest.Get(path);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    json = req.downloadHandler.text;
            }
            else if (System.IO.File.Exists(path))
            {
                json = System.IO.File.ReadAllText(path);
            }

            VideoDetectionTrack track = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { track = JsonUtility.FromJson<VideoDetectionTrack>(json); }
                catch (Exception e) { Debug.LogWarning($"[VideoDetectionTrack] parse failed: {e.Message}"); }
            }
            onDone?.Invoke(track);
        }
    }
}
