using System;
using Parcial2.FSM;

namespace Parcial2.Wolves
{
    public readonly struct WolfStateSet
    {
        public readonly State Patrol;
        public readonly State Chase;
        public readonly State Investigate;

        public WolfStateSet(State patrol, State chase, State investigate)
        {
            Patrol = patrol;
            Chase = chase;
            Investigate = investigate;
        }
    }

    public sealed class WolfBrain
    {
        private readonly StateMachine<WolfStateId> machine = new();

        public WolfBrain(IWolfConditions conditions, WolfStateSet states)
        {
            machine.RegisterState(WolfStateId.Patrol, states.Patrol);
            machine.RegisterState(WolfStateId.Chase, states.Chase);
            machine.RegisterState(WolfStateId.Investigate, states.Investigate);

            machine.AddAnyTransition(WolfStateId.Chase, () => conditions.SeesHunter);
            machine.AddAnyTransition(WolfStateId.Investigate, () => conditions.HasLastKnownPosition);

            machine.AddTransition(WolfStateId.Chase, WolfStateId.Patrol, () => !conditions.SeesHunter);
            machine.AddTransition(WolfStateId.Investigate, WolfStateId.Patrol,
                () => !conditions.HasLastKnownPosition);
        }

        public WolfStateId Current => machine.CurrentKey;

        public event Action<WolfStateId, WolfStateId> StateChanged
        {
            add => machine.StateChanged += value;
            remove => machine.StateChanged -= value;
        }

        public void Start() => machine.Start(WolfStateId.Patrol);

        public void Update() => machine.Update();
    }
}
