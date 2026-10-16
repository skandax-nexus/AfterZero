using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 04 crossing signal: shows STOP (red lamp + text + raised arm) while its tram is within
    /// warnDistance, GO otherwise. Text and arm shape keep it readable without colour.
    /// </summary>
    public class L04TramSignal : MonoBehaviour
    {
        public Transform tram;
        public float warnDistance = 20f;
        public TimeMask activeIn = TimeMask.Past;
        public Light lamp;
        public Renderer lampRenderer;
        public GameObject stopVisual;
        public GameObject goVisual;
        public Color stopColor = new Color(1f, 0.15f, 0.1f);
        public Color goColor = new Color(0.3f, 1f, 0.6f);

        MaterialPropertyBlock block;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        int last = -1;

        void Awake() { block = new MaterialPropertyBlock(); }

        void Update()
        {
            bool on = activeIn.Contains(TemporalStateManager.CurrentState) && tram != null;
            int mode = !on ? 0 : (Vector3.Distance(tram.position, transform.position) < warnDistance ? 2 : 1);
            if (mode == last) return;
            last = mode;
            Color c = mode == 2 ? stopColor : goColor;
            if (lamp != null) { lamp.enabled = mode != 0; lamp.color = c; }
            if (lampRenderer != null)
            {
                lampRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionId, mode == 0 ? Color.black : c * 4f);
                lampRenderer.SetPropertyBlock(block);
            }
            if (stopVisual != null) stopVisual.SetActive(mode == 2);
            if (goVisual != null) goVisual.SetActive(mode == 1);
        }
    }
}
