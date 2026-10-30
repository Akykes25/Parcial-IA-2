using Parcial2.FSM;

namespace Parcial2.Wolves.States
{
    /// <summary>
    /// The Hunter is in sight: howl once to alert the pack and pursue it while perception holds. The target is
    /// visible, so the movement rule steers straight at it without A*.
    /// </summary>
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
