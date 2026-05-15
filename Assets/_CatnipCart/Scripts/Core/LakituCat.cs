using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;

namespace CatnipCart.Core
{
    /// <summary>
    /// Lakitu-style cat on a hot air balloon. Floats above the track following the player.
    /// - Shows lap signs
    /// - Rescues karts that fall off the track
    /// - Holds a fishing rod with a dangling fish
    /// - Procedurally built from primitives
    /// </summary>
    public class LakituCat : MonoBehaviour
    {
        [Header("Follow")]
        public Transform target; // Player kart
        public float followHeight = 12f;
        public float followDistance = -5f; // Behind the player
        public float followSpeed = 4f;
        public float bobSpeed = 1.5f;
        public float bobAmount = 0.5f;

        [Header("Rescue")]
        public float rescueHeight = 8f;
        public float rescueDuration = 2f;

        private Transform balloonVisual;
        private TrackSpline spline;
        private bool isRescuing;
        private float rescueTimer;
        private Transform rescueTarget;
        private Vector3 rescueDropPos;
        private float bobTimer;

        void Start()
        {
            spline = FindAnyObjectByType<TrackSpline>();
            BuildBalloonCat();
        }

        void BuildBalloonCat()
        {
            balloonVisual = new GameObject("BalloonVisual").transform;
            balloonVisual.SetParent(transform, false);

            // Meowtu's colors — a special blue/cloud theme
            Color bodyCol = new Color(0.55f, 0.75f, 0.95f);      // Sky blue
            Color bodyDark = new Color(0.40f, 0.60f, 0.85f);     // Darker blue
            Color bellyCol = new Color(0.80f, 0.90f, 1.0f);      // Light blue highlight
            Color earInner = new Color(1f, 0.56f, 0.67f);        // Pink inner ear
            Color noseCol = new Color(1f, 0.42f, 0.54f);         // Pink nose

            Material bodyMat = MakeMat(bodyCol);
            Material bodyDarkMat = MakeMat(bodyDark);
            Material bellyMat = MakeMat(bellyCol);
            Material earInnerMat = MakeMat(earInner);
            Material eyeWhiteMat = MakeMat(Color.white);
            Material pupilMat = MakeMat(new Color(0.1f, 0.1f, 0.18f));
            Material noseMat = MakeMat(noseCol);
            Material whiskerMat = MakeMat(new Color(0.87f, 0.87f, 0.87f));
            Material hatMat = MakeMat(new Color(0.2f, 0.5f, 0.9f));     // Blue hat (cloud theme)
            Material bandMat = MakeMat(new Color(1f, 0.84f, 0f));       // Gold band
            Material gemMat = MakeMat(new Color(0f, 0.9f, 0.8f));       // Anticatite gem

            // === HOT AIR BALLOON ===
            // Balloon envelope
            var envelope = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            envelope.name = "BalloonEnvelope";
            envelope.transform.SetParent(balloonVisual, false);
            envelope.transform.localPosition = new Vector3(0, 3f, 0);
            envelope.transform.localScale = new Vector3(3f, 3.5f, 3f);
            envelope.GetComponent<Renderer>().material = MakeMat(new Color(1f, 0.4f, 0.2f));
            Destroy(envelope.GetComponent<Collider>());

            // Balloon stripes
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Box("BalloonStripe", MakeMat(new Color(1f, 0.85f, 0f)),
                    new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 1.4f, 3f, Mathf.Cos(angle * Mathf.Deg2Rad) * 1.4f),
                    new Vector3(0.15f, 3.2f, 0.15f));
            }

            // Ropes
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                float x = Mathf.Sin(angle * Mathf.Deg2Rad) * 0.6f;
                float z = Mathf.Cos(angle * Mathf.Deg2Rad) * 0.6f;
                var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rope.name = $"Rope_{i}";
                rope.transform.SetParent(balloonVisual, false);
                rope.transform.localPosition = new Vector3(x, 0.8f, z);
                rope.transform.localScale = new Vector3(0.03f, 0.8f, 0.03f);
                rope.GetComponent<Renderer>().material = MakeMat(new Color(0.55f, 0.35f, 0.15f));
                Destroy(rope.GetComponent<Collider>());
            }

            // Basket
            Box("Basket", MakeMat(new Color(0.55f, 0.35f, 0.15f)),
                new Vector3(0, -0.1f, 0), new Vector3(1.2f, 0.6f, 1.2f));

            // ============ MEOWTU CAT (SCW cube style!) ============
            float catY = 0.1f; // Sitting in basket

            // Body
            Box("MeowtuBody", bodyMat,
                new Vector3(0, catY + 0.35f, 0), new Vector3(0.40f, 0.35f, 0.28f));
            Box("MeowtuBelly", bellyMat,
                new Vector3(0, catY + 0.34f, 0.05f), new Vector3(0.30f, 0.25f, 0.20f));

            // Head
            Box("MeowtuHead", bodyMat,
                new Vector3(0, catY + 0.62f, 0.04f), new Vector3(0.38f, 0.30f, 0.28f));
            Box("MeowtuHeadHi", bellyMat,
                new Vector3(0, catY + 0.63f, 0.07f), new Vector3(0.28f, 0.22f, 0.20f));

            // Ears (outer + pink inner)
            for (int side = -1; side <= 1; side += 2)
            {
                float xOff = side * 0.12f;
                Box($"MeowtuEar_{side}", bodyMat,
                    new Vector3(xOff, catY + 0.82f, 0.04f), new Vector3(0.08f, 0.12f, 0.06f));
                Box($"MeowtuEarInner_{side}", earInnerMat,
                    new Vector3(xOff, catY + 0.83f, 0.055f), new Vector3(0.04f, 0.08f, 0.04f));
            }

            // Eyes (white + dark pupil)
            Box("MeowtuEyeL", eyeWhiteMat,
                new Vector3(-0.08f, catY + 0.66f, 0.19f), new Vector3(0.07f, 0.07f, 0.03f));
            Box("MeowtuPupilL", pupilMat,
                new Vector3(-0.06f, catY + 0.655f, 0.205f), new Vector3(0.045f, 0.055f, 0.02f));
            Box("MeowtuEyeR", eyeWhiteMat,
                new Vector3(0.08f, catY + 0.66f, 0.19f), new Vector3(0.07f, 0.07f, 0.03f));
            Box("MeowtuPupilR", pupilMat,
                new Vector3(0.10f, catY + 0.655f, 0.205f), new Vector3(0.045f, 0.055f, 0.02f));

            // Nose
            Box("MeowtuNose", noseMat,
                new Vector3(0, catY + 0.59f, 0.19f), new Vector3(0.04f, 0.03f, 0.03f));

            // Whiskers
            Box("MeowtuWhiskerL1", whiskerMat,
                new Vector3(-0.22f, catY + 0.63f, 0.19f), new Vector3(0.16f, 0.012f, 0.01f));
            Box("MeowtuWhiskerL2", whiskerMat,
                new Vector3(-0.22f, catY + 0.59f, 0.19f), new Vector3(0.18f, 0.012f, 0.01f));
            Box("MeowtuWhiskerR1", whiskerMat,
                new Vector3(0.22f, catY + 0.63f, 0.19f), new Vector3(0.16f, 0.012f, 0.01f));
            Box("MeowtuWhiskerR2", whiskerMat,
                new Vector3(0.22f, catY + 0.59f, 0.19f), new Vector3(0.18f, 0.012f, 0.01f));

            // Hat (blue cloud theme)
            Box("MeowtuBrim", hatMat,
                new Vector3(0, catY + 0.80f, 0.02f), new Vector3(0.42f, 0.05f, 0.30f));
            Box("MeowtuCrown", hatMat,
                new Vector3(0, catY + 0.89f, 0.02f), new Vector3(0.26f, 0.14f, 0.24f));
            Box("MeowtuBand", bandMat,
                new Vector3(0, catY + 0.82f, 0.02f), new Vector3(0.27f, 0.04f, 0.25f));
            Box("MeowtuGem", gemMat,
                new Vector3(0, catY + 0.87f, 0.17f), new Vector3(0.08f, 0.08f, 0.03f));

            // Arms (little blocky arms holding over basket edge)
            Box("MeowtuArmL", bodyDarkMat,
                new Vector3(-0.22f, catY + 0.25f, 0.12f), new Vector3(0.08f, 0.12f, 0.08f));
            Box("MeowtuArmR", bodyDarkMat,
                new Vector3(0.22f, catY + 0.25f, 0.12f), new Vector3(0.08f, 0.12f, 0.08f));

            // Paws (white)
            Box("MeowtuPawL", eyeWhiteMat,
                new Vector3(-0.22f, catY + 0.20f, 0.14f), new Vector3(0.09f, 0.04f, 0.10f));
            Box("MeowtuPawR", eyeWhiteMat,
                new Vector3(0.22f, catY + 0.20f, 0.14f), new Vector3(0.09f, 0.04f, 0.10f));

            // === FISHING ROD ===
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "FishingRod";
            rod.transform.SetParent(balloonVisual, false);
            rod.transform.localPosition = new Vector3(0.35f, catY + 0.5f, 0.4f);
            rod.transform.localScale = new Vector3(0.04f, 1.0f, 0.04f);
            rod.transform.localRotation = Quaternion.Euler(-50, 0, 15);
            rod.GetComponent<Renderer>().material = MakeMat(new Color(0.4f, 0.25f, 0.1f));
            Destroy(rod.GetComponent<Collider>());

            // Fishing line
            var line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            line.name = "FishingLine";
            line.transform.SetParent(balloonVisual, false);
            line.transform.localPosition = new Vector3(0.7f, -0.5f, 1.0f);
            line.transform.localScale = new Vector3(0.01f, 0.8f, 0.01f);
            line.GetComponent<Renderer>().material = MakeMat(Color.white);
            Destroy(line.GetComponent<Collider>());

            // Dangling fish (cube style to match)
            Box("DanglingFish", MakeMat(new Color(0.3f, 0.6f, 0.9f)),
                new Vector3(0.7f, -1.3f, 1.0f), new Vector3(0.20f, 0.12f, 0.35f));
            Box("FishTail", MakeMat(new Color(0.2f, 0.5f, 0.85f)),
                new Vector3(0.7f, -1.3f, 0.75f), new Vector3(0.05f, 0.20f, 0.15f));

            // Remove all colliders
            foreach (var col in GetComponentsInChildren<Collider>())
                Destroy(col);
        }

        /// <summary>Helper: creates a cube in the balloon visual (SCW style)</summary>
        GameObject Box(string name, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(balloonVisual, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = mat;
            Destroy(go.GetComponent<Collider>());
            return go;
        }

        void Update()
        {
            if (target == null) return;

            bobTimer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(bobTimer) * bobAmount;

            if (isRescuing)
            {
                UpdateRescue();
            }
            else
            {
                // Follow the player from above and behind
                Vector3 desiredPos = target.position
                    + Vector3.up * (followHeight + bob)
                    + target.forward * followDistance;

                transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

                // Look at the player
                Vector3 lookDir = target.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.1f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(lookDir), 3f * Time.deltaTime);
            }

            // Gentle balloon sway
            balloonVisual.localRotation = Quaternion.Euler(
                Mathf.Sin(bobTimer * 0.7f) * 3f,
                0,
                Mathf.Sin(bobTimer * 1.1f) * 2f);
        }

        /// <summary>
        /// Called when a kart falls off the track and needs rescue.
        /// Picks them up and drops them back on the track.
        /// </summary>
        public void RescueKart(KartController kart)
        {
            if (isRescuing) return;

            isRescuing = true;
            rescueTimer = rescueDuration;
            rescueTarget = kart.transform;

            // Find where to drop them back on the track
            if (spline != null)
            {
                float dist = spline.GetNearestDistance(kart.transform.position);
                // Drop them a bit ahead of where they fell
                dist += 10f;
                rescueDropPos = spline.GetPointAtDistance(dist) + Vector3.up * 2f;
            }
            else
            {
                rescueDropPos = kart.transform.position + Vector3.up * 2f;
            }

            // Freeze the kart during rescue
            var rb = kart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        void UpdateRescue()
        {
            rescueTimer -= Time.deltaTime;

            if (rescueTarget == null) { isRescuing = false; return; }

            if (rescueTimer > rescueDuration * 0.5f)
            {
                // Phase 1: Fly to the kart and lift it up
                Vector3 liftPos = rescueTarget.position + Vector3.up * rescueHeight;
                transform.position = Vector3.Lerp(transform.position, liftPos, 6f * Time.deltaTime);
                rescueTarget.position = Vector3.Lerp(rescueTarget.position,
                    transform.position + Vector3.down * 4f, 8f * Time.deltaTime);
            }
            else if (rescueTimer > 0)
            {
                // Phase 2: Carry to the drop position
                Vector3 carryPos = rescueDropPos + Vector3.up * rescueHeight;
                transform.position = Vector3.Lerp(transform.position, carryPos, 4f * Time.deltaTime);
                rescueTarget.position = Vector3.Lerp(rescueTarget.position,
                    transform.position + Vector3.down * 4f, 8f * Time.deltaTime);
            }
            else
            {
                // Phase 3: Drop!
                rescueTarget.position = rescueDropPos;

                // Orient kart to face the track direction
                if (spline != null)
                {
                    float dist = spline.GetNearestDistance(rescueDropPos);
                    Vector3 dir = spline.GetDirectionAtDistance(dist);
                    rescueTarget.rotation = Quaternion.LookRotation(dir);
                }

                // Unfreeze
                var rb = rescueTarget.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;

                isRescuing = false;
                rescueTarget = null;
            }
        }

        Material MakeMat(Color c)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            return mat;
        }
    }
}
