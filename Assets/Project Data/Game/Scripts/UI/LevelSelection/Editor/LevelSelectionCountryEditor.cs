using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Editing the countries of LevelSelection.unity:
    /// - selecting a country (or anything inside it) in the Hierarchy shows that country on the
    ///   Canvas and hides the others;
    /// - country pictures (LevelSelectionLazySprite) are shown as a preview but not saved in the
    ///   scene, so the scene does not load every country's pictures. A picture swapped on the Image
    ///   is kept: from a Resources folder it stays on-demand, otherwise it becomes a normal reference.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelSelectionCountryEditor
    {
        private static readonly List<LevelSelectionLazySprite> stripped = new List<LevelSelectionLazySprite>();

        static LevelSelectionCountryEditor()
        {
            Selection.selectionChanged += ShowSelectedCountry;
            EditorSceneManager.sceneSaving += (scene, path) => StripPreviews(scene);
            EditorSceneManager.sceneSaved += scene => RestorePreviews();
            EditorApplication.playModeStateChanged += state =>
            {
                // Play starts from the scene as saved: pictures load on demand there too.
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        StripPreviews(SceneManager.GetSceneAt(i));
                    stripped.Clear();
                }
            };
        }

        private static void ShowSelectedCountry()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid())
                return;

            LevelSelectionCountry country = selected.GetComponentInParent<LevelSelectionCountry>(true);
            if (country == null)
                return;

            bool changed = false;
            foreach (LevelSelectionCountry other in Object.FindObjectsByType<LevelSelectionCountry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other.gameObject.scene != country.gameObject.scene)
                    continue;

                bool show = other == country;
                if (other.gameObject.activeSelf != show)
                {
                    other.gameObject.SetActive(show);
                    changed = true;
                }
            }

            if (changed)
                SceneView.RepaintAll();
        }

        private static void StripPreviews(Scene scene)
        {
            foreach (LevelSelectionLazySprite lazy in Object.FindObjectsByType<LevelSelectionLazySprite>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (lazy.gameObject.scene != scene || string.IsNullOrEmpty(lazy.ResourcePath))
                    continue;

                Image image = lazy.GetComponent<Image>();
                if (image == null || image.sprite == null)
                    continue;

                if (image.sprite != lazy.Preview)
                {
                    // Picture swapped by hand: keep it.
                    string assetPath = AssetDatabase.GetAssetPath(image.sprite);
                    int resources = assetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
                    if (resources < 0)
                    {
                        lazy.EditorSetPath(string.Empty);   // normal reference from now on
                        continue;
                    }

                    string path = assetPath.Substring(resources + "/Resources/".Length);
                    lazy.EditorSetPath(System.IO.Path.ChangeExtension(path, null).Replace('\\', '/'));
                }

                image.sprite = null;
                stripped.Add(lazy);
            }
        }

        private static void RestorePreviews()
        {
            foreach (LevelSelectionLazySprite lazy in stripped)
            {
                if (lazy != null && lazy.isActiveAndEnabled)
                    lazy.Load();
            }
            stripped.Clear();
        }
    }
}
