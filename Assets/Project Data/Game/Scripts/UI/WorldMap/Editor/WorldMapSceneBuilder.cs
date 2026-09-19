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
    /// ONE-TIME editor baker for WorldMap.unity.
    ///
    /// This deliberately follows the working menu.unity / loading.unity architecture:
    /// - the complete visible UI is serialized into the scene;
    /// - Canvas uses Scale With Screen Size at 1080x1920 / Match 0.5;
    /// - NEW World Map is a real editable scene hierarchy;
    /// - runtime scripts are behaviour-only and do not rebuild/rearrange the UI;
    /// - no InitializeOnLoad, no scene-open regeneration, no menu/loading scene mutation.
    ///
    /// After baking, edit WorldMap.unity directly in the Scene/Inspector like menu.unity.
    /// Do NOT rebake unless you intentionally want to replace the World Map layout.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMapSceneBuilder
    {
        private static bool repairQueued;

        static WorldMapSceneBuilder()
        {
            EditorSceneManager.sceneOpened -= OnWorldMapSceneOpened;
            EditorSceneManager.sceneOpened += OnWorldMapSceneOpened;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            QueueWorldMapArtworkRepair();
        }

        private static void OnWorldMapSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath)
                QueueWorldMapArtworkRepair();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            if (SceneManager.GetActiveScene().path == ScenePath)
                EditorApplication.delayCall += FocusCurrentWorldMapCanvasIn2D;
        }

        private static void QueueWorldMapArtworkRepair()
        {
            if (repairQueued)
                return;

            repairQueued = true;
            EditorApplication.delayCall += RepairOpenWorldMapArtworkAndView;
        }

        private static void RepairOpenWorldMapArtworkAndView()
        {
            repairQueued = false;

            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            EnsureFolders();
            AssetDatabase.Refresh();
            ImportWorldMapTexturesAsSprites();

            List<string> missing = GetMissingAssets();

            if (missing.Count > 0 && TryImportAssetPackFromKnownLocations())
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ImportWorldMapTexturesAsSprites();
                missing = GetMissingAssets();
            }

            if (missing.Count == 0)
            {
                int rebound = RebindArtworkInOpenWorldMap(saveScene: true);

                if (rebound > 0)
                {
                    Debug.Log(
                        "[WorldMap] Automatically rebound " + rebound +
                        " generated sprites into WorldMap.unity without changing any RectTransforms.");
                }
            }
            else
            {
                Debug.LogWarning(
                    "[WorldMap] Generated artwork is not in the Unity project yet. " +
                    "Download/select " + AssetPackFileName + " using Conveyor Chef > World Map > 1. Import Generated Art Pack. " +
                    "Until those PNGs exist, Unity can only show blank/white placeholder Images.");
            }

            FocusEditableWorldMapInSceneView(scene);
        }

        private const string ScenePath = "Assets/Project Data/Game/Scenes/WorldMap.unity";
        private const string AssetFolder = "Assets/Project Data/Game/Images/WorldMap";
        private const string AssetPackFileName = "ConveyorChef_WorldMap_Assets_ForUnity.zip";

        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;

        private static readonly string[] RequiredAssetFiles =
        {
            "tropical_ocean_map_adventure.png",
            "colorful_cartoon_north_america_map.png",
            "colourful_south_america_game_map.png",
            "vibrant_cartoon_europe_map.png",
            "whimsical_africa_adventure_map.png",
            "whimsical_isometric_asia_game_map.png",
            "australia_and_oceania_adventure_map.png",
            "conveyor_chef_world_map_logo.png",
            "glossy_blue_game_back_button.png",
            "glossy_blue_gear_settings_icon.png",
            "glossy_blue_back_arrow_button.png",
            "glossy_blue_right_arrow_button.png",
            "glossy_chef_map_pin_icon.png",
            "glossy_blue_map_pin_lock_icon.png",
            "golden_magical_energy_burst.png",
            "glossy_chef_s_game_ui_banner.png",
            "locked_culinary_chapter_card.png",
            "glossy_blue_game_ui_panel.png",
            "drag_to_explore_game_button.png",
            "ornate_golden_blue_compass_rose.png"
        };

        private static readonly string[] ContinentNames =
        {
            "North America",
            "South America",
            "Europe",
            "Africa",
            "Asia",
            "Australia / Oceania"
        };

        private static readonly string[] ContinentFiles =
        {
            "colorful_cartoon_north_america_map.png",
            "colourful_south_america_game_map.png",
            "vibrant_cartoon_europe_map.png",
            "whimsical_africa_adventure_map.png",
            "whimsical_isometric_asia_game_map.png",
            "australia_and_oceania_adventure_map.png"
        };

        // Authored positions inside MapContent. These are only the initial baked values.
        // After baking, the designer is free to move/resize every node directly in WorldMap.unity.
        private static readonly Vector2[] ContinentPositions =
        {
            new Vector2(-620f, 430f),
            new Vector2(-500f, -520f),
            new Vector2(10f, 520f),
            new Vector2(80f, -300f),
            new Vector2(650f, 360f),
            new Vector2(680f, -650f)
        };

        private static readonly Vector2[] ContinentSizes =
        {
            new Vector2(760f, 760f),
            new Vector2(650f, 820f),
            new Vector2(650f, 650f),
            new Vector2(650f, 760f),
            new Vector2(760f, 760f),
            new Vector2(650f, 650f)
        };

        [MenuItem("Conveyor Chef/World Map/1. Import Generated Art Pack", priority = 1)]
        public static void ImportGeneratedArtPack()
        {
            if (!ImportAssetPackInteractive())
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportWorldMapTexturesAsSprites();

            List<string> missing = GetMissingAssets();
            if (missing.Count == 0)
            {
                if (SceneManager.GetActiveScene().path == ScenePath)
                    RebindArtworkInOpenWorldMap(saveScene: false);

                EditorUtility.DisplayDialog(
                    "World Map Art",
                    "All 20 generated World Map sprites are now available in the project.\n\n" +
                    "If WorldMap.unity is open, the artwork has also been rebound without moving any RectTransforms.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "World Map Art",
                    "The selected ZIP is missing these required files:\n\n" +
                    string.Join("\n", missing),
                    "OK");
            }
        }

        [MenuItem("Conveyor Chef/World Map/2. Bake/Replace Editable World Map Scene", priority = 2)]
        public static void BakeEditableWorldMap()
        {
            if (!EnsureGeneratedAssetsAvailable())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (File.Exists(ScenePath))
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace WorldMap.unity?",
                    "This will replace the current WorldMap.unity layout with a fresh serialized editable layout.\n\n" +
                    "Use this only while setting up the World Map, or when you intentionally want to reset it.\n" +
                    "Your menu.unity and loading.unity are NOT touched.",
                    "Replace World Map",
                    "Cancel");

                if (!replace)
                    return;
            }

            BakeScene();
        }

        [MenuItem("Conveyor Chef/World Map/3. Rebind Artwork In Current World Map (Keeps Layout)", priority = 3)]
        public static void RebindArtworkCommand()
        {
            if (!EnsureGeneratedAssetsAvailable())
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int rebound = RebindArtworkInOpenWorldMap(saveScene: true);

            EditorUtility.DisplayDialog(
                "World Map Artwork",
                "Rebound " + rebound + " Image components to the generated World Map sprites.\n\n" +
                "No RectTransform position, size, anchor, pivot or scale was changed.",
                "OK");

            FocusEditableWorldMapInSceneView(scene);
        }

        [MenuItem("Conveyor Chef/World Map/4. Open Editable World Map", priority = 4)]
        public static void OpenEditableWorldMap()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog(
                    "World Map",
                    "WorldMap.unity does not exist yet. Import the art pack and run 'Bake/Replace Editable World Map Scene' first.",
                    "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureSceneInBuildSettings(ScenePath);

            GameObject newRoot = GameObject.Find("NEW World Map");
            GameObject oldRoot = GameObject.Find("WorldMapRoot");

            if (newRoot == null && oldRoot != null)
            {
                Debug.LogWarning(
                    "[WorldMap] This project copy is still using the OLD generated WorldMapRoot hierarchy. " +
                    "Use Conveyor Chef > World Map > 2. Bake/Replace Editable World Map Scene once to convert it " +
                    "to the same editable Canvas > NEW World Map architecture as menu/loading.");
            }

            FocusEditableWorldMapInSceneView(scene);
        }

        [MenuItem("Conveyor Chef/World Map/5. Validate Editable World Map", priority = 5)]
        public static void ValidateEditableWorldMap()
        {
            List<string> missing = GetMissingAssets();
            bool sceneExists = File.Exists(ScenePath);
            bool inBuild = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath);

            string hierarchyState = "NOT OPEN";
            int imageCount = 0;
            int spriteCount = 0;
            int missingSpriteCount = 0;

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.path == ScenePath)
            {
                GameObject root = GameObject.Find("NEW World Map");
                GameObject oldRoot = GameObject.Find("WorldMapRoot");

                hierarchyState = root != null
                    ? "OK - EDITABLE NEW WORLD MAP"
                    : (oldRoot != null
                        ? "OLD GENERATED WorldMapRoot - BAKE/REPLACE ONCE"
                        : "MISSING NEW World Map ROOT");

                if (root != null)
                {
                    Image[] images = root.GetComponentsInChildren<Image>(true);
                    imageCount = images.Length;
                    spriteCount = images.Count(i => i != null && i.sprite != null);
                    missingSpriteCount = images.Count(i => i != null && i.sprite == null);
                }
            }

            string message =
                "WorldMap.unity: " + (sceneExists ? "OK" : "MISSING") + "\n" +
                "Build Settings: " + (inBuild ? "OK" : "MISSING") + "\n" +
                "Serialized hierarchy: " + hierarchyState + "\n" +
                "Generated art: " + (RequiredAssetFiles.Length - missing.Count) + "/" + RequiredAssetFiles.Length + "\n" +
                "Scene Images: " + imageCount + "\n" +
                "Images with Sprite: " + spriteCount + "\n" +
                "Images without Sprite: " + missingSpriteCount;

            if (missing.Count > 0)
                message += "\n\nMissing generated assets:\n- " + string.Join("\n- ", missing);

            Debug.Log("[WorldMapValidator]\n" + message);
            EditorUtility.DisplayDialog("Conveyor Chef World Map", message, "OK");
        }

        [MenuItem("Conveyor Chef/World Map/6. Focus World Map Canvas in 2D", priority = 6)]
        public static void FocusCurrentWorldMapCanvasIn2D()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            FocusEditableWorldMapInSceneView(scene);
        }

        [MenuItem("Conveyor Chef/World Map/Open World Map Art Folder", priority = 20)]
        public static void OpenWorldMapArtFolder()
        {
            EnsureFolders();
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetFolder);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        private static void BakeScene()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportWorldMapTexturesAsSprites();

            Sprite ocean = RequireSprite("tropical_ocean_map_adventure.png");
            Sprite logo = RequireSprite("conveyor_chef_world_map_logo.png");
            Sprite back = RequireSprite("glossy_blue_game_back_button.png");
            Sprite settings = RequireSprite("glossy_blue_gear_settings_icon.png");
            Sprite left = RequireSprite("glossy_blue_back_arrow_button.png");
            Sprite right = RequireSprite("glossy_blue_right_arrow_button.png");
            Sprite unlockedPin = RequireSprite("glossy_chef_map_pin_icon.png");
            Sprite lockedPin = RequireSprite("glossy_blue_map_pin_lock_icon.png");
            Sprite glow = RequireSprite("golden_magical_energy_burst.png");
            Sprite activeCard = RequireSprite("glossy_chef_s_game_ui_banner.png");
            Sprite lockedCard = RequireSprite("locked_culinary_chapter_card.png");
            Sprite bottomPanel = RequireSprite("glossy_blue_game_ui_panel.png");
            Sprite dragHint = RequireSprite("drag_to_explore_game_button.png");
            Sprite compass = RequireSprite("ornate_golden_blue_compass_rose.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateSceneEventSystem();

            Canvas canvas = CreateCanvas();

            // EXACT SAME OWNERSHIP MODEL AS THE WORKING MENU/LOADING SCENES:
            // Canvas -> NEW World Map -> complete editable visual hierarchy.
            RectTransform worldRoot = CreateRect("NEW World Map", canvas.transform);
            Stretch(worldRoot);

            worldRoot.gameObject.AddComponent<WorldMapResponsiveLayout>();

            Image background = CreateImage(
                "Background Artwork",
                worldRoot,
                ocean,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(DesignWidth, DesignHeight),
                false);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            // HEADER ----------------------------------------------------------
            RectTransform header = CreateRect("Header", worldRoot);
            SetRect(
                header,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -150f),
                new Vector2(DesignWidth, 300f),
                new Vector2(0.5f, 0.5f));

            Button backButton = CreateImageButton(
                "BackButton",
                header,
                back,
                new Vector2(0f, 0.5f),
                new Vector2(90f, 15f),
                new Vector2(120f, 120f),
                true);

            Button settingsButton = CreateImageButton(
                "SettingsButton",
                header,
                settings,
                new Vector2(1f, 0.5f),
                new Vector2(-90f, 15f),
                new Vector2(120f, 120f),
                true);

            Image logoImage = CreateImage(
                "WorldMapLogo",
                header,
                logo,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 32f),
                new Vector2(650f, 220f),
                true);
            logoImage.raycastTarget = false;

            TextMeshProUGUI chapterText = CreateText(
                "SelectedChapterText",
                header,
                "CHAPTER 1  •  NORTH AMERICA",
                34f,
                TextAlignmentOptions.Center,
                new Vector2(0f, -84f),
                new Vector2(820f, 48f));
            chapterText.color = Color.white;

            TextMeshProUGUI statusText = CreateText(
                "StatusText",
                header,
                "UNLOCKED  •  15 LEVELS",
                22f,
                TextAlignmentOptions.Center,
                new Vector2(0f, -128f),
                new Vector2(860f, 38f));
            statusText.color = new Color(0.88f, 0.96f, 1f, 1f);

            // MAP -------------------------------------------------------------
            RectTransform viewportRoot = CreateRect("MapViewport", worldRoot);
            SetRect(
                viewportRoot,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(DesignWidth, 1320f),
                new Vector2(0.5f, 0.5f));

            Image viewportRaycast = viewportRoot.gameObject.AddComponent<Image>();
            viewportRaycast.color = new Color(1f, 1f, 1f, 0.001f);
            viewportRaycast.raycastTarget = true;

            viewportRoot.gameObject.AddComponent<RectMask2D>();

            ScrollRect scroll = viewportRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRoot;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.08f;
            scroll.scrollSensitivity = 24f;

            RectTransform mapContent = CreateRect("MapContent", viewportRoot);
            SetRect(
                mapContent,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(2600f, 2700f),
                new Vector2(0.5f, 0.5f));
            scroll.content = mapContent;

            GameObject controllerObject = new GameObject("World Map Controller");
            controllerObject.transform.SetParent(worldRoot, false);
            WorldMapSceneController controller = controllerObject.AddComponent<WorldMapSceneController>();

            WorldMapScrollEvents scrollEvents = viewportRoot.gameObject.AddComponent<WorldMapScrollEvents>();
            scrollEvents.EditorConfigure(controller);

            List<WorldMapContinentNode> nodes = new List<WorldMapContinentNode>();

            for (int i = 0; i < ContinentNames.Length; i++)
            {
                Sprite continentSprite = RequireSprite(ContinentFiles[i]);

                RectTransform nodeRoot = CreateRect("Continent_" + (i + 1) + "_" + SafeName(ContinentNames[i]), mapContent);
                SetRect(
                    nodeRoot,
                    new Vector2(0.5f, 0.5f),
                    ContinentPositions[i],
                    ContinentSizes[i],
                    new Vector2(0.5f, 0.5f));

                Image glowImage = CreateImage(
                    "CurrentGlow",
                    nodeRoot,
                    glow,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    ContinentSizes[i] * 0.95f,
                    true);
                glowImage.raycastTarget = false;
                glowImage.gameObject.SetActive(i == 0);

                Button mapButton = CreateImageButton(
                    "ContinentArtwork",
                    nodeRoot,
                    continentSprite,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    ContinentSizes[i] * 0.88f,
                    true);

                Image continentImage = mapButton.targetGraphic as Image;

                Image pin = CreateImage(
                    "ChapterPin",
                    nodeRoot,
                    i == 0 ? unlockedPin : lockedPin,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -ContinentSizes[i].y * 0.10f),
                    new Vector2(145f, 175f),
                    true);
                pin.raycastTarget = false;

                TextMeshProUGUI label = CreateText(
                    "ContinentName",
                    nodeRoot,
                    ContinentNames[i],
                    34f,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, -ContinentSizes[i].y * 0.33f),
                    new Vector2(540f, 70f));
                label.color = Color.white;

                WorldMapContinentNode node = nodeRoot.gameObject.AddComponent<WorldMapContinentNode>();
                node.EditorConfigure(
                    i,
                    ContinentNames[i],
                    continentImage,
                    mapButton,
                    pin,
                    glowImage,
                    label,
                    unlockedPin,
                    lockedPin,
                    activeCard,
                    lockedCard);

                nodes.Add(node);
            }

            // FIXED MAP CONTROLS ---------------------------------------------
            Button leftButton = CreateImageButton(
                "PreviousContinent",
                worldRoot,
                left,
                new Vector2(0f, 0.5f),
                new Vector2(72f, 35f),
                new Vector2(108f, 108f),
                true);

            Button rightButton = CreateImageButton(
                "NextContinent",
                worldRoot,
                right,
                new Vector2(1f, 0.5f),
                new Vector2(-72f, 35f),
                new Vector2(108f, 108f),
                true);

            Image compassImage = CreateImage(
                "Compass",
                worldRoot,
                compass,
                new Vector2(0f, 0f),
                new Vector2(120f, 325f),
                new Vector2(190f, 190f),
                true);
            compassImage.raycastTarget = false;

            Image dragHintImage = CreateImage(
                "DragHint",
                worldRoot,
                dragHint,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -355f),
                new Vector2(560f, 150f),
                true);
            dragHintImage.raycastTarget = false;

            CanvasGroup dragHintGroup = dragHintImage.gameObject.AddComponent<CanvasGroup>();
            dragHintGroup.blocksRaycasts = false;
            dragHintGroup.interactable = false;

            // BOTTOM CHAPTER SELECTOR ---------------------------------------
            RectTransform bottom = CreateRect("ChapterSelector", worldRoot);
            SetRect(
                bottom,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 150f),
                new Vector2(DesignWidth, 300f),
                new Vector2(0.5f, 0.5f));

            Image selectorPanel = CreateImage(
                "SelectorPanel",
                bottom,
                bottomPanel,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1050f, 260f),
                false);
            selectorPanel.raycastTarget = false;

            const float cardWidth = 164f;
            const float cardHeight = 123f;
            const float spacing = 8f;
            float totalWidth = ContinentNames.Length * cardWidth + (ContinentNames.Length - 1) * spacing;
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

            for (int i = 0; i < nodes.Count; i++)
            {
                Vector2 position = new Vector2(startX + i * (cardWidth + spacing), 0f);

                Button cardButton = CreateImageButton(
                    "ChapterCard_" + (i + 1),
                    bottom,
                    i == 0 ? activeCard : lockedCard,
                    new Vector2(0.5f, 0.5f),
                    position,
                    new Vector2(cardWidth, cardHeight),
                    true);

                Image frameImage = cardButton.targetGraphic as Image;

                TextMeshProUGUI cardLabel = CreateText(
                    "CardLabel",
                    cardButton.transform,
                    "CHAPTER " + (i + 1) + "\n" + ContinentNames[i].ToUpperInvariant(),
                    15f,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, -4f),
                    new Vector2(cardWidth - 20f, cardHeight - 18f));
                cardLabel.color = i == 0
                    ? new Color(0.09f, 0.16f, 0.32f, 1f)
                    : new Color(0.78f, 0.82f, 0.9f, 1f);

                nodes[i].EditorAssignCard(cardButton, frameImage, cardLabel);
            }

            // SETTINGS MODAL -------------------------------------------------
            GameObject settingsPanel = CreateSettingsPanel(
                worldRoot,
                bottomPanel,
                out Button closeSettings,
                out Button soundButton,
                out TextMeshProUGUI soundLabel,
                out Button vibrationButton,
                out TextMeshProUGUI vibrationLabel);
            settingsPanel.SetActive(false);

            controller.EditorConfigure(
                scroll,
                mapContent,
                nodes.ToArray(),
                leftButton,
                rightButton,
                backButton,
                settingsButton,
                chapterText,
                statusText,
                dragHintGroup,
                settingsPanel,
                closeSettings,
                soundButton,
                vibrationButton,
                soundLabel,
                vibrationLabel);

            AddButtonFX(worldRoot.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            FocusEditableWorldMapInSceneView(scene);

            Debug.Log(
                "[WorldMap] Baked a fully serialized editable WorldMap.unity. " +
                "From now on, edit Canvas > NEW World Map directly. " +
                "Runtime scripts do not rebuild or rearrange the authored UI.");

            EditorUtility.DisplayDialog(
                "World Map Ready",
                "WorldMap.unity is now a normal serialized editable scene, using the same authoring model as menu.unity and loading.unity.\n\n" +
                "Edit Canvas > NEW World Map directly and save the scene.\n\n" +
                "Do NOT run Bake/Replace again unless you intentionally want to reset this layout.",
                "OK");
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 0;

            // Identical authoring settings to the working menu/loading scenes.
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return canvas;
        }

        private static void CreateCamera()
        {
            // Camera exists for scene consistency, but UI renders Screen Space Overlay.
            // No scene AudioListener is added: AudioController owns the persistent listener.
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.22f, 0.44f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
        }

        private static void CreateSceneEventSystem()
        {
            // Same practical pattern as menu/loading: a scene-local EventSystem is serialized
            // so WorldMap can also be tested directly. It starts disabled; the behaviour
            // controller enables it only when no persistent EventSystem exists.
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem));

            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputModuleType != null)
                eventObject.AddComponent(inputModuleType);
            else
                eventObject.AddComponent<StandaloneInputModule>();

            eventObject.SetActive(false);
        }

        private static GameObject CreateSettingsPanel(
            RectTransform parent,
            Sprite panelSprite,
            out Button close,
            out Button sound,
            out TextMeshProUGUI soundText,
            out Button vibration,
            out TextMeshProUGUI vibrationText)
        {
            RectTransform overlay = CreateRect("SettingsPanel", parent);
            Stretch(overlay);

            Image blocker = overlay.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.01f, 0.03f, 0.08f, 0.72f);
            blocker.raycastTarget = true;

            RectTransform card = CreateRect("SettingsCard", overlay);
            SetRect(
                card,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 650f),
                new Vector2(0.5f, 0.5f));

            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = panelSprite;
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = false;
            cardImage.color = Color.white;

            TextMeshProUGUI title = CreateText(
                "Title",
                card,
                "SETTINGS",
                54f,
                TextAlignmentOptions.Center,
                new Vector2(0f, 220f),
                new Vector2(600f, 90f));
            title.color = Color.white;

            sound = CreateTextButton(card, "SoundButton", "SOUND: ON", new Vector2(0f, 70f), out soundText);
            vibration = CreateTextButton(card, "VibrationButton", "VIBRATION: ON", new Vector2(0f, -65f), out vibrationText);
            close = CreateTextButton(card, "CloseButton", "CLOSE", new Vector2(0f, -215f), out _);

            return overlay.gameObject;
        }

        private static Button CreateTextButton(
            Transform parent,
            string name,
            string text,
            Vector2 position,
            out TextMeshProUGUI label)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(500f, 105f),
                new Vector2(0.5f, 0.5f));

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.56f, 0.98f, 1f);
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            label = CreateText(
                "Label",
                rect,
                text,
                34f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                rect.sizeDelta);
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private static Button CreateImageButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            bool preserveAspect)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchor, position, size, new Vector2(0.5f, 0.5f));

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.97f);
            colors.pressedColor = new Color(0.84f, 0.91f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.55f, 0.65f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            return button;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            bool preserveAspect)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchor, position, size, new Vector2(0.5f, 0.5f));

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;

            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                position,
                size,
                new Vector2(0.5f, 0.5f));

            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;

            return tmp;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void AddButtonFX(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.GetComponent<WorldMapButtonFX>() == null)
                    button.gameObject.AddComponent<WorldMapButtonFX>();
            }
        }

        private static void FocusEditableWorldMapInSceneView(Scene scene)
        {
            if (!scene.IsValid())
                return;

            GameObject root = null;

            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                Transform found = FindDeep(candidate.transform, "NEW World Map");
                if (found == null)
                    found = FindDeep(candidate.transform, "WorldMapRoot");
                if (found == null)
                    found = FindDeep(candidate.transform, "Canvas");

                if (found != null)
                {
                    root = found.gameObject;
                    break;
                }
            }

            if (root == null)
                return;

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.FrameSelected();
                sceneView.Repaint();
            }
        }

        private static Transform FindDeep(Transform parent, string targetName)
        {
            if (parent == null)
                return null;

            if (string.Equals(parent.name, targetName, StringComparison.Ordinal))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindDeep(parent.GetChild(i), targetName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static string SafeName(string value)
        {
            return value.Replace(" ", string.Empty).Replace("/", string.Empty);
        }


        private static int RebindArtworkInOpenWorldMap(bool saveScene)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return 0;

            int changed = 0;

            // Support both the new editable hierarchy and the old generated hierarchy,
            // so importing artwork can immediately fix the white/blue placeholder look
            // without touching the designer's RectTransforms.
            changed += BindSpriteByNames(
                RequireSprite("tropical_ocean_map_adventure.png"),
                false,
                "Background Artwork",
                "World Map Backdrop",
                "ScrollableOcean");

            changed += BindSpriteByNames(
                RequireSprite("conveyor_chef_world_map_logo.png"),
                true,
                "WorldMapLogo");

            changed += BindSpriteByNames(
                RequireSprite("glossy_blue_game_back_button.png"),
                true,
                "BackButton");

            changed += BindSpriteByNames(
                RequireSprite("glossy_blue_gear_settings_icon.png"),
                true,
                "SettingsButton");

            changed += BindSpriteByNames(
                RequireSprite("glossy_blue_back_arrow_button.png"),
                true,
                "PreviousContinent");

            changed += BindSpriteByNames(
                RequireSprite("glossy_blue_right_arrow_button.png"),
                true,
                "NextContinent");

            changed += BindSpriteByNames(
                RequireSprite("ornate_golden_blue_compass_rose.png"),
                true,
                "Compass");

            changed += BindSpriteByNames(
                RequireSprite("drag_to_explore_game_button.png"),
                true,
                "DragHint");

            changed += BindSpriteByNames(
                RequireSprite("glossy_blue_game_ui_panel.png"),
                false,
                "SelectorPanel");

            Sprite unlockedPinSprite = RequireSprite("glossy_chef_map_pin_icon.png");
            Sprite lockedPinSprite = RequireSprite("glossy_blue_map_pin_lock_icon.png");
            Sprite glowSprite = RequireSprite("golden_magical_energy_burst.png");
            Sprite activeCardSprite = RequireSprite("glossy_chef_s_game_ui_banner.png");
            Sprite lockedCardSprite = RequireSprite("locked_culinary_chapter_card.png");

            for (int i = 0; i < ContinentNames.Length; i++)
            {
                Transform node = FindObjectByPrefixInScene("Continent_" + (i + 1));
                if (node == null)
                    continue;

                Sprite continentSprite = RequireSprite(ContinentFiles[i]);
                Image continentImage = FindImageUnder(node, "ContinentArtwork", "ContinentButton");
                if (continentImage != null)
                    changed += AssignSprite(continentImage, continentSprite, true);

                Image pin = FindImageUnder(node, "ChapterPin", "StatePin");
                if (pin != null)
                {
                    Sprite visiblePin = i == 0 ? unlockedPinSprite : lockedPinSprite;
                    changed += AssignSprite(pin, visiblePin, true);
                }

                Image glow = FindImageUnder(node, "CurrentGlow");
                if (glow != null)
                    changed += AssignSprite(glow, glowSprite, true);

                Transform card = FindObjectByExactNameInScene("ChapterCard_" + (i + 1));
                Image cardImage = card != null ? card.GetComponent<Image>() : null;
                Button cardButton = card != null ? card.GetComponent<Button>() : null;
                TextMeshProUGUI cardLabel =
                    card != null ? card.GetComponentInChildren<TextMeshProUGUI>(true) : null;

                if (cardImage != null)
                {
                    Sprite visibleCard = i == 0 ? activeCardSprite : lockedCardSprite;
                    changed += AssignSprite(cardImage, visibleCard, true);
                }

                WorldMapContinentNode continentNode = node.GetComponent<WorldMapContinentNode>();
                if (continentNode != null)
                {
                    Button mapButton =
                        continentImage != null ? continentImage.GetComponent<Button>() : null;
                    TextMeshProUGUI mapLabel =
                        node.GetComponentsInChildren<TextMeshProUGUI>(true)
                            .FirstOrDefault(label => label != null && label.gameObject.name == "ContinentName");

                    RepairContinentNodeSerializedReferences(
                        continentNode,
                        i,
                        ContinentNames[i],
                        continentImage,
                        mapButton,
                        pin,
                        glow,
                        mapLabel,
                        cardButton,
                        cardImage,
                        cardLabel,
                        unlockedPinSprite,
                        lockedPinSprite,
                        activeCardSprite,
                        lockedCardSprite);

                    changed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
                EditorSceneManager.SaveScene(scene);

            return changed;
        }

        private static int BindSpriteByNames(Sprite sprite, bool preserveAspect, params string[] names)
        {
            int changed = 0;

            foreach (string name in names)
            {
                Transform target = FindObjectByExactNameInScene(name);
                if (target == null)
                    continue;

                Image image = target.GetComponent<Image>();
                if (image != null)
                    changed += AssignSprite(image, sprite, preserveAspect);
            }

            return changed;
        }

        private static void RepairContinentNodeSerializedReferences(
            WorldMapContinentNode node,
            int index,
            string displayName,
            Image continentImage,
            Button mapButton,
            Image pinImage,
            Image glowImage,
            TextMeshProUGUI mapLabel,
            Button cardButton,
            Image cardFrameImage,
            TextMeshProUGUI cardLabel,
            Sprite unlockedPinSprite,
            Sprite lockedPinSprite,
            Sprite activeCardSprite,
            Sprite lockedCardSprite)
        {
            if (node == null)
                return;

            SerializedObject serialized = new SerializedObject(node);

            SetSerializedInt(serialized, "continentIndex", index);
            SetSerializedString(serialized, "continentName", displayName);

            SetSerializedObject(serialized, "continentImage", continentImage);
            SetSerializedObject(serialized, "mapButton", mapButton);
            SetSerializedObject(serialized, "pinImage", pinImage);
            SetSerializedObject(serialized, "glowImage", glowImage);
            SetSerializedObject(serialized, "mapLabel", mapLabel);

            SetSerializedObject(serialized, "cardButton", cardButton);
            SetSerializedObject(serialized, "cardFrameImage", cardFrameImage);
            SetSerializedObject(serialized, "cardLabel", cardLabel);

            SetSerializedObject(serialized, "unlockedPinSprite", unlockedPinSprite);
            SetSerializedObject(serialized, "lockedPinSprite", lockedPinSprite);
            SetSerializedObject(serialized, "activeCardSprite", activeCardSprite);
            SetSerializedObject(serialized, "lockedCardSprite", lockedCardSprite);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(node);
        }

        private static void SetSerializedObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetSerializedInt(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetSerializedString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static int AssignSprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null || sprite == null)
                return 0;

            bool changed =
                image.sprite != sprite ||
                image.color != Color.white ||
                image.preserveAspect != preserveAspect ||
                !image.enabled;

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
            image.enabled = true;

            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(image.gameObject);

            return changed ? 1 : 0;
        }

        private static Transform FindObjectByExactNameInScene(string objectName)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDeep(root.transform, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindObjectByPrefixInScene(string prefix)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform tr in all)
                {
                    if (tr != null && tr.name.StartsWith(prefix, StringComparison.Ordinal))
                        return tr;
                }
            }

            return null;
        }

        private static Image FindImageUnder(Transform parent, params string[] names)
        {
            if (parent == null)
                return null;

            Transform[] all = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform tr in all)
            {
                if (tr == null)
                    continue;

                for (int i = 0; i < names.Length; i++)
                {
                    if (!string.Equals(tr.name, names[i], StringComparison.Ordinal))
                        continue;

                    Image image = tr.GetComponent<Image>();
                    if (image != null)
                        return image;
                }
            }

            return null;
        }

        private static void EnsureSceneInBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            int index = scenes.FindIndex(s => s.path == path);
            if (index < 0)
            {
                // Keep WorldMap before Game when possible.
                int gameIndex = scenes.FindIndex(s => s.path.EndsWith("/Game.unity", StringComparison.OrdinalIgnoreCase));

                EditorBuildSettingsScene entry = new EditorBuildSettingsScene(path, true);
                if (gameIndex >= 0)
                    scenes.Insert(gameIndex, entry);
                else
                    scenes.Add(entry);
            }
            else if (!scenes[index].enabled)
            {
                scenes[index] = new EditorBuildSettingsScene(path, true);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static bool EnsureGeneratedAssetsAvailable()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            ImportWorldMapTexturesAsSprites();

            List<string> missing = GetMissingAssets();
            if (missing.Count == 0)
                return true;

            if (TryImportAssetPackFromKnownLocations())
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ImportWorldMapTexturesAsSprites();

                missing = GetMissingAssets();
                if (missing.Count == 0)
                    return true;
            }

            bool selectZip = EditorUtility.DisplayDialog(
                "World Map artwork is missing",
                "Unity cannot find " + missing.Count + " of the 20 generated World Map PNG assets.\n\n" +
                "Select " + AssetPackFileName + " and they will be imported into:\n" +
                AssetFolder + "\n\n" +
                "No placeholder rectangles will be baked.",
                "Select ZIP",
                "Cancel");

            if (!selectZip)
                return false;

            if (!ImportAssetPackInteractive())
                return false;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportWorldMapTexturesAsSprites();

            missing = GetMissingAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "World Map assets still missing",
                    "The selected ZIP does not contain every required PNG. Missing:\n\n" +
                    string.Join("\n", missing),
                    "OK");
                return false;
            }

            return true;
        }

        private static bool TryImportAssetPackFromKnownLocations()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            List<string> candidates = new List<string>();

            if (!string.IsNullOrEmpty(projectRoot))
            {
                candidates.Add(Path.Combine(projectRoot, AssetPackFileName));
                candidates.Add(Path.Combine(projectRoot, "Assets", AssetPackFileName));

                candidates.AddRange(
                    Directory.Exists(projectRoot)
                        ? Directory.GetFiles(projectRoot, "ConveyorChef_WorldMap_Assets_ForUnity*.zip", SearchOption.TopDirectoryOnly)
                        : Array.Empty<string>());
            }

            if (Directory.Exists(downloads))
            {
                candidates.Add(Path.Combine(downloads, AssetPackFileName));
                candidates.AddRange(
                    Directory.GetFiles(downloads, "ConveyorChef_WorldMap_Assets_ForUnity*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(File.GetLastWriteTimeUtc));
            }

            foreach (string candidate in candidates
                         .Where(path => !string.IsNullOrWhiteSpace(path))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(candidate))
                    continue;

                if (ExtractAssetPack(candidate))
                {
                    Debug.Log("[WorldMap] Imported generated artwork from: " + candidate);
                    return true;
                }
            }

            return false;
        }

        private static bool ImportAssetPackInteractive()
        {
            string selected = EditorUtility.OpenFilePanel(
                "Select Conveyor Chef World Map Asset Pack",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "zip");

            if (string.IsNullOrEmpty(selected))
                return false;

            return ExtractAssetPack(selected);
        }

        private static bool ExtractAssetPack(string zipPath)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
                return false;

            EnsureFolders();

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return false;

            string absoluteAssetFolder = Path.Combine(
                projectRoot,
                AssetFolder.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(absoluteAssetFolder);

            HashSet<string> required = new HashSet<string>(
                RequiredAssetFiles,
                StringComparer.OrdinalIgnoreCase);

            int extracted = 0;

            try
            {
                using (FileStream stream = File.OpenRead(zipPath))
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fileName = Path.GetFileName(entry.FullName);
                        if (string.IsNullOrWhiteSpace(fileName) || !required.Contains(fileName))
                            continue;

                        string destination = Path.Combine(absoluteAssetFolder, fileName);
                        entry.ExtractToFile(destination, true);
                        extracted++;
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ImportWorldMapTexturesAsSprites();

                Debug.Log(
                    "[WorldMap] Imported " + extracted +
                    " generated PNG assets into " + AssetFolder + ".");

                return extracted > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError("[WorldMap] Failed to import generated art pack: " + ex);
                EditorUtility.DisplayDialog(
                    "World Map import failed",
                    "Could not import the selected ZIP.\n\n" + ex.Message,
                    "OK");
                return false;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Project Data/Game/Images", "WorldMap");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void ImportWorldMapTexturesAsSprites()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { AssetFolder });
            foreach (string guid in guids)
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

        private static Sprite RequireSprite(string fileName)
        {
            Sprite sprite = FindSprite(fileName);
            if (sprite == null)
                throw new InvalidOperationException(
                    "Required World Map sprite is missing after validation: " + fileName);

            return sprite;
        }

        private static Sprite FindSprite(string fileName)
        {
            string directPath = AssetFolder + "/" + fileName;
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(directPath);
            if (direct != null)
                return direct;

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string[] guids = AssetDatabase.FindAssets(baseName + " t:Texture2D");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        private static List<string> GetMissingAssets()
        {
            return RequiredAssetFiles.Where(name => FindSprite(name) == null).ToList();
        }
    }
}
#endif
