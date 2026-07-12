# Experimenter console for the study app — wraps the adb keyevents from
# Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/Study/README.md.
#
# Usage (from the repo root, any PowerShell):
#   .\Tools\study-console.ps1 logcat          # live [Sequencer]/study log (Ctrl+C to stop)
#   .\Tools\study-console.ps1 pid 12          # type participant ID 12 + ENTER (opens session)
#   .\Tools\study-console.ps1 practice        # P — practice run
#   .\Tools\study-console.ps1 space           # advance (lock window / start condition / next)
#   .\Tools\study-console.ps1 tlx             # toggle TLX_START / TLX_END
#   .\Tools\study-console.ps1 place           # W — (re)place CPT panel (Block A)
#   .\Tools\study-console.ps1 skipc           # K — skip Block C
#   .\Tools\study-console.ps1 incident        # I — incident marker
#   .\Tools\study-console.ps1 switch          # V — passthrough <-> video environment
#   .\Tools\study-console.ps1 end             # E — end session (closes the CSV)
#   .\Tools\study-console.ps1 install         # install -r Builds/Android/study.apk + launch
#   .\Tools\study-console.ps1 launch          # (re)launch the app
#   .\Tools\study-console.ps1 pull            # pull session CSVs into .\logs\ and list them
#
# adb is resolved from PATH, falling back to Unity's bundled platform-tools.

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("logcat", "pid", "practice", "space", "tlx", "place", "skipc",
                 "incident", "switch", "end", "install", "launch", "pull")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Arg
)

$ErrorActionPreference = "Stop"
$Package = "com.samples.passthroughcamera"
$RepoRoot = Split-Path $PSScriptRoot -Parent

$adbCmd = Get-Command adb -ErrorAction SilentlyContinue
if ($adbCmd) { $adb = $adbCmd.Source }
else {
    $adb = "C:\Program Files\Unity\Hub\Editor\6000.0.61f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
    if (-not (Test-Path $adb)) { throw "adb not found on PATH or in Unity's platform-tools." }
}

function Send-Key([int]$code, [string]$label) {
    & $adb shell input keyevent $code | Out-Null
    Write-Host "sent: $label (keyevent $code)"
}

switch ($Command) {
    "logcat"   { & $adb logcat -s Unity:V }
    "pid"      {
        if (-not $Arg -or $Arg -notmatch '^\d+$') { throw "Usage: study-console.ps1 pid <number>" }
        foreach ($ch in $Arg.ToCharArray()) { Send-Key (7 + [int]::Parse($ch)) "digit $ch" }
        Send-Key 66 "ENTER (open session)"
    }
    "practice" { Send-Key 44 "P (practice)" }
    "space"    { Send-Key 62 "SPACE (advance)" }
    "tlx"      { Send-Key 48 "T (TLX toggle)" }
    "place"    { Send-Key 51 "W (place CPT panel)" }
    "skipc"    { Send-Key 39 "K (skip Block C)" }
    "incident" { Send-Key 37 "I (incident marker)" }
    "switch"   { Send-Key 50 "V (environment switch)" }
    "end"      { Send-Key 33 "E (end session)" }
    "launch"   { & $adb shell monkey -p $Package -c android.intent.category.LAUNCHER 1 | Out-Null; Write-Host "launched $Package" }
    "install"  {
        $apk = Join-Path $RepoRoot "Builds\Android\study.apk"
        if (-not (Test-Path $apk)) { throw "APK not found: $apk (run Meta > Study > Build Study APK in Unity)" }
        & $adb install -r $apk
        & $adb shell monkey -p $Package -c android.intent.category.LAUNCHER 1 | Out-Null
        Write-Host "installed + launched $Package"
    }
    "pull"     {
        $dest = Join-Path $RepoRoot "logs"
        New-Item -ItemType Directory -Force $dest | Out-Null
        & $adb pull "/sdcard/Android/data/$Package/files/" $dest
        Get-ChildItem -Recurse $dest -Filter "study_P*.csv" | Sort-Object LastWriteTime |
            ForEach-Object { Write-Host ("{0}  {1:N0} KB" -f $_.FullName, ($_.Length / 1KB)) }
        Write-Host "Next: python Tools\validate_session.py <csv>"
    }
}
