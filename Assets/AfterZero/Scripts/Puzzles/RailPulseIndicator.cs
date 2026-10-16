using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// A rail segment that pulses brighter and faster the closer the vibration machine is, so the
    /// player can "read" its position through the rails. Pulses also scale the emissive strip,
    /// so the cue is readable without relying on colour.
    /// </summary>
    public class RailPulseIndicator : MonoBehaviour
    {
        public Renderer strip;
        public Light glow;
        public float range = 20f;
        public Color pulseColor = new Color(0.2f, 0.9f, 1f);

        MaterialPropertyBlock block;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        Vector3 baseScale;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (strip != null) baseScale = strip.transform.localScale;
        }

        void Update()
        {
            float nearest = float.MaxValue;
            foreach (var s in VibrationSentinel.All)
                if (s != null && s.isActiveAndEnabled) nearest = Mathf.Min(nearest, Vector3.Distance(s.Position, transform.position));
            float k = nearest < range ? 1f - nearest / range : 0f;
            float rate = Mathf.Lerp(0.5f, 6f, k);
            float pulse = k * (0.5f + 0.5f * Mathf.Sin(Time.time * rate * Mathf.PI * 2f));

            if (strip != null)
            {
                strip.GetPropertyBlock(block);
                block.SetColor(EmissionId, pulseColor * (0.15f + pulse * 6f));
                block.SetColor(BaseColorId, Color.Lerp(Color.black, pulseColor, 0.1f + pulse * 0.4f));
                strip.SetPropertyBlock(block);
                strip.transform.localScale = new Vector3(baseScale.x * (1f + pulse * 0.6f), baseScale.y * (1f + pulse * 1.5f), baseScale.z);
            }
            if (glow != null) glow.intensity = pulse * 3f;
        }
    }
}
