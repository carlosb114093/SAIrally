using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Música de fondo caribeña. Carga los bucles desde Resources/Musica y hace
    /// fundidos entre la pista del menú y la de la carrera. Sigue sonando en pausa.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public enum Pista { Ninguna, Menu, Carrera }

        public static MusicPlayer Instance { get; private set; }

        const string KeyVolumen = "SanAndres_volMusica";
        static float volumen = -1f;

        /// <summary>Volumen general de la música (0..1), guardado en PlayerPrefs.</summary>
        public static float Volumen
        {
            get { if (volumen < 0f) volumen = PlayerPrefs.GetFloat(KeyVolumen, 0.6f); return volumen; }
            set { volumen = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyVolumen, volumen); }
        }

        public float duracionFundido = 1.2f;

        AudioSource a, b;          // a = la que suena, b = la que se apaga
        AudioClip clipMenu, clipCarrera;
        Pista actual = Pista.Ninguna;
        float mezcla = 1f;         // 0 → 1 durante el fundido
        float atenuacion = 1f, atenuacionObjetivo = 1f;

        void Awake()
        {
            Instance = this;
            a = CrearFuente();
            b = CrearFuente();
            clipMenu = Resources.Load<AudioClip>("Musica/CaribeMenu");
            clipCarrera = Resources.Load<AudioClip>("Musica/CaribeCarrera");
            if (clipMenu == null || clipCarrera == null)
                Debug.LogWarning("[San Andrés] No se encontró la música en Assets/SanAndres/Resources/Musica.");
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        AudioSource CrearFuente()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = true; // sigue sonando con el juego en pausa
            s.volume = 0f;
            return s;
        }

        public void Reproducir(Pista p)
        {
            if (p == actual) return;
            actual = p;
            var clip = p == Pista.Menu ? clipMenu : p == Pista.Carrera ? clipCarrera : null;
            (a, b) = (b, a);
            a.Stop();
            a.clip = clip;
            a.volume = 0f;
            if (clip != null) a.Play();
            mezcla = 0f;
        }

        /// <summary>Baja la música (p. ej. en el menú de pausa) sin detenerla.</summary>
        public void Atenuar(bool on) { atenuacionObjetivo = on ? 0.45f : 1f; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            mezcla = Mathf.MoveTowards(mezcla, 1f, dt / Mathf.Max(0.01f, duracionFundido));
            atenuacion = Mathf.MoveTowards(atenuacion, atenuacionObjetivo, dt * 2f);
            float v = Volumen * atenuacion;
            a.volume = v * Mathf.Sqrt(mezcla);
            b.volume = v * Mathf.Sqrt(1f - mezcla);
            if (mezcla >= 1f && b.isPlaying) b.Stop();
        }
    }
}
