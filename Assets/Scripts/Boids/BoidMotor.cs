using Parcial2.Core;
using UnityEngine;

namespace Parcial2.Boids
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoidMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maxSpeed = 4.2f;
        [SerializeField, Min(0.1f)] private float acceleration = 6f;
        [SerializeField, Min(0f)] private float boundaryMargin = 0.65f;
        [SerializeField, Min(0f)] private float turnSpeed = 8f;

        private Rigidbody body;
        private Vector3 velocity;

        public Vector3 Velocity => velocity;
        public float MaxSpeed => maxSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Initialize(ZoneVolume zone, Vector3 initialVelocity)
        {
            velocity = FlattenAndClamp(initialVelocity, maxSpeed);
            body.position = ConstrainPosition(zone, body.position, ref velocity, boundaryMargin);
        }

        public void Move(Vector3 desiredVelocity, ZoneVolume zone, float deltaTime)
        {
            desiredVelocity.y = 0f;
            velocity = Vector3.MoveTowards(
                velocity,
                Vector3.ClampMagnitude(desiredVelocity, maxSpeed),
                acceleration * deltaTime);

            Vector3 proposedPosition = body.position + velocity * deltaTime;
            Vector3 constrainedPosition = ConstrainPosition(zone, proposedPosition, ref velocity, boundaryMargin);
            body.MovePosition(constrainedPosition);

            Vector3 flatVelocity = FlattenAndClamp(velocity, maxSpeed);
            if (flatVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatVelocity.normalized);
                body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * deltaTime));
            }
        }

        public void Stop()
        {
            velocity = Vector3.zero;
        }

        public void RespawnInside(ZoneVolume zone, float height)
        {
            if (zone == null)
            {
                Stop();
                return;
            }

            Bounds bounds = zone.Bounds;
            float halfWidth = Mathf.Max(0f, bounds.extents.x - boundaryMargin);
            float halfDepth = Mathf.Max(0f, bounds.extents.z - boundaryMargin);
            Vector3 position = new Vector3(
                Random.Range(bounds.center.x - halfWidth, bounds.center.x + halfWidth),
                height,
                Random.Range(bounds.center.z - halfDepth, bounds.center.z + halfDepth));

            body.position = ConstrainPosition(zone, position, ref velocity, boundaryMargin);
            velocity = FlattenAndClamp(Random.insideUnitSphere, maxSpeed * 0.7f);
            if (velocity.sqrMagnitude < 0.01f)
                velocity = Vector3.forward * (maxSpeed * 0.7f);
        }

        public static Vector3 ConstrainPosition(
            ZoneVolume zone,
            Vector3 proposedPosition,
            ref Vector3 velocity,
            float margin)
        {
            if (zone == null)
                return proposedPosition;

            Bounds bounds = zone.Bounds;
            float safeMargin = Mathf.Clamp(
                margin,
                0f,
                Mathf.Max(0f, Mathf.Min(bounds.extents.x, bounds.extents.z) - 0.01f));
            Vector3 constrained = zone.ClampInside(proposedPosition, safeMargin);

            if (!Mathf.Approximately(constrained.x, proposedPosition.x))
                velocity.x = constrained.x < proposedPosition.x ? -Mathf.Abs(velocity.x) : Mathf.Abs(velocity.x);

            if (!Mathf.Approximately(constrained.z, proposedPosition.z))
                velocity.z = constrained.z < proposedPosition.z ? -Mathf.Abs(velocity.z) : Mathf.Abs(velocity.z);

            return constrained;
        }

        private static Vector3 FlattenAndClamp(Vector3 value, float speed)
        {
            value.y = 0f;
            return Vector3.ClampMagnitude(value, Mathf.Max(0f, speed));
        }
    }
}
