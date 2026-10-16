using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Moves along waypoints and back (ghost trains, swinging counterweights). Headlight, horn
    /// and rail vibration warn ahead of it; touching the front hurts.
    /// </summary>
    public class MovingHazard : MonoBehaviour
    {
        public List<Transform> waypoints = new List<Transform>();
        public float speed = 9f;
        public float pauseAtEnds = 3f;
        public bool pingPong = true;
        public TimeMask activeIn = TimeMask.All;
        public string stoppedByFlag = "";
        public float warnDistance = 18f;
        public Light headlight;
        [TextArea] public string deathMessage = "Feel the rails: the pulse arrives before the train.";

        int index;
        int dir = 1;
        float pause;
        float hornCooldown;
        Collider[] colliders;

        void Start()
        {
            if (waypoints.Count > 0) transform.position = waypoints[0].position;
            colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.isTrigger = true;
        }

        void Update()
        {
            bool on = activeIn.Contains(TemporalStateManager.CurrentState) && !WorldState.Get(stoppedByFlag);
            if (headlight != null) headlight.enabled = on;
            if (!on || waypoints.Count < 2) return;

            if (pause > 0f) { pause -= Time.deltaTime; return; }

            Vector3 target = waypoints[index].position;
            Vector3 to = target - transform.position;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 4f);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            // Rail vibration ahead of the vehicle: readable warning for the player.
            hornCooldown -= Time.deltaTime;
            var p = PlayerController.Instance;
            if (p != null && hornCooldown <= 0f && Vector3.Distance(p.transform.position, transform.position) < warnDistance)
            {
                hornCooldown = 4f;
                AudioCues.PlayAt(Cue.HazardWarn, transform.position, 1f);
                NoiseSystem.Emit(transform.position, 0f, gameObject);
            }

            if (to.magnitude < 0.05f)
            {
                if (pingPong)
                {
                    if (index == waypoints.Count - 1 || index == 0 && dir < 0) { dir = -dir; pause = pauseAtEnds; }
                    index = Mathf.Clamp(index + dir, 0, waypoints.Count - 1);
                }
                else
                {
                    index = (index + 1) % waypoints.Count;
                    if (index == 0) { transform.position = waypoints[0].position; pause = pauseAtEnds; }
                }
            }
        }

        void LateUpdate()
        {
            if (!activeIn.Contains(TemporalStateManager.CurrentState) || WorldState.Get(stoppedByFlag)) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 p = player.transform.position + Vector3.up * 0.9f;
            foreach (var c in colliders)
            {
                var b = c.bounds;
                b.Expand(0.8f);
                if (b.Contains(p)) { player.Die(deathMessage); return; }
            }
        }
    }
}
