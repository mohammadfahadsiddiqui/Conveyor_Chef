using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Turns the picture boats in the open World Map into 3D boats (WorldMapBoat3D), keeping their
    /// position, size and the rest of your scene edits. Safe to run more than once.
    /// </summary>
    public static class WorldMapBoatUpgrade
    {
        private const string ModelFolder = "Assets/Project Data/Game/Resources/WorldMapSea/";
        private const string EffectsPath = "Assets/Project Data/Game/Images/WorldMap/Props/boat_fx.png";

        [MenuItem("Conveyor Chef/World Map/9. Use 3D Boats In Open World Map", priority = 9)]
        private static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[WorldMap] Stop Play mode first, then run 'Use 3D Boats' again.");
                return;
            }

            var effects = AssetDatabase.LoadAssetAtPath<Texture2D>(EffectsPath);
            int boats = 0, extras = 0;

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;

                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name != "Map Props")
                            continue;

                        foreach (Transform prop in t)
                        {
                            extras += RemoveDuplicateAnimators(prop.gameObject);

                            if (prop.name != "Sailboat" && prop.name != "Steamship")
                                continue;

                            var model = AssetDatabase.LoadAssetAtPath<TextAsset>(ModelFolder + prop.name.ToLowerInvariant() + ".txt");
                            if (model == null)
                                continue;

                            if (UpgradeBoat(prop, model, effects))
                                boats++;
                        }
                        changed = true;
                    }
                }

                if (changed && (boats > 0 || extras > 0))
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[WorldMap] 3D boats: " + boats + " boat(s) upgraded, " + extras +
                      " duplicate animation component(s) removed. Save the scene to keep it.");
        }

        private static bool UpgradeBoat(Transform prop, TextAsset model, Texture2D effects)
        {
            if (prop.GetComponent<WorldMapBoat3D>() != null)
                return false;

            bool facingLeft = prop.localScale.x < 0f;
            Image picture = prop.GetComponent<Image>();
            if (picture != null)
                Undo.DestroyObjectImmediate(picture);

            Undo.RecordObject(prop, "Use 3D Boat");
            Vector3 scale = prop.localScale;
            scale.x = Mathf.Abs(scale.x);
            prop.localScale = scale;

            var boat = Undo.AddComponent<WorldMapBoat3D>(prop.gameObject);
            var so = new SerializedObject(boat);
            so.FindProperty("model").objectReferenceValue = model;
            so.FindProperty("effects").objectReferenceValue = effects;
            so.FindProperty("yaw").floatValue = facingLeft ? 180f : 0f;
            so.FindProperty("size").floatValue = 0.46f;
            so.FindProperty("wakeAlpha").floatValue = 1f;
            so.FindProperty("smokePuffs").intValue = prop.name == "Steamship" ? 4 : 0;
            so.FindProperty("m_RaycastTarget").boolValue = false;
            so.ApplyModifiedProperties();
            return true;
        }

        // Boats and whales copied from an already animated one could carry two animators.
        private static int RemoveDuplicateAnimators(GameObject go)
        {
            WorldMapAmbientProp[] all = go.GetComponents<WorldMapAmbientProp>();
            for (int i = 0; i < all.Length - 1; i++)
                Undo.DestroyObjectImmediate(all[i]);
            return Mathf.Max(0, all.Length - 1);
        }
    }
}
