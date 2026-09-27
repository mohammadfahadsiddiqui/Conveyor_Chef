using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Root of the separate Game UI canvas (GAME MAIN CANVAS [NEW UI]).
    ///
    /// UIController only scans direct children of UI Main Canvas for pages. When this
    /// canvas is active, UIController registers the UIGame on this object instead and
    /// every other UIGame page in the scene is switched off at runtime only, so the
    /// old pages inside UI Main Canvas stay untouched in the saved scene.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(UIGame))]
    public sealed class GameMainCanvas : MonoBehaviour
    {
        [SerializeField] UIGame gamePage;
        [SerializeField] RectTransform safeArea;
        [SerializeField] RectTransform topBackplate;
        [SerializeField] float topBackplateHeight = 150f;

        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private Rect appliedSafeArea;

        public UIGame GamePage => gamePage;

        private void Awake()
        {
            CacheComponents();
            HideOtherGamePages();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != appliedSafeArea)
                ApplySafeArea();
        }

        /// <summary>
        /// Called by UIController.Initialise so this canvas uses the same camera and
        /// scaling as UI Main Canvas and draws just below it. Result/store/popup pages
        /// in UI Main Canvas therefore always appear above the gameplay HUD.
        /// </summary>
        public void AttachTo(Canvas mainCanvas, bool isTablet)
        {
            CacheComponents();

            if (mainCanvas != null)
            {
                canvas.renderMode = mainCanvas.renderMode;
                canvas.worldCamera = mainCanvas.worldCamera != null ? mainCanvas.worldCamera : Camera.main;
                canvas.planeDistance = mainCanvas.planeDistance;
                canvas.sortingLayerID = mainCanvas.sortingLayerID;
                canvas.sortingOrder = mainCanvas.sortingOrder - 1;
            }

            if (canvasScaler != null)
                canvasScaler.matchWidthOrHeight = isTablet ? 1f : 0f;

            ApplySafeArea();
        }

        private void CacheComponents()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            if (gamePage == null)
                gamePage = GetComponent<UIGame>();
        }

        private void HideOtherGamePages()
        {
            UIGame[] pages = FindObjectsByType<UIGame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != gamePage && pages[i].gameObject.scene == gameObject.scene)
                    pages[i].gameObject.SetActive(false);
            }
        }

        private void ApplySafeArea()
        {
            appliedSafeArea = Screen.safeArea;

            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            float topInset = Screen.height - appliedSafeArea.yMax;

            if (safeArea != null)
            {
                Vector2 min = appliedSafeArea.position;
                Vector2 max = appliedSafeArea.position + appliedSafeArea.size;

                safeArea.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
                safeArea.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
                safeArea.offsetMin = Vector2.zero;
                safeArea.offsetMax = Vector2.zero;
            }

            // The dark top bar sits outside the safe area so it still reaches the top
            // edge behind a notch or camera hole.
            if (topBackplate != null)
            {
                float insetInCanvasUnits = topInset / GetCanvasScale();

                topBackplate.sizeDelta = new Vector2(topBackplate.sizeDelta.x, topBackplateHeight + insetInCanvasUnits);
            }
        }

        // Same formula CanvasScaler uses, so it is valid before the scaler's first update.
        private float GetCanvasScale()
        {
            if (canvasScaler == null || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                return canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

            Vector2 reference = canvasScaler.referenceResolution;
            float logWidth = Mathf.Log(Screen.width / reference.x, 2f);
            float logHeight = Mathf.Log(Screen.height / reference.y, 2f);

            return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, canvasScaler.matchWidthOrHeight));
        }

#if UNITY_EDITOR
        public void EditorConfigure(UIGame page, RectTransform safeAreaRoot, RectTransform backplate, float backplateHeight)
        {
            gamePage = page;
            safeArea = safeAreaRoot;
            topBackplate = backplate;
            topBackplateHeight = backplateHeight;
        }
#endif
    }
}
