using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;
using CatnipCart.Items;
using CatnipCart.AI;
using CatnipCart.UI;

namespace CatnipCart.Core
{
    /// <summary>
    /// Master scene initializer. Creates the entire race scene procedurally:
    /// track, karts, cats, camera, UI, lighting, skybox.
    /// Attach to an empty GameObject in the scene and press Play.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Race Config")]
        public int totalLaps = 3;
        [Range(1, 10)] public int aiRacers = 5;

        void Awake()
        {
            BuildScene();
        }

        void BuildScene()
        {
            // === LIGHTING ===
            SetupLighting();

            // === TRACK ===
            var trackGO = new GameObject("Track");
            var spline = trackGO.AddComponent<TrackSpline>();
            spline.waypoints = CreateCatnipGardensLayout();
            spline.isClosed = true;
            spline.CalculateLengths();

            var trackGen = trackGO.AddComponent<TrackGenerator>();
            trackGen.roadWidth = 14f;
            trackGen.resolution = 200;

            var checkpoints = trackGO.AddComponent<CheckpointSystem>();
            checkpoints.spline = spline;
            checkpoints.totalLaps = totalLaps;
            checkpoints.checkpointCount = 20;

            // === PLAYER KART ===
            var playerKart = CreateKart("PlayerKart", CatColorData.CreateGinger(), true,
                spline, GetStartPosition(spline, 0));

            // === AI KARTS (9 unique cats!) ===
            var aiColors = CatColorData.GetAllAIColors();
            int aiCount = Mathf.Clamp(aiRacers, 1, aiColors.Length);
            for (int i = 0; i < aiCount; i++)
            {
                var aiKart = CreateKart($"AI_{aiColors[i].catName}", aiColors[i], false,
                    spline, GetStartPosition(spline, i + 1));
            }

            // === CAMERA ===
            var camGO = new GameObject("RaceCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.75f, 1f);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 500f;

            var camCtrl = camGO.AddComponent<CameraController>();
            camCtrl.target = playerKart.transform;
            camCtrl.kart = playerKart.GetComponent<KartController>();

            // Remove default camera
            var defaultCam = Camera.main;
            if (defaultCam != null && defaultCam != cam)
                Destroy(defaultCam.gameObject);

            // === RACE MANAGER ===
            var rmGO = new GameObject("RaceManager");
            var rm = rmGO.AddComponent<RaceManager>();
            rm.spline = spline;
            rm.checkpointSystem = checkpoints;
            rm.totalLaps = totalLaps;

            // === UI ===
            var uiGO = new GameObject("RaceUI");
            var ui = uiGO.AddComponent<RaceUI>();
            ui.raceManager = rm;
            ui.checkpointSystem = checkpoints;
            ui.playerKart = playerKart.GetComponent<KartController>();

            // === ITEM BOXES ===
            PlaceItemBoxes(spline);

            // === BOOST PADS ===
            PlaceBoostPads(spline);

            // === JUMP RAMPS ===
            PlaceJumpRamps(spline);

            // === DECORATION ===
            PlaceDecorations(spline);

            // === UNDERGROUND TUNNEL ===
            BuildTunnel(spline);

            // === LAKITU CAT (balloon rescue cat!) ===
            var lakituGO = new GameObject("LakituCat");
            lakituGO.transform.position = playerKart.transform.position + Vector3.up * 12f;
            var lakitu = lakituGO.AddComponent<LakituCat>();
            lakitu.target = playerKart.transform;

            // === RESTART HANDLER ===
            gameObject.AddComponent<RestartHandler>();
        }

        GameObject CreateKart(string name, CatColorData colors, bool isPlayer,
            TrackSpline spline, Vector3 position)
        {
            var kartGO = new GameObject(name);
            kartGO.transform.position = position;
            kartGO.transform.rotation = Quaternion.LookRotation(spline.GetDirectionAtDistance(0));

            // Rigidbody
            var rb = kartGO.AddComponent<Rigidbody>();
            rb.mass = 1000f;

            // Box collider for kart body
            var col = kartGO.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 0.8f, 1.8f);
            col.center = new Vector3(0, 0.4f, 0);

            // Ground raycast origin
            var rayOrigin = new GameObject("GroundRay").transform;
            rayOrigin.SetParent(kartGO.transform, false);
            rayOrigin.localPosition = new Vector3(0, 0.5f, 0);

            // Kart stats
            var stats = ScriptableObject.CreateInstance<KartStats>();
            if (isPlayer)
            {
                // Player gets a slight edge to offset human input imperfection
                stats.maxSpeed += 3f;    // 28 vs AI's 22-25
                stats.acceleration += 5f; // 45 vs AI's 33-40
            }
            else
            {
                // AI is slightly slower — they have perfect inputs so need a handicap
                stats.maxSpeed += Random.Range(-3f, 0f);     // 22-25
                stats.acceleration += Random.Range(-7f, 0f); // 33-40
            }

            // Kart controller
            var kc = kartGO.AddComponent<KartController>();
            kc.stats = stats;
            kc.groundRayOrigin = rayOrigin;

            // Input (player or AI)
            if (isPlayer)
            {
                kartGO.AddComponent<KartInput>();
            }
            else
            {
                var ai = kartGO.AddComponent<AIInput>();
                ai.spline = spline;
                ai.kart = kc;
                ai.lookAheadDistance = Random.Range(12f, 20f);
                ai.maxSpeedMultiplier = Random.Range(0.8f, 0.95f);
                kartGO.AddComponent<AIItemUser>();
            }

            // Item holder
            kartGO.AddComponent<ItemHolder>();

            // Visuals — kart body
            var kartVisualGO = new GameObject("KartModel");
            kartVisualGO.transform.SetParent(kartGO.transform, false);
            var kartBuilder = kartVisualGO.AddComponent<KartBuilder>();
            kartBuilder.primaryColor = colors.kartPrimary;
            kartBuilder.secondaryColor = colors.kartSecondary;
            kartBuilder.accentColor = colors.kartAccent;

            // Visuals — cat driver
            var catGO = new GameObject("CatDriver");
            catGO.transform.SetParent(kartGO.transform, false);
            catGO.transform.localPosition = new Vector3(0, 0.25f, -0.05f);
            catGO.transform.localScale = Vector3.one * 0.8f;
            var catBuilder = catGO.AddComponent<CatBuilder>();
            catBuilder.colorData = colors;
            catBuilder.wearHat = isPlayer; // Only player gets the hat
            catBuilder.kart = kc;

            // Kart visuals effects
            var kv = kartGO.AddComponent<KartVisuals>();
            kv.kart = kc;
            kv.kartBody = kartVisualGO.transform;

            return kartGO;
        }

        Vector3 GetStartPosition(TrackSpline spline, int index)
        {
            // Stagger karts at the start line
            float dist = index * 4f; // 4m apart
            Vector3 pos = spline.GetPointAtDistance(spline.TotalLength - dist);
            Vector3 right = Vector3.Cross(Vector3.up, spline.GetDirectionAtDistance(spline.TotalLength - dist));
            float lateralOff = (index % 2 == 0 ? -1 : 1) * 2.5f;
            pos += right * lateralOff;
            pos.y += 1f; // Slight lift so they settle onto the road
            return pos;
        }

        System.Collections.Generic.List<Vector3> CreateCatnipGardensLayout()
        {
            // Expanded "Catnip Gardens Grand Prix" circuit — big and exciting!
            return new System.Collections.Generic.List<Vector3>
            {
                // === START / FINISH STRAIGHT ===
                new Vector3(0, 0, 0),
                new Vector3(60, 0, 5),
                new Vector3(120, 0, 0),         // Long opening straight

                // === SWEEPING RIGHT INTO THE VALLEY ===
                new Vector3(170, 0, 20),
                new Vector3(200, 0, 60),
                new Vector3(210, 0, 110),        // Right curve

                // === HAIRPIN LEFT ===
                new Vector3(190, 0, 160),
                new Vector3(150, 0, 190),        // Tight hairpin

                // === BACKSTRETCH WITH S-CURVES ===
                new Vector3(100, 0, 200),
                new Vector3(60, 0, 180),         // S-curve part 1
                new Vector3(30, 0, 210),         // S-curve part 2
                new Vector3(-10, 0, 190),        // S-curve part 3

                // === TUNNEL SECTION (track stays flat, tunnel built over it) ===
                new Vector3(-50, 0, 160),
                new Vector3(-80, 0, 120),

                // === WIDE LEFT SWEEPER (inside tunnel!) ===
                new Vector3(-120, 0, 90),
                new Vector3(-140, 0, 50),

                // === TUNNEL EXIT / CHICANE ===
                new Vector3(-120, 0, 20),
                new Vector3(-100, 0, -10),       // Quick left-right
                new Vector3(-70, 0, -20),

                // === FINAL CURVE BACK TO START ===
                new Vector3(-30, 0, -30),
                new Vector3(-10, 0, -15),        // Final bend
            };
        }

        void PlaceItemBoxes(TrackSpline spline)
        {
            // Place item box rows at 6 locations around the bigger track
            float totalLen = spline.TotalLength;
            float[] placements = { 0.1f, 0.25f, 0.4f, 0.55f, 0.72f, 0.88f };

            foreach (float t in placements)
            {
                float dist = t * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                // Row of 3 item boxes
                for (int i = -1; i <= 1; i++)
                {
                    var itemGO = new GameObject($"ItemBox_{t}_{i}");
                    itemGO.transform.position = center + right * (i * 3.5f) + Vector3.up * 1.5f;
                    itemGO.transform.rotation = Quaternion.LookRotation(fwd);
                    var box = itemGO.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.5f, 1.5f, 1.5f);
                    box.isTrigger = true;
                    itemGO.AddComponent<ItemBox>();
                }
            }
        }

        void PlaceBoostPads(TrackSpline spline)
        {
            float totalLen = spline.TotalLength;
            float[] placements = { 0.12f, 0.32f, 0.5f, 0.68f, 0.88f };

            foreach (float t in placements)
            {
                float dist = t * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);

                var padGO = new GameObject($"BoostPad_{t}");
                padGO.transform.position = center + Vector3.up * 0.05f;
                padGO.transform.rotation = Quaternion.LookRotation(fwd);
                padGO.AddComponent<BoxCollider>().size = new Vector3(4f, 1f, 6f);
                padGO.AddComponent<BoostPad>();
            }
        }

        void PlaceJumpRamps(TrackSpline spline)
        {
            float totalLen = spline.TotalLength;
            // Place 3 jump ramps at exciting spots on the track
            float[] placements = { 0.2f, 0.5f, 0.8f };

            foreach (float t in placements)
            {
                float dist = t * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);

                var rampGO = new GameObject($"JumpRamp_{t}");
                rampGO.transform.position = center + Vector3.up * 0.05f;
                rampGO.transform.rotation = Quaternion.LookRotation(fwd);
                rampGO.AddComponent<JumpRamp>();
            }
        }

        void PlaceDecorations(TrackSpline spline)
        {
            float totalLen = spline.TotalLength;
            Material treeMat = MakeMat(new Color(0.15f, 0.5f, 0.1f));
            Material trunkMat = MakeMat(new Color(0.45f, 0.3f, 0.15f));
            Material yarnMat = MakeMat(new Color(0.9f, 0.2f, 0.3f));

            // === GROUND PLANE — large green floor under everything ===
            var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGO.name = "TempFloor";
            groundGO.transform.position = new Vector3(25, -0.05f, 60); // Center of the track area
            groundGO.transform.localScale = new Vector3(40, 1, 40); // 400x400 unit plane
            var groundMat = MakeMat(new Color(0.18f, 0.55f, 0.12f));
            groundMat.SetFloat("_Smoothness", 0.05f);
            groundGO.GetComponent<Renderer>().material = groundMat;

            // Trees along the track
            for (int i = 0; i < 30; i++)
            {
                float dist = (i / 30f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 12f + Random.Range(3f, 10f);
                Vector3 treePos = center + right * side * offset;
                treePos.y = 0;

                CreateTree(treePos, treeMat, trunkMat);
            }

            // Yarn ball decorations — placed OFF track as scenery
            for (int i = 0; i < 5; i++)
            {
                float dist = (i / 5f + 0.1f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                // Place well off to the side of the track (past the barriers)
                float side = (i % 2 == 0) ? 1 : -1;
                float yarnOffset = 18f + Random.Range(2f, 6f);
                Vector3 pos = center + right * side * yarnOffset;
                pos.y = 1f;

                var yarnGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                yarnGO.name = $"YarnDecor_{i}";
                yarnGO.transform.position = pos;
                yarnGO.transform.localScale = Vector3.one * 2f;
                yarnGO.GetComponent<Renderer>().material = yarnMat;
                Destroy(yarnGO.GetComponent<Collider>()); // No collision — just scenery
            }

            // Sun / directional light
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.85f);
            sun.transform.rotation = Quaternion.Euler(45, -30, 0);
        }

        void CreateTree(Vector3 pos, Material leafMat, Material trunkMat)
        {
            var tree = new GameObject("Tree");
            tree.transform.position = pos;

            float height = Random.Range(4f, 8f);
            float radius = Random.Range(2f, 3.5f);

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0, height * 0.4f, 0);
            trunk.transform.localScale = new Vector3(0.4f, height * 0.4f, 0.4f);
            trunk.GetComponent<Renderer>().material = trunkMat;

            // Canopy (stacked spheres)
            for (int i = 0; i < 3; i++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.transform.SetParent(tree.transform, false);
                float y = height * 0.6f + i * radius * 0.5f;
                float r = radius * (1f - i * 0.25f);
                leaf.transform.localPosition = new Vector3(
                    Random.Range(-0.3f, 0.3f), y, Random.Range(-0.3f, 0.3f));
                leaf.transform.localScale = Vector3.one * r;
                leaf.GetComponent<Renderer>().material = leafMat;
                Destroy(leaf.GetComponent<Collider>());
            }
        }

        void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
        }

        Material MakeMat(Color c)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            return mat;
        }

        void BuildTunnel(TrackSpline spline)
        {
            var tp = new GameObject("Tunnel");
            Material stone = MakeMat(new Color(0.35f, 0.30f, 0.28f));
            Material dark = MakeMat(new Color(0.25f, 0.22f, 0.20f));
            Material crystal = MakeMat(new Color(0.2f, 0.9f, 0.7f));
            Material arch = MakeMat(new Color(0.45f, 0.38f, 0.32f));

            float totalLen = spline.TotalLength;
            float tStart = 0.58f;
            float tEnd = 0.78f;
            int segs = 25;
            float w = 9f;
            float h = 5f;

            for (int i = 0; i <= segs; i++)
            {
                float pct = Mathf.Lerp(tStart, tEnd, i / (float)segs);
                float d = pct * totalLen;
                Vector3 p = spline.GetPointAtDistance(d);
                Vector3 f = spline.GetDirectionAtDistance(d);
                Vector3 r = Vector3.Cross(Vector3.up, f).normalized;
                float sl = (tEnd - tStart) * totalLen / segs + 0.5f;

                var lw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lw.transform.SetParent(tp.transform, false);
                lw.transform.position = p + r * w + Vector3.up * h * 0.5f;
                lw.transform.rotation = Quaternion.LookRotation(f);
                lw.transform.localScale = new Vector3(1f, h, sl);
                lw.GetComponent<Renderer>().material = stone;
                Destroy(lw.GetComponent<Collider>());

                var rw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rw.transform.SetParent(tp.transform, false);
                rw.transform.position = p - r * w + Vector3.up * h * 0.5f;
                rw.transform.rotation = Quaternion.LookRotation(f);
                rw.transform.localScale = new Vector3(1f, h, sl);
                rw.GetComponent<Renderer>().material = stone;
                Destroy(rw.GetComponent<Collider>());

                var cl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cl.transform.SetParent(tp.transform, false);
                cl.transform.position = p + Vector3.up * h;
                cl.transform.rotation = Quaternion.LookRotation(f);
                cl.transform.localScale = new Vector3(w * 2f + 1f, 0.8f, sl);
                cl.GetComponent<Renderer>().material = dark;
                Destroy(cl.GetComponent<Collider>());

                if (i % 3 == 0)
                {
                    var cr = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cr.transform.SetParent(tp.transform, false);
                    cr.transform.position = p + r * (w - 0.5f) + Vector3.up * 3f;
                    cr.transform.localScale = new Vector3(0.3f, 0.8f, 0.3f);
                    cr.GetComponent<Renderer>().material = crystal;
                    Destroy(cr.GetComponent<Collider>());

                    var lg = new GameObject("CL");
                    lg.transform.SetParent(tp.transform, false);
                    lg.transform.position = p + Vector3.up * 3f;
                    var lt = lg.AddComponent<Light>();
                    lt.type = LightType.Point;
                    lt.color = new Color(0.2f, 0.9f, 0.7f);
                    lt.range = 14f;
                    lt.intensity = 2.5f;
                }
            }

            foreach (float ap in new[] { tStart, tEnd })
            {
                float d = ap * totalLen;
                Vector3 p = spline.GetPointAtDistance(d);
                Vector3 f = spline.GetDirectionAtDistance(d);
                Vector3 r = Vector3.Cross(Vector3.up, f).normalized;

                var lp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lp.transform.SetParent(tp.transform, false);
                lp.transform.position = p + r * (w - 0.5f) + Vector3.up * h * 0.5f;
                lp.transform.rotation = Quaternion.LookRotation(f);
                lp.transform.localScale = new Vector3(2f, h, 2f);
                lp.GetComponent<Renderer>().material = arch;
                Destroy(lp.GetComponent<Collider>());

                var rp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rp.transform.SetParent(tp.transform, false);
                rp.transform.position = p - r * (w - 0.5f) + Vector3.up * h * 0.5f;
                rp.transform.rotation = Quaternion.LookRotation(f);
                rp.transform.localScale = new Vector3(2f, h, 2f);
                rp.GetComponent<Renderer>().material = arch;
                Destroy(rp.GetComponent<Collider>());

                var bm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bm.transform.SetParent(tp.transform, false);
                bm.transform.position = p + Vector3.up * (h + 0.4f);
                bm.transform.rotation = Quaternion.LookRotation(f);
                bm.transform.localScale = new Vector3(w * 2f + 1f, 1.5f, 2f);
                bm.GetComponent<Renderer>().material = arch;
                Destroy(bm.GetComponent<Collider>());
            }
        }
    }

    /// <summary>Simple restart handler.</summary>
    public class RestartHandler : MonoBehaviour
    {
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
