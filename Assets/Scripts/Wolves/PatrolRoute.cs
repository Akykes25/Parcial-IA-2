using System.Collections.Generic;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Wolves
{
    public enum PatrolMode
    {
        Loop,
        PingPong
    }

    /// <summary>
    /// Patrol waypoints of one wolf. Waypoints are graph nodes, so an unreachable waypoint is solved with the
    /// same A* the rest of the agents use. Safe-zone nodes are not valid waypoints.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        [SerializeField] private PatrolMode mode = PatrolMode.Loop;
        [SerializeField] private List<GraphNode> waypoints = new();

        public PatrolMode Mode => mode;
        public int Count => waypoints.Count;
        public GraphNode this[int index] => waypoints[index];

        public void Configure(PatrolMode patrolMode, IEnumerable<GraphNode> nodes)
        {
            mode = patrolMode;
            waypoints.Clear();
            waypoints.AddRange(nodes);
        }

        public int ClosestIndex(Vector3 position)
        {
            int closest = 0;
            float closestSqr = float.MaxValue;
            for (int i = 0; i < waypoints.Count; i++)
            {
                float sqr = (waypoints[i].Position - position).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = i;
                }
            }

            return closest;
        }

        private void OnValidate()
        {
            foreach (GraphNode node in waypoints)
            {
                if (node != null && node.IsSafeZone)
                    Debug.LogWarning($"{name}: waypoint {node.name} is inside a safe zone; wolves cannot use it.", this);
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints.Count < 2)
                return;

            Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.8f);
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].Position, waypoints[i + 1].Position);
            }

            if (mode == PatrolMode.Loop && waypoints[0] != null && waypoints[^1] != null)
                Gizmos.DrawLine(waypoints[^1].Position, waypoints[0].Position);
        }
    }
}
