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
    /// Creates a SEPARATE gameplay UI page.
    ///
    /// The original UIGame object is never redesigned. It is moved unchanged
    /// under an inactive backup object. A full duplicate of the original page
    /// becomes the NEW active page, so every existing serialized gameplay
    /// reference is preserved before the visuals are changed.
    ///
    /// Result:
    /// UI Main Canvas
    ///   OLD GAME SCENE [OFF]              (inactive)
    ///      UI Game (Original - OFF)        (untouched original)
    ///   NEW GAME SCENE [ACTIVE]            (direct UIGame page)
    ///      Safe Zone
    ///      Tutorial Canvas
    ///      Power Up Purchase Panel
    ///      Quit Pop Up
    ///      ...all original working systems...
    ///
    /// UIController only scans direct children of UI Main Canvas, therefore only
    /// NEW GAME SCENE [ACTIVE] participates as UIGame at runtime.
    /// </summary>
    public static class GameSceneUIBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/Game.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/GameUI";
        private const string AssetPackFileName = "ConveyorChef_GameUI_ArtPack.zip";

        private const string NewPageName = "NEW GAME SCENE [ACTIVE]";
        private const string OldContainerName = "OLD GAME SCENE [OFF]";
        private const string OldPageName = "UI Game (Original - OFF)";
        private const string DesignRootName = "NEW GAME UI DESIGN";
        private const int LayoutVersion = 3;

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
                "All generated Game UI sprites are imported.\n\nNext run:\n" +
                "Conveyor Chef > Game Scene > 2. CREATE Separate NEW Game Scene (Old OFF)",
                "OK");
        }

        [MenuItem("Conveyor Chef/Game Scene/1. Restore Original Hierarchy", priority = 1)]
        public static void RestoreOriginalHierarchy()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[GameUI] UI Main Canvas was not found.");
                return;
            }

            Transform newPage = canvas.transform.Find(NewPageName);
            if (newPage != null)
                UnityEngine.Object.DestroyImmediate(newPage.gameObject);

            Transform oldContainer = canvas.transform.Find(OldContainerName);
            if (oldContainer != null)
            {
                UIGame oldGame = oldContainer.GetComponentInChildren<UIGame>(true);
                if (oldGame != null)
                {
                    oldGame.transform.SetParent(canvas.transform, false);
                    oldGame.name = "UI Game";
                    oldGame.gameObject.SetActive(true);
                }

                UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
            }

            // Cleanup only the experimental overlay created by the older builder.
            GameObject directGame = FindDirectPage<UIGame>(canvas.transform);
            if (directGame != null)
            {
                Transform oldExperiment = FindDescendant(directGame.transform, "NEW Game UI");
                if (oldExperiment != null)
                    UnityEngine.Object.DestroyImmediate(oldExperiment.gameObject);

                directGame.name = "UI Game";
                directGame.SetActive(true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (directGame != null)
                Selection.activeGameObject = directGame;

            Debug.Log(
                "[GameUI] Hierarchy restored. If an older bake changed Game.unity before this " +
                "builder was installed, use Git restore on Game.unity once to recover the exact baseline.");
        }

        [MenuItem("Conveyor Chef/Game Scene/2. CREATE Separate NEW Game Scene (Old OFF)", priority = 2)]
        public static void CreateSeparateNewGameScene()
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
                    "The separate NEW Game Scene was not created because " + missing.Count +
                    " generated sprites are missing.\n\nUse:\n" +
                    "Conveyor Chef > Game Scene > 0. Import Generated Game UI Art Pack",
                    "OK");
                return;
            }

            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[GameUI] UI Main Canvas was not found.");
                return;
            }

            // Remove only a previous v3 build if the command is run again.
            Transform existingNew = canvas.transform.Find(NewPageName);
            Transform existingOldContainer = canvas.transform.Find(OldContainerName);
            if (existingNew != null || existingOldContainer != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Rebuild Separate Game Scene",
                        "A separate NEW/OLD Game Scene hierarchy already exists.\n\n" +
                        "Restore the original hierarchy and rebuild it?",
                        "Rebuild",
                        "Cancel"))
                    return;

                RestoreExistingSeparateBuild(canvas);
            }

            GameObject oldPage = FindDirectPage<UIGame>(canvas.transform);
            if (oldPage == null)
            {
                Debug.LogError(
                    "[GameUI] Could not find the original direct UI Game page. " +
                    "The scene was not changed.");
                return;
            }

            // Older experimental builder placed an overlay INSIDE the old UI Game.
            // Remove that overlay before cloning so the new page starts from the
            // functional original hierarchy, not the experimental art layer.
            Transform experimental = FindDescendant(oldPage.transform, "NEW Game UI");
            if (experimental != null)
                UnityEngine.Object.DestroyImmediate(experimental.gameObject);

            // STEP 1: duplicate the complete functional page FIRST.
            // Unity remaps references between components and cloned children.
            GameObject newPage = UnityEngine.Object.Instantiate(oldPage, canvas.transform, false);
            newPage.name = NewPageName;
            newPage.SetActive(true);
            newPage.transform.SetAsLastSibling();

            // STEP 2: move the untouched original under a clearly visible OFF backup.
            RectTransform oldContainer = CreateRect(OldContainerName, canvas.transform);
            Stretch(oldContainer);
            oldContainer.SetAsFirstSibling();

            oldPage.transform.SetParent(oldContainer, false);
            oldPage.name = OldPageName;
            oldContainer.gameObject.SetActive(false);

            // The new page must be a direct child because UIController scans direct
            // children and builds a dictionary keyed by UIPage type.
            UIGame newGame = newPage.GetComponent<UIGame>();
            if (newGame == null)
            {
                Debug.LogError("[GameUI] Cloned page has no UIGame component. Rolling back.");
                UnityEngine.Object.DestroyImmediate(newPage);
                oldPage.transform.SetParent(canvas.transform, false);
                oldPage.name = "UI Game";
                oldPage.SetActive(true);
                UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
                return;
            }

            GameUIResponsiveLayout marker = newPage.GetComponent<GameUIResponsiveLayout>();
            if (marker == null)
                marker = newPage.AddComponent<GameUIResponsiveLayout>();
            marker.EditorConfigure(LayoutVersion);

            GameSceneHUDController bridge = newPage.GetComponent<GameSceneHUDController>();
            if (bridge == null)
                bridge = newPage.AddComponent<GameSceneHUDController>();

            Canvas pageCanvas = newPage.GetComponent<Canvas>();
            if (pageCanvas != null)
            {
                pageCanvas.overrideSorting = true;
                pageCanvas.sortingOrder = 50;
                pageCanvas.enabled = true;
            }

            GraphicRaycaster raycaster = newPage.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = true;

            Transform safeZone = FindDescendant(newPage.transform, "Safe Zone");
            Transform levelTextTransform = FindDescendant(newPage.transform, "Level Text");
            Transform replayButtonTransform = FindDescendant(newPage.transform, "Replay Button");
            Transform powerUpPanel = FindDescendant(newPage.transform, "Power Up Panel");
            Transform orderPanel = FindDescendant(newPage.transform, "OrderPanel");
            Transform quitPopup = FindDescendantByPrefix(newPage.transform, "Quit Pop Up");
            Transform tutorialCanvas = FindDescendant(newPage.transform, "Tutorial Canvas");

            if (safeZone == null || levelTextTransform == null || replayButtonTransform == null ||
                powerUpPanel == null || orderPanel == null || quitPopup == null)
            {
                Debug.LogError(
                    "[GameUI] The cloned original page is missing one or more required functional objects. " +
                    "Rolling back without touching the old page.");

                UnityEngine.Object.DestroyImmediate(newPage);
                oldPage.transform.SetParent(canvas.transform, false);
                oldPage.name = "UI Game";
                oldPage.SetActive(true);
                UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
                return;
            }

            RectTransform safeRect = safeZone as RectTransform;

            BuildNewDesign(
                safeRect,
                levelTextTransform,
                replayButtonTransform,
                powerUpPanel,
                orderPanel,
                quitPopup,
                out Button settingsButton,
                out Button homeButton,
                out CurrencyUIPanelSimple coinPanel,
                out CurrencyUIPanelSimple diamondPanel,
                out Image coinIcon,
                out Image diamondIcon);

            // Rewire TutorialController to the NEW clone. It is external to UIGame,
            // so Unity cannot automatically remap this reference during duplication.
            RewireTutorialController(tutorialCanvas);

            UILevelQuitPopUp quitLogic = quitPopup.GetComponent<UILevelQuitPopUp>();
            bridge.EditorConfigure(
                coinPanel,
                diamondPanel,
                coinIcon,
                diamondIcon,
                settingsButton,
                homeButton,
                quitLogic,
                LoadSprite("Toolbar/undo_button.png"),
                LoadSprite("Toolbar/hint_button.png"),
                LoadSprite("Toolbar/shuffle_button.png"),
                LoadSprite("Toolbar/powerup_count_badge.png"),
                LoadSprite("TopHUD/coin_icon.png"),
                LoadSprite("TopHUD/diamond_icon.png"));

            ApplyResultPageArtwork();
            ApplyOrderItemPrefabArtwork();

            // Keep the original visually and functionally untouched inside OFF backup.
            oldContainer.gameObject.SetActive(false);

            EditorUtility.SetDirty(newPage);
            EditorUtility.SetDirty(oldContainer.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = newPage;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            EditorUtility.DisplayDialog(
                "Separate Game Scene Created",
                "Done.\n\nHierarchy now contains:\n\n" +
                "OLD GAME SCENE [OFF]  (inactive original)\n" +
                "NEW GAME SCENE [ACTIVE]  (new working UI)\n\n" +
                "The original gameplay systems are connected through the duplicated UIGame page.",
                "OK");

            Debug.Log(
                "[GameUI] SUCCESS: OLD GAME SCENE [OFF] preserved and disabled; " +
                "NEW GAME SCENE [ACTIVE] created as the direct working UIGame page.");
        }

        [MenuItem("Conveyor Chef/Game Scene/3. Focus NEW Game Scene", priority = 3)]
        public static void FocusNewGameScene()
        {
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
                return;

            Transform newPage = canvas.transform.Find(NewPageName);
            if (newPage == null)
            {
                Debug.LogWarning("[GameUI] NEW GAME SCENE [ACTIVE] has not been created yet.");
                return;
            }

            Selection.activeGameObject = newPage.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        [MenuItem("Conveyor Chef/Game Scene/4. Validate NEW/OLD Hierarchy", priority = 4)]
        public static void ValidateHierarchy()
        {
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[GameUI] Validation failed: UI Main Canvas not found.");
                return;
            }

            Transform oldContainer = canvas.transform.Find(OldContainerName);
            Transform newPage = canvas.transform.Find(NewPageName);

            bool oldOk =
                oldContainer != null &&
                !oldContainer.gameObject.activeSelf &&
                oldContainer.GetComponentInChildren<UIGame>(true) != null;

            bool newOk =
                newPage != null &&
                newPage.gameObject.activeSelf &&
                newPage.GetComponent<UIGame>() != null &&
                newPage.parent == canvas.transform;

            if (oldOk && newOk)
            {
                Debug.Log(
                    "[GameUI] VALID: original UIGame is nested under OLD GAME SCENE [OFF], " +
                    "and NEW GAME SCENE [ACTIVE] is the direct active UIGame page.");
            }
            else
            {
                Debug.LogError(
                    "[GameUI] INVALID hierarchy. Run '2. CREATE Separate NEW Game Scene (Old OFF)' again.");
            }
        }

        private static void BuildNewDesign(
            RectTransform safeZone,
            Transform levelTextTransform,
            Transform replayButtonTransform,
            Transform powerUpPanel,
            Transform orderPanel,
            Transform quitPopup,
            out Button settingsButton,
            out Button homeButton,
            out CurrencyUIPanelSimple coinPanel,
            out CurrencyUIPanelSimple diamondPanel,
            out Image coinIcon,
            out Image diamondIcon)
        {
            Transform previousDesign = FindDescendant(safeZone, DesignRootName);
            if (previousDesign != null)
                UnityEngine.Object.DestroyImmediate(previousDesign.gameObject);

            RectTransform design = CreateRect(DesignRootName, safeZone);
            Stretch(design);
            design.SetAsFirstSibling();

            // Opaque top strip deliberately covers the old UIMainMenu HUD that may
            // still exist behind UIGame in the legacy flow.
            RectTransform topBack = CreateSolidImage(
                "Top HUD Backplate",
                design,
                new Color32(38, 49, 69, 255),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                new Vector2(0f, 225f));
            topBack.pivot = new Vector2(0.5f, 1f);
            topBack.anchoredPosition = Vector2.zero;

            RectTransform levelPanel = CreateImage(
                "Level Panel",
                design,
                LoadSprite("TopHUD/level_title_panel.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-42f, -86f),
                new Vector2(315f, 105f),
                false);

            CreateImage(
                "Chef Hat",
                levelPanel,
                LoadSprite("TopHUD/chef_hat_icon.png"),
                Center,
                Center,
                new Vector2(0f, 49f),
                new Vector2(68f, 68f),
                true);

            RectTransform levelRect = levelTextTransform as RectTransform;
            levelTextTransform.SetParent(safeZone, false);
            levelRect.anchorMin = new Vector2(0.5f, 1f);
            levelRect.anchorMax = new Vector2(0.5f, 1f);
            levelRect.pivot = Center;
            levelRect.anchoredPosition = new Vector2(-42f, -91f);
            levelRect.sizeDelta = new Vector2(245f, 62f);
            levelRect.localScale = Vector3.one;
            levelTextTransform.SetAsLastSibling();

            TextMeshProUGUI levelText = levelTextTransform.GetComponent<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.fontSize = 40f;
                levelText.fontStyle = FontStyles.Bold;
                levelText.alignment = TextAlignmentOptions.Center;
                levelText.color = Color.white;
                levelText.enableAutoSizing = true;
                levelText.fontSizeMin = 24f;
                levelText.fontSizeMax = 40f;
                levelText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // Existing replay button remains the exact old functional button.
            replayButtonTransform.name = "Pause Button";
            RectTransform replayRect = replayButtonTransform as RectTransform;
            replayRect.SetParent(safeZone, false);
            replayRect.anchorMin = new Vector2(1f, 1f);
            replayRect.anchorMax = new Vector2(1f, 1f);
            replayRect.pivot = Center;
            replayRect.anchoredPosition = new Vector2(-78f, -70f);
            replayRect.sizeDelta = new Vector2(88f, 88f);
            replayRect.localScale = Vector3.one;
            replayButtonTransform.SetAsLastSibling();
            SetImageSprite(replayButtonTransform, LoadSprite("TopHUD/pause_button.png"), true);
            HideTMPChildren(replayButtonTransform);

            settingsButton = CreateButton(
                "Settings Button",
                safeZone,
                LoadSprite("TopHUD/settings_button.png"),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-78f, -166f),
                new Vector2(82f, 82f));
            settingsButton.transform.SetAsLastSibling();

            // Original logic, new visual instances.
            BuildLivesHUD(safeZone);

            BuildCurrencyHUD(
                safeZone,
                CurrencyType.Coins,
                "Coin Counter",
                "TopHUD/coin_counter_panel.png",
                new Vector2(-303f, -42f),
                out coinPanel,
                out coinIcon);

            BuildCurrencyHUD(
                safeZone,
                CurrencyType.Diamonds,
                "Diamond Counter",
                "TopHUD/diamond_counter_panel.png",
                new Vector2(-303f, -126f),
                out diamondPanel,
                out diamondIcon);

            // Kitchen/conveyor visual treatment.
            RectTransform kitchenStrip = CreateImage(
                "Kitchen Counter Strip",
                design,
                LoadSprite("Kitchen/kitchen_counter_strip.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -340f),
                new Vector2(1030f, 280f),
                false);
            kitchenStrip.GetComponent<Image>().raycastTarget = false;

            // Small decorations from generated set.
            CreateImage(
                "Hanging Lamp Left",
                design,
                LoadSprite("Decor/hanging_pendant_lamp.png"),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(82f, -305f),
                new Vector2(120f, 175f),
                true);

            CreateImage(
                "Kitchen Shelf",
                design,
                LoadSprite("Decor/stocked_kitchen_shelf.png"),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(235f, -345f),
                new Vector2(245f, 150f),
                true);

            RectTransform conveyor = CreateRect("Conveyor UI", design);
            conveyor.anchorMin = new Vector2(0.5f, 1f);
            conveyor.anchorMax = new Vector2(0.5f, 1f);
            conveyor.pivot = new Vector2(0.5f, 1f);
            conveyor.anchoredPosition = new Vector2(-35f, -465f);
            conveyor.sizeDelta = new Vector2(875f, 215f);

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
                new Vector2(-30f, -3f),
                new Vector2(245f, 105f),
                true);

            RectTransform serving = CreateImage(
                "Serving Slots Panel",
                design,
                LoadSprite("Board/serving_slots_panel.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-82f, -697f),
                new Vector2(760f, 125f),
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

            // Orders artwork behind the cloned functional UIOrderPanel.
            RectTransform objective = CreateRect("Objective Panel Artwork", design);
            objective.anchorMin = new Vector2(1f, 1f);
            objective.anchorMax = new Vector2(1f, 1f);
            objective.pivot = new Vector2(1f, 1f);
            objective.anchoredPosition = new Vector2(-30f, -265f);
            objective.sizeDelta = new Vector2(300f, 420f);

            CreateImage(
                "Objective Highlight",
                objective,
                LoadSprite("States/objective_highlight.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                Vector2.zero,
                false);

            CreateImage(
                "Orders Panel",
                objective,
                LoadSprite("Orders/orders_panel.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                new Vector2(-12f, -12f),
                false);

            TextMeshProUGUI ordersTitle = CreateTMP(
                "Orders Title",
                objective,
                "ORDERS",
                34f,
                FontStyles.Bold,
                new Vector2(0f, 160f),
                new Vector2(235f, 54f));
            ordersTitle.color = new Color32(83, 45, 22, 255);

            ConfigureFunctionalOrderPanel(orderPanel);
            orderPanel.SetAsLastSibling();

            // Preserve the real power-up controller and its three behaviours.
            ConfigureFunctionalPowerUps(powerUpPanel);
            powerUpPanel.SetAsLastSibling();

            RectTransform toolbar = CreateImage(
                "Bottom Toolbar",
                design,
                LoadSprite("Toolbar/bottom_toolbar_panel.png"),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 90f),
                new Vector2(790f, 180f),
                false);

            CreateImage(
                "Toolbar Glow",
                toolbar,
                LoadSprite("States/panel_glow.png"),
                StretchMin,
                StretchMax,
                Vector2.zero,
                new Vector2(42f, 34f),
                false);

            homeButton = CreateButton(
                "Home Button",
                toolbar,
                LoadSprite("Toolbar/home_button.png"),
                Center,
                Center,
                new Vector2(-292f, 0f),
                new Vector2(118f, 118f));

            // Gameplay frame is intentionally subtle. The 3D game remains visible.
            RectTransform frame = CreateImage(
                "Gameplay Board Frame",
                design,
                LoadSprite("Decor/industrial_board_frame.png"),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 475f),
                new Vector2(835f, 820f),
                false);
            Image frameImage = frame.GetComponent<Image>();
            frameImage.color = new Color(1f, 1f, 1f, 0.14f);
            frameImage.raycastTarget = false;
            frame.SetAsFirstSibling();

            ConfigureQuitPopup(quitPopup);

            // Every generated asset remains in the editable hierarchy. Assets that
            // would cover the 3D board are kept in an inactive designer library.
            BuildExtraAssetLibrary(design);
        }

        private static void BuildLivesHUD(RectTransform safeZone)
        {
            GameObject indicatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LivesIndicatorPrefab);
            GameObject addLivesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AddLivesPanelPrefab);
            if (indicatorPrefab == null || addLivesPrefab == null)
                return;

            GameObject addPanel = (GameObject)PrefabUtility.InstantiatePrefab(addLivesPrefab, safeZone);
            addPanel.name = "NEW Add Lives Panel";
            RectTransform addRect = addPanel.transform as RectTransform;
            if (addRect != null)
                Stretch(addRect);

            GameObject indicator = (GameObject)PrefabUtility.InstantiatePrefab(indicatorPrefab, safeZone);
            indicator.name = "Life Counter";

            RectTransform rect = indicator.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(30f, -38f);
            rect.sizeDelta = new Vector2(292f, 88f);
            rect.localScale = Vector3.one;

            Image bg = indicator.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = LoadSprite("TopHUD/life_counter_panel.png");
                bg.color = Color.white;
                bg.type = Image.Type.Simple;
            }

            SetImageSprite(FindDescendant(indicator.transform, "Heart Image"), LoadSprite("TopHUD/heart_icon.png"), true);
            SetImageSprite(FindDescendant(indicator.transform, "Add Button"), LoadSprite("TopHUD/green_plus_button.png"), true);

            LivesIndicator logic = indicator.GetComponent<LivesIndicator>();
            AddLivesPanel panelLogic = addPanel.GetComponent<AddLivesPanel>();
            if (logic != null && panelLogic != null)
            {
                SerializedObject so = new SerializedObject(logic);
                SerializedProperty p = so.FindProperty("addLivesPanel");
                if (p != null)
                {
                    p.objectReferenceValue = panelLogic;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            addPanel.SetActive(false);
            indicator.transform.SetAsLastSibling();
        }

        private static void BuildCurrencyHUD(
            RectTransform safeZone,
            CurrencyType type,
            string objectName,
            string backgroundAsset,
            Vector2 position,
            out CurrencyUIPanelSimple panelLogic,
            out Image iconImage)
        {
            panelLogic = null;
            iconImage = null;

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

            Image bg = panel.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = LoadSprite(backgroundAsset);
                bg.color = Color.white;
                bg.type = Image.Type.Simple;
            }

            panelLogic = panel.GetComponent<CurrencyUIPanelSimple>();
            if (panelLogic != null)
            {
                SerializedObject so = new SerializedObject(panelLogic);
                SerializedProperty currencyType = so.FindProperty("currencyType");
                if (currencyType != null)
                    currencyType.enumValueIndex = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform plus = FindDescendant(panel.transform, "Add Button");
            SetImageSprite(plus, LoadSprite("TopHUD/green_plus_button.png"), true);

            Transform icon = FindDescendant(panel.transform, "Currency Icon");
            if (icon != null)
            {
                iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = LoadSprite(
                        type == CurrencyType.Coins
                            ? "TopHUD/coin_icon.png"
                            : "TopHUD/diamond_icon.png");
                    iconImage.preserveAspect = true;
                }
            }

            panel.transform.SetAsLastSibling();
        }

        private static void ConfigureFunctionalOrderPanel(Transform orderPanel)
        {
            RectTransform rect = orderPanel as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(-180f, -330f);
                rect.localScale = Vector3.one;
            }

            Image image = orderPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = false;
            }

            VerticalLayoutGroup layout = orderPanel.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 8f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childAlignment = TextAnchor.UpperCenter;
            }
        }

        private static void ConfigureFunctionalPowerUps(Transform powerUpPanel)
        {
            RectTransform rect = powerUpPanel as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(105f, 42f);
                rect.sizeDelta = new Vector2(545f, 165f);
                rect.localScale = Vector3.one;
            }

            Transform container = FindDescendant(powerUpPanel, "Container");
            if (container != null)
            {
                HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 18f;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }
            }
        }

        private static void ConfigureQuitPopup(Transform popup)
        {
            SetImageSprite(FindDescendant(popup, "Panel Back"), LoadSprite("Popups/pause_popup_panel.png"), false);

            Transform quitButton = FindDescendant(popup, "Quit Button");
            Transform closeButton = FindDescendant(popup, "Close Button");

            SetImageSprite(quitButton, LoadSprite("Popups/restart_button.png"), true);
            SetImageSprite(closeButton, LoadSprite("Popups/continue_button.png"), true);
            HideTMPChildren(quitButton);
            HideTMPChildren(closeButton);
        }

        private static void RewireTutorialController(Transform tutorialCanvas)
        {
            if (tutorialCanvas == null)
                return;

            TutorialCanvasController newCanvas = tutorialCanvas.GetComponent<TutorialCanvasController>();
            TutorialController controller =
                UnityEngine.Object.FindFirstObjectByType<TutorialController>(FindObjectsInactive.Include);

            if (newCanvas == null || controller == null)
                return;

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty property = so.FindProperty("tutorialCanvasController");
            if (property != null)
            {
                property.objectReferenceValue = newCanvas;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }
        }

        private static void ApplyResultPageArtwork()
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

            GameObject failed = FindObjectByExactNameInScene("UI Game Over");
            if (failed != null)
            {
                SetImageSprite(
                    FindDescendant(failed.transform, "Background Image"),
                    LoadSprite("Popups/level_failed_popup.png"),
                    true);

                Transform replay = FindDescendant(failed.transform, "Replay Button");
                SetImageSprite(replay, LoadSprite("Popups/restart_button.png"), true);
                HideTMPChildren(replay);

                SetImageSprite(
                    FindDescendant(failed.transform, "home (2)"),
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

        private static void BuildExtraAssetLibrary(RectTransform design)
        {
            RectTransform library = CreateRect("ALL EXTRA GENERATED ASSETS [OFF]", design);
            Stretch(library);
            library.gameObject.SetActive(false);

            string[] assets =
            {
                "Kitchen/kitchen_gameplay_background.png",
                "Board/game_board_background.png",
                "Board/game_tile_slot.png",
                "Orders/order_completed_overlay.png",
                "Toolbar/menu_button.png",
                "States/button_pressed_overlay.png",
                "States/button_disabled_overlay.png",
                "States/notification_badge.png",
                "Decor/kitchen_chalkboard_menu.png",
                "Decor/dome_kitchen_oven.png",
                "Decor/condiment_tray.png",
                "Decor/metal_stovetop_station.png",
                "Decor/potted_kitchen_plant.png",
                "Decor/wooden_cutting_board.png",
                "Decor/chef_apron.png",
            };

            int columns = 4;
            float cellW = 245f;
            float cellH = 205f;
            float startX = -367.5f;
            float startY = 760f;

            for (int i = 0; i < assets.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;

                RectTransform holder = CreateRect(Path.GetFileNameWithoutExtension(assets[i]), library);
                holder.anchorMin = Center;
                holder.anchorMax = Center;
                holder.pivot = Center;
                holder.anchoredPosition = new Vector2(startX + col * cellW, startY - row * cellH);
                holder.sizeDelta = new Vector2(210f, 170f);

                CreateImage(
                    "Artwork",
                    holder,
                    LoadSprite(assets[i]),
                    Center,
                    Center,
                    new Vector2(0f, 5f),
                    new Vector2(180f, 145f),
                    true);
            }
        }

        private static void RestoreExistingSeparateBuild(Canvas canvas)
        {
            Transform newPage = canvas.transform.Find(NewPageName);
            if (newPage != null)
                UnityEngine.Object.DestroyImmediate(newPage.gameObject);

            Transform oldContainer = canvas.transform.Find(OldContainerName);
            if (oldContainer == null)
                return;

            UIGame oldGame = oldContainer.GetComponentInChildren<UIGame>(true);
            if (oldGame != null)
            {
                oldGame.transform.SetParent(canvas.transform, false);
                oldGame.name = "UI Game";
                oldGame.gameObject.SetActive(true);
            }

            UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
        }

        private static Canvas FindMainCanvas()
        {
            GameObject root = FindObjectByExactNameInScene("UI Main Canvas");
            return root != null ? root.GetComponent<Canvas>() : null;
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

        private static RectTransform CreateSolidImage(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
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
            image.sprite = null;
            image.color = color;
            image.raycastTarget = false;
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
                text.gameObject.SetActive(false);
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
                Debug.Log("[GameUI] Imported generated Game UI art pack from: " + zipPath);
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
