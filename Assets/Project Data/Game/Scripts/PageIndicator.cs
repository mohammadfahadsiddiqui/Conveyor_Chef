// using UnityEngine;
// using UnityEngine.UI;
// using System;

// namespace Watermelon.BusStop
// {
//     public class PageIndicator : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] Button button;
//         [SerializeField] Image backgroundImage;
//         [SerializeField] GameObject lockedIcon;
//         [SerializeField] GameObject newLabel; // Optional "New" label

//         [Header("Colors")]
//         [SerializeField] Color normalColor = Color.white;
//         [SerializeField] Color currentColor = Color.yellow;
//         [SerializeField] Color lockedColor = Color.gray;

//         private int pageIndex;
//         private bool isUnlocked;
//         private bool isCurrent;
//         private Action onClickCallback;

//         public void Setup(int pageIndex, bool isUnlocked, Action onClickCallback)
//         {
//             this.pageIndex = pageIndex;
//             this.isUnlocked = isUnlocked;
//             this.onClickCallback = onClickCallback;

//             // Setup button
//             button.interactable = isUnlocked;
//             button.onClick.RemoveAllListeners();
//             button.onClick.AddListener(OnClick);

//             // Update visual state
//             UpdateVisuals();
//         }

//         public void SetState(bool isCurrent, bool isUnlocked)
//         {
//             this.isCurrent = isCurrent;
//             this.isUnlocked = isUnlocked;
//             button.interactable = isUnlocked;
//             UpdateVisuals();
//         }

//         public void UpdateUnlockState(bool isUnlocked)
//         {
//             this.isUnlocked = isUnlocked;
//             button.interactable = isUnlocked;
//             UpdateVisuals();
//         }

//         private void UpdateVisuals()
//         {
//             // Update color based on state
//             if (!isUnlocked)
//             {
//                 backgroundImage.color = lockedColor;
//             }
//             else if (isCurrent)
//             {
//                 backgroundImage.color = currentColor;
//             }
//             else
//             {
//                 backgroundImage.color = normalColor;
//             }

//             // Show/hide locked icon
//             if (lockedIcon != null)
//             {
//                 lockedIcon.SetActive(!isUnlocked);
//             }

//             // Optional: Show "New" label for newly unlocked pages
//             if (newLabel != null)
//             {
//                 // You can add logic here to show "New" for recently unlocked pages
//                 newLabel.SetActive(false);
//             }
//         }

//         private void OnClick()
//         {
//             if (isUnlocked)
//             {
//                 onClickCallback?.Invoke();
//             }
//         }
//     }
// }


// using UnityEngine;
// using UnityEngine.UI;
// using System;
// using TMPro; // Add this if using TextMeshPro

// namespace Watermelon.BusStop
// {
//     public class PageIndicator : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] Button button;
//         [SerializeField] Image backgroundImage;
//         [SerializeField] GameObject lockedIcon;
//         [SerializeField] TextMeshProUGUI pageNumberText; // Use this for TextMeshPro
//         // OR use this for standard Unity Text:
//         // [SerializeField] Text pageNumberText;

//         [Header("Colors")]
//         [SerializeField] Color normalColor = Color.white;
//         [SerializeField] Color currentColor = Color.yellow;
//         [SerializeField] Color lockedColor = Color.gray;

//         private int pageIndex;
//         private bool isUnlocked;
//         private bool isCurrent;
//         private Action onClickCallback;

//         public void Setup(int pageIndex, bool isUnlocked, Action onClickCallback)
//         {
//             this.pageIndex = pageIndex;
//             this.isUnlocked = isUnlocked;
//             this.onClickCallback = onClickCallback;

//             // Setup button
//             button.interactable = isUnlocked;
//             button.onClick.RemoveAllListeners();
//             button.onClick.AddListener(OnClick);

//             // Set page number (display as 1-based instead of 0-based)
//             if (pageNumberText != null)
//             {
//                 pageNumberText.text = (pageIndex + 1).ToString();
//             }

//             // Update visual state
//             UpdateVisuals();
//         }

//         public void SetState(bool isCurrent, bool isUnlocked)
//         {
//             this.isCurrent = isCurrent;
//             this.isUnlocked = isUnlocked;
//             button.interactable = isUnlocked;
//             UpdateVisuals();
//         }

//         public void UpdateUnlockState(bool isUnlocked)
//         {
//             this.isUnlocked = isUnlocked;
//             button.interactable = isUnlocked;
//             UpdateVisuals();
//         }

//         private void UpdateVisuals()
//         {
//             // Update color based on state
//             if (!isUnlocked)
//             {
//                 backgroundImage.color = lockedColor;
//             }
//             else if (isCurrent)
//             {
//                 backgroundImage.color = currentColor;
//             }
//             else
//             {
//                 backgroundImage.color = normalColor;
//             }

//             // Show/hide locked icon
//             if (lockedIcon != null)
//             {
//                 lockedIcon.SetActive(!isUnlocked);
//             }

//             // Show/hide page number text (hide when locked, show lock icon instead)
//             if (pageNumberText != null)
//             {
//                 pageNumberText.gameObject.SetActive(isUnlocked);
//             }
//         }

//         private void OnClick()
//         {
//             if (isUnlocked)
//             {
//                 onClickCallback?.Invoke();
//             }
//         }
//     }
// }




using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Watermelon.BusStop
{
    public class PageIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Button button;
        [SerializeField] Image backgroundImage;
        [SerializeField] GameObject lockedIcon;
        [SerializeField] TextMeshProUGUI pageNumberText;

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

            // CHANGED: Button is always interactable now
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);

            // Set page number
            if (pageNumberText != null)
            {
                pageNumberText.text = (pageIndex + 1).ToString();
            }

            UpdateVisuals();
        }

        public void SetState(bool isCurrent, bool isUnlocked)
        {
            this.isCurrent = isCurrent;
            this.isUnlocked = isUnlocked;
            button.interactable = true; // CHANGED: Always interactable
            UpdateVisuals();
        }

        public void UpdateUnlockState(bool isUnlocked)
        {
            this.isUnlocked = isUnlocked;
            button.interactable = true; // CHANGED: Always interactable
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

            // Show lock icon on locked pages
            if (lockedIcon != null)
            {
                lockedIcon.SetActive(!isUnlocked);
            }

            // CHANGED: Always show page number
            if (pageNumberText != null)
            {
                pageNumberText.gameObject.SetActive(true);
                // Optional: Make text more transparent for locked pages
                if (!isUnlocked)
                {
                    Color textColor = pageNumberText.color;
                    textColor.a = 0.5f; // 50% transparent
                    pageNumberText.color = textColor;
                }
                else
                {
                    Color textColor = pageNumberText.color;
                    textColor.a = 1f; // Fully visible
                    pageNumberText.color = textColor;
                }
            }
        }

        private void OnClick()
        {
            // CHANGED: Always call callback, even for locked pages
            onClickCallback?.Invoke();
        }
    }
}