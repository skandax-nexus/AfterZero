using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Pell has two "changed" routines: a per-loop diversion (talk to him and he takes the long way round)
    /// and, once the cup is saved, the walk up to the tower. This swaps the motor's changed routine and the
    /// persona's changed lines when <see cref="finalFlag"/> is set.
    /// </summary>
    public class L01PellSwitch : MonoBehaviour
    {
        public NPCMotor motor;
        public NPCPersona persona;
        public string finalFlag = "";
        public List<RoutineStep> finalRoutine = new List<RoutineStep>();
        public List<SubtitleLine> finalLines = new List<SubtitleLine>();
        bool applied;

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }
        void Start() { if (WorldState.Get(finalFlag)) Apply(); }
        void OnFlag(string f, bool v) { if (v && f == finalFlag) Apply(); }

        void Apply()
        {
            if (applied || motor == null) return;
            applied = true;
            // Keep the list length so the motor's current step index stays valid.
            var list = motor.changedRoutine;
            for (int i = 0; i < list.Count && i < finalRoutine.Count; i++) list[i] = finalRoutine[i];
            motor.changedFlag = finalFlag;
            if (persona != null && finalLines.Count > 0)
            {
                persona.changedLines.Clear();
                persona.changedLines.AddRange(finalLines);
            }
        }
    }
}
