using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// The tram line's maintenance machine. It is blind: it tracks vibration through the rails.
    /// Patrols, investigates the loudest recent noise, and catches the player if they get close
    /// while making noise. Crouch-walking is nearly silent; decoys pull it away.
    /// </summary>
    public class VibrationSentinel : MonoBehaviour
    {
        public List<Transform> patrol = new List<Transform>();
        public float patrolSpeed = 2.2f;
        public float investigateSpeed = 4.2f;
        public float hearingRadius = 16f;
        [Tooltip("Perceived loudness (loudness × falloff) needed to react.")]
        public float threshold = 0.18f;
        public float catchRadius = 2.2f;
        public float searchTime = 3.5f;
        public TimeMask activeIn = TimeMask.Present;
        public string disabledByFlag = "";

        [Header("Feedback")]
        public Light sensorLight;
        public Transform scanner;
        [TextArea] public string caughtMessage = "The machine felt your steps through the rails. Crouch to move quietly.";

        public enum Mode { Patrol, Investigate, Search }
        public Mode Current { get; private set; } = Mode.Patrol;
        public Vector3 Position => transform.position;

        int patrolIndex;
        Vector3 investigateTarget;
        float searchTimer;
        float alertCooldown;

        static readonly List<VibrationSentinel> all = new List<VibrationSentinel>();
        public static IReadOnlyList<VibrationSentinel> All => all;

        void Awake()
        {
            hearingRadius *= GameSettings.PerceptionScale;
            threshold /= GameSettings.PerceptionScale;
        }

        void OnEnable() { NoiseSystem.Emitted += OnNoise; all.Add(this); }
        void OnDisable() { NoiseSystem.Emitted -= OnNoise; all.Remove(this); }

        bool Active => activeIn.Contains(TemporalStateManager.CurrentState) && !WorldState.Get(disabledByFlag);

        void OnNoise(NoiseEvent e)
        {
            if (!Active || e.source == gameObject) return;
            float d = Vector3.Distance(e.position, transform.position);
            if (d > hearingRadius) return;
            float perceived = e.loudness * (1f - d / hearingRadius);
            if (perceived < threshold) return;

            investigateTarget = e.position;
            if (Current != Mode.Investigate && alertCooldown <= 0f)
            {
                AudioCues.PlayAt(Cue.Alert, transform.position);
                alertCooldown = 2f;
            }
            Current = Mode.Investigate;
        }

        void Update()
        {
            alertCooldown -= Time.deltaTime;
            bool active = Active;
            if (sensorLight != null) sensorLight.enabled = active;
            if (!active) return;

            switch (Current)
            {
                case Mode.Patrol:
                    if (patrol.Count > 0)
                    {
                        Vector3 p = patrol[patrolIndex].position;
                        if (MoveTowards(p, patrolSpeed)) patrolIndex = (patrolIndex + 1) % patrol.Count;
                    }
                    break;
                case Mode.Investigate:
                    if (MoveTowards(investigateTarget, investigateSpeed)) { Current = Mode.Search; searchTimer = searchTime; }
                    break;
                case Mode.Search:
                    searchTimer -= Time.deltaTime;
                    transform.Rotate(0f, 90f * Time.deltaTime, 0f);
                    if (searchTimer <= 0f) Current = Mode.Patrol;
                    break;
            }

            float pulseRate = Current == Mode.Patrol ? 1.2f : 6f;
            if (sensorLight != null) sensorLight.intensity = 2f + 1.5f * Mathf.Sin(Time.time * pulseRate * Mathf.PI * 2f);
            if (scanner != null) scanner.Rotate(0f, (Current == Mode.Patrol ? 60f : 240f) * Time.deltaTime, 0f, Space.Self);

            var player = PlayerController.Instance;
            if (player != null)
            {
                float d = Vector3.Distance(player.transform.position, transform.position);
                bool quiet = player.IsCrouching || player.Velocity.sqrMagnitude < 0.2f;
                if (d < catchRadius && (!quiet || d < catchRadius * 0.45f))
                {
                    Current = Mode.Patrol;
                    player.Die(caughtMessage);
                }
            }
        }

        bool MoveTowards(Vector3 target, float speed)
        {
            target.y = transform.position.y;
            Vector3 to = target - transform.position;
            if (to.magnitude < 0.3f) return true;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 5f);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            return false;
        }
    }
}
