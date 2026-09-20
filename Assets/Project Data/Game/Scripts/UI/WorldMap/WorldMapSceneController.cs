using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Runtime behaviour for the serialized World Map scene.
    /// It does not rebuild or reposition designer-authored UI at startup.
    /// Only the ScrollRect content moves when the player navigates the map.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapSceneController : MonoBehaviour
    {
        private const int LevelsPerContinent = 15;
        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";
        private const string DragHintSeenKey = "CC_WorldMap_DragHintSeen";

        [Header("Map")]
        [SerializeField] private ScrollRect mapScrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private WorldMapContinentNode[] continents;

        [Header("Navigation")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI selectedChapterText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Drag Hint")]
        [SerializeField] private CanvasGroup dragHintCanvasGroup;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TextMeshProUGUI soundText;
        [SerializeField] private TextMeshProUGUI vibrationText;

        [Header("Motion")]
        [SerializeField, Min(0.05f)] private float focusDuration = 0.35f;

        [Header("Scene-authored startup")]
        [Tooltip("When enabled, Play mode starts with exactly the sprites, colors, active states, sizes and positions saved in WorldMap.unity.")]
        [SerializeField] private bool preserveAuthoredStartupVisuals = true;
        [SerializeField, Range(0, 5)] private int authoredSelectedContinent = 0;
        [Tooltip("Normally OFF. Enabling this intentionally moves MapContent when Play begins.")]
        [SerializeField] private bool focusSelectedContinentOnStart = false;

        private int selectedContinent;
        private Coroutine focusRoutine;
        private Coroutine hintRoutine;

        // Captured from the serialized WorldMap.unity before any runtime navigation.
        // This is the position designers see and edit in the Scene/Simulator before Play.
        private Vector2 authoredMapContentPosition;

        public int SelectedContinent => selectedContinent;

        private void Awake()
        {
            if (mapContent != null)
                authoredMapContentPosition = mapContent.anchoredPosition;

            EnsureEventSystem();
            EnsureSaveControllerReady();
            EnsureMapInteractionReady();

            Wire(leftButton, PreviousContinent);
            Wire(rightButton, NextContinent);
            Wire(backButton, BackToMenu);
            Wire(settingsButton, OpenSettings);
            Wire(closeSettingsButton, CloseSettings);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);

            // Preserve the serialized active/inactive state of SettingsPanel.
            // The initial runtime frame must match the Scene/Simulator preview.
            RepairContinentReferencesIfNeeded();

            if (continents != null)
            {
                foreach (WorldMapContinentNode node in continents)
                {
                    // Do not use ?. with UnityEngine.Object references.
                    // A destroyed Unity object is not managed-null, so ?. can still
                    // call into its destroyed native object and throw MissingReferenceException.
                    if (node != null)
                        node.Bind(this);
                }
            }

            if (mapScrollRect != null)
                mapScrollRect.onValueChanged.AddListener(OnMapScrollValueChanged);
        }

        private void Start()
        {
            if (preserveAuthoredStartupVisuals)
            {
                selectedContinent = Mathf.Clamp(
                    authoredSelectedContinent,
                    0,
                    continents != null && continents.Length > 0 ? continents.Length - 1 : 0);

                if (mapContent != null)
                    mapContent.anchoredPosition = authoredMapContentPosition;

                // Apply gameplay progression immediately so locked continents are
                // correct on the first visible frame. RefreshAll is now visual-safe:
                // it updates lock sprites/glow/button states only and never rewrites
                // designer-authored labels, text colors or continent artwork colors.
                RefreshAll();
            }
            else
            {
                selectedContinent = RestoreSelectedContinent();
                RefreshAll();
            }

            Canvas.ForceUpdateCanvases();

            if (focusSelectedContinentOnStart)
                FocusContinent(selectedContinent, false);

            // DragHint's serialized state remains scene-authored.
        }

        private void OnDestroy()
        {
            if (mapScrollRect != null)
                mapScrollRect.onValueChanged.RemoveListener(OnMapScrollValueChanged);
        }

        private void RepairContinentReferencesIfNeeded()
        {
            bool needsRepair = continents == null || continents.Length == 0;

            if (!needsRepair)
            {
                for (int i = 0; i < continents.Length; i++)
                {
                    // Unity's overloaded == correctly treats destroyed objects as null.
                    if (continents[i] == null)
                    {
                        needsRepair = true;
                        break;
                    }
                }
            }

            if (mapContent == null)
                return;

            WorldMapContinentNode[] liveNodes =
                mapContent.GetComponentsInChildren<WorldMapContinentNode>(true);

            if (liveNodes == null || liveNodes.Length == 0)
                return;

            if (!needsRepair && continents.Length == liveNodes.Length)
                return;

            Array.Sort(
                liveNodes,
                (a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return a.ContinentIndex.CompareTo(b.ContinentIndex);
                });

            continents = liveNodes;

            for (int i = 0; i < continents.Length; i++)
            {
                WorldMapContinentNode node = continents[i];
                if (node != null)
                    node.Bind(this);
            }

            Debug.Log(
                "[WorldMap] Repaired continent references from the live serialized MapContent hierarchy.");
        }


        private void EnsureMapInteractionReady()
        {
            // Interaction-only repair. This never changes positions, sizes, anchors,
            // pivots, scale or any other authored visual layout.
            if (mapScrollRect == null)
                return;

            mapScrollRect.enabled = true;
            mapScrollRect.horizontal = true;
            mapScrollRect.vertical = true;

            if (mapContent != null && mapScrollRect.content != mapContent)
                mapScrollRect.content = mapContent;

            RectTransform viewport = mapScrollRect.viewport;
            if (viewport == null)
                viewport = mapScrollRect.transform as RectTransform;

            if (viewport != null)
            {
                mapScrollRect.viewport = viewport;

                Graphic viewportGraphic = viewport.GetComponent<Graphic>();
                if (viewportGraphic != null)
                    viewportGraphic.raycastTarget = true;

                Canvas canvas = viewport.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                    if (raycaster == null)
                        raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();

                    raycaster.enabled = true;
                }
            }

            mapScrollRect.StopMovement();

            Debug.Log(
                "[WorldMap Input] Ready. EventSystem=" +
                (EventSystem.current != null ? EventSystem.current.name : "NULL") +
                ", ScrollRect=" + mapScrollRect.enabled +
                ", Horizontal=" + mapScrollRect.horizontal +
                ", Vertical=" + mapScrollRect.vertical +
                ", Content=" + (mapScrollRect.content != null ? mapScrollRect.content.name : "NULL") +
                ", Viewport=" + (mapScrollRect.viewport != null ? mapScrollRect.viewport.name : "NULL"));
        }

        private static void EnsureEventSystem()
        {
            UIEventSystemRuntime.UseCurrentSceneEventSystem();
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
                Debug.LogError("[WorldMap] Failed to initialise SaveController: " + ex.Message);
            }
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            // Do not change interactable/visual state on startup. The Button's
            // serialized state in WorldMap.unity is authoritative.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public bool IsContinentUnlocked(int continentIndex)
        {
            if (continentIndex <= 0)
                return true;

            int previousStart = (continentIndex - 1) * LevelsPerContinent;
            int previousEndExclusive = previousStart + LevelsPerContinent;

            for (int level = previousStart; level < previousEndExclusive; level++)
            {
                if (!LevelController.IsLevelCompleted(level))
                    return false;
            }

            return true;
        }

        private int RestoreSelectedContinent()
        {
            if (continents == null || continents.Length == 0)
                return 0;

            int highestUnlocked = 0;
            for (int i = 0; i < continents.Length; i++)
            {
                if (IsContinentUnlocked(i))
                    highestUnlocked = i;
                else
                    break;
            }

            int stored = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedContinentKey, highestUnlocked),
                0,
                continents.Length - 1);

            return IsContinentUnlocked(stored) ? stored : highestUnlocked;
        }

        public void HandleContinentPressed(int continentIndex)
        {
            if (continents == null || continentIndex < 0 || continentIndex >= continents.Length)
                return;

            SelectContinent(continentIndex, true);

            if (!IsContinentUnlocked(continentIndex))
            {
                if (statusText != null)
                    statusText.text = "LOCKED • COMPLETE THE PREVIOUS CONTINENT";

                return;
            }

            PlayerPrefs.SetInt(SelectedContinentKey, continentIndex);
            PlayerPrefs.SetInt("CC_CountryMap_LaunchedFromWorldMap", 1);
            PlayerPrefs.Save();

            PlayClick();
            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("CountryMap");
        }

        private void SelectContinent(int continentIndex, bool animated)
        {
            if (continents == null || continents.Length == 0)
                return;

            selectedContinent = Mathf.Clamp(continentIndex, 0, continents.Length - 1);
            PlayerPrefs.SetInt(SelectedContinentKey, selectedContinent);
            PlayerPrefs.Save();

            RefreshAll();
            FocusContinent(selectedContinent, animated);
        }

        private void PreviousContinent()
        {
            if (continents == null || continents.Length == 0)
                return;

            PlayClick();
            SelectContinent(Mathf.Max(0, selectedContinent - 1), true);
        }

        private void NextContinent()
        {
            if (continents == null || continents.Length == 0)
                return;

            PlayClick();
            SelectContinent(Mathf.Min(continents.Length - 1, selectedContinent + 1), true);
        }

        private void RefreshAll()
        {
            RepairContinentReferencesIfNeeded();

            if (continents == null || continents.Length == 0)
                return;

            selectedContinent = Mathf.Clamp(selectedContinent, 0, continents.Length - 1);

            for (int i = 0; i < continents.Length; i++)
            {
                bool unlocked = IsContinentUnlocked(i);
                WorldMapContinentNode node = continents[i];

                if (node != null)
                    node.Refresh(unlocked, i == selectedContinent);
            }

            // Do not rewrite selectedChapterText or statusText here.
            // Those are designer-authored scene elements. Runtime selection/progression
            // is communicated by lock sprites, glow and button availability.

            if (leftButton != null)
                leftButton.interactable = selectedContinent > 0;

            if (rightButton != null)
                rightButton.interactable = selectedContinent < continents.Length - 1;
        }

        public void FocusContinent(int index, bool animated)
        {
            RepairContinentReferencesIfNeeded();

            if (mapScrollRect == null || mapContent == null || continents == null ||
                index < 0 || index >= continents.Length || continents[index] == null)
            {
                return;
            }

            RectTransform viewport = mapScrollRect.viewport;
            RectTransform target = continents[index].MapTarget;

            if (viewport == null || target == null)
                return;

            Canvas.ForceUpdateCanvases();

            Vector2 desired = -target.anchoredPosition;

            float maxX = Mathf.Max(0f, (mapContent.rect.width - viewport.rect.width) * 0.5f);
            float maxY = Mathf.Max(0f, (mapContent.rect.height - viewport.rect.height) * 0.5f);

            desired.x = Mathf.Clamp(desired.x, -maxX, maxX);
            desired.y = Mathf.Clamp(desired.y, -maxY, maxY);

            if (focusRoutine != null)
                StopCoroutine(focusRoutine);

            mapScrollRect.StopMovement();

            if (animated && isActiveAndEnabled)
                focusRoutine = StartCoroutine(FocusRoutine(desired));
            else
                mapContent.anchoredPosition = desired;
        }

        private IEnumerator FocusRoutine(Vector2 targetPosition)
        {
            Vector2 start = mapContent.anchoredPosition;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, focusDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                mapContent.anchoredPosition = Vector2.LerpUnclamped(start, targetPosition, eased);
                yield return null;
            }

            mapContent.anchoredPosition = targetPosition;
            focusRoutine = null;
        }

        public void SelectNearestToViewport()
        {
            // Intentionally no-op for free-drag scrolling.
            //
            // Dragging is viewport navigation only; it must not change selectedContinent
            // or refresh authored UI. Selection changes only through explicit continent
            // clicks or Previous/Next buttons.
        }

        public void NotifyMapDragged()
        {
            if (PlayerPrefs.GetInt(DragHintSeenKey, 0) == 0)
            {
                PlayerPrefs.SetInt(DragHintSeenKey, 1);
                PlayerPrefs.Save();
            }

            SetDragHintVisible(false, immediate: false);
        }

        private void OnMapScrollValueChanged(Vector2 _)
        {
            if (mapScrollRect != null && mapScrollRect.velocity.sqrMagnitude > 16f)
                NotifyMapDragged();
        }

        private void SetDragHintVisible(bool visible, bool immediate)
        {
            if (dragHintCanvasGroup == null)
                return;

            if (hintRoutine != null)
                StopCoroutine(hintRoutine);

            float target = visible ? 1f : 0f;

            if (immediate || !isActiveAndEnabled)
            {
                dragHintCanvasGroup.alpha = target;
                dragHintCanvasGroup.blocksRaycasts = false;
                dragHintCanvasGroup.interactable = false;
                return;
            }

            hintRoutine = StartCoroutine(FadeHint(target));
        }

        private IEnumerator FadeHint(float target)
        {
            float start = dragHintCanvasGroup.alpha;
            float elapsed = 0f;
            const float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                dragHintCanvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            dragHintCanvasGroup.alpha = target;
            hintRoutine = null;
        }

        private void BackToMenu()
        {
            PlayClick();
            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen("menu");
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

        private static void PlayClick()
        {
            try
            {
                AudioController.PlaySound(AudioController.Sounds.buttonSound);
            }
            catch
            {
                // UI stays functional even if audio is not ready.
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            ScrollRect scrollRect,
            RectTransform content,
            WorldMapContinentNode[] nodes,
            Button previous,
            Button next,
            Button back,
            Button settings,
            TextMeshProUGUI chapterText,
            TextMeshProUGUI stateText,
            CanvasGroup hint,
            GameObject settingsRoot,
            Button settingsClose,
            Button sound,
            Button vibration,
            TextMeshProUGUI soundLabel,
            TextMeshProUGUI vibrationLabel)
        {
            mapScrollRect = scrollRect;
            mapContent = content;
            continents = nodes;
            leftButton = previous;
            rightButton = next;
            backButton = back;
            settingsButton = settings;
            selectedChapterText = chapterText;
            statusText = stateText;
            dragHintCanvasGroup = hint;
            settingsPanel = settingsRoot;
            closeSettingsButton = settingsClose;
            soundButton = sound;
            vibrationButton = vibration;
            soundText = soundLabel;
            vibrationText = vibrationLabel;
        }
#endif
    }
}
