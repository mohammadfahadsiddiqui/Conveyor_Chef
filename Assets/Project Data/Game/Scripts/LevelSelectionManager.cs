// using UnityEngine;
// using UnityEngine.SceneManagement;

// namespace Watermelon.BusStop
// {
//     public class LevelSelectionController : MonoBehaviour
//     {
//         public static LevelSelectionController Instance { get; private set; }

//         [Header("References")]
//         [SerializeField] LevelDatabase levelDatabase;
//         [SerializeField] LevelButton levelButtonPrefab;
//         [SerializeField] Transform buttonsContainer;

//         private LevelSave levelSave;

//         private void Awake()
//         {
//             Instance = this;
            
//             SaveController.Initialise(useAutoSave: false);
//             levelSave = SaveController.GetSaveObject<LevelSave>("level");
//         }

//         private void Start()
//         {
//             CreateLevelButtons();
//         }

//         private void CreateLevelButtons()
//         {
//             int totalLevels = levelDatabase.Levels.Length;

//             for (int i = 0; i < totalLevels; i++)
//             {
//                 LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

//                 bool isUnlocked = LevelController.IsLevelUnlocked(i);
//                 bool isCompleted = LevelController.IsLevelCompleted(i);
//                 int starsEarned = LevelController.GetLevelStars(i);

//                 button.Setup(i, isUnlocked, isCompleted, starsEarned);
//             }
//         }

//         // public void LoadSelectedLevel(int levelIndex)
//         // {
//         //     // Save which level was selected
//         //     levelSave.selectedLevelIndex = levelIndex;
//         //     levelSave.isPlayingFromLevelSelection = true;
//         //     levelSave.ReplayingLevelAgain = false;
            
//         //     SaveController.MarkAsSaveIsRequired();
//         //     SaveController.Save(true);

//         //     // Load game scene (replace "Game" with your actual game scene name)
//         //     SceneManager.LoadScene("Game");
//         // }

//         public void LoadSelectedLevel(int levelIndex)
//         {
//             Debug.Log($"[LevelSelection] Loading level {levelIndex + 1}");
            
//             // Get the save object
//             LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            
//             // Set the selected level
//             levelSave.selectedLevelIndex = levelIndex;
//             levelSave.isPlayingFromLevelSelection = true;
//             levelSave.ReplayingLevelAgain = false;
            
//             // Save before loading
//             SaveController.MarkAsSaveIsRequired();
//             SaveController.Save(true);
            
//             // Load the game scene
//             SceneManager.LoadScene("Game"); // Replace "Game" with your actual game scene name
//         }

//         public void BackToMainMenu()
//         {
//             // Replace with your menu scene name
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

        [Header("Settings")]
        [SerializeField] int levelsPerPage = 5;

        private LevelSave levelSave;
        private LevelPageController[] pages;
        private int currentPageIndex = 0;

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
            ShowPage(0);
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

            // Check if page is unlocked
            if (!IsPageUnlocked(pageIndex))
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

        // public void LoadSelectedLevel(int levelIndex)
        // {
        //     Debug.Log($"[LevelSelection] Loading level {levelIndex + 1}");
            
        //     LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            
        //     levelSave.selectedLevelIndex = levelIndex;
        //     levelSave.isPlayingFromLevelSelection = true;
        //     levelSave.ReplayingLevelAgain = false;
            
        //     SaveController.MarkAsSaveIsRequired();
        //     SaveController.Save(true);
            
        //     SceneManager.LoadScene("Game");
        // }

        public void LoadSelectedLevel(int levelIndex)
        {
            Debug.Log($"[LevelSelection] Loading level {levelIndex + 1}");
            
            // IMPORTANT: Re-get the save object to ensure we're working with the latest
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            
            // Set the selected level
            levelSave.selectedLevelIndex = levelIndex;
            levelSave.isPlayingFromLevelSelection = true;
            levelSave.ReplayingLevelAgain = false;
            
            Debug.Log($"[LevelSelection] Set levelSave values - selectedIndex: {levelSave.selectedLevelIndex}, isPlayingFromLevelSelection: {levelSave.isPlayingFromLevelSelection}");
            
            // CRITICAL: Force save immediately and synchronously
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true); // true = force immediate save
            
            // Verify save was successful
            LevelSave verifyLoad = SaveController.GetSaveObject<LevelSave>("level");
            Debug.Log($"[LevelSelection] Verified save - isPlayingFromLevelSelection: {verifyLoad.isPlayingFromLevelSelection}, selectedIndex: {verifyLoad.selectedLevelIndex}");
            
            // Load the game scene directly (bypass loading screen since we're already in game)
            Debug.Log("[LevelSelection] Loading Game scene now...");
            SceneManager.LoadScene("Game");
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("Menu");
        }
    }
}