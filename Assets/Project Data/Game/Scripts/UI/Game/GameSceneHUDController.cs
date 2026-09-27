using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;

namespace Watermelon
{
    /// <summary>
    /// Small runtime bridge for NEW GAME SCENE [ACTIVE].
    /// Existing UIGame, LivesIndicator, CurrencyUIPanelSimple, UIOrderPanel and
    /// PUUIController remain the actual gameplay/UI logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneHUDController : MonoBehaviour
    {
        [Header("Currency Logic")]
        [SerializeField] private CurrencyUIPanelSimple coinPanel;
        [SerializeField] private CurrencyUIPanelSimple diamondPanel;
        [SerializeField] private Image coinIconImage;
        [SerializeField] private Image diamondIconImage;

        [Header("Extra Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button homeButton;

        [Header("Existing Pause Logic")]
        [SerializeField] private UILevelQuitPopUp pausePopup;

        [Header("Power-up Artwork")]
        [SerializeField] private Sprite undoButtonSprite;
        [SerializeField] private Sprite hintButtonSprite;
        [SerializeField] private Sprite shuffleButtonSprite;
        [SerializeField] private Sprite countBadgeSprite;

        [Header("Currency Artwork")]
        [SerializeField] private Sprite coinIconSprite;
        [SerializeField] private Sprite diamondIconSprite;

        private PUUIController lastPowerUpsController;
        private int lastPowerUpCount = -1;

        private void Awake()
        {
            if (coinPanel != null && coinPanel.AddButton != null)
                coinPanel.AddButton.onClick.AddListener(OpenCurrencyStore);

            if (diamondPanel != null && diamondPanel.AddButton != null)
                diamondPanel.AddButton.onClick.AddListener(OpenCurrencyStore);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenPauseOptions);

            if (homeButton != null)
                homeButton.onClick.AddListener(ReturnToLevelSelection);
        }

        private void Start()
        {
            // CurrencyUIPanelSimple keeps the original live update logic.
            CurrenciesController.InvokeOrSubcrtibe(InitialiseCurrencyPanels);
            TrySkinPowerUps(true);
        }

        private void Update()
        {
            TrySkinPowerUps(false);
        }

        private void InitialiseCurrencyPanels()
        {
            if (coinPanel != null)
                coinPanel.Initialise();

            if (diamondPanel != null)
                diamondPanel.Initialise();

            // CurrencyUIPanelSimple assigns the database icons while initialising.
            // Re-apply the generated art after that so logic stays original while
            // the visible icon follows the redesigned Game UI.
            if (coinIconImage != null && coinIconSprite != null)
            {
                coinIconImage.sprite = coinIconSprite;
                coinIconImage.preserveAspect = true;
            }

            if (diamondIconImage != null && diamondIconSprite != null)
            {
                diamondIconImage.sprite = diamondIconSprite;
                diamondIconImage.preserveAspect = true;
            }
        }

        private static void OpenCurrencyStore()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            UIController.ShowPage<UIIAPStore>();
        }

        private void OpenPauseOptions()
        {
            if (pausePopup == null)
                return;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            pausePopup.Show();
        }

        private static void ReturnToLevelSelection()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            GameController.ReturnToLevelSelection();
        }

        private void TrySkinPowerUps(bool force)
        {
            UIGame game = GetComponent<UIGame>();
            PUUIController controller = game != null ? game.PowerUpsUIController : null;

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
                if (background != null && fullButtonSprite != null)
                {
                    Image image = background.GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = fullButtonSprite;
                        image.color = Color.white;
                        image.type = Image.Type.Simple;
                        image.preserveAspect = true;
                    }
                }

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
                        badge.type = Image.Type.Simple;
                        badge.preserveAspect = true;
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            CurrencyUIPanelSimple coins,
            CurrencyUIPanelSimple diamonds,
            Image coinIcon,
            Image diamondIcon,
            Button settings,
            Button home,
            UILevelQuitPopUp popup,
            Sprite undo,
            Sprite hint,
            Sprite shuffle,
            Sprite badge,
            Sprite coinSprite,
            Sprite diamondSprite)
        {
            coinPanel = coins;
            diamondPanel = diamonds;
            coinIconImage = coinIcon;
            diamondIconImage = diamondIcon;
            settingsButton = settings;
            homeButton = home;
            pausePopup = popup;
            undoButtonSprite = undo;
            hintButtonSprite = hint;
            shuffleButtonSprite = shuffle;
            countBadgeSprite = badge;
            coinIconSprite = coinSprite;
            diamondIconSprite = diamondSprite;
        }
#endif
    }
}
