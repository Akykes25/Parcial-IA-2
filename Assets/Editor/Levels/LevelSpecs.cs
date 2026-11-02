using Parcial2.Core;
using Parcial2.Wolves;
using UnityEngine;

namespace Parcial2.Editor.Levels
{
    internal readonly struct AreaSpec
    {
        public readonly string Name;
        public readonly Rect Bounds;
        public readonly ZoneType? Zone;

        public AreaSpec(string name, float xMin, float zMin, float xMax, float zMax, ZoneType? zone = null)
        {
            Name = name;
            Bounds = Rect.MinMaxRect(xMin, zMin, xMax, zMax);
            Zone = zone;
        }
    }

    internal readonly struct NodeSpec
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly bool IsSafeZone;

        public NodeSpec(string name, float x, float z, bool isSafeZone = false)
        {
            Name = name;
            Position = new Vector2(x, z);
            IsSafeZone = isSafeZone;
        }
    }

    internal readonly struct RouteSpec
    {
        public readonly string WolfName;
        public readonly PatrolMode Mode;
        public readonly int[] Waypoints;

        public RouteSpec(string wolfName, PatrolMode mode, params int[] waypoints)
        {
            WolfName = wolfName;
            Mode = mode;
            Waypoints = waypoints;
        }
    }
}
