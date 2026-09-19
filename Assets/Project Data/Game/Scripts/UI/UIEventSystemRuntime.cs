using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Guarantees exactly one active EventSystem, owned by the CURRENT scene.
    ///
    /// Important project behaviour:
    /// WorldMap input works when WorldMap.unity is started directly, but previously
    /// stopped working after Menu -> Loading -> WorldMap because a persistent
    /// Initialiser EventSystem was carried across the transition.
    ///
    /// The reliable solution is to use the same scene-local EventSystem in both cases.
    /// On every scene load:
    /// 1) disable every other EventSystem and its input modules;
    /// 2) activate the EventSystem serialized in the newly loaded scene;
    /// 3) enable its configured input module;
    /// 4) make it EventSystem.current.
    ///
    /// This makes direct WorldMap testing and the full game flow use identical input.
    /// </summary>
    public static class UIEventSystemRuntime
    {
        private static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            hooked = false;
            EventSystem.current = null;
        }

        public static void Adopt(EventSystem system)
        {
            HookSceneEvents();

            // Initialiser may call this before the first UI scene. It is only a
            // temporary bootstrap EventSystem; the next loaded scene will replace it
            // with its own serialized local EventSystem.
            if (system == null)
                return;

            ActivateExactly(system);
        }

        public static EventSystem Ensure()
        {
            HookSceneEvents();
            return ActivateForScene(SceneManager.GetActiveScene());
        }

        public static EventSystem UseCurrentSceneEventSystem()
        {
            HookSceneEvents();
            return ActivateForScene(SceneManager.GetActiveScene());
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
            ActivateForScene(scene);
        }

        private static EventSystem ActivateForScene(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            EventSystem selected = FindSceneEventSystem(scene);

            if (selected == null)
            {
                GameObject go = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(go, scene);
                selected = go.AddComponent<EventSystem>();
            }

            ActivateExactly(selected);
            return selected;
        }

        private static EventSystem FindSceneEventSystem(Scene scene)
        {
            EventSystem[] all = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                EventSystem candidate = all[i];
                if (candidate != null && candidate.gameObject.scene == scene)
                    return candidate;
            }

            return null;
        }

        private static void ActivateExactly(EventSystem selected)
        {
            if (selected == null)
                return;

            EventSystem[] all = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            // Disable everything else FIRST. This prevents the Unity warning and also
            // prevents an old input module from consuming pointer/drag events.
            for (int i = 0; i < all.Length; i++)
            {
                EventSystem other = all[i];
                if (other == null || other == selected)
                    continue;

                BaseInputModule[] otherModules = other.GetComponents<BaseInputModule>();
                for (int m = 0; m < otherModules.Length; m++)
                {
                    if (otherModules[m] != null)
                        otherModules[m].enabled = false;
                }

                other.enabled = false;

                if (other.gameObject.activeSelf)
                    other.gameObject.SetActive(false);
            }

            EnsureInputModule(selected);

            if (!selected.gameObject.activeSelf)
                selected.gameObject.SetActive(true);

            selected.enabled = true;

            BaseInputModule[] selectedModules = selected.GetComponents<BaseInputModule>();
            for (int i = 0; i < selectedModules.Length; i++)
            {
                if (selectedModules[i] != null)
                    selectedModules[i].enabled = true;
            }

            EventSystem.current = selected;
            selected.SetSelectedGameObject(null);

            Debug.Log(
                "[UI Input] Active scene EventSystem: " +
                selected.gameObject.scene.name + "/" + selected.gameObject.name +
                " | module=" + GetModuleName(selected));
        }

        private static void EnsureInputModule(EventSystem system)
        {
            if (system == null)
                return;

            BaseInputModule[] modules = system.GetComponents<BaseInputModule>();
            if (modules != null && modules.Length > 0)
                return;

            Type inputSystemModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModuleType != null)
            {
                system.gameObject.AddComponent(inputSystemModuleType);
            }
            else
            {
                system.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private static string GetModuleName(EventSystem system)
        {
            if (system == null)
                return "none";

            BaseInputModule module = system.GetComponent<BaseInputModule>();
            return module != null ? module.GetType().Name : "none";
        }
    }
}
