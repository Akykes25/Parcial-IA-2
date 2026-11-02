using UnityEngine;

namespace Parcial2.Perception
{
    public static class VisionQuery
    {
        public const float FullAngle = 360f;

        public static bool IsInRange(Vector3 origin, Vector3 point, float range)
        {
            return (point - origin).sqrMagnitude <= range * range;
        }

        public static bool IsInsideViewAngle(Vector3 forward, Vector3 toPoint, float viewAngle)
        {
            if (viewAngle >= FullAngle)
                return true;

            forward.y = 0f;
            toPoint.y = 0f;
            if (toPoint.sqrMagnitude < Mathf.Epsilon)
                return true;

            return Vector3.Angle(forward, toPoint) <= viewAngle * 0.5f;
        }

        public static bool HasLineOfSight(Vector3 origin, Vector3 point, int obstacleMask)
        {
            Vector3 toPoint = point - origin;
            float distance = toPoint.magnitude;
            if (distance < Mathf.Epsilon)
                return true;

            return !Physics.Raycast(origin, toPoint / distance, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        public static bool CanSee(Vector3 origin, Vector3 forward, Vector3 point, float range, float viewAngle,
            int obstacleMask)
        {
            return IsInRange(origin, point, range)
                && IsInsideViewAngle(forward, point - origin, viewAngle)
                && HasLineOfSight(origin, point, obstacleMask);
        }
    }
}
