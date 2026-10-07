using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Sea life for the 3D sea: whales that swim, surface and spout; dolphin pods leaping with
    /// splashes; seagulls circling with flapping wings; floating cargo. They live around the part
    /// of the map on screen, stay in open water (continent land masks), and are moved to fresh
    /// water near the view when they drift off-screen, so every part of the map has some life.
    /// </summary>
    public sealed partial class WorldMapSea3D
    {
        [Header("Sea Life (around the view)")]
        [SerializeField, Range(0, 8)] private int roamingWhales = 4;
        [SerializeField, Range(0, 6)] private int dolphinPods = 2;
        [SerializeField, Range(1, 5)] private int dolphinsPerPod = 3;
        [SerializeField, Range(0, 10)] private int seagulls = 4;
        [SerializeField, Range(0, 8)] private int floatingCargo = 3;
        [SerializeField, Range(0.5f, 2f)] private float lifeSize = 1f;
        [SerializeField] private TextAsset dolphinModel;
        [SerializeField] private TextAsset gullBodyModel;
        [SerializeField] private TextAsset gullWingModel;
        [SerializeField] private TextAsset crateModel;
        [SerializeField] private TextAsset barrelModel;

        private enum Kind { Whale, Dolphin, Gull, Cargo }

        private sealed class Pod
        {
            public Vector2 map;
            public float heading;
            public float target;
            public float phase;
        }

        private sealed class Critter
        {
            public Kind kind;
            public Transform body, wingLeft, wingRight;
            public WorldMapModelText.Model model;
            public Vector2 map;              // where it is on the map (map units)
            public float heading, target;    // yaw, degrees
            public float scale, phase, speed;
            public Vector2 centre;           // gulls: circle centre
            public float radius, spin;       // gulls: circle radius, turn direction
            public Pod pod;
            public int slot;
            public float visible;            // 0..1 (whales surfacing)
        }

        private readonly List<Critter> life = new List<Critter>();
        private readonly List<WorldMapLandMask> masks = new List<WorldMapLandMask>();
        private Rect viewMap;

        private void LoadLifeDefaults()
        {
            if (dolphinModel == null) dolphinModel = Resources.Load<TextAsset>(ResourceFolder + "dolphin");
            if (gullBodyModel == null) gullBodyModel = Resources.Load<TextAsset>(ResourceFolder + "gull_body");
            if (gullWingModel == null) gullWingModel = Resources.Load<TextAsset>(ResourceFolder + "gull_wing");
            if (crateModel == null) crateModel = Resources.Load<TextAsset>(ResourceFolder + "crate");
            if (barrelModel == null) barrelModel = Resources.Load<TextAsset>(ResourceFolder + "barrel");
        }

        private void BuildLife()
        {
            LoadLifeDefaults();
            if (mapProps.parent != null)
                mapProps.parent.GetComponentsInChildren(true, masks);
            SyncCamera();                       // viewMap for the first placement

            for (int i = 0; i < roamingWhales && whaleModel != null; i++)
                Spawn(NewCritter(Kind.Whale, whaleModel, 70f), true);

            for (int p = 0; p < dolphinPods && dolphinModel != null; p++)
            {
                var pod = new Pod { phase = Random.value };
                PlacePod(pod, true);
                for (int i = 0; i < dolphinsPerPod; i++)
                {
                    Critter d = NewCritter(Kind.Dolphin, dolphinModel, 46f);
                    d.pod = pod;
                    d.slot = i;
                    d.phase = pod.phase + i * 0.29f;
                }
            }

            for (int i = 0; i < seagulls && gullBodyModel != null; i++)
            {
                Critter g = NewCritter(Kind.Gull, gullBodyModel, 40f);
                if (gullWingModel != null)
                {
                    g.wingLeft = Part(g.body, gullWingModel, "Wing L", false);
                    g.wingRight = Part(g.body, gullWingModel, "Wing R", true);
                }
                Spawn(g, true);
            }

            for (int i = 0; i < floatingCargo; i++)
            {
                TextAsset text = i % 2 == 0 ? crateModel : barrelModel;
                if (text != null)
                    Spawn(NewCritter(Kind.Cargo, text, i % 2 == 0 ? 60f : 52f), true);
            }
        }

        private Critter NewCritter(Kind kind, TextAsset text, float size)
        {
            var c = new Critter
            {
                kind = kind,
                body = MakeRenderer(world, text, kind + " (sea life)", false),
                model = models[text],
                scale = size * lifeSize * Random.Range(0.9f, 1.1f),
                phase = Random.value,
                heading = Random.Range(0f, 360f),
            };
            c.target = c.heading;
            life.Add(c);
            return c;
        }

        private Transform Part(Transform parent, TextAsset text, string name, bool mirror)
        {
            Transform t = MakeRenderer(parent, text, name, mirror);
            t.localPosition = Vector3.zero;
            return t;
        }

        // ---------------------------------------------------------------- placement

        private bool IsSea(Vector2 map, float radius)
        {
            Rect r = mapProps.rect;
            if (map.x < r.xMin + radius || map.x > r.xMax - radius || map.y < r.yMin + radius || map.y > r.yMax - radius)
                return false;
            for (int k = 0; k < 5; k++)
            {
                Vector2 p = k == 0 ? map : map + radius * new Vector2(Mathf.Cos(k * 1.571f), Mathf.Sin(k * 1.571f));
                Vector3 world = mapProps.TransformPoint(p);
                for (int i = 0; i < masks.Count; i++)
                {
                    if (masks[i] != null && masks[i].IsLand(world))
                        return false;
                }
            }
            return true;
        }

        // A random open-water point near the view; off-screen when 'outside' (so it drifts in).
        private bool RandomSea(float radius, bool outside, out Vector2 map)
        {
            Rect area = Grow(viewMap, 450f);
            for (int attempt = 0; attempt < 24; attempt++)
            {
                map = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                if (outside && viewMap.Contains(map))
                    continue;
                if (IsSea(map, radius))
                    return true;
            }
            map = Vector2.zero;
            return false;
        }

        private static Rect Grow(Rect r, float by)
        {
            return Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);
        }

        private void Spawn(Critter c, bool first)
        {
            float radius = c.kind == Kind.Gull ? 180f : c.scale * 1.4f;
            bool outside = !first && (c.kind == Kind.Gull || c.kind == Kind.Cargo);
            if (!RandomSea(radius, outside, out Vector2 map))
            {
                c.body.gameObject.SetActive(false);
                c.map = c.centre = new Vector2(float.MaxValue, float.MaxValue);
                return;
            }

            c.map = map;
            c.heading = c.target = Random.Range(0f, 360f);
            if (c.kind == Kind.Gull)
            {
                c.centre = map;
                c.radius = Random.Range(110f, 190f);
                c.spin = Random.value < 0.5f ? 1f : -1f;
            }
            if (c.kind == Kind.Whale)
                c.phase = first ? Random.value : Mathf.Repeat(0.86f - Time.time / 14f, 1f);   // re-spawned whales start under water
        }

        private void PlacePod(Pod pod, bool first)
        {
            if (RandomSea(160f, !first, out Vector2 map))
                pod.map = map;
            else
                pod.map = new Vector2(float.MaxValue, float.MaxValue);
            pod.heading = pod.target = Random.Range(0f, 360f);
        }

        // ---------------------------------------------------------------- every frame

        private void UpdateLife(float t, float dt, Vector3 sun)
        {
            Rect keep = Grow(viewMap, 700f);

            // pods swim together
            foreach (Critter c in life)
            {
                if (c.kind == Kind.Dolphin && c.slot == 0 && c.pod != null &&
                    !Steer(ref c.pod.map, ref c.pod.heading, ref c.pod.target, 60f, 160f, dt, keep))
                    PlacePod(c.pod, false);
            }

            foreach (Critter c in life)
            {
                switch (c.kind)
                {
                    case Kind.Whale: UpdateWhale(c, t, dt, keep, sun); break;
                    case Kind.Dolphin: UpdateDolphin(c, t, sun); break;
                    case Kind.Gull: UpdateGull(c, t, dt, keep, sun); break;
                    case Kind.Cargo: UpdateCargo(c, t, dt, keep, sun); break;
                }
            }
        }

        // Swim forward and turn away from land. False when it has left the area near the view.
        private bool Steer(ref Vector2 map, ref float heading, ref float target, float speed, float clearance,
                           float dt, Rect keep)
        {
            if (!keep.Contains(map))
                return false;

            Vector2 dir = MapDirection(heading);
            if (!IsSea(map + dir * (clearance + 80f), clearance * 0.5f))
            {
                if (Mathf.Abs(Mathf.DeltaAngle(heading, target)) < 5f)
                    target = heading + (Random.value < 0.5f ? 140f : -140f);
            }
            else if (Random.value < dt * 0.05f)
            {
                target = heading + Random.Range(-50f, 50f);      // wander
            }

            heading = Mathf.MoveTowardsAngle(heading, target, 28f * dt);
            Vector2 next = map + MapDirection(heading) * speed * dt;
            if (IsSea(next, clearance * 0.4f))
                map = next;
            return true;
        }

        // A heading (yaw) as a direction on the map: the sea's depth axis is foreshortened on screen.
        private Vector2 MapDirection(float yaw)
        {
            float r = yaw * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), -Mathf.Sin(r) * sinView);
        }

        private void UpdateWhale(Critter c, float t, float dt, Rect keep, Vector3 sun)
        {
            float u = Mathf.Repeat(t / 14f + c.phase, 1f);
            float up = u < 0.12f ? Smooth01(u / 0.12f) : u < 0.7f ? 1f : u < 0.84f ? 1f - Smooth01((u - 0.7f) / 0.14f) : 0f;
            c.visible = up;

            if (up <= 0f && !Grow(viewMap, 250f).Contains(c.map))
                Spawn(c, false);                                 // only move it while it is under water
            if (!Steer(ref c.map, ref c.heading, ref c.target, 13f, c.scale * 1.5f, dt, Grow(keep, 600f)))
                Spawn(c, false);

            bool show = up > 0.001f && c.map.x < float.MaxValue;
            if (c.body.gameObject.activeSelf != show)
                c.body.gameObject.SetActive(show);
            if (!show)
                return;

            Vector3 at = ToSea(c.map, 0f);
            float water = WaterHeight(at.x, at.z, waveTime);
            float under = 1f - up;
            Pose(c.body, c.map, water - under * 0.9f * c.scale, c.heading, -16f * under, 2f * Mathf.Sin(t * 0.8f + c.phase * 6f), c.scale);

            Vector2 bow = new Vector2(Mathf.Cos(c.heading * Mathf.Deg2Rad), -Mathf.Sin(c.heading * Mathf.Deg2Rad));
            FlatQuad(c.map + new Vector2(-sun.x, -sun.z * sinView).normalized * 0.25f * c.scale, bow, 1.3f * c.scale, 0.55f * c.scale,
                     new Color(0f, 0.05f, 0.15f, shadowStrength * up), PuffUV, water + 2f);
            if (up > 0.5f)
                Puffs(c.body, c.model.smoke, true, Mathf.Clamp01(up * 2f - 1f), t + c.phase * 3f);
        }

        private void UpdateDolphin(Critter c, float t, Vector3 sun)
        {
            Pod pod = c.pod;
            bool show = pod != null && pod.map.x < float.MaxValue;
            if (c.body.gameObject.activeSelf != show)
                c.body.gameObject.SetActive(show);
            if (!show)
                return;

            // place in the pod, behind and to the side of the leader
            float r = pod.heading * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(r), -Mathf.Sin(r));
            Vector2 side = new Vector2(-forward.y, forward.x);
            float back = c.slot * 0.95f * c.scale, across = (c.slot % 2 == 0 ? 1f : -1f) * c.slot * 0.55f * c.scale;
            Vector2 sea = forward * -back + side * across;                       // on the water plane (x, z)

            float u = Mathf.Repeat(t / 2.6f + c.phase, 1f);
            const float air = 0.42f;
            float height, pitch, along;
            if (u < air)
            {
                float s = u / air;
                height = Mathf.Sin(Mathf.PI * s) * 0.95f;
                pitch = 48f * Mathf.Cos(Mathf.PI * s);
                along = (s - 0.5f) * 1.8f;
            }
            else
            {
                height = -1.3f;                                                  // swimming below: hidden by the water
                pitch = 0f;
                along = 0f;
            }
            sea += forward * along * c.scale;
            c.map = pod.map + new Vector2(sea.x, sea.y * sinView);

            Vector3 at = ToSea(c.map, 0f);
            float water = WaterHeight(at.x, at.z, waveTime);
            Pose(c.body, c.map, water + height * c.scale, pod.heading, pitch, 0f, c.scale);

            // splashes where it leaves and re-enters the water
            float leave = u / air, enter = (u - air) / 0.18f;
            if (leave < 0.25f)
                Splash(pod.map + Flat(sea - forward * along * c.scale + forward * -0.9f * c.scale), leave / 0.25f, c.scale, water);
            if (enter >= 0f && enter < 1f)
                Splash(pod.map + Flat(sea + forward * 0.9f * c.scale), enter, c.scale, water);
        }

        private Vector2 Flat(Vector2 sea)
        {
            return new Vector2(sea.x, sea.y * sinView);
        }

        private void UpdateGull(Critter c, float t, float dt, Rect keep, Vector3 sun)
        {
            if (!keep.Contains(c.centre))
                Spawn(c, false);
            bool show = c.centre.x < float.MaxValue;
            if (c.body.gameObject.activeSelf != show)
                c.body.gameObject.SetActive(show);
            if (!show)
                return;

            float a = c.phase * 6.283f + c.spin * t * 0.45f;
            Vector2 sea = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * c.radius;
            c.map = c.centre + Flat(sea);
            float yaw = -Mathf.Atan2(c.spin * Mathf.Cos(a), -c.spin * Mathf.Sin(a)) * Mathf.Rad2Deg;
            float height = 3.4f * c.scale + 10f * Mathf.Sin(t * 0.7f + c.phase * 5f);
            Pose(c.body, c.map, height, yaw, 4f * Mathf.Sin(t * 1.3f), -24f * c.spin, c.scale);

            // flap in bursts, glide in between
            bool flapping = Mathf.Repeat(t / 3.5f + c.phase, 1f) < 0.45f;
            float wing = flapping ? 38f * Mathf.Sin(t * 10f + c.phase * 10f) : 6f;
            if (c.wingLeft != null) c.wingLeft.localRotation = Quaternion.Euler(-wing, 0f, 0f);
            if (c.wingRight != null) c.wingRight.localRotation = Quaternion.Euler(wing, 0f, 0f);

            Vector3 at = ToSea(c.map, 0f);
            FlatQuad(c.map, Vector2.right, 0.7f * c.scale, 0.45f * c.scale, new Color(0f, 0.05f, 0.15f, 0.12f), PuffUV,
                     WaterHeight(at.x, at.z, waveTime) + 2f);
        }

        private void UpdateCargo(Critter c, float t, float dt, Rect keep, Vector3 sun)
        {
            if (!keep.Contains(c.map))
                Spawn(c, false);
            bool show = c.map.x < float.MaxValue;
            if (c.body.gameObject.activeSelf != show)
                c.body.gameObject.SetActive(show);
            if (!show)
                return;

            Vector2 drift = MapDirection(c.heading) * 5f * dt;
            if (IsSea(c.map + drift * 30f, c.scale))
                c.map += drift;
            else
                c.heading += 90f * dt;

            Vector3 at = ToSea(c.map, 0f);
            float water = WaterHeight(at.x, at.z, waveTime);
            Vector2 slope = WaterSlope(at.x, at.z, waveTime);
            Pose(c.body, c.map, water - 0.05f * c.scale, c.heading + 12f * t * (c.phase - 0.5f),
                 Mathf.Atan(slope.x) * Mathf.Rad2Deg + 5f * Mathf.Sin(t * 1.7f + c.phase * 9f),
                 -Mathf.Atan(slope.y) * Mathf.Rad2Deg + 6f * Mathf.Sin(t * 1.3f + c.phase * 7f), c.scale);
            FlatQuad(c.map + new Vector2(0.15f, -0.1f) * c.scale, Vector2.right, 0.55f * c.scale, 0.5f * c.scale,
                     new Color(1f, 1f, 1f, 0.35f), PuffUV, water + 2f);          // a little foam around it
        }

        private void Pose(Transform body, Vector2 map, float y, float yaw, float pitch, float roll, float scale)
        {
            Vector3 at = ToSea(map, 0f);
            body.SetPositionAndRotation(new Vector3(at.x, y, at.z),
                Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, pitch) * Quaternion.Euler(roll, 0f, 0f));
            body.localScale = Vector3.one * scale;
        }

        // White spray: a few puffs spreading out from a point on the water and fading (age 0..1).
        private void Splash(Vector2 map, float age, float scale, float water)
        {
            float alpha = (1f - age) * Mathf.Min(1f, age * 8f + 0.2f);
            Vector3 c = ToSea(map, water + 2f);
            Vector3 right = seaCamera.transform.right, up = seaCamera.transform.up;
            for (int k = 0; k < 5; k++)
            {
                float a = k * 1.2566f;
                Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.15f + 0.45f * age) * scale +
                            Vector3.up * Mathf.Sin(Mathf.PI * Mathf.Min(1f, age * 1.4f)) * 0.5f * scale;
                float r = (0.08f + 0.12f * age) * scale;
                AddBillboard(p, right, up, r, new Vector4(1f, 1f, 1f, alpha * 0.9f));
            }
        }

        private static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
