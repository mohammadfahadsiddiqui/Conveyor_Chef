
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // If using TextMeshPro for text

public class EnhancedLoadingScreen : MonoBehaviour
{
    public static EnhancedLoadingScreen Instance;
    
    [Header("Loading Screen UI")]
    public GameObject loadingScreenPanel;
    public Image backgroundImage;
    public Image progressBarFill; // Use Image.fillAmount for smooth filling
    public Slider progressBarSlider; // OR use a Slider component
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI loadingTipText;
    
    [Header("Animation Settings")]
    public float minimumLoadTime = 2f;
    public float progressSmoothness = 5f; // Higher = smoother but slower
    public bool showPercentage = true;
    
    [Header("Auto-Show on Scene Start")]
    public bool showOnSceneStart = true; // NEW: Auto-show when scene loads
    public float initialLoadDuration = 3f; // Duration for initial loading (increased from 2f)
    public bool autoLoadGameScene = true; // NEW: Automatically load game scene after loading
    public string gameSceneName = "Game"; // Name of the game scene to load
    
    [Header("Optional: Rotating Icon")]
    public GameObject loadingIcon; // Optional spinning icon/logo
    public float iconRotationSpeed = 100f;
    
    [Header("Loading Tips")]
    public string[] loadingTips = new string[]
    {
        "Match 3 or more items to complete orders...",
        "Drag items between boxes to organize them...",
        "Use boosters to help clear difficult levels...",
        "Complete all orders before running out of space...",
        "The conveyor belt never stops!"
    };
    
    [Header("Fade Settings")]
    public bool useFadeTransition = true;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public CanvasGroup loadingCanvasGroup;
    
    private bool isLoading = false;
    private float targetProgress = 0f;
    private float currentProgress = 0f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Setup canvas group for fade if not assigned
        if (useFadeTransition && loadingCanvasGroup == null && loadingScreenPanel != null)
        {
            loadingCanvasGroup = loadingScreenPanel.GetComponent<CanvasGroup>();
            if (loadingCanvasGroup == null)
            {
                loadingCanvasGroup = loadingScreenPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Keep panel disabled initially - we'll enable it when needed
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }
    }
    
    void Start()
    {
        // Auto-show loading screen on scene start if enabled
        if (showOnSceneStart)
        {
            StartCoroutine(InitialLoadingSequence());
        }
    }
    
    void Update()
    {
        // Smooth progress bar animation
        if (isLoading)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressSmoothness);
            
            // Update progress bar (support both Image fill and Slider)
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = currentProgress;
            }
            
            if (progressBarSlider != null)
            {
                progressBarSlider.value = currentProgress;
            }
            
            // Update percentage text
            if (showPercentage && progressText != null)
            {
                int percentage = Mathf.RoundToInt(currentProgress * 100f);
                progressText.text = percentage + "%";
            }
        }
        
        // Rotate loading icon
        if (isLoading && loadingIcon != null)
        {
            loadingIcon.transform.Rotate(0f, 0f, -iconRotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// NEW: Show loading screen when scene first starts
    /// </summary>
    IEnumerator InitialLoadingSequence()
    {
        isLoading = true;
        
        // Show loading screen
        yield return StartCoroutine(ShowLoadingScreen());
        
        // Reset progress
        targetProgress = 0f;
        currentProgress = 0f;
        
        // Show random tip
        ShowRandomTip();
        
        // Simulate loading
        float elapsed = 0f;
        while (elapsed < initialLoadDuration)
        {
            elapsed += Time.deltaTime;
            targetProgress = elapsed / initialLoadDuration;
            yield return null;
        }
        
        // Complete progress
        targetProgress = 1f;
        
        // Wait for smooth progress to catch up
        while (currentProgress < 0.99f)
        {
            yield return null;
        }
        
        // Brief pause at 100%
        yield return new WaitForSeconds(0.3f);
        
        // Hide loading screen
        yield return StartCoroutine(HideLoadingScreen());
        
        isLoading = false;
        
        // Notify that loading is complete (other scripts can listen for this)
        OnLoadingComplete();
    }
    
    /// <summary>
    /// Called when initial loading completes - override or add listeners
    /// </summary>
    void OnLoadingComplete()
    {
        Debug.Log("Loading screen complete - game ready!");
        
        // Automatically load the game scene after loading screen
        if (autoLoadGameScene && !string.IsNullOrEmpty(gameSceneName))
        {
            Debug.Log($"Auto-loading game scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        
        // OR start the game in the current scene if not auto-loading:
        // else if (GameStateManager.Instance != null)
        // {
        //     // GameStateManager.Instance.SetState(GameState.Playing);
        // }
    }
    
    /// <summary>
    /// Load scene with loading screen
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneWithLoadingScreen(sceneName));
        }
    }
    
    /// <summary>
    /// Load scene by index with loading screen
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneWithLoadingScreen(sceneIndex));
        }
    }
    
    IEnumerator LoadSceneWithLoadingScreen(string sceneName)
    {
        isLoading = true;
        
        // Show loading screen with fade
        yield return StartCoroutine(ShowLoadingScreen());
        
        // Reset progress
        targetProgress = 0f;
        currentProgress = 0f;
        
        // Show random tip
        ShowRandomTip();
        
        // Track time
        float startTime = Time.realtimeSinceStartup;
        
        // Start loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // Update progress
        while (!asyncLoad.isDone)
        {
            // Scene loading is 0-0.9
            targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Check if loading complete
            if (asyncLoad.progress >= 0.9f)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                
                if (elapsed >= minimumLoadTime)
                {
                    // Complete progress
                    targetProgress = 1f;
                    
                    // Wait for smooth progress to catch up
                    while (currentProgress < 0.99f)
                    {
                        yield return null;
                    }
                    
                    yield return new WaitForSeconds(0.3f);
                    
                    // Activate scene
                    asyncLoad.allowSceneActivation = true;
                }
            }
            
            yield return null;
        }
        
        // Hide loading screen with fade
        yield return StartCoroutine(HideLoadingScreen());
        
        isLoading = false;
    }
    
    IEnumerator LoadSceneWithLoadingScreen(int sceneIndex)
    {
        isLoading = true;
        
        yield return StartCoroutine(ShowLoadingScreen());
        
        targetProgress = 0f;
        currentProgress = 0f;
        
        ShowRandomTip();
        
        float startTime = Time.realtimeSinceStartup;
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false;
        
        while (!asyncLoad.isDone)
        {
            targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (asyncLoad.progress >= 0.9f)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                
                if (elapsed >= minimumLoadTime)
                {
                    targetProgress = 1f;
                    
                    while (currentProgress < 0.99f)
                    {
                        yield return null;
                    }
                    
                    yield return new WaitForSeconds(0.3f);
                    asyncLoad.allowSceneActivation = true;
                }
            }
            
            yield return null;
        }
        
        yield return StartCoroutine(HideLoadingScreen());
        isLoading = false;
    }
    
    IEnumerator ShowLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }
        
        if (useFadeTransition && loadingCanvasGroup != null)
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            loadingCanvasGroup.alpha = 1f;
        }
    }
    
    IEnumerator HideLoadingScreen()
    {
        if (useFadeTransition && loadingCanvasGroup != null)
        {
            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            loadingCanvasGroup.alpha = 0f;
        }
        
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }
    }
    
    void ShowRandomTip()
    {
        if (loadingTipText != null && loadingTips.Length > 0)
        {
            int randomIndex = Random.Range(0, loadingTips.Length);
            loadingTipText.text = loadingTips[randomIndex];
        }
    }
    
    /// <summary>
    /// Show fake loading for transitions (level start, etc.)
    /// </summary>
    public void ShowTransitionScreen(float duration)
    {
        StartCoroutine(TransitionScreen(duration));
    }
    
    IEnumerator TransitionScreen(float duration)
    {
        isLoading = true;
        
        yield return StartCoroutine(ShowLoadingScreen());
        
        ShowRandomTip();
        
        targetProgress = 0f;
        currentProgress = 0f;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            targetProgress = elapsed / duration;
            yield return null;
        }
        
        targetProgress = 1f;
        while (currentProgress < 0.99f)
        {
            yield return null;
        }
        
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(HideLoadingScreen());
        
        isLoading = false;
    }
    
    /// <summary>
    /// Manually show loading screen (for testing)
    /// </summary>
    public void ManualShowLoading()
    {
        StartCoroutine(ShowLoadingScreen());
    }
    
    /// <summary>
    /// Manually hide loading screen (for testing)
    /// </summary>
    public void ManualHideLoading()
    {
        StartCoroutine(HideLoadingScreen());
    }
}