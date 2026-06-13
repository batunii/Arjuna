# System: Sentis Inference

Last updated: 2026-06-03

Runs the YOLOv9 model on passthrough frames using the Unity Inference Engine (Sentis,
`com.unity.ai.inference` 2.2.1). Lives in `MultiObjectDetection/SentisInference/`.

## Key scripts

- `SentisInferenceRunManager` — loads/runs the model on each camera frame, applies non-max
  suppression, forwards results to the UI manager. Notable members: static `PreloadModel(ModelAsset)`
  (called from `StartMenu`), `CalculateIoU(...)`, serialized `m_sentisModel` (ModelAsset),
  `m_iouThreshold`, `m_scoreThreshold`, backend config, plus `PassthroughCameraAccess`.
- `SentisInferenceUiManager` — draws 2D bounding boxes and places them in 3D via environment
  raycasting; pools boxes; `SetLabels(TextAsset)`, `DrawUIBoxes(...)`, `ClearAnnotations()`; raises
  `OnObjectsDetected` (`UnityEvent<int>`). Depends on `PassthroughCameraAccess`,
  `EnvironmentRayCastSampleManager`.
- `SentisModelEditorConverter` (Editor) — custom inspector for `SentisInferenceRunManager`; adds a
  button to convert the ONNX YOLO model to `.sentis` format with an integrated NMS layer.

## Assets

- `Model/yolov9onnx.onnx` — source model.
- `Model/yolov9sentis.sentis` — converted runtime model.
- `Model/SentisYoloClasses.txt` — class label list.

See [Sentis Model asset doc](<../assets/sentis-model.md>).

## Dependencies

`Unity.InferenceEngine`, `PassthroughCameraAccess`, `EnvironmentRayCastSampleManager`,
`DetectionManager`, `DetectionUiMenuManager`.

Related: [Object Detection](object-detection.md), [Environment Raycast](environment-raycast.md).
