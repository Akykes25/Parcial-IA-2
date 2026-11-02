using System.Collections.Generic;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Core
{
    public class SimulationContext : MonoBehaviour
    {
        [SerializeField] private NavigationGraph graph;
        [SerializeField] private ZoneVolume startZone;
        [SerializeField] private ZoneVolume huntingGround;
        [SerializeField] private ZoneVolume exitZone;
        [SerializeField] private List<ZoneVolume> safeZones = new();

        public static SimulationContext Instance { get; private set; }

        public NavigationGraph Graph => graph;
        public ZoneVolume StartZone => startZone;
        public ZoneVolume HuntingGround => huntingGround;
        public ZoneVolume ExitZone => exitZone;
        public IReadOnlyList<ZoneVolume> SafeZones => safeZones;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(NavigationGraph navigationGraph, ZoneVolume start, ZoneVolume hunting,
            ZoneVolume exit, IEnumerable<ZoneVolume> safe)
        {
            graph = navigationGraph;
            startZone = start;
            huntingGround = hunting;
            exitZone = exit;
            safeZones.Clear();
            safeZones.AddRange(safe);
        }

        public bool IsInsideSafeZone(Vector3 position)
        {
            foreach (ZoneVolume zone in safeZones)
            {
                if (zone.Contains(position))
                    return true;
            }

            return false;
        }
    }
}
