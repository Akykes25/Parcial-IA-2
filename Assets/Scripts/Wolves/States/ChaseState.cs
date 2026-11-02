using Parcial2.FSM;

namespace Parcial2.Wolves.States
{
    public sealed class ChaseState : State
    {
        private readonly WolfController wolf;

        public ChaseState(WolfController wolf)
        {
            this.wolf = wolf;
        }

        public override void Enter() => wolf.Howl();

        public override void Update() => wolf.MoveTo(wolf.LastKnownPosition, wolf.ChaseSpeed);
    }
}
