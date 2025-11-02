using UnityEngine;
using UnityEngine.UI;
using System;

namespace Watermelon.BusStop
{
    public class PageIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Button button;
        [SerializeField] Image backgroundImage;
        [SerializeField] GameObject lockedIcon;
        [SerializeField] GameObject newLabel; // Optional "New" label

        [Header("Colors")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color currentColor = Color.yellow;
        [SerializeField] Color lockedColor = Color.gray;

        private int pageIndex;
        private bool isUnlocked;
        private bool isCurrent;
        private Action onClickCallback;

        public void Setup(int pageIndex, bool isUnlocked, Action onClickCallback)
        {
            this.pageIndex = pageIndex;
            this.isUnlocked = isUnlocked;
            this.onClickCallback = onClickCallback;

            // Setup button
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);

            // Update visual state
            UpdateVisuals();
        }

        public void SetState(bool isCurrent, bool isUnlocked)
        {
            this.isCurrent = isCurrent;
            this.isUnlocked = isUnlocked;
            button.interactable = isUnlocked;
            UpdateVisuals();
        }

        public void UpdateUnlockState(bool isUnlocked)
        {
            this.isUnlocked = isUnlocked;
            button.interactable = isUnlocked;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Update color based on state
            if (!isUnlocked)
            {
                backgroundImage.color = lockedColor;
            }
            else if (isCurrent)
            {
                backgroundImage.color = currentColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }

            // Show/hide locked icon
            if (lockedIcon != null)
            {
                lockedIcon.SetActive(!isUnlocked);
            }

            // Optional: Show "New" label for newly unlocked pages
            if (newLabel != null)
            {
                // You can add logic here to show "New" for recently unlocked pages
                newLabel.SetActive(false);
            }
        }

        private void OnClick()
        {
            if (isUnlocked)
            {
                onClickCallback?.Invoke();
            }
        }
    }
}