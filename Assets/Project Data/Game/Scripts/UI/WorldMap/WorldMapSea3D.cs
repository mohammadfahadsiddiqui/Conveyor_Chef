using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// A real 3D sea under the World Map UI: a wavy water surface lit by the sun, 3D boats and
    /// whales that ride the waves and sit in the water, wakes, smoke, whale spray, and shadows of
    /// the boats and of the clouds. A camera behind the UI follows the map's scrolling every frame,
    /// so everything stays exactly where the map puts it.
    ///
    /// Positions still come from the props under MapContent/Map Props (move or animate those in
    /// the Hierarchy, WorldMapAmbientProp settings included); this only replaces how they look.
    /// Runs in Play mode only; in the editor the 2D map stays as it was.
    /// </summary>
    [DefaultExecutionOrder(10000)]          // after the ScrollRect has moved the map this frame
    [DisallowMultipleComponent]
    public sealed class WorldMapSea3D : MonoBehaviour
    {
        private const string ResourceFolder = "WorldMapSea/";

        [Header("Map (UI)")]
        [Tooltip("MapContent/Map Props: boats and whales are read from here.")]
        [SerializeField] private RectTransform mapProps;
        [Tooltip("MapContent/Sky: the clouds cast shadows on the sea.")]
        [SerializeField] private RectTransform sky;
        [Tooltip("Flat ocean pictures replaced by the 3D sea while playing.")]
        [SerializeField] private GameObject[] hideWhileRunning = new GameObject[0];

        [Header("Look")]
        [Tooltip("How steeply the camera looks down at the sea (match WorldMapBoat3D's View Angle).")]
        [SerializeField, Range(20f, 80f)] private float viewAngle = 42f;
        [Tooltip("Direction toward the sun (x right, y up, z away from the camera).")]
        [SerializeField] private Vector3 sunDirection = new Vector3(-0.5f, 0.8f, -0.3f);
        [SerializeField, Range(0f, 3f)] private float waveHeight = 1f;
        [SerializeField, Range(0f, 2f)] private float waveSpeed = 1f;

        [Header("Boats and Whales")]
        [SerializeField, Range(0.1f, 1f)] private float boatSize = 0.46f;
        [SerializeField, Range(0.1f, 1f)] private float whaleSize = 0.36f;
        [SerializeField, Range(0f, 1f)] private float shadowStrength = 0.28f;
        [SerializeField, Range(0f, 1f)] private float cloudShadowStrength = 0.16f;

        [Header("Assets (empty = Resources/WorldMapSea)")]
        [SerializeField] private TextAsset sailboatModel;
        [SerializeField] private TextAsset steamshipModel;
        [SerializeField] private TextAsset whaleModel;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material modelMaterial;
        [SerializeField] private Material effectsMaterial;

        [Header("Rendering")]
        [Tooltip("Layer for the 3D sea (only the sea camera draws it).")]
        [SerializeField, Range(0, 31)] private int seaLayer = 4;
        [SerializeField, Min(0f)] private float margin = 2500f;
        [SerializeField, Range(16, 200)] private int waterCells = 110;

        // ---- waves: shared with the water shader so boats ride the same swell
        private static readonly Vector4[] WaveSet =
        {
            Wave(1f, 0.35f, 520f, 4.0f, 0.9f),
            Wave(-0.45f, 1f, 330f, 2.6f, 1.1f),
            Wave(0.8f, -0.6f, 210f, 1.6f, 1.5f),
            Wave(-0.2f, -1f, 140f, 0.9f, 1.9f),
        };

        private static Vector4 Wave(float dx, float dz, float length, float height, float speed)
        {
            Vector2 k = new Vector2(dx, dz).normalized * (2f * Mathf.PI / length);
            return new Vector4(k.x, k.y, height, speed);
        }

        private sealed class Floater
        {
            public WorldMapAmbientProp prop;
            public RectTransform rect;
            public Transform body;
            public WorldMapModelText.Model model;
            public bool whale;
            public float waterline;
            public Vector3 lastPosition;
            public float speed;
        }

        private static readonly Dictionary<TextAsset, Mesh> meshes = new Dictionary<TextAsset, Mesh>();
        private static readonly Dictionary<TextAsset, WorldMapModelText.Model> models = new Dictionary<TextAsset, WorldMapModelText.Model>();

        private readonly List<Floater> floaters = new List<Floater>();
        private readonly List<WorldMapAmbientProp> clouds = new List<WorldMapAmbientProp>();
        private readonly Vector4[] waves = new Vector4[4];

        private readonly List<Vector3> fxVerts = new List<Vector3>();
        private readonly List<Color32> fxColors = new List<Color32>();
        private readonly List<Vector2> fxUV = new List<Vector2>();
        private readonly List<int> fxTris = new List<int>();

        private Camera seaCamera;
        private Transform world;
        private Mesh fxMesh;
        private float sinView, cosView, fxLift;
        private bool running;

        // boat_fx.png regions (512 x 256)
        private const float WakeTop = 0.75f, PuffLeft = 448f / 512f, PuffBottom = 0.75f;
        private static readonly Vector3[] WakeQuad =
        {
            new Vector3(-2.3f, 0f, 0.68f), new Vector3(1.3f, 0f, 0.68f), new Vector3(1.3f, 0f, -0.68f), new Vector3(-2.3f, 0f, -0.68f),
        };
        private static readonly Vector2[] WakeUV =
        {
            new Vector2(0f, WakeTop), new Vector2(1f, WakeTop), new Vector2(1f, 0f), new Vector2(0f, 0f),
        };

        // ---------------------------------------------------------------- set-up

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        // The World Map gets its 3D sea even if the open scene has no "Sea 3D" object yet.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.isLoaded)
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<WorldMapSea3D>(true) != null)
                    return;
            }

            RectTransform props = FindMapProps(scene);
            if (props == null)
                return;

            var go = new GameObject("Sea 3D");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<WorldMapSea3D>().mapProps = props;
        }

        private static RectTransform FindMapProps(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "Map Props" && rt.parent != null && rt.parent.name == "MapContent")
                        return rt;
                }
            }
            return null;
        }

        private void Start()
        {
            if (mapProps == null)
                mapProps = FindMapProps(gameObject.scene);
            Canvas canvas = mapProps != null ? mapProps.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning("[WorldMap] 3D sea needs the World Map on a Screen Space Overlay canvas; keeping the 2D sea.");
                enabled = false;
                return;
            }

            LoadDefaults(canvas);
            if (waterMaterial == null || modelMaterial == null || effectsMaterial == null ||
                !waterMaterial.shader.isSupported || !modelMaterial.shader.isSupported || !effectsMaterial.shader.isSupported)
            {
                Debug.LogWarning("[WorldMap] 3D sea materials missing or not supported on this device; keeping the 2D sea.");
                enabled = false;
                return;
            }

            sinView = Mathf.Sin(viewAngle * Mathf.Deg2Rad);
            cosView = Mathf.Cos(viewAngle * Mathf.Deg2Rad);

            world = new GameObject("Sea 3D World").transform;
            world.SetParent(transform, false);
            world.gameObject.layer = seaLayer;

            BuildCamera();
            BuildWater();
            BuildFloaters();
            BuildEffects();

            foreach (GameObject go in hideWhileRunning)
            {
                if (go != null)
                    go.SetActive(false);
            }

            running = true;
            LateUpdate();
        }

        private void LoadDefaults(Canvas canvas)
        {
            if (sailboatModel == null) sailboatModel = Resources.Load<TextAsset>(ResourceFolder + "sailboat");
            if (steamshipModel == null) steamshipModel = Resources.Load<TextAsset>(ResourceFolder + "steamship");
            if (whaleModel == null) whaleModel = Resources.Load<TextAsset>(ResourceFolder + "whale");
            if (waterMaterial == null) waterMaterial = Resources.Load<Material>(ResourceFolder + "SeaWater");
            if (modelMaterial == null) modelMaterial = Resources.Load<Material>(ResourceFolder + "SeaModel");
            if (effectsMaterial == null) effectsMaterial = Resources.Load<Material>(ResourceFolder + "SeaEffects");

            Transform content = mapProps.parent;
            if (sky == null && content != null)
                sky = content.Find("Sky") as RectTransform;

            if (hideWhileRunning == null || hideWhileRunning.Length == 0)
            {
                var list = new List<GameObject>();
                if (content != null)
                {
                    AddIfFound(list, content.Find("ScrollableOcean"));
                    AddIfFound(list, content.Find("Ocean FX"));
                    Transform viewport = content.parent;
                    if (viewport != null && viewport.parent != null)
                        AddIfFound(list, viewport.parent.Find("Background Artwork"));
                }
                hideWhileRunning = list.ToArray();
            }
        }

        private static void AddIfFound(List<GameObject> list, Transform t)
        {
            if (t != null)
                list.Add(t.gameObject);
        }

        private void BuildCamera()
        {
            var go = new GameObject("Sea Camera");
            go.transform.SetParent(world, false);
            seaCamera = go.AddComponent<Camera>();
            seaCamera.orthographic = true;
            seaCamera.clearFlags = CameraClearFlags.SolidColor;
            seaCamera.backgroundColor = new Color(0.12f, 0.5f, 0.8f, 1f);
            seaCamera.cullingMask = 1 << seaLayer;
            seaCamera.nearClipPlane = 10f;
            seaCamera.farClipPlane = 20000f;
            seaCamera.allowHDR = false;
            seaCamera.allowMSAA = true;

            float depth = -1f;
            foreach (Camera other in Camera.allCameras)
            {
                if (other == seaCamera)
                    continue;
                other.cullingMask &= ~(1 << seaLayer);     // nobody else draws the sea
                depth = Mathf.Max(depth, other.depth);
            }
            seaCamera.depth = depth + 1f;                    // drawn after the scene cameras, under the UI
        }

        private void BuildWater()
        {
            Rect map = mapProps.rect;
            float x0 = map.xMin - margin, x1 = map.xMax + margin;
            float z0 = (map.yMin - margin) / sinView, z1 = (map.yMax + margin) / sinView;
            int nx = waterCells;
            int nz = Mathf.Clamp(Mathf.RoundToInt(nx * (z1 - z0) / (x1 - x0)), 8, 64000 / (nx + 1) - 1);

            var verts = new Vector3[(nx + 1) * (nz + 1)];
            for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
                verts[j * (nx + 1) + i] = new Vector3(Mathf.Lerp(x0, x1, (float)i / nx), 0f, Mathf.Lerp(z0, z1, (float)j / nz));

            var tris = new int[nx * nz * 6];
            int k = 0;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = a + nx + 1;
                tris[k++] = a; tris[k++] = b; tris[k++] = b + 1;
                tris[k++] = a; tris[k++] = b + 1; tris[k++] = a + 1;
            }

            var mesh = new Mesh { name = "Sea Water" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 200f, z1 - z0));

            var go = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = seaLayer;
            go.transform.SetParent(world, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = waterMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private void BuildFloaters()
        {
            // one per prop object (an object carrying two animators uses the last, like the animation does)
            var byObject = new Dictionary<Transform, WorldMapAmbientProp>();
            foreach (WorldMapAmbientProp prop in mapProps.GetComponentsInChildren<WorldMapAmbientProp>(true))
                byObject[prop.transform] = prop;

            foreach (WorldMapAmbientProp prop in byObject.Values)
            {
                if (prop.PropMotion == WorldMapAmbientProp.Motion.Drift)
                    continue;

                bool whale = prop.PropMotion == WorldMapAmbientProp.Motion.Surface || prop.name.Contains("Whale");
                TextAsset text = whale ? whaleModel : prop.name.Contains("Steam") ? steamshipModel : sailboatModel;
                if (text == null)
                    continue;

                var go = new GameObject(prop.name + " 3D", typeof(MeshFilter), typeof(MeshRenderer));
                go.layer = seaLayer;
                go.transform.SetParent(world, false);
                go.GetComponent<MeshFilter>().sharedMesh = GetMesh(text);
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = modelMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;

                // the 2D picture / UI boat stays as the editable stand-in but isn't drawn
                Graphic picture = prop.GetComponent<Graphic>();
                if (picture != null)
                    picture.enabled = false;

                floaters.Add(new Floater
                {
                    prop = prop,
                    rect = (RectTransform)prop.transform,
                    body = go.transform,
                    model = models[text],
                    whale = whale,
                    waterline = whale ? -0.05f : -0.12f,
                });
            }

            if (sky != null)
            {
                foreach (WorldMapAmbientProp cloud in sky.GetComponentsInChildren<WorldMapAmbientProp>(true))
                {
                    if (cloud.PropMotion == WorldMapAmbientProp.Motion.Drift)
                        clouds.Add(cloud);
                }
            }
        }

        private static Mesh GetMesh(TextAsset text)
        {
            if (!meshes.TryGetValue(text, out Mesh mesh) || mesh == null)
            {
                WorldMapModelText.Model m = WorldMapModelText.Parse(text.text);
                models[text] = m;
                mesh = WorldMapModelText.BuildMesh(m, text.name);
                meshes[text] = mesh;
            }
            return mesh;
        }

        private void BuildEffects()
        {
            fxMesh = new Mesh { name = "Sea Effects" };
            fxMesh.MarkDynamic();
            var go = new GameObject("Effects", typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = seaLayer;
            go.transform.SetParent(world, false);
            go.GetComponent<MeshFilter>().sharedMesh = fxMesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = effectsMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            float total = 0f;
            foreach (Vector4 w in WaveSet)
                total += w.z;
            fxLift = total * waveHeight + 1f;   // effects float just above the highest crest
        }

        private void OnDestroy()
        {
            if (!running)
                return;
            foreach (GameObject go in hideWhileRunning)
            {
                if (go != null)
                    go.SetActive(true);
            }
            foreach (Floater f in floaters)
            {
                Graphic picture = f.prop != null ? f.prop.GetComponent<Graphic>() : null;
                if (picture != null)
                    picture.enabled = true;
            }
            if (fxMesh != null)
                Destroy(fxMesh);
        }

        // ---------------------------------------------------------------- every frame

        private void LateUpdate()
        {
            if (!running || mapProps == null)
                return;

            float t = Time.time * waveSpeed;
            for (int i = 0; i < 4; i++)
                waves[i] = new Vector4(WaveSet[i].x, WaveSet[i].y, WaveSet[i].z * waveHeight, WaveSet[i].w);

            SyncCamera();

            Vector3 sun = sunDirection.sqrMagnitude > 0f ? sunDirection.normalized : Vector3.up;
            Shader.SetGlobalVectorArray("_SeaWaves", waves);
            Shader.SetGlobalFloat("_SeaTime", t);
            Shader.SetGlobalFloat("_SeaFlatten", sinView);
            Shader.SetGlobalVector("_SeaSunDir", sun);
            Shader.SetGlobalVector("_SeaViewDir", -seaCamera.transform.forward);

            fxVerts.Clear(); fxColors.Clear(); fxUV.Clear(); fxTris.Clear();

            foreach (WorldMapAmbientProp cloud in clouds)
            {
                if (cloud != null && cloud.isActiveAndEnabled)
                    CloudShadow(cloud, sun);
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            foreach (Floater f in floaters)
            {
                if (f.prop == null)
                    continue;
                bool shown = f.prop.isActiveAndEnabled && (!f.whale || f.prop.Surfacing > 0.001f);
                if (f.body.gameObject.activeSelf != shown)
                    f.body.gameObject.SetActive(shown);
                if (!shown)
                    continue;
                Place(f, t, dt, sun);
            }

            // smoke and spray last so they draw over everything
            foreach (Floater f in floaters)
            {
                if (f.prop != null && f.body.gameObject.activeSelf)
                    Puffs(f, Time.time);
            }

            fxMesh.Clear();
            fxMesh.SetVertices(fxVerts);
            fxMesh.SetColors(fxColors);
            fxMesh.SetUVs(0, fxUV);
            fxMesh.SetTriangles(fxTris, 0);
            fxMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        }

        // The sea camera shows exactly the part of the map the UI shows.
        private void SyncCamera()
        {
            Vector2 centre = mapProps.InverseTransformPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            float pixelsPerUnit = Mathf.Max(1e-4f, mapProps.lossyScale.y);

            seaCamera.orthographicSize = Screen.height * 0.5f / pixelsPerUnit;
            Quaternion look = Quaternion.Euler(viewAngle, 0f, 0f);
            Vector3 target = ToSea(centre, 0f);
            seaCamera.transform.SetPositionAndRotation(target - look * Vector3.forward * 8000f, look);
        }

        /// <summary>Map point (map units) at a height above the water, so it lands on that map point on screen.</summary>
        private Vector3 ToSea(Vector2 map, float height)
        {
            // raising a point moves it up the screen by height * cos(view); pull it nearer to cancel that
            return new Vector3(map.x, height, (map.y - height * cosView) / sinView);
        }

        private float WaterHeight(float x, float z, float t)
        {
            float h = 0f;
            for (int i = 0; i < 4; i++)
                h += waves[i].z * Mathf.Sin(waves[i].x * x + waves[i].y * z + waves[i].w * t);
            return h;
        }

        private Vector2 WaterSlope(float x, float z, float t)
        {
            Vector2 g = Vector2.zero;
            for (int i = 0; i < 4; i++)
                g += new Vector2(waves[i].x, waves[i].y) * (waves[i].z * Mathf.Cos(waves[i].x * x + waves[i].y * z + waves[i].w * t));
            return g;
        }

        private void Place(Floater f, float t, float dt, Vector3 sun)
        {
            WorldMapAmbientProp p = f.prop;
            Rect r = f.rect.rect;
            float scale = r.width * (f.whale ? whaleSize : boatSize);
            Vector2 map = p.SeaPosition + new Vector2(0f, f.waterline * r.height);

            Vector3 at = ToSea(map, 0f);
            float yaw = p.PoseYaw;
            float rad = yaw * Mathf.Deg2Rad;
            Vector2 bow = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));          // heading on the water (x, z)
            Vector2 slope = WaterSlope(at.x, at.z, t);
            float pitch = p.PosePitch + Mathf.Atan(Vector2.Dot(slope, bow)) * Mathf.Rad2Deg * 0.8f;
            float roll = p.PoseRoll - Mathf.Atan(Vector2.Dot(slope, new Vector2(-bow.y, bow.x))) * Mathf.Rad2Deg * 0.8f;
            float water = WaterHeight(at.x, at.z, t);
            float y = water + p.PoseLift * 0.3f;

            if (f.whale)
            {
                float under = 1f - p.Surfacing;
                y -= under * 0.85f * scale;
                pitch -= 18f * under;
            }

            Vector3 pos = new Vector3(at.x, y, at.z);
            f.body.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, pitch) * Quaternion.Euler(roll, 0f, 0f));
            f.body.localScale = Vector3.one * scale;

            f.speed = Mathf.Lerp(f.speed, (new Vector2(pos.x - f.lastPosition.x, pos.z - f.lastPosition.z)).magnitude / dt, 0.1f);
            f.lastPosition = pos;

            // shadow on the water, away from the sun
            Vector2 away = new Vector2(-sun.x, -sun.z).normalized * 0.22f * scale;
            float shadowAlpha = shadowStrength * (f.whale ? p.Surfacing : 1f);
            FlatQuad(map + new Vector2(away.x, away.y * sinView), bow, 1.25f * scale, 0.6f * scale,
                     new Color(0f, 0.05f, 0.15f, shadowAlpha), PuffUV, water + 2f);

            if (!f.whale)
            {
                float wake = Mathf.Clamp(f.speed / 8f, 0.35f, 1f);
                Wake(map, bow, scale, wake, water + 3f);
            }
        }

        private void CloudShadow(WorldMapAmbientProp cloud, Vector3 sun)
        {
            RectTransform rt = (RectTransform)cloud.transform;
            Vector2 size = rt.rect.size * Mathf.Abs(rt.localScale.x);
            // clouds are high up: their shadows fall well away from the sun
            Vector2 map = cloud.SeaPosition + new Vector2(-sun.x, -sun.z * sinView).normalized * size.x * 0.45f;
            FlatQuad(map, Vector2.right, size.x * 0.42f, size.y * 0.24f / sinView,
                     new Color(0f, 0.04f, 0.12f, cloudShadowStrength), PuffUV, fxLift);
        }

        private static readonly Vector2[] PuffUV =
        {
            new Vector2(PuffLeft, PuffBottom), new Vector2(1f, PuffBottom), new Vector2(1f, 1f), new Vector2(PuffLeft, 1f),
        };

        // a quad lying on the water, centred on a map point, long side along dir (x, z on the sea)
        private void FlatQuad(Vector2 map, Vector2 dir, float halfLength, float halfWidth, Color color, Vector2[] uv, float height)
        {
            Vector3 c = ToSea(map, height);
            Vector3 along = new Vector3(dir.x, 0f, dir.y) * halfLength;
            Vector3 across = new Vector3(-dir.y, 0f, dir.x) * halfWidth;
            int start = fxVerts.Count;
            fxVerts.Add(c - along - across);
            fxVerts.Add(c + along - across);
            fxVerts.Add(c + along + across);
            fxVerts.Add(c - along + across);
            Color32 c32 = color;
            for (int i = 0; i < 4; i++)
            {
                fxColors.Add(c32);
                fxUV.Add(uv[i]);
            }
            fxTris.Add(start); fxTris.Add(start + 1); fxTris.Add(start + 2);
            fxTris.Add(start); fxTris.Add(start + 2); fxTris.Add(start + 3);
        }

        private void Wake(Vector2 map, Vector2 bow, float scale, float alpha, float height)
        {
            Vector3 c = ToSea(map, height);
            Vector3 x = new Vector3(bow.x, 0f, bow.y);
            Vector3 z = new Vector3(-bow.y, 0f, bow.x);
            int start = fxVerts.Count;
            Color32 col = new Color(1f, 1f, 1f, alpha);
            for (int i = 0; i < 4; i++)
            {
                fxVerts.Add(c + (x * WakeQuad[i].x + z * WakeQuad[i].z) * scale);
                fxColors.Add(col);
                fxUV.Add(WakeUV[i]);
            }
            fxTris.Add(start); fxTris.Add(start + 1); fxTris.Add(start + 2);
            fxTris.Add(start); fxTris.Add(start + 2); fxTris.Add(start + 3);
        }

        private void Puffs(Floater f, float time)
        {
            Vector3[] points = f.model.smoke;
            if (points.Length == 0)
                return;

            float scale = f.body.localScale.x;
            float strength = f.whale ? Mathf.Clamp01(f.prop.Surfacing * 2f - 1f) : 1f;
            if (strength <= 0f)
                return;

            Vector3 right = seaCamera.transform.right, up = seaCamera.transform.up;
            Color tint = f.whale ? new Color(0.85f, 0.95f, 1f) : new Color(0.96f, 0.96f, 0.98f);
            const int count = 4;
            float cycle = f.whale ? 1.6f : 3.2f;

            foreach (Vector3 local in points)
            {
                Vector3 from = f.body.TransformPoint(local);
                for (int k = 0; k < count; k++)
                {
                    float u = Mathf.Repeat(time / cycle + (float)k / count, 1f);
                    float radius = (f.whale ? 0.08f + 0.12f * u : 0.1f + 0.16f * u) * scale;
                    Vector3 c = from + Vector3.up * ((f.whale ? 0.45f : 0.55f) * u * scale) +
                                Vector3.left * ((f.whale ? 0.05f : 0.35f) * u * scale);
                    tint.a = strength * (1f - u) * Mathf.Min(1f, u * 6f);
                    Color32 col = tint;
                    int start = fxVerts.Count;
                    fxVerts.Add(c + (-right - up) * radius);
                    fxVerts.Add(c + (right - up) * radius);
                    fxVerts.Add(c + (right + up) * radius);
                    fxVerts.Add(c + (-right + up) * radius);
                    for (int i = 0; i < 4; i++)
                    {
                        fxColors.Add(col);
                        fxUV.Add(PuffUV[i]);
                    }
                    fxTris.Add(start); fxTris.Add(start + 1); fxTris.Add(start + 2);
                    fxTris.Add(start); fxTris.Add(start + 2); fxTris.Add(start + 3);
                }
            }
        }
    }
}
