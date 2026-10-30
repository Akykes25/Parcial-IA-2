using System.Collections;
using System.Collections.Generic;
using Parcial2.Core;
using Parcial2.Perception;
using Parcial2.Steering;
using UnityEngine;

namespace Parcial2.Boids
{
    public enum BoidBehaviourState
    {
        Flocking,
        Arrive,
        Evade,
        Inactive,
        Collected
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoidFlocking), typeof(BoidMotor), typeof(SphereCollider))]
    public sealed class BoidAgent : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [Header("Sensing")]
        [SerializeField, Min(0.1f)] private float hunterVisionRadius = 8f;
        [SerializeField, Min(0.1f)] private float interestSearchRadius = 13f;
        [SerializeField] private LayerMask hunterLayerMask = GameLayers.HunterMask;
        [SerializeField] private LayerMask interestLayerMask = GameLayers.InterestMask;

        [Header("Steering")]
        [SerializeField, Min(0f)] private float arriveWeight = 1.65f;
        [SerializeField, Min(0f)] private float evadeWeight = 3.5f;
        [SerializeField, Min(0.1f)] private float slowingRadius = 2f;
        [SerializeField, Min(0.1f)] private float interestInteractionDistance = 1.35f;

        [Header("Life and interaction")]
        [SerializeField, Min(0.1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float interactionInterval = 0.7f;
        [SerializeField, Min(0f)] private float interestDamagePerInteraction = 0.5f;
        [SerializeField, Min(0f)] private float respawnDelay = 4f;
        [SerializeField] private Color baseColor = Color.cyan;

        private BoidFlocking flocking;
        private BoidMotor motor;
        private Collider bodyCollider;
        private Renderer[] visualRenderers;
        private MaterialPropertyBlock colorProperties;
        private PhysicsSensorBuffer hunterSensor;
        private PhysicsSensorBuffer interestSensor;
        private readonly HashSet<InterestObject> sensedInterests = new HashSet<InterestObject>();
        private ZoneVolume huntingGround;
        private float health;
        private float interactionTimer;
        private float hitFlashTimer;
        private bool isCollected;
        private InterestObject currentInterest;
        private BoidBehaviourState behaviourState = BoidBehaviourState.Flocking;

        public Vector3 Velocity => motor != null ? motor.Velocity : Vector3.zero;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool IsAlive => health > 0f && !isCollected;
        public bool IsDead => health <= 0f && !isCollected;
        public bool IsCollected => isCollected;
        public BoidBehaviourState BehaviourState => behaviourState;
        public string CurrentBehaviour => behaviourState.ToString();
        public InterestObject CurrentInterest => currentInterest;
        public float HunterDetectionRadius => hunterVisionRadius;

        private void Awake()
        {
            flocking = GetComponent<BoidFlocking>();
            motor = GetComponent<BoidMotor>();
            bodyCollider = GetComponent<Collider>();
            visualRenderers = GetComponentsInChildren<Renderer>();
            colorProperties = new MaterialPropertyBlock();
            hunterSensor = new PhysicsSensorBuffer();
            interestSensor = new PhysicsSensorBuffer();
            health = maxHealth;
        }

        private void Start()
        {
            SimulationContext context = SimulationContext.Instance;
            huntingGround = context != null ? context.HuntingGround : null;
            if (huntingGround == null)
            {
                Debug.LogError("A BoidAgent requires SimulationContext.HuntingGround to stay inside its zone.", this);
                enabled = false;
                return;
            }

            motor.Initialize(huntingGround, Random.insideUnitSphere * motor.MaxSpeed * 0.7f);
            ApplyVisualColor();
        }

        private void FixedUpdate()
        {
            if (isCollected)
                return;

            if (!IsAlive)
            {
                motor.Stop();
                SetBehaviourState(BoidBehaviourState.Inactive);
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            Vector3 flockingSteering = flocking.CalculateSteering();
            Vector3 desiredVelocity;

            if (TryFindHunter(out Transform hunter, out Vector3 hunterVelocity))
            {
                currentInterest = null;
                SetBehaviourState(BoidBehaviourState.Evade);
                Vector3 evadeDirection = SteeringBehaviours.Evade(
                    transform.position,
                    hunter.position,
                    hunterVelocity,
                    1.1f);
                desiredVelocity = evadeDirection * evadeWeight + flockingSteering;
            }
            else
            {
                currentInterest = FindNearestInterest();
                if (currentInterest != null)
                {
                    SetBehaviourState(BoidBehaviourState.Arrive);
                    Vector3 offsetToInterest = currentInterest.transform.position - transform.position;
                    offsetToInterest.y = 0f;
                    Vector3 approachTarget = currentInterest.transform.position;
                    if (offsetToInterest.sqrMagnitude > 0.001f)
                        approachTarget -= offsetToInterest.normalized * interestInteractionDistance;

                    Vector3 arriveVelocity = SteeringBehaviours.Arrive(
                        transform.position,
                        approachTarget,
                        motor.MaxSpeed,
                        slowingRadius);
                    desiredVelocity = arriveVelocity * arriveWeight + flockingSteering;
                    InteractWithInterest(currentInterest, deltaTime);
                }
                else
                {
                    SetBehaviourState(BoidBehaviourState.Flocking);
                    desiredVelocity = flockingSteering.sqrMagnitude > 0.001f
                        ? flockingSteering.normalized * motor.MaxSpeed
                        : transform.forward * motor.MaxSpeed;
                }
            }

            desiredVelocity.y = 0f;
            KeepSafeDistanceFromInterest(ref desiredVelocity, deltaTime);
            motor.Move(desiredVelocity, huntingGround, deltaTime);
            UpdateVisualFeedback(deltaTime);
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive)
                return;

            health = Mathf.Max(0f, health - Mathf.Max(0f, damage));
            hitFlashTimer = 0.22f;
            if (health <= 0f)
            {
                motor.Stop();
                currentInterest = null;
                SetBehaviourState(BoidBehaviourState.Inactive);
            }
        }

        public void CollectAndRespawn()
        {
            if (!IsDead || isCollected)
                return;

            isCollected = true;
            motor.Stop();
            currentInterest = null;
            SetBehaviourState(BoidBehaviourState.Collected);
            if (bodyCollider != null)
                bodyCollider.enabled = false;

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                    visualRenderer.enabled = false;
            }

            StopAllCoroutines();
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            motor.RespawnInside(huntingGround, transform.position.y);
            health = maxHealth;
            isCollected = false;
            interactionTimer = 0.4f;

            if (bodyCollider != null)
                bodyCollider.enabled = true;

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                    visualRenderer.enabled = true;
            }

            SetBehaviourState(BoidBehaviourState.Flocking);
        }

        private bool TryFindHunter(out Transform hunter, out Vector3 hunterVelocity)
        {
            int colliderCount = hunterSensor.Query(transform.position, hunterVisionRadius, hunterLayerMask.value);
            hunter = null;
            hunterVelocity = Vector3.zero;
            float closestSqrDistance = hunterVisionRadius * hunterVisionRadius;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider nearbyCollider = hunterSensor[i];
                if (nearbyCollider == null)
                    continue;

                Transform candidate = nearbyCollider.attachedRigidbody != null
                    ? nearbyCollider.attachedRigidbody.transform
                    : nearbyCollider.transform.root;
                Vector3 offset = candidate.position - transform.position;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance > closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                hunter = candidate;
                if (nearbyCollider.attachedRigidbody != null)
                    hunterVelocity = nearbyCollider.attachedRigidbody.linearVelocity;
            }

            return hunter != null;
        }

        private InterestObject FindNearestInterest()
        {
            int colliderCount = interestSensor.Query(
                transform.position,
                interestSearchRadius,
                interestLayerMask.value);
            sensedInterests.Clear();
            InterestObject closestInterest = null;
            float closestSqrDistance = interestSearchRadius * interestSearchRadius;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider nearbyCollider = interestSensor[i];
                if (nearbyCollider == null)
                    continue;

                InterestObject interest = nearbyCollider.GetComponentInParent<InterestObject>();
                if (interest == null || !interest.IsAvailable || !sensedInterests.Add(interest))
                    continue;

                float sqrDistance = (interest.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance <= closestSqrDistance)
                {
                    closestInterest = interest;
                    closestSqrDistance = sqrDistance;
                }
            }

            return closestInterest;
        }

        private void InteractWithInterest(InterestObject interest, float deltaTime)
        {
            Vector3 offset = interest.transform.position - transform.position;
            if (offset.sqrMagnitude > interestInteractionDistance * interestInteractionDistance)
                return;

            interactionTimer -= deltaTime;
            if (interactionTimer > 0f)
                return;

            interactionTimer = interactionInterval;
            interest.ReceiveDamage(interestDamagePerInteraction);
        }

        private void KeepSafeDistanceFromInterest(ref Vector3 desiredVelocity, float deltaTime)
        {
            if (currentInterest == null || !currentInterest.IsAvailable)
                return;

            Vector3 offsetFromInterest = transform.position - currentInterest.transform.position;
            offsetFromInterest.y = 0f;
            float distance = offsetFromInterest.magnitude;
            if (distance > interestInteractionDistance + desiredVelocity.magnitude * deltaTime)
                return;

            Vector3 outwardDirection = distance > 0.001f ? offsetFromInterest / distance : transform.right;
            float inwardSpeed = Vector3.Dot(desiredVelocity, outwardDirection);
            if (inwardSpeed < 0f)
                desiredVelocity -= outwardDirection * inwardSpeed;

            if (distance < interestInteractionDistance)
                desiredVelocity += outwardDirection * (motor.MaxSpeed * 0.5f);
        }

        private void SetBehaviourState(BoidBehaviourState newState)
        {
            if (behaviourState == newState)
                return;

            behaviourState = newState;
            ApplyVisualColor();
        }

        private void UpdateVisualFeedback(float deltaTime)
        {
            if (hitFlashTimer <= 0f)
                return;

            hitFlashTimer -= deltaTime;
            ApplyVisualColor();
        }

        private void ApplyVisualColor()
        {
            Color stateColor = baseColor;
            switch (behaviourState)
            {
                case BoidBehaviourState.Arrive:
                    stateColor = new Color(1f, 0.72f, 0.12f);
                    break;
                case BoidBehaviourState.Evade:
                    stateColor = new Color(1f, 0.22f, 0.78f);
                    break;
                case BoidBehaviourState.Inactive:
                    stateColor = new Color(0.28f, 0.28f, 0.32f);
                    break;
                case BoidBehaviourState.Collected:
                    stateColor = Color.clear;
                    break;
            }

            if (hitFlashTimer > 0f)
                stateColor = Color.white;

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer == null || !visualRenderer.enabled)
                    continue;

                visualRenderer.GetPropertyBlock(colorProperties);
                colorProperties.SetColor(BaseColorProperty, stateColor);
                colorProperties.SetColor(ColorProperty, stateColor);
                visualRenderer.SetPropertyBlock(colorProperties);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.75f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, hunterVisionRadius);
            Gizmos.color = new Color(1f, 0.78f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, interestSearchRadius);
        }
    }
}
