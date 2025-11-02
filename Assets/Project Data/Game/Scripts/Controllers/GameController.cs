using UnityEngine;
using Watermelon.BusStop;
using Watermelon.SkinStore;

namespace Watermelon
{
    public class GameController : MonoBehaviour
    {
        private static GameController gameController;

        [DrawReference]
        [SerializeField] GameData data;

        [SerializeField] UIController uiController;

        private ParticlesController particlesController;
        private CurrenciesController currenciesController;
        private LevelController levelController;
        private TutorialController tutorialController;
        private PUController powerUpsController;

        private static bool isGameActive;
        public static bool IsGameActive => isGameActive;

        public static event SimpleCallback OnLevelChangedEvent;
        private static LevelSave levelSave;

        public static GameData Data => gameController.data;

        private void Awake()
        {
            gameController = this;

            SaveController.Initialise(useAutoSave: false);
            levelSave = SaveController.GetSaveObject<LevelSave>("level");

            // Cache components
            CacheComponent(out particlesController);
            CacheComponent(out currenciesController);
            CacheComponent(out levelController);
            CacheComponent(out tutorialController);
            CacheComponent(out powerUpsController);
            
        }

        private void Start()
        {
            InitialiseGame();
        }

        // public void InitialiseGame()
        // {
        //     uiController.Initialise();

        //     particlesController.Initialise();
        //     currenciesController.Initialise();
        //     tutorialController.Initialise();
        //     powerUpsController.Initialise();

        //     uiController.InitialisePages();

        //     // Add raycast controller component
        //     RaycastController raycastController = gameObject.AddComponent<RaycastController>();
        //     raycastController.Initialise();

        //     SkinStoreController.Init();

        //     levelController.Initialise();

        //     if(LevelController.RealLevelNumber != 0)
        //     {
        //         UIController.ShowPage<UIMainMenu>();
        //     }
        //     else
        //     {
        //         UIController.ShowPage<UIGame>();
        //     }

        //     LoadLevel(() =>
        //     {
        //         GameLoading.MarkAsReadyToHide();

        //         if (LevelController.RealLevelNumber == 0)
        //         {
        //             StartGame();
        //         }
        //     });
        // }

        // public void InitialiseGame()
        // {
        //     uiController.Initialise();
        //     particlesController.Initialise();
        //     currenciesController.Initialise();
        //     tutorialController.Initialise();
        //     powerUpsController.Initialise();
        //     uiController.InitialisePages();

        //     RaycastController raycastController = gameObject.AddComponent<RaycastController>();
        //     raycastController.Initialise();

        //     SkinStoreController.Init();
        //     levelController.Initialise();

        //     UIController.ShowPage<UIMainMenu>();

        //     // NEW: Check if playing from level selection
        //     if (levelSave.isPlayingFromLevelSelection)
        //     {
        //         // Load the selected level directly
        //         UIController.ShowPage<UIGame>();
        //         LoadLevel(() =>
        //         {
        //             GameLoading.MarkAsReadyToHide();
        //             StartGame();
        //         });
        //     }
        //     else
        //     {
        //         // Normal game flow
        //         // if (LevelController.RealLevelNumber != 0)
        //         // {
        //         //     UIController.ShowPage<UIMainMenu>();
        //         // }
        //         // else
        //         // {
        //         //     UIController.ShowPage<UIGame>();
        //         // }

        //         LoadLevel(() =>
        //         {
        //             GameLoading.MarkAsReadyToHide();
        //             if (LevelController.RealLevelNumber == 0)
        //             {
        //                 StartGame();
        //             }
        //         });
        //     }
        // }
        public void InitialiseGame()
        {
            uiController.Initialise();
            particlesController.Initialise();
            currenciesController.Initialise();
            tutorialController.Initialise();
            powerUpsController.Initialise();
            uiController.InitialisePages();

            RaycastController raycastController = gameObject.AddComponent<RaycastController>();
            raycastController.Initialise();

            SkinStoreController.Init();
            levelController.Initialise();

            // IMPORTANT: Re-get the save object after all initializations
            levelSave = SaveController.GetSaveObject<LevelSave>("level");

            Debug.Log($"[GameController] InitialiseGame - isPlayingFromLevelSelection: {levelSave.isPlayingFromLevelSelection}, selectedLevel: {levelSave.selectedLevelIndex}");

            // Check if playing from level selection
            if (levelSave.isPlayingFromLevelSelection)
            {
                Debug.Log($"[GameController] Loading from level selection - Level {levelSave.selectedLevelIndex + 1}");

                // Show game UI directly
                UIController.ShowPage<UIGame>();

                LoadLevel(() =>
                {
                    GameLoading.MarkAsReadyToHide();
                    StartGame();
                });
            }
            else
            {
                // Normal game flow
                UIController.ShowPage<UIMainMenu>();

                LoadLevel(() =>
                {
                    GameLoading.MarkAsReadyToHide();
                    if (LevelController.RealLevelNumber == 0)
                    {
                        StartGame();
                    }
                });
            }
        }

        // private static void LoadLevel(System.Action OnComplete = null)
        // {
        //     gameController.levelController.LoadLevel(() =>
        //     {
        //         OnComplete?.Invoke();
        //     });

        //     OnLevelChangedEvent?.Invoke();
        // }

        // private static void LoadLevel(System.Action OnComplete = null)
        // {
        //     gameController.levelController.LoadLevel(() =>
        //     {
        //         // Initialize orders AFTER level is loaded
        //         if (LevelController.OrderTracker != null)
        //         {
        //             UIGame gameUI = UIController.GetPage<UIGame>();
        //             UIMainMenu mainMenu = UIController.GetPage<UIMainMenu>();
        //             if (mainMenu != null)
        //             {
        //                 LevelData levelData = LevelController.LoadedStageData;
        //                 mainMenu.InitializeOrders(levelData.BusSpawnQueue);
        //                 //gameUI.InitializeOrders(levelData.BusSpawnQueue);
        //             }
        //         }

        //         OnComplete?.Invoke();
        //     });

        //     OnLevelChangedEvent?.Invoke();
        // }

        // private static void LoadLevel(System.Action OnComplete = null)
        // {
        //     gameController.levelController.LoadLevel(() =>
        //     {
        //         // Initialize orders in UIMainMenu (preview)
        //         UIMainMenu mainMenu = UIController.GetPage<UIMainMenu>();
        //         if (mainMenu != null && LevelController.OrderTracker != null)
        //         {
        //             LevelData levelData = LevelController.LoadedStageData;
        //             mainMenu.InitializeOrders(levelData.BusSpawnQueue);
        //         }

        //         // Initialize orders in UIGame (live tracking)
        //         if (LevelController.OrderTracker != null)
        //         {
        //             UIGame gameUI = UIController.GetPage<UIGame>();
        //             if (gameUI != null)
        //             {
        //                 LevelData levelData = LevelController.LoadedStageData;
        //                 gameUI.InitializeOrders(levelData.BusSpawnQueue);
        //             }
        //         }

        //         OnComplete?.Invoke();
        //     });

        //     OnLevelChangedEvent?.Invoke();
        // }

        private static void LoadLevel(System.Action OnComplete = null)
        {
            gameController.levelController.LoadLevel(() =>
            {
                // Initialize orders in UIMainMenu (preview)
                UIMainMenu mainMenu = UIController.GetPage<UIMainMenu>();
                if (mainMenu != null && LevelController.OrderTracker != null)
                {
                    LevelData levelData = LevelController.LoadedStageData;
                    mainMenu.InitializeOrders(levelData.BusSpawnQueue);
                }
        
                // Initialize orders in UIGame (live tracking)
                if (LevelController.OrderTracker != null)
                {
                    UIGame gameUI = UIController.GetPage<UIGame>();
                    if (gameUI != null)
                    {
                        LevelData levelData = LevelController.LoadedStageData;
                        gameUI.InitializeOrders(levelData.BusSpawnQueue);
                    }
                }
        
                OnComplete?.Invoke();
            });
        
            OnLevelChangedEvent?.Invoke();
        }

        public static void ReturnToLevelSelection()
        {
            // Reset game state
            isGameActive = false;

            // Reset the level selection flag
            levelSave.isPlayingFromLevelSelection = false;

            // Save the state
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            // Disable raycast controller if active
            RaycastController.Disable();

            Debug.Log("[GameController] Returning to level selection");

            // Load the level selection scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelection");
        }


        // public static void StartGame()
        // {
        //     // On Level is loaded
        //     isGameActive = true;

        //     //UIController.HidePage<UIMainMenu>();
        //     UIController.ShowPage<UIGame>();

        //     //Tween.DelayedCall(2f, LivesManager.RemoveLife);
        // }

        public static void StartGame()
        {
            // Check if player has lives before starting
            if (LivesManager.Lives <= 0)
            {
                Debug.Log("[GameController] Cannot start game - No lives remaining");

                // Show the add lives panel
                UIMainMenu mainMenu = UIController.GetPage<UIMainMenu>();
                if (mainMenu != null)
                {
                    mainMenu.ShowAddLivesPanel();
                }

                return; // Don't start the game
            }

            // On Level is loaded
            isGameActive = true;

            UIController.ShowPage<UIGame>();

            // Remove a life when starting the game
            //Tween.DelayedCall(2f, LivesManager.RemoveLife);

            Debug.Log($"[GameController] Game started - Lives remaining: {LivesManager.Lives}");
        }


        public static void LoseGame()
        {
            if (!isGameActive)
                return;

            isGameActive = false;

            RaycastController.Disable();
            LivesManager.RemoveLife();

            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIGameOver>();

            AudioController.PlaySound(AudioController.Sounds.failSound);

            levelSave.ReplayingLevelAgain = true;
        }

        // public static void WinGame()
        // {
        //     if (!isGameActive)
        //         return;

        //     isGameActive = false;

        //     RaycastController.Disable();

        //     levelSave.ReplayingLevelAgain = false;

        //     LevelData completedLevel = LevelController.LoadedStageData;

        //     UIController.HidePage<UIGame>();
        //     UIController.ShowPage<UIComplete>();

        //     AudioController.PlaySound(AudioController.Sounds.completeSound);
        // }

        public static void WinGame()
        {
            if (!isGameActive)
                return;

            isGameActive = false;

            RaycastController.Disable();

            levelSave.ReplayingLevelAgain = false;

            LevelData completedLevel = LevelController.LoadedStageData;

            // Mark the completed level with correct index
            int completedLevelIndex;
            if (levelSave.isPlayingFromLevelSelection)
            {
                completedLevelIndex = levelSave.selectedLevelIndex;
            }
            else
            {
                completedLevelIndex = levelSave.RealLevelNumber;
            }

            Debug.Log($"Level {completedLevelIndex + 1} completed!");

            LevelController.MarkLevelCompleted(completedLevelIndex, 3);
            SaveController.Save(true);

            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIComplete>();

            AudioController.PlaySound(AudioController.Sounds.completeSound);
        }

        public static void ReturnToMainMenu()
        {
            levelSave.isPlayingFromLevelSelection = false;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            UIController.ShowPage<UIMainMenu>();
        }




        // NEW: Add this method
        private static void MarkLevelCompleted(int levelIndex, int stars)
        {
            // if (!levelSave.levelProgress.ContainsKey(levelIndex))
            // {
            //     levelSave.levelProgress[levelIndex] = new LevelProgressData();
            // }
            // Just call the static method from LevelController
            LevelController.MarkLevelCompleted(levelIndex, 3);

            levelSave.levelProgress[levelIndex].isCompleted = true;

            // Save best star rating
            int currentStars = levelSave.levelProgress[levelIndex].starsEarned;
            levelSave.levelProgress[levelIndex].starsEarned = Mathf.Max(currentStars, stars);

            SaveController.MarkAsSaveIsRequired();
        }

        // public static void LoadNextLevel()
        // {
        //     if (isGameActive)
        //         return;

        //     // NEW: Check if playing from level selection
        //     if (levelSave.isPlayingFromLevelSelection)
        //     {
        //         // Load next level in sequence
        //         int nextLevelIndex = levelSave.selectedLevelIndex + 1;

        //         // Check if next level exists and is unlocked
        //         if (nextLevelIndex < gameController.levelController.GetTotalLevels())
        //         {
        //             // Set the next level as selected
        //             levelSave.selectedLevelIndex = nextLevelIndex;
        //             levelSave.isPlayingFromLevelSelection = true;
        //             levelSave.ReplayingLevelAgain = false;
        //         }
        //         else
        //         {
        //             // No more levels, return to level selection
        //             levelSave.isPlayingFromLevelSelection = false;
        //             UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelection");
        //             return;
        //         }
        //     }
        //     else
        //     {
        //         // Normal progression mode
        //         gameController.levelController.AdjustLevelNumber();
        //         levelSave.ReplayingLevelAgain = false;
        //     }

        //     UIController.ShowPage<UIMainMenu>();

        //     AdsManager.ShowInterstitial(null);

        //     LoadLevel();
        // }

        public static void LoadNextLevel()
        {
            if (isGameActive)
                return;

            if (levelSave.isPlayingFromLevelSelection)
            {
                // DON'T increment here - already done in UIComplete
                levelSave.ReplayingLevelAgain = false;

                Debug.Log($"Loading next level in sequence: Level {levelSave.selectedLevelIndex + 1}");
            }
            else
            {
                // Normal progression mode
                gameController.levelController.AdjustLevelNumber();
                levelSave.ReplayingLevelAgain = false;
            }

            UIController.ShowPage<UIMainMenu>();

            AdsManager.ShowInterstitial(null);

            //LoadLevel();
            LoadLevel(() =>
    {
        // Refresh level number AFTER level loads
        UIMainMenu mainMenu = UIController.GetPage<UIMainMenu>();
        if (mainMenu != null && mainMenu.IsPageDisplayed)
        {
            mainMenu.RefreshLevelNumber();

            // Also refresh orders for new level
            if (LevelController.OrderTracker != null)
            {
                LevelData levelData = LevelController.LoadedStageData;
                mainMenu.InitializeOrders(levelData.BusSpawnQueue);
            }
        }
    });
        }



        /// <summary>
        /// Get total number of levels in the database
        /// </summary>




        // public static void LoadNextLevel()
        // {
        //     if (isGameActive)
        //         return;

        //     gameController.levelController.AdjustLevelNumber();

        //     UIController.ShowPage<UIMainMenu>();

        //     levelSave.ReplayingLevelAgain = false;

        //     AdsManager.ShowInterstitial(null);

        //     LoadLevel();
        // }

        // public static void ReplayLevel()
        // {
        //     isGameActive = false;

        //     UIController.ShowPage<UIMainMenu>();

        //     levelSave.ReplayingLevelAgain = true;

        //     AdsManager.ShowInterstitial(null);

        //     LoadLevel();
        // }

        // public static void ReplayLevel()
        // {
        //     isGameActive = false;

        //     UIController.ShowPage<UIMainMenu>();

        //     levelSave.ReplayingLevelAgain = true;

        //     // Don't change selectedLevelIndex - replay same level

        //     AdsManager.ShowInterstitial(null);

        //     LoadLevel();
        // }

        public static void ReplayLevel()
        {
            isGameActive = false;

            // Check lives before replaying
            if (LivesManager.Lives <= 0)
            {
                Debug.Log("[GameController] Cannot replay level - No lives remaining");
                UIController.ShowPage<UIMainMenu>();

                UIMainMenu mainMenu = UIController.GetPage<UIMainMenu>();
                if (mainMenu != null)
                {
                    mainMenu.ShowAddLivesPanel();
                }
                return;
            }

            UIController.ShowPage<UIMainMenu>();
            levelSave.ReplayingLevelAgain = true;

            AdsManager.ShowInterstitial(null);
            LoadLevel();
        }



        public static void RefreshLevelDev()
        {
            UIController.ShowPage<UIGame>();
            levelSave.ReplayingLevelAgain = true;

            LoadLevel();
        }

        private void OnApplicationQuit()
        {
            // to make sure we will load similar level next time game launched (in case we outside level bounds)
            levelSave.ReplayingLevelAgain = true;
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;

                return true;
            }

            Debug.LogError(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;

            return false;
        }
        #endregion
    }
}