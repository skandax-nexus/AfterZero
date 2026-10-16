using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 03: water reaches channel k only when junctions 1..k all point the right way.
    /// Sets flagPrefix+k for each routed channel (used as extinguish flags for the burning stacks).
    /// </summary>
    public class L03ChannelFlags : MonoBehaviour
    {
        public List<RotatableElement> junctions = new List<RotatableElement>();
        public string flagPrefix = "L03.channel";
        public string toastPrefix = "Water runs past junction ";

        void OnEnable() { foreach (var j in junctions) if (j != null) j.Rotated += OnRotated; }
        void OnDisable() { foreach (var j in junctions) if (j != null) j.Rotated -= OnRotated; }
        void Start() => Evaluate(false);
        void OnRotated(RotatableElement e) => Evaluate(true);

        void Evaluate(bool feedback)
        {
            bool upstream = true;
            for (int i = 0; i < junctions.Count; i++)
            {
                upstream &= junctions[i] != null && junctions[i].IsCorrect;
                string f = flagPrefix + (i + 1);
                bool was = WorldState.Get(f);
                WorldState.Set(f, upstream);
                if (feedback && upstream && !was)
                {
                    AudioCues.PlayAt(Cue.WaterRush, junctions[i].transform.position, 0.6f);
                    HUD.Toast(toastPrefix + junctions[i].glyph + ".", 2f);
                }
            }
        }
    }
}
