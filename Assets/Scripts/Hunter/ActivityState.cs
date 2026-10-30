using Parcial2.FSM;

namespace Parcial2.Hunter
{
    /// <summary>A state that runs a task to completion; the FSM leaves it once the task stops.</summary>
    public abstract class ActivityState : State
    {
        public bool InProgress { get; protected set; }

        public override void Exit()
        {
            InProgress = false;
        }
    }
}
