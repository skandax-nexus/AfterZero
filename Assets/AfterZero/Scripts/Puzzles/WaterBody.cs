using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// A water surface whose height is driven by flags (drain a chamber, redirect a channel).
    /// While high, the optional blocker collider stops the player wading through.
    /// Drowning is never instant: the player is pushed back out with a warning.
    /// </summary>
    public class WaterBody : MonoBehaviour
    {
        public Transform surface;
        public float highY = 0f;
        public float lowY = -3f;
        [Tooltip("Water is LOW when all of these flags are set.")]
        public List<string> lowWhenAll = new List<string>();
        public float speed = 0.6f;
        public Collider blockerWhenHigh;
        public string drainedFlag = "";
        public bool emitDrainCue = true;

        float currentY;
        bool wasLow;

        void Start()
        {
            if (surface == null) surface = transform;
            bool low = WorldState.All(lowWhenAll) && lowWhenAll.Count > 0;
            currentY = low ? lowY : highY;
            wasLow = low;
            Apply();
        }

        void Update()
        {
            bool low = lowWhenAll.Count > 0 && WorldState.All(lowWhenAll);
            float target = low ? lowY : highY;
            if (!Mathf.Approximately(currentY, target))
            {
                currentY = Mathf.MoveTowards(currentY, target, speed * Time.deltaTime);
                Apply();
            }
            // Fires once the surface has actually reached the low level (not only within the frame the flags changed).
            if (low && Mathf.Approximately(currentY, lowY) && !string.IsNullOrEmpty(drainedFlag) && !WorldState.Get(drainedFlag))
            {
                WorldState.Set(drainedFlag);
                if (emitDrainCue) AudioCues.PlayAt(Cue.RuleChange, surface.position);
            }
            if (low != wasLow && emitDrainCue) AudioCues.PlayAt(Cue.WaterRush, surface.position);
            wasLow = low;
        }

        void Apply()
        {
            var p = surface.localPosition;
            p.y = currentY;
            surface.localPosition = p;
            if (blockerWhenHigh != null) blockerWhenHigh.enabled = currentY > Mathf.Lerp(lowY, highY, 0.35f);
        }
    }
}
