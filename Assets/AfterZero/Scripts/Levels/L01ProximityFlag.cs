using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Sets a flag when a tracked transform (an NPC on its routine) comes within range of this point -
    /// e.g. the courier reaching the postbox. Optional fallback time inside the loop so the beat can never stall.
    /// </summary>
    public class L01ProximityFlag : MonoBehaviour
    {
        public Transform target;
        public float radius = 1.6f;
        public string setFlag = "";
        [Tooltip("If > 0: also set the flag once the loop clock passes this time.")]
        public float fallbackLoopTime = 0f;

        void Update()
        {
            if (string.IsNullOrEmpty(setFlag) || WorldState.Get(setFlag)) return;
            bool near = target != null && target.gameObject.activeInHierarchy &&
                        Vector3.Distance(Flat(target.position), Flat(transform.position)) < radius;
            bool late = fallbackLoopTime > 0f && LoopManager.LoopTime >= fallbackLoopTime * L01LoopTimeline.Scale;
            if (near || late) WorldState.Set(setFlag);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
