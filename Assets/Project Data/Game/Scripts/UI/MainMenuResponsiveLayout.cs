using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Responsive layout for the real UI hierarchy stored in menu.unity.
    /// Keeps the approved professional-menu composition automatically aligned
    /// for portrait phones/tablets and safe areas. No runtime UI is generated.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MainMenuResponsiveLayout : MonoBehaviour
    {
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        [Header("Automatic Layout")]
        [SerializeField] private bool applySafeArea = true;
        [SerializeField, Range(0.75f, 1.15f)] private float minimumVisualScale = 0.82f;
        [SerializeField, Range(0.9f, 1.3f)] private float maximumVisualScale = 1.08f;

        private RectTransform root;
        private RectTransform background;
        private RectTransform topHud;
        private RectTransform chefAvatar;
        private RectTransform playerNamePlate;
        private RectTransform starCounter;
        private RectTransform coinCounter;
        private RectTransform diamondCounter;
        private RectTransform gameLogo;
        private RectTransform chefCharacter;
        private RectTransform mainButtons;
        private RectTransform playButton;
        private RectTransform storyButton;
        private RectTransform challengesButton;
        private RectTransform customizeButton;
        private RectTransform settingsButton;
        private RectTransform bottomNavigation;
        private RectTransform shopButton;
        private RectTransform collectionButton;
        private RectTransform achievementsButton;
        private RectTransform leaderboardButton;
        private RectTransform modal;
        private RectTransform modalPanel;

        private Vector2 lastRootSize = new Vector2(-1f, -1f);
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);

        private void OnEnable()
        {
            Cache();
            ConfigureCanvasScaler();
            ApplyLayout();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            Cache();
            ConfigureCanvasScaler();
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            ApplyLayout();
        }

        private void Update()
        {
            if (root == null)
                Cache();

            if (root == null)
                return;

            Rect safeArea = CurrentSafeArea();
            if (lastRootSize != root.rect.size || lastSafeArea != safeArea)
                ApplyLayout();
        }

        [ContextMenu("Apply Responsive Layout")]
        public void ApplyLayout()
        {
            if (root == null)
                Cache();

            if (root == null)
                return;

            Vector2 rootSize = root.rect.size;
            if (rootSize.x <= 1f || rootSize.y <= 1f)
                return;

            Rect safeArea = CurrentSafeArea();
            lastRootSize = rootSize;
            lastSafeArea = safeArea;

            float visualScale = Mathf.Clamp(
                Mathf.Min(rootSize.x / ReferenceWidth, rootSize.y / ReferenceHeight),
                minimumVisualScale,
                maximumVisualScale);

            // CanvasScaler already handles most resolution scaling. Keep the approved
            // visual scale close to 1 and only compress on unusually narrow canvases.
            float widthCompression = Mathf.Clamp(rootSize.x / ReferenceWidth, 0.78f, 1f);
            visualScale = Mathf.Min(visualScale, Mathf.Lerp(0.88f, 1f, widthCompression));

            float topInset = 0f;
            float bottomInset = 0f;

            if (applySafeArea && Screen.width > 0 && Screen.height > 0)
            {
                topInset = Mathf.Max(0f, Screen.height - safeArea.yMax) / Screen.height * rootSize.y;
                bottomInset = Mathf.Max(0f, safeArea.yMin) / Screen.height * rootSize.y;
            }

            // Root/background always cover the full Canvas. Safe area affects content,
            // never the artwork backdrop.
            Stretch(root);
            Stretch(background);

            LayoutTopHud(rootSize.x, visualScale, topInset);
            LayoutHero(visualScale, topInset, bottomInset);
            LayoutMainButtons(visualScale, topInset);
            LayoutBottomNavigation(rootSize.x, visualScale, bottomInset);
            LayoutModal(rootSize, visualScale);
        }

        private void LayoutTopHud(float width, float scale, float topInset)
        {
            if (topHud == null)
                return;

            SetRect(topHud,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -(24f + topInset)),
                new Vector2(0f, 116f * scale),
                new Vector2(0.5f, 1f));

            // Use normalized horizontal anchors so all five HUD regions keep their
            // spacing automatically when the screen gets wider/narrower.
            SetPoint(chefAvatar, new Vector2(0.045f, 0.5f), Vector2.zero,
                new Vector2(92f, 92f) * scale, new Vector2(0.5f, 0.5f));

            SetPoint(playerNamePlate, new Vector2(0.17f, 0.5f), Vector2.zero,
                new Vector2(170f, 66f) * scale, new Vector2(0.5f, 0.5f));

            SetPoint(starCounter, new Vector2(0.43f, 0.5f), Vector2.zero,
                new Vector2(155f, 70f) * scale, new Vector2(0.5f, 0.5f));

            SetPoint(coinCounter, new Vector2(0.66f, 0.5f), Vector2.zero,
                new Vector2(270f, 96f) * scale, new Vector2(0.5f, 0.5f));

            SetPoint(diamondCounter, new Vector2(0.865f, 0.5f), Vector2.zero,
                new Vector2(270f, 96f) * scale, new Vector2(0.5f, 0.5f));
        }

        private void LayoutHero(float scale, float topInset, float bottomInset)
        {
            // Approved composition: logo over the upper-middle/right area,
            // chef character occupying the left half.
            SetPoint(gameLogo,
                new Vector2(0.5f, 1f),
                new Vector2(145f * scale, -(280f * scale + topInset)),
                new Vector2(565f, 430f) * scale,
                new Vector2(0.5f, 0.5f));

            SetPoint(chefCharacter,
                new Vector2(0f, 0f),
                new Vector2(285f * scale, 590f * scale + bottomInset * 0.35f),
                new Vector2(500f, 720f) * scale,
                new Vector2(0.5f, 0.5f));
        }

        private void LayoutMainButtons(float scale, float topInset)
        {
            if (mainButtons == null)
                return;

            SetPoint(mainButtons,
                new Vector2(1f, 1f),
                new Vector2(-225f * scale, -(610f * scale + topInset)),
                new Vector2(460f, 800f) * scale,
                new Vector2(0.5f, 1f));

            float gap = 150f * scale;

            SetPoint(playButton, new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                new Vector2(440f, 145f) * scale, new Vector2(0.5f, 1f));

            SetPoint(storyButton, new Vector2(0.5f, 1f), new Vector2(0f, -gap),
                new Vector2(420f, 138f) * scale, new Vector2(0.5f, 1f));

            SetPoint(challengesButton, new Vector2(0.5f, 1f), new Vector2(0f, -gap * 2f),
                new Vector2(420f, 138f) * scale, new Vector2(0.5f, 1f));

            SetPoint(customizeButton, new Vector2(0.5f, 1f), new Vector2(0f, -gap * 3f),
                new Vector2(420f, 138f) * scale, new Vector2(0.5f, 1f));

            SetPoint(settingsButton, new Vector2(0.5f, 1f), new Vector2(0f, -gap * 4f),
                new Vector2(420f, 138f) * scale, new Vector2(0.5f, 1f));
        }

        private void LayoutBottomNavigation(float width, float scale, float bottomInset)
        {
            if (bottomNavigation == null)
                return;

            float navFitScale = Mathf.Min(1f, Mathf.Max(0.72f, (width - 36f) / 960f));
            float navScale = scale * navFitScale;

            SetPoint(bottomNavigation,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 18f + bottomInset),
                new Vector2(960f, 235f) * navScale,
                new Vector2(0.5f, 0f));

            // Relative positions inside the navigation holder keep spacing even.
            SetPoint(shopButton, new Vector2(0.14f, 0f), new Vector2(0f, 2f),
                new Vector2(205f, 205f) * navScale, new Vector2(0.5f, 0f));

            SetPoint(collectionButton, new Vector2(0.38f, 0f), new Vector2(0f, 2f),
                new Vector2(205f, 205f) * navScale, new Vector2(0.5f, 0f));

            SetPoint(achievementsButton, new Vector2(0.62f, 0f), new Vector2(0f, 2f),
                new Vector2(205f, 205f) * navScale, new Vector2(0.5f, 0f));

            SetPoint(leaderboardButton, new Vector2(0.86f, 0f), new Vector2(0f, 2f),
                new Vector2(205f, 205f) * navScale, new Vector2(0.5f, 0f));
        }

        private void LayoutModal(Vector2 rootSize, float scale)
        {
            Stretch(modal);

            if (modalPanel == null)
                return;

            float fit = Mathf.Min(
                1f,
                (rootSize.x - 80f) / 850f,
                (rootSize.y - 180f) / 1040f);

            float modalScale = Mathf.Clamp(scale * fit, 0.72f, 1f);

            SetPoint(modalPanel,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(850f, 1040f) * modalScale,
                new Vector2(0.5f, 0.5f));
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
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void Cache()
        {
            root = transform as RectTransform;
            if (root == null)
                return;

            background = FindRect("Background Artwork");
            topHud = FindRect("Top HUD");
            chefAvatar = FindRect("Chef Avatar");
            playerNamePlate = FindRect("Player Name Plate");
            starCounter = FindRect("Star Counter");
            coinCounter = FindRect("Coin Counter");
            diamondCounter = FindRect("Diamond Counter");

            gameLogo = FindRect("Game Logo");
            chefCharacter = FindRect("Chef Character");

            mainButtons = FindRect("Main Buttons");
            playButton = FindRect("PLAY");
            storyButton = FindRect("STORY");
            challengesButton = FindRect("CHALLENGES");
            customizeButton = FindRect("CUSTOMIZE");
            settingsButton = FindRect("SETTINGS");

            bottomNavigation = FindRect("Bottom Navigation");
            shopButton = FindRect("SHOP");
            collectionButton = FindRect("COLLECTION");
            achievementsButton = FindRect("ACHIEVEMENTS");
            leaderboardButton = FindRect("LEADERBOARD");

            modal = FindRect("MainMenu Modal");
            modalPanel = modal != null ? FindRectUnder(modal, "Panel") : null;
        }

        private RectTransform FindRect(string objectName)
        {
            return FindRectUnder(root, objectName);
        }

        private static RectTransform FindRectUnder(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            if (string.Equals(parent.name, objectName, StringComparison.Ordinal))
                return parent as RectTransform;

            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform found = FindRectUnder(parent.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetPoint(
            RectTransform rect,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static Rect CurrentSafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return new Rect(0f, 0f, 1f, 1f);

            return Screen.safeArea;
        }
    }
}
