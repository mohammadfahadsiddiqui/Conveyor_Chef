using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Production loading screen for Conveyor Chef.
    /// All gameplay scene transitions can route through the dedicated "loading" scene.
    /// The progress bar reflects Unity's real asynchronous scene-loading progress.
    /// </summary>
    public class EnhancedLoadingScreen : MonoBehaviour
    {
        private const string LoadingSceneName = "loading";
        private const string PendingSceneKey = "ConveyorChef.PendingScene";

        private static string pendingSceneName;

        [Header("Loading Screen UI")]
        public GameObject loadingScreenPanel;
        public Image backgroundImage;
        public Image progressBarFill;
        public Slider progressBarSlider;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI loadingTipText;

        [Header("Animated Elements")]
        public RectTransform scooterTransform;
        public RectTransform mapTransform;
        public RectTransform shadowTransform;
        public RectTransform logoTransform;

        [Header("Logo Splash")]
        [Min(0.1f)] public float logoSplashDuration = 0.55f;
        [Range(0.1f, 1f)] public float logoSplashStartScale = 0.72f;
        [Range(1f, 1.3f)] public float logoSplashOvershoot = 1.08f;

        [Header("Animation Settings")]
        [Min(0.1f)] public float minimumLoadTime = 1.25f;
        [Min(0.1f)] public float progressSmoothness = 5f;
        public bool showPercentage = true;

        [Header("Scooter Animation")]
        public float scooterBounceHeight = 20f;
        [Min(0.1f)] public float scooterBounceDuration = 1f;

        [Header("Map Animation")]
        [Min(0.1f)] public float mapRotationDuration = 8f;

        [Header("Shadow Animation")]
        public float shadowScaleMin = 0.9f;
        public float shadowScaleMax = 1.1f;

        [Header("Auto-Show on Scene Start")]
        public bool showOnSceneStart = true;

        // Kept for compatibility with the existing scene/prefab data.
        // Real loading progress is used instead of this value.
        public float initialLoadDuration = 1.25f;
        public bool autoLoadGameScene = true;
        public string gameSceneName = "menu";

        [Header("Optional: Rotating Icon")]
        public GameObject loadingIcon;
        public float iconRotationSpeed = 100f;

        [Header("Loading Tips")]
        public string[] loadingTips =
        {
            "Build recipes in the correct order for a perfect dish.",
            "Keep queue slots free so new ingredients have room.",
            "Check the customer order before sending an ingredient.",
            "A wrong ingredient can spoil the order - choose carefully.",
            "Fast, accurate serving keeps the kitchen moving."
        };

        [Header("Loading Copy")]
        [Min(0.5f)] public float tipRotationInterval = 2.75f;
        public string preparingMessage = "Preparing your kitchen...";
        public string readyMessage = "Kitchen ready!";

        [Header("Fade Settings")]
        public bool useFadeTransition = true;
        [Min(0f)] public float fadeInDuration = 0.35f;
        [Min(0f)] public float fadeOutDuration = 0.25f;
        public CanvasGroup loadingCanvasGroup;

        private bool isLoading;
        private bool isAnimating;
        private float displayedProgress;

        private Vector2 scooterStartPos;
        private Vector3 mapStartRotation;
        private Vector3 shadowStartScale;
        private Vector3 logoBaseScale;

        /// <summary>
        /// Routes a scene change through the dedicated Conveyor Chef loading scene.
        /// </summary>
        public static void LoadViaLoadingScreen(string targetSceneName)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogError("[LoadingScreen] Target scene name is empty.");
                return;
            }

            if (targetSceneName == LoadingSceneName)
            {
                Debug.LogWarning("[LoadingScreen] Ignoring request to load the loading scene through itself.");
                return;
            }

            pendingSceneName = targetSceneName;
            PlayerPrefs.SetString(PendingSceneKey, targetSceneName);
            PlayerPrefs.Save();

            SceneManager.LoadScene(LoadingSceneName);
        }

        private void Awake()
        {
            if (loadingScreenPanel != null)
            {
                loadingScreenPanel.SetActive(true);
            }

            if (loadingCanvasGroup == null && loadingScreenPanel != null)
            {
                loadingCanvasGroup = loadingScreenPanel.GetComponent<CanvasGroup>();
                if (loadingCanvasGroup == null)
                {
                    loadingCanvasGroup = loadingScreenPanel.AddComponent<CanvasGroup>();
                }
            }

            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.enabled = true;
                // The loading scene must cover the screen immediately. Starting at alpha 0
                // exposed the camera clear color (blue) while bootstrap initialization ran.
                loadingCanvasGroup.alpha = 1f;
                loadingCanvasGroup.interactable = false;
                loadingCanvasGroup.blocksRaycasts = true;
            }

            if (progressBarSlider != null)
            {
                progressBarSlider.minValue = 0f;
                progressBarSlider.maxValue = 1f;
                progressBarSlider.wholeNumbers = false;
                progressBarSlider.interactable = false;
                progressBarSlider.SetValueWithoutNotify(0f);
            }

            if (progressBarFill != null && progressBarFill.type == Image.Type.Filled)
            {
                progressBarFill.fillAmount = 0f;
            }

            if (progressText != null)
            {
                progressText.text = showPercentage ? "0%" : string.Empty;
            }

            if (loadingTipText != null)
            {
                loadingTipText.text = preparingMessage;
            }

            if (scooterTransform != null)
            {
                scooterStartPos = scooterTransform.anchoredPosition;
            }

            if (mapTransform != null)
            {
                mapStartRotation = mapTransform.localEulerAngles;
            }

            if (shadowTransform != null)
            {
                shadowStartScale = shadowTransform.localScale;
            }

            if (logoTransform != null)
            {
                logoBaseScale = logoTransform.localScale;
                logoTransform.localScale = logoBaseScale * logoSplashStartScale;
            }
        }

        private void Start()
        {
            if (showOnSceneStart && autoLoadGameScene)
            {
                StartCoroutine(BeginLoadingWhenReady());
            }
        }

        private IEnumerator BeginLoadingWhenReady()
        {
            // The dedicated loading scene now owns the screen immediately.
            // Do not hide it while waiting for the legacy bootstrap loader: doing so
            // produced a visible blue camera frame and made the loading screen appear twice.
            yield return null;
            yield return StartCoroutine(RunLoadingSequence());
        }

        private IEnumerator RunLoadingSequence()
        {
            if (isLoading)
            {
                yield break;
            }

            isLoading = true;
            displayedProgress = 0f;
            UpdateProgressUI(0f);

            string targetScene = ResolveTargetScene();

            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError("[LoadingScreen] Scene is not in Build Settings: " + targetScene);

                if (!string.IsNullOrWhiteSpace(gameSceneName) &&
                    targetScene != gameSceneName &&
                    Application.CanStreamedLevelBeLoaded(gameSceneName))
                {
                    targetScene = gameSceneName;
                }
                else
                {
                    if (loadingTipText != null)
                    {
                        loadingTipText.text = "Unable to load the next scene.";
                    }

                    isLoading = false;
                    yield break;
                }
            }

            if (loadingCanvasGroup != null && loadingCanvasGroup.alpha < 0.999f)
            {
                yield return StartCoroutine(FadeCanvas(loadingCanvasGroup.alpha, 1f, fadeInDuration));
            }

            StartAnimations();
            Coroutine tipRoutine = StartCoroutine(RotateTips());

            float startTime = Time.realtimeSinceStartup;
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);

            if (operation == null)
            {
                Debug.LogError("[LoadingScreen] Unity could not start loading scene: " + targetScene);
                StopAnimations();
                isLoading = false;
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f || Time.realtimeSinceStartup - startTime < minimumLoadTime)
            {
                float actualProgress = Mathf.Clamp01(operation.progress / 0.9f);
                displayedProgress = MoveDisplayedProgress(displayedProgress, actualProgress);
                UpdateProgressUI(displayedProgress);
                yield return null;
            }

            while (displayedProgress < 1f)
            {
                displayedProgress = Mathf.MoveTowards(
                    displayedProgress,
                    1f,
                    Mathf.Max(0.35f, progressSmoothness * 0.22f) * Time.unscaledDeltaTime);

                UpdateProgressUI(displayedProgress);
                yield return null;
            }

            UpdateProgressUI(1f);

            if (tipRoutine != null)
            {
                StopCoroutine(tipRoutine);
            }

            if (loadingTipText != null)
            {
                loadingTipText.text = readyMessage;
            }

            yield return new WaitForSecondsRealtime(0.15f);

            StopAnimations();
            operation.allowSceneActivation = true;
        }

        private string ResolveTargetScene()
        {
            string targetScene = pendingSceneName;

            if (string.IsNullOrWhiteSpace(targetScene) && PlayerPrefs.HasKey(PendingSceneKey))
            {
                targetScene = PlayerPrefs.GetString(PendingSceneKey);
            }

            if (string.IsNullOrWhiteSpace(targetScene))
            {
                targetScene = gameSceneName;
            }

            pendingSceneName = null;

            if (PlayerPrefs.HasKey(PendingSceneKey))
            {
                PlayerPrefs.DeleteKey(PendingSceneKey);
                PlayerPrefs.Save();
            }

            return targetScene;
        }

        private float MoveDisplayedProgress(float current, float target)
        {
            float speed = Mathf.Max(0.25f, progressSmoothness * 0.18f);
            return Mathf.MoveTowards(current, target, speed * Time.unscaledDeltaTime);
        }

        private void UpdateProgressUI(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (progressBarSlider != null)
            {
                progressBarSlider.SetValueWithoutNotify(progress);
            }

            // The existing Conveyor Chef bar uses a Slider with a sliced fill image.
            // fillAmount is only applied when an alternate Filled image is assigned.
            if (progressBarFill != null && progressBarFill.type == Image.Type.Filled)
            {
                progressBarFill.fillAmount = progress;
            }

            if (progressText != null)
            {
                progressText.text = showPercentage
                    ? "LOADING... " + Mathf.RoundToInt(progress * 100f) + "%"
                    : "LOADING...";
            }
        }

        private IEnumerator RotateTips()
        {
            if (loadingTipText == null)
            {
                yield break;
            }

            if (loadingTips == null || loadingTips.Length == 0)
            {
                loadingTipText.text = preparingMessage;
                yield break;
            }

            int index = Random.Range(0, loadingTips.Length);

            while (isLoading)
            {
                loadingTipText.text = "TIP  •  " + loadingTips[index];
                index = (index + 1) % loadingTips.Length;
                yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, tipRotationInterval));
            }
        }

        private void StartAnimations()
        {
            if (isAnimating)
            {
                return;
            }

            isAnimating = true;

            if (scooterTransform != null)
            {
                StartCoroutine(AnimateScooterBounce());
            }

            if (mapTransform != null)
            {
                StartCoroutine(AnimateMapRotation());
            }

            if (shadowTransform != null)
            {
                StartCoroutine(AnimateShadowPulse());
            }

            if (logoTransform != null)
            {
                StartCoroutine(AnimateLogoSplash());
            }
        }

        private void StopAnimations()
        {
            isAnimating = false;

            if (scooterTransform != null)
            {
                scooterTransform.anchoredPosition = scooterStartPos;
            }

            if (mapTransform != null)
            {
                mapTransform.localEulerAngles = mapStartRotation;
            }

            if (shadowTransform != null)
            {
                shadowTransform.localScale = shadowStartScale;
            }

            if (logoTransform != null)
            {
                logoTransform.localScale = logoBaseScale;
            }
        }

        private IEnumerator AnimateLogoSplash()
        {
            if (logoTransform == null)
                yield break;

            float duration = Mathf.Max(0.1f, logoSplashDuration);
            float firstPhase = duration * 0.65f;
            float secondPhase = duration - firstPhase;
            float elapsed = 0f;

            Vector3 start = logoBaseScale * logoSplashStartScale;
            Vector3 overshoot = logoBaseScale * logoSplashOvershoot;

            logoTransform.localScale = start;

            while (elapsed < firstPhase)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / firstPhase);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                logoTransform.localScale = Vector3.LerpUnclamped(start, overshoot, eased);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < secondPhase)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, secondPhase));
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                logoTransform.localScale = Vector3.LerpUnclamped(overshoot, logoBaseScale, eased);
                yield return null;
            }

            logoTransform.localScale = logoBaseScale;
        }

        private IEnumerator AnimateScooterBounce()
        {
            while (isAnimating && scooterTransform != null)
            {
                float elapsed = 0f;
                float duration = Mathf.Max(0.1f, scooterBounceDuration);

                while (elapsed < duration && isAnimating)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float normalized = Mathf.Clamp01(elapsed / duration);
                    float wave = Mathf.Sin(normalized * Mathf.PI);
                    scooterTransform.anchoredPosition = scooterStartPos + Vector2.up * (wave * scooterBounceHeight);
                    yield return null;
                }
            }
        }

        private IEnumerator AnimateMapRotation()
        {
            float angle = mapStartRotation.z;

            while (isAnimating && mapTransform != null)
            {
                angle -= 360f / Mathf.Max(0.1f, mapRotationDuration) * Time.unscaledDeltaTime;
                mapTransform.localEulerAngles = new Vector3(mapStartRotation.x, mapStartRotation.y, angle);
                yield return null;
            }
        }

        private IEnumerator AnimateShadowPulse()
        {
            float elapsed = 0f;

            while (isAnimating && shadowTransform != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float cycle = (Mathf.Sin(elapsed * Mathf.PI * 2f / Mathf.Max(0.1f, scooterBounceDuration)) + 1f) * 0.5f;
                float scale = Mathf.Lerp(shadowScaleMax, shadowScaleMin, cycle);
                shadowTransform.localScale = shadowStartScale * scale;
                yield return null;
            }
        }

        private void Update()
        {
            if (isLoading && loadingIcon != null && loadingIcon.activeInHierarchy)
            {
                loadingIcon.transform.Rotate(0f, 0f, -iconRotationSpeed * Time.unscaledDeltaTime);
            }
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            if (loadingCanvasGroup == null)
            {
                yield break;
            }

            if (!useFadeTransition || duration <= 0f)
            {
                loadingCanvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            loadingCanvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            loadingCanvasGroup.alpha = to;
        }

        // Backward-compatible inspector/API methods.
        public void LoadScene(string sceneName)
        {
            LoadViaLoadingScreen(sceneName);
        }

        public void LoadScene(int sceneIndex)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                LoadViaLoadingScreen(sceneName);
            }
        }

        public void ShowTransitionScreen(float duration)
        {
            StartCoroutine(ShowTransitionRoutine(duration));
        }

        private IEnumerator ShowTransitionRoutine(float duration)
        {
            isLoading = true;
            displayedProgress = 0f;

            yield return StartCoroutine(FadeCanvas(loadingCanvasGroup != null ? loadingCanvasGroup.alpha : 1f, 1f, fadeInDuration));
            StartAnimations();

            float elapsed = 0f;
            duration = Mathf.Max(0.1f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                displayedProgress = Mathf.Clamp01(elapsed / duration);
                UpdateProgressUI(displayedProgress);
                yield return null;
            }

            StopAnimations();
            isLoading = false;
        }

        public void ManualShowLoading()
        {
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 1f;
                loadingCanvasGroup.blocksRaycasts = true;
            }

            StartAnimations();
        }

        public void ManualHideLoading()
        {
            StopAnimations();

            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                loadingCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDestroy()
        {
            StopAnimations();
        }
    }
}
