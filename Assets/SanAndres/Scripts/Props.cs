using System.Collections.Generic;
using UnityEngine;

namespace SanAndres
{
    /// <summary>Fábrica de objetos low-poly hechos con primitivas: palmeras, casas isleñas, barcos, etc.</summary>
    public static class Props
    {
        public static readonly Color[] HousePalette =
        {
            new Color(0.95f, 0.45f, 0.62f), new Color(0.25f, 0.80f, 0.78f), new Color(0.98f, 0.85f, 0.30f),
            new Color(0.60f, 0.85f, 0.35f), new Color(0.70f, 0.60f, 0.90f), new Color(0.98f, 0.60f, 0.40f),
            new Color(0.45f, 0.70f, 0.95f), new Color(0.96f, 0.96f, 0.92f), new Color(0.60f, 0.92f, 0.75f)
        };
        public static readonly Color[] RoofPalette =
        {
            new Color(0.70f, 0.20f, 0.15f), new Color(0.95f, 0.95f, 0.95f), new Color(0.15f, 0.50f, 0.55f),
            new Color(0.60f, 0.62f, 0.64f), new Color(0.85f, 0.35f, 0.20f)
        };
        public static readonly Color[] BeachColors =
        {
            new Color(0.95f, 0.25f, 0.30f), new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.6f, 0.95f),
            new Color(0.2f, 0.8f, 0.5f), new Color(1f, 0.55f, 0.15f)
        };

        public static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        public static readonly Color DarkWood = new Color(0.30f, 0.19f, 0.11f);
        public static readonly Color Rock = new Color(0.40f, 0.38f, 0.34f);

        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale,
                                      Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject MeshPart(Transform parent, Mesh mesh, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject Root(Transform parent, string name, Vector3 pos, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        // ------------------------------------------------------------ palmera

        static Mesh palmMesh;
        static Material palmTrunk, palmLeaf;

        public static Mesh PalmMesh
        {
            get
            {
                if (palmMesh != null) return palmMesh;
                Mesh cyl = MatLib.Primitive(PrimitiveType.Cylinder);
                Mesh cube = MatLib.Primitive(PrimitiveType.Cube);
                Mesh sphere = MatLib.Primitive(PrimitiveType.Sphere);
                var trunk = new List<CombineInstance>();
                var leaves = new List<CombineInstance>();
                const int segs = 6;
                for (int s = 0; s < segs; s++)
                {
                    float xo = 0.06f * s * s;
                    float r = 0.55f - s * 0.04f;
                    trunk.Add(new CombineInstance
                    {
                        mesh = cyl,
                        transform = Matrix4x4.TRS(new Vector3(xo, s * 1.4f + 0.7f, 0f), Quaternion.Euler(0f, 0f, -s * 4f),
                                                  new Vector3(r, 0.74f, r))
                    });
                }
                Vector3 top = new Vector3(0.06f * (segs - 1) * (segs - 1) + 0.25f, segs * 1.4f, 0f);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 120f * Mathf.Deg2Rad;
                    trunk.Add(new CombineInstance
                    {
                        mesh = sphere,
                        transform = Matrix4x4.TRS(top + new Vector3(Mathf.Cos(a) * 0.35f, -0.45f, Mathf.Sin(a) * 0.35f),
                                                  Quaternion.identity, Vector3.one * 0.38f)
                    });
                }
                for (int i = 0; i < 9; i++)
                {
                    Quaternion rot = Quaternion.Euler(0f, i * 40f, 0f) * Quaternion.Euler(28f, 0f, 0f);
                    leaves.Add(new CombineInstance
                    {
                        mesh = cube,
                        transform = Matrix4x4.TRS(top + rot * new Vector3(0f, 0f, 1.7f), rot, new Vector3(1.0f, 0.07f, 3.6f))
                    });
                }
                for (int i = 0; i < 5; i++)
                {
                    Quaternion rot = Quaternion.Euler(0f, i * 72f + 20f, 0f) * Quaternion.Euler(-12f, 0f, 0f);
                    leaves.Add(new CombineInstance
                    {
                        mesh = cube,
                        transform = Matrix4x4.TRS(top + Vector3.up * 0.2f + rot * new Vector3(0f, 0f, 1.2f), rot,
                                                  new Vector3(0.8f, 0.07f, 2.5f))
                    });
                }
                var trunkMesh = new Mesh();
                trunkMesh.CombineMeshes(trunk.ToArray(), true, true);
                var leafMesh = new Mesh();
                leafMesh.CombineMeshes(leaves.ToArray(), true, true);
                palmMesh = new Mesh { name = "Palmera" };
                palmMesh.CombineMeshes(new[]
                {
                    new CombineInstance { mesh = trunkMesh, transform = Matrix4x4.identity },
                    new CombineInstance { mesh = leafMesh, transform = Matrix4x4.identity }
                }, false, true);
                palmMesh.RecalculateBounds();
                return palmMesh;
            }
        }

        public static GameObject Palm(Transform parent, Vector3 pos, float yaw, float scale, GameObject prefab = null)
        {
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
                inst.transform.localScale *= scale;
                return inst;
            }
            if (palmTrunk == null) palmTrunk = MatLib.Solid(new Color(0.47f, 0.35f, 0.23f));
            if (palmLeaf == null) palmLeaf = MatLib.Solid(new Color(0.18f, 0.55f, 0.20f), 0.25f);
            var go = Root(parent, "Palmera", pos, yaw);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = PalmMesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { palmTrunk, palmLeaf };
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0.4f, 4f, 0f);
            col.radius = 0.35f;
            col.height = 8f;
            return go;
        }

        // ------------------------------------------------------------ casas

        public static GameObject House(Transform parent, Vector3 groundPos, float yaw, float w, float depth, int floors,
                                       Color wall, Color roof, bool gable)
        {
            var go = Root(parent, floors >= 3 ? "Hotel" : "Casa isleña", groundPos, yaw);
            var t = go.transform;
            float H = floors * 3.2f;
            Part(t, PrimitiveType.Cube, new Vector3(0f, H * 0.5f - 1f, 0f), Vector3.zero, new Vector3(w, H + 2f, depth),
                 MatLib.Solid(wall), true);
            var roofMat = MatLib.Solid(roof, 0.3f);
            if (gable)
                Part(t, PrimitiveType.Cube, new Vector3(0f, H, 0f), new Vector3(0f, 0f, 45f),
                     new Vector3(w * 0.72f, w * 0.72f, depth + 0.6f), roofMat);
            else
                Part(t, PrimitiveType.Cube, new Vector3(0f, H + 0.3f, 0f), Vector3.zero,
                     new Vector3(w + 0.3f, 0.6f, depth + 0.3f), roofMat);

            var doorMat = MatLib.Solid(DarkWood);
            var winMat = MatLib.Solid(new Color(0.16f, 0.30f, 0.45f), 0.8f);
            var trimMat = MatLib.Solid(Color.white);
            float front = depth * 0.5f + 0.03f;
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.1f, front), Vector3.zero, new Vector3(1.2f, 2.2f, 0.08f), doorMat);
            for (int f = 0; f < floors; f++)
            {
                float y = f * 3.2f + 1.7f;
                float[] xs = f == 0 ? new[] { -w * 0.3f, w * 0.3f } : new[] { -w * 0.3f, 0f, w * 0.3f };
                foreach (float x in xs)
                {
                    Part(t, PrimitiveType.Cube, new Vector3(x, y, front), Vector3.zero, new Vector3(1.05f, 1.15f, 0.05f), trimMat);
                    Part(t, PrimitiveType.Cube, new Vector3(x, y, front + 0.02f), Vector3.zero, new Vector3(0.85f, 0.95f, 0.05f), winMat);
                }
            }
            if (floors <= 2 && gable)
            {
                // porche de madera, típico de la arquitectura raizal
                Part(t, PrimitiveType.Cube, new Vector3(0f, 2.7f, front + 0.9f), new Vector3(-12f, 0f, 0f),
                     new Vector3(w * 0.8f, 0.12f, 2f), roofMat);
                Part(t, PrimitiveType.Cylinder, new Vector3(-w * 0.38f, 1.25f, front + 1.7f), Vector3.zero, new Vector3(0.15f, 1.25f, 0.15f), trimMat);
                Part(t, PrimitiveType.Cylinder, new Vector3(w * 0.38f, 1.25f, front + 1.7f), Vector3.zero, new Vector3(0.15f, 1.25f, 0.15f), trimMat);
            }
            return go;
        }

        public static GameObject Kiosk(Transform parent, Vector3 pos, float yaw)
        {
            var go = Root(parent, "Kiosco", pos, yaw);
            var t = go.transform;
            var wood = MatLib.Solid(Wood);
            foreach (var p in new[] { new Vector2(-1.6f, -1.6f), new Vector2(1.6f, -1.6f), new Vector2(-1.6f, 1.6f), new Vector2(1.6f, 1.6f) })
                Part(t, PrimitiveType.Cylinder, new Vector3(p.x, 1.3f, p.y), Vector3.zero, new Vector3(0.2f, 1.3f, 0.2f), wood);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1f, 1.4f), Vector3.zero, new Vector3(3.4f, 1.1f, 0.5f), wood, true);
            MeshPart(t, MatLib.Cone(8), new Vector3(0f, 2.5f, 0f), Vector3.zero, new Vector3(3.2f, 1.6f, 3.2f),
                     MatLib.Solid(new Color(0.78f, 0.66f, 0.40f)));
            return go;
        }

        public static GameObject Umbrella(Transform parent, Vector3 pos, Color color, System.Random rng)
        {
            var go = Root(parent, "Sombrilla", pos, (float)rng.NextDouble() * 360f);
            var t = go.transform;
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), Vector3.zero, new Vector3(0.07f, 1.2f, 0.07f), MatLib.Solid(Color.white));
            MeshPart(t, MatLib.Cone(8, true), new Vector3(0f, 2.15f, 0f), Vector3.zero, new Vector3(1.6f, 0.55f, 1.6f), MatLib.Solid(color));
            var lounger = MatLib.Solid(new Color(0.97f, 0.97f, 0.95f));
            Part(t, PrimitiveType.Cube, new Vector3(-0.8f, 0.25f, 0.2f), new Vector3(-8f, 0f, 0f), new Vector3(0.65f, 0.15f, 1.9f), lounger);
            Part(t, PrimitiveType.Cube, new Vector3(0.8f, 0.25f, 0.2f), new Vector3(-8f, 0f, 0f), new Vector3(0.65f, 0.15f, 1.9f), lounger);
            return go;
        }

        public static GameObject RockCluster(Transform parent, Vector3 center, int count, float radius, System.Random rng,
                                             float minAngle = 0f, float maxAngle = 360f, float clearRadius = 0f)
        {
            var go = Root(parent, "Rocas", center, 0f);
            var a = MatLib.Solid(Rock, 0.1f);
            var b = MatLib.Solid(new Color(0.30f, 0.28f, 0.26f), 0.1f);
            for (int i = 0; i < count; i++)
            {
                float ang = Mathf.Lerp(minAngle, maxAngle, (float)rng.NextDouble()) * Mathf.Deg2Rad;
                float r = Mathf.Lerp(clearRadius, radius, Mathf.Sqrt((float)rng.NextDouble()));
                Vector3 s = new Vector3(Rnd(rng, 1.5f, 4.5f), Rnd(rng, 0.8f, 2.2f), Rnd(rng, 1.5f, 4.5f));
                var rock = Part(go.transform, PrimitiveType.Sphere, new Vector3(Mathf.Cos(ang) * r, -0.3f, Mathf.Sin(ang) * r),
                                new Vector3(0f, Rnd(rng, 0f, 360f), Rnd(rng, -15f, 15f)), s, i % 2 == 0 ? a : b);
                rock.name = "Roca";
            }
            return go;
        }

        public static float Rnd(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ------------------------------------------------------------ barcos

        public static GameObject Boat(Transform parent, Vector3 pos, float yaw, Color color)
        {
            var go = Root(parent, "Lancha", pos, yaw);
            var t = go.transform;
            var hull = MatLib.Solid(color, 0.4f);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), Vector3.zero, new Vector3(1.8f, 0.8f, 4.6f), hull);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.2f, 2.3f), new Vector3(0f, 45f, 0f), new Vector3(1.27f, 0.8f, 1.27f), hull);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.65f, -0.6f), Vector3.zero, new Vector3(1.6f, 0.15f, 2.6f), MatLib.Solid(Color.white));
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, -2.4f), Vector3.zero, new Vector3(0.4f, 0.8f, 0.4f), MatLib.Solid(new Color(0.15f, 0.15f, 0.15f)));
            go.AddComponent<Bobber>();
            return go;
        }

        public static GameObject PirateShip(Transform parent, Vector3 pos, float yaw)
        {
            var go = Root(parent, "Barco pirata de Morgan", pos, yaw);
            var t = go.transform;
            var hull = MatLib.Solid(DarkWood);
            var wood = MatLib.Solid(Wood);
            var sail = MatLib.Solid(new Color(0.12f, 0.12f, 0.12f));
            var gold = MatLib.Solid(new Color(0.95f, 0.75f, 0.2f), 0.6f, 0.6f);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), Vector3.zero, new Vector3(4.2f, 2.4f, 15f), hull);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, 7.4f), new Vector3(0f, 45f, 0f), new Vector3(2.97f, 2.4f, 2.97f), hull);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 2.6f, -5.8f), Vector3.zero, new Vector3(4.2f, 1.8f, 3.6f), wood);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.85f, 0f), Vector3.zero, new Vector3(4.3f, 0.12f, 15.2f), gold);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 6.5f, 2f), Vector3.zero, new Vector3(0.35f, 5f, 0.35f), wood);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 6f, -2.5f), Vector3.zero, new Vector3(0.3f, 4.5f, 0.3f), wood);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 7.2f, 2.3f), Vector3.zero, new Vector3(5.5f, 4f, 0.12f), sail);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 6.6f, -2.2f), Vector3.zero, new Vector3(4.6f, 3.4f, 0.12f), sail);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 10.6f, 2f), Vector3.zero, new Vector3(1.1f, 0.3f, 1.1f), wood);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 11.9f, 1.3f), Vector3.zero, new Vector3(0.05f, 0.9f, 1.4f), sail);
            Part(t, PrimitiveType.Sphere, new Vector3(0.04f, 12f, 1.25f), Vector3.zero, new Vector3(0.05f, 0.35f, 0.35f), MatLib.Solid(Color.white));
            var b = go.AddComponent<Bobber>();
            b.amplitude = 0.35f; b.roll = 3f; b.speed = 0.8f;
            return go;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursively(c.gameObject, layer);
        }
    }
}
