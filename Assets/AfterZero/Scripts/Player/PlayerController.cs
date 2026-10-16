using System.Collections;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// First-person controller for Arin. Handles movement, look, state shifting,
    /// footstep noise (for the vibration-tracking machine) and respawning.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("References")]
        public Camera playerCamera;
        public Transform cameraPivot;

        [Header("Movement")]
        public float walkSpeed = 3.6f;
        public float sprintSpeed = 6.2f;
        public float crouchSpeed = 1.8f;
        public float jumpHeight = 1.1f;
        public float gravity = -19.6f;
        public float acceleration = 14f;
        public float pushForce = 16f;

        [Header("Look")]
        public float mouseSensitivity = 0.08f;
        [Tooltip("Degrees per screen pixel when dragging to look on a touch screen.")]
        public float touchSensitivity = 0.12f;
        public float pitchLimit = 85f;

        [Header("Feel")]
        public float baseFov = 72f;
        public float sprintFovBoost = 7f;
        public float bobAmount = 0.035f;
        public float standHeight = 1.75f;
        public float crouchHeight = 1.1f;

        [Header("Noise (vibration)")]
        public float walkNoise = 0.45f;
        public float sprintNoise = 1f;
        public float crouchNoise = 0.1f;
        public float landNoise = 1.2f;
        public float stepInterval = 0.5f;

        public CharacterController Controller { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector3 Velocity => velocity;

        Vector3 velocity;
        float pitch;
        float yaw;
        float stepTimer;
        float bobPhase;
        bool wasGrounded = true;
        bool dying;
        float camBaseY;

        void Awake()
        {
            Instance = this;
            Controller = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (cameraPivot == null && playerCamera != null) cameraPivot = playerCamera.transform.parent;
            yaw = transform.eulerAngles.y;
            camBaseY = cameraPivot != null ? cameraPivot.localPosition.y : 1.6f;
            Checkpoint.SetFallback(transform.position, transform.rotation);
        }

        void OnEnable()
        {
            GameInput.Ensure();
            GameInput.UpdateCursor();
        }

        void Start()
        {
            if (TemporalStateManager.Instance != null)
                TemporalStateManager.Instance.isPlayerObstructed = IsObstructed;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (dying) return;
            bool locked = GameInput.GameplayLocked;
            HandleLook(locked);
            HandleMove(locked);
            if (!locked) HandleStateInput();
            HandleFeel();
        }

        void HandleLook(bool locked)
        {
            if (locked) return;
            Vector2 look = GameInput.Look.ReadValue<Vector2>() * mouseSensitivity
                         + TouchLookArea.Consume() * touchSensitivity * (1080f / Mathf.Max(360f, Screen.height));
            look *= GameSettings.LookSensitivity;
            if (GameSettings.InvertY) look.y = -look.y;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleMove(bool locked)
        {
            bool grounded = Controller.isGrounded;
            Vector2 input = locked ? Vector2.zero : Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f);

            IsCrouching = !locked && GameInput.Crouch.IsPressed();
            // On touch screens, pushing the stick to the rim sprints (no extra button needed).
            bool touchSprint = AZPlatform.UseTouchControls && input.magnitude > 0.95f;
            IsSprinting = !locked && !IsCrouching && (GameInput.Sprint.IsPressed() || touchSprint) && input.y > 0.1f;

            float targetHeight = IsCrouching ? crouchHeight : standHeight;
            if (!IsCrouching && Controller.height < standHeight - 0.01f && Physics.SphereCast(transform.position + Vector3.up * Controller.height * 0.5f,
                    Controller.radius * 0.9f, Vector3.up, out _, standHeight - Controller.height + 0.05f, ~0, QueryTriggerInteraction.Ignore))
                targetHeight = Controller.height; // something above: stay crouched
            Controller.height = Mathf.MoveTowards(Controller.height, targetHeight, Time.deltaTime * 4f);
            Controller.center = new Vector3(0f, Controller.height * 0.5f, 0f);

            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 wish = (transform.right * input.x + transform.forward * input.y) * speed;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, wish, acceleration * Time.deltaTime * Mathf.Max(1f, speed));
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (grounded && velocity.y < 0f) velocity.y = -2f;
            if (grounded && !locked && GameInput.Jump.WasPressedThisFrame() && !IsCrouching)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            velocity.y += gravity * Time.deltaTime;

            Controller.Move(velocity * Time.deltaTime);

            // Footsteps → noise events and step sounds.
            float planar = horizontal.magnitude;
            if (grounded && planar > 0.5f)
            {
                stepTimer -= Time.deltaTime * (planar / walkSpeed);
                if (stepTimer <= 0f)
                {
                    stepTimer = stepInterval;
                    float loud = IsCrouching ? crouchNoise : IsSprinting ? sprintNoise : walkNoise;
                    NoiseSystem.Emit(transform.position, loud, gameObject);
                    AudioCues.PlayAt(Cue.Footstep, transform.position, IsCrouching ? 0.15f : IsSprinting ? 0.55f : 0.35f);
                }
            }

            if (grounded && !wasGrounded && velocity.y < -6f)
                NoiseSystem.Emit(transform.position, landNoise, gameObject);
            wasGrounded = grounded;

            if (transform.position.y < -60f) Die("You fell between the states.");
        }

        void HandleStateInput()
        {
            var tsm = TemporalStateManager.Instance;
            if (tsm == null) return;
            if (GameInput.Hum.WasPressedThisFrame()) ArinHum.TryHum(this, transform.position);
            if (GameInput.StatePast.WasPressedThisFrame()) tsm.TryShift(TimeState.Past);
            else if (GameInput.StatePresent.WasPressedThisFrame()) tsm.TryShift(TimeState.Present);
            else if (GameInput.StateFuture.WasPressedThisFrame()) tsm.TryShift(TimeState.Future);
            else if (GameInput.StatePrev.WasPressedThisFrame()) tsm.Step(-1);
            else if (GameInput.StateNext.WasPressedThisFrame()) tsm.Step(1);
        }

        void HandleFeel()
        {
            if (playerCamera == null || cameraPivot == null) return;
            float targetFov = baseFov + (IsSprinting ? sprintFovBoost : 0f);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * 6f);

            float planar = new Vector2(velocity.x, velocity.z).magnitude;
            bobPhase += Time.deltaTime * planar * 2.2f;
            float bob = Controller.isGrounded ? Mathf.Sin(bobPhase) * bobAmount * Mathf.Clamp01(planar / walkSpeed) : 0f;
            float eye = camBaseY - (standHeight - Controller.height);
            var lp = cameraPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, eye + bob, Time.deltaTime * 12f);
            cameraPivot.localPosition = lp;
        }

        /// <summary>Would the player's capsule overlap solid geometry right now?</summary>
        public bool IsObstructed()
        {
            float r = Controller.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * (r + 0.05f);
            Vector3 top = transform.position + Vector3.up * (Controller.height - r - 0.05f);
            var hits = Physics.OverlapCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(transform)) continue;
                if (h.attachedRigidbody != null && !h.attachedRigidbody.isKinematic) continue;
                return true;
            }
            return false;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Controller.enabled = false;
            transform.position = position;
            yaw = rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            velocity = Vector3.zero;
            Controller.enabled = true;
        }

        public void Die(string message)
        {
            if (dying) return;
            StartCoroutine(DieRoutine(message));
        }

        IEnumerator DieRoutine(string message)
        {
            dying = true;
            AudioCues.Play(Cue.Hurt);
            AZHaptics.Pulse();
            if (!string.IsNullOrEmpty(message)) HUD.Toast(message, 3f);
            yield return ScreenFader.FadeOut(0.45f);
            var held = GetComponent<PlayerInteractor>();
            if (held != null) held.DropHeld();
            Checkpoint.RespawnPlayer();
            yield return new WaitForSeconds(0.2f);
            dying = false;
            yield return ScreenFader.FadeIn(0.6f);
        }

        void LateUpdate()
        {
            // Ride moving platforms (gear teeth, lifts, rafts).
            if (dying || !Controller.isGrounded) return;
            if (!Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.6f, ~0, QueryTriggerInteraction.Ignore)) return;
            var platform = hit.collider.GetComponentInParent<MovingPlatform>();
            if (platform == null) return;
            Vector3 delta = platform.DeltaFor(transform.position);
            if (delta.sqrMagnitude > 0.0000001f && delta.sqrMagnitude < 1f) Controller.Move(delta);
            float yawDelta = platform.YawDelta();
            if (Mathf.Abs(yawDelta) > 0.0001f && Mathf.Abs(yawDelta) < 30f) { yaw += yawDelta; transform.rotation = Quaternion.Euler(0f, yaw, 0f); }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic || hit.moveDirection.y < -0.3f) return;
            Vector3 push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            body.AddForce(push.normalized * pushForce, ForceMode.Acceleration);
            var pushable = body.GetComponent<Pushable>();
            if (pushable != null) pushable.NotifyPushed();
        }
    }
}
