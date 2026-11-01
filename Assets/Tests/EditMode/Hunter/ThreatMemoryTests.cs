using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Hunter;
using UnityEngine;

namespace Parcial2.Tests
{
    public sealed class ThreatMemoryTests
    {
        private readonly List<GameObject> created = new();
        private readonly List<Transform> visible = new();
        private ThreatMemory memory;
        private Transform wolf;

        [SetUp]
        public void SetUp()
        {
            memory = new ThreatMemory(avoidRadius: 3f, memoryTime: 5f, movedDistance: 1.5f);
            wolf = Track(new GameObject("Wolf")).transform;
            wolf.position = new Vector3(0f, 0f, 10f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                Object.DestroyImmediate(go);
            created.Clear();
            visible.Clear();
        }

        [Test]
        public void Record_NewWolf_ReportsChangeAndBlocksNearbyPoints()
        {
            visible.Add(wolf);

            Assert.That(memory.Record(visible, 0f), Is.True);
            Assert.That(memory.IsPointClear(new Vector3(1f, 0f, 10f)), Is.False);
            Assert.That(memory.IsPointClear(new Vector3(5f, 0f, 10f)), Is.True);
        }

        [Test]
        public void Record_SameWolfBarelyMoving_DoesNotReportChange()
        {
            visible.Add(wolf);
            memory.Record(visible, 0f);

            wolf.position += Vector3.right;

            Assert.That(memory.Record(visible, 0.2f), Is.False);
        }

        [Test]
        public void Record_SameWolfMovingFar_ReportsChange()
        {
            visible.Add(wolf);
            memory.Record(visible, 0f);

            wolf.position += Vector3.right * 2f;

            Assert.That(memory.Record(visible, 0.2f), Is.True);
        }

        [Test]
        public void Threat_IsForgottenAfterMemoryTime()
        {
            visible.Add(wolf);
            memory.Record(visible, 0f);
            visible.Clear();

            memory.Record(visible, 4f);
            Assert.That(memory.HasThreats, Is.True);

            memory.Record(visible, 6f);
            Assert.That(memory.HasThreats, Is.False);
            Assert.That(memory.IsPointClear(wolf.position), Is.True);
        }

        [Test]
        public void IsSegmentClear_BlocksOnlyThreatsAhead()
        {
            visible.Add(wolf);
            memory.Record(visible, 0f);

            Assert.That(memory.IsSegmentClear(Vector3.zero, new Vector3(0f, 0f, 20f)), Is.False, "Wolf on the way.");
            Assert.That(memory.IsSegmentClear(new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, 30f)), Is.True,
                "Wolf behind the start of the segment.");
            Assert.That(memory.IsSegmentClear(Vector3.zero, new Vector3(10f, 0f, 0f)), Is.True, "Wolf far aside.");
        }

        [Test]
        public void Clear_ForgetsEveryThreat()
        {
            visible.Add(wolf);
            memory.Record(visible, 0f);

            memory.Clear();

            Assert.That(memory.HasThreats, Is.False);
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }
    }
}
