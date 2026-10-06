using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Gentle life for a World Map prop: boats sail back and forth and rock on the waves,
    /// whales surface and dive, clouds drift across the map. Runs in Play mode only, so the
    /// position, size and mirroring saved in the scene stay exactly as placed in the Hierarchy.
    /// A 3D boat (WorldMapBoat3D) turns round in 3D; set its Yaw to 180 to make it start sailing left.
    /// For a picture boat, mirror it (Scale X = -1) instead.
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
        private WorldMapBoat3D boat3D;
        private float baseYaw;
        private Vector2 home;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float phase;

        private void Awake()
        {
            rect = (RectTransform)transform;
            area = rect.parent as RectTransform;
            canvasRenderer = GetComponent<CanvasRenderer>();
            boat3D = GetComponent<WorldMapBoat3D>();
            baseYaw = boat3D != null ? boat3D.Yaw : 0f;
            home = rect.anchoredPosition;
            baseScale = rect.localScale;
            baseRotation = rect.localRotation;

            // Different start points so props never move in step; stable per position.
            phase = Mathf.Repeat(home.x * 0.0137f + home.y * 0.0091f, 1f);

            bool facingLeft = boat3D != null ? Mathf.Cos(baseYaw * Mathf.Deg2Rad) < 0f : baseScale.x < 0f;
            if (motion == Motion.Sail && facingLeft)
                phase += 0.5f;   // boats facing left start heading left

            SeaPosition = home;
            PoseYaw = StartYaw;
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
            if (boat3D != null)
                boat3D.SetPose(baseYaw, 0f, 0f, 0f);
            if (canvasRenderer != null)
                canvasRenderer.SetAlpha(1f);
        }

        // ---- pose, readable by the 3D sea (WorldMapSea3D) ----
        public Motion PropMotion => motion;
        /// <summary>Where the prop is on the map (map units, before bobbing).</summary>
        public Vector2 SeaPosition { get; private set; }
        /// <summary>0 = bow right, 180 = bow left, 90 = bow toward the camera.</summary>
        public float PoseYaw { get; private set; }
        public float PoseRoll { get; private set; }
        public float PosePitch { get; private set; }
        public float PoseLift { get; private set; }
        /// <summary>1 = at the surface, 0 = fully dived (whales).</summary>
        public float Surfacing { get; private set; } = 1f;
        public float StartYaw => boat3D != null ? baseYaw : (baseScale.x < 0f ? 180f : 0f);

        private void Update()
        {
            float t = Time.time;
            switch (motion)
            {
                case Motion.Sail: Sail(t); break;
                case Motion.Float: Float(t, home, StartYaw); break;
                case Motion.Surface: Surface(t); break;
                case Motion.Drift: Drift(t); break;
            }
        }

        private void Sail(float t)
        {
            float a = (t / tripSeconds + phase) * Tau;
            float heading = Mathf.Cos(a);    // > 0 sailing toward +Travel
            float dir = travel.x >= 0f ? 1f : -1f;

            // a real 3D turn: the bow swings round toward the camera at each end of the trip
            float turn = Mathf.Clamp(heading * dir * 3f, -1f, 1f);
            Vector2 at = home + travel * Mathf.Sin(a);

            if (boat3D == null)
            {
                // picture boat: face the way it is going; the quick squash reads as turning around
                float face = Mathf.Clamp(heading * 10f, -1f, 1f);
                rect.localScale = new Vector3(Mathf.Abs(baseScale.x) * face * dir, baseScale.y, baseScale.z);
            }
            Float(t, at, 90f - 90f * turn);
        }

        private void Float(float t, Vector2 centre, float yawDegrees)
        {
            float b = (t / bobSeconds + phase) * Tau;
            SeaPosition = centre;
            PoseYaw = yawDegrees;
            PoseRoll = rockDegrees * Mathf.Sin(b + 1.1f);
            PosePitch = 0.6f * rockDegrees * Mathf.Sin(b * 0.7f + 0.4f);
            PoseLift = bobHeight * Mathf.Sin(b);

            if (boat3D != null)
            {
                rect.anchoredPosition = centre;
                boat3D.SetPose(PoseYaw, PoseRoll, PosePitch, PoseLift);
                return;
            }

            rect.anchoredPosition = centre + new Vector2(0f, PoseLift);
            rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, PoseRoll);
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

            Float(t, home + new Vector2(0f, -diveDepth * (1f - up)), StartYaw);
            SeaPosition = home;
            Surfacing = up;
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
            SeaPosition = rect.anchoredPosition;
            rect.localScale = baseScale * (1f + 0.025f * sway);
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
