using System;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Owns the current state (Past / Present / Future) for the loaded level.
    /// Everything that looks different per state listens to <see cref="StateChanged"/>.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class TemporalStateManager : MonoBehaviour
    {
        public static TemporalStateManager Instance { get; private set; }

        /// <summary>(previous, current). Fired after the shift has been accepted.</summary>
        public static event Action<TimeState, TimeState> StateChanged;
        /// <summary>Fired when a shift is refused (not allowed, cooldown, or the player would be inside geometry).</summary>
        public static event Action<TimeState, string> ShiftRefused;
        /// <summary>Fired once per successful player/scripted shift — drives transition VFX and audio.</summary>
        public static event Action<TimeState, TimeState> ShiftCompleted;

        [Tooltip("States the player may shift into in this level.")]
        public TimeMask allowedStates = TimeMask.All;
        public TimeState startState = TimeState.Present;
        [Min(0f)] public float shiftCooldown = 0.75f;

        /// <summary>Set by the player so a shift can be undone if it would trap them inside geometry.</summary>
        public Func<bool> isPlayerObstructed;

        public TimeState Current { get; private set; }
        public bool Locked { get; set; }

        float lastShiftTime = -999f;

        public static TimeState CurrentState => Instance != null ? Instance.Current : TimeState.Present;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            StateChanged = null;
            ShiftRefused = null;
            ShiftCompleted = null;
            Instance = null;
        }

        void Awake()
        {
            Instance = this;
            Current = startState;
        }

        void Start()
        {
            // Broadcast the initial state so every listener configures itself.
            StateChanged?.Invoke(Current, Current);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsAllowed(TimeState state) => allowedStates.Contains(state);

        public bool TryShift(TimeState target)
        {
            if (target == Current) return false;
            if (Locked) { ShiftRefused?.Invoke(target, "The cycle holds you here."); return false; }
            if (!IsAllowed(target)) { ShiftRefused?.Invoke(target, "That state is not reachable here yet."); return false; }
            if (Time.time - lastShiftTime < shiftCooldown) return false;

            var previous = Current;
            Apply(previous, target);

            if (isPlayerObstructed != null && isPlayerObstructed())
            {
                Apply(target, previous);
                ShiftRefused?.Invoke(target, "Something occupies this space in the " + target.ToString().ToUpperInvariant() + ".");
                return false;
            }

            lastShiftTime = Time.time;
            ShiftCompleted?.Invoke(previous, target);
            return true;
        }

        /// <summary>Cycle forward/backward through allowed states (Past → Present → Future).</summary>
        public bool Step(int direction)
        {
            for (int i = 1; i <= 2; i++)
            {
                int idx = ((int)Current + direction * i + 3) % 3;
                var candidate = (TimeState)idx;
                if (IsAllowed(candidate)) return TryShift(candidate);
            }
            return false;
        }

        /// <summary>Force a state without checks (used by scripted sequences and checkpoints).</summary>
        public void ForceState(TimeState state)
        {
            var previous = Current;
            if (previous == state) return;
            Apply(previous, state);
            ShiftCompleted?.Invoke(previous, state);
        }

        void Apply(TimeState from, TimeState to)
        {
            Current = to;
            StateChanged?.Invoke(from, to);
            Physics.SyncTransforms();
        }
    }
}
