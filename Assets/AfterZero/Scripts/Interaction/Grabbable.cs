using UnityEngine;

namespace AfterZero
{
    /// <summary>An object Arin can carry (blocks, a keepsake, the carved zero, a cable end).</summary>
    public class Grabbable : Interactable, ILoopResettable
    {
        [Tooltip("Matched by SnapSockets that accept this object.")]
        public string key = "";
        public string displayName = "object";
        [Tooltip("If false, shifting state while holding it returns it to where it was found.")]
        public bool carryAcrossStates = false;
        public bool resetOnLoop = true;
        [Tooltip("Optional flag set the first time it is picked up.")]
        public string pickedUpFlag = "";

        public bool IsHeld { get; private set; }
        public SnapSocket Socket { get; set; }

        Rigidbody body;
        Collider[] colliders;
        Transform originParent;
        Vector3 originPos;
        Quaternion originRot;
        Vector3 originScale;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            originParent = transform.parent;
            originPos = transform.localPosition;
            originRot = transform.localRotation;
            originScale = transform.localScale;
            if (string.IsNullOrEmpty(prompt) || prompt == "Inspect") prompt = "Take " + displayName;
        }

        void OnEnable()
        {
            LoopManager.Register(this);
            TemporalStateManager.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            LoopManager.Unregister(this);
            TemporalStateManager.StateChanged -= OnStateChanged;
        }

        public override bool IsUsableNow => base.IsUsableNow && !IsHeld;

        protected override void OnInteract(PlayerInteractor who)
        {
            if (Socket != null) { Socket.Release(this); }
            who.PickUp(this);
            if (!string.IsNullOrEmpty(pickedUpFlag)) WorldState.Set(pickedUpFlag);
        }

        public void AttachTo(Transform point)
        {
            IsHeld = true;
            if (body != null) { body.isKinematic = true; body.interpolation = RigidbodyInterpolation.None; }
            foreach (var c in colliders) c.enabled = false;
            transform.SetParent(point, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Detach(Vector3 inheritVelocity)
        {
            IsHeld = false;
            transform.SetParent(originParent, true);
            foreach (var c in colliders) c.enabled = true;
            if (body != null)
            {
                body.isKinematic = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                SetBodyVelocity(inheritVelocity * 0.5f);
            }
        }

        /// <summary>Lock in place inside a socket.</summary>
        public void SnapTo(Transform anchor)
        {
            IsHeld = false;
            transform.SetParent(anchor, true); // keep world scale whatever the socket's scale is
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            foreach (var c in colliders) c.enabled = true;
            if (body != null) body.isKinematic = true;
        }

        public void ReturnToOrigin()
        {
            if (Socket != null) Socket.Release(this);
            var interactor = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<PlayerInteractor>() : null;
            if (interactor != null && interactor.Held == this) interactor.ReleaseHeld();
            IsHeld = false;
            transform.SetParent(originParent, false);
            transform.localPosition = originPos;
            transform.localRotation = originRot;
            transform.localScale = originScale;
            foreach (var c in colliders) c.enabled = true;
            if (body != null) { body.isKinematic = false; SetBodyVelocity(Vector3.zero); }
        }

        void OnStateChanged(TimeState from, TimeState to)
        {
            if (IsHeld && !carryAcrossStates && from != to)
            {
                ReturnToOrigin();
                HUD.Toast("The " + displayName + " stays in the " + from.ToString().ToUpperInvariant() + ".", 2.5f);
            }
        }

        public void OnLoopReset()
        {
            if (resetOnLoop && Socket == null) ReturnToOrigin();
        }

        void SetBodyVelocity(Vector3 v)
        {
            if (body == null || body.isKinematic) return;
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = v;
#else
            body.velocity = v;
#endif
        }
    }
}
