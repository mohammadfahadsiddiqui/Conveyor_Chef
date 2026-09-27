#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.EditorTools
{
    /// <summary>
    /// Builds a completely new UIGame page while preserving the original page as
    /// an inactive legacy backup in the hierarchy.
    ///
    /// Final hierarchy:
    /// UI Main Canvas
    ///   OLD GAME UI [Legacy Disabled]
    ///     OLD UI Game [Legacy Disabled]
    ///   NEW UI Game   <-- the only direct UIGame page scanned by UIController
    ///
    /// All gameplay systems remain the original systems. The builder copies the
    /// old functional Tutorial, power-up, purchase, order and quit-popup objects
    /// into the new page, then rewires the existing UIGame and TutorialController
    /// references to those copies.
    /// </summary>
    public static class GameSceneUIBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/Game.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/GameUI";
        private const string AssetPackFileName = "ConveyorChef_GameUI_ArtPack.zip";

        private const string NewPageName = "NEW UI Game";
        private const string LegacyContainerName = "OLD GAME UI [Legacy Disabled]";
        private const string LegacyPageName = "OLD UI Game [Legacy Disabled]";
        private const int LayoutVersion = 2;

        private const string LivesIndicatorPrefab =
            "Assets/Project Data/Watermelon Core/Extra Components/Lives System/Prefabs/Lives Indicator.prefab";
        private const string AddLivesPanelPrefab =
            "Assets/Project Data/Watermelon Core/Extra Components/Lives System/Prefabs/Add Lives Panel.prefab";
        private const string CurrencyPanelPrefab =
            "Assets/Project Data/Watermelon Core/Modules/Currencies Module/Prefabs/Currency Panel Simple.prefab";

        private static readonly string[] RequiredAssets =
        {
            "TopHUD/heart_icon.png",
            "TopHUD/life_counter_panel.png",
            "TopHUD/green_plus_button.png",
            "TopHUD/level_title_panel.png",
            "TopHUD/chef_hat_icon.png",
            "TopHUD/coin_counter_panel.png",
            "TopHUD/coin_icon.png",
            "TopHUD/diamond_counter_panel.png",
            "TopHUD/diamond_icon.png",
            "TopHUD/pause_button.png",
            "TopHUD/settings_button.png",
            "Orders/orders_panel.png",
            "Orders/order_item_slot.png",
            "Orders/order_completed_overlay.png",
            "Board/serving_slots_panel.png",
            "Board/serving_slot.png",
            "Board/game_board_background.png",
            "Board/game_tile_slot.png",
            "Toolbar/bottom_toolbar_panel.png",
            "Toolbar/menu_button.png",
            "Toolbar/undo_button.png",
            "Toolbar/hint_button.png",
            "Toolbar/shuffle_button.png",
            "Toolbar/powerup_count_badge.png",
            "Toolbar/home_button.png",
            "Kitchen/kitchen_gameplay_background.png",
            "Kitchen/kitchen_counter_strip.png",
            "Kitchen/conveyor_belt_background.png",
            "Kitchen/conveyor_rail_frame.png",
            "Kitchen/purple_serving_tray.png",
            "Popups/pause_popup_panel.png",
            "Popups/level_complete_popup.png",
            "Popups/level_failed_popup.png",
            "Popups/restart_button.png",
            "Popups/continue_button.png",
            "States/button_pressed_overlay.png",
            "States/button_disabled_overlay.png",
            "States/panel_glow.png",
            "States/notification_badge.png",
            "States/objective_highlight.png",
            "Decor/kitchen_chalkboard_menu.png",
            "Decor/stocked_kitchen_shelf.png",
            "Decor/dome_kitchen_oven.png",
            "Decor/condiment_tray.png",
            "Decor/metal_stovetop_station.png",
            "Decor/potted_kitchen_plant.png",
            "Decor/hanging_pendant_lamp.png",
            "Decor/wooden_cutting_board.png",
            "Decor/chef_apron.png",
            "Decor/industrial_board_frame.png",
        };

        [MenuItem("Conveyor Chef/Game Scene/0. Import Generated Game UI Art Pack", priority = 0)]
        public static void ImportGeneratedArtPack()
        {
            string zipPath = EditorUtility.OpenFilePanel(
                "Select " + AssetPackFileName,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "zip");

            if (string.IsNullOrWhiteSpace(zipPath))
                return;

            if (!ExtractAssetPack(zipPath))
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = MissingAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Game UI Assets Missing",
                    "The selected ZIP is missing:\n\n- " + string.Join("\n- ", missing),
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Game UI Art Ready",
                "All 50 Game UI sprites are imported.\n\nNow use:\n" +
                "Conveyor Chef > Game Scene > 2. Build NEW Complete Game UI",
                "OK");
        }

        [MenuItem("Conveyor Chef/Game Scene/1. Restore Original Game UI", priority = 1)]
        public static void RestoreOriginalGameUI()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool changed = RestoreLegacyInternal(scene);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            GameObject oldPage = FindDirectPage<UIGame>(FindMainCanvas()?.transform);
            if (oldPage != null)
                Selection.activeGameObject = oldPage;

            Debug.Log("[GameUI] Restored the original UI Game page as the active direct child of UI Main Canvas.");
        }

        [MenuItem("Conveyor Chef/Game Scene/2. Build NEW Complete Game UI", priority = 2)]
        public static void BuildNewCompleteGameUI()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = MissingAssets();
            if (missing.Count > 0 && TryImportAssetPackFromKnownLocations())
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ImportSprites();
                missing = MissingAssets();
            }

            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Game UI Art Pack Required",
                    "The NEW Game UI was not built because " + missing.Count +
                    " sprites are still missing.\n\nUse:\n" +
                    "Conveyor Chef > Game Scene > 0. Import Generated Game UI Art Pack\n\nMissing:\n- " +
                    string.Join("\n- ", missing),
                    "OK");
                return;
            }

            // Always go back to the original authored state first. This makes the
            // operation deterministic and protects against partial previous bakes.
            RestoreLegacyInternal(scene);

            Canvas mainCanvas = FindMainCanvas();
            if (mainCanvas == null)
            {
                Debug.LogError("[GameUI] UI Main Canvas was not found. No changes were made.");
                return;
            }

            GameObject oldPage = FindDirectPage<UIGame>(mainCanvas.transform);
            if (oldPage == null)
            {
                Debug.LogError("[GameUI] Original UI Game page was not found. No changes were made.");
                return;
            }

            BuildFromLegacy(scene, mainCanvas, oldPage);
        }

        [MenuItem("Conveyor Chef/Game Scene/3. Focus NEW UI Game", priority = 3)]
        public static void FocusNewUI()
        {
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
                return;

            Transform child = canvas.transform.Find(NewPageName);
            if (child != null)
            {
                Selection.activeGameObject = child.gameObject;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private static void BuildFromLegacy(Scene scene, Canvas mainCanvas, GameObject oldPage)
        {
            Transform oldPageTransform = oldPage.transform;

            Transform oldSafeZone = FindDescendant(oldPageTransform, "Safe Zone");
            Transform oldPowerUpPanel = FindDescendant(oldPageTransform, "Power Up Panel");
            Transform oldPurchasePanel = FindDescendantByPrefix(oldPageTransform, "Power Up Purchase Panel");
            Transform oldQuitPopup = FindDescendantByPrefix(oldPageTransform, "Quit Pop Up");
            Transform oldTutorialCanvas = FindDescendant(oldPageTransform, "Tutorial Canvas");
            Transform oldDevOverlay = FindDescendant(oldPageTransform, "Dev Overlay");
            Transform oldOrderPanel = FindDescendant(oldPageTransform, "OrderPanel");

            if (oldSafeZone == null || oldPowerUpPanel == null || oldPurchasePanel == null ||
                oldQuitPopup == null || oldDevOverlay == null || oldOrderPanel == null)
            {
                Debug.LogError(
                    "[GameUI] The original UI Game hierarchy is incomplete. " +
                    "The old page was left untouched.");
                return;
            }

            // Create legacy backup container first. The original UIGame page is moved
            // under it so UIController will no longer discover it as a direct page.
            RectTransform legacyContainer = CreateRect(LegacyContainerName, mainCanvas.transform);
            Stretch(legacyContainer);
            legacyContainer.SetAsFirstSibling();

            oldPageTransform.SetParent(legacyContainer, false);
            oldPage.name = LegacyPageName;

            // New page must be a DIRECT child of UI Main Canvas because UIController
            // scans direct children for UIPage components.
            GameObject newPage = new GameObject(
                NewPageName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(UIGame),
                typeof(GameUIResponsiveLayout),
                typeof(GameSceneHUDController));

            newPage.layer = 5;
            RectTransform pageRect = newPage.GetComponent<RectTransform>();
            pageRect.SetParent(mainCanvas.transform, false);
            Stretch(pageRect);

            Canvas pageCanvas = newPage.GetComponent<Canvas>();
            pageCanvas.overrideSorting = false;
            pageCanvas.enabled = true;

            GameUIResponsiveLayout marker = newPage.GetComponent<GameUIResponsiveLayout>();
            marker.EditorConfigure(LayoutVersion);

            RectTransform safeZone = CreateRect("Safe Zone", pageRect);
            Stretch(safeZone);

            BuildVisualShell(
                safeZone,
                out TextMeshProUGUI levelText,
                out Button pauseButton,
                out Button settingsButton,
                out Button homeButton,
                out Button coinPlusButton,
                out Button diamondPlusButton);

            // Lives uses the original LivesIndicator/AddLivesPanel logic.
            BuildLivesHUD(safeZone);

            // Currency counters use original CurrencyUIPanelSimple logic.
            BuildCurrencyHUD(
                safeZone,
                CurrencyType.Coins,
                "Coin Counter",
                "TopHUD/coin_counter_panel.png",
                new Vector2(-330f, -58f),
                out coinPlusButton);

            BuildCurrencyHUD(
                safeZone,
                CurrencyType.Diamonds,
                "Diamond Counter",
                "TopHUD/diamond_counter_panel.png",
                new Vector2(-330f, -145f),
                out diamondPlusButton);

            // Copy functional systems from the old page BEFORE disabling it.
            GameObject powerUpClone = CloneLegacyObject(oldPowerUpPanel, safeZone, "Power Up Panel");
            GameObject purchaseClone = CloneLegacyObject(oldPurchasePanel, pageRect, "Power Up Purchase Panel");
            GameObject quitClone = CloneLegacyObject(oldQuitPopup, pageRect, "Quit Pop Up");
            GameObject devClone = CloneLegacyObject(oldDevOverlay, safeZone, "Dev Overlay");
            GameObject orderClone = CloneLegacyObject(oldOrderPanel, safeZone, "OrderPanel");
            GameObject tutorialClone = oldTutorialCanvas != null
                ? CloneLegacyObject(oldTutorialCanvas, pageRect, "Tutorial Canvas")
                : null;

            PUUIController powerUps = powerUpClone != null ? powerUpClone.GetComponent<PUUIController>() : null;
            PUUIPurchasePanel purchase = purchaseClone != null ? purchaseClone.GetComponent<PUUIPurchasePanel>() : null;
            UILevelQuitPopUp quitPopup = quitClone != null ? quitClone.GetComponent<UILevelQuitPopUp>() : null;
            Watermelon.BusStop.UIOrderPanel orderPanel =
                orderClone != null ? orderClone.GetComponent<Watermelon.BusStop.UIOrderPanel>() : null;

            if (powerUps == null || purchase == null || quitPopup == null || orderPanel == null)
            {
                UnityEngine.Object.DestroyImmediate(newPage);
                UnityEngine.Object.DestroyImmediate(legacyContainer.gameObject);

                oldPageTransform.SetParent(mainCanvas.transform, false);
                oldPage.name = "UI Game";
                oldPage.SetActive(true);

                Debug.LogError(
                    "[GameUI] Failed to duplicate one of the old functional UI systems. " +
                    "The original UI Game was restored.");
                return;
            }

            ConfigurePowerUps(powerUpClone, powerUps, purchase);
            ConfigureOrders(orderClone);
            ConfigureQuitPopup(quitClone);
            ConfigureTutorialReference(tutorialClone);

            if (devClone != null)
                devClone.SetActive(false);

            // Wire the original UIGame logic to the new authored objects.
            UIGame newGame = newPage.GetComponent<UIGame>();
            SerializedObject gameSO = new SerializedObject(newGame);
            gameSO.FindProperty("safeZoneTransform").objectReferenceValue = safeZone;
            gameSO.FindProperty("powerUpsUIController").objectReferenceValue = powerUps;
            gameSO.FindProperty("replayButton").objectReferenceValue = pauseButton;
            gameSO.FindProperty("exitPopUp").objectReferenceValue = quitPopup;
            gameSO.FindProperty("levelText").objectReferenceValue = levelText;
            gameSO.FindProperty("orderPanel").objectReferenceValue = orderPanel;
            gameSO.FindProperty("devOverlay").objectReferenceValue =
                devClone != null ? devClone : newPage;
            gameSO.ApplyModifiedPropertiesWithoutUndo();

            GameSceneHUDController bridge = newPage.GetComponent<GameSceneHUDController>();
            bridge.EditorConfigure(
                coinPlusButton,
                diamondPlusButton,
                settingsButton,
                homeButton,
                quitPopup,
                LoadSprite("Toolbar/undo_button.png"),
                LoadSprite("Toolbar/hint_button.png"),
                LoadSprite("Toolbar/shuffle_button.png"),
                LoadSprite("Toolbar/powerup_count_badge.png"));

            ApplyOrderItemPrefabArtwork();
            ApplyExistingResultPageArtwork();

            // Only after all new references are valid do we disable the old page.
            oldPage.SetActive(false);

            EditorUtility.SetDirty(newPage);
            EditorUtility.SetDirty(oldPage);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = newPage;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log(
                "[GameUI] NEW UI Game built successfully. The original UI Game is preserved " +
                "under OLD GAME UI [Legacy Disabled] and is inactive. Existing gameplay, " +
                "tutorial, order, power-up, purchase and quit logic is connected to NEW UI Game.");
        }

        private static void BuildVisualShell(
            RectTransform safeZone,
            out TextMeshProUGUI levelText,
            out Button pauseButton,
            out Button settingsButton,
            out Button homeButton,
            out Button coinPlusButton,
            out Button diamondPlusButton)
        {
            coinPlusButton = null;
            diamondPlusButton = null;

            RectTransform artRoot = CreateRect("Game UI Artwork", safeZone);
            Stretch(artRoot);
            artRoot.SetAsFirstSibling();

            RectTransform topHud = CreateRect("Top HUD", artRoot);
            topHud.anchorMin = new Vector2(0f, 1f);
            topHud.anchorMax = new Vector2(1f, 1f);
            topHud.pivot = new Vector2(0.5f, 1f);
            topHud.anchoredPosition = Vector2.zero;
            topHud.sizeDelta = new Vector2(0f, 235f);

            RectTransform levelPanel = CreateImage(
                "Level Counter",
                topHud,
                LoadSprite("TopHUD/level_title_panel.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-45f, -92f),
                new Vector2(315f, 108f),
                false);

            CreateImage(
                "Chef Hat",
                levelPanel,
                LoadSprite("TopHUD/chef_hat_icon.png"),
                Center,
                Center,
                new Vector2(0f, 48f),
                new Vector2(70f, 70f),
                true);

            levelText = CreateTMP(
                "Level Value",
                levelPanel,
                "LEVEL 1",
                40f,
                FontStyles.Bold,
                new Vector2(0f, -5f),
                new Vector2(250f, 65f));

            levelText.color = Color.white;
            levelText.outlineWidth = 0.15f;
            levelText.outlineColor = new Color32(25, 70, 128, 255);

            pauseButton = CreateButton(
                "Pause Button",
                topHud,
                LoadSprite("TopHUD/pause_button.png"),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-86f, -66f),
                new Vector2(88f, 88f));

            settingsButton = CreateButton(
                "Settings Button",
                topHud,
                LoadSprite("TopHUD/settings_button.png"),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-86f, -164f),
                new Vector2(82f, 82f));

            // Decorative kitchen strip. Kept above the actual interactive world so it
            // never covers the central puzzle board.
            RectTransform kitchenStrip = CreateImage(
                "Kitchen Counter Strip",
                artRoot,
                LoadSprite("Kitchen/kitchen_counter_strip.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -320f),
                new Vector2(1010f, 250f),
                false);
            kitchenStrip.GetComponent<Image>().raycastTarget = false;

            RectTransform conveyor = CreateRect("Conveyor UI", artRoot);
            conveyor.anchorMin = new Vector2(0.5f, 1f);
            conveyor.anchorMax = new Vector2(0.5f, 1f);
            conveyor.pivot = new Vector2(0.5f, 1f);
            conveyor.anchoredPosition = new Vector2(0f, -455f);
            conveyor.sizeDelta = new Vector2(930f, 230f);

            CreateImage(
                "Conveyor Belt",
                conveyor,
                LoadSprite("Kitchen/conveyor_belt_background.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                Vector2.zero,
                false);

            CreateImage(
                "Conveyor Rail Frame",
                conveyor,
                LoadSprite("Kitchen/conveyor_rail_frame.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                Vector2.zero,
                false);

            CreateImage(
                "Purple Serving Tray",
                conveyor,
                LoadSprite("Kitchen/purple_serving_tray.png"),
                Center,
                Center,
                new Vector2(-20f, -5f),
                new Vector2(250f, 112f),
                true);

            RectTransform serving = CreateImage(
                "Serving Slots Panel",
                artRoot,
                LoadSprite("Board/serving_slots_panel.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-70f, -690f),
                new Vector2(760f, 128f),
                false);

            float startX = -294f;
            const float spacing = 98f;
            for (int i = 0; i < 7; i++)
            {
                CreateImage(
                    "Serving Slot " + (i + 1),
                    serving,
                    LoadSprite("Board/serving_slot.png"),
                    Center,
                    Center,
                    new Vector2(startX + spacing * i, 0f),
                    new Vector2(82f, 82f),
                    true);
            }

            RectTransform objectiveArt = CreateRect("Objective Panel Art", artRoot);
            objectiveArt.anchorMin = new Vector2(1f, 1f);
            objectiveArt.anchorMax = new Vector2(1f, 1f);
            objectiveArt.pivot = new Vector2(1f, 1f);
            objectiveArt.anchoredPosition = new Vector2(-34f, -270f);
            objectiveArt.sizeDelta = new Vector2(300f, 420f);

            CreateImage(
                "Objective Highlight",
                objectiveArt,
                LoadSprite("States/objective_highlight.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                Vector2.zero,
                false);

            CreateImage(
                "Orders Panel",
                objectiveArt,
                LoadSprite("Orders/orders_panel.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                new Vector2(-12f, -12f),
                false);

            TextMeshProUGUI ordersTitle = CreateTMP(
                "Orders Title",
                objectiveArt,
                "ORDERS",
                34f,
                FontStyles.Bold,
                new Vector2(0f, 160f),
                new Vector2(240f, 55f));
            ordersTitle.color = new Color32(78, 43, 22, 255);

            // Subtle frame only; never place an opaque UI image over the puzzle world.
            RectTransform boardFrame = CreateImage(
                "Gameplay Board Frame",
                artRoot,
                LoadSprite("Decor/industrial_board_frame.png"),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 465f),
                new Vector2(830f, 810f),
                false);
            Image frameImage = boardFrame.GetComponent<Image>();
            frameImage.color = new Color(1f, 1f, 1f, 0.15f);
            frameImage.raycastTarget = false;
            boardFrame.SetAsFirstSibling();

            RectTransform toolbar = CreateImage(
                "Bottom Toolbar",
                artRoot,
                LoadSprite("Toolbar/bottom_toolbar_panel.png"),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 92f),
                new Vector2(780f, 180f),
                false);

            CreateImage(
                "Toolbar Glow",
                toolbar,
                LoadSprite("States/panel_glow.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                new Vector2(45f, 35f),
                false);

            homeButton = CreateButton(
                "Home Button",
                toolbar,
                LoadSprite("Toolbar/home_button.png"),
                Center,
                Center,
                new Vector2(-290f, 0f),
                new Vector2(120f, 120f));

            BuildArtLibrary(artRoot);
        }

        private static void BuildLivesHUD(RectTransform safeZone)
        {
            GameObject indicatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LivesIndicatorPrefab);
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AddLivesPanelPrefab);

            if (indicatorPrefab == null || panelPrefab == null)
                return;

            GameObject addLivesPanel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, safeZone);
            addLivesPanel.name = "Add Lives Panel";
            RectTransform addLivesRect = addLivesPanel.transform as RectTransform;
            if (addLivesRect != null)
                Stretch(addLivesRect);

            GameObject indicator = (GameObject)PrefabUtility.InstantiatePrefab(indicatorPrefab, safeZone);
            indicator.name = "Life Counter";

            RectTransform rect = indicator.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(35f, -40f);
            rect.sizeDelta = new Vector2(300f, 88f);
            rect.localScale = Vector3.one;

            Image background = indicator.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = LoadSprite("TopHUD/life_counter_panel.png");
                background.color = Color.white;
                background.type = Image.Type.Simple;
            }

            Transform heart = FindDescendant(indicator.transform, "Heart Image");
            SetImageSprite(heart, LoadSprite("TopHUD/heart_icon.png"), true);

            Transform add = FindDescendant(indicator.transform, "Add Button");
            SetImageSprite(add, LoadSprite("TopHUD/green_plus_button.png"), true);

            LivesIndicator livesIndicator = indicator.GetComponent<LivesIndicator>();
            AddLivesPanel livesPanel = addLivesPanel.GetComponent<AddLivesPanel>();
            if (livesIndicator != null && livesPanel != null)
            {
                SerializedObject so = new SerializedObject(livesIndicator);
                so.FindProperty("addLivesPanel").objectReferenceValue = livesPanel;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            addLivesPanel.SetActive(false);
        }

        private static void BuildCurrencyHUD(
            RectTransform safeZone,
            CurrencyType type,
            string objectName,
            string backgroundAsset,
            Vector2 position,
            out Button addButton)
        {
            addButton = null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyPanelPrefab);
            if (prefab == null)
                return;

            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, safeZone);
            panel.name = objectName;

            RectTransform rect = panel.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(250f, 74f);
            rect.localScale = Vector3.one;

            Image background = panel.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = LoadSprite(backgroundAsset);
                background.color = Color.white;
                background.type = Image.Type.Simple;
            }

            CurrencyUIPanelSimple currencyPanel = panel.GetComponent<CurrencyUIPanelSimple>();
            if (currencyPanel != null)
            {
                SerializedObject so = new SerializedObject(currencyPanel);
                SerializedProperty currencyType = so.FindProperty("currencyType");
                if (currencyType != null)
                    currencyType.enumValueIndex = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform plus = FindDescendant(panel.transform, "Add Button");
            SetImageSprite(plus, LoadSprite("TopHUD/green_plus_button.png"), true);
            if (plus != null)
                addButton = plus.GetComponent<Button>();

            Transform icon = FindDescendant(panel.transform, "Currency Icon");
            if (icon != null)
            {
                Image iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = LoadSprite(
                        type == CurrencyType.Coins
                            ? "TopHUD/coin_icon.png"
                            : "TopHUD/diamond_icon.png");
                    iconImage.preserveAspect = true;
                }
            }

            Transform amount = FindDescendant(panel.transform, "Amount Text");
            if (amount != null)
            {
                TMP_Text text = amount.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.fontStyle = FontStyles.Bold;
                    text.color = new Color32(72, 47, 32, 255);
                }
            }
        }

        private static void ConfigurePowerUps(
            GameObject powerUpObject,
            PUUIController controller,
            PUUIPurchasePanel purchase)
        {
            RectTransform rect = powerUpObject.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(105f, 45f);
                rect.sizeDelta = new Vector2(545f, 165f);
                rect.localScale = Vector3.one;
            }

            Transform container = FindDescendant(powerUpObject.transform, "Container");
            RectTransform containerRect = container as RectTransform;
            if (containerRect != null)
            {
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;

                HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 18f;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }
            }

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("powerUpPurchasePanel").objectReferenceValue = purchase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureOrders(GameObject orderObject)
        {
            RectTransform rect = orderObject.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(-183f, -335f);
                rect.localScale = Vector3.one;
            }

            Image image = orderObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = false;
            }

            VerticalLayoutGroup layout = orderObject.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 9f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childAlignment = TextAnchor.UpperCenter;
            }
        }

        private static void ConfigureQuitPopup(GameObject popup)
        {
            SetImageSprite(FindDescendant(popup.transform, "Panel Back"), LoadSprite("Popups/pause_popup_panel.png"), false);

            Transform quitButton = FindDescendant(popup.transform, "Quit Button");
            Transform closeButton = FindDescendant(popup.transform, "Close Button");

            SetImageSprite(quitButton, LoadSprite("Popups/restart_button.png"), true);
            SetImageSprite(closeButton, LoadSprite("Popups/continue_button.png"), true);

            HideTMPChildren(quitButton);
            HideTMPChildren(closeButton);

            popup.SetActive(false);
        }

        private static void ConfigureTutorialReference(GameObject tutorialClone)
        {
            if (tutorialClone == null)
                return;

            TutorialCanvasController canvasController = tutorialClone.GetComponent<TutorialCanvasController>();
            TutorialController tutorialController =
                UnityEngine.Object.FindFirstObjectByType<TutorialController>(FindObjectsInactive.Include);

            if (canvasController == null || tutorialController == null)
                return;

            SerializedObject so = new SerializedObject(tutorialController);
            SerializedProperty prop = so.FindProperty("tutorialCanvasController");
            if (prop != null)
            {
                prop.objectReferenceValue = canvasController;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tutorialController);
            }
        }

        private static void ApplyExistingResultPageArtwork()
        {
            GameObject complete = FindObjectByExactNameInScene("UI Complete");
            if (complete != null)
            {
                SetImageSprite(
                    FindDescendant(complete.transform, "Background Image"),
                    LoadSprite("Popups/level_complete_popup.png"),
                    true);

                Transform continueButton = FindDescendant(complete.transform, "No Thanks Button");
                SetImageSprite(continueButton, LoadSprite("Popups/continue_button.png"), true);
                HideTMPChildren(continueButton);

                SetImageSprite(
                    FindDescendant(complete.transform, "Go Home"),
                    LoadSprite("Toolbar/home_button.png"),
                    true);
            }

            GameObject gameOver = FindObjectByExactNameInScene("UI Game Over");
            if (gameOver != null)
            {
                SetImageSprite(
                    FindDescendant(gameOver.transform, "Background Image"),
                    LoadSprite("Popups/level_failed_popup.png"),
                    true);

                Transform replay = FindDescendant(gameOver.transform, "Replay Button");
                SetImageSprite(replay, LoadSprite("Popups/restart_button.png"), true);
                HideTMPChildren(replay);

                SetImageSprite(
                    FindDescendant(gameOver.transform, "home (2)"),
                    LoadSprite("Toolbar/home_button.png"),
                    true);
            }
        }

        private static void ApplyOrderItemPrefabArtwork()
        {
            const string prefabPath = "Assets/Project Data/Game/Prefabs/OrderItem.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return;

            try
            {
                Image background = root.GetComponent<Image>();
                if (background != null)
                {
                    background.sprite = LoadSprite("Orders/order_item_slot.png");
                    background.color = Color.white;
                    background.type = Image.Type.Simple;
                    background.preserveAspect = false;
                }

                RectTransform rect = root.transform as RectTransform;
                if (rect != null)
                    rect.sizeDelta = new Vector2(210f, 92f);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildArtLibrary(RectTransform artRoot)
        {
            RectTransform library = CreateRect("Extra Generated Art [Disabled]", artRoot);
            Stretch(library);
            library.gameObject.SetActive(false);

            string[] extraAssets =
            {
                "Kitchen/kitchen_gameplay_background.png",
                "Board/game_board_background.png",
                "Board/game_tile_slot.png",
                "Orders/order_item_slot.png",
                "Orders/order_completed_overlay.png",
                "Toolbar/menu_button.png",
                "States/button_pressed_overlay.png",
                "States/button_disabled_overlay.png",
                "States/notification_badge.png",
                "Decor/kitchen_chalkboard_menu.png",
                "Decor/stocked_kitchen_shelf.png",
                "Decor/dome_kitchen_oven.png",
                "Decor/condiment_tray.png",
                "Decor/metal_stovetop_station.png",
                "Decor/potted_kitchen_plant.png",
                "Decor/hanging_pendant_lamp.png",
                "Decor/wooden_cutting_board.png",
                "Decor/chef_apron.png",
            };

            int columns = 4;
            float cellW = 245f;
            float cellH = 205f;
            float startX = -367.5f;
            float startY = 760f;

            for (int i = 0; i < extraAssets.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;

                RectTransform holder = CreateRect(Path.GetFileNameWithoutExtension(extraAssets[i]), library);
                holder.anchorMin = Center;
                holder.anchorMax = Center;
                holder.pivot = Center;
                holder.anchoredPosition = new Vector2(startX + col * cellW, startY - row * cellH);
                holder.sizeDelta = new Vector2(210f, 170f);

                CreateImage(
                    "Artwork",
                    holder,
                    LoadSprite(extraAssets[i]),
                    Center,
                    Center,
                    new Vector2(0f, 10f),
                    new Vector2(180f, 135f),
                    true);
            }
        }

        private static bool RestoreLegacyInternal(Scene scene)
        {
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
                return false;

            bool changed = false;

            Transform newPage = canvas.transform.Find(NewPageName);
            if (newPage != null)
            {
                UnityEngine.Object.DestroyImmediate(newPage.gameObject);
                changed = true;
            }

            Transform legacyContainer = canvas.transform.Find(LegacyContainerName);
            if (legacyContainer != null)
            {
                UIGame oldGame = legacyContainer.GetComponentInChildren<UIGame>(true);
                if (oldGame != null)
                {
                    oldGame.transform.SetParent(canvas.transform, false);
                    oldGame.gameObject.name = "UI Game";
                    oldGame.gameObject.SetActive(true);
                    changed = true;
                }

                UnityEngine.Object.DestroyImmediate(legacyContainer.gameObject);
                changed = true;
            }

            // Clean the earlier experimental "NEW Game UI" overlay if it exists
            // inside the original page.
            GameObject directOld = FindDirectPage<UIGame>(canvas.transform);
            if (directOld != null)
            {
                Transform previousOverlay = FindDescendant(directOld.transform, "NEW Game UI");
                if (previousOverlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(previousOverlay.gameObject);
                    changed = true;
                }

                directOld.name = "UI Game";
                directOld.SetActive(true);
            }

            if (changed)
                EditorUtility.SetDirty(canvas.gameObject);

            return changed;
        }

        private static GameObject CloneLegacyObject(Transform source, Transform parent, string name)
        {
            if (source == null)
                return null;

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            clone.name = name;
            clone.transform.localScale = Vector3.one;
            return clone;
        }

        private static Canvas FindMainCanvas()
        {
            GameObject go = FindObjectByExactNameInScene("UI Main Canvas");
            return go != null ? go.GetComponent<Canvas>() : null;
        }

        private static GameObject FindDirectPage<T>(Transform canvas) where T : Component
        {
            if (canvas == null)
                return null;

            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child.GetComponent<T>() != null)
                    return child.gameObject;
            }

            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool preserveAspect)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Center;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;

            return rect;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            RectTransform rect = CreateImage(
                name, parent, sprite, anchorMin, anchorMax, anchoredPosition, sizeDelta, true);

            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static TextMeshProUGUI CreateTMP(
            string name,
            Transform parent,
            string value,
            float fontSize,
            FontStyles style,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            go.layer = parent.gameObject.layer;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Center;
            rect.anchorMax = Center;
            rect.pivot = Center;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, fontSize * 0.58f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            return text;
        }

        private static void HideTMPChildren(Transform root)
        {
            if (root == null)
                return;

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.gameObject.SetActive(false);
                EditorUtility.SetDirty(text.gameObject);
            }
        }

        private static void SetImageSprite(Transform target, Sprite sprite, bool preserveAspect)
        {
            if (target == null || sprite == null)
                return;

            Image image = target.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            EditorUtility.SetDirty(image);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = StretchMin;
            rect.anchorMax = StretchMax;
            rect.pivot = Center;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static GameObject FindObjectByExactNameInScene(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindSelfOrDescendant(root.transform, name);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindSelfOrDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root)
            {
                Transform match = FindSelfOrDescendant(child, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;

                Transform match = FindDescendant(child, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDescendantByPrefix(Transform root, string prefix)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                    return child;

                Transform match = FindDescendantByPrefix(child, prefix);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + relativePath);
        }

        private static List<string> MissingAssets()
        {
            return RequiredAssets.Where(path => LoadSprite(path) == null).ToList();
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder("Assets/Project Data/Game/Images");
            EnsureAssetFolder(ArtFolder);
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void ImportSprites()
        {
            foreach (string relativePath in RequiredAssets)
            {
                string path = ArtFolder + "/" + relativePath;
                if (!File.Exists(path))
                    continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static bool TryImportAssetPackFromKnownLocations()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string[] candidates =
            {
                string.IsNullOrEmpty(projectRoot) ? null : Path.Combine(projectRoot, AssetPackFileName),
                string.IsNullOrEmpty(userHome) ? null : Path.Combine(userHome, "Downloads", AssetPackFileName),
                string.IsNullOrEmpty(userHome) ? null : Path.Combine(userHome, "Desktop", AssetPackFileName),
            };

            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                    return ExtractAssetPack(candidate);
            }

            return false;
        }

        private static bool ExtractAssetPack(string zipPath)
        {
            try
            {
                EnsureFolders();

                string fullTarget = Path.GetFullPath(ArtFolder) + Path.DirectorySeparatorChar;

                using (FileStream stream = File.OpenRead(zipPath))
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        string relative = entry.FullName.Replace('\\', '/');
                        if (relative.StartsWith("/", StringComparison.Ordinal) || relative.Contains("../"))
                            continue;

                        string destination = Path.GetFullPath(Path.Combine(ArtFolder, relative));
                        if (!destination.StartsWith(fullTarget, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string directory = Path.GetDirectoryName(destination);
                        if (!string.IsNullOrEmpty(directory))
                            Directory.CreateDirectory(directory);

                        using (Stream input = entry.Open())
                        using (FileStream output = File.Create(destination))
                            input.CopyTo(output);
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[GameUI] Imported generated art pack from: " + zipPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[GameUI] Could not import Game UI art pack: " + exception);
                return false;
            }
        }

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 StretchMin = Vector2.zero;
        private static readonly Vector2 StretchMax = Vector2.one;
    }
}
#endif
