using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Cross-level evidence gate (e.g. L07: list from L04 + record from L03 + register from L05).
    /// When <see cref="triggerFlag"/> is set it checks the flags and sets pass or fail.
    /// </summary>
    public class EvidenceCheck : MonoBehaviour
    {
        public string triggerFlag = "";
        public List<string> requiredFlags = new List<string>();
        [Tooltip("How many of the required flags are enough (0 = all).")]
        public int minimum = 0;
        public string passFlag = "";
        public string failFlag = "";
        [TextArea] public string passToast = "";
        [TextArea] public string failToast = "";

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }
        void Start() { if (string.IsNullOrEmpty(triggerFlag)) Evaluate(); }

        void OnFlag(string f, bool v) { if (v && f == triggerFlag) Evaluate(); }

        public int Count()
        {
            int n = 0;
            foreach (var f in requiredFlags) if (WorldState.Get(f)) n++;
            return n;
        }

        public void Evaluate()
        {
            int need = minimum <= 0 ? requiredFlags.Count : minimum;
            bool pass = Count() >= need;
            WorldState.Set(pass ? passFlag : failFlag);
            string t = pass ? passToast : failToast;
            if (!string.IsNullOrEmpty(t)) HUD.Toast(t, 4f);
        }
    }
}
