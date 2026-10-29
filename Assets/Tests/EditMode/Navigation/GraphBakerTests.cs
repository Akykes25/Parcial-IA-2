using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Core;
using Parcial2.Editor;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Tests.Navigation
{
    public class GraphBakerTests
    {
        private readonly List<GameObject> created = new();
        private NavigationGraph graph;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("Graph");
            created.Add(go);
            graph = go.AddComponent<NavigationGraph>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                Object.DestroyImmediate(go);
            created.Clear();
        }

        private GraphNode Node(float x, float z)
        {
            var go = new GameObject($"Node({x},{z})");
            created.Add(go);
            go.transform.position = new Vector3(x, 0f, z);
            return go.AddComponent<GraphNode>();
        }

        private void Wall(float x, float z, float sizeX, float sizeZ)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.Add(wall);
            wall.layer = GameLayers.Obstacle;
            wall.transform.position = new Vector3(x, 1f, z);
            wall.transform.localScale = new Vector3(sizeX, 2f, sizeZ);
        }

        [Test]
        public void LinksNodesWithClearLineInBothDirections()
        {
            GraphNode a = Node(0, 0);
            GraphNode b = Node(5, 0);
            graph.SetNodes(new[] { a, b });

            GraphBaker.Bake(graph, maxLinkDistance: 8f, clearanceRadius: 0.2f);

            CollectionAssert.Contains(a.Neighbors, b);
            CollectionAssert.Contains(b.Neighbors, a);
        }

        [Test]
        public void DoesNotLinkNodesSeparatedByAWall()
        {
            GraphNode a = Node(0, 0);
            GraphNode b = Node(6, 0);
            Wall(3, 0, 0.5f, 4f);
            graph.SetNodes(new[] { a, b });

            GraphBaker.Bake(graph, maxLinkDistance: 8f, clearanceRadius: 0.2f);

            CollectionAssert.DoesNotContain(a.Neighbors, b);
            CollectionAssert.DoesNotContain(b.Neighbors, a);
        }

        [Test]
        public void DoesNotLinkNodesBeyondMaxDistance()
        {
            GraphNode a = Node(0, 0);
            GraphNode b = Node(20, 0);
            graph.SetNodes(new[] { a, b });

            GraphBaker.Bake(graph, maxLinkDistance: 8f, clearanceRadius: 0.2f);

            CollectionAssert.DoesNotContain(a.Neighbors, b);
        }

        [Test]
        public void RebakingReplacesStaleLinks()
        {
            GraphNode a = Node(0, 0);
            GraphNode b = Node(5, 0);
            graph.SetNodes(new[] { a, b });
            a.SetNeighbors(new[] { b });
            Wall(2.5f, 0, 0.5f, 4f);

            GraphBaker.Bake(graph, maxLinkDistance: 8f, clearanceRadius: 0.2f);

            CollectionAssert.DoesNotContain(a.Neighbors, b);
        }
    }
}
