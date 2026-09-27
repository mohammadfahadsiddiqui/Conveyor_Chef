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
    /// One-time editor baker for the Conveyor Chef Game scene UI.
    ///
    /// Architecture intentionally mirrors the approved Loading / WorldMap /
    /// CountryMap / LevelSelection workflow:
    /// - imported art is stored as individual Sprite assets;
    /// - the visible gameplay HUD is serialized into Game.unity;
    /// - the authored hierarchy remains directly editable in Canvas/Inspector;
    /// - runtime scripts update values/interaction only; they never rebuild layout;
    /// - gameplay world objects and ScriptHolder are never touched.
    /// </summary>
    [InitializeOnLoad]
    public static class GameSceneUIBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/Game.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/GameUI";
        private const string AssetPackFileName = "ConveyorChef_GameUI_ArtPack.zip";
        private const string NewRootName = "NEW Game UI";
        private const int LayoutVersion = 1;

        private static bool bakeQueued;

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
            EditorApplication.delayCall += TryAutoBakeOpenScene;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath)
                QueueAutoBake();
        }

        private static void QueueAutoBake()
        {
            if (bakeQueued)
                return;

            bakeQueued = true;
            EditorApplication.delayCall += TryAutoBakeOpenScene;
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

            BakeEditableGameUI();
        }

        [MenuItem("Conveyor Chef/Game Scene/1. Bake Editable Game UI", priority = 1)]
        public static void BakeEditableGameUI()
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
                    "Missing " + missing.Count + " generated sprites.\n\n" +
                    "Download " + AssetPackFileName + " and keep it in your Downloads folder " +
                    "or the project root, then run this command again.\n\nMissing:\n- " +
                    string.Join("\n- ", missing),
                    "OK");
                return;
            }

            GameObject existing = FindObjectByExactNameInScene(NewRootName);
            if (existing != null)
            {
                GameUIResponsiveLayout marker = existing.GetComponent<GameUIResponsiveLayout>();
                if (marker != null && marker.LayoutVersion >= LayoutVersion)
                {
                    FocusEditableRoot(existing);
                    Debug.Log("[GameUI] NEW Game UI already exists. Preserving the authored Canvas layout.");
                    return;
                }

                if (!EditorUtility.DisplayDialog(
                        "Replace Game UI",
                        "An older NEW Game UI hierarchy already exists. Replace only that authored UI root?\n\n" +
                        "Gameplay objects, ScriptHolder and legacy functional prefabs will not be deleted.",
                        "Replace UI Root",
                        "Cancel"))
                    return;

                UnityEngine.Object.DestroyImmediate(existing);
            }

            Build(scene);
        }

        [MenuItem("Conveyor Chef/Game Scene/2. Rebind Game UI Artwork Only", priority = 2)]
        public static void RebindArtworkOnly()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ImportSprites();
            ApplyExistingPopupArtwork();
            ApplyOrderItemPrefabArtwork();
            ApplyLegacyVisualCleanup();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameUI] Rebound Game UI artwork without rebuilding NEW Game UI geometry.");
        }

        [MenuItem("Conveyor Chef/Game Scene/3. Focus Editable Game UI", priority = 3)]
        public static void FocusGameUI()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = FindObjectByExactNameInScene(NewRootName);
            if (root != null)
                FocusEditableRoot(root);
        }

        private static void TryAutoBakeOpenScene()
        {
            bakeQueued = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            GameObject existing = FindObjectByExactNameInScene(NewRootName);
            if (existing != null)
            {
                FocusEditableRoot(existing, frameSceneView: false);
                return;
            }

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

            if (missing.Count == 0)
            {
                Debug.Log("[GameUI] Generated art pack found. Installing the editable NEW Game UI into Game.unity.");
                Build(scene);
            }
            else
            {
                Debug.LogWarning(
                    "[GameUI] Game UI art pack is not imported yet. Missing " + missing.Count +
                    " sprites. Put " + AssetPackFileName + " in Downloads/project root or use " +
                    "Conveyor Chef > Game Scene > 0. Import Generated Game UI Art Pack.");
            }
        }

        private static void Build(Scene scene)
        {
            GameObject uiGame = FindObjectByExactNameInScene("UI Game");
            if (uiGame == null)
            {
                Debug.LogError("[GameUI] Could not find the existing UI Game page in Game.unity. No changes were made.");
                return;
            }

            Transform safeZone = FindDescendant(uiGame.transform, "Safe Zone");
            if (safeZone == null)
            {
                Debug.LogError("[GameUI] Could not find UI Game/Safe Zone. No changes were made.");
                return;
            }

            Canvas canvas = uiGame.GetComponentInParent<Canvas>();
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
            }

            RectTransform root = CreateRect(NewRootName, safeZone);
            Stretch(root);
            root.SetAsFirstSibling();

            GameUIResponsiveLayout marker = root.gameObject.AddComponent<GameUIResponsiveLayout>();
            marker.EditorConfigure(LayoutVersion);

            BuildTopHUD(root, out HUDRefs hud);
            BuildObjectivePanel(root);
            BuildConveyorAndServingUI(root);
            BuildBottomToolbar(root, out Button homeButton);
            BuildOptionalArtLibrary(root);

            GameSceneHUDController controller = root.gameObject.AddComponent<GameSceneHUDController>();
            controller.EditorConfigure(
                hud.levelText,
                hud.livesText,
                hud.coinsText,
                hud.diamondsText,
                hud.lifePlus,
                hud.coinPlus,
                hud.diamondPlus,
                hud.pause,
                hud.settings,
                homeButton,
                LoadSprite("Toolbar/undo_button.png"),
                LoadSprite("Toolbar/hint_button.png"),
                LoadSprite("Toolbar/shuffle_button.png"),
                LoadSprite("Toolbar/powerup_count_badge.png"));

            ConfigureExistingOrderPanel();
            ConfigureExistingPowerUpPanel();
            ApplyExistingPopupArtwork();
            ApplyOrderItemPrefabArtwork();
            ApplyLegacyVisualCleanup();

            EditorUtility.SetDirty(root.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            FocusEditableRoot(root.gameObject);

            Debug.Log(
                "[GameUI] Installed NEW Game UI as an editable serialized Canvas hierarchy. " +
                "Gameplay world objects and ScriptHolder were preserved. Dynamic values remain TMP-based.");
        }

        private struct HUDRefs
        {
            public TMP_Text levelText;
            public TMP_Text livesText;
            public TMP_Text coinsText;
            public TMP_Text diamondsText;
            public Button lifePlus;
            public Button coinPlus;
            public Button diamondPlus;
            public Button pause;
            public Button settings;
        }

        private static void BuildTopHUD(RectTransform root, out HUDRefs refs)
        {
            RectTransform top = CreateRect("Top HUD", root);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0f, 245f);

            RectTransform life = CreateImage(
                "Life Counter", top, LoadSprite("TopHUD/life_counter_panel.png"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -82f), new Vector2(280f, 92f), false);

            CreateImage("Heart Icon", life, LoadSprite("TopHUD/heart_icon.png"),
                Center, Center, new Vector2(-92f, 0f), new Vector2(70f, 70f), true);

            refs.livesText = CreateTMP("Life Value", life, "FULL", 34f, FontStyles.Bold,
                new Vector2(-5f, 0f), new Vector2(115f, 58f));

            refs.lifePlus = CreateButton("Life Plus Button", life, LoadSprite("TopHUD/green_plus_button.png"),
                Center, Center, new Vector2(100f, 0f), new Vector2(58f, 58f));

            RectTransform level = CreateImage(
                "Level Counter", top, LoadSprite("TopHUD/level_title_panel.png"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, -90f), new Vector2(320f, 108f), false);

            CreateImage("Chef Hat", level, LoadSprite("TopHUD/chef_hat_icon.png"),
                Center, Center, new Vector2(0f, 52f), new Vector2(72f, 72f), true);

            refs.levelText = CreateTMP("Level Value", level, "LEVEL 1", 39f, FontStyles.Bold,
                new Vector2(0f, -4f), new Vector2(250f, 65f));
            refs.levelText.color = Color.white;
            refs.levelText.outlineWidth = 0.18f;
            refs.levelText.outlineColor = new Color32(20, 68, 130, 255);

            RectTransform coin = CreateImage(
                "Coin Counter", top, LoadSprite("TopHUD/coin_counter_panel.png"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-315f, -62f), new Vector2(250f, 74f), false);

            CreateImage("Coin Icon", coin, LoadSprite("TopHUD/coin_icon.png"),
                Center, Center, new Vector2(-86f, 0f), new Vector2(58f, 58f), true);
            refs.coinsText = CreateTMP("Coin Value", coin, "0", 30f, FontStyles.Bold,
                new Vector2(5f, 0f), new Vector2(100f, 52f));
            refs.coinsText.color = new Color32(74, 47, 34, 255);
            refs.coinPlus = CreateButton("Coin Plus Button", coin, LoadSprite("TopHUD/green_plus_button.png"),
                Center, Center, new Vector2(94f, 0f), new Vector2(50f, 50f));

            RectTransform diamond = CreateImage(
                "Diamond Counter", top, LoadSprite("TopHUD/diamond_counter_panel.png"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-315f, -146f), new Vector2(250f, 74f), false);

            CreateImage("Diamond Icon", diamond, LoadSprite("TopHUD/diamond_icon.png"),
                Center, Center, new Vector2(-86f, 0f), new Vector2(58f, 58f), true);
            refs.diamondsText = CreateTMP("Diamond Value", diamond, "0", 30f, FontStyles.Bold,
                new Vector2(5f, 0f), new Vector2(100f, 52f));
            refs.diamondsText.color = new Color32(74, 47, 34, 255);
            refs.diamondPlus = CreateButton("Diamond Plus Button", diamond, LoadSprite("TopHUD/green_plus_button.png"),
                Center, Center, new Vector2(94f, 0f), new Vector2(50f, 50f));

            refs.pause = CreateButton("Pause Button", top, LoadSprite("TopHUD/pause_button.png"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-95f, -70f), new Vector2(90f, 90f));

            refs.settings = CreateButton("Settings Button", top, LoadSprite("TopHUD/settings_button.png"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-95f, -170f), new Vector2(88f, 88f));
        }

        private static void BuildObjectivePanel(RectTransform root)
        {
            RectTransform objectiveRoot = CreateRect("Objective Panel", root);
            objectiveRoot.anchorMin = new Vector2(1f, 1f);
            objectiveRoot.anchorMax = new Vector2(1f, 1f);
            objectiveRoot.pivot = new Vector2(1f, 1f);
            objectiveRoot.anchoredPosition = new Vector2(-42f, -270f);
            objectiveRoot.sizeDelta = new Vector2(300f, 420f);

            Image highlight = CreateImage("Objective Highlight", objectiveRoot, LoadSprite("States/objective_highlight.png"),
                StretchMin, StretchMax, Vector2.zero, Vector2.zero, false).GetComponent<Image>();
            highlight.color = new Color(1f, 1f, 1f, 0.72f);

            RectTransform panel = CreateImage("Orders Panel Art", objectiveRoot, LoadSprite("Orders/orders_panel.png"),
                StretchMin, StretchMax, Vector2.zero, new Vector2(-18f, -18f), false);

            TMP_Text title = CreateTMP("Orders Title", panel, "ORDERS", 34f, FontStyles.Bold,
                new Vector2(0f, 162f), new Vector2(230f, 55f));
            title.color = new Color32(83, 43, 20, 255);
        }

        private static void BuildConveyorAndServingUI(RectTransform root)
        {
            RectTransform conveyor = CreateRect("Conveyor Overlay", root);
            conveyor.anchorMin = new Vector2(0.5f, 1f);
            conveyor.anchorMax = new Vector2(0.5f, 1f);
            conveyor.pivot = new Vector2(0.5f, 1f);
            conveyor.anchoredPosition = new Vector2(0f, -435f);
            conveyor.sizeDelta = new Vector2(900f, 245f);

            Image rail = CreateImage("Conveyor Rail Frame", conveyor, LoadSprite("Kitchen/conveyor_rail_frame.png"),
                StretchMin, StretchMax, Vector2.zero, Vector2.zero, false).GetComponent<Image>();
            rail.raycastTarget = false;
            rail.color = new Color(1f, 1f, 1f, 0.82f);

            RectTransform tray = CreateImage("Purple Serving Tray", conveyor, LoadSprite("Kitchen/purple_serving_tray.png"),
                Center, Center, new Vector2(0f, -4f), new Vector2(250f, 112f), true);
            tray.GetComponent<Image>().raycastTarget = false;

            RectTransform serving = CreateImage("Serving Slots", root, LoadSprite("Board/serving_slots_panel.png"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-85f, -695f), new Vector2(760f, 128f), false);
            serving.GetComponent<Image>().raycastTarget = false;

            float startX = -294f;
            const float spacing = 98f;
            for (int i = 0; i < 7; i++)
            {
                RectTransform slot = CreateImage("Serving Slot " + (i + 1), serving, LoadSprite("Board/serving_slot.png"),
                    Center, Center, new Vector2(startX + spacing * i, 0f), new Vector2(82f, 82f), true);
                slot.GetComponent<Image>().raycastTarget = false;
            }

            RectTransform boardFrame = CreateImage("Gameplay Board Frame", root, LoadSprite("Decor/industrial_board_frame.png"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 455f), new Vector2(830f, 800f), false);
            Image boardFrameImage = boardFrame.GetComponent<Image>();
            boardFrameImage.raycastTarget = false;
            boardFrameImage.color = new Color(1f, 1f, 1f, 0.16f);
            boardFrame.SetAsFirstSibling();
        }

        private static void BuildBottomToolbar(RectTransform root, out Button homeButton)
        {
            RectTransform toolbar = CreateImage("Bottom Toolbar", root, LoadSprite("Toolbar/bottom_toolbar_panel.png"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(760f, 178f), false);
            toolbar.GetComponent<Image>().raycastTarget = false;

            homeButton = CreateButton("Home Button", toolbar, LoadSprite("Toolbar/home_button.png"),
                Center, Center, new Vector2(-282f, 0f), new Vector2(122f, 122f));

            RectTransform glow = CreateImage("Toolbar Glow", toolbar, LoadSprite("States/panel_glow.png"),
                StretchMin, StretchMax, Vector2.zero, new Vector2(42f, 35f), false);
            Image glowImage = glow.GetComponent<Image>();
            glowImage.raycastTarget = false;
            glowImage.color = new Color(1f, 1f, 1f, 0.32f);
            glow.SetAsFirstSibling();
        }

        private static void BuildOptionalArtLibrary(RectTransform root)
        {
            RectTransform library = CreateRect("Optional Decorative Art [Disabled]", root);
            Stretch(library);
            library.gameObject.SetActive(false);

            string[] previewAssets =
            {
                "Kitchen/kitchen_gameplay_background.png",
                "Kitchen/kitchen_counter_strip.png",
                "Kitchen/conveyor_belt_background.png",
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
            float cellH = 210f;
            float startX = -367.5f;
            float startY = 760f;

            for (int i = 0; i < previewAssets.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;
                string label = Path.GetFileNameWithoutExtension(previewAssets[i]);

                RectTransform holder = CreateRect(label, library);
                holder.anchorMin = Center;
                holder.anchorMax = Center;
                holder.pivot = Center;
                holder.anchoredPosition = new Vector2(startX + col * cellW, startY - row * cellH);
                holder.sizeDelta = new Vector2(210f, 175f);

                RectTransform image = CreateImage("Artwork", holder, LoadSprite(previewAssets[i]),
                    Center, Center, new Vector2(0f, 10f), new Vector2(180f, 135f), true);
                image.GetComponent<Image>().raycastTarget = false;

                TMP_Text text = CreateTMP("Asset Name", holder, label, 18f, FontStyles.Bold,
                    new Vector2(0f, -70f), new Vector2(205f, 30f));
                text.color = Color.white;
            }
        }

        private static void ConfigureExistingOrderPanel()
        {
            GameObject uiGame = FindObjectByExactNameInScene("UI Game");
            if (uiGame == null)
                return;

            Transform order = FindDescendant(uiGame.transform, "OrderPanel");
            if (order == null)
                return;

            RectTransform rect = order as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-190f, -405f);
                rect.localScale = Vector3.one;
            }

            Image image = order.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = false;
            }

            VerticalLayoutGroup vertical = order.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.spacing = 10f;
                vertical.padding = new RectOffset(0, 0, 0, 0);
                vertical.childAlignment = TextAnchor.UpperCenter;
            }

            EditorUtility.SetDirty(order.gameObject);
        }

        private static void ConfigureExistingPowerUpPanel()
        {
            GameObject uiGame = FindObjectByExactNameInScene("UI Game");
            if (uiGame == null)
                return;

            Transform powerUpPanel = FindDescendant(uiGame.transform, "Power Up Panel");
            if (powerUpPanel == null)
                return;

            RectTransform rect = powerUpPanel as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(92f, 52f);
                rect.sizeDelta = new Vector2(560f, 165f);
                rect.localScale = Vector3.one;
            }

            Transform container = FindDescendant(powerUpPanel, "Container");
            RectTransform containerRect = container as RectTransform;
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0f, 0f);
                containerRect.anchorMax = new Vector2(1f, 1f);
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;

                HorizontalLayoutGroup horizontal = container.GetComponent<HorizontalLayoutGroup>();
                if (horizontal != null)
                {
                    horizontal.spacing = 18f;
                    horizontal.childAlignment = TextAnchor.MiddleCenter;
                    horizontal.childForceExpandWidth = false;
                    horizontal.childForceExpandHeight = false;
                }
            }

            EditorUtility.SetDirty(powerUpPanel.gameObject);
        }

        private static void ApplyLegacyVisualCleanup()
        {
            GameObject mainMenu = FindObjectByExactNameInScene("UI Main Menu");
            if (mainMenu == null)
                return;

            SetDescendantActive(mainMenu.transform, "Level Text", false);
            SetDescendantActive(mainMenu.transform, "Currency Panel Simple", false);
            SetDescendantActive(mainMenu.transform, "Lives Indicator", false);
            SetDescendantActive(mainMenu.transform, "OrderPanel (1)", false);
            SetDescendantActive(mainMenu.transform, "SettingsButton", false);

            Transform safeZone = FindDescendant(mainMenu.transform, "Safe Zone");
            if (safeZone != null)
            {
                foreach (Transform child in safeZone)
                {
                    if (child.name != "Image")
                        continue;

                    RectTransform rect = child as RectTransform;
                    if (rect != null && rect.sizeDelta.x > 1000f && rect.sizeDelta.y > 250f)
                    {
                        child.gameObject.SetActive(false);
                        EditorUtility.SetDirty(child.gameObject);
                        break;
                    }
                }
            }

            GameObject uiGame = FindObjectByExactNameInScene("UI Game");
            if (uiGame != null)
            {
                SetDirectOrDescendantActive(uiGame.transform, "Level Text", false);
                SetDirectOrDescendantActive(uiGame.transform, "Replay Button", false);
            }
        }

        private static void ApplyExistingPopupArtwork()
        {
            GameObject complete = FindObjectByExactNameInScene("UI Complete");
            if (complete != null)
            {
                SetImageSprite(FindDescendant(complete.transform, "Background Image"), LoadSprite("Popups/level_complete_popup.png"), true);
                Transform continueButton = FindDescendant(complete.transform, "No Thanks Button");
                SetImageSprite(continueButton, LoadSprite("Popups/continue_button.png"), true);
                HideTMPChildren(continueButton);
                SetImageSprite(FindDescendant(complete.transform, "Go Home"), LoadSprite("Toolbar/home_button.png"), true);

                Transform noThanksText = FindDescendant(complete.transform, "No Thanks Text");
                if (noThanksText != null)
                {
                    TMP_Text tmp = noThanksText.GetComponent<TMP_Text>();
                    if (tmp != null)
                        tmp.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            GameObject gameOver = FindObjectByExactNameInScene("UI Game Over");
            if (gameOver != null)
            {
                SetImageSprite(FindDescendant(gameOver.transform, "Background Image"), LoadSprite("Popups/level_failed_popup.png"), true);
                Transform replayButton = FindDescendant(gameOver.transform, "Replay Button");
                SetImageSprite(replayButton, LoadSprite("Popups/restart_button.png"), true);
                HideTMPChildren(replayButton);

                Transform failedText = FindDescendant(gameOver.transform, "Level Failed Text");
                if (failedText != null)
                    failedText.gameObject.SetActive(false);

                Transform home = FindDescendant(gameOver.transform, "home (2)");
                if (home != null)
                    SetImageSprite(home, LoadSprite("Toolbar/home_button.png"), true);
            }

            GameObject uiGame = FindObjectByExactNameInScene("UI Game");
            if (uiGame != null)
            {
                Transform popup = FindDescendant(uiGame.transform, "Quit Pop Up");
                if (popup != null)
                {
                    SetImageSprite(FindDescendant(popup, "Panel Back"), LoadSprite("Popups/pause_popup_panel.png"), false);
                    Transform quitButton = FindDescendant(popup, "Quit Button");
                    Transform closeButton = FindDescendant(popup, "Close Button");
                    SetImageSprite(quitButton, LoadSprite("Popups/restart_button.png"), true);
                    SetImageSprite(closeButton, LoadSprite("Popups/continue_button.png"), true);
                    HideTMPChildren(quitButton);
                    HideTMPChildren(closeButton);

                    Transform quitText = FindDescendant(popup, "Quit Text");
                    if (quitText != null)
                        quitText.gameObject.SetActive(false);
                }
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
            RectTransform rect = CreateImage(name, parent, sprite, anchorMin, anchorMax, anchoredPosition, sizeDelta, true);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;

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
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

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

        private static void SetImageSprite(Transform transform, Sprite sprite, bool preserveAspect)
        {
            if (transform == null || sprite == null)
                return;

            Image image = transform.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            EditorUtility.SetDirty(image);
        }

        private static void SetDescendantActive(Transform root, string name, bool active)
        {
            Transform found = FindDescendant(root, name);
            if (found == null)
                return;

            found.gameObject.SetActive(active);
            EditorUtility.SetDirty(found.gameObject);
        }

        private static void SetDirectOrDescendantActive(Transform root, string name, bool active)
        {
            Transform found = root.Find(name) ?? FindDescendant(root, name);
            if (found == null)
                return;

            found.gameObject.SetActive(active);
            EditorUtility.SetDirty(found.gameObject);
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

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    dirty = true;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    dirty = true;
                }
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    dirty = true;
                }

                if (dirty)
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
                Debug.LogError("[GameUI] Could not import art pack: " + exception);
                return false;
            }
        }

        private static void FocusEditableRoot(GameObject root, bool frameSceneView = true)
        {
            if (root == null)
                return;

            Selection.activeGameObject = root;
            if (frameSceneView && SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 StretchMin = Vector2.zero;
        private static readonly Vector2 StretchMax = Vector2.one;
    }
}
#endif
