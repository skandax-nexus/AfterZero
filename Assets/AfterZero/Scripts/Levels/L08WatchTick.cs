using UnityEngine;

namespace AfterZero
{
    /// <summary>Level 08: Arin's watch ("For A. - keep time for me. I.M.") starts ticking once a flag is set.</summary>
    public class L08WatchTick : MonoBehaviour
    {
        public string startFlag = "";
        public string setsFlag = "arin.watchTicking";
        public float interval = 1f;
        [Range(0f, 1f)] public float volume = 0.3f;
        [TextArea] public string toast = "Your watch has started ticking.";

        bool ticking;
        float t;

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }
        void Start() { if (WorldState.Get(startFlag)) Begin(false); }
        void OnFlag(string f, bool v) { if (v && f == startFlag) Begin(true); }

        void Begin(bool feedback)
        {
            if (ticking) return;
            ticking = true;
            WorldState.Set(setsFlag);
            if (!feedback) return;
            AudioCues.Play(Cue.RuleChange);
            AZHaptics.Pulse();
            if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 4f);
        }

        void Update()
        {
            if (!ticking) return;
            t += Time.deltaTime;
            if (t < interval) return;
            t = 0f;
            AudioCues.Play(Cue.Tick, volume);
        }
    }
}
