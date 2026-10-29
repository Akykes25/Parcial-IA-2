using System.Collections.Generic;
using UnityEngine;

namespace Parcial2.Wolves
{
    public enum PatrolMode
    {
        Loop,
        PingPong
    }

    public class PatrolRoute : MonoBehaviour
    {
        [SerializeField] private PatrolMode mode = PatrolMode.Loop;
        [SerializeField] private List<Transform> waypoints = new();

        public PatrolMode Mode => mode;
        public IReadOnlyList<Transform> Waypoints => waypoints;

        public void Configure(PatrolMode patrolMode, IEnumerable<Transform> points)
        {
            mode = patrolMode;
            waypoints.Clear();
            waypoints.AddRange(points);
        }

        private void OnDrawGizmos()
        {
            if (waypoints.Count < 2)
                return;

            Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.8f);
            for (int i = 0; i < waypoints.Count - 1; i++)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);

            if (mode == PatrolMode.Loop)
                Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
        }
    }
}
