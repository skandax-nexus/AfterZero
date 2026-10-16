using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Lise Ashgrove's loop moment: at ~0:14 (scaled with the loop length) she sprints from the lane mouth
    /// into Mirela's stall. Talking to her beforehand, or standing in her path while she runs, stops her
    /// (sets <see cref="stoppedFlag"/>, a loop.* flag).
    /// </summary>
    public class L01LiseRun : MonoBehaviour
    {
        public NPCMotor lise;
        public Transform target;
        public float baseTime = 14f;
        public float speed = 4.8f;
        public float stopRadius = 1.9f;
        public string stoppedFlag = "loop.L01.liseStopped";
        public string ranFlag = "loop.L01.liseRan";
        public string seenFlag = "L01.liseRanSeen";
        public string disabledByFlag = "";
        [Tooltip("The 'seen' flag only counts once this flag is set (so the beat is noticed after the cart lesson).")]
        public string seenRequiresFlag = "";

        bool running, done;
        Vector3 startPos;
        Quaternion startRot;

        void Start()
        {
            if (lise != null) { startPos = lise.transform.position; startRot = lise.transform.rotation; }
        }

        void OnEnable() { LoopManager.LoopRestarted += OnLoop; }
        void OnDisable() { LoopManager.LoopRestarted -= OnLoop; }

        void OnLoop(int n)
        {
            running = false;
            done = false;
            if (lise == null) return;
            lise.Paused = false;
            lise.transform.SetPositionAndRotation(startPos, startRot);
        }

        void Update()
        {
            if (lise == null || target == null || WorldState.Get(disabledByFlag)) { if (lise != null && running) { running = false; lise.Paused = false; } return; }
            var lm = LoopManager.Instance;
            if (lm == null || !lm.Running) return;

            if (!running && !done && LoopManager.LoopTime >= baseTime * L01LoopTimeline.Scale)
            {
                done = true;
                if (WorldState.Get(stoppedFlag)) return;
                running = true;
                lise.Paused = true;
                Subtitles.Bark(lise.displayName, "Cup! Cup! Mirela, wait for me!", lise.transform.position);
            }
            if (!running) return;

            var p = PlayerController.Instance;
            if (p != null && TemporalStateManager.CurrentState == TimeState.Present &&
                Vector3.Distance(Flat(p.transform.position), Flat(lise.transform.position)) < stopRadius)
            {
                running = false;
                WorldState.Set(stoppedFlag);
                AudioCues.PlayAt(Cue.Interact, lise.transform.position);
                Subtitles.Bark(lise.displayName, "Oh! Sorry - I nearly ran right into you.", lise.transform.position);
                return;
            }

            Vector3 goal = target.position; goal.y = lise.transform.position.y;
            Vector3 to = goal - lise.transform.position;
            if (to.magnitude < 0.2f)
            {
                running = false;
                WorldState.Set(ranFlag);
                if (string.IsNullOrEmpty(seenRequiresFlag) || WorldState.Get(seenRequiresFlag)) WorldState.Set(seenFlag);
                AudioCues.PlayAt(Cue.Clang, target.position);
                Subtitles.Bark(lise.displayName, "Oops! Sorry, Mirela!", lise.transform.position);
                return;
            }
            lise.transform.rotation = Quaternion.LookRotation(to.normalized);
            lise.transform.position = Vector3.MoveTowards(lise.transform.position, goal, speed * Time.deltaTime);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
