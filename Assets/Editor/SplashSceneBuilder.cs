#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor script to build the Conveyor Chef: Food Rush Splash / Loading Scene.
/// Access via Tools > Build Splash Scene or Conveyor Chef > Build Splash Screen in the menu bar.
/// Replaces 'Assets/Project Data/Game/Scenes/loading.unity' with the polished splash screen.
/// </summary>
public static class SplashSceneBuilder
{
    private const string SPLASH_SCENE_PATH = "Assets/Project Data/Game/Scenes/loading.unity";
    private const string ART_FOLDER = "Assets/Art/Splash";
    private const string FONT_PATH = "Assets/Project Data/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50.asset";
    private const string FONT_120_PATH = "Assets/Project Data/Game/Fonts/FredokaOne/Fredoka One 120/FredokaOne 120.asset";

    [MenuItem("Tools/Build Splash Scene")]
    [MenuItem("Conveyor Chef/Build Splash Screen")]
    public static void BuildSplashScene()
    {
        BuildSplashSceneInternal(true);
    }

    public static void BuildSplashSceneSilent()
    {
        BuildSplashSceneInternal(false);
    }

    public static void BuildSplashSceneInternal(bool showDialog)
    {
        Debug.Log("[SplashSceneBuilder] Starting Splash Scene Build...");

        // 1. Configure sprite import settings
        ConfigureSpriteImports();

        // 2. Create clean new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 3. Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 1f); // Deep warm dark
        cam.tag = "MainCamera";

        // 4. Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 5. Load Sprites
        Sprite splashBgSprite = LoadSprite("splash_screen") ?? LoadSprite("splash_background");
        Sprite barBgSprite = LoadSprite("loading_bar_bg");
        Sprite barFillSprite = LoadSprite("loading_bar_fill");
        Sprite sparkleSprite = LoadSprite("loading_sparkle");

        // Load Game Font (FredokaOne)
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH) 
                               ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_120_PATH);

        // 6. Background Image (Full screen stretch)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        StretchToFill(bgRect);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = splashBgSprite;
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;
        bgImg.preserveAspect = false; // Stretch to full screen bleed

        // 7. Loading UI Container (Anchored at bottom center, y ~ 260px)
        GameObject loadingUIObj = new GameObject("LoadingUI");
        loadingUIObj.transform.SetParent(canvasObj.transform, false);
        RectTransform loadingUIRect = loadingUIObj.AddComponent<RectTransform>();
        loadingUIRect.anchorMin = new Vector2(0.5f, 0f);
        loadingUIRect.anchorMax = new Vector2(0.5f, 0f);
        loadingUIRect.pivot = new Vector2(0.5f, 0.5f);
        loadingUIRect.sizeDelta = new Vector2(850f, 180f);
        loadingUIRect.anchoredPosition = new Vector2(0f, 260f);

        // 7a. Tip / Status Text (Above progress bar)
        GameObject tipObj = new GameObject("TipText");
        tipObj.transform.SetParent(loadingUIObj.transform, false);
        RectTransform tipRect = tipObj.AddComponent<RectTransform>();
        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.pivot = new Vector2(0.5f, 0.5f);
        tipRect.sizeDelta = new Vector2(800f, 45f);
        tipRect.anchoredPosition = new Vector2(0f, 52f);

        TextMeshProUGUI tipTMP = tipObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tipTMP.font = fontAsset;
        tipTMP.text = "Prepping the kitchen...";
        tipTMP.fontSize = 28;
        tipTMP.alignment = TextAlignmentOptions.Center;
        tipTMP.color = new Color(1f, 0.95f, 0.85f, 0.95f);
        tipTMP.fontStyle = FontStyles.Normal;
        tipTMP.enableWordWrapping = false;

        // 7b. Progress Bar Container (Rounded capsule frame)
        GameObject barBgObj = new GameObject("ProgressBarContainer");
        barBgObj.transform.SetParent(loadingUIObj.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        barBgRect.pivot = new Vector2(0.5f, 0.5f);
        barBgRect.sizeDelta = new Vector2(740f, 44f);
        barBgRect.anchoredPosition = new Vector2(0f, 0f);

        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.sprite = barBgSprite;
        barBgImg.type = Image.Type.Sliced;
        barBgImg.color = Color.white;
        barBgImg.raycastTarget = false;

        // 7b-i. Fill Area (Slightly inset from border)
        GameObject fillAreaObj = new GameObject("FillArea");
        fillAreaObj.transform.SetParent(barBgObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        // 7b-i-1. Fill Image
        GameObject fillObj = new GameObject("ProgressBarFill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        StretchToFill(fillRect);

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = barFillSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;
        fillImg.color = Color.white;
        fillImg.raycastTarget = false;

        // 7b-i-2. Sparkle Star Icon (Travels with progress)
        GameObject sparkleObj = new GameObject("Sparkle");
        sparkleObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform sparkleRect = sparkleObj.AddComponent<RectTransform>();
        sparkleRect.anchorMin = new Vector2(0f, 0.5f);
        sparkleRect.anchorMax = new Vector2(0f, 0.5f);
        sparkleRect.pivot = new Vector2(0.5f, 0.5f);
        sparkleRect.sizeDelta = new Vector2(40f, 40f);
        sparkleRect.anchoredPosition = Vector2.zero;

        Image sparkleImg = sparkleObj.AddComponent<Image>();
        sparkleImg.sprite = sparkleSprite;
        sparkleImg.color = new Color(1f, 1f, 0.85f, 1f);
        sparkleImg.raycastTarget = false;

        // 7c. Progress Percent Text (Below progress bar)
        GameObject percentObj = new GameObject("ProgressPercentText");
        percentObj.transform.SetParent(loadingUIObj.transform, false);
        RectTransform percentRect = percentObj.AddComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(0.5f, 0.5f);
        percentRect.anchorMax = new Vector2(0.5f, 0.5f);
        percentRect.pivot = new Vector2(0.5f, 0.5f);
        percentRect.sizeDelta = new Vector2(800f, 45f);
        percentRect.anchoredPosition = new Vector2(0f, -50f);

        TextMeshProUGUI percentTMP = percentObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) percentTMP.font = fontAsset;
        percentTMP.text = "Loading... 0%";
        percentTMP.fontSize = 28;
        percentTMP.alignment = TextAlignmentOptions.Center;
        percentTMP.color = Color.white;
        percentTMP.fontStyle = FontStyles.Bold;
        percentTMP.enableWordWrapping = false;

        // 8. Version / Studio Footer Text (Bottom edge)
        GameObject versionObj = new GameObject("VersionText");
        versionObj.transform.SetParent(canvasObj.transform, false);
        RectTransform versionRect = versionObj.AddComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(0.5f, 0f);
        versionRect.anchorMax = new Vector2(0.5f, 0f);
        versionRect.pivot = new Vector2(0.5f, 0.5f);
        versionRect.sizeDelta = new Vector2(600f, 30f);
        versionRect.anchoredPosition = new Vector2(0f, 40f);

        TextMeshProUGUI versionTMP = versionObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) versionTMP.font = fontAsset;
        versionTMP.text = "v1.0.0 • CONVEYOR CHEF FOOD RUSH";
        versionTMP.fontSize = 20;
        versionTMP.alignment = TextAlignmentOptions.Center;
        versionTMP.color = new Color(1f, 1f, 1f, 0.5f);
        versionTMP.fontStyle = FontStyles.SmallCaps;

        // 9. Fade Overlay (Solid black, starts alpha = 1)
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform fadeRect = fadeObj.AddComponent<RectTransform>();
        StretchToFill(fadeRect);
        Image fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        fadeImage.raycastTarget = false;

        // 10. SplashController Component
        GameObject controllerObj = new GameObject("SplashController");
        SplashController controller = controllerObj.AddComponent<SplashController>();

        // Wire up serialized fields via SerializedObject
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("fadeOverlay").objectReferenceValue = fadeImage;
        so.FindProperty("backgroundTransform").objectReferenceValue = bgRect;
        so.FindProperty("progressBarFill").objectReferenceValue = fillImg;
        so.FindProperty("progressBarContainer").objectReferenceValue = fillAreaRect;
        so.FindProperty("sparkleIcon").objectReferenceValue = sparkleRect;
        so.FindProperty("progressPercentText").objectReferenceValue = percentTMP;
        so.FindProperty("tipText").objectReferenceValue = tipTMP;
        so.FindProperty("versionText").objectReferenceValue = versionTMP;
        so.FindProperty("nextSceneName").stringValue = "menu";
        so.FindProperty("fadeInDuration").floatValue = 0.5f;
        so.FindProperty("loadDuration").floatValue = 2.8f;
        so.FindProperty("holdDuration").floatValue = 0.4f;
        so.FindProperty("fadeOutDuration").floatValue = 0.5f;
        so.FindProperty("enableBreathing").boolValue = true;
        so.FindProperty("breathingScale").floatValue = 0.015f;
        so.FindProperty("breathingSpeed").floatValue = 1.0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 11. Save Scene
        EditorSceneManager.SaveScene(scene, SPLASH_SCENE_PATH);

        // 12. Ensure Build Settings
        EnsureSceneInBuildSettings(SPLASH_SCENE_PATH);

        AssetDatabase.Refresh();

        Debug.Log("[SplashSceneBuilder] Splash scene successfully built and saved to: " + SPLASH_SCENE_PATH);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Conveyor Chef Splash Screen Built!",
                "The splash/loading screen has been built with your artwork and polished UI at:\n\n" +
                SPLASH_SCENE_PATH + "\n\nPress Play in Unity to test the full sequence!",
                "Awesome!");
        }
    }

    private static Sprite LoadSprite(string baseName)
    {
        string[] extensions = { ".png", ".jpg" };
        foreach (string ext in extensions)
        {
            string path = ART_FOLDER + "/" + baseName + ext;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        Debug.LogWarning("[SplashSceneBuilder] Could not find sprite: " + baseName + " in " + ART_FOLDER);
        return null;
    }

    private static void StretchToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void ConfigureSpriteImports()
    {
        // Configure main background
        ConfigureSingleSprite("splash_screen.png", Vector4.zero);
        ConfigureSingleSprite("splash_screen.jpg", Vector4.zero);
        ConfigureSingleSprite("splash_background.jpg", Vector4.zero);

        // Configure 9-sliced loading bar sprites (border: Left, Bottom, Right, Top)
        ConfigureSingleSprite("loading_bar_bg.png", new Vector4(32, 24, 32, 24));
        ConfigureSingleSprite("loading_bar_fill.png", new Vector4(24, 20, 24, 20));
        ConfigureSingleSprite("loading_sparkle.png", Vector4.zero);
    }

    private static void ConfigureSingleSprite(string fileName, Vector4 border)
    {
        string path = ART_FOLDER + "/" + fileName;
        if (!File.Exists(path)) return;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool modified = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            modified = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            modified = true;
        }

        if (border != Vector4.zero && importer.spriteBorder != border)
        {
            importer.spriteBorder = border;
            modified = true;
        }

        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            modified = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            modified = true;
        }

        if (importer.maxTextureSize < 2048)
        {
            importer.maxTextureSize = 2048;
            modified = true;
        }

        if (modified)
        {
            importer.SaveAndReimport();
            Debug.Log("[SplashSceneBuilder] Configured and reimported sprite: " + path);
        }
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool found = false;
        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path == scenePath)
            {
                s.enabled = true;
                found = true;
                break;
            }
        }

        if (!found)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[SplashSceneBuilder] Added scene to Build Settings: " + scenePath);
        }
    }
}
#endif
