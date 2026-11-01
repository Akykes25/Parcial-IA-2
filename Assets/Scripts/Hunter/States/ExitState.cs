using Parcial2.Core;
using Parcial2.FSM;

namespace Parcial2.Hunter.States
{
    /// <summary>Goes to the exit with A*; on arrival the cycle restarts from the start zone.</summary>
    public sealed class ExitState : State
    {
        private readonly HunterController hunter;

        public ExitState(HunterController hunter)
        {
            this.hunter = hunter;
        }

        public override void Update()
        {
            ZoneVolume exit = hunter.Context.ExitZone;
            if (exit.Contains(hunter.Position))
            {
                hunter.CompleteExit();
                return;
            }

            hunter.MoveTo(exit.Center, hunter.ExitSpeed);
        }
    }
}
