using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Optional collectible: a hologram shard holding a page from a citizen's life.
    /// 3 per level, 24 in total. Collecting all of them unlocks an extra Epilogue line.
    /// </summary>
    public class MemoryFragment : Interactable
    {
        public const int Total = 24;

        public string fragmentId = "L01.1";
        public string title = "";
        [TextArea(2, 6)] public string text = "";

        void Awake() { if (prompt == "Inspect") prompt = "Touch the memory fragment"; }

        void Start() { if (WorldState.Get(Flag)) gameObject.SetActive(false); }

        string Flag => "frag." + fragmentId;

        public static int Collected()
        {
            int n = 0;
            foreach (var e in WorldState.Journal) if (e.id.StartsWith("frag.", System.StringComparison.Ordinal)) n++;
            return n;
        }

        protected override void OnInteract(PlayerInteractor who)
        {
            WorldState.AddJournal(Flag, "Memory fragment: " + title, text);
            WorldState.Set(Flag);
            AudioCues.PlayAt(Cue.Memory, transform.position, 0.7f);
            TemporalVisuals.Pulse(0.35f);
            HUD.Toast(string.Format("Memory fragment {0}/{1}: {2}", Collected(), Total, title), 4f);
            gameObject.SetActive(false);
        }
    }
}
