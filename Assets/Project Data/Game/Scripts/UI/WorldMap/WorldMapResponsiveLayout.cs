using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Keeps the World Map authored as one fixed 1080x1920 design frame.
    /// This intentionally mirrors the stable Main Menu strategy: child UI is
    /// never rearranged at runtime; only the complete design root is uniformly
    /// scaled into the device safe area.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class WorldMapResponsiveLayout : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        [SerializeField] private bool fitInsideScreen = true;
        [SerializeField] private bool useSafeArea = true;
        [SerializeField] private Image backgroundArtwork;

        private RectTransform root;
        private Canvas canvas;
        private CanvasScaler scaler;
        private RectTransform fullscreenBackdrop;
        private Image fullscreenBackdropImage;

        private int lastWidth = -1;
        private int lastHeight = -1;
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);

        private void Awake()
        {
            root = transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;

            ConfigureCanvas();
            CreateFullscreenBackdrop();
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

            // Important: exactly the same deterministic approach that fixed the
            // menu Scene-view-vs-Play-mode layout mismatch.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
        }

        private void CreateFullscreenBackdrop()
        {
            if (canvas == null || root == null || backgroundArtwork == null || backgroundArtwork.sprite == null)
                return;

            Transform existing = canvas.transform.Find("__Fullscreen World Map Backdrop");
            GameObject backdropObject;

            if (existing != null)
            {
                backdropObject = existing.gameObject;
            }
            else
            {
                backdropObject = new GameObject(
                    "__Fullscreen World Map Backdrop",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                backdropObject.layer = gameObject.layer;
                backdropObject.transform.SetParent(canvas.transform, false);
            }

            fullscreenBackdrop = backdropObject.GetComponent<RectTransform>();
            fullscreenBackdropImage = backdropObject.GetComponent<Image>();

            fullscreenBackdrop.anchorMin = new Vector2(0.5f, 0.5f);
            fullscreenBackdrop.anchorMax = new Vector2(0.5f, 0.5f);
            fullscreenBackdrop.pivot = new Vector2(0.5f, 0.5f);
            fullscreenBackdrop.anchoredPosition = Vector2.zero;
            fullscreenBackdrop.localScale = Vector3.one;

            fullscreenBackdropImage.sprite = backgroundArtwork.sprite;
            fullscreenBackdropImage.color = backgroundArtwork.color;
            fullscreenBackdropImage.material = backgroundArtwork.material;
            fullscreenBackdropImage.raycastTarget = false;
            fullscreenBackdropImage.preserveAspect = false;

            fullscreenBackdrop.SetAsFirstSibling();
            ResizeFullscreenBackdrop();
        }

        private void ResizeFullscreenBackdrop()
        {
            if (fullscreenBackdrop == null ||
                fullscreenBackdropImage == null ||
                fullscreenBackdropImage.sprite == null ||
                canvas == null)
            {
                return;
            }

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Vector2 parentSize = canvasRect.rect.size;
            if (parentSize.x <= 0.01f || parentSize.y <= 0.01f)
                return;

            Rect spriteRect = fullscreenBackdropImage.sprite.rect;
            if (spriteRect.width <= 0.01f || spriteRect.height <= 0.01f)
                return;

            float imageAspect = spriteRect.width / spriteRect.height;
            float screenAspect = parentSize.x / parentSize.y;

            Vector2 coverSize;
            if (screenAspect > imageAspect)
            {
                coverSize.x = parentSize.x;
                coverSize.y = parentSize.x / imageAspect;
            }
            else
            {
                coverSize.y = parentSize.y;
                coverSize.x = parentSize.y * imageAspect;
            }

            fullscreenBackdrop.sizeDelta = coverSize;
            fullscreenBackdrop.anchoredPosition = Vector2.zero;
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

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = ReferenceResolution;

            Vector2 safeCenter = safe.center;
            Vector2 screenCenter = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);
            root.anchoredPosition = safeCenter - screenCenter;
            root.localScale = new Vector3(scale, scale, 1f);

            ResizeFullscreenBackdrop();

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }

        public void EditorConfigure(Image sourceBackground)
        {
            backgroundArtwork = sourceBackground;
        }
    }
}
