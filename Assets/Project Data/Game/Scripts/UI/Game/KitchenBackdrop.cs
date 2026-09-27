using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// Painted kitchen behind the 3D level (GAME BACKGROUND CANVAS [NEW UI]).
    ///
    /// The canvas is a Screen Space - Camera canvas placed near the camera's far plane,
    /// so the 3D conveyor, trays, waiting slots, board and donuts always draw in front of
    /// it. In Play mode the renderers of the 3D kitchen room, props and floor are switched
    /// off (colliders and GameObjects stay as they are), so the painting shows instead.
    ///
    /// The painting is drawn in two parts from one texture: the kitchen wall keeps its
    /// shape and is scaled so its countertop meets the top of the 3D conveyor; the floor
    /// and front counter below it stretch down to the bottom of any screen.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public sealed class KitchenBackdrop : MonoBehaviour
    {
        [SerializeField] RectTransform artwork;
        [SerializeField] RawImage kitchenPart;
        [SerializeField] RawImage floorPart;
        [SerializeField] Vector2 artPixelSize = new Vector2(941f, 1672f);

        [Tooltip("Row where the kitchen part ends and the floor part starts, from the top, as a fraction of the artwork height.")]
        [SerializeField] float floorStartRow = 640f / 1672f;
        [Tooltip("Row of the countertop front edge in the artwork, from the top, as a fraction of its height.")]
        [SerializeField] float countertopRow = 450f / 1672f;

        [Tooltip("Direct children of the level environment whose renderers are hidden (name prefixes).")]
        [SerializeField] string[] hiddenEnvironmentParts = { "FreeRoom", "_ObjectsWrapper", "Wall", "Kitchen", "Back", "Plane" };
        [Tooltip("Direct child of the level environment that is the conveyor (name prefix).")]
        [SerializeField] string conveyorName = "Conveyor";
        [SerializeField] float farPlaneFraction = 0.95f;

        private Canvas canvas;
        private EnvironmentBehavior environment;
        private readonly List<Renderer> conveyorRenderers = new List<Renderer>();
        private readonly Vector3[] corners = new Vector3[8];

        private void OnEnable()
        {
            canvas = GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            if (canvas == null || artwork == null)
                return;

            Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (cam == null)
                return;

            canvas.worldCamera = cam;
            canvas.planeDistance = cam.farClipPlane * farPlaneFraction;

            if (Application.isPlaying && LevelController.Environment != environment)
            {
                environment = LevelController.Environment;
                if (environment != null)
                    PrepareEnvironment(environment);
            }

            Layout(cam);
        }

        private void PrepareEnvironment(EnvironmentBehavior env)
        {
            conveyorRenderers.Clear();

            foreach (Transform child in env.transform)
            {
                if (child.name.StartsWith(conveyorName))
                    conveyorRenderers.AddRange(child.GetComponentsInChildren<Renderer>(true));

                for (int i = 0; i < hiddenEnvironmentParts.Length; i++)
                {
                    if (!child.name.StartsWith(hiddenEnvironmentParts[i]))
                        continue;

                    foreach (Renderer r in child.GetComponentsInChildren<Renderer>(true))
                        r.enabled = false;
                    break;
                }
            }
        }

        private void Layout(Camera cam)
        {
            RectTransform root = (RectTransform)transform;
            float width = root.rect.width;
            float height = root.rect.height;
            if (width <= 0f || height <= 0f)
                return;

            float scale = width / artPixelSize.x; // always cover the full width

            float conveyorTop = GetConveyorTopFromCanvasTop(cam, height);
            if (conveyorTop > 0f)
                scale = Mathf.Max(scale, conveyorTop / (countertopRow * artPixelSize.y));

            float artWidth = artPixelSize.x * scale;
            float kitchenHeight = artPixelSize.y * floorStartRow * scale;
            float floorHeight = Mathf.Max(artPixelSize.y * (1f - floorStartRow) * scale, height - kitchenHeight);

            SetRect(artwork, 0f, new Vector2(artWidth, kitchenHeight + floorHeight));

            if (kitchenPart != null)
            {
                SetRect(kitchenPart.rectTransform, 0f, new Vector2(artWidth, kitchenHeight));
                kitchenPart.uvRect = new Rect(0f, 1f - floorStartRow, 1f, floorStartRow);
            }

            if (floorPart != null)
            {
                SetRect(floorPart.rectTransform, -kitchenHeight, new Vector2(artWidth, floorHeight));
                floorPart.uvRect = new Rect(0f, 0f, 1f, 1f - floorStartRow);
            }
        }

        private static void SetRect(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }

        // Distance from the top of the canvas to the highest point of the 3D conveyor.
        private float GetConveyorTopFromCanvasTop(Camera cam, float canvasHeight)
        {
            if (conveyorRenderers.Count == 0)
                return -1f;

            float maxY = float.MinValue;
            foreach (Renderer r in conveyorRenderers)
            {
                if (r == null || !r.enabled)
                    continue;

                Bounds b = r.bounds;
                Vector3 min = b.min, max = b.max;
                corners[0] = new Vector3(min.x, min.y, min.z);
                corners[1] = new Vector3(max.x, min.y, min.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(max.x, max.y, min.z);
                corners[4] = new Vector3(min.x, min.y, max.z);
                corners[5] = new Vector3(max.x, min.y, max.z);
                corners[6] = new Vector3(min.x, max.y, max.z);
                corners[7] = new Vector3(max.x, max.y, max.z);

                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 screen = cam.WorldToScreenPoint(corners[i]);
                    if (screen.z > 0f && screen.y > maxY)
                        maxY = screen.y;
                }
            }

            if (maxY == float.MinValue || cam.pixelHeight <= 0)
                return -1f;

            float fromTop01 = 1f - Mathf.Clamp01(maxY / cam.pixelHeight);
            return fromTop01 * canvasHeight;
        }

#if UNITY_EDITOR
        public void EditorConfigure(RectTransform art, RawImage kitchen, RawImage floor, Vector2 pixelSize)
        {
            artwork = art;
            kitchenPart = kitchen;
            floorPart = floor;
            artPixelSize = pixelSize;
        }
#endif
    }
}
