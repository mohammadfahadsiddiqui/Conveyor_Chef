using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// A short fade between scenes, used instead of the full loading screen for quick hops
    /// (menu, World Map, Country Map, Level Selection, and every level after the first one).
    /// The screen fades to the dark kitchen blue, the scene loads, then it fades back in.
    /// Unscaled time, so it also works while the game is paused.
    /// </summary>
    public sealed class QuickSceneTransition : MonoBehaviour
    {
        private const float FadeOut = 0.16f;
        private const float FadeIn = 0.24f;
        private static readonly Color Cover = new Color(0.07f, 0.11f, 0.2f, 1f);

        private static QuickSceneTransition running;

        public static bool IsRunning => running != null;

        public static void Load(string sceneName, System.Action onFinished = null)
        {
            if (running != null)
                return;

            var go = new GameObject("Quick Scene Transition", typeof(Canvas), typeof(CanvasGroup));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var cover = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cover.transform.SetParent(go.transform, false);
            var rect = (RectTransform)cover.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            cover.GetComponent<Image>().color = Cover;

            running = go.AddComponent<QuickSceneTransition>();
            running.StartCoroutine(running.Run(sceneName, go.GetComponent<CanvasGroup>(), onFinished));
        }

        private IEnumerator Run(string sceneName, CanvasGroup group, System.Action onFinished)
        {
            group.alpha = 0f;
            group.blocksRaycasts = true;            // no double taps while switching
            for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / FadeOut;
                yield return null;
            }
            group.alpha = 1f;

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone)
                yield return null;
            yield return null;                       // let the new scene lay itself out

            for (float t = 0f; t < FadeIn; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeIn;
                yield return null;
            }

            running = null;
            onFinished?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (running == this)
                running = null;
        }
    }
}
