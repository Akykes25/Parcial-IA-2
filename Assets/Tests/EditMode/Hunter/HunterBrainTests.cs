using NUnit.Framework;
using Parcial2.FSM;
using Parcial2.Hunter;

namespace Parcial2.Tests
{
    public sealed class HunterBrainTests
    {
        private sealed class FakeConditions : IHunterConditions
        {
            public bool IsCaught { get; set; }
            public bool SeesWolf { get; set; }
            public bool HasRequiredGathers { get; set; }
            public bool SeesDeadBoid { get; set; }
            public bool CanAttack { get; set; }
        }

        private sealed class FakeState : State
        {
        }

        private sealed class FakeActivity : ActivityState
        {
            public override void Enter() => InProgress = true;

            public void Finish() => InProgress = false;
        }

        private FakeConditions conditions;
        private FakeActivity attack;
        private FakeActivity gather;
        private FakeActivity flee;
        private HunterBrain brain;

        [SetUp]
        public void SetUp()
        {
            conditions = new FakeConditions();
            attack = new FakeActivity();
            gather = new FakeActivity();
            flee = new FakeActivity();
            brain = new HunterBrain(conditions,
                new HunterStateSet(new FakeState(), attack, gather, flee, new FakeState(), new FakeState()));
            brain.Start();
        }

        [Test]
        public void Start_BeginsInSearch()
        {
            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Search));
        }

        [Test]
        public void Search_PrefersGatherOverAttack()
        {
            conditions.SeesDeadBoid = true;
            conditions.CanAttack = true;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Gather));
        }

        [Test]
        public void Search_GoesToAttackWhenAttackIsReady()
        {
            conditions.CanAttack = true;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Attack));
        }

        [Test]
        public void Attack_ReturnsToSearchWhenFinished()
        {
            conditions.CanAttack = true;
            brain.Update();
            conditions.CanAttack = false;
            attack.Finish();

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Search));
        }

        [Test]
        public void Flee_PreemptsGatherAndCancelsIt()
        {
            conditions.SeesDeadBoid = true;
            brain.Update();
            conditions.SeesWolf = true;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Flee));
            Assert.That(gather.InProgress, Is.False);
        }

        [Test]
        public void Flee_HoldsUntilFinishedEvenWithoutWolfInSight()
        {
            conditions.SeesWolf = true;
            brain.Update();
            conditions.SeesWolf = false;

            brain.Update();
            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Flee));

            flee.Finish();
            brain.Update();
            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Search));
        }

        [Test]
        public void Flee_GoesToExitWhenGathersAreComplete()
        {
            conditions.SeesWolf = true;
            brain.Update();
            conditions.SeesWolf = false;
            conditions.HasRequiredGathers = true;
            flee.Finish();

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Exit));
        }

        [Test]
        public void Exit_BlocksGatherAndAttack()
        {
            conditions.HasRequiredGathers = true;
            brain.Update();
            conditions.SeesDeadBoid = true;
            conditions.CanAttack = true;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Exit));
        }

        [Test]
        public void Exit_ReturnsToSearchAfterGathersReset()
        {
            conditions.HasRequiredGathers = true;
            brain.Update();
            conditions.HasRequiredGathers = false;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Search));
        }

        [Test]
        public void Dead_PreemptsFleeAndHoldsWhileCaught()
        {
            conditions.SeesWolf = true;
            brain.Update();
            conditions.IsCaught = true;

            brain.Update();
            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Dead));
        }

        [Test]
        public void Dead_ReturnsToSearchAfterRespawn()
        {
            conditions.IsCaught = true;
            brain.Update();
            conditions.IsCaught = false;

            brain.Update();

            Assert.That(brain.Current, Is.EqualTo(HunterStateId.Search));
        }
    }
}
