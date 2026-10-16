using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Something that happens at a fixed moment inside the recurrence loop (the bell rings twice,
    /// a cart rolls, a cup breaks). If any <see cref="preventedBy"/> flag is set when the moment
    /// arrives, it does not happen and <see cref="preventedFlag"/> is set instead.
    /// With <see cref="persistsAcrossLoops"/>, the aftermath survives loop resets until the cause is removed —
    /// the anomaly the player notices first ("the broken cup stays broken").
    /// </summary>
    public class LoopScheduledEvent : MonoBehaviour, ILoopResettable
    {
        public float fireAt = 18f;
        public List<string> preventedBy = new List<string>();
        public TimeMask happensIn = TimeMask.Present;

        [Header("When it happens")]
        public List<GameObject> enableOnFire = new List<GameObject>();
        public List<GameObject> disableOnFire = new List<GameObject>();
        public string setFlagOnFire = "";
        public Cue cue = Cue.Shatter;
        public Transform cuePoint;
        public float noise = 0.8f;
        public List<SubtitleLine> lines = new List<SubtitleLine>();

        [Header("When prevented")]
        public string preventedFlag = "";
        [TextArea] public string preventedToast = "";

        [Header("Persistence")]
        public bool persistsAcrossLoops = false;
        public bool startFired = false;

        bool fired;
        bool resolvedThisLoop;

        void OnEnable() { LoopManager.Register(this); }
        void OnDisable() { LoopManager.Unregister(this); }

        void Start()
        {
            if (startFired) ApplyFired(true, false); else ApplyFired(false, false);
        }

        bool Prevented()
        {
            foreach (var f in preventedBy) if (WorldState.Get(f)) return true;
            return false;
        }

        void Update()
        {
            if (resolvedThisLoop || LoopManager.Instance == null || !LoopManager.Instance.Running) return;
            if (LoopManager.LoopTime < fireAt) return;
            resolvedThisLoop = true;

            if (Prevented())
            {
                if (!WorldState.Get(preventedFlag))
                {
                    WorldState.Set(preventedFlag);
                    AudioCues.Play(Cue.RuleChange);
                    if (!string.IsNullOrEmpty(preventedToast)) HUD.Toast(preventedToast, 4f);
                }
                return;
            }
            // The moment replays every loop (bell, crash, shout) even if its aftermath already persists.
            if (!fired) ApplyFired(true, true);
            else PlayFeedback();
        }

        void ApplyFired(bool value, bool withFeedback)
        {
            fired = value;
            foreach (var g in enableOnFire) if (g != null) g.SetActive(value);
            foreach (var g in disableOnFire) if (g != null) g.SetActive(!value);
            if (value) WorldState.Set(setFlagOnFire);
            if (value && withFeedback) PlayFeedback();
        }

        void PlayFeedback()
        {
            if (!happensIn.Contains(TemporalStateManager.CurrentState)) return;
            Vector3 p = cuePoint != null ? cuePoint.position : transform.position;
            AudioCues.PlayAt(cue, p, 1f);
            NoiseSystem.Emit(p, noise, gameObject);
            if (lines.Count > 0) Subtitles.PlaySequence(lines);
        }

        public void OnLoopReset()
        {
            resolvedThisLoop = false;
            if (!persistsAcrossLoops || Prevented()) ApplyFired(false, false);
        }
    }
}
