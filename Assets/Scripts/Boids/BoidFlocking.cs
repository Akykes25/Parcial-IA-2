using System.Collections.Generic;
using Parcial2.Core;
using Parcial2.Perception;
using Parcial2.Steering;
using UnityEngine;

namespace Parcial2.Boids
{
    [DisallowMultipleComponent]
    public sealed class BoidFlocking : MonoBehaviour
    {
        [Header("Perception")]
        [SerializeField, Min(0.1f)] private float perceptionRadius = 7.5f;
        [SerializeField, Min(0.1f)] private float separationRadius = 2f;
        [SerializeField] private LayerMask boidLayerMask = GameLayers.BoidMask;

        [Header("Flocking weights")]
        [SerializeField, Min(0f)] private float separationWeight = 2.2f;
        [SerializeField, Min(0f)] private float alignmentWeight = 1f;
        [SerializeField, Min(0f)] private float cohesionWeight = 0.85f;

        private readonly HashSet<BoidAgent> sensedBoids = new HashSet<BoidAgent>();
        private PhysicsSensorBuffer sensorBuffer;

        private void Awake()
        {
            sensorBuffer = new PhysicsSensorBuffer();
        }

        public Vector3 CalculateSteering()
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int colliderCount = sensorBuffer.Query(transform.position, perceptionRadius, boidLayerMask.value);
            sensedBoids.Clear();

            for (int i = 0; i < colliderCount; i++)
            {
                Collider nearbyCollider = sensorBuffer[i];
                if (nearbyCollider == null)
                    continue;

                BoidAgent neighbour = nearbyCollider.GetComponent<BoidAgent>();
                if (neighbour == null || neighbour.transform == transform || !neighbour.IsAlive
                    || !sensedBoids.Add(neighbour))
                {
                    continue;
                }

                Vector3 offsetFromNeighbour = transform.position - neighbour.transform.position;
                offsetFromNeighbour.y = 0f;
                float distance = offsetFromNeighbour.magnitude;

                if (distance > 0.01f && distance <= separationRadius)
                    separation += offsetFromNeighbour.normalized / distance;

                alignment += SteeringBehaviours.FlatDirection(neighbour.Velocity);
                cohesion += neighbour.transform.position;
            }

            if (sensedBoids.Count == 0)
                return Vector3.zero;

            alignment = SteeringBehaviours.FlatDirection(alignment / sensedBoids.Count);
            Vector3 localCentre = cohesion / sensedBoids.Count;
            cohesion = SteeringBehaviours.Seek(transform.position, localCentre);
            separation = SteeringBehaviours.FlatDirection(separation);

            return separation * separationWeight
                + alignment * alignmentWeight
                + cohesion * cohesionWeight;
        }

        private void OnDrawGizmos()
        {
            Parcial2.Feedback.FeedbackGizmos.DrawCircle(transform.position, perceptionRadius,
                new Color(0.1f, 0.8f, 1f, 0.25f));
            Parcial2.Feedback.FeedbackGizmos.DrawCircle(transform.position, separationRadius,
                new Color(0.4f, 0.4f, 1f, 0.6f));
        }
    }
}
