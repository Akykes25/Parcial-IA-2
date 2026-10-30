using System;
using System.Collections.Generic;

namespace Parcial2.FSM
{
    /// <summary>
    /// Agent-agnostic state machine. Transitions are registered as conditions and evaluated by the
    /// machine itself: "any" transitions first, in registration order (highest priority first),
    /// then the transitions that leave the current state. While the highest-priority satisfied "any"
    /// transition targets the current state, no lower-priority transition can preempt it.
    /// </summary>
    public class StateMachine<TKey> where TKey : struct, Enum
    {
        private readonly struct Transition
        {
            public readonly TKey To;
            public readonly Func<bool> Condition;

            public Transition(TKey to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }

        private readonly Dictionary<TKey, State> states = new();
        private readonly Dictionary<TKey, List<Transition>> transitionsFrom = new();
        private readonly List<Transition> anyTransitions = new();
        private bool isRunning;

        public TKey CurrentKey { get; private set; }
        public State CurrentState { get; private set; }

        public event Action<TKey, TKey> StateChanged;

        public void RegisterState(TKey key, State state)
        {
            states[key] = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void AddTransition(TKey from, TKey to, Func<bool> condition)
        {
            if (!transitionsFrom.TryGetValue(from, out List<Transition> list))
            {
                list = new List<Transition>();
                transitionsFrom[from] = list;
            }

            list.Add(new Transition(to, condition));
        }

        public void AddAnyTransition(TKey to, Func<bool> condition)
        {
            anyTransitions.Add(new Transition(to, condition));
        }

        public void Start(TKey initialKey)
        {
            isRunning = true;
            EnterState(initialKey);
        }

        public void Update()
        {
            if (!isRunning)
                return;

            if (TryGetNextState(out TKey nextKey))
                ChangeState(nextKey);

            CurrentState?.Update();
        }

        private bool TryGetNextState(out TKey nextKey)
        {
            foreach (Transition transition in anyTransitions)
            {
                if (!transition.Condition())
                    continue;

                // The highest-priority satisfied "any" transition wins, even when it targets the current state.
                nextKey = transition.To;
                return !IsCurrent(nextKey);
            }

            if (transitionsFrom.TryGetValue(CurrentKey, out List<Transition> list))
            {
                foreach (Transition transition in list)
                {
                    if (!transition.Condition())
                        continue;

                    nextKey = transition.To;
                    return true;
                }
            }

            nextKey = default;
            return false;
        }

        private void ChangeState(TKey nextKey)
        {
            TKey previousKey = CurrentKey;
            CurrentState?.Exit();
            EnterState(nextKey);
            StateChanged?.Invoke(previousKey, nextKey);
        }

        private void EnterState(TKey key)
        {
            if (!states.TryGetValue(key, out State state))
                throw new InvalidOperationException($"State {key} is not registered.");

            CurrentKey = key;
            CurrentState = state;
            CurrentState.Enter();
        }

        private bool IsCurrent(TKey key) => EqualityComparer<TKey>.Default.Equals(CurrentKey, key);
    }
}
