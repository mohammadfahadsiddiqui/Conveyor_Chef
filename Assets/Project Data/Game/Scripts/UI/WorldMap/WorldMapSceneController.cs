using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [Tooltip("Keep this OFF to make Play mode start with the exact MapContent position saved in WorldMap.unity.")]
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

            Wire(leftButton, PreviousContinent);
            Wire(rightButton, NextContinent);
            Wire(backButton, BackToMenu);
            Wire(settingsButton, OpenSettings);
            Wire(closeSettingsButton, CloseSettings);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

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
            selectedContinent = RestoreSelectedContinent();
            RefreshAll();

            // CRITICAL: do not move MapContent automatically on scene start.
            // menu.unity/loading.unity keep their serialized visual composition when
            // Play begins, and WorldMap must behave the same way. The old code called
            // FocusContinent here, which changed MapContent.anchoredPosition and made
            // the Simulator jump from the full world layout to North America.
            if (mapContent != null)
                mapContent.anchoredPosition = authoredMapContentPosition;

            Canvas.ForceUpdateCanvases();

            // Optional for future use only. It is deliberately OFF by default so the
            // runtime starts exactly where the designer saved WorldMap.unity.
            if (focusSelectedContinentOnStart)
                FocusContinent(selectedContinent, false);

            bool hintSeen = PlayerPrefs.GetInt(DragHintSeenKey, 0) == 1;
            SetDragHintVisible(!hintSeen, immediate: true);
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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null && EventSystem.current.gameObject.activeInHierarchy)
                return;

            EventSystem existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null)
                existing.gameObject.SetActive(true);
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

            button.interactable = true;
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

            PlayClick();
            selectedContinent = continentIndex;
            PlayerPrefs.SetInt(SelectedContinentKey, selectedContinent);
            PlayerPrefs.Save();

            RefreshAll();
            FocusContinent(selectedContinent, true);
        }

        private void PreviousContinent()
        {
            if (continents == null || continents.Length == 0)
                return;

            PlayClick();
            HandleContinentPressed(Mathf.Max(0, selectedContinent - 1));
        }

        private void NextContinent()
        {
            if (continents == null || continents.Length == 0)
                return;

            PlayClick();
            HandleContinentPressed(Mathf.Min(continents.Length - 1, selectedContinent + 1));
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

            WorldMapContinentNode selectedNode = continents[selectedContinent];
            bool selectedUnlocked = IsContinentUnlocked(selectedContinent);

            if (selectedChapterText != null)
            {
                string displayName = selectedNode != null
                    ? selectedNode.ContinentName.ToUpperInvariant()
                    : "CONTINENT " + (selectedContinent + 1);

                selectedChapterText.text =
                    "CHAPTER " + (selectedContinent + 1) + "  •  " +
                    displayName;
            }

            if (statusText != null)
            {
                statusText.text = selectedUnlocked
                    ? "UNLOCKED  •  15 LEVELS"
                    : "LOCKED  •  COMPLETE THE PREVIOUS CONTINENT";
            }

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
            RepairContinentReferencesIfNeeded();

            if (continents == null || continents.Length == 0 || mapContent == null)
                return;

            Vector2 visibleCenterInContent = -mapContent.anchoredPosition;
            int nearest = selectedContinent;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < continents.Length; i++)
            {
                WorldMapContinentNode node = continents[i];

                // Explicit Unity null check is required here. Null-conditional (?.)
                // does not respect UnityEngine.Object's destroyed-object semantics.
                if (node == null)
                    continue;

                RectTransform target = node.MapTarget;
                if (target == null)
                    continue;

                float distance = (target.anchoredPosition - visibleCenterInContent).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = i;
                }
            }

            if (nearest != selectedContinent)
            {
                selectedContinent = nearest;
                PlayerPrefs.SetInt(SelectedContinentKey, selectedContinent);
                PlayerPrefs.Save();
                RefreshAll();
            }
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
