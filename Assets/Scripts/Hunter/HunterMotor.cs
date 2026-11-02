using UnityEngine;

namespace Parcial2.Hunter
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HunterMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float acceleration = 60f;
        [SerializeField, Min(0f)] private float turnSpeed = 10f;

        private Rigidbody body;

        public Vector3 DesiredVelocity { get; set; }
        public Vector3 Velocity => body.isKinematic ? Vector3.zero : body.linearVelocity;
        public Vector3 Position => body.position;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        private void FixedUpdate()
        {
            if (body.isKinematic)
                return;

            Vector3 desired = DesiredVelocity;
            desired.y = 0f;
            Vector3 velocity = Vector3.MoveTowards(body.linearVelocity, desired, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;

            if (velocity.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z));
                body.MoveRotation(Quaternion.Slerp(body.rotation, look, turnSpeed * Time.fixedDeltaTime));
            }
        }

        public void FaceTowards(Vector3 point)
        {
            Vector3 offset = point - body.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.0001f)
                body.MoveRotation(Quaternion.LookRotation(offset));
        }

        public void Teleport(Vector3 position)
        {
            DesiredVelocity = Vector3.zero;
            if (!body.isKinematic)
                body.linearVelocity = Vector3.zero;

            body.position = position;
            transform.position = position;
        }

        public void SetSimulated(bool simulated)
        {
            DesiredVelocity = Vector3.zero;
            if (!body.isKinematic)
                body.linearVelocity = Vector3.zero;

            body.isKinematic = !simulated;
            body.detectCollisions = simulated;
        }
    }
}
