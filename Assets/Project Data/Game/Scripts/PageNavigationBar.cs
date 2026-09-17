

// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;

// namespace Watermelon.BusStop
// {
//     public class PageNavigationBar : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] PageIndicator pageIndicatorPrefab;
//         [SerializeField] ScrollRect indicatorsScrollRect; // ADD THIS - the ScrollRect for indicators
//         [SerializeField] Transform indicatorsContainer;
//         [SerializeField] Button leftButton;
//         [SerializeField] Button rightButton;

//         [Header("Settings")]
//         [SerializeField] float scrollDuration = 0.3f;

//         private PageIndicator[] indicators;
//         private LevelSelectionController controller;
//         private int currentPage = 0;
//         private Coroutine scrollCoroutine;

//         public void Setup(int totalPages, LevelSelectionController controller)
//         {
//             this.controller = controller;
//             CreateIndicators(totalPages);
//             SetupNavigationButtons();
//             UpdateNavigationButtons();
//         }

//         private void SetupNavigationButtons()
//         {
//             if (leftButton != null)
//             {
//                 leftButton.onClick.RemoveAllListeners();
//                 leftButton.onClick.AddListener(OnLeftButtonClicked);
//             }

//             if (rightButton != null)
//             {
//                 rightButton.onClick.RemoveAllListeners();
//                 rightButton.onClick.AddListener(OnRightButtonClicked);
//             }
//         }

//         private void OnLeftButtonClicked()
//         {
//             if (currentPage > 0)
//             {
//                 controller.ShowPage(currentPage - 1);
//             }
//         }

//         private void OnRightButtonClicked()
//         {
//             if (currentPage < indicators.Length - 1)
//             {
//                 int nextPage = currentPage + 1;
//                 if (controller.IsPageUnlocked(nextPage))
//                 {
//                     controller.ShowPage(nextPage);
//                 }
//             }
//         }

//         private void UpdateNavigationButtons()
//         {
//             if (leftButton != null)
//             {
//                 leftButton.interactable = currentPage > 0;
//             }

//             if (rightButton != null)
//             {
//                 bool hasNextPage = currentPage < indicators.Length - 1;
//                 bool nextPageUnlocked = hasNextPage && controller.IsPageUnlocked(currentPage + 1);
//                 rightButton.interactable = hasNextPage && nextPageUnlocked;
//             }
//         }

//         private void CreateIndicators(int totalPages)
//         {
//             indicators = new PageIndicator[totalPages];

//             for (int i = 0; i < totalPages; i++)
//             {
//                 PageIndicator indicator = Instantiate(pageIndicatorPrefab, indicatorsContainer);

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

//             // Update indicator states
//             for (int i = 0; i < indicators.Length; i++)
//             {
//                 bool isCurrent = i == currentPage;
//                 bool isUnlocked = controller.IsPageUnlocked(i);
//                 indicators[i].SetState(isCurrent, isUnlocked);
//             }

//             // SCROLL THE INDICATORS!
//             ScrollIndicators(pageIndex);

//             UpdateNavigationButtons();
//         }

//         private void ScrollIndicators(int pageIndex)
//         {
//             if (indicatorsScrollRect == null || indicators.Length <= 1)
//                 return;

//             // Stop any ongoing scroll
//             if (scrollCoroutine != null)
//             {
//                 StopCoroutine(scrollCoroutine);
//             }

//             // Calculate position (same as level pages)
//             float targetPosition = (float)pageIndex / (float)(indicators.Length - 1);

//             scrollCoroutine = StartCoroutine(SmoothScrollTo(targetPosition));
//         }

//         private IEnumerator SmoothScrollTo(float targetPosition)
//         {
//             float startPosition = indicatorsScrollRect.horizontalNormalizedPosition;
//             float elapsed = 0f;

//             while (elapsed < scrollDuration)
//             {
//                 elapsed += Time.deltaTime;
//                 float t = elapsed / scrollDuration;
//                 t = t * t * (3f - 2f * t);
                
//                 indicatorsScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
//                 yield return null;
//             }

//             indicatorsScrollRect.horizontalNormalizedPosition = targetPosition;
//             scrollCoroutine = null;
//         }

//         public void RefreshIndicators()
//         {
//             for (int i = 0; i < indicators.Length; i++)
//             {
//                 bool isUnlocked = controller.IsPageUnlocked(i);
//                 indicators[i].UpdateUnlockState(isUnlocked);
//             }
            
//             UpdateNavigationButtons();
//         }
//     }
// }


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Watermelon.BusStop
{
    public class PageNavigationBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PageIndicator pageIndicatorPrefab;
        [SerializeField] ScrollRect indicatorsScrollRect;
        [SerializeField] Transform indicatorsContainer;
        [SerializeField] Button leftButton;
        [SerializeField] Button rightButton;

        [Header("Settings")]
        [SerializeField] float scrollDuration = 0.3f;

        private PageIndicator[] indicators;
        private LevelSelectionController controller;
        private int currentPage = 0;
        private Coroutine scrollCoroutine;

        public void Setup(int totalPages, LevelSelectionController controller)
        {
            this.controller = controller;
            CreateIndicators(totalPages);
            SetupNavigationButtons();
            UpdateNavigationButtons();
        }

        private void SetupNavigationButtons()
        {
            if (leftButton != null)
            {
                leftButton.onClick.RemoveAllListeners();
                leftButton.onClick.AddListener(OnLeftButtonClicked);
            }

            if (rightButton != null)
            {
                rightButton.onClick.RemoveAllListeners();
                rightButton.onClick.AddListener(OnRightButtonClicked);
            }
        }

        private void OnLeftButtonClicked()
        {
            if (currentPage > 0)
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
                // CHANGED: Always allow navigation, don't check if unlocked
                controller.ShowPage(currentPage - 1);
            }
        }

        private void OnRightButtonClicked()
        {
            if (currentPage < indicators.Length - 1)
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
                // CHANGED: Always allow navigation to next page
                controller.ShowPage(currentPage + 1);
            }
        }

        private void UpdateNavigationButtons()
        {
            if (leftButton != null)
            {
                // Disable left button only if at first page
                leftButton.interactable = currentPage > 0;
            }

            if (rightButton != null)
            {
                // Disable right button only if at last page
                bool hasNextPage = currentPage < indicators.Length - 1;
                rightButton.interactable = hasNextPage;
                
                // REMOVED: Lock check for next page
                // bool nextPageUnlocked = hasNextPage && controller.IsPageUnlocked(currentPage + 1);
                // rightButton.interactable = hasNextPage && nextPageUnlocked;
            }
        }

        private void CreateIndicators(int totalPages)
        {
            indicators = new PageIndicator[totalPages];

            for (int i = 0; i < totalPages; i++)
            {
                PageIndicator indicator = Instantiate(pageIndicatorPrefab, indicatorsContainer);

                int pageIndex = i;
                bool isUnlocked = controller.IsPageUnlocked(pageIndex);
                indicator.Setup(pageIndex, isUnlocked, () => OnPageIndicatorClicked(pageIndex));

                indicators[i] = indicator;
            }
        }

        private void OnPageIndicatorClicked(int pageIndex)
        {
            // CHANGED: Always allow clicking to any page
            controller.ShowPage(pageIndex);
        }

        public void SetCurrentPage(int pageIndex)
        {
            currentPage = pageIndex;

            // Update indicator states
            for (int i = 0; i < indicators.Length; i++)
            {
                bool isCurrent = i == currentPage;
                bool isUnlocked = controller.IsPageUnlocked(i);
                indicators[i].SetState(isCurrent, isUnlocked);
            }

            // Scroll the indicators
            ScrollIndicators(pageIndex);

            UpdateNavigationButtons();
        }

        private void ScrollIndicators(int pageIndex)
        {
            if (indicatorsScrollRect == null || indicators.Length <= 1)
                return;

            // Stop any ongoing scroll
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }

            // Calculate position (same as level pages)
            float targetPosition = (float)pageIndex / (float)(indicators.Length - 1);

            scrollCoroutine = StartCoroutine(SmoothScrollTo(targetPosition));
        }

        private IEnumerator SmoothScrollTo(float targetPosition)
        {
            float startPosition = indicatorsScrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;

            while (elapsed < scrollDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scrollDuration;
                t = t * t * (3f - 2f * t);
                
                indicatorsScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            indicatorsScrollRect.horizontalNormalizedPosition = targetPosition;
            scrollCoroutine = null;
        }

        public void RefreshIndicators()
        {
            for (int i = 0; i < indicators.Length; i++)
            {
                bool isUnlocked = controller.IsPageUnlocked(i);
                indicators[i].UpdateUnlockState(isUnlocked);
            }
            
            UpdateNavigationButtons();
        }
    }
}