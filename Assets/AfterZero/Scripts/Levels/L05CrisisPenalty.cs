using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 05: if Arin lied to Mother Ondine, the Keepers hunt the basin. Once the flood timer is
    /// running this removes a one-off chunk of time, and more each time the Keepers catch Arin
    /// (respawn) while the hunt is on. Uses CrisisTimer.Penalize.
    /// </summary>
    public class L05CrisisPenalty : MonoBehaviour
    {
        public CrisisTimer timer;
        public string whenFlag = "L05.keepersHunt";
        public float initialPenalty = 25f;
        public float perCatch = 10f;
        [TextArea] public string toast = "The Keepers are in the basin. The flood will not wait for you to hide.";

        bool applied;

        void OnEnable() { Checkpoint.Respawned += OnRespawn; }
        void OnDisable() { Checkpoint.Respawned -= OnRespawn; }

        void Update()
        {
            if (applied || timer == null || !timer.Running || !WorldState.Get(whenFlag)) return;
            applied = true;
            timer.Penalize(initialPenalty);
            if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 4f);
        }

        void OnRespawn()
        {
            if (timer != null && timer.Running && WorldState.Get(whenFlag)) timer.Penalize(perCatch);
        }
    }
}
