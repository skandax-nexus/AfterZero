using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// L01 pressure: every time a Watcher Keeper reports an anomaly, the count rises; at the threshold the
    /// Keepers tighten the loop (30 → 24 → 18 s). The count clears each loop, and the loop never goes below
    /// the minimum (on Story difficulty it never tightens).
    /// </summary>
    public class LoopEscalation : MonoBehaviour
    {
        public int anomaliesPerStep = 3;
        public float step = 6f;
        public float minimumDuration = 18f;
        public string escalatedFlag = "L01.loopTightened";

        int count;

        void OnEnable() { KeeperPatrol.Anomaly += OnAnomaly; LoopManager.LoopRestarted += OnLoop; }
        void OnDisable() { KeeperPatrol.Anomaly -= OnAnomaly; LoopManager.LoopRestarted -= OnLoop; }

        void OnLoop(int n) { count = 0; }

        void OnAnomaly(KeeperPatrol k)
        {
            if (GameSettings.Difficulty == Difficulty.Story) return;
            count++;
            var lm = LoopManager.Instance;
            if (lm == null) return;
            HUD.Toast(k.displayName + " noticed you (" + count + "/" + anomaliesPerStep + ").", 2.5f);
            if (count < anomaliesPerStep || lm.loopDuration <= minimumDuration) return;
            count = 0;
            lm.loopDuration = Mathf.Max(minimumDuration, lm.loopDuration - step);
            WorldState.Set(escalatedFlag);
            AudioCues.Play(Cue.LoopReset);
            TemporalVisuals.Pulse(0.8f);
            HUD.Toast("The Keepers tighten the loop: " + Mathf.RoundToInt(lm.loopDuration) + " seconds.", 4f);
        }
    }
}
