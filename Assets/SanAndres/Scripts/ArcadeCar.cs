using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Controlador arcade con suspensión por raycast (sin WheelColliders): estable, fácil de ajustar
    /// y divertido. Pensado para la "mulita", el carrito de golf con el que se recorre San Andrés.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ArcadeCar : MonoBehaviour
    {
        [System.Serializable]
        public class Wheel
        {
            public Transform anchor, pivot, spinner;
            public bool front;
            [System.NonSerialized] public float dist;
            [System.NonSerialized] public bool grounded;
        }

        [Header("Motor")]
        public float maxSpeed = 30f;           // m/s (~108 km/h)
        public float acceleration = 13f;
        public float brakeDeceleration = 24f;
        public float reverseMaxSpeed = 9f;

        [Header("Turbo")]
        public float turboMultiplier = 1.35f;
        public float turboDrain = 0.28f;
        public float turboRecharge = 0.09f;

        [Header("Dirección")]
        public float maxSteerAngle = 34f;
        public float highSpeedSteerAngle = 11f;

        [Header("Suspensión")]
        public float suspensionRest = 0.45f;
        public float wheelRadius = 0.35f;
        public float springStrength = 6500f;
        public float springDamper = 1100f;

        [Header("Agarre")]
        [Range(0f, 1f)] public float grip = 0.55f;
        [Range(0f, 1f)] public float handbrakeGrip = 0.12f;
        public float frictionLimit = 1.6f;

        public Wheel[] wheels;
        public bool inputEnabled = true;

        public float Turbo { get; private set; } = 1f;
        public bool TurboActive { get; private set; }
        public float ForwardSpeed { get; private set; }
        public float ThrottleInput { get; private set; }
        public bool Grounded => groundedWheels > 0;
        public Rigidbody Body => rb;
        public float SpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        public float Speed01 => Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / (maxSpeed * turboMultiplier));

        Rigidbody rb;
        int groundedWheels;
        float steerAngle, steerInput;
        bool handbrake, turboHeld;
        int mask;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            mask = ~(1 << 2); // ignora la capa "Ignore Raycast" donde vive el carro
        }

        void Update()
        {
            if (inputEnabled)
            {
                ThrottleInput = GameInput.Throttle();
                steerInput = GameInput.Steer();
                handbrake = GameInput.Handbrake();
                turboHeld = GameInput.Boost();
            }
            else
            {
                ThrottleInput = 0f; steerInput = 0f; handbrake = true; turboHeld = false;
            }
            TurboActive = turboHeld && Turbo > 0.02f && ThrottleInput > 0.1f && Grounded;
            Turbo = Mathf.Clamp01(Turbo + (TurboActive ? -turboDrain : turboRecharge) * Time.deltaTime);
            UpdateWheelVisuals();
        }

        void FixedUpdate()
        {
            if (wheels == null || wheels.Length == 0) return;
            float dt = Time.fixedDeltaTime;
            ForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float speedFactor = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / maxSpeed);
            float targetSteer = steerInput * Mathf.Lerp(maxSteerAngle, highSpeedSteerAngle, speedFactor);
            steerAngle = Mathf.MoveTowards(steerAngle, targetSteer, 160f * dt);

            groundedWheels = 0;
            Vector3 normalSum = Vector3.zero;
            float rayLen = suspensionRest + wheelRadius;
            float massPerWheel = rb.mass / wheels.Length;
            Vector3 up = transform.up;

            foreach (var w in wheels)
            {
                Vector3 origin = w.anchor.position;
                if (Physics.Raycast(origin, -up, out RaycastHit hit, rayLen, mask, QueryTriggerInteraction.Ignore))
                {
                    w.grounded = true; w.dist = hit.distance;
                    groundedWheels++;
                    normalSum += hit.normal;

                    Vector3 pv = rb.GetPointVelocity(origin);
                    float compression = rayLen - hit.distance;
                    float spring = Mathf.Max(0f, compression * springStrength - Vector3.Dot(up, pv) * springDamper);
                    rb.AddForceAtPosition(up * spring, origin);

                    Quaternion steerRot = w.front ? Quaternion.AngleAxis(steerAngle, up) : Quaternion.identity;
                    Vector3 right = Vector3.ProjectOnPlane(steerRot * transform.right, hit.normal).normalized;
                    float sideVel = Vector3.Dot(pv, right);
                    float g = (handbrake && !w.front) ? handbrakeGrip : grip;
                    float lateral = -sideVel * g * massPerWheel / dt;
                    float maxLat = spring * frictionLimit;
                    rb.AddForceAtPosition(right * Mathf.Clamp(lateral, -maxLat, maxLat), origin);
                }
                else
                {
                    w.grounded = false; w.dist = rayLen;
                }
            }

            if (groundedWheels > 0)
            {
                Vector3 n = normalSum.normalized;
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
                float gf = groundedWheels / (float)wheels.Length;
                float top = maxSpeed * (TurboActive ? turboMultiplier : 1f);
                float acc = acceleration * (TurboActive ? 1.5f : 1f);
                Vector3 a = Vector3.zero;
                float th = ThrottleInput;
                if (th > 0.01f)
                {
                    if (ForwardSpeed < -0.5f) a = fwd * brakeDeceleration * th;
                    else if (ForwardSpeed < top) a = fwd * acc * th * Mathf.Clamp01(1.15f - ForwardSpeed / top);
                }
                else if (th < -0.01f)
                {
                    if (ForwardSpeed > 0.5f) a = -fwd * brakeDeceleration * -th;
                    else if (ForwardSpeed > -reverseMaxSpeed) a = fwd * acc * 0.6f * th;
                }
                else
                {
                    a = -fwd * ForwardSpeed * 0.35f; // rodadura
                }
                if (handbrake) a += -fwd * Mathf.Clamp(ForwardSpeed, -1f, 1f) * 7f;
                rb.AddForce(a * gf, ForceMode.Acceleration);
                rb.AddForce(-n * Mathf.Abs(ForwardSpeed) * 0.15f, ForceMode.Acceleration); // carga aerodinámica
            }

            // ayuda para mantenerse derecho y control en el aire
            Vector3 axis = Vector3.Cross(up, Vector3.up);
            rb.AddTorque(axis * (groundedWheels == 0 ? 6f : 2.5f), ForceMode.Acceleration);
            if (groundedWheels == 0) rb.AddTorque(up * steerInput * 2.5f, ForceMode.Acceleration);
        }

        void UpdateWheelVisuals()
        {
            if (wheels == null) return;
            float spin = ForwardSpeed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
            foreach (var w in wheels)
            {
                if (w.pivot == null) continue;
                float dist = w.dist > 0f ? w.dist : suspensionRest + wheelRadius;
                w.pivot.localPosition = new Vector3(0f, -(dist - wheelRadius), 0f);
                w.pivot.localRotation = Quaternion.Euler(0f, w.front ? steerAngle : 0f, 0f);
                if (w.spinner != null) w.spinner.Rotate(spin, 0f, 0f, Space.Self);
            }
        }

        public void ResetTo(Vector3 pos, Quaternion rot)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
            transform.SetPositionAndRotation(pos, rot);
            steerAngle = 0f;
            Turbo = 1f;
        }
    }
}
