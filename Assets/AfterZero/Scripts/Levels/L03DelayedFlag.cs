using UnityEngine;

namespace AfterZero
{
    /// <summary>Sets a flag a few seconds after another flag (Ondine discovering the forged record).</summary>
    public class L03DelayedFlag : MonoBehaviour
    {
        public string whenFlag = "";
        public float delay = 8f;
        public string setFlag = "";
        public string toast = "";
        public Cue cue = Cue.Alert;

        float timer = -1f;

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }
        void OnFlag(string f, bool v) { if (v && f == whenFlag && timer < 0f && !WorldState.Get(setFlag)) timer = delay; }

        void Update()
        {
            if (timer < 0f) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = -1f;
            WorldState.Set(setFlag);
            AudioCues.Play(cue);
            if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 3.5f);
        }
    }
}
