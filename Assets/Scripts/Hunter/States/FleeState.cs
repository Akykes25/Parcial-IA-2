using Parcial2.Core;
using UnityEngine;

namespace Parcial2.Hunter.States
{
    public sealed class FleeState : ActivityState
    {
        private readonly HunterController hunter;
        private ZoneVolume zone;
        private float waited;

        public FleeState(HunterController hunter)
        {
            this.hunter = hunter;
        }

        public float WaitProgress => Mathf.Clamp01(waited / Mathf.Max(0.01f, hunter.SafeZoneWaitTime));

        public override void Enter() => Begin();

        public override void Update()
        {
            if (!InProgress)
            {
                if (hunter.SeesWolf)
                    Begin();
                return;
            }

            if (zone == null)
            {
                InProgress = false;
                return;
            }

            if (!zone.Contains(hunter.Position))
            {
                waited = 0f;
                hunter.MoveTo(zone.Center, hunter.FleeSpeed);
                return;
            }

            if (!hunter.HasArrived(zone.Center))
                hunter.MoveTo(zone.Center, hunter.FleeSpeed);
            else
                hunter.Halt();

            waited += Time.deltaTime;
            if (waited >= hunter.SafeZoneWaitTime)
                InProgress = false;
        }

        public override void Exit()
        {
            base.Exit();
            zone = null;
            hunter.ClearSafeZone();
        }

        private void Begin()
        {
            waited = 0f;
            zone = FindNearestSafeZone();
            InProgress = zone != null;
            if (zone != null)
                hunter.ChooseSafeZone(zone);
        }

        private ZoneVolume FindNearestSafeZone()
        {
            return FindNearestSafeZone(avoidThreats: true) ?? FindNearestSafeZone(avoidThreats: false);
        }

        private ZoneVolume FindNearestSafeZone(bool avoidThreats)
        {
            ZoneVolume best = null;
            float bestCost = float.MaxValue;

            foreach (ZoneVolume candidate in hunter.Context.SafeZones)
            {
                if (candidate == null)
                    continue;

                if (candidate.Contains(hunter.Position))
                    return candidate;

                if (hunter.Navigation.TryGetPathCost(hunter.Position, candidate.Center, out float cost, avoidThreats)
                    && cost < bestCost)
                {
                    bestCost = cost;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
