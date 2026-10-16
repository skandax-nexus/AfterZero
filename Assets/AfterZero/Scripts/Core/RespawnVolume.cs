using UnityEngine;

namespace AfterZero
{
    /// <summary>Kill plane / hazard volume — respawns the player at the last checkpoint.</summary>
    [RequireComponent(typeof(Collider))]
    public class RespawnVolume : MonoBehaviour
    {
        [TextArea] public string message = "";

        void Start() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            player.Die(message);
        }
    }
}
