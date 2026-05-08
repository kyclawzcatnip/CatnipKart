using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;

namespace CatnipCart.AI
{
    /// <summary>
    /// AI input that follows the track spline with robust obstacle avoidance.
    /// Uses OverlapSphere to find nearby obstacles and steers away from them.
    /// </summary>
    public class AIInput : MonoBehaviour, IKartInput
    {
        [Header("References")]
        public TrackSpline spline;
        public KartController kart;

        [Header("Difficulty")]
        [Range(5f, 30f)] public float lookAheadDistance = 15f;
        [Range(0.5f, 1f)] public float maxSpeedMultiplier = 0.9f;

        [Header("Rubber Banding")]
        public bool enableRubberBanding = true;
        public float rubberBandSpeedBoost = 0.15f;

        // IKartInput implementation
        public float Accelerate { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public bool Drift { get; private set; }
        public bool UseItem { get; private set; }
        public bool LookBack { get; private set; }

        private float currentDist;
        private float steerSmooth;
        private CheckpointSystem cachedCS;
        private Collider[] overlapResults = new Collider[20];
        private Collider myCollider;

        void Start()
        {
            cachedCS = FindAnyObjectByType<CheckpointSystem>();
            myCollider = GetComponent<Collider>();
        }

        void Update()
        {
            if (spline == null || kart == null) return;

            // Find current position on spline
            currentDist = spline.GetNearestDistance(transform.position);

            // Look ahead target on the spline
            float targetDist = currentDist + lookAheadDistance;
            Vector3 target = spline.GetPointAtDistance(targetDist);

            // === SPLINE FOLLOWING ===
            Vector3 toTarget = (target - transform.position);
            toTarget.y = 0;
            if (toTarget.sqrMagnitude > 0.01f) toTarget.Normalize();
            Vector3 fwd = transform.forward;
            fwd.y = 0;
            fwd.Normalize();

            float signedAngle = Vector3.SignedAngle(fwd, toTarget, Vector3.up);
            float splineSteer = Mathf.Clamp(signedAngle / 40f, -1f, 1f);

            // === OBSTACLE AVOIDANCE ===
            float avoidSteer = GetAvoidanceSteering(out float urgency, out bool shouldBrake);

            // Combine: avoidance ADDS to spline steering when urgent
            float finalSteer;
            if (urgency > 0.1f)
            {
                // Mix: urgency controls how much avoidance overrides spline following
                finalSteer = splineSteer + avoidSteer * urgency * 2f;
            }
            else
            {
                finalSteer = splineSteer;
            }
            finalSteer = Mathf.Clamp(finalSteer, -1f, 1f);

            // Fast response when avoiding, smooth otherwise
            float smoothSpeed = urgency > 0.3f ? 20f : 8f;
            steerSmooth = Mathf.Lerp(steerSmooth, finalSteer, smoothSpeed * Time.deltaTime);
            Steer = steerSmooth;

            // === ACCELERATION / BRAKING ===
            float absAngle = Mathf.Abs(signedAngle);

            if (shouldBrake)
            {
                Accelerate = 0.1f;
                Brake = 0.9f;
            }
            else if (urgency > 0.6f)
            {
                Accelerate = 0.3f;
                Brake = 0.4f;
            }
            else if (absAngle > 60f)
            {
                Accelerate = 0.3f;
                Brake = 0.5f;
            }
            else if (absAngle > 35f)
            {
                Accelerate = 0.6f;
                Brake = 0f;
            }
            else
            {
                Accelerate = 1f;
                Brake = 0f;
            }

            // Drift on sharp turns
            Drift = absAngle > 40f && kart.CurrentSpeed > 10f && kart.IsGrounded;

            // Rubber banding
            if (enableRubberBanding && cachedCS != null)
            {
                var progress = cachedCS.GetProgress(transform);
                if (progress != null)
                {
                    if (progress.position >= 3)
                        Accelerate = Mathf.Min(1f, Accelerate + rubberBandSpeedBoost);
                    if (progress.position == 1 && kart.CurrentSpeed > kart.stats.maxSpeed * 0.85f)
                        Accelerate *= 0.85f;
                }
            }

            UseItem = false;
            LookBack = false;
        }

        /// <summary>
        /// Uses OverlapSphere to find nearby obstacles and computes avoidance steering.
        /// Much more reliable than raycasting for spherical/irregular obstacles.
        /// </summary>
        float GetAvoidanceSteering(out float urgency, out bool shouldBrake)
        {
            urgency = 0f;
            shouldBrake = false;

            // Scan area ahead of the kart
            Vector3 scanCenter = transform.position + transform.forward * 6f + Vector3.up * 1f;
            float scanRadius = 10f;

            int hitCount = Physics.OverlapSphereNonAlloc(scanCenter, scanRadius, overlapResults);

            float totalAvoidX = 0f;
            float maxUrgency = 0f;
            int obstacleCount = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = overlapResults[i];
                if (col == null) continue;
                if (col == myCollider) continue;        // Skip self
                if (col.isTrigger) continue;            // Skip triggers (checkpoints, items)

                // Identify what we hit
                string objName = col.gameObject.name;

                // Skip track surfaces
                if (objName == "Road" || objName == "TempFloor") continue;
                if (objName.StartsWith("Grass")) continue;
                if (objName.StartsWith("Curb")) continue;

                // Skip trees (far off track, don't need to avoid)
                if (objName == "Trunk" || objName == "Leaves") continue;

                // Everything else is an obstacle: barriers, yarn balls, other karts, hairball traps
                Vector3 obstaclePos = col.ClosestPoint(transform.position);
                Vector3 toObstacle = obstaclePos - transform.position;
                float dist = toObstacle.magnitude;

                // Only care about obstacles ahead of us (not behind)
                float dotFwd = Vector3.Dot(toObstacle.normalized, transform.forward);
                if (dotFwd < -0.2f) continue; // Behind us, ignore

                // How urgent is this? (closer = more urgent)
                float maxDist = 12f;
                float thisUrgency = Mathf.Clamp01(1f - dist / maxDist);

                // Obstacles directly ahead are more urgent
                thisUrgency *= Mathf.Clamp01(dotFwd + 0.5f);

                if (thisUrgency > maxUrgency) maxUrgency = thisUrgency;

                // Which side is the obstacle on?
                float dotRight = Vector3.Dot(toObstacle.normalized, transform.right);

                // Steer AWAY from the obstacle
                // Obstacle on right (dotRight > 0) → steer left (negative)
                // Obstacle on left (dotRight < 0) → steer right (positive)
                float avoidDir;
                if (Mathf.Abs(dotRight) < 0.15f)
                {
                    // Dead ahead — pick the side with more room using the spline
                    float splineDist = spline.GetNearestDistance(transform.position);
                    Vector3 splineCenter = spline.GetPointAtDistance(splineDist);
                    Vector3 toCenter = splineCenter - transform.position;
                    float centerDot = Vector3.Dot(toCenter.normalized, transform.right);
                    // Steer toward track center to get around the obstacle
                    avoidDir = centerDot > 0 ? 1f : -1f;
                }
                else
                {
                    avoidDir = dotRight > 0 ? -1f : 1f;
                }

                totalAvoidX += avoidDir * thisUrgency;
                obstacleCount++;
            }

            urgency = maxUrgency;

            if (obstacleCount > 0)
            {
                float avgAvoid = totalAvoidX / obstacleCount;

                // Brake if obstacle is very close and dead ahead
                shouldBrake = maxUrgency > 0.85f;

                return Mathf.Clamp(avgAvoid * 1.5f, -1f, 1f);
            }

            return 0f;
        }
    }
}
