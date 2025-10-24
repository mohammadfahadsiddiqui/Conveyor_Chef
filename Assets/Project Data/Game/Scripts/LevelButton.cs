// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// namespace Watermelon.BusStop
// {
//     public class LevelButton : MonoBehaviour
//     {
//         [SerializeField] Button button;
//         [SerializeField] TextMeshProUGUI levelNumberText;
//         [SerializeField] GameObject lockedIcon;
//         [SerializeField] GameObject completedIcon;
//         [SerializeField] Image[] stars; // For star rating if you have one

//         private int levelIndex;
//         private bool isUnlocked;

//         public void Initialize(int levelIndex, bool isUnlocked, bool isCompleted, int starsEarned)
//         {
//             this.levelIndex = levelIndex;
//             this.isUnlocked = isUnlocked;

//             // Set level number (display as 1-based)
//             levelNumberText.text = (levelIndex + 1).ToString();

//             // Show/hide locked state
//             lockedIcon.SetActive(!isUnlocked);
//             completedIcon.SetActive(isCompleted);

//             // Enable/disable button
//             button.interactable = isUnlocked;

//             // Set stars if you have them
//             if (stars != null && stars.Length > 0)
//             {
//                 for (int i = 0; i < stars.Length; i++)
//                 {
//                     stars[i].gameObject.SetActive(i < starsEarned);
//                 }
//             }

//             // Add click listener
//             button.onClick.RemoveAllListeners();
//             button.onClick.AddListener(OnLevelButtonClicked);
//         }

//         private void OnLevelButtonClicked()
//         {
//             if (isUnlocked)
//             {
//                 LevelSelectionManager.Instance.LoadLevel(levelIndex);
//             }
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Watermelon.BusStop
{
    public class LevelButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Button button;
        [SerializeField] TextMeshProUGUI levelNumberText;
        [SerializeField] GameObject lockedOverlay;
        [SerializeField] GameObject completedCheckmark;
        [SerializeField] Image[] stars; // Optional: for star rating
        
        private int levelIndex;
        private bool isUnlocked;

        public void Setup(int levelIndex, bool isUnlocked, bool isCompleted, int starsEarned)
        {
            this.levelIndex = levelIndex;
            this.isUnlocked = isUnlocked;

            // Display level number (1-based)
            levelNumberText.text = (levelIndex + 1).ToString();
            
            // Show/hide locked state
            if (lockedOverlay != null)
                lockedOverlay.SetActive(!isUnlocked);
            
            // Show/hide completed checkmark
            if (completedCheckmark != null)
                completedCheckmark.SetActive(isCompleted);
            
            // Enable/disable button
            button.interactable = isUnlocked;

            // Setup stars (optional)
            if (stars != null && stars.Length > 0)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    stars[i].gameObject.SetActive(i < starsEarned);
                }
            }

            // Add click listener
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            if (isUnlocked)
            {
                LevelSelectionController.Instance.LoadSelectedLevel(levelIndex);
            }
        }
    }
}