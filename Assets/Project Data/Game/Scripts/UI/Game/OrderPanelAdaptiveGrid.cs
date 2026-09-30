using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Lays out the ORDERS panel for any number of orders (levels use 1-7), always inside
    /// the same content width so nothing reaches the frame's border:
    ///   1-3 orders: one column of large cards (icon left, count right)
    ///   4 orders:   one column of slightly shorter cards
    ///   5-7 orders: two columns of stacked cards (icon on top, count below)
    /// UIOrderPanel keeps spawning and updating the items; this only changes their layout.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class OrderPanelAdaptiveGrid : MonoBehaviour
    {
        // Width of the panel inside the frame's padding. Every layout fits in it.
        [SerializeField] float contentWidth = 232f;

        private const int MaxSingleColumnItems = 4;
        private const float ColumnSpacing = 10f;
        private const float RowSpacing = 10f;
        private const float LargeCardHeight = 96f;
        private const float MediumCardHeight = 84f;
        private const float StackedCardHeight = 100f;

        private enum Style { Large, Medium, Stacked }

        private GridLayoutGroup grid;
        private LayoutElement layoutElement;
        private int lastCount = -1;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 0, 0);

            // A fixed width keeps the frame the same size whatever the column count.
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = contentWidth;
            layoutElement.preferredWidth = contentWidth;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        private void LateUpdate()
        {
            int count = CountActiveItems();
            if (count != lastCount)
                Relayout(count);
        }

        /// <summary>Lays the items out now (called by UIOrderPanel after it spawns them).</summary>
        public void Refresh()
        {
            if (grid == null)
                Awake();
            Relayout(CountActiveItems());
        }

        private int CountActiveItems()
        {
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        private void Relayout(int count)
        {
            lastCount = count;

            Style style = count > MaxSingleColumnItems ? Style.Stacked
                : count == MaxSingleColumnItems ? Style.Medium
                : Style.Large;

            if (style == Style.Stacked)
            {
                grid.constraintCount = 2;
                grid.spacing = new Vector2(ColumnSpacing, RowSpacing);
                grid.cellSize = new Vector2((contentWidth - ColumnSpacing) * 0.5f, StackedCardHeight);
            }
            else
            {
                grid.constraintCount = 1;
                grid.spacing = new Vector2(0f, RowSpacing);
                grid.cellSize = new Vector2(contentWidth, style == Style.Medium ? MediumCardHeight : LargeCardHeight);
            }

            for (int i = 0; i < transform.childCount; i++)
                ApplyItemLayout(transform.GetChild(i), style, grid.cellSize);

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        /// <summary>Large card layout, used by the editor builder for the prefab.</summary>
        public static void ApplyItemLayout(Transform item, bool compact)
        {
            ApplyItemLayout(item, compact ? Style.Stacked : Style.Large,
                compact ? new Vector2(111f, StackedCardHeight) : new Vector2(232f, LargeCardHeight));
        }

        private static void ApplyItemLayout(Transform item, Style style, Vector2 cell)
        {
            RectTransform icon = item.Find("Icon") as RectTransform;
            RectTransform text = item.Find("Order Text") as RectTransform;

            if (style == Style.Stacked)
            {
                // Icon centred in the upper part, count on its own line below.
                const float textHeight = 30f;
                const float inset = 8f;
                float iconSize = Mathf.Min(cell.x - inset * 2f, cell.y - textHeight - inset * 1.5f);

                if (icon != null)
                {
                    icon.anchorMin = new Vector2(0.5f, 1f);
                    icon.anchorMax = new Vector2(0.5f, 1f);
                    icon.pivot = new Vector2(0.5f, 1f);
                    icon.anchoredPosition = new Vector2(0f, -inset);
                    icon.sizeDelta = new Vector2(iconSize, iconSize);
                }

                if (text != null)
                {
                    text.anchorMin = new Vector2(0f, 0f);
                    text.anchorMax = new Vector2(1f, 0f);
                    text.pivot = new Vector2(0.5f, 0f);
                    text.anchoredPosition = new Vector2(0f, inset * 0.5f);
                    text.sizeDelta = new Vector2(-inset * 2f, textHeight);
                }
            }
            else
            {
                // Icon on the left, count filling the rest of the card.
                const float inset = 10f;
                float iconSize = cell.y - inset * 2f;

                if (icon != null)
                {
                    icon.anchorMin = new Vector2(0f, 0.5f);
                    icon.anchorMax = new Vector2(0f, 0.5f);
                    icon.pivot = new Vector2(0f, 0.5f);
                    icon.anchoredPosition = new Vector2(inset, 0f);
                    icon.sizeDelta = new Vector2(iconSize, iconSize);
                }

                if (text != null)
                {
                    text.anchorMin = Vector2.zero;
                    text.anchorMax = Vector2.one;
                    text.pivot = new Vector2(0.5f, 0.5f);
                    text.offsetMin = new Vector2(inset + iconSize + 6f, 6f);
                    text.offsetMax = new Vector2(-inset - 4f, -6f);
                }
            }

            // Autosize may shrink far enough that the count never spills past the card.
            TextMeshProUGUI label = text != null ? text.GetComponent<TextMeshProUGUI>() : null;
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.alignment = TextAlignmentOptions.Center;
                label.overflowMode = TextOverflowModes.Overflow;
            }
        }
    }
}
