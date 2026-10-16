using UnityEngine;

namespace AfterZero
{
    /// <summary>Neon / failing-lamp flicker for lights and emissive renderers.</summary>
    public class NeonFlicker : MonoBehaviour
    {
        public Light targetLight;
        public Renderer targetRenderer;
        [Range(0f, 1f)] public float flickerChance = 0.04f;
        public float hum = 0.06f;
        public Color emission = new Color(0f, 2.5f, 3f);

        float baseIntensity;
        float dropout;
        MaterialPropertyBlock block;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            if (targetLight == null) targetLight = GetComponentInChildren<Light>();
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetLight != null) baseIntensity = targetLight.intensity;
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (dropout > 0f) dropout -= Time.deltaTime;
            else if (Random.value < flickerChance * Time.deltaTime * 30f) dropout = Random.Range(0.03f, 0.18f);

            float k = dropout > 0f ? 0.1f : 1f - hum * (0.5f + 0.5f * Mathf.Sin(Time.time * 120f));
            if (targetLight != null) targetLight.intensity = baseIntensity * k;
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionId, emission * k);
                targetRenderer.SetPropertyBlock(block);
            }
        }
    }
}
