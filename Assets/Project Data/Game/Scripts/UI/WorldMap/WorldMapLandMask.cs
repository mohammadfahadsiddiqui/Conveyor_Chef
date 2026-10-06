using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// A small land/sea grid for one continent picture, so World Map sea effects (wave crests,
    /// sparkles) stay on the water. The grid is in the picture's own space, so it follows the
    /// continent when you move or resize it in the Hierarchy. After changing the picture, use
    /// the component menu "Bake From Sprite".
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapLandMask : MonoBehaviour
    {
        [SerializeField, HideInInspector] private int size = 48;
        [Tooltip("size x size cells, 4 per hex digit, rows from the bottom. Written by Bake From Sprite.")]
        [SerializeField, TextArea(2, 4)] private string cells = "";

        private RectTransform rect;
        private bool[] land;

        private void Awake()
        {
            Decode();
        }

        private void Decode()
        {
            rect = (RectTransform)transform;
            land = new bool[size * size];
            for (int i = 0; i < cells.Length && i * 4 < land.Length; i++)
            {
                int v = System.Convert.ToInt32(cells[i].ToString(), 16);
                for (int b = 0; b < 4 && i * 4 + b < land.Length; b++)
                    land[i * 4 + b] = (v & (8 >> b)) != 0;
            }
        }

        /// <summary>True when the world-space point lies on this continent's land.</summary>
        public bool IsLand(Vector3 worldPoint)
        {
            if (land == null)
                Decode();
            if (!isActiveAndEnabled)
                return false;

            Rect r = DrawnRect();
            Vector2 p = rect.InverseTransformPoint(worldPoint);
            float u = (p.x - r.xMin) / r.width;
            float v = (p.y - r.yMin) / r.height;
            if (u < 0f || v < 0f || u >= 1f || v >= 1f)
                return false;

            return land[(int)(v * size) * size + (int)(u * size)];
        }

        // The area the picture really covers (Preserve Aspect letterboxes it inside the rect).
        private Rect DrawnRect()
        {
            Rect r = rect.rect;
            Image image = GetComponent<Image>();
            if (image == null || !image.preserveAspect || image.sprite == null)
                return r;

            float aspect = image.sprite.rect.width / image.sprite.rect.height;
            if (r.width / r.height > aspect)
            {
                float w = r.height * aspect;
                return new Rect(r.center.x - w * 0.5f, r.yMin, w, r.height);
            }

            float h = r.width / aspect;
            return new Rect(r.xMin, r.center.y - h * 0.5f, r.width, h);
        }

#if UNITY_EDITOR
        [ContextMenu("Bake From Sprite")]
        private void BakeFromSprite()
        {
            Image image = GetComponent<Image>();
            if (image == null || image.sprite == null)
                return;

            string path = UnityEditor.AssetDatabase.GetAssetPath(image.sprite.texture);
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(System.IO.File.ReadAllBytes(path)))
                return;

            Rect sr = image.sprite.rect;
            var sb = new System.Text.StringBuilder();
            int nibble = 0, bits = 0;
            for (int i = 0; i < size * size; i++)
            {
                int x = i % size, y = i / size;
                int hits = 0;
                // land = opaque and not the light-blue shallow water drawn around the coast
                for (int sy = 0; sy < 3; sy++)
                for (int sx = 0; sx < 3; sx++)
                {
                    Color c = tex.GetPixel((int)(sr.x + (x + (sx + 0.5f) / 3f) / size * sr.width),
                                           (int)(sr.y + (y + (sy + 0.5f) / 3f) / size * sr.height));
                    bool water = c.b > c.r + 0.2f && c.b >= c.g - 0.06f;
                    if (c.a > 0.63f && !water)
                        hits++;
                }

                nibble = (nibble << 1) | (hits >= 5 ? 1 : 0);
                if (++bits == 4)
                {
                    sb.Append(nibble.ToString("x"));
                    nibble = bits = 0;
                }
            }
            if (bits > 0)
                sb.Append((nibble << (4 - bits)).ToString("x"));

            DestroyImmediate(tex);
            UnityEditor.Undo.RecordObject(this, "Bake Land Mask");
            cells = sb.ToString();
            land = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
