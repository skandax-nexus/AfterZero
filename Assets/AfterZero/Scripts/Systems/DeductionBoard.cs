using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Pick the one right answer out of several candidates using clues found in the level (no direct hint).
    /// Wrong answers: Story = free retry, Normal = penalty (time off a crisis timer, or a noise alarm),
    /// Hard = the puzzle resets (listed clue flags are cleared so the player must re-check them).
    /// </summary>
    public class DeductionBoard : Interactable
    {
        [TextArea] public string question = "Which record is the real one?";
        public List<string> candidates = new List<string>();
        public int correctIndex;
        public string solvedFlag = "";
        public List<SubtitleLine> solvedLines = new List<SubtitleLine>();
        [Header("Wrong answers")]
        public CrisisTimer penaltyTimer;
        public float penaltySeconds = 10f;
        public bool alarmOnWrong = true;
        public List<string> resetFlagsOnHard = new List<string>();
        [TextArea] public string wrongMessage = "That doesn't fit the evidence.";

        bool solved;

        void Awake() { if (prompt == "Inspect") prompt = "Study the evidence"; }

        public override bool IsUsableNow => base.IsUsableNow && !solved;

        protected override void OnInteract(PlayerInteractor who)
        {
            var en = new bool[candidates.Count];
            for (int i = 0; i < en.Length; i++) en[i] = true;
            GameInput.PushLock();
            HUD.ShowChoices(question, candidates.ToArray(), en, Pick);
        }

        void Pick(int i)
        {
            GameInput.PopLock();
            if (i == correctIndex)
            {
                solved = true;
                WorldState.Set(solvedFlag);
                AudioCues.Play(Cue.PuzzleSolved);
                if (solvedLines.Count > 0) Subtitles.PlaySequence(solvedLines);
                return;
            }
            AudioCues.Play(Cue.Denied);
            AZHaptics.Pulse();
            switch (GameSettings.Difficulty)
            {
                case Difficulty.Story:
                    HUD.Toast(wrongMessage, 3f);
                    break;
                case Difficulty.Normal:
                    HUD.Toast(wrongMessage + (penaltyTimer != null ? "  (-" + penaltySeconds + "s)" : ""), 3f);
                    if (penaltyTimer != null) penaltyTimer.Penalize(penaltySeconds);
                    if (alarmOnWrong) NoiseSystem.Emit(transform.position, 1.2f, gameObject);
                    break;
                default:
                    HUD.Toast(wrongMessage + "  The board resets - check the clues again.", 4f);
                    foreach (var f in resetFlagsOnHard) WorldState.Set(f, false);
                    if (penaltyTimer != null) penaltyTimer.Penalize(penaltySeconds);
                    break;
            }
        }
    }
}
