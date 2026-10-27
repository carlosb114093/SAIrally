using UnityEngine;

namespace SanAndres
{
    /// <summary>Hace flotar suavemente barcos y lanchas.</summary>
    public class Bobber : MonoBehaviour
    {
        public float amplitude = 0.18f, speed = 1.2f, roll = 4f;
        Vector3 basePos; Quaternion baseRot; float phase;

        void Start()
        {
            basePos = transform.position;
            baseRot = transform.rotation;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            transform.position = basePos + Vector3.up * Mathf.Sin(t) * amplitude;
            transform.rotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.7f) * roll * 0.5f, 0f, Mathf.Sin(t * 0.9f) * roll);
        }
    }
}
