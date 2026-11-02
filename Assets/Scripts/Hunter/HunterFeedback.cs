using Parcial2.Boids;
using Parcial2.Core;
using Parcial2.Feedback;
using UnityEngine;

namespace Parcial2.Hunter
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HunterController))]
    public sealed class HunterFeedback : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField, Min(0f)] private float attackLineDuration = 0.4f;
        [SerializeField] private bool logEvents = true;

        private HunterController hunter;
        private Renderer[] bodyRenderers;
        private MaterialPropertyBlock colorProperties;
        private Vector3 lastAttackTarget;
        private AttackKind lastAttackKind;
        private float lastAttackTime = float.MinValue;

        private void Awake()
        {
            hunter = GetComponent<HunterController>();
            bodyRenderers = GetComponentsInChildren<Renderer>();
            colorProperties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            hunter.StateChanged += OnStateChanged;
            hunter.SafeZoneChosen += OnSafeZoneChosen;
            hunter.AttackPerformed += OnAttackPerformed;
            hunter.Caught += OnCaught;
        }

        private void OnDisable()
        {
            hunter.StateChanged -= OnStateChanged;
            hunter.SafeZoneChosen -= OnSafeZoneChosen;
            hunter.AttackPerformed -= OnAttackPerformed;
            hunter.Caught -= OnCaught;
        }

        private void Start() => ApplyColor(ColorFor(HunterStateId.Search));

        private void OnStateChanged(HunterStateId from, HunterStateId to) => ApplyColor(ColorFor(to));

        private void OnSafeZoneChosen(ZoneVolume zone, Transform wolf)
        {
            if (logEvents)
                Debug.Log($"[Hunter] Wolf detected ({(wolf != null ? wolf.name : "lost")}), fleeing to {zone.name}.", this);
        }

        private void OnAttackPerformed(BoidAgent target, AttackKind kind)
        {
            lastAttackTarget = target.transform.position;
            lastAttackKind = kind;
            lastAttackTime = Time.time;
        }

        private void OnCaught()
        {
            if (logEvents)
                Debug.Log("[Hunter] Caught by a wolf.", this);
        }

        public static Color ColorFor(HunterStateId state) => state switch
        {
            HunterStateId.Attack => new Color(0.9f, 0.15f, 0.15f),
            HunterStateId.Gather => new Color(0.95f, 0.8f, 0.1f),
            HunterStateId.Flee => new Color(0.6f, 0.4f, 0.85f),
            HunterStateId.Exit => new Color(0.25f, 0.5f, 0.95f),
            HunterStateId.Dead => new Color(0.2f, 0.2f, 0.2f),
            _ => new Color(0.2f, 0.75f, 0.35f)
        };

        private void ApplyColor(Color color)
        {
            foreach (Renderer part in bodyRenderers)
            {
                part.GetPropertyBlock(colorProperties);
                colorProperties.SetColor(BaseColorProperty, color);
                colorProperties.SetColor(ColorProperty, color);
                part.SetPropertyBlock(colorProperties);
            }
        }

        private void OnDrawGizmos()
        {
            HunterController owner = hunter != null ? hunter : GetComponent<HunterController>();
            if (owner == null)
                return;

            if (Application.isPlaying && !owner.IsPresent)
            {
                DrawLabel(owner);
                return;
            }

            Vector3 center = transform.position;
            FeedbackGizmos.DrawCircle(center, owner.VisionRadius, new Color(1f, 0.9f, 0.2f));
            FeedbackGizmos.DrawCircle(center, owner.RangeAttackRadius, new Color(1f, 0.55f, 0.1f));
            FeedbackGizmos.DrawCircle(center, owner.MeleeAttackRadius, new Color(0.95f, 0.15f, 0.15f));

            if (!Application.isPlaying || owner.Navigation == null)
                return;

            FeedbackGizmos.DrawPath(owner.Navigation);

            if (owner.ChosenSafeZone != null)
            {
                Gizmos.color = ZoneVolume.ColorFor(ZoneType.Safe);
                Gizmos.DrawLine(center, owner.ChosenSafeZone.Center);
                Gizmos.DrawWireSphere(owner.ChosenSafeZone.Center, 0.6f);
            }

            if (Time.time - lastAttackTime <= attackLineDuration)
            {
                Gizmos.color = lastAttackKind == AttackKind.Melee ? Color.red : new Color(1f, 0.55f, 0.1f);
                Gizmos.DrawLine(center, lastAttackTarget);
            }

            DrawLabel(owner);
        }

        private void DrawLabel(HunterController owner)
        {
#if UNITY_EDITOR
            string zone = owner.ChosenSafeZone != null ? $"\nSafe zone: {owner.ChosenSafeZone.name}" : string.Empty;
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Hunter: {owner.CurrentState}\nGathers: {owner.Gathers}/{owner.RequiredGathers}{zone}");
#endif
        }
    }
}
