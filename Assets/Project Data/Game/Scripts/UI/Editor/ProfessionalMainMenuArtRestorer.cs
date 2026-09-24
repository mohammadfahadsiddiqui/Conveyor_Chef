#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Materialises the exact approved embedded Professional Main Menu artwork
    /// into normal editable Unity sprite assets and binds those assets to
    /// Canvas -> NEW Main Menu in menu.unity.
    /// </summary>
    public static class ProfessionalMainMenuArtRestorer
    {
        private const string MenuScenePath = "Assets/Project Data/Game/Scenes/menu.unity";
        private const string BakedFolder = "Assets/Project Data/Game/Images/ProfessionalMainMenuBaked";
        private const string SessionKey = "ConveyorChef.ProfessionalMenu.ArtRestore.v3";

        private static readonly string[] AssetNames =
        {
            "background", "logo", "chef", "avatar",
            "play", "story", "challenges", "customize", "settings",
            "coin_bar", "diamond_bar", "star",
            "shop", "collection", "achievements", "leaderboard"
        };

        private static readonly Dictionary<string, string> SceneBindings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "background", "Background Artwork" },
                { "logo", "Game Logo" },
                { "chef", "Chef Character" },
                { "avatar", "Chef Avatar" },
                { "play", "PLAY" },
                { "story", "STORY" },
                { "challenges", "CHALLENGES" },
                { "customize", "CUSTOMIZE" },
                { "settings", "SETTINGS" },
                { "coin_bar", "Coin Counter" },
                { "diamond_bar", "Diamond Counter" },
                { "star", "Star Icon" },
                { "shop", "SHOP" },
                { "collection", "COLLECTION" },
                { "achievements", "ACHIEVEMENTS" },
                { "leaderboard", "LEADERBOARD" }
            };

        [InitializeOnLoadMethod]
        private static void RestoreOnceAfterCompile()
        {
            // IMPORTANT: menu.unity is the authoritative serialized Main Menu.
            // Never open/save/rewrite it automatically after a compile.
            // Keep the manual Tools > Conveyor Chef > Restore Approved Main Menu Artwork
            // command available for explicit recovery only.
        }

        private static void TryRestoreAutomatically()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryRestoreAutomatically;
                return;
            }

            if (!File.Exists(MenuScenePath))
                return;

            RestoreApprovedArtwork(false);
        }

        [MenuItem("Tools/Conveyor Chef/Restore Approved Main Menu Artwork")]
        public static void RestoreApprovedArtworkMenu()
        {
            RestoreApprovedArtwork(true);
        }

        private static void RestoreApprovedArtwork(bool explicitRequest)
        {
            Directory.CreateDirectory(BakedFolder);

            foreach (string assetName in AssetNames)
            {
                if (!BakeEmbeddedSprite(assetName))
                {
                    Debug.LogError("[ProfessionalMainMenu] Failed to bake approved sprite: " + assetName);
                    return;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene = SceneManager.GetActiveScene().path == MenuScenePath
                ? SceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            Transform newMenu = FindTransformInScene(scene, "NEW Main Menu");
            if (newMenu == null)
            {
                Debug.LogError("[ProfessionalMainMenu] NEW Main Menu was not found in menu.unity.");
                return;
            }

            foreach (KeyValuePair<string, string> binding in SceneBindings)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetBakedPath(binding.Key));
                BindImage(newMenu, binding.Value, sprite);
            }

            // Approved art already contains button labels/icons.
            string[] bakedButtonNames =
            {
                "PLAY", "STORY", "CHALLENGES", "CUSTOMIZE", "SETTINGS",
                "SHOP", "COLLECTION", "ACHIEVEMENTS", "LEADERBOARD"
            };

            foreach (string buttonName in bakedButtonNames)
                SetChildActive(newMenu, buttonName, "Label", false);

            // These icons are already part of the approved currency bar artwork.
            SetActive(newMenu, "Coin Icon", false);
            SetActive(newMenu, "Diamond Icon", false);

            // Make sure hero pieces are explicitly active and visible.
            SetActive(newMenu, "Background Artwork", true);
            SetActive(newMenu, "Game Logo", true);
            SetActive(newMenu, "Chef Character", true);
            SetActive(newMenu, "Chef Avatar", true);

            // Remove obsolete duplicate generated roots from the old menu system.
            DestroyRootIfPresent(scene, "ConveyorChef_MainMenu_Canvas");
            DestroyRootIfPresent(scene, "Professional Main Menu");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ProfessionalMainMenu] Complete approved menu artwork restored: background, logo, chef, HUD, buttons and navigation.");
        }

        private static bool BakeEmbeddedSprite(string assetName)
        {
            Sprite source = ProfessionalMainMenuEmbeddedAssets.GetSprite(assetName);
            if (source == null || source.texture == null)
                return false;

            Rect sourceRect = source.rect;
            int width = Mathf.RoundToInt(sourceRect.width);
            int height = Mathf.RoundToInt(sourceRect.height);

            Color[] pixels;
            try
            {
                pixels = source.texture.GetPixels(
                    Mathf.RoundToInt(sourceRect.x),
                    Mathf.RoundToInt(sourceRect.y),
                    width,
                    height);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ProfessionalMainMenu] Could not read " + assetName + ": " + ex.Message);
                return false;
            }

            Texture2D baked = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            baked.name = "PMM_" + assetName;
            baked.SetPixels(pixels);
            baked.Apply(false, false);

            byte[] png = baked.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(baked);

            string assetPath = GetBakedPath(assetName);
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = !string.Equals(assetName, "background", StringComparison.OrdinalIgnoreCase);
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) != null;
        }

        private static string GetBakedPath(string assetName)
        {
            return BakedFolder + "/" + assetName + ".png";
        }

        private static void BindImage(Transform root, string objectName, Sprite sprite)
        {
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

            if (sprite == null)
            {
                Debug.LogError("[ProfessionalMainMenu] Baked sprite missing for: " + objectName);
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.enabled = true;

            bool preserveAspect =
                objectName == "Game Logo" ||
                objectName == "Chef Character" ||
                objectName == "Chef Avatar" ||
                objectName == "Star Icon" ||
                objectName == "SHOP" ||
                objectName == "COLLECTION" ||
                objectName == "ACHIEVEMENTS" ||
                objectName == "LEADERBOARD";

            image.preserveAspect = preserveAspect;
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(target.gameObject);
        }

        private static void SetChildActive(Transform root, string parentName, string childName, bool state)
        {
            Transform parent = FindDeep(root, parentName);
            if (parent == null)
                return;

            Transform child = parent.Find(childName);
            if (child == null)
                return;

            child.gameObject.SetActive(state);
            EditorUtility.SetDirty(child.gameObject);
        }

        private static void SetActive(Transform root, string objectName, bool state)
        {
            Transform target = FindDeep(root, objectName);
            if (target == null)
                return;

            target.gameObject.SetActive(state);
            EditorUtility.SetDirty(target.gameObject);
        }

        private static void DestroyRootIfPresent(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, rootName, StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    return;
                }
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
