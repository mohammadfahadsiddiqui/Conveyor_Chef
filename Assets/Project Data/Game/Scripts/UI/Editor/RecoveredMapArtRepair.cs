#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.EditorTools
{
    /// <summary>
    /// Repairs only missing sprite references for the shared map HUD artwork.
    /// It never changes RectTransform geometry, hierarchy, text, buttons or gameplay logic.
    /// </summary>
    [InitializeOnLoad]
    public static class RecoveredMapArtRepair
    {
        private const string LevelSelectionScene = "Assets/Project Data/Game/Scenes/LevelSelection.unity";
        private const string CountryMapScene = "Assets/Project Data/Game/Scenes/CountryMap.unity";

        private const string RestoredBubblePath =
            "Assets/Project Data/Game/Images/CountryMap/restored_glossy_chef_s_dialogue_bubble.png";

        private const string RestoredCounterPath =
            "Assets/Project Data/Game/Images/CountryMap/restored_glossy_blue_game_progress_panel.png";

        private static readonly HashSet<string> CounterNames =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Coin Counter",
                "Diamond Counter",
                "Star Counter"
            };

        static RecoveredMapArtRepair()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += RepairActiveScene;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == LevelSelectionScene || scene.path == CountryMapScene)
                EditorApplication.delayCall += RepairActiveScene;
        }

        [MenuItem("Conveyor Chef/Repair/Restore Missing Map HUD Art", priority = 30)]
        private static void RepairActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                (scene.path != LevelSelectionScene && scene.path != CountryMapScene))
            {
                return;
            }

            Sprite bubble = LoadSprite(RestoredBubblePath);
            Sprite counter = LoadSprite(RestoredCounterPath);

            if (bubble == null || counter == null)
            {
                Debug.LogWarning(
                    "[RecoveredMapArtRepair] Restored map HUD sprites are not imported yet. " +
                    "Unity will retry when the scene is reopened.");
                return;
            }

            bool wasDirty = scene.isDirty;
            bool changed = false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Image[] images = root.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    if (image == null || image.sprite != null)
                        continue;

                    if (image.name == "Chef Speech Bubble")
                    {
                        image.sprite = bubble;
                        image.preserveAspect = false;
                        EditorUtility.SetDirty(image);
                        changed = true;
                    }
                    else if (CounterNames.Contains(image.name))
                    {
                        image.sprite = counter;
                        image.preserveAspect = false;
                        EditorUtility.SetDirty(image);
                        changed = true;
                    }
                }
            }

            if (!changed)
                return;

            EditorSceneManager.MarkSceneDirty(scene);

            // Never auto-save over the user's own unsaved layout work.
            if (!wasDirty)
                EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "[RecoveredMapArtRepair] Restored missing Chef Speech Bubble / counter art in " +
                scene.name + " without changing UI layout.");
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
