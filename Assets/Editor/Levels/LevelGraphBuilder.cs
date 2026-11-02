using System.Collections.Generic;
using Parcial2.Core;
using Parcial2.Navigation;
using UnityEditor;
using UnityEngine;

namespace Parcial2.Editor.Levels
{
    internal static class LevelGraphBuilder
    {
        public static List<GraphNode> Build(NavigationGraph graph, IReadOnlyList<NodeSpec> specs,
            IReadOnlyList<(int a, int b)> links, float nodeHeight)
        {
            var nodes = new List<GraphNode>(specs.Count);
            foreach (NodeSpec spec in specs)
            {
                var go = new GameObject(spec.Name);
                go.transform.SetParent(graph.transform, false);
                go.transform.position = new Vector3(spec.Position.x, nodeHeight, spec.Position.y);
                GraphNode node = go.AddComponent<GraphNode>();
                node.SetSafeZone(spec.IsSafeZone);
                nodes.Add(node);
            }

            var neighbors = new List<List<GraphNode>>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
                neighbors.Add(new List<GraphNode>());

            foreach ((int a, int b) in links)
            {
                neighbors[a].Add(nodes[b]);
                neighbors[b].Add(nodes[a]);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].SetNeighbors(neighbors[i]);
                EditorUtility.SetDirty(nodes[i]);
            }

            graph.SetNodes(nodes);
            EditorUtility.SetDirty(graph);
            return nodes;
        }

        public static int ReportBlocked(IReadOnlyList<GraphNode> nodes, float bodyRadius)
        {
            Physics.SyncTransforms();
            int problems = 0;
            foreach (GraphNode node in nodes)
            {
                if (Physics.CheckSphere(node.Position, bodyRadius, GameLayers.ObstacleMask))
                {
                    Debug.LogError($"{node.name} overlaps an obstacle.", node);
                    problems++;
                }

                foreach (GraphNode neighbor in node.Neighbors)
                {
                    if (string.CompareOrdinal(node.name, neighbor.name) > 0)
                        continue;

                    Vector3 offset = neighbor.Position - node.Position;
                    if (Physics.SphereCast(node.Position, bodyRadius, offset.normalized, out RaycastHit hit,
                            offset.magnitude, GameLayers.ObstacleMask))
                    {
                        Debug.LogError($"Link {node.name} - {neighbor.name} is blocked by {hit.collider.name}.", node);
                        problems++;
                    }
                }
            }

            return problems;
        }
    }
}
