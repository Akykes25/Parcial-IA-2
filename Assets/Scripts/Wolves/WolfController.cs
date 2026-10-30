using System;
using Parcial2.Core;
using Parcial2.Hunter;
using Parcial2.Navigation;
using Parcial2.Perception;
using Parcial2.Wolves.States;
using UnityEngine;

namespace Parcial2.Wolves
{
    /// <summary>
    /// Wolf NPC: owns its tunables, keeps what it knows about the Hunter (sight and last known position) and
    /// offers the actions its states use. Every decision is taken by <see cref="WolfBrain"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WolfMotor), typeof(PatrolRoute))]
    public sealed class WolfController : MonoBehaviour, IWolfConditions
    {
        [Header("Perception")]
        [SerializeField] private VisionSensor sensor;
        [SerializeField, Min(0.1f)] private float visionRange = 9f;
        [SerializeField, Range(1f, VisionQuery.FullAngle)] private float viewAngle = 110f;

        [Header("Movement (keep every speed below the Hunter's slowest, 4.5)")]
        [SerializeField, Min(0.1f)] private float patrolSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float investigateSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 4f;
        [SerializeField, Min(0.05f)] private float agentRadius = 0.45f;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.7f;
        [SerializeField, Min(0.05f)] private float slowingRadius = 1.2f;
        [SerializeField, Min(0f)] private float safeZoneMargin = 0.6f;

        [Header("Hunting")]
        [SerializeField, Min(0.1f)] private float captureRadius = 1.1f;
        [SerializeField, Min(0f)] private float howlCooldown = 3f;
        [SerializeField, Min(0f)] private float searchDuration = 5f;
        [SerializeField, Min(0.1f)] private float searchRadius = 5f;
        [SerializeField, Min(1f)] private float maxInvestigateTime = 20f;
        [SerializeField, Min(0f)] private float lookAroundSpeed = 90f;

        private WolfMotor motor;
        private WolfBrain brain;
        private Transform cachedTarget;
        private HunterController cachedHunter;
        private HunterController knownHunter;
        private float nextHowlTime;

        public float PatrolSpeed => patrolSpeed;
        public float InvestigateSpeed => investigateSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float CaptureRadius => captureRadius;
        public float SearchDuration => searchDuration;
        public float SearchRadius => searchRadius;
        public float MaxInvestigateTime => maxInvestigateTime;
        public float ArrivalDistance => arrivalDistance;

        public SimulationContext Context { get; private set; }
        public WolfPack Pack { get; private set; }
        public NavigationAgent Navigation { get; private set; }
        public PatrolRoute Route { get; private set; }
        public VisionSensor Sensor => sensor;
        public Vector3 Position => motor.Position;
        public WolfStateId CurrentState => brain != null ? brain.Current : WolfStateId.Patrol;

        public bool SeesHunter { get; private set; }
        public bool HasLastKnownPosition { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }

        /// <summary>Changes every time the last known position is replaced, so states can restart on news.</summary>
        public int KnowledgeVersion { get; private set; }

        public event Action<WolfStateId, WolfStateId> StateChanged;
        public event Action<Vector3> HunterDetected;
        public event Action<Vector3> Howled;
        public event Action<WolfController, Vector3> AlertReceived;
        public event Action TargetLost;

        private void Awake()
        {
            motor = GetComponent<WolfMotor>();
            Route = GetComponent<PatrolRoute>();
            sensor.Configure(visionRange, viewAngle, GameLayers.HunterMask, GameLayers.ObstacleMask);
            sensor.TargetFilter = IsDetectable;
        }

        private void Start()
        {
            Context = SimulationContext.Instance;
            Pack = WolfPack.Instance;
            if (Context == null || Context.Graph == null || Pack == null || Route.Count == 0)
            {
                Debug.LogError("WolfController requires a SimulationContext with graph, a WolfPack and a route.", this);
                enabled = false;
                return;
            }

            Navigation = new NavigationAgent(Context.Graph, agentRadius, arrivalDistance, slowingRadius,
                GameLayers.ObstacleMask) { NodeFilter = node => !node.IsSafeZone };
            brain = new WolfBrain(this, new WolfStateSet(new PatrolState(this), new ChaseState(this),
                new InvestigateState(this)));
            brain.StateChanged += OnBrainStateChanged;
            Pack.HowlRaised += OnHowlRaised;
            Pack.HunterSheltered += OnTargetGone;
            Pack.HunterCaught += OnTargetGone;
            brain.Start();
        }

        private void OnDestroy()
        {
            if (brain != null)
                brain.StateChanged -= OnBrainStateChanged;

            if (Pack != null)
            {
                Pack.HowlRaised -= OnHowlRaised;
                Pack.HunterSheltered -= OnTargetGone;
                Pack.HunterCaught -= OnTargetGone;
            }
        }

        private void Update()
        {
            motor.DesiredVelocity = Vector3.zero;
            RefreshSight();
            brain.Update();
            TryCapture();
        }

        /// <summary>Moves through the graph without safe-zone nodes: straight when clear, A* otherwise.</summary>
        public void MoveTo(Vector3 destination, float speed)
        {
            destination.y = Position.y;
            Navigation.SetDestination(destination);
            Vector3 velocity = Navigation.Tick(Position, speed, Time.time);

            // Direct steering could cut through a safe-zone doorway; the wolf stops at its edge instead.
            if (velocity.sqrMagnitude > 0.0001f
                && Context.IsInsideSafeZone(Position + velocity.normalized * safeZoneMargin))
                velocity = Vector3.zero;

            motor.DesiredVelocity = velocity;
        }

        public void Halt()
        {
            Navigation.Stop();
            motor.DesiredVelocity = Vector3.zero;
        }

        public void LookAround()
        {
            Halt();
            motor.Turn(lookAroundSpeed);
        }

        public bool HasArrived(Vector3 destination)
        {
            Vector3 offset = destination - Position;
            offset.y = 0f;
            return offset.sqrMagnitude <= arrivalDistance * arrivalDistance;
        }

        /// <summary>Alerts the pack with the position where the Hunter was just seen.</summary>
        public void Howl()
        {
            if (knownHunter == null || Time.time < nextHowlTime)
                return;

            nextHowlTime = Time.time + howlCooldown;
            Howled?.Invoke(LastKnownPosition);
            Pack.RaiseHowl(this, knownHunter, LastKnownPosition);
        }

        /// <summary>Drops the target and its last known position; the brain goes back to patrol.</summary>
        public void ForgetHunter()
        {
            bool hadTarget = HasLastKnownPosition;
            HasLastKnownPosition = false;
            Navigation.Stop();
            if (hadTarget)
                TargetLost?.Invoke();
        }

        private void RefreshSight()
        {
            bool sawHunter = SeesHunter;
            // Re-checked here because the last scan can be up to one scan interval old.
            SeesHunter = sensor.HasTarget && IsDetectable(sensor.ClosestTarget);
            if (!SeesHunter)
                return;

            knownHunter = cachedHunter;
            Remember(knownHunter.Position);
            if (sawHunter)
                return;

            Pack.Track(this, knownHunter, LastKnownPosition);
            HunterDetected?.Invoke(LastKnownPosition);
        }

        private void Remember(Vector3 position)
        {
            LastKnownPosition = position;
            HasLastKnownPosition = true;
            KnowledgeVersion++;
        }

        private void TryCapture()
        {
            if (knownHunter == null || !knownHunter.IsPresent)
                return;

            Vector3 offset = knownHunter.Position - Position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= captureRadius * captureRadius)
                knownHunter.Capture();
        }

        /// <summary>Sensor veto: a Hunter that is caught, dead or sheltered in a safe zone cannot be detected.</summary>
        private bool IsDetectable(Transform target)
        {
            HunterController hunter = ResolveHunter(target);
            return hunter != null && hunter.IsPresent && !hunter.IsCaught && !hunter.IsSheltered;
        }

        private HunterController ResolveHunter(Transform target)
        {
            if (target == null)
                return null;

            if (target != cachedTarget)
            {
                cachedTarget = target;
                target.TryGetComponent(out cachedHunter);
            }

            return cachedHunter;
        }

        private void OnHowlRaised(WolfController source, Vector3 lastKnownPosition)
        {
            if (source == this)
                return;

            knownHunter = Pack.Target;
            AlertReceived?.Invoke(source, lastKnownPosition);
            Pack.ReportAlertReceived(this, source, lastKnownPosition);
            if (!SeesHunter)
                Remember(lastKnownPosition);
        }

        /// <summary>Sheltered or caught: drop the target and the last known position, back to patrol.</summary>
        private void OnTargetGone()
        {
            SeesHunter = false;
            ForgetHunter();
        }

        private void OnBrainStateChanged(WolfStateId from, WolfStateId to) => StateChanged?.Invoke(from, to);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.85f, 0.15f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, captureRadius);

            if (!HasLastKnownPosition)
                return;

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(transform.position, LastKnownPosition);
            Gizmos.DrawWireCube(LastKnownPosition, Vector3.one * 0.6f);
        }
    }
}
