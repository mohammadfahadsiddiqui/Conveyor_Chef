#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Unity's "The referenced script (Unknown) on this Behaviour is missing!" warning does not
    /// say which object it is. This names every object with a missing script (its full Hierarchy
    /// path and scene) when a scene loads. Editor and development builds only.
    /// </summary>
    public static class MissingScriptReporter
    {
        private static readonly List<Component> components = new List<Component>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    t.GetComponents(components);
                    int missing = 0;
                    for (int i = 0; i < components.Count; i++)
                    {
                        if (components[i] == null)
                            missing++;
                    }

                    if (missing > 0)
                        Debug.LogWarning("[Missing Script] " + missing + " missing script(s) on '" + PathOf(t) +
                                         "' in scene '" + scene.name + "'. Select it and remove the empty component.", t.gameObject);
                }
            }
            components.Clear();
        }

        private static string PathOf(Transform t)
        {
            StringBuilder path = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
                path.Insert(0, p.name + "/");
            return path.ToString();
        }
    }
}
#endif
