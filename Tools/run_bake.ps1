# Detached YOLO bake runner — launched via a one-shot Scheduled Task so it survives
# Claude Code session/process teardown (managed background tasks were getting reaped).
# Progress is appended live to DevVideos\bake_progress.log; a DONE/FAILED marker is
# written at the end. Uses full tool paths so it works in the Task Scheduler context.

$ErrorActionPreference = 'Continue'
$repo = 'C:\Users\syson\Documents\Code\Unity-PassthroughCameraApiSamples'
$uv   = 'C:\Users\syson\AppData\Local\Microsoft\WinGet\Packages\astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe\uv.exe'
$ff   = 'C:\Users\syson\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin'
$log  = Join-Path $repo 'Builds\StudyVideo\bake_progress.log'

# Bakes against the ACTUAL sideloaded study video (Builds/StudyVideo/study_video.mp4,
# produced by encode_study_video.py), not the older times_sq_6k_cut.mp4 source this
# script used to point at — that produced a track with the wrong duration entirely
# (420s baked vs. the real 520s clip), silently misaligned by video time.
$env:Path += ";$ff"
Set-Location $repo
"START $(Get-Date -Format o)" | Out-File $log -Encoding utf8

& $uv run Tools/bake_detections.py `
    --video 'Builds\StudyVideo\study_video.mp4' `
    --out   'Builds\StudyVideo\study_video.detections.json' `
    --model yolo11l.pt --interval 0.5 --imgsz 1280 --cols 4 --rows 3 *>> $log

if (Test-Path 'Builds\StudyVideo\study_video.detections.json') {
    $mb = (Get-Item 'Builds\StudyVideo\study_video.detections.json').Length / 1MB
    "DONE $(Get-Date -Format o) size=$([math]::Round($mb,2))MB" | Out-File $log -Append -Encoding utf8
} else {
    "FAILED $(Get-Date -Format o)" | Out-File $log -Append -Encoding utf8
}
