using System;
using Parcial2.FSM;

namespace Parcial2.Hunter
{
    public readonly struct HunterStateSet
    {
        public readonly State Search;
        public readonly ActivityState Attack;
        public readonly ActivityState Gather;
        public readonly ActivityState Flee;
        public readonly State Exit;
        public readonly State Dead;

        public HunterStateSet(State search, ActivityState attack, ActivityState gather, ActivityState flee,
            State exit, State dead)
        {
            Search = search;
            Attack = attack;
            Gather = gather;
            Flee = flee;
            Exit = exit;
            Dead = dead;
        }
    }

    public sealed class HunterBrain
    {
        private readonly StateMachine<HunterStateId> machine = new();

        public HunterBrain(IHunterConditions conditions, HunterStateSet states)
        {
            machine.RegisterState(HunterStateId.Search, states.Search);
            machine.RegisterState(HunterStateId.Attack, states.Attack);
            machine.RegisterState(HunterStateId.Gather, states.Gather);
            machine.RegisterState(HunterStateId.Flee, states.Flee);
            machine.RegisterState(HunterStateId.Exit, states.Exit);
            machine.RegisterState(HunterStateId.Dead, states.Dead);

            machine.AddAnyTransition(HunterStateId.Dead, () => conditions.IsCaught);
            machine.AddAnyTransition(HunterStateId.Flee, () => conditions.SeesWolf || states.Flee.InProgress);
            machine.AddAnyTransition(HunterStateId.Exit, () => conditions.HasRequiredGathers);

            machine.AddTransition(HunterStateId.Search, HunterStateId.Gather, () => conditions.SeesDeadBoid);
            machine.AddTransition(HunterStateId.Search, HunterStateId.Attack, () => conditions.CanAttack);

            machine.AddTransition(HunterStateId.Gather, HunterStateId.Search, () => !states.Gather.InProgress);
            machine.AddTransition(HunterStateId.Attack, HunterStateId.Search, () => !states.Attack.InProgress);
            machine.AddTransition(HunterStateId.Flee, HunterStateId.Search, () => !states.Flee.InProgress);
            machine.AddTransition(HunterStateId.Exit, HunterStateId.Search, () => !conditions.HasRequiredGathers);
            machine.AddTransition(HunterStateId.Dead, HunterStateId.Search, () => !conditions.IsCaught);
        }

        public HunterStateId Current => machine.CurrentKey;

        public event Action<HunterStateId, HunterStateId> StateChanged
        {
            add => machine.StateChanged += value;
            remove => machine.StateChanged -= value;
        }

        public void Start() => machine.Start(HunterStateId.Search);

        public void Update() => machine.Update();
    }
}
