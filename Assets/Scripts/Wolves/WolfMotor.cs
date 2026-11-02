using UnityEngine;

namespace Parcial2.Wolves
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WolfMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float acceleration = 30f;
        [SerializeField, Min(0f)] private float turnSpeed = 8f;

        private Rigidbody body;

        public Vector3 DesiredVelocity { get; set; }
        public Vector3 Position => body.position;
        public Vector3 Forward => body.rotation * Vector3.forward;

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

        public void Turn(float degreesPerSecond)
        {
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, degreesPerSecond * Time.deltaTime, 0f));
        }
    }
}
