using UnityEngine;
using CatnipCart.Kart;

namespace CatnipCart.Track
{
    /// <summary>
    /// Boost pad on the track. Gives a speed boost when driven over.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BoostPad : MonoBehaviour
    {
        public float boostForce = 12f;
        public float boostDuration = 1f;

        [Header("Visuals")]
        public Color padColor = new Color(0.2f, 0.8f, 1f);
        private Material padMat;
        private float pulseTimer;

        void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            // Main pad surface
            padMat = new Material(shader);
            padMat.color = padColor;
            padMat.SetFloat("_Smoothness", 0.8f);
            padMat.EnableKeyword("_EMISSION");
            padMat.SetColor("_EmissionColor", padColor * 2f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "BoostVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0, 0.02f, 0);
            visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
            visual.transform.localScale = new Vector3(3f, 5f, 1f);
            visual.GetComponent<Renderer>().material = padMat;
            Destroy(visual.GetComponent<Collider>());

            // Chevron arrows (3 arrows pointing forward)
            var arrowMat = new Material(shader);
            arrowMat.color = new Color(1f, 1f, 1f, 0.9f);
            arrowMat.EnableKeyword("_EMISSION");
            arrowMat.SetColor("_EmissionColor", Color.white * 2f);

            for (int i = 0; i < 3; i++)
            {
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = $"Arrow_{i}";
                arrow.transform.SetParent(transform, false);
                arrow.transform.localPosition = new Vector3(0, 0.04f, -1.2f + i * 1.2f);
                arrow.transform.localRotation = Quaternion.Euler(0, 45, 0);
                arrow.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
                arrow.GetComponent<Renderer>().material = arrowMat;
                Destroy(arrow.GetComponent<Collider>());
            }

            // Edge glow strips
            var glowMat = new Material(shader);
            glowMat.color = new Color(0f, 1f, 0.8f);
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", new Color(0f, 1f, 0.8f) * 3f);

            for (int side = -1; side <= 1; side += 2)
            {
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "EdgeGlow";
                strip.transform.SetParent(transform, false);
                strip.transform.localPosition = new Vector3(side * 1.5f, 0.03f, 0);
                strip.transform.localScale = new Vector3(0.1f, 0.04f, 5f);
                strip.GetComponent<Renderer>().material = glowMat;
                Destroy(strip.GetComponent<Collider>());
            }
        }

        void Update()
        {
            // Pulsing glow
            pulseTimer += Time.deltaTime;
            float pulse = 0.7f + Mathf.Sin(pulseTimer * 4f) * 0.3f;
            padMat.SetColor("_EmissionColor", padColor * pulse * 2f);
        }

        void OnTriggerEnter(Collider other)
        {
            var kart = other.GetComponentInParent<KartController>();
            if (kart != null)
            {
                kart.ApplyBoost(boostForce, boostDuration);
            }
        }
    }
}
