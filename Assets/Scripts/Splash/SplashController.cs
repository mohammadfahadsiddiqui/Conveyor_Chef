using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Controls the Conveyor Chef: Food Rush splash / loading screen.
/// Features:
/// - Smooth cinematic fade-in from black
/// - Realistic progressive loading bar with glossy candy fill
/// - Traveling sparkling star at the head of the progress bar
/// - Percentage counter and dynamic culinary tips
/// - Organic idle breathing on the full splash background
/// - Smooth cinematic fade-out to black
/// - Transitions to the "menu" scene
/// </summary>
public class SplashController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private RectTransform backgroundTransform;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private RectTransform progressBarContainer;
    [SerializeField] private RectTransform sparkleIcon;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private TextMeshProUGUI versionText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float loadDuration = 2.8f;
    [SerializeField] private float holdDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "menu";

    [Header("Background Animation")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingScale = 0.015f;
    [SerializeField] private float breathingSpeed = 1.0f;

    [Header("Loading Tips")]
    [SerializeField] private string[] culinaryTips = new string[]
    {
        "Prepping the kitchen...",
        "Firing up the ovens...",
        "Checking conveyor belts...",
        "Baking fresh burger buns...",
        "Glazing strawberry donuts...",
        "Plating delicious orders...",
        "Ready to serve!"
    };

    private bool isFinished = false;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float barWidth = 700f;

    private void Start()
    {
        // Calculate fill bar travel width
        if (progressBarContainer != null)
        {
            barWidth = Mathf.Max(100f, progressBarContainer.rect.width - 24f);
        }

        // Initialize UI states
        if (progressBarFill != null)
        {
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressBarFill.fillAmount = 0f;
        }

        if (sparkleIcon != null)
        {
            sparkleIcon.anchoredPosition = new Vector2(0f, 0f);
        }

        if (progressPercentText != null)
        {
            progressPercentText.text = "Loading... 0%";
        }

        if (tipText != null && culinaryTips != null && culinaryTips.Length > 0)
        {
            tipText.text = culinaryTips[0];
        }

        // Ensure fade overlay starts fully black
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        // Begin splash sequence
        StartCoroutine(ExecuteSplashSequence());
    }

    private void Update()
    {
        // Idle breathing motion on background illustration
        if (enableBreathing && backgroundTransform != null)
        {
            float scale = 1.0f + Mathf.Sin(Time.time * breathingSpeed) * breathingScale;
            backgroundTransform.localScale = new Vector3(scale, scale, 1.0f);
        }

        // Spin and pulse the sparkle icon at the head of the progress bar
        if (sparkleIcon != null && !isFinished)
        {
            sparkleIcon.Rotate(0f, 0f, -150f * Time.deltaTime);
            float pulse = 1.0f + Mathf.Sin(Time.time * 6f) * 0.15f;
            sparkleIcon.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    private IEnumerator ExecuteSplashSequence()
    {
        // Phase 1: Fade In from black
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // Phase 2: Simulated Game Loading with Organic Easing
        yield return StartCoroutine(SimulateLoadingProcess());

        // Phase 3: Brief completion hold
        if (tipText != null)
        {
            tipText.text = "Ready! Let's Cook!";
        }
        if (progressPercentText != null)
        {
            progressPercentText.text = "100%";
        }

        yield return new WaitForSeconds(holdDuration);

        // Phase 4: Fade Out to black
        isFinished = true;
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // Phase 5: Load Menu Scene
        LoadNextScene();
    }

    private IEnumerator SimulateLoadingProcess()
    {
        float elapsed = 0f;
        int lastTipIndex = 0;

        while (elapsed < loadDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / loadDuration);

            // Realistic multi-stage loading curve:
            // 0 -> 0.3 (fast initial burst), 0.3 -> 0.85 (smooth asset load), 0.85 -> 1.0 (final check)
            float eased;
            if (normalizedTime < 0.25f)
            {
                eased = Mathf.Lerp(0f, 0.35f, normalizedTime / 0.25f);
            }
            else if (normalizedTime < 0.8f)
            {
                float t = (normalizedTime - 0.25f) / 0.55f;
                eased = Mathf.Lerp(0.35f, 0.88f, Mathf.SmoothStep(0f, 1f, t));
            }
            else
            {
                float t = (normalizedTime - 0.8f) / 0.2f;
                eased = Mathf.Lerp(0.88f, 1.0f, t * t);
            }

            targetProgress = eased;
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 1.5f);

            // Update Progress Bar
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = currentProgress;
            }

            // Move Sparkle icon to current progress position
            if (sparkleIcon != null)
            {
                sparkleIcon.anchoredPosition = new Vector2(currentProgress * barWidth, 0f);
            }

            // Update Percent Text
            if (progressPercentText != null)
            {
                int pct = Mathf.RoundToInt(currentProgress * 100f);
                progressPercentText.text = $"Loading... {pct}%";
            }

            // Cycle Tips dynamically as milestones are passed
            if (tipText != null && culinaryTips != null && culinaryTips.Length > 0)
            {
                int tipIndex = Mathf.Clamp(
                    Mathf.FloorToInt(currentProgress * (culinaryTips.Length - 1)),
                    0,
                    culinaryTips.Length - 1
                );

                if (tipIndex != lastTipIndex)
                {
                    lastTipIndex = tipIndex;
                    tipText.text = culinaryTips[tipIndex];
                }
            }

            yield return null;
        }

        // Final 100% snap
        currentProgress = 1f;
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 1f;
        }
        if (sparkleIcon != null)
        {
            sparkleIcon.anchoredPosition = new Vector2(barWidth, 0f);
        }
        if (progressPercentText != null)
        {
            progressPercentText.text = "Loading... 100%";
        }
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            c.a = Mathf.Lerp(fromAlpha, toAlpha, smoothT);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = toAlpha;
        fadeOverlay.color = c;
    }

    private void LoadNextScene()
    {
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == nextSceneName)
            {
                sceneExists = true;
                break;
            }
        }

        if (sceneExists)
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning($"[SplashController] Scene '{nextSceneName}' not found in Build Settings.");
        }
    }
}
