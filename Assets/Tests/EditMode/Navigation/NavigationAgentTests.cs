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
        private static readonly Vector3 SidePosition = new(6f, 0f, 5f);

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
            Assert.That(agent.Path.Count, Is.EqualTo(2), "The start node is skipped: Side is already reachable.");
            Assert.That(agent.Path[0].name, Is.EqualTo("Side"));
            Assert.That(velocity.x, Is.GreaterThan(0f), "Should head to the side node around the wall.");
        }

        [Test]
        public void Tick_WhenSecondNodeIsHidden_KeepsStartNode()
        {
            CreateWall();
            // From behind the wall's west end, Side (6, 0, 5) is out of reach in a straight line.
            GameObject block = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            block.layer = GameLayers.Obstacle;
            block.transform.position = new Vector3(3f, 0f, 2f);
            block.transform.localScale = new Vector3(0.5f, 2f, 3f);
            Physics.SyncTransforms();
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            agent.Tick(Vector3.zero, Speed, 0f);

            Assert.That(agent.Path[0].name, Is.EqualTo("Start"));
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
            Assert.That(agent.Path[0].name, Is.EqualTo("Start"),
                "Recovery heads for the closest node instead of the shortcut that got the body stuck.");
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
        public void Tick_WhenStuckOnDirectRoute_FallsBackToAStar()
        {
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);
            agent.Tick(Vector3.zero, Speed, 0.5f);
            Assert.That(agent.IsFollowingPath, Is.False, "Not stuck yet.");

            agent.Tick(Vector3.zero, Speed, 1f);

            Assert.That(agent.IsFollowingPath, Is.True);
            Assert.That(agent.PathRequests, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterDirectRouteFallback_KeepsPathUntilCooldownEnds()
        {
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);
            agent.Tick(Vector3.zero, Speed, 1f);

            agent.Tick(new Vector3(0.5f, 0f, 0.5f), Speed, 1.5f);
            Assert.That(agent.IsFollowingPath, Is.True, "Direct route stays suppressed during the cooldown.");

            agent.Tick(new Vector3(1f, 0f, 1f), Speed, 4f);
            Assert.That(agent.IsFollowingPath, Is.False, "Direct route is allowed again once the cooldown ends.");
        }

        [Test]
        public void Tick_SlowingNearDestination_IsNotStuck()
        {
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            Vector3 nearGoal = new(0f, 0f, 9.5f);

            agent.Tick(nearGoal, Speed, 0f);
            agent.Tick(nearGoal, Speed, 1f);
            agent.Tick(nearGoal, Speed, 2f);

            Assert.That(agent.PathRequests, Is.EqualTo(0));
            Assert.That(agent.IsFollowingPath, Is.False);
        }

        [Test]
        public void TryGetPathCost_ReturnsPathLength()
        {
            bool found = agent.TryGetPathCost(Vector3.zero, new Vector3(0f, 0f, 10f), out float cost);

            Assert.That(found, Is.True);
            Assert.That(cost, Is.EqualTo(2f * Mathf.Sqrt(61f)).Within(0.01f));
        }

        [Test]
        public void Tick_WithThreatOnDirectLine_PlansWithAStar()
        {
            agent.Threats = new StubThreats { SegmentClear = false };
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            agent.Tick(Vector3.zero, Speed, 0f);

            Assert.That(agent.IsFollowingPath, Is.True);
        }

        [Test]
        public void Tick_WhenOnlyRoutePassesAThreat_FallsBackToIt()
        {
            CreateWall();
            agent.Threats = new StubThreats { Segment = (from, to) => to != SidePosition };
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            agent.Tick(Vector3.zero, Speed, 0f);

            Assert.That(agent.IsFollowingPath, Is.True);
            Assert.That(agent.IsAvoidingThreats, Is.False);
        }

        [Test]
        public void TryGetPathCost_AvoidingThreats_IgnoresRoutesThroughThem()
        {
            agent.Threats = new StubThreats { Segment = (from, to) => to != SidePosition };

            Assert.That(agent.TryGetPathCost(Vector3.zero, new Vector3(0f, 0f, 10f), out _, avoidThreats: true),
                Is.False);
            Assert.That(agent.TryGetPathCost(Vector3.zero, new Vector3(0f, 0f, 10f), out _), Is.True);
        }

        [Test]
        public void Tick_WithThreatBehind_DoesNotStartPathTowardsIt()
        {
            CreateWall();
            // The threat sits at the origin: stepping back to the Start node means walking into it.
            agent.Threats = new StubThreats { Segment = (from, to) => to != Vector3.zero };
            agent.SetDestination(new Vector3(0f, 0f, 10f));

            agent.Tick(new Vector3(0f, 0f, 2f), Speed, 0f);

            Assert.That(agent.Path[0].name, Is.EqualTo("Side"));
        }

        [TestCase(3f, true, TestName = "Tick_ShortDetourAroundThreat_IsTaken")]
        [TestCase(30f, false, TestName = "Tick_LongDetourAroundThreat_IsRejected")]
        public void Tick_ThreatDetour_IsCappedByLength(float detourX, bool expectAvoiding)
        {
            CreateWall();
            GraphNode a = CreateNode("A", Vector3.zero);
            GraphNode b = CreateNode("B", new Vector3(0f, 0f, 10f));
            GraphNode detour = CreateNode("Detour", new Vector3(detourX, 0f, 5f));
            a.SetNeighbors(new[] { b, detour });
            b.SetNeighbors(new[] { a, detour });
            detour.SetNeighbors(new[] { a, b });
            NavigationGraph detourGraph = Track(new GameObject("DetourGraph")).AddComponent<NavigationGraph>();
            detourGraph.SetNodes(new[] { a, b, detour });
            var detourAgent = new NavigationAgent(detourGraph, 0.4f, 0.5f, 1f, GameLayers.ObstacleMask)
            {
                // The direct edge A -> B runs into the threat.
                Threats = new StubThreats { Segment = (from, to) => !(from == Vector3.zero && to == b.Position) }
            };
            detourAgent.SetDestination(b.Position);

            detourAgent.Tick(Vector3.zero, Speed, 0f);

            Assert.That(detourAgent.IsAvoidingThreats, Is.EqualTo(expectAvoiding));
            Assert.That(detourAgent.IsFollowingPath, Is.True);
        }

        [Test]
        public void InvalidatePath_PlansAgainOnNextTick()
        {
            CreateWall();
            agent.SetDestination(new Vector3(0f, 0f, 10f));
            agent.Tick(Vector3.zero, Speed, 0f);

            agent.InvalidatePath();
            agent.Tick(new Vector3(1f, 0f, 1f), Speed, 0.1f);

            Assert.That(agent.PathRequests, Is.EqualTo(2));
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

        private sealed class StubThreats : INavigationThreats
        {
            public bool SegmentClear = true;
            public System.Func<Vector3, Vector3, bool> Segment;

            public bool IsSegmentClear(Vector3 from, Vector3 to) => Segment?.Invoke(from, to) ?? SegmentClear;
        }
    }
}
