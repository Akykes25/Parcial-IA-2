using UnityEngine;

namespace Parcial2.Steering
{
    public static class SteeringBehaviours
    {
        public static Vector3 Seek(Vector3 position, Vector3 target)
        {
            return FlatDirection(target - position);
        }

        public static Vector3 Arrive(Vector3 position, Vector3 target, float maxSpeed, float slowingRadius)
        {
            Vector3 offset = target - position;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance < 0.01f)
                return Vector3.zero;

            float safeSlowingRadius = Mathf.Max(0.01f, slowingRadius);
            float desiredSpeed = distance < safeSlowingRadius
                ? maxSpeed * (distance / safeSlowingRadius)
                : maxSpeed;

            return offset.normalized * desiredSpeed;
        }

        public static Vector3 Pursuit(Vector3 position, float speed, Vector3 targetPosition,
            Vector3 targetVelocity, float maxPrediction)
        {
            Vector3 futurePosition = PredictPosition(position, speed, targetPosition, targetVelocity, maxPrediction);
            return Seek(position, futurePosition);
        }

        public static Vector3 Evade(Vector3 position, Vector3 pursuerPosition, Vector3 pursuerVelocity,
            float maxPrediction)
        {
            float distance = Vector3.Distance(position, pursuerPosition);
            float pursuerSpeed = pursuerVelocity.magnitude;
            float prediction = pursuerSpeed > 0.01f ? distance / pursuerSpeed : maxPrediction;

            prediction = Mathf.Min(prediction, maxPrediction);
            Vector3 futurePosition = pursuerPosition + pursuerVelocity * prediction;
            return FlatDirection(position - futurePosition);
        }

        public static Vector3 PredictPosition(Vector3 position, float speed, Vector3 targetPosition,
            Vector3 targetVelocity, float maxPrediction)
        {
            float distance = Vector3.Distance(position, targetPosition);
            float prediction = speed > 0.01f ? Mathf.Min(distance / speed, maxPrediction) : maxPrediction;
            return targetPosition + targetVelocity * prediction;
        }

        public static Vector3 FlatDirection(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.zero;
        }
    }
}
