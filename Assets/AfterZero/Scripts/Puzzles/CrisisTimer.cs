using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Countdown for the bell catastrophe set-piece. Starts when <see cref="startFlag"/> is set,
    /// stops when <see cref="successFlag"/> is set. On expiry the catastrophe plays out and the
    /// level restarts from its opening state (the cycle repeats).
    /// </summary>
    public class CrisisTimer : MonoBehaviour
    {
        public float duration = 300f;
        public string startFlag = "";
        public string successFlag = "";
        [TextArea] public string failMessage = "The bell rings a second time. The cycle begins again.";
        [Tooltip("Optional: seconds removed when a hazard catches the player.")]
        public float penaltyOnDeath = 0f;
        [TextArea] public string startMessage = "The bell has rung once. Evacuate the square before it rings again.";
        public string timerLabel = "SECOND BELL IN";

        public float Remaining { get; private set; }
        public bool Running { get; private set; }
        bool finished;

        void Start()
        {
            duration *= GameSettings.TimerScale;
            Remaining = duration;
            if (string.IsNullOrEmpty(startFlag)) Begin();
        }

        /// <summary>Remove time (wrong answers, getting caught). Public so other systems don't need reflection.</summary>
        public void Penalize(float seconds)
        {
            if (!Running) return;
            Remaining = Mathf.Max(1f, Remaining - seconds);
            AudioCues.Play(Cue.Tick);
        }

        void Begin()
        {
            Running = true;
            AudioCues.Play(Cue.Bell);
            if (!string.IsNullOrEmpty(startMessage)) HUD.Toast(startMessage, 5f);
            HUD.TimerLabel = timerLabel;
        }

        void Update()
        {
            if (finished) return;
            if (!Running)
            {
                if (WorldState.Get(startFlag)) Begin();
                HUD.SetTimer(-1f);
                return;
            }
            if (WorldState.Get(successFlag))
            {
                finished = true;
                Running = false;
                HUD.SetTimer(-1f);
                return;
            }

            Remaining -= Time.deltaTime;
            HUD.SetTimer(Remaining);
            if (Remaining <= 30f && Mathf.FloorToInt(Remaining + Time.deltaTime) != Mathf.FloorToInt(Remaining))
                AudioCues.Play(Cue.Tick);
            if (Remaining <= 0f)
            {
                finished = true;
                StartCoroutine(Fail());
            }
        }

        System.Collections.IEnumerator Fail()
        {
            HUD.SetTimer(-1f);
            AudioCues.Play(Cue.Bell);
            TemporalVisuals.Pulse(2f);
            HUD.Toast(failMessage, 4f);
            yield return new WaitForSeconds(3f);
            GameFlow.RestartLevel();
        }
    }
}
