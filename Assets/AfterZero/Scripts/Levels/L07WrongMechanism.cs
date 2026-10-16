using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 07: one of the two wrong bell drives. Using it never stops the bell; it jolts, sparks,
    /// warns the player and costs time on the CrisisTimer.
    /// </summary>
    public class L07WrongMechanism : Interactable
    {
        public CrisisTimer timer;
        public float penaltySeconds = 30f;
        [TextArea] public string warning = "Wrong drive. The bell shudders and the clock lurches forward.";
        public Transform lever;
        public Vector3 jolt = new Vector3(-35f, 0f, 0f);
        public ParticleSystem sparks;

        float cooldown;
        float anim;
        Quaternion baseRot;

        void Awake()
        {
            if (lever != null) baseRot = lever.localRotation;
        }

        public override bool IsUsableNow => base.IsUsableNow && cooldown <= 0f;

        protected override void OnInteract(PlayerInteractor who)
        {
            cooldown = 2.5f;
            anim = 1f;
            AudioCues.PlayAt(Cue.Clang, transform.position);
            AudioCues.Play(Cue.Alert);
            TemporalVisuals.Pulse(0.45f);
            if (sparks != null) sparks.Play();

            bool penalised = timer != null && timer.Running;
            if (penalised) timer.Penalize(penaltySeconds);
            HUD.Toast(warning + (penalised ? "  (-" + Mathf.RoundToInt(penaltySeconds) + "s)" : ""), 3.5f);
        }

        void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
            if (lever == null || anim <= 0f) return;
            anim = Mathf.MoveTowards(anim, 0f, Time.deltaTime * 1.5f);
            lever.localRotation = baseRot * Quaternion.Euler(jolt * Mathf.Sin(anim * Mathf.PI));
        }
    }
}
