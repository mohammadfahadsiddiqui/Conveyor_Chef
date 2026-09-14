using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    public static class ConveyorChefUIHotfix
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GameObject runner = new GameObject("CC_UI_HotfixRunner");
            runner.AddComponent<HotfixRunner>().Run(scene.name);
        }

        private sealed class HotfixRunner : MonoBehaviour
        {
            private string sceneName;

            public void Run(string loadedSceneName)
            {
                sceneName = loadedSceneName;
                StartCoroutine(Apply());
            }

            private IEnumerator Apply()
            {
                yield return null;
                yield return null;

                if (sceneName.Equals("loading", System.StringComparison.OrdinalIgnoreCase))
                {
                    GameObject duplicate = GameObject.Find("CC_LoadingPolish");
                    if (duplicate != null)
                        Destroy(duplicate);
                }
                else if (sceneName.Equals("menu", System.StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < 30; i++)
                    {
                        GameObject play = GameObject.Find("PlayHitTarget");
                        GameObject settings = GameObject.Find("SettingsHitTarget");

                        if (play != null)
                            EnsureFeedback(play);
                        if (settings != null)
                            EnsureFeedback(settings);

                        if (play != null && settings != null)
                            break;

                        yield return null;
                    }
                }
                else if (sceneName.Equals("LevelSelection", System.StringComparison.OrdinalIgnoreCase))
                {
                    SuppressScooter();

                    foreach (LevelSelectionInteractionRepair repair in FindObjectsByType<LevelSelectionInteractionRepair>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (repair != null)
                            Destroy(repair.gameObject);
                    }

                    GameObject oldControls = GameObject.Find("CC_RealLevelControls");
                    if (oldControls != null)
                        Destroy(oldControls);

                    GameObject oldArtworkControls = GameObject.Find("CC_LevelSelectionArtworkInteractions");
                    if (oldArtworkControls != null)
                        Destroy(oldArtworkControls);

                    GameObject interactions = new GameObject("CC_LevelSelectionArtworkInteractions");
                    interactions.AddComponent<LevelArtworkInteractions>();
                }
                else if (sceneName.Equals("Game", System.StringComparison.OrdinalIgnoreCase))
                {
                    SuppressScooter();
                }

                Destroy(gameObject);
            }

            private static void SuppressScooter()
            {
                foreach (ScooterAnimationController scooter in FindObjectsByType<ScooterAnimationController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (scooter == null)
                        continue;

                    scooter.enabled = false;
                    scooter.gameObject.SetActive(false);
                }
            }

            private static void EnsureFeedback(GameObject target)
            {
                if (target.GetComponent<ArtworkButtonFeedback>() == null)
                    target.AddComponent<ArtworkButtonFeedback>();
            }
        }
    }

    public sealed class ArtworkButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Image image;
        private Button button;
        private Coroutine restoreRoutine;
        private Vector3 baseScale;
        private Color baseColor;
        private bool transparentTarget;

        private void Awake()
        {
            image = GetComponent<Image>();
            button = GetComponent<Button>();
            baseScale = transform.localScale;

            if (image != null)
            {
                baseColor = image.color;
                transparentTarget = image.color.a < 0.02f;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable)
                return;

            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            transform.localScale = baseScale * 0.92f;

            if (image != null)
            {
                if (transparentTarget)
                    image.color = new Color(1f, 1f, 1f, 0.20f);
                else
                    image.color = new Color(baseColor.r * 0.82f, baseColor.g * 0.82f, baseColor.b * 0.82f, baseColor.a);
            }
        }

        public void OnPointerUp(PointerEventData eventData) => RestoreAnimated();
        public void OnPointerExit(PointerEventData eventData) => RestoreAnimated();

        private void RestoreAnimated()
        {
            if (!isActiveAndEnabled)
                return;

            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            restoreRoutine = StartCoroutine(RestoreRoutine());
        }

        private IEnumerator RestoreRoutine()
        {
            Vector3 startScale = transform.localScale;
            Color startColor = image != null ? image.color : Color.white;
            float elapsed = 0f;
            const float duration = 0.13f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, baseScale, eased);

                if (image != null)
                    image.color = Color.Lerp(startColor, baseColor, eased);

                yield return null;
            }

            transform.localScale = baseScale;
            if (image != null)
                image.color = baseColor;
            restoreRoutine = null;
        }
    }

    public sealed class LevelArtworkInteractions : MonoBehaviour
    {
        private const int LevelsPerPage = 12;
        private const int TotalLevels = 50;
        private const string EditorResetKey = "CC_DYNAMIC_LEVEL_GRID_RESET_V2";

        private static readonly Color CompletedColor = new Color(1f, 0.62f, 0.05f, 1f);
        private static readonly Color CurrentColor = new Color(0.31f, 0.82f, 0.13f, 1f);
        private static readonly Color LockedColor = new Color(0.39f, 0.40f, 0.43f, 1f);
        private static readonly Color DarkText = new Color(0.25f, 0.12f, 0.05f, 1f);

        private LevelSelectionController controller;
        private TMP_FontAsset font;
        private Sprite levelButtonSprite;
        private Image.Type levelButtonImageType = Image.Type.Simple;
        private Sprite lockSprite;
        private Sprite starSprite;

        private Button[] levelButtons;
        private TMP_Text[] levelLabels;
        private Image[] levelImages;
        private Image[][] stars;
        private Image[] lockIcons;
        private Button backButton;
        private Button nextButton;
        private TMP_Text pageLabel;
        private int pageIndex;

        private IEnumerator Start()
        {
            for (int i = 0; i < 60; i++)
            {
                controller = FindFirstObjectByType<LevelSelectionController>(FindObjectsInactive.Include);
                if (controller != null)
                    break;
                yield return null;
            }

            if (controller == null)
            {
                Destroy(gameObject);
                yield break;
            }

#if UNITY_EDITOR
            ResetEditorProgressOnce();
#endif

            EnsureEventSystem();
            CaptureLegacyVisualsAndHideButtons();
            BuildControls();
            Refresh();
        }

#if UNITY_EDITOR
        private void ResetEditorProgressOnce()
        {
            if (PlayerPrefs.GetInt(EditorResetKey, 0) == 1)
                return;

            LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
            save.RealLevelNumber = 0;
            save.DisplayLevelNumber = 0;
            save.selectedLevelIndex = 0;
            save.isPlayingFromLevelSelection = false;
            save.ReplayingLevelAgain = false;
            save.levelProgress.Clear();

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);
            PlayerPrefs.SetInt(EditorResetKey, 1);
            PlayerPrefs.Save();

            Debug.Log("[Conveyor Chef UI] Editor test progression reset once: Level 1 unlocked, 0 stars.");
        }
#endif

        private void EnsureEventSystem()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (systems.Length == 0)
            {
                GameObject eventSystem = new GameObject("CC_LevelSelectionEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(null);
                return;
            }

            EventSystem keeper = null;
            foreach (EventSystem system in systems)
            {
                if (system != null && system.enabled && system.gameObject.activeInHierarchy)
                {
                    keeper = system;
                    break;
                }
            }

            if (keeper == null)
            {
                keeper = systems[0];
                keeper.enabled = true;
                keeper.gameObject.SetActive(true);
            }

            foreach (EventSystem system in systems)
            {
                if (system == null || system == keeper)
                    continue;

                system.enabled = false;
                foreach (BaseInputModule module in system.GetComponents<BaseInputModule>())
                    module.enabled = false;
            }
        }

        private void CaptureLegacyVisualsAndHideButtons()
        {
            foreach (LevelButton levelButton in FindObjectsByType<LevelButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                TextMeshProUGUI text = levelButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (font == null && text != null)
                    font = text.font;

                Button sourceButton = levelButton.GetComponentInChildren<Button>(true);
                if (sourceButton != null && sourceButton.targetGraphic is Image sourceImage && levelButtonSprite == null)
                {
                    levelButtonSprite = sourceImage.sprite;
                    levelButtonImageType = sourceImage.type;
                }

                foreach (Image childImage in levelButton.GetComponentsInChildren<Image>(true))
                {
                    string n = childImage.name.ToLowerInvariant();
                    if (lockSprite == null && n.Contains("lock") && childImage.sprite != null)
                        lockSprite = childImage.sprite;
                    if (starSprite == null && n.Contains("star") && childImage.sprite != null)
                        starSprite = childImage.sprite;
                }

                levelButton.gameObject.SetActive(false);
            }
        }

        private void BuildControls()
        {
            GameObject root = new GameObject("CC_DynamicLevelGrid", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            levelButtons = new Button[LevelsPerPage];
            levelLabels = new TMP_Text[LevelsPerPage];
            levelImages = new Image[LevelsPerPage];
            lockIcons = new Image[LevelsPerPage];
            stars = new Image[LevelsPerPage][];

            float[] xs = { 0.285f, 0.5f, 0.715f };
            float[] ys = { 0.585f, 0.455f, 0.325f, 0.195f };

            int slot = 0;
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 3; column++, slot++)
                    CreateLevelButton(root.transform, slot, xs[column], ys[row]);
            }

            backButton = CreateNavigationButton(root.transform, "BackButton", "BACK", new Vector2(0.045f, 0.035f), new Vector2(0.205f, 0.115f), PreviousPage);
            nextButton = CreateNavigationButton(root.transform, "NextButton", "NEXT", new Vector2(0.775f, 0.035f), new Vector2(0.955f, 0.115f), NextPage);

            pageLabel = CreateText(root.transform, "PageLabel", "", 28f);
            RectTransform pageRect = pageLabel.rectTransform;
            pageRect.anchorMin = new Vector2(0.38f, 0.045f);
            pageRect.anchorMax = new Vector2(0.62f, 0.095f);
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;
            pageLabel.color = new Color(1f, 0.82f, 0.24f, 1f);
        }

        private void CreateLevelButton(Transform parent, int slot, float x, float y)
        {
            GameObject go = new GameObject("DynamicLevel_" + slot, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(x, y);
            rect.sizeDelta = new Vector2(205f, 190f);

            Image image = go.GetComponent<Image>();
            image.sprite = levelButtonSprite;
            image.type = levelButtonSprite != null ? levelButtonImageType : Image.Type.Simple;
            image.color = LockedColor;
            levelImages[slot] = image;

            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.13f, 0.06f, 0.02f, 0.42f);
            shadow.effectDistance = new Vector2(0f, -7f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.fadeDuration = 0.07f;
            button.colors = colors;

            int capturedSlot = slot;
            button.onClick.AddListener(() => OpenLevel(capturedSlot));
            levelButtons[slot] = button;
            go.AddComponent<ArtworkButtonFeedback>();

            TMP_Text label = CreateText(go.transform, "LevelNumber", "", 62f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.08f, 0.28f);
            labelRect.anchorMax = new Vector2(0.92f, 0.92f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            levelLabels[slot] = label;

            GameObject lockObject = new GameObject("Lock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lockObject.transform.SetParent(go.transform, false);
            RectTransform lockRect = lockObject.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.42f);
            lockRect.sizeDelta = new Vector2(62f, 70f);
            Image lockImage = lockObject.GetComponent<Image>();
            lockImage.sprite = lockSprite;
            lockImage.preserveAspect = true;
            lockImage.color = Color.white;
            lockImage.raycastTarget = false;
            lockIcons[slot] = lockImage;

            stars[slot] = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject starObject = new GameObject("Star_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                starObject.transform.SetParent(go.transform, false);
                RectTransform starRect = starObject.GetComponent<RectTransform>();
                starRect.anchorMin = starRect.anchorMax = new Vector2(0.34f + i * 0.16f, 0.16f);
                starRect.sizeDelta = new Vector2(46f, 46f);
                Image starImage = starObject.GetComponent<Image>();
                starImage.sprite = starSprite;
                starImage.preserveAspect = true;
                starImage.color = new Color(1f, 0.84f, 0.05f, 1f);
                starImage.raycastTarget = false;
                starObject.SetActive(false);
                stars[slot][i] = starImage;
            }
        }

        private Button CreateNavigationButton(Transform parent, string objectName, string labelText, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.sprite = levelButtonSprite;
            image.type = levelButtonSprite != null ? levelButtonImageType : Image.Type.Simple;
            image.color = new Color(1f, 0.94f, 0.80f, 1f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            go.AddComponent<ArtworkButtonFeedback>();

            TMP_Text text = CreateText(go.transform, objectName + "Label", labelText, 27f);
            text.color = DarkText;
            return button;
        }

        private TMP_Text CreateText(Transform parent, string objectName, string value, float fontSize)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DarkText;
            text.raycastTarget = false;
            if (font != null)
                text.font = font;
            return text;
        }

        private void Refresh()
        {
            int startIndex = pageIndex * LevelsPerPage;

            for (int i = 0; i < LevelsPerPage; i++)
            {
                int levelIndex = startIndex + i;
                bool exists = levelIndex < TotalLevels;
                levelButtons[i].gameObject.SetActive(exists);
                if (!exists)
                    continue;

                bool unlocked = LevelController.IsLevelUnlocked(levelIndex);
                bool completed = LevelController.IsLevelCompleted(levelIndex);
                int starsEarned = LevelController.GetLevelStars(levelIndex);

                levelButtons[i].interactable = unlocked;
                levelImages[i].color = !unlocked ? LockedColor : completed ? CompletedColor : CurrentColor;
                levelLabels[i].text = (levelIndex + 1).ToString();
                levelLabels[i].color = unlocked ? DarkText : Color.white;

                if (lockIcons[i] != null)
                {
                    lockIcons[i].gameObject.SetActive(!unlocked);
                    if (lockSprite == null)
                        lockIcons[i].gameObject.SetActive(false);
                }

                for (int star = 0; star < 3; star++)
                {
                    bool showStar = completed && star < starsEarned && starSprite != null;
                    stars[i][star].gameObject.SetActive(showStar);
                }
            }

            int totalPages = Mathf.CeilToInt(TotalLevels / (float)LevelsPerPage);
            backButton.interactable = true;
            nextButton.interactable = pageIndex < totalPages - 1;
            pageLabel.text = $"{pageIndex + 1} / {totalPages}";
        }

        private void OpenLevel(int slot)
        {
            int levelIndex = pageIndex * LevelsPerPage + slot;
            if (levelIndex >= TotalLevels || !LevelController.IsLevelUnlocked(levelIndex))
                return;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            controller.LoadSelectedLevel(levelIndex);
        }

        private void PreviousPage()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);

            if (pageIndex <= 0)
            {
                SceneManager.LoadScene("menu");
                return;
            }

            pageIndex--;
            Refresh();
        }

        private void NextPage()
        {
            int totalPages = Mathf.CeilToInt(TotalLevels / (float)LevelsPerPage);
            if (pageIndex >= totalPages - 1)
                return;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            pageIndex++;
            Refresh();
        }
    }
}
