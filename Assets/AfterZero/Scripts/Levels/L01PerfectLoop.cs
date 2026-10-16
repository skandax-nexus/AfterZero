using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// The L01 climax: sets <see cref="perfectFlag"/> (a loop.* flag) only when every required loop flag is
    /// set in the same loop, announces progress, and at the report moment explains which part failed.
    /// </summary>
    public class L01PerfectLoop : MonoBehaviour
    {
        public List<string> required = new List<string>();
        public List<string> labels = new List<string>();
        [Tooltip("The loop is spoiled if any of these is set (e.g. Lise already ran this loop).")]
        public List<string> forbidden = new List<string>();
        public string perfectFlag = "loop.L01.perfectLoop";
        [Tooltip("Only give feedback once the player knows about the climax.")]
        public string activeAfterFlag = "";
        public string doneFlag = "";
        public float baseReportTime = 18.3f;

        bool reported;

        void OnEnable() { WorldState.FlagChanged += OnFlag; LoopManager.LoopRestarted += OnLoop; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; LoopManager.LoopRestarted -= OnLoop; }

        void OnLoop(int n) { reported = false; }

        int Count()
        {
            int c = 0;
            foreach (var f in required) if (WorldState.Get(f)) c++;
            return c;
        }

        void OnFlag(string f, bool v)
        {
            if (!v || WorldState.Get(doneFlag)) return;
            if (forbidden.Contains(f) && WorldState.Get(activeAfterFlag)) { HUD.Toast("Lise reached the stall. This loop is spoiled.", 3f); return; }
            if (!required.Contains(f)) return;
            int c = Count();
            bool spoiled = false;
            foreach (var x in forbidden) if (WorldState.Get(x)) spoiled = true;
            if (c == required.Count && !spoiled) WorldState.Set(perfectFlag);
            if (WorldState.Get(activeAfterFlag))
            {
                int i = required.IndexOf(f);
                string what = i >= 0 && i < labels.Count ? labels[i] : f;
                HUD.Toast("Perfect loop " + c + "/" + required.Count + ": " + what, 2.5f);
                AudioCues.Play(c == required.Count ? Cue.PuzzleSolved : Cue.Click);
            }
        }

        void Update()
        {
            if (reported || WorldState.Get(doneFlag) || !WorldState.Get(activeAfterFlag)) return;
            var lm = LoopManager.Instance;
            if (lm == null || !lm.Running) return;
            if (LoopManager.LoopTime < baseReportTime * L01LoopTimeline.Scale) return;
            reported = true;
            if (WorldState.Get(perfectFlag)) return;
            var missing = new List<string>();
            for (int i = 0; i < required.Count; i++)
                if (!WorldState.Get(required[i])) missing.Add(i < labels.Count ? labels[i] : required[i]);
            HUD.Toast("Not this loop. Still missing: " + string.Join(", ", missing.ToArray()) + ".", 4.5f);
        }
    }
}
