using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Makes every scene's UI look exactly as it does on the design phone (Samsung Galaxy S20
    /// Ultra, 1440x3200, used in the Device Simulator) on any device, phone or tablet.
    ///
    /// Each root canvas is scaled so the design phone's whole screen fits on the device, and that
    /// "design frame" sits centred. Everything placed on the screen (buttons, cards, panels, text)
    /// is moved into the design frame, so it keeps the same size, shape and position relative to
    /// the others. Full-screen art (backgrounds, dims, bars) still fills the whole screen: picture
    /// backgrounds are enlarged with their design-phone proportions, so they are cropped, never
    /// stretched. On the design phone nothing changes.
    ///
    /// Only anchors are changed at runtime; positions, sizes and the scene file are untouched, so
    /// the scene is still edited as before (preview it with the S20 Ultra in the Simulator).
    /// A canvas that already fits itself (main menu, kitchen backdrop) is left alone, and any
    /// canvas can opt out with <see cref="DesignFrameIgnore"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class DesignFrame : MonoBehaviour
    {
        /// <summary>The design phone's screen (Samsung Galaxy S20 Ultra, portrait).</summary>
        public static readonly Vector2 DesignScreen = new Vector2(1440f, 3200f);

        private const float RefreshInterval = 0.5f;   // picks up UI created or re-anchored later
        private const float Epsilon = 0.0001f;

        private sealed class Entry
        {
            public Vector2 originalMin, originalMax;   // as authored (or as last set by another script)
            public Vector2 appliedMin, appliedMax;     // what this component set

            // Looked up once.
            public bool skip;                          // DesignFrameIgnore, or placed by script (tutorial)
            public bool block;                         // layout group: moves as one piece
            public bool scrollView;
            public bool masked;
            public Image image;
            public RawImage rawImage;
        }

        private readonly Dictionary<RectTransform, Entry> entries = new Dictionary<RectTransform, Entry>();
        private Canvas canvas;
        private CanvasScaler scaler;
        private Vector2 designSize;
        private Vector2 lastScreen;
        private float nextRefresh;

        #region Attach

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Runs after the scene's Awake/OnEnable and before Start.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas found in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (IsRootCanvas(found) && ShouldFit(found) && found.GetComponent<DesignFrame>() == null)
                        found.gameObject.AddComponent<DesignFrame>();
                }
            }
        }

        private static bool IsRootCanvas(Canvas candidate)
        {
            Transform parent = candidate.transform.parent;
            return parent == null || parent.GetComponentInParent<Canvas>(true) == null;
        }

        private static bool ShouldFit(Canvas candidate)
        {
            if (candidate.renderMode == RenderMode.WorldSpace)
                return false;

            CanvasScaler candidateScaler = candidate.GetComponent<CanvasScaler>();
            if (candidateScaler == null || candidateScaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPhysicalSize)
                return false;

            // These already keep their own exact composition on every screen.
            return candidate.GetComponentInChildren<DesignFrameIgnore>(true) == null &&
                   candidate.GetComponentInChildren<MainMenuResponsiveLayout>(true) == null &&
                   candidate.GetComponentInChildren<KitchenBackdrop>(true) == null;
        }

        #endregion

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            scaler = GetComponent<CanvasScaler>();
            designSize = DesignCanvasSize(scaler);
        }

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (screen != lastScreen || Time.unscaledTime >= nextRefresh)
                Apply();
        }

        /// <summary>Canvas size the authored scaler gives on the design phone.</summary>
        private static Vector2 DesignCanvasSize(CanvasScaler source)
        {
            float scale;
            if (source.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                Vector2 reference = source.referenceResolution;
                float width = DesignScreen.x / Mathf.Max(1f, reference.x);
                float height = DesignScreen.y / Mathf.Max(1f, reference.y);
                switch (source.screenMatchMode)
                {
                    case CanvasScaler.ScreenMatchMode.Expand: scale = Mathf.Min(width, height); break;
                    case CanvasScaler.ScreenMatchMode.Shrink: scale = Mathf.Max(width, height); break;
                    default:
                        scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(width, 2f), Mathf.Log(height, 2f), source.matchWidthOrHeight));
                        break;
                }
            }
            else
            {
                scale = Mathf.Max(0.01f, source.scaleFactor);
            }

            return DesignScreen / scale;
        }

        private void Apply()
        {
            lastScreen = new Vector2(Screen.width, Screen.height);
            nextRefresh = Time.unscaledTime + RefreshInterval;

            if (scaler == null || lastScreen.x < 1f || lastScreen.y < 1f)
                return;

            // The whole design frame always fits: the canvas is at least the design size.
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (scaler.referenceResolution != designSize)
                scaler.referenceResolution = designSize;

            float scale = Mathf.Min(lastScreen.x / designSize.x, lastScreen.y / designSize.y);
            Vector2 canvasSize = lastScreen / scale;

            // Where the design frame lies inside the canvas, in 0-1 of the canvas.
            Vector2 frameMin = (canvasSize - designSize) * 0.5f;
            Vector2 normalizedMin = new Vector2(frameMin.x / canvasSize.x, frameMin.y / canvasSize.y);
            Vector2 normalizedMax = Vector2.one - normalizedMin;

            Fit((RectTransform)transform, canvasSize, normalizedMin, normalizedMax);
            PruneDestroyed();
        }

        private readonly List<RectTransform> destroyed = new List<RectTransform>();

        private void PruneDestroyed()
        {
            foreach (RectTransform rect in entries.Keys)
            {
                if (rect == null)
                    destroyed.Add(rect);
            }

            for (int i = 0; i < destroyed.Count; i++)
                entries.Remove(destroyed[i]);
            destroyed.Clear();
        }

        /// <summary>
        /// Places the children of <paramref name="parent"/> (size in canvas units) whose design
        /// frame lies at frameMin..frameMax (0-1 of the parent).
        /// </summary>
        private void Fit(RectTransform parent, Vector2 parentSize, Vector2 frameMin, Vector2 frameMax)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform child = parent.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                Entry entry = Track(child);
                if (entry.skip)
                    continue;

                Vector2 min = entry.originalMin;
                Vector2 max = entry.originalMax;

                bool fullX = Mathf.Abs(min.x) < Epsilon && Mathf.Abs(max.x - 1f) < Epsilon;
                bool fullY = Mathf.Abs(min.y) < Epsilon && Mathf.Abs(max.y - 1f) < Epsilon;
                Vector2 frameSize = frameMax - frameMin;

                // A picture stretched along one axis only would distort: move it as one piece.
                bool picture = IsPicture(entry);
                if (picture && fullX != fullY)
                    fullX = fullY = false;

                Vector2 newMin, newMax, childFrameMin, childFrameMax;
                if (entry.scrollView && fullX && fullY)
                {
                    // A full-screen scroll view (a map) just shows more of its content.
                    SetAnchors(child, entry, min, max);
                    continue;
                }

                // Layout groups and scroll views move as one piece, like any placed element.
                if (entry.block || entry.scrollView || (!fullX && !fullY))
                {
                    // Placed element: same place inside the design frame.
                    newMin = frameMin + Vector2.Scale(min, frameSize);
                    newMax = frameMin + Vector2.Scale(max, frameSize);
                    SetAnchors(child, entry, newMin, newMax);
                    continue;
                }

                if (fullX && fullY && picture)
                {
                    // Full-screen picture: the design frame enlarged to cover the parent, so the
                    // picture keeps the proportions it had on the design phone.
                    Vector2 frameUnits = Vector2.Scale(frameSize, parentSize);
                    float cover = Mathf.Max(parentSize.x / frameUnits.x, parentSize.y / frameUnits.y);
                    Vector2 centre = (frameMin + frameMax) * 0.5f;
                    Vector2 half = frameSize * (cover * 0.5f);
                    newMin = centre - half;
                    newMax = centre + half;
                    childFrameMin = new Vector2(0.5f - 0.5f / cover, 0.5f - 0.5f / cover);
                    childFrameMax = Vector2.one - childFrameMin;
                }
                else
                {
                    // Full along an axis (backgrounds, dims, bars, containers): keep filling the
                    // parent there. Along a kept axis the design frame inside the child is the
                    // parent's frame; along a moved axis the child is inside the frame already.
                    newMin = new Vector2(fullX ? min.x : frameMin.x + min.x * frameSize.x, fullY ? min.y : frameMin.y + min.y * frameSize.y);
                    newMax = new Vector2(fullX ? max.x : frameMin.x + max.x * frameSize.x, fullY ? max.y : frameMin.y + max.y * frameSize.y);

                    Vector2 childStart = Vector2.Scale(newMin, parentSize) + child.offsetMin;
                    Vector2 childSize = Vector2.Scale(newMax - newMin, parentSize) + child.sizeDelta;
                    Vector2 frameStart = Vector2.Scale(frameMin, parentSize);
                    Vector2 frameEnd = Vector2.Scale(frameMax, parentSize);
                    childFrameMin = new Vector2(
                        fullX ? (frameStart.x - childStart.x) / Mathf.Max(1f, childSize.x) : 0f,
                        fullY ? (frameStart.y - childStart.y) / Mathf.Max(1f, childSize.y) : 0f);
                    childFrameMax = new Vector2(
                        fullX ? (frameEnd.x - childStart.x) / Mathf.Max(1f, childSize.x) : 1f,
                        fullY ? (frameEnd.y - childStart.y) / Mathf.Max(1f, childSize.y) : 1f);
                }

                SetAnchors(child, entry, newMin, newMax);

                if (child.childCount > 0 && !entry.masked)
                {
                    Vector2 childSize = Vector2.Scale(newMax - newMin, parentSize) + child.sizeDelta;
                    Fit(child, childSize, childFrameMin, childFrameMax);
                }
            }
        }

        private Entry Track(RectTransform rect)
        {
            if (!entries.TryGetValue(rect, out Entry entry))
            {
                entry = new Entry
                {
                    originalMin = rect.anchorMin,
                    originalMax = rect.anchorMax,
                    appliedMin = rect.anchorMin,
                    appliedMax = rect.anchorMax,
                    skip = rect.GetComponent<DesignFrameIgnore>() != null ||
                           rect.GetComponent<TutorialCanvasController>() != null ||   // follows the board
                           rect.GetComponent<AspectRatioFitter>() != null,          // sizes itself
                    block = rect.GetComponent<LayoutGroup>() != null,
                    scrollView = rect.GetComponent<ScrollRect>() != null,
                    masked = rect.GetComponent<Mask>() != null,
                    image = rect.GetComponent<Image>(),
                    rawImage = rect.GetComponent<RawImage>(),
                };
                entries[rect] = entry;
            }
            else if (rect.anchorMin != entry.appliedMin || rect.anchorMax != entry.appliedMax)
            {
                // Another script placed it since (e.g. a safe-area fitter): that is the new layout.
                entry.originalMin = rect.anchorMin;
                entry.originalMax = rect.anchorMax;
            }
            return entry;
        }

        private static void SetAnchors(RectTransform rect, Entry entry, Vector2 min, Vector2 max)
        {
            if (rect.anchorMin != min)
                rect.anchorMin = min;
            if (rect.anchorMax != max)
                rect.anchorMax = max;
            entry.appliedMin = rect.anchorMin;
            entry.appliedMax = rect.anchorMax;
        }

        // A plain (not 9-sliced or tiled) image or raw image: it would distort if stretched.
        private static bool IsPicture(Entry entry)
        {
            if (entry.image != null)
                return entry.image.sprite != null && entry.image.type == Image.Type.Simple && !entry.image.preserveAspect;

            return entry.rawImage != null && entry.rawImage.texture != null;
        }
    }
}
