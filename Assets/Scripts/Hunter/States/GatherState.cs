using Parcial2.Boids;
using UnityEngine;

namespace Parcial2.Hunter.States
{
    public sealed class GatherState : ActivityState
    {
        private readonly HunterController hunter;
        private BoidAgent target;
        private float elapsed;

        public GatherState(HunterController hunter)
        {
            this.hunter = hunter;
        }

        public BoidAgent Target => target;
        public float Progress => InProgress ? Mathf.Clamp01(elapsed / Mathf.Max(0.01f, hunter.GatherDuration)) : 0f;

        public override void Enter()
        {
            target = hunter.Perception.FindClosestBoid(dead: true, hunter.Position);
            elapsed = 0f;
            InProgress = target != null;
        }

        public override void Update()
        {
            if (!InProgress)
                return;

            if (target == null || !target.IsDead)
            {
                InProgress = false;
                return;
            }

            Vector3 offset = target.transform.position - hunter.Position;
            offset.y = 0f;
            if (offset.sqrMagnitude > hunter.GatherReachDistance * hunter.GatherReachDistance)
            {
                elapsed = 0f;
                hunter.MoveTo(target.transform.position, hunter.GatherSpeed);
                return;
            }

            hunter.Halt();
            elapsed += Time.deltaTime;
            if (elapsed < hunter.GatherDuration)
                return;

            target.CollectAndRespawn();
            hunter.AddGather();
            InProgress = false;
        }

        public override void Exit()
        {
            base.Exit();
            target = null;
            elapsed = 0f;
        }
    }
}
