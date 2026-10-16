using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 05 flood water that only exists in some states (normally the PRESENT).
    /// Unlike a WaterBody inside a StateGroup, this lives in shared space and owns its surface
    /// renderer and blocker itself, so the blocker is always correct at the instant of a shift:
    /// if the water was drained by a PAST action while the player was elsewhere, the level is
    /// already low when they arrive (the past changed it), and they are never refused a shift
    /// into water that should already be gone.
    /// highY is used while the flags are not all set, lowY once they are (lowY may be ABOVE highY
    /// for water that rises when a crisis begins).
    /// </summary>
    public class L05FloodLevel : MonoBehaviour
    {
        public Transform surface;
        [Tooltip("Renderers shown only while the water exists in the current state (surface, floating debris).")]
        public List<Renderer> renderers = new List<Renderer>();
        public Collider blocker;
        public TimeMask existsIn = TimeMask.Present;
        public float highY = 2f;
        public float lowY = 0f;
        public List<string> lowWhenAll = new List<string>();
        [Tooltip("Metres per second while the player watches it change.")]
        public float speed = 0.6f;
        [Tooltip("Blocker is solid while the surface is above this height.")]
        public float blockAboveY = 1.1f;
        public string reachedFlag = "";
        public bool emitCues = true;
        [Tooltip("If true, changes made while the player is in another state are already complete on return.")]
        public bool snapWhenAway = true;

        float currentY;
        bool reached;

        bool Low => lowWhenAll.Count > 0 && WorldState.All(lowWhenAll);
        bool Here => existsIn.Contains(TemporalStateManager.CurrentState);

        void OnEnable() { TemporalStateManager.StateChanged += OnState; }
        void OnDisable() { TemporalStateManager.StateChanged -= OnState; }

        void Start()
        {
            currentY = Low ? lowY : highY;
            Apply();
        }

        void OnState(TimeState from, TimeState to)
        {
            // Whatever happened while away has already happened.
            if (snapWhenAway && !existsIn.Contains(from)) currentY = Low ? lowY : highY;
            Apply();
        }

        void Update()
        {
            float target = Low ? lowY : highY;
            if (!Here)
            {
                if (snapWhenAway) currentY = target;
                Apply(); CheckReached(target); return;
            }
            if (!Mathf.Approximately(currentY, target))
            {
                bool first = Mathf.Approximately(currentY, Low ? highY : lowY);
                if (first && emitCues) AudioCues.PlayAt(Cue.WaterRush, surface != null ? surface.position : transform.position);
                currentY = Mathf.MoveTowards(currentY, target, speed * Time.deltaTime);
                Apply();
            }
            CheckReached(target);
        }

        void CheckReached(float target)
        {
            if (reached || !Low || !Mathf.Approximately(currentY, target)) return;
            reached = true;
            if (emitCues && Here) AudioCues.PlayAt(Cue.RuleChange, surface != null ? surface.position : transform.position);
            if (!string.IsNullOrEmpty(reachedFlag)) WorldState.Set(reachedFlag);
        }

        void Apply()
        {
            bool here = Here;
            if (surface != null)
            {
                var p = surface.position;
                p.y = currentY;
                surface.position = p;
            }
            foreach (var r in renderers) if (r != null) r.enabled = here;
            if (blocker != null) blocker.enabled = here && currentY > blockAboveY;
        }
    }
}
