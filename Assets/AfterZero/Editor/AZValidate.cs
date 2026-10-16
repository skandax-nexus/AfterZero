using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// AfterZero ▸ Validate Levels — opens every generated level and checks it against the design
    /// document's "NPC life and implementation" and "Readability and QA" requirements.
    /// </summary>
    public static class AZValidate
    {
        [MenuItem("AfterZero/Validate Levels", priority = 50)]
        public static void ValidateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var report = new StringBuilder();
            int problems = 0;
            for (int i = 0; i < LevelCatalog.Scenes.Length; i++)
            {
                string path = AZBuild.SceneFolder + "/" + LevelCatalog.Scenes[i] + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    report.AppendLine("L" + (i + 1) + ": scene missing — run Build All Levels.");
                    problems++;
                    continue;
                }
                EditorSceneManager.OpenScene(path);
                var issues = ValidateOpenScene();
                problems += issues.Count;
                report.AppendLine(string.Format("L{0:00} {1}: {2}", i + 1, LevelCatalog.Titles[i], issues.Count == 0 ? "OK" : issues.Count + " issue(s)"));
                foreach (var s in issues) report.AppendLine("    - " + s);
            }
            if (problems == 0) Debug.Log("[AfterZero] Validation passed.\n" + report);
            else Debug.LogWarning("[AfterZero] Validation found " + problems + " issue(s).\n" + report);
        }

        public static List<string> ValidateOpenScene()
        {
            var issues = new List<string>();

            var director = Object.FindFirstObjectByType<LevelDirector>();
            if (director == null) issues.Add("No LevelDirector.");
            else
            {
                if (director.objectives.Count < 3) issues.Add("Fewer than 3 objectives (one per playable beat expected).");
                foreach (var o in director.objectives)
                    if (string.IsNullOrEmpty(o.completeFlag)) issues.Add("Objective without flag: " + o.text);
                if (string.IsNullOrEmpty(director.payoffFlag) || string.IsNullOrEmpty(director.payoffClueText)) issues.Add("Clue and payoff not configured.");
            }

            if (Object.FindFirstObjectByType<TemporalStateManager>() == null) issues.Add("No TemporalStateManager.");
            if (Object.FindFirstObjectByType<TemporalVisuals>() == null) issues.Add("No TemporalVisuals.");
            if (Object.FindFirstObjectByType<PlayerController>() == null) issues.Add("No player.");
            if (Object.FindFirstObjectByType<LevelExit>() == null) issues.Add("No LevelExit.");

            var checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            bool start = false;
            foreach (var c in checkpoints) start |= c.isLevelStart;
            if (!start) issues.Add("No level-start checkpoint.");
            if (checkpoints.Length < 2) issues.Add("Only " + checkpoints.Length + " checkpoint(s); add one before long sequences.");

            if (Object.FindObjectsByType<AmbientZone>(FindObjectsSortMode.None).Length == 0) issues.Add("No local ambient audio.");

            var personas = Object.FindObjectsByType<NPCPersona>(FindObjectsSortMode.None);
            if (personas.Length < 2) issues.Add("Fewer than 2 named NPCs.");
            foreach (var p in personas)
            {
                var m = p.GetComponent<NPCMotor>();
                string n = m != null ? m.displayName : p.name;
                if (m == null || m.routine.Count < 2) issues.Add(n + ": no daily routine.");
                if (string.IsNullOrEmpty(p.task)) issues.Add(n + ": no purposeful task.");
                if (p.approachBarks.Count == 0) issues.Add(n + ": no reaction when approached.");
                if (p.conditional.Count == 0) issues.Add(n + ": no object-specific line.");
                if (m == null || string.IsNullOrEmpty(m.changedFlag) || p.changedLines.Count == 0) issues.Add(n + ": no changed state after the objective.");
                if (p.talkLines.Count == 0) issues.Add(n + ": no default conversation.");
            }

            int background = 0;
            foreach (var m in Object.FindObjectsByType<NPCMotor>(FindObjectsSortMode.None))
                if (m.GetComponent<NPCPersona>() == null) background++;
            if (background == 0) issues.Add("No background NPCs.");

            foreach (var h in Object.FindObjectsByType<TelegraphedHazard>(FindObjectsSortMode.None))
                if (h.warnLight == null && h.warnParticles == null && h.shakeTarget == null)
                    issues.Add("Hazard '" + h.name + "' has no telegraph (light, particles or motion).");

            foreach (var e in Object.FindObjectsByType<LevelExit>(FindObjectsSortMode.None))
                if (e.requiredFlags.Count > 0 && string.IsNullOrEmpty(e.lockedMessage))
                    issues.Add("Exit '" + e.name + "' is locked without explaining why.");

            if (Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Length == 0)
                issues.Add("No world text/glyphs: critical clues may be colour-only.");

            return issues;
        }
    }
}
