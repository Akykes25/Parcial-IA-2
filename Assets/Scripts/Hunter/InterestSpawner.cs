using System.Collections.Generic;
using Parcial2.Boids;
using Parcial2.Core;
using UnityEngine;

namespace Parcial2.Hunter
{
    [DisallowMultipleComponent]
    public sealed class InterestSpawner : MonoBehaviour
    {
        [SerializeField] private InterestObject prefab;
        [SerializeField, Min(1)] private int maxActive = 5;
        [SerializeField, Min(0.1f)] private float objectLife = 4f;
        [SerializeField, Min(0f)] private float edgeMargin = 1.5f;
        [SerializeField] private float spawnHeight = 0.5f;

        private readonly List<InterestObject> pool = new();
        private Transform poolRoot;

        public int MaxActive => maxActive;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (InterestObject item in pool)
                {
                    if (item.gameObject.activeSelf && item.IsAvailable)
                        count++;
                }

                return count;
            }
        }

        public bool TrySpawn(ZoneVolume zone)
        {
            if (prefab == null || zone == null || ActiveCount >= maxActive)
                return false;

            Bounds bounds = zone.Bounds;
            Vector3 point = new(Random.Range(bounds.min.x, bounds.max.x), spawnHeight,
                Random.Range(bounds.min.z, bounds.max.z));
            GetFreeObject().Activate(zone.ClampInside(point, edgeMargin), objectLife);
            return true;
        }

        private InterestObject GetFreeObject()
        {
            foreach (InterestObject item in pool)
            {
                if (!item.gameObject.activeSelf)
                    return item;
            }

            if (poolRoot == null)
                poolRoot = new GameObject("Interest Objects").transform;

            InterestObject created = Instantiate(prefab, poolRoot);
            pool.Add(created);
            return created;
        }

        private void OnDestroy()
        {
            if (poolRoot != null)
                Destroy(poolRoot.gameObject);
        }
    }
}
