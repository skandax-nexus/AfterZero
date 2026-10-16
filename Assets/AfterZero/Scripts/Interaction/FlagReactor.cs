using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Causality made visible: when the required flags are all true, enable/disable objects and
    /// move/rotate a transform (doors opening, water draining, a street rearranging itself).
    /// Fires again in reverse if the flags become false and <see cref="reversible"/> is set.
    /// </summary>
    public class FlagReactor : MonoBehaviour
    {
        public List<string> whenAll = new List<string>();
        [Tooltip("Optional: all of these must be false.")]
        public List<string> whenNone = new List<string>();
        public bool reversible = true;

        [Header("Toggle")]
        public List<GameObject> enableWhenTrue = new List<GameObject>();
        public List<GameObject> disableWhenTrue = new List<GameObject>();

        [Header("Motion")]
        public Transform moveTarget;
        public Vector3 moveBy = Vector3.zero;
        public Vector3 rotateBy = Vector3.zero;
        [Min(0.05f)] public float duration = 1.5f;

        [Header("Feedback")]
        public bool announce = false;
        public Cue cue = Cue.RuleChange;
        public string toast = "";
        [Tooltip("Optional flag set once the reaction has happened (chain reactions).")]
        public string setFlagWhenTrue = "";

        public bool IsTrue { get; private set; }

        Vector3 basePos;
        Quaternion baseRot;
        float t;
        bool initialised;

        void Awake()
        {
            if (moveTarget != null) { basePos = moveTarget.localPosition; baseRot = moveTarget.localRotation; }
        }

        void OnEnable()
        {
            WorldState.FlagChanged += OnFlagChanged;
            Evaluate(true);
        }

        void OnDisable() { WorldState.FlagChanged -= OnFlagChanged; }

        void OnFlagChanged(string f, bool v) => Evaluate(false);

        bool Condition()
        {
            if (!WorldState.All(whenAll)) return false;
            foreach (var f in whenNone) if (WorldState.Get(f)) return false;
            return true;
        }

        void Evaluate(bool instant)
        {
            bool now = Condition();
            if (initialised && now == IsTrue) return;
            if (initialised && !now && !reversible) return;
            bool changed = initialised;
            initialised = true;
            IsTrue = now;

            foreach (var g in enableWhenTrue) if (g != null) g.SetActive(now);
            foreach (var g in disableWhenTrue) if (g != null) g.SetActive(!now);

            if (instant && moveTarget != null)
            {
                t = now ? 1f : 0f;
                ApplyMotion();
            }

            if (changed && now)
            {
                if (announce) AudioCues.PlayAt(cue, transform.position);
                if (!string.IsNullOrEmpty(toast)) HUD.Toast(toast, 3f);
                WorldState.Set(setFlagWhenTrue);
            }
            else if (!changed && now)
            {
                WorldState.Set(setFlagWhenTrue);
            }
        }

        void Update()
        {
            if (moveTarget == null) return;
            float goal = IsTrue ? 1f : 0f;
            if (Mathf.Approximately(t, goal)) return;
            t = Mathf.MoveTowards(t, goal, Time.deltaTime / duration);
            ApplyMotion();
        }

        void ApplyMotion()
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            moveTarget.localPosition = basePos + moveBy * e;
            moveTarget.localRotation = baseRot * Quaternion.Euler(rotateBy * e);
        }
    }
}
