using System.Collections.Generic;
using Parcial2.Steering;
using UnityEngine;

namespace Parcial2.Navigation
{
    /// <summary>
    /// Walks an A* path using steering: seek between waypoints, arrive on the last one.
    /// Returns desired velocity; the owner decides how to move. When the target is visible the owner
    /// calls <see cref="SteerDirect"/> instead and no path is needed.
    /// </summary>
    public class PathFollower
    {
        private readonly List<Vector3> waypoints = new();
        private readonly float reachDistance;
        private readonly float slowingRadius;
        private int index;

        public PathFollower(float reachDistance, float slowingRadius)
        {
            this.reachDistance = reachDistance;
            this.slowingRadius = slowingRadius;
        }

        public bool IsComplete => index >= waypoints.Count;
        public Vector3 CurrentTarget => waypoints[index];

        public void SetPath(IReadOnlyList<GraphNode> path)
        {
            waypoints.Clear();
            foreach (GraphNode node in path)
                waypoints.Add(node.Position);

            index = 0;
        }

        public void Clear()
        {
            waypoints.Clear();
            index = 0;
        }

        public Vector3 Evaluate(Vector3 position, float maxSpeed)
        {
            AdvanceReachedWaypoints(position);
            if (IsComplete)
                return Vector3.zero;

            Vector3 target = waypoints[index];
            bool isLast = index == waypoints.Count - 1;

            return isLast
                ? SteeringBehaviours.Arrive(position, target, maxSpeed, slowingRadius)
                : SteeringBehaviours.Seek(position, target) * maxSpeed;
        }

        public Vector3 SteerDirect(Vector3 position, Vector3 target, float maxSpeed)
        {
            return SteeringBehaviours.Seek(position, target) * maxSpeed;
        }

        private void AdvanceReachedWaypoints(Vector3 position)
        {
            while (!IsComplete && FlatDistance(position, waypoints[index]) <= reachDistance)
                index++;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
