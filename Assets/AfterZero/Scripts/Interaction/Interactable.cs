using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Base for anything the player can use with the Interact button.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>Fired whenever the player successfully uses any interactable (watchers treat it as an anomaly).</summary>
        public static event System.Action<Interactable> AnyInteracted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { AnyInteracted = null; }

        [Tooltip("Verb shown in the prompt, e.g. 'Read letter'.")]
        public string prompt = "Inspect";
        public TimeMask usableIn = TimeMask.All;
        public List<string> requiredFlags = new List<string>();
        [TextArea] public string lockedMessage = "";

        public virtual bool IsUsableNow =>
            isActiveAndEnabled && usableIn.Contains(TemporalStateManager.CurrentState);

        public virtual bool RequirementsMet => WorldState.All(requiredFlags);

        public virtual string GetPrompt(PlayerInteractor who) => prompt;

        /// <summary>Called when the player presses Interact while aiming at this.</summary>
        public void TryInteract(PlayerInteractor who)
        {
            if (!IsUsableNow) return;
            if (!RequirementsMet)
            {
                if (!string.IsNullOrEmpty(lockedMessage)) HUD.Toast(lockedMessage, 3f);
                AudioCues.Play(Cue.Denied);
                return;
            }
            OnInteract(who);
            AnyInteracted?.Invoke(this);
        }

        protected abstract void OnInteract(PlayerInteractor who);
    }
}
