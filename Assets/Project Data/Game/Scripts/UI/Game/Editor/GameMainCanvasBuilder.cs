#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon.EditorTools
{
    /// <summary>
    /// Builds GAME MAIN CANVAS [NEW UI]: a separate root canvas for the Game scene HUD.
    ///
    /// UI Main Canvas (with OLD GAME SCENE [OFF] and NEW GAME SCENE [ACTIVE]) is not
    /// modified. The working UIGame page is cloned so every gameplay link (power-ups,
    /// orders, lives, currencies, quit popup, tutorial) keeps working, then the clone is
    /// laid out like "Game UI Reference.png" at 1080x1920. The conveyor, tray, 7 waiting
    /// slots and donut board are the real 3D level, so the middle of this canvas stays
    /// transparent and they show through.
    /// </summary>
    [InitializeOnLoad]
    public static class GameMainCanvasBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/Game.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/GameUI";
        private const string MainCanvasName = "UI Main Canvas";
        private const string SourcePageName = "NEW GAME SCENE [ACTIVE]";
        private const string CanvasName = "GAME MAIN CANVAS [NEW UI]";
        private const string BackgroundCanvasName = "GAME BACKGROUND CANVAS [NEW UI]";
        private const string KitchenArt = "Kitchen/kitchen_gameplay_background.png";
        private const string SourceDesignRootName = "NEW GAME UI DESIGN";
        private const string OrderItemSourcePrefab = "Assets/Project Data/Game/Prefabs/OrderItem.prefab";
        private const string OrderItemPrefab = "Assets/Project Data/Game/Prefabs/OrderItem (Game Main Canvas).prefab";
        private const string OrdersFrameSprite = "Orders/orders_panel_frame.png";
        private const string OrderCardSprite = "Orders/order_item_slot.png";
        private const string PreviewOrderPrefix = "Preview Order";

        private const string LivesIndicatorPrefab =
            "Assets/Project Data/Watermelon Core/Extra Components/Lives System/Prefabs/Lives Indicator.prefab";
        private const string AddLivesPanelPrefab =
            "Assets/Project Data/Watermelon Core/Extra Components/Lives System/Prefabs/Add Lives Panel.prefab";
        private const string CurrencyPanelPrefab =
            "Assets/Project Data/Watermelon Core/Modules/Currencies Module/Prefabs/Currency Panel Simple.prefab";

        // Layout at the 1080x1920 reference resolution, measured from Game UI Reference.png.
        private const float TopBarHeight = 150f;
        private const float TopRowY = -86f;
        private const float ToolbarWidth = 1070f;
        private const float ToolbarHeight = 357f;
        private const float ToolbarBottom = -38f;
        private const float ToolbarButtonSize = 172f;
        private const float PowerUpPrefabSize = 175f;

        private static readonly Vector2 OrderCell = new Vector2(232f, 96f);
        private static readonly Vector2 OrderSpacing = new Vector2(8f, 12f);

        // Slot centres of bottom_toolbar_panel.png as a fraction of its width, and the
        // slot centre height above the image bottom as a fraction of its height.
        private const float ToolbarInnerSlot = 0.0993f;
        private const float ToolbarOuterSlot = 0.2975f;
        private const float ToolbarSlotHeight = 0.531f;

        private static readonly Color32 TopBarColor = new Color32(34, 58, 72, 255);
        private static readonly Color32 TopBarEdgeColor = new Color32(18, 32, 42, 255);
        private static readonly Color32 BrownText = new Color32(92, 52, 24, 255);
        private static readonly Color32 NavyText = new Color32(35, 48, 70, 255);
        private static readonly Color32 OrdersTitleColor = new Color32(110, 54, 14, 255);

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        // Bump when the generated layout changes: an older canvas is rebuilt once
        // automatically. Auto-build runs once per version and project copy, so deleting
        // the canvas later is respected.
        private const int LayoutVersion = 5;
        private static readonly string AutoBuiltKey =
            "ConveyorChef.GameMainCanvas.AutoBuilt.v" + LayoutVersion + "." + Application.dataPath.GetHashCode();

        private static bool autoQueued;

        static GameMainCanvasBuilder()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            QueueAutoBuild();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => QueueAutoBuild();

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                QueueAutoBuild();
        }

        private static void QueueAutoBuild()
        {
            if (autoQueued || EditorPrefs.GetBool(AutoBuiltKey, false))
                return;

            autoQueued = true;
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            autoQueued = false;

            if (EditorPrefs.GetBool(AutoBuiltKey, false))
                return;

            // Play mode is retried from OnPlayModeChanged when the editor returns to Edit mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueAutoBuild();
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            GameObject existing = FindRoot(CanvasName);
            GameMainCanvas existingMarker = existing != null ? existing.GetComponent<GameMainCanvas>() : null;
            if (existingMarker != null && existingMarker.LayoutVersion >= LayoutVersion)
            {
                EditorPrefs.SetBool(AutoBuiltKey, true);
                return;
            }

            if (BuildAndSave(scene))
                EditorPrefs.SetBool(AutoBuiltKey, true);
        }

        [MenuItem("Conveyor Chef/Game Scene/NEW MAIN CANVAS/1. Build or Rebuild Game Main Canvas", priority = 20)]
        public static void BuildMenu()
        {
            if (!PrepareScene(out Scene scene))
                return;

            BuildAndSave(scene);
        }

        private static bool BuildAndSave(Scene scene)
        {
            GameObject root = Build();
            if (root == null)
                return false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = root;
            Validate(root, logSuccess: true);

            Debug.Log("[GameMainCanvas] Built and saved '" + CanvasName + "' in Game.unity. UI Main Canvas was not modified.");
            return true;
        }

        [MenuItem("Conveyor Chef/Game Scene/NEW MAIN CANVAS/2. Select Game Main Canvas", priority = 21)]
        public static void SelectMenu()
        {
            GameObject root = FindRoot(CanvasName);
            if (root == null)
            {
                Debug.LogWarning("[GameMainCanvas] '" + CanvasName + "' is not in the open scene. Run menu item 1.");
                return;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        [MenuItem("Conveyor Chef/Game Scene/NEW MAIN CANVAS/3. Validate Game Main Canvas", priority = 22)]
        public static void ValidateMenu()
        {
            GameObject root = FindRoot(CanvasName);
            if (root == null)
            {
                Debug.LogError("[GameMainCanvas] '" + CanvasName + "' is not in the open scene. Run menu item 1.");
                return;
            }

            Validate(root, logSuccess: true);
        }

        private static bool PrepareScene(out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath)
                return true;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return scene.IsValid();
        }

        private static GameObject Build()
        {
            GameObject mainCanvasObject = FindRoot(MainCanvasName);
            Canvas mainCanvas = mainCanvasObject != null ? mainCanvasObject.GetComponent<Canvas>() : null;
            if (mainCanvas == null)
            {
                Debug.LogError("[GameMainCanvas] '" + MainCanvasName + "' was not found.");
                return null;
            }

            UIGame source = FindSourcePage(mainCanvas.transform);
            if (source == null)
            {
                Debug.LogError("[GameMainCanvas] No UIGame page was found inside '" + MainCanvasName + "'.");
                return null;
            }

            ConfigureSprites();
            GameObject orderItemPrefab = BuildOrderItemPrefab();

            foreach (GameObject previous in FindRoots(CanvasName))
                Object.DestroyImmediate(previous);

            // Clone the complete working page. Unity remaps every reference that points
            // inside the cloned hierarchy, so the source page stays exactly as it was.
            GameObject root = Object.Instantiate(source.gameObject);
            root.name = CanvasName;
            root.SetActive(true);
            SceneManager.MoveGameObjectToScene(root, mainCanvasObject.scene);
            root.transform.SetSiblingIndex(mainCanvasObject.transform.GetSiblingIndex() + 1);

            ConfigureRootCanvas(root, mainCanvas);
            BuildBackgroundCanvas(mainCanvas);
            RestyleResultPages(mainCanvas.transform);

            Transform t = root.transform;
            RectTransform safeZone = Find(t, "Safe Zone") as RectTransform;
            if (safeZone == null)
            {
                Object.DestroyImmediate(root);
                Debug.LogError("[GameMainCanvas] The source page has no 'Safe Zone'. Nothing was changed.");
                return null;
            }

            Stretch(safeZone);

            // Remove the 2D picture copies of the conveyor, tray, slots and board. The
            // real 3D level shows through the transparent middle of this canvas.
            DestroyIfExists(Find(t, SourceDesignRootName));

            UIGame game = root.GetComponent<UIGame>();
            GameSceneHUDController hud = root.GetComponent<GameSceneHUDController>();
            if (hud == null)
                hud = root.AddComponent<GameSceneHUDController>();

            TMP_FontAsset font = null;
            TextMeshProUGUI levelText = FindComponent<TextMeshProUGUI>(t, "Level Text");
            if (levelText != null)
                font = levelText.font;

            RectTransform backplate = BuildTopBackplate(t);

            RectTransform toolbar = BuildToolbar(safeZone, out Button homeButton);
            PUUIController powerUps = LayoutPowerUps(t, toolbar);

            // In front of the toolbar ends, like the plant and utensils in the reference.
            RectTransform decorLayer = CreateRect("Bottom Decor", safeZone);
            Stretch(decorLayer);
            BuildDecor(decorLayer);

            UIOrderPanel orderPanel = BuildOrders(t, safeZone, orderItemPrefab, font);

            LivesIndicator lives = LayoutLives(t, safeZone);

            RectTransform levelPanel = BuildLevelPanel(safeZone, levelText);

            CurrencyUIPanelSimple coins = LayoutCurrency(
                t, safeZone, "Coin Counter", CurrencyType.Coins, "TopHUD/coin_counter_panel.png",
                new Vector2(-295f, TopRowY), new Vector2(206f, 70f), iconBakedIntoPanel: true,
                out Image coinIcon);

            CurrencyUIPanelSimple diamonds = LayoutCurrency(
                t, safeZone, "Diamond Counter", CurrencyType.Diamonds, "TopHUD/diamond_counter_panel.png",
                new Vector2(-128f, TopRowY), new Vector2(166f, 62f), iconBakedIntoPanel: false,
                out Image diamondIcon);

            Button pauseButton = LayoutPauseButton(t, safeZone);

            Button settingsButton = FindComponent<Button>(t, "Settings Button");
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(false);

            // UIGame switches this on at runtime only when the dev panel is enabled.
            Transform devOverlay = Find(t, "Dev Overlay");
            if (devOverlay != null)
                devOverlay.gameObject.SetActive(false);

            Transform addLivesPanel = Find(t, "NEW Add Lives Panel");
            if (addLivesPanel != null)
            {
                addLivesPanel.SetParent(safeZone, false);
                Stretch(addLivesPanel as RectTransform);
                addLivesPanel.gameObject.SetActive(false);
                addLivesPanel.SetAsLastSibling();
            }

            UILevelQuitPopUp quitPopup = FindComponent<UILevelQuitPopUp>(t, "Quit Pop Up") ??
                                         root.GetComponentInChildren<UILevelQuitPopUp>(true);
            if (quitPopup != null)
                quitPopup.transform.SetAsLastSibling();

            // Draw order inside Safe Zone, back to front.
            Transform[] order =
            {
                toolbar,
                decorLayer,
                orderPanel != null ? orderPanel.transform.parent : null,
                lives != null ? lives.transform : null,
                levelPanel,
                coins != null ? coins.transform : null,
                diamonds != null ? diamonds.transform : null,
                pauseButton != null ? pauseButton.transform : null,
                settingsButton != null ? settingsButton.transform : null,
                devOverlay,
                addLivesPanel,
            };
            foreach (Transform item in order)
            {
                if (item != null && item.parent == safeZone)
                    item.SetAsLastSibling();
            }

            RemoveClicksIntoOldPages(root, mainCanvasObject.transform);

            // Explicit wiring. The source page's replay button and quit popup pointed at
            // objects in the OLD page, so they are set to this canvas's own objects here.
            SetReferences(game,
                ("safeZoneTransform", safeZone),
                ("powerUpsUIController", powerUps),
                ("replayButton", pauseButton),
                ("exitPopUp", quitPopup),
                ("levelText", levelText),
                ("orderPanel", orderPanel),
                ("devOverlay", devOverlay != null ? devOverlay.gameObject : null));

            if (powerUps != null)
                SetReferences(powerUps, ("powerUpPurchasePanel", root.GetComponentInChildren<PUUIPurchasePanel>(true)));

            if (lives != null && addLivesPanel != null)
                SetReferences(lives, ("addLivesPanel", addLivesPanel.GetComponent<AddLivesPanel>()));

            hud.EditorConfigure(
                coins,
                diamonds,
                coinIcon,
                diamondIcon,
                settingsButton,
                homeButton,
                quitPopup,
                LoadSprite("Toolbar/undo_button.png"),
                LoadSprite("Toolbar/hint_button.png"),
                LoadSprite("Toolbar/shuffle_button.png"),
                LoadSprite("Toolbar/powerup_count_badge.png"),
                LoadSprite("TopHUD/coin_icon.png"),
                LoadSprite("TopHUD/diamond_icon.png"));

            GameMainCanvas marker = root.GetComponent<GameMainCanvas>();
            if (marker == null)
                marker = root.AddComponent<GameMainCanvas>();
            marker.EditorConfigure(game, safeZone, backplate, TopBarHeight, LayoutVersion);

            RewireTutorial(root);

            // Edits to prefab instances (sample buttons/orders, result pages) are only
            // kept on save when recorded as instance overrides.
            RecordPrefabInstanceEdits(root.transform);
            RecordPrefabInstanceEdits(mainCanvas.transform.Find("UI Complete"));
            RecordPrefabInstanceEdits(mainCanvas.transform.Find("UI Game Over"));

            EditorUtility.SetDirty(root);
            return root;
        }

        #region Canvas

        private static UIGame FindSourcePage(Transform mainCanvas)
        {
            Transform named = mainCanvas.Find(SourcePageName);
            if (named != null && named.GetComponent<UIGame>() != null)
                return named.GetComponent<UIGame>();

            UIGame fallback = null;
            foreach (UIGame page in mainCanvas.GetComponentsInChildren<UIGame>(true))
            {
                if (page.GetComponent<GameSceneHUDController>() != null)
                    return page;

                if (fallback == null)
                    fallback = page;
            }

            return fallback;
        }

        // Painted kitchen behind the 3D level; see KitchenBackdrop for how it is placed.
        private static void BuildBackgroundCanvas(Canvas mainCanvas)
        {
            foreach (GameObject previous in FindRoots(BackgroundCanvasName))
                Object.DestroyImmediate(previous);

            GameObject mainCanvasObject = mainCanvas.gameObject;
            GameObject root = new GameObject(BackgroundCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.layer = mainCanvasObject.layer;
            SceneManager.MoveGameObjectToScene(root, mainCanvasObject.scene);
            root.transform.SetSiblingIndex(mainCanvasObject.transform.GetSiblingIndex());

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCanvas.worldCamera;
            Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            canvas.planeDistance = cam != null ? cam.farClipPlane * 0.95f : 100f;
            canvas.sortingLayerID = mainCanvas.sortingLayerID;
            canvas.sortingOrder = -100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            Sprite sprite = LoadSprite(KitchenArt);
            Texture texture = sprite != null ? sprite.texture : null;
            Vector2 artSize = sprite != null ? sprite.rect.size : new Vector2(941f, 1672f);

            RectTransform art = CreateRect("Kitchen Painting", root.transform);
            Place(art, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, artSize * (1080f / artSize.x));

            // One texture drawn twice: the kitchen wall keeps its shape, the floor and
            // front counter stretch to the bottom of tall screens (see KitchenBackdrop).
            RawImage kitchen = CreateRawImage("Kitchen Wall", art, texture);
            RawImage floor = CreateRawImage("Kitchen Floor", art, texture);

            // No GraphicRaycaster: the painting must never catch taps meant for the level.
            KitchenBackdrop backdrop = root.AddComponent<KitchenBackdrop>();
            backdrop.EditorConfigure(art, kitchen, floor, artSize);
        }

        private static void ConfigureRootCanvas(GameObject root, Canvas mainCanvas)
        {
            root.layer = mainCanvas.gameObject.layer;

            RectTransform rect = (RectTransform)root.transform;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.enabled = true;
            canvas.renderMode = mainCanvas.renderMode;
            canvas.worldCamera = mainCanvas.worldCamera;
            canvas.planeDistance = mainCanvas.planeDistance;
            canvas.overrideSorting = false;
            canvas.sortingLayerID = mainCanvas.sortingLayerID;
            // Above UI Main Canvas while editing so it is visible in the Game view.
            // GameMainCanvas.AttachTo moves it just below UI Main Canvas in Play mode.
            canvas.sortingOrder = mainCanvas.sortingOrder + 1;
            canvas.additionalShaderChannels = mainCanvas.additionalShaderChannels;

            CanvasScaler mainScaler = mainCanvas.GetComponent<CanvasScaler>();
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = root.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = mainScaler != null ? mainScaler.referenceResolution : new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = mainScaler != null ? mainScaler.matchWidthOrHeight : 0f;
            scaler.referencePixelsPerUnit = mainScaler != null ? mainScaler.referencePixelsPerUnit : 100f;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();
        }

        // Inspector onClick events copied from the old page may call objects inside
        // UI Main Canvas (for example the legacy settings panel). Those would act on
        // hidden pages, so they are removed from the new canvas only.
        private static void RemoveClicksIntoOldPages(GameObject root, Transform mainCanvas)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    Object target = button.onClick.GetPersistentTarget(i);
                    if (target == null || !IsInside(target, mainCanvas))
                        continue;

                    Debug.Log("[GameMainCanvas] Removed onClick '" + button.onClick.GetPersistentMethodName(i) +
                              "' on '" + button.name + "' (it targeted the old UI Main Canvas).");
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
                }
            }
        }

        private static void RewireTutorial(GameObject root)
        {
            TutorialCanvasController tutorialCanvas = root.GetComponentInChildren<TutorialCanvasController>(true);
            TutorialController controller = Object.FindFirstObjectByType<TutorialController>(FindObjectsInactive.Include);
            if (tutorialCanvas == null || controller == null)
                return;

            SetReferences(controller, ("tutorialCanvasController", tutorialCanvas));
            EditorUtility.SetDirty(controller);
        }

        #endregion

        #region Top HUD

        private static RectTransform BuildTopBackplate(Transform root)
        {
            RectTransform backplate = CreateImage("Top HUD Backplate", root, null, TopBarColor, false);
            backplate.anchorMin = new Vector2(0f, 1f);
            backplate.anchorMax = new Vector2(1f, 1f);
            backplate.pivot = new Vector2(0.5f, 1f);
            backplate.anchoredPosition = Vector2.zero;
            backplate.sizeDelta = new Vector2(0f, TopBarHeight);
            backplate.SetAsFirstSibling();

            RectTransform edge = CreateImage("Bottom Edge", backplate, null, TopBarEdgeColor, false);
            edge.anchorMin = new Vector2(0f, 0f);
            edge.anchorMax = new Vector2(1f, 0f);
            edge.pivot = new Vector2(0.5f, 0f);
            edge.anchoredPosition = Vector2.zero;
            edge.sizeDelta = new Vector2(0f, 6f);

            return backplate;
        }

        private static LivesIndicator LayoutLives(Transform root, RectTransform safeZone)
        {
            LivesIndicator lives = FindComponent<LivesIndicator>(root, "Life Counter") ??
                                   root.GetComponentInChildren<LivesIndicator>(true);

            if (lives == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LivesIndicatorPrefab);
                if (prefab == null)
                    return null;

                lives = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, safeZone)).GetComponent<LivesIndicator>();
                lives.name = "Life Counter";
            }

            if (Find(root, "NEW Add Lives Panel") == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AddLivesPanelPrefab);
                if (prefab != null)
                    ((GameObject)PrefabUtility.InstantiatePrefab(prefab, safeZone)).name = "NEW Add Lives Panel";
            }

            Transform t = lives.transform;
            t.SetParent(safeZone, false);
            lives.gameObject.SetActive(true);
            Place(t, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(78f, TopRowY), new Vector2(204f, 62f));
            SetSprite(lives.GetComponent<Image>(), LoadSprite("TopHUD/life_counter_panel.png"), false);

            // Heart overlaps the left end of the pill, as in the reference.
            Transform heart = Find(t, "Heart Image");
            Place(heart, new Vector2(0f, 0.5f), Center, new Vector2(-6f, 2f), new Vector2(96f, 88f));
            SetSprite(heart, LoadSprite("TopHUD/heart_icon.png"), true);
            if (heart != null)
                heart.SetAsLastSibling();

            // The lives number sits on the heart; in the prefab it may be a child of the
            // heart or of the panel.
            Transform amount = Find(t, "Lives Amount");
            PlaceOnHeart(amount, heart, new Vector2(60f, 52f));
            StyleText(amount, Color.white, 24f, 46f);

            PlaceOnHeart(Find(t, "Infinity"), heart, new Vector2(44f, 44f));

            Transform time = Find(t, "Time Text");
            Fill(time, new Vector2(40f, 4f), new Vector2(-46f, -4f));
            StyleText(time, NavyText, 20f, 36f);

            Transform add = Find(t, "Add Button");
            Place(add, new Vector2(1f, 0.5f), Center, new Vector2(-22f, 0f), new Vector2(46f, 46f));
            SetSprite(add, LoadSprite("TopHUD/green_plus_button.png"), true);

            return lives;
        }

        private static void PlaceOnHeart(Transform target, Transform heart, Vector2 size)
        {
            if (target == null)
                return;

            if (heart != null && target.parent == heart)
            {
                Place(target, Center, Center, new Vector2(0f, 4f), size);
            }
            else
            {
                Place(target, new Vector2(0f, 0.5f), Center, new Vector2(-6f, 6f), size);
                target.SetAsLastSibling();
            }
        }

        private static RectTransform BuildLevelPanel(RectTransform safeZone, TextMeshProUGUI levelText)
        {
            RectTransform panel = CreateImage("Level Panel", safeZone, LoadSprite("TopHUD/level_title_panel.png"), Color.white, false);
            Place(panel, new Vector2(0.5f, 1f), Center, new Vector2(-103f, TopRowY - 10f), new Vector2(276f, 92f));

            if (levelText != null)
            {
                levelText.transform.SetParent(panel, false);
                levelText.gameObject.SetActive(true);
                Fill(levelText.transform, new Vector2(24f, 10f), new Vector2(-24f, -12f));
                StyleText(levelText.transform, Color.white, 26f, 52f);
                levelText.text = "LEVEL 1";
            }

            RectTransform hat = CreateImage("Chef Hat", panel, LoadSprite("TopHUD/chef_hat_icon.png"), Color.white, true);
            Place(hat, Center, Center, new Vector2(-6f, 64f), new Vector2(80f, 70f));

            return panel;
        }

        private static CurrencyUIPanelSimple LayoutCurrency(
            Transform root,
            RectTransform safeZone,
            string name,
            CurrencyType type,
            string panelSprite,
            Vector2 position,
            Vector2 size,
            bool iconBakedIntoPanel,
            out Image iconImage)
        {
            iconImage = null;

            CurrencyUIPanelSimple panel = FindComponent<CurrencyUIPanelSimple>(root, name);
            if (panel == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyPanelPrefab);
                if (prefab == null)
                    return null;

                panel = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, safeZone)).GetComponent<CurrencyUIPanelSimple>();
                panel.name = name;
            }

            SetEnum(panel, "currencyType", (int)type);

            Transform t = panel.transform;
            t.SetParent(safeZone, false);
            panel.gameObject.SetActive(true);
            Place(t, new Vector2(1f, 1f), new Vector2(1f, 0.5f), position, size);
            SetSprite(panel.GetComponent<Image>(), LoadSprite(panelSprite), false);

            Transform icon = Find(t, "Currency Icon");
            Image image = icon != null ? icon.GetComponent<Image>() : null;
            if (image != null)
            {
                if (iconBakedIntoPanel)
                {
                    // coin_counter_panel.png already has the coin drawn on its left end.
                    image.enabled = false;
                }
                else
                {
                    Place(icon, new Vector2(0f, 0.5f), Center, new Vector2(30f, 2f), new Vector2(74f, 74f));
                    SetSprite(image, LoadSprite("TopHUD/diamond_icon.png"), true);
                    image.enabled = true;
                    iconImage = image;
                }
            }

            float textLeft = iconBakedIntoPanel ? 70f : 60f;
            Transform amount = Find(t, "Amount Text");
            Fill(amount, new Vector2(textLeft, 4f), new Vector2(-43f, -4f));
            StyleText(amount, NavyText, 20f, 36f);

            Transform add = Find(t, "Add Button");
            if (add != null)
            {
                add.gameObject.SetActive(true);
                Place(add, new Vector2(1f, 0.5f), Center, new Vector2(-20f, 0f), new Vector2(44f, 44f));
                SetSprite(add, LoadSprite("TopHUD/green_plus_button.png"), true);
                add.SetAsLastSibling();
            }

            if (icon != null)
                icon.SetAsLastSibling();

            return panel;
        }

        private static Button LayoutPauseButton(Transform root, RectTransform safeZone)
        {
            Transform pause = Find(root, "Pause Button");
            Button button = pause != null ? pause.GetComponent<Button>() : null;

            if (button == null)
            {
                RectTransform created = CreateImage("Pause Button", safeZone, null, Color.white, true);
                created.GetComponent<Image>().raycastTarget = true;
                button = created.gameObject.AddComponent<Button>();
                button.targetGraphic = created.GetComponent<Image>();
                pause = created;
            }

            pause.SetParent(safeZone, false);
            pause.gameObject.SetActive(true);
            Place(pause, new Vector2(1f, 1f), Center, new Vector2(-64f, TopRowY), new Vector2(104f, 104f));
            SetSprite(pause, LoadSprite("TopHUD/pause_button.png"), true);

            // The old replay button kept its art on a child, so its own Image was off.
            Image pauseImage = pause.GetComponent<Image>();
            if (pauseImage != null)
            {
                pauseImage.enabled = true;
                pauseImage.raycastTarget = true;
                button.targetGraphic = pauseImage;
            }

            // The replay button art had an icon and heart overlay; the pause sprite is complete.
            foreach (Transform child in pause)
                child.gameObject.SetActive(false);

            return button;
        }

        #endregion

        #region Orders

        private static UIOrderPanel BuildOrders(Transform root, RectTransform safeZone, GameObject orderItemPrefab, TMP_FontAsset font)
        {
            UIOrderPanel orderPanel = FindComponent<UIOrderPanel>(root, "OrderPanel") ??
                                      root.GetComponentInChildren<UIOrderPanel>(true);
            if (orderPanel == null)
            {
                Debug.LogWarning("[GameMainCanvas] No UIOrderPanel in the source page. ORDERS panel skipped.");
                return null;
            }

            RectTransform frame = CreateImage("Orders Frame", safeZone, LoadSprite(OrdersFrameSprite), Color.white, false);
            Image frameImage = frame.GetComponent<Image>();
            frameImage.type = Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 2.4f;
            frameImage.fillCenter = true;
            frame.anchorMin = new Vector2(1f, 1f);
            frame.anchorMax = new Vector2(1f, 1f);
            frame.pivot = new Vector2(1f, 1f);
            frame.anchoredPosition = new Vector2(8f, -(TopBarHeight + 18f));
            frame.sizeDelta = new Vector2(316f, 390f);

            VerticalLayoutGroup frameLayout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            frameLayout.padding = new RectOffset(46, 46, 90, 24);
            frameLayout.spacing = 0f;
            frameLayout.childAlignment = TextAnchor.UpperCenter;
            frameLayout.childControlWidth = true;
            frameLayout.childControlHeight = true;
            frameLayout.childForceExpandWidth = false;
            frameLayout.childForceExpandHeight = false;

            ContentSizeFitter frameFitter = frame.gameObject.AddComponent<ContentSizeFitter>();
            frameFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            frameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = CreateText("Orders Title", frame, "ORDERS", font, OrdersTitleColor, 24f, 40f);
            title.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = Center;
            titleRect.anchoredPosition = new Vector2(0f, -40f);
            titleRect.sizeDelta = new Vector2(-96f, 54f);

            Transform panel = orderPanel.transform;
            panel.SetParent(frame, false);
            panel.gameObject.SetActive(true);
            RectTransform panelRect = (RectTransform)panel;
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.localScale = Vector3.one;

            // Orders grow from 1 column to 2, so the old vertical list is replaced by a grid.
            foreach (LayoutGroup group in panel.GetComponents<LayoutGroup>())
                Object.DestroyImmediate(group);
            foreach (ContentSizeFitter fitter in panel.GetComponents<ContentSizeFitter>())
                Object.DestroyImmediate(fitter);

            GridLayoutGroup grid = panel.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = OrderCell;
            grid.spacing = OrderSpacing;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;

            if (panel.GetComponent<OrderPanelAdaptiveGrid>() == null)
                panel.gameObject.AddComponent<OrderPanelAdaptiveGrid>();

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(1f, 1f, 1f, 0f);
                panelImage.raycastTarget = false;
            }

            SerializedObject so = new SerializedObject(orderPanel);
            so.FindProperty("orderItemsContainer").objectReferenceValue = panel;
            if (orderItemPrefab != null)
                so.FindProperty("orderItemPrefab").objectReferenceValue = orderItemPrefab.GetComponent<UIOrderItem>();
            so.ApplyModifiedPropertiesWithoutUndo();

            for (int i = panel.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(panel.GetChild(i).gameObject);

            AddPreviewOrders(orderPanel, orderItemPrefab);

            return orderPanel;
        }

        // Three sample rows so the panel can be edited in the Scene view. UIOrderPanel
        // clears the container when a level starts, and OrderPanelAdaptiveGrid removes
        // them in Play mode for levels without orders.
        private static void AddPreviewOrders(UIOrderPanel orderPanel, GameObject orderItemPrefab)
        {
            if (orderItemPrefab == null)
                return;

            SerializedObject so = new SerializedObject(orderPanel);
            SerializedProperty sprites = so.FindProperty("busTypeSprites");
            string[] amounts = { "0/1", "0/2", "0/1" };

            for (int i = 0; i < amounts.Length; i++)
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(orderItemPrefab, orderPanel.transform);
                item.name = PreviewOrderPrefix + " " + (i + 1);
                item.AddComponent<EditorPreviewOnly>();

                UIOrderItem logic = item.GetComponent<UIOrderItem>();
                SerializedObject itemSo = new SerializedObject(logic);

                Image icon = itemSo.FindProperty("busIcon").objectReferenceValue as Image;
                if (icon != null && sprites != null && sprites.arraySize > 0)
                {
                    SerializedProperty entry = sprites.GetArrayElementAtIndex(i % sprites.arraySize);
                    icon.sprite = entry.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
                }

                TextMeshProUGUI text = itemSo.FindProperty("orderText").objectReferenceValue as TextMeshProUGUI;
                if (text != null)
                    text.text = amounts[i];
            }
        }

        private static GameObject BuildOrderItemPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OrderItemSourcePrefab) == null)
            {
                Debug.LogWarning("[GameMainCanvas] " + OrderItemSourcePrefab + " not found. Orders keep the old item prefab.");
                return null;
            }

            // A separate copy, so OrderItem.prefab used by the old pages is not changed.
            // The styling below is repeatable, so a rebuild keeps the copy and its GUID.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OrderItemPrefab) == null)
                AssetDatabase.CopyAsset(OrderItemSourcePrefab, OrderItemPrefab);

            GameObject contents = PrefabUtility.LoadPrefabContents(OrderItemPrefab);
            try
            {
                RectTransform rect = (RectTransform)contents.transform;
                rect.sizeDelta = OrderCell;

                Image card = contents.GetComponent<Image>();
                if (card != null)
                {
                    card.sprite = LoadSprite(OrderCardSprite);
                    card.color = Color.white;
                    card.type = Image.Type.Sliced;
                    card.pixelsPerUnitMultiplier = 4.5f;
                    card.raycastTarget = false;
                }

                UIOrderItem logic = contents.GetComponent<UIOrderItem>();
                SerializedObject so = new SerializedObject(logic);

                Image icon = so.FindProperty("busIcon").objectReferenceValue as Image;
                if (icon != null)
                {
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }

                TextMeshProUGUI text = so.FindProperty("orderText").objectReferenceValue as TextMeshProUGUI;
                if (text != null)
                    StyleText(text.transform, BrownText, 20f, 40f);

                // Same layout OrderPanelAdaptiveGrid applies at runtime for large cards.
                OrderPanelAdaptiveGrid.ApplyItemLayout(contents.transform, compact: false);

                // The dim checkmark squares do not fit the compact cards.
                SerializedProperty checkmarks = so.FindProperty("checkmarks");
                for (int i = 0; i < checkmarks.arraySize; i++)
                {
                    Image mark = checkmarks.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                    if (mark != null)
                        mark.gameObject.SetActive(false);
                }
                checkmarks.arraySize = 0;

                GameObject oldOverlay = so.FindProperty("completedOverlay").objectReferenceValue as GameObject;
                foreach (Transform child in contents.transform)
                {
                    if (child.name.StartsWith("overlay") && child.gameObject != oldOverlay)
                        child.gameObject.SetActive(false);
                }

                GameObject overlay = oldOverlay;
                if (overlay == null || overlay.transform.parent != contents.transform)
                    overlay = CreateImage("Completed Check", contents.transform, null, Color.white, true).gameObject;

                overlay.name = "Completed Check";
                Place(overlay.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(6f, 6f), new Vector2(36f, 36f));
                SetSprite(overlay.transform, LoadSprite("Orders/order_completed_overlay.png"), true);
                overlay.transform.SetAsLastSibling();
                overlay.SetActive(false);
                so.FindProperty("completedOverlay").objectReferenceValue = overlay;

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, OrderItemPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(OrderItemPrefab);
        }

        #endregion

        #region Toolbar

        private static RectTransform BuildToolbar(RectTransform safeZone, out Button homeButton)
        {
            RectTransform toolbar = CreateImage("Bottom Toolbar", safeZone, LoadSprite("Toolbar/bottom_toolbar_panel.png"), Color.white, false);
            toolbar.GetComponent<Image>().raycastTarget = true;
            Place(toolbar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ToolbarBottom), new Vector2(ToolbarWidth, ToolbarHeight));

            RectTransform home = CreateImage("Home Button", toolbar, LoadSprite("Toolbar/menu_button.png"), Color.white, true);
            Image homeImage = home.GetComponent<Image>();
            homeImage.raycastTarget = true;
            Place(home, Center, Center, ToolbarSlot(-ToolbarOuterSlot), new Vector2(ToolbarButtonSize, ToolbarButtonSize));

            homeButton = home.gameObject.AddComponent<Button>();
            homeButton.targetGraphic = homeImage;

            return toolbar;
        }

        private static PUUIController LayoutPowerUps(Transform root, RectTransform toolbar)
        {
            PUUIController powerUps = root.GetComponentInChildren<PUUIController>(true);
            if (powerUps == null)
            {
                Debug.LogWarning("[GameMainCanvas] No PUUIController in the source page. Power-up buttons skipped.");
                return null;
            }

            // Power-up buttons are spawned from a 175px prefab; the panel is scaled so
            // they match the toolbar slots.
            float scale = ToolbarButtonSize / PowerUpPrefabSize;
            float slotSpacing = ToolbarWidth * (ToolbarOuterSlot - ToolbarInnerSlot);

            Transform panel = powerUps.transform;
            panel.SetParent(toolbar, false);
            panel.gameObject.SetActive(true);

            Vector2 left = ToolbarSlot(-ToolbarInnerSlot);
            Vector2 right = ToolbarSlot(ToolbarOuterSlot);
            Place(panel, Center, Center, (left + right) * 0.5f, new Vector2(3f * slotSpacing / scale, 200f));
            panel.localScale = new Vector3(scale, scale, 1f);

            SerializedObject so = new SerializedObject(powerUps);
            Transform container = so.FindProperty("containerTransform").objectReferenceValue as Transform;
            if (container != null)
            {
                RectTransform containerRect = (RectTransform)container;
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.pivot = Center;
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;

                HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.padding = new RectOffset(0, 0, 0, 0);
                    layout.spacing = slotSpacing / scale - PowerUpPrefabSize;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.childControlWidth = false;
                    layout.childControlHeight = false;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }

                // Only spawned power-up buttons belong in the row. The old page kept its
                // grid/settings button here, which pushed the power-ups out of place.
                for (int i = container.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(container.GetChild(i).gameObject);

                AddPreviewPowerUps(container, so.FindProperty("itemPrefab").objectReferenceValue as GameObject);
            }

            return powerUps;
        }

        // Sample undo / hint / shuffle buttons so the toolbar is complete in the Scene
        // view. PUUIController spawns the real ones in the same row at runtime.
        private static void AddPreviewPowerUps(Transform container, GameObject itemPrefab)
        {
            if (itemPrefab == null)
                return;

            string[] art = { "Toolbar/undo_button.png", "Toolbar/hint_button.png", "Toolbar/shuffle_button.png" };
            foreach (string sprite in art)
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab, container);
                item.name = "Preview Power Up (" + System.IO.Path.GetFileNameWithoutExtension(sprite) + ")";
                item.AddComponent<EditorPreviewOnly>();

                Transform t = item.transform;
                SetSprite(Find(t, "Icon"), LoadSprite(sprite), true);
                SetActive(Find(t, "Icon"), true);
                SetActive(Find(t, "Timer"), false);
                SetActive(Find(t, "Purchase Icon"), false);

                Transform badge = Find(t, "Amount Background");
                SetActive(badge, true);
                SetSprite(badge, LoadSprite("Toolbar/powerup_count_badge.png"), true);

                TextMeshProUGUI amount = FindComponent<TextMeshProUGUI>(t, "Amount Text");
                if (amount != null)
                    amount.text = "3";
            }
        }

        private static void SetActive(Transform t, bool state)
        {
            if (t != null)
                t.gameObject.SetActive(state);
        }

        private static Vector2 ToolbarSlot(float fraction)
        {
            return new Vector2(ToolbarWidth * fraction, ToolbarHeight * (ToolbarSlotHeight - 0.5f));
        }

        private static void BuildDecor(RectTransform layer)
        {
            RectTransform plant = CreateImage("Potted Plant", layer, LoadSprite("Decor/potted_kitchen_plant.png"), Color.white, true);
            Place(plant, Vector2.zero, Vector2.zero, new Vector2(-10f, 95f), new Vector2(140f, 170f));

            RectTransform condiments = CreateImage("Condiment Tray", layer, LoadSprite("Decor/condiment_tray.png"), Color.white, true);
            Place(condiments, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(10f, 95f), new Vector2(140f, 160f));
        }

        #endregion

        #region Result pages

        private const string PopupFrameName = "Popup Frame";

        // UI Complete / UI Game Over live in UI Main Canvas. Their full-screen backgrounds
        // had been given the popup art and stretched; here the background becomes a dim
        // overlay and the popup art is shown as a centred frame with the content on it.
        // Only properties and added children change, so the prefab link is kept.
        private static void RestyleResultPages(Transform mainCanvas)
        {
            RestyleLevelComplete(mainCanvas.Find("UI Complete"));
            RestyleLevelFailed(mainCanvas.Find("UI Game Over"));
        }

        private static void RestyleLevelComplete(Transform page)
        {
            if (page == null)
                return;

            DimBackground(Find(page, "Background Image"), new Color(0.04f, 0.07f, 0.12f, 0.75f));

            // Frame 900x1200 centred at (0, 30); ribbon at 25% and cream area 33-89% of its height.
            const float frameY = 30f, frameH = 1200f;
            float top = frameY + frameH * 0.5f;

            Transform holder = Find(page, "Level Complered Holder");
            if (holder != null)
            {
                float ribbonY = top - frameH * 0.25f;
                Place(holder, Center, Center, new Vector2(0f, ribbonY), new Vector2(620f, 96f));
                AddPopupFrame(holder, "Popups/level_complete_popup.png", new Vector2(0f, frameY - ribbonY), new Vector2(900f, frameH));

                SetActive(Find(holder, "LevelText"), false);
                Transform title = Find(holder, "Completed Text");
                if (title != null)
                {
                    Fill(title, Vector2.zero, Vector2.zero);
                    StyleText(title, Color.white, 30f, 62f);
                    title.GetComponent<TextMeshProUGUI>().text = "LEVEL COMPLETED";
                }
            }

            Place(Find(page, "Reward Label"), Center, Center, new Vector2(0f, top - frameH * 0.45f), new Vector2(360f, 130f));
            StyleText(Find(page, "Reward Amount Text"), BrownText, 40f, 90f);

            // The page's own continue button (wired by UIComplete) becomes NEXT.
            Transform next = Find(page, "No Thanks Button");
            if (next != null)
            {
                Place(next, Center, Center, new Vector2(0f, top - frameH * 0.66f), new Vector2(500f, 157f));
                SetSprite(next, LoadSprite("Popups/continue_button.png"), true);
                SetActive(Find(next, "No Thanks Text"), false);
            }

            Place(Find(page, "Multiply Reward Button"), Center, Center, new Vector2(0f, top - frameH * 0.81f), new Vector2(440f, 150f));

            // A NEXT button added in the scene had no click action; the one above replaces it.
            Transform brokenNext = page.Find("next");
            Button brokenButton = brokenNext != null ? brokenNext.GetComponent<Button>() : null;
            if (brokenButton != null && brokenButton.onClick.GetPersistentEventCount() == 0)
                brokenNext.gameObject.SetActive(false);
        }

        private static void RestyleLevelFailed(Transform page)
        {
            if (page == null)
                return;

            DimBackground(Find(page, "Background Image"), new Color(0.12f, 0.03f, 0.05f, 0.78f));

            // Frame 880x1100 centred at (0, 30); "LEVEL FAILED" header at 29%, cream 38-85%.
            const float frameY = 30f, frameH = 1100f;
            float top = frameY + frameH * 0.5f;

            Transform header = Find(page, "Level Failed Text");
            if (header != null)
            {
                float headerY = top - frameH * 0.29f;
                Place(header, Center, Center, new Vector2(0f, headerY), new Vector2(600f, 110f));
                AddPopupFrame(header, "Popups/level_failed_popup.png", new Vector2(0f, frameY - headerY), new Vector2(880f, frameH));

                // The title is part of the art; the object stays for its pop-in animation.
                TextMeshProUGUI text = header.GetComponent<TextMeshProUGUI>();
                if (text != null)
                    text.enabled = false;
            }

            Place(Find(page, "Hearth Image"), Center, Center, new Vector2(0f, top - frameH * 0.52f), new Vector2(240f, 240f));

            Transform replay = Find(page, "Replay Button");
            if (replay != null)
            {
                Place(replay, Center, Center, new Vector2(0f, top - frameH * 0.68f), new Vector2(480f, 158f));
                SetSprite(replay, LoadSprite("Popups/restart_button.png"), true);
                foreach (Transform child in replay)
                    child.gameObject.SetActive(false);
            }

            Transform home = page.Find("home (2)");
            if (home != null)
            {
                Place(home, Center, Center, new Vector2(0f, top - frameH * 0.83f), new Vector2(120f, 120f));
                foreach (TMP_Text label in home.GetComponentsInChildren<TMP_Text>(true))
                    label.gameObject.SetActive(false);
            }
        }

        private static void DimBackground(Transform background, Color color)
        {
            if (background == null)
                return;

            Fill(background, Vector2.zero, Vector2.zero);
            Image image = background.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.color = color;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.raycastTarget = true;
            }
        }

        private static void AddPopupFrame(Transform parent, string sprite, Vector2 position, Vector2 size)
        {
            Transform previous = parent.Find(PopupFrameName);
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);

            RectTransform frame = CreateImage(PopupFrameName, parent, LoadSprite(sprite), Color.white, true);
            Place(frame, Center, Center, position, size);
            frame.SetAsFirstSibling();
        }

        private static void RecordPrefabInstanceEdits(Transform root)
        {
            if (root == null)
                return;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(t))
                    continue;

                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                foreach (Component component in t.GetComponents<Component>())
                {
                    if (component != null)
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
        }

        #endregion

        #region Validation

        private static void Validate(GameObject root, bool logSuccess)
        {
            List<string> problems = new List<string>();

            UIGame game = root.GetComponent<UIGame>();
            if (game == null)
                problems.Add("UIGame is missing on the root.");
            if (root.GetComponent<GameMainCanvas>() == null)
                problems.Add("GameMainCanvas is missing on the root.");
            if (root.transform.parent != null)
                problems.Add("The canvas must be a root object.");
            GameObject background = FindRoot(BackgroundCanvasName);
            if (background == null || background.GetComponent<KitchenBackdrop>() == null)
                problems.Add("'" + BackgroundCanvasName + "' with KitchenBackdrop is missing.");
            else if (background.GetComponent<GraphicRaycaster>() != null)
                problems.Add("'" + BackgroundCanvasName + "' must not have a GraphicRaycaster.");

            if (FindRoots(CanvasName).Count != 1)
                problems.Add("There must be exactly one '" + CanvasName + "'.");

            if (game != null)
            {
                SerializedObject so = new SerializedObject(game);
                foreach (string field in new[] { "safeZoneTransform", "powerUpsUIController", "replayButton", "exitPopUp", "levelText", "orderPanel", "devOverlay" })
                {
                    Object value = so.FindProperty(field).objectReferenceValue;
                    if (value == null)
                        problems.Add("UIGame." + field + " is not assigned.");
                    else if (!IsInside(value, root.transform))
                        problems.Add("UIGame." + field + " points outside this canvas.");
                }
            }

            // Any reference from this canvas into UI Main Canvas would make a button or
            // popup act on the old, hidden pages.
            GameObject mainCanvas = FindRoot(MainCanvasName);
            if (mainCanvas != null)
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;

                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                            continue;

                        if (IsInside(property.objectReferenceValue, mainCanvas.transform))
                            problems.Add(component.GetType().Name + " on '" + component.name + "' ." + property.propertyPath + " points into UI Main Canvas.");
                    }
                }
            }

            if (problems.Count == 0)
            {
                if (logSuccess)
                    Debug.Log("[GameMainCanvas] VALID: separate canvas is wired to its own objects only.");
            }
            else
            {
                Debug.LogError("[GameMainCanvas] Problems found:\n- " + string.Join("\n- ", problems));
            }
        }

        private static bool IsInside(Object value, Transform root)
        {
            Transform t = value is GameObject go ? go.transform : value is Component c ? c.transform : null;
            return t != null && (t == root || t.IsChildOf(root));
        }

        #endregion

        #region Sprites

        private static void ConfigureSprites()
        {
            SetSpriteBorder(OrdersFrameSprite, new Vector4(100f, 38f, 95f, 212f));
            SetSpriteBorder(OrderCardSprite, new Vector4(180f, 130f, 180f, 130f));
        }

        // border = (left, bottom, right, top) in sprite pixels.
        private static void SetSpriteBorder(string relativePath, Vector4 border)
        {
            string path = ArtFolder + "/" + relativePath;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[GameMainCanvas] Sprite not found: " + path);
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.spriteBorder == border)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string relativePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + relativePath);
            if (sprite == null)
                Debug.LogWarning("[GameMainCanvas] Sprite not found: " + ArtFolder + "/" + relativePath);
            return sprite;
        }

        #endregion

        #region Helpers

        private static GameObject FindRoot(string name)
        {
            List<GameObject> roots = FindRoots(name);
            return roots.Count > 0 ? roots[0] : null;
        }

        private static List<GameObject> FindRoots(string name)
        {
            List<GameObject> result = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return result;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    result.Add(root);
            }

            return result;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;

                Transform match = Find(child, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            // TryGetComponent returns a real null (not the editor's fake null), so callers
            // can fall back with ??.
            Transform t = Find(root, name);
            return t != null && t.TryGetComponent(out T component) ? component : null;
        }

        private static void DestroyIfExists(Transform t)
        {
            if (t != null)
                Object.DestroyImmediate(t.gameObject);
        }

        private static void SetReferences(Object target, params (string field, Object value)[] values)
        {
            if (target == null)
                return;

            SerializedObject so = new SerializedObject(target);
            foreach ((string field, Object value) in values)
            {
                SerializedProperty property = so.FindProperty(field);
                if (property == null)
                {
                    Debug.LogWarning("[GameMainCanvas] " + target.GetType().Name + " has no field '" + field + "'.");
                    continue;
                }

                if (value == null)
                    Debug.LogWarning("[GameMainCanvas] " + target.GetType().Name + "." + field + " could not be found in the new canvas.");

                property.objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                return;

            property.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return rect;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();

            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, TMP_FontAsset font, Color color, float minSize, float maxSize)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();

            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            StyleText(rect, color, minSize, maxSize);
            return text;
        }

        private static void StyleText(Transform t, Color color, float minSize, float maxSize)
        {
            TextMeshProUGUI text = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (text == null)
                return;

            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.fontSize = maxSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        private static void Place(Transform t, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rect = t as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Fill(Transform t, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = t as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            Fill(rect, Vector2.zero, Vector2.zero);
        }

        private static void SetSprite(Transform t, Sprite sprite, bool preserveAspect)
        {
            SetSprite(t != null ? t.GetComponent<Image>() : null, sprite, preserveAspect);
        }

        private static void SetSprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
        }

        #endregion
    }
}
#endif
