using UnityEngine;

namespace Parcial2.Navigation
{
    /// <summary>
    /// Soft constraints for a <see cref="NavigationAgent"/>: places the agent prefers to avoid (e.g. where a
    /// wolf was seen). Unlike <see cref="NavigationAgent.NodeFilter"/>, they are dropped when no route
    /// around them exists.
    /// </summary>
    public interface INavigationThreats
    {
        /// <summary>False when moving from <paramref name="from"/> to <paramref name="to"/> runs towards a threat.</summary>
        bool IsSegmentClear(Vector3 from, Vector3 to);
    }
}
