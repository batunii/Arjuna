// Continuous session telemetry: one CSV row per frame (heartbeat) plus labelled event rows.
// CSV contract shared with Tools/validate_session.py and Tools/analysis/ — see
// Tools/README-study-tools.md. Do not change columns or event labels without updating both.
//
// Output: Application.persistentDataPath/study_P<pid>_<yyyyMMdd_HHmmss>.csv
// Pull:   adb pull /sdcard/Android/data/<package>/files/<file> ./logs/
//
// Flushed every frame — a crash loses less than one frame of data (testing-strategy-v2 §10.3).

using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class StudyLogger : MonoBehaviour
    {
        public static StudyLogger Instance { get; private set; }

        // State stamped onto every row. The ConditionSequencer owns these.
        public string Pid { get; private set; } = "";
        public string Block { get; set; } = "";
        public string Condition { get; set; } = "";
        public float DrIntensity { get; set; }
        public string Mode { get; set; } = "";

        public string FilePath { get; private set; }
        public bool SessionOpen => m_writer != null;

        private const string k_header =
            "t_ms,pid,block,condition,yaw_deg,pitch_deg,roll_deg,head_speed_dps,dr_intensity,mode,event,payload";

        private StreamWriter m_writer;
        private readonly StringBuilder m_sb = new(256);

        // Head pose cached once per frame so heartbeat + any events in the same frame agree.
        private Quaternion m_lastHeadRot;
        private bool  m_hasLastRot;
        private float m_yaw, m_pitch, m_roll, m_headSpeed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void BeginSession(string pid)
        {
            if (SessionOpen) EndSession("superseded");
            Pid = Sanitize(pid);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            FilePath = Path.Combine(Application.persistentDataPath, $"study_P{Pid}_{stamp}.csv");
            m_writer = new StreamWriter(FilePath, false, new UTF8Encoding(false));
            m_writer.WriteLine(k_header);
            LogEvent("SESSION_START",
                $"app_version={Application.version};unity={Application.unityVersion};device={SystemInfo.deviceModel}");
            Debug.Log($"[StudyLogger] Session open: {FilePath}");
        }

        public void LogEvent(string label, string payload = "")
        {
            if (!SessionOpen)
            {
                if (!string.IsNullOrEmpty(label))
                    Debug.LogWarning($"[StudyLogger] Dropped event '{label}' — no session open.");
                return;
            }
            WriteRow(label, payload);
            m_writer.Flush(); // events land immediately
        }

        public void LogIncident(string text) => LogEvent("INCIDENT", $"text={Sanitize(text)}");

        public void EndSession(string reason = "normal")
        {
            if (!SessionOpen) return;
            LogEvent("SESSION_END", $"reason={Sanitize(reason)}");
            m_writer.Flush();
            m_writer.Dispose();
            m_writer = null;
            Debug.Log($"[StudyLogger] Session closed: {FilePath}");
        }

        private void Update()
        {
            // Cache head pose once per frame (events later this frame reuse it).
            Transform head = Camera.main != null ? Camera.main.transform : null;
            if (head == null) { m_headSpeed = 0f; return; }
            Vector3 e = head.rotation.eulerAngles;
            m_yaw   = Norm180(e.y);
            m_pitch = Norm180(e.x);
            m_roll  = Norm180(e.z);
            m_headSpeed = m_hasLastRot
                ? Quaternion.Angle(m_lastHeadRot, head.rotation) / Mathf.Max(Time.deltaTime, 0.001f)
                : 0f;
            m_lastHeadRot = head.rotation;
            m_hasLastRot  = true;
        }

        private void LateUpdate()
        {
            if (!SessionOpen) return;
            WriteRow("", ""); // heartbeat
            m_writer.Flush();
        }

        private void WriteRow(string label, string payload)
        {
            long tMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            m_sb.Clear();
            m_sb.Append(tMs).Append(',')
                .Append(Pid).Append(',')
                .Append(Block).Append(',')
                .Append(Condition).Append(',')
                .Append(m_yaw.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(m_pitch.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(m_roll.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(m_headSpeed.ToString("F1", CultureInfo.InvariantCulture)).Append(',')
                .Append(DrIntensity.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(Mode).Append(',')
                .Append(label).Append(',')
                .Append(Sanitize(payload));
            m_writer.WriteLine(m_sb);
        }

        // Payload uses key=value pairs joined by ';' — commas/newlines would break the CSV.
        private static string Sanitize(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');

        private static float Norm180(float deg)
        {
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg < -180f) deg += 360f;
            return deg;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && SessionOpen) m_writer.Flush();
        }

        private void OnDestroy()
        {
            if (SessionOpen) EndSession("destroyed");
            if (Instance == this) Instance = null;
        }
    }
}
