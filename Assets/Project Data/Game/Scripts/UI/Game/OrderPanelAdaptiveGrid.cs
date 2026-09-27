using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Keeps the ORDERS panel compact: large cards in one column for a few orders, smaller
    /// cards in two columns when a level has more order types (levels use up to 7).
    /// UIOrderPanel keeps spawning and updating the items; this only changes their layout.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class OrderPanelAdaptiveGrid : MonoBehaviour
    {
        [SerializeField] int maxSingleColumnItems = 3;
        [SerializeField] Vector2 singleColumnCell = new Vector2(232f, 96f);
        [SerializeField] Vector2 doubleColumnCell = new Vector2(112f, 80f);
        [SerializeField] Vector2 spacing = new Vector2(8f, 12f);

        private GridLayoutGroup grid;
        private int lastCount = -1;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.spacing = spacing;
        }

        private void LateUpdate()
        {
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                    count++;
            }

            if (count == lastCount)
                return;

            lastCount = count;

            bool twoColumns = count > maxSingleColumnItems;
            grid.constraintCount = twoColumns ? 2 : 1;
            grid.cellSize = twoColumns ? doubleColumnCell : singleColumnCell;

            for (int i = 0; i < transform.childCount; i++)
                ApplyItemLayout(transform.GetChild(i), twoColumns);
        }

        /// <summary>Icon on the left, amount text filling the rest of the card.</summary>
        public static void ApplyItemLayout(Transform item, bool compact)
        {
            float iconSize = compact ? 50f : 78f;
            float iconLeft = compact ? 4f : 8f;

            RectTransform icon = item.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = new Vector2(0f, 0.5f);
                icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0f, 0.5f);
                icon.anchoredPosition = new Vector2(iconLeft, 0f);
                icon.sizeDelta = new Vector2(iconSize, iconSize);
            }

            RectTransform text = item.Find("Order Text") as RectTransform;
            if (text != null)
            {
                text.anchorMin = Vector2.zero;
                text.anchorMax = Vector2.one;
                text.pivot = new Vector2(0.5f, 0.5f);
                text.offsetMin = new Vector2(iconLeft + iconSize + (compact ? 2f : 6f), 4f);
                text.offsetMax = new Vector2(compact ? -4f : -10f, -4f);
            }
        }
    }
}
