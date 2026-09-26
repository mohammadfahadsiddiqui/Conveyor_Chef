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
        private const string LaunchedFromWorldMapKey = "CC_CountryMap_LaunchedFromWorldMap";
        private const string SelectedCountryLevelStartKey = "CC_CountryMap_SelectedLevelStart";
        private const int AsiaContinentIndex = 0;
        private const int AuthoredGameplayLevelStart = 0;

        private static readonly string[] ContinentNames =
        {
            "Asia", "North America", "South America", "Europe", "Africa", "Australia"
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
        [SerializeField, HideInInspector] private int progressWidgetLayoutVersion;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TextMeshProUGUI soundText;
        [SerializeField] private TextMeshProUGUI vibrationText;

        [Header("Current Art Pack")]
        [Tooltip("The generated art pack currently contains the complete Asia country map.")]
        [SerializeField] private GameObject unsupportedContinentPanel;
        [SerializeField] private TextMeshProUGUI unsupportedContinentText;

        private int selectedContinent;
        private int selectedCountry;
        private bool currencySubscribed;

        private void Awake()
        {
            UIEventSystemRuntime.UseCurrentSceneEventSystem();
            EnsureSaveControllerReady();
            ResolveProgressReferences();
            EnsureChefBehindProgressPanel();

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

        private void ResolveProgressReferences()
        {
            Transform root = transform.parent;
            if (root == null)
                return;

            if (progressTitleText == null)
            {
                Transform t = root.Find("Continent Progress Panel/Title Group/Progress Title");
                if (t != null)
                    progressTitleText = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressValueText == null)
            {
                Transform t = root.Find("Continent Progress Panel/Value Badge/Progress Value");
                if (t != null)
                    progressValueText = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressFill == null)
            {
                Transform t = root.Find("Continent Progress Panel/Progress Track/Progress Fill");
                if (t != null)
                    progressFill = t.GetComponent<Image>();
            }

            if (statusText == null)
            {
                Transform t = root.Find("Status Text");
                if (t != null)
                    statusText = t.GetComponent<TextMeshProUGUI>();
            }
        }

        private void EnsureChefBehindProgressPanel()
        {
            Transform root = transform.parent;
            if (root == null)
                return;

            Transform chef = root.Find("Chef Guide Mascot");
            Transform progressPanel = root.Find("Continent Progress Panel");

            if (chef == null || progressPanel == null)
                return;

            // In Unity UI, later siblings render on top. Keep the authored position
            // unchanged and only correct the draw order when the chef is in front.
            if (chef.GetSiblingIndex() > progressPanel.GetSiblingIndex())
                chef.SetSiblingIndex(progressPanel.GetSiblingIndex());
        }

        private void Start()
        {
            int requestedWorldMapContinent = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedContinentKey, AsiaContinentIndex),
                0,
                ContinentNames.Length - 1);

            PlayerPrefs.DeleteKey(LaunchedFromWorldMapKey);

            // This scene is specifically the Asia country-map pack.
            // Asia is canonical Chapter 1 / continent index 0.
            selectedContinent = AsiaContinentIndex;

            selectedCountry = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedCountryKey, 0),
                0,
                CountriesPerContinent - 1);

            if (requestedWorldMapContinent != selectedContinent)
            {
                Debug.Log(
                    "[CountryMap] Requested " + ContinentNames[requestedWorldMapContinent] +
                    ", but the installed CountryMap art pack is " +
                    ContinentNames[selectedContinent] + ". Showing the authored Asia map.");
            }

            // Do not overwrite CC_WorldMap_SelectedContinent here. WorldMap owns that
            // value and should return to the same continent after Back.
            PlayerPrefs.SetInt(SelectedCountryKey, selectedCountry);
            PlayerPrefs.Save();

            ApplyContinentHeader();
            ConfigureProgressFillRendering();
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

            // The current scene is the authored Asia pack. Never cover the map with
            // a runtime "unsupported continent" modal; unsupported WorldMap choices
            // fall back to this pack until their own art/data packs are added.
            if (unsupportedContinentPanel != null)
                unsupportedContinentPanel.SetActive(false);
        }

        private void ConfigureProgressFillRendering()
        {
            ResolveProgressReferences();

            if (progressFill == null)
            {
                Debug.LogError("[CountryMap] Progress Fill reference is missing.");
                return;
            }

            // IMPORTANT: never change RectTransform position, anchors, pivot, height or
            // scale at runtime. The Canvas owns the layout and designers can edit it.
            // Runtime only changes the width inside SetProgressVisual().
            progressFill.type = Image.Type.Simple;
            progressFill.preserveAspect = false;
            progressFill.raycastTarget = false;
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

            LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
            if (save == null)
            {
                Debug.LogWarning("[CountryMap] Level save is unavailable; country progress cannot be displayed yet.");
                SetProgressVisual(0, LevelsPerCountry);

                if (progressTitleText != null)
                    progressTitleText.text = "COUNTRY PROGRESS";

                if (progressValueText != null)
                    progressValueText.text = "0/" + LevelsPerCountry;

                return;
            }

            const bool supported = true;
            int highestUnlocked = 0;
            int countryCount = Mathf.Min(CountriesPerContinent, countryNodes.Length);

            if (countryCount <= 0)
                return;

            selectedCountry = Mathf.Clamp(selectedCountry, 0, countryCount - 1);

            // Refresh every country node using its real saved level completion.
            for (int country = 0; country < countryCount; country++)
            {
                int completed = GetCompletedLevelsInCountry(country, save);
                bool unlocked = supported && IsCountryUnlocked(country, save);

                if (unlocked)
                    highestUnlocked = country;

                CountryMapCountryNode node = countryNodes[country];
                if (node != null)
                    node.Refresh(unlocked, supported && country == selectedCountry, completed, LevelsPerCountry);
            }

            if (!IsCountryUnlocked(selectedCountry, save))
            {
                selectedCountry = highestUnlocked;
                PlayerPrefs.SetInt(SelectedCountryKey, selectedCountry);
                PlayerPrefs.Save();
            }

            CountryMapCountryNode selectedNode =
                selectedCountry >= 0 && selectedCountry < countryNodes.Length
                    ? countryNodes[selectedCountry]
                    : null;

            int selectedCompleted = GetCompletedLevelsInCountry(selectedCountry, save);
            string selectedCountryName = selectedNode != null
                ? selectedNode.CountryName.ToUpperInvariant()
                : "COUNTRY";

            if (progressTitleText != null)
                progressTitleText.text = selectedCountryName + " PROGRESS";

            if (progressValueText != null)
                progressValueText.text = selectedCompleted + "/" + LevelsPerCountry;

            SetProgressVisual(selectedCompleted, LevelsPerCountry);

            if (statusText != null)
            {
                statusText.text = selectedNode != null
                    ? selectedCountryName + "  •  " + selectedCompleted + "/" + LevelsPerCountry
                    : "SELECT A COUNTRY";
            }

            Debug.Log("[CountryMap] " + selectedCountryName + " real progress: " +
                      selectedCompleted + "/" + LevelsPerCountry);
        }

        private void SetProgressVisual(int completedLevels, int totalLevels)
        {
            if (progressFill == null)
                return;

            float normalized = totalLevels > 0
                ? Mathf.Clamp01((float)completedLevels / totalLevels)
                : 0f;

            RectTransform fillRect = progressFill.rectTransform;
            RectTransform trackRect = fillRect.parent as RectTransform;

            // Derive the full width from the authored track instead of hard-coding it.
            // With the default layout: 420 track width - (20 * 2) inset = 380.
            // If the designer adjusts the fill X position or track width in Canvas,
            // Play Mode respects that layout instead of snapping back.
            float leftInset = Mathf.Max(0f, fillRect.anchoredPosition.x);
            float fullWidth = trackRect != null
                ? Mathf.Max(0f, trackRect.rect.width - (leftInset * 2f))
                : Mathf.Max(0f, fillRect.sizeDelta.x);

            Vector2 size = fillRect.sizeDelta;
            size.x = fullWidth * normalized;
            fillRect.sizeDelta = size;

            progressFill.enabled = true;
        }

        public void HandleCountryPressed(int countryIndex)
        {
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
                int firstHumanLevel = AuthoredGameplayLevelStart + countryIndex * LevelsPerCountry + 1;
                int lastHumanLevel = firstHumanLevel + LevelsPerCountry - 1;
                statusText.text = countryNodes[countryIndex].CountryName.ToUpperInvariant() +
                                  " SELECTED  •  LEVELS " + firstHumanLevel + "-" + lastHumanLevel;
            }

            // Hand off the selected country to the existing LevelSelection scene.
            // The level-selection controller reads these keys, opens the page that
            // contains this country's first level, and routes Back to CountryMap.
            int selectedLevelStart =
                AuthoredGameplayLevelStart + countryIndex * LevelsPerCountry;

            PlayerPrefs.SetInt(SelectedCountryLevelStartKey, selectedLevelStart);
            PlayerPrefs.SetInt("CC_LevelSelection_FromCountryMap", 1);
            PlayerPrefs.Save();
            EnhancedLoadingScreen.LoadViaLoadingScreen("LevelSelection");
        }

        private bool IsCountryUnlocked(int countryIndex)
        {
            LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
            return save != null && IsCountryUnlocked(countryIndex, save);
        }

        private bool IsCountryUnlocked(int countryIndex, LevelSave save)
        {
            if (countryIndex <= 0)
                return true;

            int previousCountryLastLevel =
                AuthoredGameplayLevelStart + countryIndex * LevelsPerCountry - 1;

            return IsLevelCompletedForCountryMap(previousCountryLastLevel, save);
        }

        private int GetCompletedLevelsInCountry(int countryIndex)
        {
            LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
            return save != null ? GetCompletedLevelsInCountry(countryIndex, save) : 0;
        }

        private int GetCompletedLevelsInCountry(int countryIndex, LevelSave save)
        {
            int start = AuthoredGameplayLevelStart + countryIndex * LevelsPerCountry;
            int completed = 0;

            for (int i = 0; i < LevelsPerCountry; i++)
            {
                if (IsLevelCompletedForCountryMap(start + i, save))
                    completed++;
            }

            return completed;
        }

        private static bool IsLevelCompletedForCountryMap(int levelIndex, LevelSave save)
        {
            if (save == null || levelIndex < 0)
                return false;

            // Primary source: explicit per-level completion written by GameController.
            LevelProgressData progress = save.GetLevelProgress(levelIndex);
            if (progress != null && progress.isCompleted)
                return true;

            // Compatibility with saves created before per-level progress was added.
            // DisplayLevelNumber is the number of levels already advanced through in
            // the original linear progression, so treat those earlier map slots as done.
            int legacyCompletedCount = Mathf.Max(0, save.DisplayLevelNumber);
            return levelIndex < legacyCompletedCount;
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

            // Visual button feedback is serialized by the editor baker.
            // Runtime must not add/remove components or mutate the authored hierarchy.
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
        public int EditorProgressWidgetLayoutVersion => progressWidgetLayoutVersion;

        public void EditorConfigureProgress(
            TextMeshProUGUI progressTitle,
            TextMeshProUGUI progressValue,
            Image progressBarFill,
            TextMeshProUGUI state)
        {
            progressTitleText = progressTitle;
            progressValueText = progressValue;
            progressFill = progressBarFill;
            statusText = state;
        }

        public void EditorSetProgressWidgetLayoutVersion(int version)
        {
            progressWidgetLayoutVersion = version;
        }

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
            progressWidgetLayoutVersion = 2;
            settingsPanel = settingsRoot;
            closeSettingsButton = closeSettings;
            soundButton = sound;
            soundText = soundLabel;
            vibrationButton = vibration;
            vibrationText = vibrationLabel;
            unsupportedContinentPanel = unsupportedRoot;
            unsupportedContinentText = unsupportedText;
        }
#endif
    }
}
