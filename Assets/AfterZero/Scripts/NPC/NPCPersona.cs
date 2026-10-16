using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    [Serializable]
    public class ConditionalLines
    {
        [Tooltip("Shown when this flag is set (e.g. a clue the player found).")]
        public string whenFlag = "";
        [Tooltip("...or when the player is carrying an object with this Grabbable key.")]
        public string whenHolding = "";
        public List<SubtitleLine> lines = new List<SubtitleLine>();
        public string setsFlag = "";
    }

    /// <summary>
    /// The conversational side of a named NPC. Covers the per-NPC checklist from the design:
    /// routine (NPCMotor), purposeful task (<see cref="task"/>), approach reaction, object-specific
    /// lines, and a changed state after the objective.
    /// </summary>
    [RequireComponent(typeof(NPCMotor))]
    public class NPCPersona : Interactable
    {
        [TextArea] public string task = "";
        [Header("Reaction when approached")]
        public List<string> approachBarks = new List<string>();
        [Header("Default conversation")]
        public List<SubtitleLine> talkLines = new List<SubtitleLine>();
        public string talkedFlag = "";
        [Header("Object-specific / clue-specific lines (first match wins)")]
        public List<ConditionalLines> conditional = new List<ConditionalLines>();
        [Header("Changed state after the objective")]
        public List<SubtitleLine> changedLines = new List<SubtitleLine>();

        NPCMotor motor;
        bool approachedThisLoop;
        float approachCooldown;

        void Awake()
        {
            motor = GetComponent<NPCMotor>();
            if (prompt == "Inspect" || string.IsNullOrEmpty(prompt)) prompt = "Talk to " + motor.displayName;
        }

        void Update()
        {
            approachCooldown -= Time.deltaTime;
            var p = PlayerController.Instance;
            if (p == null || approachBarks.Count == 0) return;
            float d = Vector3.Distance(p.transform.position, transform.position);
            if (d < motor.noticeRadius && !approachedThisLoop && approachCooldown <= 0f)
            {
                approachedThisLoop = true;
                approachCooldown = 25f;
                Subtitles.Bark(motor.displayName, approachBarks[UnityEngine.Random.Range(0, approachBarks.Count)], transform.position);
            }
            else if (d > motor.noticeRadius * 2.5f) approachedThisLoop = false;
        }

        protected override void OnInteract(PlayerInteractor who)
        {
            string held = who.Held != null ? who.Held.key : "";
            foreach (var c in conditional)
            {
                bool match = (!string.IsNullOrEmpty(c.whenHolding) && c.whenHolding == held) ||
                             (!string.IsNullOrEmpty(c.whenFlag) && string.IsNullOrEmpty(c.whenHolding) && WorldState.Get(c.whenFlag));
                if (!match) continue;
                Subtitles.PlaySequence(c.lines);
                WorldState.Set(c.setsFlag);
                WorldState.Set(talkedFlag);
                return;
            }

            if (!string.IsNullOrEmpty(motor.changedFlag) && WorldState.Get(motor.changedFlag) && changedLines.Count > 0)
                Subtitles.PlaySequence(changedLines);
            else
                Subtitles.PlaySequence(talkLines);
            WorldState.Set(talkedFlag);
        }
    }
}
