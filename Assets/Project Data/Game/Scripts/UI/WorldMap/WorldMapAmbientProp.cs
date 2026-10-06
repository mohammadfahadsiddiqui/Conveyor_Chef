using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Gentle life for a World Map prop: boats sail back and forth and rock on the waves,
    /// whales surface and dive, clouds drift across the map. Runs in Play mode only, so the
    /// position, size and mirroring saved in the scene stay exactly as placed in the Hierarchy.
    /// Mirror a boat (Scale X = -1) to make it start sailing left.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapAmbientProp : MonoBehaviour
    {
        public enum Motion
        {
            Sail = 0,       // travels along Travel, turns around at each end, rocks on the waves
            Float = 1,      // stays in place, bobs and rocks
            Surface = 2,    // whale: rises, floats a while, dives, stays under, repeats
            Drift = 3,      // cloud: slides across the map and wraps around at the edge
        }

        [SerializeField] private Motion motion = Motion.Sail;

        [Header("Sail")]
        [Tooltip("How far the boat sails each way from where it is placed (map units).")]
        [SerializeField] private Vector2 travel = new Vector2(120f, 0f);
        [Tooltip("Seconds for a full trip there and back.")]
        [SerializeField, Min(2f)] private float tripSeconds = 20f;

        [Header("Waves")]
        [SerializeField, Min(0f)] private float bobHeight = 6f;
        [SerializeField, Min(0.5f)] private float bobSeconds = 2.8f;
        [SerializeField, Range(0f, 15f)] private float rockDegrees = 4f;

        [Header("Surface")]
        [Tooltip("Seconds for one rise, float, dive and rest cycle.")]
        [SerializeField, Min(3f)] private float surfaceCycleSeconds = 11f;
        [SerializeField, Min(0f)] private float diveDepth = 22f;

        [Header("Drift")]
        [SerializeField] private float driftSpeed = 14f;
        [SerializeField, Range(0f, 1f)] private float driftAlpha = 0.92f;

        private const float Tau = Mathf.PI * 2f;

        private RectTransform rect;
        private RectTransform area;
        private CanvasRenderer canvasRenderer;
        private Vector2 home;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float phase;

        private void Awake()
        {
            rect = (RectTransform)transform;
            area = rect.parent as RectTransform;
            canvasRenderer = GetComponent<CanvasRenderer>();
            home = rect.anchoredPosition;
            baseScale = rect.localScale;
            baseRotation = rect.localRotation;

            // Different start points so props never move in step; stable per position.
            phase = Mathf.Repeat(home.x * 0.0137f + home.y * 0.0091f, 1f);

            if (motion == Motion.Sail && baseScale.x < 0f)
                phase += 0.5f;   // mirrored boats start heading left
        }

        private void OnEnable()
        {
            if (motion == Motion.Drift && canvasRenderer != null)
                canvasRenderer.SetAlpha(driftAlpha);
        }

        private void OnDisable()
        {
            if (rect == null)
                return;

            rect.anchoredPosition = home;
            rect.localScale = baseScale;
            rect.localRotation = baseRotation;
            if (canvasRenderer != null)
                canvasRenderer.SetAlpha(1f);
        }

        private void Update()
        {
            float t = Time.time;
            switch (motion)
            {
                case Motion.Sail: Sail(t); break;
                case Motion.Float: Float(t, home); break;
                case Motion.Surface: Surface(t); break;
                case Motion.Drift: Drift(t); break;
            }
        }

        private void Sail(float t)
        {
            float a = (t / tripSeconds + phase) * Tau;
            float heading = Mathf.Cos(a);    // > 0 sailing toward +Travel

            // Face the way it is going; the quick squash at each end reads as turning around.
            float face = Mathf.Clamp(heading * 10f, -1f, 1f);
            float dir = travel.x >= 0f ? 1f : -1f;
            rect.localScale = new Vector3(Mathf.Abs(baseScale.x) * face * dir, baseScale.y, baseScale.z);

            Float(t, home + travel * Mathf.Sin(a));
        }

        private void Float(float t, Vector2 centre)
        {
            float b = (t / bobSeconds + phase) * Tau;
            rect.anchoredPosition = centre + new Vector2(0f, bobHeight * Mathf.Sin(b));
            rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rockDegrees * Mathf.Sin(b + 1.1f));
        }

        private void Surface(float t)
        {
            float u = Mathf.Repeat(t / surfaceCycleSeconds + phase, 1f);

            // 0-.12 rise, .12-.68 float, .68-.82 dive, .82-1 under water
            float up;
            if (u < 0.12f) up = Smooth(u / 0.12f);
            else if (u < 0.68f) up = 1f;
            else if (u < 0.82f) up = 1f - Smooth((u - 0.68f) / 0.14f);
            else up = 0f;

            Float(t, home + new Vector2(0f, -diveDepth * (1f - up)));
            if (canvasRenderer != null)
                canvasRenderer.SetAlpha(up);
        }

        private void Drift(float t)
        {
            float x = home.x + driftSpeed * t + phase * 600f;
            if (area != null)
            {
                float half = area.rect.width * 0.5f + rect.rect.width * 0.5f * Mathf.Abs(baseScale.x);
                x = Mathf.Repeat(x + half, half * 2f) - half;
            }

            float sway = Mathf.Sin((t / 23f + phase) * Tau);
            rect.anchoredPosition = new Vector2(x, home.y + 12f * sway);
            rect.localScale = baseScale * (1f + 0.025f * sway);
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
