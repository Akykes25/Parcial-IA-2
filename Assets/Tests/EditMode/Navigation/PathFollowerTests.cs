using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Tests.Navigation
{
    public class PathFollowerTests
    {
        private readonly List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                Object.DestroyImmediate(go);
            created.Clear();
        }

        private List<GraphNode> StraightPath(params float[] xs)
        {
            var result = new List<GraphNode>();
            foreach (float x in xs)
            {
                var go = new GameObject($"Node{x}");
                created.Add(go);
                go.transform.position = new Vector3(x, 0f, 0f);
                result.Add(go.AddComponent<GraphNode>());
            }

            return result;
        }

        [Test]
        public void SteersTowardsTheFirstWaypoint()
        {
            var follower = new PathFollower(reachDistance: 0.5f, slowingRadius: 1f);
            follower.SetPath(StraightPath(5, 10));

            Vector3 velocity = follower.Evaluate(Vector3.zero, 4f);

            Assert.AreEqual(4f, velocity.magnitude, 0.001f);
            Assert.AreEqual(1f, velocity.normalized.x, 0.001f);
        }

        [Test]
        public void AdvancesToNextWaypointWhenReached()
        {
            var follower = new PathFollower(reachDistance: 0.5f, slowingRadius: 1f);
            follower.SetPath(StraightPath(5, 10));

            follower.Evaluate(new Vector3(4.8f, 0f, 0f), 4f);

            Assert.AreEqual(10f, follower.CurrentTarget.x, 0.001f);
        }

        [Test]
        public void CompletesAndStopsAtTheLastWaypoint()
        {
            var follower = new PathFollower(reachDistance: 0.5f, slowingRadius: 1f);
            follower.SetPath(StraightPath(5));

            Vector3 velocity = follower.Evaluate(new Vector3(4.9f, 0f, 0f), 4f);

            Assert.IsTrue(follower.IsComplete);
            Assert.AreEqual(Vector3.zero, velocity);
        }

        [Test]
        public void SlowsDownOnApproachToTheFinalWaypoint()
        {
            var follower = new PathFollower(reachDistance: 0.1f, slowingRadius: 2f);
            follower.SetPath(StraightPath(10));

            Vector3 velocity = follower.Evaluate(new Vector3(9f, 0f, 0f), 4f);

            Assert.Less(velocity.magnitude, 4f);
            Assert.Greater(velocity.magnitude, 0f);
        }

        [Test]
        public void EmptyPathIsCompleteAndYieldsNoVelocity()
        {
            var follower = new PathFollower(reachDistance: 0.5f, slowingRadius: 1f);

            Assert.IsTrue(follower.IsComplete);
            Assert.AreEqual(Vector3.zero, follower.Evaluate(Vector3.zero, 4f));
        }

        [Test]
        public void ClearDropsTheCurrentPath()
        {
            var follower = new PathFollower(reachDistance: 0.5f, slowingRadius: 1f);
            follower.SetPath(StraightPath(5));

            follower.Clear();

            Assert.IsTrue(follower.IsComplete);
        }

        [Test]
        public void SteerDirectIgnoresThePathAndHeadsForTheVisibleTarget()
        {
            var follower = new PathFollower(reachDistance: 0.5f, slowingRadius: 1f);
            follower.SetPath(StraightPath(-5));

            Vector3 velocity = follower.SteerDirect(Vector3.zero, new Vector3(0f, 0f, 10f), 3f);

            Assert.AreEqual(3f, velocity.magnitude, 0.001f);
            Assert.AreEqual(1f, velocity.normalized.z, 0.001f);
        }
    }
}
