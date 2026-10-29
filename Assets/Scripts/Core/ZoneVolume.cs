using System;
using UnityEngine;

namespace Parcial2.Core
{
    public enum ZoneType
    {
        Start,
        HuntingGround,
        Safe,
        Exit
    }

    [RequireComponent(typeof(BoxCollider))]
    public class ZoneVolume : MonoBehaviour
    {
        [SerializeField] private ZoneType zoneType;
        [SerializeField] private BoxCollider area;

        public ZoneType Type => zoneType;
        public Bounds Bounds => area.bounds;
        public Vector3 Center => area.bounds.center;

        public event Action<ZoneVolume, Collider> BodyEntered;
        public event Action<ZoneVolume, Collider> BodyExited;

        private void Reset()
        {
            area = GetComponent<BoxCollider>();
            area.isTrigger = true;
        }

        private void Awake()
        {
            if (area == null)
                area = GetComponent<BoxCollider>();
        }

        public void Configure(ZoneType type)
        {
            zoneType = type;
            area = GetComponent<BoxCollider>();
            area.isTrigger = true;
        }

        public bool Contains(Vector3 position)
        {
            Bounds bounds = area.bounds;
            return position.x >= bounds.min.x && position.x <= bounds.max.x
                && position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        public Vector3 ClampInside(Vector3 position, float margin)
        {
            Bounds bounds = area.bounds;
            position.x = Mathf.Clamp(position.x, bounds.min.x + margin, bounds.max.x - margin);
            position.z = Mathf.Clamp(position.z, bounds.min.z + margin, bounds.max.z - margin);
            return position;
        }

        private void OnTriggerEnter(Collider other) => BodyEntered?.Invoke(this, other);

        private void OnTriggerExit(Collider other) => BodyExited?.Invoke(this, other);

        public static Color ColorFor(ZoneType type) => type switch
        {
            ZoneType.Start => new Color(0.3f, 0.75f, 0.4f),
            ZoneType.HuntingGround => new Color(0.95f, 0.65f, 0.2f),
            ZoneType.Safe => new Color(0.6f, 0.4f, 0.8f),
            _ => new Color(0.25f, 0.5f, 0.9f)
        };

        private void OnDrawGizmos()
        {
            BoxCollider box = area != null ? area : GetComponent<BoxCollider>();
            if (box == null)
                return;

            Color color = ColorFor(zoneType);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.15f);
            Gizmos.DrawCube(box.bounds.center, box.bounds.size);
            Gizmos.color = color;
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
}
