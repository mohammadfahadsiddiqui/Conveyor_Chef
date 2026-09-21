using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// One reusable sequential continent progression map.
    /// Countries are visual/story zones only: players never choose a country.
    /// Progress is strictly Level N -> N+1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContinentMapSceneController : MonoBehaviour
    {
        public const int LevelsPerContinent = 15;
        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";

        private static readonly string[] ContinentNames =
        {
            "Asia",
            "North America",
            "South America",
            "Europe",
            "Africa",
            "Australia / Oceania"
        };

        private static readonly string[,] ZoneNames =
        {
            { "India", "Japan", "China" },
            { "USA", "Mexico", "Canada" },
            { "Brazil", "Argentina", "Peru" },
            { "Italy", "France", "United Kingdom" },
            { "Egypt", "Morocco", "South Africa" },
            { "Australia", "New Zealand", "Pacific Islands" }
        };

        private static readonly string[] AsiaFoods =
        {
            "Butter Chicken", "Samosa", "Biryani", "Dosa", "India Finale",
            "Sushi", "Ramen", "Tempura", "Onigiri", "Japan Finale",
            "Dumplings", "Fried Rice", "Noodles", "Bao", "China Finale"
        };

        [Header("Map")]
        [SerializeField] private ScrollRect mapScrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private ContinentMapLevelNode[] levelNodes;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;

        [Header("Level Popup")]
        [SerializeField] private GameObject levelPopup;
        [SerializeField] private TextMeshProUGUI popupTitle;
        [SerializeField] private TextMeshProUGUI popupBody;
        [SerializeField] private TextMeshProUGUI popupStars;
        [SerializeField] private Button popupStartButton;
        [SerializeField] private Button popupCloseButton;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TextMeshProUGUI soundText;
        [SerializeField] private TextMeshProUGUI vibrationText;

        [Header("Focus")]
        [SerializeField] private bool autoFocusCurrentLevel = true;

        private int selectedContinent;
        private int selectedGlobalLevel = -1;
        private int selectedLocalLevel = -1;

        private void Awake()
        {
            UIEventSystemRuntime.UseCurrentSceneEventSystem();
            EnsureSaveControllerReady();

            Wire(backButton, BackToWorldMap);
            Wire(settingsButton, OpenSettings);
            Wire(closeSettingsButton, CloseSettings);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);
            Wire(popupStartButton, StartSelectedLevel);
            Wire(popupCloseButton, CloseLevelPopup);

            if (levelPopup != null)
                levelPopup.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            EnsureInteractionReady();
        }

        private void Start()
        {
            selectedContinent = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedContinentKey, 0),
                0,
                ContinentNames.Length - 1);

            // Safety: never allow jumping into a continent that is still locked.
            if (!IsContinentUnlocked(selectedContinent))
                selectedContinent = HighestUnlockedContinent();

            RefreshMap();

            if (autoFocusCurrentLevel)
                FocusCurrentLevel();
        }

        private static void EnsureSaveControllerReady()
        {
            if (SaveController.IsSaveLoaded)
                return;

            try
            {
                SaveController.Initialise(useAutoSave: false);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ContinentMap] SaveController initialisation failed: " + ex.Message);
            }
        }

        private void EnsureInteractionReady()
        {
            if (mapScrollRect == null)
                return;

            mapScrollRect.enabled = true;
            mapScrollRect.horizontal = false;
            mapScrollRect.vertical = true;

            if (mapContent != null)
                mapScrollRect.content = mapContent;

            Canvas canvas = mapScrollRect.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();

                raycaster.enabled = true;
            }

            Graphic viewportGraphic = mapScrollRect.viewport != null
                ? mapScrollRect.viewport.GetComponent<Graphic>()
                : null;

            if (viewportGraphic != null)
                viewportGraphic.raycastTarget = true;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void RefreshMap()
        {
            if (titleText != null)
                titleText.text = "CHAPTER " + (selectedContinent + 1) + " • " + ContinentNames[selectedContinent].ToUpperInvariant();

            int completedCount = 0;
            int currentLocal = FindCurrentLocalLevel();

            for (int local = 0; local < LevelsPerContinent; local++)
            {
                int global = ToGlobalLevel(selectedContinent, local);
                bool completed = LevelController.IsLevelCompleted(global);
                bool unlocked = LevelController.IsLevelUnlocked(global);
                int stars = LevelController.GetLevelStars(global);

                if (completed)
                    completedCount++;

                if (levelNodes != null && local < levelNodes.Length && levelNodes[local] != null)
                {
                    string number = (selectedContinent + 1) + "-" + (local + 1);
                    levelNodes[local].Bind(
                        this,
                        global,
                        local,
                        number,
                        unlocked,
                        completed,
                        stars,
                        local == currentLocal);
                }
            }

            if (progressText != null)
            {
                progressText.text = completedCount >= LevelsPerContinent
                    ? "CONTINENT COMPLETE • 15/15"
                    : "PROGRESS " + completedCount + "/15 • NEXT " +
                      (selectedContinent + 1) + "-" + (currentLocal + 1);
            }
        }

        private int FindCurrentLocalLevel()
        {
            for (int local = 0; local < LevelsPerContinent; local++)
            {
                int global = ToGlobalLevel(selectedContinent, local);

                if (LevelController.IsLevelUnlocked(global) &&
                    !LevelController.IsLevelCompleted(global))
                {
                    return local;
                }
            }

            return LevelsPerContinent - 1;
        }

        private void FocusCurrentLevel()
        {
            if (mapScrollRect == null || levelNodes == null || levelNodes.Length == 0)
                return;

            int current = Mathf.Clamp(FindCurrentLocalLevel(), 0, levelNodes.Length - 1);

            // Nodes are authored bottom-to-top. ScrollRect normalized value 0 is bottom,
            // 1 is top, so level 1 starts at the bottom and later progress moves upward.
            float normalized = LevelsPerContinent <= 1
                ? 0f
                : (float)current / (LevelsPerContinent - 1);

            Canvas.ForceUpdateCanvases();
            mapScrollRect.StopMovement();
            mapScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized);
        }

        public void OpenLevel(int globalLevelIndex, int localLevelIndex)
        {
            if (!LevelController.IsLevelUnlocked(globalLevelIndex))
            {
                ShowLockedMessage(localLevelIndex);
                return;
            }

            selectedGlobalLevel = globalLevelIndex;
            selectedLocalLevel = localLevelIndex;

            string zone = ZoneNames[selectedContinent, Mathf.Clamp(localLevelIndex / 5, 0, 2)];
            bool completed = LevelController.IsLevelCompleted(globalLevelIndex);
            int stars = LevelController.GetLevelStars(globalLevelIndex);

            if (popupTitle != null)
                popupTitle.text = "LEVEL " + (selectedContinent + 1) + "-" + (localLevelIndex + 1);

            if (popupBody != null)
            {
                string food = selectedContinent == 0 && localLevelIndex < AsiaFoods.Length
                    ? AsiaFoods[localLevelIndex]
                    : "Chef Challenge";

                popupBody.text =
                    zone.ToUpperInvariant() + "\n" +
                    food + "\n\n" +
                    (completed ? "Completed • Replay available" : "Complete this level to unlock the next stop.");
            }

            if (popupStars != null)
                popupStars.text = completed
                    ? "BEST  " + new string('★', Mathf.Clamp(stars, 0, 3)) +
                      new string('☆', 3 - Mathf.Clamp(stars, 0, 3))
                    : "NOT COMPLETED";

            if (popupStartButton != null)
                popupStartButton.interactable = true;

            if (levelPopup != null)
                levelPopup.SetActive(true);

            PlayClick();
        }

        public void ShowLockedMessage(int localLevelIndex)
        {
            if (progressText != null)
            {
                progressText.text = localLevelIndex <= 0
                    ? "LEVEL LOCKED"
                    : "COMPLETE " + (selectedContinent + 1) + "-" + localLevelIndex +
                      " TO UNLOCK " + (selectedContinent + 1) + "-" + (localLevelIndex + 1);
            }

            PlayClick();
        }

        private void StartSelectedLevel()
        {
            if (selectedGlobalLevel < 0 ||
                !LevelController.IsLevelUnlocked(selectedGlobalLevel))
            {
                return;
            }

            LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
            bool replaying = LevelController.IsLevelCompleted(selectedGlobalLevel);

            save.selectedLevelIndex = selectedGlobalLevel;
            save.isPlayingFromLevelSelection = true;
            save.ReplayingLevelAgain = replaying;

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            CloseLevelPopup();
            PlayClick();

            EnhancedLoadingScreen.LoadViaLoadingScreen("Game");
        }

        private void CloseLevelPopup()
        {
            if (levelPopup != null)
                levelPopup.SetActive(false);
        }

        private void BackToWorldMap()
        {
            PlayClick();
            EnhancedLoadingScreen.LoadViaLoadingScreen("WorldMap");
        }

        private void OpenSettings()
        {
            PlayClick();

            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            RefreshSettingsLabels();
        }

        private void CloseSettings()
        {
            PlayClick();

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void ToggleSound()
        {
            bool enabled = AudioController.GetVolume() > 0.001f;
            AudioController.SetVolume(enabled ? 0f : 1f);
            RefreshSettingsLabels();
            PlayClick();
        }

        private void ToggleVibration()
        {
            AudioController.SetVibrationState(!AudioController.IsVibrationEnabled());
            RefreshSettingsLabels();
            PlayClick();
        }

        private void RefreshSettingsLabels()
        {
            if (soundText != null)
                soundText.text = "SOUND: " + (AudioController.GetVolume() > 0.001f ? "ON" : "OFF");

            if (vibrationText != null)
                vibrationText.text = "VIBRATION: " + (AudioController.IsVibrationEnabled() ? "ON" : "OFF");
        }

        private static int ToGlobalLevel(int continentIndex, int localLevelIndex)
        {
            return continentIndex * LevelsPerContinent + localLevelIndex;
        }

        private static bool IsContinentUnlocked(int continentIndex)
        {
            if (continentIndex <= 0)
                return true;

            int previousEndLevel = continentIndex * LevelsPerContinent - 1;
            return LevelController.IsLevelCompleted(previousEndLevel);
        }

        private static int HighestUnlockedContinent()
        {
            int highest = 0;

            for (int i = 1; i < ContinentNames.Length; i++)
            {
                if (!IsContinentUnlocked(i))
                    break;

                highest = i;
            }

            return highest;
        }

        private static void PlayClick()
        {
            try
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
            }
            catch
            {
                // Keep map navigation functional if audio is not ready.
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            ScrollRect scroll,
            RectTransform content,
            ContinentMapLevelNode[] nodes,
            TextMeshProUGUI title,
            TextMeshProUGUI progress,
            Button back,
            Button settings,
            GameObject popup,
            TextMeshProUGUI popupTitleText,
            TextMeshProUGUI popupBodyText,
            TextMeshProUGUI popupStarsText,
            Button startButton,
            Button closePopupButton,
            GameObject settingsRoot,
            Button closeSettings,
            Button sound,
            Button vibration,
            TextMeshProUGUI soundLabel,
            TextMeshProUGUI vibrationLabel)
        {
            mapScrollRect = scroll;
            mapContent = content;
            levelNodes = nodes;
            titleText = title;
            progressText = progress;
            backButton = back;
            settingsButton = settings;

            levelPopup = popup;
            popupTitle = popupTitleText;
            popupBody = popupBodyText;
            popupStars = popupStarsText;
            popupStartButton = startButton;
            popupCloseButton = closePopupButton;

            settingsPanel = settingsRoot;
            closeSettingsButton = closeSettings;
            soundButton = sound;
            vibrationButton = vibration;
            soundText = soundLabel;
            vibrationText = vibrationLabel;
        }
#endif
    }
}
