using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Attaches the shared Conveyor Chef theme to the existing live scenes/pages.
    /// This keeps the old gameplay callbacks and serialized references intact.
    /// </summary>
    public static class ConveyorChefUIRuntimeBridge
    {
        private const string RUNNER_NAME = "CC_UI_RuntimeBridge";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GameObject existing = GameObject.Find(RUNNER_NAME);
            if (existing != null)
                Object.Destroy(existing);

            GameObject runner = new GameObject(RUNNER_NAME);
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<BridgeRunner>().Begin(scene.name);
        }

        private sealed class BridgeRunner : MonoBehaviour
        {
            private string sceneName;

            public void Begin(string loadedSceneName)
            {
                sceneName = loadedSceneName;
                StartCoroutine(ApplyWhenReady());
            }

            private IEnumerator ApplyWhenReady()
            {
                yield return null;
                yield return null;

                if (sceneName.Equals("LevelSelection", System.StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < 30; i++)
                    {
                        LevelSelectionController controller = Object.FindFirstObjectByType<LevelSelectionController>(FindObjectsInactive.Include);
                        if (controller != null)
                        {
                            ConveyorChefLevelSelectionStyler.Style(controller);
                            break;
                        }
                        yield return null;
                    }
                }
                else if (sceneName.Equals("Game", System.StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < 60; i++)
                    {
                        bool styledAnything = false;

                        global::Watermelon.UIMainMenu[] mainMenus = Object.FindObjectsByType<global::Watermelon.UIMainMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (global::Watermelon.UIMainMenu menu in mainMenus)
                        {
                            ConveyorChefGameplayHUDStyler.Style(menu);
                            styledAnything = true;
                        }

                        global::Watermelon.UIGame[] gamePages = Object.FindObjectsByType<global::Watermelon.UIGame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (global::Watermelon.UIGame page in gamePages)
                        {
                            TMPro.TextMeshProUGUI level = FindLevelLabel(page.gameObject);
                            UnityEngine.UI.Button replay = FindNamedButton(page.gameObject, "replay");
                            ConveyorChefUITheme.StyleGameplayPage(page.gameObject, level, replay);
                            ConveyorChefGameplayHUDStyler.StylePowerups(page.gameObject);
                            styledAnything = true;
                        }

                        global::Watermelon.UIComplete[] completePages = Object.FindObjectsByType<global::Watermelon.UIComplete>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (global::Watermelon.UIComplete page in completePages)
                        {
                            ConveyorChefUITheme.StyleCompletePage(page.gameObject);
                            styledAnything = true;
                        }

                        global::Watermelon.UIGameOver[] gameOverPages = Object.FindObjectsByType<global::Watermelon.UIGameOver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (global::Watermelon.UIGameOver page in gameOverPages)
                        {
                            ConveyorChefUITheme.StyleGameOverPage(page.gameObject);
                            styledAnything = true;
                        }

                        if (styledAnything)
                            break;

                        yield return null;
                    }
                }

                Destroy(gameObject);
            }

            private static TMPro.TextMeshProUGUI FindLevelLabel(GameObject root)
            {
                TMPro.TextMeshProUGUI[] labels = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (TMPro.TextMeshProUGUI label in labels)
                {
                    string n = label.name.ToLowerInvariant();
                    if (n.Contains("level"))
                        return label;
                }
                return null;
            }

            private static UnityEngine.UI.Button FindNamedButton(GameObject root, string token)
            {
                UnityEngine.UI.Button[] buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (UnityEngine.UI.Button button in buttons)
                {
                    if (button.name.ToLowerInvariant().Contains(token))
                        return button;
                }
                return null;
            }
        }
    }
}
