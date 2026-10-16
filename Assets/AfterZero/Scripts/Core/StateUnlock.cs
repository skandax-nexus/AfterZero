using UnityEngine;

namespace AfterZero
{
    /// <summary>Adds states to the level's allowed set once a flag is set (introduce a new state after observation).</summary>
    public class StateUnlock : MonoBehaviour
    {
        public string whenFlag = "";
        public TimeMask unlock = TimeMask.Past;
        [TextArea] public string toast = "";

        bool done;

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }
        void Start() { if (WorldState.Get(whenFlag)) Apply(false); }

        void OnFlag(string f, bool v) { if (v && f == whenFlag) Apply(true); }

        void Apply(bool feedback)
        {
            if (done) return;
            var tsm = TemporalStateManager.Instance;
            if (tsm == null) return;
            done = true;
            tsm.allowedStates |= unlock;
            if (!feedback) return;
            AudioCues.Play(Cue.RuleChange);
            TemporalVisuals.Pulse(0.6f);
            if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 5f);
        }
    }
}
