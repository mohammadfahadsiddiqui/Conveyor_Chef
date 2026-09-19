using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Responsive wrapper for the designer-authored menu in menu.unity.
    ///
    /// The scene layout is authored once at 1080x1920. At runtime we do NOT
    /// recalculate individual button/logo/chef positions. Instead, all authored
    /// content is placed inside one fixed design frame and that whole frame is
    /// uniformly fitted inside the current device Safe Area.
    ///
    /// Result:
    /// - the saved scene composition stays visually identical;
    /// - no button-to-button spacing drift;
    /// - no stretching of the chef/logo/UI art;
    /// - tall/wide/notched phones are handled automatically;
    /// - background still fills the complete screen.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class MainMenuResponsiveLayout : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private const string RuntimeContentName = "__Responsive Design Frame";

        [Header("Responsive Behaviour")]
        [SerializeField] private bool useSafeArea = true;
        [SerializeField] private bool keepModalFullScreen = true;
        [SerializeField] private bool preserveBackgroundAspect = true;

        private RectTransform root;
        private RectTransform designFrame;
        private RectTransform background;
        private RectTransform modalRoot;

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private Vector2 lastRootSize = new Vector2(-1f, -1f);

        private bool built;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Editor-time configuration is limited to CanvasScaler only.
            // Never touch the designer-authored child RectTransforms here.
            ConfigureCanvasScaler();
        }
#endif

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            root = transform as RectTransform;
            if (root == null)
                return;

            ConfigureCanvasScaler();
            BuildResponsiveFrame();
            ApplyResponsiveFit(true);
        }

        private void Update()
        {
            if (!Application.isPlaying || !built || root == null)
                return;

            Rect safe = GetTargetSafeArea();

            if (lastScreenWidth != Screen.width ||
                lastScreenHeight != Screen.height ||
                lastSafeArea != safe ||
                lastRootSize != root.rect.size)
            {
                ApplyResponsiveFit(false);
            }
        }

        private void ConfigureCanvasScaler()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void BuildResponsiveFrame()
        {
            if (built)
                return;

            background = FindDirectRect("Background Artwork");
            modalRoot = FindDirectRect("MainMenu Modal");

            Transform existing = transform.Find(RuntimeContentName);
            if (existing != null)
            {
                designFrame = existing as RectTransform;
            }
            else
            {
                GameObject frameObject = new GameObject(RuntimeContentName, typeof(RectTransform));
                frameObject.layer = gameObject.layer;

                designFrame = frameObject.GetComponent<RectTransform>();
                designFrame.SetParent(root, false);
            }

            designFrame.anchorMin = new Vector2(0.5f, 0.5f);
            designFrame.anchorMax = new Vector2(0.5f, 0.5f);
            designFrame.pivot = new Vector2(0.5f, 0.5f);
            designFrame.sizeDelta = ReferenceResolution;
            designFrame.anchoredPosition = Vector2.zero;
            designFrame.localRotation = Quaternion.identity;
            designFrame.localScale = Vector3.one;

            // Move only the authored menu composition into the fixed design frame.
            // Background remains full-screen. Modal can remain full-screen too.
            List<RectTransform> toMove = new List<RectTransform>();

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform child = root.GetChild(i) as RectTransform;
                if (child == null ||
                    child == designFrame ||
                    child == background ||
                    (keepModalFullScreen && child == modalRoot))
                {
                    continue;
                }

                toMove.Add(child);
            }

            foreach (RectTransform child in toMove)
            {
                RectState state = RectState.Capture(child);
                child.SetParent(designFrame, false);
                state.Restore(child);
            }

            ConfigureBackground();
            ConfigureModal();

            built = true;
        }

        private void ConfigureBackground()
        {
            if (background == null)
                return;

            background.SetAsFirstSibling();

            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = Vector2.zero;
            background.sizeDelta = Vector2.zero;
            background.localScale = Vector3.one;

            if (!preserveBackgroundAspect)
                return;

            Image image = background.GetComponent<Image>();
            if (image == null || image.sprite == null)
                return;

            AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = background.gameObject.AddComponent<AspectRatioFitter>();

            Rect spriteRect = image.sprite.rect;
            if (spriteRect.height > 0.01f)
                fitter.aspectRatio = spriteRect.width / spriteRect.height;

            // Cover the entire device screen. Extra image area is cropped rather
            // than stretching the artwork.
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            image.preserveAspect = false;
        }

        private void ConfigureModal()
        {
            if (!keepModalFullScreen || modalRoot == null)
                return;

            modalRoot.anchorMin = Vector2.zero;
            modalRoot.anchorMax = Vector2.one;
            modalRoot.pivot = new Vector2(0.5f, 0.5f);
            modalRoot.anchoredPosition = Vector2.zero;
            modalRoot.sizeDelta = Vector2.zero;
            modalRoot.localScale = Vector3.one;
            modalRoot.SetAsLastSibling();
        }

        private void ApplyResponsiveFit(bool force)
        {
            if (root == null || designFrame == null)
                return;

            Vector2 rootSize = root.rect.size;
            if (rootSize.x <= 0.01f || rootSize.y <= 0.01f)
                return;

            Rect safePixels = GetTargetSafeArea();

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            // Convert pixel safe area into this root RectTransform's local units.
            float safeWidth = rootSize.x * (safePixels.width / screenWidth);
            float safeHeight = rootSize.y * (safePixels.height / screenHeight);

            float uniformScale = Mathf.Min(
                safeWidth / ReferenceResolution.x,
                safeHeight / ReferenceResolution.y);

            uniformScale = Mathf.Max(0.0001f, uniformScale);

            Vector2 safeCenterNormalized = new Vector2(
                safePixels.center.x / screenWidth,
                safePixels.center.y / screenHeight);

            Vector2 localCenter = new Vector2(
                (safeCenterNormalized.x - 0.5f) * rootSize.x,
                (safeCenterNormalized.y - 0.5f) * rootSize.y);

            designFrame.anchorMin = new Vector2(0.5f, 0.5f);
            designFrame.anchorMax = new Vector2(0.5f, 0.5f);
            designFrame.pivot = new Vector2(0.5f, 0.5f);
            designFrame.sizeDelta = ReferenceResolution;
            designFrame.anchoredPosition = localCenter;
            designFrame.localScale = new Vector3(uniformScale, uniformScale, 1f);

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = safePixels;
            lastRootSize = rootSize;
        }

        private Rect GetTargetSafeArea()
        {
            if (!useSafeArea || Screen.width <= 0 || Screen.height <= 0)
                return new Rect(0f, 0f, Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));

            Rect safe = Screen.safeArea;

            // Some editor/simulator configurations can briefly report an empty
            // safe area while changing devices. Fall back to the full screen.
            if (safe.width <= 1f || safe.height <= 1f)
                return new Rect(0f, 0f, Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));

            return safe;
        }

        private RectTransform FindDirectRect(string objectName)
        {
            Transform child = transform.Find(objectName);
            return child as RectTransform;
        }

        private readonly struct RectState
        {
            private readonly Vector2 anchorMin;
            private readonly Vector2 anchorMax;
            private readonly Vector2 anchoredPosition;
            private readonly Vector2 sizeDelta;
            private readonly Vector2 pivot;
            private readonly Vector3 localScale;
            private readonly Quaternion localRotation;

            private RectState(
                Vector2 anchorMin,
                Vector2 anchorMax,
                Vector2 anchoredPosition,
                Vector2 sizeDelta,
                Vector2 pivot,
                Vector3 localScale,
                Quaternion localRotation)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.anchoredPosition = anchoredPosition;
                this.sizeDelta = sizeDelta;
                this.pivot = pivot;
                this.localScale = localScale;
                this.localRotation = localRotation;
            }

            public static RectState Capture(RectTransform rect)
            {
                return new RectState(
                    rect.anchorMin,
                    rect.anchorMax,
                    rect.anchoredPosition,
                    rect.sizeDelta,
                    rect.pivot,
                    rect.localScale,
                    rect.localRotation);
            }

            public void Restore(RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
                rect.pivot = pivot;
                rect.localScale = localScale;
                rect.localRotation = localRotation;
            }
        }
    }
}
