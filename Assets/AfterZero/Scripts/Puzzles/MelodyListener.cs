using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Plays the target melody of a SequencePuzzle (optionally with gaps where notes are missing).</summary>
    public class MelodyListener : Interactable
    {
        public SequencePuzzle puzzle;
        public bool bellLike = false;
        [Tooltip("Indices of notes that are silent in this recording (the 'missing' lullaby notes).")]
        public List<int> missingNotes = new List<int>();
        public string heardFlag = "";

        bool playing;

        void Awake() { if (prompt == "Inspect") prompt = "Listen"; }

        protected override void OnInteract(PlayerInteractor who)
        {
            if (playing || puzzle == null) return;
            StartCoroutine(Play());
        }

        System.Collections.IEnumerator Play()
        {
            playing = true;
            yield return puzzle.PlayDemo(bellLike, missingNotes);
            WorldState.Set(heardFlag);
            playing = false;
        }
    }
}
