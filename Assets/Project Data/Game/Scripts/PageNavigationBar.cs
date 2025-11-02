// using UnityEngine;
// using UnityEngine.UI;

// namespace Watermelon.BusStop
// {
//     public class PageNavigationBar : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] PageIndicator pageIndicatorPrefab;
//         [SerializeField] Transform indicatorsContainer;
//         [SerializeField] ScrollRect scrollRect; // Optional: for swipe navigation

//         [Header("Settings")]
//         [SerializeField] float indicatorSpacing = 80f;

//         private PageIndicator[] indicators;
//         private LevelSelectionController controller;
//         private int currentPage = 0;

//         public void Setup(int totalPages, LevelSelectionController controller)
//         {
//             this.controller = controller;
//             CreateIndicators(totalPages);
//         }

//         private void CreateIndicators(int totalPages)
//         {
//             indicators = new PageIndicator[totalPages];

//             for (int i = 0; i < totalPages; i++)
//             {
//                 PageIndicator indicator = Instantiate(pageIndicatorPrefab, indicatorsContainer);
                
//                 // Position indicator
//                 RectTransform rt = indicator.GetComponent<RectTransform>();
//                 rt.anchoredPosition = new Vector2(i * indicatorSpacing, 0);

//                 // Setup indicator
//                 int pageIndex = i;
//                 bool isUnlocked = controller.IsPageUnlocked(pageIndex);
//                 indicator.Setup(pageIndex, isUnlocked, () => OnPageIndicatorClicked(pageIndex));

//                 indicators[i] = indicator;
//             }
//         }

//         private void OnPageIndicatorClicked(int pageIndex)
//         {
//             controller.ShowPage(pageIndex);
//         }

//         public void SetCurrentPage(int pageIndex)
//         {
//             currentPage = pageIndex;

//             // Update all indicators
//             for (int i = 0; i < indicators.Length; i++)
//             {
//                 bool isCurrent = i == currentPage;
//                 bool isUnlocked = controller.IsPageUnlocked(i);
//                 indicators[i].SetState(isCurrent, isUnlocked);
//             }
//         }

//         // Optional: Add swipe support
//         public void OnSwipeLeft()
//         {
//             if (currentPage < indicators.Length - 1)
//             {
//                 controller.ShowPage(currentPage + 1);
//             }
//         }

//         public void OnSwipeRight()
//         {
//             if (currentPage > 0)
//             {
//                 controller.ShowPage(currentPage - 1);
//             }
//         }

//         // Call this when a page unlocks (e.g., after completing all levels in previous page)
//         public void RefreshIndicators()
//         {
//             for (int i = 0; i < indicators.Length; i++)
//             {
//                 bool isUnlocked = controller.IsPageUnlocked(i);
//                 indicators[i].UpdateUnlockState(isUnlocked);
//             }
//         }
//     }
// }


using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    public class PageNavigationBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PageIndicator pageIndicatorPrefab;
        [SerializeField] Transform indicatorsContainer;
        [SerializeField] ScrollRect scrollRect; // Optional: for swipe navigation

        [Header("Settings")]
        [SerializeField] float indicatorSpacing = 80f;

        private PageIndicator[] indicators;
        private LevelSelectionController controller;
        private int currentPage = 0;

        public void Setup(int totalPages, LevelSelectionController controller)
        {
            this.controller = controller;
            CreateIndicators(totalPages);
            
            // Scroll to beginning or last unlocked page after a frame
            StartCoroutine(ScrollToStartPosition());
        }

        private System.Collections.IEnumerator ScrollToStartPosition()
        {
            // Wait for layout to update
            yield return new WaitForEndOfFrame();
            
            // Reset scroll to beginning (leftmost position)
            if (scrollRect != null)
            {
                scrollRect.horizontalNormalizedPosition = 0f; // 0 = left, 1 = right
            }
        }

        private void CreateIndicators(int totalPages)
        {
            indicators = new PageIndicator[totalPages];

            for (int i = 0; i < totalPages; i++)
            {
                PageIndicator indicator = Instantiate(pageIndicatorPrefab, indicatorsContainer);
                
                // Position indicator
                RectTransform rt = indicator.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * indicatorSpacing, 0);

                // Setup indicator
                int pageIndex = i;
                bool isUnlocked = controller.IsPageUnlocked(pageIndex);
                indicator.Setup(pageIndex, isUnlocked, () => OnPageIndicatorClicked(pageIndex));

                indicators[i] = indicator;
            }
        }

        private void OnPageIndicatorClicked(int pageIndex)
        {
            controller.ShowPage(pageIndex);
        }

        public void SetCurrentPage(int pageIndex)
        {
            currentPage = pageIndex;

            // Update all indicators
            for (int i = 0; i < indicators.Length; i++)
            {
                bool isCurrent = i == currentPage;
                bool isUnlocked = controller.IsPageUnlocked(i);
                indicators[i].SetState(isCurrent, isUnlocked);
            }
        }

        // Optional: Add swipe support
        public void OnSwipeLeft()
        {
            if (currentPage < indicators.Length - 1)
            {
                controller.ShowPage(currentPage + 1);
            }
        }

        public void OnSwipeRight()
        {
            if (currentPage > 0)
            {
                controller.ShowPage(currentPage - 1);
            }
        }

        // Call this when a page unlocks (e.g., after completing all levels in previous page)
        public void RefreshIndicators()
        {
            for (int i = 0; i < indicators.Length; i++)
            {
                bool isUnlocked = controller.IsPageUnlocked(i);
                indicators[i].UpdateUnlockState(isUnlocked);
            }
        }
    }
}