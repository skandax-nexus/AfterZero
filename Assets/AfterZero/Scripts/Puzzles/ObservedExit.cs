using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// The nursery door only stays where it is while someone is looking at it. Unobserved for longer
    /// than <see cref="graceTime"/>, it slides to the next anchor — until <see cref="anchoredFlag"/> is set.
    /// A soft hum and a ring on the HUD edge warn as the grace time runs out.
    /// </summary>
    public class ObservedExit : MonoBehaviour
    {
        public List<Transform> anchors = new List<Transform>();
        public float graceTime = 2.5f;
        public string anchoredFlag = "";
        public Renderer doorRenderer;

        public float Unobserved { get; private set; }
        int anchorIndex;

        void Update()
        {
            if (WorldState.Get(anchoredFlag) || anchors.Count < 2) { Unobserved = 0f; return; }
            var cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            if (cam == null) return;

            if (IsSeen(cam)) Unobserved = 0f;
            else Unobserved += Time.deltaTime;

            if (Unobserved > graceTime * 0.6f) HUD.Toast("The door is drifting. Keep it in view.", 0.2f);
            if (Unobserved > graceTime)
            {
                Unobserved = 0f;
                anchorIndex = (anchorIndex + 1) % anchors.Count;
                transform.SetPositionAndRotation(anchors[anchorIndex].position, anchors[anchorIndex].rotation);
                AudioCues.PlayAt(Cue.RuleChange, transform.position, 0.8f);
                TemporalVisuals.Pulse(0.4f);
            }
        }

        bool IsSeen(Camera cam)
        {
            Vector3 c = doorRenderer != null ? doorRenderer.bounds.center : transform.position;
            Vector3 v = cam.WorldToViewportPoint(c);
            if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return false;
            if (Physics.Linecast(cam.transform.position, c, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(transform) && hit.distance < Vector3.Distance(cam.transform.position, c) - 0.3f)
                {
                    // Carried objects are disabled colliders, so anything hit here is real occlusion.
                    return false;
                }
            }
            return true;
        }
    }
}
