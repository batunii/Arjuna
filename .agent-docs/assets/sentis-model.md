# Asset: Sentis YOLO Model

Last updated: 2026-06-03

ML assets for the MultiObjectDetection sample. Located in
`MultiObjectDetection/SentisInference/Model/`.

| File | What it is | Used how |
|---|---|---|
| `yolov9onnx.onnx` | Source YOLOv9 model in ONNX format | Input to the editor converter |
| `yolov9sentis.sentis` | Converted Unity Sentis model (with integrated NMS) | Runtime `ModelAsset` loaded by `SentisInferenceRunManager` |
| `SentisYoloClasses.txt` | Newline-separated COCO-style class labels | Loaded via `SentisInferenceUiManager.SetLabels(TextAsset)` for box labels |

## Conversion

`SentisModelEditorConverter` (Editor) adds a button to the `SentisInferenceRunManager` inspector that
converts the ONNX model to `.sentis`, baking in a non-max-suppression layer. Run this if you swap in a
new ONNX model.

## Licensing

Per the README, files in this `Model/` folder are MIT-licensed (from the MultimediaTechLab/YOLO project),
unlike the rest of the repo which uses the Oculus License.

Related: [Sentis Inference system](<../systems/sentis-inference.md>).
