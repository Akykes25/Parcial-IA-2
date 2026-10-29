using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Tests.Navigation
{
    public class AStarPathfinderTests
    {
        private readonly List<GameObject> created = new();
        private AStarPathfinder pathfinder;
        private List<GraphNode> path;

        [SetUp]
        public void SetUp()
        {
            pathfinder = new AStarPathfinder();
            path = new List<GraphNode>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                Object.DestroyImmediate(go);
            created.Clear();
        }

        private GraphNode Node(float x, float z, bool safe = false)
        {
            var go = new GameObject($"Node({x},{z})");
            created.Add(go);
            go.transform.position = new Vector3(x, 0f, z);
            GraphNode node = go.AddComponent<GraphNode>();
            node.SetSafeZone(safe);
            return node;
        }

        private static void Link(GraphNode a, GraphNode b)
        {
            var aNeighbors = new List<GraphNode>(a.Neighbors) { b };
            var bNeighbors = new List<GraphNode>(b.Neighbors) { a };
            a.SetNeighbors(aNeighbors);
            b.SetNeighbors(bNeighbors);
        }

        [Test]
        public void FindsShortestPathOverLongerDetour()
        {
            GraphNode start = Node(0, 0);
            GraphNode mid = Node(5, 0);
            GraphNode goal = Node(10, 0);
            GraphNode detour = Node(5, 20);
            Link(start, mid);
            Link(mid, goal);
            Link(start, detour);
            Link(detour, goal);

            bool found = pathfinder.TryFindPath(start, goal, path, out float cost);

            Assert.IsTrue(found);
            CollectionAssert.AreEqual(new[] { start, mid, goal }, path);
            Assert.AreEqual(10f, cost, 0.001f);
        }

        [Test]
        public void ReturnsFalseAndEmptyPathWhenGoalIsDisconnected()
        {
            GraphNode start = Node(0, 0);
            GraphNode island = Node(50, 50);
            path.Add(start);

            bool found = pathfinder.TryFindPath(start, island, path, out float cost);

            Assert.IsFalse(found);
            Assert.IsEmpty(path);
            Assert.AreEqual(0f, cost);
        }

        [Test]
        public void StartEqualToGoalReturnsSingleNodePath()
        {
            GraphNode start = Node(0, 0);

            bool found = pathfinder.TryFindPath(start, start, path, out float cost);

            Assert.IsTrue(found);
            CollectionAssert.AreEqual(new[] { start }, path);
            Assert.AreEqual(0f, cost);
        }

        [Test]
        public void FilterExcludesNodesFromTheRoute()
        {
            GraphNode start = Node(0, 0);
            GraphNode safe = Node(5, 0, safe: true);
            GraphNode goal = Node(10, 0);
            GraphNode detour = Node(5, 8);
            Link(start, safe);
            Link(safe, goal);
            Link(start, detour);
            Link(detour, goal);

            bool found = pathfinder.TryFindPath(start, goal, path, out _, node => !node.IsSafeZone);

            Assert.IsTrue(found);
            CollectionAssert.DoesNotContain(path, safe);
            CollectionAssert.Contains(path, detour);
        }

        [Test]
        public void FilterThatBlocksTheOnlyRouteFindsNothing()
        {
            GraphNode start = Node(0, 0);
            GraphNode safe = Node(5, 0, safe: true);
            GraphNode goal = Node(10, 0);
            Link(start, safe);
            Link(safe, goal);

            bool found = pathfinder.TryFindPath(start, goal, path, out _, node => !node.IsSafeZone);

            Assert.IsFalse(found);
        }

        [Test]
        public void CanBeReusedAcrossSearches()
        {
            GraphNode a = Node(0, 0);
            GraphNode b = Node(4, 0);
            GraphNode c = Node(8, 0);
            Link(a, b);
            Link(b, c);

            Assert.IsTrue(pathfinder.TryFindPath(a, c, path, out float first));
            Assert.IsTrue(pathfinder.TryFindPath(c, a, path, out float second));

            Assert.AreEqual(first, second, 0.001f);
            CollectionAssert.AreEqual(new[] { c, b, a }, path);
        }

        [Test]
        public void PositionOverloadSnapsToClosestNodes()
        {
            var graphObject = new GameObject("Graph");
            created.Add(graphObject);
            NavigationGraph graph = graphObject.AddComponent<NavigationGraph>();
            GraphNode a = Node(0, 0);
            GraphNode b = Node(10, 0);
            Link(a, b);
            graph.SetNodes(new[] { a, b });

            bool found = pathfinder.TryFindPath(graph, new Vector3(1, 0, 1), new Vector3(9, 0, -1), path, out _);

            Assert.IsTrue(found);
            CollectionAssert.AreEqual(new[] { a, b }, path);
        }
    }
}
