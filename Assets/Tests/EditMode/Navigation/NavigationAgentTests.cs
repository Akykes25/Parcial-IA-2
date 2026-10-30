using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Core;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Tests
{
    public sealed class NavigationAgentTests
    {
        private const float Speed = 4f;

        private readonly List<GameObject> created = new();
        private NavigationGraph graph;
        private NavigationAgent agent;

        [SetUp]
        public void SetUp()
        {
            // Start (0,0,0) -> Side (6,0,5) -> Goal (0,0,10). The wall at z = 5 blocks the straight line.
            GraphNode start = CreateNode("Start", new Vector3(0f, 0f, 0f));
            GraphNode side = CreateNode("Side", new Vector3(6f, 0f, 5f));
            GraphNode goal = CreateNode("Goal", new Vector3(0f, 0f, 10f));
            GraphNode far = CreateNode("Far", new Vector3(-1f, 0f, 14f));
            start.SetNeighbors(new[] { side });
            side.SetNeighbors(new[] { start, goal, far });
            goal.SetNeighbors(new[] { side });
            far.SetNeighbors(new[] { side });

            graph = Track(new GameObject("Graph")).AddComponent<NavigationGraph>();
            graph.SetNodes(new[] { start, side, goal, far });
            agent = new NavigationAgent(graph, 0.4f, 0.5f, 1f, GameLayers.ObstacleMask);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                Object.DestroyImmediate(go);
            created.Clear();
        }

        [Test]
        public void Tick_WithClearRoute_SteersDirectlyWithoutPathfinding()
        {
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            Vector3 velocity = agent.Tick(Vector3.zero, Speed, 0f);

            Assert.That(agent.PathRequests, Is.EqualTo(0));
            Assert.That(agent.IsFollowingPath, Is.False);
            Assert.That(velocity.normalized.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Tick_WithBlockedRoute_FollowsAStarPath()
        {
            CreateWall();
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            Vector3 velocity = agent.Tick(Vector3.zero, Speed, 0f);

            Assert.That(agent.PathRequests, Is.EqualTo(1));
            Assert.That(agent.IsFollowingPath, Is.True);
            Assert.That(agent.Path.Count, Is.EqualTo(3));
            Assert.That(velocity.x, Is.GreaterThan(0f), "Should head to the side node around the wall.");
        }

        [Test]
        public void Tick_DoesNotRepathWhileDestinationKeepsItsNode()
        {
            CreateWall();
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);

            agent.SetDestination(new Vector3(0.3f, 0f, 10.2f));
            agent.Tick(new Vector3(1f, 0f, 1f), Speed, 0.5f);

            Assert.That(agent.PathRequests, Is.EqualTo(1));
        }

        [Test]
        public void Tick_RepathsWhenDestinationChangesNode()
        {
            CreateWall();
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);

            agent.SetDestination(new Vector3(-1f, 0f, 14f));
            agent.Tick(Vector3.zero, Speed, 1f);

            Assert.That(agent.PathRequests, Is.EqualTo(2));
            Assert.That(agent.Path[^1].name, Is.EqualTo("Far"));
        }

        [Test]
        public void Tick_WithNodeFilter_DoesNotUseFilteredNodes()
        {
            CreateWall();
            agent.NodeFilter = node => node.name != "Side";
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            Vector3 velocity = agent.Tick(Vector3.zero, Speed, 0f);
            agent.Tick(Vector3.zero, Speed, 1f);

            Assert.That(agent.IsFollowingPath, Is.False);
            Assert.That(velocity, Is.EqualTo(Vector3.zero));
            Assert.That(agent.PathRequests, Is.EqualTo(1), "A failed search must not be retried every frame.");
        }

        [Test]
        public void Tick_RepathsWhenStuckOnPath()
        {
            CreateWall();
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);

            agent.Tick(Vector3.zero, Speed, 0.5f);
            Assert.That(agent.PathRequests, Is.EqualTo(1), "Not stuck yet.");

            agent.Tick(Vector3.zero, Speed, 1f);
            Assert.That(agent.PathRequests, Is.EqualTo(2));
        }

        [Test]
        public void Tick_DoesNotRepathWhileMakingProgress()
        {
            CreateWall();
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);

            agent.Tick(new Vector3(1f, 0f, 1f), Speed, 0.6f);
            agent.Tick(new Vector3(2f, 0f, 2f), Speed, 1.2f);

            Assert.That(agent.PathRequests, Is.EqualTo(1));
        }

        [Test]
        public void TryGetPathCost_ReturnsPathLength()
        {
            bool found = agent.TryGetPathCost(Vector3.zero, new Vector3(0f, 0f, 10f), out float cost);

            Assert.That(found, Is.True);
            Assert.That(cost, Is.EqualTo(2f * Mathf.Sqrt(61f)).Within(0.01f));
        }

        [Test]
        public void Stop_ClearsDestinationAndVelocity()
        {
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Stop();

            Assert.That(agent.HasDestination, Is.False);
            Assert.That(agent.Tick(Vector3.zero, Speed, 0f), Is.EqualTo(Vector3.zero));
        }

        private GraphNode CreateNode(string name, Vector3 position)
        {
            GameObject go = Track(new GameObject(name));
            go.transform.position = position;
            return go.AddComponent<GraphNode>();
        }

        private void CreateWall()
        {
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.layer = GameLayers.Obstacle;
            wall.transform.position = new Vector3(0f, 0f, 5f);
            wall.transform.localScale = new Vector3(4f, 2f, 0.5f);
            Physics.SyncTransforms();
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }
    }
}
