using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Keeps the ORDERS panel compact: one wide column for a few orders, two narrow
    /// columns when a level has more order types than fit on screen (levels use up to 7).
    /// UIOrderPanel keeps spawning and updating the items; this only changes the grid.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class OrderPanelAdaptiveGrid : MonoBehaviour
    {
        // Sample rows placed by the editor builder so the panel is visible while editing.
        public const string PreviewItemPrefix = "Preview Order";

        [SerializeField] int maxSingleColumnItems = 3;
        [SerializeField] Vector2 singleColumnCell = new Vector2(244f, 84f);
        [SerializeField] Vector2 doubleColumnCell = new Vector2(118f, 76f);

        private GridLayoutGroup grid;
        private int lastCount = -1;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.name.StartsWith(PreviewItemPrefix))
                {
                    child.SetActive(false);
                    Destroy(child);
                }
            }
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
        }
    }
}
