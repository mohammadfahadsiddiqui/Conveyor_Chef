using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Reads the World Map's low-poly models: OBJ text (v / f / usemtl) plus "#color name r g b
    /// [doublesided]" and "#smoke x y z" lines. X = forward, Y = up, Z = left.
    /// Used by the UI boats (WorldMapBoat3D) and the 3D sea (WorldMapSea3D).
    /// </summary>
    public static class WorldMapModelText
    {
        public sealed class Model
        {
            public Vector3[] corners;      // 3 per triangle
            public Vector3[] normals;      // per triangle
            public Vector3[] centres;      // per triangle (of the original face)
            public Color32[] colors;       // per triangle
            public bool[] twoSided;        // per triangle
            public Vector3[] smoke;
        }

        public static Model Parse(string text)
        {
            var verts = new List<Vector3>();
            var palette = new Dictionary<string, (Color32 color, bool twoSided)>();
            var smoke = new List<Vector3>();
            var corners = new List<Vector3>();
            var normals = new List<Vector3>();
            var centres = new List<Vector3>();
            var colors = new List<Color32>();
            var twoSided = new List<bool>();
            var face = new List<Vector3>();
            (Color32 color, bool twoSided) current = (new Color32(200, 200, 200, 255), false);
            CultureInfo inv = CultureInfo.InvariantCulture;

            foreach (string raw in text.Split('\n'))
            {
                string[] p = raw.Trim().Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0)
                    continue;

                switch (p[0])
                {
                    case "#color" when p.Length >= 5:
                        palette[p[1]] = (new Color32(byte.Parse(p[2], inv), byte.Parse(p[3], inv), byte.Parse(p[4], inv), 255),
                                         p.Length > 5 && p[5] == "doublesided");
                        break;
                    case "#smoke" when p.Length >= 4:
                        smoke.Add(new Vector3(float.Parse(p[1], inv), float.Parse(p[2], inv), float.Parse(p[3], inv)));
                        break;
                    case "v" when p.Length >= 4:
                        verts.Add(new Vector3(float.Parse(p[1], inv), float.Parse(p[2], inv), float.Parse(p[3], inv)));
                        break;
                    case "usemtl" when p.Length >= 2:
                        if (!palette.TryGetValue(p[1], out current))
                            current = (new Color32(200, 200, 200, 255), false);
                        break;
                    case "f" when p.Length >= 4:
                        face.Clear();
                        Vector3 centre = Vector3.zero;
                        for (int i = 1; i < p.Length; i++)
                        {
                            int slash = p[i].IndexOf('/');
                            int index = int.Parse(slash < 0 ? p[i] : p[i].Substring(0, slash), inv);
                            Vector3 v = verts[index > 0 ? index - 1 : verts.Count + index];
                            face.Add(v);
                            centre += v;
                        }
                        centre /= face.Count;

                        Vector3 n = Vector3.Cross(face[1] - face[0], face[2] - face[0]);
                        if (n.sqrMagnitude < 1e-12f && face.Count > 3)
                            n = Vector3.Cross(face[2] - face[0], face[3] - face[0]);
                        if (n.sqrMagnitude < 1e-12f)
                            break;
                        n.Normalize();

                        for (int i = 1; i + 1 < face.Count; i++)   // fan
                        {
                            corners.Add(face[0]);
                            corners.Add(face[i]);
                            corners.Add(face[i + 1]);
                            normals.Add(n);
                            centres.Add(centre);
                            colors.Add(current.color);
                            twoSided.Add(current.twoSided);
                        }
                        break;
                }
            }

            return new Model
            {
                corners = corners.ToArray(),
                normals = normals.ToArray(),
                centres = centres.ToArray(),
                colors = colors.ToArray(),
                twoSided = twoSided.ToArray(),
                smoke = smoke.ToArray(),
            };
        }

        /// <summary>
        /// A flat-shaded Unity mesh that needs no vertex colours: normals = face normals (lighting),
        /// uv0 = this face's colour in the palette texture, uv1 = smooth normals (outline).
        /// Palette alpha below 1 marks thin double-sided parts (kept bright by the shader).
        /// </summary>
        public static Mesh BuildMesh(Model m, string name, out Texture2D palette)
        {
            int n = m.corners.Length;
            var keys = new List<Color32>();
            var faceSlot = new int[m.colors.Length];
            for (int f = 0; f < m.colors.Length; f++)
            {
                Color32 c = m.colors[f];
                c.a = m.twoSided[f] ? (byte)128 : (byte)255;
                int slot = keys.FindIndex(k => k.r == c.r && k.g == c.g && k.b == c.b && k.a == c.a);
                if (slot < 0)
                {
                    slot = keys.Count;
                    keys.Add(c);
                }
                faceSlot[f] = slot;
            }

            palette = new Texture2D(Mathf.Max(1, keys.Count), 1, TextureFormat.RGBA32, false)
            {
                name = name + " Palette",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            palette.SetPixels32(keys.Count > 0 ? keys.ToArray() : new[] { new Color32(255, 255, 255, 255) });
            palette.Apply(false, true);

            var sums = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < n; i++)
            {
                Vector3Int key = Key(m.corners[i]);
                sums.TryGetValue(key, out Vector3 sum);
                sums[key] = sum + m.normals[i / 3];
            }

            var vertices = new Vector3[n];
            var normals = new Vector3[n];
            var uv = new Vector2[n];
            var smooth = new List<Vector3>(n);
            var indices = new int[n];
            float width = Mathf.Max(1, keys.Count);
            for (int i = 0; i < n; i++)
            {
                vertices[i] = m.corners[i];
                normals[i] = m.normals[i / 3];
                uv[i] = new Vector2((faceSlot[i / 3] + 0.5f) / width, 0.5f);
                Vector3 s = sums[Key(m.corners[i])];
                smooth.Add(s.sqrMagnitude > 1e-8f ? s.normalized : m.normals[i / 3]);
                indices[i] = i;
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.SetUVs(1, smooth);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3Int Key(Vector3 p)
        {
            return new Vector3Int(Mathf.RoundToInt(p.x * 1000f), Mathf.RoundToInt(p.y * 1000f), Mathf.RoundToInt(p.z * 1000f));
        }
    }
}
