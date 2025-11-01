// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;

// namespace Watermelon.BusStop
// {
//     public class LevelSelectionManager : MonoBehaviour
//     {
//         public static LevelSelectionManager Instance { get; private set; }

//         [SerializeField] LevelDatabase levelDatabase;
//         [SerializeField] LevelButton levelButtonPrefab;
//         [SerializeField] Transform buttonsContainer;
//         [SerializeField] ScrollRect scrollRect;

//         private LevelButton[] levelButtons;

//         private void Awake()
//         {
//             Instance = this;
//         }

//         private void Start()
//         {
//             GenerateLevelButtons();
//         }

//         private void GenerateLevelButtons()
//         {
//             int totalLevels = levelDatabase.Levels.Length;
//             levelButtons = new LevelButton[totalLevels];

//             for (int i = 0; i < totalLevels; i++)
//             {
//                 LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

//                 bool isUnlocked = IsLevelUnlocked(i);
//                 bool isCompleted = IsLevelCompleted(i);
//                 int starsEarned = GetLevelStars(i);

//                 button.Initialize(i, isUnlocked, isCompleted, starsEarned);

//                 levelButtons[i] = button;
//             }
//         }

//         public void LoadLevel(int levelIndex)
//         {
//             // Save the selected level using PlayerPrefs
//             PlayerPrefs.SetInt("selected_level", levelIndex);
//             PlayerPrefs.Save();

//             // Load game scene
//             SceneManager.LoadScene("GameScene");
//         }

//         private bool IsLevelUnlocked(int levelIndex)
//         {
//             if (levelIndex == 0) return true;
//             return IsLevelCompleted(levelIndex - 1);
//         }

//         private bool IsLevelCompleted(int levelIndex)
//         {
//             return PlayerPrefs.GetInt($"level_{levelIndex}_completed", 0) == 1;
//         }

//         private int GetLevelStars(int levelIndex)
//         {
//             return PlayerPrefs.GetInt($"level_{levelIndex}_stars", 0);
//         }

//         public static void MarkLevelCompleted(int levelIndex, int stars)
//         {
//             PlayerPrefs.SetInt($"level_{levelIndex}_completed", 1);
//             PlayerPrefs.SetInt($"level_{levelIndex}_stars", stars);
//             PlayerPrefs.Save();
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
        [SerializeField] LevelButton levelButtonPrefab;
        [SerializeField] Transform buttonsContainer;

        private LevelSave levelSave;

        private void Awake()
        {
            Instance = this;
            
            SaveController.Initialise(useAutoSave: false);
            levelSave = SaveController.GetSaveObject<LevelSave>("level");
        }

        private void Start()
        {
            CreateLevelButtons();
        }

        private void CreateLevelButtons()
        {
            int totalLevels = levelDatabase.Levels.Length;

            for (int i = 0; i < totalLevels; i++)
            {
                LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

                bool isUnlocked = LevelController.IsLevelUnlocked(i);
                bool isCompleted = LevelController.IsLevelCompleted(i);
                int starsEarned = LevelController.GetLevelStars(i);

                button.Setup(i, isUnlocked, isCompleted, starsEarned);
            }
        }

        // public void LoadSelectedLevel(int levelIndex)
        // {
        //     // Save which level was selected
        //     levelSave.selectedLevelIndex = levelIndex;
        //     levelSave.isPlayingFromLevelSelection = true;
        //     levelSave.ReplayingLevelAgain = false;
            
        //     SaveController.MarkAsSaveIsRequired();
        //     SaveController.Save(true);

        //     // Load game scene (replace "Game" with your actual game scene name)
        //     SceneManager.LoadScene("Game");
        // }

        public void LoadSelectedLevel(int levelIndex)
        {
            Debug.Log($"[LevelSelection] Loading level {levelIndex + 1}");
            
            // Get the save object
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            
            // Set the selected level
            levelSave.selectedLevelIndex = levelIndex;
            levelSave.isPlayingFromLevelSelection = true;
            levelSave.ReplayingLevelAgain = false;
            
            // Save before loading
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);
            
            // Load the game scene
            SceneManager.LoadScene("Game"); // Replace "Game" with your actual game scene name
        }

        public void BackToMainMenu()
        {
            // Replace with your menu scene name
            SceneManager.LoadScene("Menu");
        }
    }
}