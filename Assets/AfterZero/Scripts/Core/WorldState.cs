using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Causal memory of the world. Flags survive state shifts and loop resets, which is what lets
    /// an action in the Past persist into the Present and Future ("causal persistence").
    /// Flags prefixed with "loop." are cleared each time the recurrence loop restarts.
    /// </summary>
    public static class WorldState
    {
        public const string LoopPrefix = "loop.";

        static readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();
        static readonly List<JournalEntry> journal = new List<JournalEntry>();

        public static event Action<string, bool> FlagChanged;
        public static event Action<JournalEntry> JournalAdded;

        public static IReadOnlyList<JournalEntry> Journal => journal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            flags.Clear();
            journal.Clear();
            FlagChanged = null;
            JournalAdded = null;
        }

        public static bool Get(string flag) => !string.IsNullOrEmpty(flag) && flags.TryGetValue(flag, out bool v) && v;

        public static void Set(string flag, bool value = true)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (flags.TryGetValue(flag, out bool old) && old == value) return;
            flags[flag] = value;
            FlagChanged?.Invoke(flag, value);
        }

        /// <summary>True when every flag in the list is set (an empty list counts as satisfied).</summary>
        public static bool All(IList<string> required)
        {
            if (required == null) return true;
            for (int i = 0; i < required.Count; i++)
                if (!string.IsNullOrEmpty(required[i]) && !Get(required[i])) return false;
            return true;
        }

        public static void ClearLoopFlags()
        {
            var toClear = new List<string>();
            foreach (var kv in flags)
                if (kv.Value && kv.Key.StartsWith(LoopPrefix, StringComparison.Ordinal)) toClear.Add(kv.Key);
            foreach (var key in toClear) Set(key, false);
        }

        public static void AddJournal(string id, string title, string body)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < journal.Count; i++)
                if (journal[i].id == id) return;
            var entry = new JournalEntry { id = id, title = title, body = body };
            journal.Add(entry);
            Set("clue." + id);
            JournalAdded?.Invoke(entry);
        }

        // ---------- snapshot / save ----------

        public static Snapshot Capture()
        {
            var s = new Snapshot();
            foreach (var kv in flags)
                if (kv.Value) s.flags.Add(kv.Key);
            s.journal.AddRange(journal);
            return s;
        }

        public static void Restore(Snapshot s)
        {
            if (s == null) return;
            var keep = new HashSet<string>(s.flags);
            var current = new List<string>(flags.Keys);
            foreach (var key in current)
                if (!keep.Contains(key)) Set(key, false);
            foreach (var key in s.flags) Set(key, true);
            journal.Clear();
            journal.AddRange(s.journal);
        }

        [Serializable]
        public class Snapshot
        {
            public List<string> flags = new List<string>();
            public List<JournalEntry> journal = new List<JournalEntry>();
        }
    }

    [Serializable]
    public class JournalEntry
    {
        public string id;
        public string title;
        [TextArea] public string body;
    }
}
