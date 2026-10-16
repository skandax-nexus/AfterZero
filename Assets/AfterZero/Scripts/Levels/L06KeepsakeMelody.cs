using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 06: Maren's music box (present only if the keepsake was saved in Level 05). When all
    /// flags are set (the lullaby solved), it plays the complete lullaby once, with no note missing.
    /// </summary>
    public class L06KeepsakeMelody : MonoBehaviour
    {
        public SequencePuzzle puzzle;
        public List<string> whenAll = new List<string>();
        public float delay = 1.2f;
        public string playedFlag = "";
        [TextArea] public string toast = "";

        bool played;
        float wait = -1f;

        void Update()
        {
            if (played || puzzle == null || !WorldState.All(whenAll)) return;
            if (wait < 0f) wait = delay;
            wait -= Time.deltaTime;
            if (wait > 0f) return;
            played = true;
            if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 4f);
            StartCoroutine(puzzle.PlayDemo(false, null));
            WorldState.Set(playedFlag);
        }
    }
}
