// using UnityEngine;
// using UnityEngine.SceneManagement;

// namespace Watermelon.BusStop
// {
//     public class LevelSelectionController : MonoBehaviour
//     {
//         public static LevelSelectionController Instance { get; private set; }

//         [Header("References")]
//         [SerializeField] LevelDatabase levelDatabase;
//         [SerializeField] LevelPageController pageControllerPrefab;
//         [SerializeField] Transform pagesContainer;
//         [SerializeField] PageNavigationBar navigationBar;

//         [Header("Settings")]
//         [SerializeField] int levelsPerPage = 5;

//         private LevelSave levelSave;
//         private LevelPageController[] pages;
//         private int currentPageIndex = 0;

        

//         private void Awake()
//         {
//             Instance = this;
            
//             SaveController.Initialise(useAutoSave: false);
//             levelSave = SaveController.GetSaveObject<LevelSave>("level");
//         }

//         private void Start()
//         {
//             CreatePages();
//             InitializeNavigationBar();
//             ShowPage(0);
//         }

//         private void CreatePages()
//         {
//             int totalLevels = levelDatabase.Levels.Length;
//             int totalPages = Mathf.CeilToInt((float)totalLevels / levelsPerPage);

//             pages = new LevelPageController[totalPages];

//             for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
//             {
//                 LevelPageController page = Instantiate(pageControllerPrefab, pagesContainer);
//                 page.gameObject.SetActive(false);

//                 // Calculate which levels belong to this page
//                 int startLevelIndex = pageIndex * levelsPerPage;
//                 int endLevelIndex = Mathf.Min(startLevelIndex + levelsPerPage, totalLevels);
//                 int levelsInPage = endLevelIndex - startLevelIndex;

//                 // Setup the page
//                 page.Setup(pageIndex, startLevelIndex, levelsInPage);

//                 pages[pageIndex] = page;
//             }
//         }

//         private void InitializeNavigationBar()
//         {
//             int totalPages = pages.Length;
//             navigationBar.Setup(totalPages, this);
//         }

//         public void ShowPage(int pageIndex)
//         {
//             if (pageIndex < 0 || pageIndex >= pages.Length)
//                 return;

//             // Check if page is unlocked
//             if (!IsPageUnlocked(pageIndex))
//             {
//                 Debug.Log($"Page {pageIndex} is locked!");
//                 return;
//             }

//             // Hide current page
//             if (currentPageIndex >= 0 && currentPageIndex < pages.Length)
//             {
//                 pages[currentPageIndex].gameObject.SetActive(false);
//             }

//             // Show new page
//             currentPageIndex = pageIndex;
//             pages[currentPageIndex].gameObject.SetActive(true);

//             // Update navigation bar
//             navigationBar.SetCurrentPage(currentPageIndex);
//         }

//         public bool IsPageUnlocked(int pageIndex)
//         {
//             if (pageIndex == 0) return true;

//             // Check if all levels in previous page are completed
//             int previousPageStartLevel = (pageIndex - 1) * levelsPerPage;
//             int previousPageEndLevel = Mathf.Min(previousPageStartLevel + levelsPerPage, levelDatabase.Levels.Length);

//             for (int i = previousPageStartLevel; i < previousPageEndLevel; i++)
//             {
//                 if (!LevelController.IsLevelCompleted(i))
//                 {
//                     return false;
//                 }
//             }

//             return true;
//         }

//         // public void LoadSelectedLevel(int levelIndex)
//         // {
//         //     Debug.Log($"[LevelSelection] Loading level {levelIndex + 1}");

//         //     LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");

//         //     levelSave.selectedLevelIndex = levelIndex;
//         //     levelSave.isPlayingFromLevelSelection = true;
//         //     levelSave.ReplayingLevelAgain = false;

//         //     SaveController.MarkAsSaveIsRequired();
//         //     SaveController.Save(true);

//         //     Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("Game");
//         // }

//         // public void LoadSelectedLevel(int levelIndex)
//         // {
//         //     Debug.Log($"[LevelSelection] Loading level {levelIndex + 1}");

//         //     // IMPORTANT: Re-get the save object to ensure we're working with the latest
//         //     LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");

//         //     // Set the selected level
//         //     levelSave.selectedLevelIndex = levelIndex;
//         //     levelSave.isPlayingFromLevelSelection = true;
//         //     levelSave.ReplayingLevelAgain = false;

//         //     Debug.Log($"[LevelSelection] Set levelSave values - selectedIndex: {levelSave.selectedLevelIndex}, isPlayingFromLevelSelection: {levelSave.isPlayingFromLevelSelection}");

//         //     // CRITICAL: Force save immediately and synchronously
//         //     SaveController.MarkAsSaveIsRequired();
//         //     SaveController.Save(true); // true = force immediate save

//         //     // Verify save was successful
//         //     LevelSave verifyLoad = SaveController.GetSaveObject<LevelSave>("level");
//         //     Debug.Log($"[LevelSelection] Verified save - isPlayingFromLevelSelection: {verifyLoad.isPlayingFromLevelSelection}, selectedIndex: {verifyLoad.selectedLevelIndex}");

//         //     // Load the game scene directly (bypass loading screen since we're already in game)
//         //     Debug.Log("[LevelSelection] Loading Game scene now...");
//         //     SceneManager.LoadScene("Game");
//         // }

//         public void LoadSelectedLevel(int levelIndex)
//         {
//             Debug.Log($"Loading level {levelIndex + 1}");

//             LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
//             levelSave.selectedLevelIndex = levelIndex;
//             levelSave.isPlayingFromLevelSelection = true;
//             levelSave.ReplayingLevelAgain = false;

//             SaveController.MarkAsSaveIsRequired();
//             SaveController.Save(true);

//             // Just load the scene
//             SceneManager.LoadScene("Game");
//         }


//         public void BackToMainMenu()
//         {
//             if (PlayerPrefs.GetInt(FromCountryMapKey, 0) == 1)
            {
                PlayerPrefs.DeleteKey(FromCountryMapKey);
                PlayerPrefs.Save();
                Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("CountryMap");
            }
            else
            {
                Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("menu");
            }
//         }
//     }
// }



// using UnityEngine;
// using UnityEngine.SceneManagement;

// namespace Watermelon.BusStop
// {
//     public class LevelSelectionController : MonoBehaviour
//     {
//         public static LevelSelectionController Instance { get; private set; }

//         [Header("References")]
//         [SerializeField] LevelDatabase levelDatabase;
//         [SerializeField] LevelPageController pageControllerPrefab;
//         [SerializeField] Transform pagesContainer;
//         [SerializeField] PageNavigationBar navigationBar;

//         [Header("Animation")]
//         [SerializeField] ScooterAnimationController scooterAnimationController;

//         [Header("Settings")]
//         [SerializeField] int levelsPerPage = 5;

//         private LevelSave levelSave;
//         private LevelPageController[] pages;
//         private int currentPageIndex = 0;

//         private void Awake()
//         {
//             Instance = this;
            
//             SaveController.Initialise(useAutoSave: false);
//             levelSave = SaveController.GetSaveObject<LevelSave>("level");
//         }

//         private void Start()
//         {
//             CreatePages();
//             InitializeNavigationBar();
//             ShowPage(0);
            
//             // Start animations
//             if (scooterAnimationController != null)
//             {
//                 scooterAnimationController.StartAnimations();
//             }
//         }

//         private void OnDestroy()
//         {
//             // Stop animations when leaving the scene
//             if (scooterAnimationController != null)
//             {
//                 scooterAnimationController.StopAnimations();
//             }
//         }

//         private void CreatePages()
//         {
//             int totalLevels = levelDatabase.Levels.Length;
//             int totalPages = Mathf.CeilToInt((float)totalLevels / levelsPerPage);

//             pages = new LevelPageController[totalPages];

//             for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
//             {
//                 LevelPageController page = Instantiate(pageControllerPrefab, pagesContainer);
//                 page.gameObject.SetActive(false);

//                 // Calculate which levels belong to this page
//                 int startLevelIndex = pageIndex * levelsPerPage;
//                 int endLevelIndex = Mathf.Min(startLevelIndex + levelsPerPage, totalLevels);
//                 int levelsInPage = endLevelIndex - startLevelIndex;

//                 // Setup the page
//                 page.Setup(pageIndex, startLevelIndex, levelsInPage);

//                 pages[pageIndex] = page;
//             }
//         }

//         private void InitializeNavigationBar()
//         {
//             int totalPages = pages.Length;
//             navigationBar.Setup(totalPages, this);
//         }

//         public void ShowPage(int pageIndex)
//         {
//             if (pageIndex < 0 || pageIndex >= pages.Length)
//                 return;

//             // Check if page is unlocked
//             if (!IsPageUnlocked(pageIndex))
//             {
//                 Debug.Log($"Page {pageIndex} is locked!");
//                 return;
//             }

//             // Hide current page
//             if (currentPageIndex >= 0 && currentPageIndex < pages.Length)
//             {
//                 pages[currentPageIndex].gameObject.SetActive(false);
//             }

//             // Show new page
//             currentPageIndex = pageIndex;
//             pages[currentPageIndex].gameObject.SetActive(true);

//             // Update navigation bar
//             navigationBar.SetCurrentPage(currentPageIndex);
//         }

//         public bool IsPageUnlocked(int pageIndex)
//         {
//             if (pageIndex == 0) return true;

//             // Check if all levels in previous page are completed
//             int previousPageStartLevel = (pageIndex - 1) * levelsPerPage;
//             int previousPageEndLevel = Mathf.Min(previousPageStartLevel + levelsPerPage, levelDatabase.Levels.Length);

//             for (int i = previousPageStartLevel; i < previousPageEndLevel; i++)
//             {
//                 if (!LevelController.IsLevelCompleted(i))
//                 {
//                     return false;
//                 }
//             }

//             return true;
//         }

//         public void LoadSelectedLevel(int levelIndex)
//         {
//             Debug.Log($"Loading level {levelIndex + 1}");

//             LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
//             levelSave.selectedLevelIndex = levelIndex;
//             levelSave.isPlayingFromLevelSelection = true;
//             levelSave.ReplayingLevelAgain = false;

//             SaveController.MarkAsSaveIsRequired();
//             SaveController.Save(true);

//             // Stop animations before loading
//             if (scooterAnimationController != null)
//             {
//                 scooterAnimationController.StopAnimations();
//             }

//             // Just load the scene
//             SceneManager.LoadScene("Game");
//         }

//         public void BackToMainMenu()
//         {
//             // Stop animations before loading
//             if (scooterAnimationController != null)
//             {
//                 scooterAnimationController.StopAnimations();
//             }
            
//             SceneManager.LoadScene("Menu");
//         }
//     }
// }



using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon.BusStop
{
    public class LevelSelectionController : MonoBehaviour
    {
        public static LevelSelectionController Instance { get; private set; }

        [Header("References")]
        [SerializeField] LevelDatabase levelDatabase;
        [SerializeField] LevelPageController pageControllerPrefab;
        [SerializeField] Transform pagesContainer;
        [SerializeField] PageNavigationBar navigationBar;

        [Header("Animation")]
        [SerializeField] ScooterAnimationController scooterAnimationController;

        [Header("Settings")]
        [SerializeField] int levelsPerPage = 5;
        [SerializeField] bool allowNavigationToLockedPages = true; // NEW: Allow viewing locked pages

        private LevelSave levelSave;
        private LevelPageController[] pages;
        private int currentPageIndex = 0;

        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const string FromCountryMapKey = "CC_LevelSelection_FromCountryMap";
        private const int LevelsPerContinent = 15;
        private const int LevelsPerCountry = 3;

        private void Awake()
        {
            Instance = this;
            
            SaveController.Initialise(useAutoSave: false);
            levelSave = SaveController.GetSaveObject<LevelSave>("level");
        }

        private void Start()
        {
            CreatePages();
            InitializeNavigationBar();

            int initialPage = 0;
            if (PlayerPrefs.GetInt(FromCountryMapKey, 0) == 1)
            {
                int continent = Mathf.Max(0, PlayerPrefs.GetInt(SelectedContinentKey, 0));
                int country = Mathf.Clamp(PlayerPrefs.GetInt(SelectedCountryKey, 0), 0, 4);
                int firstCountryLevel = continent * LevelsPerContinent + country * LevelsPerCountry;
                initialPage = Mathf.Clamp(firstCountryLevel / Mathf.Max(1, levelsPerPage), 0, pages.Length - 1);
            }

            ShowPage(initialPage);
            
            // Start animations
            if (scooterAnimationController != null)
            {
                scooterAnimationController.StartAnimations();
            }
        }

        private void OnDestroy()
        {
            // Stop animations when leaving the scene
            if (scooterAnimationController != null)
            {
                scooterAnimationController.StopAnimations();
            }
        }

        private void CreatePages()
        {
            int totalLevels = levelDatabase.Levels.Length;
            int totalPages = Mathf.CeilToInt((float)totalLevels / levelsPerPage);

            pages = new LevelPageController[totalPages];

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                LevelPageController page = Instantiate(pageControllerPrefab, pagesContainer);
                page.gameObject.SetActive(false);

                // Calculate which levels belong to this page
                int startLevelIndex = pageIndex * levelsPerPage;
                int endLevelIndex = Mathf.Min(startLevelIndex + levelsPerPage, totalLevels);
                int levelsInPage = endLevelIndex - startLevelIndex;

                // Setup the page
                page.Setup(pageIndex, startLevelIndex, levelsInPage);

                pages[pageIndex] = page;
            }
        }

        

        private void InitializeNavigationBar()
        {
            int totalPages = pages.Length;
            navigationBar.Setup(totalPages, this);
        }

        public void ShowPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= pages.Length)
                return;

            // REMOVED: Lock check - now you can view locked pages
            // Check if page is unlocked ONLY if we don't allow navigation to locked pages
            if (!allowNavigationToLockedPages && !IsPageUnlocked(pageIndex))
            {
                Debug.Log($"Page {pageIndex} is locked!");
                return;
            }

            // Hide current page
            if (currentPageIndex >= 0 && currentPageIndex < pages.Length)
            {
                pages[currentPageIndex].gameObject.SetActive(false);
            }

            // Show new page
            currentPageIndex = pageIndex;
            pages[currentPageIndex].gameObject.SetActive(true);

            // Update navigation bar
            navigationBar.SetCurrentPage(currentPageIndex);
        }

        public bool IsPageUnlocked(int pageIndex)
        {
            if (pageIndex == 0) return true;

            // Check if all levels in previous page are completed
            int previousPageStartLevel = (pageIndex - 1) * levelsPerPage;
            int previousPageEndLevel = Mathf.Min(previousPageStartLevel + levelsPerPage, levelDatabase.Levels.Length);

            for (int i = previousPageStartLevel; i < previousPageEndLevel; i++)
            {
                if (!LevelController.IsLevelCompleted(i))
                {
                    return false;
                }
            }

            return true;
        }

        public void LoadSelectedLevel(int levelIndex)
        {
            Debug.Log($"Loading level {levelIndex + 1}");

            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            levelSave.selectedLevelIndex = levelIndex;
            levelSave.isPlayingFromLevelSelection = true;
            levelSave.ReplayingLevelAgain = false;

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            // Stop animations before loading
            if (scooterAnimationController != null)
            {
                scooterAnimationController.StopAnimations();
            }

            // Just load the scene
            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("Game");
        }

        public void BackToMainMenu()
        {
            // Stop animations before loading
            if (scooterAnimationController != null)
            {
                scooterAnimationController.StopAnimations();
            }
            
            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("menu");
        }

        public int GetCurrentPageIndex()
        {
            return currentPageIndex;
        }
    }
}