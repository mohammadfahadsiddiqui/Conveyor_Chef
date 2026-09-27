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

        [Header("Selection Glow")]
        [SerializeField] private Color selectionGlowColor = new Color(1f, 0.9f, 0.35f, 1f);
        [Tooltip("How far the glow reaches outside the card, in canvas units.")]
        [SerializeField] private float selectionGlowWidth = 56f;
        [Tooltip("How far the glow starts inside the card edge (covers transparent margins of the frame art).")]
        [SerializeField] private float selectionGlowInset = 6f;
        [SerializeField] private float selectionGlowFadeSpeed = 8f;
        [Tooltip("Transparent margins of the card frame art around its blue body, as fractions of the frame: left, right, top (below the number badge), bottom.")]
        [SerializeField] private Vector4 frameBodyInsets = new Vector4(0.103f, 0.0755f, 0.083f, 0.016f);

        private LevelSelectionController owner;
        private bool unlocked;

        private Image selectionGlow;
        private bool glowTarget;
        private float glowVisibility;
        private static Sprite glowSprite;

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
            EnsureSelectionGlow();
            EnsureVisualLayering();
            CaptureAuthoredGeometry();
        }

        private void LateUpdate()
        {
            RestoreAuthoredGeometry();
            AnimateSelectionGlow();
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

            // The glow fades out on the previous card and in on this one, so it appears
            // to move when the side arrows change the selected mission.
            EnsureSelectionGlow();
            glowTarget = isSelected;

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

            // The selection glow is the back-most child; the card art sits above it.
            int firstIndex = 0;
            if (selectionGlow != null && selectionGlow.transform.parent == transform)
            {
                selectionGlow.transform.SetSiblingIndex(0);
                firstIndex = 1;
            }

            if (thumbnailViewport.GetSiblingIndex() != firstIndex)
                thumbnailViewport.SetSiblingIndex(firstIndex);

            int desiredFrameIndex = Mathf.Min(firstIndex + 1, transform.childCount - 1);
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

                // The runtime glow animates its own scale; it is not authored UI.
                if (selectionGlow != null && rect == selectionGlow.rectTransform)
                    continue;
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

        #region Selection Glow

        private void EnsureSelectionGlow()
        {
            if (selectionGlow != null || !Application.isPlaying)
                return;

            GameObject go = new GameObject("Selection Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;

            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.SetAsFirstSibling();

            selectionGlow = go.GetComponent<Image>();
            selectionGlow.sprite = GetGlowSprite();
            selectionGlow.type = Image.Type.Sliced;
            selectionGlow.fillCenter = true;
            selectionGlow.pixelsPerUnitMultiplier = GlowTextureWidth / Mathf.Max(1f, selectionGlowWidth);
            selectionGlow.raycastTarget = false;
            selectionGlow.color = new Color(selectionGlowColor.r, selectionGlowColor.g, selectionGlowColor.b, 0f);

            LayoutSelectionGlow();
        }

        // Fits the glow to the visible blue body of the Card Frame art. The card root
        // is stretched by percentage anchors and is larger than the art on tall screens,
        // so it cannot be used for the glow's shape.
        private void LayoutSelectionGlow()
        {
            RectTransform glowRect = selectionGlow.rectTransform;
            RectTransform frame = cardFrame != null ? cardFrame.rectTransform : null;
            float outset = selectionGlowWidth - selectionGlowInset;

            if (frame == null || frame.parent != transform)
            {
                RectTransform root = (RectTransform)transform;
                glowRect.localPosition = (Vector3)root.rect.center;
                glowRect.sizeDelta = root.rect.size + Vector2.one * (2f * outset);
                return;
            }

            Vector2 size = frame.rect.size;
            float left = size.x * frameBodyInsets.x;
            float right = size.x * frameBodyInsets.y;
            float top = size.y * frameBodyInsets.z;
            float bottom = size.y * frameBodyInsets.w;

            Vector2 bodySize = new Vector2(size.x - left - right, size.y - top - bottom);
            Vector2 bodyCenter = frame.rect.center + new Vector2((left - right) * 0.5f, (bottom - top) * 0.5f);

            glowRect.localPosition = frame.localPosition + (Vector3)bodyCenter;
            glowRect.sizeDelta = bodySize + Vector2.one * (2f * outset);
        }

        private void AnimateSelectionGlow()
        {
            if (selectionGlow == null)
                return;

            LayoutSelectionGlow();

            glowVisibility = Mathf.MoveTowards(glowVisibility, glowTarget ? 1f : 0f, selectionGlowFadeSpeed * Time.unscaledDeltaTime);

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
            Color color = selectionGlowColor;
            color.a = glowVisibility * Mathf.Lerp(0.75f, 1f, pulse);
            selectionGlow.color = color;

            float scale = 1f + 0.025f * pulse * glowVisibility;
            selectionGlow.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // Glow width in texture pixels and the corner radius of the card shape.
        private const int GlowTextureWidth = 44;
        private const int GlowCornerRadius = 16;

        // A soft rounded-rectangle outline generated once: transparent inside the card
        // shape, brightest at its edge and fading outwards. Used as a 9-sliced sprite so
        // the glow keeps an even width on any card size.
        private static Sprite GetGlowSprite()
        {
            if (glowSprite != null)
                return glowSprite;

            int border = GlowTextureWidth + GlowCornerRadius;
            int size = border * 2 + 2;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Level Card Selection Glow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            float innerMin = GlowTextureWidth;
            float innerMax = size - GlowTextureWidth;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float cx = Mathf.Clamp(px, innerMin + GlowCornerRadius, innerMax - GlowCornerRadius);
                    float cy = Mathf.Clamp(py, innerMin + GlowCornerRadius, innerMax - GlowCornerRadius);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - GlowCornerRadius;

                    float alpha;
                    if (distance <= 0f)
                        alpha = Mathf.Clamp01(1f + distance / 6f); // short fade just inside the edge
                    else
                        alpha = Mathf.Pow(1f - Mathf.Clamp01(distance / GlowTextureWidth), 1.3f);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            glowSprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            glowSprite.name = texture.name;
            return glowSprite;
        }

        #endregion

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
