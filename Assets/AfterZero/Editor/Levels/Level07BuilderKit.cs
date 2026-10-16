using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>Private dressing / gameplay helpers shared by the Level 07 and Level 08 builders.</summary>
    internal static class Level07BuilderKit
    {
        /// <summary>Yaw that makes a Label at <paramref name="at"/> readable from <paramref name="viewer"/>.</summary>
        public static float FaceYaw(Vector3 at, Vector3 viewer)
        {
            Vector3 d = at - viewer; d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return 0f;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        public static GameObject Tilted(string name, Transform parent, Vector3 center, Vector3 size, string mat, Vector3 euler, bool collider = true)
        {
            var go = Box(name, parent, center, size, mat, collider);
            go.transform.rotation = Quaternion.Euler(euler);
            return go;
        }

        public static void NoShadow(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Distant skyline tower: mass + emissive floor bands + optional neon edges and sign. No colliders.</summary>
        public static Transform SkyTower(string name, Transform parent, Vector3 ground, Vector3 size, string wall, string band, int seed, bool neonEdges = false, string sign = null, string signMat = "neon_magenta")
        {
            var t = Group(name, parent);
            t.position = ground;
            float yaw = FaceYaw(Vector3.zero, ground) + 180f; // forward points away from the square: front face (-forward) faces the square
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject B(string n, Vector3 local, Vector3 s, string m)
            {
                var go = Box(n, t, t.TransformPoint(local), s, m, false);
                go.transform.rotation = t.rotation;
                return go;
            }
            B("Mass", new Vector3(0f, size.y * 0.5f, 0f), size, wall);
            var rng = new System.Random(seed);
            int bands = Mathf.Clamp(Mathf.FloorToInt(size.y / 7f), 1, 6);
            for (int i = 0; i < bands; i++)
            {
                if (rng.NextDouble() < 0.25) continue;
                float y = 3f + i * (size.y - 4f) / bands;
                float w = size.x * (0.5f + (float)rng.NextDouble() * 0.4f);
                NoShadow(B("Band", new Vector3(((float)rng.NextDouble() - 0.5f) * (size.x - w), y, -size.z * 0.5f - 0.06f), new Vector3(w, 0.45f + (float)rng.NextDouble() * 0.6f, 0.1f), band));
            }
            if (neonEdges)
            {
                NoShadow(B("EdgeL", new Vector3(-size.x * 0.5f - 0.05f, size.y * 0.5f, -size.z * 0.5f - 0.05f), new Vector3(0.18f, size.y, 0.18f), band));
                NoShadow(B("EdgeR", new Vector3(size.x * 0.5f + 0.05f, size.y * 0.5f, -size.z * 0.5f - 0.05f), new Vector3(0.18f, size.y, 0.18f), band));
                B("Crown", new Vector3(0f, size.y + 1.2f, 0f), new Vector3(size.x * 0.4f, 2.4f, size.z * 0.4f), wall);
            }
            if (!string.IsNullOrEmpty(sign))
            {
                var p = t.TransformPoint(new Vector3(0f, size.y * 0.62f, -size.z * 0.5f - 0.3f));
                NoShadow(B("SignBack", new Vector3(0f, size.y * 0.62f, -size.z * 0.5f - 0.2f), new Vector3(sign.Length * 0.9f + 1.2f, 2.2f, 0.1f), signMat));
                Label("Sign", t, p, yaw, sign, 4.5f, Color.white);
            }
            return t;
        }

        /// <summary>
        /// A perimeter building around a square: shared mass + cornice, windows that exist in the given
        /// states, and neon trims / cracks in the Future.
        /// </summary>
        public static void Facade(string name, Transform shared, Transform windowsGroup, Transform futureGroup, Vector3 center, Vector3 size, Vector3 normal, string wallMat, string windowMat, int seed, int skipBelow = 1)
        {
            var g = Group(name, shared);
            Box("Mass", g, center + Vector3.up * size.y * 0.5f, size, wallMat);
            Box("Cornice", g, center + Vector3.up * (size.y + 0.2f), new Vector3(size.x + 0.5f, 0.4f, size.z + 0.5f), "stone_warm");
            Box("Plinth", g, center + Vector3.up * 0.4f, new Vector3(size.x + 0.2f, 0.8f, size.z + 0.2f), "stone_wet");
            Vector3 n = normal.normalized;
            Vector3 t = Vector3.Cross(Vector3.up, n);
            Vector3 an = new Vector3(Mathf.Abs(n.x), 0f, Mathf.Abs(n.z));
            Vector3 at = new Vector3(Mathf.Abs(t.x), 0f, Mathf.Abs(t.z));
            float width = Vector3.Dot(size, at);
            float depth = Vector3.Dot(size, an);
            Vector3 face = center + n * (depth * 0.5f + 0.04f);
            var rng = new System.Random(seed);
            int cols = Mathf.Max(1, Mathf.FloorToInt(width / 3.2f));
            int floors = Mathf.Max(1, Mathf.FloorToInt((size.y - 2f) / 3.4f));
            var wg = Group(name + " Windows", windowsGroup);
            for (int f = skipBelow; f < floors; f++)
            for (int c = 0; c < cols; c++)
            {
                if (rng.NextDouble() < 0.3) continue;
                float off = -width * 0.5f + (c + 0.5f) * width / cols;
                Vector3 p = face + t * off + Vector3.up * (2.2f + f * 3.4f);
                NoShadow(Box("Window", wg, p, at * 1.1f + Vector3.up * 1.7f + an * 0.08f, windowMat, false));
            }
            if (futureGroup != null)
            {
                var fg = Group(name + " Neon", futureGroup);
                string neon = seed % 2 == 0 ? "neon_cyan" : "neon_magenta";
                NoShadow(Box("TrimTop", fg, face + Vector3.up * (size.y - 0.5f), at * width + Vector3.up * 0.15f + an * 0.1f, neon, false));
                NoShadow(Box("TrimMid", fg, face + Vector3.up * (size.y * 0.45f), at * (width * 0.6f) + Vector3.up * 0.1f + an * 0.1f, "neon_violet", false));
                // A glowing crack running down the facade.
                var crack = Box("Crack", fg, face + t * (width * 0.18f) + Vector3.up * (size.y * 0.55f), at * 0.15f + Vector3.up * (size.y * 0.6f) + an * 0.12f, "mineral_trace", false);
                crack.transform.rotation = Quaternion.AngleAxis(12f, n) * crack.transform.rotation;
                NoShadow(crack);
            }
        }

        public static NPCPersona Persona(NPCMotor m, string task, string[] approach, SubtitleLine[] talk,
            string condFlag, string condHolding, SubtitleLine[] cond, string changedFlag, SubtitleLine[] changed, string talkedFlag = "")
        {
            var p = m.GetComponent<NPCPersona>();
            p.task = task;
            p.approachBarks.AddRange(approach);
            p.talkLines.AddRange(talk);
            p.talkedFlag = talkedFlag;
            var c = new ConditionalLines { whenFlag = condFlag ?? "", whenHolding = condHolding ?? "" };
            c.lines.AddRange(cond);
            p.conditional.Add(c);
            m.changedFlag = changedFlag;
            p.changedLines.AddRange(changed);
            return p;
        }

        public static void AddCond(NPCPersona p, string flag, string holding, params SubtitleLine[] lines)
        {
            var c = new ConditionalLines { whenFlag = flag ?? "", whenHolding = holding ?? "" };
            c.lines.AddRange(lines);
            p.conditional.Add(c);
        }

        /// <summary>String of festival bunting between two points (Past dressing).</summary>
        public static void Bunting(Transform parent, Vector3 a, Vector3 b, int flags, float sag = 0.8f)
        {
            var g = Group("Bunting", parent);
            string[] mats = { "fabric_red", "fabric_gold", "fabric_teal" };
            for (int i = 0; i < flags; i++)
            {
                float k = (i + 0.5f) / flags;
                Vector3 p = Vector3.Lerp(a, b, k) + Vector3.down * sag * 4f * k * (1f - k);
                var f = Box("Pennant", g, p, new Vector3(0.35f, 0.45f, 0.03f), mats[i % 3], false);
                f.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, (b - a).normalized)) * Quaternion.Euler(0f, 0f, 45f);
                NoShadow(f);
            }
        }

        /// <summary>Falling masonry: warning light + debris trickle + shaking chunk, then an impact that hurts.</summary>
        public static TelegraphedHazard Masonry(string name, Transform parent, Vector3 ground, Vector3 chunkPos, float offset, TimeMask activeIn, string disabledBy, string chunkMat = "stone_wet")
        {
            var g = Group(name, parent);
            g.position = ground;
            var chunk = Box("LooseMasonry", g, chunkPos, new Vector3(1.6f, 0.7f, 1.2f), chunkMat, false);
            chunk.transform.rotation = Quaternion.Euler(0f, 0f, 6f);
            // Shape telegraph on the ground: a cracked ring, always visible.
            var ring = Cylinder("CrackRing", g, ground + Vector3.up * 0.015f, 1.7f, 0.02f, "ruin_dark", false);
            NoShadow(ring);
            var warn = PointLight("WarnLight", g, ground + Vector3.up * 2.6f, new Color(1f, 0.25f, 0.12f), 7f, 7f);
            var trickle = Particles("DebrisTrickle", g, chunkPos + Vector3.down * 0.5f, new Vector3(1.4f, 0.2f, 1f), "debris");
            var impact = Particles("Impact", g, ground + Vector3.up * 0.3f, new Vector3(2.5f, 0.2f, 2.5f), "debris");
            var em = impact.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
            var main = impact.main; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); main.loop = false;
            var rubble = Group("ImpactRubble", g);
            Tilted("Slab", rubble, ground + Vector3.up * 0.3f, new Vector3(1.4f, 0.6f, 1.0f), chunkMat, new Vector3(8f, 30f, 12f), false);
            Tilted("Shard", rubble, ground + new Vector3(0.8f, 0.2f, -0.5f), new Vector3(0.6f, 0.4f, 0.5f), chunkMat, new Vector3(-10f, 70f, 20f), false);
            rubble.gameObject.SetActive(false);

            var zoneGo = new GameObject("DamageZone");
            zoneGo.transform.SetParent(g, false);
            zoneGo.transform.position = ground + Vector3.up * 1.2f;
            var bc = zoneGo.AddComponent<BoxCollider>();
            bc.size = new Vector3(3f, 2.4f, 3f);
            bc.isTrigger = true;
            var h = zoneGo.AddComponent<TelegraphedHazard>();
            h.idleTime = 4.5f; h.warnTime = 1.8f; h.activeTime = 1.1f; h.startOffset = offset;
            h.activeIn = activeIn; h.disabledByFlag = disabledBy;
            h.warnLight = warn; h.warnParticles = trickle; h.activeParticles = impact;
            h.shakeTarget = chunk.transform; h.activeVisual = rubble.gameObject; h.damageZone = bc;
            h.deathMessage = "Masonry. Watch for the red light and falling grit before it drops.";
            return h;
        }
    }
}
