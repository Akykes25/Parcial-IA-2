using Parcial2.Boids;
using Parcial2.Steering;
using UnityEngine;

namespace Parcial2.Hunter.States
{
    /// <summary>
    /// Picks a visible live boid, then melee inside MeleeAttackRadius, ranged inside RangeAttackRadius,
    /// otherwise Pursuit. One successful attack resets the TBA and ends the state; losing sight of the
    /// target ends it without touching the TBA.
    /// </summary>
    public sealed class AttackState : ActivityState
    {
        private readonly HunterController hunter;
        private BoidAgent target;

        public AttackState(HunterController hunter)
        {
            this.hunter = hunter;
        }

        public BoidAgent Target => target;

        public override void Enter()
        {
            target = hunter.Perception.FindClosestBoid(dead: false, hunter.Position);
            InProgress = target != null;
        }

        public override void Update()
        {
            if (!InProgress)
                return;

            if (target == null || !target.IsAlive || !hunter.Perception.CanSee(target))
            {
                InProgress = false;
                return;
            }

            Vector3 offset = target.transform.position - hunter.Position;
            offset.y = 0f;
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance <= hunter.MeleeAttackRadius * hunter.MeleeAttackRadius)
                Strike(AttackKind.Melee);
            else if (sqrDistance <= hunter.RangeAttackRadius * hunter.RangeAttackRadius)
                Strike(AttackKind.Ranged);
            else
                Chase();
        }

        public override void Exit()
        {
            base.Exit();
            target = null;
        }

        private void Strike(AttackKind kind)
        {
            hunter.PerformAttack(target, kind);
            InProgress = false;
        }

        private void Chase()
        {
            Vector3 direction = SteeringBehaviours.Pursuit(hunter.Position, hunter.AttackSpeed,
                target.transform.position, target.Velocity, hunter.MaxPrediction);
            hunter.Steer(direction * hunter.AttackSpeed);
        }
    }
}
