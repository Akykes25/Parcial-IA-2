using System.Collections.Generic;
using Parcial2.Core;
using Parcial2.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parcial2.Editor
{
    public static class GraphBaker
    {
        private const float DefaultMaxLinkDistance = 9f;
        private const float DefaultClearanceRadius = 0.3f;

        [MenuItem("Tools/Bake Navigation Graph")]
        private static void BakeFromMenu()
        {
            NavigationGraph graph = FindGraphInActiveScene();
            if (graph == null)
            {
                Debug.LogError("No NavigationGraph found in the active scene.");
                return;
            }

            Bake(graph, DefaultMaxLinkDistance, DefaultClearanceRadius);
        }

        public static void Bake(NavigationGraph graph, float maxLinkDistance, float clearanceRadius)
        {
            Physics.SyncTransforms();

            IReadOnlyList<GraphNode> nodes = graph.Nodes;
            var links = new List<List<GraphNode>>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
                links.Add(new List<GraphNode>());

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (!CanConnect(nodes[i].Position, nodes[j].Position, maxLinkDistance, clearanceRadius))
                        continue;

                    links[i].Add(nodes[j]);
                    links[j].Add(nodes[i]);
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].SetNeighbors(links[i]);
                EditorUtility.SetDirty(nodes[i]);
            }

            EditorUtility.SetDirty(graph);
        }

        private static bool CanConnect(Vector3 from, Vector3 to, float maxDistance, float radius)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude;
            if (distance > maxDistance)
                return false;

            return !Physics.SphereCast(from, radius, offset / distance, out _, distance, GameLayers.ObstacleMask);
        }

        private static NavigationGraph FindGraphInActiveScene()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                NavigationGraph graph = root.GetComponentInChildren<NavigationGraph>();
                if (graph != null)
                    return graph;
            }

            return null;
        }
    }
}
