using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Cycles Idle → Warning → Active. During Warning the hazard telegraphs with light, sound,
    /// motion and particles (dust, sparks, steam) so damage is always readable before it lands.
    /// </summary>
    public class TelegraphedHazard : MonoBehaviour
    {
        public float idleTime = 3f;
        public float warnTime = 1.4f;
        public float activeTime = 1.2f;
        public float startOffset = 0f;
        public TimeMask activeIn = TimeMask.All;
        public string disabledByFlag = "";

        [Header("Visuals")]
        public Light warnLight;
        public ParticleSystem warnParticles;
        public ParticleSystem activeParticles;
        public Transform shakeTarget;
        public GameObject activeVisual;
        public Cue warnCue = Cue.HazardWarn;
        public Cue activeCue = Cue.HazardActive;

        [Header("Damage")]
        public Collider damageZone;
        [TextArea] public string deathMessage = "Watch for the warning before it strikes.";

        enum Phase { Idle, Warning, Active }
        Phase phase = Phase.Idle;
        float timer;
        Vector3 shakeBase;
        float baseLightIntensity;

        void Start()
        {
            timer = idleTime - startOffset;
            if (shakeTarget != null) shakeBase = shakeTarget.localPosition;
            if (warnLight != null) { baseLightIntensity = warnLight.intensity; warnLight.enabled = false; }
            if (damageZone != null) damageZone.isTrigger = true;
            SetPhase(Phase.Idle);
        }

        bool Enabled => activeIn.Contains(TemporalStateManager.CurrentState) && !WorldState.Get(disabledByFlag);

        void Update()
        {
            if (!Enabled) { if (phase != Phase.Idle) SetPhase(Phase.Idle); return; }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                switch (phase)
                {
                    case Phase.Idle: SetPhase(Phase.Warning); break;
                    case Phase.Warning: SetPhase(Phase.Active); break;
                    default: SetPhase(Phase.Idle); break;
                }
            }

            if (phase == Phase.Warning)
            {
                float k = 1f - Mathf.Clamp01(timer / warnTime);
                if (warnLight != null) warnLight.intensity = baseLightIntensity * (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(8f, 30f, k)));
                if (shakeTarget != null) shakeTarget.localPosition = shakeBase + Random.insideUnitSphere * 0.02f * k;
            }
        }

        void SetPhase(Phase p)
        {
            phase = p;
            switch (p)
            {
                case Phase.Idle:
                    timer = idleTime;
                    if (warnLight != null) warnLight.enabled = false;
                    if (warnParticles != null) warnParticles.Stop();
                    if (activeParticles != null) activeParticles.Stop();
                    if (activeVisual != null) activeVisual.SetActive(false);
                    if (shakeTarget != null) shakeTarget.localPosition = shakeBase;
                    if (damageZone != null) damageZone.enabled = false;
                    break;
                case Phase.Warning:
                    timer = warnTime;
                    if (warnLight != null) warnLight.enabled = true;
                    if (warnParticles != null) warnParticles.Play();
                    AudioCues.PlayAt(warnCue, transform.position);
                    break;
                case Phase.Active:
                    timer = activeTime;
                    if (warnParticles != null) warnParticles.Stop();
                    if (activeParticles != null) activeParticles.Play();
                    if (activeVisual != null) activeVisual.SetActive(true);
                    if (damageZone != null) damageZone.enabled = true;
                    AudioCues.PlayAt(activeCue, transform.position);
                    break;
            }
        }

        void LateUpdate()
        {
            // Manual overlap test: reliable even when neither body is moving.
            if (phase != Phase.Active || damageZone == null || !damageZone.enabled) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            var b = damageZone.bounds;
            b.Expand(new Vector3(0.6f, 0f, 0.6f));
            Vector3 p = player.transform.position + Vector3.up * 0.9f;
            if (b.Contains(p)) player.Die(deathMessage);
        }
    }
}
