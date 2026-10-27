using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Punto de entrada del prototipo. Al iniciar la escena genera la isla de San Andrés,
    /// la mulita, la cámara, la carrera, el HUD, el menú y la música.
    /// </summary>
    public class SanAndresGame : MonoBehaviour
    {
        [Header("Carrera")]
        [Range(1, 5)] public int vueltas = 2;
        public int semilla = 7;

        [Header("Opcional: assets propios (si se dejan vacíos se usan los procedurales)")]
        [Tooltip("Carrocería del vehículo. Se conservan las ruedas y la física procedurales.")]
        public GameObject carroceriaPrefab;
        [Tooltip("Palmera. El pivote debe estar en la base del tronco.")]
        public GameObject palmeraPrefab;
        [Tooltip("Casas. El frente (puerta) debe mirar a +Z y el pivote estar en el piso.")]
        public GameObject[] casasPrefabs;

        [Header("Menú y música")]
        [Tooltip("Si se desactiva, la carrera empieza de una (sin menú principal).")]
        public bool mostrarMenu = true;

        [Header("Render")]
        [Tooltip("Shader Lit de URP referenciado en la escena para que se incluya en los builds.")]
        public Shader litShader;

        void Awake()
        {
            float t0 = Time.realtimeSinceStartup;
            MatLib.ClearCaches();
            if (litShader != null) MatLib.LitShader = litShader;

            var island = IslandBuilder.Build();
            var world = new GameObject("Isla de San Andrés").transform;
            IslandBuilder.BuildGround(island, world);
            WorldDecor.Build(island, world, palmeraPrefab, casasPrefabs, semilla);
            SetupLighting();

            int start = island.NearestRoadIndex(new Vector2(130f, 610f)); // frente a Spratt Bight
            var car = CarFactory.Create(island.RoadPoint(start) + Vector3.up, Quaternion.identity, carroceriaPrefab);

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.farClipPlane = 3000f;
            cam.nearClipPlane = 0.2f;
            var follow = camGo.GetComponent<CameraFollow>();
            if (follow == null) follow = camGo.AddComponent<CameraFollow>();
            follow.target = car;

            gameObject.AddComponent<MusicPlayer>();
            var race = gameObject.AddComponent<RaceManager>();
            race.Init(island, car, follow, start, vueltas, !mostrarMenu);
            if (mostrarMenu) gameObject.AddComponent<MainMenu>().Init(race, follow, vueltas);

            Debug.Log($"[San Andrés] Isla generada en {Time.realtimeSinceStartup - t0:0.00} s · " +
                      $"Circunvalar de {island.road.Count * 4f / 1000f:0.0} km · {island.landmarks.Count} sitios");
        }

        static void SetupLighting()
        {
            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                sun = new GameObject("Sol del Caribe").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 350f;
            RenderSettings.fogEndDistance = 1700f;
            RenderSettings.fogColor = new Color(0.68f, 0.84f, 0.95f);
        }
    }
}
