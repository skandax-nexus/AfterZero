using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Keeps a state-bound physics prop (the Past festival cart) from falling through the world while its
    /// state is hidden: TemporalPresence disables its collider, so the body is made kinematic until the
    /// state returns. Listens to the state event directly so it works while its own Behaviour is disabled.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class L01CartPhysics : MonoBehaviour
    {
        public TimeMask existsIn = TimeMask.Past;
        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            TemporalStateManager.StateChanged += OnState;
        }

        void OnDestroy() { TemporalStateManager.StateChanged -= OnState; }

        void Start() { Apply(TemporalStateManager.CurrentState); }

        void OnState(TimeState from, TimeState to) { if (this != null) Apply(to); }

        void Apply(TimeState s)
        {
            if (body == null) return;
            bool present = existsIn.Contains(s);
            if (!present)
            {
#if UNITY_6000_0_OR_NEWER
                if (!body.isKinematic) body.linearVelocity = Vector3.zero;
#else
                if (!body.isKinematic) body.velocity = Vector3.zero;
#endif
                body.isKinematic = true;
            }
            else body.isKinematic = false;
        }
    }
}
