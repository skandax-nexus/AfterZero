using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Level exit. Locked until all required flags are set; tells the player why if not.</summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour
    {
        public List<string> requiredFlags = new List<string>();
        [TextArea] public string lockedMessage = "The way forward is still sealed.";
        bool used;

        void Start() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (used || other.GetComponentInParent<PlayerController>() == null) return;
            if (!WorldState.All(requiredFlags))
            {
                HUD.Toast(lockedMessage, 3f);
                AudioCues.Play(Cue.Denied);
                return;
            }
            used = true;
            AudioCues.Play(Cue.LevelExit);
            if (LevelDirector.Instance != null) LevelDirector.Instance.Complete();
            else GameFlow.LoadScene(LevelCatalog.MainMenu);
        }
    }
}
