using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Ordered input puzzle (missing lullaby notes, the bell sequence). Each SequenceButton plays a
    /// pitched tone; pressing them in <see cref="solution"/> order sets <see cref="solvedFlag"/>.
    /// </summary>
    public class SequencePuzzle : MonoBehaviour
    {
        public List<int> solution = new List<int>();
        public string solvedFlag = "";
        public List<string> requiredFlags = new List<string>();
        [Tooltip("Let the player hear the target melody by using the 'listen' source.")]
        public float demoNoteGap = 0.55f;
        public float[] notePitches = { 1f, 1.122f, 1.26f, 1.335f, 1.498f, 1.682f, 1.888f, 2f };

        readonly List<int> entered = new List<int>();
        bool solved;

        public bool Solved => solved;

        public void Press(SequenceButton button)
        {
            if (solved) return;
            if (!WorldState.All(requiredFlags)) { HUD.Toast("Nothing answers yet.", 2f); return; }
            AudioCues.PlayTone(PitchFor(button.noteIndex), button.transform.position, button.bellLike);
            entered.Add(button.noteIndex);

            int i = entered.Count - 1;
            if (i >= solution.Count || entered[i] != solution[i])
            {
                entered.Clear();
                if (button.noteIndex == (solution.Count > 0 ? solution[0] : -1)) entered.Add(button.noteIndex);
                else AudioCues.PlayAt(Cue.Denied, transform.position, 0.4f);
                return;
            }
            if (entered.Count == solution.Count)
            {
                solved = true;
                WorldState.Set(solvedFlag);
                AudioCues.Play(Cue.PuzzleSolved);
            }
        }

        public float PitchFor(int note) => notePitches[Mathf.Clamp(note, 0, notePitches.Length - 1)];

        public System.Collections.IEnumerator PlayDemo(bool bellLike, IList<int> skipIndices = null)
        {
            for (int i = 0; i < solution.Count; i++)
            {
                bool skip = skipIndices != null && skipIndices.Contains(i);
                if (!skip) AudioCues.PlayTone(PitchFor(solution[i]), transform.position, bellLike);
                yield return new WaitForSeconds(demoNoteGap);
            }
        }
    }
}
