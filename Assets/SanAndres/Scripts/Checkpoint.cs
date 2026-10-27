using UnityEngine;

namespace SanAndres
{
    /// <summary>Punto de control de la carrera (trigger atravesando la vía).</summary>
    public class Checkpoint : MonoBehaviour
    {
        public int index;
        public RaceManager manager;
        public GameObject visual;

        void OnTriggerEnter(Collider other)
        {
            if (manager == null) return;
            var car = other.GetComponentInParent<ArcadeCar>();
            if (car != null) manager.OnCheckpoint(this);
        }

        public void SetHighlight(bool on)
        {
            if (visual != null) visual.SetActive(on);
        }
    }
}
