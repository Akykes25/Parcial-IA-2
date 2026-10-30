using Parcial2.FSM;
using UnityEngine;

namespace Parcial2.Hunter.States
{
    /// <summary>Out of the scene for RespawnTime, then back at the start zone with zero gathers.</summary>
    public sealed class DeadState : State
    {
        private readonly HunterController hunter;
        private float elapsed;

        public DeadState(HunterController hunter)
        {
            this.hunter = hunter;
        }

        public float RemainingTime => Mathf.Max(0f, hunter.RespawnTime - elapsed);

        public override void Enter()
        {
            elapsed = 0f;
            hunter.Halt();
            hunter.SetPresent(false);
        }

        public override void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed < hunter.RespawnTime)
                return;

            hunter.RestartCycle();
            hunter.SetPresent(true);
        }

        public override void Exit()
        {
            if (!hunter.IsPresent)
                hunter.SetPresent(true);
        }
    }
}
