using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Sonido de motor procedural (no necesita archivos de audio).
    /// En escritorio y en el editor se sintetiza muestra a muestra con OnAudioFilterRead.
    /// En WebGL (navegador) OnAudioFilterRead no existe, así que se genera un bucle corto
    /// por código y se varía su tono (pitch) y volumen según la velocidad.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EngineAudio : MonoBehaviour
    {
        public ArcadeCar car;
        public static bool Muted;
        /// <summary>Lo activa el menú para que el motor no suene detrás de la música.</summary>
        public static bool Silenced;

        const string KeyVolumen = "SanAndres_volMotor";
        static float volumen = -1f;
        /// <summary>Volumen del motor (0..1), guardado en PlayerPrefs.</summary>
        public static float Volume
        {
            get { if (volumen < 0f) volumen = PlayerPrefs.GetFloat(KeyVolumen, 1f); return volumen; }
            set { volumen = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyVolumen, volumen); }
        }

        volatile float targetFreq = 50f, targetGain;

        void Update()
        {
            if (car == null) return;
            float s = car.Speed01;
            float th = Mathf.Abs(car.ThrottleInput);
            targetFreq = 42f + s * 120f + th * 14f + (car.TurboActive ? 18f : 0f);
            targetGain = (Muted || Silenced) ? 0f : (0.05f + 0.05f * th + 0.03f * s) * Volume;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // ---------- Versión navegador: bucle pregenerado + pitch ----------

        // 1 s de audio a 70 Hz = 70 ciclos exactos (y 35 del sub-grave), así el bucle no tiene cortes.
        const float BaseFreq = 70f;
        const int LoopRate = 44100;
        static AudioClip bucle;

        AudioSource src;
        float freq = 50f, gain;

        void Start()
        {
            src = GetComponent<AudioSource>();
            src.clip = CrearBucle();
            src.loop = true;
            src.volume = 0f;
            src.pitch = freq / BaseFreq;
            src.Play();
        }

        void LateUpdate()
        {
            if (src == null) return;
            float dt = Time.unscaledDeltaTime;
            freq = Mathf.Lerp(freq, targetFreq, 1f - Mathf.Exp(-dt / 0.04f));
            gain = Mathf.Lerp(gain, targetGain, 1f - Mathf.Exp(-dt / 0.02f));
            src.pitch = Mathf.Clamp(freq / BaseFreq, 0.1f, 3f);
            // El bucle está normalizado más alto que la síntesis directa; se compensa aquí.
            src.volume = Mathf.Clamp01(gain * 1.6f);
        }

        static AudioClip CrearBucle()
        {
            if (bucle != null) return bucle;
            int len = LoopRate;
            var data = new float[len];
            uint noise = 22222;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)LoopRate;
                float phase = (t * BaseFreq) % 1f;
                float phase2 = (t * BaseFreq * 0.5f) % 1f;
                noise = noise * 1664525u + 1013904223u;
                float n = ((noise >> 9) / 8388608f) - 1f;
                float saw = phase * 2f - 1f;
                float sub = Mathf.Sin(phase2 * Mathf.PI * 2f);
                data[i] = (saw * 0.45f + sub * 0.45f + n * 0.1f) * 0.6f;
            }
            bucle = AudioClip.Create("MotorBucle", len, 1, LoopRate, false);
            bucle.SetData(data, 0);
            return bucle;
        }
#else
        // ---------- Versión escritorio / editor: síntesis en tiempo real ----------

        float freq = 50f, gain, phase, phase2;
        int sampleRate;
        uint noise = 22222;

        void Start()
        {
            sampleRate = AudioSettings.outputSampleRate;
            GetComponent<AudioSource>().Play();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (sampleRate <= 0) return;
            float tf = targetFreq, tg = targetGain;
            for (int i = 0; i < data.Length; i += channels)
            {
                freq += (tf - freq) * 0.0005f;
                gain += (tg - gain) * 0.001f;
                phase += freq / sampleRate; if (phase > 1f) phase -= 1f;
                phase2 += freq * 0.5f / sampleRate; if (phase2 > 1f) phase2 -= 1f;
                noise = noise * 1664525u + 1013904223u;
                float n = ((noise >> 9) / 8388608f) - 1f;
                float saw = phase * 2f - 1f;
                float sub = Mathf.Sin(phase2 * Mathf.PI * 2f);
                float v = (saw * 0.45f + sub * 0.45f + n * 0.1f) * gain;
                for (int c = 0; c < channels; c++) data[i + c] = v;
            }
        }
#endif
    }
}
