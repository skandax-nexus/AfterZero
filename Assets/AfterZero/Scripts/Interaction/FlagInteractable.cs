using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// General-purpose "use this" object: levers, valves, wedges, letters, recordings, records.
    /// Sets a WorldState flag, optionally plays dialogue, records a clue and animates a part.
    /// </summary>
    public class FlagInteractable : Interactable, ILoopResettable
    {
        [Header("Effect")]
        public string setFlag = "";
        public bool toggle = false;
        public bool oneShot = true;
        [Tooltip("Reset (flag cleared, animation reversed) when the loop restarts. Use with loop.* flags.")]
        public bool resetOnLoop = false;
        public Cue cue = Cue.Interact;

        [Header("Clue (optional)")]
        public string clueId = "";
        public string clueTitle = "";
        [TextArea(2, 6)] public string clueText = "";
        [Tooltip("Show the clue text as a readable document panel.")]
        public bool showAsDocument = false;

        [Header("Dialogue (optional)")]
        public List<SubtitleLine> lines = new List<SubtitleLine>();

        [Header("Animation (optional)")]
        public Transform animateTarget;
        public Vector3 moveBy = Vector3.zero;
        public Vector3 rotateBy = Vector3.zero;
        [Min(0.05f)] public float animateDuration = 0.8f;

        [Header("After use")]
        public string usedPrompt = "";
        public GameObject disableAfterUse;

        bool used;
        bool animatedOn;
        Vector3 basePos;
        Quaternion baseRot;
        float animT;
        bool animating;
        bool animForward;

        void Awake()
        {
            if (animateTarget != null)
            {
                basePos = animateTarget.localPosition;
                baseRot = animateTarget.localRotation;
            }
        }

        void OnEnable() { LoopManager.Register(this); }
        void OnDisable() { LoopManager.Unregister(this); }

        public override bool IsUsableNow => base.IsUsableNow && !(oneShot && used && string.IsNullOrEmpty(usedPrompt));

        public override string GetPrompt(PlayerInteractor who) =>
            used && !string.IsNullOrEmpty(usedPrompt) ? usedPrompt : prompt;

        protected override void OnInteract(PlayerInteractor who)
        {
            if (used && oneShot)
            {
                // Re-reading: replay text only.
                if (showAsDocument && !string.IsNullOrEmpty(clueText)) HUD.ShowDocument(clueTitle, clueText);
                else if (lines.Count > 0) Subtitles.PlaySequence(lines);
                return;
            }

            used = true;
            if (toggle) WorldState.Set(setFlag, !WorldState.Get(setFlag));
            else WorldState.Set(setFlag, true);

            AudioCues.PlayAt(cue, transform.position);

            if (!string.IsNullOrEmpty(clueId)) WorldState.AddJournal(clueId, clueTitle, clueText);
            if (showAsDocument && !string.IsNullOrEmpty(clueText)) HUD.ShowDocument(clueTitle, clueText);
            if (lines.Count > 0) Subtitles.PlaySequence(lines);

            if (animateTarget != null)
            {
                animForward = toggle ? !animatedOn : true;
                animatedOn = animForward;
                animating = true;
            }

            if (disableAfterUse != null) disableAfterUse.SetActive(false);
        }

        void Update()
        {
            if (!animating || animateTarget == null) return;
            animT = Mathf.MoveTowards(animT, animForward ? 1f : 0f, Time.deltaTime / animateDuration);
            float e = Mathf.SmoothStep(0f, 1f, animT);
            animateTarget.localPosition = basePos + moveBy * e;
            animateTarget.localRotation = baseRot * Quaternion.Euler(rotateBy * e);
            if (Mathf.Approximately(animT, animForward ? 1f : 0f)) animating = false;
        }

        public void OnLoopReset()
        {
            if (!resetOnLoop) return;
            used = false;
            animatedOn = false;
            animT = 0f;
            animating = false;
            if (animateTarget != null)
            {
                animateTarget.localPosition = basePos;
                animateTarget.localRotation = baseRot;
            }
            if (disableAfterUse != null) disableAfterUse.SetActive(true);
        }
    }
}
