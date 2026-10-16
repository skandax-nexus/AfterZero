using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Solved when every element is at its correct index (and the optional socket flags are set).</summary>
    public class AlignmentPuzzle : MonoBehaviour
    {
        public List<RotatableElement> elements = new List<RotatableElement>();
        public List<string> alsoRequire = new List<string>();
        public string solvedFlag = "";
        public bool lockWhenSolved = true;
        public string solvedToast = "";

        bool solved;

        void OnEnable()
        {
            foreach (var e in elements) if (e != null) e.Rotated += OnRotated;
            WorldState.FlagChanged += OnFlag;
        }

        void OnDisable()
        {
            foreach (var e in elements) if (e != null) e.Rotated -= OnRotated;
            WorldState.FlagChanged -= OnFlag;
        }

        void Start() => Check();
        void OnRotated(RotatableElement e) => Check();
        void OnFlag(string f, bool v) { if (alsoRequire.Contains(f)) Check(); }

        void Check()
        {
            if (solved && lockWhenSolved) return;
            bool ok = WorldState.All(alsoRequire);
            foreach (var e in elements) if (e == null || !e.IsCorrect) { ok = false; break; }
            if (ok == solved) return;
            solved = ok;
            WorldState.Set(solvedFlag, ok);
            if (ok)
            {
                AudioCues.Play(Cue.PuzzleSolved);
                if (!string.IsNullOrEmpty(solvedToast)) HUD.Toast(solvedToast, 3f);
                if (lockWhenSolved) foreach (var e in elements) if (e != null) e.enabled = false;
            }
        }
    }
}
