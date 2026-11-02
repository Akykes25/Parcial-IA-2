using System;
using Parcial2.Boids;
using Parcial2.Core;
using Parcial2.Navigation;
using Parcial2.Perception;
using Parcial2.Hunter.States;
using UnityEngine;

namespace Parcial2.Hunter
{
    public enum AttackKind
    {
        Melee,
        Ranged
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(HunterMotor), typeof(InterestSpawner))]
    public sealed class HunterController : MonoBehaviour, IHunterConditions
    {
        [Header("Objective")]
        [SerializeField, Min(1)] private int requiredGathers = 3;
        [SerializeField, Min(0f)] private float respawnTime = 5f;
        [SerializeField, Min(0f)] private float safeZoneWaitTime = 3f;

        [Header("Perception")]
        [SerializeField, Min(0.1f)] private float visionRadius = 10f;
        [SerializeField, Min(0.1f)] private float wolfDetectionRadius = 9f;
        [SerializeField] private VisionSensor boidSensor;
        [SerializeField] private VisionSensor wolfSensor;
        [Tooltip("Routes avoid graph nodes this close to a wolf seen recently.")]
        [SerializeField, Min(0.1f)] private float threatAvoidRadius = 3.5f;
        [Tooltip("Seconds a seen wolf position keeps shaping the Hunter's routes.")]
        [SerializeField, Min(0f)] private float threatMemoryTime = 20f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float timeBetweenAttacks = 1.5f;
        [SerializeField, Min(0.1f)] private float meleeAttackRadius = 1.6f;
        [SerializeField, Min(0.1f)] private float rangeAttackRadius = 5f;
        [SerializeField, Min(0f)] private float meleeDamage = 100f;
        [SerializeField, Min(0f)] private float rangedDamage = 50f;
        [SerializeField, Min(0f)] private float maxPrediction = 1f;

        [Header("Gather")]
        [SerializeField, Min(0f)] private float gatherDuration = 1.5f;
        [SerializeField, Min(0.1f)] private float gatherReachDistance = 1.2f;

        [Header("Search")]
        [SerializeField, Min(0.1f)] private float interestSpawnInterval = 4f;

        [Header("Movement (wolves must stay slower than every speed here)")]
        [SerializeField, Min(0.1f)] private float searchSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float attackSpeed = 6f;
        [SerializeField, Min(0.1f)] private float gatherSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float fleeSpeed = 6.5f;
        [SerializeField, Min(0.1f)] private float exitSpeed = 5f;
        [SerializeField, Min(0.05f)] private float agentRadius = 0.45f;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.6f;
        [SerializeField, Min(0.05f)] private float slowingRadius = 1.5f;

        private HunterMotor motor;
        private Renderer[] bodyRenderers;
        private HunterBrain brain;
        private float nextAttackTime;

        public int RequiredGathers => requiredGathers;
        public float RespawnTime => respawnTime;
        public float SafeZoneWaitTime => safeZoneWaitTime;
        public float VisionRadius => visionRadius;
        public float WolfDetectionRadius => wolfDetectionRadius;
        public float MeleeAttackRadius => meleeAttackRadius;
        public float RangeAttackRadius => rangeAttackRadius;
        public float MeleeDamage => meleeDamage;
        public float RangedDamage => rangedDamage;
        public float MaxPrediction => maxPrediction;
        public float GatherDuration => gatherDuration;
        public float GatherReachDistance => gatherReachDistance;
        public float InterestSpawnInterval => interestSpawnInterval;
        public float SearchSpeed => searchSpeed;
        public float AttackSpeed => attackSpeed;
        public float GatherSpeed => gatherSpeed;
        public float FleeSpeed => fleeSpeed;
        public float ExitSpeed => exitSpeed;
        public float ArrivalDistance => arrivalDistance;

        public SimulationContext Context { get; private set; }
        public NavigationAgent Navigation { get; private set; }
        public HunterPerception Perception { get; private set; }
        public ThreatMemory Threats { get; private set; }
        public InterestSpawner Spawner { get; private set; }
        public HunterMotor Motor => motor;
        public Vector3 Position => motor.Position;
        public int Gathers { get; private set; }

        public int ExitsReached { get; private set; }
        public bool IsPresent { get; private set; } = true;
        public bool IsSheltered => IsPresent && Context != null && Context.IsInsideSafeZone(Position);
        public HunterStateId CurrentState => brain != null ? brain.Current : HunterStateId.Search;
        public ZoneVolume ChosenSafeZone { get; private set; }
        public bool IsAttackReady => Time.time >= nextAttackTime;

        public bool IsCaught { get; private set; }
        public bool SeesWolf => IsPresent && Perception.SeesWolf;
        public bool HasRequiredGathers => Gathers >= requiredGathers;
        public bool SeesDeadBoid => Perception.SeesBoid(dead: true);
        public bool CanAttack => IsAttackReady && Perception.SeesBoid(dead: false);

        public event Action<HunterStateId, HunterStateId> StateChanged;
        public event Action<int, int> GathersChanged;
        public event Action<ZoneVolume, Transform> SafeZoneChosen;
        public event Action<BoidAgent, AttackKind> AttackPerformed;
        public event Action Caught;

        private void Awake()
        {
            motor = GetComponent<HunterMotor>();
            Spawner = GetComponent<InterestSpawner>();
            bodyRenderers = GetComponentsInChildren<Renderer>();
            boidSensor.Configure(visionRadius, VisionQuery.FullAngle, GameLayers.BoidMask, GameLayers.ObstacleMask);
            wolfSensor.Configure(wolfDetectionRadius, VisionQuery.FullAngle, GameLayers.WolfMask,
                GameLayers.ObstacleMask);
            Perception = new HunterPerception(boidSensor, wolfSensor);
            Threats = new ThreatMemory(threatAvoidRadius, threatMemoryTime);
        }

        private void Start()
        {
            Context = SimulationContext.Instance;
            if (Context == null || Context.Graph == null || Context.StartZone == null)
            {
                Debug.LogError("HunterController requires a SimulationContext with graph and zones.", this);
                enabled = false;
                return;
            }

            Navigation = new NavigationAgent(Context.Graph, agentRadius, arrivalDistance, slowingRadius,
                GameLayers.ObstacleMask) { Threats = Threats };
            brain = new HunterBrain(this, new HunterStateSet(new SearchState(this), new AttackState(this),
                new GatherState(this), new FleeState(this), new ExitState(this), new DeadState(this)));
            brain.StateChanged += OnBrainStateChanged;
            brain.Start();
        }

        private void OnDestroy()
        {
            if (brain != null)
                brain.StateChanged -= OnBrainStateChanged;
        }

        private void Update()
        {
            motor.DesiredVelocity = Vector3.zero;
            if (IsPresent && Threats.Record(Perception.VisibleWolves, Time.time))
                Navigation.InvalidatePath();
            brain.Update();
        }

        public void MoveTo(Vector3 destination, float speed)
        {
            destination.y = Position.y;
            Navigation.SetDestination(destination);
            motor.DesiredVelocity = Navigation.Tick(Position, speed, Time.time);
        }

        public void Steer(Vector3 velocity)
        {
            Navigation.Stop();
            motor.DesiredVelocity = velocity;
        }

        public void Halt()
        {
            Navigation.Stop();
            motor.DesiredVelocity = Vector3.zero;
        }

        public bool HasArrived(Vector3 destination)
        {
            Vector3 offset = destination - Position;
            offset.y = 0f;
            return offset.sqrMagnitude <= arrivalDistance * arrivalDistance;
        }

        public void PerformAttack(BoidAgent target, AttackKind kind)
        {
            Halt();
            motor.FaceTowards(target.transform.position);
            target.TakeDamage(kind == AttackKind.Melee ? meleeDamage : rangedDamage);
            nextAttackTime = Time.time + timeBetweenAttacks;
            AttackPerformed?.Invoke(target, kind);
        }

        public void AddGather() => SetGathers(Gathers + 1);

        public void ChooseSafeZone(ZoneVolume zone)
        {
            ChosenSafeZone = zone;
            SafeZoneChosen?.Invoke(zone, Perception.ClosestWolf);
        }

        public void ClearSafeZone() => ChosenSafeZone = null;

        public void Capture()
        {
            if (IsCaught || !IsPresent || IsSheltered)
                return;

            IsCaught = true;
            Caught?.Invoke();
        }

        public void SetPresent(bool present)
        {
            IsPresent = present;
            motor.SetSimulated(present);
            foreach (Renderer part in bodyRenderers)
                part.enabled = present;
        }

        public void CompleteExit()
        {
            ExitsReached++;
            RestartCycle();
        }

        public void RestartCycle()
        {
            Halt();
            Vector3 spawn = Context.StartZone.Center;
            spawn.y = Position.y;
            motor.Teleport(spawn);
            IsCaught = false;
            Threats.Clear();
            ClearSafeZone();
            SetGathers(0);
        }

        private void SetGathers(int value)
        {
            Gathers = value;
            GathersChanged?.Invoke(Gathers, requiredGathers);
        }

        private void OnBrainStateChanged(HunterStateId from, HunterStateId to) => StateChanged?.Invoke(from, to);
    }
}
