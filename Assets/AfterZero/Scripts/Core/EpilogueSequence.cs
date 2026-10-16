using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Plays the ending that matches the player's choices (L08 stability/evidence, L05 records/keepsake,
    /// all memory fragments), then rolls credits and returns to the main menu.
    /// </summary>
    public class EpilogueSequence : MonoBehaviour
    {
        public float startDelay = 6f;
        public GameObject stabilityScene;
        public GameObject evidenceScene;
        public List<SubtitleLine> stabilityLines = new List<SubtitleLine>();
        public List<SubtitleLine> evidenceLines = new List<SubtitleLine>();
        public List<SubtitleLine> recordsLines = new List<SubtitleLine>();
        public List<SubtitleLine> keepsakeLines = new List<SubtitleLine>();
        public List<SubtitleLine> allFragmentsLines = new List<SubtitleLine>();
        public List<SubtitleLine> allGroupsLines = new List<SubtitleLine>();
        public List<SubtitleLine> groupsLostLines = new List<SubtitleLine>();
        [TextArea(4, 20)] public string credits = "";

        bool Evidence => WorldState.Get("choice.L08.evidence");

        void Start()
        {
            if (stabilityScene != null) stabilityScene.SetActive(!Evidence);
            if (evidenceScene != null) evidenceScene.SetActive(Evidence);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(startDelay);
            var lines = new List<SubtitleLine>(Evidence ? evidenceLines : stabilityLines);
            if (WorldState.Get("choice.L05.records")) lines.AddRange(recordsLines);
            else if (WorldState.Get("choice.L05.keepsake")) lines.AddRange(keepsakeLines);
            int groups = (WorldState.Get("L07.group1Saved") ? 1 : 0) + (WorldState.Get("L07.group2Saved") ? 1 : 0) + (WorldState.Get("L07.group3Saved") ? 1 : 0);
            lines.AddRange(groups >= 3 ? allGroupsLines : groupsLostLines);
            if (MemoryFragment.Collected() >= MemoryFragment.Total) lines.AddRange(allFragmentsLines);

            float total = 0f;
            foreach (var l in lines) total += l.duration;
            Subtitles.PlaySequence(lines);
            yield return new WaitForSeconds(total + 1f);

            AudioCues.Play(Cue.Revelation);
            HUD.ShowRevelation(Evidence ? "The states begin to separate" : "The cycle holds, and remembers", credits);
            yield return new WaitForSeconds(8f);
            SaveSystem.Delete();
            GameFlow.LoadScene(LevelCatalog.MainMenu);
        }
    }
}
