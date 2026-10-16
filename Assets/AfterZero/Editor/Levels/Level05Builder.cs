using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 05 — The Drowned District (Old Veyra Flood Basin).
    /// A sunken square of terraced houses. PAST: dry festival lanes and a broken communal pump.
    /// PRESENT: flooded to the windows; mending the pump in the PAST drains the basin, then three
    /// sluices redirect the lane water so the family rooms can be reached. FUTURE: crystal ruins
    /// with holographic echoes of the families. Rescue Maren, then choose records or keepsake while
    /// the flood returns, and step into the impossible nursery.
    /// Layout (y = 0 basin floor): Weir Terrace (start, y = 3) south at z -36..-24, stairs down to the
    /// square x -14..14 / z -24..16; lanes West (z -2..2), East (z -2..2), North (x -2..2) to the
    /// Vell, Oskel and Corran houses; Sluice House SW, Ward Office NW, Laundry alley SE.
    /// </summary>
    public static partial class Level05Builder
    {
        // ---- flags ----
        const string F_Pump = "L05.pumpRepaired";
        const string F_SluiceW = "L05.sluiceWest";
        const string F_SluiceN = "L05.sluiceNorth";
        const string F_SluiceE = "L05.sluiceEast";
        const string F_Redirected = "L05.waterRedirected";
        const string F_RoomVell = "L05.roomVell";
        const string F_RoomCorran = "L05.roomCorran";
        const string F_RoomOskel = "L05.roomOskel";
        const string F_Rooms = "L05.roomsCompared";
        const string F_Debris = "L05.debrisCleared";
        const string F_Child = "L05.childRescued";
        const string F_Crisis = "L05.crisis";
        const string F_Choice = "choice.L05.made";
        const string F_Records = "choice.L05.records";
        const string F_Keepsake = "choice.L05.keepsake";
        const string F_Entered = "L05.nurseryEntered";
        const string K_Handle = "L05_handle";

        static readonly Color Gas = new Color(1f, 0.74f, 0.42f);
        static readonly Color Sodium = new Color(1f, 0.58f, 0.26f);
        static readonly Color ColdTeal = new Color(0.45f, 0.75f, 0.85f);
        static readonly Color Cyan = new Color(0.3f, 0.9f, 1f);
        static readonly Color Magenta = new Color(1f, 0.3f, 0.85f);
        static readonly Color Violet = new Color(0.6f, 0.4f, 1f);
        static readonly Color TextWarm = new Color(1f, 0.9f, 0.7f);
        static readonly Color TextCold = new Color(0.8f, 0.92f, 1f);

        public static void Build(Transform root)
        {
            var d = Systems(root, 4, TimeMask.All, TimeState.Present, out var visuals);
            TuneVisuals(visuals);

            d.storySetup = "Old Veyra's flood basin drowned long ago in the PRESENT. Its terraced homes still hold the families struck from the public record by Founder Aurel Halden's order - the Vells, the Corrans, the Oskels - and every one of them was expecting tomorrow.";
            d.interludeTitle = "The Lift Down";
            d.interludeLines.Add(L("Ignatz Marrow (echo)", "Mind the gap, mind the gap... The lift goes down to the basin. Nobody rides it back up.", 4.5f));
            d.interludeLines.Add(L("Ignatz Marrow (echo)", "Forty trains in nine days. They told me it was a drill. Children, mostly. Children and their mothers.", 5f));
            d.interludeLines.Add(L("Arin", "Who told you to run them?", 3f));
            d.interludeLines.Add(L("Ignatz Marrow (echo)", "My sister knew. Ilse always knew more than she said. She always carried a carved zero - turned it in her fingers like a coin.", 6f));
            d.interludeLines.Add(L("Ignatz Marrow (echo)", "If you see her, tell her I kept the trains running. Tell her I never asked why.", 4.5f));
            d.interludeLines.Add(L("Arin", "The lift shudders to a stop. Below: rain, black water, and rooftops.", 4f));
            d.introLines.Add(L("Arin", "The whole district is under water. Doors, lanes... it's up to the windows."));
            d.introLines.Add(L("Hesk Lowry", "Mind the rail, stranger. The basin took everything that wasn't nailed to the sky."));
            d.introLines.Add(L("Arin", "In the PAST these lanes were dry. Something kept the water out. Something I can fix."));

            AddObjective(d, "Repair the communal hand-pump in the PAST", F_Pump, "Tobin the pump mender lost the handle. In the PAST, look along the north lane by the Corran house.");
            AddObjective(d, "Open the three lane sluices in the PRESENT", F_Redirected, "With the basin drained, wade to the Sluice House (south-west corner) and turn all three wheels.");
            AddObjective(d, "Compare the three family rooms", F_Rooms, "Read the calendar in the Vell (west), Corran (north) and Oskel (east) houses in the PRESENT. Shift inside to see each room in the other states.");
            AddObjective(d, "Rescue Maren from the flooded laundry", F_Child, "Three timber shores lie around the PAST square. Set them under the sagging yard beam in the PAST; each clears one rubble bank in the PRESENT. Hum (H) so Maren trusts you, then lead her to the Weir Stairs.");
            AddObjective(d, "Face Mother Ondine at the Keeper boat", "L05.ondineDone", "Her boat waits near the Weir Stairs. If you carry the evacuation list, show it to her.");
            AddObjective(d, "The flood is returning: save the register or the music box", F_Choice, "Both lie in the Ward Office on the north-west side of the square. You can only carry one.");
            AddObjective(d, "Enter the impossible nursery", F_Entered, "The door with no house stands in the north-east corner of the square.");

            d.payoffFlag = F_Rooms;
            d.payoffClueTitle = "Every Calendar Marks Tomorrow";
            d.payoffClueText = "Three families the city no longer remembers. Three calendars in three different hands - and every one marks tomorrow as the day of the bell. They were not surprised by the catastrophe. They were preparing for it.";

            Player(root, new Vector3(0f, 3.05f, -31f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0f, 3f, -31f), new Vector3(6f, 3f, 4f), 0f, true);
            Checkpoint("CP_Square", root, new Vector3(0f, 0f, -16.5f), new Vector3(6f, 3f, 3f), 0f);
            Checkpoint("CP_Climax", root, new Vector3(6f, 0f, -16.5f), new Vector3(3f, 3f, 3f), 0f).climax = true;
            Checkpoint("CP_Alley", root, new Vector3(19.8f, 0f, -12f), new Vector3(0.8f, 3f, 3f), 90f);

            var shared = Group("Shared", root);
            var logic = Group("Logic", root);
            var past = StateGroup("Past Dressing", root, TimeMask.Past, false);
            var present = StateGroup("Present Dressing", root, TimeMask.Present, false);
            var future = StateGroup("Future Dressing", root, TimeMask.Future, false);

            BuildShell(shared);
            BuildSkyline(shared);
            BuildPump(root, logic, past, present, future);
            BuildLanesAndSluices(root, logic, past, present, future);
            BuildHouses(root, logic);
            BuildLaundryAndRescue(root, logic, past, present, future);
            BuildWardOfficeAndChoice(root, logic, past, present, future);
            BuildNurseryDoor(root, logic, shared);
            BuildNpcs(root, past, present, future);
            BuildDressing(shared, past, present, future);
            BuildSchoolhouse(root, logic);
            BuildFragments(root);
            BuildAudio(root);
        }

        // =====================================================================
        // Look
        // =====================================================================

        static void TuneVisuals(TemporalVisuals v)
        {
            v.past.fogDensity = 0.010f;
            v.past.sunEuler = new Vector3(24f, -60f, 0f);
            v.present.fogDensity = 0.03f;
            v.present.fogColor = new Color(0.2f, 0.28f, 0.34f);
            v.present.bloomIntensity = 1.3f;
            v.future.fogDensity = 0.024f;
            v.future.bloomIntensity = 2.4f;
            v.future.fogColor = new Color(0.12f, 0.06f, 0.22f);
        }

        // =====================================================================
        // Small private helpers
        // =====================================================================

        static void NotStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        static GameObject TiltBox(string name, Transform parent, Vector3 c, Vector3 size, string mat, Vector3 euler, bool collider = false)
        {
            var go = Box(name, parent, c, size, mat, collider);
            go.transform.rotation = Quaternion.Euler(euler);
            return go;
        }

        /// <summary>A gate whose panel lives in a state group while its reactor stays in shared logic.</summary>
        static FlagReactor Gate(string name, Transform logic, Transform visualParent, Vector3 ground, Vector3 size, string mat, Vector3 openBy, float yaw, string toast, params string[] flags)
        {
            var g = Group(name, visualParent);
            var mover = Group("Mover", g);
            mover.position = ground;
            var panel = Box("Panel", mover, ground + Vector3.up * size.y * 0.5f, size, mat, true, yaw);
            Box("Trim", mover, ground + Vector3.up * (size.y + 0.05f), new Vector3(size.x + 0.2f, 0.1f, size.z + 0.2f), "neon_amber", false, yaw);
            NotStatic(mover.gameObject);
            var r = Reactor(name + " Reactor", logic, flags);
            r.moveTarget = mover;
            r.moveBy = openBy;
            r.duration = 2.2f;
            r.announce = true;
            r.toast = toast;
            return r;
        }

        /// <summary>Snap socket whose anchor is not under a scaled transform (so the item keeps its shape).</summary>
        static SnapSocket MySocket(string name, Transform parent, Vector3 center, string key, string flag, string prompt, string mat = "iron_rust")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.7f, 0.5f, 0.7f);
            bc.isTrigger = true;
            Prim(PrimitiveType.Cylinder, "Yoke", go.transform, center, new Vector3(0.3f, 0.08f, 0.3f), mat, false, false);
            var s = go.AddComponent<SnapSocket>();
            s.acceptsKey = key;
            s.filledFlag = flag;
            s.prompt = prompt;
            s.wrongItemMessage = "That will not seat in the pump yoke.";
            s.anchor = Marker("Anchor", go.transform, center + Vector3.up * 0.12f);
            Hologram(name + "_Ring", parent, center + Vector3.up * 0.02f, new Vector3(0.6f, 0.01f, 0.6f), PrimitiveType.Cylinder, "hologram_amber", false, true);
            return s;
        }

        /// <summary>Flood water owned by an L05FloodLevel in shared logic (surface + optional blocker).</summary>
        static L05FloodLevel Flood(string name, Transform logic, Vector3 center, Vector2 surfaceSize, Vector2 blockerSize, float highY, float lowY, float blockAbove, bool snapWhenAway, string[] lowWhenAll, Vector3? blockerCenter = null)
        {
            var g = Group(name, logic);
            var surfRoot = Group("SurfaceRoot", g);
            surfRoot.position = new Vector3(center.x, highY, center.z);
            var surf = Box("Surface", surfRoot, new Vector3(center.x, highY, center.z), new Vector3(surfaceSize.x, 0.05f, surfaceSize.y), "water", false);
            surf.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            NotStatic(surf);
            var w = g.gameObject.AddComponent<L05FloodLevel>();
            w.surface = surfRoot;
            w.renderers.Add(surf.GetComponent<Renderer>());
            w.highY = highY;
            w.lowY = lowY;
            w.blockAboveY = blockAbove;
            w.snapWhenAway = snapWhenAway;
            w.lowWhenAll = new List<string>(lowWhenAll);
            if (blockerSize.x > 0f)
            {
                Vector3 bc = blockerCenter ?? center;
                var b = Box("DeepWaterBlocker", g, new Vector3(bc.x, 1.2f, bc.z), new Vector3(blockerSize.x, 4.4f, blockerSize.y), "water", true);
                Object.DestroyImmediate(b.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(b.GetComponent<MeshFilter>());
                w.blocker = b.GetComponent<Collider>();
            }
            return w;
        }

        /// <summary>Adds floating flotsam that rides on a flood surface.</summary>
        static void Flotsam(L05FloodLevel w, Vector3 center, Vector2 area, int count, int seed)
        {
            var rng = new System.Random(seed);
            string[] mats = { "wood", "wood_dark", "paper", "fabric_teal", "iron_rust" };
            for (int i = 0; i < count; i++)
            {
                Vector3 p = center + new Vector3(((float)rng.NextDouble() - 0.5f) * area.x, 0f, ((float)rng.NextDouble() - 0.5f) * area.y);
                p.y = w.surface.position.y + 0.03f;
                var size = new Vector3(0.3f + (float)rng.NextDouble() * 0.9f, 0.08f + (float)rng.NextDouble() * 0.12f, 0.2f + (float)rng.NextDouble() * 0.6f);
                var go = TiltBox("Flotsam", w.surface, p, size, mats[rng.Next(mats.Length)], new Vector3(0f, (float)rng.NextDouble() * 360f, ((float)rng.NextDouble() - 0.5f) * 10f));
                NotStatic(go);
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var bob = go.AddComponent<FloatBob>();
                bob.amplitude = 0.03f; bob.frequency = 0.3f + (float)rng.NextDouble() * 0.3f; bob.drift = new Vector3(0f, 6f, 0f);
                w.renderers.Add(go.GetComponent<Renderer>());
            }
        }

        static ParticleSystem Spray(string name, Transform parent, Vector3 pos, Vector3 euler, float speed, float rate, Color color)
        {
            var ps = Particles(name, parent, pos, new Vector3(0.2f, 0.2f, 0.2f), "steam");
            ps.transform.rotation = Quaternion.Euler(euler);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.7f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startLifetime = 0.9f;
            main.gravityModifier = 1.1f;
            main.startColor = color;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.angle = 8f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = AZMaterials.ParticleMaterial("spray", new Color(0.75f, 0.9f, 1f, 0.55f), false);
            return ps;
        }

        static void Crystals(string name, Transform parent, Vector3 ground, float scale, int seed, bool light = false)
        {
            var g = Group(name, parent);
            var rng = new System.Random(seed);
            int n = 4 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                float h = (0.8f + (float)rng.NextDouble() * 1.8f) * scale;
                float w = (0.25f + (float)rng.NextDouble() * 0.35f) * scale;
                Vector3 off = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.4f * scale, h * 0.35f, ((float)rng.NextDouble() - 0.5f) * 1.4f * scale);
                var c = TiltBox("Shard", g, ground + off, new Vector3(w, h, w), i == 0 ? "mineral_trace" : "crystal",
                    new Vector3(((float)rng.NextDouble() - 0.5f) * 40f, (float)rng.NextDouble() * 90f, ((float)rng.NextDouble() - 0.5f) * 40f), true);
                c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (light) PointLight("CrystalGlow", g, ground + Vector3.up * 1.2f * scale, Violet, 2.2f, 6f * scale);
        }

        /// <summary>Retints a resident into a holographic echo (Future memories of the families).</summary>
        static void MakeHolo(NPCMotor m, string key = "hologram")
        {
            foreach (var r in m.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                r.sharedMaterial = AZMaterials.Hologram(key);
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void Bunting(Transform parent, Vector3 a, Vector3 b, int flags, int seed)
        {
            var g = Group("Bunting", parent);
            Vector3 d = b - a;
            var line = Box("Cord", g, (a + b) * 0.5f, new Vector3(0.02f, 0.02f, d.magnitude), "wood_dark", false);
            line.transform.rotation = Quaternion.LookRotation(d.normalized);
            string[] mats = { "fabric_red", "fabric_gold", "fabric_teal" };
            for (int i = 0; i < flags; i++)
            {
                float t = (i + 0.5f) / flags;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 0.6f + 0.18f);
                var f = Box("Pennant", g, p, new Vector3(0.32f, 0.36f, 0.02f), mats[(i + seed) % 3], false);
                f.transform.rotation = Quaternion.LookRotation(d.normalized) * Quaternion.Euler(0f, 90f, 45f);
                f.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // =====================================================================
        // Shell: basin floor, terrace, stairs, building masses
        // =====================================================================

        static void BuildShell(Transform shared)
        {
            var s = Group("Basin Shell", shared);
            Floor("BasinFloor", s, new Vector3(0f, 0f, 0f), new Vector2(84f, 84f), "stone_warm", 0.6f);
            // Paving accents in the square (shared, cosmetic).
            Floor("SquarePaving", s, new Vector3(0f, 0.02f, -4f), new Vector2(27.6f, 39.6f), "stone_wet", 0.05f);
            Floor("PumpCircle", s, new Vector3(0f, 0.04f, -2f), new Vector2(7f, 7f), "brick_warm", 0.05f);

            // Weir Terrace (start): a solid retaining block, top at y = 3.
            Floor("WeirTerrace", s, new Vector3(0f, 3f, -30f), new Vector2(36f, 12f), "stone_warm", 3f);
            Box("TerraceLip", s, new Vector3(0f, 3.05f, -24.1f), new Vector3(36f, 0.12f, 0.3f), "brass");
            Wall("TerraceRailW", s, new Vector3(-18f, 3f, -24.15f), new Vector3(-2.2f, 3f, -24.15f), 1.05f, 0.25f, "stone_warm");
            Wall("TerraceRailE", s, new Vector3(2.2f, 3f, -24.15f), new Vector3(18f, 3f, -24.15f), 1.05f, 0.25f, "stone_warm");
            Wall("TerraceBack", s, new Vector3(-18f, 3f, -36f), new Vector3(18f, 3f, -36f), 4f, 0.5f, "brick_warm");
            Wall("TerraceSideW", s, new Vector3(-18f, 3f, -36f), new Vector3(-18f, 3f, -24f), 1.05f, 0.4f, "stone_warm");
            Wall("TerraceSideE", s, new Vector3(18f, 3f, -36f), new Vector3(18f, 3f, -24f), 1.05f, 0.4f, "stone_warm");
            Label("WeirPlaque", s, new Vector3(0f, 2.3f, -24.0f), 180f, "OLD VEYRA FLOOD BASIN\nWEIR STAIRS", 0.32f, TextWarm);

            Stairs("WeirStairs", s, new Vector3(0f, 0f, -19.5f), 180f, 15, 0.2f, 0.3f, 4f, "stone_warm");
            Wall("StairWallW", s, new Vector3(-2.2f, 0f, -19.5f), new Vector3(-2.2f, 0f, -24f), 3.9f, 0.4f, "stone_warm");
            Wall("StairWallE", s, new Vector3(2.2f, 0f, -19.5f), new Vector3(2.2f, 0f, -24f), 3.9f, 0.4f, "stone_warm");

            // Retaining wall below the terrace along the square.
            Wall("WeirFaceW", s, new Vector3(-14f, 0f, -24f), new Vector3(-2.4f, 0f, -24f), 3f, 0.3f, "stone_wet");
            Wall("WeirFaceE", s, new Vector3(2.4f, 0f, -24f), new Vector3(14f, 0f, -24f), 3f, 0.3f, "stone_wet");

            // Corner blocks (terraced houses). Windows face the lanes and square.
            var masses = Group("Terraces", s);
            Building("SW_A", masses, new Vector3(-22f, 0f, -20f), new Vector3(16f, 8f, 8f), "plaster_warm", "window_cold", "stone_warm", 51);
            Building("SW_B", masses, new Vector3(-26f, 0f, -12f), new Vector3(8f, 8.5f, 8f), "brick_warm", "window_cold", "stone_warm", 52);
            Building("SW_C", masses, new Vector3(-22f, 0f, -5f), new Vector3(16f, 7f, 6f), "plaster_warm", "window_cold", "stone_warm", 53);
            Building("NW_A", masses, new Vector3(-22f, 0f, 4f), new Vector3(16f, 9f, 4f), "brick_warm", "window_cold", "stone_warm", 54);
            Building("NW_B", masses, new Vector3(-26f, 0f, 10f), new Vector3(8f, 9f, 8f), "plaster_warm", "window_cold", "stone_warm", 55);
            Building("NW_C", masses, new Vector3(-22f, 0f, 15f), new Vector3(16f, 9f, 2f), "brick_warm", "window_cold", "stone_warm", 56);
            Building("N_W", masses, new Vector3(-16f, 0f, 23f), new Vector3(28f, 10f, 14f), "plaster_warm", "window_cold", "stone_warm", 57);
            Building("N_E", masses, new Vector3(16f, 0f, 23f), new Vector3(28f, 10f, 14f), "brick_warm", "window_cold", "stone_warm", 58);
            Building("NE_A", masses, new Vector3(27f, 0f, 9f), new Vector3(6f, 9f, 14f), "plaster_warm", "window_cold", "stone_warm", 59);
            Box("NE_SchoolS", masses, new Vector3(19f, 4.5f, 3f), new Vector3(10f, 9f, 2f), "brick_warm");
            Box("NE_SchoolN", masses, new Vector3(19f, 4.5f, 15f), new Vector3(10f, 9f, 2f), "brick_warm");
            Building("SE_A", masses, new Vector3(22f, 0f, -5f), new Vector3(16f, 8f, 6f), "brick_warm", "window_cold", "stone_warm", 60);
            Building("SE_B", masses, new Vector3(19f, 0f, -9.25f), new Vector3(10f, 8f, 2.5f), "plaster_warm", "window_cold", "stone_warm", 61);
            Building("SE_C", masses, new Vector3(19f, 0f, -14.75f), new Vector3(10f, 8f, 2.5f), "plaster_warm", "window_cold", "stone_warm", 62);
            Building("SE_D", masses, new Vector3(22f, 0f, -20f), new Vector3(16f, 8f, 8f), "brick_warm", "window_cold", "stone_warm", 63);
            Box("SE_E", masses, new Vector3(34f, 0f, -12f) + Vector3.up * new Vector3(8f, 8f, 16f).y * 0.5f, new Vector3(8f, 8f, 16f), "plaster_warm");
            Box("W_Back", masses, new Vector3(-36f, 0f, -14f) + Vector3.up * new Vector3(10f, 9f, 20f).y * 0.5f, new Vector3(10f, 9f, 20f), "brick_warm");
            Box("W_BackN", masses, new Vector3(-36f, 0f, 14f) + Vector3.up * new Vector3(10f, 9f, 20f).y * 0.5f, new Vector3(10f, 9f, 20f), "plaster_warm");
            Box("E_BackN", masses, new Vector3(36f, 0f, 14f) + Vector3.up * new Vector3(10f, 9f, 20f).y * 0.5f, new Vector3(10f, 9f, 20f), "brick_warm");
            Box("N_BackW", masses, new Vector3(-14f, 0f, 36f) + Vector3.up * new Vector3(18f, 11f, 12f).y * 0.5f, new Vector3(18f, 11f, 12f), "brick_warm");
            Box("N_BackE", masses, new Vector3(14f, 0f, 36f) + Vector3.up * new Vector3(18f, 11f, 12f).y * 0.5f, new Vector3(18f, 11f, 12f), "plaster_warm");
            Building("TerraceW", masses, new Vector3(-26f, 0f, -31f), new Vector3(16f, 11f, 14f), "plaster_warm", "window_cold", "stone_warm", 70);
            Building("TerraceE", masses, new Vector3(26f, 0f, -31f), new Vector3(16f, 11f, 14f), "brick_warm", "window_cold", "stone_warm", 71);
            Box("TerraceS", masses, new Vector3(0f, 0f, -41f) + Vector3.up * new Vector3(40f, 12f, 9f).y * 0.5f, new Vector3(40f, 12f, 9f), "brick_warm");

            ReflectionProbe(s, new Vector3(0f, 3f, -4f), new Vector3(30f, 12f, 42f));
            ReflectionProbe(s, new Vector3(0f, 5f, -30f), new Vector3(36f, 8f, 12f));
        }

        static void BuildSkyline(Transform shared)
        {
            var g = Group("Skyline", shared);
            var rng = new System.Random(505);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f + 0.2f;
                float r = 62f + (float)rng.NextDouble() * 20f;
                var c = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                var size = new Vector3(12f + (float)rng.NextDouble() * 6f, 14f + (float)rng.NextDouble() * 10f, 10f + (float)rng.NextDouble() * 8f);
                string sign = i % 3 == 0 ? "WARD 0" : i % 3 == 1 ? "VEYRA" : null;
                Building("Far" + i, g, c, size, i % 2 == 0 ? "brick_warm" : "plaster_cold", i % 3 == 0 ? "window_warm" : "window_cold", "stone_warm", 900 + i, sign, i % 2 == 0 ? "neon_cyan" : "neon_magenta");
            }
            // The clock tower on the horizon (north): the motif of the whole game.
            var tower = Group("DistantClockTower", g);
            Box("Shaft", tower, new Vector3(0f, 26f, 95f), new Vector3(9f, 52f, 9f), "stone_warm");
            Box("Belfry", tower, new Vector3(0f, 55f, 95f), new Vector3(11f, 6f, 11f), "brass");
            Box("Spire", tower, new Vector3(0f, 61f, 95f), new Vector3(4f, 6f, 4f), "wood_dark");
            var face = Prim(PrimitiveType.Cylinder, "ClockFace", tower, new Vector3(0f, 47f, 90.3f), new Vector3(7f, 0.1f, 7f), "zero_glow", false);
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("Seal", tower, new Vector3(0f, 47f, 90.1f), 0f, "(0)", 3f, new Color(0.2f, 0.3f, 0.4f));
            PointLight("TowerGlow", tower, new Vector3(0f, 47f, 86f), new Color(0.9f, 0.95f, 1f), 3f, 18f);
        }

        // =====================================================================
        // Beat 1: the pump (PAST) drains the basin (PRESENT)
        // =====================================================================

        static void BuildPump(Transform root, Transform logic, Transform past, Transform present, Transform future)
        {
            var p0 = new Vector3(0f, 0f, -2f);
            var shared = Group("Pump (shared plinth)", root);
            Box("Plinth", shared, p0 + Vector3.up * 0.3f, new Vector3(2.2f, 0.6f, 2.2f), "stone_warm");
            Box("PlinthStep", shared, p0 + Vector3.up * 0.1f, new Vector3(3.2f, 0.2f, 3.2f), "stone_warm");
            Box("Trough", shared, p0 + new Vector3(0f, 0.35f, -2.1f), new Vector3(2.2f, 0.7f, 0.9f), "stone_wet");

            // PAST: the pump itself, broken (handle missing). Gameplay group: echo on.
            var pastPump = StateGroup("Past Pump", root, TimeMask.Past, true);
            Cylinder("Column", pastPump, p0 + new Vector3(0f, 1.4f, 0f), 0.13f, 1.6f, "iron_rust");
            Sphere("Cap", pastPump, p0 + new Vector3(0f, 2.25f, 0f), 0.18f, "brass", false);
            var spout = Box("Spout", pastPump, p0 + new Vector3(0f, 1.6f, -0.35f), new Vector3(0.14f, 0.14f, 0.6f), "brass");
            Label("PumpSign", pastPump, p0 + new Vector3(0f, 1.15f, -1.12f), 0f, "COMMUNAL PUMP\nHANDLE MISSING - T. ASH", 0.18f, TextWarm);
            var socket = MySocket("PumpYoke", pastPump, p0 + new Vector3(0.32f, 2.0f, 0f), K_Handle, F_Pump, "Seat the handle in the pump yoke");
            socket.usableIn = TimeMask.Past;
            var gushPast = Spray("PumpGush", pastPump, p0 + new Vector3(0f, 1.55f, -0.65f), new Vector3(80f, 180f, 0f), 1.2f, 70f, new Color(0.8f, 0.9f, 1f, 0.6f));
            gushPast.gameObject.SetActive(false);
            PointLight("PumpLamp", pastPump, p0 + new Vector3(1.6f, 2.6f, -1.6f), Gas, 2.2f, 7f);

            // The handle: in the PAST, dropped in the children's chalk game on the north lane.
            var handleGroup = StateGroup("Past Handle", root, TimeMask.Past, true);
            var handle = Grab("PumpHandle", handleGroup, new Vector3(1.1f, 0.12f, 26.6f), new Vector3(0.12f, 0.12f, 1.1f), "iron_rust", K_Handle, "pump handle");
            handle.usableIn = TimeMask.Past;
            for (int i = 0; i < 6; i++)
            {
                var sq = Box("ChalkSquare", handleGroup, new Vector3(-0.6f + (i % 2) * 0.9f, 0.01f, 20f + i * 1.0f), new Vector3(0.8f, 0.01f, 0.8f), "paper", false);
                sq.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Label("Chalk", handleGroup, new Vector3(0f, 0.6f, 25.6f), 0f, "1 2 3 ... 0", 0.25f, Color.white);

            // PRESENT: the rusted pump. If mended in the past, water pours from the sump outlet.
            var presPump = StateGroup("Present Pump", root, TimeMask.Present, true);
            Cylinder("ColumnRust", presPump, p0 + new Vector3(0f, 1.4f, 0f), 0.13f, 1.6f, "iron_rust");
            var bar = Box("HandleRust", presPump, p0 + new Vector3(0.32f, 2.0f, 0.4f), new Vector3(0.1f, 0.1f, 1.0f), "iron_rust", false);
            bar.SetActive(false);
            var noHandle = Label("NoHandle", presPump, p0 + new Vector3(0f, 2.6f, -0.3f), 0f, "no handle - bolt holes worn smooth", 0.14f, TextCold);
            var outflow = Spray("SumpOutflow", presPump, p0 + new Vector3(0f, 0.5f, -2.6f), new Vector3(30f, 180f, 0f), 2.4f, 140f, new Color(0.6f, 0.75f, 0.85f, 0.5f));
            outflow.gameObject.SetActive(false);

            // FUTURE: the pump is a crystal-sheathed relic with a memorial plaque.
            var futPump = StateGroup("Future Pump", root, TimeMask.Future, true);
            Crystals("PumpCrystal", futPump, p0 + Vector3.up * 0.6f, 0.9f, 77);
            var plaque = Hologram("PumpPlaque", futPump, p0 + new Vector3(0f, 3.0f, 0f), new Vector3(1.6f, 0.5f, 0.02f), PrimitiveType.Cube, "hologram_amber", true, false);
            Label("PlaqueText", futPump, p0 + new Vector3(0f, 3.0f, -0.05f), 0f, "SUMP SEALED - CYCLE 1", 0.2f, Color.white);
            var plaqueFixed = Label("PlaqueFixed", futPump, p0 + new Vector3(0f, 3.5f, -0.05f), 0f, "SUMP OPEN SINCE THE FIRST CYCLE", 0.18f, Cyan);
            plaqueFixed.SetActive(false);

            // Consequence reactors (shared).
            var r = Reactor("Pump Repaired Reactor", logic, F_Pump);
            r.enableWhenTrue.Add(gushPast.gameObject);
            r.enableWhenTrue.Add(outflow.gameObject);
            r.enableWhenTrue.Add(bar);
            r.enableWhenTrue.Add(plaqueFixed);
            r.disableWhenTrue.Add(noHandle);
            r.announce = true;
            r.toast = "The mended pump keeps the old sump open. In the PRESENT, the basin drains.";

            // PRESENT basin flood: up to the windows until the pump is mended in the past.
            // Surfaces: north band (with the W/E lanes) and south band (stops short of the dry laundry).
            // Blocker: square + sluice house + ward office + alley mouth (x -22.2..16.4, z -21..16); the upper Weir Stairs stay usable.
            var basin = Flood("Basin Flood", logic, new Vector3(-8f, 0f, 7f), new Vector2(44f, 18f), new Vector2(38.6f, 37f), 2.2f, 0.35f, 1.1f, true,
                new[] { F_Pump }, new Vector3(-2.9f, 0f, -2.5f));
            var southSurf = Box("SurfaceSouth", basin.surface, new Vector3(-3f, 2.2f, -13f), new Vector3(54f, 0.05f, 22f), "water", false);
            southSurf.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            NotStatic(southSurf);
            basin.renderers.Add(southSurf.GetComponent<Renderer>());
            Flotsam(basin, new Vector3(0f, 0f, -4f), new Vector2(24f, 34f), 10, 15);
        }

        // =====================================================================
        // Beat 2: sluices + lanes (PRESENT); lane blocks in PAST (latched doors) and FUTURE (crystal)
        // =====================================================================

        static void BuildLanesAndSluices(Transform root, Transform logic, Transform past, Transform present, Transform future)
        {
            // Sluice House (SW): x -22..-14, z -16..-8, door on the east wall.
            var sh = Group("Sluice House", root);
            Room("Shell", sh, new Vector3(-18f, 0f, -12f), new Vector2(8f, 8f), 4.6f, "stone_wet", null, "wood_dark", 1);
            Label("SluiceSign", sh, new Vector3(-13.75f, 3.3f, -12f), -90f, "SLUICE HOUSE", 0.4f, TextWarm);

            var valves = StateGroup("Present Sluice Board", root, TimeMask.Present, true);
            Box("Board", valves, new Vector3(-21.6f, 1.8f, -12f), new Vector3(0.15f, 2.6f, 6.4f), "copper_verdigris");
            Label("BoardTitle", valves, new Vector3(-21.45f, 3.4f, -12f), -90f, "LANE SLUICES - TURN TO SEND EACH LANE TO THE CULVERT", 0.16f, TextCold);
            string[] lane = { "WEST LANE  (VELL)  <<", "NORTH LANE  (CORRAN)  [ ]", "EAST LANE  (OSKEL)  >>" };
            string[] flags = { F_SluiceW, F_SluiceN, F_SluiceE };
            for (int i = 0; i < 3; i++)
            {
                float z = -14f + i * 2f;
                var pivot = new GameObject("Wheel_" + i);
                pivot.transform.SetParent(valves, false);
                pivot.transform.position = new Vector3(-21.25f, 1.5f, z);
                pivot.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                var col = pivot.AddComponent<BoxCollider>();
                col.size = new Vector3(0.95f, 0.2f, 0.95f);
                Prim(PrimitiveType.Cylinder, "Rim", pivot.transform, pivot.transform.position, new Vector3(0.9f, 0.04f, 0.9f), "iron_rust", false, false).transform.rotation = pivot.transform.rotation;
                for (int k = 0; k < 2; k++)
                {
                    var spoke = Prim(PrimitiveType.Cube, "Spoke", pivot.transform, pivot.transform.position, new Vector3(0.06f, 0.1f, 0.85f), "brass", false, false);
                    spoke.transform.rotation = pivot.transform.rotation * Quaternion.Euler(0f, k * 90f, 0f);
                }
                var wheel = pivot.AddComponent<FlagInteractable>();
                wheel.prompt = "Turn the " + lane[i].Split(' ')[0].ToLower() + " lane sluice";
                wheel.setFlag = flags[i];
                wheel.usableIn = TimeMask.Present;
                wheel.animateTarget = pivot.transform;
                wheel.rotateBy = new Vector3(0f, 540f, 0f);
                wheel.animateDuration = 2.2f;
                wheel.cue = Cue.Clang;
                wheel.requiredFlags.Add(F_Pump);
                wheel.lockedMessage = "The wheel will not move against a basin full of water. Something in the PAST must drain it first.";
                Label("Lane" + i, valves, new Vector3(-21.4f, 2.35f, z), -90f, lane[i], 0.13f, TextCold);
            }
            PointLight("SluiceLight", valves, new Vector3(-18f, 4f, -12f), Sodium, 2f, 9f);
            var pastSluice = StateGroup("Past Sluice House", root, TimeMask.Past, false);
            Label("PastSluiceNote", pastSluice, new Vector3(-21.45f, 2.2f, -12f), -90f, "CULVERT GATES OPEN\nAS LONG AS THE PUMP RUNS", 0.2f, TextWarm);
            PointLight("PastSluiceLamp", pastSluice, new Vector3(-18f, 3.8f, -12f), Gas, 1.5f, 8f);
            var futSluice = StateGroup("Future Sluice House", root, TimeMask.Future, false);
            Crystals("SluiceCrystals", futSluice, new Vector3(-20f, 0f, -14.5f), 0.8f, 31);

            // Lanes: x/z extents, gate positions, house locations.
            BuildLane(root, logic, "West", F_SluiceW, new Vector3(-22.2f, 0f, 0f), new Vector2(15.6f, 4f), new Vector3(-14.2f, 0f, 0f), 90f, new Vector3(-15.2f, 0f, 0f), new Vector3(-29.8f, 0f, 0f), 90f);
            BuildLane(root, logic, "East", F_SluiceE, new Vector3(22.2f, 0f, 0f), new Vector2(15.6f, 4f), new Vector3(14.2f, 0f, 0f), 90f, new Vector3(15.2f, 0f, 0f), new Vector3(29.8f, 0f, 0f), 90f);
            BuildLane(root, logic, "North", F_SluiceN, new Vector3(0f, 0f, 23.1f), new Vector2(4f, 13.8f), new Vector3(0f, 0f, 16.2f), 0f, new Vector3(0f, 0f, 17.2f), new Vector3(0f, 0f, 29.8f), 0f);

            var all = Reactor("Water Redirected", logic, F_SluiceW, F_SluiceN, F_SluiceE);
            all.setFlagWhenTrue = F_Redirected;
            all.announce = true;
            all.cue = Cue.PuzzleSolved;
            all.toast = "All three lanes are running to the culvert. The family houses are open.";
        }

        static void BuildLane(Transform root, Transform logic, string name, string flag, Vector3 center, Vector2 size, Vector3 gatePos, float gateYaw, Vector3 crystalPos, Vector3 doorPos, float doorYaw)
        {
            bool alongX = gateYaw == 90f;
            // Lane water (PRESENT) behind the sluice gate.
            var w = Flood(name + " Lane Flood", logic, center, size, size, 1.9f, -0.35f, 0.9f, true, new[] { flag });
            Flotsam(w, center, size * 0.8f, 3, name.Length * 13);

            // PRESENT: the sluice gate across the lane mouth; slides into the floor once opened.
            var gateGroup = StateGroup("Present " + name + " Sluice Gate", root, TimeMask.Present, true);
            Gate(name + " Gate", logic, gateGroup, gatePos, new Vector3(4.2f, 2.8f, 0.3f), "iron_rust", new Vector3(0f, -2.9f, 0f), gateYaw,
                "The " + name.ToLower() + " lane drains into the culvert.", flag);
            Label(name + "GateSign", gateGroup, gatePos + Vector3.up * 3.3f + (alongX ? new Vector3(gatePos.x > 0 ? -0.3f : 0.3f, 0, 0) : new Vector3(0, 0, -0.3f)),
                alongX ? (gatePos.x > 0 ? 90f : -90f) : 0f, "SLUICE - " + name.ToUpper() + " LANE", 0.22f, TextCold);
            var drain = Spray(name + "Drain", gateGroup, gatePos + Vector3.up * 0.4f, new Vector3(20f, alongX ? (gatePos.x > 0 ? -90f : 90f) : 180f, 0f), 2.6f, 120f, new Color(0.6f, 0.75f, 0.85f, 0.5f));
            drain.gameObject.SetActive(false);
            var dr = Reactor(name + " Drain FX", logic, flag);
            dr.enableWhenTrue.Add(drain.gameObject);

            // FUTURE: crystal growth seals the lane mouth.
            var fut = StateGroup("Future " + name + " Crystal Seal", root, TimeMask.Future, true);
            for (int i = -1; i <= 1; i++)
            {
                Vector3 o = alongX ? new Vector3(0f, 0f, i * 1.4f) : new Vector3(i * 1.4f, 0f, 0f);
                Crystals("Seal" + i, fut, crystalPos + o, 1.3f, 300 + i + name.Length);
            }
            Box("CrystalWall", fut, crystalPos + Vector3.up * 1.6f, alongX ? new Vector3(0.6f, 3.2f, 4f) : new Vector3(4f, 3.2f, 0.6f), "crystal");

            // PAST: the house door is latched (the family is at supper).
            var latched = StateGroup("Past " + name + " Latched Door", root, TimeMask.Past, true);
            var door = Box("LatchedDoor", latched, doorPos + Vector3.up * 1.3f, alongX ? new Vector3(0.15f, 2.6f, 2.0f) : new Vector3(2.0f, 2.6f, 0.15f), "wood_dark");
            Vector3 outward = alongX ? new Vector3(doorPos.x > 0 ? -0.15f : 0.15f, 0f, 0f) : new Vector3(0f, 0f, -0.15f);
            Label("LatchNote", latched, doorPos + Vector3.up * 1.7f + outward, alongX ? (doorPos.x > 0 ? 90f : -90f) : 0f, "LATCHED\nsupper is on - come back after Bell Day", 0.13f, TextWarm);
            Sphere("Knocker", latched, doorPos + Vector3.up * 1.2f + outward, 0.07f, "brass", false);
        }

        // =====================================================================
        // Beat 3: compare the three family rooms
        // =====================================================================

        static void BuildHouses(Transform root, Transform logic)
        {
            House(root, "Vell", new Vector3(-34f, 0f, 0f), 1, Vector3.right, F_RoomVell, 11,
                "The Vell family - wall calendar",
                "Swollen with flood water, but the ink held. Every day is crossed out up to today. Tomorrow is circled three times: 'BELL DAY. Children to the tower at first light.'\n\nBeside it, the family portrait. Every face has been scratched away.");
            House(root, "Oskel", new Vector3(34f, 0f, 0f), 3, Vector3.left, F_RoomOskel, 22,
                "The Oskel family - calendar above the cot",
                "Tomorrow is marked with a little carved paper zero. Under it, a mother's note: 'Maren - if we are separated, wait where the water cannot follow. We will come for you.' A newer slip is pinned over the family name: STRUCK BY ORDER OF FOUNDER A. HALDEN.\n\nThe portrait's faces are scratched out. Only the smallest one was left - a girl with a lantern.");
            House(root, "Corran", new Vector3(0f, 0f, 34f), 2, Vector3.back, F_RoomCorran, 33,
                "The Corran family - kitchen calendar",
                "Tomorrow ringed in red pencil. In a child's hand underneath: 'the bell will carry us'. Someone scraped the Corran name off the door plate, and off the portrait frame - but missed this.");

            var compared = Reactor("Rooms Compared", logic, F_RoomVell, F_RoomCorran, F_RoomOskel);
            compared.setFlagWhenTrue = F_Rooms;
            compared.announce = true;
            compared.cue = Cue.Revelation;
        }

        /// <param name="doorSide">Room door side: 0 = N, 1 = E, 2 = S, 3 = W.</param>
        /// <param name="toDoor">Direction from the room centre toward its door.</param>
        static void House(Transform root, string family, Vector3 c, int doorSide, Vector3 toDoor, string clueFlag, int seed, string clueTitle, string clueText)
        {
            var g = Group("House " + family, root);
            Room("Shell", g, c, new Vector2(8f, 8f), 4.6f, "plaster_warm", null, "wood_dark", doorSide);
            Box("Floorboards", g, c + Vector3.up * 0.015f, new Vector3(7.6f, 0.03f, 7.6f), "wood_dark", false);
            // Upper storey and roof (the terraced look).
            Box("UpperStorey", g, c + Vector3.up * 6.6f, new Vector3(8.4f, 3.6f, 8.4f), "brick_warm");
            Box("Roof", g, c + Vector3.up * 8.6f, new Vector3(8.8f, 0.4f, 8.8f), "wood_dark");
            Vector3 front = c + toDoor * 4.25f;
            float signYaw = Quaternion.LookRotation(-toDoor).eulerAngles.y;
            Label("NamePlate", g, front + Vector3.up * 3.1f, signYaw, "No. " + (seed / 11) + "  " + family.ToUpper(), 0.3f, TextWarm);

            Vector3 back = c - toDoor * 3.6f;
            Vector3 right = Vector3.Cross(Vector3.up, toDoor).normalized; // along the back wall
            float faceIn = Quaternion.LookRotation(-toDoor).eulerAngles.y; // read by someone facing the back wall
            Vector3 portrait = back + right * -1.8f + Vector3.up * 2.1f;
            Vector3 calendar = back + right * 1.8f + Vector3.up * 1.7f;
            Vector3 wallNormal = toDoor; // points into the room from the back wall

            // ---------------- PAST ----------------
            var past = StateGroup("Past " + family + " Home", g, TimeMask.Past, false);
            Box("Table", past, c + Vector3.up * 0.75f, new Vector3(2.2f, 0.1f, 1.2f), "wood");
            Box("TableLegs", past, c + Vector3.up * 0.37f, new Vector3(1.9f, 0.74f, 0.9f), "wood_dark");
            for (int i = 0; i < 4; i++)
            {
                Vector3 o = new Vector3(i < 2 ? -0.7f : 0.7f, 0f, i % 2 == 0 ? -0.95f : 0.95f);
                Box("Chair", past, c + o + Vector3.up * 0.45f, new Vector3(0.45f, 0.9f, 0.45f), "wood");
            }
            Box("Supper", past, c + Vector3.up * 0.86f, new Vector3(1.4f, 0.12f, 0.5f), "fabric_gold", false);
            Box("PortraitFrame", past, portrait + wallNormal * 0.05f, Rot(new Vector3(1.6f, 1.2f, 0.06f), toDoor), "brass", false);
            Box("PortraitCanvas", past, portrait + wallNormal * 0.09f, Rot(new Vector3(1.4f, 1.0f, 0.02f), toDoor), "plaster_warm", false);
            for (int i = 0; i < 4; i++)
                Sphere("Face" + i, past, portrait + wallNormal * 0.11f + right * (-0.5f + i * 0.33f) + Vector3.up * (i == 3 ? -0.18f : 0.05f), i == 3 ? 0.08f : 0.11f, "skin", false);
            Label("PortraitName", past, portrait + wallNormal * 0.12f + Vector3.down * 0.75f, faceIn, "THE " + family.ToUpper() + " FAMILY", 0.16f, TextWarm);
            Box("CalendarPast", past, calendar + wallNormal * 0.05f, Rot(new Vector3(0.7f, 0.9f, 0.03f), toDoor), "paper", false);
            Label("CalendarPastText", past, calendar + wallNormal * 0.08f, faceIn, "TOMORROW\nBELL DAY", 0.13f, new Color(0.6f, 0.15f, 0.1f));
            PointLight("Hearth", past, c + Vector3.up * 3.2f, Gas, 2.6f, 9f);
            var fam0 = Npc(family + " Elder", past, c + right * -0.9f + toDoor * 1.4f, 0f, "coat_burgundy", false, seed);

            fam0.reactionBarks.Add("Who's knocking at supper?");

            Route(fam0, false, (fam0.transform.position, 6f, NPCActivity.Work, ""), (c + right * 1.6f, 4f, NPCActivity.Carry, ""), (c - toDoor * 2f, 4f, NPCActivity.Look, ""));

            // ---------------- PRESENT ----------------
            var pres = StateGroup("Present " + family + " Room", g, TimeMask.Present, true);
            TiltBox("OverturnedTable", pres, c + new Vector3(0.4f, 0.6f, 0.2f), new Vector3(2.2f, 0.1f, 1.2f), "wood_dark", new Vector3(80f, 20f, 0f), true);
            TiltBox("Chair", pres, c + right * 1.6f + Vector3.up * 0.25f, new Vector3(0.45f, 0.9f, 0.45f), "wood_dark", new Vector3(0f, 30f, 85f));
            Box("Puddle", pres, c + Vector3.up * 0.02f, new Vector3(6.5f, 0.02f, 6.5f), "water_clear", false);
            for (int i = 0; i < 5; i++)
                Box("WetWallpaper", pres, back + wallNormal * 0.04f + right * (-3f + i * 1.5f) + Vector3.up * (2.2f + (i % 2) * 0.3f), Rot(new Vector3(0.6f, 2.2f, 0.02f), toDoor), "plaster_cold", false);
            Box("PortraitFrame", pres, portrait + wallNormal * 0.05f, Rot(new Vector3(1.6f, 1.2f, 0.06f), toDoor), "iron_rust", false);
            Box("PortraitCanvas", pres, portrait + wallNormal * 0.09f, Rot(new Vector3(1.4f, 1.0f, 0.02f), toDoor), "plaster_cold", false);
            for (int i = 0; i < 4; i++)
            {
                bool spared = family == "Oskel" && i == 3;
                Vector3 fp = portrait + wallNormal * 0.11f + right * (-0.5f + i * 0.33f) + Vector3.up * (i == 3 ? -0.18f : 0.05f);
                if (spared) Sphere("SparedFace", pres, fp, 0.08f, "skin", false);
                else Box("Scratched" + i, pres, fp, Rot(new Vector3(0.24f, 0.24f, 0.02f), toDoor), "ruin_dark", false);
            }
            Label("ScratchNote", pres, portrait + wallNormal * 0.13f + Vector3.down * 0.75f, faceIn, "faces scratched away", 0.14f, TextCold);
            Clue("Calendar", pres, calendar + wallNormal * 0.06f, Rot(new Vector3(0.7f, 0.9f, 0.08f), toDoor), "paper", "Read the " + family + " calendar", clueFlag,
                "clue.L05." + family.ToLower(), clueTitle, clueText, true, TimeMask.Present);
            Label("CalendarMark", pres, calendar + wallNormal * 0.12f + Vector3.up * 0.1f, faceIn, "TOMORROW\n(O)", 0.16f, new Color(1f, 0.35f, 0.3f));
            PointLight("ColdLight", pres, c + Vector3.up * 3.6f + toDoor * 2f, ColdTeal, 1.6f, 8f);
            var drips = Particles("Drips", pres, c + Vector3.up * 4.2f, new Vector3(6f, 0.1f, 6f), "rain");
            var dem = drips.emission; dem.rateOverTime = 120f;

            // ---------------- FUTURE ----------------
            var fut = StateGroup("Future " + family + " Ruin", g, TimeMask.Future, false);
            Crystals("RoomCrystalsA", fut, c + right * 2.6f + toDoor * -2.2f, 1.1f, seed * 3);
            Crystals("RoomCrystalsB", fut, c + right * -2.8f + toDoor * 1.8f, 0.9f, seed * 5);
            Box("NeonSeam", fut, back + wallNormal * 0.05f + Vector3.up * 0.1f, Rot(new Vector3(7.6f, 0.06f, 0.06f), toDoor), "neon_magenta", false);
            Box("NeonSeamTop", fut, back + wallNormal * 0.05f + Vector3.up * 4.4f, Rot(new Vector3(7.6f, 0.06f, 0.06f), toDoor), "neon_cyan", false);
            for (int i = 0; i < 3; i++)
            {
                float h = i == 2 ? 1.0f : 1.65f;
                Vector3 fp = c + right * (-1.2f + i * 1.2f) - toDoor * 0.6f;
                var body = Hologram("FamilyEcho" + i, fut, fp + Vector3.up * h * 0.5f, new Vector3(0.45f, h * 0.5f, 0.3f), PrimitiveType.Capsule, "hologram_magenta", true, false);
                Hologram("EchoHead" + i, fut, fp + Vector3.up * (h + 0.12f), Vector3.one * 0.26f / 1f, PrimitiveType.Sphere, "hologram_magenta", false, false);
            }
            Label("Expunged", fut, c + Vector3.up * 3.2f, faceIn, family.ToUpper() + " - RECORD EXPUNGED", 0.22f, new Color(1f, 0.5f, 0.9f));
            Hologram("RecordGlyph", fut, c + Vector3.up * 2.6f, new Vector3(0.5f, 0.5f, 0.5f), PrimitiveType.Cube, "hologram", true, true);
            PointLight("EchoLight", fut, c + Vector3.up * 2.2f, Magenta, 1.8f, 7f);
            var motes = Particles("Motes", fut, c + Vector3.up * 2f, new Vector3(7f, 3.5f, 7f), "motes");
            var mem = motes.emission; mem.rateOverTime = 40f;

            // Future echo voices (optional flavour; reading them is not required).
            var echoVoice = Zone(family + " Echo Voices", g, c + Vector3.up * 1f, new Vector3(6f, 2f, 6f), "L05.echo" + family, "", TimeMask.Future);
            echoVoice.lines.Add(L(family + " echo", family == "Oskel" ? "Maren, hold the lantern high. We'll find you by the light." :
                family == "Vell" ? "Pack the good linen. We leave for the tower before dawn." : "The bell will carry us. Everyone says so."));
        }

        /// <summary>Returns a box size rotated so its local X runs along the wall whose normal is n.</summary>
        static Vector3 Rot(Vector3 size, Vector3 n)
        {
            return Mathf.Abs(n.x) > 0.5f ? new Vector3(size.z, size.y, size.x) : size;
        }
    }
}
