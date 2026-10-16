using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 04 lift: descends while its start flag is set, enabling the shaft hazards during the ride.
    /// If the player dies mid-ride the car returns to the top and the start flag is cleared so the ride
    /// can be retried (prevents a soft-lock with the car stuck at the bottom).
    /// </summary>
    public class L04LiftRide : MonoBehaviour
    {
        public Transform car;
        public Vector3 drop = new Vector3(0f, -10f, 0f);
        public float duration = 14f;
        public string startFlag = "L04.descending";
        public string arrivedFlag = "L04.liftArrived";
        public GameObject rideHazards;

        Vector3 top;
        float t;
        bool running;

        void Awake() { if (car != null) top = car.localPosition; if (rideHazards != null) rideHazards.SetActive(false); }

        void OnEnable() { WorldState.FlagChanged += OnFlag; Checkpoint.Respawned += OnRespawn; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; Checkpoint.Respawned -= OnRespawn; }

        void OnFlag(string f, bool v)
        {
            if (f != startFlag || !v || running || WorldState.Get(arrivedFlag)) return;
            running = true;
            t = 0f;
            if (rideHazards != null) rideHazards.SetActive(true);
            AudioCues.Play(Cue.RuleChange);
        }

        void OnRespawn()
        {
            if (WorldState.Get(arrivedFlag) || car == null) return;
            running = false;
            t = 0f;
            car.localPosition = top;
            if (rideHazards != null) rideHazards.SetActive(false);
            WorldState.Set(startFlag, false);
        }

        void Update()
        {
            if (!running || car == null) return;
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime / Mathf.Max(0.1f, duration));
            car.localPosition = top + drop * Mathf.SmoothStep(0f, 1f, t);
            if (t >= 1f)
            {
                running = false;
                if (rideHazards != null) rideHazards.SetActive(false);
                WorldState.Set(arrivedFlag);
                AudioCues.Play(Cue.Clang);
            }
        }
    }
}
