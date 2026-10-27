using System.Collections.Generic;
using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Materiales y mallas generados por código. Usa URP/Lit si el proyecto es URP
    /// (plantilla 3D de Unity 6) y cae a Standard si es Built-in.
    /// </summary>
    public static class MatLib
    {
        public static Shader LitShader;
        static readonly Dictionary<(Color, float, float, bool), Material> solidCache = new Dictionary<(Color, float, float, bool), Material>();
        static readonly Dictionary<PrimitiveType, Mesh> primitiveCache = new Dictionary<PrimitiveType, Mesh>();
        static readonly Dictionary<(int, bool), Mesh> coneCache = new Dictionary<(int, bool), Mesh>();

        public static Shader Lit
        {
            get
            {
                if (LitShader != null) return LitShader;
                LitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (LitShader == null) LitShader = Shader.Find("Standard");
                return LitShader;
            }
        }

        public static void ClearCaches()
        {
            solidCache.Clear();
            coneCache.Clear();
            primitiveCache.Clear();
        }

        public static Material Solid(Color c, float smoothness = 0.15f, float metallic = 0f, bool emissive = false)
        {
            var key = (c, smoothness, metallic, emissive);
            if (solidCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var m = new Material(Lit) { color = c, enableInstancing = true };
            SetSurface(m, smoothness, metallic);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.6f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            solidCache[key] = m;
            return m;
        }

        public static Material Textured(Texture2D tex, float smoothness = 0.1f, float metallic = 0f)
        {
            var m = new Material(Lit) { color = Color.white, mainTexture = tex };
            SetSurface(m, smoothness, metallic);
            return m;
        }

        static void SetSurface(Material m, float smoothness, float metallic)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        }

        public static Mesh Primitive(PrimitiveType type)
        {
            if (primitiveCache.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            primitiveCache[type] = mesh;
            return mesh;
        }

        /// <summary>Cono (o pirámide si sides = 4) de radio 1 y altura 1, base en y = 0, sombreado plano.</summary>
        public static Mesh Cone(int sides, bool doubleSided = false)
        {
            if (coneCache.TryGetValue((sides, doubleSided), out var cached) && cached != null) return cached;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 apex = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                Vector3 b0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 b1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                int s = verts.Count;
                verts.Add(b0); verts.Add(apex); verts.Add(b1);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                if (doubleSided)
                {
                    s = verts.Count;
                    verts.Add(b0); verts.Add(apex); verts.Add(b1);
                    tris.Add(s); tris.Add(s + 2); tris.Add(s + 1);
                }
                else
                {
                    // tapa inferior
                    s = verts.Count;
                    verts.Add(Vector3.zero); verts.Add(b0); verts.Add(b1);
                    tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                }
            }
            var mesh = new Mesh { name = $"Cono{sides}" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            coneCache[(sides, doubleSided)] = mesh;
            return mesh;
        }

        public static Mesh Quad(float minX, float minZ, float maxX, float maxZ, float y)
        {
            var mesh = new Mesh { name = "Quad" };
            mesh.vertices = new[]
            {
                new Vector3(minX, y, minZ), new Vector3(minX, y, maxZ),
                new Vector3(maxX, y, maxZ), new Vector3(maxX, y, minZ)
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Texture2D NewTexture(int w, int h, bool mipmaps = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mipmaps)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            return t;
        }

        public static Texture2D Checker(int cellsX, int cellsY, int cellPx)
        {
            var t = NewTexture(cellsX * cellPx, cellsY * cellPx, false);
            t.filterMode = FilterMode.Point;
            var px = new Color32[t.width * t.height];
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                {
                    bool black = ((x / cellPx) + (y / cellPx)) % 2 == 0;
                    px[y * t.width + x] = black ? new Color32(20, 20, 20, 255) : new Color32(245, 245, 245, 255);
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }
}
