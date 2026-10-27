using UnityEngine;

namespace SanAndres
{
    /// <summary>Sonido de motor procedural (no necesita archivos de audio).</summary>
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
        float freq = 50f, gain, phase, phase2;
        int sampleRate;
        uint noise = 22222;

        void Start()
        {
            sampleRate = AudioSettings.outputSampleRate;
            GetComponent<AudioSource>().Play();
        }

        void Update()
        {
            if (car == null) return;
            float s = car.Speed01;
            float th = Mathf.Abs(car.ThrottleInput);
            targetFreq = 42f + s * 120f + th * 14f + (car.TurboActive ? 18f : 0f);
            targetGain = (Muted || Silenced) ? 0f : (0.05f + 0.05f * th + 0.03f * s) * Volume;
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
    }
}
