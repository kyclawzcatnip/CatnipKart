using UnityEngine;
using CatnipCart.Kart;

namespace CatnipCart.Track
{
    /// <summary>
    /// Jump ramp — launches karts into the air when driven over.
    /// Builds a visual ramp from primitives.
    /// </summary>
    public class JumpRamp : MonoBehaviour
    {
        [Header("Jump Settings")]
        public float launchForce = 14f;
        public float forwardBoost = 5f;

        [Header("Visuals")]
        public Color rampColor = new Color(0.95f, 0.6f, 0.1f);
        public Color stripeColor = new Color(0.9f, 0.2f, 0.2f);

        void Start()
        {
            BuildRampVisual();

            // Trigger collider at the top of the ramp
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(6f, 2f, 3f);
            col.center = new Vector3(0, 1f, 1f);
        }

        void BuildRampVisual()
        {
            Material rampMat = MakeMat(rampColor);
            Material stripeMat = MakeMat(stripeColor);

            // Main ramp surface — angled cube
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "RampSurface";
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = new Vector3(0, 0.5f, 0);
            surface.transform.localScale = new Vector3(6f, 0.3f, 5f);
            surface.transform.localRotation = Quaternion.Euler(-20, 0, 0);
            surface.GetComponent<Renderer>().material = rampMat;
            Destroy(surface.GetComponent<Collider>());

            // Side walls
            for (int side = -1; side <= 1; side += 2)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"RampWall_{side}";
                wall.transform.SetParent(transform, false);
                wall.transform.localPosition = new Vector3(side * 3.1f, 0.7f, 0);
                wall.transform.localScale = new Vector3(0.2f, 1.5f, 5f);
                wall.transform.localRotation = Quaternion.Euler(-20, 0, 0);
                wall.GetComponent<Renderer>().material = stripeMat;
                Destroy(wall.GetComponent<Collider>());
            }

            // Chevron stripes on the ramp face
            for (int i = 0; i < 3; i++)
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = $"Stripe_{i}";
                stripe.transform.SetParent(transform, false);
                stripe.transform.localPosition = new Vector3(0, 0.55f, -1.5f + i * 1.5f);
                stripe.transform.localScale = new Vector3(5.5f, 0.35f, 0.3f);
                stripe.transform.localRotation = Quaternion.Euler(-20, 0, 0);
                stripe.GetComponent<Renderer>().material = stripeMat;
                Destroy(stripe.GetComponent<Collider>());
            }

            // Arrow pointing up on the front face
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "Arrow";
            arrow.transform.SetParent(transform, false);
            arrow.transform.localPosition = new Vector3(0, 1.2f, 2.3f);
            arrow.transform.localScale = new Vector3(1.5f, 1.5f, 0.15f);
            arrow.transform.localRotation = Quaternion.Euler(0, 0, 45);
            arrow.GetComponent<Renderer>().material = stripeMat;
            Destroy(arrow.GetComponent<Collider>());
        }

        void OnTriggerEnter(Collider other)
        {
            var kart = other.GetComponentInParent<KartController>();
            if (kart == null) return;

            var rb = kart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Launch upward + forward boost
                Vector3 launchDir = (Vector3.up + transform.forward * 0.3f).normalized;
                rb.AddForce(launchDir * launchForce, ForceMode.VelocityChange);
                rb.AddForce(transform.forward * forwardBoost, ForceMode.VelocityChange);
            }
        }

        Material MakeMat(Color c)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }
    }
}
