using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// A Keeper of the Hour carrying a sight-lantern. Patrols waypoints, sees in a cone (line of sight,
    /// shorter range when Arin crouches), hears noises, and fills a detection meter before reacting.
    /// Hunter mode: alarm then chase, catching = respawn. Watcher mode (Sister Vossel): never attacks, but
    /// reports "anomalies" when it sees Arin shift state or tamper with objects (drives LoopEscalation).
    /// </summary>
    public class KeeperPatrol : MonoBehaviour
    {
        public enum Mode { Hunter, Watcher }

        public static event Action<KeeperPatrol> Anomaly;
        public static event Action<KeeperPatrol> Alarm;
        static readonly List<KeeperPatrol> all = new List<KeeperPatrol>();
        public static IReadOnlyList<KeeperPatrol> All => all;

        public string displayName = "Keeper";
        public Mode mode = Mode.Hunter;
        public List<Transform> patrol = new List<Transform>();
        public float walkSpeed = 1.6f;
        public float chaseSpeed = 4.4f;
        public float pauseAtPoint = 2f;
        public TimeMask activeIn = TimeMask.Present;
        public string disabledByFlag = "";

        [Header("Senses")]
        public float viewDistance = 12f;
        [Range(10f, 170f)] public float viewAngle = 70f;
        public float crouchDistanceFactor = 0.55f;
        [Tooltip("Seconds of continuous sight before full detection.")]
        public float detectTime = 1.4f;
        public float hearingRadius = 9f;
        public float catchRadius = 1.3f;
        public Transform eye;
        public Light lantern;

        [Header("Story")]
        public string alarmFlag = "";
        public List<string> spotBarks = new List<string> { "Who's there?", "Something moved." };
        public List<string> alarmBarks = new List<string> { "Anomaly! Stop!", "There - the one who remembers!" };
        [TextArea] public string caughtMessage = "The Keepers caught you. Stay out of the lantern light.";

        public float Detection { get; private set; }
        public bool Chasing { get; private set; }

        int index;
        float wait;
        Vector3 startPos;
        Quaternion startRot;
        Vector3? investigate;
        float barkCooldown;
        float lostTimer;

        void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            float k = GameSettings.PerceptionScale;
            viewDistance *= Mathf.Lerp(1f, k, 0.6f);
            hearingRadius *= k;
            detectTime /= k;
            if (eye == null) eye = transform;
        }

        void OnEnable()
        {
            all.Add(this);
            NoiseSystem.Emitted += OnNoise;
            TemporalStateManager.ShiftCompleted += OnShift;
            Interactable.AnyInteracted += OnInteracted;
            Checkpoint.Respawned += ResetGuard;
        }

        void OnDisable()
        {
            all.Remove(this);
            NoiseSystem.Emitted -= OnNoise;
            TemporalStateManager.ShiftCompleted -= OnShift;
            Interactable.AnyInteracted -= OnInteracted;
            Checkpoint.Respawned -= ResetGuard;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Anomaly = null; Alarm = null; all.Clear(); }

        bool Active => activeIn.Contains(TemporalStateManager.CurrentState) && !WorldState.Get(disabledByFlag);

        void ResetGuard()
        {
            transform.SetPositionAndRotation(startPos, startRot);
            Detection = 0f;
            Chasing = false;
            investigate = null;
            index = 0;
        }

        // ---------- perception ----------

        public bool CanSeePlayer()
        {
            var p = PlayerController.Instance;
            if (p == null) return false;
            Vector3 eyePos = eye.position;
            Vector3 target = p.transform.position + Vector3.up * (p.IsCrouching ? 0.8f : 1.4f);
            Vector3 to = target - eyePos;
            float range = viewDistance * (p.IsCrouching ? crouchDistanceFactor : 1f);
            if (to.magnitude > range) return false;
            Vector3 flatFwd = transform.forward; flatFwd.y = 0f;
            Vector3 flatTo = to; flatTo.y = 0f;
            if (Vector3.Angle(flatFwd, flatTo) > viewAngle * 0.5f) return false;
            if (Physics.Raycast(eyePos, to.normalized, out RaycastHit hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(p.transform) && !hit.transform.IsChildOf(transform)) return false;
            return true;
        }

        void OnNoise(NoiseEvent e)
        {
            if (!Active || mode == Mode.Watcher || Chasing || e.source == gameObject) return;
            float d = Vector3.Distance(e.position, transform.position);
            if (d < hearingRadius * Mathf.Clamp01(e.loudness + 0.2f)) investigate = e.position;
        }

        void OnShift(TimeState from, TimeState to)
        {
            // A shift is visible from the state you leave.
            if (mode == Mode.Watcher && activeIn.Contains(from) && CanSeePlayerIgnoringState()) ReportAnomaly();
        }

        void OnInteracted(Interactable it)
        {
            if (mode == Mode.Watcher && Active && CanSeePlayer()) ReportAnomaly();
        }

        bool CanSeePlayerIgnoringState() => CanSeePlayer();

        void ReportAnomaly()
        {
            Bark(spotBarks);
            AudioCues.PlayAt(Cue.Alert, transform.position, 0.6f);
            AZHaptics.Pulse();
            Anomaly?.Invoke(this);
        }

        // ---------- behaviour ----------

        void Update()
        {
            barkCooldown -= Time.deltaTime;
            bool active = Active;
            if (lantern != null) lantern.enabled = active;
            if (!active) { Detection = 0f; return; }

            bool sees = CanSeePlayer();
            if (mode == Mode.Hunter)
            {
                if (sees) Detection = Mathf.MoveTowards(Detection, 1f, Time.deltaTime / detectTime);
                else if (!Chasing) Detection = Mathf.MoveTowards(Detection, 0f, Time.deltaTime * 0.35f);
                if (Detection > 0.35f && Detection < 1f && sees) { investigate = PlayerController.Instance.transform.position; Bark(spotBarks); }
                if (Detection >= 1f && !Chasing) StartChase();
            }

            if (lantern != null) lantern.color = Chasing ? new Color(1f, 0.25f, 0.2f) : Color.Lerp(new Color(1f, 0.85f, 0.55f), new Color(1f, 0.45f, 0.2f), Detection);

            if (Chasing) TickChase(sees);
            else if (investigate.HasValue) { if (MoveTo(investigate.Value, walkSpeed * 1.4f)) { investigate = null; wait = pauseAtPoint * 1.5f; } }
            else TickPatrol();
        }

        void StartChase()
        {
            Chasing = true;
            lostTimer = 0f;
            Bark(alarmBarks);
            AudioCues.Play(Cue.Alert);
            AZHaptics.Pulse();
            WorldState.Set(alarmFlag);
            Alarm?.Invoke(this);
        }

        void TickChase(bool sees)
        {
            var p = PlayerController.Instance;
            if (p == null) return;
            lostTimer = sees ? 0f : lostTimer + Time.deltaTime;
            if (lostTimer > 4f) { Chasing = false; Detection = 0.5f; investigate = p.transform.position; return; }
            MoveTo(p.transform.position, chaseSpeed);
            if (Vector3.Distance(p.transform.position, transform.position) < catchRadius)
            {
                p.Die(caughtMessage);
                ResetGuard();
            }
        }

        void TickPatrol()
        {
            if (patrol.Count == 0) return;
            if (wait > 0f)
            {
                wait -= Time.deltaTime;
                transform.Rotate(0f, Mathf.Sin(Time.time * 0.8f) * 40f * Time.deltaTime, 0f); // look around
                return;
            }
            if (MoveTo(patrol[index].position, walkSpeed)) { index = (index + 1) % patrol.Count; wait = pauseAtPoint; }
        }

        bool MoveTo(Vector3 target, float speed)
        {
            target.y = transform.position.y;
            Vector3 to = target - transform.position;
            if (to.magnitude < 0.3f) return true;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 5f);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            return false;
        }

        void Bark(List<string> lines)
        {
            if (barkCooldown > 0f || lines == null || lines.Count == 0) return;
            barkCooldown = 6f;
            Subtitles.Bark(displayName, lines[UnityEngine.Random.Range(0, lines.Count)], transform.position);
        }

        /// <summary>Highest detection among active hunters (drives the HUD eye meter).</summary>
        public static float MaxDetection()
        {
            float m = 0f;
            foreach (var k in all) if (k != null && k.mode == Mode.Hunter && k.isActiveAndEnabled) m = Mathf.Max(m, k.Chasing ? 1f : k.Detection);
            return m;
        }
    }
}
