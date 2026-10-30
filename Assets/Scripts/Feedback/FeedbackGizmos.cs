using System.Collections.Generic;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Feedback
{
    /// <summary>Gizmo helpers shared by the Hunter and wolf feedback components.</summary>
    public static class FeedbackGizmos
    {
        private const int CircleSegments = 40;

        public static void DrawCircle(Vector3 center, float radius, Color color)
        {
            Gizmos.color = color;
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        /// <summary>Cyan A* path when pathfinding is in use, a faint marker when the route is clear.</summary>
        public static void DrawPath(NavigationAgent navigation)
        {
            if (navigation == null || !navigation.HasDestination)
                return;

            if (!navigation.IsFollowingPath)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.5f);
                Gizmos.DrawWireSphere(navigation.Destination, 0.3f);
                return;
            }

            IReadOnlyList<GraphNode> path = navigation.Path;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i].Position, path[i + 1].Position);
            Gizmos.DrawWireSphere(navigation.Destination, 0.3f);
        }
    }
}
