#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    public static class ProfessionalMainMenuSceneBaker
    {
        private const string AutoBakeSessionKey = "ConveyorChef.ProfessionalMainMenu.AutoBake.v2";
        private const string MenuScenePath = "Assets/Project Data/Game/Scenes/menu.unity";
        private const string BakedImageFolder = "Assets/Project Data/Game/Images/ProfessionalMainMenuBaked";

        private static readonly string[] AssetNames =
        {
            "background",
            "logo",
            "chef",
            "avatar",
            "play",
            "story",
            "challenges",
            "customize",
            "settings",
            "coin_bar",
            "diamond_bar",
            "star",
            "shop",
            "collection",
            "achievements",
            "leaderboard"
        };

        // Legacy runtime-generated menu baker retained only for manual recovery.
        // Automatic execution is intentionally disabled because menu.unity now contains
        // the authoritative editable Canvas -> NEW Main Menu hierarchy, matching loading.unity.
        [MenuItem("Tools/Conveyor Chef/Legacy/Rebuild Runtime-Generated Main Menu")]
        public static void RebuildProfessionalMainMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder(BakedImageFolder);

            foreach (string assetName in AssetNames)
            {
                BakeSprite(assetName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            ProfessionalMainMenuInstaller installer =
                UnityEngine.Object.FindFirstObjectByType<ProfessionalMainMenuInstaller>(
                    FindObjectsInactive.Include);

            if (installer != null)
            {
                UnityEngine.Object.DestroyImmediate(installer.gameObject);
            }

            // Build the professional menu exactly like loading.unity:
            // the UI root itself is a scene-level RectTransform/Canvas.
            GameObject root = new GameObject(
                "ConveyorChef_MainMenu_Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            installer = root.AddComponent<ProfessionalMainMenuInstaller>();
            installer.RebuildSceneMenuForEditor();

            DisableLegacySceneObjects(installer.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = installer.gameObject;

            Debug.Log("[ProfessionalMainMenu] menu.unity rebuilt and saved with the new professional menu hierarchy.");
        }

        private static void BakeSprite(string assetName)
        {
            Sprite source = ProfessionalMainMenuEmbeddedAssets.GetSprite(assetName);
            if (source == null || source.texture == null)
            {
                Debug.LogError("[ProfessionalMainMenu] Could not bake sprite: " + assetName);
                return;
            }

            Texture2D readable = CopySpriteTexture(source);
            if (readable == null)
                return;

            byte[] png = readable.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(readable);

            string path = BakedImageFolder + "/" + assetName + ".png";
            File.WriteAllBytes(path, png);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D CopySpriteTexture(Sprite sprite)
        {
            Rect rect = sprite.rect;
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            RenderTexture rt = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(sprite.texture, rt);

                RenderTexture.active = rt;

                Texture2D fullTexture = new Texture2D(
                    sprite.texture.width,
                    sprite.texture.height,
                    TextureFormat.RGBA32,
                    false);

                // Read the whole source texture first so atlas sub-rect extraction stays correct.
                RenderTexture whole = RenderTexture.GetTemporary(
                    sprite.texture.width,
                    sprite.texture.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);

                Graphics.Blit(sprite.texture, whole);
                RenderTexture.active = whole;

                fullTexture.ReadPixels(
                    new Rect(0, 0, sprite.texture.width, sprite.texture.height),
                    0,
                    0);

                fullTexture.Apply();

                Color[] pixels = fullTexture.GetPixels(
                    Mathf.RoundToInt(rect.x),
                    Mathf.RoundToInt(rect.y),
                    width,
                    height);

                UnityEngine.Object.DestroyImmediate(fullTexture);
                RenderTexture.ReleaseTemporary(whole);

                Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.SetPixels(pixels);
                result.Apply();

                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static void DisableLegacySceneObjects(Transform professionalRoot)
        {
            Scene scene = professionalRoot.gameObject.scene;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == professionalRoot.gameObject)
                    continue;

                if (root.name == "Canvas" ||
                    root.name == "scooter" ||
                    root.name == "scene")
                {
                    root.SetActive(false);
                    EditorUtility.SetDirty(root);
                }
            }

            // Keep the scene camera/light/event system available for editing/runtime systems.
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
