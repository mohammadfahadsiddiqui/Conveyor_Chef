using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// Behaviour-only controller for the real, serialized UI stored in menu.unity.
    /// It does not create, move, resize or replace visual UI objects.
    /// Designers can freely edit the complete menu hierarchy directly in Unity.
    /// </summary>
    public sealed class MainMenuSceneController : MonoBehaviour
    {
        private const string DiamondsKey = "CC_Diamonds"; // legacy PlayerPrefs key
        private const string DiamondCurrencyMigrationKey = "CC_DiamondCurrencyMigrated";

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
            UIEventSystemRuntime.UseCurrentSceneEventSystem();
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
            EnhancedLoadingScreen.LoadViaLoadingScreen("WorldMap");
        }

        // Each menu button opens its panel (see Scripts/UI/MenuPanels).
        private void OpenStory() => MenuPanel.Show<StoryPanel>();

        private void OpenChallenges() => MenuPanel.Show<ChallengesPanel>();

        private void OpenCollection() => MenuPanel.Show<CollectionPanel>();

        private void OpenAchievements() => MenuPanel.Show<AchievementsPanel>();

        private void OpenLeaderboard() => MenuPanel.Show<LeaderboardPanel>();

        private void OpenShop() => MenuPanel.Show<ShopPanel>();

        private void OpenCustomize() => MenuPanel.Show<CustomizePanel>();

        // Every settings button in the game opens the shared Settings panel.
        private void OpenSettings()
        {
            ConveyorSettingsPanel.Show();
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

            if (diamondText != null)
                diamondText.text = GetDiamondAmount().ToString("N0");
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            RefreshHUD();
        }

        private static int GetDiamondAmount()
        {
            try
            {
                if (!PlayerPrefs.HasKey(DiamondCurrencyMigrationKey))
                {
                    int legacyAmount = PlayerPrefs.GetInt(DiamondsKey, 50);
                    PlayerPrefs.SetInt(DiamondCurrencyMigrationKey, 1);
                    PlayerPrefs.Save();
                    CurrenciesController.Set(CurrencyType.Diamonds, legacyAmount);
                }

                return CurrenciesController.Get(CurrencyType.Diamonds);
            }
            catch
            {
                return PlayerPrefs.GetInt(DiamondsKey, 50);
            }
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
