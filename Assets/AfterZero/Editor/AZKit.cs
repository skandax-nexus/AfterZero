using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// Greybox + dressing toolkit used by the level builders. Everything is plain GameObjects with
    /// primitives, URP materials, lights and particle systems so levels are playable immediately and
    /// can be art-passed later (replace meshes/materials, keep the gameplay components).
    /// </summary>
    public static class AZKit
    {
        // =====================================================================
        // Hierarchy
        // =====================================================================

        public static Transform Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>Root whose children only exist in the given states (Past/Present/Future dressing).</summary>
        public static Transform StateGroup(string name, Transform parent, TimeMask existsIn, bool showEcho = true)
        {
            var t = Group(name + " [" + existsIn + "]", parent);
            var p = t.gameObject.AddComponent<TemporalPresence>();
            p.existsIn = existsIn;
            p.showEcho = showEcho;
            return t;
        }

        public static Transform Marker(string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            var t = Group(name, parent);
            t.position = position;
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        // =====================================================================
        // Geometry
        // =====================================================================

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, string mat, bool collider = true, bool isStatic = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = AZMaterials.Get(mat);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (type == PrimitiveType.Cylinder)
            {
                // Unity's cylinder primitive uses a capsule collider, which turns flat discs into spheres.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = !isStatic;
            }
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, string mat, bool collider = true, float yaw = 0f)
        {
            var go = Prim(PrimitiveType.Cube, name, parent, center, size, mat, collider);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 center, float radius, float height, string mat, bool collider = true)
        {
            return Prim(PrimitiveType.Cylinder, name, parent, center, new Vector3(radius * 2f, height * 0.5f, radius * 2f), mat, collider);
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 center, float radius, string mat, bool collider = true)
        {
            return Prim(PrimitiveType.Sphere, name, parent, center, Vector3.one * radius * 2f, mat, collider);
        }

        /// <summary>Floor slab whose TOP surface sits at center.y.</summary>
        public static GameObject Floor(string name, Transform parent, Vector3 center, Vector2 size, string mat, float thickness = 0.5f)
        {
            return Box(name, parent, center - Vector3.up * thickness * 0.5f, new Vector3(size.x, thickness, size.y), mat);
        }

        /// <summary>Wall from a to b (ground points), standing on a.y.</summary>
        public static GameObject Wall(string name, Transform parent, Vector3 a, Vector3 b, float height, float thickness, string mat)
        {
            Vector3 d = b - a; d.y = 0f;
            float len = d.magnitude;
            var go = Box(name, parent, (a + b) * 0.5f + Vector3.up * height * 0.5f, new Vector3(thickness, height, len), mat);
            if (len > 0.001f) go.transform.rotation = Quaternion.LookRotation(d.normalized);
            return go;
        }

        /// <summary>Wall from a to b with a doorway gap in the middle. Returns the wall group.</summary>
        public static Transform WallWithDoor(string name, Transform parent, Vector3 a, Vector3 b, float height, float thickness, string mat, float doorWidth = 2f, float doorHeight = 2.6f, float doorCenter = 0.5f)
        {
            var g = Group(name, parent);
            Vector3 d = b - a; d.y = 0f;
            float len = d.magnitude;
            Vector3 dir = d.normalized;
            float c = Mathf.Clamp(doorCenter * len, doorWidth * 0.5f, len - doorWidth * 0.5f);
            Vector3 l1 = a + dir * (c - doorWidth * 0.5f);
            Vector3 l2 = a + dir * (c + doorWidth * 0.5f);
            if ((l1 - a).magnitude > 0.05f) Wall("Left", g, a, l1, height, thickness, mat);
            if ((b - l2).magnitude > 0.05f) Wall("Right", g, l2, b, height, thickness, mat);
            if (height - doorHeight > 0.05f)
            {
                var lintel = Wall("Lintel", g, l1, l2, height - doorHeight, thickness, mat);
                lintel.transform.position += Vector3.up * doorHeight;
            }
            return g;
        }

        /// <summary>Four walls around a rectangle (optionally with door gaps on given sides: 0=N,1=E,2=S,3=W).</summary>
        public static Transform Room(string name, Transform parent, Vector3 center, Vector2 size, float height, string wallMat, string floorMat, string ceilingMat = null, params int[] doorSides)
        {
            var g = Group(name, parent);
            if (!string.IsNullOrEmpty(floorMat)) Floor("Floor", g, center, size, floorMat);
            float hx = size.x * 0.5f, hz = size.y * 0.5f;
            Vector3 nw = center + new Vector3(-hx, 0, hz), ne = center + new Vector3(hx, 0, hz);
            Vector3 se = center + new Vector3(hx, 0, -hz), sw = center + new Vector3(-hx, 0, -hz);
            var sides = new[] { (nw, ne), (ne, se), (se, sw), (sw, nw) };
            var doors = new HashSet<int>(doorSides ?? new int[0]);
            for (int i = 0; i < 4; i++)
            {
                if (doors.Contains(i)) WallWithDoor("Wall" + i, g, sides[i].Item1, sides[i].Item2, height, 0.4f, wallMat);
                else Wall("Wall" + i, g, sides[i].Item1, sides[i].Item2, height, 0.4f, wallMat);
            }
            if (!string.IsNullOrEmpty(ceilingMat)) Box("Ceiling", g, center + Vector3.up * (height + 0.2f), new Vector3(size.x, 0.4f, size.y), ceilingMat);
            return g;
        }

        public static Transform Stairs(string name, Transform parent, Vector3 start, float yaw, int steps, float stepHeight, float stepDepth, float width, string mat)
        {
            var g = Group(name, parent);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < steps; i++)
            {
                // Each step is solid down to the start height so stairs never have gaps underneath.
                var s = Box("Step" + i, g, start + rot * new Vector3(0f, stepHeight * (i + 1) * 0.5f, stepDepth * (i + 0.5f)),
                            new Vector3(width, stepHeight * (i + 1), stepDepth), mat);
                s.transform.rotation = rot;
            }
            return g;
        }

        /// <summary>Sloped plank/ramp from a to b (both are top-surface points).</summary>
        public static GameObject Ramp(string name, Transform parent, Vector3 a, Vector3 b, float width, string mat, float thickness = 0.3f)
        {
            Vector3 d = b - a;
            var go = Box(name, parent, (a + b) * 0.5f - Vector3.up * thickness * 0.5f, new Vector3(width, thickness, d.magnitude), mat);
            go.transform.rotation = Quaternion.LookRotation(d.normalized);
            return go;
        }

        /// <summary>A narrow beam/bridge between two points (top surface).</summary>
        public static GameObject Beam(string name, Transform parent, Vector3 a, Vector3 b, float width, string mat) => Ramp(name, parent, a, b, width, mat, 0.25f);

        /// <summary>City facade block with emissive windows and optional neon sign — background dressing.</summary>
        public static Transform Building(string name, Transform parent, Vector3 groundCenter, Vector3 size, string wallMat, string windowMat, string trimMat = null, int seed = 0, string sign = null, string signMat = "neon_cyan")
        {
            var g = Group(name, parent);
            Box("Mass", g, groundCenter + Vector3.up * size.y * 0.5f, size, wallMat);
            if (!string.IsNullOrEmpty(trimMat))
                Box("Cornice", g, groundCenter + Vector3.up * (size.y + 0.15f), new Vector3(size.x + 0.4f, 0.3f, size.z + 0.4f), trimMat);

            var rng = new System.Random(seed);
            int floors = Mathf.Max(1, Mathf.FloorToInt((size.y - 1f) / 3f));
            int cols = Mathf.Max(1, Mathf.FloorToInt(size.x / 2.4f));
            for (int f = 0; f < floors; f++)
            for (int c = 0; c < cols; c++)
            {
                if (rng.NextDouble() < 0.35) continue;
                float x = -size.x * 0.5f + (c + 0.5f) * size.x / cols;
                float y = 2f + f * 3f;
                foreach (float side in new[] { 1f, -1f })
                    Box("Window", g, groundCenter + new Vector3(x, y, side * (size.z * 0.5f + 0.03f)), new Vector3(1.1f, 1.5f, 0.06f), windowMat, false);
            }
            if (!string.IsNullOrEmpty(sign))
            {
                var t = Label("Sign", g, groundCenter + new Vector3(0f, Mathf.Min(size.y - 1f, 4.5f), -size.z * 0.5f - 0.12f), 180f, sign, 0.45f, Color.white);
                Box("SignBack", g, t.transform.position + new Vector3(0, 0, 0.06f), new Vector3(Mathf.Min(size.x - 0.5f, sign.Length * 0.35f + 0.6f), 0.8f, 0.05f), signMat, false);
            }
            return g;
        }

        // =====================================================================
        // Text, glyphs, holograms
        // =====================================================================

        /// <summary>World-space text (signs, carved glyphs, state marks). yaw faces the text toward -forward.</summary>
        public static GameObject Label(string name, Transform parent, Vector3 position, float yaw, string text, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = size * 0.1f;
            tm.fontSize = 64;
            tm.color = color;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            return go;
        }

        /// <summary>A floating hologram (Future projections, clue highlights, The Witness).</summary>
        public static GameObject Hologram(string name, Transform parent, Vector3 position, Vector3 scale, PrimitiveType shape = PrimitiveType.Cube, string holoKey = "hologram", bool bob = true, bool spin = false)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = AZMaterials.Hologram(holoKey);
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (bob) go.AddComponent<FloatBob>().amplitude = 0.08f;
            if (spin) go.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 40f, 0f);
            return go;
        }

        // =====================================================================
        // Lighting
        // =====================================================================

        public static Light Sun(Transform parent, Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.9f;
            RenderSettings.sun = l;
            return l;
        }

        public static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        public static Light SpotLight(string name, Transform parent, Vector3 position, Vector3 euler, Color color, float intensity, float range, float angle, bool shadows = true)
        {
            var l = PointLight(name, parent, position, color, intensity, range, shadows);
            l.type = LightType.Spot;
            l.spotAngle = angle;
            l.transform.rotation = Quaternion.Euler(euler);
            return l;
        }

        /// <summary>Street lamp: post, glowing head and a point light. style: "gas" (Past), "sodium" (Present), "neon" (Future).</summary>
        public static Transform Lamp(string name, Transform parent, Vector3 ground, string style = "sodium", float height = 4.2f)
        {
            var g = Group(name, parent);
            string postMat = style == "gas" ? "brass" : style == "neon" ? "obsidian" : "metal_dark";
            string headMat = style == "gas" ? "lamp_warm" : style == "neon" ? "neon_cyan" : "neon_amber";
            Color c = style == "gas" ? new Color(1f, 0.75f, 0.45f) : style == "neon" ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.6f, 0.3f);
            Cylinder("Post", g, ground + Vector3.up * height * 0.5f, 0.08f, height, postMat);
            var head = Sphere("Head", g, ground + Vector3.up * height, 0.22f, headMat, false);
            head.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var l = PointLight("Light", g, ground + Vector3.up * (height - 0.2f), c, style == "neon" ? 3f : 4f, 11f, false);
            if (style != "gas")
            {
                var f = g.gameObject.AddComponent<NeonFlicker>();
                f.targetLight = l;
                f.targetRenderer = head.GetComponent<Renderer>();
                f.emission = AZMaterials.Get(headMat).GetColor("_EmissionColor");
                f.flickerChance = style == "neon" ? 0.02f : 0.01f;
            }
            return g;
        }

        public static void ReflectionProbe(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var p = go.AddComponent<UnityEngine.ReflectionProbe>();
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            p.size = size;
            p.boxProjection = true;
            p.resolution = 256;
            p.hdr = true;
        }

        // =====================================================================
        // Particles (rain, dust, data motes, sparks, steam, mist)
        // =====================================================================

        public static ParticleSystem Particles(string name, Transform parent, Vector3 center, Vector3 boxSize, string kind)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;
            var vel = ps.velocityOverLifetime;
            var noise = ps.noise;

            switch (kind)
            {
                case "rain":
                    main.startLifetime = 1.2f; main.startSpeed = 0f; main.startSize3D = true;
                    main.startSizeX = 0.015f; main.startSizeY = 0.5f; main.startSizeZ = 0.015f;
                    main.maxParticles = 6000; emission.rateOverTime = 3500f;
                    main.startColor = new Color(0.7f, 0.8f, 0.9f, 0.35f);
                    vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.6f); vel.y = new ParticleSystem.MinMaxCurve(-14f); vel.z = new ParticleSystem.MinMaxCurve(0.3f);
                    r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.03f; r.lengthScale = 1f;
                    r.sharedMaterial = AZMaterials.ParticleMaterial("rain", new Color(0.75f, 0.85f, 1f, 0.4f), false);
                    break;
                case "dust":
                    main.startLifetime = 9f; main.startSpeed = 0.05f; main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
                    main.maxParticles = 800; emission.rateOverTime = 60f;
                    main.startColor = new Color(1f, 0.85f, 0.6f, 0.6f);
                    noise.enabled = true; noise.strength = 0.15f; noise.frequency = 0.3f;
                    r.sharedMaterial = AZMaterials.ParticleMaterial("dust", new Color(1f, 0.9f, 0.7f, 0.6f), true);
                    break;
                case "motes":
                    main.startLifetime = 6f; main.startSpeed = 0.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
                    main.maxParticles = 1200; emission.rateOverTime = 120f;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.95f, 1f, 0.9f), new Color(0.9f, 0.4f, 1f, 0.9f));
                    vel.enabled = true; vel.y = new ParticleSystem.MinMaxCurve(0.25f); vel.x = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);
                    noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.5f;
                    r.sharedMaterial = AZMaterials.ParticleMaterial("motes", new Color(0.5f, 0.95f, 1f, 1f), true);
                    break;
                case "sparks":
                    main.startLifetime = 0.6f; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); main.startSize = 0.04f;
                    main.maxParticles = 300; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
                    main.gravityModifier = 1.2f; main.loop = true; main.duration = 0.4f;
                    main.startColor = new Color(1f, 0.7f, 0.3f, 1f);
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.scale = Vector3.one * 0.2f;
                    r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f;
                    r.sharedMaterial = AZMaterials.ParticleMaterial("sparks", new Color(1f, 0.7f, 0.3f, 1f), true);
                    main.playOnAwake = false;
                    break;
                case "steam":
                    main.startLifetime = 3f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f); main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
                    main.maxParticles = 300; emission.rateOverTime = 40f;
                    main.startColor = new Color(0.85f, 0.9f, 0.95f, 0.18f);
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 15f;
                    go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                    r.sharedMaterial = AZMaterials.ParticleMaterial("steam", new Color(0.9f, 0.95f, 1f, 0.2f), false);
                    break;
                case "mist":
                    main.startLifetime = 12f; main.startSpeed = 0.05f; main.startSize = new ParticleSystem.MinMaxCurve(3f, 6f);
                    main.maxParticles = 120; emission.rateOverTime = 8f;
                    main.startColor = new Color(0.7f, 0.8f, 0.9f, 0.06f);
                    r.sharedMaterial = AZMaterials.ParticleMaterial("mist", new Color(0.8f, 0.85f, 0.95f, 0.08f), false);
                    break;
                case "fire":
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f); main.maxParticles = 300; emission.rateOverTime = 60f;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f, 0.9f), new Color(1f, 0.85f, 0.3f, 0.8f));
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 12f;
                    go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                    var col = ps.colorOverLifetime; col.enabled = true;
                    var grad = new Gradient();
                    grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f), new GradientColorKey(new Color(0.6f, 0.1f, 0.05f), 1f) },
                                 new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                    col.color = grad;
                    r.sharedMaterial = AZMaterials.ParticleMaterial("fire", new Color(1f, 0.6f, 0.2f, 1f), true);
                    main.playOnAwake = false;
                    break;
                case "smoke":
                    main.startLifetime = 4f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f); main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2f);
                    main.maxParticles = 150; emission.rateOverTime = 14f;
                    main.startColor = new Color(0.15f, 0.14f, 0.13f, 0.35f);
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 20f;
                    go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                    r.sharedMaterial = AZMaterials.ParticleMaterial("smoke", new Color(0.2f, 0.2f, 0.2f, 0.4f), false);
                    break;
                case "debris":
                    main.startLifetime = 1.5f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f); main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                    main.maxParticles = 200; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
                    main.gravityModifier = 1f; main.duration = 1f;
                    main.startColor = new Color(0.6f, 0.55f, 0.5f, 1f);
                    r.sharedMaterial = AZMaterials.ParticleMaterial("debris", new Color(0.6f, 0.55f, 0.5f, 1f), false);
                    main.playOnAwake = false;
                    break;
            }
            return ps;
        }

        // =====================================================================
        // Audio
        // =====================================================================

        public static AmbientZone Ambient(string name, Transform parent, Vector3 position, AmbientKind kind, float volume = 0.35f, TimeMask audibleIn = TimeMask.All, float spatial = 1f, float maxDistance = 30f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<AudioSource>();
            var z = go.AddComponent<AmbientZone>();
            z.kind = kind;
            z.volume = volume;
            z.audibleIn = audibleIn;
            z.spatial = spatial;
            z.maxDistance = maxDistance;
            return z;
        }

        // =====================================================================
        // Systems, player, flow
        // =====================================================================

        /// <summary>Creates _Systems (state manager, visuals, director, optional loop) + sun + sky. Returns the director.</summary>
        public static LevelDirector Systems(Transform root, int levelIndex, TimeMask allowed, TimeState start, out TemporalVisuals visuals, float loopDuration = 0f, string breakLoopFlag = "")
        {
            var sys = Group("_Systems", root);

            var tsm = sys.gameObject.AddComponent<TemporalStateManager>();
            tsm.allowedStates = allowed;
            tsm.startState = start;

            if (loopDuration > 0f)
            {
                var lm = sys.gameObject.AddComponent<LoopManager>();
                lm.loopDuration = loopDuration;
                lm.breakLoopFlag = breakLoopFlag;
            }

            var director = sys.gameObject.AddComponent<LevelDirector>();
            director.levelIndex = levelIndex;
            director.levelTitle = LevelCatalog.Titles[levelIndex];
            director.location = LevelCatalog.Locations[levelIndex];

            var sun = Sun(root, new Vector3(35f, -40f, 0f), Color.white, 1.2f);
            visuals = sys.gameObject.AddComponent<TemporalVisuals>();
            visuals.sun = sun;

            RenderSettings.skybox = AZMaterials.Skybox();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            return director;
        }

        public static void AddObjective(LevelDirector d, string text, string flag, string hint = "")
        {
            d.objectives.Add(new Objective { text = text, completeFlag = flag, hint = hint });
        }

        public static PlayerController Player(Transform root, Vector3 position, float yaw)
        {
            var go = new GameObject("Arin (Player)");
            go.transform.SetParent(root, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.32f;
            cc.center = new Vector3(0f, 0.875f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;

            var pivot = Group("CameraPivot", go.transform);
            pivot.localPosition = new Vector3(0f, 1.62f, 0f);
            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 72f;
            cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraFXSetup>();

            var pc = go.AddComponent<PlayerController>();
            pc.playerCamera = cam;
            pc.cameraPivot = pivot;
            go.AddComponent<PlayerInteractor>();
            return pc;
        }

        public static Checkpoint Checkpoint(string name, Transform parent, Vector3 position, Vector3 size, float yaw = 0f, bool isLevelStart = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position + Vector3.up * size.y * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            var cp = go.AddComponent<Checkpoint>();
            cp.isLevelStart = isLevelStart;
            var spawn = Marker("Spawn", go.transform, position + Vector3.up * 0.05f, yaw);
            cp.spawnPoint = spawn;
            return cp;
        }

        public static LevelExit Exit(string name, Transform parent, Vector3 position, Vector3 size, string lockedMessage, params string[] requiredFlags)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position + Vector3.up * size.y * 0.5f;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            var ex = go.AddComponent<LevelExit>();
            ex.requiredFlags = new List<string>(requiredFlags);
            ex.lockedMessage = lockedMessage;
            // Readable exit: light shaft + glyph.
            Hologram("ExitBeacon", go.transform, position + Vector3.up * (size.y + 0.6f), new Vector3(0.3f, 0.3f, 0.3f), PrimitiveType.Sphere, "hologram", true, true);
            PointLight("ExitLight", go.transform, position + Vector3.up * 2f, new Color(0.5f, 0.95f, 1f), 2.5f, 8f);
            return ex;
        }

        public static RespawnVolume KillPlane(Transform parent, Vector3 center, Vector3 size, string message = "")
        {
            var go = new GameObject("KillVolume");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            var rv = go.AddComponent<RespawnVolume>();
            rv.message = message;
            return rv;
        }

        public static BoxCollider TriggerBox(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            return bc;
        }

        public static TriggerFlag Zone(string name, Transform parent, Vector3 center, Vector3 size, string flag, string toast = "", TimeMask activeIn = TimeMask.All)
        {
            var t = TriggerBox(name, parent, center, size).gameObject.AddComponent<TriggerFlag>();
            t.setFlag = flag;
            t.toast = toast;
            t.activeIn = activeIn;
            return t;
        }

        // =====================================================================
        // Interactables
        // =====================================================================

        /// <summary>A visible object you can use to set a flag (lever, valve, letter, recording…).</summary>
        public static FlagInteractable Use(string name, Transform parent, Vector3 center, Vector3 size, string mat, string prompt, string flag, TimeMask usableIn = TimeMask.All, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = Prim(shape, name, parent, center, size, mat, true, false);
            var fi = go.AddComponent<FlagInteractable>();
            fi.prompt = prompt;
            fi.setFlag = flag;
            fi.usableIn = usableIn;
            return fi;
        }

        /// <summary>Readable document / clue. Sets flag and adds a journal entry.</summary>
        public static FlagInteractable Clue(string name, Transform parent, Vector3 center, Vector3 size, string mat, string prompt, string flag, string clueId, string title, string text, bool asDocument = true, TimeMask usableIn = TimeMask.All)
        {
            var fi = Use(name, parent, center, size, mat, prompt, flag, usableIn);
            fi.clueId = clueId;
            fi.clueTitle = title;
            fi.clueText = text;
            fi.showAsDocument = asDocument;
            fi.cue = Cue.Interact;
            fi.usedPrompt = "Read again";
            // Subtle highlight so clues read without relying on colour: a slow-spinning ring.
            Hologram(name + "_Marker", parent, center + Vector3.up * (size.y * 0.5f + 0.25f), new Vector3(0.18f, 0.02f, 0.18f), PrimitiveType.Cylinder, "hologram_amber", true, true);
            return fi;
        }

        public static Grabbable Grab(string name, Transform parent, Vector3 center, Vector3 size, string mat, string key, string displayName, PrimitiveType shape = PrimitiveType.Cube, bool carryAcrossStates = false)
        {
            var go = Prim(shape, name, parent, center, size, mat, true, false);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var g = go.AddComponent<Grabbable>();
            g.key = key;
            g.displayName = displayName;
            g.prompt = "Take " + displayName;
            g.carryAcrossStates = carryAcrossStates;
            return g;
        }

        public static SnapSocket Socket(string name, Transform parent, Vector3 center, string acceptsKey, string filledFlag, string prompt = "Place", float radius = 0.35f)
        {
            // Unscaled root (so placed objects keep their size) with a visual base and an aim collider.
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            Prim(PrimitiveType.Cylinder, "Base", go.transform, center, new Vector3(radius * 2f, 0.04f, radius * 2f), "metal_dark", true, false);
            var aim = go.AddComponent<BoxCollider>();
            aim.isTrigger = true;
            aim.size = new Vector3(radius * 2.4f, 0.8f, radius * 2.4f);
            aim.center = new Vector3(0f, 0.35f, 0f);
            var s = go.AddComponent<SnapSocket>();
            s.acceptsKey = acceptsKey;
            s.filledFlag = filledFlag;
            s.prompt = prompt;
            var anchor = Marker("Anchor", go.transform, center + Vector3.up * 0.25f);
            s.anchor = anchor;
            Hologram(name + "_Ring", parent, center + Vector3.up * 0.05f, new Vector3(radius * 2.2f, 0.01f, radius * 2.2f), PrimitiveType.Cylinder, "hologram", false, true);
            return s;
        }

        public static Pushable Pushable(string name, Transform parent, Vector3 center, Vector3 size, string mat, float mass = 60f)
        {
            var go = Prim(PrimitiveType.Cube, name, parent, center, size, mat, true, false);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            return go.AddComponent<Pushable>();
        }

        public static FlagReactor Reactor(string name, Transform parent, params string[] whenAll)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<FlagReactor>();
            r.whenAll = new List<string>(whenAll);
            return r;
        }

        /// <summary>A door that slides up/aside when flags are set. Returns the reactor.</summary>
        public static FlagReactor Door(string name, Transform parent, Vector3 groundCenter, Vector3 size, string mat, Vector3 openOffset, float yaw, params string[] whenAll)
        {
            var g = Group(name, parent);
            var panel = Box("Panel", g, groundCenter + Vector3.up * size.y * 0.5f, size, mat, true, yaw);
            var trim = Box("Trim", g, groundCenter + Vector3.up * (size.y + 0.05f), new Vector3(size.x + 0.2f, 0.1f, size.z + 0.2f), "neon_cyan", false, yaw);
            var r = g.gameObject.AddComponent<FlagReactor>();
            r.whenAll = new List<string>(whenAll);
            r.moveTarget = panel.transform;
            r.moveBy = openOffset;
            r.duration = 1.6f;
            r.announce = true;
            return r;
        }

        public static WaterBody Water(string name, Transform parent, Vector3 center, Vector2 size, float highY, float lowY, string drainedFlag, params string[] lowWhenAll)
        {
            var g = Group(name, parent);
            g.position = new Vector3(center.x, 0f, center.z);
            var surf = Box("Surface", g, new Vector3(center.x, highY, center.z), new Vector3(size.x, 0.05f, size.y), "water", false);
            surf.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // Invisible wall that stops wading into deep water while the level is high.
            var blocker = Box("DeepWaterBlocker", g, new Vector3(center.x, highY - 1f, center.z), new Vector3(size.x, 2f + 2f, size.y), "water", true);
            Object.DestroyImmediate(blocker.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(blocker.GetComponent<MeshFilter>());
            var w = g.gameObject.AddComponent<WaterBody>();
            w.surface = surf.transform;
            w.highY = highY;
            w.lowY = lowY;
            w.drainedFlag = drainedFlag;
            w.lowWhenAll = new List<string>(lowWhenAll);
            w.blockerWhenHigh = blocker.GetComponent<Collider>();
            return w;
        }

        public static RotatableElement Rotatable(string name, Transform parent, Vector3 center, Vector3 size, string mat, int steps, int start, int correct, string glyph = "")
        {
            var g = Group(name, parent);
            g.position = center;
            var vis = Box("Visual", g, center, size, mat);
            GameObjectUtility.SetStaticEditorFlags(vis, 0);
            var r = g.gameObject.AddComponent<RotatableElement>();
            var bc = g.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(Mathf.Max(size.x, size.z), size.y + 0.2f, Mathf.Max(size.x, size.z));
            bc.isTrigger = true;
            r.pivot = vis.transform;
            r.steps = steps;
            r.startIndex = start;
            r.correctIndex = correct;
            r.glyph = glyph;
            // Directional arrow on top so orientation is readable by shape, not colour.
            var arrow = Box("Arrow", vis.transform, center + new Vector3(0f, size.y * 0.5f + 0.03f, size.z * 0.3f), new Vector3(0.12f, 0.04f, size.z * 0.45f), "neon_amber", false);
            arrow.transform.localScale = new Vector3(0.12f / Mathf.Max(0.01f, size.x), 0.04f / Mathf.Max(0.01f, size.y), 0.45f);
            if (!string.IsNullOrEmpty(glyph)) Label("Glyph", g, center + Vector3.up * (size.y * 0.5f + 0.45f), 0f, glyph, 0.6f, Color.white);
            return r;
        }

        // =====================================================================
        // NPCs
        // =====================================================================

        static readonly string[] coats = { "coat_navy", "coat_ochre", "coat_olive", "coat_burgundy", "coat_grey", "coat_white" };

        /// <summary>
        /// Builds a stylised resident (body, head, visor band, arms). named=true adds an NPCPersona
        /// for conversation. Returns the motor; use Route() to give it a routine.
        /// </summary>
        public static NPCMotor Npc(string displayName, Transform parent, Vector3 ground, float yaw, string coatMat = null, bool named = true, int seed = 0, string propMat = null)
        {
            var rng = new System.Random(seed + displayName.GetHashCode());
            if (string.IsNullOrEmpty(coatMat)) coatMat = coats[rng.Next(coats.Length)];
            string skin = rng.NextDouble() < 0.5 ? "skin" : "skin_dark";
            float h = 0.92f + (float)rng.NextDouble() * 0.16f;

            var root = Group((named ? "NPC " : "Resident ") + displayName, parent);
            root.position = ground;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            var body = Group("Body", root);
            body.localPosition = Vector3.zero;

            var torso = Prim(PrimitiveType.Capsule, "Torso", body, ground + Vector3.up * 1.0f * h, new Vector3(0.5f, 0.55f * h, 0.34f), coatMat, false, false);
            torso.transform.localRotation = Quaternion.identity;
            var legs = Prim(PrimitiveType.Cube, "Legs", body, ground + Vector3.up * 0.4f * h, new Vector3(0.36f, 0.8f * h, 0.24f), "coat_grey", false, false);
            var headPivot = Group("Head", body);
            headPivot.position = ground + Vector3.up * 1.62f * h;
            Prim(PrimitiveType.Sphere, "Skull", headPivot, headPivot.position, Vector3.one * 0.28f, skin, false, false);
            var visor = Prim(PrimitiveType.Cube, "Visor", headPivot, headPivot.position + root.forward * 0.12f + Vector3.up * 0.02f, new Vector3(0.24f, 0.05f, 0.06f), "visor", false, false);
            visor.transform.rotation = root.rotation;

            Transform Arm(string n, float side)
            {
                var pivot = Group(n, body);
                pivot.position = ground + Vector3.up * 1.38f * h + root.right * side * 0.3f;
                pivot.rotation = root.rotation;
                var a = Prim(PrimitiveType.Capsule, "Limb", pivot, pivot.position + Vector3.down * 0.32f, new Vector3(0.12f, 0.32f, 0.12f), coatMat, false, false);
                a.transform.rotation = root.rotation;
                return pivot;
            }
            var la = Arm("LeftArm", -1f);
            var ra = Arm("RightArm", 1f);

            GameObject prop = null;
            if (!string.IsNullOrEmpty(propMat))
            {
                prop = Prim(PrimitiveType.Cube, "CarriedProp", body, ground + Vector3.up * 1.1f + root.forward * 0.35f, new Vector3(0.45f, 0.3f, 0.3f), propMat, false, false);
                prop.transform.rotation = root.rotation;
            }

            // Interaction collider on the root.
            var cap = root.gameObject.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.9f * h, 0f);
            cap.height = 1.8f * h;
            cap.radius = 0.32f;

            var m = root.gameObject.AddComponent<NPCMotor>();
            m.displayName = displayName;
            m.body = body;
            m.head = headPivot;
            m.leftArm = la;
            m.rightArm = ra;
            m.carriedProp = prop;
            m.walkSpeed = 1.2f + (float)rng.NextDouble() * 0.4f;
            if (named)
            {
                var p = root.gameObject.AddComponent<NPCPersona>();
                p.prompt = "Talk to " + displayName;
                // Name tag above head (small, readable).
                Label("NameTag", root, ground + Vector3.up * (2.05f * h), 0f, displayName, 0.25f, new Color(0.8f, 0.95f, 1f, 0.9f));
            }
            return m;
        }

        /// <summary>Adds routine steps (creates waypoint markers under a sibling "Route" group).</summary>
        public static void Route(NPCMotor m, bool changed, params (Vector3 pos, float wait, NPCActivity activity, string bark)[] steps)
        {
            var g = Group(m.name + (changed ? " ChangedRoute" : " Route"), m.transform.parent);
            var list = changed ? m.changedRoutine : m.routine;
            for (int i = 0; i < steps.Length; i++)
            {
                var mk = Marker("P" + i, g, steps[i].pos, m.transform.eulerAngles.y);
                list.Add(new RoutineStep { point = mk, wait = steps[i].wait, activity = steps[i].activity, bark = steps[i].bark });
            }
        }

        /// <summary>Background residents: wander between points, some converse in pairs, some carry goods.</summary>
        public static List<NPCMotor> Crowd(string name, Transform parent, Vector3 center, Vector2 area, int count, int seed, string[] barks = null, string propMat = "wood")
        {
            var g = Group(name, parent);
            var rng = new System.Random(seed);
            var list = new List<NPCMotor>();
            Vector3 RandPoint() => center + new Vector3(((float)rng.NextDouble() - 0.5f) * area.x, 0f, ((float)rng.NextDouble() - 0.5f) * area.y);

            for (int i = 0; i < count; i++)
            {
                bool talker = i % 4 == 1 && list.Count > 0;
                bool carrier = i % 3 == 0;
                Vector3 p = talker ? list[list.Count - 1].transform.position + new Vector3(1.2f, 0f, 0.4f) : RandPoint();
                var m = Npc("Resident " + (i + 1), g, p, (float)rng.NextDouble() * 360f, null, false, seed + i, carrier ? propMat : null);
                if (barks != null && barks.Length > 0) m.reactionBarks.AddRange(barks);
                if (talker)
                {
                    var partner = list[list.Count - 1];
                    m.conversePartner = partner.transform;
                    partner.conversePartner = m.transform;
                    Route(m, false, (p, 999f, NPCActivity.Talk, ""));
                    partner.routine.Clear();
                    Route(partner, false, (partner.transform.position, 999f, NPCActivity.Talk, ""));
                }
                else
                {
                    var act = carrier ? NPCActivity.Carry : (i % 2 == 0 ? NPCActivity.Work : NPCActivity.Look);
                    Route(m, false, (RandPoint(), 2f + (float)rng.NextDouble() * 4f, act, ""), (RandPoint(), 2f + (float)rng.NextDouble() * 4f, NPCActivity.Idle, ""), (p, 3f, act, ""));
                }
                list.Add(m);
            }
            return list;
        }

        public static SubtitleLine L(string speaker, string text, float duration = 3.5f) => new SubtitleLine(speaker, text, duration);

        // =====================================================================
        // Design v2 helpers
        // =====================================================================

        /// <summary>A Keeper of the Hour: hooded figure with a sight-lantern (spot light + faint visible cone).</summary>
        public static KeeperPatrol Keeper(string displayName, Transform parent, Vector3 ground, float yaw, KeeperPatrol.Mode mode, TimeMask activeIn, params Vector3[] patrol)
        {
            var npc = Npc(displayName, parent, ground, yaw, "coat_grey", false, displayName.Length);
            var body = npc.transform;
            Object.DestroyImmediate(npc); // Keepers are driven by KeeperPatrol, not a routine
            var root = Group("Keeper " + displayName, parent);
            root.position = ground;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.SetParent(root, true);
            body.name = "Body";
            Prim(PrimitiveType.Cylinder, "Hood", body, ground + Vector3.up * 1.72f, new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            var eye = Marker("Eye", root, ground + Vector3.up * 1.6f + root.forward * 0.2f, yaw);
            var lanternGo = new GameObject("Lantern");
            lanternGo.transform.SetParent(root, false);
            lanternGo.transform.position = ground + Vector3.up * 1.2f + root.forward * 0.35f + root.right * 0.25f;
            lanternGo.transform.rotation = Quaternion.Euler(12f, yaw, 0f);
            var l = lanternGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(1f, 0.85f, 0.55f);
            l.intensity = 6f;
            l.range = mode == KeeperPatrol.Mode.Watcher ? 9f : 13f;
            l.spotAngle = 70f;
            l.shadows = LightShadows.None;
            Sphere("LanternGlass", lanternGo.transform, lanternGo.transform.position, 0.1f, "lamp_warm", false);
            var cone = Hologram("SightCone", lanternGo.transform, lanternGo.transform.position + lanternGo.transform.forward * 4f, new Vector3(5f, 4f, 0.01f), PrimitiveType.Quad, "hologram_amber", false, false);
            cone.transform.rotation = Quaternion.LookRotation(Vector3.up, lanternGo.transform.forward);
            cone.transform.localScale = new Vector3(5f, 8f, 1f);

            var k = root.gameObject.AddComponent<KeeperPatrol>();
            k.displayName = displayName;
            k.mode = mode;
            k.activeIn = activeIn;
            k.eye = eye;
            k.lantern = l;
            var route = Group(displayName + " Patrol", parent);
            foreach (var p in patrol) k.patrol.Add(Marker("P", route, p));
            return k;
        }

        /// <summary>A fire node for SpreadingFire: flames + smoke + glow at a point, put out by a flag.</summary>
        public static FireNode FireNodeAt(Transform parent, Vector3 position, string extinguishFlag)
        {
            var g = Group("FireNode", parent);
            g.position = position;
            var flames = Particles("Flames", g, position, new Vector3(1.2f, 0.2f, 1.2f), "fire");
            Particles("Smoke", g, position + Vector3.up * 1.5f, new Vector3(1.4f, 0.2f, 1.4f), "smoke");
            var glow = PointLight("FireGlow", g, position + Vector3.up * 0.8f, new Color(1f, 0.5f, 0.2f), 0f, 7f);
            glow.enabled = false;
            return new FireNode { point = g, extinguishFlag = extinguishFlag, flames = flames, glow = glow };
        }

        /// <summary>Branching conversation attached to a talkable object or NPC (or triggered by a flag).</summary>
        public static DialogueChoice Dialogue(GameObject host, string speaker, string question, string triggerFlag = "")
        {
            var d = host.AddComponent<DialogueChoice>();
            d.speaker = speaker;
            d.question = question;
            d.triggerFlag = triggerFlag;
            d.prompt = "Talk to " + speaker;
            return d;
        }

        public static DialogueOption Option(string text, string setsFlag, params SubtitleLine[] response)
        {
            var o = new DialogueOption { text = text, setsFlag = setsFlag };
            o.response.AddRange(response);
            return o;
        }

        /// <summary>Marks a moving surface so the player rides it (gears, lifts, rafts).</summary>
        public static MovingPlatform Ride(GameObject surface) => surface.AddComponent<MovingPlatform>();

        /// <summary>Optional collectible hologram shard (3 per level). id like "L03.2".</summary>
        public static MemoryFragment Fragment(Transform parent, Vector3 position, string id, string title, string text)
        {
            var go = Hologram("Fragment " + id, parent, position, new Vector3(0.35f, 0.5f, 0.35f), PrimitiveType.Capsule, "hologram_amber", true, true);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.2f;
            var f = go.AddComponent<MemoryFragment>();
            f.fragmentId = id;
            f.title = title;
            f.text = text;
            PointLight("FragmentGlow", go.transform, position, new Color(1f, 0.7f, 0.35f), 1.2f, 4f);
            return f;
        }
    }
}
