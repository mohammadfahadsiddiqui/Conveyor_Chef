using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Makes every scene's UI look exactly as it does on the design phone (Samsung Galaxy S20
    /// Ultra, 1440x3200, used in the Device Simulator) on any device, phone or tablet, and
    /// re-fits it the moment the screen changes (another device in the Simulator, rotation,
    /// split screen, a foldable opening).
    ///
    /// Each root canvas is scaled so the design phone's whole screen fits on the device, and that
    /// "design frame" sits centred. Everything placed on the screen (buttons, cards, panels, text)
    /// is moved into the design frame, so it keeps the same size, shape and position relative to
    /// the others. Full-screen art (backgrounds, dims, bars) still fills the whole screen: picture
    /// backgrounds are enlarged with their design-phone proportions, so they are cropped, never
    /// stretched. On the design phone nothing changes.
    ///
    /// Only anchors and the canvas scaler change at runtime; the scene file is untouched. The same
    /// fit is previewed in Edit mode (DesignFramePreview) when another device is picked in the
    /// Simulator. A canvas that already fits itself (main menu, kitchen backdrop) is left alone,
    /// and anything can opt out with <see cref="DesignFrameIgnore"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-2000)]   // before the canvas scaler updates, so a new screen fits the same frame
    public sealed class DesignFrame : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;   // picks up UI created or re-anchored later

        private DesignFrameFitter fitter;
        private Canvas canvas;
        private float textScale = -1f;
        private Vector2 lastScreen;
        private Rect lastSafeArea;
        private bool refitLate;
        private float nextRefresh;

        public static Vector2 DesignScreen => DesignFrameFitter.DesignScreen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Runs after the scene's Awake/OnEnable and before Start.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas found in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (DesignFrameFitter.ShouldFit(found) && found.GetComponent<DesignFrame>() == null)
                        found.gameObject.AddComponent<DesignFrame>();
                }
            }
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            fitter = new DesignFrameFitter(canvas);
        }

        private void OnEnable()
        {
            Refit();
        }

        private void Update()
        {
            // New screen: fit now, and again after this frame's scripts (safe areas) ran.
            if (new Vector2(Screen.width, Screen.height) != lastScreen || Screen.safeArea != lastSafeArea)
            {
                Refit();
                refitLate = true;
            }
        }

        private void LateUpdate()
        {
            if (refitLate || Time.unscaledTime >= nextRefresh)
            {
                refitLate = false;
                Refit();
            }

            // Text drawn at another canvas scale would stay soft: redraw it at the new scale.
            if (canvas != null && canvas.scaleFactor > 0f && !Mathf.Approximately(canvas.scaleFactor, textScale))
            {
                textScale = canvas.scaleFactor;
                SharpText.Rebuild(transform);
            }
        }

        private void Refit()
        {
            lastScreen = new Vector2(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
            nextRefresh = Time.unscaledTime + RefreshInterval;

            if (fitter != null)
                fitter.Apply(lastScreen, false);
        }
    }
}
