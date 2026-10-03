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
        private const string DiamondsKey = "CC_Diamonds";
        private const string DiamondCurrencyMigrationKey = "CC_DiamondCurrencyMigrated";
        private const int LevelsPerCountry = 3;

        private const string CountrySubtitle = "FLAVORS, CULTURE, JOURNEY";
        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";

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
        [SerializeField] private Image diamondCounterImage;

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
        private int sceneCountry = -1;

        // Inside a country's Level Selection (LevelSelectionCountry): that country, with the
        // hero, background, icon and thumbnails exactly as set in the Hierarchy.
        private bool UsesSceneArt => sceneCountry >= 0;

        private void Awake()
        {
            LevelSelectionCountry country = GetComponentInParent<LevelSelectionCountry>(true);
            if (country != null)
            {
                if (!LevelSelectionCountry.Resolve(country))
                    return;   // another country is shown; this copy is being removed

                sceneCountry = LevelSelectionCountry.IndexOf(country.CountryId);
            }

            Instance = this;
            EnsureSaveControllerReady();
            UIEventSystemRuntime.UseCurrentSceneEventSystem();

            levelSave = SaveController.GetSaveObject<LevelSave>("level");
            ApplyApprovedDiamondArtwork();

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
            // Global country index (0-29) across all continents, see WorldCatalog.
            selectedCountry = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedCountryKey, 2),
                0,
                WorldCatalog.CountryCount - 1);

            if (sceneCountry >= 0)
                selectedCountry = sceneCountry;

            countryLevelStart = WorldCatalog.FirstLevelOfCountry(selectedCountry);

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
            WorldCatalog.Country country = WorldCatalog.GetCountry(selectedCountry);
            string countryName = country != null ? country.Name : "Country";

            if (countryTitleText != null)
                countryTitleText.text = countryName.ToUpperInvariant();

            if (countrySubtitleText != null)
                countrySubtitleText.text = CountrySubtitle;

            if (descriptionText != null && country != null)
                descriptionText.text = country.Description;

            if (guideText != null)
                guideText.text = "Complete all 3 missions\nto master " + countryName + "’s flavors!";

            if (progressTitleText != null)
                progressTitleText.text = "COUNTRY PROGRESS";

            // A country's own scene keeps the art set in its Hierarchy.
            if (UsesSceneArt)
                return;

            // Country art by name (Resources/World/<continent>/<country>/...), with the
            // existing Asian art standing in until a country has its own.
            Sprite hero = WorldArt.ForCountry(selectedCountry, WorldArt.LevelSelectHero);
            if (hero == null)
                hero = indiaHeroSprite;

            if (hero != null)
            {
                if (heroImage != null)
                    heroImage.sprite = hero;
                SetSceneSprite("Background Artwork", hero);
            }

            Sprite icon = WorldArt.ForCountry(selectedCountry, WorldArt.LevelSelectIcon);
            if (icon != null)
            {
                SetSceneSprite("India Icon", icon);
                SetSceneSprite("Country Icon", icon);
            }

            if (countryFlagImage != null)
            {
                Sprite flag = WorldArt.ForCountry(selectedCountry, WorldArt.FlagRound);
                if (flag != null)
                    countryFlagImage.sprite = flag;
            }
        }

        private void SetSceneSprite(string objectName, Sprite sprite)
        {
            if (sprite == null)
                return;

            Transform target = FindDeep(transform.root, objectName);
            if (target == null)
            {
                GameObject found = GameObject.Find(objectName);
                target = found != null ? found.transform : null;
            }

            Image image = target != null ? target.GetComponent<Image>() : null;
            if (image != null)
                image.sprite = sprite;
        }

        private static Transform FindDeep(Transform parent, string objectName)
        {
            if (parent == null)
                return null;
            if (parent.name == objectName)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), objectName);
                if (found != null)
                    return found;
            }
            return null;
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

        private void ApplyApprovedDiamondArtwork()
        {
            if (diamondCounterImage == null)
                return;

            // Use the exact approved Main Menu diamond counter artwork. That artwork
            // already contains the faceted blue gem and the green plus-button chrome.
            Sprite approvedDiamondBar = ProfessionalMainMenuEmbeddedAssets.GetSprite("diamond_bar");
            if (approvedDiamondBar != null)
            {
                diamondCounterImage.sprite = approvedDiamondBar;
                diamondCounterImage.color = Color.white;
                diamondCounterImage.preserveAspect = false;
            }

            // Older LevelSelection scenes had a separate placeholder diamond icon and
            // a visible Plus image. Hide those visuals so only the approved artwork is
            // rendered, while keeping the Plus Button component clickable.
            Transform legacyIcon = diamondCounterImage.transform.Find("Diamond Icon");
            if (legacyIcon != null)
                legacyIcon.gameObject.SetActive(false);

            Transform plusTransform = diamondCounterImage.transform.Find("Diamond Plus");
            Image plusImage = plusTransform != null ? plusTransform.GetComponent<Image>() : null;
            if (plusImage != null)
                plusImage.color = new Color(1f, 1f, 1f, 0f);
        }

        private void RefreshHUD()
        {
            if (coinText != null)
                coinText.text = GetCurrencyAmountSafe(CurrencyType.Coins).ToString("N0");

            if (diamondText != null)
                diamondText.text = GetDiamondAmount().ToString("N0");

            if (starText != null)
                starText.text = (levelSave != null ? levelSave.GetTotalStars() : 0).ToString("N0");
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

                // Null keeps the card's own thumbnail (a country scene's Hierarchy art).
                Sprite thumbnail = UsesSceneArt ? null : WorldArt.ForCountry(selectedCountry, WorldArt.LevelThumbnail(slot));
                if (thumbnail == null && !UsesSceneArt && indiaThumbnails != null && slot < indiaThumbnails.Length)
                    thumbnail = indiaThumbnails[slot];

                WorldCatalog.Country country = WorldCatalog.GetCountry(selectedCountry);
                string mission = country != null && slot < country.Missions.Length ? country.Missions[slot] : "Mission " + (slot + 1);

                card.Refresh(
                    levelIndex,
                    mission,
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
            // Back to this country's continent on the Country Map.
            PlayerPrefs.SetInt(SelectedContinentKey, Mathf.Max(0, WorldCatalog.ContinentOfCountry(selectedCountry)));
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

        // Every settings button in the game opens the shared Settings panel.
        private void OpenSettings()
        {
            ConveyorSettingsPanel.Show();
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

        private bool IsLevelUnlocked(int levelIndex) => GameProgress.IsLevelUnlocked(levelIndex);

        private bool IsLevelCompleted(int levelIndex) => GameProgress.IsLevelCompleted(levelIndex);

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
            Image diamondCounter,
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
            diamondCounterImage = diamondCounter;
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
