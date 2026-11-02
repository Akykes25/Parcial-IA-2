using Parcial2.Core;
using Parcial2.FSM;

namespace Parcial2.Hunter.States
{
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
