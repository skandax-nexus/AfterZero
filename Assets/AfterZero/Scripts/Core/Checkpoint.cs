using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Trigger volume. The latest one touched is where the player returns after a fall or hazard.</summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        public static Checkpoint Active { get; private set; }
        /// <summary>Fired after the player is put back at a checkpoint (chasers and fires reset on this).</summary>
        public static event System.Action Respawned;

        [Tooltip("Climax checkpoints are removed on Hard difficulty.")]
        public bool climax;

        public Transform spawnPoint;
        [Tooltip("State the player is returned to on respawn.")]
        public TimeState respawnState = TimeState.Present;
        public bool isLevelStart;

        static Vector3 fallbackPos;
        static Quaternion fallbackRot;
        static bool hasFallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = null; hasFallback = false; Respawned = null; }

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void Start()
        {
            if (climax && !GameSettings.ClimaxCheckpoints) { gameObject.SetActive(false); return; }
            GetComponent<Collider>().isTrigger = true;
            if (isLevelStart && Active == null) Active = this;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Active == this || other.GetComponentInParent<PlayerController>() == null) return;
            Active = this;
            respawnState = TemporalStateManager.CurrentState;
            AudioCues.Play(Cue.Checkpoint);
            HUD.Toast("Checkpoint", 1.5f);
        }

        public static void SetFallback(Vector3 pos, Quaternion rot)
        {
            fallbackPos = pos; fallbackRot = rot; hasFallback = true;
        }

        public static void RespawnPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            if (Active != null)
            {
                var t = Active.spawnPoint != null ? Active.spawnPoint : Active.transform;
                if (TemporalStateManager.Instance != null) TemporalStateManager.Instance.ForceState(Active.respawnState);
                player.Teleport(t.position, t.rotation);
            }
            else if (hasFallback)
            {
                player.Teleport(fallbackPos, fallbackRot);
            }
            Respawned?.Invoke();
        }
    }
}
