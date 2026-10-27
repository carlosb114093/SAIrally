using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Construye la "mulita": el carrito de golf típico para recorrer San Andrés.
    /// Si se asigna un prefab propio (p. ej. un carro del Car Kit de Kenney), reemplaza la carrocería
    /// y se conservan las ruedas y la física.
    /// </summary>
    public static class CarFactory
    {
        public static ArcadeCar Create(Vector3 pos, Quaternion rot, GameObject bodyPrefab = null)
        {
            var root = new GameObject("Mulita");
            root.transform.SetPositionAndRotation(pos, rot);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 700f;
            rb.linearDamping = 0.02f;
            rb.angularDamping = 1.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var pm = new PhysicsMaterial("Carrocería")
            {
                dynamicFriction = 0.05f, staticFriction = 0.05f, bounciness = 0.1f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            var body = root.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.55f, 0f);
            body.size = new Vector3(1.5f, 0.55f, 2.8f);
            body.sharedMaterial = pm;
            var roofCol = root.AddComponent<BoxCollider>();
            roofCol.center = new Vector3(0f, 2.15f, -0.15f);
            roofCol.size = new Vector3(1.6f, 0.12f, 2.45f);
            roofCol.sharedMaterial = pm;
            rb.centerOfMass = new Vector3(0f, 0.15f, 0f);

            var model = new GameObject("Modelo").transform;
            model.SetParent(root.transform, false);
            if (bodyPrefab != null)
            {
                var inst = Object.Instantiate(bodyPrefab, model);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            }
            else BuildMulitaBody(model);

            var car = root.AddComponent<ArcadeCar>();
            var tire = MatLib.Solid(new Color(0.08f, 0.08f, 0.08f), 0.2f);
            var hub = MatLib.Solid(new Color(0.85f, 0.85f, 0.85f), 0.6f, 0.6f);
            Vector3[] anchors =
            {
                new Vector3(-0.72f, 0.15f, 1.0f), new Vector3(0.72f, 0.15f, 1.0f),
                new Vector3(-0.72f, 0.15f, -1.0f), new Vector3(0.72f, 0.15f, -1.0f)
            };
            string[] names = { "Rueda DI", "Rueda DD", "Rueda TI", "Rueda TD" };
            car.wheels = new ArcadeCar.Wheel[4];
            for (int i = 0; i < 4; i++)
            {
                var anchor = new GameObject(names[i]).transform;
                anchor.SetParent(root.transform, false);
                anchor.localPosition = anchors[i];
                var pivot = new GameObject("Dirección").transform;
                pivot.SetParent(anchor, false);
                var spinner = new GameObject("Giro").transform;
                spinner.SetParent(pivot, false);
                Props.Part(spinner, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.7f, 0.13f, 0.7f), tire);
                Props.Part(spinner, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.36f, 0.14f, 0.36f), hub);
                Props.Part(spinner, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.29f, 0.5f, 0.08f), hub);
                car.wheels[i] = new ArcadeCar.Wheel { anchor = anchor, pivot = pivot, spinner = spinner, front = i < 2 };
            }

            var src = root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            var audio = root.AddComponent<EngineAudio>();
            audio.car = car;

            Props.SetLayerRecursively(root, 2); // Ignore Raycast
            return car;
        }

        static void BuildMulitaBody(Transform t)
        {
            var yellow = MatLib.Solid(new Color(1f, 0.82f, 0.12f), 0.55f);
            var white = MatLib.Solid(new Color(0.97f, 0.97f, 0.95f), 0.4f);
            var black = MatLib.Solid(new Color(0.1f, 0.1f, 0.1f), 0.3f);
            var seat = MatLib.Solid(new Color(0.15f, 0.55f, 0.6f), 0.3f);
            var metal = MatLib.Solid(new Color(0.8f, 0.8f, 0.82f), 0.6f, 0.7f);
            var light = MatLib.Solid(new Color(1f, 0.97f, 0.85f), 0.9f, 0f, true);

            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(1.5f, 0.35f, 2.8f), yellow);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.78f, 1.12f), new Vector3(-12f, 0f, 0f), new Vector3(1.42f, 0.45f, 0.62f), yellow);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.42f, 1.45f), Vector3.zero, new Vector3(1.55f, 0.18f, 0.12f), black);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.42f, -1.45f), Vector3.zero, new Vector3(1.55f, 0.18f, 0.12f), black);
            // bancas
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.82f, -0.1f), Vector3.zero, new Vector3(1.3f, 0.25f, 0.6f), seat);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 1.15f, -0.43f), new Vector3(-8f, 0f, 0f), new Vector3(1.3f, 0.6f, 0.14f), seat);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.82f, -0.95f), Vector3.zero, new Vector3(1.3f, 0.25f, 0.55f), seat);
            // parales y techo
            foreach (var p in new[] { new Vector2(-0.68f, 0.85f), new Vector2(0.68f, 0.85f), new Vector2(-0.68f, -1.2f), new Vector2(0.68f, -1.2f) })
                Props.Part(t, PrimitiveType.Cylinder, new Vector3(p.x, 1.55f, p.y), Vector3.zero, new Vector3(0.05f, 0.6f, 0.05f), metal);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0f, 2.17f, -0.15f), Vector3.zero, new Vector3(1.6f, 0.08f, 2.45f), white);
            // timón (el conductor va a la izquierda, como en Colombia)
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(-0.35f, 1.22f, 0.42f), new Vector3(-55f, 0f, 0f), new Vector3(0.36f, 0.02f, 0.36f), black);
            // farolas
            Props.Part(t, PrimitiveType.Sphere, new Vector3(-0.5f, 0.78f, 1.44f), Vector3.zero, new Vector3(0.2f, 0.2f, 0.1f), light);
            Props.Part(t, PrimitiveType.Sphere, new Vector3(0.5f, 0.78f, 1.44f), Vector3.zero, new Vector3(0.2f, 0.2f, 0.1f), light);
            // conductor con camisa de flores y sombrero
            var skin = MatLib.Solid(new Color(0.55f, 0.38f, 0.27f));
            var shirt = MatLib.Solid(new Color(0.95f, 0.35f, 0.55f));
            var straw = MatLib.Solid(new Color(0.88f, 0.76f, 0.45f));
            Props.Part(t, PrimitiveType.Cube, new Vector3(-0.35f, 1.3f, -0.15f), Vector3.zero, new Vector3(0.45f, 0.55f, 0.3f), shirt);
            Props.Part(t, PrimitiveType.Sphere, new Vector3(-0.35f, 1.78f, -0.13f), Vector3.zero, Vector3.one * 0.32f, skin);
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(-0.35f, 1.93f, -0.13f), Vector3.zero, new Vector3(0.55f, 0.02f, 0.55f), straw);
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(-0.35f, 1.99f, -0.13f), Vector3.zero, new Vector3(0.3f, 0.06f, 0.3f), straw);
            Props.Part(t, PrimitiveType.Cube, new Vector3(-0.55f, 1.32f, 0.12f), new Vector3(60f, 0f, 0f), new Vector3(0.11f, 0.11f, 0.5f), skin);
            Props.Part(t, PrimitiveType.Cube, new Vector3(-0.15f, 1.32f, 0.12f), new Vector3(60f, 0f, 0f), new Vector3(0.11f, 0.11f, 0.5f), skin);
            // banderita de Colombia
            Props.Part(t, PrimitiveType.Cylinder, new Vector3(0.62f, 2.7f, -1.25f), Vector3.zero, new Vector3(0.03f, 0.5f, 0.03f), metal);
            Props.Part(t, PrimitiveType.Cube, new Vector3(0.62f, 3.1f, -1.55f), Vector3.zero, new Vector3(0.02f, 0.2f, 0.6f), MatLib.Solid(new Color(0.99f, 0.82f, 0.09f)));
            Props.Part(t, PrimitiveType.Cube, new Vector3(0.62f, 2.95f, -1.55f), Vector3.zero, new Vector3(0.02f, 0.1f, 0.6f), MatLib.Solid(new Color(0.0f, 0.22f, 0.58f)));
            Props.Part(t, PrimitiveType.Cube, new Vector3(0.62f, 2.85f, -1.55f), Vector3.zero, new Vector3(0.02f, 0.1f, 0.6f), MatLib.Solid(new Color(0.81f, 0.07f, 0.15f)));
        }
    }
}
