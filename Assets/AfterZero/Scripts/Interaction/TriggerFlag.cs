using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Sets a flag when the player (or an object with a matching Grabbable/Pushable tag name) enters.</summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerFlag : MonoBehaviour
    {
        public string setFlag = "";
        public List<string> requiredFlags = new List<string>();
        public TimeMask activeIn = TimeMask.All;
        [Tooltip("Empty = player. Otherwise the name of the object (or its root) that must enter, e.g. 'FestivalCart'.")]
        public string requireObjectNamed = "";
        public bool clearOnExit = false;
        public string toast = "";
        public List<SubtitleLine> lines = new List<SubtitleLine>();
        public bool oneShot = true;

        bool fired;

        void Start() { GetComponent<Collider>().isTrigger = true; }

        bool Matches(Collider other)
        {
            if (string.IsNullOrEmpty(requireObjectNamed))
                return other.GetComponentInParent<PlayerController>() != null;
            var t = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            return t.name == requireObjectNamed || t.root.name == requireObjectNamed;
        }

        void OnTriggerEnter(Collider other)
        {
            if (fired && oneShot) return;
            if (!activeIn.Contains(TemporalStateManager.CurrentState)) return;
            if (!WorldState.All(requiredFlags) || !Matches(other)) return;
            fired = true;
            WorldState.Set(setFlag, true);
            if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 3f);
            if (lines.Count > 0) Subtitles.PlaySequence(lines);
        }

        void OnTriggerExit(Collider other)
        {
            if (!clearOnExit || !Matches(other)) return;
            fired = false;
            WorldState.Set(setFlag, false);
        }
    }
}
