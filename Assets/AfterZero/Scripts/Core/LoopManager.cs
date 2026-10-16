using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Anything that should snap back when the recurrence loop restarts.</summary>
    public interface ILoopResettable
    {
        void OnLoopReset();
    }

    /// <summary>
    /// The recurrence: the city repeats a short window of time. Objects implementing
    /// <see cref="ILoopResettable"/> rewind; WorldState flags (except "loop.*") persist,
    /// which is how deliberate changes survive the repeat.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }
        public static event Action<int> LoopRestarted;

        static readonly List<ILoopResettable> resettables = new List<ILoopResettable>();

        [Min(5f)] public float loopDuration = 30f;
        public bool runOnStart = true;
        [Tooltip("When this flag becomes true the loop stops repeating (the cycle is broken).")]
        public string breakLoopFlag = "";
        [Tooltip("Optional: teleport the player here each time the loop restarts.")]
        public Transform playerResetPoint;

        public float Elapsed { get; private set; }
        public float Normalized => loopDuration > 0f ? Mathf.Clamp01(Elapsed / loopDuration) : 0f;
        public int LoopCount { get; private set; }
        public bool Running { get; private set; }

        public static float LoopTime => Instance != null && Instance.Running ? Instance.Elapsed : Time.timeSinceLevelLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            resettables.Clear();
            LoopRestarted = null;
            Instance = null;
        }

        public static void Register(ILoopResettable r) { if (r != null && !resettables.Contains(r)) resettables.Add(r); }
        public static void Unregister(ILoopResettable r) { resettables.Remove(r); }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            Running = runOnStart && !WorldState.Get(breakLoopFlag);
        }

        void Update()
        {
            if (!Running) return;
            if (!string.IsNullOrEmpty(breakLoopFlag) && WorldState.Get(breakLoopFlag))
            {
                Running = false;
                return;
            }

            Elapsed += Time.deltaTime;
            if (Elapsed >= loopDuration) Restart();
        }

        public void Restart()
        {
            Elapsed = 0f;
            LoopCount++;
            WorldState.ClearLoopFlags();
            // Copy: a reset handler may register/unregister.
            var snapshot = resettables.ToArray();
            foreach (var r in snapshot)
            {
                try { r.OnLoopReset(); }
                catch (Exception e) { Debug.LogException(e); }
            }

            if (playerResetPoint != null && PlayerController.Instance != null)
                PlayerController.Instance.Teleport(playerResetPoint.position, playerResetPoint.rotation);

            AudioCues.Play(Cue.LoopReset);
            LoopRestarted?.Invoke(LoopCount);
        }

        public void StopLoop() { Running = false; }
    }
}
