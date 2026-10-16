using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// The clockwork counterweight lift: moves between its start height and start + rise, pausing at each end,
    /// while <see cref="runFlag"/> is set and <see cref="stopFlag"/> is not. Pair with MovingPlatform (AZKit.Ride).
    /// </summary>
    public class L02Lift : MonoBehaviour
    {
        public float rise = 6f;
        public float speed = 0.8f;
        public float pause = 3f;
        public string runFlag = "";
        public string stopFlag = "";

        Vector3 bottom;
        float t;        // 0..1 along the travel
        int dir = 1;
        float wait;

        void Awake() { bottom = transform.localPosition; }

        void Update()
        {
            if (!WorldState.Get(runFlag) || WorldState.Get(stopFlag)) return;
            if (wait > 0f) { wait -= Time.deltaTime; return; }
            t += dir * speed * Time.deltaTime / Mathf.Max(0.1f, rise);
            if (t >= 1f) { t = 1f; dir = -1; wait = pause; AudioCues.PlayAt(Cue.Clang, transform.position, 0.5f); }
            else if (t <= 0f) { t = 0f; dir = 1; wait = pause; AudioCues.PlayAt(Cue.Clang, transform.position, 0.5f); }
            transform.localPosition = bottom + Vector3.up * rise * Mathf.SmoothStep(0f, 1f, t);
        }
    }
}
