using UnityEngine;
using CatnipCart.Core;

namespace CatnipCart.Kart
{
    /// <summary>
    /// Builds a cat character from Unity primitives, matching the
    /// Super Cat World art style exactly. Uses cubes for blocky pixel-art look.
    /// Features: triangular ears with pink inner, red hat with gold band and
    /// anticatite gem, white eyes with dark pupils, pink nose, whiskers,
    /// 3 legs with white paws, animated tail.
    /// </summary>
    public class CatBuilder : MonoBehaviour
    {
        public CatColorData colorData;
        public bool wearHat = true;

        [Header("Animation")]
        public KartController kart;

        // Generated parts for animation
        private Transform head;
        private Transform leftEar, rightEar;
        private Transform[] tailSegments;
        private Transform hat;
        private Transform leftPupil, rightPupil;

        void Start()
        {
            if (colorData == null) colorData = CatColorData.CreateGinger();
            BuildCat();
        }

        void Update()
        {
            AnimateCat();
        }

        void BuildCat()
        {
            // Create materials from Super Cat World palette
            Material bodyMat = MakeMat(colorData.body);
            Material highlightMat = MakeMat(colorData.belly);      // Lighter center highlight
            Material legMat = MakeMat(colorData.bodyDark);         // Darker orange for legs
            Material earInnerMat = MakeMat(colorData.innerEar);    // Pink inner ear
            Material eyeWhiteMat = MakeMat(Color.white);           // White sclera
            Material pupilMat = MakeMat(colorData.eyes);           // Dark pupils
            Material noseMat = MakeMat(colorData.nose);            // Pink nose
            Material pawMat = MakeMat(colorData.paw);              // White paws
            Material hatMat = MakeMat(colorData.hatRed);           // Red hat
            Material bandMat = MakeMat(colorData.hatBand);         // Gold band
            Material gemMat = MakeMat(colorData.gemColor);         // Anticatite gem
            Material gemHiMat = MakeMat(colorData.gemHighlight);   // Gem highlight
            Material whiskerMat = MakeMat(new Color(0.87f, 0.87f, 0.87f)); // #DDD whiskers

            // ========== BODY ==========
            // Outer body block
            MakeBox("Body", bodyMat,
                new Vector3(0, 0.35f, 0), new Vector3(0.40f, 0.35f, 0.28f));
            // Inner highlight (belly patch, slightly forward)
            MakeBox("BodyHighlight", highlightMat,
                new Vector3(0, 0.34f, 0.05f), new Vector3(0.30f, 0.25f, 0.20f));

            // ========== HEAD ==========
            // Main head block
            var headObj = MakeBox("Head", bodyMat,
                new Vector3(0, 0.62f, 0.04f), new Vector3(0.38f, 0.30f, 0.28f));
            head = headObj.transform;
            // Head highlight
            MakeBox("HeadHighlight", highlightMat,
                new Vector3(0, 0.63f, 0.07f), new Vector3(0.28f, 0.22f, 0.20f));

            // ========== EARS (with pink inner) ==========
            leftEar = MakeEar("LeftEar", -0.12f, bodyMat, earInnerMat);
            rightEar = MakeEar("RightEar", 0.12f, bodyMat, earInnerMat);

            // ========== FACE (placed well in front of head surface) ==========
            // The head front face is at Z = 0.04 + 0.14 = 0.18
            // Face features must be at Z >= 0.19 to be visible

            // Left eye white
            MakeBox("LeftEyeWhite", eyeWhiteMat,
                new Vector3(-0.08f, 0.66f, 0.19f), new Vector3(0.07f, 0.07f, 0.03f));
            // Left pupil (slightly forward)
            var lpObj = MakeBox("LeftPupil", pupilMat,
                new Vector3(-0.06f, 0.655f, 0.205f), new Vector3(0.045f, 0.055f, 0.02f));
            leftPupil = lpObj.transform;

            // Right eye white
            MakeBox("RightEyeWhite", eyeWhiteMat,
                new Vector3(0.08f, 0.66f, 0.19f), new Vector3(0.07f, 0.07f, 0.03f));
            // Right pupil (slightly forward)
            var rpObj = MakeBox("RightPupil", pupilMat,
                new Vector3(0.10f, 0.655f, 0.205f), new Vector3(0.045f, 0.055f, 0.02f));
            rightPupil = rpObj.transform;

            // ========== NOSE (pink, below eyes) ==========
            MakeBox("Nose", noseMat,
                new Vector3(0, 0.59f, 0.19f), new Vector3(0.04f, 0.03f, 0.03f));

            // ========== WHISKERS (thin boxes extending from cheeks) ==========
            // Left whiskers
            MakeBox("LeftWhisker1", whiskerMat,
                new Vector3(-0.22f, 0.63f, 0.19f), new Vector3(0.16f, 0.012f, 0.01f));
            MakeBox("LeftWhisker2", whiskerMat,
                new Vector3(-0.22f, 0.59f, 0.19f), new Vector3(0.18f, 0.012f, 0.01f));
            // Right whiskers
            MakeBox("RightWhisker1", whiskerMat,
                new Vector3(0.22f, 0.63f, 0.19f), new Vector3(0.16f, 0.012f, 0.01f));
            MakeBox("RightWhisker2", whiskerMat,
                new Vector3(0.22f, 0.59f, 0.19f), new Vector3(0.18f, 0.012f, 0.01f));

            // ========== 3 LEGS + WHITE PAWS (like the original!) ==========
            float legY = 0.10f;
            float pawY = 0.02f;
            // Left leg
            MakeBox("LegLeft", legMat,
                new Vector3(-0.12f, legY, 0), new Vector3(0.08f, 0.14f, 0.08f));
            MakeBox("PawLeft", pawMat,
                new Vector3(-0.12f, pawY, 0), new Vector3(0.09f, 0.04f, 0.10f));
            // Middle leg
            MakeBox("LegMiddle", legMat,
                new Vector3(0, legY, 0), new Vector3(0.08f, 0.14f, 0.08f));
            MakeBox("PawMiddle", pawMat,
                new Vector3(0, pawY, 0), new Vector3(0.09f, 0.04f, 0.10f));
            // Right leg
            MakeBox("LegRight", legMat,
                new Vector3(0.12f, legY, 0), new Vector3(0.08f, 0.14f, 0.08f));
            MakeBox("PawRight", pawMat,
                new Vector3(0.12f, pawY, 0), new Vector3(0.09f, 0.04f, 0.10f));

            // ========== TAIL (segmented, animated) ==========
            BuildTail(bodyMat, legMat);

            // ========== HAT (red with gold band + anticatite gem) ==========
            if (wearHat)
            {
                hat = new GameObject("Hat").transform;
                hat.SetParent(head, false);
                hat.localPosition = new Vector3(0, 0.18f, -0.02f);

                // Hat brim (wide, flat box)
                var brim = MakeBox("HatBrim", hatMat,
                    Vector3.zero, new Vector3(0.42f, 0.05f, 0.30f));
                brim.transform.SetParent(hat, false);
                brim.transform.localPosition = Vector3.zero;

                // Hat crown (taller, narrower box)
                var crown = MakeBox("HatCrown", hatMat,
                    Vector3.zero, new Vector3(0.26f, 0.14f, 0.24f));
                crown.transform.SetParent(hat, false);
                crown.transform.localPosition = new Vector3(0, 0.09f, 0);

                // Gold band
                var band = MakeBox("HatBand", bandMat,
                    Vector3.zero, new Vector3(0.27f, 0.04f, 0.25f));
                band.transform.SetParent(hat, false);
                band.transform.localPosition = new Vector3(0, 0.02f, 0);

                // Anticatite gem (front of hat)
                var gem = MakeBox("HatGem", gemMat,
                    Vector3.zero, new Vector3(0.08f, 0.08f, 0.03f));
                gem.transform.SetParent(hat, false);
                gem.transform.localPosition = new Vector3(0, 0.07f, 0.13f);

                // Gem highlight
                var gemHi = MakeBox("HatGemHi", gemHiMat,
                    Vector3.zero, new Vector3(0.04f, 0.04f, 0.02f));
                gemHi.transform.SetParent(hat, false);
                gemHi.transform.localPosition = new Vector3(0, 0.08f, 0.14f);
            }

            // Remove colliders from all child parts (kart has its own collider)
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (col.gameObject != gameObject)
                    Destroy(col);
            }
        }

        void BuildTail(Material bodyMat, Material legMat)
        {
            tailSegments = new Transform[5];
            for (int i = 0; i < tailSegments.Length; i++)
            {
                float t = i / (float)(tailSegments.Length - 1);
                // Tail curves up and back
                Material segMat = (i < tailSegments.Length - 1) ? bodyMat : legMat;
                float segSize = 0.07f - t * 0.02f;
                var seg = MakeBox($"Tail_{i}", segMat,
                    new Vector3(0, 0.40f + t * 0.22f, -0.16f - t * 0.16f),
                    Vector3.one * segSize);
                tailSegments[i] = seg.transform;
            }
        }

        Transform MakeEar(string name, float xOff, Material outerMat, Material innerMat)
        {
            var earParent = new GameObject(name).transform;
            earParent.SetParent(transform, false);
            earParent.localPosition = new Vector3(xOff, 0.82f, 0.04f);

            // Outer ear (cube, tilted slightly outward)
            var outer = MakeBox(name + "_Outer", outerMat,
                Vector3.zero, new Vector3(0.08f, 0.12f, 0.06f));
            outer.transform.SetParent(earParent, false);
            outer.transform.localPosition = Vector3.zero;
            outer.transform.localRotation = Quaternion.Euler(0, 0, xOff > 0 ? -10 : 10);

            // Inner ear (pink, smaller)
            var inner = MakeBox(name + "_Inner", innerMat,
                Vector3.zero, new Vector3(0.04f, 0.08f, 0.04f));
            inner.transform.SetParent(earParent, false);
            inner.transform.localPosition = new Vector3(0, 0.01f, 0.015f);
            inner.transform.localRotation = Quaternion.Euler(0, 0, xOff > 0 ? -10 : 10);

            return earParent;
        }

        /// <summary>
        /// Creates a cube primitive (blocky pixel-art style, matching Super Cat World)
        /// </summary>
        GameObject MakeBox(string name, Material mat, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        Material MakeMat(Color c)
        {
            // Use URP Lit shader
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = c;
            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }

        // === ANIMATION ===
        void AnimateCat()
        {
            if (kart == null) return;
            float t = Time.time;

            // Head turns toward steering
            if (head != null)
            {
                float steerLook = kart.CurrentState == KartController.KartState.Drifting
                    ? kart.DriftDirection * 25f
                    : 0f;
                Quaternion targetRot = Quaternion.Euler(0, steerLook, 0);
                head.localRotation = Quaternion.Slerp(head.localRotation, targetRot, 5f * Time.deltaTime);
            }

            // Pupils look toward turns
            if (leftPupil != null && rightPupil != null)
            {
                float lookX = kart.NormalizedSpeed > 0.1f ? 0.01f : 0f;
                leftPupil.localPosition = new Vector3(-0.06f + lookX, 0.655f, 0.205f);
                rightPupil.localPosition = new Vector3(0.10f + lookX, 0.655f, 0.205f);
            }

            // Ears flatten when boosting
            if (leftEar != null && rightEar != null)
            {
                float earAngle = kart.IsBoosting ? 45f : 0f;
                leftEar.localRotation = Quaternion.Slerp(leftEar.localRotation,
                    Quaternion.Euler(earAngle, 0, 0), 8f * Time.deltaTime);
                rightEar.localRotation = Quaternion.Slerp(rightEar.localRotation,
                    Quaternion.Euler(earAngle, 0, 0), 8f * Time.deltaTime);
            }

            // Tail sways
            if (tailSegments != null)
            {
                bool drifting = kart.CurrentState == KartController.KartState.Drifting;
                for (int i = 0; i < tailSegments.Length; i++)
                {
                    if (tailSegments[i] == null) continue;
                    float seg_t = i / (float)(tailSegments.Length - 1);
                    float sway = drifting ? 0f : Mathf.Sin(t * 3f + i * 0.5f) * 0.03f * seg_t;
                    var pos = tailSegments[i].localPosition;
                    pos.x = sway;
                    tailSegments[i].localPosition = pos;
                }
            }
        }
    }
}
