using System;
using System.Collections.Generic;
using Parcial2.Core;
using UnityEngine;

namespace Parcial2.Perception
{
    /// <summary>
    /// Periodic vision scan: overlap sphere on the target layers, then range, view angle and a line-of-sight
    /// raycast for each candidate only. Agents read the results instead of querying physics themselves.
    /// </summary>
    public class VisionSensor : MonoBehaviour
    {
        private const int CircleSegments = 32;

        [SerializeField, Min(0f)] private float range = 10f;
        [SerializeField, Range(1f, VisionQuery.FullAngle)] private float viewAngle = VisionQuery.FullAngle;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private LayerMask obstacleMask = GameLayers.ObstacleMask;
        [SerializeField, Min(0f)] private float eyeHeight = 0.5f;
        [SerializeField, Min(0f)] private float scanInterval = 0.1f;
        [SerializeField] private Color gizmoColor = new(1f, 0.9f, 0.2f);

        private readonly PhysicsSensorBuffer buffer = new();
        private readonly List<Transform> visibleTargets = new();
        private float nextScanTime;

        public float Range => range;
        public float ViewAngle => viewAngle;
        public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
        public IReadOnlyList<Transform> VisibleTargets => visibleTargets;
        public Transform ClosestTarget { get; private set; }
        public bool HasTarget => ClosestTarget != null;

        /// <summary>Optional veto, e.g. wolves ignore a Hunter sheltered in a safe zone.</summary>
        public Predicate<Transform> TargetFilter { get; set; }

        public void Configure(float maxDistance, float angle, int targets, int obstacles)
        {
            range = Mathf.Max(0f, maxDistance);
            viewAngle = Mathf.Clamp(angle, 1f, VisionQuery.FullAngle);
            targetMask = targets;
            obstacleMask = obstacles;
        }

        private void OnEnable()
        {
            // Stagger sensors so they do not all query physics on the same frame.
            nextScanTime = Time.time + UnityEngine.Random.Range(0f, scanInterval);
        }

        private void Update()
        {
            if (Time.time < nextScanTime)
                return;

            nextScanTime = Time.time + scanInterval;
            Scan();
        }

        public void Scan()
        {
            visibleTargets.Clear();
            ClosestTarget = null;

            Vector3 eye = EyePosition;
            Vector3 forward = transform.forward;
            float closestSqr = float.MaxValue;
            int count = buffer.Query(eye, range, targetMask);

            for (int i = 0; i < count; i++)
            {
                Collider candidate = buffer[i];
                Transform target = candidate.attachedRigidbody != null
                    ? candidate.attachedRigidbody.transform
                    : candidate.transform;

                if (target.IsChildOf(transform) || visibleTargets.Contains(target))
                    continue;
                if (TargetFilter != null && !TargetFilter(target))
                    continue;

                Vector3 point = candidate.bounds.center;
                if (!VisionQuery.CanSee(eye, forward, point, range, viewAngle, obstacleMask))
                    continue;

                visibleTargets.Add(target);
                float sqrDistance = (point - eye).sqrMagnitude;
                if (sqrDistance < closestSqr)
                {
                    closestSqr = sqrDistance;
                    ClosestTarget = target;
                }
            }
        }

        public bool CanSee(Vector3 point)
        {
            return VisionQuery.CanSee(EyePosition, transform.forward, point, range, viewAngle, obstacleMask);
        }

        private void OnDrawGizmos()
        {
            Vector3 eye = EyePosition;
            Gizmos.color = gizmoColor;

            if (viewAngle >= VisionQuery.FullAngle)
                DrawArc(eye, transform.forward, VisionQuery.FullAngle);
            else
            {
                Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * range;
                Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * range;
                Gizmos.DrawLine(eye, eye + left);
                Gizmos.DrawLine(eye, eye + right);
                DrawArc(eye, transform.forward, viewAngle);
            }

            foreach (Transform target in visibleTargets)
            {
                if (target != null)
                    Gizmos.DrawLine(eye, target.position);
            }
        }

        private void DrawArc(Vector3 center, Vector3 forward, float angle)
        {
            int segments = Mathf.Max(2, Mathf.CeilToInt(CircleSegments * angle / VisionQuery.FullAngle));
            float step = angle / segments;
            Vector3 previous = center + Quaternion.Euler(0f, -angle * 0.5f, 0f) * forward * range;

            for (int i = 1; i <= segments; i++)
            {
                Vector3 next = center + Quaternion.Euler(0f, -angle * 0.5f + step * i, 0f) * forward * range;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
