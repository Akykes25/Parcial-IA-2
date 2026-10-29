using System.Collections.Generic;
using UnityEngine;

namespace Parcial2.Navigation
{
    public class GraphNode : MonoBehaviour
    {
        [SerializeField] private bool isSafeZone;
        [SerializeField] private List<GraphNode> neighbors = new();

        public bool IsSafeZone => isSafeZone;
        public Vector3 Position => transform.position;
        public IReadOnlyList<GraphNode> Neighbors => neighbors;

        public void SetSafeZone(bool value) => isSafeZone = value;

        public void SetNeighbors(IEnumerable<GraphNode> nodes)
        {
            neighbors.Clear();
            neighbors.AddRange(nodes);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isSafeZone ? new Color(0.6f, 0.4f, 0.8f) : Color.white;
            Gizmos.DrawSphere(Position, 0.25f);

            Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            foreach (GraphNode neighbor in neighbors)
            {
                if (neighbor != null)
                    Gizmos.DrawLine(Position, neighbor.Position);
            }
        }
    }
}
