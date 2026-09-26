using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Runtime behaviour for the serialized, designer-editable LevelSelection scene.
    /// The scene owns every RectTransform and visual position. Runtime only updates
    /// text, sprites, visibility, button state and the progress-fill width.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectionController : MonoBehaviour
    {
        public static LevelSelectionController Instance { get; private set; }

        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const string FromCountryMapKey = "CC_LevelSelection_FromCountryMap";
        private const string SelectedCountryLevelStartKey = "CC_CountryMap_SelectedLevelStart";
        private const int LevelsPerCountry = 3;

        private static readonly string[] CountryNames =
        {
            "China", "Japan", "India", "South Korea", "Thailand"
        };

        private static readonly string[] CountrySubtitles =
        {
            "FLAVORS, CULTURE, JOURNEY",
            "FLAVORS, CULTURE, JOURNEY",
            "FLAVORS, CULTURE, JOURNEY",
            "FLAVORS, CULTURE, JOURNEY",
            "FLAVORS, CULTURE, JOURNEY"
        };

        private static readonly string[,] MissionTitles =
        {
            { "Beijing Bites", "Shanghai Rush", "Sichuan Station" },
            { "Tokyo Treats", "Kyoto Kitchen", "Osaka Rush" },
            { "Varanasi Ghats", "Delhi Streets", "Mumbai Docks" },
            { "Seoul Street Food", "Busan Harbor", "Jeonju Kitchen" },
            { "Bangkok Market", "Chiang Mai Feast", "Phuket Pier" }
        };

        private static readonly string[] CountryDescriptions =
        {
            "Explore China's iconic cities and bold regional flavors as you master three culinary missions.",
            "Travel across Japan through fast kitchens, classic streets and unforgettable food destinations.",
            "Explore India's rich food culture, vibrant cities and iconic destinations as you deliver delicious dishes across the country!",
            "Discover Korea's energetic food streets, coastal stops and traditional culinary culture.",
            "Serve your way through Thailand's colorful markets, northern kitchens and tropical waterfronts."
        };

        [Header("Navigation")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button coinPlusButton;
        [FormerlySerializedAs("chefPlusButton")]
        [SerializeField] private Button diamondPlusButton;
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;

        [Header("Top HUD")]
        [SerializeField] private TextMeshProUGUI coinText;
        [FormerlySerializedAs("chefCurrencyText")]
        [SerializeField] private TextMeshProUGUI diamondText;
        [SerializeField] private TextMeshProUGUI starText;

        [Header("Country Header")]
        [SerializeField] private TextMeshProUGUI countryTitleText;
        [SerializeField] private TextMeshProUGUI countrySubtitleText;
        [SerializeField] private Image countryFlagImage;

        [Header("Hero / Description")]
        [SerializeField] private Image heroImage;
        [SerializeField] private Sprite indiaHeroSprite;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Level Cards")]
        [SerializeField] private LevelSelectionLevelCard[] levelCards = new LevelSelectionLevelCard[3];
        [SerializeField] private Sprite[] indiaThumbnails = new Sprite[3];

        [Header("Guide")]
        [SerializeField] private TextMeshProUGUI guideText;

        [Header("Country Progress")]
        [SerializeField] private Image progressEmblem;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressTitleText;
        [SerializeField] private TextMeshProUGUI progressValueText;

        [Header("State")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TextMeshProUGUI soundText;
        [SerializeField] private TextMeshProUGUI vibrationText;

        private LevelSave levelSave;
        private int selectedCountry;
        private int countryLevelStart;
        private int selectedSlot;
        private bool currencySubscribed;

        private void Awake()
        {
            Instance = this;
            EnsureSaveControllerReady();
            UIEventSystemRuntime.UseCurrentSceneEventSystem();

            levelSave = SaveController.GetSaveObject<LevelSave>("level");

            Wire(homeButton, HomePressed);
            Wire(backButton, BackPressed);
            Wire(settingsButton, OpenSettings);
            Wire(coinPlusButton, CoinPlusPressed);
            Wire(diamondPlusButton, DiamondPlusPressed);
            Wire(leftArrowButton, PreviousLevel);
            Wire(rightArrowButton, NextLevel);
            Wire(closeSettingsButton, CloseSettings);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);

            if (levelCards != null)
            {
                for (int i = 0; i < levelCards.Length; i++)
                    levelCards[i]?.Bind(this);
            }

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void Start()
        {
            selectedCountry = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedCountryKey, 2),
                0,
                CountryNames.Length - 1);

            countryLevelStart = Mathf.Max(
                0,
                PlayerPrefs.GetInt(
                    SelectedCountryLevelStartKey,
                    selectedCountry * LevelsPerCountry));

            PlayerPrefs.SetInt(SelectedCountryKey, selectedCountry);
            PlayerPrefs.SetInt(SelectedCountryLevelStartKey, countryLevelStart);
            PlayerPrefs.Save();

            selectedSlot = ResolveInitialSelectedSlot();

            ApplyCountryPresentation();
            RefreshAll();
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

            if (Instance == this)
                Instance = null;
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            if (currency == null)
                return;

            if (currency.CurrencyType == CurrencyType.Coins ||
                currency.CurrencyType == CurrencyType.Diamonds)
            {
                RefreshHUD();
            }
        }

        private int ResolveInitialSelectedSlot()
        {
            for (int slot = 0; slot < LevelsPerCountry; slot++)
            {
                int levelIndex = countryLevelStart + slot;
                if (IsLevelUnlocked(levelIndex) && !IsLevelCompleted(levelIndex))
                    return slot;
            }

            for (int slot = LevelsPerCountry - 1; slot >= 0; slot--)
            {
                if (IsLevelUnlocked(countryLevelStart + slot))
                    return slot;
            }

            return 0;
        }

        private void ApplyCountryPresentation()
        {
            string countryName = CountryNames[selectedCountry];

            if (countryTitleText != null)
                countryTitleText.text = countryName.ToUpperInvariant();

            if (countrySubtitleText != null)
                countrySubtitleText.text = CountrySubtitles[selectedCountry];

            if (descriptionText != null)
                descriptionText.text = CountryDescriptions[selectedCountry];

            if (guideText != null)
                guideText.text = "Complete all 3 missions\nto master " + countryName + "’s flavors!";

            if (progressTitleText != null)
                progressTitleText.text = "COUNTRY PROGRESS";

            // The first art pack is India. Keep its thumbnails authored and editable.
            // Additional country art packs can be assigned later without changing layout.
            if (selectedCountry == 2 && heroImage != null && indiaHeroSprite != null)
                heroImage.sprite = indiaHeroSprite;
        }

        private void RefreshAll()
        {
            RefreshHUD();
            RefreshCards();
            RefreshProgress();

            if (leftArrowButton != null)
                leftArrowButton.interactable = selectedSlot > 0;

            if (rightArrowButton != null)
                rightArrowButton.interactable = selectedSlot < LevelsPerCountry - 1;
        }

        private void RefreshHUD()
        {
            if (coinText != null)
                coinText.text = GetCurrencyAmountSafe(CurrencyType.Coins).ToString("N0");

            if (diamondText != null)
                diamondText.text = GetCurrencyAmountSafe(CurrencyType.Diamonds).ToString("N0");

            if (starText != null)
                starText.text = (levelSave != null ? levelSave.GetTotalStars() : 0).ToString("N0");
        }

        private static int GetCurrencyAmountSafe(CurrencyType currencyType)
        {
            try
            {
                return CurrenciesController.Get(currencyType);
            }
            catch
            {
                return 0;
            }
        }

        private void RefreshCards()
        {
            if (levelCards == null)
                return;

            for (int slot = 0; slot < levelCards.Length && slot < LevelsPerCountry; slot++)
            {
                LevelSelectionLevelCard card = levelCards[slot];
                if (card == null)
                    continue;

                int levelIndex = countryLevelStart + slot;
                bool unlocked = IsLevelUnlocked(levelIndex);
                bool completed = IsLevelCompleted(levelIndex);
                int stars = GetLevelStars(levelIndex);

                Sprite thumbnail = null;
                if (selectedCountry == 2 &&
                    indiaThumbnails != null &&
                    slot < indiaThumbnails.Length)
                {
                    thumbnail = indiaThumbnails[slot];
                }

                card.Refresh(
                    levelIndex,
                    MissionTitles[selectedCountry, slot],
                    thumbnail,
                    unlocked,
                    slot == selectedSlot,
                    completed,
                    stars);
            }

            if (statusText != null)
            {
                int levelIndex = countryLevelStart + selectedSlot;
                statusText.text = IsLevelUnlocked(levelIndex)
                    ? "MISSION " + (selectedSlot + 1) + " SELECTED"
                    : "COMPLETE THE PREVIOUS MISSION TO UNLOCK";
            }
        }

        private void RefreshProgress()
        {
            int completed = 0;
            for (int i = 0; i < LevelsPerCountry; i++)
            {
                if (IsLevelCompleted(countryLevelStart + i))
                    completed++;
            }

            if (progressValueText != null)
                progressValueText.text = completed + "/" + LevelsPerCountry;

            if (progressFill == null)
                return;

            float normalized = completed / (float)LevelsPerCountry;

            // Never move or resize authored UI in Play Mode.
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.fillClockwise = true;
            progressFill.fillAmount = normalized;
            progressFill.preserveAspect = false;
            progressFill.raycastTarget = false;
        }

        public void HandleCardPressed(int slot)
        {
            slot = Mathf.Clamp(slot, 0, LevelsPerCountry - 1);
            selectedSlot = slot;
            PlayClick();
            RefreshAll();
        }

        public void HandlePlayPressed(int slot)
        {
            slot = Mathf.Clamp(slot, 0, LevelsPerCountry - 1);
            int levelIndex = countryLevelStart + slot;

            if (!IsLevelUnlocked(levelIndex))
            {
                HandleLockedPressed(slot);
                return;
            }

            LoadSelectedLevel(levelIndex);
        }

        public void HandleLockedPressed(int slot)
        {
            selectedSlot = Mathf.Clamp(slot, 0, LevelsPerCountry - 1);
            PlayClick();

            if (statusText != null)
                statusText.text = "COMPLETE THE PREVIOUS MISSION TO UNLOCK";

            RefreshCards();
        }

        private void PreviousLevel()
        {
            if (selectedSlot <= 0)
                return;

            selectedSlot--;
            PlayClick();
            RefreshAll();
        }

        private void NextLevel()
        {
            if (selectedSlot >= LevelsPerCountry - 1)
                return;

            selectedSlot++;
            PlayClick();
            RefreshAll();
        }

        public void LoadSelectedLevel(int levelIndex)
        {
            if (levelSave == null)
            {
                Debug.LogError("[LevelSelection] LevelSave is unavailable.");
                return;
            }

            if (!IsLevelUnlocked(levelIndex))
                return;

            levelSave.selectedLevelIndex = levelIndex;
            levelSave.isPlayingFromLevelSelection = true;
            levelSave.ReplayingLevelAgain = IsLevelCompleted(levelIndex);

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            // Keep CountryMap context so gameplay -> LevelSelection returns to
            // this same country screen instead of the legacy generic selector.
            PlayerPrefs.SetInt(FromCountryMapKey, 1);
            PlayerPrefs.SetInt(SelectedCountryLevelStartKey, countryLevelStart);
            PlayerPrefs.Save();

            PlayClick();
            EnhancedLoadingScreen.LoadViaLoadingScreen("Game");
        }

        public void BackToMainMenu()
        {
            BackPressed();
        }

        public void ShowPage(int pageIndex)
        {
            selectedSlot = Mathf.Clamp(pageIndex, 0, LevelsPerCountry - 1);
            RefreshAll();
        }

        public bool IsPageUnlocked(int pageIndex)
        {
            int slot = Mathf.Clamp(pageIndex, 0, LevelsPerCountry - 1);
            return IsLevelUnlocked(countryLevelStart + slot);
        }

        public int GetCurrentPageIndex()
        {
            return selectedSlot;
        }

        private void BackPressed()
        {
            PlayClick();
            PlayerPrefs.SetInt(FromCountryMapKey, 1);
            PlayerPrefs.Save();
            EnhancedLoadingScreen.LoadViaLoadingScreen("CountryMap");
        }

        private void HomePressed()
        {
            PlayClick();
            EnhancedLoadingScreen.LoadViaLoadingScreen("menu");
        }

        private void CoinPlusPressed()
        {
            PlayClick();
            if (statusText != null)
                statusText.text = "COINS ARE MANAGED FROM THE MAIN MENU SHOP";
        }

        private void DiamondPlusPressed()
        {
            PlayClick();
            if (statusText != null)
                statusText.text = "DIAMONDS ARE MANAGED FROM THE MAIN MENU SHOP";
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

        private bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex <= 0)
                return true;

            return IsLevelCompleted(levelIndex - 1);
        }

        private bool IsLevelCompleted(int levelIndex)
        {
            if (levelSave == null || levelIndex < 0)
                return false;

            LevelProgressData progress = levelSave.GetLevelProgress(levelIndex);
            if (progress != null && progress.isCompleted)
                return true;

            int legacyCompletedCount = Mathf.Max(0, levelSave.DisplayLevelNumber);
            return levelIndex < legacyCompletedCount;
        }

        private int GetLevelStars(int levelIndex)
        {
            if (levelSave == null)
                return 0;

            LevelProgressData progress = levelSave.GetLevelProgress(levelIndex);
            return progress != null ? Mathf.Clamp(progress.starsEarned, 0, 3) : 0;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
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
                Debug.LogError("[LevelSelection] SaveController initialisation failed: " + ex.Message);
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
                // UI remains functional when audio has not initialised yet.
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            Button home,
            Button back,
            Button settings,
            Button coinPlus,
            Button diamondPlus,
            Button leftArrow,
            Button rightArrow,
            TextMeshProUGUI coins,
            TextMeshProUGUI diamonds,
            TextMeshProUGUI stars,
            TextMeshProUGUI countryTitle,
            TextMeshProUGUI countrySubtitle,
            Image flag,
            Image hero,
            Sprite indiaHero,
            TextMeshProUGUI description,
            LevelSelectionLevelCard[] cards,
            Sprite[] indiaMissionThumbnails,
            TextMeshProUGUI guide,
            Image emblem,
            Image fill,
            TextMeshProUGUI progressTitle,
            TextMeshProUGUI progressValue,
            TextMeshProUGUI state,
            GameObject settingsRoot,
            Button closeSettings,
            Button sound,
            Button vibration,
            TextMeshProUGUI soundLabel,
            TextMeshProUGUI vibrationLabel)
        {
            homeButton = home;
            backButton = back;
            settingsButton = settings;
            coinPlusButton = coinPlus;
            diamondPlusButton = diamondPlus;
            leftArrowButton = leftArrow;
            rightArrowButton = rightArrow;
            coinText = coins;
            diamondText = diamonds;
            starText = stars;
            countryTitleText = countryTitle;
            countrySubtitleText = countrySubtitle;
            countryFlagImage = flag;
            heroImage = hero;
            indiaHeroSprite = indiaHero;
            descriptionText = description;
            levelCards = cards;
            indiaThumbnails = indiaMissionThumbnails;
            guideText = guide;
            progressEmblem = emblem;
            progressFill = fill;
            progressTitleText = progressTitle;
            progressValueText = progressValue;
            statusText = state;
            settingsPanel = settingsRoot;
            closeSettingsButton = closeSettings;
            soundButton = sound;
            vibrationButton = vibration;
            soundText = soundLabel;
            vibrationText = vibrationLabel;
        }
#endif
    }
}
