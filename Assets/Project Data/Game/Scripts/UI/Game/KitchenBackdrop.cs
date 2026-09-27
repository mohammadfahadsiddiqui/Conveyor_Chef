using System.Collections.Generic;
using UnityEngine;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// Painted kitchen behind the 3D level (GAME BACKGROUND CANVAS [NEW UI]).
    ///
    /// The canvas is a Screen Space - Camera canvas placed near the camera's far plane,
    /// so the 3D conveyor, trays, waiting slots, donuts and floor always draw in front
    /// of it. In Play mode the renderers of the 3D kitchen room and props are switched
    /// off (colliders and GameObjects stay as they are), and the painting is scaled so
    /// its countertop edge meets the top of the 3D conveyor on any screen shape.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public sealed class KitchenBackdrop : MonoBehaviour
    {
        [SerializeField] RectTransform artwork;
        [Tooltip("Row of the countertop front edge in the artwork, from the top, as a fraction of its height.")]
        [SerializeField] float countertopRow = 450f / 1672f;
        [Tooltip("Direct children of the level environment whose renderers are hidden (name prefixes).")]
        [SerializeField] string[] hiddenEnvironmentParts = { "FreeRoom", "_ObjectsWrapper", "Wall", "Kitchen", "Back" };
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

            Vector2 artSize = GetArtSize();
            float scale = width / artSize.x; // always cover the full width

            float conveyorTop = GetConveyorTopFromCanvasTop(cam, height);
            if (conveyorTop > 0f)
                scale = Mathf.Max(scale, conveyorTop / (countertopRow * artSize.y));

            artwork.anchorMin = new Vector2(0.5f, 1f);
            artwork.anchorMax = new Vector2(0.5f, 1f);
            artwork.pivot = new Vector2(0.5f, 1f);
            artwork.anchoredPosition = Vector2.zero;
            artwork.sizeDelta = artSize * scale;
        }

        private Vector2 GetArtSize()
        {
            UnityEngine.UI.Image image = artwork.GetComponent<UnityEngine.UI.Image>();
            if (image != null && image.sprite != null)
                return image.sprite.rect.size;

            return new Vector2(941f, 1672f);
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
        public void EditorConfigure(RectTransform art)
        {
            artwork = art;
        }
#endif
    }
}
