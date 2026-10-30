using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Core;
using Parcial2.Perception;
using UnityEngine;

namespace Parcial2.Tests
{
    public sealed class VisionSensorTests
    {
        private const float Range = 10f;

        private readonly List<GameObject> created = new();
        private VisionSensor sensor;

        [SetUp]
        public void SetUp()
        {
            GameObject eye = Track(new GameObject("Sensor"));
            sensor = eye.AddComponent<VisionSensor>();
            sensor.Configure(Range, 360f, GameLayers.BoidMask, GameLayers.ObstacleMask);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                Object.DestroyImmediate(go);
            created.Clear();
        }

        [Test]
        public void Scan_SeesTargetInsideRange()
        {
            Transform target = CreateTarget(new Vector3(0f, 0f, 5f));

            Scan();

            Assert.That(sensor.ClosestTarget, Is.SameAs(target));
            Assert.That(sensor.VisibleTargets, Is.EquivalentTo(new[] { target }));
        }

        [Test]
        public void Scan_IgnoresTargetBeyondMaxDistance()
        {
            CreateTarget(new Vector3(0f, 0f, Range + 2f));

            Scan();

            Assert.That(sensor.HasTarget, Is.False);
        }

        [Test]
        public void Scan_IgnoresTargetBehindObstacle()
        {
            CreateTarget(new Vector3(0f, 0f, 6f));
            CreateWall(new Vector3(0f, 0f, 3f));

            Scan();

            Assert.That(sensor.HasTarget, Is.False);
        }

        [Test]
        public void Scan_IgnoresTargetOutsideViewAngle()
        {
            sensor.Configure(Range, 90f, GameLayers.BoidMask, GameLayers.ObstacleMask);
            CreateTarget(new Vector3(0f, 0f, -5f));
            Transform ahead = CreateTarget(new Vector3(1f, 0f, 5f));

            Scan();

            Assert.That(sensor.VisibleTargets, Is.EquivalentTo(new[] { ahead }));
        }

        [Test]
        public void Scan_WithFullAngle_SeesTargetBehind()
        {
            Transform behind = CreateTarget(new Vector3(0f, 0f, -5f));

            Scan();

            Assert.That(sensor.ClosestTarget, Is.SameAs(behind));
        }

        [Test]
        public void Scan_PicksClosestVisibleTarget()
        {
            CreateTarget(new Vector3(0f, 0f, 8f));
            Transform near = CreateTarget(new Vector3(3f, 0f, 0f));
            CreateTarget(new Vector3(0f, 0f, -2f));
            CreateWall(new Vector3(0f, 0f, -1f));

            Scan();

            Assert.That(sensor.ClosestTarget, Is.SameAs(near));
            Assert.That(sensor.VisibleTargets.Count, Is.EqualTo(2));
        }

        [Test]
        public void Scan_IgnoresTargetsOutsideTargetMask()
        {
            CreateTarget(new Vector3(0f, 0f, 5f), GameLayers.Wolf);

            Scan();

            Assert.That(sensor.HasTarget, Is.False);
        }

        [Test]
        public void Scan_IgnoresTargetsRejectedByFilter()
        {
            Transform target = CreateTarget(new Vector3(0f, 0f, 5f));
            sensor.TargetFilter = candidate => candidate != target;

            Scan();

            Assert.That(sensor.HasTarget, Is.False);
        }

        [Test]
        public void Scan_DropsTargetThatIsNoLongerVisible()
        {
            Transform target = CreateTarget(new Vector3(0f, 0f, 5f));
            Scan();
            Assert.That(sensor.HasTarget, Is.True);

            target.position = new Vector3(0f, 0f, Range + 5f);
            Scan();

            Assert.That(sensor.HasTarget, Is.False);
            Assert.That(sensor.VisibleTargets, Is.Empty);
        }

        [Test]
        public void CanSee_ChecksRangeAndLineOfSightForAPoint()
        {
            CreateWall(new Vector3(3f, 0f, 0f));
            Physics.SyncTransforms();

            Assert.That(sensor.CanSee(new Vector3(0f, 0f, 5f)), Is.True);
            Assert.That(sensor.CanSee(new Vector3(6f, 0f, 0f)), Is.False);
            Assert.That(sensor.CanSee(new Vector3(0f, 0f, Range + 1f)), Is.False);
        }

        private void Scan()
        {
            Physics.SyncTransforms();
            sensor.Scan();
        }

        private Transform CreateTarget(Vector3 position, int layer = GameLayers.Boid)
        {
            GameObject target = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            target.layer = layer;
            target.transform.position = position;
            return target.transform;
        }

        private void CreateWall(Vector3 position)
        {
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.layer = GameLayers.Obstacle;
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
            wall.transform.rotation = Quaternion.LookRotation(-position);
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }
    }
}
