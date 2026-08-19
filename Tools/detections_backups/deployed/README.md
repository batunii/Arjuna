# Deployed detection track

The exact baked track the user study ran, plus the sidecars the curation pass
(`Tools/triage_traffic_lights.py`) split out of it. Chapter 4 reports this set:
13,939 detections across 1,040 sampled frames of the 8:39 study clip.

| File | Contents |
|---|---|
| `study_video.detections.json` | The deployed track. Copy to the device's `persistentDataPath` as `study_video.detections.json`, or beside the clip, to reproduce the stimulus. |
| `study_video.detections.irrelevant_signals.json` | Lane-irrelevant signals removed by the curation pass (cross-street, pedestrian-facing, turned away). |
| `study_video.detections.flashers_removed_2026-07-18.json` | Flashing-signal lifetimes removed in the same pass. |

The dated snapshots in the parent directory are earlier bakes, kept for history; they
are **not** what the study ran. Verify a candidate file with:

```
python -c "import json;d=json.load(open('study_video.detections.json'));s=d['samples'];print(len(s),sum(len(x['d']) for x in s))"
```

which must print `1040 13939`.
