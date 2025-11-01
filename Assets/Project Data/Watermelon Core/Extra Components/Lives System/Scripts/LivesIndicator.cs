// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.UI;

// namespace Watermelon
// {
//     public class LivesIndicator : MonoBehaviour
//     {
//         [Space]
//         [SerializeField] TextMeshProUGUI livesCountText;
//         [SerializeField] Image infinityImage;
//         [SerializeField] TextMeshProUGUI durationText;

//         [Space]
//         [SerializeField] Button addButton;
//         [SerializeField] AddLivesPanel addLivesPanel;

//         private LivesData Data { get; set; }

//         private bool isInitialised;

//         public void Init(LivesData data)
//         {
//             if (isInitialised) return;
            
//             Data = data;

//             if(addLivesPanel != null)
//             {
//                 addButton.gameObject.SetActive(true);
//                 addButton.onClick.AddListener(() => addLivesPanel.Show());
//             } else
//             {
//                 addButton.gameObject.SetActive(false);
//             }

//             isInitialised = true;
//         }

//         public void SetInfinite(bool isInfinite)
//         {
//             infinityImage.gameObject.SetActive(isInfinite);
//             livesCountText.gameObject.SetActive(!isInfinite);
//         }

//         public void SetLivesCount(int count)
//         {
//             if (!isInitialised) return;

//             livesCountText.text = count.ToString();

//             addButton.gameObject.SetActive(count != Data.maxLivesCount && addLivesPanel != null);
//             if(count == Data.maxLivesCount)
//             {
//                 FullText();
//             }
//         }

//         public void SetDuration(TimeSpan duration) 
//         {
//             if (!isInitialised) return;

//             if (duration >= TimeSpan.FromHours(1))
//             {
//                 durationText.text = string.Format(Data.longTimespanFormat, duration);
//             }
//             else
//             {
//                 durationText.text = string.Format(Data.timespanFormat, duration);
//             }

//             SetTextSize(!addButton.gameObject.activeSelf);
//         }

//         public void FullText()
//         {
//             if (!isInitialised) return;

//             durationText.text = Data.fullText;

//             SetTextSize(true);
//         }

//         private void SetTextSize(bool fullPanel)
//         {
//             if(fullPanel)
//             {
//                 durationText.rectTransform.offsetMin = new Vector2(70, 0);
//                 durationText.rectTransform.offsetMax = new Vector2(-38, 0);
//             }
//             else
//             {
//                 durationText.rectTransform.offsetMin = new Vector2(95, 0);
//                 durationText.rectTransform.offsetMax = new Vector2(-100, 0);
//             }
//         }

//         private void OnEnable()
//         {
//             LivesManager.AddIndicator(this);
//         }

//         private void OnDisable()
//         {
//             LivesManager.RemoveIndicator(this);
//         }
//     }
// }


using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Watermelon
{
    public class LivesIndicator : MonoBehaviour
    {
        [Space]
        [SerializeField] TextMeshProUGUI livesCountText;
        [SerializeField] Image infinityImage;
        [SerializeField] TextMeshProUGUI durationText;

        [Space]
        [SerializeField] Button addButton;
        [SerializeField] AddLivesPanel addLivesPanel;

        private LivesData Data { get; set; }

        private bool isInitialised;

        public void Init(LivesData data)
        {
            if (isInitialised)
            {
                Debug.Log($"[LivesIndicator] {gameObject.name} - Already initialized, skipping");
                return;
            }
            
            Data = data;

            if(addLivesPanel != null)
            {
                addButton.gameObject.SetActive(true);
                addButton.onClick.AddListener(() => addLivesPanel.Show());
            } else
            {
                addButton.gameObject.SetActive(false);
            }

            isInitialised = true;
            Debug.Log($"[LivesIndicator] {gameObject.name} - Initialized with maxLives: {data.maxLivesCount}");
        }

        public void SetInfinite(bool isInfinite)
        {
            Debug.Log($"[LivesIndicator] {gameObject.name} - SetInfinite: {isInfinite}");
            
            infinityImage.gameObject.SetActive(isInfinite);
            livesCountText.gameObject.SetActive(!isInfinite);
        }

        public void SetLivesCount(int count)
        {
            if (!isInitialised)
            {
                Debug.LogWarning($"[LivesIndicator] {gameObject.name} - SetLivesCount called but not initialized! Count: {count}");
                return;
            }

            Debug.Log($"[LivesIndicator] {gameObject.name} - SetLivesCount: {count} (Max: {Data.maxLivesCount})");
            
            livesCountText.text = count.ToString();

            addButton.gameObject.SetActive(count != Data.maxLivesCount && addLivesPanel != null);
            if(count == Data.maxLivesCount)
            {
                FullText();
            }
        }

        public void SetDuration(TimeSpan duration) 
        {
            if (!isInitialised)
            {
                Debug.LogWarning($"[LivesIndicator] {gameObject.name} - SetDuration called but not initialized!");
                return;
            }

            if (duration >= TimeSpan.FromHours(1))
            {
                durationText.text = string.Format(Data.longTimespanFormat, duration);
            }
            else
            {
                durationText.text = string.Format(Data.timespanFormat, duration);
            }

            SetTextSize(!addButton.gameObject.activeSelf);
        }

        public void FullText()
        {
            if (!isInitialised)
            {
                Debug.LogWarning($"[LivesIndicator] {gameObject.name} - FullText called but not initialized!");
                return;
            }

            Debug.Log($"[LivesIndicator] {gameObject.name} - FullText set to: {Data.fullText}");
            
            durationText.text = Data.fullText;

            SetTextSize(true);
        }

        private void SetTextSize(bool fullPanel)
        {
            if(fullPanel)
            {
                durationText.rectTransform.offsetMin = new Vector2(70, 0);
                durationText.rectTransform.offsetMax = new Vector2(-38, 0);
            }
            else
            {
                durationText.rectTransform.offsetMin = new Vector2(95, 0);
                durationText.rectTransform.offsetMax = new Vector2(-100, 0);
            }
        }

        private void OnEnable()
        {
            Debug.Log($"[LivesIndicator] {gameObject.name} - OnEnable - Registering with LivesManager");
            LivesManager.AddIndicator(this);
        }

        private void OnDisable()
        {
            Debug.Log($"[LivesIndicator] {gameObject.name} - OnDisable - Unregistering from LivesManager");
            LivesManager.RemoveIndicator(this);
        }
    }
}
