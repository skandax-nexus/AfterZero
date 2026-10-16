using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Keeps a state-specific physics prop (the Level 04 cable plug) from falling through the floor
    /// while its state is hidden (its collider is switched off then). Listens even while disabled.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class L04FreezeWhenAbsent : MonoBehaviour
    {
        public TimeMask existsIn = TimeMask.Present;

        Rigidbody body;
        bool frozen;
        bool wasKinematic;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            TemporalStateManager.StateChanged += OnState;
        }

        void OnDestroy() { TemporalStateManager.StateChanged -= OnState; }

        void OnState(TimeState from, TimeState to)
        {
            if (body == null) return;
            bool present = existsIn.Contains(to);
            if (!present && !frozen)
            {
                wasKinematic = body.isKinematic;
                body.isKinematic = true;
                frozen = true;
            }
            else if (present && frozen)
            {
                body.isKinematic = wasKinematic;
                frozen = false;
            }
        }
    }
}
