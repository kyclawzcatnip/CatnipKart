using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;

namespace CatnipCart.AI
{
    /// <summary>
    /// AI input that follows the track spline. Implements IKartInput
    /// so it can drive any KartController exactly like player input.
    /// Simplified: focus on following the racing line, not obstacle avoidance.
    /// </summary>
    public class AIInput : MonoBehaviour, IKartInput
    {
        [Header("References")]
        public TrackSpline spline;
        public KartController kart;

        [Header("Difficulty")]
        [Range(8f, 40f)] public float lookAheadDistance = 18f;
        [Range(0.5f, 1f)] public float maxSpeedMultiplier = 0.9f;

        [Header("Rubber Banding")]
        public bool enableRubberBanding = true;
        public float rubberBandSpeedBoost = 0.1f;

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
        private float lastGoodDist; // Track last known good spline distance

        void Start()
        {
            cachedCS = FindAnyObjectByType<CheckpointSystem>();
            // Initialize our position on the spline
            if (spline != null)
            {
                currentDist = spline.GetNearestDistance(transform.position);
                lastGoodDist = currentDist;
            }
        }

        void Update()
        {
            if (spline == null || kart == null) return;

            // Find current position on spline using a smarter search
            // Instead of brute-force nearest, search near our last known position
            currentDist = GetSmartDistance();
            lastGoodDist = currentDist;

            // Look ahead — farther when going fast, shorter when slow
            float speedFactor = Mathf.Clamp01(kart.CurrentSpeed / 20f);
            float dynamicLookAhead = Mathf.Lerp(lookAheadDistance * 0.5f, lookAheadDistance * 1.5f, speedFactor);

            float targetDist = currentDist + dynamicLookAhead;
            Vector3 target = spline.GetPointAtDistance(targetDist);

            // Calculate steering toward the look-ahead point
            Vector3 toTarget = (target - transform.position);
            toTarget.y = 0;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = transform.forward;
            toTarget.Normalize();

            Vector3 fwd = transform.forward;
            fwd.y = 0;
            fwd.Normalize();

            float signedAngle = Vector3.SignedAngle(fwd, toTarget, Vector3.up);
            float targetSteer = Mathf.Clamp(signedAngle / 35f, -1f, 1f);

            // Smooth the steering — fast enough to actually turn but not jittery
            steerSmooth = Mathf.Lerp(steerSmooth, targetSteer, 12f * Time.deltaTime);
            Steer = steerSmooth;

            // === ACCELERATION ===
            float absAngle = Mathf.Abs(signedAngle);

            if (absAngle > 70f)
            {
                // Very sharp turn — slow down hard
                Accelerate = 0.2f;
                Brake = 0.6f;
            }
            else if (absAngle > 45f)
            {
                // Medium turn — ease off
                Accelerate = 0.5f;
                Brake = 0.1f;
            }
            else if (absAngle > 25f)
            {
                // Gentle turn — slight reduction
                Accelerate = 0.8f;
                Brake = 0f;
            }
            else
            {
                // Straight — full throttle!
                Accelerate = 1f;
                Brake = 0f;
            }

            // Drift on sharp turns when going fast
            Drift = absAngle > 40f && kart.CurrentSpeed > 8f && kart.IsGrounded;

            // Rubber banding — catch up if behind, slow down if way ahead
            if (enableRubberBanding && cachedCS != null)
            {
                var progress = cachedCS.GetProgress(transform);
                if (progress != null)
                {
                    if (progress.position >= 3)
                    {
                        // Behind — push harder
                        Accelerate = Mathf.Min(1f, Accelerate + rubberBandSpeedBoost);
                        Brake = 0f;
                    }
                    else if (progress.position == 1 && kart.CurrentSpeed > kart.stats.maxSpeed * 0.9f)
                    {
                        // Way ahead — ease off slightly
                        Accelerate *= 0.9f;
                    }
                }
            }

            // Not using items or looking back from AI for now
            UseItem = false;
            LookBack = false;
        }

        /// <summary>
        /// Smarter spline distance finding. Instead of brute-force searching the whole track,
        /// search near our last known position first. Falls back to global search if needed.
        /// </summary>
        float GetSmartDistance()
        {
            float totalLen = spline.TotalLength;
            if (totalLen < 1f) return 0f;

            // Search in a window around our last position
            float searchWindow = 30f; // meters around last pos
            float bestDist = float.MaxValue;
            float bestT = lastGoodDist;
            int samples = 30;

            for (int i = 0; i < samples; i++)
            {
                float t = lastGoodDist - searchWindow + (i / (float)samples) * searchWindow * 2f;
                t = Mathf.Repeat(t, totalLen); // Wrap around
                Vector3 p = spline.GetPointAtDistance(t);
                float sqDist = (p - transform.position).sqrMagnitude;
                if (sqDist < bestDist)
                {
                    bestDist = sqDist;
                    bestT = t;
                }
            }

            // If we're too far from the spline, do a full global search
            if (bestDist > 400f) // > 20m away
            {
                bestT = spline.GetNearestDistance(transform.position);
            }

            return bestT;
        }
    }
}
