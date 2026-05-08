using UnityEngine;
using CatnipCart.Core;

namespace CatnipCart.Kart
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : MonoBehaviour
    {
        [Header("Configuration")]
        public KartStats stats;
        [Header("Ground Detection")]
        public Transform groundRayOrigin;
        public LayerMask groundLayer = ~0;

        public enum KartState { Normal, Drifting, Boosting, SpinOut, Falling }
        public KartState CurrentState { get; private set; } = KartState.Normal;
        public float CurrentSpeed { get; private set; }
        public float NormalizedSpeed => stats ? Mathf.Clamp01(CurrentSpeed / stats.maxSpeed) : 0f;
        public bool IsGrounded { get; private set; }
        public int DriftStage { get; private set; }
        public float DriftTime { get; private set; }
        public int DriftDirection { get; private set; }
        public bool IsBoosting => boostTimer > 0f;
        public bool IsEntangled => entangleTimer > 0f;

        private Rigidbody rb;
        private IKartInput input;
        private float currentSteerInput, boostTimer, boostForce, spinOutTimer, entangleTimer;
        private Vector3 groundNormal = Vector3.up;
        private bool wasGrounded;
        private float airborneTimer; // Track how long we've been in the air

        public System.Action OnDriftStart, OnDriftEnd, OnSpinOut, OnLand, OnEntangle;
        public System.Action<int> OnDriftStageChange;
        public System.Action<float> OnBoostStart;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<IKartInput>() as IKartInput;
        }

        void Start()
        {
            // Retry finding input in Start() in case it was added after our Awake()
            if (input == null)
                input = GetComponent<IKartInput>() as IKartInput;

            if (stats == null)
                Debug.LogError($"[KartController] No KartStats assigned on {gameObject.name}!", this);
            if (input == null)
                Debug.LogError($"[KartController] No IKartInput found on {gameObject.name}!", this);

            rb.mass = stats != null ? stats.mass : 1000f;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private Items.ItemHolder itemHolder;

        void FixedUpdate()
        {
            if (input == null || stats == null) return;
            CheckGround();
            HandleState();
        }

        void Update()
        {
            if (input == null) return;

            // Item use — must be in Update() because wasPressedThisFrame only works per-frame
            if (input.UseItem)
            {
                if (itemHolder == null)
                    itemHolder = GetComponent<Items.ItemHolder>();
                if (itemHolder != null && itemHolder.HasItem)
                    itemHolder.UseItem();
            }
        }

        void CheckGround()
        {
            Vector3 origin = groundRayOrigin ? groundRayOrigin.position : transform.position + Vector3.up * 0.5f;
            wasGrounded = IsGrounded;

            // Generous ground check distance — increases when airborne to help re-acquire ground
            float checkDist = stats.groundCheckDistance * 2f;
            if (!wasGrounded) checkDist *= 1.5f; // Even more generous when trying to re-land

            bool foundGround = false;
            RaycastHit bestHit = default;
            float bestDist = float.MaxValue;

            // Center ray + 4 corner rays + 4 wider rays
            Vector3[] offsets = new Vector3[]
            {
                Vector3.zero,
                transform.forward * 0.5f,
                -transform.forward * 0.5f,
                transform.right * 0.4f,
                -transform.right * 0.4f,
                transform.forward * 0.5f + transform.right * 0.4f,
                transform.forward * 0.5f - transform.right * 0.4f,
                -transform.forward * 0.5f + transform.right * 0.4f,
                -transform.forward * 0.5f - transform.right * 0.4f,
            };

            foreach (var offset in offsets)
            {
                // Cast straight down — use default layer detection (skips "Ignore Raycast" layer)
                if (Physics.Raycast(origin + offset, Vector3.down, out RaycastHit hit, checkDist))
                {
                    // Skip trigger colliders (checkpoints etc.)
                    if (hit.collider.isTrigger) continue;

                    // Skip small objects (yarn balls, item boxes) — only ground on large surfaces
                    // Track meshes use MeshCollider, floor uses MeshCollider on Plane
                    if (hit.collider is SphereCollider) continue; // yarn balls
                    if (hit.collider is BoxCollider bc && bc.size.x < 3f) continue; // small boxes

                    if (hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        bestHit = hit;
                        foundGround = true;
                    }
                }
            }

            if (foundGround)
            {
                IsGrounded = true;
                groundNormal = bestHit.normal;
                airborneTimer = 0f;

                // Smoothly settle onto the ground
                float targetY = bestHit.point.y + 0.5f;
                Vector3 pos = transform.position;
                float diff = targetY - pos.y;

                if (diff > 0) // Below ground — push up
                {
                    pos.y = Mathf.Lerp(pos.y, targetY, Time.fixedDeltaTime * 15f);
                    transform.position = pos;
                }
                else if (diff > -0.3f) // Slightly above — settle
                {
                    pos.y = Mathf.Lerp(pos.y, targetY, Time.fixedDeltaTime * 10f);
                    transform.position = pos;
                }

                // Kill downward velocity when grounded
                if (rb.linearVelocity.y < -1f)
                {
                    var v = rb.linearVelocity;
                    v.y *= 0.5f;
                    rb.linearVelocity = v;
                }

                if (!wasGrounded) OnLand?.Invoke();
            }
            else
            {
                IsGrounded = false;
                groundNormal = Vector3.up;
                airborneTimer += Time.fixedDeltaTime;

                // Safety: if airborne too long, teleport back to track
                if (airborneTimer > 4f)
                {
                    RespawnOnTrack();
                    airborneTimer = 0f;
                }
            }
        }

        void RespawnOnTrack()
        {
            // Find the track spline and put us back on it
            var spline = FindAnyObjectByType<Track.TrackSpline>();
            if (spline != null)
            {
                float dist = spline.GetNearestDistance(transform.position);
                Vector3 trackPos = spline.GetPointAtDistance(dist);
                Vector3 trackFwd = spline.GetDirectionAtDistance(dist);
                transform.position = trackPos + Vector3.up * 1.5f;
                transform.rotation = Quaternion.LookRotation(trackFwd);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                CurrentState = KartState.Normal;
            }
        }

        void HandleState()
        {
            switch (CurrentState)
            {
                case KartState.Normal:
                case KartState.Boosting:
                    UpdateDriving(); break;
                case KartState.Drifting:
                    UpdateDrifting(); break;
                case KartState.SpinOut:
                    UpdateSpinOut(); break;
                case KartState.Falling:
                    if (IsGrounded) CurrentState = KartState.Normal; break;
            }
            if (entangleTimer > 0f) entangleTimer -= Time.fixedDeltaTime;
            if (boostTimer > 0f) { boostTimer -= Time.fixedDeltaTime; if (boostTimer <= 0) { boostTimer = 0; boostForce = 0; if (CurrentState == KartState.Boosting) CurrentState = KartState.Normal; } }
            if (!IsGrounded) rb.AddForce(Vector3.down * stats.gravity, ForceMode.Acceleration);
            AlignToGround();
            CurrentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        }

        void UpdateDriving()
        {
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, input.Steer, stats.steerInputSmoothing * Time.fixedDeltaTime);
            if (input.Drift && IsGrounded && Mathf.Abs(currentSteerInput) > 0.3f && CurrentSpeed > 5f) { StartDrift(); return; }
            float eMult = IsEntangled ? 0.6f : 1f;
            float maxSpd = stats.maxSpeed * eMult + (boostTimer > 0 ? boostForce : 0);

            // Always allow acceleration (even in air for recovery)
            if (input.Accelerate > 0.1f && CurrentSpeed < maxSpd)
            {
                float accelMult = IsGrounded ? 1f : 0.5f;
                rb.AddForce(transform.forward * stats.acceleration * input.Accelerate * accelMult, ForceMode.Acceleration);
            }
            else if (CurrentSpeed > 0.5f && IsGrounded)
            {
                rb.AddForce(-transform.forward * stats.coastDeceleration, ForceMode.Acceleration);
            }

            if (input.Brake > 0.1f)
            {
                if (CurrentSpeed > 0) rb.AddForce(-transform.forward * stats.brakeForce * input.Brake, ForceMode.Acceleration);
                else if (CurrentSpeed > -stats.maxReverseSpeed) rb.AddForce(-transform.forward * stats.acceleration * 0.5f * input.Brake, ForceMode.Acceleration);
            }

            // Allow steering both grounded AND airborne (reduced control in air)
            if (Mathf.Abs(CurrentSpeed) > 0.3f)
            {
                float steerMultiplier = IsGrounded ? 1f : 0.4f;
                float s = currentSteerInput * stats.turnSpeed * steerMultiplier * Time.fixedDeltaTime * (CurrentSpeed < 0 ? -1 : 1);
                transform.Rotate(0, s, 0, Space.Self);
            }

            ApplyLateralFriction(IsGrounded ? stats.lateralGrip : stats.lateralGrip * 0.15f);
            ClampSpeed(maxSpd);
        }

        void StartDrift()
        {
            CurrentState = KartState.Drifting;
            DriftDirection = currentSteerInput > 0 ? 1 : -1;
            DriftTime = 0; DriftStage = 0;
            OnDriftStart?.Invoke();
        }

        void UpdateDrifting()
        {
            if (!input.Drift || !IsGrounded) { EndDrift(); return; }
            DriftTime += Time.fixedDeltaTime;
            int ns = 0;
            for (int i = stats.miniTurboThresholds.Length - 1; i >= 0; i--) if (DriftTime >= stats.miniTurboThresholds[i]) { ns = i + 1; break; }
            if (ns != DriftStage) { DriftStage = ns; OnDriftStageChange?.Invoke(DriftStage); }
            float eMult = IsEntangled ? 0.6f : 1f;
            float maxSpd = stats.maxSpeed * eMult + (boostTimer > 0 ? boostForce : 0);
            if (input.Accelerate > 0.1f && CurrentSpeed < maxSpd) rb.AddForce(transform.forward * stats.acceleration * input.Accelerate * 0.8f, ForceMode.Acceleration);
            float totalSteer = (DriftDirection * stats.turnSpeed * stats.driftTurnMultiplier + input.Steer * stats.turnSpeed * 0.5f) * Time.fixedDeltaTime;
            transform.Rotate(0, totalSteer, 0, Space.Self);
            ApplyLateralFriction(stats.lateralGrip * stats.driftStiffness);
            ClampSpeed(maxSpd);
        }

        void EndDrift()
        {
            if (DriftStage > 0 && DriftStage <= stats.miniTurboForces.Length) ApplyBoost(stats.miniTurboForces[DriftStage - 1], stats.miniTurboDurations[DriftStage - 1]);
            CurrentState = KartState.Normal; DriftStage = 0; DriftTime = 0; DriftDirection = 0;
            OnDriftEnd?.Invoke();
        }

        void UpdateSpinOut()
        {
            spinOutTimer -= Time.fixedDeltaTime;
            transform.Rotate(0, 720 * Time.fixedDeltaTime, 0, Space.Self);
            rb.AddForce(-rb.linearVelocity * 3f, ForceMode.Acceleration);
            if (spinOutTimer <= 0) CurrentState = KartState.Normal;
        }

        void ApplyLateralFriction(float grip)
        {
            Vector3 lat = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
            rb.AddForce(-lat * grip, ForceMode.Acceleration);
        }

        void ClampSpeed(float maxSpeed)
        {
            float fs = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (fs > maxSpeed) { Vector3 lat = rb.linearVelocity - transform.forward * fs; rb.linearVelocity = transform.forward * maxSpeed + lat; }
        }

        void AlignToGround()
        {
            if (IsGrounded) { Quaternion t = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation; transform.rotation = Quaternion.Slerp(transform.rotation, t, stats.groundAlignSpeed * Time.fixedDeltaTime); }
        }

        public void ApplyBoost(float force, float duration)
        {
            boostForce = force; boostTimer = duration;
            if (CurrentState == KartState.Drifting) EndDrift();
            if (CurrentState == KartState.Normal) CurrentState = KartState.Boosting;
            rb.AddForce(transform.forward * force * 2f, ForceMode.VelocityChange);
            OnBoostStart?.Invoke(duration);
        }

        public void SpinOut(float duration = 1.5f)
        {
            if (CurrentState == KartState.SpinOut) return;
            CurrentState = KartState.SpinOut; spinOutTimer = duration;
            rb.linearVelocity *= 0.2f; OnSpinOut?.Invoke();
        }

        public void Entangle(float duration = 3f) { entangleTimer = duration; OnEntangle?.Invoke(); }
        public void HairballHit() { SpinOut(1f); Entangle(3f); }
        public void SetInput(IKartInput newInput) { input = newInput; }
    }
}
