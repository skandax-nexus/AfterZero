using UnityEngine;

namespace AfterZero
{
    /// <summary>Heavy physics object the player shoves by walking into it (the festival cart).</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Pushable : MonoBehaviour, ILoopResettable
    {
        public float damping = 4f;
        public bool resetOnLoop = false;
        public Cue scrapeCue = Cue.Scrape;

        Rigidbody body;
        Vector3 startPos;
        Quaternion startRot;
        float lastScrape;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = Mathf.Max(body.mass, 40f);
#if UNITY_6000_0_OR_NEWER
            body.linearDamping = damping;
            body.angularDamping = damping * 2f;
#else
            body.drag = damping;
            body.angularDrag = damping * 2f;
#endif
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            startPos = transform.position;
            startRot = transform.rotation;
        }

        void OnEnable() { LoopManager.Register(this); }
        void OnDisable() { LoopManager.Unregister(this); }

        public void NotifyPushed()
        {
            if (Time.time - lastScrape < 0.6f) return;
            lastScrape = Time.time;
            AudioCues.PlayAt(scrapeCue, transform.position, 0.4f);
            NoiseSystem.Emit(transform.position, 0.6f, gameObject);
        }

        public void OnLoopReset()
        {
            if (!resetOnLoop) return;
            body.position = startPos;
            body.rotation = startRot;
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            body.angularVelocity = Vector3.zero;
        }
    }
}
