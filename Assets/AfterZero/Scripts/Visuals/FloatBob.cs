using UnityEngine;

namespace AfterZero
{
    /// <summary>Gentle hover for Future-state debris, floating records, hologram glyphs.</summary>
    public class FloatBob : MonoBehaviour
    {
        public float amplitude = 0.15f;
        public float frequency = 0.4f;
        public Vector3 drift = new Vector3(0f, 8f, 0f);

        Vector3 basePos;
        float phase;

        void Awake()
        {
            basePos = transform.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            transform.localPosition = basePos + Vector3.up * Mathf.Sin((Time.time + phase) * frequency * Mathf.PI * 2f) * amplitude;
            transform.Rotate(drift * Time.deltaTime, Space.Self);
        }
    }
}
