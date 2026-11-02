using Parcial2.FSM;

namespace Parcial2.Hunter
{
    public abstract class ActivityState : State
    {
        public bool InProgress { get; protected set; }

        public override void Exit()
        {
            InProgress = false;
        }
    }
}
