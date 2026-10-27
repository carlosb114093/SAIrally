using UnityEngine;

namespace SanAndres
{
    /// <summary>Cámara de persecución con tres modos (C para cambiar).</summary>
    public class CameraFollow : MonoBehaviour
    {
        public ArcadeCar target;
        public int mode;
        Vector3 vel, smoothFwd = Vector3.forward;
        Camera cam;
        static readonly string[] ModeNames = { "Persecución", "Lejana", "Conductor" };
        public string ModeName => ModeNames[mode];

        void Awake() { cam = GetComponent<Camera>(); }

        public void Cycle() { mode = (mode + 1) % 3; Snap(); }

        public void Snap()
        {
            if (target == null) return;
            smoothFwd = Flat(target.transform.forward);
            transform.position = Desired(out Vector3 look);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
            vel = Vector3.zero;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward; }

        Vector3 Desired(out Vector3 look)
        {
            float dist = mode == 0 ? 6.5f : 12f;
            float height = mode == 0 ? 2.6f : 5f;
            look = target.transform.position + Vector3.up * 1.2f;
            Vector3 desired = look - smoothFwd * dist + Vector3.up * height;
            if (Physics.Linecast(look, desired, out RaycastHit hit, ~(1 << 2), QueryTriggerInteraction.Ignore))
                desired = hit.point + hit.normal * 0.3f;
            return desired;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            if (mode == 2)
            {
                transform.position = target.transform.TransformPoint(new Vector3(-0.35f, 1.8f, 0.15f));
                transform.rotation = Quaternion.LookRotation(target.transform.forward, target.transform.up);
            }
            else
            {
                Vector3 fwd = Flat(target.transform.forward);
                // al ir en reversa la cámara no gira de golpe
                if (target.ForwardSpeed < -2f) fwd = smoothFwd;
                smoothFwd = Vector3.Slerp(smoothFwd, fwd, 1f - Mathf.Exp(-5f * dt));
                Vector3 desired = Desired(out Vector3 look);
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, 0.08f);
                transform.rotation = Quaternion.LookRotation(look - transform.position);
            }
            if (cam != null)
            {
                float fov = 60f + target.Speed01 * 16f + (target.TurboActive ? 6f : 0f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, dt * 3f);
            }
        }
    }
}
