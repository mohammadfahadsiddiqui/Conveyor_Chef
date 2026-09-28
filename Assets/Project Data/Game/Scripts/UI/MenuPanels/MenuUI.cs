using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Small uGUI toolkit used by the menu panels: rects, images, texts, buttons, cards,
    /// progress bars and layout helpers, all built from code with the MenuPanelArt look.
    /// </summary>
    public static class MenuUI
    {
        public static readonly Color TextDark = new Color32(92, 52, 24, 255);
        public static readonly Color TextSoft = new Color32(138, 96, 60, 255);
        public static readonly Color CardFill = new Color32(255, 250, 238, 255);
        public static readonly Color CardBorder = new Color32(226, 178, 96, 255);
        public static readonly Color CardHighlight = new Color32(255, 236, 170, 255);
        public static readonly Color Green = new Color32(64, 170, 64, 255);
        public static readonly Color Gold = new Color32(255, 196, 40, 255);

        private static readonly Dictionary<string, Sprite> roundedSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Material> outlineMaterials = new Dictionary<string, Material>();

        public static TMP_FontAsset Font;

        #region Basic objects

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color? color = null, bool preserveAspect = true)
        {
            RectTransform rect = Rect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color ?? Color.white;
            image.preserveAspect = preserveAspect && sprite != null && sprite.border == Vector4.zero;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
                image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool wrap = false)
        {
            RectTransform rect = Rect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null)
                text.font = Font;
            text.text = value;
            text.color = color;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, size * 0.55f);
            text.fontSizeMax = size;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>White text with a dark outline, for titles and button labels.</summary>
        public static TextMeshProUGUI OutlinedText(string name, Transform parent, string value, float size, Color outline,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI text = Text(name, parent, value, size, Color.white, alignment);
            Material material = GetOutlineMaterial(outline);
            if (material != null)
                text.fontSharedMaterial = material;
            return text;
        }

        private static Material GetOutlineMaterial(Color outline)
        {
            if (Font == null || Font.material == null)
                return null;

            string key = ColorUtility.ToHtmlStringRGBA(outline);
            if (outlineMaterials.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            Material material = new Material(Font.material) { name = "Menu Outline " + key };
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", outline);
            material.SetFloat("_OutlineWidth", 0.22f);
            material.SetFloat("_FaceDilate", 0.18f);
            outlineMaterials[key] = material;
            return material;
        }

        public static Button Button(Image image, UnityEngine.Events.UnityAction action, bool pressEffect = true)
        {
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
            button.colors = colors;
            button.onClick.AddListener(() => { PlayClick(); action?.Invoke(); });
            if (pressEffect)
                image.gameObject.AddComponent<MenuButtonPress>();
            return button;
        }

        /// <summary>A 9-sliced art button with an outlined label (and an optional icon before it).</summary>
        public static Button LabelButton(string name, Transform parent, Sprite sprite, string label, float fontSize,
            UnityEngine.Events.UnityAction action, Sprite icon = null, Color? outline = null)
        {
            Image image = Image(name, parent, sprite, sprite == null ? Gold : (Color?)null, false);
            image.pixelsPerUnitMultiplier = 2.2f;
            Button button = Button(image, action);

            RectTransform row = Rect("Label Row", image.transform);
            Stretch(row, 14f, 10f, 14f, 16f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (icon != null)
            {
                Image iconImage = Image("Icon", row, icon);
                LayoutElement iconLayout = iconImage.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = fontSize * 1.2f;
                iconLayout.preferredHeight = fontSize * 1.2f;
            }

            TextMeshProUGUI text = OutlinedText("Label", row, label, fontSize, outline ?? new Color32(40, 60, 20, 255));
            text.enableAutoSizing = false;
            text.fontSize = fontSize;
            return button;
        }

        #endregion

        #region Cards, bars, pills

        /// <summary>Rounded, 9-sliced sprite made in code (cached), with baked fill and border colours.</summary>
        public static Sprite Rounded(int radius, int borderWidth, Color fill, Color border)
        {
            string key = radius + "_" + borderWidth + "_" + ColorUtility.ToHtmlStringRGBA(fill) + "_" + ColorUtility.ToHtmlStringRGBA(border);
            if (roundedSprites.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            int size = radius * 2 + 4;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Rounded " + key,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            Color32[] pixels = new Color32[size * size];
            float centre = size * 0.5f;
            float inner = centre - 2f - radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - centre) - inner - 0.0001f);
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - centre) - inner - 0.0001f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);         // distance from the straight part
                    float coverage = Mathf.Clamp01(radius + 0.5f - d);
                    float borderMix = borderWidth > 0 ? Mathf.Clamp01(d - (radius - borderWidth) + 0.5f) : 0f;
                    Color c = Color.Lerp(fill, border, borderMix);
                    c.a *= coverage;
                    pixels[y * size + x] = c;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            int b = radius + 2;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            roundedSprites[key] = sprite;
            return sprite;
        }

        /// <summary>Cream card with a golden border and a soft shadow; returns the card rect.</summary>
        public static RectTransform Card(string name, Transform parent, float height, bool highlighted = false)
        {
            RectTransform holder = Rect(name, parent);
            LayoutElement layout = holder.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            Image shadow = Image("Shadow", holder, Rounded(26, 0, new Color(0.35f, 0.2f, 0.05f, 0.22f), Color.clear), null, false);
            Stretch(shadow.rectTransform, 0f, -6f, 0f, 6f);

            Image card = Image("Card", holder, Rounded(26, 4, highlighted ? CardHighlight : CardFill, highlighted ? Gold : CardBorder), null, false);
            Stretch(card.rectTransform);
            return card.rectTransform;
        }

        public static RectTransform ProgressBar(Transform parent, float value, Color fillColor, string label = null)
        {
            Image track = Image("Progress", parent, Rounded(18, 3, new Color32(232, 214, 186, 255), new Color32(196, 160, 110, 255)), null, false);
            RectTransform trackRect = track.rectTransform;

            float clamped = Mathf.Clamp01(value);
            if (clamped > 0.001f)
            {
                Image fill = Image("Fill", trackRect, Rounded(15, 0, fillColor, fillColor), null, false);
                RectTransform fillRect = fill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(Mathf.Max(clamped, 0.08f), 1f);
                fillRect.offsetMin = new Vector2(4f, 4f);
                fillRect.offsetMax = new Vector2(-4f, -4f);
            }

            if (!string.IsNullOrEmpty(label))
            {
                TextMeshProUGUI text = OutlinedText("Value", trackRect, label, 30f, new Color32(70, 45, 20, 255));
                Stretch(text.rectTransform, 6f, 2f, 6f, 2f);
            }

            return trackRect;
        }

        /// <summary>Icon + amount, e.g. a reward or a price.</summary>
        public static RectTransform Pill(string name, Transform parent, Sprite icon, string amount, float size, Color textColor)
        {
            RectTransform row = Rect(name, parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (icon != null)
            {
                Image iconImage = Image("Icon", row, icon);
                LayoutElement il = iconImage.gameObject.AddComponent<LayoutElement>();
                il.preferredWidth = size * 1.25f;
                il.preferredHeight = size * 1.25f;
            }

            TextMeshProUGUI text = Text("Amount", row, amount, size, textColor);
            text.enableAutoSizing = false;
            return row;
        }

        #endregion

        #region Layout

        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Anchor inside a parent by normalised coordinates, with a fixed size.</summary>
        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        public static VerticalLayoutGroup Vertical(RectTransform rect, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperCenter)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static RectTransform Section(Transform parent, string title)
        {
            RectTransform holder = Rect("Section " + title, parent);
            LayoutElement layout = holder.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 64f;

            TextMeshProUGUI text = OutlinedText("Title", holder, title, 44f, new Color32(150, 60, 20, 255), TextAlignmentOptions.Left);
            Stretch(text.rectTransform, 8f, 0f, 8f, 0f);
            return holder;
        }

        public static RectTransform Spacer(Transform parent, float height)
        {
            RectTransform rect = Rect("Spacer", parent);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            return rect;
        }

        #endregion

        public static void PlayClick()
        {
            try { AudioController.PlaySound(AudioController.Sounds.buttonSound); } catch { }
        }
    }

    /// <summary>Small press-down scale on menu buttons.</summary>
    public sealed class MenuButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 baseScale = Vector3.one;
        private bool pressed;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            transform.localScale = baseScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            transform.localScale = baseScale;
        }
    }
}
