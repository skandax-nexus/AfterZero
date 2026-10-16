using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    [Serializable]
    public class Objective
    {
        [Tooltip("Shown in the HUD while this objective is active.")]
        public string text;
        [Tooltip("WorldState flag that completes this objective.")]
        public string completeFlag;
        [Tooltip("Optional hint shown after the player has been on this objective for a while.")]
        public string hint;
    }

    [Serializable]
    public class SubtitleLine
    {
        public string speaker;
        [TextArea] public string text;
        [Min(0.5f)] public float duration = 3.5f;

        public SubtitleLine() { }
        public SubtitleLine(string speaker, string text, float duration = 3.5f)
        {
            this.speaker = speaker; this.text = text; this.duration = duration;
        }
    }

    /// <summary>
    /// One per level scene. Tracks the playable beats as an ordered list of objectives,
    /// shows the title card, and announces the clue/payoff when the level resolves.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class LevelDirector : MonoBehaviour
    {
        public static LevelDirector Instance { get; private set; }
        public static event Action<Objective> ObjectiveChanged;

        public int levelIndex;
        public string levelTitle;
        public string location;
        [TextArea] public string storySetup;

        public List<Objective> objectives = new List<Objective>();
        public List<SubtitleLine> introLines = new List<SubtitleLine>();

        [Header("Interlude (story scene played before the title card)")]
        public string interludeTitle = "";
        public List<SubtitleLine> interludeLines = new List<SubtitleLine>();

        [Header("Clue and payoff")]
        public string payoffFlag;
        public string payoffClueTitle;
        [TextArea] public string payoffClueText;

        [Min(10f)] public float hintDelay = 90f;

        public Objective Current { get; private set; }
        float currentSince;
        bool hintShown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { ObjectiveChanged = null; Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable() { WorldState.FlagChanged += OnFlagChanged; }
        void OnDisable() { WorldState.FlagChanged -= OnFlagChanged; }

        IEnumerator Start()
        {
            GameFlow.MarkLevelStarted();
            Refresh();
            if (interludeLines.Count > 0)
            {
                GameInput.PushLock();
                TemporalVisuals.SetMemoryMode(true);
                HUD.ShowTitleCard("INTERLUDE", interludeTitle, "");
                yield return new WaitForSeconds(3.6f);
                float total = 0f;
                foreach (var l in interludeLines) total += l.duration;
                Subtitles.PlaySequence(interludeLines);
                yield return new WaitForSeconds(total + 0.4f);
                TemporalVisuals.SetMemoryMode(false);
                GameInput.PopLock();
            }
            string small = levelIndex == LevelCatalog.PrologueIndex ? "PROLOGUE"
                         : levelIndex == LevelCatalog.EpilogueIndex ? "EPILOGUE"
                         : string.Format("LEVEL {0:00}", levelIndex + 1);
            HUD.ShowTitleCard(small, levelTitle, location);
            yield return new WaitForSeconds(3.5f);
            if (introLines.Count > 0) Subtitles.PlaySequence(introLines);
        }

        void Update()
        {
            if (Current == null || hintShown || string.IsNullOrEmpty(Current.hint)) return;
            if (Time.time - currentSince > Mathf.Min(hintDelay, GameSettings.HintDelay) && !float.IsInfinity(GameSettings.HintDelay))
            {
                hintShown = true;
                HUD.Toast("Hint: " + Current.hint, 6f);
            }
        }

        void OnFlagChanged(string flag, bool value)
        {
            if (!value) return;
            if (Current != null && flag == Current.completeFlag) AudioCues.Play(Cue.ObjectiveComplete);
            if (!string.IsNullOrEmpty(payoffFlag) && flag == payoffFlag && !string.IsNullOrEmpty(payoffClueTitle))
            {
                WorldState.AddJournal("payoff.L" + (levelIndex + 1), payoffClueTitle, payoffClueText);
                HUD.ShowRevelation(payoffClueTitle, payoffClueText);
            }
            Refresh();
        }

        void Refresh()
        {
            Objective next = null;
            foreach (var o in objectives)
            {
                if (!WorldState.Get(o.completeFlag)) { next = o; break; }
            }
            if (next == Current) return;
            Current = next;
            currentSince = Time.time;
            hintShown = false;
            ObjectiveChanged?.Invoke(Current);
            HUD.SetObjective(Current != null ? Current.text : "");
        }

        // Phones suspend apps at any moment: keep progress (restart of this level) when sent to background.
        void OnApplicationPause(bool paused)
        {
            if (paused && levelIndex >= 0 && levelIndex < LevelCatalog.Scenes.Length) SaveSystem.Save(levelIndex);
        }

        public void Complete()
        {
            GameFlow.CompleteLevel(levelIndex);
        }
    }
}
