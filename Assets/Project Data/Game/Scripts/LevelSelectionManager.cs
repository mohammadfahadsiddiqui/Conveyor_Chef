using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Drives the existing LevelSelection scene.
    /// When opened from CountryMap it focuses the page containing the selected
    /// country's first level and Back returns to CountryMap.
    /// </summary>
    public class LevelSelectionController : MonoBehaviour
    {
        public static LevelSelectionController Instance { get; private set; }

        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const string FromCountryMapKey = "CC_LevelSelection_FromCountryMap";

        private const int LevelsPerContinent = 15;
        private const int LevelsPerCountry = 3;
        private const int CountriesPerContinent = 5;

        [Header("References")]
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private LevelPageController pageControllerPrefab;
        [SerializeField] private Transform pagesContainer;
        [SerializeField] private PageNavigationBar navigationBar;

        [Header("Animation")]
        [SerializeField] private ScooterAnimationController scooterAnimationController;

        [Header("Settings")]
        [SerializeField, Min(1)] private int levelsPerPage = 5;
        [SerializeField] private bool allowNavigationToLockedPages = true;

        private LevelSave levelSave;
        private LevelPageController[] pages;
        private int currentPageIndex;

        private void Awake()
        {
            Instance = this;

            if (!SaveController.IsSaveLoaded)
                SaveController.Initialise(useAutoSave: false);

            levelSave = SaveController.GetSaveObject<LevelSave>("level");
        }

        private void Start()
        {
            CreatePages();
            InitializeNavigationBar();

            int initialPage = ResolveInitialPage();
            ShowPage(initialPage);

            if (scooterAnimationController != null)
                scooterAnimationController.StartAnimations();
        }

        private void OnDestroy()
        {
            if (scooterAnimationController != null)
                scooterAnimationController.StopAnimations();

            if (Instance == this)
                Instance = null;
        }

        private int ResolveInitialPage()
        {
            if (pages == null || pages.Length == 0)
                return 0;

            if (PlayerPrefs.GetInt(FromCountryMapKey, 0) != 1)
                return 0;

            int continent = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedContinentKey, 0),
                0,
                5);

            int country = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedCountryKey, 0),
                0,
                CountriesPerContinent - 1);

            int firstCountryLevel =
                continent * LevelsPerContinent +
                country * LevelsPerCountry;

            int safeLevelsPerPage = Mathf.Max(1, levelsPerPage);
            return Mathf.Clamp(
                firstCountryLevel / safeLevelsPerPage,
                0,
                pages.Length - 1);
        }

        private void CreatePages()
        {
            if (levelDatabase == null)
            {
                Debug.LogError("[LevelSelection] LevelDatabase reference is missing.");
                pages = new LevelPageController[0];
                return;
            }

            if (pageControllerPrefab == null || pagesContainer == null)
            {
                Debug.LogError("[LevelSelection] Page prefab or PagesContainer reference is missing.");
                pages = new LevelPageController[0];
                return;
            }

            int totalLevels = levelDatabase.Levels.Length;
            int safeLevelsPerPage = Mathf.Max(1, levelsPerPage);
            int totalPages = Mathf.CeilToInt((float)totalLevels / safeLevelsPerPage);

            pages = new LevelPageController[totalPages];

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                LevelPageController page = Instantiate(pageControllerPrefab, pagesContainer);
                page.gameObject.SetActive(false);

                int startLevelIndex = pageIndex * safeLevelsPerPage;
                int endLevelIndex = Mathf.Min(startLevelIndex + safeLevelsPerPage, totalLevels);
                int levelsInPage = endLevelIndex - startLevelIndex;

                page.Setup(pageIndex, startLevelIndex, levelsInPage);
                pages[pageIndex] = page;
            }
        }

        private void InitializeNavigationBar()
        {
            if (navigationBar != null)
                navigationBar.Setup(pages != null ? pages.Length : 0, this);
        }

        public void ShowPage(int pageIndex)
        {
            if (pages == null || pages.Length == 0)
                return;

            if (pageIndex < 0 || pageIndex >= pages.Length)
                return;

            if (!allowNavigationToLockedPages && !IsPageUnlocked(pageIndex))
            {
                Debug.Log("[LevelSelection] Page " + pageIndex + " is locked.");
                return;
            }

            if (currentPageIndex >= 0 &&
                currentPageIndex < pages.Length &&
                pages[currentPageIndex] != null)
            {
                pages[currentPageIndex].gameObject.SetActive(false);
            }

            currentPageIndex = pageIndex;

            if (pages[currentPageIndex] != null)
                pages[currentPageIndex].gameObject.SetActive(true);

            if (navigationBar != null)
                navigationBar.SetCurrentPage(currentPageIndex);
        }

        public bool IsPageUnlocked(int pageIndex)
        {
            if (pageIndex <= 0)
                return true;

            if (levelDatabase == null)
                return false;

            int safeLevelsPerPage = Mathf.Max(1, levelsPerPage);
            int previousPageStartLevel = (pageIndex - 1) * safeLevelsPerPage;
            int previousPageEndLevel = Mathf.Min(
                previousPageStartLevel + safeLevelsPerPage,
                levelDatabase.Levels.Length);

            for (int i = previousPageStartLevel; i < previousPageEndLevel; i++)
            {
                if (!LevelController.IsLevelCompleted(i))
                    return false;
            }

            return true;
        }

        public void LoadSelectedLevel(int levelIndex)
        {
            if (levelDatabase == null ||
                levelIndex < 0 ||
                levelIndex >= levelDatabase.Levels.Length)
            {
                Debug.LogError("[LevelSelection] Invalid level index: " + levelIndex);
                return;
            }

            Debug.Log("[LevelSelection] Loading level " + (levelIndex + 1));

            LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
            save.selectedLevelIndex = levelIndex;
            save.isPlayingFromLevelSelection = true;
            save.ReplayingLevelAgain = LevelController.IsLevelCompleted(levelIndex);

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            if (scooterAnimationController != null)
                scooterAnimationController.StopAnimations();

            // The country-map navigation context has served its purpose once
            // gameplay starts. Clear it so later generic level-selection visits
            // retain their original Main Menu back behavior.
            PlayerPrefs.DeleteKey(FromCountryMapKey);
            PlayerPrefs.Save();

            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("Game");
        }

        public void BackToMainMenu()
        {
            if (scooterAnimationController != null)
                scooterAnimationController.StopAnimations();

            if (PlayerPrefs.GetInt(FromCountryMapKey, 0) == 1)
            {
                PlayerPrefs.DeleteKey(FromCountryMapKey);
                PlayerPrefs.Save();
                Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("CountryMap");
                return;
            }

            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("menu");
        }

        public int GetCurrentPageIndex()
        {
            return currentPageIndex;
        }
    }
}
