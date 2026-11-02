using Parcial2.Wolves;
using UnityEngine;

namespace Parcial2.Feedback
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WolfController))]
    public sealed class WolfFeedback : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private static readonly Color SpottedFlash = Color.white;
        private static readonly Color HowlFlash = new(0.2f, 0.95f, 1f);
        private static readonly Color AlertFlash = new(1f, 0.9f, 0.2f);

        [SerializeField, Min(0f)] private float flashDuration = 0.6f;
        [SerializeField] private bool logEvents = true;

        private WolfController wolf;
        private Renderer[] bodyRenderers;
        private MaterialPropertyBlock colorProperties;
        private float flashUntil;
        private bool flashing;
        private string lastEvent = string.Empty;

        private void Awake()
        {
            wolf = GetComponent<WolfController>();
            bodyRenderers = GetComponentsInChildren<Renderer>();
            colorProperties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            wolf.StateChanged += OnStateChanged;
            wolf.HunterDetected += OnHunterDetected;
            wolf.Howled += OnHowled;
            wolf.AlertReceived += OnAlertReceived;
        }

        private void OnDisable()
        {
            wolf.StateChanged -= OnStateChanged;
            wolf.HunterDetected -= OnHunterDetected;
            wolf.Howled -= OnHowled;
            wolf.AlertReceived -= OnAlertReceived;
        }

        private void Start() => ApplyColor(ColorFor(WolfStateId.Patrol));

        private void Update()
        {
            if (flashing && Time.time >= flashUntil)
            {
                flashing = false;
                ApplyColor(ColorFor(wolf.CurrentState));
            }
        }

        public static Color ColorFor(WolfStateId state) => state switch
        {
            WolfStateId.Chase => new Color(0.9f, 0.1f, 0.1f),
            WolfStateId.Investigate => new Color(1f, 0.55f, 0.1f),
            _ => new Color(0.55f, 0.55f, 0.62f)
        };

        private void OnStateChanged(WolfStateId from, WolfStateId to)
        {
            if (!flashing)
                ApplyColor(ColorFor(to));
        }

        private void OnHunterDetected(Vector3 position)
        {
            Flash(SpottedFlash, "spotted the Hunter");
            Log($"{name}: Hunter detected at {position:F1}.");
        }

        private void OnHowled(Vector3 position)
        {
            Flash(HowlFlash, "howled");
            Log($"{name}: alert emitted, last known position {position:F1}.");
        }

        private void OnAlertReceived(WolfController source, Vector3 position)
        {
            Flash(AlertFlash, $"alert from {source.name}");
            Log($"{name}: alert received from {source.name}, going to {position:F1}.");
        }

        private void Flash(Color color, string description)
        {
            lastEvent = description;
            flashing = true;
            flashUntil = Time.time + flashDuration;
            ApplyColor(color);
        }

        private void Log(string message)
        {
            if (logEvents)
                Debug.Log($"[Wolf] {message}", this);
        }

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
            WolfController owner = wolf != null ? wolf : GetComponent<WolfController>();
            if (owner == null)
                return;

            if (Application.isPlaying)
                FeedbackGizmos.DrawPath(owner.Navigation);

            DrawLabel(owner);
        }

        private void DrawLabel(WolfController owner)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;

            string sight = owner.SeesHunter ? "\nSEES HUNTER" : string.Empty;
            string recent = flashing ? $"\n{lastEvent}" : string.Empty;
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.8f,
                $"{name}: {owner.CurrentState}{sight}{recent}");
#endif
        }
    }
}
