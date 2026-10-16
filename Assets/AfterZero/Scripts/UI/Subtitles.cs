using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Spoken lines. Every line is subtitled; barks only show when the speaker is near.</summary>
    public static class Subtitles
    {
        public static void PlaySequence(IList<SubtitleLine> lines, bool interrupt = true)
        {
            if (lines == null || lines.Count == 0) return;
            HUD.EnqueueSubtitles(lines, interrupt);
            var p = PlayerController.Instance;
            AudioCues.PlayAt(Cue.Murmur, p != null ? p.transform.position + p.transform.forward : Vector3.zero, 0.15f);
        }

        public static void Say(string speaker, string text, float duration = 3.5f, bool interrupt = true)
        {
            HUD.EnqueueSubtitles(new[] { new SubtitleLine(speaker, text, duration) }, interrupt);
        }

        /// <summary>Short ambient line from an NPC at a position (murmur audio + subtitle).</summary>
        public static void Bark(string speaker, string text, Vector3 position)
        {
            AudioCues.PlayAt(Cue.Murmur, position, 0.5f, Random.Range(0.85f, 1.2f));
            HUD.EnqueueSubtitles(new[] { new SubtitleLine(speaker, text, 2.8f) }, false);
        }
    }
}
