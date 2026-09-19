using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Keeps the exact designer-authored menu composition from menu.unity.
    ///
    /// The NEW Main Menu hierarchy is authored at 1080x1920. At runtime we never:
    /// - reparent its children,
    /// - move individual elements,
    /// - resize individual elements,
    /// - change anchors,
    /// - recalculate spacing.
    ///
    /// We only scale the whole NEW Main Menu root uniformly so the Simulator/device
    /// shows the same composition as the Scene view.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class MainMenuResponsiveLayout : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        [Header("Exact Scene Layout")]
        [SerializeField] private bool fitInsideScreen = true;
        [SerializeField] private bool useSafeArea = true;

        private RectTransform root;
        private Canvas canvas;
        private CanvasScaler scaler;

        private int lastWidth = -1;
        private int lastHeight = -1;
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);

        private void Awake()
        {
            root = transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;

            ConfigureCanvas();
            ApplyExactFit();
        }

        private void Start()
        {
            ApplyExactFit();
        }

        private void Update()
        {
            if (Screen.width != lastWidth ||
                Screen.height != lastHeight ||
                Screen.safeArea != lastSafeArea)
            {
                ApplyExactFit();
            }
        }

        private void ConfigureCanvas()
        {
            if (scaler == null)
                return;

            // Constant Pixel Size makes the relationship between our fixed
            // 1080x1920 design frame and the actual device deterministic.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
        }

        private void ApplyExactFit()
        {
            if (root == null)
                return;

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            Rect safe = useSafeArea
                ? Screen.safeArea
                : new Rect(0f, 0f, screenWidth, screenHeight);

            if (safe.width <= 1f || safe.height <= 1f)
                safe = new Rect(0f, 0f, screenWidth, screenHeight);

            float scale = 1f;

            if (fitInsideScreen)
            {
                scale = Mathf.Min(
                    safe.width / ReferenceResolution.x,
                    safe.height / ReferenceResolution.y);
            }

            // NEW Main Menu becomes one fixed reference frame. All authored child
            // RectTransforms keep exactly the values saved in menu.unity.
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = ReferenceResolution;

            // Position the whole design frame in the device safe area.
            Vector2 safeCenter = safe.center;
            Vector2 screenCenter = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);
            root.anchoredPosition = safeCenter - screenCenter;
            root.localScale = new Vector3(scale, scale, 1f);

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }
    }
}
