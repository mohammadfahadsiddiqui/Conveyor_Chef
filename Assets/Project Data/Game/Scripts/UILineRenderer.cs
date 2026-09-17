using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Watermelon.BusStop
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : Graphic
    {
        [SerializeField] private float lineThickness = 10f;
        [SerializeField] private bool useMargins;
        [SerializeField] private Vector2 margin;

        private Vector2[] points;
        private List<Vector2> drawingPoints = new List<Vector2>();

        public float LineThickness
        {
            get { return lineThickness; }
            set { lineThickness = value; SetVerticesDirty(); }
        }

        public void SetPoints(Vector2[] newPoints)
        {
            if (newPoints == null || newPoints.Length < 2)
            {
                points = null;
                SetVerticesDirty();
                return;
            }

            points = newPoints;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (points == null || points.Length < 2)
                return;

            drawingPoints.Clear();

            // Add all points
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 point = points[i];
                if (useMargins)
                {
                    point += margin;
                }
                drawingPoints.Add(point);
            }

            // Draw the line segments
            for (int i = 0; i < drawingPoints.Count - 1; i++)
            {
                Vector2 point1 = drawingPoints[i];
                Vector2 point2 = drawingPoints[i + 1];

                // Calculate perpendicular direction
                Vector2 direction = (point2 - point1).normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x) * lineThickness * 0.5f;

                // Create quad vertices
                int vertexIndex = vh.currentVertCount;

                vh.AddVert(point1 - perpendicular, color, Vector2.zero);
                vh.AddVert(point1 + perpendicular, color, Vector2.zero);
                vh.AddVert(point2 + perpendicular, color, Vector2.zero);
                vh.AddVert(point2 - perpendicular, color, Vector2.zero);

                // Add triangles
                vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
                vh.AddTriangle(vertexIndex + 2, vertexIndex + 3, vertexIndex);
            }
        }
    }
}