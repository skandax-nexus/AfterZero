using UnityEngine;

namespace AfterZero
{
    /// <summary>A slot that accepts a specific Grabbable (gear wedge slot, block cradle, zero niche).</summary>
    public class SnapSocket : Interactable
    {
        public string acceptsKey = "";
        public Transform anchor;
        [Tooltip("Flag set while the correct object sits in this socket.")]
        public string filledFlag = "";
        public bool lockWhenFilled = true;
        [TextArea] public string wrongItemMessage = "That does not fit here.";

        public Grabbable Occupant { get; private set; }

        void Awake()
        {
            if (anchor == null) anchor = transform;
            if (prompt == "Inspect") prompt = "Place";
        }

        public override bool IsUsableNow => base.IsUsableNow && Occupant == null && HeldByPlayer() != null;

        public override string GetPrompt(PlayerInteractor who)
        {
            var held = who.Held;
            return held != null ? prompt + " " + held.displayName : prompt;
        }

        static Grabbable HeldByPlayer()
        {
            var p = PlayerController.Instance;
            if (p == null) return null;
            var i = p.GetComponent<PlayerInteractor>();
            return i != null ? i.Held : null;
        }

        protected override void OnInteract(PlayerInteractor who)
        {
            var held = who.Held;
            if (held == null) return;
            if (!string.IsNullOrEmpty(acceptsKey) && held.key != acceptsKey)
            {
                HUD.Toast(wrongItemMessage, 2.5f);
                AudioCues.Play(Cue.Denied);
                return;
            }
            who.ReleaseHeld();
            Occupant = held;
            held.Socket = this;
            held.SnapTo(anchor);
            if (lockWhenFilled) held.enabled = false;
            AudioCues.PlayAt(Cue.Place, transform.position);
            WorldState.Set(filledFlag);
        }

        public void Release(Grabbable g)
        {
            if (Occupant != g) return;
            Occupant = null;
            g.Socket = null;
            WorldState.Set(filledFlag, false);
        }
    }
}
