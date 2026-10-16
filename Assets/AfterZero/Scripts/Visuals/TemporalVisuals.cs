using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterZero
{
    /// <summary>Complete look for one state: sun, ambience, fog, sky and post-processing grade.</summary>
    [Serializable]
    public class StateLook
    {
        [Header("Sun")]
        public Color sunColor = Color.white;
        public float sunIntensity = 1.2f;
        public Vector3 sunEuler = new Vector3(40f, -30f, 0f);

        [Header("Ambient / Fog / Sky")]
        public Color ambientSky = new Color(0.5f, 0.55f, 0.6f);
        public Color ambientEquator = new Color(0.35f, 0.35f, 0.38f);
        public Color ambientGround = new Color(0.15f, 0.14f, 0.13f);
        public Color fogColor = new Color(0.5f, 0.55f, 0.6f);
        public float fogDensity = 0.015f;
        public Color skyTint = new Color(0.5f, 0.5f, 0.5f);
        public float skyExposure = 1.2f;
        public float atmosphereThickness = 1f;

        [Header("Grade")]
        public float postExposure = 0f;
        public float contrast = 10f;
        public float saturation = 0f;
        public Color colorFilter = Color.white;
        public float temperature = 0f;
        public float tint = 0f;
        public Color splitShadows = new Color(0.5f, 0.5f, 0.5f);
        public Color splitHighlights = new Color(0.5f, 0.5f, 0.5f);
        public float bloomIntensity = 0.6f;
        public float bloomThreshold = 0.95f;
        public Color bloomTint = Color.white;
        public float vignette = 0.25f;
        public float chromatic = 0.05f;
        public float filmGrain = 0.1f;

        public static StateLook DefaultPast() => new StateLook
        {
            // Golden festival afternoon: warm, soft, nostalgic.
            sunColor = new Color(1f, 0.82f, 0.58f), sunIntensity = 1.6f, sunEuler = new Vector3(28f, -55f, 0f),
            ambientSky = new Color(0.78f, 0.66f, 0.5f), ambientEquator = new Color(0.55f, 0.42f, 0.3f), ambientGround = new Color(0.25f, 0.18f, 0.12f),
            fogColor = new Color(0.85f, 0.7f, 0.5f), fogDensity = 0.012f,
            skyTint = new Color(0.75f, 0.6f, 0.45f), skyExposure = 1.35f, atmosphereThickness = 1.4f,
            postExposure = 0.15f, contrast = 8f, saturation = -8f, colorFilter = new Color(1f, 0.95f, 0.86f),
            temperature = 22f, tint = 4f, splitShadows = new Color(0.45f, 0.38f, 0.3f), splitHighlights = new Color(0.7f, 0.6f, 0.45f),
            bloomIntensity = 0.8f, bloomThreshold = 0.9f, bloomTint = new Color(1f, 0.85f, 0.65f),
            vignette = 0.3f, chromatic = 0.03f, filmGrain = 0.35f
        };

        public static StateLook DefaultPresent() => new StateLook
        {
            // Rain-soaked dusk: cold teal, wet reflections, sodium lamps.
            sunColor = new Color(0.62f, 0.72f, 0.85f), sunIntensity = 0.55f, sunEuler = new Vector3(15f, 140f, 0f),
            ambientSky = new Color(0.28f, 0.36f, 0.45f), ambientEquator = new Color(0.18f, 0.22f, 0.27f), ambientGround = new Color(0.07f, 0.08f, 0.1f),
            fogColor = new Color(0.22f, 0.29f, 0.36f), fogDensity = 0.028f,
            skyTint = new Color(0.32f, 0.4f, 0.48f), skyExposure = 0.6f, atmosphereThickness = 2.2f,
            postExposure = -0.1f, contrast = 22f, saturation = -15f, colorFilter = new Color(0.88f, 0.96f, 1f),
            temperature = -18f, tint = -4f, splitShadows = new Color(0.3f, 0.45f, 0.55f), splitHighlights = new Color(0.65f, 0.55f, 0.45f),
            bloomIntensity = 1.1f, bloomThreshold = 0.85f, bloomTint = new Color(1f, 0.8f, 0.55f),
            vignette = 0.35f, chromatic = 0.06f, filmGrain = 0.15f
        };

        public static StateLook DefaultFuture() => new StateLook
        {
            // Split-open night: violet sky, cyan holograms, magenta neon — high bloom and fringe.
            sunColor = new Color(0.55f, 0.45f, 1f), sunIntensity = 0.35f, sunEuler = new Vector3(60f, 20f, 0f),
            ambientSky = new Color(0.18f, 0.12f, 0.35f), ambientEquator = new Color(0.08f, 0.14f, 0.25f), ambientGround = new Color(0.03f, 0.02f, 0.06f),
            fogColor = new Color(0.1f, 0.06f, 0.2f), fogDensity = 0.022f,
            skyTint = new Color(0.25f, 0.12f, 0.5f), skyExposure = 0.35f, atmosphereThickness = 0.4f,
            postExposure = 0.05f, contrast = 28f, saturation = 18f, colorFilter = new Color(0.9f, 0.92f, 1f),
            temperature = -8f, tint = 22f, splitShadows = new Color(0.35f, 0.3f, 0.65f), splitHighlights = new Color(0.4f, 0.8f, 0.85f),
            bloomIntensity = 2.2f, bloomThreshold = 0.75f, bloomTint = new Color(0.6f, 0.85f, 1f),
            vignette = 0.4f, chromatic = 0.22f, filmGrain = 0.08f
        };
    }

    /// <summary>
    /// The visual identity of the three states. Builds one global URP Volume per state and blends
    /// between them (plus lighting, fog and sky) on every shift, with a lens-warp "time ripple".
    /// Also drives Echo Sense: hold F to see holographic echoes of what exists in the other states.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class TemporalVisuals : MonoBehaviour
    {
        public static TemporalVisuals Instance { get; private set; }

        public StateLook past = StateLook.DefaultPast();
        public StateLook present = StateLook.DefaultPresent();
        public StateLook future = StateLook.DefaultFuture();

        public Light sun;
        public float blendTime = 0.9f;
        [Tooltip("Echoes always visible (used in the Nursery).")]
        public bool echoesAlwaysOn = false;

        Volume[] stateVolumes = new Volume[3];
        Volume rippleVolume;
        Volume memoryVolume;
        LensDistortion rippleLens;
        ChromaticAberration rippleChroma;
        MotionBlur rippleBlur;
        ColorAdjustments rippleColor;

        float[] weights = new float[3];
        TimeState target = TimeState.Present;
        float ripple;
        float rippleStrength = 1f;
        float memoryTarget;
        float echoReveal;
        Material skyInstance;

        static readonly int EchoRevealId = Shader.PropertyToID("_AZ_EchoReveal");

        StateLook Look(TimeState s) => s == TimeState.Past ? past : s == TimeState.Present ? present : future;

        void Awake()
        {
            Instance = this;
            if (sun == null) sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            }

            for (int i = 0; i < 3; i++)
                stateVolumes[i] = CreateVolume("Look_" + (TimeState)i, BuildStateProfile(Look((TimeState)i)), 10);
            BuildRipple();
            BuildMemory();

            if (RenderSettings.skybox != null)
            {
                skyInstance = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyInstance;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Trilight;
        }

        void OnEnable()
        {
            TemporalStateManager.StateChanged += OnStateChanged;
            TemporalStateManager.ShiftCompleted += OnShiftCompleted;
            TemporalStateManager.ShiftRefused += OnShiftRefused;
            LoopManager.LoopRestarted += OnLoopRestarted;
        }

        void OnDisable()
        {
            TemporalStateManager.StateChanged -= OnStateChanged;
            TemporalStateManager.ShiftCompleted -= OnShiftCompleted;
            TemporalStateManager.ShiftRefused -= OnShiftRefused;
            LoopManager.LoopRestarted -= OnLoopRestarted;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalFloat(EchoRevealId, 0f);
        }

        void Start()
        {
            target = TemporalStateManager.CurrentState;
            for (int i = 0; i < 3; i++) weights[i] = i == (int)target ? 1f : 0f;
            ApplyBlend();
        }

        void OnStateChanged(TimeState from, TimeState to) { target = to; }

        void OnShiftCompleted(TimeState from, TimeState to)
        {
            Pulse(1f);
            AZHaptics.Pulse();
            AudioCues.Play(to == TimeState.Past ? Cue.ShiftPast : to == TimeState.Present ? Cue.ShiftPresent : Cue.ShiftFuture);
        }

        void OnShiftRefused(TimeState t, string reason)
        {
            Pulse(0.25f);
            AudioCues.Play(Cue.ShiftBlocked);
            HUD.Toast(reason, 2.2f);
        }

        void OnLoopRestarted(int count) { Pulse(1.6f); }

        public static void Pulse(float strength)
        {
            if (Instance == null) return;
            Instance.ripple = 1f;
            Instance.rippleStrength = strength;
        }

        public static void SetMemoryMode(bool on)
        {
            if (Instance != null) Instance.memoryTarget = on ? 1f : 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool changed = false;
            for (int i = 0; i < 3; i++)
            {
                float goal = i == (int)target ? 1f : 0f;
                if (!Mathf.Approximately(weights[i], goal))
                {
                    weights[i] = Mathf.MoveTowards(weights[i], goal, dt / Mathf.Max(0.05f, blendTime));
                    changed = true;
                }
            }
            if (changed) ApplyBlend();

            // Time ripple: quick lens warp + fringe + blur that decays.
            if (ripple > 0f)
            {
                ripple = Mathf.MoveTowards(ripple, 0f, dt * 1.8f);
                float e = Mathf.Sin(ripple * Mathf.PI) * rippleStrength;
                rippleVolume.weight = Mathf.Clamp01(e);
                rippleLens.intensity.value = Mathf.Lerp(0f, -0.55f, Mathf.Clamp01(e));
                rippleLens.scale.value = Mathf.Lerp(1f, 1.08f, Mathf.Clamp01(e));
            }
            else if (rippleVolume.weight > 0f) rippleVolume.weight = 0f;

            memoryVolume.weight = Mathf.MoveTowards(memoryVolume.weight, memoryTarget, dt * 0.8f);

            bool echoHeld = !GameInput.GameplayLocked && GameInput.Echo != null && GameInput.Echo.IsPressed();
            float echoGoal = echoesAlwaysOn || echoHeld ? 1f : 0f;
            echoReveal = Mathf.MoveTowards(echoReveal, echoGoal, dt * 3f);
            Shader.SetGlobalFloat(EchoRevealId, echoReveal);
            TemporalPresence.SetEchoRendering(echoReveal > 0.001f);
        }

        void ApplyBlend()
        {
            for (int i = 0; i < 3; i++) stateVolumes[i].weight = weights[i];

            float total = weights[0] + weights[1] + weights[2];
            if (total <= 0f) return;
            float w0 = weights[0] / total, w1 = weights[1] / total, w2 = weights[2] / total;

            Color C(Func<StateLook, Color> f) => f(past) * w0 + f(present) * w1 + f(future) * w2;
            float F(Func<StateLook, float> f) => f(past) * w0 + f(present) * w1 + f(future) * w2;

            if (sun != null)
            {
                sun.color = C(l => l.sunColor);
                sun.intensity = F(l => l.sunIntensity);
                float pastPresent = w0 + w1 > 0.0001f ? w1 / (w0 + w1) : 1f;
                Quaternion q = Quaternion.Slerp(Quaternion.Euler(past.sunEuler), Quaternion.Euler(present.sunEuler), pastPresent);
                q = Quaternion.Slerp(q, Quaternion.Euler(future.sunEuler), w2);
                sun.transform.rotation = q;
            }

            RenderSettings.ambientSkyColor = C(l => l.ambientSky);
            RenderSettings.ambientEquatorColor = C(l => l.ambientEquator);
            RenderSettings.ambientGroundColor = C(l => l.ambientGround);
            RenderSettings.fogColor = C(l => l.fogColor);
            RenderSettings.fogDensity = F(l => l.fogDensity);

            if (skyInstance != null)
            {
                if (skyInstance.HasProperty("_SkyTint")) skyInstance.SetColor("_SkyTint", C(l => l.skyTint));
                if (skyInstance.HasProperty("_Exposure")) skyInstance.SetFloat("_Exposure", F(l => l.skyExposure));
                if (skyInstance.HasProperty("_AtmosphereThickness")) skyInstance.SetFloat("_AtmosphereThickness", F(l => l.atmosphereThickness));
                if (skyInstance.HasProperty("_GroundColor")) skyInstance.SetColor("_GroundColor", C(l => l.fogColor));
            }
        }

        // ---------- profile construction ----------

        Volume CreateVolume(string name, VolumeProfile profile, float priority)
        {
            var go = new GameObject("[Volume] " + name);
            go.transform.SetParent(transform, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = priority;
            v.weight = 0f;
            v.sharedProfile = profile;
            return v;
        }

        static VolumeProfile BuildStateProfile(StateLook l)
        {
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);

            var ca = p.Add<ColorAdjustments>(true);
            ca.postExposure.Override(l.postExposure);
            ca.contrast.Override(l.contrast);
            ca.saturation.Override(l.saturation);
            ca.colorFilter.Override(l.colorFilter);

            var wb = p.Add<WhiteBalance>(true);
            wb.temperature.Override(l.temperature);
            wb.tint.Override(l.tint);

            var st = p.Add<SplitToning>(true);
            st.shadows.Override(l.splitShadows);
            st.highlights.Override(l.splitHighlights);
            st.balance.Override(0f);

            var bl = p.Add<Bloom>(true);
            bl.intensity.Override(l.bloomIntensity);
            bl.threshold.Override(l.bloomThreshold);
            bl.scatter.Override(0.72f);
            bl.tint.Override(l.bloomTint);
            bl.highQualityFiltering.Override(true);

            var vg = p.Add<Vignette>(true);
            vg.intensity.Override(l.vignette);
            vg.smoothness.Override(0.45f);

            var cab = p.Add<ChromaticAberration>(true);
            cab.intensity.Override(l.chromatic);

            var fg = p.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Thin1);
            fg.intensity.Override(l.filmGrain);
            return p;
        }

        void BuildRipple()
        {
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            rippleLens = p.Add<LensDistortion>(true);
            rippleLens.intensity.Override(0f);
            rippleLens.scale.Override(1f);
            rippleChroma = p.Add<ChromaticAberration>(true);
            rippleChroma.intensity.Override(1f);
            rippleBlur = p.Add<MotionBlur>(true);
            rippleBlur.intensity.Override(0.6f);
            rippleColor = p.Add<ColorAdjustments>(true);
            rippleColor.postExposure.Override(0.6f);
            rippleVolume = CreateVolume("TimeRipple", p, 50);
        }

        void BuildMemory()
        {
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = p.Add<ColorAdjustments>(true);
            ca.saturation.Override(-85f);
            ca.colorFilter.Override(new Color(1f, 0.93f, 0.82f));
            ca.contrast.Override(-10f);
            var vg = p.Add<Vignette>(true);
            vg.intensity.Override(0.55f);
            vg.smoothness.Override(0.8f);
            var fg = p.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Medium4);
            fg.intensity.Override(0.7f);
            var bl = p.Add<Bloom>(true);
            bl.intensity.Override(1.6f);
            memoryVolume = CreateVolume("Memory", p, 40);
        }
    }
}
