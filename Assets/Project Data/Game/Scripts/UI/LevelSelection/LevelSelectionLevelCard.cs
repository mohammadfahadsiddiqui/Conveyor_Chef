using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Behaviour-only wrapper for one designer-authored level card.
    /// Runtime may swap state sprites/text and toggle lock/completion visuals,
    /// but it never creates, reparents, moves or resizes UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectionLevelCard : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int slotIndex;

        [Header("Interaction")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Button actionButton;

        [Header("Visuals")]
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image numberBadge;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image[] stars;
        [SerializeField] private Image actionButtonImage;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private GameObject actionLockIcon;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject completedBadge;

        [Header("State Art")]
        [SerializeField] private Sprite selectedFrameSprite;
        [SerializeField] private Sprite unlockedFrameSprite;
        [SerializeField] private Sprite lockedFrameSprite;
        [SerializeField] private Sprite playButtonSprite;
        [SerializeField] private Sprite lockedButtonSprite;

        private LevelSelectionController owner;
        private bool unlocked;

        // The Level Selection scene is designer-authored. Cache every RectTransform
        // under this card when Play Mode starts and restore it after runtime UI
        // updates so state changes can never resize/reposition an individual card.
        private RectTransformState[] authoredGeometry;

        private struct RectTransformState
        {
            public RectTransform rect;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector3 localScale;
            public Quaternion localRotation;
        }

        public int SlotIndex => slotIndex;

        private void Awake()
        {
            EnsureVisualLayering();
            CaptureAuthoredGeometry();
        }

        private void LateUpdate()
        {
            RestoreAuthoredGeometry();
        }

        public void Bind(LevelSelectionController controller)
        {
            owner = controller;

            // Capture here as well in case this component was enabled after Awake
            // or the scene was rebuilt by the editor baker immediately before play.
            EnsureVisualLayering();
            CaptureAuthoredGeometry();

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(HandleSelected);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(HandleAction);
            }
        }

        public void Refresh(
            int gameplayLevelIndex,
            string missionTitle,
            Sprite thumbnail,
            bool isUnlocked,
            bool isSelected,
            bool isCompleted,
            int starsEarned)
        {
            unlocked = isUnlocked;

            if (numberText != null)
                numberText.text = (slotIndex + 1).ToString();

            if (titleText != null)
                titleText.text = missionTitle;

            if (thumbnailImage != null && thumbnail != null)
                thumbnailImage.sprite = thumbnail;

            // Sprite swaps must never bring mission art in front of the card chrome.
            // This keeps Varanasi/Delhi/Mumbai and future thumbnail sprites behind
            // Card Frame without resizing or reparenting designer-authored UI.
            EnsureVisualLayering();

            if (cardFrame != null)
            {
                if (!isUnlocked && lockedFrameSprite != null)
                    cardFrame.sprite = lockedFrameSprite;
                else if (isSelected && selectedFrameSprite != null)
                    cardFrame.sprite = selectedFrameSprite;
                else if (unlockedFrameSprite != null)
                    cardFrame.sprite = unlockedFrameSprite;

                // State sprites may have different source texture dimensions or
                // transparent margins. Never let that affect the authored UI rect.
                cardFrame.type = Image.Type.Simple;
                cardFrame.preserveAspect = false;
                cardFrame.color = Color.white;
            }

            if (lockOverlay != null)
                lockOverlay.SetActive(false);

            if (completedBadge != null)
                completedBadge.SetActive(isCompleted);

            starsEarned = Mathf.Clamp(starsEarned, 0, 3);
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null)
                        continue;

                    stars[i].color = i < starsEarned
                        ? Color.white
                        : new Color(0.16f, 0.21f, 0.30f, 0.90f);
                }
            }

            if (actionButtonImage != null)
            {
                Sprite target = isUnlocked ? playButtonSprite : lockedButtonSprite;
                if (target != null)
                    actionButtonImage.sprite = target;

                actionButtonImage.color = Color.white;
            }

            if (actionText != null)
                actionText.text = isUnlocked ? "PLAY" : string.Empty;

            if (actionLockIcon != null)
                actionLockIcon.SetActive(!isUnlocked);

            if (actionButton != null)
                actionButton.interactable = isUnlocked;

            if (selectButton != null)
                selectButton.interactable = true;

            RestoreAuthoredGeometry();
        }

        private void EnsureVisualLayering()
        {
            if (thumbnailImage == null || cardFrame == null)
                return;

            Transform thumbnailViewport = thumbnailImage.transform.parent;
            if (thumbnailViewport == null || thumbnailViewport.parent != transform)
                return;

            if (thumbnailViewport.GetSiblingIndex() != 0)
                thumbnailViewport.SetSiblingIndex(0);

            int desiredFrameIndex = Mathf.Min(1, transform.childCount - 1);
            if (cardFrame.transform.parent == transform &&
                cardFrame.transform.GetSiblingIndex() != desiredFrameIndex)
            {
                cardFrame.transform.SetSiblingIndex(desiredFrameIndex);
            }
        }

        private void CaptureAuthoredGeometry()
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            authoredGeometry = new RectTransformState[rects.Length];

            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                authoredGeometry[i] = new RectTransformState
                {
                    rect = rect,
                    anchorMin = rect.anchorMin,
                    anchorMax = rect.anchorMax,
                    pivot = rect.pivot,
                    anchoredPosition = rect.anchoredPosition,
                    sizeDelta = rect.sizeDelta,
                    localScale = rect.localScale,
                    localRotation = rect.localRotation
                };
            }
        }

        private void RestoreAuthoredGeometry()
        {
            if (authoredGeometry == null)
                return;

            for (int i = 0; i < authoredGeometry.Length; i++)
            {
                RectTransformState state = authoredGeometry[i];
                if (state.rect == null)
                    continue;

                state.rect.anchorMin = state.anchorMin;
                state.rect.anchorMax = state.anchorMax;
                state.rect.pivot = state.pivot;
                state.rect.anchoredPosition = state.anchoredPosition;
                state.rect.sizeDelta = state.sizeDelta;
                state.rect.localScale = state.localScale;
                state.rect.localRotation = state.localRotation;
            }
        }

        private void HandleSelected()
        {
            owner?.HandleCardPressed(slotIndex);
        }

        private void HandleAction()
        {
            if (unlocked)
                owner?.HandlePlayPressed(slotIndex);
            else
                owner?.HandleLockedPressed(slotIndex);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            int index,
            Button cardButton,
            Button playButton,
            Image frame,
            Image thumbnail,
            Image badge,
            TextMeshProUGUI number,
            TextMeshProUGUI title,
            Image[] starImages,
            Image playButtonImage,
            TextMeshProUGUI playText,
            GameObject playLockIcon,
            GameObject lockRoot,
            GameObject completedRoot,
            Sprite selectedFrame,
            Sprite unlockedFrame,
            Sprite lockedFrame,
            Sprite playSprite,
            Sprite lockedButton)
        {
            slotIndex = index;
            selectButton = cardButton;
            actionButton = playButton;
            cardFrame = frame;
            thumbnailImage = thumbnail;
            numberBadge = badge;
            numberText = number;
            titleText = title;
            stars = starImages;
            actionButtonImage = playButtonImage;
            actionText = playText;
            actionLockIcon = playLockIcon;
            lockOverlay = lockRoot;
            completedBadge = completedRoot;
            selectedFrameSprite = selectedFrame;
            unlockedFrameSprite = unlockedFrame;
            lockedFrameSprite = lockedFrame;
            playButtonSprite = playSprite;
            lockedButtonSprite = lockedButton;
        }
#endif
    }
}
