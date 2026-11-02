using System.Collections.Generic;
using Parcial2.Boids;
using Parcial2.Perception;
using UnityEngine;

namespace Parcial2.Hunter
{
    public sealed class HunterPerception
    {
        private readonly VisionSensor boidSensor;
        private readonly VisionSensor wolfSensor;
        private readonly Dictionary<Transform, BoidAgent> boidCache = new();

        public HunterPerception(VisionSensor boidSensor, VisionSensor wolfSensor)
        {
            this.boidSensor = boidSensor;
            this.wolfSensor = wolfSensor;
        }

        public bool SeesWolf => wolfSensor.HasTarget;
        public Transform ClosestWolf => wolfSensor.ClosestTarget;
        public IReadOnlyList<Transform> VisibleWolves => wolfSensor.VisibleTargets;

        public bool SeesBoid(bool dead) => FindClosestBoid(dead, boidSensor.transform.position) != null;

        public bool CanSee(BoidAgent boid)
        {
            if (boid == null)
                return false;

            IReadOnlyList<Transform> visible = boidSensor.VisibleTargets;
            for (int i = 0; i < visible.Count; i++)
            {
                if (visible[i] == boid.transform)
                    return true;
            }

            return false;
        }

        public BoidAgent FindClosestBoid(bool dead, Vector3 from)
        {
            BoidAgent closest = null;
            float closestSqr = float.MaxValue;
            IReadOnlyList<Transform> visible = boidSensor.VisibleTargets;

            for (int i = 0; i < visible.Count; i++)
            {
                BoidAgent boid = GetBoid(visible[i]);
                if (boid == null || (dead ? !boid.IsDead : !boid.IsAlive))
                    continue;

                float sqr = (boid.transform.position - from).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = boid;
                }
            }

            return closest;
        }

        private BoidAgent GetBoid(Transform target)
        {
            if (target == null)
                return null;

            if (!boidCache.TryGetValue(target, out BoidAgent boid))
            {
                target.TryGetComponent(out boid);
                boidCache[target] = boid;
            }

            return boid;
        }
    }
}
