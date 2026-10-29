using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parcial2.Navigation
{
    public class NavigationGraph : MonoBehaviour
    {
        [SerializeField] private List<GraphNode> nodes = new();

        public IReadOnlyList<GraphNode> Nodes => nodes;

        public void SetNodes(IEnumerable<GraphNode> graphNodes)
        {
            nodes.Clear();
            nodes.AddRange(graphNodes);
        }

        public GraphNode GetClosestNode(Vector3 position, Predicate<GraphNode> filter = null)
        {
            GraphNode closest = null;
            float closestSqrDistance = float.MaxValue;

            foreach (GraphNode node in nodes)
            {
                if (filter != null && !filter(node))
                    continue;

                float sqrDistance = (node.Position - position).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closest = node;
            }

            return closest;
        }
    }
}
