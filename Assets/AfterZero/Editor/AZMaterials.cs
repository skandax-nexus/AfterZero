using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// Generates the project's material library (URP Lit + custom hologram) under
    /// Assets/AfterZero/Generated/Materials. Level builders refer to materials by key.
    /// Swap any generated material for a textured one later and every level picks it up.
    /// </summary>
    public static class AZMaterials
    {
        public const string Root = "Assets/AfterZero/Generated";
        public const string MatFolder = Root + "/Materials";
        public const string ResourcesFolder = "Assets/AfterZero/Resources/AfterZero";

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        struct Spec
        {
            public Color color; public float metallic; public float smooth;
            public Color emission; public bool transparent; public float alpha;
            public Spec(Color c, float m, float s) { color = c; metallic = m; smooth = s; emission = Color.black; transparent = false; alpha = 1f; }
        }

        static Spec S(float r, float g, float b, float metal = 0f, float smooth = 0.3f) => new Spec(new Color(r, g, b), metal, smooth);
        static Spec E(float r, float g, float b, float intensity) { var s = S(r * 0.2f, g * 0.2f, b * 0.2f, 0f, 0.6f); s.emission = new Color(r, g, b) * intensity; return s; }
        static Spec T(float r, float g, float b, float alpha, float smooth) { var s = S(r, g, b, 0f, smooth); s.transparent = true; s.alpha = alpha; return s; }

        /// <summary>Every material key available to level builders.</summary>
        static readonly Dictionary<string, Spec> specs = new Dictionary<string, Spec>
        {
            // --- Past: warm, crafted, sunlit ---
            { "stone_warm",      S(0.78f, 0.70f, 0.58f, 0f, 0.25f) },
            { "plaster_warm",    S(0.90f, 0.82f, 0.70f, 0f, 0.15f) },
            { "brick_warm",      S(0.62f, 0.36f, 0.26f, 0f, 0.2f) },
            { "wood",            S(0.45f, 0.30f, 0.18f, 0f, 0.35f) },
            { "wood_dark",       S(0.25f, 0.16f, 0.10f, 0f, 0.4f) },
            { "brass",           S(0.85f, 0.65f, 0.30f, 1f, 0.75f) },
            { "fabric_red",      S(0.70f, 0.16f, 0.14f, 0f, 0.1f) },
            { "fabric_gold",     S(0.85f, 0.62f, 0.20f, 0f, 0.15f) },
            { "fabric_teal",     S(0.10f, 0.45f, 0.48f, 0f, 0.1f) },
            { "paper",           S(0.93f, 0.90f, 0.80f, 0f, 0.05f) },
            // --- Present: wet, cold, worn ---
            { "stone_wet",       S(0.20f, 0.22f, 0.25f, 0f, 0.88f) },
            { "plaster_cold",    S(0.42f, 0.46f, 0.50f, 0f, 0.55f) },
            { "metal_dark",      S(0.16f, 0.17f, 0.19f, 1f, 0.65f) },
            { "iron_rust",       S(0.35f, 0.20f, 0.12f, 0.6f, 0.35f) },
            { "copper_verdigris",S(0.30f, 0.55f, 0.48f, 0.7f, 0.45f) },
            { "rail",            S(0.55f, 0.56f, 0.58f, 1f, 0.8f) },
            { "concrete",        S(0.38f, 0.38f, 0.37f, 0f, 0.3f) },
            { "tile_cold",       S(0.60f, 0.68f, 0.70f, 0f, 0.8f) },
            { "cup_blue",        S(0.10f, 0.32f, 0.85f, 0f, 0.9f) },
            // --- Future: fractured, obsidian, neon ---
            { "obsidian",        S(0.04f, 0.04f, 0.06f, 0.4f, 0.95f) },
            { "ruin_dark",       S(0.12f, 0.11f, 0.14f, 0f, 0.35f) },
            { "crystal",         S(0.35f, 0.25f, 0.70f, 0.2f, 0.95f) },
            { "mineral_trace",   E(0.55f, 1.00f, 0.80f, 2.2f) },
            // --- Emissive / signage ---
            { "neon_cyan",       E(0.20f, 0.90f, 1.00f, 4f) },
            { "neon_magenta",    E(1.00f, 0.20f, 0.80f, 4f) },
            { "neon_amber",      E(1.00f, 0.62f, 0.20f, 3.5f) },
            { "neon_violet",     E(0.55f, 0.30f, 1.00f, 4f) },
            { "lamp_warm",       E(1.00f, 0.78f, 0.45f, 5f) },
            { "lamp_cold",       E(0.70f, 0.85f, 1.00f, 4f) },
            { "window_warm",     E(1.00f, 0.70f, 0.40f, 1.6f) },
            { "window_cold",     E(0.40f, 0.65f, 0.90f, 1.0f) },
            { "warning_red",     E(1.00f, 0.15f, 0.10f, 4f) },
            { "rail_strip",      E(0.20f, 0.90f, 1.00f, 0.5f) },
            { "zero_glow",       E(0.90f, 0.95f, 1.00f, 6f) },
            // --- Transparent ---
            { "glass",           T(0.65f, 0.80f, 0.90f, 0.25f, 0.95f) },
            { "water",           T(0.08f, 0.22f, 0.28f, 0.72f, 0.97f) },
            { "water_clear",     T(0.15f, 0.40f, 0.45f, 0.45f, 0.97f) },
            // --- Characters ---
            { "skin",            S(0.80f, 0.62f, 0.50f, 0f, 0.35f) },
            { "skin_dark",       S(0.45f, 0.30f, 0.22f, 0f, 0.35f) },
            { "coat_navy",       S(0.12f, 0.16f, 0.28f, 0f, 0.25f) },
            { "coat_ochre",      S(0.65f, 0.45f, 0.16f, 0f, 0.2f) },
            { "coat_olive",      S(0.30f, 0.35f, 0.18f, 0f, 0.2f) },
            { "coat_burgundy",   S(0.40f, 0.10f, 0.14f, 0f, 0.25f) },
            { "coat_grey",       S(0.40f, 0.40f, 0.42f, 0f, 0.25f) },
            { "coat_white",      S(0.85f, 0.85f, 0.82f, 0f, 0.25f) },
            { "arin_coat",       S(0.10f, 0.11f, 0.13f, 0.2f, 0.5f) },
            { "visor",           E(0.30f, 0.95f, 1.00f, 3f) },
            // --- Ground ---
            { "ground_dark",     S(0.10f, 0.10f, 0.11f, 0f, 0.4f) },
            { "grass_dry",       S(0.45f, 0.42f, 0.22f, 0f, 0.1f) },
            { "mud",             S(0.22f, 0.18f, 0.13f, 0f, 0.6f) },
        };

        public static IEnumerable<string> Keys => specs.Keys;

        public static Material Get(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "concrete";
            if (cache.TryGetValue(key, out var m) && m != null) return m;

            if (key == "hologram" || key == "hologram_amber" || key == "hologram_magenta") return Hologram(key);

            string path = MatFolder + "/AZ_" + key + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                if (!specs.TryGetValue(key, out var spec))
                {
                    Debug.LogWarning("[AfterZero] Unknown material key '" + key + "', using concrete.");
                    return Get("concrete");
                }
                EnsureFolder(MatFolder);
                m = new Material(LitShader());
                Configure(m, spec);
                AssetDatabase.CreateAsset(m, path);
            }
            cache[key] = m;
            return m;
        }

        public static Material Hologram(string key = "hologram")
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            string path = MatFolder + "/AZ_" + key + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                EnsureFolder(MatFolder);
                m = new Material(HologramShader());
                Color c = key == "hologram_amber" ? new Color(1f, 0.6f, 0.2f) : key == "hologram_magenta" ? new Color(1f, 0.25f, 0.8f) : new Color(0.2f, 0.8f, 1f);
                m.SetColor("_BaseColor", c * 1.2f);
                m.SetColor("_RimColor", Color.Lerp(c, Color.white, 0.4f) * 2f);
                m.SetFloat("_Alpha", 0.65f);
                AssetDatabase.CreateAsset(m, path);
            }
            cache[key] = m;
            return m;
        }

        /// <summary>Material loaded at runtime by TemporalPresence for echoes (must live in Resources).</summary>
        public static Material EchoMaterial()
        {
            string path = ResourcesFolder + "/EchoHologram.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            EnsureFolder(ResourcesFolder);
            m = new Material(HologramShader());
            m.SetColor("_BaseColor", new Color(0.3f, 0.7f, 1f) * 0.6f);
            m.SetColor("_RimColor", new Color(0.6f, 0.95f, 1f) * 1.6f);
            m.SetFloat("_Alpha", 0.5f);
            m.SetFloat("_GridStrength", 0.15f);
            m.SetFloat("_UseEchoReveal", 1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static Material ParticleMaterial(string key, Color color, bool additive)
        {
            string cacheKey = "particle_" + key;
            if (cache.TryGetValue(cacheKey, out var m) && m != null) return m;
            string path = MatFolder + "/AZ_" + cacheKey + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                EnsureFolder(MatFolder);
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                m = new Material(shader);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (additive) m.EnableKeyword("_BLENDMODE_ADD");
                m.renderQueue = (int)RenderQueue.Transparent;
                var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
                if (tex != null) m.SetTexture("_BaseMap", tex);
                AssetDatabase.CreateAsset(m, path);
            }
            cache[cacheKey] = m;
            return m;
        }

        public static Material Skybox()
        {
            string path = MatFolder + "/AZ_Skybox.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            EnsureFolder(MatFolder);
            m = new Material(Shader.Find("Skybox/Procedural"));
            m.SetFloat("_SunSize", 0.035f);
            m.SetFloat("_AtmosphereThickness", 1.2f);
            m.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.55f));
            m.SetColor("_GroundColor", new Color(0.2f, 0.2f, 0.22f));
            m.SetFloat("_Exposure", 1.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void Configure(Material m, Spec s)
        {
            m.SetColor("_BaseColor", new Color(s.color.r, s.color.g, s.color.b, s.alpha));
            m.SetFloat("_Metallic", s.metallic);
            m.SetFloat("_Smoothness", s.smooth);
            if (s.emission.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", s.emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (s.transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
                m.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            m.enableInstancing = true;
        }

        static Shader LitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null)
            {
                Debug.LogError("[AfterZero] URP Lit shader not found. Create the project from the Universal 3D (URP) template.");
                s = Shader.Find("Standard");
            }
            return s;
        }

        static Shader HologramShader()
        {
            var s = Shader.Find("AfterZero/Hologram");
            if (s == null) s = AssetDatabase.LoadAssetAtPath<Shader>("Assets/AfterZero/Shaders/Hologram.shader");
            return s;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static void ClearCache() => cache.Clear();
    }
}
