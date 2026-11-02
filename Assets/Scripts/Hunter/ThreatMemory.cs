using System.Collections.Generic;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Hunter
{
    public sealed class ThreatMemory : INavigationThreats
    {
        private readonly Transform[] wolves;
        private readonly Vector3[] positions;
        private readonly float[] seenAt;
        private readonly float avoidRadiusSqr;
        private readonly float memoryTime;
        private readonly float movedSqr;
        private float now;

        public ThreatMemory(float avoidRadius, float memoryTime, float movedDistance = 1.5f, int capacity = 8)
        {
            wolves = new Transform[capacity];
            positions = new Vector3[capacity];
            seenAt = new float[capacity];
            avoidRadiusSqr = avoidRadius * avoidRadius;
            this.memoryTime = memoryTime;
            movedSqr = movedDistance * movedDistance;
        }

        public float AvoidRadius => Mathf.Sqrt(avoidRadiusSqr);

        public bool Record(IReadOnlyList<Transform> visible, float time)
        {
            now = time;
            bool changed = false;
            for (int i = 0; i < visible.Count; i++)
            {
                Transform wolf = visible[i];
                if (wolf == null)
                    continue;

                int slot = FindSlot(wolf);
                Vector3 position = wolf.position;
                if (wolves[slot] != wolf || !IsActive(slot) || Flat(position - positions[slot]).sqrMagnitude > movedSqr)
                {
                    changed = true;
                    positions[slot] = position;
                }

                wolves[slot] = wolf;
                seenAt[slot] = time;
            }

            return changed;
        }

        public void Clear()
        {
            for (int i = 0; i < wolves.Length; i++)
                wolves[i] = null;
        }

        public bool HasThreats
        {
            get
            {
                for (int i = 0; i < wolves.Length; i++)
                {
                    if (IsActive(i))
                        return true;
                }

                return false;
            }
        }

        public bool IsPointClear(Vector3 point)
        {
            for (int i = 0; i < wolves.Length; i++)
            {
                if (IsActive(i) && Flat(point - positions[i]).sqrMagnitude < avoidRadiusSqr)
                    return false;
            }

            return true;
        }

        public bool IsSegmentClear(Vector3 from, Vector3 to)
        {
            Vector3 segment = Flat(to - from);
            float lengthSqr = segment.sqrMagnitude;
            for (int i = 0; i < wolves.Length; i++)
            {
                if (!IsActive(i))
                    continue;

                Vector3 toThreat = Flat(positions[i] - from);
                float t = lengthSqr > 0.0001f ? Vector3.Dot(toThreat, segment) / lengthSqr : 0f;
                if (t <= 0f)
                    continue;

                Vector3 closest = segment * Mathf.Min(t, 1f);
                if ((toThreat - closest).sqrMagnitude < avoidRadiusSqr)
                    return false;
            }

            return true;
        }

        private bool IsActive(int slot) => wolves[slot] != null && now - seenAt[slot] <= memoryTime;

        private int FindSlot(Transform wolf)
        {
            int oldest = 0;
            for (int i = 0; i < wolves.Length; i++)
            {
                if (wolves[i] == wolf)
                    return i;
                if (wolves[i] == null || seenAt[i] < seenAt[oldest])
                    oldest = i;
            }

            return oldest;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
