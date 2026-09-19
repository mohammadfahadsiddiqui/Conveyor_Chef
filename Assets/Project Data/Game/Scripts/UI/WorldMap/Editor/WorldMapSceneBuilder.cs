#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    /// One-click builder for the Conveyor Chef continent World Map.
    ///
    /// Design rule:
    /// - The visual composition is authored once at 1080x1920.
    /// - Runtime responsiveness scales the complete WorldMapRoot uniformly.
    /// - Individual authored children are never rearranged in Play mode.
    ///
    /// This is the same strategy that fixed the Main Menu simulator mismatch.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMapSceneBuilder
    {
        static WorldMapSceneBuilder()
        {
            // Keep the committed placeholder from ever becoming a dead-end scene.
            // Once a real WorldMap has been generated, it is never auto-overwritten.
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            QueueWorldMapBootstrap();
        }

        private static void QueueWorldMapBootstrap()
        {
            EditorApplication.delayCall -= EnsureEditableWorldMapSceneExists;
            EditorApplication.delayCall += EnsureEditableWorldMapSceneExists;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath)
                QueueWorldMapBootstrap();
        }

        private const string ScenePath = "Assets/Project Data/Game/Scenes/WorldMap.unity";
        private const string MenuScenePath = "Assets/Project Data/Game/Scenes/menu.unity";
        private const string AssetFolder = "Assets/Project Data/Game/Images/WorldMap";

        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;

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

        private static readonly Vector2[] ContinentPositions =
        {
            new Vector2(-520f, 620f),
            new Vector2(-420f, -520f),
            new Vector2(210f, 650f),
            new Vector2(170f, -260f),
            new Vector2(660f, 290f),
            new Vector2(680f, -690f)
        };

        private static readonly Vector2[] ContinentSizes =
        {
            new Vector2(760f, 760f),
            new Vector2(620f, 860f),
            new Vector2(680f, 650f),
            new Vector2(620f, 790f),
            new Vector2(760f, 760f),
            new Vector2(650f, 650f)
        };

        private static void EnsureEditableWorldMapSceneExists()
        {
            if (EditorApplication.isCompiling)
            {
                QueueWorldMapBootstrap();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool sceneExists = File.Exists(ScenePath);
            bool isPlaceholder = IsPlaceholderSceneFile();

            if (sceneExists)
                EnsureSceneInBuildSettings(ScenePath);

            // A normal serialized WorldMap scene already exists, so never overwrite
            // the designer's Canvas/layout automatically.
            if (sceneExists && !isPlaceholder)
                return;

            Scene currentScene = SceneManager.GetActiveScene();
            bool placeholderIsCurrentlyOpen =
                currentScene.IsValid() &&
                currentScene.path == ScenePath &&
                IsPlaceholderSceneLoaded(currentScene);

            // Protect unrelated unsaved scenes. The WorldMap placeholder itself is safe
            // to replace even if Unity has marked it dirty during import/open.
            if (currentScene.IsValid() &&
                currentScene.isDirty &&
                !placeholderIsCurrentlyOpen)
            {
                Debug.Log(
                    "[WorldMapBuilder] Waiting to generate WorldMap because the currently open scene has unsaved changes. " +
                    "Save that scene, then open WorldMap.unity or use Conveyor Chef > World Map > Create/Open Editable World Map.");
                return;
            }

            string previousScenePath = currentScene.IsValid() ? currentScene.path : string.Empty;

            RebuildWorldMap();

            if (!string.IsNullOrEmpty(previousScenePath) &&
                previousScenePath != ScenePath &&
                File.Exists(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static bool IsPlaceholderSceneFile()
        {
            if (!File.Exists(ScenePath))
                return false;

            try
            {
                return File.ReadAllText(ScenePath).Contains("__WORLD_MAP_PLACEHOLDER__");
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPlaceholderSceneLoaded(Scene scene)
        {
            if (!scene.IsValid())
                return false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == "__WORLD_MAP_PLACEHOLDER__")
                    return true;
            }

            return false;
        }

        [MenuItem("Conveyor Chef/World Map/Create/Open Editable World Map", priority = 1)]
        public static void CreateOrOpenEditableWorldMap()
        {
            // File.Exists alone is not enough because the repository intentionally ships
            // a tiny placeholder so Build Settings can reference WorldMap immediately.
            if (!File.Exists(ScenePath) || IsPlaceholderSceneFile())
            {
                RebuildWorldMap();
            }
            else
            {
                EnsureSceneInBuildSettings(ScenePath);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject root = GameObject.Find("WorldMapRoot");
            if (root != null)
            {
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
            }
        }

        [MenuItem("Conveyor Chef/World Map/Rebuild Responsive World Map")]
        public static void RebuildWorldMap()
        {
            EnsureFolders();
            ImportWorldMapTexturesAsSprites();

            Sprite ocean = FindSprite("tropical_ocean_map_adventure.png");
            Sprite logo = FindSprite("conveyor_chef_world_map_logo.png");
            Sprite back = FindSprite("glossy_blue_game_back_button.png");
            Sprite settings = FindSprite("glossy_blue_gear_settings_icon.png");
            Sprite left = FindSprite("glossy_blue_back_arrow_button.png");
            Sprite right = FindSprite("glossy_blue_right_arrow_button.png");
            Sprite unlockedPin = FindSprite("glossy_chef_map_pin_icon.png");
            Sprite lockedPin = FindSprite("glossy_blue_map_pin_lock_icon.png");
            Sprite glow = FindSprite("golden_magical_energy_burst.png");
            Sprite activeCard = FindSprite("glossy_chef_s_game_ui_banner.png");
            Sprite lockedCard = FindSprite("locked_culinary_chapter_card.png");
            Sprite bottomPanel = FindSprite("glossy_blue_game_ui_panel.png");
            Sprite dragHint = FindSprite("drag_to_explore_game_button.png");
            Sprite compass = FindSprite("ornate_golden_blue_compass_rose.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();

            Canvas canvas = CreateCanvas();

            Image fullBackdrop = CreateImage(
                "World Map Backdrop",
                canvas.transform,
                ocean,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(DesignWidth, DesignHeight));
            Stretch(fullBackdrop.rectTransform);
            fullBackdrop.preserveAspect = false;
            fullBackdrop.raycastTarget = false;
            if (ocean == null)
                fullBackdrop.color = new Color(0.02f, 0.48f, 0.86f, 1f);

            RectTransform worldRoot = CreateRect("WorldMapRoot", canvas.transform);
            worldRoot.anchorMin = worldRoot.anchorMax = worldRoot.pivot = new Vector2(0.5f, 0.5f);
            worldRoot.sizeDelta = new Vector2(DesignWidth, DesignHeight);
            worldRoot.anchoredPosition = Vector2.zero;

            WorldMapResponsiveLayout responsive = worldRoot.gameObject.AddComponent<WorldMapResponsiveLayout>();
            responsive.EditorConfigure(fullBackdrop);

            // HEADER
            RectTransform header = CreateRect("Header", worldRoot);
            SetRect(header, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1080f, 300f));

            Button backButton = CreateImageButton(
                "BackButton", header, back, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(120f, 120f));

            Button settingsButton = CreateImageButton(
                "SettingsButton", header, settings, new Vector2(1f, 0.5f), new Vector2(-80f, 0f), new Vector2(120f, 120f));

            Image logoImage = CreateImage(
                "WorldMapLogo", header, logo, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(720f, 210f));
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            TextMeshProUGUI chapterText = CreateText(
                "SelectedChapterText", header, "CHAPTER 1  •  NORTH AMERICA",
                34, TextAlignmentOptions.Center, new Vector2(0f, -92f), new Vector2(800f, 54f));
            chapterText.color = Color.white;

            TextMeshProUGUI statusText = CreateText(
                "StatusText", header, "UNLOCKED  •  15 LEVELS",
                22, TextAlignmentOptions.Center, new Vector2(0f, -132f), new Vector2(820f, 40f));
            statusText.color = new Color(0.86f, 0.95f, 1f, 1f);

            // MAP VIEWPORT
            RectTransform viewportRoot = CreateRect("MapViewport", worldRoot);
            viewportRoot.anchorMin = viewportRoot.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRoot.pivot = new Vector2(0.5f, 0.5f);
            viewportRoot.anchoredPosition = new Vector2(0f, 20f);
            viewportRoot.sizeDelta = new Vector2(1080f, 1340f);

            Image viewportGraphic = viewportRoot.gameObject.AddComponent<Image>();
            viewportGraphic.color = Color.white;
            viewportGraphic.raycastTarget = true;
            RectMask2D mask = viewportRoot.gameObject.AddComponent<RectMask2D>();

            ScrollRect scroll = viewportRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRoot;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.08f;
            scroll.scrollSensitivity = 20f;

            RectTransform mapContent = CreateRect("MapContent", viewportRoot);
            mapContent.anchorMin = mapContent.anchorMax = mapContent.pivot = new Vector2(0.5f, 0.5f);
            mapContent.anchoredPosition = Vector2.zero;
            mapContent.sizeDelta = new Vector2(2300f, 2600f);
            scroll.content = mapContent;

            Image oceanMap = CreateImage(
                "ScrollableOcean", mapContent, ocean,
                new Vector2(0.5f, 0.5f), Vector2.zero, mapContent.sizeDelta);
            oceanMap.preserveAspect = false;
            oceanMap.raycastTarget = false;
            if (ocean == null)
                oceanMap.color = new Color(0.02f, 0.5f, 0.9f, 1f);
            oceanMap.rectTransform.SetAsFirstSibling();

            // Controller exists before nodes so we can wire scroll events.
            GameObject controllerObject = new GameObject("WorldMapController");
            controllerObject.transform.SetParent(worldRoot, false);
            WorldMapSceneController controller = controllerObject.AddComponent<WorldMapSceneController>();

            WorldMapScrollEvents scrollEvents = viewportRoot.gameObject.AddComponent<WorldMapScrollEvents>();
            scrollEvents.EditorConfigure(controller);

            // CONTINENTS
            List<WorldMapContinentNode> nodes = new List<WorldMapContinentNode>();

            for (int i = 0; i < ContinentNames.Length; i++)
            {
                Sprite continentSprite = FindSprite(ContinentFiles[i]);

                RectTransform nodeRoot = CreateRect("Continent_" + (i + 1), mapContent);
                nodeRoot.anchorMin = nodeRoot.anchorMax = nodeRoot.pivot = new Vector2(0.5f, 0.5f);
                nodeRoot.anchoredPosition = ContinentPositions[i];
                nodeRoot.sizeDelta = ContinentSizes[i];

                Image glowImage = CreateImage(
                    "CurrentGlow", nodeRoot, glow,
                    new Vector2(0.5f, 0.5f), Vector2.zero,
                    ContinentSizes[i] * 0.95f);
                glowImage.preserveAspect = true;
                glowImage.raycastTarget = false;
                glowImage.gameObject.SetActive(i == 0);

                Button mapButton = CreateImageButton(
                    "ContinentButton", nodeRoot, continentSprite,
                    new Vector2(0.5f, 0.5f), Vector2.zero, ContinentSizes[i] * 0.86f);

                Image continentImage = mapButton.targetGraphic as Image;
                if (continentImage != null)
                {
                    continentImage.preserveAspect = true;
                    if (continentSprite == null)
                        continentImage.color = new Color(0.25f, 0.75f, 0.35f, 0.85f);
                }

                Image pin = CreateImage(
                    "StatePin", nodeRoot,
                    i == 0 ? unlockedPin : lockedPin,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -ContinentSizes[i].y * 0.12f),
                    new Vector2(145f, 175f));
                pin.preserveAspect = true;
                pin.raycastTarget = false;

                TextMeshProUGUI label = CreateText(
                    "ContinentName", nodeRoot, ContinentNames[i],
                    34, TextAlignmentOptions.Center,
                    new Vector2(0f, -ContinentSizes[i].y * 0.31f),
                    new Vector2(500f, 70f));
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

            // SIDE NAVIGATION BUTTONS stay fixed while the map scrolls.
            Button leftButton = CreateImageButton(
                "PreviousContinent", worldRoot, left,
                new Vector2(0f, 0.5f), new Vector2(70f, 80f), new Vector2(105f, 105f));

            Button rightButton = CreateImageButton(
                "NextContinent", worldRoot, right,
                new Vector2(1f, 0.5f), new Vector2(-70f, 80f), new Vector2(105f, 105f));

            // Compass decoration.
            Image compassImage = CreateImage(
                "Compass", worldRoot, compass,
                new Vector2(0f, 0f), new Vector2(120f, 330f), new Vector2(190f, 190f));
            compassImage.preserveAspect = true;
            compassImage.raycastTarget = false;

            // Drag hint (first-run helper only).
            Image dragHintImage = CreateImage(
                "DragHint", worldRoot, dragHint,
                new Vector2(0.5f, 1f), new Vector2(0f, -325f), new Vector2(600f, 140f));
            dragHintImage.preserveAspect = true;
            dragHintImage.raycastTarget = false;
            CanvasGroup dragHintGroup = dragHintImage.gameObject.AddComponent<CanvasGroup>();
            dragHintGroup.blocksRaycasts = false;
            dragHintGroup.interactable = false;

            // BOTTOM CHAPTER SELECTOR
            RectTransform bottom = CreateRect("ChapterSelector", worldRoot);
            bottom.anchorMin = bottom.anchorMax = new Vector2(0.5f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = new Vector2(1080f, 300f);

            Image selectorPanel = CreateImage(
                "SelectorPanel", bottom, bottomPanel,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 290f));
            selectorPanel.preserveAspect = false;
            selectorPanel.raycastTarget = false;
            if (bottomPanel == null)
                selectorPanel.color = new Color(0.02f, 0.18f, 0.42f, 0.96f);

            float cardWidth = 164f;
            float spacing = 6f;
            float totalWidth = ContinentNames.Length * cardWidth + (ContinentNames.Length - 1) * spacing;
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

            for (int i = 0; i < nodes.Count; i++)
            {
                Vector2 cardPos = new Vector2(startX + i * (cardWidth + spacing), 0f);
                Sprite frameSprite = i == 0 ? activeCard : lockedCard;

                Button cardButton = CreateImageButton(
                    "ChapterCard_" + (i + 1),
                    bottom,
                    frameSprite,
                    new Vector2(0.5f, 0.5f),
                    cardPos,
                    new Vector2(cardWidth, 235f));

                Image frameImage = cardButton.targetGraphic as Image;
                if (frameImage != null && frameSprite == null)
                    frameImage.color = i == 0
                        ? new Color(1f, 0.84f, 0.24f, 1f)
                        : new Color(0.18f, 0.25f, 0.4f, 1f);

                TextMeshProUGUI cardLabel = CreateText(
                    "CardLabel",
                    cardButton.transform,
                    "CHAPTER " + (i + 1) + "\n" + ContinentNames[i].ToUpperInvariant(),
                    20,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, -45f),
                    new Vector2(cardWidth - 16f, 105f));

                nodes[i].EditorAssignCard(cardButton, frameImage, cardLabel);
            }

            // SETTINGS MODAL
            GameObject settingsPanel = CreateSettingsPanel(
                worldRoot,
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EnsureSceneInBuildSettings(ScenePath);

            // The menu route is serialized directly in menu.unity. Never open another
            // scene additively from the builder; that can temporarily create duplicate
            // EventSystems/AudioListeners and pollute the Console.

            // WorldMapRoot is still valid because no other scene is opened here.
            // but still guard the editor selection so scene reloads/imports can never
            // leave us holding a destroyed RectTransform reference.
            if (worldRoot != null)
            {
                Selection.activeGameObject = worldRoot.gameObject;
                EditorGUIUtility.PingObject(worldRoot.gameObject);
            }
            else
            {
                GameObject rebuiltRoot = GameObject.Find("WorldMapRoot");
                if (rebuiltRoot != null)
                {
                    Selection.activeGameObject = rebuiltRoot;
                    EditorGUIUtility.PingObject(rebuiltRoot);
                }
            }

            List<string> missing = GetMissingAssets();
            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    "[WorldMapBuilder] Scene created and fully functional, but these generated art files were not found yet:\n- " +
                    string.Join("\n- ", missing) +
                    "\n\nPlace them in " + AssetFolder + " and run Rebuild Responsive World Map again.");
            }
            else
            {
                Debug.Log("[WorldMapBuilder] Responsive WorldMap scene created successfully with all generated artwork.");
            }
        }

        [MenuItem("Conveyor Chef/World Map/Validate World Map")]
        public static void ValidateWorldMap()
        {
            List<string> missing = GetMissingAssets();

            bool sceneExists = File.Exists(ScenePath);
            bool isPlaceholder = IsPlaceholderSceneFile();
            bool inBuild = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath);

            string sceneState = !sceneExists
                ? "MISSING"
                : (isPlaceholder ? "PLACEHOLDER - REBUILD REQUIRED" : "OK");

            string message =
                "WorldMap scene: " + sceneState + "\n" +
                "Build Settings: " + (inBuild ? "OK" : "MISSING") + "\n" +
                "Generated art: " + (missing.Count == 0 ? "ALL 20 ASSETS FOUND" : (20 - missing.Count) + "/20 FOUND");

            if (missing.Count > 0)
                message += "\n\nMissing:\n- " + string.Join("\n- ", missing);

            Debug.Log("[WorldMapValidator]\n" + message);
            EditorUtility.DisplayDialog("Conveyor Chef World Map", message, "OK");
        }

        [MenuItem("Conveyor Chef/World Map/Open World Map Asset Folder")]
        public static void SelectAssetFolder()
        {
            EnsureFolders();
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetFolder);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
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

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            // Serialized editor fallback. WorldMapResponsiveLayout switches this
            // to ConstantPixelSize before the first rendered gameplay frame.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return canvas;
        }

        private static void CreateCamera()
        {
            // AudioController owns one persistent AudioListener for the entire app.
            // Do not serialize another listener into WorldMap.
            GameObject go = new GameObject("Main Camera", typeof(Camera));
            Camera camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.22f, 0.44f, 1f);
            camera.orthographic = true;
            go.tag = "MainCamera";
        }

        private static void CreateEventSystem()
        {
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem));

            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputModuleType != null)
                eventObject.AddComponent(inputModuleType);
            else
                eventObject.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateSettingsPanel(
            RectTransform parent,
            out Button close,
            out Button sound,
            out TextMeshProUGUI soundText,
            out Button vibration,
            out TextMeshProUGUI vibrationText)
        {
            RectTransform overlay = CreateRect("SettingsPanel", parent);
            Stretch(overlay);

            Image blocker = overlay.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.01f, 0.03f, 0.08f, 0.75f);
            blocker.raycastTarget = true;

            RectTransform card = CreateRect("SettingsCard", overlay);
            SetRect(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 650f));

            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.04f, 0.24f, 0.55f, 0.98f);

            TextMeshProUGUI title = CreateText(
                "Title", card, "SETTINGS", 54,
                TextAlignmentOptions.Center,
                new Vector2(0f, 225f), new Vector2(600f, 90f));
            title.color = Color.white;

            sound = CreateTextButton(card, "SoundButton", "SOUND: ON", new Vector2(0f, 75f), out soundText);
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
            SetRect(rect, new Vector2(0.5f, 0.5f), position, new Vector2(500f, 105f));

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.56f, 0.98f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            label = CreateText(
                "Label", rect, text, 34,
                TextAlignmentOptions.Center, Vector2.zero, rect.sizeDelta);
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
            Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchor, position, size);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            if (sprite == null)
                image.color = new Color(0.02f, 0.55f, 1f, 0.92f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.82f, 0.9f, 1f, 1f);
            colors.disabledColor = new Color(0.45f, 0.5f, 0.6f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            return button;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchor, position, size);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;

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
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);

            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

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
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
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

        private static void EnsureSceneInBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            if (!scenes.Any(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            else
            {
                bool changed = false;
                for (int i = 0; i < scenes.Count; i++)
                {
                    if (scenes[i].path == path && !scenes[i].enabled)
                    {
                        scenes[i] = new EditorBuildSettingsScene(path, true);
                        changed = true;
                    }
                }

                if (changed)
                    EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void UpdateLegacyMenuRoute()
        {
            if (!File.Exists(MenuScenePath))
                return;

            Scene activeBefore = SceneManager.GetActiveScene();
            Scene menuScene = default;
            bool openedTemporarily = false;

            try
            {
                // Never open menu.unity in Single mode from the WorldMap builder.
                // Doing that destroys the freshly-created Canvas/RectTransforms and
                // caused MissingReferenceException at the end of RebuildWorldMap.
                if (activeBefore.IsValid() && activeBefore.path == MenuScenePath)
                {
                    menuScene = activeBefore;
                }
                else
                {
                    menuScene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
                    openedTemporarily = true;
                }

                bool changed = false;

                foreach (GameObject root in menuScene.GetRootGameObjects())
                {
                    Watermelon.BusStop.sceneloading[] routers =
                        root.GetComponentsInChildren<Watermelon.BusStop.sceneloading>(true);

                    foreach (Watermelon.BusStop.sceneloading router in routers)
                    {
                        if (router != null && router.gameSceneName == "LevelSelection")
                        {
                            router.gameSceneName = "WorldMap";
                            EditorUtility.SetDirty(router);
                            changed = true;
                        }
                    }
                }

                if (changed)
                    EditorSceneManager.SaveScene(menuScene);
            }
            finally
            {
                if (openedTemporarily && menuScene.IsValid())
                    EditorSceneManager.CloseScene(menuScene, true);

                if (activeBefore.IsValid() && activeBefore.isLoaded)
                    SceneManager.SetActiveScene(activeBefore);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Project Data/Game/Images", "WorldMap");
            EnsureFolder("Assets/Project Data/Game/Scripts/UI/WorldMap", "Editor");
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

                if (changed)
                    importer.SaveAndReimport();
            }
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
            string[] all =
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

            return all.Where(name => FindSprite(name) == null).ToList();
        }
    }
}
#endif
