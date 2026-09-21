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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon.EditorTools
{
    /// <summary>
    /// One-time editor baker for CountryMap.unity.
    /// The complete UI is serialized into the scene, exactly like menu/loading.
    /// Runtime scripts never rebuild or rearrange the authored layout.
    /// </summary>
    [InitializeOnLoad]
    public static class CountryMapSceneBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/CountryMap.unity";
        private const string AssetFolder = "Assets/Project Data/Game/Images/CountryMap";
        private const float W = 1080f;
        private const float H = 1920f;

        private static readonly string[] Required =
        {
            "glossy_blue_back_button.png",
            "glossy_blue_gear_settings_icon.png",
            "glossy_gold_dollar_coin_icon.png",
            "glossy_green_add_button.png",
            "glossy_golden_game_star_icon.png",
            "glossy_blue_locked_level_icon.png",
            "glossy_blue_game_map_node.png",
            "glossy_golden_compass_rose_icon.png",
            "magical_golden_blue_selection_halo.png",
            "colourful_fantasy_map_of_asia.png",
            "glossy_chef_s_wooden_title_banner.png",
            "hanging_culinary_treasure_map_scroll.png",
            "glossy_country_name_badge_frame.png",
            "glossy_blue_game_progress_panel.png",
            "chinese_pagoda_and_dumpling_garden.png",
            "japanese_island_diorama_with_mount_fuji.png",
            "india_themed_taj_mahal_garden_diorama.png",
            "korean_palace_and_bibimbap_diorama.png",
            "thai_temple_island_adventure.png",
            "glossy_china_flag_game_badge.png",
            "glossy_japanese_sun_emblem.png",
            "glossy_india_flag_badge.png",
            "glossy_south_korean_flag_badge.png",
            "glossy_thailand_flag_badge.png",
            "cheerful_chef_mascot_welcoming_gesture.png",
            "glossy_chef_s_dialogue_bubble.png",
            "asia_progress_map_ui_panel.png",
            "glossy_green_to_gold_progress_bar.png",
            "crowned_globe_completion_badge.png",
            "glossy_locked_level_badge.png"
        };

        private static readonly string[] ProgressWidgetAssets =
        {
            "progress_ui_outer_blue.png",
            "progress_ui_inner_cream.png",
            "progress_ui_globe_frame.png",
            "progress_ui_globe_asia.png",
            "progress_ui_title_plaque.png",
            "progress_ui_leaf_left.png",
            "progress_ui_leaf_right.png",
            "progress_ui_track.png",
            "progress_ui_fill.png",
            "progress_ui_value_badge.png"
        };

        private static readonly string[] CountryNames =
        {
            "China", "Japan", "India", "South Korea", "Thailand"
        };

        private static readonly string[] Landmarks =
        {
            "chinese_pagoda_and_dumpling_garden.png",
            "japanese_island_diorama_with_mount_fuji.png",
            "india_themed_taj_mahal_garden_diorama.png",
            "korean_palace_and_bibimbap_diorama.png",
            "thai_temple_island_adventure.png"
        };

        private static readonly string[] Flags =
        {
            "glossy_china_flag_game_badge.png",
            "glossy_japanese_sun_emblem.png",
            "glossy_india_flag_badge.png",
            "glossy_south_korean_flag_badge.png",
            "glossy_thailand_flag_badge.png"
        };

        private static readonly Vector2[] Positions =
        {
            new Vector2(-285f, 350f),
            new Vector2(285f, 245f),
            new Vector2(-305f, -60f),
            new Vector2(290f, -170f),
            new Vector2(20f, -495f)
        };

        static CountryMapSceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBake;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != ScenePath)
                return;

            EditorApplication.delayCall += TryAutoBake;
        }

        private static void TryAutoBake()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != ScenePath)
                return;

            EnsureFolders();
            AssetDatabase.Refresh();
            ImportSprites();

            if (Missing().Count == 0 && IsPlaceholder())
            {
                Bake(false);
            }
            else
            {
                EnsureChefBehindProgressPanel(active, false);

                if (MissingProgressWidgetAssets().Count == 0)
                    UpgradeEditableProgressWidget(active, false);

                if (active.isDirty)
                    EditorSceneManager.SaveScene(active);

                Focus(active);
            }
        }

        [MenuItem("Conveyor Chef/Country Map/0. Import Generated Art Pack", priority = 0)]
        public static void ImportGeneratedArtPack()
        {
            string zipPath = EditorUtility.OpenFilePanel(
                "Select ConveyorChef_CountryMap_Assets_ForUnity.zip",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "zip");

            if (string.IsNullOrWhiteSpace(zipPath))
                return;

            EnsureFolders();

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (string required in Required)
                    {
                        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e =>
                            string.Equals(Path.GetFileName(e.FullName), required, StringComparison.OrdinalIgnoreCase));

                        if (entry == null)
                            continue;

                        string destination = (AssetFolder + "/" + required).Replace('\\', '/');
                        entry.ExtractToFile(destination, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CountryMap] Generated art import failed: " + ex);
                EditorUtility.DisplayDialog("Country Map Art", "Import failed:\n\n" + ex.Message, "OK");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = Missing();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Country Map Art",
                    "The selected ZIP is missing:\n\n- " + string.Join("\n- ", missing),
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Bake(false);

            EditorUtility.DisplayDialog(
                "Country Map Ready",
                "All 30 generated assets were imported and the complete editable CountryMap.unity scene was baked.\n\n" +
                "Open Canvas > NEW Country Map to adjust every element in Scene/Inspector.",
                "OK");
        }

        [MenuItem("Conveyor Chef/Country Map/1. Bake or Replace Editable Country Map", priority = 1)]
        public static void BakeMenu()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            ImportSprites();

            List<string> missing = Missing();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Country Map Art Missing",
                    "Copy/extract the 30 generated PNG files into:\n\n" + AssetFolder +
                    "\n\nMissing:\n- " + string.Join("\n- ", missing),
                    "OK");
                return;
            }

            if (File.Exists(ScenePath) && !IsPlaceholder())
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace CountryMap.unity?",
                    "This resets the Country Map layout. Use it only if you intentionally want a fresh scene.",
                    "Replace",
                    "Cancel");

                if (!replace)
                    return;
            }

            Bake(true);
        }

        [MenuItem("Conveyor Chef/Country Map/2. Open Editable Country Map", priority = 2)]
        public static void OpenMenu()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Country Map", "CountryMap.unity does not exist yet.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureBuildSettings();

            if (IsPlaceholder() && Missing().Count == 0)
                Bake(false);
            else
                Focus(scene);
        }

        [MenuItem("Conveyor Chef/Country Map/3. Validate Country Map", priority = 3)]
        public static void ValidateMenu()
        {
            List<string> missing = Missing();
            bool exists = File.Exists(ScenePath);
            bool inBuild = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath);

            string hierarchy = "NOT OPEN";
            int images = 0;
            int sprites = 0;

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.path == ScenePath)
            {
                GameObject root = GameObject.Find("NEW Country Map");
                hierarchy = root != null ? "OK - EDITABLE SERIALIZED UI" : "PLACEHOLDER";
                if (root != null)
                {
                    Image[] all = root.GetComponentsInChildren<Image>(true);
                    images = all.Length;
                    sprites = all.Count(i => i != null && i.sprite != null);
                }
            }

            string message =
                "CountryMap.unity: " + (exists ? "OK" : "MISSING") + "\n" +
                "Build Settings: " + (inBuild ? "OK" : "MISSING") + "\n" +
                "Hierarchy: " + hierarchy + "\n" +
                "Generated art: " + (Required.Length - missing.Count) + "/" + Required.Length + "\n" +
                "Scene Images: " + images + "\n" +
                "Images with Sprite: " + sprites;

            if (missing.Count > 0)
                message += "\n\nMissing:\n- " + string.Join("\n- ", missing);

            Debug.Log("[CountryMapValidator]\n" + message);
            EditorUtility.DisplayDialog("Country Map", message, "OK");
        }

        [MenuItem("Conveyor Chef/Country Map/4. Put Chef Behind Progress", priority = 4)]
        public static void PutChefBehindProgressMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = EnsureChefBehindProgressPanel(scene, true);
            Focus(scene);

            EditorUtility.DisplayDialog(
                "Country Map",
                changed
                    ? "Chef draw order fixed. The progress panel now renders in front of the chef."
                    : "Chef is already behind the progress panel.",
                "OK");
        }

        [MenuItem("Conveyor Chef/Country Map/5. Import Country Progress Widget Assets", priority = 5)]
        public static void ImportCountryProgressWidgetAssets()
        {
            string zipPath = EditorUtility.OpenFilePanel(
                "Select ConveyorChef_CountryProgress_Widget_Assets.zip",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "zip");

            if (string.IsNullOrWhiteSpace(zipPath))
                return;

            EnsureFolders();

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (string required in ProgressWidgetAssets)
                    {
                        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e =>
                            string.Equals(Path.GetFileName(e.FullName), required, StringComparison.OrdinalIgnoreCase));

                        if (entry == null)
                            continue;

                        string destination = (AssetFolder + "/" + required).Replace('\\', '/');
                        entry.ExtractToFile(destination, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CountryMap] Country progress widget import failed: " + ex);
                EditorUtility.DisplayDialog("Country Progress Widget", "Import failed:\n\n" + ex.Message, "OK");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = MissingProgressWidgetAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Country Progress Widget",
                    "The selected ZIP is missing:\n\n- " + string.Join("\n- ", missing),
                    "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            UpgradeEditableProgressWidget(scene, true);
            Focus(scene);

            EditorUtility.DisplayDialog(
                "Country Progress Widget Ready",
                "The 10 separated progress assets were imported and the Country Map progress section was rebuilt using only those sprites.\n\n" +
                "All progress references were rebound and the scene was saved.",
                "OK");
        }

        [MenuItem("Conveyor Chef/Country Map/6. Install or Refresh Editable Country Progress Widget", priority = 6)]
        public static void InstallEditableCountryProgressWidget()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = MissingProgressWidgetAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Country Progress Widget",
                    "The correct progress_ui_* sprites are required before installation.\n\nMissing:\n- " +
                    string.Join("\n- ", missing),
                    "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RectTransform panel =
                GameObject.Find("NEW Country Map")?.transform.Find("Continent Progress Panel") as RectTransform;

            if (panel != null)
            {
                BuildEditableProgressWidget(
                    panel,
                    out TextMeshProUGUI title,
                    out TextMeshProUGUI value,
                    out Image fill);

                CountryMapSceneController controller =
                    panel.GetComponentInParent<CountryMapSceneController>();

                if (controller == null)
                {
                    GameObject rootObject = GameObject.Find("NEW Country Map");
                    controller = rootObject != null
                        ? rootObject.GetComponentInChildren<CountryMapSceneController>(true)
                        : null;
                }

                TextMeshProUGUI status =
                    GameObject.Find("NEW Country Map")?.transform.Find("Status Text")
                        ?.GetComponent<TextMeshProUGUI>();

                if (controller != null)
                {
                    controller.EditorConfigureProgress(title, value, fill, status);
                    EditorUtility.SetDirty(controller);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                UpgradeEditableProgressWidget(scene, true);
            }

            Focus(scene);

            EditorUtility.DisplayDialog(
                "Country Progress Widget",
                "The progress panel was rebuilt using only the correct progress_ui_* sprites, references were rebound, and CountryMap.unity was saved.",
                "OK");
        }

        [MenuItem("Conveyor Chef/Country Map/7. Repair Missing Progress Panel + References", priority = 7)]
        public static void RepairMissingProgressPanelAndReferences()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = MissingProgressWidgetAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Country Progress Widget",
                    "Cannot repair with incorrect/fallback artwork. The following required sprites are missing:\n\n- " +
                    string.Join("\n- ", missing) +
                    "\n\nPlace them in:\n" + AssetFolder,
                    "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            UpgradeEditableProgressWidget(scene, true);
            Focus(scene);

            GameObject root = GameObject.Find("NEW Country Map");
            RectTransform panel =
                root != null ? root.transform.Find("Continent Progress Panel") as RectTransform : null;

            if (panel != null)
                Selection.activeGameObject = panel.gameObject;

            EditorUtility.DisplayDialog(
                "Country Progress Widget Repaired",
                "Continent Progress Panel was created/repaired, all separated children were placed, controller references were rebound, and CountryMap.unity was saved.",
                "OK");
        }

        [MenuItem("Conveyor Chef/Country Map/8. Prepare Asia Play-Mode Preview", priority = 8)]
        public static void PrepareAsiaPreview()
        {
            PlayerPrefs.SetInt("CC_WorldMap_SelectedContinent", 0);
            PlayerPrefs.SetInt("CC_CountryMap_SelectedCountry", 0);
            PlayerPrefs.Save();

            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Country Map", "CountryMap.unity does not exist yet.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Focus(scene);

            Debug.Log("[CountryMap] Asia preview state prepared. Press Play to test the generated Asia country map.");
        }

        private static void Bake(bool showDialog)
        {
            EnsureFolders();
            ImportSprites();

            if (Missing().Count > 0)
                return;

            Sprite back = S("glossy_blue_back_button.png");
            Sprite settings = S("glossy_blue_gear_settings_icon.png");
            Sprite coin = S("glossy_gold_dollar_coin_icon.png");
            Sprite plus = S("glossy_green_add_button.png");
            Sprite star = S("glossy_golden_game_star_icon.png");
            Sprite routeLock = S("glossy_blue_locked_level_icon.png");
            Sprite routeNode = S("glossy_blue_game_map_node.png");
            Sprite compass = S("glossy_golden_compass_rose_icon.png");
            Sprite glow = S("magical_golden_blue_selection_halo.png");
            Sprite background = S("colourful_fantasy_map_of_asia.png");
            Sprite titleBoard = S("glossy_chef_s_wooden_title_banner.png");
            Sprite parchment = S("hanging_culinary_treasure_map_scroll.png");
            Sprite nameFrame = S("glossy_country_name_badge_frame.png");
            Sprite progressFrame = S("glossy_blue_game_progress_panel.png");
            Sprite chef = S("cheerful_chef_mascot_welcoming_gesture.png");
            Sprite bubble = S("glossy_chef_s_dialogue_bubble.png");
            Sprite bottomPanel = S("asia_progress_map_ui_panel.png");
            Sprite progressFillSprite = S("glossy_green_to_gold_progress_bar.png");
            Sprite completedBadge = S("crowned_globe_completion_badge.png");
            Sprite lockedOverlay = S("glossy_locked_level_badge.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            RectTransform root = R("NEW Country Map", canvas.transform);
            Stretch(root);
            root.gameObject.AddComponent<Watermelon.CountryMapResponsiveLayout>();

            Image bg = I("Background Artwork", root, background, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, H), false);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            RectTransform routeLayer = R("Route Layer", root);
            Stretch(routeLayer);
            BuildRoute(routeLayer, routeNode, routeLock);

            Button backButton = B("BackButton", root, back, new Vector2(0f, 1f), new Vector2(145f, -78f), new Vector2(250f, 84f), true);
            Button settingsButton = B("SettingsButton", root, settings, new Vector2(1f, 1f), new Vector2(-55f, -75f), new Vector2(82f, 82f), true);
            AddButtonFX(backButton.gameObject);
            AddButtonFX(settingsButton.gameObject);

            Image title = I("CountryMapTitleBoard", root, titleBoard, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(520f, 174f), true);
            TextMeshProUGUI titleText = T("ContinentTitle", title.transform, "ASIA", 66f, new Vector2(0f, 14f), new Vector2(380f, 82f));
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.12f, 0.20f, 0.36f, 1f);

            TextMeshProUGUI subtitleText = T("ContinentSubtitle", title.transform, "CULINARY JOURNEY", 23f, new Vector2(0f, -43f), new Vector2(380f, 42f));
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.color = new Color(0.32f, 0.19f, 0.08f, 1f);

            Image coinBar = I("Coin Counter", root, progressFrame, new Vector2(1f, 1f), new Vector2(-188f, -76f), new Vector2(245f, 82f), false);
            Image coinIcon = I("Coin Icon", coinBar.transform, coin, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(68f, 68f), true);
            coinIcon.raycastTarget = false;

            TextMeshProUGUI coinText = T("Coin Value", coinBar.transform, "1,250", 29f, new Vector2(18f, 0f), new Vector2(110f, 54f));
            coinText.fontStyle = FontStyles.Bold;
            Button plusButton = B("Coin Plus Button", coinBar.transform, plus, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(62f, 62f), true);
            AddButtonFX(plusButton.gameObject);

            Image info = I("CountryInfoParchment", root, parchment, new Vector2(0f, 1f), new Vector2(132f, -315f), new Vector2(230f, 288f), true);
            TextMeshProUGUI infoText = T("CountryInfoText", info.transform, "Explore amazing cuisines\nand cultures across Asia!", 25f, new Vector2(0f, -4f), new Vector2(166f, 170f));
            infoText.fontStyle = FontStyles.Bold;
            infoText.color = new Color(0.22f, 0.14f, 0.08f, 1f);

            RectTransform countries = R("Countries", root);
            Stretch(countries);

            List<CountryMapCountryNode> nodes = new List<CountryMapCountryNode>();
            for (int i = 0; i < CountryNames.Length; i++)
            {
                nodes.Add(CreateCountryNode(
                    countries,
                    i,
                    CountryNames[i],
                    Positions[i],
                    S(Landmarks[i]),
                    S(Flags[i]),
                    glow,
                    nameFrame,
                    progressFrame,
                    star,
                    lockedOverlay,
                    completedBadge,
                    i == 0,
                    i == 0));
            }

            Image compassImage = I("Compass", root, compass, new Vector2(1f, 0f), new Vector2(-95f, 315f), new Vector2(150f, 150f), true);
            compassImage.raycastTarget = false;

            Image chefImage = I("Chef Guide Mascot", root, chef, new Vector2(0f, 0f), new Vector2(115f, 240f), new Vector2(210f, 280f), true);
            chefImage.raycastTarget = false;

            Image speech = I("Chef Speech Bubble", root, bubble, new Vector2(0f, 0f), new Vector2(365f, 265f), new Vector2(350f, 117f), false);
            TextMeshProUGUI guideText = T("Guide Text", speech.transform, "Complete countries to unlock new recipes and levels!", 21f, Vector2.zero, new Vector2(270f, 74f));
            guideText.fontStyle = FontStyles.Bold;
            guideText.color = new Color(0.12f, 0.20f, 0.34f, 1f);

            Image progressPanel = I("Continent Progress Panel", root, bottomPanel, new Vector2(0.5f, 0f), new Vector2(35f, 112f), new Vector2(790f, 263f), false);
            TextMeshProUGUI progressTitle;
            TextMeshProUGUI progressValue;
            Image fill;

            List<string> missingProgressAssets = MissingProgressWidgetAssets();
            if (missingProgressAssets.Count > 0)
            {
                throw new InvalidOperationException(
                    "Country Map progress widget assets are missing:\n- " +
                    string.Join("\n- ", missingProgressAssets));
            }

            BuildEditableProgressWidget(
                progressPanel.rectTransform,
                out progressTitle,
                out progressValue,
                out fill);

            TextMeshProUGUI status = T("Status Text", root, "CHINA  •  0/3", 22f, new Vector2(0f, -685f), new Vector2(650f, 46f));
            status.fontStyle = FontStyles.Bold;

            GameObject settingsPanel = BuildSettings(root, nameFrame, out Button closeSettings, out Button sound, out TextMeshProUGUI soundText, out Button vibration, out TextMeshProUGUI vibrationText);
            settingsPanel.SetActive(false);

            GameObject unsupported = BuildUnsupported(root, nameFrame, out TextMeshProUGUI unsupportedText);
            unsupported.SetActive(false);

            GameObject controllerObject = new GameObject("Country Map Controller");
            controllerObject.transform.SetParent(root, false);
            CountryMapSceneController controller = controllerObject.AddComponent<CountryMapSceneController>();
            controller.EditorConfigure(
                nodes.ToArray(),
                backButton,
                settingsButton,
                plusButton,
                titleText,
                subtitleText,
                coinText,
                infoText,
                guideText,
                progressTitle,
                progressValue,
                fill,
                status,
                settingsPanel,
                closeSettings,
                sound,
                soundText,
                vibration,
                vibrationText,
                unsupported,
                unsupportedText);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Focus(scene);

            Debug.Log("[CountryMap] Baked complete editable CountryMap.unity. Runtime will not rebuild the UI.");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Country Map Ready",
                    "Canvas > NEW Country Map now contains the complete editable 9:16 design.\n\n" +
                    "Every generated asset is a real Image object and can be adjusted from Scene/Inspector.",
                    "OK");
            }
        }

        private static CountryMapCountryNode CreateCountryNode(
            RectTransform parent,
            int index,
            string displayName,
            Vector2 position,
            Sprite landmarkSprite,
            Sprite flagSprite,
            Sprite glowSprite,
            Sprite labelSprite,
            Sprite progressSprite,
            Sprite starSprite,
            Sprite lockedSprite,
            Sprite completedSprite,
            bool unlocked,
            bool selected)
        {
            RectTransform root = R("Country_" + (index + 1) + "_" + Safe(displayName), parent);
            Set(root, new Vector2(0.5f, 0.5f), position, new Vector2(350f, 350f));

            Image glow = I("Selected Glow", root, glowSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(300f, 300f), true);
            glow.raycastTarget = false;
            glow.gameObject.SetActive(selected && unlocked);

            Image landmark = I("Landmark", root, landmarkSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(270f, 270f), true);
            landmark.raycastTarget = false;
            landmark.color = unlocked ? Color.white : new Color(0.58f, 0.64f, 0.72f, 0.9f);

            Image label = I("Country Name Frame", root, labelSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(270f, 90f), false);
            Image flag = I("Flag", label.transform, flagSprite, new Vector2(0f, 0.5f), new Vector2(41f, 0f), new Vector2(72f, 72f), true);
            flag.raycastTarget = false;

            TextMeshProUGUI name = T("Country Name", label.transform, displayName, 29f, new Vector2(34f, 0f), new Vector2(180f, 58f));
            name.fontStyle = FontStyles.Bold;
            name.color = new Color(0.12f, 0.20f, 0.34f, 1f);

            Image progress = I("Country Progress Frame", root, progressSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, -145f), new Vector2(195f, 65f), false);
            Image star = I("Star", progress.transform, starSprite, new Vector2(0f, 0.5f), new Vector2(35f, 0f), new Vector2(48f, 48f), true);
            star.raycastTarget = false;

            TextMeshProUGUI progressText = T("Progress", progress.transform, "0/3", 26f, new Vector2(32f, 0f), new Vector2(86f, 48f));
            progressText.fontStyle = FontStyles.Bold;

            Image locked = I("Locked Country Overlay", root, lockedSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(112f, 112f), true);
            locked.raycastTarget = false;
            locked.gameObject.SetActive(!unlocked);

            Image completed = I("Completed Country Badge", root, completedSprite, new Vector2(1f, 1f), new Vector2(-42f, -46f), new Vector2(92f, 92f), true);
            completed.raycastTarget = false;
            completed.gameObject.SetActive(false);

            // Transparent raycast graphic + Button live on the country ROOT.
            // WorldMapButtonFX is added to this Button at runtime, so the complete
            // landmark/label/progress cluster visibly presses instead of scaling
            // an invisible child hitbox.
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.001f);
            hit.raycastTarget = true;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            AddButtonFX(root.gameObject);

            CountryMapCountryNode node = root.gameObject.AddComponent<CountryMapCountryNode>();
            node.EditorConfigure(index, displayName, button, landmark, flag, glow, label, name, progress, star, progressText, locked.gameObject, completed.gameObject);
            return node;
        }

        private static void BuildEditableProgressWidget(
            RectTransform panel,
            out TextMeshProUGUI progressTitle,
            out TextMeshProUGUI progressValue,
            out Image fill)
        {
            // Parent is only a layout container now. Keep it serialized/editable.
            Image legacyImage = panel.GetComponent<Image>();
            if (legacyImage != null)
            {
                legacyImage.sprite = null;
                legacyImage.color = Color.clear;
                legacyImage.raycastTarget = false;
            }

            // Clean only the progress-panel children; never touch the rest of CountryMap.
            for (int i = panel.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(panel.GetChild(i).gameObject);

            Set(panel, new Vector2(0.5f, 0f), new Vector2(35f, 112f), new Vector2(790f, 263f));

            Image outer = I("Outer Base", panel, ProgressSprite("progress_ui_outer_blue.png", "asia_progress_map_ui_panel.png"),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(760f, 185f), false);

            Image inner = I("Inner Cream Panel", panel, ProgressSprite("progress_ui_inner_cream.png", "glossy_country_name_badge_frame.png"),
                new Vector2(0.5f, 0.5f), new Vector2(45f, -10f), new Vector2(625f, 145f), false);

            RectTransform globeGroup = R("Globe Group", panel);
            Set(globeGroup, new Vector2(0.5f, 0.5f), new Vector2(-300f, -10f), new Vector2(190f, 190f));
            I("Globe Frame", globeGroup, ProgressSprite("progress_ui_globe_frame.png", "crowned_globe_completion_badge.png"),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f), true);
            I("Globe Icon", globeGroup, ProgressSprite("progress_ui_globe_asia.png", "asia_progress_map_ui_panel.png"),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(145f, 145f), true);

            RectTransform titleGroup = R("Title Group", panel);
            Set(titleGroup, new Vector2(0.5f, 0.5f), new Vector2(25f, 79f), new Vector2(460f, 110f));

            Image leftLeaf = I("Left Leaf", titleGroup, ProgressSprite("progress_ui_leaf_left.png", "magical_golden_blue_selection_halo.png"),
                new Vector2(0.5f, 0.5f), new Vector2(-205f, 3f), new Vector2(100f, 100f), true);
            leftLeaf.transform.localEulerAngles = new Vector3(0f, 0f, -8f);

            Image rightLeaf = I("Right Leaf", titleGroup, ProgressSprite("progress_ui_leaf_right.png", "magical_golden_blue_selection_halo.png"),
                new Vector2(0.5f, 0.5f), new Vector2(205f, 3f), new Vector2(100f, 100f), true);
            rightLeaf.transform.localEulerAngles = new Vector3(0f, 0f, 8f);

            I("Title Plaque", titleGroup, ProgressSprite("progress_ui_title_plaque.png", "glossy_country_name_badge_frame.png"),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(410f, 103f), false);

            progressTitle = T("Progress Title", titleGroup, "CHINA PROGRESS", 29f,
                new Vector2(0f, 1f), new Vector2(330f, 52f));
            progressTitle.fontStyle = FontStyles.Bold;
            progressTitle.color = new Color(0.05f, 0.19f, 0.43f, 1f);
            progressTitle.textWrappingMode = TextWrappingModes.NoWrap;

            Image track = I("Progress Track", panel, ProgressSprite("progress_ui_track.png", "glossy_blue_game_progress_panel.png"),
                new Vector2(0.5f, 0.5f), new Vector2(30f, -28f), new Vector2(420f, 82f), false);

            fill = I("Progress Fill", track.transform, ProgressSprite("progress_ui_fill.png", "glossy_green_to_gold_progress_bar.png"),
                new Vector2(0f, 0.5f), new Vector2(43f, 0f), new Vector2(0f, 34f), false);
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(43f, 0f);
            fill.rectTransform.sizeDelta = new Vector2(0f, 34f);
            fill.type = Image.Type.Simple;
            fill.preserveAspect = false;
            fill.raycastTarget = false;

            Image badge = I("Value Badge", panel, ProgressSprite("progress_ui_value_badge.png", "glossy_country_name_badge_frame.png"),
                new Vector2(0.5f, 0.5f), new Vector2(307f, -28f), new Vector2(126f, 78f), false);

            progressValue = T("Progress Value", badge.transform, "0/3", 31f,
                Vector2.zero, new Vector2(102f, 54f));
            progressValue.fontStyle = FontStyles.Bold;
            progressValue.color = new Color(0.05f, 0.19f, 0.43f, 1f);
            progressValue.textWrappingMode = TextWrappingModes.NoWrap;

            // Explicit ordering: decorative base first, then globe/title/track/value.
            outer.transform.SetAsFirstSibling();
            inner.transform.SetSiblingIndex(1);
            globeGroup.SetSiblingIndex(2);
            titleGroup.SetSiblingIndex(3);
            track.transform.SetSiblingIndex(4);
            badge.transform.SetSiblingIndex(5);
        }

        private static bool UpgradeEditableProgressWidget(Scene scene, bool saveIfChanged)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                return false;

            List<string> missingAssets = MissingProgressWidgetAssets();
            if (missingAssets.Count > 0)
            {
                Debug.LogError(
                    "[CountryMap] Cannot build progress widget. Missing:\n- " +
                    string.Join("\n- ", missingAssets));
                return false;
            }

            GameObject root = GameObject.Find("NEW Country Map");
            if (root == null)
                return false;

            CountryMapSceneController controller =
                root.GetComponentInChildren<CountryMapSceneController>(true);

            if (controller == null)
                return false;

            bool changed = false;

            RectTransform panel = root.transform.Find("Continent Progress Panel") as RectTransform;
            if (panel == null)
            {
                panel = R("Continent Progress Panel", root.transform);
                Set(panel, new Vector2(0.5f, 0f), new Vector2(35f, 112f), new Vector2(790f, 263f));

                Image containerImage = panel.gameObject.AddComponent<Image>();
                containerImage.color = Color.clear;
                containerImage.raycastTarget = false;
                changed = true;
            }

            if (!HasEditableProgressWidget(panel))
            {
                BuildEditableProgressWidget(
                    panel,
                    out TextMeshProUGUI rebuiltTitle,
                    out TextMeshProUGUI rebuiltValue,
                    out Image rebuiltFill);

                changed = true;
            }

            TextMeshProUGUI title =
                panel.Find("Title Group/Progress Title")?.GetComponent<TextMeshProUGUI>();

            TextMeshProUGUI value =
                panel.Find("Value Badge/Progress Value")?.GetComponent<TextMeshProUGUI>();

            Image fill =
                panel.Find("Progress Track/Progress Fill")?.GetComponent<Image>();

            TextMeshProUGUI status =
                root.transform.Find("Status Text")?.GetComponent<TextMeshProUGUI>();

            if (status == null)
            {
                status = T(
                    "Status Text",
                    root.transform,
                    "CHINA  •  0/3",
                    22f,
                    new Vector2(0f, -685f),
                    new Vector2(650f, 46f));

                status.fontStyle = FontStyles.Bold;
                status.color = Color.white;
                changed = true;
            }

            if (title == null || value == null || fill == null)
            {
                Debug.LogError("[CountryMap] Progress widget hierarchy could not be repaired.");
                return false;
            }

            controller.EditorConfigureProgress(title, value, fill, status);

            // Keep the exact hierarchy order requested in the editable Canvas:
            // Chef Guide Mascot -> Continent Progress Panel -> Chef Speech Bubble.
            Transform chef = root.transform.Find("Chef Guide Mascot");
            Transform speech = root.transform.Find("Chef Speech Bubble");

            if (chef != null)
            {
                int desiredPanelIndex = Mathf.Min(chef.GetSiblingIndex() + 1, root.transform.childCount - 1);
                if (panel.GetSiblingIndex() != desiredPanelIndex)
                {
                    panel.SetSiblingIndex(desiredPanelIndex);
                    changed = true;
                }

                // SetSiblingIndex can move the speech index. Ensure it remains in front.
                if (speech != null && speech.GetSiblingIndex() <= panel.GetSiblingIndex())
                {
                    speech.SetSiblingIndex(
                        Mathf.Min(panel.GetSiblingIndex() + 1, root.transform.childCount - 1));
                    changed = true;
                }
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(panel);

            if (changed)
                EditorSceneManager.MarkSceneDirty(scene);

            if (saveIfChanged && scene.isDirty)
                EditorSceneManager.SaveScene(scene);

            Debug.Log("[CountryMap] Progress panel hierarchy and controller references are valid.");
            return changed;
        }

        private static bool HasEditableProgressWidget(RectTransform panel)
        {
            if (panel == null)
                return false;

            return
                HasExpectedSprite(panel, "Outer Base", "progress_ui_outer_blue.png") &&
                HasExpectedSprite(panel, "Inner Cream Panel", "progress_ui_inner_cream.png") &&
                HasExpectedSprite(panel, "Globe Group/Globe Frame", "progress_ui_globe_frame.png") &&
                HasExpectedSprite(panel, "Globe Group/Globe Icon", "progress_ui_globe_asia.png") &&
                HasExpectedSprite(panel, "Title Group/Left Leaf", "progress_ui_leaf_left.png") &&
                HasExpectedSprite(panel, "Title Group/Right Leaf", "progress_ui_leaf_right.png") &&
                HasExpectedSprite(panel, "Title Group/Title Plaque", "progress_ui_title_plaque.png") &&
                panel.Find("Title Group/Progress Title") != null &&
                HasExpectedSprite(panel, "Progress Track", "progress_ui_track.png") &&
                HasExpectedSprite(panel, "Progress Track/Progress Fill", "progress_ui_fill.png") &&
                HasExpectedSprite(panel, "Value Badge", "progress_ui_value_badge.png") &&
                panel.Find("Value Badge/Progress Value") != null;
        }

        private static bool HasExpectedSprite(
            RectTransform panel,
            string relativePath,
            string fileName)
        {
            Transform child = panel.Find(relativePath);
            if (child == null)
                return false;

            Image image = child.GetComponent<Image>();
            Sprite expected = AssetDatabase.LoadAssetAtPath<Sprite>(AssetFolder + "/" + fileName);

            return image != null && expected != null && image.sprite == expected;
        }

        private static bool EnsureChefBehindProgressPanel(Scene scene, bool saveIfChanged)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                return false;

            GameObject root = GameObject.Find("NEW Country Map");
            if (root == null)
                return false;

            Transform chef = root.transform.Find("Chef Guide Mascot");
            Transform progressPanel = root.transform.Find("Continent Progress Panel");
            if (chef == null || progressPanel == null)
                return false;

            if (chef.GetSiblingIndex() <= progressPanel.GetSiblingIndex())
                return false;

            // Do not reposition or resize anything. Only change UI sibling order.
            chef.SetSiblingIndex(progressPanel.GetSiblingIndex());

            EditorSceneManager.MarkSceneDirty(scene);
            if (saveIfChanged)
                EditorSceneManager.SaveScene(scene);

            return true;
        }

        private static void BuildRoute(RectTransform parent, Sprite nodeSprite, Sprite lockSprite)
        {
            Vector2[] p =
            {
                new Vector2(-250f, 260f), new Vector2(-110f, 205f), new Vector2(70f, 175f),
                new Vector2(215f, 150f), new Vector2(100f, 55f), new Vector2(-80f, 0f),
                new Vector2(-225f, -35f), new Vector2(-115f, -125f), new Vector2(50f, -145f),
                new Vector2(205f, -155f), new Vector2(190f, -265f), new Vector2(115f, -375f),
                new Vector2(35f, -445f)
            };

            for (int i = 0; i < p.Length - 1; i++)
                Dotted(parent, p[i], p[i + 1]);

            RouteIcon("Route Node 1", parent, nodeSprite, new Vector2(-110f, 205f), 58f);
            RouteIcon("Route Lock 1", parent, lockSprite, new Vector2(100f, 55f), 64f);
            RouteIcon("Route Lock 2", parent, lockSprite, new Vector2(-115f, -125f), 64f);
            RouteIcon("Route Node 2", parent, nodeSprite, new Vector2(50f, -145f), 58f);
            RouteIcon("Route Lock 3", parent, lockSprite, new Vector2(190f, -265f), 64f);
            RouteIcon("Route Node 3", parent, nodeSprite, new Vector2(35f, -445f), 58f);
        }

        private static void Dotted(RectTransform parent, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float length = d.magnitude;
            if (length < 1f)
                return;

            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            int count = Mathf.Max(1, Mathf.FloorToInt(length / 42f));

            for (int i = 0; i <= count; i++)
            {
                RectTransform dash = R("Route Dash", parent);
                Set(dash, new Vector2(0.5f, 0.5f), Vector2.Lerp(a, b, (float)i / count), new Vector2(24f, 9f));
                dash.localEulerAngles = new Vector3(0f, 0f, angle);
                Image img = dash.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.95f);
                img.raycastTarget = false;
            }
        }

        private static void RouteIcon(string name, RectTransform parent, Sprite sprite, Vector2 position, float size)
        {
            Image image = I(name, parent, sprite, new Vector2(0.5f, 0.5f), position, new Vector2(size, size), true);
            image.raycastTarget = false;
        }

        private static GameObject BuildSettings(
            RectTransform parent,
            Sprite frame,
            out Button close,
            out Button sound,
            out TextMeshProUGUI soundText,
            out Button vibration,
            out TextMeshProUGUI vibrationText)
        {
            RectTransform root = R("Settings Panel", parent);
            Stretch(root);

            Image dim = Solid("Dim", root, new Color(0.01f, 0.05f, 0.13f, 0.74f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            Image panel = Solid("Panel", root, new Color(1f, 0.95f, 0.82f, 1f));
            Set(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 660f));
            panel.raycastTarget = true;

            TextMeshProUGUI title = T("Title", panel.transform, "SETTINGS", 54f, new Vector2(0f, 235f), new Vector2(600f, 90f));
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.05f, 0.30f, 0.70f, 1f);

            sound = TextButton("SOUND Button", panel.transform, frame, "SOUND: ON", new Vector2(0f, 85f), out soundText);
            vibration = TextButton("VIBRATION Button", panel.transform, frame, "VIBRATION: ON", new Vector2(0f, -65f), out vibrationText);
            close = TextButton("CLOSE Button", panel.transform, frame, "CLOSE", new Vector2(0f, -225f), out _);
            AddButtonFX(sound.gameObject);
            AddButtonFX(vibration.gameObject);
            AddButtonFX(close.gameObject);

            return root.gameObject;
        }

        private static GameObject BuildUnsupported(RectTransform parent, Sprite frame, out TextMeshProUGUI text)
        {
            RectTransform root = R("Unsupported Continent Notice", parent);
            Set(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(830f, 430f));

            Image dim = Solid("Dim", root, new Color(0.01f, 0.05f, 0.13f, 0.82f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = false;

            Image banner = I("Notice Frame", root, frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740f, 250f), false);
            text = T("Notice Text", banner.transform, "COUNTRY ART PACK\nNOT INSTALLED YET", 34f, Vector2.zero, new Vector2(610f, 170f));
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.08f, 0.18f, 0.34f, 1f);
            return root.gameObject;
        }

        private static void AddButtonFX(GameObject target)
        {
            if (target == null)
                return;

            if (target.GetComponent<Watermelon.WorldMapButtonFX>() == null)
                target.AddComponent<Watermelon.WorldMapButtonFX>();
        }

        private static Button TextButton(string name, Transform parent, Sprite frame, string label, Vector2 pos, out TextMeshProUGUI text)
        {
            Button b = B(name, parent, frame, new Vector2(0.5f, 0.5f), pos, new Vector2(500f, 105f), false);
            text = T("Label", b.transform, label, 31f, Vector2.zero, new Vector2(420f, 70f));
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.08f, 0.18f, 0.34f, 1f);
            return b;
        }

        private static void CreateCamera()
        {
            GameObject go = new GameObject("Main Camera");
            Camera c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.05f, 0.46f, 0.82f, 1f);
            c.orthographic = true;
            c.transform.position = new Vector3(0f, 0f, -10f);
            go.tag = "MainCamera";
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.SetActive(false);
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            int layer = LayerMask.NameToLayer("UI");
            go.layer = layer >= 0 ? layer : 5;

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            return canvas;
        }

        private static RectTransform R(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            int layer = LayerMask.NameToLayer("UI");
            go.layer = layer >= 0 ? layer : 5;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image I(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool preserve)
        {
            RectTransform rect = R(name, parent);
            Set(rect, anchor, pos, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserve;
            image.raycastTarget = false;
            return image;
        }

        private static Image Solid(string name, Transform parent, Color color)
        {
            RectTransform rect = R(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button B(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool preserve)
        {
            Image image = I(name, parent, sprite, anchor, pos, size, preserve);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static TextMeshProUGUI T(string name, Transform parent, string value, float fontSize, Vector2 pos, Vector2 size)
        {
            RectTransform rect = R(name, parent);
            Set(rect, new Vector2(0.5f, 0.5f), pos, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            text.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static void Set(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Sprite ProgressSprite(string preferredFile, string fallbackFile)
        {
            // Progress widget must use only the dedicated progress_ui_* sprites.
            // Keeping the second parameter preserves existing call sites while
            // intentionally disabling the old fallback art.
            string preferredPath = AssetFolder + "/" + preferredFile;
            Sprite preferred = AssetDatabase.LoadAssetAtPath<Sprite>(preferredPath);

            if (preferred == null)
            {
                throw new InvalidOperationException(
                    "Missing required Country Map progress sprite: " + preferredFile +
                    "\nExpected at: " + preferredPath);
            }

            return preferred;
        }

        private static Sprite S(string file)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetFolder + "/" + file);
            if (sprite == null)
                throw new InvalidOperationException("Missing Country Map sprite: " + file);
            return sprite;
        }

        private static void ImportSprites()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { AssetFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool changed = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (importer.maxTextureSize < 4096)
                {
                    importer.maxTextureSize = 4096;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (changed)
                    importer.SaveAndReimport();
            }
        }

        private static List<string> MissingProgressWidgetAssets()
        {
            return ProgressWidgetAssets.Where(f => !File.Exists(AssetFolder + "/" + f)).ToList();
        }

        private static List<string> Missing()
        {
            return Required.Where(f => !File.Exists(AssetFolder + "/" + f)).ToList();
        }

        private static bool IsPlaceholder()
        {
            if (!File.Exists(ScenePath))
                return false;

            try
            {
                return File.ReadAllText(ScenePath).Contains("__COUNTRY_MAP_PLACEHOLDER__");
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Project Data/Game/Images");
            EnsureFolder(AssetFolder);
            EnsureFolder("Assets/Project Data/Game/Scenes");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => s.path == ScenePath);
            if (index >= 0)
                scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
            else
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void Focus(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            GameObject root = GameObject.Find("NEW Country Map");
            if (root == null)
                return;

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        private static string Safe(string value)
        {
            return value.Replace(" ", string.Empty).Replace("/", string.Empty);
        }
    }
}
#endif
