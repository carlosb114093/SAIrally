using UnityEngine;

namespace SanAndres
{
    /// <summary>El Hoyo Soplador: géiser marino que sopla cada pocos segundos. Lanza por el aire al vehículo que pase encima.</summary>
    public class HoyoSoplador : MonoBehaviour
    {
        public Transform column;
        public Transform[] drops;
        public float launchRadius = 9f;
        public float duration = 2.2f;

        float wait, t = -1f;
        bool launched;
        Vector3[] vel;

        void Start()
        {
            wait = Random.Range(2f, 5f);
            vel = new Vector3[drops.Length];
            HideDrops();
        }

        void HideDrops() { foreach (var d in drops) d.gameObject.SetActive(false); }

        void Update()
        {
            if (t < 0f)
            {
                wait -= Time.deltaTime;
                if (wait > 0f) return;
                t = 0f; launched = false;
                for (int i = 0; i < drops.Length; i++)
                {
                    drops[i].localPosition = Vector3.up * 0.5f;
                    vel[i] = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(11f, 18f), Random.Range(-2.5f, 2.5f));
                    drops[i].gameObject.SetActive(true);
                }
            }

            t += Time.deltaTime;
            float k = t / duration;
            float h = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 13f;
            column.localScale = new Vector3(1.4f, Mathf.Max(0.01f, h * 0.5f), 1.4f);
            column.localPosition = new Vector3(0f, h * 0.5f, 0f);
            for (int i = 0; i < drops.Length; i++)
            {
                if (!drops[i].gameObject.activeSelf) continue;
                vel[i] += Vector3.down * 9.81f * Time.deltaTime;
                drops[i].localPosition += vel[i] * Time.deltaTime;
                if (drops[i].localPosition.y < 0f) drops[i].gameObject.SetActive(false);
            }

            if (!launched && k > 0.15f && k < 0.7f) TryLaunch();

            if (t > duration + 1.5f)
            {
                t = -1f;
                wait = Random.Range(4f, 8f);
                HideDrops();
            }
        }

        void TryLaunch()
        {
            var rm = RaceManager.Instance;
            if (rm == null || rm.Car == null) return;
            Vector3 delta = rm.Car.transform.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude > launchRadius) return;
            launched = true;
            Vector3 push = delta.sqrMagnitude > 0.01f ? delta.normalized * 4f : Vector3.zero;
            rm.Car.Body.AddForce(Vector3.up * 13f + push, ForceMode.VelocityChange);
            rm.Car.Body.AddTorque(new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)), ForceMode.VelocityChange);
            rm.Flash("¡El Hoyo Soplador te lanzó por los aires!");
        }
    }
}
