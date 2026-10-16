using UnityEngine;

namespace AfterZero
{
    /// <summary>Aims from the camera, shows prompts, and carries one Grabbable at a time.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        public float reach = 3.2f;
        public Transform holdPoint;

        public Grabbable Held { get; private set; }
        public Interactable Focus { get; private set; }

        PlayerController player;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            if (holdPoint == null && player.playerCamera != null)
            {
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(player.playerCamera.transform, false);
                hp.localPosition = new Vector3(0.25f, -0.2f, 1.1f);
                holdPoint = hp;
            }
        }

        void Update()
        {
            if (GameInput.GameplayLocked) { HUD.SetPrompt(""); return; }

            Focus = FindFocus();
            string promptText = "";
            if (Focus != null)
            {
                promptText = (AZPlatform.UseTouchControls ? "USE: " : "[E] ") + Focus.GetPrompt(this);
                if (!Focus.RequirementsMet && !string.IsNullOrEmpty(Focus.lockedMessage)) promptText += "  (locked)";
            }
            else if (Held != null)
            {
                promptText = (AZPlatform.UseTouchControls ? "USE: drop " : "[E] Drop ") + Held.displayName;
            }
            HUD.SetPrompt(promptText);

            if (GameInput.Interact.WasPressedThisFrame())
            {
                if (Focus != null) Focus.TryInteract(this);
                else if (Held != null) DropHeld();
            }
        }

        Interactable FindFocus()
        {
            var cam = player.playerCamera;
            if (cam == null) return null;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            // Touch screens get a little aim assist: a thin sphere instead of a pixel-perfect ray.
            var hits = AZPlatform.UseTouchControls
                ? Physics.SphereCastAll(ray, 0.22f, reach, ~0, QueryTriggerInteraction.Collide)
                : Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                var it = hit.collider.GetComponentInParent<Interactable>();
                if (it != null && it != Held && it.IsUsableNow) return it;
                if (!hit.collider.isTrigger) return null; // solid geometry blocks the view
            }
            return null;
        }

        public void PickUp(Grabbable g)
        {
            if (Held != null) DropHeld();
            Held = g;
            g.AttachTo(holdPoint);
            AudioCues.Play(Cue.PickUp);
        }

        public void DropHeld()
        {
            if (Held == null) return;
            var g = Held;
            Held = null;
            g.Detach(player.Velocity);
        }

        /// <summary>Hand the held object to a socket (does not drop physics).</summary>
        public Grabbable ReleaseHeld()
        {
            var g = Held;
            Held = null;
            return g;
        }
    }
}
