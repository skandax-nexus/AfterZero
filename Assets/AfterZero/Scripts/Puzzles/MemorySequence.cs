using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// A short playable memory / recording (the clockkeeper's recording, The Witness, Arin's memory).
    /// Locks controls, tints the world, plays lines, then sets a flag.
    /// </summary>
    public class MemorySequence : MonoBehaviour
    {
        public string triggerFlag = "";
        public List<SubtitleLine> lines = new List<SubtitleLine>();
        public string completeFlag = "";
        public bool lockControls = true;
        public GameObject showDuring;
        public Cue startCue = Cue.Memory;

        bool played;

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }

        void Start()
        {
            if (showDuring != null) showDuring.SetActive(false);
            if (string.IsNullOrEmpty(triggerFlag)) Play();
        }

        void OnFlag(string f, bool v) { if (v && f == triggerFlag) Play(); }

        public void Play()
        {
            if (played) return;
            played = true;
            StartCoroutine(Run());
        }

        System.Collections.IEnumerator Run()
        {
            if (lockControls) GameInput.PushLock();
            AudioCues.Play(startCue);
            TemporalVisuals.SetMemoryMode(true);
            if (showDuring != null) showDuring.SetActive(true);
            float total = 0f;
            foreach (var l in lines) total += l.duration;
            Subtitles.PlaySequence(lines);
            yield return new WaitForSeconds(total + 0.5f);
            if (showDuring != null) showDuring.SetActive(false);
            TemporalVisuals.SetMemoryMode(false);
            if (lockControls) GameInput.PopLock();
            WorldState.Set(completeFlag);
        }
    }
}
