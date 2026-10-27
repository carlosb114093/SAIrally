using System.Collections.Generic;
using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Coloca los sitios emblemáticos de San Andrés y la vegetación:
    /// North End / El Centro y Spratt Bight, aeropuerto Gustavo Rojas Pinilla, Johnny Cay,
    /// Rocky Cay con el barco hundido, San Luis, Haynes Cay y El Acuario, La Loma y la
    /// Primera Iglesia Bautista, Hoyo Soplador, West View, Cueva de Morgan y El Cove.
    /// </summary>
    public static class WorldDecor
    {
        static System.Random rng;
        static readonly List<Vector3> occupied = new List<Vector3>(); // x, radio, z

        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        public static void Build(IslandData d, Transform root, GameObject palmPrefab, GameObject[] housePrefabs, int seed)
        {
            rng = new System.Random(seed);
            occupied.Clear();
            var props = new GameObject("Decoración").transform;
            props.SetParent(root, false);

            Airport(d, Group(props, "Aeropuerto"));
            Church(d, Group(props, "La Loma"));
            HoyoSoplador(d, Group(props, "Hoyo Soplador"));
            MorganCave(d, Group(props, "Cueva de Morgan"));
            ElCove(d, Group(props, "El Cove"));
            WestView(d, Group(props, "West View"));
            RockyCay(d, Group(props, "Rocky Cay"));
            Cays(d, Group(props, "Cayos"), palmPrefab);
            SprattBight(d, Group(props, "Spratt Bight"));
            Towns(d, Group(props, "Pueblos"), housePrefabs);
            Vegetation(d, Group(props, "Vegetación"), palmPrefab);

            d.landmarks.Add(new Landmark("North End · El Centro", new Vector3(40f, 0f, 650f)));
            d.landmarks.Add(new Landmark("San Luis", new Vector3(170f, 0f, 160f)));
            d.landmarks.Add(new Landmark("South End", new Vector3(0f, 0f, -640f)));
            d.landmarks.Add(new Landmark("La Loma", new Vector3(-10f, 0f, -150f)));
        }

        static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        static bool Free(IslandData d, float x, float z, float radius, float roadClear)
        {
            if (d.DistToRoad(x, z) < roadClear) return false;
            Rect r = d.runway;
            if (x > r.xMin - 10f && x < r.xMax + 10f && z > r.yMin - 10f && z < r.yMax + 10f) return false;
            foreach (var o in occupied)
            {
                float dx = o.x - x, dz = o.z - z, rr = o.y + radius;
                if (dx * dx + dz * dz < rr * rr) return false;
            }
            return true;
        }

        static void Occupy(float x, float z, float radius) => occupied.Add(new Vector3(x, radius, z));

        static float YawToRoad(IslandData d, Vector2 p)
        {
            Vector3 rp = d.RoadPoint(d.NearestRoadIndex(p));
            return Mathf.Atan2(rp.x - p.x, rp.z - p.y) * Mathf.Rad2Deg;
        }

        static float MinGround(IslandData d, float x, float z, float rad)
        {
            float m = d.SampleHeight(x, z);
            m = Mathf.Min(m, d.SampleHeight(x + rad, z), d.SampleHeight(x - rad, z));
            m = Mathf.Min(m, d.SampleHeight(x, z + rad), d.SampleHeight(x, z - rad));
            return m;
        }

        // ------------------------------------------------------------ aeropuerto

        static void Airport(IslandData d, Transform g)
        {
            var r = d.runway;
            float y = d.runwayHeight;
            d.landmarks.Add(new Landmark("Aeropuerto Gustavo Rojas Pinilla", new Vector3(r.center.x, y, r.center.y)));

            // terminal
            Vector2 tp = new Vector2(45f, 562f);
            float ty = MinGround(d, tp.x, tp.y, 24f);
            var term = Props.Root(g, "Terminal", new Vector3(tp.x, ty, tp.y), 180f);
            Props.Part(term.transform, PrimitiveType.Cube, new Vector3(0f, 3f, 0f), Vector3.zero, new Vector3(48f, 10f, 16f), MatLib.Solid(new Color(0.95f, 0.95f, 0.93f)), true);
            Props.Part(term.transform, PrimitiveType.Cube, new Vector3(0f, 4.5f, 0f), Vector3.zero, new Vector3(48.2f, 2.2f, 16.2f), MatLib.Solid(new Color(0.2f, 0.45f, 0.65f), 0.85f));
            Props.Part(term.transform, PrimitiveType.Cube, new Vector3(0f, 8.2f, 0f), Vector3.zero, new Vector3(50f, 0.5f, 18f), MatLib.Solid(new Color(0.25f, 0.65f, 0.75f)));
            Occupy(tp.x, tp.y, 28f);

            Vector2 cp = new Vector2(88f, 556f);
            float cy = d.SampleHeight(cp.x, cp.y);
            var tower = Props.Root(g, "Torre de control", new Vector3(cp.x, cy, cp.y), 0f);
            Props.Part(tower.transform, PrimitiveType.Cylinder, new Vector3(0f, 8f, 0f), Vector3.zero, new Vector3(3.5f, 9f, 3.5f), MatLib.Solid(Color.white), true);
            Props.Part(tower.transform, PrimitiveType.Cylinder, new Vector3(0f, 17.8f, 0f), Vector3.zero, new Vector3(6f, 1.4f, 6f), MatLib.Solid(new Color(0.2f, 0.45f, 0.65f), 0.9f));
            Props.MeshPart(tower.transform, MatLib.Cone(12), new Vector3(0f, 19.2f, 0f), Vector3.zero, new Vector3(3.4f, 1.6f, 3.4f), MatLib.Solid(new Color(0.85f, 0.2f, 0.2f)));
            Occupy(cp.x, cp.y, 6f);

            // avión estacionado en la cabecera occidental
            var plane = Props.Root(g, "Avión", new Vector3(r.xMin + 38f, y, r.center.y), 90f);
            var t = plane.transform;
            var white = MatLib.Solid(new Color(0.97f, 0.97f, 0.97f), 0.5f);
            Props.Part(t, PrimitiveType.Capsule, new Vector3(0f, 2.8f, 0f), new Vector3(90f, 0f, 0f), new Vector3(2.4f, 8f, 2.4f), white);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 2.3f, 0.5f), Vector3.zero, new Vector3(22f, 0.35f, 3.2f), white);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 4.8f, -6.8f), Vector3.zero, new Vector3(0.3f, 3.4f, 2.4f), MatLib.Solid(new Color(0.15f, 0.3f, 0.75f)));
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 3.1f, -7f), Vector3.zero, new Vector3(7f, 0.25f, 1.8f), white);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 3.0f, 0f), Vector3.zero, new Vector3(2.45f, 0.35f, 13f), MatLib.Solid(new Color(1f, 0.82f, 0.1f)));
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 3.5f, 7.1f), Vector3.zero, new Vector3(1.6f, 0.45f, 0.8f), MatLib.Solid(new Color(0.1f, 0.12f, 0.18f), 0.9f));
            foreach (float side in new[] { -4.5f, 4.5f })
                Props.Part(t, PrimitiveType.Cylinder, new Vector3(side, 1.6f, 1.2f), new Vector3(90f, 0f, 0f), new Vector3(1.1f, 1.2f, 1.1f), MatLib.Solid(new Color(0.75f, 0.75f, 0.78f), 0.6f, 0.5f));
            foreach (var p in new[] { new Vector3(0f, 0.8f, 5.5f), new Vector3(-2f, 0.8f, 0f), new Vector3(2f, 0.8f, 0f) })
                Props.Part(t, PrimitiveType.Cylinder, p, new Vector3(0f, 0f, 90f), new Vector3(0.9f, 0.25f, 0.9f), MatLib.Solid(new Color(0.1f, 0.1f, 0.1f)));
            var bc = plane.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 2.6f, 0f); bc.size = new Vector3(3f, 3.8f, 17f);
            var wc = plane.AddComponent<BoxCollider>();
            wc.center = new Vector3(0f, 2.3f, 0.5f); wc.size = new Vector3(22f, 0.6f, 3.2f);
        }

        // ------------------------------------------------------------ La Loma

        static void Church(IslandData d, Transform g)
        {
            float bestH = float.MinValue; Vector2 best = new Vector2(-10f, -60f);
            for (int iz = 0; iz < d.nz; iz += 2)
                for (int ix = 0; ix < d.nx; ix += 2)
                {
                    float h = d.heights[ix, iz];
                    if (h <= bestH) continue;
                    float x = d.minX + ix * d.CellX, z = d.minZ + iz * d.CellZ;
                    if (d.DistToRoad(x, z) < 26f || d.SignedDistance(x, z) < 45f) continue;
                    bestH = h; best = new Vector2(x, z);
                }
            float gy = MinGround(d, best.x, best.y, 12f);
            float yaw = YawToRoad(d, best);
            var church = Props.Root(g, "Primera Iglesia Bautista", new Vector3(best.x, gy, best.y), yaw);
            var t = church.transform;
            var white = MatLib.Solid(new Color(0.97f, 0.97f, 0.95f));
            var roof = MatLib.Solid(new Color(0.65f, 0.15f, 0.12f), 0.3f);
            var dark = MatLib.Solid(new Color(0.12f, 0.2f, 0.35f), 0.8f);
            const float H = 9f;
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, H * 0.5f - 1f, 0f), Vector3.zero, new Vector3(10f, H + 2f, 20f), white, true);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, H, 0f), new Vector3(0f, 0f, 45f), new Vector3(7.2f, 7.2f, 20.6f), roof);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 7f, 12.25f), Vector3.zero, new Vector3(4.5f, 18f, 4.5f), white, true);
            Props.MeshPart(t, MatLib.Cone(4), new Vector3(0f, 16f, 12.25f), new Vector3(0f, 45f, 0f), new Vector3(3.4f, 7f, 3.4f), roof);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 23.8f, 12.25f), Vector3.zero, new Vector3(0.18f, 1.6f, 0.18f), white);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 24.1f, 12.25f), Vector3.zero, new Vector3(0.9f, 0.18f, 0.18f), white);
            foreach (float side in new[] { -1f, 1f })
            {
                for (float z = -6f; z <= 6.1f; z += 4f)
                    Props.Part(t, PrimitiveType.Cube, new Vector3(side * 5.03f, 4f, z), Vector3.zero, new Vector3(0.06f, 2.8f, 1.1f), dark);
                Props.Part(t, PrimitiveType.Cube, new Vector3(side * 2.27f, 13f, 12.25f), Vector3.zero, new Vector3(0.06f, 2.2f, 1.2f), dark);
            }
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 1.75f, 14.55f), Vector3.zero, new Vector3(2f, 3.5f, 0.1f), MatLib.Solid(Props.DarkWood));
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 13f, 14.53f), Vector3.zero, new Vector3(1.2f, 2.2f, 0.06f), dark);
            Occupy(best.x, best.y, 20f);
            d.landmarks.Add(new Landmark("La Loma · Primera Iglesia Bautista", new Vector3(best.x, gy, best.y)));
        }

        // ------------------------------------------------------------ Hoyo Soplador

        static void HoyoSoplador(IslandData d, Transform g)
        {
            Vector2 p = d.CoastPoint(new Vector2(55f, -615f), out Vector2 inw);
            Vector2 c = p + inw * 9f;
            float y = Mathf.Max(d.SampleHeight(c.x, c.y), 0.5f);
            var root = Props.Root(g, "Hoyo Soplador", new Vector3(c.x, y, c.y), 0f);
            float seaAngle = Mathf.Atan2(-inw.y, -inw.x) * Mathf.Rad2Deg;
            Props.RockCluster(root.transform, root.transform.position, 14, 12f, rng, seaAngle - 100f, seaAngle + 100f, 5f);
            Props.Part(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(2.4f, 0.05f, 2.4f), MatLib.Solid(new Color(0.08f, 0.1f, 0.12f)));
            var foam = MatLib.Solid(new Color(0.9f, 0.97f, 1f), 0.6f, 0f, true);
            var column = Props.Part(root.transform, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(1.3f, 0.01f, 1.3f), foam);
            column.name = "Chorro";
            var drops = new Transform[16];
            for (int i = 0; i < drops.Length; i++)
                drops[i] = Props.Part(root.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.7f, foam).transform;
            var hs = root.AddComponent<HoyoSoplador>();
            hs.column = column.transform;
            hs.drops = drops;
            Occupy(c.x, c.y, 13f);
            d.landmarks.Add(new Landmark("Hoyo Soplador", root.transform.position));
        }

        // ------------------------------------------------------------ Cueva de Morgan

        static void MorganCave(IslandData d, Transform g)
        {
            Vector2 p = d.CoastPoint(new Vector2(-160f, -65f), out Vector2 inw);
            Vector2 c = p + inw * 10f;
            float y = MinGround(d, c.x, c.y, 6f);
            var root = Props.Root(g, "Cueva de Morgan", new Vector3(c.x, y, c.y), YawToRoad(d, c));
            var t = root.transform;
            var mound = Props.Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.5f, -1f), Vector3.zero, new Vector3(16f, 9f, 12f), MatLib.Solid(new Color(0.45f, 0.40f, 0.33f), 0.1f));
            mound.AddComponent<MeshCollider>().sharedMesh = MatLib.Primitive(PrimitiveType.Sphere);
            Props.RockCluster(t, t.position + t.forward * -2f, 6, 10f, rng, 0f, 360f, 6f);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 1.6f, 4.6f), Vector3.zero, new Vector3(4f, 3.5f, 2f), MatLib.Solid(new Color(0.03f, 0.03f, 0.03f)));
            var flame = MatLib.Solid(new Color(1f, 0.55f, 0.1f), 0.2f, 0f, true);
            foreach (float side in new[] { -2.7f, 2.7f })
            {
                Props.Part(t, PrimitiveType.Cylinder, new Vector3(side, 1f, 6f), Vector3.zero, new Vector3(0.18f, 1f, 0.18f), MatLib.Solid(Props.Wood));
                Props.Part(t, PrimitiveType.Sphere, new Vector3(side, 2.15f, 6f), Vector3.zero, new Vector3(0.45f, 0.6f, 0.45f), flame);
            }
            // cofre del tesoro
            Props.Part(t, PrimitiveType.Cube, new Vector3(3.4f, 0.4f, 6.6f), new Vector3(0f, 20f, 0f), new Vector3(1.2f, 0.8f, 0.8f), MatLib.Solid(Props.DarkWood), true);
            Props.Part(t, PrimitiveType.Cube, new Vector3(3.4f, 0.85f, 6.6f), new Vector3(0f, 20f, 0f), new Vector3(1.0f, 0.2f, 0.6f), MatLib.Solid(new Color(1f, 0.8f, 0.2f), 0.7f, 0.8f, true));
            // bandera pirata
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(2f, 7.5f, -1f), Vector3.zero, new Vector3(0.12f, 3f, 0.12f), MatLib.Solid(Props.Wood));
            Props.Part(t, PrimitiveType.Cube, new Vector3(3.1f, 9.8f, -1f), Vector3.zero, new Vector3(2.2f, 1.4f, 0.05f), MatLib.Solid(new Color(0.08f, 0.08f, 0.08f)));
            Props.Part(t, PrimitiveType.Sphere, new Vector3(3.1f, 9.9f, -0.97f), Vector3.zero, new Vector3(0.5f, 0.5f, 0.05f), MatLib.Solid(Color.white));
            // el barco de Henry Morgan fondeado frente a la cueva
            Vector2 s = p - inw * 38f;
            float coastYaw = Mathf.Atan2(inw.y, -inw.x) * Mathf.Rad2Deg; // paralelo a la costa
            Props.PirateShip(g, new Vector3(s.x, 0f, s.y), coastYaw);
            Occupy(c.x, c.y, 14f);
            d.landmarks.Add(new Landmark("Cueva de Morgan", new Vector3(c.x, y, c.y)));
        }

        // ------------------------------------------------------------ El Cove

        static void ElCove(IslandData d, Transform g)
        {
            Vector2 p = d.CoastPoint(new Vector2(-176f, 140f), out Vector2 inw);
            Vector2 outDir = -inw;
            Vector2 mid = p + outDir * 13f;
            float yaw = Mathf.Atan2(outDir.x, outDir.y) * Mathf.Rad2Deg;
            var dock = Props.Root(g, "Muelle de El Cove", new Vector3(mid.x, 0f, mid.y), yaw);
            var wood = MatLib.Solid(Props.Wood);
            Props.Part(dock.transform, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), Vector3.zero, new Vector3(3.2f, 0.35f, 32f), wood, true);
            for (float z = -14f; z <= 15f; z += 4f)
                foreach (float x in new[] { -1.5f, 1.5f })
                    Props.Part(dock.transform, PrimitiveType.Cylinder, new Vector3(x, -0.5f, z), Vector3.zero, new Vector3(0.3f, 1.5f, 0.3f), MatLib.Solid(Props.DarkWood));
            Vector2 tan = new Vector2(inw.y, -inw.x);
            Props.Boat(g, new Vector3(mid.x + tan.x * 6f + outDir.x * 6f, 0.1f, mid.y + tan.y * 6f + outDir.y * 6f), yaw + 10f, new Color(0.2f, 0.6f, 0.9f));
            Props.Boat(g, new Vector3(mid.x - tan.x * 6f + outDir.x * 2f, 0.1f, mid.y - tan.y * 6f + outDir.y * 2f), yaw - 15f, new Color(0.95f, 0.4f, 0.3f));
            Occupy(p.x, p.y, 8f);
            d.landmarks.Add(new Landmark("El Cove", new Vector3(p.x, 0f, p.y)));
        }

        // ------------------------------------------------------------ West View

        static void WestView(IslandData d, Transform g)
        {
            Vector2 p = d.CoastPoint(new Vector2(-142f, -215f), out Vector2 inw);
            Vector2 b = p - inw * 4f;
            float yaw = Mathf.Atan2(-inw.x, -inw.y) * Mathf.Rad2Deg;
            var root = Props.Root(g, "Plataforma de West View", new Vector3(b.x, 0f, b.y), yaw);
            var t = root.transform;
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 2f, 0f), Vector3.zero, new Vector3(3f, 6f, 3f), MatLib.Solid(new Color(0.35f, 0.75f, 0.9f)), true);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 5.2f, 0f), Vector3.zero, new Vector3(6f, 0.4f, 6f), MatLib.Solid(Color.white), true);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 2.9f, 5f), new Vector3(35f, 0f, 0f), new Vector3(1.3f, 0.15f, 8f), MatLib.Solid(new Color(1f, 0.8f, 0.15f), 0.6f));
            Props.Part(t, PrimitiveType.Cube, new Vector3(2.2f, 5.45f, 4.2f), Vector3.zero, new Vector3(0.8f, 0.15f, 3f), MatLib.Solid(new Color(0.9f, 0.3f, 0.3f)));
            Props.Kiosk(g, new Vector3(p.x + inw.x * 9f, d.SampleHeight(p.x + inw.x * 9f, p.y + inw.y * 9f), p.y + inw.y * 9f), yaw + 180f);
            Occupy(p.x, p.y, 12f);
            d.landmarks.Add(new Landmark("West View", new Vector3(p.x, 0f, p.y)));
        }

        // ------------------------------------------------------------ Rocky Cay y barco hundido

        static void RockyCay(IslandData d, Transform g)
        {
            var c = IslandBuilder.RockyCay;
            Props.RockCluster(g, new Vector3(c.x, d.SampleHeight(c.x, c.y), c.y), 9, 8f, rng);
            var ship = new GameObject("Barco hundido");
            ship.transform.SetParent(g, false);
            ship.transform.position = new Vector3(c.x + 24f, 0.4f, c.y + 20f);
            ship.transform.rotation = Quaternion.Euler(10f, 35f, 22f);
            var t = ship.transform;
            var rust = MatLib.Solid(new Color(0.48f, 0.26f, 0.14f), 0.2f, 0.3f);
            var rust2 = MatLib.Solid(new Color(0.35f, 0.20f, 0.12f), 0.2f, 0.3f);
            Props.Part(t, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(4.5f, 3.2f, 17f), rust, true);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0f, 8.6f), new Vector3(0f, 45f, 0f), new Vector3(3.2f, 3.2f, 3.2f), rust);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 2.7f, -4f), Vector3.zero, new Vector3(3f, 2.4f, 4f), rust2);
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 4.6f, -4f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), rust2);
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 4.4f, 3f), new Vector3(0f, 0f, 12f), new Vector3(0.25f, 3f, 0.25f), rust2);
            d.landmarks.Add(new Landmark("Rocky Cay · Barco hundido", new Vector3(c.x, 0f, c.y)));
        }

        // ------------------------------------------------------------ Cayos

        static void Cays(IslandData d, Transform g, GameObject palmPrefab)
        {
            CayDecor(d, g, IslandBuilder.JohnnyCay, 16, 4, true, palmPrefab);
            CayDecor(d, g, IslandBuilder.HaynesCay, 6, 1, true, palmPrefab);
            CayDecor(d, g, IslandBuilder.Acuario, 4, 2, false, palmPrefab);
            var j = IslandBuilder.JohnnyCay; var h = IslandBuilder.HaynesCay; var a = IslandBuilder.Acuario;
            d.landmarks.Add(new Landmark("Johnny Cay", new Vector3(j.x, 0f, j.y)));
            d.landmarks.Add(new Landmark("Haynes Cay · El Acuario", new Vector3((h.x + a.x) * 0.5f, 0f, (h.y + a.y) * 0.5f)));
        }

        static void CayDecor(IslandData d, Transform g, Vector4 cay, int palms, int umbrellas, bool kiosk, GameObject palmPrefab)
        {
            int placed = 0;
            for (int i = 0; i < palms * 20 && placed < palms; i++)
            {
                float a = R(0f, Mathf.PI * 2f), r = Mathf.Sqrt(R(0f, 1f)) * cay.z * 0.75f;
                float x = cay.x + Mathf.Cos(a) * r, z = cay.y + Mathf.Sin(a) * r;
                float h = d.SampleHeight(x, z);
                if (h < 0.6f || !Free(d, x, z, 2f, 0f)) continue;
                Props.Palm(g, new Vector3(x, h - 0.1f, z), R(0f, 360f), R(0.8f, 1.25f), palmPrefab);
                Occupy(x, z, 2f);
                placed++;
            }
            for (int i = 0; i < umbrellas; i++)
            {
                float a = i * Mathf.PI * 2f / Mathf.Max(1, umbrellas) + 0.5f;
                float x = cay.x + Mathf.Cos(a) * cay.z * 0.82f, z = cay.y + Mathf.Sin(a) * cay.z * 0.82f;
                Props.Umbrella(g, new Vector3(x, Mathf.Max(0.3f, d.SampleHeight(x, z)), z), Props.BeachColors[i % Props.BeachColors.Length], rng);
            }
            if (kiosk)
            {
                float x = cay.x + cay.z * 0.2f, z = cay.y - cay.z * 0.25f;
                Props.Kiosk(g, new Vector3(x, d.SampleHeight(x, z), z), R(0f, 360f));
                Occupy(x, z, 3f);
            }
        }

        // ------------------------------------------------------------ Spratt Bight

        static void SprattBight(IslandData d, Transform g)
        {
            int count = 0;
            for (int i = 0; i < d.outline.Length; i += 3)
            {
                Vector2 o = d.outline[i];
                if (o.y < 540f || o.x < 70f) continue;
                Vector2 inw = d.InwardNormal(i);
                Vector2 p = o + inw * R(6f, 9f);
                float h = d.SampleHeight(p.x, p.y);
                if (h < 0.3f) continue;
                Props.Umbrella(g, new Vector3(p.x, h, p.y), Props.BeachColors[count % Props.BeachColors.Length], rng);
                count++;
            }
            d.landmarks.Add(new Landmark("Spratt Bight", new Vector3(135f, 0f, 610f)));
        }

        // ------------------------------------------------------------ pueblos

        static void Towns(IslandData d, Transform g, GameObject[] prefabs)
        {
            // North End / El Centro: hoteles frente a Spratt Bight y casas
            PlaceHouses(d, g, new Vector2(40f, 640f), 95f, 60, 8f, 200f, 0.35f, prefabs, 6f, 10f);
            // San Luis: casas de madera entre la vía y el mar
            PlaceHouses(d, g, new Vector2(175f, 170f), 110f, 26, 5f, 14f, 0f, prefabs, 5f, 6.5f);
            // La Loma
            Vector3 church = d.landmarks.Find(l => l.name.StartsWith("La Loma")).position;
            PlaceHouses(d, g, new Vector2(church.x, church.z), 140f, 26, 30f, 400f, 0f, prefabs, 6f, 8.5f);
            // South End
            PlaceHouses(d, g, new Vector2(0f, -600f), 90f, 10, 8f, 400f, 0f, prefabs, 6f, 8f);
            // casas dispersas por toda la isla
            PlaceHouses(d, g, new Vector2(0f, 0f), 900f, 70, 28f, 400f, 0f, prefabs, 6f, 8.5f);
        }

        static void PlaceHouses(IslandData d, Transform g, Vector2 center, float radius, int count, float minSd, float maxSd,
                                float hotelChance, GameObject[] prefabs, float minSize, float maxSize)
        {
            int placed = 0;
            for (int attempt = 0; attempt < count * 40 && placed < count; attempt++)
            {
                float a = R(0f, Mathf.PI * 2f), r = Mathf.Sqrt(R(0f, 1f)) * radius;
                float x = center.x + Mathf.Cos(a) * r, z = center.y + Mathf.Sin(a) * r;
                float sd = d.SignedDistance(x, z);
                if (sd < minSd || sd > maxSd) continue;
                bool hotel = R(0f, 1f) < hotelChance && sd < 60f;
                float w = hotel ? R(10f, 14f) : R(minSize, maxSize);
                float dep = hotel ? R(9f, 12f) : R(minSize, maxSize);
                float rad = Mathf.Max(w, dep) * 0.72f;
                if (!Free(d, x, z, rad, d.HalfRoad + 1.5f + rad)) continue;
                float gy = MinGround(d, x, z, rad);
                if (gy < 0.4f || d.SampleHeight(x, z) - gy > 4f) continue;
                float yaw = YawToRoad(d, new Vector2(x, z));
                var pos = new Vector3(x, gy, z);
                if (prefabs != null && prefabs.Length > 0 && !hotel)
                {
                    var pf = prefabs[rng.Next(prefabs.Length)];
                    if (pf != null) Object.Instantiate(pf, pos, Quaternion.Euler(0f, yaw, 0f), g);
                }
                else
                {
                    int floors = hotel ? rng.Next(3, 7) : (R(0f, 1f) < 0.3f ? 2 : 1);
                    Color wall = Props.HousePalette[rng.Next(Props.HousePalette.Length)];
                    Color roof = hotel ? new Color(0.9f, 0.9f, 0.88f) : Props.RoofPalette[rng.Next(Props.RoofPalette.Length)];
                    Props.House(g, pos, yaw, w, dep, floors, wall, roof, !hotel);
                }
                Occupy(x, z, rad);
                placed++;
            }
        }

        // ------------------------------------------------------------ vegetación

        static void Vegetation(IslandData d, Transform g, GameObject palmPrefab)
        {
            int palms = 0;
            for (int attempt = 0; attempt < 7000 && palms < 480; attempt++)
            {
                float x = R(-200f, 200f), z = R(-700f, 740f);
                float sd = d.SignedDistance(x, z);
                if (sd < 3f) continue;
                if (R(0f, 1f) > (sd < 35f ? 0.85f : 0.22f)) continue;
                float h = d.SampleHeight(x, z);
                if (h < 0.7f || !Free(d, x, z, 1.4f, d.HalfRoad + 2.5f)) continue;
                Props.Palm(g, new Vector3(x, h - 0.1f, z), R(0f, 360f), R(0.75f, 1.3f), palmPrefab);
                Occupy(x, z, 1.4f);
                palms++;
            }

            var greens = new[]
            {
                MatLib.Solid(new Color(0.22f, 0.50f, 0.20f)), MatLib.Solid(new Color(0.30f, 0.58f, 0.22f)),
                MatLib.Solid(new Color(0.17f, 0.42f, 0.19f))
            };
            var sphere = MatLib.Primitive(PrimitiveType.Sphere);
            int bushes = 0;
            for (int attempt = 0; attempt < 3000 && bushes < 360; attempt++)
            {
                float x = R(-200f, 200f), z = R(-700f, 740f);
                float sd = d.SignedDistance(x, z);
                if (sd < 12f) continue;
                if (!Free(d, x, z, 2f, d.HalfRoad + 3f)) continue;
                float h = d.SampleHeight(x, z);
                var b = Props.MeshPart(g, sphere, new Vector3(x, h - 0.3f, z), new Vector3(0f, R(0f, 360f), 0f),
                                       new Vector3(R(2f, 4.5f), R(1.2f, 2.6f), R(2f, 4.5f)), greens[rng.Next(greens.Length)]);
                b.name = "Arbusto";
                bushes++;
            }
        }
    }
}
