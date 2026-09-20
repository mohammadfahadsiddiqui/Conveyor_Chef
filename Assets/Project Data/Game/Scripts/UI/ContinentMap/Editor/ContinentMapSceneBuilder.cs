#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    /// One-time baker for the reusable sequential ContinentMap scene.
    /// Countries are visual route sections only; there is no country chooser.
    /// </summary>
    public static class ContinentMapSceneBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/ContinentMap.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/WorldMap";

        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;

        [MenuItem("Conveyor Chef/Continent Map/1. Bake North America Sequential Map", priority = 1)]
        public static void BakeNorthAmerica()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (File.Exists(ScenePath))
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace ContinentMap.unity?",
                    "This will replace the current ContinentMap.unity layout with a fresh editable sequential North America map.\n\n" +
                    "Use this only while setting up the scene or when you intentionally want to reset it.",
                    "Replace",
                    "Cancel");

                if (!replace)
                    return;
            }

            Sprite ocean = FindSprite("tropical_ocean_map_adventure.png");
            Sprite northAmerica = FindSprite("colorful_cartoon_north_america_map.png");
            Sprite back = FindSprite("glossy_blue_game_back_button.png");
            Sprite settings = FindSprite("glossy_blue_gear_settings_icon.png");
            Sprite unlockedPin = FindSprite("glossy_chef_map_pin_icon.png");
            Sprite lockedPin = FindSprite("glossy_blue_map_pin_lock_icon.png");
            Sprite glow = FindSprite("golden_magical_energy_burst.png");
            Sprite panel = FindSprite("glossy_blue_game_ui_panel.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateSceneEventSystem();

            Canvas canvas = CreateCanvas();

            RectTransform root = CreateRect("NEW Continent Map", canvas.transform);
            Stretch(root);
            root.gameObject.AddComponent<Watermelon.ContinentMapResponsiveLayout>();

            Image background = CreateImage(
                "Background Artwork",
                root,
                ocean,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(DesignWidth, DesignHeight),
                false);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            // HEADER
            RectTransform header = CreateRect("Header", root);
            SetRect(header, new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(1080f, 250f));

            Button backButton = CreateImageButton(
                "BackButton", header, back,
                new Vector2(0f, 0.5f), new Vector2(85f, 0f), new Vector2(120f, 120f));

            Button settingsButton = CreateImageButton(
                "SettingsButton", header, settings,
                new Vector2(1f, 0.5f), new Vector2(-85f, 0f), new Vector2(120f, 120f));

            TextMeshProUGUI title = CreateText(
                "ContinentTitle", header,
                "CHAPTER 1 • NORTH AMERICA",
                46f, TextAlignmentOptions.Center,
                new Vector2(0f, 34f), new Vector2(760f, 70f));
            title.color = Color.white;

            TextMeshProUGUI progress = CreateText(
                "ProgressText", header,
                "PROGRESS 0/15 • NEXT 1-1",
                26f, TextAlignmentOptions.Center,
                new Vector2(0f, -40f), new Vector2(800f, 50f));
            progress.color = new Color(0.88f, 0.96f, 1f, 1f);

            // MAP VIEWPORT
            RectTransform viewport = CreateRect("MapViewport", root);
            SetRect(
                viewport,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f),
                new Vector2(1080f, 1450f));

            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            viewport.gameObject.AddComponent<RectMask2D>();

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.08f;
            scroll.scrollSensitivity = 30f;

            RectTransform contentRoot = CreateRect("MapContent", viewport);
            contentRoot.anchorMin = new Vector2(0.5f, 0f);
            contentRoot.anchorMax = new Vector2(0.5f, 0f);
            contentRoot.pivot = new Vector2(0.5f, 0f);
            contentRoot.sizeDelta = new Vector2(1080f, 4300f);
            contentRoot.anchoredPosition = Vector2.zero;
            scroll.content = contentRoot;

            Image contentOcean = CreateImage(
                "ScrollableBackground",
                contentRoot,
                ocean,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                contentRoot.sizeDelta,
                false);
            Stretch(contentOcean.rectTransform);
            contentOcean.raycastTarget = false;

            Image continentArt = CreateImage(
                "NorthAmericaArtwork",
                contentRoot,
                northAmerica,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(1000f, 3650f),
                true);
            continentArt.raycastTarget = false;
            continentArt.color = new Color(1f, 1f, 1f, 0.92f);

            // Story/country zone labels. These are not selectable.
            CreateZoneLabel(contentRoot, "USA Zone", "USA • LEVELS 1-1 → 1-5", 650f);
            CreateZoneLabel(contentRoot, "Mexico Zone", "MEXICO • LEVELS 1-6 → 1-10", 1950f);
            CreateZoneLabel(contentRoot, "Canada Zone", "CANADA • LEVELS 1-11 → 1-15", 3250f);

            GameObject controllerObject = new GameObject("ContinentMapController");
            controllerObject.transform.SetParent(root, false);
            ContinentMapSceneController controller = controllerObject.AddComponent<ContinentMapSceneController>();

            List<ContinentMapLevelNode> nodes = new List<ContinentMapLevelNode>();

            // 15 authored sequential nodes, bottom -> top.
            for (int i = 0; i < 15; i++)
            {
                float y = 380f + i * 245f;
                float x = Mathf.Sin(i * 0.9f) * 250f;

                RectTransform nodeRoot = CreateRect("Level_" + (i + 1).ToString("00"), contentRoot);
                nodeRoot.anchorMin = nodeRoot.anchorMax = new Vector2(0.5f, 0f);
                nodeRoot.pivot = new Vector2(0.5f, 0.5f);
                nodeRoot.anchoredPosition = new Vector2(x, y);
                nodeRoot.sizeDelta = new Vector2(220f, 190f);

                Image nodeGlow = CreateImage(
                    "CurrentGlow",
                    nodeRoot,
                    glow,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(190f, 190f),
                    true);
                nodeGlow.raycastTarget = false;
                nodeGlow.gameObject.SetActive(i == 0);

                Button nodeButton = CreateImageButton(
                    "LevelButton",
                    nodeRoot,
                    i == 0 ? unlockedPin : lockedPin,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 10f),
                    new Vector2(130f, 150f));

                Image pinImage = nodeButton.targetGraphic as Image;

                TextMeshProUGUI levelLabel = CreateText(
                    "LevelLabel",
                    nodeRoot,
                    "1-" + (i + 1),
                    30f,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, -68f),
                    new Vector2(170f, 42f));
                levelLabel.color = Color.white;

                TextMeshProUGUI starsLabel = CreateText(
                    "StarsLabel",
                    nodeRoot,
                    i == 0 ? "PLAY" : "LOCKED",
                    20f,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, -106f),
                    new Vector2(190f, 36f));
                starsLabel.color = new Color(0.9f, 0.95f, 1f, 1f);

                GameObject completed = new GameObject("CompletedBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                completed.transform.SetParent(nodeRoot, false);
                RectTransform completedRect = completed.GetComponent<RectTransform>();
                SetRect(completedRect, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(42f, 42f));
                Image completedImage = completed.GetComponent<Image>();
                completedImage.color = new Color(0.2f, 1f, 0.35f, 1f);
                completed.SetActive(false);

                ContinentMapLevelNode node = nodeRoot.gameObject.AddComponent<ContinentMapLevelNode>();
                node.EditorConfigure(
                    nodeButton,
                    pinImage,
                    nodeGlow,
                    levelLabel,
                    starsLabel,
                    completed,
                    unlockedPin,
                    lockedPin);

                nodes.Add(node);
            }

            // Level start popup
            GameObject popup = CreatePopup(
                root,
                panel,
                out TextMeshProUGUI popupTitle,
                out TextMeshProUGUI popupBody,
                out TextMeshProUGUI popupStars,
                out Button startButton,
                out Button closePopup);
            popup.SetActive(false);

            // Settings modal
            GameObject settingsPanel = CreateSettingsPanel(
                root,
                panel,
                out Button closeSettings,
                out Button soundButton,
                out Button vibrationButton,
                out TextMeshProUGUI soundLabel,
                out TextMeshProUGUI vibrationLabel);
            settingsPanel.SetActive(false);

            controller.EditorConfigure(
                scroll,
                contentRoot,
                nodes.ToArray(),
                title,
                progress,
                backButton,
                settingsButton,
                popup,
                popupTitle,
                popupBody,
                popupStars,
                startButton,
                closePopup,
                settingsPanel,
                closeSettings,
                soundButton,
                vibrationButton,
                soundLabel,
                vibrationLabel);

            AddButtonFeedback(root.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog(
                "Continent Map Ready",
                "Created an editable sequential North America map with 15 levels.\n\n" +
                "USA: 1-1 to 1-5\nMexico: 1-6 to 1-10\nCanada: 1-11 to 1-15\n\n" +
                "Countries are route sections only; players cannot choose or skip countries.",
                "OK");
        }

        private static void CreateZoneLabel(Transform parent, string name, string text, float y)
        {
            RectTransform panel = CreateRect(name, parent);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, y);
            panel.sizeDelta = new Vector2(760f, 90f);

            Image image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.2f, 0.48f, 0.78f);
            image.raycastTarget = false;

            TextMeshProUGUI label = CreateText(
                "Label", panel, text,
                30f, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(720f, 70f));
            label.color = Color.white;
        }

        private static GameObject CreatePopup(
            RectTransform parent,
            Sprite panelSprite,
            out TextMeshProUGUI title,
            out TextMeshProUGUI body,
            out TextMeshProUGUI stars,
            out Button start,
            out Button close)
        {
            RectTransform overlay = CreateRect("LevelStartPopup", parent);
            Stretch(overlay);

            Image blocker = overlay.gameObject.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.65f);
            blocker.raycastTarget = true;

            RectTransform card = CreateRect("PopupCard", overlay);
            SetRect(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 760f));

            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = panelSprite;
            cardImage.color = panelSprite != null ? Color.white : new Color(0.02f, 0.2f, 0.5f, 1f);

            title = CreateText("Title", card, "LEVEL 1-1", 54f, TextAlignmentOptions.Center, new Vector2(0f, 225f), new Vector2(620f, 80f));
            title.color = Color.white;

            body = CreateText("Body", card, "USA\nBURGER", 34f, TextAlignmentOptions.Center, new Vector2(0f, 50f), new Vector2(620f, 260f));
            body.color = Color.white;
            body.textWrappingMode = TextWrappingModes.Normal;

            stars = CreateText("Stars", card, "NOT COMPLETED", 28f, TextAlignmentOptions.Center, new Vector2(0f, -110f), new Vector2(600f, 60f));
            stars.color = new Color(1f, 0.9f, 0.3f, 1f);

            start = CreateTextButton(card, "StartButton", "START", new Vector2(0f, -225f));
            close = CreateTextButton(card, "CloseButton", "CLOSE", new Vector2(0f, -315f));

            return overlay.gameObject;
        }

        private static GameObject CreateSettingsPanel(
            RectTransform parent,
            Sprite panelSprite,
            out Button close,
            out Button sound,
            out Button vibration,
            out TextMeshProUGUI soundLabel,
            out TextMeshProUGUI vibrationLabel)
        {
            RectTransform overlay = CreateRect("SettingsPanel", parent);
            Stretch(overlay);

            Image blocker = overlay.gameObject.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.68f);
            blocker.raycastTarget = true;

            RectTransform card = CreateRect("SettingsCard", overlay);
            SetRect(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 650f));

            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = panelSprite;
            cardImage.color = panelSprite != null ? Color.white : new Color(0.02f, 0.2f, 0.5f, 1f);

            TextMeshProUGUI title = CreateText("Title", card, "SETTINGS", 52f, TextAlignmentOptions.Center, new Vector2(0f, 210f), new Vector2(620f, 80f));
            title.color = Color.white;

            sound = CreateTextButton(card, "SoundButton", "SOUND: ON", new Vector2(0f, 70f));
            soundLabel = sound.GetComponentInChildren<TextMeshProUGUI>();

            vibration = CreateTextButton(card, "VibrationButton", "VIBRATION: ON", new Vector2(0f, -65f));
            vibrationLabel = vibration.GetComponentInChildren<TextMeshProUGUI>();

            close = CreateTextButton(card, "CloseButton", "CLOSE", new Vector2(0f, -220f));

            return overlay.gameObject;
        }

        private static Button CreateTextButton(Transform parent, string name, string text, Vector2 position)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, new Vector2(0.5f, 0.5f), position, new Vector2(460f, 88f));

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.55f, 1f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI label = CreateText("Label", rect, text, 32f, TextAlignmentOptions.Center, Vector2.zero, rect.sizeDelta);
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateCamera()
        {
            GameObject go = new GameObject("Main Camera", typeof(Camera));
            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.18f, 0.4f, 1f);
            go.tag = "MainCamera";
        }

        private static void CreateSceneEventSystem()
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputModuleType != null)
                go.AddComponent(inputModuleType);
            else
                go.AddComponent<StandaloneInputModule>();

            go.SetActive(false);
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
            image.color = sprite != null ? Color.white : new Color(0.02f, 0.55f, 1f, 1f);
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

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
            SetRect(rect, anchor, position, size);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.color = sprite != null ? Color.white : new Color(0.02f, 0.45f, 0.85f, 1f);

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

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
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
        }

        private static void AddButtonFeedback(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                if (button.GetComponent<Watermelon.WorldMapButtonFX>() == null)
                    button.gameObject.AddComponent<Watermelon.WorldMapButtonFX>();
            }
        }

        private static Sprite FindSprite(string fileName)
        {
            string direct = ArtFolder + "/" + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(direct);
            if (sprite != null)
                return sprite;

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

                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return null;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != scenePath)
                    continue;

                scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                found = true;
                break;
            }

            if (!found)
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
