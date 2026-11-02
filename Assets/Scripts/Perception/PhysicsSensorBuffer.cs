using System;
using UnityEngine;

namespace Parcial2.Perception
{
    public sealed class PhysicsSensorBuffer
    {
        private const int DefaultCapacity = 32;

        private Collider[] colliders;

        public PhysicsSensorBuffer(int initialCapacity = DefaultCapacity)
        {
            colliders = new Collider[Mathf.Max(1, initialCapacity)];
        }

        public Collider this[int index] => colliders[index];

        public int Query(Vector3 position, float radius, int layerMask)
        {
            float safeRadius = Mathf.Max(0f, radius);
            int resultCount = Physics.OverlapSphereNonAlloc(position, safeRadius, colliders, layerMask,
                QueryTriggerInteraction.Ignore);

            while (resultCount == colliders.Length)
            {
                Array.Resize(ref colliders, colliders.Length * 2);
                resultCount = Physics.OverlapSphereNonAlloc(position, safeRadius, colliders, layerMask,
                    QueryTriggerInteraction.Ignore);
            }

            return resultCount;
        }
    }
}
