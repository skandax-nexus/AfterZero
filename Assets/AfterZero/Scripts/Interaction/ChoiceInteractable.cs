using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// One side of a meaningful trade-off (records vs keepsake, stability vs evidence).
    /// Choosing sets this option's flag plus the shared group flag and removes the alternatives.
    /// Choices are saved and can be read by later levels ("choice.*" flags).
    /// </summary>
    public class ChoiceInteractable : Interactable
    {
        public string groupFlag = "choice.L05.made";
        public string optionFlag = "choice.L05.records";
        [TextArea] public string confirmText = "Take the records? The other will be lost.";
        public List<GameObject> alternatives = new List<GameObject>();
        public List<SubtitleLine> lines = new List<SubtitleLine>();

        public override bool IsUsableNow => base.IsUsableNow && !WorldState.Get(groupFlag);

        protected override void OnInteract(PlayerInteractor who)
        {
            HUD.Confirm(confirmText, Choose, null);
        }

        void Choose()
        {
            if (WorldState.Get(groupFlag)) return;
            WorldState.Set(optionFlag);
            WorldState.Set(groupFlag);
            AudioCues.PlayAt(Cue.RuleChange, transform.position);
            foreach (var a in alternatives) if (a != null) a.SetActive(false);
            if (lines.Count > 0) Subtitles.PlaySequence(lines);
        }
    }
}
