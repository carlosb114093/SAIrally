using System.Collections.Generic;
using UnityEngine;

namespace SanAndres
{
    public class Landmark
    {
        public string name;
        public Vector3 position;
        public Landmark(string name, Vector3 position) { this.name = name; this.position = position; }
    }

    /// <summary>
    /// Datos de la isla generada: alturas, contorno de la costa, vía Circunvalar y puntos de interés.
    /// Ejes: X = este, Z = norte. Escala aproximada 1 unidad = 1 m, isla reducida a ~1:9
    /// (la real mide ~12,5 x 3 km; aquí ~1,4 x 0,37 km para que la vuelta dure un par de minutos).
    /// </summary>
    public class IslandData
    {
        public float minX = -440f, maxX = 440f, minZ = -880f, maxZ = 900f;
        public int nx = 177, nz = 357;
        public float[,] heights;
        public Vector2[] outline;
        public readonly List<Vector3> road = new List<Vector3>();
        public readonly List<Vector3> roadTangent = new List<Vector3>();
        public float roadWidth = 11f;
        public Rect runway;
        public float runwayHeight;
        public readonly List<Landmark> landmarks = new List<Landmark>();
        public Texture2D minimap;

        public float CellX => (maxX - minX) / (nx - 1);
        public float CellZ => (maxZ - minZ) / (nz - 1);
        public float HalfRoad => roadWidth * 0.5f;
        public float SizeX => maxX - minX;
        public float SizeZ => maxZ - minZ;

        const float Bucket = 32f;
        Dictionary<long, List<int>> roadBuckets;

        public float SampleHeight(float x, float z)
        {
            float fx = (x - minX) / CellX, fz = (z - minZ) / CellZ;
            int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, nx - 2);
            int iz = Mathf.Clamp(Mathf.FloorToInt(fz), 0, nz - 2);
            float tx = Mathf.Clamp01(fx - ix), tz = Mathf.Clamp01(fz - iz);
            float h00 = heights[ix, iz], h10 = heights[ix + 1, iz];
            float h01 = heights[ix, iz + 1], h11 = heights[ix + 1, iz + 1];
            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
        }

        /// <summary>Distancia con signo a la costa: positiva dentro de la isla, negativa en el mar.</summary>
        public float SignedDistance(float x, float z)
        {
            float best = float.MaxValue;
            bool inside = false;
            var o = outline;
            int n = o.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 a = o[j], b = o[i];
                if (((b.y > z) != (a.y > z)) && (x < (a.x - b.x) * (z - b.y) / (a.y - b.y) + b.x))
                    inside = !inside;
                float abx = b.x - a.x, abz = b.y - a.y;
                float len2 = abx * abx + abz * abz;
                float t = len2 > 1e-6f ? Mathf.Clamp01(((x - a.x) * abx + (z - a.y) * abz) / len2) : 0f;
                float dx = a.x + abx * t - x, dz = a.y + abz * t - z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best) best = d2;
            }
            float dist = Mathf.Sqrt(best);
            return inside ? dist : -dist;
        }

        static long Key(int ix, int iz) => ((long)ix << 32) ^ (uint)iz;

        public void BuildRoadBuckets()
        {
            roadBuckets = new Dictionary<long, List<int>>();
            int n = road.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = road[i], b = road[(i + 1) % n];
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - Bucket) / Bucket);
                int x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + Bucket) / Bucket);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.z, b.z) - Bucket) / Bucket);
                int z1 = Mathf.FloorToInt((Mathf.Max(a.z, b.z) + Bucket) / Bucket);
                for (int ix = x0; ix <= x1; ix++)
                    for (int iz = z0; iz <= z1; iz++)
                    {
                        long k = Key(ix, iz);
                        if (!roadBuckets.TryGetValue(k, out var list)) roadBuckets[k] = list = new List<int>();
                        list.Add(i);
                    }
            }
        }

        /// <summary>
        /// Distancia horizontal al eje de la vía. Exacta hasta 32 m; más allá puede devolver 999.
        /// roadY es la altura de la calzada en el punto más cercano.
        /// </summary>
        public float DistToRoad(Vector2 p, out float roadY, out int segment)
        {
            roadY = 0f; segment = -1;
            if (roadBuckets == null) return 999f;
            long k = Key(Mathf.FloorToInt(p.x / Bucket), Mathf.FloorToInt(p.y / Bucket));
            if (!roadBuckets.TryGetValue(k, out var list)) return 999f;
            float best = float.MaxValue;
            int n = road.Count;
            foreach (int i in list)
            {
                Vector3 a = road[i], b = road[(i + 1) % n];
                float abx = b.x - a.x, abz = b.z - a.z;
                float len2 = abx * abx + abz * abz;
                float t = len2 > 1e-6f ? Mathf.Clamp01(((p.x - a.x) * abx + (p.y - a.z) * abz) / len2) : 0f;
                float dx = a.x + abx * t - p.x, dz = a.z + abz * t - p.y;
                float d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; segment = i; roadY = Mathf.Lerp(a.y, b.y, t); }
            }
            return Mathf.Sqrt(best);
        }

        public float DistToRoad(float x, float z) => DistToRoad(new Vector2(x, z), out _, out _);

        public int NearestRoadIndex(Vector2 p)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < road.Count; i++)
            {
                float dx = road[i].x - p.x, dz = road[i].z - p.y;
                float d = dx * dx + dz * dz;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        public Vector3 RoadPoint(int i) => road[((i % road.Count) + road.Count) % road.Count];
        public Vector3 RoadTangent(int i) => roadTangent[((i % road.Count) + road.Count) % road.Count];
        public Vector3 RoadRight(int i) { var t = RoadTangent(i); return new Vector3(t.z, 0f, -t.x); }

        /// <summary>Punto de la costa más cercano a 'near' y la normal hacia el interior de la isla.</summary>
        public Vector2 CoastPoint(Vector2 near, out Vector2 inward)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < outline.Length; i++)
            {
                float d = (outline[i] - near).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            inward = InwardNormal(best);
            return outline[best];
        }

        public Vector2 InwardNormal(int i)
        {
            int n = outline.Length;
            Vector2 t = (outline[(i + 1) % n] - outline[(i - 1 + n) % n]).normalized;
            Vector2 nr = new Vector2(-t.y, t.x);
            Vector2 p = outline[i] + nr * 4f;
            if (SignedDistance(p.x, p.y) < 0f) nr = -nr;
            return nr;
        }

        public Vector2 WorldToMap01(Vector3 w) => new Vector2((w.x - minX) / SizeX, (w.z - minZ) / SizeZ);
    }
}
