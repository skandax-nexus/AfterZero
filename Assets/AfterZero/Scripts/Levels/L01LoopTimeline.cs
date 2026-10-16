using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Keeps the L01 loop's scheduled moments proportional to the loop length when the Keepers tighten it
    /// (30 -> 24 -> 18 s): every LoopScheduledEvent's fireAt is rescaled from its 30-second base time.
    /// Also exposes Scale for other L01 scripts.
    /// </summary>
    public class L01LoopTimeline : MonoBehaviour
    {
        public float baseDuration = 30f;
        public List<LoopScheduledEvent> events = new List<LoopScheduledEvent>();

        readonly List<float> baseTimes = new List<float>();
        float lastDuration = -1f;

        public static float Scale
        {
            get
            {
                var lm = LoopManager.Instance;
                return lm != null && lm.loopDuration > 0f ? Mathf.Clamp01(lm.loopDuration / 30f) : 1f;
            }
        }

        void Awake()
        {
            foreach (var e in events) baseTimes.Add(e != null ? e.fireAt : 0f);
        }

        void Update()
        {
            var lm = LoopManager.Instance;
            if (lm == null || Mathf.Approximately(lm.loopDuration, lastDuration)) return;
            lastDuration = lm.loopDuration;
            float k = lm.loopDuration / baseDuration;
            for (int i = 0; i < events.Count; i++)
                if (events[i] != null) events[i].fireAt = baseTimes[i] * k;
        }
    }
}
