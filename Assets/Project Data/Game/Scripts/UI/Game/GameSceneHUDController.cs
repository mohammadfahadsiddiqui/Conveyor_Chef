using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;

namespace Watermelon
{
    /// <summary>
    /// Runtime bridge for the NEW editable Game UI.
    /// The layout itself is fully serialized by GameSceneUIBuilder.
    /// This script only connects existing game systems to the new authored controls.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneHUDController : MonoBehaviour
    {
        [Header("HUD Buttons")]
        [SerializeField] private Button coinPlusButton;
        [SerializeField] private Button diamondPlusButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button homeButton;

        [Header("Existing Popup Logic")]
        [SerializeField] private UILevelQuitPopUp pausePopup;

        [Header("Power-up Artwork")]
        [SerializeField] private Sprite undoButtonSprite;
        [SerializeField] private Sprite hintButtonSprite;
        [SerializeField] private Sprite shuffleButtonSprite;
        [SerializeField] private Sprite countBadgeSprite;

        private PUUIController lastPowerUpsController;
        private int lastPowerUpCount = -1;

        private void Awake()
        {
            if (coinPlusButton != null)
                coinPlusButton.onClick.AddListener(OpenCurrencyStore);

            if (diamondPlusButton != null)
                diamondPlusButton.onClick.AddListener(OpenCurrencyStore);

            // The game did not previously have a dedicated settings popup in UIGame.
            // Reuse the existing safe pause/restart popup so this button is functional
            // without introducing a second SettingsPanel singleton.
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenPauseOptions);

            if (homeButton != null)
                homeButton.onClick.AddListener(ReturnToLevelSelection);
        }

        private void Start()
        {
            TrySkinPowerUps(true);
        }

        private void Update()
        {
            TrySkinPowerUps(false);
        }

        private static void OpenCurrencyStore()
        {
            if (UIController.GetPage<UIIAPStore>() != null)
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
                UIController.ShowPage<UIIAPStore>();
            }
        }

        private void OpenPauseOptions()
        {
            if (pausePopup != null)
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
                pausePopup.Show();
            }
        }

        private static void ReturnToLevelSelection()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
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

                // Generated power-up artwork already contains the symbol.
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
            Button coinPlus,
            Button diamondPlus,
            Button settings,
            Button home,
            UILevelQuitPopUp popup,
            Sprite undo,
            Sprite hint,
            Sprite shuffle,
            Sprite badge)
        {
            coinPlusButton = coinPlus;
            diamondPlusButton = diamondPlus;
            settingsButton = settings;
            homeButton = home;
            pausePopup = popup;
            undoButtonSprite = undo;
            hintButtonSprite = hint;
            shuffleButtonSprite = shuffle;
            countBadgeSprite = badge;
        }
#endif
    }
}
