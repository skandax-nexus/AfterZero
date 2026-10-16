using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Scripted chaser (Brother Calder, collapsing debris front): once started, runs along a fixed lane of
    /// waypoints behind the player. Catching the player sends them to the last checkpoint and resets the chase.
    /// </summary>
    public class Pursuer : MonoBehaviour
    {
        public string displayName = "Brother Calder";
        public string startFlag = "";
        public string stopFlag = "";
        public List<Transform> lane = new List<Transform>();
        public float speed = 4.6f;
        [Tooltip("Speeds up when far behind so the chase stays tense (rubber band).")]
        public float catchUpSpeed = 6.5f;
        public float catchUpDistance = 14f;
        public float catchRadius = 1.4f;
        public float startDelay = 1.5f;
        public List<string> barks = new List<string> { "You can't outrun the hour!" };
        [TextArea] public string caughtMessage = "Calder caught you.";

        Vector3 startPos;
        Quaternion startRot;
        int index;
        bool running;
        float delay;
        float barkTimer;

        void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            speed *= Mathf.Lerp(1f, GameSettings.PerceptionScale, 0.4f);
        }

        void OnEnable() { WorldState.FlagChanged += OnFlag; Checkpoint.Respawned += ResetChase; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; Checkpoint.Respawned -= ResetChase; }

        void OnFlag(string f, bool v)
        {
            if (v && f == startFlag && !running && !WorldState.Get(stopFlag)) Begin();
            if (v && f == stopFlag) running = false;
        }

        void Begin()
        {
            running = true;
            delay = startDelay;
            index = 0;
            AudioCues.Play(Cue.Alert);
            AZHaptics.Pulse();
        }

        void ResetChase()
        {
            transform.SetPositionAndRotation(startPos, startRot);
            index = 0;
            if (running) delay = startDelay + 1f;
        }

        void Update()
        {
            if (!running || lane.Count == 0) return;
            if (delay > 0f) { delay -= Time.deltaTime; return; }
            var p = PlayerController.Instance;
            if (p == null) return;

            float d = Vector3.Distance(p.transform.position, transform.position);
            float s = d > catchUpDistance ? catchUpSpeed : speed;
            Vector3 target = index < lane.Count ? lane[index].position : p.transform.position;
            target.y = transform.position.y;
            Vector3 to = target - transform.position;
            if (to.magnitude < 0.4f && index < lane.Count) index++;
            if (to.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 6f);
            transform.position = Vector3.MoveTowards(transform.position, target, s * Time.deltaTime);

            barkTimer -= Time.deltaTime;
            if (barkTimer <= 0f && barks.Count > 0) { barkTimer = 7f; Subtitles.Bark(displayName, barks[Random.Range(0, barks.Count)], transform.position); }

            if (d < catchRadius) p.Die(caughtMessage);
        }
    }
}
