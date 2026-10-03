using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Fits one canvas's UI to a screen so it looks exactly as on the design phone (Samsung
    /// Galaxy S20 Ultra, 1440x3200). Used by <see cref="DesignFrame"/> in Play mode and by the
    /// editor preview (Simulator / Game view) in Edit mode; see DesignFrame for the rules.
    /// Only anchors and the canvas scaler change, and <see cref="Restore"/> puts both back.
    /// </summary>
    public sealed class DesignFrameFitter
    {
        /// <summary>The design phone's screen (Samsung Galaxy S20 Ultra, portrait).</summary>
        public static readonly Vector2 DesignScreen = new Vector2(1440f, 3200f);

        private const float Epsilon = 0.0001f;

        private sealed class Entry
        {
            public Vector2 originalMin, originalMax;   // as authored (or as last set by a script / the designer)
            public Vector2 appliedMin, appliedMax;     // what the fitter set
            public Vector2 mapOffset, mapScale = Vector2.one;   // applied = mapOffset + original * mapScale, per axis

            // Looked up once.
            public bool skip;                          // DesignFrameIgnore, tutorial, aspect-ratio fitter
            public bool block;                         // layout group: moves as one piece
            public bool scrollView;
            public bool masked;
            public Image image;
            public RawImage rawImage;
        }

        private readonly Dictionary<RectTransform, Entry> entries = new Dictionary<RectTransform, Entry>();
        private readonly List<RectTransform> destroyed = new List<RectTransform>();
        private readonly CanvasScaler scaler;

        // The scaler as authored.
        private readonly CanvasScaler.ScaleMode originalMode;
        private readonly Vector2 originalReference;
        private readonly CanvasScaler.ScreenMatchMode originalMatchMode;
        private readonly float originalMatch;

        private bool designerEdits;
        private bool changed;

        public Canvas Canvas { get; }
        public Vector2 DesignSize { get; }

        public DesignFrameFitter(Canvas canvas)
        {
            Canvas = canvas;
            scaler = canvas.GetComponent<CanvasScaler>();
            originalMode = scaler.uiScaleMode;
            originalReference = scaler.referenceResolution;
            originalMatchMode = scaler.screenMatchMode;
            originalMatch = scaler.matchWidthOrHeight;
            DesignSize = DesignCanvasSize(scaler);
        }

        /// <summary>Root canvases the fitter handles (others fit themselves or are world space).</summary>
        public static bool ShouldFit(Canvas candidate)
        {
            if (candidate == null || candidate.renderMode == RenderMode.WorldSpace)
                return false;

            Transform parent = candidate.transform.parent;
            if (parent != null && parent.GetComponentInParent<Canvas>(true) != null)
                return false;

            CanvasScaler candidateScaler = candidate.GetComponent<CanvasScaler>();
            if (candidateScaler == null || candidateScaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPhysicalSize)
                return false;

            // These already keep their own exact composition on every screen.
            return candidate.GetComponentInChildren<DesignFrameIgnore>(true) == null &&
                   candidate.GetComponentInChildren<MainMenuResponsiveLayout>(true) == null &&
                   candidate.GetComponentInChildren<KitchenBackdrop>(true) == null;
        }

        /// <summary>
        /// Fits the canvas to <paramref name="screen"/> (pixels). With <paramref name="fromDesigner"/>,
        /// anchors changed since the last fit were edited in the fitted view and are converted back
        /// to design values; otherwise (scripts at runtime) they are taken as design values.
        /// Returns true when anything moved.
        /// </summary>
        public bool Apply(Vector2 screen, bool fromDesigner)
        {
            if (scaler == null || Canvas == null || screen.x < 1f || screen.y < 1f)
                return false;

            designerEdits = fromDesigner;
            changed = false;

            // The whole design frame always fits: the canvas is at least the design size.
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; changed = true; }
            if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand) { scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; changed = true; }
            if (scaler.referenceResolution != DesignSize) { scaler.referenceResolution = DesignSize; changed = true; }

            float scale = Mathf.Min(screen.x / DesignSize.x, screen.y / DesignSize.y);
            Vector2 canvasSize = screen / scale;

            // Where the design frame lies inside the canvas, in 0-1 of the canvas.
            Vector2 frameMin = (canvasSize - DesignSize) * 0.5f;
            Vector2 normalizedMin = new Vector2(frameMin.x / canvasSize.x, frameMin.y / canvasSize.y);

            Fit((RectTransform)Canvas.transform, canvasSize, normalizedMin, Vector2.one - normalizedMin);
            PruneDestroyed();
            return changed;
        }

        /// <summary>Puts every anchor and the scaler back as authored.</summary>
        public void Restore()
        {
            foreach (KeyValuePair<RectTransform, Entry> pair in entries)
            {
                RectTransform rect = pair.Key;
                if (rect == null)
                    continue;

                Entry entry = pair.Value;
                // Edited since the last fit: keep the edit, converted to design values.
                if (rect.anchorMin != entry.appliedMin || rect.anchorMax != entry.appliedMax)
                    Adopt(rect, entry);

                rect.anchorMin = entry.originalMin;
                rect.anchorMax = entry.originalMax;
            }
            entries.Clear();

            if (scaler != null)
            {
                scaler.uiScaleMode = originalMode;
                scaler.referenceResolution = originalReference;
                scaler.screenMatchMode = originalMatchMode;
                scaler.matchWidthOrHeight = originalMatch;
            }
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

                if (entry.scrollView && fullX && fullY)
                {
                    // A full-screen scroll view (a map) just shows more of its content.
                    Map(child, entry, Vector2.zero, Vector2.one);
                    continue;
                }

                // Layout groups and scroll views move as one piece, like any placed element.
                if (entry.block || entry.scrollView || (!fullX && !fullY))
                {
                    Map(child, entry, frameMin, frameSize);
                    continue;
                }

                Vector2 childFrameMin, childFrameMax;
                if (fullX && fullY && picture)
                {
                    // Full-screen picture: the design frame enlarged to cover the parent, so the
                    // picture keeps the proportions it had on the design phone.
                    Vector2 frameUnits = Vector2.Scale(frameSize, parentSize);
                    float cover = Mathf.Max(parentSize.x / frameUnits.x, parentSize.y / frameUnits.y);
                    Vector2 coverSize = frameSize * cover;
                    Map(child, entry, (frameMin + frameMax - coverSize) * 0.5f, coverSize);

                    childFrameMin = Vector2.one * (0.5f - 0.5f / cover);
                    childFrameMax = Vector2.one - childFrameMin;
                }
                else
                {
                    // Full along an axis (backgrounds, dims, bars, containers): keep filling the
                    // parent there. Along a kept axis the design frame inside the child is the
                    // parent's frame; along a moved axis the child is inside the frame already.
                    Map(child, entry,
                        new Vector2(fullX ? 0f : frameMin.x, fullY ? 0f : frameMin.y),
                        new Vector2(fullX ? 1f : frameSize.x, fullY ? 1f : frameSize.y));

                    Vector2 childStart = Vector2.Scale(child.anchorMin, parentSize) + child.offsetMin;
                    Vector2 childSize = Vector2.Scale(child.anchorMax - child.anchorMin, parentSize) + child.sizeDelta;
                    Vector2 frameStart = Vector2.Scale(frameMin, parentSize);
                    Vector2 frameEnd = Vector2.Scale(frameMax, parentSize);
                    childFrameMin = new Vector2(
                        fullX ? (frameStart.x - childStart.x) / Mathf.Max(1f, childSize.x) : 0f,
                        fullY ? (frameStart.y - childStart.y) / Mathf.Max(1f, childSize.y) : 0f);
                    childFrameMax = new Vector2(
                        fullX ? (frameEnd.x - childStart.x) / Mathf.Max(1f, childSize.x) : 1f,
                        fullY ? (frameEnd.y - childStart.y) / Mathf.Max(1f, childSize.y) : 1f);
                }

                if (child.childCount > 0 && !entry.masked)
                {
                    Vector2 size = Vector2.Scale(child.anchorMax - child.anchorMin, parentSize) + child.sizeDelta;
                    Fit(child, size, childFrameMin, childFrameMax);
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
                Adopt(rect, entry);
            }
            return entry;
        }

        // Anchors were changed since the last fit, by a script (design values, e.g. a safe-area
        // fitter) or by the designer in the fitted view (converted back to design values).
        private void Adopt(RectTransform rect, Entry entry)
        {
            if (designerEdits)
            {
                entry.originalMin = Unmap(rect.anchorMin, entry);
                entry.originalMax = Unmap(rect.anchorMax, entry);
            }
            else
            {
                entry.originalMin = rect.anchorMin;
                entry.originalMax = rect.anchorMax;
            }
        }

        private static Vector2 Unmap(Vector2 applied, Entry entry)
        {
            return new Vector2(
                (applied.x - entry.mapOffset.x) / Mathf.Max(Epsilon, entry.mapScale.x),
                (applied.y - entry.mapOffset.y) / Mathf.Max(Epsilon, entry.mapScale.y));
        }

        // applied = offset + original * scale (per axis).
        private void Map(RectTransform rect, Entry entry, Vector2 offset, Vector2 scale)
        {
            entry.mapOffset = offset;
            entry.mapScale = scale;

            Vector2 min = offset + Vector2.Scale(entry.originalMin, scale);
            Vector2 max = offset + Vector2.Scale(entry.originalMax, scale);
            if (rect.anchorMin != min) { rect.anchorMin = min; changed = true; }
            if (rect.anchorMax != max) { rect.anchorMax = max; changed = true; }
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
    }
}
