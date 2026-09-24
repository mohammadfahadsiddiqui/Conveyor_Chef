using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Watermelon.BusStop;
using Watermelon.IAPStore;
using Watermelon.SkinStore;

namespace Watermelon
{
    /// <summary>
    /// Behaviour-only controller for the real, serialized UI stored in menu.unity.
    /// It does not create, move, resize or replace visual UI objects.
    /// Designers can freely edit the complete menu hierarchy directly in Unity.
    /// </summary>
    public sealed class MainMenuSceneController : MonoBehaviour
    {
        private const string DiamondsKey = "CC_Diamonds";
        private const string LocalBestScoreKey = "CC_LocalBestScore";

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button storyButton;
        [SerializeField] private Button challengesButton;
        [SerializeField] private Button customizeButton;
        [SerializeField] private Button settingsButton;

        [Header("Bottom Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button leaderboardButton;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI starText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI diamondText;

        [Header("Modal")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI modalTitle;
        [SerializeField] private TextMeshProUGUI modalBody;
        [SerializeField] private Button closeModalButton;

        [Header("Settings")]
        [SerializeField] private GameObject settingsControls;
        [SerializeField] private Button soundButton;
        [SerializeField] private TextMeshProUGUI soundButtonText;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TextMeshProUGUI vibrationButtonText;

        private Type externalPageType;
        private bool externalPageSubscribed;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureSaveControllerReady();

            Wire(playButton, PlayGame, true);
            Wire(storyButton, OpenStory);
            Wire(challengesButton, OpenChallenges);
            Wire(customizeButton, OpenCustomize);
            Wire(settingsButton, OpenSettings);

            Wire(shopButton, OpenShop);
            Wire(collectionButton, OpenCollection);
            Wire(achievementsButton, OpenAchievements);
            Wire(leaderboardButton, OpenLeaderboard);

            Wire(closeModalButton, HideModal);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void Start()
        {
            RefreshHUD();

            CurrenciesController.InvokeOrSubcrtibe(() =>
            {
                RefreshHUD();
                CurrenciesController.SubscribeGlobalCallback(OnCurrencyChanged);
            });
        }

        private void OnDestroy()
        {
            try
            {
                CurrenciesController.UnsubscribeGlobalCallback(OnCurrencyChanged);
            }
            catch
            {
                // Currency systems may already be shutting down.
            }

            UnsubscribeExternalPage();
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action, bool pulse = false)
        {
            if (button == null)
                return;

            button.interactable = true;

            Graphic graphic = button.targetGraphic;
            if (graphic == null)
                graphic = button.GetComponent<Graphic>();

            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            ProfessionalMainMenuButtonFX fx = button.GetComponent<ProfessionalMainMenuButtonFX>();
            if (fx == null)
                fx = button.gameObject.AddComponent<ProfessionalMainMenuButtonFX>();

            fx.Configure(pulse, pulse);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null && EventSystem.current.gameObject.activeInHierarchy)
                return;

            EventSystem existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null)
                existing.gameObject.SetActive(true);
        }

        private static void EnsureSaveControllerReady()
        {
            if (SaveController.IsSaveLoaded)
                return;

            try
            {
                SaveController.Initialise(useAutoSave: false);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MainMenu] Failed to initialise SaveController: " + ex.Message);
            }
        }

        private void PlayGame()
        {
            PlayClick();
            EnhancedLoadingScreen.LoadViaLoadingScreen("LevelSelection");
        }

        private void OpenStory()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out int bestLevel);

            ShowModal(
                "STORY",
                "CONVEYOR CHEF: FOOD RUSH\n\n" +
                "Build your kitchen journey one order at a time. Complete levels, discover recipes and master increasingly busy food-rush challenges.\n\n" +
                $"Levels completed: {completed}\n" +
                $"Best level reached: {bestLevel}\n" +
                $"Stars collected: {stars}\n\n" +
                "Press PLAY to continue through the level map.");
        }

        private void OpenChallenges()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out _);

            ShowModal(
                "CHALLENGES",
                BuildProgressLine("FIRST SERVICE", completed, 1) + "\n\n" +
                BuildProgressLine("RISING CHEF", completed, 3) + "\n\n" +
                BuildProgressLine("STAR HUNTER", stars, 9) + "\n\n" +
                BuildProgressLine("KITCHEN MASTER", completed, 10));
        }

        private void OpenCollection()
        {
            PlayClick();
            GetProgress(out int completed, out _, out _);

            string[] recipes = { "Burger", "Croissant", "Chocolate Donut", "Cake", "Pizza", "Sushi" };
            int unlocked = Mathf.Clamp(completed + 1, 1, recipes.Length);

            string body = $"RECIPES DISCOVERED: {unlocked}/{recipes.Length}\n\n";
            for (int i = 0; i < recipes.Length; i++)
                body += (i < unlocked ? "[DONE]  " : "[LOCKED]  ") + recipes[i] + (i == recipes.Length - 1 ? "" : "\n");

            ShowModal("COLLECTION", body);
        }

        private void OpenAchievements()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out _);

            ShowModal(
                "ACHIEVEMENTS",
                AchievementLine("FIRST ORDER", completed >= 1, "Complete your first level") + "\n\n" +
                AchievementLine("RISING CHEF", completed >= 3, "Complete 3 levels") + "\n\n" +
                AchievementLine("STAR COLLECTOR", stars >= 9, "Collect 9 stars") + "\n\n" +
                AchievementLine("KITCHEN VETERAN", completed >= 10, "Complete 10 levels"));
        }

        private void OpenLeaderboard()
        {
            PlayClick();
            GetProgress(out int completed, out int stars, out int bestLevel);

            int score = stars * 100 + completed * 50;
            int storedBest = PlayerPrefs.GetInt(LocalBestScoreKey, 0);

            if (score > storedBest)
            {
                storedBest = score;
                PlayerPrefs.SetInt(LocalBestScoreKey, storedBest);
                PlayerPrefs.Save();
            }

            ShowModal(
                "LEADERBOARD",
                $"Chef Score: {score:N0}\n" +
                $"Personal Best: {storedBest:N0}\n" +
                $"Stars: {stars}\n" +
                $"Levels Completed: {completed}\n" +
                $"Best Level: {bestLevel}");
        }

        private void OpenSettings()
        {
            PlayClick();
            ShowModal("SETTINGS", "Tune your kitchen experience.");

            if (settingsControls != null)
                settingsControls.SetActive(true);

            UpdateSettingsLabels();
        }

        private void OpenShop()
        {
            PlayClick();

            try
            {
                UIIAPStore page = UIController.GetPage<UIIAPStore>();
                if (page == null)
                    throw new InvalidOperationException("IAP store page is not registered.");

                OpenExternalPage(typeof(UIIAPStore), () => UIController.ShowPage<UIIAPStore>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MainMenu] Store unavailable: " + ex.Message);
                ShowModal("SHOP", "The store is currently unavailable in this build.");
            }
        }

        private void OpenCustomize()
        {
            PlayClick();

            try
            {
                UISkinStore page = UIController.GetPage<UISkinStore>();
                if (page == null)
                    throw new InvalidOperationException("Skin store page is not registered.");

                OpenExternalPage(typeof(UISkinStore), SkinStoreController.OpenStore);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MainMenu] Customize unavailable: " + ex.Message);
                ShowModal("CUSTOMIZE", "Character customization is currently unavailable in this build.");
            }
        }

        private void OpenExternalPage(Type pageType, Action opener)
        {
            HideModal();
            externalPageType = pageType;

            if (!externalPageSubscribed)
            {
                UIController.OnPageClosedEvent += OnExternalPageClosed;
                externalPageSubscribed = true;
            }

            gameObject.SetActive(false);
            opener.Invoke();
        }

        private void OnExternalPageClosed(UIPage page, Type pageType)
        {
            if (externalPageType == null || pageType != externalPageType)
                return;

            externalPageType = null;
            gameObject.SetActive(true);
            RefreshHUD();
            UnsubscribeExternalPage();
        }

        private void UnsubscribeExternalPage()
        {
            if (!externalPageSubscribed)
                return;

            UIController.OnPageClosedEvent -= OnExternalPageClosed;
            externalPageSubscribed = false;
        }

        private void ToggleSound()
        {
            bool enabled = AudioController.GetVolume() > 0.001f;
            AudioController.SetVolume(enabled ? 0f : 1f);
            UpdateSettingsLabels();
            PlayClick();
        }

        private void ToggleVibration()
        {
            AudioController.SetVibrationState(!AudioController.IsVibrationEnabled());
            UpdateSettingsLabels();
            PlayClick();
        }

        private void UpdateSettingsLabels()
        {
            if (soundButtonText != null)
                soundButtonText.text = "SOUND: " + (AudioController.GetVolume() > 0.001f ? "ON" : "OFF");

            if (vibrationButtonText != null)
                vibrationButtonText.text = "VIBRATION: " + (AudioController.IsVibrationEnabled() ? "ON" : "OFF");
        }

        private void ShowModal(string title, string body)
        {
            if (modalTitle != null)
                modalTitle.text = title;

            if (modalBody != null)
                modalBody.text = body;

            if (settingsControls != null)
                settingsControls.SetActive(false);

            if (modalRoot != null)
                modalRoot.SetActive(true);
        }

        private void HideModal()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void RefreshHUD()
        {
            GetProgress(out _, out int stars, out _);

            if (starText != null)
                starText.text = stars.ToString();

            if (coinText != null)
            {
                try
                {
                    coinText.text = CurrenciesController.Get(CurrencyType.Coins).ToString("N0");
                }
                catch
                {
                    coinText.text = "0";
                }
            }

            if (!PlayerPrefs.HasKey(DiamondsKey))
            {
                PlayerPrefs.SetInt(DiamondsKey, 50);
                PlayerPrefs.Save();
            }

            if (diamondText != null)
                diamondText.text = PlayerPrefs.GetInt(DiamondsKey, 50).ToString("N0");
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            RefreshHUD();
        }

        private static void GetProgress(out int completed, out int stars, out int bestLevel)
        {
            completed = 0;
            stars = 0;
            bestLevel = 0;

            if (!SaveController.IsSaveLoaded)
                return;

            try
            {
                LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
                if (save == null || save.levelProgress == null)
                    return;

                for (int i = 0; i < save.levelProgress.Count; i++)
                {
                    LevelProgressData progress = save.levelProgress[i];
                    if (progress == null)
                        continue;

                    if (progress.isCompleted)
                    {
                        completed++;
                        bestLevel = Mathf.Max(bestLevel, progress.levelIndex + 1);
                    }

                    stars += Mathf.Max(0, progress.starsEarned);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MainMenu] Could not read level progress: " + ex.Message);
            }
        }

        private static string BuildProgressLine(string label, int current, int target)
        {
            int shown = Mathf.Min(current, target);
            return (current >= target ? "[DONE] " : "- ") + label + $"   {shown}/{target}";
        }

        private static string AchievementLine(string title, bool unlocked, string description)
        {
            return (unlocked ? "[DONE]  " : "[LOCKED]  ") + title + "\n    " + description;
        }

        private static void PlayClick()
        {
            try
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
            }
            catch
            {
                // Keep menu navigation usable if audio has not initialized yet.
            }
        }
    }
}
