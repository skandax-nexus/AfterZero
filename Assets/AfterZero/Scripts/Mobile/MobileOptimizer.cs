using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AfterZero
{
    /// <summary>
    /// Keeps phones smooth. On mobile it applies a quality preset (Low / Medium / High, or Auto) and,
    /// in Auto, adapts render scale to the measured frame time (dynamic resolution) so the game holds
    /// its target frame rate as scenes get heavier. Desktop builds are untouched.
    /// </summary>
    public class MobileOptimizer : MonoBehaviour
    {
        struct Preset { public float scale, shadow, particles; public int fps; public bool hdrBloomHQ; }

        static readonly Preset[] presets =
        {
            new Preset { scale = 0.65f, shadow = 18f, particles = 0.25f, fps = 30 },
            new Preset { scale = 0.8f, shadow = 32f, particles = 0.4f, fps = 60 },
            new Preset { scale = 1.0f, shadow = 50f, particles = 0.7f, fps = 60 },
        };

        static MobileOptimizer instance;
        float avg = 1f / 60f;
        float adjustTimer;
        float scale = 0.8f;

        public static float CurrentScale => instance != null ? instance.scale : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!AZPlatform.IsMobile || instance != null) return;
            var go = new GameObject("[AfterZero MobileOptimizer]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MobileOptimizer>();
        }

        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            ApplyPreset();
            GameSettings.Changed += ApplyPreset;
            SceneManager.sceneLoaded += (s, m) => OptimizeScene();
            OptimizeScene();
        }

        void OnDestroy() { GameSettings.Changed -= ApplyPreset; }

        Preset Current => presets[GameSettings.Quality < 0 ? 1 : GameSettings.Quality];

        void ApplyPreset()
        {
            var p = Current;
            Application.targetFrameRate = p.fps;
            scale = p.scale;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = scale;
                urp.shadowDistance = p.shadow;
                urp.msaaSampleCount = 1;
                urp.supportsCameraOpaqueTexture = false;
            }
        }

        void Update()
        {
            // Dynamic resolution only in Auto mode.
            if (GameSettings.Quality >= 0) return;
            avg = Mathf.Lerp(avg, Time.unscaledDeltaTime, 0.05f);
            adjustTimer += Time.unscaledDeltaTime;
            if (adjustTimer < 1.5f) return;
            adjustTimer = 0f;
            float target = 1f / Mathf.Max(30, Application.targetFrameRate);
            if (avg > target * 1.15f && scale > 0.55f) scale -= 0.05f;
            else if (avg < target * 0.9f && scale < 0.9f) scale += 0.025f;
            else return;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) urp.renderScale = scale;
        }

        void OptimizeScene()
        {
            var p = Current;
            foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var main = ps.main;
                main.maxParticles = Mathf.Max(20, Mathf.RoundToInt(main.maxParticles * p.particles));
                var em = ps.emission;
                em.rateOverTimeMultiplier *= p.particles;
            }
            int shadowed = 0;
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) continue;
                if (l.shadows != LightShadows.None && shadowed++ >= 1) l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
            }
            int probes = 0;
            foreach (var r in FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                r.resolution = 64;
                if (probes++ >= 2) r.enabled = false;
            }
        }
    }
}
