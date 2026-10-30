using NUnit.Framework;
using Parcial2.FSM;
using Parcial2.Wolves;

namespace Parcial2.Tests
{
    public sealed class WolfBrainTests
    {
        private sealed class FakeConditions : IWolfConditions
        {
            public bool SeesHunter { get; set; }
            public bool HasLastKnownPosition { get; set; }
        }

        private sealed class CountingState : State
        {
            public int Entered { get; private set; }

            public override void Enter() => Entered++;
        }

        private FakeConditions conditions;
        private CountingState chase;
        private WolfBrain brain;

        [SetUp]
        public void SetUp()
        {
            conditions = new FakeConditions();
            chase = new CountingState();
            brain = new WolfBrain(conditions, new WolfStateSet(new CountingState(), chase, new CountingState()));
            brain.Start();
        }

        [Test]
        public void Start_BeginsInPatrol()
        {
            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Patrol));
        }

        [Test]
        public void Patrol_ChasesWhenTheHunterIsSeen()
        {
            conditions.SeesHunter = true;
            conditions.HasLastKnownPosition = true;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Chase));
        }

        [Test]
        public void Patrol_InvestigatesWhenAHowlGivesAPosition()
        {
            conditions.HasLastKnownPosition = true;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Investigate));
        }

        [Test]
        public void Chase_InvestigatesTheLastKnownPositionWhenSightIsLost()
        {
            conditions.SeesHunter = true;
            conditions.HasLastKnownPosition = true;
            brain.Update();

            conditions.SeesHunter = false;
            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Investigate));
        }

        [Test]
        public void Investigate_ChasesAgainWhenTheHunterIsSeen()
        {
            conditions.HasLastKnownPosition = true;
            brain.Update();

            conditions.SeesHunter = true;
            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Chase));
            Assert.That(chase.Entered, Is.EqualTo(1));
        }

        [Test]
        public void Investigate_ReturnsToPatrolWhenThePositionIsForgotten()
        {
            conditions.HasLastKnownPosition = true;
            brain.Update();

            conditions.HasLastKnownPosition = false;
            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Patrol));
        }

        [Test]
        public void Chase_ReturnsToPatrolWhenTheHunterShelters()
        {
            conditions.SeesHunter = true;
            conditions.HasLastKnownPosition = true;
            brain.Update();

            conditions.SeesHunter = false;
            conditions.HasLastKnownPosition = false;
            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Patrol));
        }

        [Test]
        public void Chase_StaysWhileTheHunterIsInSight()
        {
            conditions.SeesHunter = true;
            conditions.HasLastKnownPosition = true;
            brain.Update();
            brain.Update();
            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(WolfStateId.Chase));
            Assert.That(chase.Entered, Is.EqualTo(1));
        }
    }
}
