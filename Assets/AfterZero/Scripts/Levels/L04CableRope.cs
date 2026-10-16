using UnityEngine;

namespace AfterZero
{
    /// <summary>Draws a sagging cable from a fixed anchor to a moving end (the grabbable cable plug).</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class L04CableRope : MonoBehaviour
    {
        public Transform anchor;
        public Transform end;
        [Range(4, 32)] public int segments = 14;
        public float sag = 0.6f;

        LineRenderer line;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = segments + 1;
            line.useWorldSpace = true;
        }

        void LateUpdate()
        {
            if (anchor == null || end == null || line == null) return;
            Vector3 a = anchor.position, b = end.position;
            float drop = sag * Mathf.Clamp01(Vector3.Distance(a, b) / 6f);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= Mathf.Sin(t * Mathf.PI) * drop;
                line.SetPosition(i, p);
            }
        }
    }
}
