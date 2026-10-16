using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Calder's power cut: when <see cref="cutFlag"/> is set the Past clockwork stops (<see cref="powerOutFlag"/> = true);
    /// when <see cref="restartFlag"/> is set it runs again (powerOut cleared, <see cref="restartedFlag"/> set).
    /// </summary>
    public class L02PowerRelay : MonoBehaviour
    {
        public string cutFlag = "";
        public string restartFlag = "";
        public string powerOutFlag = "L02.powerOut";
        public string restartedFlag = "";

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }

        void OnFlag(string f, bool v)
        {
            if (!v) return;
            if (f == cutFlag && !WorldState.Get(restartFlag))
            {
                WorldState.Set(powerOutFlag, true);
                AudioCues.Play(Cue.RuleChange);
                HUD.Toast("The clockwork winds down. Calder has cut the power.", 3.5f);
            }
            else if (f == restartFlag && WorldState.Get(cutFlag))
            {
                WorldState.Set(powerOutFlag, false);
                WorldState.Set(restartedFlag);
                AudioCues.Play(Cue.PuzzleSolved);
                HUD.Toast("The PAST clockwork turns again. The counterweight lift is moving.", 3.5f);
            }
        }
    }
}
