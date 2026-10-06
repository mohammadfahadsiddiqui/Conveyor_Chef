using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Little white wave crests and sparkles that rise and fade on the open sea, always around the
    /// part of the map the player is looking at. Uses a small fixed pool (no allocations while
    /// playing) and skips land using each continent's <see cref="WorldMapLandMask"/>.
    /// Runs in Play mode only; nothing is saved into the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapWaveCrests : MonoBehaviour
    {
        [SerializeField] private Sprite crestSprite;
        [SerializeField] private Sprite sparkleSprite;
        [SerializeField, Range(0, 40)] private int count = 18;
        [SerializeField, Range(0f, 1f)] private float sparkleShare = 0.3f;

        [Header("Crests")]
        [SerializeField] private Vector2 crestWidth = new Vector2(70f, 120f);
        [SerializeField] private Vector2 crestSeconds = new Vector2(2.4f, 4f);
        [SerializeField, Range(0f, 1f)] private float crestAlpha = 0.7f;
        [SerializeField] private float crestTravel = 24f;

        [Header("Sparkles")]
        [SerializeField] private Vector2 sparkleSize = new Vector2(26f, 46f);
        [SerializeField] private Vector2 sparkleSeconds = new Vector2(1f, 1.8f);
        [SerializeField, Range(0f, 1f)] private float sparkleAlpha = 0.95f;

        private struct Fx
        {
            public RectTransform rect;
            public CanvasRenderer renderer;
            public bool sparkle;
            public Vector2 start;
            public float size;
            public float born;
            public float life;
        }

        private readonly List<WorldMapLandMask> masks = new List<WorldMapLandMask>();
        private readonly Vector3[] corners = new Vector3[4];
        private Fx[] pool;
        private RectTransform area;
        private RectTransform viewport;
        private float crestAspect = 0.4f;

        private void Start()
        {
            area = (RectTransform)transform;
            ScrollRect scroll = GetComponentInParent<ScrollRect>();
            viewport = scroll != null && scroll.viewport != null ? scroll.viewport : null;
            if (transform.parent != null)
                transform.parent.GetComponentsInChildren(true, masks);

            if (crestSprite != null)
                crestAspect = crestSprite.rect.height / crestSprite.rect.width;

            pool = new Fx[count];
            for (int i = 0; i < pool.Length; i++)
            {
                bool sparkle = sparkleSprite != null && (crestSprite == null || i < Mathf.RoundToInt(count * sparkleShare));
                Sprite sprite = sparkle ? sparkleSprite : crestSprite;
                if (sprite == null)
                    continue;

                var go = new GameObject(sparkle ? "Sparkle" : "Wave Crest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.maskable = true;

                var cr = go.GetComponent<CanvasRenderer>();
                cr.cullTransparentMesh = true;   // faded-out ones cost nothing
                pool[i] = new Fx { rect = rt, renderer = cr, sparkle = sparkle };
                // stagger the first wave so they don't all appear together
                Respawn(ref pool[i], Time.time - Random.value * 3f);
            }
        }

        private void Update()
        {
            if (pool == null)
                return;

            float now = Time.time;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].rect == null)
                    continue;

                float u = (now - pool[i].born) / pool[i].life;
                if (u >= 1f)
                {
                    Respawn(ref pool[i], now);
                    u = 0f;
                }
                Animate(ref pool[i], Mathf.Max(0f, u));
            }
        }

        private void Animate(ref Fx fx, float u)
        {
            float fade = Mathf.Sin(u * Mathf.PI);
            if (fx.sparkle)
            {
                float s = fx.size * (0.4f + 0.6f * fade);
                fx.rect.sizeDelta = new Vector2(s, s);
                fx.rect.localRotation = Quaternion.Euler(0f, 0f, 45f * u);
                fx.renderer.SetAlpha(sparkleAlpha * fade * fade);
            }
            else
            {
                fx.rect.anchoredPosition = fx.start + new Vector2(crestTravel * u, 3f * fade);
                fx.rect.localScale = new Vector3(0.75f + 0.35f * u, 0.85f + 0.15f * fade, 1f);
                fx.renderer.SetAlpha(crestAlpha * fade);
            }
        }

        private void Respawn(ref Fx fx, float now)
        {
            fx.born = now;
            fx.life = fx.sparkle ? Random.Range(sparkleSeconds.x, sparkleSeconds.y) : Random.Range(crestSeconds.x, crestSeconds.y);
            fx.size = fx.sparkle ? Random.Range(sparkleSize.x, sparkleSize.y) : Random.Range(crestWidth.x, crestWidth.y);

            Rect view = VisibleArea();
            float half = fx.size * 0.5f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var p = new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax));
                if (OnLand(p) || OnLand(p + new Vector2(-half, 0f)) || OnLand(p + new Vector2(half + crestTravel, 0f)))
                    continue;

                fx.start = p;
                fx.rect.anchoredPosition = p;
                if (!fx.sparkle)
                {
                    fx.rect.sizeDelta = new Vector2(fx.size, fx.size * crestAspect);
                    fx.rect.localRotation = Quaternion.identity;
                }
                return;
            }

            // nowhere free this time (looking at mostly land): try again shortly
            fx.renderer.SetAlpha(0f);
            fx.life = 0.4f;
            fx.start = new Vector2(area.rect.xMax + 1000f, 0f);
            fx.rect.anchoredPosition = fx.start;
        }

        private bool OnLand(Vector2 local)
        {
            Vector3 world = area.TransformPoint(local);
            for (int i = 0; i < masks.Count; i++)
            {
                if (masks[i] != null && masks[i].IsLand(world))
                    return true;
            }
            return false;
        }

        // The part of the map on screen (a little larger), in this object's local space.
        private Rect VisibleArea()
        {
            Rect full = area.rect;
            if (viewport == null)
                return full;

            viewport.GetWorldCorners(corners);
            Vector2 a = area.InverseTransformPoint(corners[0]);
            Vector2 b = area.InverseTransformPoint(corners[2]);
            Rect view = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - 80f, Mathf.Min(a.y, b.y) - 80f,
                                        Mathf.Max(a.x, b.x) + 80f, Mathf.Max(a.y, b.y) + 80f);
            return Rect.MinMaxRect(Mathf.Max(view.xMin, full.xMin), Mathf.Max(view.yMin, full.yMin),
                                   Mathf.Min(view.xMax, full.xMax), Mathf.Min(view.yMax, full.yMax));
        }
    }
}
