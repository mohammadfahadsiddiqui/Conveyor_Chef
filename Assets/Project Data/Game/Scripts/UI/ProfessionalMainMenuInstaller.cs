using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BusStop;
using Watermelon.IAPStore;
using Watermelon.SkinStore;

namespace Watermelon
{
    /// <summary>
    /// Installs the production Conveyor Chef main menu whenever menu.unity is loaded.
    /// The UI is built from Resources/ProfessionalMainMenu so the menu stays responsive
    /// and does not depend on fragile scene YAML references.
    /// </summary>
    public sealed class ProfessionalMainMenuInstaller : MonoBehaviour
    {
        private const string MenuSceneName = "menu";
        private const string ResourceRoot = "ProfessionalMainMenu/";
        private const string DiamondsKey = "CC_Diamonds";
        private const string PlayerNameKey = "CC_PlayerName";
        private const string LocalBestScoreKey = "CC_LocalBestScore";

        private static bool bootstrapInstalled;

        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private ProfessionalMainMenuAssetCatalog assetCatalog;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform safeArea;
        private GameObject menuRoot;
        private CanvasGroup menuCanvasGroup;

        private RectTransform logoRect;
        private RectTransform chefRect;
        private Vector2 logoBasePosition;
        private Vector2 chefBasePosition;
        private Vector3 chefBaseScale;

        private TextMeshProUGUI starText;
        private TextMeshProUGUI coinText;
        private TextMeshProUGUI diamondText;

        private GameObject modalRoot;
        private TextMeshProUGUI modalTitle;
        private TextMeshProUGUI modalBody;
        private GameObject settingsControls;
        private TextMeshProUGUI soundButtonText;
        private TextMeshProUGUI vibrationButtonText;

        private Type externalPageType;
        private bool externalPageSubscribed;
        private bool introFinished;
        private bool animateGeneratedUI = true;
        private Rect lastSafeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallBootstrap()
        {
            if (bootstrapInstalled)
                return;

            bootstrapInstalled = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, MenuSceneName, StringComparison.OrdinalIgnoreCase))
                return;

            // menu.unity now owns a real serialized, editable menu hierarchy.
            // Never create the old runtime-generated menu when that controller exists.
            if (FindFirstObjectByType<MainMenuSceneController>(FindObjectsInactive.Include) != null)
                return;

            if (FindFirstObjectByType<ProfessionalMainMenuInstaller>(FindObjectsInactive.Include) != null)
                return;

            GameObject installerObject = new GameObject("Professional Main Menu");
            installerObject.AddComponent<ProfessionalMainMenuInstaller>();
        }

        private void Awake()
        {
            // If the real scene-based menu exists, this object is obsolete.
            // Disable it before Start() so it cannot create, move, animate or
            // otherwise alter the menu the designer saved in menu.unity.
            MainMenuSceneController sceneMenu =
                FindFirstObjectByType<MainMenuSceneController>(FindObjectsInactive.Include);

            if (sceneMenu != null && sceneMenu.gameObject != gameObject)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }

            // Legacy fallback only for old menu scenes that do not contain the
            // scene-based MainMenuSceneController.
            EnsureEventSystem();
            DisableLegacyMainMenu();
        }

        private IEnumerator Start()
        {
            // Let the Watermelon UI system finish creating its pages first.
            yield return null;
            yield return null;

            EnsureEventSystem();
            EnsureSaveControllerReady();

            assetCatalog = Resources.Load<ProfessionalMainMenuAssetCatalog>("ProfessionalMainMenuAssets");
            if (assetCatalog == null)
                Debug.LogError("[ProfessionalMainMenu] ProfessionalMainMenuAssets catalog is missing.");

            DisableLegacyMainMenu();

            // Prefer the real UI hierarchy baked into menu.unity. Runtime generation
            // remains only as a safe fallback for older project copies.
            if (!BindExistingSceneMenu())
            {
                BuildMenu(true);
            }

            RefreshHUD();

            CurrenciesController.InvokeOrSubcrtibe(() =>
            {
                RefreshHUD();
                CurrenciesController.SubscribeGlobalCallback(OnCurrencyChanged);
            });

            StartCoroutine(PlayIntro());
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
                return;

            try
            {
                CurrenciesController.UnsubscribeGlobalCallback(OnCurrencyChanged);
            }
            catch
            {
                // Currency module may already be shutting down.
            }

            UnsubscribeExternalPage();
        }

        private void Update()
        {
            if (canvas == null)
                return;

            if (Screen.safeArea != lastSafeArea)
                ApplySafeArea();

            if (!introFinished)
                return;

            float t = Time.unscaledTime;

            if (logoRect != null)
            {
                logoRect.anchoredPosition = logoBasePosition + Vector2.up * (Mathf.Sin(t * 1.35f) * 8f);
                logoRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * 0.6f);
            }

            if (chefRect != null)
            {
                chefRect.anchoredPosition = chefBasePosition + Vector2.up * (Mathf.Sin(t * 1.05f + 0.7f) * 5f);
                float breathe = 1f + Mathf.Sin(t * 1.6f) * 0.008f;
                chefRect.localScale = chefBaseScale * breathe;
            }

            // Modal close is handled by the visible CLOSE button. Avoid the legacy
            // UnityEngine.Input API here because this project uses the Input System package.
        }

        private void EnsureSaveControllerReady()
        {
            if (SaveController.IsSaveLoaded)
                return;

            try
            {
                // GameController previously initialised saving only after Game.unity loaded.
                // The professional main menu needs LevelSave data earlier in menu.unity.
                SaveController.Initialise(useAutoSave: false);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ProfessionalMainMenu] Failed to initialise SaveController: " + ex.Message);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null && EventSystem.current.gameObject.activeInHierarchy)
                return;

            EventSystem existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            Debug.LogWarning("[ProfessionalMainMenu] No EventSystem was found in menu.unity.");
        }

        private void DisableLegacyMainMenu()
        {
            UIMainMenu legacyMenu = FindFirstObjectByType<UIMainMenu>(FindObjectsInactive.Include);
            if (legacyMenu != null)
            {
                legacyMenu.gameObject.SetActive(false);
            }

            // The previous scene revision also contained scooter/loading-style decoration.
            // Disable only the known legacy visual names so unrelated systems remain untouched.
            string[] legacyNames = { "scooter", "map", "shadow" };
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Transform tr in transforms)
            {
                if (tr == null || tr.gameObject == gameObject)
                    continue;

                for (int i = 0; i < legacyNames.Length; i++)
                {
                    if (string.Equals(tr.name, legacyNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        tr.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        private bool BindExistingSceneMenu()
        {
            Transform bakedCanvas = null;

            // Preferred saved-scene structure: this installer is attached directly
            // to the top-level professional Canvas, exactly like loading.unity.
            if (string.Equals(gameObject.name, "ConveyorChef_MainMenu_Canvas", StringComparison.Ordinal))
            {
                bakedCanvas = transform;
            }
            else
            {
                bakedCanvas = transform.Find("ConveyorChef_MainMenu_Canvas");
            }

            if (bakedCanvas == null)
                return false;

            canvas = bakedCanvas.GetComponent<Canvas>();
            canvasRect = bakedCanvas as RectTransform;
            if (canvas == null || canvasRect == null)
                return false;

            Transform safe = bakedCanvas.Find("SafeArea");
            if (safe == null)
                return false;

            safeArea = safe as RectTransform;
            Transform root = safe.Find("MainMenuContent");
            if (root == null)
                return false;

            menuRoot = root.gameObject;
            menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();

            logoRect = FindDeepChild(root, "Conveyor Chef Logo") as RectTransform;
            chefRect = FindDeepChild(root, "Chef Character") as RectTransform;

            if (logoRect != null)
                logoBasePosition = logoRect.anchoredPosition;

            if (chefRect != null)
            {
                chefBasePosition = chefRect.anchoredPosition;
                chefBaseScale = chefRect.localScale;
            }

            Transform starValue = FindDeepChild(root, "Star Value");
            Transform coinValue = FindDeepChild(root, "Coin Value");
            Transform diamondValue = FindDeepChild(root, "Diamond Value");

            starText = starValue != null ? starValue.GetComponent<TextMeshProUGUI>() : null;
            coinText = coinValue != null ? coinValue.GetComponent<TextMeshProUGUI>() : null;
            diamondText = diamondValue != null ? diamondValue.GetComponent<TextMeshProUGUI>() : null;

            Transform modal = bakedCanvas.Find("MainMenu Modal");
            if (modal != null)
            {
                modalRoot = modal.gameObject;

                Transform title = FindDeepChild(modal, "Title");
                Transform body = FindDeepChild(modal, "Body");
                Transform controls = FindDeepChild(modal, "Settings Controls");

                modalTitle = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
                modalBody = body != null ? body.GetComponent<TextMeshProUGUI>() : null;
                settingsControls = controls != null ? controls.gameObject : null;

                Transform soundLabel = FindDeepChild(modal, "SOUND Button");
                Transform vibrationLabel = FindDeepChild(modal, "VIBRATION Button");
                soundButtonText = soundLabel != null ? soundLabel.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                vibrationButtonText = vibrationLabel != null ? vibrationLabel.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            }

            WireExistingButton(root, "PLAY", PlayGame, true, true);
            WireExistingButton(root, "STORY", OpenStory, false, true);
            WireExistingButton(root, "CHALLENGES", OpenChallenges, false, true);
            WireExistingButton(root, "CUSTOMIZE", OpenCustomize, false, true);
            WireExistingButton(root, "SETTINGS", OpenSettings, false, true);

            WireExistingButton(root, "Shop", OpenShop, false, false);
            WireExistingButton(root, "Collection", OpenCollection, false, false);
            WireExistingButton(root, "Achievements", OpenAchievements, false, false);
            WireExistingButton(root, "Leaderboard", OpenLeaderboard, false, false);
            WireExistingButton(root, "Coin Plus Hitbox", OpenShop, false, false);
            WireExistingButton(root, "Diamond Plus Hitbox", OpenShop, false, false);

            if (modal != null)
            {
                WireExistingButton(modal, "CLOSE Button", HideModal, false, false);
                WireExistingButton(modal, "SOUND Button", ToggleSound, false, true);
                WireExistingButton(modal, "VIBRATION Button", ToggleVibration, false, true);
            }

            ApplySafeArea();
            return true;
        }

        private void WireExistingButton(Transform root, string objectName, UnityEngine.Events.UnityAction action, bool pulse, bool shine)
        {
            Transform target = FindDeepChild(root, objectName);
            if (target == null)
                return;

            Button button = target.GetComponent<Button>();
            if (button == null)
                return;

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            ProfessionalMainMenuButtonFX fx = target.GetComponent<ProfessionalMainMenuButtonFX>();
            if (fx == null)
                fx = target.gameObject.AddComponent<ProfessionalMainMenuButtonFX>();

            fx.Configure(pulse, shine);
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                    return child;

                Transform nested = FindDeepChild(child, objectName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

#if UNITY_EDITOR
        public void RebuildSceneMenuForEditor()
        {
            // Clear only previously generated professional-menu children.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            gameObject.name = "ConveyorChef_MainMenu_Canvas";

            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                // A GameObject cannot swap Transform -> RectTransform in place,
                // so the editor baker is responsible for creating this object
                // with a RectTransform before this method is called.
                Debug.LogError("[ProfessionalMainMenu] Scene baker must create the professional menu root as a RectTransform.");
                return;
            }

            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = gameObject.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = rootRect;
            canvasRect.localScale = Vector3.one;

            spriteCache.Clear();
            assetCatalog = Resources.Load<ProfessionalMainMenuAssetCatalog>("ProfessionalMainMenuAssets");

            BuildMenuContents(false);

            gameObject.hideFlags = HideFlags.None;
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        private void BuildMenu(bool animate)
        {
            animateGeneratedUI = animate;

            GameObject canvasObject = new GameObject("ConveyorChef_MainMenu_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasObject.GetComponent<RectTransform>();

            BuildMenuContents(animate);
        }

        private void BuildMenuContents(bool animate)
        {
            animateGeneratedUI = animate;

            // Full-screen background sits outside SafeArea so there are never black notch bars.
            Image background = CreateImage("Background", canvasRect, LoadSprite("background"), false);
            Stretch(background.rectTransform);
            background.preserveAspect = false;
            background.raycastTarget = false;

            GameObject shadeObject = new GameObject("Background Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.SetParent(canvasRect, false);
            Stretch(shadeRect);
            Image shade = shadeObject.GetComponent<Image>();
            shade.color = new Color(0.01f, 0.035f, 0.08f, 0.08f);
            shade.raycastTarget = false;

            GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform));
            safeArea = safeObject.GetComponent<RectTransform>();
            safeArea.SetParent(canvasRect, false);
            ApplySafeArea();

            menuRoot = new GameObject("MainMenuContent", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform menuRootRect = menuRoot.GetComponent<RectTransform>();
            menuRootRect.SetParent(safeArea, false);
            Stretch(menuRootRect);
            menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();

            BuildTopHUD(menuRootRect);
            BuildHero(menuRootRect);
            BuildActionButtons(menuRootRect);
            BuildBottomNavigation(menuRootRect);
            BuildModal(canvasRect);
        }

        private void BuildTopHUD(RectTransform parent)
        {
            RectTransform hud = NewRect("Top HUD", parent);
            hud.anchorMin = new Vector2(0f, 1f);
            hud.anchorMax = new Vector2(1f, 1f);
            hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = new Vector2(0f, -24f);
            hud.sizeDelta = new Vector2(0f, 116f);

            // Profile
            Image avatar = CreateImage("Chef Avatar", hud, LoadSprite("avatar"), true);
            SetRect(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(74f, 0f), new Vector2(92f, 92f), new Vector2(0f, 0.5f));

            Image profilePlate = CreateSolidImage("Profile Plate", hud, new Color(0.015f, 0.13f, 0.29f, 0.88f));
            SetRect(profilePlate.rectTransform, new Vector2(0f, 0.5f), new Vector2(199f, 0f), new Vector2(155f, 64f), new Vector2(0.5f, 0.5f));
            AddShadow(profilePlate.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -4f));

            TextMeshProUGUI playerName = CreateText("Player Name", profilePlate.rectTransform, PlayerPrefs.GetString(PlayerNameKey, "Chef"), 34f, TextAlignmentOptions.Center);
            Stretch(playerName.rectTransform);
            playerName.margin = new Vector4(8f, 2f, 8f, 2f);

            // Star total
            Image starPlate = CreateSolidImage("Star Plate", hud, new Color(0.18f, 0.11f, 0.035f, 0.9f));
            SetRect(starPlate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), new Vector2(155f, 68f), new Vector2(0.5f, 0.5f));
            AddShadow(starPlate.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -4f));

            Image star = CreateImage("Star", starPlate.rectTransform, LoadSprite("star"), true);
            SetRect(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(35f, 0f), new Vector2(64f, 64f), new Vector2(0.5f, 0.5f));

            starText = CreateText("Star Value", starPlate.rectTransform, "0", 34f, TextAlignmentOptions.Center);
            SetRect(starText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(38f, 0f), new Vector2(74f, 54f), new Vector2(0.5f, 0.5f));

            // Coins
            bool customCoinBar = HasGeneratedResource("coin_bar");
            Image coinBar = CreateImage("Coin Counter", hud, LoadSprite("coin_bar"), false);
            SetRect(coinBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(55f, 0f), new Vector2(270f, 96f), new Vector2(0.5f, 0.5f));
            coinText = CreateText("Coin Value", coinBar.rectTransform, "0", 33f, TextAlignmentOptions.Center);
            SetRect(coinText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(120f, 52f), new Vector2(0.5f, 0.5f));
            if (!customCoinBar)
                AddFallbackCounterDecor(coinBar.rectTransform, false);
            AddInvisibleButton("Coin Plus Hitbox", coinBar.rectTransform, new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(74f, 76f), OpenShop);

            // Diamonds
            bool customDiamondBar = HasGeneratedResource("diamond_bar");
            Image diamondBar = CreateImage("Diamond Counter", hud, LoadSprite("diamond_bar"), false);
            SetRect(diamondBar.rectTransform, new Vector2(1f, 0.5f), new Vector2(-155f, 0f), new Vector2(270f, 96f), new Vector2(0.5f, 0.5f));
            diamondText = CreateText("Diamond Value", diamondBar.rectTransform, "50", 33f, TextAlignmentOptions.Center);
            SetRect(diamondText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(118f, 52f), new Vector2(0.5f, 0.5f));
            if (!customDiamondBar)
                AddFallbackCounterDecor(diamondBar.rectTransform, true);
            AddInvisibleButton("Diamond Plus Hitbox", diamondBar.rectTransform, new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(74f, 76f), OpenShop);
        }

        private void BuildHero(RectTransform parent)
        {
            Image logo = CreateImage("Conveyor Chef Logo", parent, LoadSprite("logo"), true);
            logoRect = logo.rectTransform;
            SetRect(logoRect, new Vector2(0.5f, 1f), new Vector2(145f, -280f), new Vector2(565f, 430f), new Vector2(0.5f, 0.5f));
            logoBasePosition = logoRect.anchoredPosition;
            logo.raycastTarget = false;

            Image chef = CreateImage("Chef Character", parent, LoadSprite("chef"), true);
            chefRect = chef.rectTransform;
            SetRect(chefRect, new Vector2(0f, 0f), new Vector2(285f, 590f), new Vector2(500f, 720f), new Vector2(0.5f, 0.5f));
            chefBasePosition = chefRect.anchoredPosition;
            chefBaseScale = Vector3.one;
            chef.raycastTarget = false;
        }

        private void BuildActionButtons(RectTransform parent)
        {
            float x = -225f;
            float firstY = -610f;
            float spacing = 150f;

            CreateMenuButton("PLAY", parent, "play", new Vector2(1f, 1f), new Vector2(x, firstY), new Vector2(440f, 145f), PlayGame, true, 0.06f);
            CreateMenuButton("STORY", parent, "story", new Vector2(1f, 1f), new Vector2(x, firstY - spacing), new Vector2(420f, 138f), OpenStory, false, 0.12f);
            CreateMenuButton("CHALLENGES", parent, "challenges", new Vector2(1f, 1f), new Vector2(x, firstY - spacing * 2f), new Vector2(420f, 138f), OpenChallenges, false, 0.18f);
            CreateMenuButton("CUSTOMIZE", parent, "customize", new Vector2(1f, 1f), new Vector2(x, firstY - spacing * 3f), new Vector2(420f, 138f), OpenCustomize, false, 0.24f);
            CreateMenuButton("SETTINGS", parent, "settings", new Vector2(1f, 1f), new Vector2(x, firstY - spacing * 4f), new Vector2(420f, 138f), OpenSettings, false, 0.30f);
        }

        private void BuildBottomNavigation(RectTransform parent)
        {
            RectTransform nav = NewRect("Bottom Navigation", parent);
            nav.anchorMin = new Vector2(0.5f, 0f);
            nav.anchorMax = new Vector2(0.5f, 0f);
            nav.pivot = new Vector2(0.5f, 0f);
            nav.anchoredPosition = new Vector2(0f, 18f);
            nav.sizeDelta = new Vector2(960f, 235f);

            float[] xs = { -345f, -115f, 115f, 345f };
            CreateBottomButton("Shop", nav, "shop", xs[0], OpenShop, 0.36f);
            CreateBottomButton("Collection", nav, "collection", xs[1], OpenCollection, 0.42f);
            CreateBottomButton("Achievements", nav, "achievements", xs[2], OpenAchievements, 0.48f);
            CreateBottomButton("Leaderboard", nav, "leaderboard", xs[3], OpenLeaderboard, 0.54f);
        }

        private void BuildModal(RectTransform parent)
        {
            modalRoot = new GameObject("MainMenu Modal", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.SetParent(parent, false);
            Stretch(modalRect);

            Image blocker = CreateSolidImage("Dim", modalRect, new Color(0f, 0f, 0f, 0.72f));
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;

            Image panel = CreateSolidImage("Panel", modalRect, new Color(0.025f, 0.09f, 0.18f, 0.98f));
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 1040f), new Vector2(0.5f, 0.5f));
            AddShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -12f));

            modalTitle = CreateText("Title", panel.rectTransform, "TITLE", 58f, TextAlignmentOptions.Center);
            SetRect(modalTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(680f, 100f), new Vector2(0.5f, 0.5f));
            modalTitle.color = new Color(1f, 0.79f, 0.13f);

            modalBody = CreateText("Body", panel.rectTransform, string.Empty, 34f, TextAlignmentOptions.TopLeft);
            SetRect(modalBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(690f, 660f), new Vector2(0.5f, 0.5f));
            modalBody.enableAutoSizing = true;
            modalBody.fontSizeMin = 24f;
            modalBody.fontSizeMax = 34f;
            modalBody.margin = new Vector4(16f, 16f, 16f, 16f);

            Button close = CreateTextButton("CLOSE", panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(300f, 92f), new Color(0.05f, 0.46f, 0.95f, 1f), HideModal);
            close.gameObject.AddComponent<ProfessionalMainMenuButtonFX>().Configure(false, false);

            settingsControls = new GameObject("Settings Controls", typeof(RectTransform));
            RectTransform settingsRect = settingsControls.GetComponent<RectTransform>();
            settingsRect.SetParent(panel.rectTransform, false);
            SetRect(settingsRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(620f, 360f), new Vector2(0.5f, 0.5f));

            Button soundButton = CreateTextButton("SOUND", settingsRect, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(520f, 110f), new Color(0.04f, 0.55f, 0.95f, 1f), ToggleSound);
            soundButtonText = soundButton.GetComponentInChildren<TextMeshProUGUI>();
            soundButton.gameObject.AddComponent<ProfessionalMainMenuButtonFX>().Configure(false, true);

            Button vibrationButton = CreateTextButton("VIBRATION", settingsRect, new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(520f, 110f), new Color(0.08f, 0.65f, 0.35f, 1f), ToggleVibration);
            vibrationButtonText = vibrationButton.GetComponentInChildren<TextMeshProUGUI>();
            vibrationButton.gameObject.AddComponent<ProfessionalMainMenuButtonFX>().Configure(false, true);

            modalRoot.SetActive(false);
        }

        private void CreateMenuButton(string objectName, RectTransform parent, string resourceName, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool pulse, float introDelay)
        {
            bool usingGeneratedArt = HasGeneratedResource(resourceName);
            Image image = CreateImage(objectName, parent, LoadSprite(resourceName), true);
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));

            if (!usingGeneratedArt)
            {
                TextMeshProUGUI label = CreateText("Label", image.rectTransform, objectName, objectName == "PLAY" ? 48f : 38f, TextAlignmentOptions.Center);
                Stretch(label.rectTransform);
                label.margin = new Vector4(24f, 8f, 24f, 8f);
            }

            // CreateImage() defaults to raycastTarget=false for decorative art.
            // These images are the actual hit targets, so they must receive UI raycasts.
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = true;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);

            ProfessionalMainMenuButtonFX fx = image.gameObject.AddComponent<ProfessionalMainMenuButtonFX>();
            fx.Configure(pulse, true);

            CanvasGroup cg = image.gameObject.AddComponent<CanvasGroup>();
            if (animateGeneratedUI)
            {
                cg.alpha = 0f;
                StartCoroutine(AnimateButtonIn(image.rectTransform, cg, position, introDelay));
            }
            else
            {
                cg.alpha = 1f;
                image.rectTransform.anchoredPosition = position;
            }
        }

        private void CreateBottomButton(string objectName, RectTransform parent, string resourceName, float x, UnityEngine.Events.UnityAction action, float introDelay)
        {
            bool usingGeneratedArt = HasGeneratedResource(resourceName);
            Image image = CreateImage(objectName, parent, LoadSprite(resourceName), true);
            SetRect(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, 2f), new Vector2(205f, 205f), new Vector2(0.5f, 0f));

            if (!usingGeneratedArt)
            {
                TextMeshProUGUI label = CreateText("Label", image.rectTransform, objectName.ToUpperInvariant(), 23f, TextAlignmentOptions.Bottom);
                Stretch(label.rectTransform);
                label.margin = new Vector4(5f, 5f, 5f, 14f);
            }

            // CreateImage() defaults to raycastTarget=false for decorative art.
            // These images are the actual hit targets, so they must receive UI raycasts.
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = true;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);

            image.gameObject.AddComponent<ProfessionalMainMenuButtonFX>().Configure(false, false);

            CanvasGroup cg = image.gameObject.AddComponent<CanvasGroup>();
            if (animateGeneratedUI)
            {
                cg.alpha = 0f;
                StartCoroutine(AnimateBottomIn(image.rectTransform, cg, introDelay));
            }
            else
            {
                cg.alpha = 1f;
            }
        }

        private IEnumerator PlayIntro()
        {
            menuCanvasGroup.alpha = 0f;

            Vector2 logoTarget = logoBasePosition;
            Vector2 chefTarget = chefBasePosition;

            logoRect.localScale = Vector3.one * 0.78f;
            chefRect.anchoredPosition = chefTarget + Vector2.left * 150f;

            float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - n, 3f);

                menuCanvasGroup.alpha = n;
                logoRect.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, eased);
                chefRect.anchoredPosition = Vector2.Lerp(chefTarget + Vector2.left * 150f, chefTarget, eased);
                yield return null;
            }

            logoRect.localScale = Vector3.one;
            chefRect.anchoredPosition = chefTarget;
            logoBasePosition = logoTarget;
            chefBasePosition = chefTarget;
            chefBaseScale = Vector3.one;
            menuCanvasGroup.alpha = 1f;
            introFinished = true;
        }

        private IEnumerator AnimateButtonIn(RectTransform rect, CanvasGroup group, Vector2 targetPosition, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            Vector2 start = targetPosition + Vector2.right * 150f;
            rect.anchoredPosition = start;

            float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - n, 3f);
                rect.anchoredPosition = Vector2.Lerp(start, targetPosition, eased);
                group.alpha = n;
                yield return null;
            }

            rect.anchoredPosition = targetPosition;
            group.alpha = 1f;
        }

        private IEnumerator AnimateBottomIn(RectTransform rect, CanvasGroup group, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            Vector2 target = rect.anchoredPosition;
            Vector2 start = target + Vector2.down * 100f;
            rect.anchoredPosition = start;

            float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - n, 3f);
                rect.anchoredPosition = Vector2.Lerp(start, target, eased);
                group.alpha = n;
                yield return null;
            }

            rect.anchoredPosition = target;
            group.alpha = 1f;
        }

        private void PlayGame()
        {
            PlayClick();
            EnhancedLoadingScreen.LoadViaLoadingScreen("LevelSelection");
        }

        private void OpenStory()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out int bestLevel);

            ShowModal(
                "STORY",
                "CONVEYOR CHEF: FOOD RUSH\n\n" +
                "A young chef begins a journey to turn a small kitchen into the happiest food stop in town. " +
                "Every order introduces a new recipe, a new customer, and a new kitchen challenge.\n\n" +
                $"Journey progress: {completed} levels completed\n" +
                $"Best level reached: {bestLevel}\n" +
                $"Stars collected: {stars}\n\n" +
                "Press PLAY to continue the story through the level map."
            );
        }

        private void OpenChallenges()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out int bestLevel);

            ShowModal(
                "CHALLENGES",
                BuildProgressLine("FIRST SERVICE", completed, 1) + "\n\n" +
                BuildProgressLine("RISING CHEF", completed, 3) + "\n\n" +
                BuildProgressLine("STAR HUNTER", stars, 9) + "\n\n" +
                BuildProgressLine("KITCHEN MASTER", completed, 10) + "\n\n" +
                "Challenges update automatically from your saved game progress."
            );
        }

        private void OpenCollection()
        {
            PlayClick();
            GetProgress(out int completed, out _, out _);

            string[] recipes = { "Burger", "Croissant", "Chocolate Donut", "Cake", "Pizza", "Sushi" };
            int unlocked = Mathf.Clamp(completed + 1, 1, recipes.Length);

            string list = string.Empty;
            for (int i = 0; i < recipes.Length; i++)
                list += (i < unlocked ? "[DONE]  " : "[LOCKED]  ") + recipes[i] + (i == recipes.Length - 1 ? string.Empty : "\n");

            ShowModal(
                "COLLECTION",
                $"RECIPES DISCOVERED: {unlocked}/{recipes.Length}\n\n" +
                list +
                "\n\nComplete more levels to expand your recipe collection."
            );
        }

        private void OpenAchievements()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out _);

            string body =
                AchievementLine("FIRST ORDER", completed >= 1, "Complete your first level") + "\n\n" +
                AchievementLine("RISING CHEF", completed >= 3, "Complete 3 levels") + "\n\n" +
                AchievementLine("STAR COLLECTOR", stars >= 9, "Collect 9 stars") + "\n\n" +
                AchievementLine("KITCHEN VETERAN", completed >= 10, "Complete 10 levels");

            ShowModal("ACHIEVEMENTS", body);
        }

        private void OpenLeaderboard()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out int bestLevel);

            int score = stars * 100 + completed * 50;
            int storedBest = PlayerPrefs.GetInt(LocalBestScoreKey, 0);
            if (score > storedBest)
            {
                storedBest = score;
                PlayerPrefs.SetInt(LocalBestScoreKey, storedBest);
                PlayerPrefs.Save();
            }

            ShowModal(
                "LEADERBOARD",
                "LOCAL CHEF RANKING\n\n" +
                $"Chef Score: {score:N0}\n" +
                $"Personal Best: {storedBest:N0}\n" +
                $"Stars: {stars}\n" +
                $"Levels Completed: {completed}\n" +
                $"Best Level: {bestLevel}\n\n" +
                "This leaderboard uses your saved device progress and updates automatically."
            );
        }

        private void OpenSettings()
        {
            PlayClick();
            ShowModal("SETTINGS", "Tune your kitchen experience.");
            settingsControls.SetActive(true);
            UpdateSettingsLabels();
        }

        private void OpenShop()
        {
            PlayClick();

            try
            {
                UIIAPStore page = UIController.GetPage<UIIAPStore>();
                if (page == null)
                    throw new InvalidOperationException("IAP store page is not registered.");

                OpenExternalPage(typeof(UIIAPStore), () => UIController.ShowPage<UIIAPStore>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ProfessionalMainMenu] Store unavailable: " + ex.Message);
                ShowModal("SHOP", "The store is currently unavailable in this build.");
            }
        }

        private void OpenCustomize()
        {
            PlayClick();

            try
            {
                UISkinStore page = UIController.GetPage<UISkinStore>();
                if (page == null)
                    throw new InvalidOperationException("Skin store page is not registered.");

                OpenExternalPage(typeof(UISkinStore), SkinStoreController.OpenStore);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ProfessionalMainMenu] Customize unavailable: " + ex.Message);
                ShowModal("CUSTOMIZE", "Character customization is currently unavailable in this build.");
            }
        }

        private void OpenExternalPage(Type pageType, Action opener)
        {
            HideModal();
            externalPageType = pageType;
            menuRoot.SetActive(false);

            if (!externalPageSubscribed)
            {
                UIController.OnPageClosedEvent += OnExternalPageClosed;
                externalPageSubscribed = true;
            }

            opener.Invoke();
        }

        private void OnExternalPageClosed(UIPage page, Type pageType)
        {
            if (externalPageType == null || pageType != externalPageType)
                return;

            externalPageType = null;
            menuRoot.SetActive(true);
            RefreshHUD();
            UnsubscribeExternalPage();
        }

        private void UnsubscribeExternalPage()
        {
            if (!externalPageSubscribed)
                return;

            UIController.OnPageClosedEvent -= OnExternalPageClosed;
            externalPageSubscribed = false;
        }

        private void ToggleSound()
        {
            bool enabled = AudioController.GetVolume() > 0.001f;
            AudioController.SetVolume(enabled ? 0f : 1f);
            UpdateSettingsLabels();
            PlayClick();
        }

        private void ToggleVibration()
        {
            bool enabled = AudioController.IsVibrationEnabled();
            AudioController.SetVibrationState(!enabled);
            UpdateSettingsLabels();
            PlayClick();
        }

        private void UpdateSettingsLabels()
        {
            if (soundButtonText != null)
                soundButtonText.text = "SOUND: " + (AudioController.GetVolume() > 0.001f ? "ON" : "OFF");

            if (vibrationButtonText != null)
                vibrationButtonText.text = "VIBRATION: " + (AudioController.IsVibrationEnabled() ? "ON" : "OFF");
        }

        private void ShowModal(string title, string body)
        {
            modalTitle.text = title;
            modalBody.text = body;
            settingsControls.SetActive(false);
            modalRoot.SetActive(true);

            CanvasGroup group = modalRoot.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            modalRoot.transform.localScale = Vector3.one * 0.94f;
            StartCoroutine(AnimateModalIn(group));
        }

        private IEnumerator AnimateModalIn(CanvasGroup group)
        {
            float elapsed = 0f;
            const float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(elapsed / duration);
                group.alpha = n;
                modalRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - Mathf.Pow(1f - n, 3f));
                yield return null;
            }

            group.alpha = 1f;
            modalRoot.transform.localScale = Vector3.one;
        }

        private void HideModal()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void RefreshHUD()
        {
            GetProgress(out _, out int stars, out _);

            if (starText != null)
                starText.text = stars.ToString();

            if (coinText != null)
            {
                try
                {
                    coinText.text = CurrenciesController.Get(CurrencyType.Coins).ToString("N0");
                }
                catch
                {
                    coinText.text = "0";
                }
            }

            if (!PlayerPrefs.HasKey(DiamondsKey))
            {
                PlayerPrefs.SetInt(DiamondsKey, 50);
                PlayerPrefs.Save();
            }

            if (diamondText != null)
                diamondText.text = PlayerPrefs.GetInt(DiamondsKey, 50).ToString("N0");
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            RefreshHUD();
        }

        private void GetProgress(out int completed, out int stars, out int bestLevel)
        {
            completed = 0;
            stars = 0;
            bestLevel = 0;

            if (!SaveController.IsSaveLoaded)
                return;

            try
            {
                LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
                if (save == null || save.levelProgress == null)
                    return;

                for (int i = 0; i < save.levelProgress.Count; i++)
                {
                    LevelProgressData progress = save.levelProgress[i];
                    if (progress == null)
                        continue;

                    if (progress.isCompleted)
                    {
                        completed++;
                        bestLevel = Mathf.Max(bestLevel, progress.levelIndex + 1);
                    }

                    stars += Mathf.Max(0, progress.starsEarned);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ProfessionalMainMenu] Could not read level progress: " + ex.Message);
            }
        }

        private static string BuildProgressLine(string label, int current, int target)
        {
            bool done = current >= target;
            int shown = Mathf.Min(current, target);
            return (done ? "[DONE] " : "- ") + label + $"   {shown}/{target}" + (done ? "   COMPLETE" : string.Empty);
        }

        private static string AchievementLine(string title, bool unlocked, string description)
        {
            return (unlocked ? "[DONE]  " : "[LOCKED]  ") + title + "\n    " + description;
        }

        private void PlayClick()
        {
            try
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
            }
            catch
            {
                // Menu remains usable even if audio is still initialising.
            }
        }

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect area = Screen.safeArea;
            lastSafeArea = area;

            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            safeArea.anchorMin = min;
            safeArea.anchorMax = max;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        private bool HasGeneratedResource(string resourceName)
        {
            // Embedded atlas/background are the approved generated artwork.
            if (ProfessionalMainMenuEmbeddedAssets.GetSprite(resourceName) != null)
                return true;

            return Resources.Load<Texture2D>(ResourceRoot + resourceName) != null;
        }

        private Sprite LoadSprite(string resourceName)
        {
            if (spriteCache.TryGetValue(resourceName, out Sprite cached))
                return cached;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string editorSpritePath = "Assets/Project Data/Game/Images/ProfessionalMainMenuBaked/" + resourceName + ".png";
                Sprite bakedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(editorSpritePath);
                if (bakedSprite != null)
                {
                    spriteCache[resourceName] = bakedSprite;
                    return bakedSprite;
                }
            }
#endif

            // Primary path: the exact approved generated art reconstructed from
            // Resources/ProfessionalMainMenuData.
            Sprite embedded = ProfessionalMainMenuEmbeddedAssets.GetSprite(resourceName);
            if (embedded != null)
            {
                spriteCache[resourceName] = embedded;
                return embedded;
            }

            // Optional future override: a directly imported PNG/JPG in Resources.
            Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + resourceName);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                Sprite generated = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);

                generated.name = "PMM_" + resourceName;
                spriteCache[resourceName] = generated;
                return generated;
            }

            // Last-resort compatibility fallback to existing repository art.
            Sprite fallback = GetCatalogSprite(resourceName);
            if (fallback == null)
                Debug.LogError("[ProfessionalMainMenu] Missing sprite for: " + resourceName);

            spriteCache[resourceName] = fallback;
            return fallback;
        }

        private Sprite GetCatalogSprite(string resourceName)
        {
            if (assetCatalog == null)
                return null;

            switch (resourceName)
            {
                case "background": return assetCatalog.background;
                case "logo": return assetCatalog.logo;
                case "chef": return assetCatalog.chef;
                case "avatar": return assetCatalog.avatar;
                case "play": return assetCatalog.primaryButton;
                case "story":
                case "challenges":
                case "customize":
                case "settings":
                    return assetCatalog.secondaryButton;
                case "star": return assetCatalog.star;
                case "coin_bar":
                case "diamond_bar":
                    return assetCatalog.darkPanel;
                case "shop": return assetCatalog.shop;
                case "collection":
                case "achievements":
                case "leaderboard":
                    return assetCatalog.orangeButton;
                default: return assetCatalog.secondaryButton;
            }
        }

        private void AddFallbackCounterDecor(RectTransform parent, bool diamond)
        {
            Sprite iconSprite = diamond ? assetCatalog.star : assetCatalog.coin;
            Image icon = CreateImage(diamond ? "Gem Icon" : "Coin Icon", parent, iconSprite, true);
            SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(62f, 62f), new Vector2(0.5f, 0.5f));
            if (diamond)
                icon.color = new Color(0.15f, 0.85f, 1f, 1f);

            Image plus = CreateImage("Plus Icon", parent, assetCatalog.plus, true);
            SetRect(plus.rectTransform, new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(62f, 62f), new Vector2(0.5f, 0.5f));
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolidImage(string name, Transform parent, Color color)
        {
            Image image = CreateImage(name, parent, null, false);
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(9, 32, 72, 255);
            return text;
        }

        private static Button CreateTextButton(string label, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
        {
            Image image = CreateSolidImage(label + " Button", parent, color);
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f),
                pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            button.onClick.AddListener(action);

            TextMeshProUGUI text = CreateText("Label", image.rectTransform, label, 38f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        private static void AddInvisibleButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, anchor, position, size, new Vector2(0.5f, 0.5f));

            Image image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }

    /// <summary>
    /// Touch/hover juice for professional menu buttons: press compression,
    /// release bounce, optional idle pulse and a clipped shine sweep.
    /// </summary>
    public sealed class ProfessionalMainMenuButtonFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform rect;
        private Vector3 baseScale;
        private float pointerMultiplier = 1f;
        private bool idlePulse;
        private bool shineEnabled;
        private RectTransform shineRect;
        private CanvasGroup shineGroup;
        private float shineTimer;
        private bool initialised;

        public void Configure(bool pulse, bool shine)
        {
            idlePulse = pulse;
            shineEnabled = shine;
            Initialise();

            if (shineEnabled)
                CreateShine();
        }

        private void Initialise()
        {
            if (initialised)
                return;

            rect = transform as RectTransform;
            baseScale = rect != null ? rect.localScale : Vector3.one;
            initialised = true;
        }

        private void Update()
        {
            if (!initialised)
                Initialise();

            float pulse = idlePulse ? 1f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.018f : 1f;
            Vector3 target = baseScale * (pointerMultiplier * pulse);
            transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));

            if (shineEnabled && shineRect != null)
                UpdateShine();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerMultiplier = 0.93f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerMultiplier = 1.07f;
            StartCoroutine(Settle());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pointerMultiplier >= 0.99f)
                pointerMultiplier = 1.035f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerMultiplier = 1f;
        }

        private IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(0.07f);
            pointerMultiplier = 1f;
        }

        private void CreateShine()
        {
            if (shineRect != null)
                return;

            Transform existingShine = transform.Find("Shine");
            if (existingShine != null)
            {
                shineRect = existingShine as RectTransform;
                shineGroup = existingShine.GetComponent<CanvasGroup>();
                if (shineGroup == null)
                    shineGroup = existingShine.gameObject.AddComponent<CanvasGroup>();
                return;
            }

            Image hostImage = GetComponent<Image>();
            if (hostImage != null && GetComponent<Mask>() == null)
            {
                Mask mask = gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
            }

            GameObject shineObject = new GameObject("Shine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            shineRect = shineObject.GetComponent<RectTransform>();
            shineRect.SetParent(transform, false);
            shineRect.anchorMin = new Vector2(0.5f, 0.5f);
            shineRect.anchorMax = new Vector2(0.5f, 0.5f);
            shineRect.pivot = new Vector2(0.5f, 0.5f);
            shineRect.sizeDelta = new Vector2(54f, 420f);
            shineRect.localRotation = Quaternion.Euler(0f, 0f, -18f);

            Image shineImage = shineObject.GetComponent<Image>();
            shineImage.color = new Color(1f, 1f, 1f, 0.22f);
            shineImage.raycastTarget = false;

            shineGroup = shineObject.GetComponent<CanvasGroup>();
            shineGroup.alpha = 0f;
            shineTimer = UnityEngine.Random.Range(0.4f, 2.2f);
        }

        private void UpdateShine()
        {
            shineTimer += Time.unscaledDeltaTime;
            const float cycle = 3.1f;
            const float travelDuration = 0.65f;

            float phase = shineTimer % cycle;
            if (phase > travelDuration)
            {
                shineGroup.alpha = 0f;
                return;
            }

            RectTransform host = rect;
            float n = phase / travelDuration;
            float width = host.rect.width;
            shineRect.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.7f, width * 0.7f, n), 0f);
            shineGroup.alpha = Mathf.Sin(n * Mathf.PI) * 0.9f;
        }
    }
}
