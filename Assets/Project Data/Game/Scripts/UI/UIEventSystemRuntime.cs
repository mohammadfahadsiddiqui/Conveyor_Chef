using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Owns exactly one EventSystem for the complete application lifetime.
    ///
    /// Why this exists:
    /// menu/loading/world-map each contain an inactive local EventSystem so those
    /// scenes are still testable directly in the editor. During the real flow,
    /// however, one EventSystem must survive scene changes. Recreating/reenabling a
    /// different EventSystem during scene activation can cause Unity's
    /// "There can be only one active Event System" warning and leave UI drag input
    /// in a bad state.
    ///
    /// This class adopts the Initialiser EventSystem during the normal app flow.
    /// When a UI scene is run directly, it activates exactly one scene-local
    /// EventSystem and makes it persistent before any scene transition occurs.
    /// </summary>
    public static class UIEventSystemRuntime
    {
        private static EventSystem authoritative;
        private static bool hooked;

        public static EventSystem Current => authoritative;

        public static void Adopt(EventSystem system)
        {
            HookSceneEvents();

            if (system == null)
            {
                Ensure();
                return;
            }

            authoritative = system;
            DisableAllExcept(authoritative);
            EnsureInputModule(authoritative);

            if (!authoritative.gameObject.activeSelf)
                authoritative.gameObject.SetActive(true);

            authoritative.enabled = true;
        }

        public static EventSystem Ensure()
        {
            HookSceneEvents();

            if (IsUsable(authoritative))
            {
                DisableAllExcept(authoritative);
                EnsureInputModule(authoritative);
                return authoritative;
            }

            EventSystem[] all = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            // Prefer an already-active system. This is normally the Initialiser one.
            EventSystem selected = null;

            for (int i = 0; i < all.Length; i++)
            {
                EventSystem candidate = all[i];
                if (candidate == null)
                    continue;

                if (candidate.enabled && candidate.gameObject.activeInHierarchy)
                {
                    selected = candidate;
                    break;
                }
            }

            // Direct-scene test: choose the inactive EventSystem belonging to the
            // currently active scene, rather than an unrelated inactive object.
            if (selected == null)
            {
                Scene activeScene = SceneManager.GetActiveScene();

                for (int i = 0; i < all.Length; i++)
                {
                    EventSystem candidate = all[i];
                    if (candidate == null)
                        continue;

                    if (candidate.gameObject.scene == activeScene)
                    {
                        selected = candidate;
                        break;
                    }
                }
            }

            // Last-resort fallback.
            if (selected == null && all.Length > 0)
                selected = all[0];

            if (selected == null)
            {
                GameObject go = new GameObject("[GLOBAL EVENT SYSTEM]");
                selected = go.AddComponent<EventSystem>();
            }

            authoritative = selected;

            // Crucial ordering: all other EventSystems are disabled/deactivated
            // BEFORE the authoritative one is enabled.
            DisableAllExcept(authoritative);
            EnsureInputModule(authoritative);

            if (!authoritative.gameObject.activeSelf)
                authoritative.gameObject.SetActive(true);

            authoritative.enabled = true;

            // A direct-scene EventSystem must survive Menu -> Loading -> WorldMap.
            // The Initialiser EventSystem is already a child of a DontDestroyOnLoad
            // root, so only mark root-level local systems here.
            if (authoritative.transform.parent == null &&
                authoritative.gameObject.scene.IsValid())
            {
                UnityEngine.Object.DontDestroyOnLoad(authoritative.gameObject);
            }

            return authoritative;
        }

        private static void HookSceneEvents()
        {
            if (hooked)
                return;

            hooked = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Scene-local EventSystems are serialized inactive. Keep the one
            // persistent authoritative system and explicitly suppress all others.
            if (IsUsable(authoritative))
            {
                DisableAllExcept(authoritative);
                EnsureInputModule(authoritative);

                if (!authoritative.gameObject.activeSelf)
                    authoritative.gameObject.SetActive(true);

                authoritative.enabled = true;
                return;
            }

            Ensure();
        }

        private static bool IsUsable(EventSystem system)
        {
            return system != null && system.gameObject != null;
        }

        private static void DisableAllExcept(EventSystem keep)
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < systems.Length; i++)
            {
                EventSystem system = systems[i];

                if (system == null || system == keep)
                    continue;

                system.enabled = false;

                if (system.gameObject.activeSelf)
                    system.gameObject.SetActive(false);
            }
        }

        private static void EnsureInputModule(EventSystem system)
        {
            if (system == null)
                return;

            BaseInputModule[] modules = system.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null)
                {
                    modules[i].enabled = true;
                    return;
                }
            }

            // Prefer the new Input System when the package exists.
            Type inputSystemModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModuleType != null)
            {
                Component module = system.gameObject.AddComponent(inputSystemModuleType);
                if (module is Behaviour behaviour)
                    behaviour.enabled = true;
            }
            else
            {
                system.gameObject.AddComponent<StandaloneInputModule>();
            }
        }
    }
}
