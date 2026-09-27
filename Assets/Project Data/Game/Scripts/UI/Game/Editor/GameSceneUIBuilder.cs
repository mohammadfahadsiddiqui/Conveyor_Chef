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
    [InitializeOnLoad]
    public static class GameSceneUIBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/Game.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/GameUI";
        private const string AssetPackFileName = "ConveyorChef_GameUI_ArtPack.zip";

        private const string OldContainerName = "OLD GAME SCENE [OFF]";
        private const string OldPageName = "UI Game (Original - OFF)";
        private const string NewPageName = "NEW GAME SCENE [ACTIVE]";
        private const string PreviousOldPageName = "OLD UI Game [OFF]";
        private const string PreviousNewPageName = "NEW UI Game [ACTIVE]";
        private const string DesignRootName = "NEW GAME UI DESIGN";
        private const int LayoutVersion = 5;

        private const string LivesIndicatorPrefab =
            "Assets/Project Data/Watermelon Core/Extra Components/Lives System/Prefabs/Lives Indicator.prefab";
        private const string AddLivesPanelPrefab =
            "Assets/Project Data/Watermelon Core/Extra Components/Lives System/Prefabs/Add Lives Panel.prefab";
        private const string CurrencyPanelPrefab =
            "Assets/Project Data/Watermelon Core/Modules/Currencies Module/Prefabs/Currency Panel Simple.prefab";

        private static bool autoQueued;

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

        static GameSceneUIBuilder()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            QueueAutoBuild();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath)
                QueueAutoBuild();
        }

        private static void QueueAutoBuild()
        {
            if (autoQueued)
                return;

            autoQueued = true;
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            autoQueued = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            Canvas canvas = FindMainCanvas();
            if (canvas == null)
                return;

            if (canvas.transform.Find(NewPageName) != null &&
                canvas.transform.Find(OldContainerName) != null)
                return;

            EnsureFolders();
            AssetDatabase.Refresh();

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
                Debug.LogWarning(
                    "[GameUI] Original UI is restored. NEW UI was not auto-built because " +
                    missing.Count + " generated sprites are missing. Put " +
                    AssetPackFileName + " in Downloads/project root, then use " +
                    "Conveyor Chef > Game Scene > BUILD NEW WORKING GAME UI NOW.");
                return;
            }

            BuildInternal(scene, showDialog: false);
        }

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

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildInternal(scene, showDialog: true);
        }

        [MenuItem("Conveyor Chef/Game Scene/1. Restore Original UI Only", priority = 1)]
        public static void RestoreOriginalUIOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
                return;

            Transform newPage = canvas.transform.Find(NewPageName);
            if (newPage != null)
                UnityEngine.Object.DestroyImmediate(newPage.gameObject);

            Transform previousNew = canvas.transform.Find(PreviousNewPageName);
            if (previousNew != null)
                UnityEngine.Object.DestroyImmediate(previousNew.gameObject);

            GameObject original = null;

            Transform oldContainer = canvas.transform.Find(OldContainerName);
            if (oldContainer != null)
            {
                UIGame nestedOld = oldContainer.GetComponentInChildren<UIGame>(true);
                if (nestedOld != null)
                {
                    nestedOld.transform.SetParent(canvas.transform, false);
                    nestedOld.name = "UI Game";
                    nestedOld.gameObject.SetActive(true);
                    original = nestedOld.gameObject;
                }

                UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
            }

            // Recover an earlier v4 build where the old UIGame was left as a
            // direct inactive sibling.
            Transform previousOld = canvas.transform.Find(PreviousOldPageName);
            if (previousOld != null)
            {
                previousOld.name = "UI Game";
                previousOld.gameObject.SetActive(true);
                original = previousOld.gameObject;
            }

            if (original == null)
                original = FindDirectPage<UIGame>(canvas.transform, includeInactive: true);

            if (original != null)
            {
                original.name = "UI Game";
                original.SetActive(true);

                Transform oldExperiment = FindDescendant(original.transform, "NEW Game UI");
                if (oldExperiment != null)
                    UnityEngine.Object.DestroyImmediate(oldExperiment.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = original;
            Debug.Log("[GameUI] Original UI Game restored. NEW/OLD redesign hierarchy removed.");
        }

        [MenuItem("Conveyor Chef/Game Scene/BUILD NEW WORKING GAME UI NOW", priority = 2)]
        public static void BuildNewWorkingGameUI()
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
                    "The NEW working UI cannot be skinned yet because " + missing.Count +
                    " sprites are missing.\n\nImport " + AssetPackFileName +
                    " first with menu item 0.",
                    "OK");
                return;
            }

            BuildInternal(scene, showDialog: true);
        }

        [MenuItem("Conveyor Chef/Game Scene/3. Validate Working Game UI", priority = 3)]
        public static void ValidateWorkingGameUI()
        {
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[GameUI] UI Main Canvas not found.");
                return;
            }

            Transform oldContainer = canvas.transform.Find(OldContainerName);
            Transform newPage = canvas.transform.Find(NewPageName);

            UIGame oldGame =
                oldContainer != null
                    ? oldContainer.GetComponentInChildren<UIGame>(true)
                    : null;

            bool oldOk =
                oldContainer != null &&
                !oldContainer.gameObject.activeSelf &&
                oldGame != null &&
                oldGame.transform.parent == oldContainer;

            bool newOk =
                newPage != null &&
                newPage.gameObject.activeSelf &&
                newPage.parent == canvas.transform &&
                newPage.GetComponent<UIGame>() != null &&
                FindDescendant(newPage, DesignRootName) != null;

            int directGamePages = 0;
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                if (canvas.transform.GetChild(i).GetComponent<UIGame>() != null)
                    directGamePages++;
            }

            if (oldOk && newOk && directGamePages == 1)
            {
                Debug.Log(
                    "[GameUI] VALID: OLD GAME SCENE [OFF] contains the disabled original UIGame, " +
                    "and NEW GAME SCENE [ACTIVE] is the ONLY direct active UIGame page.");
            }
            else
            {
                Debug.LogError(
                    "[GameUI] INVALID hierarchy. Expected one direct UIGame page only. " +
                    "Run BUILD NEW WORKING GAME UI NOW again.");
            }
        }

        private static void BuildInternal(Scene scene, bool showDialog)
        {
            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[GameUI] UI Main Canvas was not found.");
                return;
            }

            // Normalize any previous v4/v5 build back to one direct original page.
            Transform existingNew = canvas.transform.Find(NewPageName);
            if (existingNew != null)
                UnityEngine.Object.DestroyImmediate(existingNew.gameObject);

            Transform previousNew = canvas.transform.Find(PreviousNewPageName);
            if (previousNew != null)
                UnityEngine.Object.DestroyImmediate(previousNew.gameObject);

            Transform oldContainer = canvas.transform.Find(OldContainerName);
            if (oldContainer != null)
            {
                UIGame nestedOld = oldContainer.GetComponentInChildren<UIGame>(true);
                if (nestedOld != null)
                {
                    nestedOld.transform.SetParent(canvas.transform, false);
                    nestedOld.name = "UI Game";
                    nestedOld.gameObject.SetActive(true);
                }

                UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
            }

            Transform previousOld = canvas.transform.Find(PreviousOldPageName);
            if (previousOld != null)
            {
                previousOld.name = "UI Game";
                previousOld.gameObject.SetActive(true);
            }

            GameObject original = FindDirectPage<UIGame>(canvas.transform, includeInactive: true);
            if (original == null)
            {
                Debug.LogError("[GameUI] Could not find the original UI Game page.");
                return;
            }

            original.name = "UI Game";
            original.SetActive(true);

            Transform experimental = FindDescendant(original.transform, "NEW Game UI");
            if (experimental != null)
                UnityEngine.Object.DestroyImmediate(experimental.gameObject);

            // Duplicate the COMPLETE working UIGame before touching the original.
            // Unity remaps all child/component references inside this clone.
            GameObject newPage =
                UnityEngine.Object.Instantiate(original, canvas.transform, false);

            newPage.name = NewPageName;
            newPage.SetActive(true);
            newPage.transform.SetAsLastSibling();

            // The old UIGame must NOT remain a direct child of UI Main Canvas.
            // UIController scans direct children and keys them by UIPage type.
            // Nesting the original avoids duplicate UIGame registration while
            // keeping the entire old page visible in Hierarchy as a disabled backup.
            RectTransform backupContainer = CreateRect(OldContainerName, canvas.transform);
            Stretch(backupContainer);
            backupContainer.SetAsFirstSibling();

            original.transform.SetParent(backupContainer, false);
            original.name = OldPageName;
            original.SetActive(true);
            backupContainer.gameObject.SetActive(false);

            // New clone keeps every original UIGame serialized reference.
            UIGame newGame = newPage.GetComponent<UIGame>();
            if (newGame == null)
            {
                UnityEngine.Object.DestroyImmediate(newPage);
                original.name = "UI Game";
                original.SetActive(true);
                Debug.LogError("[GameUI] Clone did not contain UIGame. Rolled back.");
                return;
            }

            Canvas newCanvas = newPage.GetComponent<Canvas>();
            if (newCanvas != null)
            {
                newCanvas.overrideSorting = true;
                newCanvas.sortingOrder = 100;
                newCanvas.enabled = true;
            }

            GameUIResponsiveLayout marker = newPage.GetComponent<GameUIResponsiveLayout>();
            if (marker == null)
                marker = newPage.AddComponent<GameUIResponsiveLayout>();
            marker.EditorConfigure(LayoutVersion);

            GameSceneHUDController bridge = newPage.GetComponent<GameSceneHUDController>();
            if (bridge == null)
                bridge = newPage.AddComponent<GameSceneHUDController>();

            Transform safeZone = FindDescendant(newPage.transform, "Safe Zone");
            if (safeZone == null)
            {
                UnityEngine.Object.DestroyImmediate(newPage);
                original.name = "UI Game";
                original.SetActive(true);
                Debug.LogError("[GameUI] Safe Zone was missing from cloned UIGame. Rolled back.");
                return;
            }

            // Existing functional elements from the cloned original page.
            Transform levelTextTransform = FindDescendant(newPage.transform, "Level Text");
            Transform replayButtonTransform = FindDescendant(newPage.transform, "Replay Button");
            Transform powerUpPanel = FindDescendant(newPage.transform, "Power Up Panel");
            Transform orderPanel = FindDescendant(newPage.transform, "OrderPanel");
            Transform quitPopup = FindDescendantByPrefix(newPage.transform, "Quit Pop Up");
            Transform tutorialCanvas = FindDescendant(newPage.transform, "Tutorial Canvas");

            BuildDesign(
                safeZone as RectTransform,
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

            RewireTutorialController(tutorialCanvas);

            UILevelQuitPopUp quitLogic =
                quitPopup != null ? quitPopup.GetComponent<UILevelQuitPopUp>() : null;

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

            ApplyOrderItemPrefabArtwork();
            ApplyResultPageArtwork();

            EditorUtility.SetDirty(original);
            EditorUtility.SetDirty(newPage);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = newPage;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log(
                "[GameUI] SUCCESS. OLD GAME SCENE [OFF] now contains the preserved original UIGame. " +
                "NEW GAME SCENE [ACTIVE] is the only direct working UIGame page.");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "NEW Working Game UI Created",
                    "Hierarchy now contains:\n\n" +
                    "OLD GAME SCENE [OFF]\n" +
                    "  └─ UI Game (Original - OFF)\n" +
                    "NEW GAME SCENE [ACTIVE]\n\n" +
                    "The original page is preserved under an inactive backup container. " +
                    "The NEW page is the only direct functional UIGame page.",
                    "OK");
            }
        }

        private static void BuildDesign(
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
            Transform previous = FindDescendant(safeZone, DesignRootName);
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous.gameObject);

            RectTransform design = CreateRect(DesignRootName, safeZone);
            Stretch(design);
            design.SetAsFirstSibling();

            // Strong backplate makes NEW UI clearly visible even while other legacy
            // editor pages are still visible behind it.
            RectTransform topBack = CreateSolidImage(
                "Top HUD Backplate",
                design,
                new Color32(39, 51, 70, 255),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                new Vector2(0f, 235f));
            topBack.pivot = new Vector2(0.5f, 1f);
            topBack.anchoredPosition = Vector2.zero;

            // LEVEL panel.
            RectTransform levelPanel = CreateImage(
                "Level Panel",
                design,
                LoadSprite("TopHUD/level_title_panel.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-45f, -88f),
                new Vector2(315f, 108f),
                false);

            CreateImage(
                "Chef Hat",
                levelPanel,
                LoadSprite("TopHUD/chef_hat_icon.png"),
                Center,
                Center,
                new Vector2(0f, 50f),
                new Vector2(68f, 68f),
                true);

            if (levelTextTransform != null)
            {
                RectTransform rect = levelTextTransform as RectTransform;
                levelTextTransform.SetParent(safeZone, false);
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = Center;
                rect.anchoredPosition = new Vector2(-45f, -92f);
                rect.sizeDelta = new Vector2(245f, 62f);
                rect.localScale = Vector3.one;
                levelTextTransform.SetAsLastSibling();

                TextMeshProUGUI level = levelTextTransform.GetComponent<TextMeshProUGUI>();
                if (level != null)
                {
                    level.fontSize = 40f;
                    level.fontStyle = FontStyles.Bold;
                    level.alignment = TextAlignmentOptions.Center;
                    level.color = Color.white;
                    level.enableAutoSizing = true;
                    level.fontSizeMin = 22f;
                    level.fontSizeMax = 40f;
                    level.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            // Existing Replay Button becomes the pause button while keeping its old
            // onClick logic to UILevelQuitPopUp.
            if (replayButtonTransform != null)
            {
                replayButtonTransform.name = "Pause Button";
                RectTransform rect = replayButtonTransform as RectTransform;
                replayButtonTransform.SetParent(safeZone, false);
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = Center;
                rect.anchoredPosition = new Vector2(-74f, -70f);
                rect.sizeDelta = new Vector2(88f, 88f);
                rect.localScale = Vector3.one;
                SetImageSprite(replayButtonTransform, LoadSprite("TopHUD/pause_button.png"), true);
                HideTMPChildren(replayButtonTransform);
                replayButtonTransform.SetAsLastSibling();
            }

            settingsButton = CreateButton(
                "Settings Button",
                safeZone,
                LoadSprite("TopHUD/settings_button.png"),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-74f, -165f),
                new Vector2(82f, 82f));

            // New visual HUD pieces use the game's existing manager components.
            BuildLivesHUD(safeZone);

            BuildCurrencyHUD(
                safeZone,
                CurrencyType.Coins,
                "Coin Counter",
                "TopHUD/coin_counter_panel.png",
                new Vector2(-304f, -42f),
                out coinPanel,
                out coinIcon);

            BuildCurrencyHUD(
                safeZone,
                CurrencyType.Diamonds,
                "Diamond Counter",
                "TopHUD/diamond_counter_panel.png",
                new Vector2(-304f, -127f),
                out diamondPanel,
                out diamondIcon);

            // Kitchen strip.
            CreateImage(
                "Kitchen Counter Strip",
                design,
                LoadSprite("Kitchen/kitchen_counter_strip.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -345f),
                new Vector2(1030f, 280f),
                false);

            CreateImage(
                "Kitchen Shelf",
                design,
                LoadSprite("Decor/stocked_kitchen_shelf.png"),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(205f, -340f),
                new Vector2(235f, 145f),
                true);

            CreateImage(
                "Hanging Lamp",
                design,
                LoadSprite("Decor/hanging_pendant_lamp.png"),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(70f, -300f),
                new Vector2(110f, 165f),
                true);

            // Conveyor.
            RectTransform conveyor = CreateRect("Conveyor UI", design);
            conveyor.anchorMin = new Vector2(0.5f, 1f);
            conveyor.anchorMax = new Vector2(0.5f, 1f);
            conveyor.pivot = new Vector2(0.5f, 1f);
            conveyor.anchoredPosition = new Vector2(-35f, -470f);
            conveyor.sizeDelta = new Vector2(870f, 215f);

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
                new Vector2(-25f, -3f),
                new Vector2(245f, 105f),
                true);

            // Serving slots.
            RectTransform serving = CreateImage(
                "Serving Slots Panel",
                design,
                LoadSprite("Board/serving_slots_panel.png"),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-78f, -700f),
                new Vector2(760f, 126f),
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

            // Orders background, then preserve real UIOrderPanel above it.
            RectTransform objective = CreateRect("Objective Panel Artwork", design);
            objective.anchorMin = new Vector2(1f, 1f);
            objective.anchorMax = new Vector2(1f, 1f);
            objective.pivot = new Vector2(1f, 1f);
            objective.anchoredPosition = new Vector2(-25f, -260f);
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

            TextMeshProUGUI title = CreateTMP(
                "Orders Title",
                objective,
                "ORDERS",
                34f,
                FontStyles.Bold,
                new Vector2(0f, 160f),
                new Vector2(235f, 54f));
            title.color = new Color32(83, 45, 22, 255);

            if (orderPanel != null)
            {
                RectTransform rect = orderPanel as RectTransform;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(-178f, -330f);
                rect.localScale = Vector3.one;

                Image bg = orderPanel.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = new Color(1f, 1f, 1f, 0f);
                    bg.raycastTarget = false;
                }

                orderPanel.SetAsLastSibling();
            }

            // Bottom toolbar + functional power-up panel.
            RectTransform toolbar = CreateImage(
                "Bottom Toolbar",
                design,
                LoadSprite("Toolbar/bottom_toolbar_panel.png"),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 92f),
                new Vector2(790f, 182f),
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

            if (powerUpPanel != null)
            {
                RectTransform rect = powerUpPanel as RectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(108f, 42f);
                rect.sizeDelta = new Vector2(545f, 165f);
                rect.localScale = Vector3.one;
                powerUpPanel.SetAsLastSibling();
            }

            // Subtle frame around active 3D board.
            RectTransform boardFrame = CreateImage(
                "Gameplay Board Frame",
                design,
                LoadSprite("Decor/industrial_board_frame.png"),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 475f),
                new Vector2(835f, 820f),
                false);

            Image boardFrameImage = boardFrame.GetComponent<Image>();
            boardFrameImage.color = new Color(1f, 1f, 1f, 0.14f);
            boardFrameImage.raycastTarget = false;
            boardFrame.SetAsFirstSibling();

            if (quitPopup != null)
            {
                SetImageSprite(
                    FindDescendant(quitPopup, "Panel Back"),
                    LoadSprite("Popups/pause_popup_panel.png"),
                    false);

                Transform quit = FindDescendant(quitPopup, "Quit Button");
                Transform close = FindDescendant(quitPopup, "Close Button");

                SetImageSprite(quit, LoadSprite("Popups/restart_button.png"), true);
                SetImageSprite(close, LoadSprite("Popups/continue_button.png"), true);
                HideTMPChildren(quit);
                HideTMPChildren(close);
            }

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
            rect.anchoredPosition = new Vector2(28f, -38f);
            rect.sizeDelta = new Vector2(292f, 88f);
            rect.localScale = Vector3.one;

            Image bg = indicator.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = LoadSprite("TopHUD/life_counter_panel.png");
                bg.color = Color.white;
                bg.type = Image.Type.Simple;
            }

            SetImageSprite(
                FindDescendant(indicator.transform, "Heart Image"),
                LoadSprite("TopHUD/heart_icon.png"),
                true);

            SetImageSprite(
                FindDescendant(indicator.transform, "Add Button"),
                LoadSprite("TopHUD/green_plus_button.png"),
                true);

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
            string name,
            string backgroundAsset,
            Vector2 anchoredPosition,
            out CurrencyUIPanelSimple logic,
            out Image iconImage)
        {
            logic = null;
            iconImage = null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyPanelPrefab);
            if (prefab == null)
                return;

            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, safeZone);
            panel.name = name;

            RectTransform rect = panel.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(250f, 74f);
            rect.localScale = Vector3.one;

            Image bg = panel.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = LoadSprite(backgroundAsset);
                bg.color = Color.white;
                bg.type = Image.Type.Simple;
            }

            logic = panel.GetComponent<CurrencyUIPanelSimple>();
            if (logic != null)
            {
                SerializedObject so = new SerializedObject(logic);
                SerializedProperty currencyType = so.FindProperty("currencyType");
                if (currencyType != null)
                    currencyType.enumValueIndex = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform plus = FindDescendant(panel.transform, "Add Button");
            SetImageSprite(
                plus,
                LoadSprite("TopHUD/green_plus_button.png"),
                true);

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

        private static void RewireTutorialController(Transform tutorialCanvas)
        {
            if (tutorialCanvas == null)
                return;

            TutorialCanvasController newCanvas =
                tutorialCanvas.GetComponent<TutorialCanvasController>();

            TutorialController controller =
                UnityEngine.Object.FindFirstObjectByType<TutorialController>(
                    FindObjectsInactive.Include);

            if (newCanvas == null || controller == null)
                return;

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty p = so.FindProperty("tutorialCanvasController");
            if (p != null)
            {
                p.objectReferenceValue = newCanvas;
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

                Transform continueButton =
                    FindDescendant(complete.transform, "No Thanks Button");

                SetImageSprite(
                    continueButton,
                    LoadSprite("Popups/continue_button.png"),
                    true);

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

                Transform replay =
                    FindDescendant(failed.transform, "Replay Button");

                SetImageSprite(
                    replay,
                    LoadSprite("Popups/restart_button.png"),
                    true);

                HideTMPChildren(replay);

                SetImageSprite(
                    FindDescendant(failed.transform, "home (2)"),
                    LoadSprite("Toolbar/home_button.png"),
                    true);
            }
        }

        private static void ApplyOrderItemPrefabArtwork()
        {
            const string prefabPath =
                "Assets/Project Data/Game/Prefabs/OrderItem.prefab";

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
            RectTransform library =
                CreateRect("ALL EXTRA GENERATED ASSETS [OFF]", design);

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

                RectTransform holder =
                    CreateRect(Path.GetFileNameWithoutExtension(assets[i]), library);

                holder.anchorMin = Center;
                holder.anchorMax = Center;
                holder.pivot = Center;
                holder.anchoredPosition =
                    new Vector2(startX + col * cellW, startY - row * cellH);

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

        private static Canvas FindMainCanvas()
        {
            GameObject root =
                FindObjectByExactNameInScene("UI Main Canvas");

            return root != null ? root.GetComponent<Canvas>() : null;
        }

        private static GameObject FindDirectPage<T>(
            Transform canvas,
            bool includeInactive) where T : Component
        {
            if (canvas == null)
                return null;

            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);

                if (!includeInactive && !child.gameObject.activeSelf)
                    continue;

                if (child.GetComponent<T>() != null)
                    return child.gameObject;
            }

            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go =
                new GameObject(name, typeof(RectTransform));

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
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

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
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

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
                name,
                parent,
                sprite,
                anchorMin,
                anchorMax,
                anchoredPosition,
                sizeDelta,
                true);

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

        private static void SetImageSprite(
            Transform target,
            Sprite sprite,
            bool preserveAspect)
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
                Transform match =
                    FindSelfOrDescendant(root.transform, name);

                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindSelfOrDescendant(
            Transform root,
            string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root)
            {
                Transform match =
                    FindSelfOrDescendant(child, name);

                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDescendant(
            Transform root,
            string name)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;

                Transform match =
                    FindDescendant(child, name);

                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDescendantByPrefix(
            Transform root,
            string prefix)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                    return child;

                Transform match =
                    FindDescendantByPrefix(child, prefix);

                if (match != null)
                    return match;
            }

            return null;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                ArtFolder + "/" + relativePath);
        }

        private static List<string> MissingAssets()
        {
            return RequiredAssets
                .Where(path => LoadSprite(path) == null)
                .ToList();
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

                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceUpdate);

                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;

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
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName;

            string userHome =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);

            string[] candidates =
            {
                string.IsNullOrEmpty(projectRoot)
                    ? null
                    : Path.Combine(projectRoot, AssetPackFileName),

                string.IsNullOrEmpty(userHome)
                    ? null
                    : Path.Combine(
                        userHome,
                        "Downloads",
                        AssetPackFileName),

                string.IsNullOrEmpty(userHome)
                    ? null
                    : Path.Combine(
                        userHome,
                        "Desktop",
                        AssetPackFileName),
            };

            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) &&
                    File.Exists(candidate))
                    return ExtractAssetPack(candidate);
            }

            return false;
        }

        private static bool ExtractAssetPack(string zipPath)
        {
            try
            {
                EnsureFolders();

                string fullTarget =
                    Path.GetFullPath(ArtFolder) +
                    Path.DirectorySeparatorChar;

                using (FileStream stream = File.OpenRead(zipPath))
                using (ZipArchive archive =
                    new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        string relative =
                            entry.FullName.Replace('\\', '/');

                        if (relative.StartsWith("/", StringComparison.Ordinal) ||
                            relative.Contains("../"))
                            continue;

                        string destination =
                            Path.GetFullPath(
                                Path.Combine(ArtFolder, relative));

                        if (!destination.StartsWith(
                                fullTarget,
                                StringComparison.OrdinalIgnoreCase))
                            continue;

                        string directory =
                            Path.GetDirectoryName(destination);

                        if (!string.IsNullOrEmpty(directory))
                            Directory.CreateDirectory(directory);

                        using (Stream input = entry.Open())
                        using (FileStream output =
                            File.Create(destination))
                        {
                            input.CopyTo(output);
                        }
                    }
                }

                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    "[GameUI] Imported generated Game UI art pack from: " +
                    zipPath);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[GameUI] Could not import Game UI art pack: " +
                    exception);

                return false;
            }
        }

        private static readonly Vector2 Center =
            new Vector2(0.5f, 0.5f);

        private static readonly Vector2 StretchMin =
            Vector2.zero;

        private static readonly Vector2 StretchMax =
            Vector2.one;
    }
}
#endif
