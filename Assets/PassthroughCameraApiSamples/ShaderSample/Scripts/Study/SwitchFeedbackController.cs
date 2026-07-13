// Landing toast for the Y-button environment switch (see SceneSwitcher.cs), which fires a
// full SceneManager.LoadScene(..., LoadSceneMode.Single) — destroying everything in the old
// scene, including any per-scene toast object. This controller bootstraps once at app start
// and survives every later scene load via DontDestroyOnLoad (same pattern as InputManager.cs),
// so it's still alive to show a toast right after the NEW scene finishes loading.
//
// Deliberately decoupled from SceneSwitcher: it only reads the loaded scene's name from
// SceneManager.sceneLoaded, so no handoff data is needed. Deliberately not gated on
// StudyLogger.SessionOpen, matching SceneSwitcher's "always works" contract.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class SwitchFeedbackController : MonoBehaviour
    {
        private const float k_showTime = 2.8f;
        private const float k_fadeDur  = 0.35f;

        private GameObject   m_uiRoot;
        private CanvasGroup  m_uiGroup;
        private Text         m_nameText;
        private Material     m_uiMat;
        private float        m_timer;
        private bool         m_seenFirstLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject(nameof(SwitchFeedbackController));
            DontDestroyOnLoad(go);
            go.AddComponent<SwitchFeedbackController>();
        }

        private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // First callback is the cold-boot scene — nothing was "switched" yet.
            if (!m_seenFirstLoad) { m_seenFirstLoad = true; return; }

            if (m_uiRoot == null) InitUI();
            ShowToast(scene.name);
        }

        private void InitUI()
        {
            m_uiRoot = new GameObject("SwitchToastUI");
            var canvas = m_uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            m_uiRoot.AddComponent<CanvasScaler>();
            var rt = m_uiRoot.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(540, 130);
            rt.localScale = Vector3.one * 0.001f;
            m_uiGroup = m_uiRoot.AddComponent<CanvasGroup>();
            m_uiGroup.alpha = 0f; m_uiGroup.blocksRaycasts = false; m_uiGroup.interactable = false;

            // Queue 4100: same trick as VideoTestSceneManager.InitModeUI, so the toast draws
            // above the vignette sphere (queue 3000) in whichever scene just loaded.
            m_uiMat = new Material(Canvas.GetDefaultCanvasMaterial()) { renderQueue = 4100 };

            var bg = CreateChild(m_uiRoot, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color    = new Color(0.05f, 0.05f, 0.05f, 0.82f);
            bgImg.material = m_uiMat;
            StretchFill(bg);

            var stripe = CreateChild(m_uiRoot, "Stripe");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color    = new Color(0.30f, 0.85f, 1f, 1f);
            stripeImg.material = m_uiMat;
            var srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.86f); srt.anchorMax = Vector2.one;
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var nameGO = CreateChild(m_uiRoot, "SceneName");
            m_nameText = nameGO.AddComponent<Text>();
            m_nameText.material  = m_uiMat;
            m_nameText.font      = BuiltinFont();
            m_nameText.fontSize  = 38;
            m_nameText.fontStyle = FontStyle.Bold;
            m_nameText.alignment = TextAnchor.MiddleCenter;
            m_nameText.color     = Color.white;
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f); nrt.anchorMax = new Vector2(1f, 0.86f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            m_uiRoot.transform.position = Vector3.zero;
        }

        private static string EnvironmentLabel(string sceneName) => sceneName switch
        {
            "CameraSphereVignette" => "PASSTHROUGH",
            "VideoTestScene"       => "VIDEO",
            _                      => sceneName.ToUpperInvariant()
        };

        private void ShowToast(string sceneName)
        {
            if (m_nameText == null) return;
            m_nameText.text = $"SWITCHED\n{EnvironmentLabel(sceneName)}";
            m_timer = k_showTime;
        }

        private void Update()
        {
            if (m_uiRoot == null) return;

            // Never cache Camera.main here — the previous scene's camera was destroyed by
            // SceneManager.LoadScene(..., LoadSceneMode.Single).
            Transform head = Camera.main != null ? Camera.main.transform : null;
            if (head == null) return;

            m_uiRoot.transform.position = head.position + head.forward * 1.5f + Vector3.down * 0.30f;
            m_uiRoot.transform.rotation = Quaternion.LookRotation(m_uiRoot.transform.position - head.position, Vector3.up);

            if (m_timer > 0f)
            {
                m_timer -= Time.deltaTime;
                float fadeIn  = Mathf.Clamp01((k_showTime - m_timer) / k_fadeDur);
                float fadeOut = Mathf.Clamp01(m_timer / k_fadeDur);
                m_uiGroup.alpha = Mathf.Min(fadeIn, fadeOut);
            }
            else
            {
                m_uiGroup.alpha = 0f;
            }
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
