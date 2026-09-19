#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Restores the originally approved Professional Main Menu artwork from the
    /// committed source atlas/background and binds it to the real editable UI
    /// objects serialized in menu.unity.
    ///
    /// This DOES NOT generate the menu hierarchy. It only imports/binds art.
    /// </summary>
    public static class ProfessionalMainMenuArtRestorer
    {
        private const string MenuScenePath = "Assets/Project Data/Game/Scenes/menu.unity";
        private const string AtlasPath = "Assets/Project Data/Game/Images/ProfessionalMainMenuSource/approved_menu_atlas.png";
        private const string BackgroundPath = "Assets/Project Data/Game/Images/ProfessionalMainMenuSource/approved_menu_background.png";
        private const string SessionKey = "ConveyorChef.ProfessionalMenu.ArtRestore.v1";

        private readonly struct Region
        {
            public readonly string Name;
            public readonly int X;
            public readonly int YTop;
            public readonly int Width;
            public readonly int Height;

            public Region(string name, int x, int yTop, int width, int height)
            {
                Name = name;
                X = x;
                YTop = yTop;
                Width = width;
                Height = height;
            }
        }

        private static readonly Region[] Regions =
        {
            new Region("logo",          6,    6, 390, 293),
            new Region("chef",        402,    6, 337, 450),
            new Region("avatar",      745,    6, 128, 128),
            new Region("play",          6,  462, 390, 131),
            new Region("story",       402,  462, 390, 131),
            new Region("challenges",    6,  599, 390, 131),
            new Region("customize",   402,  599, 390, 131),
            new Region("settings",      6,  736, 390, 131),
            new Region("coin_bar",    402,  736, 338, 113),
            new Region("diamond_bar",   6,  873, 338, 113),
            new Region("star",        350,  873,  96,  96),
            new Region("shop",        452,  873, 165, 165),
            new Region("collection",  623,  873, 165, 172),
            new Region("achievements",794,  873, 165, 165),
            new Region("leaderboard",   6, 1051, 165, 165),
        };

        [InitializeOnLoadMethod]
        private static void RestoreOnceAfterCompile()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += TryRestoreAutomatically;
        }

        private static void TryRestoreAutomatically()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryRestoreAutomatically;
                return;
            }

            if (!File.Exists(MenuScenePath) || !File.Exists(AtlasPath) || !File.Exists(BackgroundPath))
                return;

            RestoreApprovedArtwork(false);
        }

        [MenuItem("Tools/Conveyor Chef/Restore Approved Main Menu Artwork")]
        public static void RestoreApprovedArtworkMenu()
        {
            RestoreApprovedArtwork(true);
        }

        private static void RestoreApprovedArtwork(bool openScene)
        {
            ConfigureBackgroundImporter();
            ConfigureAtlasImporter();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene;
            bool sceneWasAlreadyOpen = SceneManager.GetActiveScene().path == MenuScenePath;

            if (sceneWasAlreadyOpen)
            {
                scene = SceneManager.GetActiveScene();
            }
            else
            {
                if (!openScene)
                    scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                else
                    scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            }

            Transform newMenu = FindTransformInScene(scene, "NEW Main Menu");
            if (newMenu == null)
            {
                Debug.LogError("[ProfessionalMainMenu] NEW Main Menu was not found under menu.unity.");
                return;
            }

            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            Dictionary<string, Sprite> atlasSprites = AssetDatabase
                .LoadAllAssetsAtPath(AtlasPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s, StringComparer.OrdinalIgnoreCase);

            BindImage(newMenu, "Background Artwork", background);
            BindImage(newMenu, "Game Logo", Get(atlasSprites, "logo"));
            BindImage(newMenu, "Chef Character", Get(atlasSprites, "chef"));
            BindImage(newMenu, "Chef Avatar", Get(atlasSprites, "avatar"));
            BindImage(newMenu, "Star Icon", Get(atlasSprites, "star"));

            BindImage(newMenu, "PLAY", Get(atlasSprites, "play"));
            BindImage(newMenu, "STORY", Get(atlasSprites, "story"));
            BindImage(newMenu, "CHALLENGES", Get(atlasSprites, "challenges"));
            BindImage(newMenu, "CUSTOMIZE", Get(atlasSprites, "customize"));
            BindImage(newMenu, "SETTINGS", Get(atlasSprites, "settings"));

            // Original approved counter bars.
            BindImage(newMenu, "Coin Counter", Get(atlasSprites, "coin_bar"));
            BindImage(newMenu, "Diamond Counter", Get(atlasSprites, "diamond_bar"));

            // Original approved bottom navigation artwork.
            BindImage(newMenu, "SHOP", Get(atlasSprites, "shop"));
            BindImage(newMenu, "COLLECTION", Get(atlasSprites, "collection"));
            BindImage(newMenu, "ACHIEVEMENTS", Get(atlasSprites, "achievements"));
            BindImage(newMenu, "LEADERBOARD", Get(atlasSprites, "leaderboard"));

            // The approved button/icon sprites already contain their own labels/art.
            // Disable fallback labels created by the scene serialization migration.
            SetChildActive(newMenu, "PLAY", "Label", false);
            SetChildActive(newMenu, "STORY", "Label", false);
            SetChildActive(newMenu, "CHALLENGES", "Label", false);
            SetChildActive(newMenu, "CUSTOMIZE", "Label", false);
            SetChildActive(newMenu, "SETTINGS", "Label", false);
            SetChildActive(newMenu, "SHOP", "Label", false);
            SetChildActive(newMenu, "COLLECTION", "Label", false);
            SetChildActive(newMenu, "ACHIEVEMENTS", "Label", false);
            SetChildActive(newMenu, "LEADERBOARD", "Label", false);

            // Icons are already integrated in the approved counter bar artwork.
            SetActive(newMenu, "Coin Icon", false);
            SetActive(newMenu, "Diamond Icon", false);

            // Make sure the scene-based menu remains the only production menu.
            GameObject legacyGenerated = FindRoot(scene, "ConveyorChef_MainMenu_Canvas");
            if (legacyGenerated != null)
                UnityEngine.Object.DestroyImmediate(legacyGenerated);

            GameObject obsoleteInstaller = FindRoot(scene, "Professional Main Menu");
            if (obsoleteInstaller != null)
                UnityEngine.Object.DestroyImmediate(obsoleteInstaller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ProfessionalMainMenu] Approved original menu artwork restored to the editable NEW Main Menu hierarchy.");
        }

        private static void ConfigureBackgroundImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        private static void ConfigureAtlasImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (texture == null)
            {
                importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            }

            int atlasHeight = texture != null ? texture.height : 1222;

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            SpriteRect[] spriteRects = Regions.Select(region => new SpriteRect
            {
                name = region.Name,
                rect = new Rect(region.X, atlasHeight - region.YTop - region.Height, region.Width, region.Height),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = GUID.Generate()
            }).ToArray();

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();

            importer.SaveAndReimport();
        }

        private static Sprite Get(Dictionary<string, Sprite> sprites, string name)
        {
            if (sprites.TryGetValue(name, out Sprite sprite))
                return sprite;

            Debug.LogError("[ProfessionalMainMenu] Approved atlas sprite not found: " + name);
            return null;
        }

        private static void BindImage(Transform root, string objectName, Sprite sprite)
        {
            if (sprite == null)
                return;

            Transform target = FindDeep(root, objectName);
            if (target == null)
            {
                Debug.LogWarning("[ProfessionalMainMenu] Scene object not found: " + objectName);
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning("[ProfessionalMainMenu] Image component missing on: " + objectName);
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;

            if (objectName == "Game Logo" ||
                objectName == "Chef Character" ||
                objectName == "Chef Avatar" ||
                objectName == "Star Icon" ||
                objectName == "SHOP" ||
                objectName == "COLLECTION" ||
                objectName == "ACHIEVEMENTS" ||
                objectName == "LEADERBOARD")
            {
                image.preserveAspect = true;
            }

            EditorUtility.SetDirty(image);
        }

        private static void SetChildActive(Transform root, string parentName, string childName, bool state)
        {
            Transform parent = FindDeep(root, parentName);
            if (parent == null)
                return;

            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(state);
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        private static void SetActive(Transform root, string objectName, bool state)
        {
            Transform target = FindDeep(root, objectName);
            if (target != null)
            {
                target.gameObject.SetActive(state);
                EditorUtility.SetDirty(target.gameObject);
            }
        }

        private static Transform FindTransformInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform result = FindDeep(root.transform, objectName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, rootName, StringComparison.Ordinal))
                    return root;
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string objectName)
        {
            if (string.Equals(parent.name, objectName, StringComparison.Ordinal))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
#endif
