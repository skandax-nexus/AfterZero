using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// The clockworks flood (replaces WaterBody here): drains when all <see cref="lowWhenAll"/> flags are set,
    /// sets <see cref="drainedFlag"/> when the pit is dry, then starts a refill countdown. If the player has not
    /// reached <see cref="stopFlag"/> in time, the flags in <see cref="clearOnRefill"/> are cleared and the water
    /// returns (a player still in the pit in the PRESENT is swept back to the last checkpoint).
    /// Also owns the invisible "deep water" wall, which is solid only in <see cref="blockIn"/> while the water is high,
    /// and updates it immediately on a state shift so the shift's obstruction check sees it.
    /// </summary>
    public class L02Flood : MonoBehaviour
    {
        public Transform surface;
        public Collider blocker;
        public float highY = 0.6f;
        public float lowY = -2.45f;
        public float speed = 0.45f;
        public float blockAboveY = -2f;
        public TimeMask blockIn = TimeMask.Present;
        public List<string> lowWhenAll = new List<string>();
        public string drainedFlag = "";

        [Header("Refill")]
        public float refillSeconds = 75f;
        public string stopFlag = "";
        public List<string> clearOnRefill = new List<string>();
        public Bounds sweepArea = new Bounds(Vector3.zero, Vector3.one);
        [TextArea] public string refillMessage = "The flood is coming back!";
        [TextArea] public string sweptMessage = "The water came back. Climb before it returns.";
        public string timerLabel = "FLOOD RETURNS IN";

        float currentY;
        bool drainedSet;
        bool refilling;
        float remaining;
        bool warned;

        void Start()
        {
            currentY = highY;
            Apply();
        }

        void OnEnable() { TemporalStateManager.StateChanged += OnState; }
        void OnDisable() { TemporalStateManager.StateChanged -= OnState; }
        void OnState(TimeState from, TimeState to) => UpdateBlocker(to);

        bool Low => lowWhenAll.Count > 0 && WorldState.All(lowWhenAll);

        void Update()
        {
            bool low = Low;
            float target = low ? lowY : highY;
            if (!Mathf.Approximately(currentY, target))
            {
                bool wasHigh = Mathf.Approximately(currentY, highY);
                currentY = Mathf.MoveTowards(currentY, target, speed * Time.deltaTime);
                Apply();
                if (wasHigh && low) AudioCues.PlayAt(Cue.WaterRush, surface.position);
            }
            if (low && !drainedSet && Mathf.Approximately(currentY, lowY))
            {
                drainedSet = true;
                WorldState.Set(drainedFlag);
                AudioCues.PlayAt(Cue.RuleChange, surface.position);
                if (refillSeconds > 0f && !WorldState.Get(stopFlag))
                {
                    refilling = true;
                    warned = false;
                    remaining = refillSeconds * GameSettings.TimerScale;
                    HUD.TimerLabel = timerLabel;
                }
            }
            if (!low) drainedSet = false;

            if (refilling) TickRefill();
        }

        void TickRefill()
        {
            if (WorldState.Get(stopFlag)) { refilling = false; HUD.SetTimer(-1f); return; }
            remaining -= Time.deltaTime;
            HUD.SetTimer(Mathf.Max(0f, remaining));
            if (!warned && remaining <= 15f)
            {
                warned = true;
                HUD.Toast(refillMessage, 3f);
                AudioCues.PlayAt(Cue.WaterRush, surface.position);
                AZHaptics.Pulse();
            }
            if (remaining <= 10f && Mathf.FloorToInt(remaining + Time.deltaTime) != Mathf.FloorToInt(remaining)) AudioCues.Play(Cue.Tick);
            if (remaining > 0f) return;

            refilling = false;
            HUD.SetTimer(-1f);
            foreach (var f in clearOnRefill) WorldState.Set(f, false);
            AudioCues.Play(Cue.WaterRush);
            var p = PlayerController.Instance;
            if (p != null && TemporalStateManager.CurrentState == TimeState.Present && sweepArea.Contains(p.transform.position))
                p.Die(sweptMessage);
            else
                HUD.Toast("The flood returns to the pit.", 3f);
        }

        void Apply()
        {
            var lp = surface.localPosition;
            lp.y = currentY;
            surface.localPosition = lp;
            UpdateBlocker(TemporalStateManager.CurrentState);
        }

        void LateUpdate() => UpdateBlocker(TemporalStateManager.CurrentState);

        void UpdateBlocker(TimeState s)
        {
            if (blocker == null) return;
            bool on = blockIn.Contains(s) && surface.position.y > blockAboveY;
            if (blocker.enabled != on) blocker.enabled = on;
        }
    }
}
