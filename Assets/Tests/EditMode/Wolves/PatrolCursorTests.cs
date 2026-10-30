using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.Wolves;

namespace Parcial2.Tests
{
    public sealed class PatrolCursorTests
    {
        private static List<int> Walk(PatrolCursor cursor, int steps)
        {
            var indices = new List<int> { cursor.Index };
            for (int i = 0; i < steps; i++)
                indices.Add(cursor.Advance());

            return indices;
        }

        [Test]
        public void Loop_ReturnsToTheFirstWaypoint()
        {
            var cursor = new PatrolCursor(PatrolMode.Loop, 3);

            Assert.That(Walk(cursor, 5), Is.EqualTo(new[] { 0, 1, 2, 0, 1, 2 }));
        }

        [Test]
        public void PingPong_ReversesAtBothEnds()
        {
            var cursor = new PatrolCursor(PatrolMode.PingPong, 3);

            Assert.That(Walk(cursor, 6), Is.EqualTo(new[] { 0, 1, 2, 1, 0, 1, 2 }));
        }

        [Test]
        public void Reset_ResumesFromTheGivenWaypointGoingForward()
        {
            var cursor = new PatrolCursor(PatrolMode.PingPong, 4);
            Walk(cursor, 4);

            cursor.Reset(1);

            Assert.That(Walk(cursor, 3), Is.EqualTo(new[] { 1, 2, 3, 2 }));
        }

        [Test]
        public void SingleWaypoint_StaysInPlace()
        {
            var cursor = new PatrolCursor(PatrolMode.Loop, 1);

            Assert.That(Walk(cursor, 2), Is.EqualTo(new[] { 0, 0, 0 }));
        }
    }
}
