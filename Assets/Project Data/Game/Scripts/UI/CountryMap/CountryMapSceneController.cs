using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Runtime behaviour for the serialized CountryMap.unity scene.
    /// The scene owns all visual layout. This controller only updates state,
    /// text, progress, settings and navigation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountryMapSceneController : MonoBehaviour
    {
        public const int CountriesPerContinent = 5;
        public const int LevelsPerCountry = 3;
        public const int LevelsPerContinent = CountriesPerContinent * LevelsPerCountry;

        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const int AsiaContinentIndex = 4;

        private static readonly string[] ContinentNames =
        {
            "North America", "South America", "Europe", "Africa", "Asia", "Australia / Oceania"
        };

        [Header("Countries")]
        [SerializeField] private CountryMapCountryNode[] countryNodes;

        [Header("Header")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button coinPlusButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TextMeshProUGUI guideText;

        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI progressTitleText;
        [SerializeField] private TextMeshProUGUI progressValueText;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TextMeshProUGUI soundText;
        [SerializeField] private TextMeshProUGUI vibrationText;

        [Header("Current Art Pack")]
        [Tooltip("The generated art pack currently contains the complete Asia country map.")]
        [SerializeField] private int authoredContinentIndex = AsiaContinentIndex;
        [SerializeField] private GameObject unsupportedContinentPanel;
        [SerializeField] private TextMeshProUGUI unsupportedContinentText;

        private int selectedContinent;
        private int selectedCountry;
        private bool currencySubscribed;

        private void Awake()
        {
            UIEventSystemRuntime.UseCurrentSceneEventSystem();
            EnsureSaveControllerReady();

            Wire(backButton, BackToWorldMap);
            Wire(settingsButton, OpenSettings);
            Wire(coinPlusButton, CoinPlusPressed);
            Wire(closeSettingsButton, CloseSettings);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);

            if (countryNodes != null)
            {
                for (int i = 0; i < countryNodes.Length; i++)
                {
                    CountryMapCountryNode node = countryNodes[i];
                    if (node != null)
                        node.Bind(this);
                }
            }

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void Start()
        {
            selectedContinent = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedContinentKey, authoredContinentIndex),
                0,
                ContinentNames.Length - 1);

            selectedCountry = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedCountryKey, 0),
                0,
                CountriesPerContinent - 1);

            ApplyContinentHeader();
            RefreshHUD();
            RefreshCountryProgress();
            RefreshSettingsLabels();

            CurrenciesController.InvokeOrSubcrtibe(() =>
            {
                RefreshHUD();

                if (!currencySubscribed)
                {
                    CurrenciesController.SubscribeGlobalCallback(OnCurrencyChanged);
                    currencySubscribed = true;
                }
            });
        }

        private void OnDestroy()
        {
            if (currencySubscribed)
            {
                CurrenciesController.UnsubscribeGlobalCallback(OnCurrencyChanged);
                currencySubscribed = false;
            }
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            if (currency != null && currency.CurrencyType == CurrencyType.Coins)
                RefreshHUD();
        }

        private void ApplyContinentHeader()
        {
            string continent = ContinentNames[Mathf.Clamp(selectedContinent, 0, ContinentNames.Length - 1)];

            if (titleText != null)
                titleText.text = continent.ToUpperInvariant();

            if (subtitleText != null)
                subtitleText.text = "CULINARY JOURNEY";

            if (infoText != null)
            {
                infoText.text = selectedContinent == AsiaContinentIndex
                    ? "Explore amazing cuisines\nand cultures across Asia!"
                    : "Explore the countries and\nculinary stops of " + continent + ".";
            }

            if (guideText != null)
                guideText.text = "Complete countries to unlock new recipes and levels!";

            bool artSupported = selectedContinent == authoredContinentIndex;
            if (unsupportedContinentPanel != null)
                unsupportedContinentPanel.SetActive(!artSupported);

            if (!artSupported && unsupportedContinentText != null)
            {
                unsupportedContinentText.text =
                    continent.ToUpperInvariant() + " COUNTRY ART\nIS NOT INSTALLED YET\n\n" +
                    "The reusable Country Map system is ready.\n" +
                    "The current generated visual pack is ASIA.";
            }
        }

        private void RefreshHUD()
        {
            if (coinText == null)
                return;

            try
            {
                coinText.text = CurrenciesController.Get(CurrencyType.Coins).ToString("N0");
            }
            catch
            {
                coinText.text = "0";
            }
        }

        private void RefreshCountryProgress()
        {
            if (countryNodes == null || countryNodes.Length == 0)
                return;

            bool supported = selectedContinent == authoredContinentIndex;
            int completedContinentLevels = 0;
            int highestUnlocked = 0;

            for (int country = 0; country < countryNodes.Length; country++)
            {
                int completed = GetCompletedLevelsInCountry(country);
                completedContinentLevels += completed;

                bool unlocked = supported && IsCountryUnlocked(country);
                if (unlocked)
                    highestUnlocked = country;

                CountryMapCountryNode node = countryNodes[country];
                if (node != null)
                    node.Refresh(unlocked, supported && country == selectedCountry, completed, LevelsPerCountry);
            }

            if (!supported)
            {
                selectedCountry = 0;
            }
            else if (!IsCountryUnlocked(selectedCountry))
            {
                selectedCountry = highestUnlocked;
                PlayerPrefs.SetInt(SelectedCountryKey, selectedCountry);
                PlayerPrefs.Save();
            }

            if (progressTitleText != null)
                progressTitleText.text = ContinentNames[selectedContinent].ToUpperInvariant() + " PROGRESS";

            if (progressValueText != null)
                progressValueText.text = completedContinentLevels + "/" + LevelsPerContinent;

            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01((float)completedContinentLevels / LevelsPerContinent);

            if (statusText != null)
            {
                if (!supported)
                {
                    statusText.text = "ASIA ART PACK INSTALLED";
                }
                else
                {
                    CountryMapCountryNode selectedNode =
                        selectedCountry >= 0 && selectedCountry < countryNodes.Length
                            ? countryNodes[selectedCountry]
                            : null;

                    statusText.text = selectedNode != null
                        ? selectedNode.CountryName.ToUpperInvariant() + "  •  " +
                          GetCompletedLevelsInCountry(selectedCountry) + "/" + LevelsPerCountry
                        : "SELECT A COUNTRY";
                }
            }
        }

        public void HandleCountryPressed(int countryIndex)
        {
            if (selectedContinent != authoredContinentIndex)
            {
                if (statusText != null)
                    statusText.text = "THIS CONTINENT'S ART PACK IS NOT INSTALLED YET";

                PlayClick();
                return;
            }

            if (countryIndex < 0 || countryIndex >= CountriesPerContinent)
                return;

            if (!IsCountryUnlocked(countryIndex))
            {
                if (statusText != null)
                {
                    if (countryIndex <= 0)
                    {
                        statusText.text = "COUNTRY LOCKED";
                    }
                    else
                    {
                        string previous = countryNodes != null && countryIndex - 1 < countryNodes.Length &&
                                          countryNodes[countryIndex - 1] != null
                            ? countryNodes[countryIndex - 1].CountryName.ToUpperInvariant()
                            : "THE PREVIOUS COUNTRY";

                        statusText.text = "COMPLETE " + previous + " TO UNLOCK";
                    }
                }

                PlayClick();
                return;
            }

            selectedCountry = countryIndex;
            PlayerPrefs.SetInt(SelectedCountryKey, selectedCountry);
            PlayerPrefs.Save();

            RefreshCountryProgress();
            PlayClick();

            if (statusText != null && countryNodes != null && countryIndex < countryNodes.Length &&
                countryNodes[countryIndex] != null)
            {
                int firstHumanLevel = selectedContinent * LevelsPerContinent + countryIndex * LevelsPerCountry + 1;
                int lastHumanLevel = firstHumanLevel + LevelsPerCountry - 1;
                statusText.text = countryNodes[countryIndex].CountryName.ToUpperInvariant() +
                                  " SELECTED  •  LEVELS " + firstHumanLevel + "-" + lastHumanLevel;
            }
        }

        private bool IsCountryUnlocked(int countryIndex)
        {
            if (countryIndex <= 0)
                return true;

            int previousCountryLastLevel =
                selectedContinent * LevelsPerContinent + countryIndex * LevelsPerCountry - 1;

            return LevelController.IsLevelCompleted(previousCountryLastLevel);
        }

        private int GetCompletedLevelsInCountry(int countryIndex)
        {
            int start = selectedContinent * LevelsPerContinent + countryIndex * LevelsPerCountry;
            int completed = 0;

            for (int i = 0; i < LevelsPerCountry; i++)
            {
                if (LevelController.IsLevelCompleted(start + i))
                    completed++;
            }

            return completed;
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
                Debug.LogError("[CountryMap] SaveController initialisation failed: " + ex.Message);
            }
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            Watermelon.WorldMapButtonFX fx = button.GetComponent<Watermelon.WorldMapButtonFX>();
            if (fx == null)
                button.gameObject.AddComponent<Watermelon.WorldMapButtonFX>();
        }

        private void BackToWorldMap()
        {
            PlayClick();
            EnhancedLoadingScreen.LoadViaLoadingScreen("WorldMap");
        }

        private void CoinPlusPressed()
        {
            PlayClick();

            if (statusText != null)
                statusText.text = "COINS ARE MANAGED FROM THE MAIN MENU SHOP";
        }

        private void OpenSettings()
        {
            PlayClick();

            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            RefreshSettingsLabels();
        }

        private void CloseSettings()
        {
            PlayClick();

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void ToggleSound()
        {
            bool enabled = AudioController.GetVolume() > 0.001f;
            AudioController.SetVolume(enabled ? 0f : 1f);
            RefreshSettingsLabels();
            PlayClick();
        }

        private void ToggleVibration()
        {
            AudioController.SetVibrationState(!AudioController.IsVibrationEnabled());
            RefreshSettingsLabels();
            PlayClick();
        }

        private void RefreshSettingsLabels()
        {
            if (soundText != null)
                soundText.text = "SOUND: " + (AudioController.GetVolume() > 0.001f ? "ON" : "OFF");

            if (vibrationText != null)
                vibrationText.text = "VIBRATION: " + (AudioController.IsVibrationEnabled() ? "ON" : "OFF");
        }

        private static void PlayClick()
        {
            try
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
            }
            catch
            {
                // UI remains functional when audio has not initialised yet.
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            CountryMapCountryNode[] nodes,
            Button back,
            Button settings,
            Button coinPlus,
            TextMeshProUGUI title,
            TextMeshProUGUI subtitle,
            TextMeshProUGUI coins,
            TextMeshProUGUI info,
            TextMeshProUGUI guide,
            TextMeshProUGUI progressTitle,
            TextMeshProUGUI progressValue,
            Image progressBarFill,
            TextMeshProUGUI state,
            GameObject settingsRoot,
            Button closeSettings,
            Button sound,
            TextMeshProUGUI soundLabel,
            Button vibration,
            TextMeshProUGUI vibrationLabel,
            GameObject unsupportedRoot,
            TextMeshProUGUI unsupportedText)
        {
            countryNodes = nodes;
            backButton = back;
            settingsButton = settings;
            coinPlusButton = coinPlus;
            titleText = title;
            subtitleText = subtitle;
            coinText = coins;
            infoText = info;
            guideText = guide;
            progressTitleText = progressTitle;
            progressValueText = progressValue;
            progressFill = progressBarFill;
            statusText = state;
            settingsPanel = settingsRoot;
            closeSettingsButton = closeSettings;
            soundButton = sound;
            soundText = soundLabel;
            vibrationButton = vibration;
            vibrationText = vibrationLabel;
            authoredContinentIndex = AsiaContinentIndex;
            unsupportedContinentPanel = unsupportedRoot;
            unsupportedContinentText = unsupportedText;
        }
#endif
    }
}
