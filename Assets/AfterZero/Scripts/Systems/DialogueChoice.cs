using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    [Serializable]
    public class DialogueOption
    {
        public string text = "";
        [Tooltip("Option is only selectable when all these flags are set (evidence). Missing evidence is shown.")]
        public List<string> requiresFlags = new List<string>();
        public string missingText = "(you have no proof)";
        public string setsFlag = "";
        public List<SubtitleLine> response = new List<SubtitleLine>();
    }

    /// <summary>
    /// Branching conversation: plays opening lines, then presents up to 4 options (tap or click).
    /// Options can require evidence flags gathered in earlier levels. Starts on interact (if the
    /// component sits on an object the player can aim at) or when <see cref="triggerFlag"/> is set.
    /// Chain several DialogueChoices through option flags to build longer scenes (the Tribunal).
    /// </summary>
    public class DialogueChoice : Interactable
    {
        public string speaker = "";
        public string triggerFlag = "";
        public List<SubtitleLine> opening = new List<SubtitleLine>();
        [TextArea] public string question = "";
        public List<DialogueOption> options = new List<DialogueOption>();
        public string doneFlag = "";
        public bool lockControls = true;

        bool running;
        bool done;

        void Awake() { if (prompt == "Inspect") prompt = string.IsNullOrEmpty(speaker) ? "Talk" : "Talk to " + speaker; }
        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }

        void OnFlag(string f, bool v) { if (v && f == triggerFlag) Begin(); }

        public override bool IsUsableNow => base.IsUsableNow && !running && !done && string.IsNullOrEmpty(triggerFlag);

        protected override void OnInteract(PlayerInteractor who) => Begin();

        public void Begin()
        {
            if (running || done) return;
            StartCoroutine(Run());
        }

        System.Collections.IEnumerator Run()
        {
            running = true;
            if (lockControls) GameInput.PushLock();
            float total = 0f;
            foreach (var l in opening) total += l.duration;
            if (opening.Count > 0) { Subtitles.PlaySequence(opening); yield return new WaitForSeconds(total); }

            int picked = -1;
            var labels = new string[options.Count];
            var enabled = new bool[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                bool ok = WorldState.All(options[i].requiresFlags);
                enabled[i] = ok;
                labels[i] = ok ? options[i].text : options[i].text + "  " + options[i].missingText;
            }
            HUD.ShowChoices(string.IsNullOrEmpty(question) ? speaker : question, labels, enabled, i => picked = i);
            while (picked < 0) yield return null;

            var o = options[picked];
            WorldState.Set(o.setsFlag);
            AudioCues.Play(Cue.Interact);
            total = 0f;
            foreach (var l in o.response) total += l.duration;
            if (o.response.Count > 0) { Subtitles.PlaySequence(o.response); yield return new WaitForSeconds(total); }
            if (lockControls) GameInput.PopLock();
            done = true;
            running = false;
            WorldState.Set(doneFlag);
        }
    }
}
