using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 06 interlock: whenever the drifting door slips to another anchor (because nobody was
    /// watching it), the nursery forgets the music boxes Arin has mended so far - unless the lullaby
    /// is already whole (lockFlag). Forces the player to keep the door in view while working.
    /// </summary>
    public class L06RoomForgets : MonoBehaviour
    {
        public Transform door;
        public List<string> forgetFlags = new List<string>();
        public string lockFlag = "";
        [TextArea] public string toast = "The door slipped away - and the room forgot the song.";

        Vector3 last;

        void Start() { if (door != null) last = door.position; }

        void Update()
        {
            if (door == null) return;
            if ((door.position - last).sqrMagnitude < 0.25f) return;
            last = door.position;
            if (WorldState.Get(lockFlag)) return;
            bool any = false;
            foreach (var f in forgetFlags) if (WorldState.Get(f)) { any = true; WorldState.Set(f, false); }
            if (!any) return;
            HUD.Toast(toast, 3f);
            AudioCues.Play(Cue.LoopReset);
            AZHaptics.Pulse();
        }
    }
}
