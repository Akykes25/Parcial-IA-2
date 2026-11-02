using UnityEngine;

namespace Parcial2.Navigation
{
    public interface INavigationThreats
    {
        bool IsSegmentClear(Vector3 from, Vector3 to);
    }
}
