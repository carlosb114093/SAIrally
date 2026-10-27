using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SanAndres
{
    /// <summary>
    /// Genera la isla de San Andrés: contorno con forma de caballito de mar, La Loma en el centro,
    /// la vía Circunvalar bordeando la costa, el aeropuerto al norte, los cayos (Johnny Cay, Haynes Cay,
    /// El Acuario, Rocky Cay), la barrera coralina al oriente y el "mar de los siete colores".
    /// </summary>
    public static class IslandBuilder
    {
        // Contorno aproximado de la isla (x = este, z = norte), en sentido horario desde North End.
        static readonly Vector2[] Control =
        {
            new Vector2(40, 728), new Vector2(110, 700), new Vector2(155, 640), new Vector2(182, 560),
            new Vector2(192, 450), new Vector2(180, 340), new Vector2(168, 210), new Vector2(162, 70),
            new Vector2(152, -80), new Vector2(138, -220), new Vector2(118, -360), new Vector2(95, -470),
            new Vector2(76, -548), new Vector2(64, -622), new Vector2(40, -676), new Vector2(0, -700),
            new Vector2(-42, -688), new Vector2(-74, -642), new Vector2(-94, -566), new Vector2(-108, -470), new Vector2(-128, -345),
            new Vector2(-146, -205), new Vector2(-160, -65), new Vector2(-171, 80), new Vector2(-180, 220),
            new Vector2(-186, 360), new Vector2(-178, 485), new Vector2(-145, 595), new Vector2(-88, 672),
            new Vector2(-25, 716)
        };

        public const float RoadInset = 26f;

        // Islotes: centro x, centro z, radio, altura máxima
        public static readonly Vector4 JohnnyCay = new Vector4(232f, 772f, 30f, 2.2f);
        public static readonly Vector4 HaynesCay = new Vector4(298f, 228f, 20f, 1.6f);
        public static readonly Vector4 Acuario = new Vector4(282f, 186f, 15f, 1.4f);
        public static readonly Vector4 RockyCay = new Vector4(222f, 336f, 7f, 1.8f);

        // ------------------------------------------------------------------ datos

        public static IslandData Build()
        {
            var d = new IslandData();
            d.outline = SmoothClosed(Control, 8);
            BuildRoad(d);
            d.runway = new Rect(-105f, 508f, 210f, 24f);
            float rh = 0f;
            for (int i = 0; i < 5; i++) rh += BaseHeight(d, -100f + i * 50f, 520f);
            d.runwayHeight = Mathf.Max(2.5f, rh / 5f);
            BuildHeights(d);
            d.minimap = BuildMinimap(d);
            return d;
        }

        static Vector2[] SmoothClosed(Vector2[] c, int steps)
        {
            var list = new List<Vector2>();
            int n = c.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = c[(i - 1 + n) % n], p1 = c[i], p2 = c[(i + 1) % n], p3 = c[(i + 2) % n];
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps, t2 = t * t, t3 = t2 * t;
                    list.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                                     (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            return list.ToArray();
        }

        static List<Vector2> ResampleClosed(List<Vector2> p, float spacing)
        {
            int n = p.Count;
            var cum = new float[n + 1];
            for (int i = 0; i < n; i++) cum[i + 1] = cum[i] + Vector2.Distance(p[i], p[(i + 1) % n]);
            float total = cum[n];
            int count = Mathf.Max(8, Mathf.RoundToInt(total / spacing));
            float step = total / count;
            var res = new List<Vector2>(count);
            int seg = 0;
            for (int k = 0; k < count; k++)
            {
                float s = k * step;
                while (seg < n - 1 && cum[seg + 1] < s) seg++;
                float segLen = cum[seg + 1] - cum[seg];
                float t = segLen > 1e-4f ? (s - cum[seg]) / segLen : 0f;
                res.Add(Vector2.Lerp(p[seg], p[(seg + 1) % n], t));
            }
            return res;
        }

        static void BuildRoad(IslandData d)
        {
            var o = d.outline;
            int n = o.Length;
            var pts = new List<Vector2>(n);
            for (int i = 0; i < n; i++) pts.Add(o[i] + d.InwardNormal(i) * RoadInset);

            // suavizado (elimina quiebres en las puntas norte y sur)
            for (int pass = 0; pass < 8; pass++)
            {
                int c = pts.Count;
                var q = new List<Vector2>(c);
                for (int i = 0; i < c; i++)
                    q.Add((pts[(i - 1 + c) % c] + pts[i] * 2f + pts[(i + 1) % c]) * 0.25f);
                pts = q;
            }

            // sentido horario: desde North End se baja por la costa oriental (San Luis)
            float area = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                area += a.x * b.y - b.x * a.y;
            }
            if (area > 0f) pts.Reverse();

            var res = ResampleClosed(pts, 4f);
            int m = res.Count;
            var ys = new float[m];
            for (int i = 0; i < m; i++) ys[i] = BaseHeight(d, res[i].x, res[i].y);
            for (int pass = 0; pass < 4; pass++)
            {
                var tmp = new float[m];
                for (int i = 0; i < m; i++)
                {
                    float sum = 0f;
                    for (int k = -6; k <= 6; k++) sum += ys[(i + k + m) % m];
                    tmp[i] = sum / 13f;
                }
                ys = tmp;
            }

            d.road.Clear(); d.roadTangent.Clear();
            for (int i = 0; i < m; i++) d.road.Add(new Vector3(res[i].x, Mathf.Max(1.6f, ys[i]), res[i].y));
            for (int i = 0; i < m; i++)
            {
                Vector3 t = d.road[(i + 1) % m] - d.road[(i - 1 + m) % m];
                t.y = 0f;
                d.roadTangent.Add(t.normalized);
            }
            d.BuildRoadBuckets();
        }

        static float Islet(float x, float z, Vector4 c)
        {
            float dist = Mathf.Sqrt((x - c.x) * (x - c.x) + (z - c.y) * (z - c.y));
            if (dist < c.z)
            {
                float k = dist / c.z;
                return 0.2f + c.w * (1f - k * k);
            }
            return 0.2f - (dist - c.z) * 0.06f;
        }

        static float LomaX(float z) => -10f + z * 0.02f;

        /// <summary>Altura natural del terreno (sin aplanar vía ni pista).</summary>
        public static float BaseHeight(IslandData d, float x, float z)
        {
            float sd = d.SignedDistance(x, z);
            float n1 = Mathf.PerlinNoise(x * 0.012f + 31.7f, z * 0.012f + 11.3f);
            float n2 = Mathf.PerlinNoise(x * 0.04f + 5.1f, z * 0.04f + 77.7f);
            float h;
            if (sd < 0f)
            {
                float off = -sd;
                float depth = 0.35f + off * 0.075f + (off > 40f ? (off - 40f) * 0.05f : 0f);
                depth = Mathf.Min(depth, 16f);
                depth += (n2 - 0.5f) * 1.2f * Mathf.Clamp01(off / 30f);
                h = -depth;
                // barrera coralina al oriente y al norte: franja de aguas bajas
                float east = Mathf.Clamp01(Mathf.Clamp01((x - 30f) / 80f) + Mathf.Clamp01((z - 600f) / 120f));
                float reef = Mathf.Exp(-Mathf.Pow((sd + 120f) / 14f, 2f)) * east;
                h = Mathf.Lerp(h, -0.35f - n2 * 0.4f, reef);
            }
            else
            {
                float beach = Mathf.Min(sd, 14f) * 0.12f;
                float inland = Mathf.Max(0f, sd - 14f);
                float rise = Mathf.Min(inland * 0.12f, 6f);
                float hills = Mathf.Max((n1 - 0.4f) * 7f * Mathf.Clamp01(inland / 40f), -1f);
                float dx = x - LomaX(z);
                float ridge = 34f * Mathf.Exp(-(dx * dx) / (95f * 95f)) * Mathf.Exp(-Mathf.Pow((z + 60f) / 430f, 2f));
                ridge *= Mathf.Clamp01(inland / 70f);
                h = 0.15f + beach + rise + hills + ridge + (n2 - 0.5f) * 0.6f * Mathf.Clamp01(inland / 10f);
            }
            h = Mathf.Max(h, Islet(x, z, JohnnyCay));
            h = Mathf.Max(h, Islet(x, z, HaynesCay));
            h = Mathf.Max(h, Islet(x, z, Acuario));
            h = Mathf.Max(h, Islet(x, z, RockyCay));
            return h;
        }

        static float SmoothRange(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        public static float RectDistance(Rect r, float x, float z)
        {
            float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
            float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static void BuildHeights(IslandData d)
        {
            d.heights = new float[d.nx, d.nz];
            for (int iz = 0; iz < d.nz; iz++)
            {
                float z = d.minZ + iz * d.CellZ;
                for (int ix = 0; ix < d.nx; ix++)
                {
                    float x = d.minX + ix * d.CellX;
                    float h = BaseHeight(d, x, z);
                    float dr = d.DistToRoad(new Vector2(x, z), out float ry, out _);
                    if (dr < 30f)
                    {
                        float w = 1f - SmoothRange(d.HalfRoad + 3f, d.HalfRoad + 16f, dr);
                        h = Mathf.Lerp(h, ry - 0.25f, w);
                    }
                    float rd = RectDistance(d.runway, x, z);
                    if (rd < 16f)
                    {
                        float w = 1f - SmoothRange(0f, 16f, rd);
                        h = Mathf.Lerp(h, d.runwayHeight - 0.05f, w);
                    }
                    d.heights[ix, iz] = h;
                }
            }
        }

        // ------------------------------------------------------------------ colores

        static readonly float[] SeaDepth = { 0f, 0.35f, 1.2f, 2.6f, 4.5f, 7f, 10f, 14f };
        static readonly Color[] SeaCol =
        {
            MatLib.Hex(0xEAFFF9), MatLib.Hex(0xC4F6EB), MatLib.Hex(0x7FE6D9), MatLib.Hex(0x3DD2D0),
            MatLib.Hex(0x19ABCB), MatLib.Hex(0x1179B9), MatLib.Hex(0x0D4F97), MatLib.Hex(0x0A2E66)
        };
        public static Color DeepSea => SeaCol[SeaCol.Length - 1];

        /// <summary>El mar de los siete colores: el color depende de la profundidad.</summary>
        public static Color SeaColor(float depth)
        {
            if (depth <= SeaDepth[0]) return SeaCol[0];
            for (int i = 1; i < SeaDepth.Length; i++)
                if (depth < SeaDepth[i])
                    return Color.Lerp(SeaCol[i - 1], SeaCol[i], (depth - SeaDepth[i - 1]) / (SeaDepth[i] - SeaDepth[i - 1]));
            return SeaCol[SeaCol.Length - 1];
        }

        public static Color LandColor(float h, float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.035f + 3f, z * 0.035f + 9f);
            float n2 = Mathf.PerlinNoise(x * 0.11f, z * 0.11f);
            Color wetSand = new Color(0.84f, 0.77f, 0.58f), sand = new Color(0.95f, 0.89f, 0.70f);
            Color grass = new Color(0.38f, 0.64f, 0.27f), forest = new Color(0.20f, 0.46f, 0.18f);
            Color dry = new Color(0.62f, 0.65f, 0.34f);
            Color c;
            if (h < 1.7f)
            {
                c = Color.Lerp(wetSand, sand, Mathf.Clamp01(h / 0.9f)) * (0.96f + 0.08f * n2);
            }
            else
            {
                Color veg = Color.Lerp(grass, forest, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.75f, n)));
                veg = Color.Lerp(veg, dry, Mathf.Clamp01((n2 - 0.6f) * 2f) * 0.6f);
                veg = Color.Lerp(veg, forest * 0.85f, Mathf.Clamp01((h - 18f) / 20f));
                c = Color.Lerp(sand, veg, Mathf.Clamp01((h - 1.7f) / 0.9f)) * (0.92f + 0.12f * n2);
            }
            c.a = 1f;
            return c;
        }

        static Texture2D BuildMinimap(IslandData d)
        {
            int w = 128, h = Mathf.RoundToInt(128 * d.SizeZ / d.SizeX);
            var tex = MatLib.NewTexture(w, h, false);
            var px = new Color[w * h];
            for (int py = 0; py < h; py++)
                for (int pxi = 0; pxi < w; pxi++)
                {
                    float x = Mathf.Lerp(d.minX, d.maxX, (pxi + 0.5f) / w);
                    float z = Mathf.Lerp(d.minZ, d.maxZ, (py + 0.5f) / h);
                    float hh = d.SampleHeight(x, z);
                    Color c = hh < 0f ? SeaColor(-hh) : LandColor(hh, x, z);
                    if (RectDistance(d.runway, x, z) <= 0f) c = new Color(0.35f, 0.35f, 0.38f);
                    c.a = 0.92f;
                    px[py * w + pxi] = c;
                }
            Color roadC = new Color(0.15f, 0.15f, 0.17f, 1f);
            foreach (var p in d.road)
            {
                int cx = Mathf.FloorToInt((p.x - d.minX) / d.SizeX * w);
                int cy = Mathf.FloorToInt((p.z - d.minZ) / d.SizeZ * h);
                for (int oy = 0; oy <= 1; oy++)
                    for (int ox = 0; ox <= 1; ox++)
                    {
                        int xx = cx + ox, yy = cy + oy;
                        if (xx >= 0 && yy >= 0 && xx < w && yy < h) px[yy * w + xx] = roadC;
                    }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ mallas

        public static void BuildGround(IslandData d, Transform root)
        {
            // Terreno
            int nx = d.nx, nz = d.nz;
            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    int i = iz * nx + ix;
                    verts[i] = new Vector3(d.minX + ix * d.CellX, d.heights[ix, iz], d.minZ + iz * d.CellZ);
                    uvs[i] = new Vector2(ix / (nx - 1f), iz / (nz - 1f));
                }
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int iz = 0; iz < nz - 1; iz++)
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int a = iz * nx + ix, b = a + nx, c = a + 1, e = b + 1;
                    tris[t++] = a; tris[t++] = b; tris[t++] = c;
                    tris[t++] = b; tris[t++] = e; tris[t++] = c;
                }
            var mesh = new Mesh { name = "Terreno San Andrés", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var terrain = new GameObject("Terreno");
            terrain.transform.SetParent(root, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
            terrain.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Textured(TerrainTexture(d), 0.05f);
            terrain.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Mar de los siete colores
            var sea = new GameObject("Mar de los siete colores");
            sea.transform.SetParent(root, false);
            sea.AddComponent<MeshFilter>().sharedMesh = MatLib.Quad(d.minX, d.minZ, d.maxX, d.maxZ, 0f);
            var seaR = sea.AddComponent<MeshRenderer>();
            seaR.sharedMaterial = MatLib.Textured(SeaTexture(d), 0.88f);
            seaR.shadowCastingMode = ShadowCastingMode.Off;

            var deep = MatLib.Solid(DeepSea, 0.88f);
            const float big = 5000f;
            AddOcean(root, deep, d.minX - big, d.minZ - big, d.minX, d.maxZ + big);
            AddOcean(root, deep, d.maxX, d.minZ - big, d.maxX + big, d.maxZ + big);
            AddOcean(root, deep, d.minX, d.minZ - big, d.maxX, d.minZ);
            AddOcean(root, deep, d.minX, d.maxZ, d.maxX, d.maxZ + big);

            BuildRoadMesh(d, root);
            BuildRunwayMesh(d, root);
        }

        static void AddOcean(Transform root, Material mat, float x0, float z0, float x1, float z1)
        {
            var go = new GameObject("Océano");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = MatLib.Quad(x0, z0, x1, z1, 0f);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        static Texture2D TerrainTexture(IslandData d)
        {
            int w = 512, h = 1024;
            var tex = MatLib.NewTexture(w, h);
            var px = new Color[w * h];
            for (int py = 0; py < h; py++)
            {
                float z = Mathf.Lerp(d.minZ, d.maxZ, py / (h - 1f));
                for (int pxi = 0; pxi < w; pxi++)
                {
                    float x = Mathf.Lerp(d.minX, d.maxX, pxi / (w - 1f));
                    float hh = d.SampleHeight(x, z);
                    px[py * w + pxi] = hh < 0f ? new Color(0.84f, 0.77f, 0.58f) : LandColor(hh, x, z);
                }
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static Texture2D SeaTexture(IslandData d)
        {
            int w = 256, h = 512;
            var tex = MatLib.NewTexture(w, h);
            var px = new Color[w * h];
            for (int py = 0; py < h; py++)
            {
                float z = Mathf.Lerp(d.minZ, d.maxZ, py / (h - 1f));
                for (int pxi = 0; pxi < w; pxi++)
                {
                    float x = Mathf.Lerp(d.minX, d.maxX, pxi / (w - 1f));
                    float hh = d.SampleHeight(x, z);
                    float n = Mathf.PerlinNoise(x * 0.06f + 40f, z * 0.06f + 13f);
                    Color c = SeaColor(Mathf.Max(0f, -hh) + (n - 0.5f) * 0.5f);
                    c.a = 1f;
                    px[py * w + pxi] = c;
                }
            }
            // el borde debe coincidir con el océano profundo
            for (int py = 0; py < h; py++) { px[py * w] = DeepSea; px[py * w + w - 1] = DeepSea; }
            for (int pxi = 0; pxi < w; pxi++) { px[pxi] = DeepSea; px[(h - 1) * w + pxi] = DeepSea; }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static Texture2D RoadTexture()
        {
            int w = 64, h = 128;
            var tex = MatLib.NewTexture(w, h);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 8;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float g = 0.22f + Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.06f;
                    Color c = new Color(g, g, g * 1.04f);
                    if ((u > 0.04f && u < 0.075f) || (u > 0.925f && u < 0.96f)) c = new Color(0.92f, 0.92f, 0.9f);
                    if (u > 0.48f && u < 0.52f && v < 0.5f) c = new Color(0.98f, 0.78f, 0.12f);
                    if (u < 0.025f || u > 0.975f) c = new Color(0.7f, 0.66f, 0.55f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static void BuildRoadMesh(IslandData d, Transform root)
        {
            int n = d.road.Count;
            float half = d.HalfRoad;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float dist = 0f;
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                Vector3 c = d.road[k];
                Vector3 r = d.RoadRight(k);
                if (i > 0) dist += Vector3.Distance(d.road[(i - 1) % n], c);
                float v = dist / 12f;
                Vector3 L = c - r * half, R = c + r * half;
                // 0 = faldón izq. abajo, 1 = faldón izq. arriba, 2 = calzada izq.,
                // 3 = calzada der., 4 = faldón der. arriba, 5 = faldón der. abajo
                verts.Add(L + Vector3.down * 0.6f); uvs.Add(new Vector2(0f, v));
                verts.Add(L); uvs.Add(new Vector2(0f, v));
                verts.Add(L); uvs.Add(new Vector2(0f, v));
                verts.Add(R); uvs.Add(new Vector2(1f, v));
                verts.Add(R); uvs.Add(new Vector2(1f, v));
                verts.Add(R + Vector3.down * 0.6f); uvs.Add(new Vector2(1f, v));
                if (i > 0)
                {
                    int p = (i - 1) * 6, q = i * 6;
                    // calzada
                    tris.Add(p + 2); tris.Add(q + 2); tris.Add(p + 3);
                    tris.Add(q + 2); tris.Add(q + 3); tris.Add(p + 3);
                    // faldón izquierdo
                    tris.Add(p + 0); tris.Add(q + 1); tris.Add(p + 1);
                    tris.Add(p + 0); tris.Add(q + 0); tris.Add(q + 1);
                    // faldón derecho
                    tris.Add(p + 5); tris.Add(p + 4); tris.Add(q + 4);
                    tris.Add(p + 5); tris.Add(q + 4); tris.Add(q + 5);
                }
            }
            var mesh = new Mesh { name = "Vía Circunvalar", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Vía Circunvalar");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Textured(RoadTexture(), 0.25f);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void BuildRunwayMesh(IslandData d, Transform root)
        {
            int w = 512, h = 32;
            var tex = MatLib.NewTexture(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float g = 0.27f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.05f;
                    Color c = new Color(g, g, g);
                    bool edge = y == 1 || y == 2 || y == h - 2 || y == h - 3;
                    bool center = (y == 15 || y == 16) && (x % 32) < 18 && x > 30 && x < w - 30;
                    bool threshold = (x < 18 || x > w - 19) && (y > 4 && y < h - 5) && (y % 4) < 2;
                    if (edge || center || threshold) c = new Color(0.95f, 0.95f, 0.95f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);

            var r = d.runway;
            var go = new GameObject("Pista del aeropuerto");
            go.transform.SetParent(root, false);
            // u recorre x (largo de la pista), v recorre z (ancho)
            go.AddComponent<MeshFilter>().sharedMesh = MatLib.Quad(r.xMin, r.yMin, r.xMax, r.yMax, d.runwayHeight + 0.02f);
            go.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Textured(tex, 0.2f);
        }
    }
}
