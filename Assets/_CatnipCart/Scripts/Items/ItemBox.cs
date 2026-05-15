using UnityEngine;
using CatnipCart.Kart;

namespace CatnipCart.Items
{
    /// <summary>
    /// Floating item box that gives a random item when driven through.
    /// Only deactivates when it actually gives an item.
    /// Respawns after a delay.
    /// </summary>
    public class ItemBox : MonoBehaviour
    {
        public float respawnTime = 4f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.3f;
        public float rotateSpeed = 90f;

        private bool isActive = true;
        private float respawnTimer;
        private Vector3 startPos;
        private GameObject visual;
        private Collider myCollider;

        void Start()
        {
            startPos = transform.position;
            BuildVisual();
            myCollider = GetComponent<Collider>();
            if (myCollider != null) myCollider.isTrigger = true;
        }

        void BuildVisual()
        {
            visual = new GameObject("ItemBoxVisual");
            visual.transform.SetParent(transform, false);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            // === OUTER FRAME (dark translucent outline) ===
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(visual.transform, false);
            frame.transform.localScale = Vector3.one * 1.3f;
            Destroy(frame.GetComponent<Collider>());
            var frameMat = new Material(shader);
            frameMat.color = new Color(0.15f, 0.1f, 0.25f, 0.6f);
            frameMat.SetFloat("_Surface", 1); // Transparent
            frameMat.SetFloat("_Blend", 0);
            frameMat.SetFloat("_Smoothness", 0.9f);
            frameMat.SetOverrideTag("RenderType", "Transparent");
            frameMat.renderQueue = 3000;
            frame.GetComponent<Renderer>().material = frameMat;

            // === INNER GLOWING CUBE ===
            innerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            innerCube.name = "InnerCube";
            innerCube.transform.SetParent(visual.transform, false);
            innerCube.transform.localScale = Vector3.one * 1.0f;
            Destroy(innerCube.GetComponent<Collider>());
            innerMat = new Material(shader);
            innerMat.color = new Color(1f, 0.85f, 0f);
            innerMat.SetFloat("_Smoothness", 0.8f);
            innerMat.EnableKeyword("_EMISSION");
            innerMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0f) * 1.5f);
            innerCube.GetComponent<Renderer>().material = innerMat;

            // === PAW PRINT on each face (cat themed!) ===
            Vector3[] faceNormals = { Vector3.forward, Vector3.back, Vector3.left,
                                      Vector3.right, Vector3.up, Vector3.down };
            foreach (var dir in faceNormals)
            {
                // Big pad
                var pad = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pad.name = "Pad";
                pad.transform.SetParent(visual.transform, false);
                pad.transform.localPosition = dir * 0.52f + (dir == Vector3.up || dir == Vector3.down ? Vector3.zero : Vector3.down * 0.05f);
                pad.transform.localScale = Vector3.one * 0.22f;
                Destroy(pad.GetComponent<Collider>());
                var padMat = new Material(shader);
                padMat.color = new Color(0.95f, 0.4f, 0.6f); // Pink paw
                padMat.SetFloat("_Smoothness", 0.6f);
                pad.GetComponent<Renderer>().material = padMat;

                // 3 toe beans
                for (int t = -1; t <= 1; t++)
                {
                    var bean = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bean.name = "Bean";
                    bean.transform.SetParent(visual.transform, false);
                    Vector3 across = Vector3.Cross(dir, (dir == Vector3.up || dir == Vector3.down) ? Vector3.forward : Vector3.up).normalized;
                    Vector3 perp = Vector3.Cross(dir, across).normalized;
                    bean.transform.localPosition = dir * 0.52f + across * t * 0.1f + perp * 0.14f;
                    bean.transform.localScale = Vector3.one * 0.1f;
                    Destroy(bean.GetComponent<Collider>());
                    var beanMat = new Material(shader);
                    beanMat.color = new Color(1f, 0.55f, 0.7f); // Lighter pink beans
                    beanMat.SetFloat("_Smoothness", 0.6f);
                    bean.GetComponent<Renderer>().material = beanMat;
                }
            }

            // === SPARKLE PARTICLES (4 tiny floating cubes) ===
            for (int i = 0; i < 4; i++)
            {
                var sparkle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sparkle.name = $"Sparkle_{i}";
                sparkle.transform.SetParent(visual.transform, false);
                sparkle.transform.localScale = Vector3.one * 0.08f;
                sparkle.transform.localRotation = Quaternion.Euler(45, 45 * i, 0);
                Destroy(sparkle.GetComponent<Collider>());
                var sMat = new Material(shader);
                sMat.color = Color.white;
                sMat.EnableKeyword("_EMISSION");
                sMat.SetColor("_EmissionColor", Color.white * 3f);
                sparkle.GetComponent<Renderer>().material = sMat;
            }
        }

        private GameObject innerCube;
        private Material innerMat;

         void Update()
        {
            if (!isActive)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0)
                {
                    isActive = true;
                    visual.SetActive(true);
                    if (myCollider != null) myCollider.enabled = true;
                }
                return;
            }

            // Bob and rotate
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + Vector3.up * bob;
            visual.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.Self);

            // Rainbow color cycle on inner cube
            if (innerMat != null)
            {
                float hue = Mathf.Repeat(Time.time * 0.3f, 1f);
                Color rainbow = Color.HSVToRGB(hue, 0.7f, 1f);
                innerMat.color = rainbow;
                innerMat.SetColor("_EmissionColor", rainbow * 1.5f);
            }

            // Orbiting sparkles
            if (visual != null)
            {
                for (int i = 0; i < visual.transform.childCount; i++)
                {
                    var child = visual.transform.GetChild(i);
                    if (child.name.StartsWith("Sparkle"))
                    {
                        float angle = Time.time * 120f + i * 90f;
                        float radius = 0.9f;
                        float y = Mathf.Sin(Time.time * 2f + i) * 0.4f;
                        child.localPosition = new Vector3(
                            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                            y,
                            Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                        child.Rotate(100f * Time.deltaTime, 200f * Time.deltaTime, 0);
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            TryGiveItem(other);
        }

        // Backup for fast-moving karts
        void OnTriggerStay(Collider other)
        {
            TryGiveItem(other);
        }

        void TryGiveItem(Collider other)
        {
            if (!isActive) return;

            var kart = other.GetComponentInParent<KartController>();
            if (kart == null) return;

            // Only give item if the kart doesn't already have one
            var holder = kart.GetComponent<ItemHolder>();
            if (holder == null) return;
            if (holder.HasItem || holder.IsRoulette) return; // Already has item or rolling

            // Give the item
            holder.GiveRandomItem();

            // NOW deactivate (only when item was actually given)
            isActive = false;
            respawnTimer = respawnTime;
            visual.SetActive(false);
            if (myCollider != null) myCollider.enabled = false;
        }
    }
}
