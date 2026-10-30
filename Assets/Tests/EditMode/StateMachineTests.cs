using System;
using System.Collections.Generic;
using NUnit.Framework;
using Parcial2.FSM;

namespace Parcial2.Tests
{
    public sealed class StateMachineTests
    {
        private enum TestState
        {
            Search,
            Attack,
            Gather,
            Exit,
            Flee,
            Dead
        }

        private sealed class RecordingState : State
        {
            private readonly string name;
            private readonly List<string> log;

            public int EnterCount { get; private set; }

            public RecordingState(string name, List<string> log)
            {
                this.name = name;
                this.log = log;
            }

            public override void Enter()
            {
                EnterCount++;
                log.Add($"{name}.Enter");
            }

            public override void Update() => log.Add($"{name}.Update");

            public override void Exit() => log.Add($"{name}.Exit");
        }

        private List<string> log;
        private Dictionary<TestState, RecordingState> states;
        private StateMachine<TestState> machine;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            states = new Dictionary<TestState, RecordingState>();
            machine = new StateMachine<TestState>();

            foreach (TestState key in Enum.GetValues(typeof(TestState)))
            {
                states[key] = new RecordingState(key.ToString(), log);
                machine.RegisterState(key, states[key]);
            }
        }

        [Test]
        public void Start_EntersInitialState()
        {
            machine.Start(TestState.Search);

            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Search));
            Assert.That(machine.CurrentState, Is.SameAs(states[TestState.Search]));
            Assert.That(log, Is.EqualTo(new[] { "Search.Enter" }));
        }

        [Test]
        public void Start_WithUnregisteredState_Throws()
        {
            var empty = new StateMachine<TestState>();

            Assert.Throws<InvalidOperationException>(() => empty.Start(TestState.Search));
        }

        [Test]
        public void Update_BeforeStart_DoesNothing()
        {
            machine.AddAnyTransition(TestState.Dead, () => true);

            machine.Update();

            Assert.That(log, Is.Empty);
        }

        [Test]
        public void Update_WithoutTransition_UpdatesCurrentState()
        {
            machine.Start(TestState.Search);
            log.Clear();

            machine.Update();

            Assert.That(log, Is.EqualTo(new[] { "Search.Update" }));
        }

        [Test]
        public void Transition_ExitsThenEntersThenUpdates_AndRaisesStateChanged()
        {
            bool targetSeen = false;
            var changes = new List<(TestState from, TestState to)>();
            machine.AddTransition(TestState.Search, TestState.Attack, () => targetSeen);
            machine.StateChanged += (from, to) => changes.Add((from, to));
            machine.Start(TestState.Search);
            log.Clear();

            targetSeen = true;
            machine.Update();

            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Attack));
            Assert.That(log, Is.EqualTo(new[] { "Search.Exit", "Attack.Enter", "Attack.Update" }));
            Assert.That(changes, Is.EqualTo(new[] { (TestState.Search, TestState.Attack) }));
        }

        [Test]
        public void StateTransition_IsOnlyEvaluatedFromItsSourceState()
        {
            machine.AddTransition(TestState.Attack, TestState.Gather, () => true);
            machine.Start(TestState.Search);

            machine.Update();

            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Search));
        }

        [Test]
        public void Update_AppliesAtMostOneTransitionPerTick()
        {
            machine.AddTransition(TestState.Search, TestState.Attack, () => true);
            machine.AddTransition(TestState.Attack, TestState.Gather, () => true);
            machine.Start(TestState.Search);

            machine.Update();

            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Attack));
        }

        [Test]
        public void AnyTransitions_FollowRegistrationPriority()
        {
            bool dead = false, flee = false, exit = false, gather = false, attack = false;
            RegisterHunterPriorities(() => dead, () => flee, () => exit, () => gather, () => attack);
            machine.Start(TestState.Search);

            attack = true;
            gather = true;
            machine.Update();
            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Gather));

            exit = true;
            flee = true;
            machine.Update();
            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Flee));

            dead = true;
            machine.Update();
            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Dead));
        }

        [Test]
        public void AnyTransitions_OverrideStateTransitions()
        {
            machine.AddAnyTransition(TestState.Flee, () => true);
            machine.AddTransition(TestState.Search, TestState.Attack, () => true);
            machine.Start(TestState.Search);

            machine.Update();

            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Flee));
        }

        [Test]
        public void AnyTransition_ToCurrentState_DoesNotReenterIt()
        {
            machine.AddAnyTransition(TestState.Flee, () => true);
            machine.Start(TestState.Search);

            machine.Update();
            machine.Update();

            Assert.That(states[TestState.Flee].EnterCount, Is.EqualTo(1));
        }

        [Test]
        public void AnyTransition_ToCurrentState_BlocksLowerPriorityTransitions()
        {
            RegisterHunterPriorities(() => true, () => true, () => false, () => false, () => false);
            machine.AddTransition(TestState.Dead, TestState.Search, () => true);
            machine.Start(TestState.Search);

            machine.Update();
            machine.Update();

            Assert.That(machine.CurrentKey, Is.EqualTo(TestState.Dead),
                "While Dead still holds, neither Flee nor Dead's own exits may preempt it.");
        }

        private void RegisterHunterPriorities(Func<bool> dead, Func<bool> flee, Func<bool> exit,
            Func<bool> gather, Func<bool> attack)
        {
            machine.AddAnyTransition(TestState.Dead, dead);
            machine.AddAnyTransition(TestState.Flee, flee);
            machine.AddAnyTransition(TestState.Exit, exit);
            machine.AddAnyTransition(TestState.Gather, gather);
            machine.AddAnyTransition(TestState.Attack, attack);
        }
    }
}
