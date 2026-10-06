using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Draws a low-poly 3D boat model inside the World Map UI (the map is a Screen Space Overlay
    /// canvas, where normal 3D renderers can't appear). The model is turned, rolled and lit in 3D
    /// every time its pose changes, with a foam wake on the water and funnel smoke, all in one
    /// UI draw. Models are OBJ-format text files (Resources/WorldMapSea); rename one to .obj to
    /// open it in Blender. <see cref="WorldMapAmbientProp"/> drives the pose while playing.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorldMapBoat3D : MaskableGraphic
    {
        [SerializeField] private TextAsset model;
        [Tooltip("boat_fx.png: white block, smoke puff and the wake.")]
        [SerializeField] private Texture2D effects;

        [Header("View")]
        [Tooltip("0 = bow to the right, 180 = bow to the left, 90 = bow toward the camera.")]
        [SerializeField, Range(0f, 360f)] private float yaw;
        [Tooltip("How steeply we look down at the boat (degrees above the water).")]
        [SerializeField, Range(10f, 80f)] private float viewAngle = 42f;
        [Tooltip("One model unit as a fraction of this object's width (models are about 2 units long).")]
        [SerializeField, Range(0.05f, 1f)] private float size = 0.36f;
        [Tooltip("Water level, as a fraction of the height from the centre.")]
        [SerializeField, Range(-0.5f, 0.5f)] private float waterline = -0.12f;

        [Header("Outline")]
        [Tooltip("Cartoon ink line around the boat, in model units (0 = none).")]
        [SerializeField, Range(0f, 0.1f)] private float outline = 0.035f;
        [SerializeField] private Color32 outlineColor = new Color32(38, 34, 52, 255);

        [Header("Light")]
        [SerializeField] private Vector3 lightDirection = new Vector3(-0.45f, 0.75f, 0.5f);
        [SerializeField, Range(0f, 1f)] private float ambient = 0.58f;

        [Header("Wake and Smoke")]
        [SerializeField] private bool showWake = true;
        [SerializeField, Range(0f, 1f)] private float wakeAlpha = 0.85f;
        [SerializeField, Range(0, 8)] private int smokePuffs = 4;
        [SerializeField, Min(0.5f)] private float smokeSeconds = 3.2f;

        private const float Underwater = -0.035f;

        // atlas regions (boat_fx.png is 512 x 256)
        private static readonly Vector2 WhiteUV = new Vector2(8f / 512f, 1f - 8f / 256f);
        private const float WakeTop = 0.75f, PuffLeft = 448f / 512f, PuffBottom = 0.75f;
        private static readonly Vector3[] WakeQuad =
        {
            new Vector3(-2.3f, -0.02f, 0.68f), new Vector3(1.3f, -0.02f, 0.68f),
            new Vector3(1.3f, -0.02f, -0.68f), new Vector3(-2.3f, -0.02f, -0.68f),
        };
        private static readonly Vector2[] WakeUV =
        {
            new Vector2(0f, WakeTop), new Vector2(1f, WakeTop), new Vector2(1f, 0f), new Vector2(0f, 0f),
        };

        private static readonly Dictionary<TextAsset, WorldMapModelText.Model> cache = new Dictionary<TextAsset, WorldMapModelText.Model>();

        private float roll, pitch, lift;
        private float[] faceDepth;
        private int[] order;

        public float Yaw => yaw;

        public override Texture mainTexture => effects != null ? effects : s_WhiteTexture;

        /// <summary>Turns, rocks and lifts the boat (degrees, degrees, degrees, UI units).</summary>
        public void SetPose(float yawDegrees, float rollDegrees, float pitchDegrees, float liftUnits)
        {
            if (Mathf.Approximately(yaw, yawDegrees) && Mathf.Approximately(roll, rollDegrees) &&
                Mathf.Approximately(pitch, pitchDegrees) && Mathf.Approximately(lift, liftUnits))
                return;

            yaw = yawDegrees;
            roll = rollDegrees;
            pitch = pitchDegrees;
            lift = liftUnits;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (Application.isPlaying && smokePuffs > 0 && effects != null)
            {
                WorldMapModelText.Model m = Get();
                if (m != null && m.smoke.Length > 0)
                    SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            WorldMapModelText.Model m = Get();
            if (m == null)
                return;

            Rect r = GetPixelAdjustedRect();
            float s = r.width * size;
            Vector2 origin = new Vector2(r.center.x, r.center.y + waterline * r.height);
            Color32 tint = color;

            Matrix4x4 R = View(yaw, roll, pitch);

            // wake: flat on the water, follows the heading only
            if (showWake && effects != null && wakeAlpha > 0f)
            {
                Matrix4x4 Rw = View(yaw, 0f, 0f);
                Color32 c = tint;
                c.a = (byte)(c.a * wakeAlpha);
                int start = vh.currentVertCount;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = Rw.MultiplyVector(WakeQuad[i]);
                    vh.AddVert(new Vector3(origin.x + p.x * s, origin.y + p.y * s), c, WakeUV[i]);
                }
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }

            // hull: back faces culled, far to near
            int count = m.normals.Length;
            if (faceDepth == null || faceDepth.Length < count)
            {
                faceDepth = new float[count];
                order = new int[count];
            }

            Vector3 light = lightDirection.sqrMagnitude > 0f ? lightDirection.normalized : Vector3.up;
            int visible = 0;
            for (int i = 0; i < count; i++)
            {
                if (m.centres[i].y < Underwater)
                    continue;

                Vector3 n = R.MultiplyVector(m.normals[i]);
                if (n.z <= 0f && !m.twoSided[i])
                    continue;

                faceDepth[visible] = R.MultiplyVector(m.centres[i]).z;
                order[visible] = i;
                visible++;
            }
            System.Array.Sort(faceDepth, order, 0, visible);

            // ink outline: every face drawn a little larger in dark first; the boat then covers
            // all of it except a thin line round the silhouette
            if (outline > 0f)
            {
                float grow = outline * s;
                Color32 ink = outlineColor;
                ink.a = (byte)(ink.a * tint.a / 255);
                for (int k = 0; k < visible; k++)
                {
                    int i = order[k];
                    Vector2 a = Project(R, m.corners[i * 3], origin, s);
                    Vector2 b = Project(R, m.corners[i * 3 + 1], origin, s);
                    Vector2 c = Project(R, m.corners[i * 3 + 2], origin, s);
                    Vector2 mid = (a + b + c) / 3f;
                    int start = vh.currentVertCount;
                    vh.AddVert(Grow(a, mid, grow), ink, WhiteUV);
                    vh.AddVert(Grow(b, mid, grow), ink, WhiteUV);
                    vh.AddVert(Grow(c, mid, grow), ink, WhiteUV);
                    vh.AddTriangle(start, start + 1, start + 2);
                }
            }

            for (int k = 0; k < visible; k++)
            {
                int i = order[k];
                Vector3 n = R.MultiplyVector(m.normals[i]);
                if (n.z < 0f)
                    n = -n;

                float rim = 1f - n.z;
                float lit = ambient + 0.5f * Mathf.Max(0f, Vector3.Dot(n, light)) + 0.12f * rim * rim;
                if (m.twoSided[i])
                    lit = Mathf.Max(lit, 0.9f);

                Color32 b = m.colors[i];
                var c = new Color32(
                    (byte)Mathf.Min(255f, b.r * lit * tint.r / 255f),
                    (byte)Mathf.Min(255f, b.g * lit * tint.g / 255f),
                    (byte)Mathf.Min(255f, b.b * lit * tint.b / 255f),
                    tint.a);

                int start = vh.currentVertCount;
                for (int v = 0; v < 3; v++)
                {
                    Vector3 p = R.MultiplyVector(m.corners[i * 3 + v]);
                    vh.AddVert(new Vector3(origin.x + p.x * s, origin.y + p.y * s + lift), c, WhiteUV);
                }
                vh.AddTriangle(start, start + 1, start + 2);
            }

            // smoke puffs rising from the funnel
            if (effects != null && smokePuffs > 0 && m.smoke.Length > 0)
            {
                float t = Application.isPlaying ? Time.time : 0.7f;
                for (int sIndex = 0; sIndex < m.smoke.Length; sIndex++)
                {
                    Vector3 basePoint = R.MultiplyVector(m.smoke[sIndex]);
                    for (int k = 0; k < smokePuffs; k++)
                    {
                        float u = Mathf.Repeat(t / smokeSeconds + (float)k / smokePuffs, 1f);
                        float radius = (0.1f + 0.16f * u) * s;
                        var centre = new Vector2(origin.x + (basePoint.x - 0.35f * u) * s,
                                                 origin.y + (basePoint.y + 0.55f * u) * s + lift);
                        Color32 c = tint;
                        c.a = (byte)(tint.a * (1f - u) * Mathf.Min(1f, u * 6f));

                        int start = vh.currentVertCount;
                        vh.AddVert(new Vector3(centre.x - radius, centre.y - radius), c, new Vector2(PuffLeft, PuffBottom));
                        vh.AddVert(new Vector3(centre.x - radius, centre.y + radius), c, new Vector2(PuffLeft, 1f));
                        vh.AddVert(new Vector3(centre.x + radius, centre.y + radius), c, new Vector2(1f, 1f));
                        vh.AddVert(new Vector3(centre.x + radius, centre.y - radius), c, new Vector2(1f, PuffBottom));
                        vh.AddTriangle(start, start + 1, start + 2);
                        vh.AddTriangle(start, start + 2, start + 3);
                    }
                }
            }
        }

        private Vector2 Project(Matrix4x4 r, Vector3 p, Vector2 origin, float s)
        {
            Vector3 v = r.MultiplyVector(p);
            return new Vector2(origin.x + v.x * s, origin.y + v.y * s + lift);
        }

        private static Vector3 Grow(Vector2 p, Vector2 mid, float by)
        {
            Vector2 d = p - mid;
            float len = d.magnitude;
            return len > 1e-5f ? p + d * (by / len) : p;
        }

        // view * yaw * pitch * roll. X = bow, Y = up, Z = port; after the view, z points at the camera.
        private Matrix4x4 View(float yawDeg, float rollDeg, float pitchDeg)
        {
            Matrix4x4 ry = Matrix4x4.identity, rz = Matrix4x4.identity, rx = Matrix4x4.identity, v = Matrix4x4.identity;

            float cy = Mathf.Cos(yawDeg * Mathf.Deg2Rad), sy = Mathf.Sin(yawDeg * Mathf.Deg2Rad);
            ry.m00 = cy; ry.m02 = -sy; ry.m20 = sy; ry.m22 = cy;

            float cp = Mathf.Cos(pitchDeg * Mathf.Deg2Rad), sp = Mathf.Sin(pitchDeg * Mathf.Deg2Rad);
            rz.m00 = cp; rz.m01 = -sp; rz.m10 = sp; rz.m11 = cp;

            float cr = Mathf.Cos(rollDeg * Mathf.Deg2Rad), sr = Mathf.Sin(rollDeg * Mathf.Deg2Rad);
            rx.m11 = cr; rx.m12 = -sr; rx.m21 = sr; rx.m22 = cr;

            float ce = Mathf.Cos(viewAngle * Mathf.Deg2Rad), se = Mathf.Sin(viewAngle * Mathf.Deg2Rad);
            v.m11 = ce; v.m12 = -se; v.m21 = se; v.m22 = ce;

            return v * ry * rz * rx;
        }

        private WorldMapModelText.Model Get()
        {
            if (model == null)
                return null;
            if (!cache.TryGetValue(model, out WorldMapModelText.Model m))
            {
                m = WorldMapModelText.Parse(model.text);
                cache[model] = m;
            }
            return m;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (model != null)
                cache.Remove(model);
            base.OnValidate();
        }
#endif
    }
}
