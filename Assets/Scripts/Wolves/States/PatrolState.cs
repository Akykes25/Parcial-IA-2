using Parcial2.FSM;
using Parcial2.Navigation;

namespace Parcial2.Wolves.States
{
    /// <summary>
    /// Walks the patrol route. Each waypoint is reached straight when it is in line of sight and with A*
    /// otherwise; after a chase the wolf resumes from the closest waypoint.
    /// </summary>
    public sealed class PatrolState : State
    {
        private readonly WolfController wolf;
        private readonly PatrolCursor cursor;

        public PatrolState(WolfController wolf)
        {
            this.wolf = wolf;
            cursor = new PatrolCursor(wolf.Route.Mode, wolf.Route.Count);
        }

        public override void Enter()
        {
            cursor.Reset(wolf.Route.ClosestIndex(wolf.Position));
        }

        public override void Update()
        {
            GraphNode waypoint = wolf.Route[cursor.Index];
            if (wolf.HasArrived(waypoint.Position))
                waypoint = wolf.Route[cursor.Advance()];

            wolf.MoveTo(waypoint.Position, wolf.PatrolSpeed);
        }

        public override void Exit() => wolf.Halt();
    }
}
