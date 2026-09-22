using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Behaviour-only wrapper for one designer-authored level card.
    /// Runtime may swap state sprites/text and toggle lock/completion visuals,
    /// but it never creates, reparents, moves or resizes UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectionLevelCard : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int slotIndex;

        [Header("Interaction")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Button actionButton;

        [Header("Visuals")]
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image numberBadge;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image[] stars;
        [SerializeField] private Image actionButtonImage;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject completedBadge;

        [Header("State Art")]
        [SerializeField] private Sprite selectedFrameSprite;
        [SerializeField] private Sprite unlockedFrameSprite;
        [SerializeField] private Sprite lockedFrameSprite;
        [SerializeField] private Sprite playButtonSprite;
        [SerializeField] private Sprite lockedButtonSprite;

        private LevelSelectionController owner;
        private bool unlocked;

        public int SlotIndex => slotIndex;

        public void Bind(LevelSelectionController controller)
        {
            owner = controller;

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(HandleSelected);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(HandleAction);
            }
        }

        public void Refresh(
            int gameplayLevelIndex,
            string missionTitle,
            Sprite thumbnail,
            bool isUnlocked,
            bool isSelected,
            bool isCompleted,
            int starsEarned)
        {
            unlocked = isUnlocked;

            if (numberText != null)
                numberText.text = (slotIndex + 1).ToString();

            if (titleText != null)
                titleText.text = missionTitle;

            if (thumbnailImage != null && thumbnail != null)
                thumbnailImage.sprite = thumbnail;

            if (cardFrame != null)
            {
                if (!isUnlocked && lockedFrameSprite != null)
                    cardFrame.sprite = lockedFrameSprite;
                else if (isSelected && selectedFrameSprite != null)
                    cardFrame.sprite = selectedFrameSprite;
                else if (unlockedFrameSprite != null)
                    cardFrame.sprite = unlockedFrameSprite;

                cardFrame.color = Color.white;
            }

            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            if (completedBadge != null)
                completedBadge.SetActive(isCompleted);

            starsEarned = Mathf.Clamp(starsEarned, 0, 3);
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null)
                        continue;

                    stars[i].color = i < starsEarned
                        ? Color.white
                        : new Color(0.16f, 0.21f, 0.30f, 0.90f);
                }
            }

            if (actionButtonImage != null)
            {
                Sprite target = isUnlocked ? playButtonSprite : lockedButtonSprite;
                if (target != null)
                    actionButtonImage.sprite = target;

                actionButtonImage.color = Color.white;
            }

            if (actionText != null)
                actionText.text = isUnlocked ? "PLAY" : "LOCKED";

            if (actionButton != null)
                actionButton.interactable = isUnlocked;

            if (selectButton != null)
                selectButton.interactable = true;
        }

        private void HandleSelected()
        {
            owner?.HandleCardPressed(slotIndex);
        }

        private void HandleAction()
        {
            if (unlocked)
                owner?.HandlePlayPressed(slotIndex);
            else
                owner?.HandleLockedPressed(slotIndex);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            int index,
            Button cardButton,
            Button playButton,
            Image frame,
            Image thumbnail,
            Image badge,
            TextMeshProUGUI number,
            TextMeshProUGUI title,
            Image[] starImages,
            Image playButtonImage,
            TextMeshProUGUI playText,
            GameObject lockRoot,
            GameObject completedRoot,
            Sprite selectedFrame,
            Sprite unlockedFrame,
            Sprite lockedFrame,
            Sprite playSprite,
            Sprite lockedButton)
        {
            slotIndex = index;
            selectButton = cardButton;
            actionButton = playButton;
            cardFrame = frame;
            thumbnailImage = thumbnail;
            numberBadge = badge;
            numberText = number;
            titleText = title;
            stars = starImages;
            actionButtonImage = playButtonImage;
            actionText = playText;
            lockOverlay = lockRoot;
            completedBadge = completedRoot;
            selectedFrameSprite = selectedFrame;
            unlockedFrameSprite = unlockedFrame;
            lockedFrameSprite = lockedFrame;
            playButtonSprite = playSprite;
            lockedButtonSprite = lockedButton;
        }
#endif
    }
}
