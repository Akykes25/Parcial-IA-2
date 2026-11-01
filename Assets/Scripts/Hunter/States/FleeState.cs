using Parcial2.Core;
using UnityEngine;

namespace Parcial2.Hunter.States
{
    /// <summary>
    /// Runs with A* to the safe zone with the shortest path, waits SafeZoneWaitTime inside it and then
    /// hands control back. Seeing a wolf again while sheltered restarts the wait.
    /// </summary>
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
                // The FSM kept us here because a wolf is still in sight: flee again.
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

            // Inside: settle towards the centre so the body is fully sheltered, and wait.
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

        /// <summary>
        /// Nearest by A* path length over the graph, not by straight-line distance. Routes that keep away
        /// from the wolves seen recently win; a route past a wolf is used only when no other exists.
        /// </summary>
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
