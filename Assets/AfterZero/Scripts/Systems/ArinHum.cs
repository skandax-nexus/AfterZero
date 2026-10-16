using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Arin hums the Nursery lullaby (the only complete version: notes 1-3-5-4). Press H / HUM.
    /// HumReceivers nearby respond (music boxes, the Witness, children who recognise it).
    /// </summary>
    public static class ArinHum
    {
        public static event Action<Vector3> Hummed;
        public static readonly int[] Notes = { 0, 2, 4, 3 };
        static readonly float[] pitches = { 1f, 1.122f, 1.26f, 1.335f, 1.498f, 1.682f };
        static float last = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Hummed = null; last = -99f; }

        public static bool TryHum(MonoBehaviour host, Vector3 position)
        {
            if (Time.time - last < 3f) return false;
            last = Time.time;
            host.StartCoroutine(Play(position));
            return true;
        }

        static IEnumerator Play(Vector3 position)
        {
            Subtitles.Say("Arin", "(humming)  mm - mm - mm - mm", 2.4f, false);
            foreach (int n in Notes)
            {
                AudioCues.PlayTone(pitches[n] * 0.5f, position, false);
                yield return new WaitForSeconds(0.5f);
            }
            NoiseSystem.Emit(position, 0.3f, null);
            Hummed?.Invoke(position);
        }
    }

    /// <summary>Something that reacts when Arin hums nearby.</summary>
    public class HumReceiver : MonoBehaviour
    {
        public float radius = 6f;
        public List<string> requiredFlags = new List<string>();
        public string setsFlag = "";
        public List<SubtitleLine> lines = new List<SubtitleLine>();
        public bool oneShot = true;
        public TimeMask activeIn = TimeMask.All;

        bool fired;

        void OnEnable() { ArinHum.Hummed += OnHum; }
        void OnDisable() { ArinHum.Hummed -= OnHum; }

        void OnHum(Vector3 p)
        {
            if (fired && oneShot) return;
            if (!activeIn.Contains(TemporalStateManager.CurrentState)) return;
            if (Vector3.Distance(p, transform.position) > radius || !WorldState.All(requiredFlags)) return;
            fired = true;
            WorldState.Set(setsFlag);
            AudioCues.PlayAt(Cue.RuleChange, transform.position);
            if (lines.Count > 0) Subtitles.PlaySequence(lines, false);
        }
    }
}
