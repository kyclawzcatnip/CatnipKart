using UnityEngine;
using System.Collections.Generic;

namespace CatnipCart.Track
{
    /// <summary>
    /// Checkpoint system for tracking racer progress, lap counting, and positions.
    /// Creates visible checkpoint gates and a start/finish line.
    /// </summary>
    public class CheckpointSystem : MonoBehaviour
    {
        [Header("Setup")]
        public TrackSpline spline;
        public int checkpointCount = 16;
        public int totalLaps = 3;
        public float checkpointWidth = 15f;
        public float checkpointHeight = 5f;

        // Racer tracking
        public class RacerProgress
        {
            public Transform racer;
            public int currentCheckpoint;
            public int currentLap;
            public float distanceAlongTrack;
            public bool finished;
            public float finishTime;
            public int position; // 1st, 2nd, etc.
            public bool hasStarted; // True once they've hit checkpoint 1+

            public float TotalProgress => (currentLap * 1000f) + currentCheckpoint + (distanceAlongTrack / 1000f);
        }

        public List<RacerProgress> racers = new List<RacerProgress>();
        private List<Vector3> checkpointPositions = new List<Vector3>();
        private List<Vector3> checkpointForwards = new List<Vector3>();

        public System.Action<RacerProgress> OnLapComplete;
        public System.Action<RacerProgress> OnRaceFinish;

        void Start()
        {
            GenerateCheckpoints();
        }

        void GenerateCheckpoints()
        {
            checkpointPositions.Clear();
            checkpointForwards.Clear();

            // Materials for visuals
            Material gateMat = MakeMat(new Color(1f, 1f, 1f, 0.4f), true);
            Material startLineMat = MakeMat(new Color(1f, 0.85f, 0f, 0.6f), true);
            Material poleMat = MakeMat(new Color(0.85f, 0.85f, 0.9f));
            Material bannerMat = MakeMat(new Color(0.2f, 0.6f, 1f, 0.5f), true);

            for (int i = 0; i < checkpointCount; i++)
            {
                float dist = (i / (float)checkpointCount) * spline.TotalLength;
                Vector3 pos = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                checkpointPositions.Add(pos);
                checkpointForwards.Add(fwd);

                // --- Create checkpoint GameObject ---
                var cpGO = new GameObject($"Checkpoint_{i}");
                cpGO.transform.SetParent(transform, false);
                cpGO.transform.position = pos + Vector3.up * checkpointHeight * 0.5f;
                cpGO.transform.rotation = Quaternion.LookRotation(fwd);
                cpGO.layer = 2; // Ignore Raycast (so ground detection raycasts skip it)

                // Trigger collider — make it THICK so fast karts can't skip through
                var box = cpGO.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(checkpointWidth, checkpointHeight, 8f);

                // Kinematic Rigidbody ensures OnTriggerEnter fires reliably
                var cpRb = cpGO.AddComponent<Rigidbody>();
                cpRb.isKinematic = true;
                cpRb.useGravity = false;

                var handler = cpGO.AddComponent<CheckpointTrigger>();
                handler.system = this;
                handler.checkpointIndex = i;

                // --- Visuals ---
                bool isStartFinish = (i == 0);

                if (isStartFinish)
                {
                    // === START/FINISH LINE - Full gate with banner ===
                    float halfW = checkpointWidth * 0.5f;

                    // Left pole
                    var leftPole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leftPole.name = "LeftPole";
                    leftPole.transform.SetParent(cpGO.transform, false);
                    leftPole.transform.localPosition = new Vector3(-halfW, 0, 0);
                    leftPole.transform.localScale = new Vector3(0.4f, checkpointHeight, 0.4f);
                    leftPole.GetComponent<Renderer>().material = poleMat;
                    Destroy(leftPole.GetComponent<Collider>());

                    // Right pole
                    var rightPole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rightPole.name = "RightPole";
                    rightPole.transform.SetParent(cpGO.transform, false);
                    rightPole.transform.localPosition = new Vector3(halfW, 0, 0);
                    rightPole.transform.localScale = new Vector3(0.4f, checkpointHeight, 0.4f);
                    rightPole.GetComponent<Renderer>().material = poleMat;
                    Destroy(rightPole.GetComponent<Collider>());

                    // Top banner bar
                    var topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    topBar.name = "TopBar";
                    topBar.transform.SetParent(cpGO.transform, false);
                    topBar.transform.localPosition = new Vector3(0, checkpointHeight * 0.5f - 0.2f, 0);
                    topBar.transform.localScale = new Vector3(checkpointWidth, 0.5f, 0.5f);
                    topBar.GetComponent<Renderer>().material = poleMat;
                    Destroy(topBar.GetComponent<Collider>());

                    // Checkered banner (alternating black/white cubes)
                    float bannerY = checkpointHeight * 0.5f - 0.7f;
                    int segments = 10;
                    float segWidth = checkpointWidth / segments;
                    for (int s = 0; s < segments; s++)
                    {
                        var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        seg.name = $"CheckerSeg_{s}";
                        seg.transform.SetParent(cpGO.transform, false);
                        float x = -halfW + segWidth * 0.5f + s * segWidth;
                        seg.transform.localPosition = new Vector3(x, bannerY, 0);
                        seg.transform.localScale = new Vector3(segWidth * 0.95f, 0.8f, 0.3f);

                        bool isWhite = (s % 2 == 0);
                        seg.GetComponent<Renderer>().material = MakeMat(isWhite ? Color.white : Color.black);
                        Destroy(seg.GetComponent<Collider>());
                    }

                    // Start line on the ground (white/red stripe)
                    var groundLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    groundLine.name = "StartLine";
                    groundLine.transform.SetParent(cpGO.transform, false);
                    groundLine.transform.localPosition = new Vector3(0, -checkpointHeight * 0.5f + 0.06f, 0);
                    groundLine.transform.localScale = new Vector3(checkpointWidth, 0.1f, 1.5f);
                    groundLine.GetComponent<Renderer>().material = startLineMat;
                    Destroy(groundLine.GetComponent<Collider>());
                }
                else if (i % 4 == 0)
                {
                    // === MAJOR CHECKPOINT - Small gate posts ===
                    float halfW = checkpointWidth * 0.35f;

                    var leftPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leftPost.name = "LeftPost";
                    leftPost.transform.SetParent(cpGO.transform, false);
                    leftPost.transform.localPosition = new Vector3(-halfW, -checkpointHeight * 0.25f, 0);
                    leftPost.transform.localScale = new Vector3(0.3f, checkpointHeight * 0.5f, 0.3f);
                    leftPost.GetComponent<Renderer>().material = MakeMat(new Color(0.2f, 0.6f, 1f));
                    Destroy(leftPost.GetComponent<Collider>());

                    var rightPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rightPost.name = "RightPost";
                    rightPost.transform.SetParent(cpGO.transform, false);
                    rightPost.transform.localPosition = new Vector3(halfW, -checkpointHeight * 0.25f, 0);
                    rightPost.transform.localScale = new Vector3(0.3f, checkpointHeight * 0.5f, 0.3f);
                    rightPost.GetComponent<Renderer>().material = MakeMat(new Color(0.2f, 0.6f, 1f));
                    Destroy(rightPost.GetComponent<Collider>());

                    // Ground stripe
                    var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "GroundStripe";
                    stripe.transform.SetParent(cpGO.transform, false);
                    stripe.transform.localPosition = new Vector3(0, -checkpointHeight * 0.5f + 0.06f, 0);
                    stripe.transform.localScale = new Vector3(checkpointWidth * 0.7f, 0.1f, 0.8f);
                    stripe.GetComponent<Renderer>().material = MakeMat(new Color(0.3f, 0.7f, 1f, 0.5f), true);
                    Destroy(stripe.GetComponent<Collider>());
                }
                // Minor checkpoints: invisible (trigger only, no visual clutter)
            }
        }

        public void RegisterRacer(Transform racer)
        {
            racers.Add(new RacerProgress
            {
                racer = racer,
                currentCheckpoint = 0,
                currentLap = 0,
                distanceAlongTrack = 0,
                finished = false,
                hasStarted = false
            });
        }

        public void OnCheckpointHit(Transform racer, int cpIndex)
        {
            var progress = racers.Find(r => r.racer == racer);
            if (progress == null || progress.finished) return;

            int expected = (progress.currentCheckpoint + 1) % checkpointCount;

            // Only count if hitting the next expected checkpoint (anti-shortcut)
            if (cpIndex == expected)
            {
                progress.currentCheckpoint = cpIndex;

                // Mark that they've started racing (hit at least one non-zero checkpoint)
                if (cpIndex != 0)
                    progress.hasStarted = true;

                // Crossed start line = new lap (but only if they've been around the track)
                if (cpIndex == 0 && progress.hasStarted)
                {
                    progress.currentLap++;
                    OnLapComplete?.Invoke(progress);

                    if (progress.currentLap >= totalLaps)
                    {
                        progress.finished = true;
                        progress.finishTime = Time.time;
                        OnRaceFinish?.Invoke(progress);
                    }
                }
            }
        }

        void Update()
        {
            // Update distance along track for each racer + fallback checkpoint detection
            foreach (var r in racers)
            {
                if (r.racer == null || r.finished) continue;

                r.distanceAlongTrack = spline.GetNearestDistance(r.racer.position);

                // === FALLBACK: Distance-based checkpoint detection ===
                // If triggers fail, check if racer is close enough to the next expected checkpoint
                int expected = (r.currentCheckpoint + 1) % checkpointCount;
                if (expected < checkpointPositions.Count)
                {
                    float distToCP = Vector3.Distance(r.racer.position, checkpointPositions[expected]);
                    if (distToCP < 6f) // Within 6 units of the checkpoint
                    {
                        // Verify they're facing roughly the right direction (anti-cheat)
                        Vector3 cpFwd = checkpointForwards[expected];
                        float dot = Vector3.Dot(r.racer.forward, cpFwd);
                        if (dot > 0.2f) // Facing within ~78 degrees of checkpoint direction
                        {
                            OnCheckpointHit(r.racer, expected);
                        }
                    }
                }
            }

            // Calculate positions
            racers.Sort((a, b) => b.TotalProgress.CompareTo(a.TotalProgress));
            for (int i = 0; i < racers.Count; i++)
                racers[i].position = i + 1;
        }

        public RacerProgress GetProgress(Transform racer)
        {
            return racers.Find(r => r.racer == racer);
        }

        Material MakeMat(Color c, bool transparent = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = c;

            if (transparent && c.a < 1f)
            {
                // Enable transparency
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_AlphaClip", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }
    }

    /// <summary>Trigger handler for individual checkpoints.</summary>
    public class CheckpointTrigger : MonoBehaviour
    {
        [HideInInspector] public CheckpointSystem system;
        [HideInInspector] public int checkpointIndex;

        void OnTriggerEnter(Collider other)
        {
            TryRegisterHit(other);
        }

        // Backup: OnTriggerStay catches karts that started inside the trigger
        void OnTriggerStay(Collider other)
        {
            TryRegisterHit(other);
        }

        void TryRegisterHit(Collider other)
        {
            var kart = other.GetComponentInParent<Kart.KartController>();
            if (kart != null)
            {
                system.OnCheckpointHit(kart.transform, checkpointIndex);
            }
        }
    }
}
