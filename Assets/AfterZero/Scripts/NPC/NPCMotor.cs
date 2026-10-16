using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    public enum NPCActivity { Idle, Walk, Work, Talk, Carry, Sit, Look }

    [Serializable]
    public class RoutineStep
    {
        public Transform point;
        [Min(0f)] public float wait = 3f;
        public NPCActivity activity = NPCActivity.Idle;
        [Tooltip("Optional line spoken (as a nearby bark) on arrival.")]
        public string bark = "";
    }

    /// <summary>
    /// Body of every resident: follows a routine, animates procedurally (walk sway, arm swing,
    /// working, carrying), turns to face the player when approached, glances at world changes,
    /// and keeps living when not observed. Restarts its routine when the loop restarts.
    /// </summary>
    public class NPCMotor : MonoBehaviour, ILoopResettable
    {
        public string displayName = "Resident";
        public List<RoutineStep> routine = new List<RoutineStep>();
        [Tooltip("Used instead of 'routine' once this flag is set (changed state after the objective).")]
        public string changedFlag = "";
        public List<RoutineStep> changedRoutine = new List<RoutineStep>();
        public float walkSpeed = 1.4f;
        public bool loopRoutine = true;

        [Header("Body (auto-filled by the level builder)")]
        public Transform body;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public GameObject carriedProp;

        [Header("Awareness")]
        public float noticeRadius = 3.2f;
        public float lookAtPlayerRadius = 5f;
        public Transform conversePartner;
        public List<string> reactionBarks = new List<string>();

        public bool Paused { get; set; }
        public NPCActivity Activity { get; private set; } = NPCActivity.Idle;

        int stepIndex;
        float waitTimer;
        float animPhase;
        float startleTimer;
        float lastReactionTime = -999f;
        Vector3 startPos;
        Quaternion startRot;
        Vector3 bodyBase;
        bool usingChanged;
        bool travelling;

        List<RoutineStep> ActiveRoutine => usingChanged && changedRoutine.Count > 0 ? changedRoutine : routine;

        void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            if (body != null) bodyBase = body.localPosition;
        }

        void OnEnable()
        {
            LoopManager.Register(this);
            WorldState.FlagChanged += OnFlag;
            usingChanged = WorldState.Get(changedFlag);
        }

        void OnDisable()
        {
            LoopManager.Unregister(this);
            WorldState.FlagChanged -= OnFlag;
        }

        void Start() { BeginStep(0); }

        void OnFlag(string flag, bool value)
        {
            if (!value) return;
            if (!string.IsNullOrEmpty(changedFlag) && flag == changedFlag && !usingChanged)
            {
                usingChanged = true;
                BeginStep(0);
            }
            // Residents notice the world changing around them.
            if (!flag.StartsWith(WorldState.LoopPrefix, StringComparison.Ordinal) && !flag.StartsWith("clue.", StringComparison.Ordinal))
            {
                startleTimer = 0.6f;
                if (reactionBarks.Count > 0 && Time.time - lastReactionTime > 20f && IsNearPlayer(12f))
                {
                    lastReactionTime = Time.time;
                    Subtitles.Bark(displayName, reactionBarks[UnityEngine.Random.Range(0, reactionBarks.Count)], transform.position);
                }
            }
        }

        void BeginStep(int i)
        {
            var r = ActiveRoutine;
            if (r.Count == 0) { Activity = NPCActivity.Idle; travelling = false; return; }
            stepIndex = Mathf.Clamp(i, 0, r.Count - 1);
            travelling = r[stepIndex].point != null;
            waitTimer = r[stepIndex].wait;
            Activity = travelling ? NPCActivity.Walk : r[stepIndex].activity;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            animPhase += dt;
            if (startleTimer > 0f) startleTimer -= dt;

            var player = PlayerController.Instance;
            bool playerClose = player != null && Vector3.Distance(player.transform.position, transform.position) < noticeRadius;

            if (!Paused && !playerClose) TickRoutine(dt);
            else if (player != null) Face(player.transform.position, dt * 5f);

            Animate(dt, player);
        }

        void TickRoutine(float dt)
        {
            var r = ActiveRoutine;
            if (r.Count == 0) return;
            var step = r[stepIndex];

            if (travelling)
            {
                Vector3 target = step.point.position;
                target.y = transform.position.y;
                Vector3 to = target - transform.position;
                if (to.magnitude > 0.15f)
                {
                    Face(target, dt * 6f);
                    transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * dt);
                    Activity = carriedProp != null && step.activity == NPCActivity.Carry ? NPCActivity.Carry : NPCActivity.Walk;
                    return;
                }
                travelling = false;
                Activity = step.activity;
                transform.rotation = Quaternion.Slerp(transform.rotation, step.point.rotation, 1f);
                if (!string.IsNullOrEmpty(step.bark) && IsNearPlayer(9f)) Subtitles.Bark(displayName, step.bark, transform.position);
            }

            if (Activity == NPCActivity.Talk && conversePartner != null) Face(conversePartner.position, dt * 3f);

            waitTimer -= dt;
            if (waitTimer <= 0f)
            {
                int next = stepIndex + 1;
                if (next >= r.Count) { if (!loopRoutine) { waitTimer = float.MaxValue; return; } next = 0; }
                BeginStep(next);
            }
        }

        void Face(Vector3 point, float t)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), Mathf.Clamp01(t));
        }

        void Animate(float dt, PlayerController player)
        {
            bool walking = Activity == NPCActivity.Walk || Activity == NPCActivity.Carry && travelling;
            float swing = walking ? Mathf.Sin(animPhase * 7f) * 32f : 0f;

            if (body != null)
            {
                float bob = walking ? Mathf.Abs(Mathf.Sin(animPhase * 7f)) * 0.05f : Mathf.Sin(animPhase * 1.6f) * 0.008f; // breathing
                float sit = Activity == NPCActivity.Sit ? -0.35f : 0f;
                float hop = startleTimer > 0f ? Mathf.Sin((0.6f - startleTimer) / 0.6f * Mathf.PI) * 0.12f : 0f;
                body.localPosition = bodyBase + new Vector3(0f, bob + sit + hop, 0f);
            }

            if (leftArm != null && rightArm != null)
            {
                float lx = swing, rx = -swing;
                if (Activity == NPCActivity.Work) { rx = -60f + Mathf.Sin(animPhase * 9f) * 35f; lx = -20f; }
                else if (Activity == NPCActivity.Carry) { lx = rx = -70f; }
                else if (Activity == NPCActivity.Talk) { rx = -25f + Mathf.Sin(animPhase * 2.3f) * 18f; }
                leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(lx, 0f, 4f), dt * 10f);
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(rx, 0f, -4f), dt * 10f);
            }

            if (carriedProp != null) carriedProp.SetActive(Activity == NPCActivity.Carry);

            if (head != null)
            {
                Quaternion target = Quaternion.identity;
                if (player != null && Vector3.Distance(player.transform.position, transform.position) < lookAtPlayerRadius)
                {
                    Vector3 eye = player.playerCamera != null ? player.playerCamera.transform.position : player.transform.position + Vector3.up * 1.6f;
                    Vector3 local = transform.InverseTransformDirection(eye - head.position);
                    if (local.z > 0f) target = Quaternion.LookRotation(local.normalized);
                }
                else if (Activity == NPCActivity.Look)
                {
                    target = Quaternion.Euler(-10f, Mathf.Sin(animPhase * 0.7f) * 50f, 0f);
                }
                head.localRotation = Quaternion.Slerp(head.localRotation, target, dt * 4f);
            }
        }

        bool IsNearPlayer(float r)
        {
            var p = PlayerController.Instance;
            return p != null && Vector3.Distance(p.transform.position, transform.position) < r;
        }

        public void OnLoopReset()
        {
            transform.SetPositionAndRotation(startPos, startRot);
            usingChanged = WorldState.Get(changedFlag);
            BeginStep(0);
        }
    }
}
