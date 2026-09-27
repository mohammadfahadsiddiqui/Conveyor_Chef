using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Behaviour-only controller for the authored Game.unity HUD.
    /// No layout is created or rearranged at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneHUDController : MonoBehaviour
    {
        [Header("Dynamic Text")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text diamondsText;

        [Header("Buttons")]
        [SerializeField] private Button lifePlusButton;
        [SerializeField] private Button coinPlusButton;
        [SerializeField] private Button diamondPlusButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button homeButton;

        [Header("Power-up Art")]
        [SerializeField] private Sprite undoButtonSprite;
        [SerializeField] private Sprite hintButtonSprite;
        [SerializeField] private Sprite shuffleButtonSprite;
        [SerializeField] private Sprite countBadgeSprite;

        private bool currencyCallbacksBound;
        private int lastLives = int.MinValue;
        private int lastLevel = int.MinValue;
        private PUUIController lastPowerUpsController;
        private int lastPowerUpCount = -1;

        private void Awake()
        {
            if (lifePlusButton != null)
                lifePlusButton.onClick.AddListener(OpenLivesPanel);

            if (coinPlusButton != null)
                coinPlusButton.onClick.AddListener(OpenCurrencyStore);

            if (diamondPlusButton != null)
                diamondPlusButton.onClick.AddListener(OpenCurrencyStore);

            if (pauseButton != null)
                pauseButton.onClick.AddListener(OpenPausePopup);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(ToggleSettings);

            if (homeButton != null)
                homeButton.onClick.AddListener(ReturnToLevelSelection);
        }

        private void Start()
        {
            CurrenciesController.InvokeOrSubcrtibe(BindCurrencyCallbacks);
            RefreshAll();
            TrySkinPowerUps(force: true);
        }

        private void Update()
        {
            RefreshLevelAndLives();
            TrySkinPowerUps(force: false);
        }

        private void OnDestroy()
        {
            if (currencyCallbacksBound)
            {
                CurrenciesController.UnsubscribeGlobalCallback(OnCurrencyChanged);
                currencyCallbacksBound = false;
            }
        }

        private void BindCurrencyCallbacks()
        {
            if (!currencyCallbacksBound)
            {
                CurrenciesController.SubscribeGlobalCallback(OnCurrencyChanged);
                currencyCallbacksBound = true;
            }

            RefreshCurrencies();
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            if (currency == null)
                return;

            if (currency.CurrencyType == CurrencyType.Coins && coinsText != null)
                coinsText.text = currency.AmountFormatted;
            else if (currency.CurrencyType == CurrencyType.Diamonds && diamondsText != null)
                diamondsText.text = currency.AmountFormatted;
        }

        private void RefreshAll()
        {
            RefreshLevelAndLives();
            RefreshCurrencies();
        }

        private void RefreshLevelAndLives()
        {
            int displayLevel = GetDisplayLevel();
            if (displayLevel != lastLevel)
            {
                lastLevel = displayLevel;
                if (levelText != null)
                    levelText.text = $"LEVEL {displayLevel}";
            }

            try
            {
                int lives = LivesManager.Lives;
                if (lives != lastLives)
                {
                    lastLives = lives;
                    if (livesText != null)
                        livesText.text = LivesManager.IsMaxLives ? "FULL" : lives.ToString();
                }
            }
            catch
            {
                // LivesManager can initialise one frame after the page on some flows.
                // Leave the authored placeholder until the manager is ready.
            }
        }

        private static int GetDisplayLevel()
        {
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            if (levelSave != null && levelSave.isPlayingFromLevelSelection)
                return levelSave.selectedLevelIndex + 1;

            return Mathf.Max(1, LevelController.DisplayLevelNumber);
        }

        private void RefreshCurrencies()
        {
            try
            {
                Currency coins = CurrenciesController.GetCurrency(CurrencyType.Coins);
                Currency diamonds = CurrenciesController.GetCurrency(CurrencyType.Diamonds);

                if (coinsText != null && coins != null)
                    coinsText.text = coins.AmountFormatted;

                if (diamondsText != null && diamonds != null)
                    diamondsText.text = diamonds.AmountFormatted;
            }
            catch
            {
                // Currency module may not be initialised yet. Its initialise callback
                // will call RefreshCurrencies as soon as it becomes available.
            }
        }

        private static void OpenLivesPanel()
        {
            UIMainMenu mainMenu = Object.FindFirstObjectByType<UIMainMenu>(FindObjectsInactive.Include);
            if (mainMenu != null)
                mainMenu.ShowAddLivesPanel();
        }

        private static void OpenCurrencyStore()
        {
            UIMainMenu mainMenu = Object.FindFirstObjectByType<UIMainMenu>(FindObjectsInactive.Include);
            if (mainMenu != null)
                mainMenu.IAPStoreButton();
        }

        private static void OpenPausePopup()
        {
            UILevelQuitPopUp popup = Object.FindFirstObjectByType<UILevelQuitPopUp>(FindObjectsInactive.Include);
            if (popup != null)
                popup.Show();
        }

        private static void ToggleSettings()
        {
            SettingsPanel settingsPanel = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (settingsPanel != null)
                settingsPanel.SettingsButton();
        }

        private static void ReturnToLevelSelection()
        {
            GameController.ReturnToLevelSelection();
        }

        private void TrySkinPowerUps(bool force)
        {
            PUUIController controller = Object.FindFirstObjectByType<PUUIController>(FindObjectsInactive.Include);
            if (controller == null || controller.UIBehaviors == null)
                return;

            PUUIBehavior[] items = controller.UIBehaviors;
            if (!force && controller == lastPowerUpsController && items.Length == lastPowerUpCount)
                return;

            lastPowerUpsController = controller;
            lastPowerUpCount = items.Length;

            foreach (PUUIBehavior item in items)
            {
                if (item == null || item.Settings == null)
                    continue;

                Sprite fullButtonSprite = null;
                if (item.Settings is PUUndoSettings)
                    fullButtonSprite = undoButtonSprite;
                else if (item.Settings is PUHintSettings)
                    fullButtonSprite = hintButtonSprite;
                else if (item.Settings is PUShuffleSettings)
                    fullButtonSprite = shuffleButtonSprite;

                Transform background = item.transform.Find("Background");
                if (background != null)
                {
                    Image image = background.GetComponent<Image>();
                    if (image != null && fullButtonSprite != null)
                    {
                        image.sprite = fullButtonSprite;
                        image.color = Color.white;
                        image.preserveAspect = true;
                    }
                }

                // The generated button art already includes the power-up symbol.
                Transform icon = item.transform.Find("Icon");
                if (icon != null && fullButtonSprite != null)
                    icon.gameObject.SetActive(false);

                Transform amountBackground = item.transform.Find("Amount Background");
                if (amountBackground != null && countBadgeSprite != null)
                {
                    Image badge = amountBackground.GetComponent<Image>();
                    if (badge != null)
                    {
                        badge.sprite = countBadgeSprite;
                        badge.color = Color.white;
                        badge.preserveAspect = true;
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            TMP_Text level,
            TMP_Text lives,
            TMP_Text coins,
            TMP_Text diamonds,
            Button lifePlus,
            Button coinPlus,
            Button diamondPlus,
            Button pause,
            Button settings,
            Button home,
            Sprite undo,
            Sprite hint,
            Sprite shuffle,
            Sprite countBadge)
        {
            levelText = level;
            livesText = lives;
            coinsText = coins;
            diamondsText = diamonds;
            lifePlusButton = lifePlus;
            coinPlusButton = coinPlus;
            diamondPlusButton = diamondPlus;
            pauseButton = pause;
            settingsButton = settings;
            homeButton = home;
            undoButtonSprite = undo;
            hintButtonSprite = hint;
            shuffleButtonSprite = shuffle;
            countBadgeSprite = countBadge;
        }
#endif
    }
}
