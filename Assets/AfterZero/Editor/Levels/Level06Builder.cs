using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 06 — The Nursery With No Address (a spatial seam between states).
    /// A nursery floating in a void, stitched from all three states: a PAST wood-panelled wall and
    /// cot, PRESENT wet wallpaper, a FUTURE holographic lattice. Echoes are always visible here.
    /// Route (y = 0): arrival landing z -24..-17 -> state-segment bridge (PAST plank, PRESENT grate,
    /// FUTURE light) -> nursery x -8..8 / z 0..16 -> three ledges on the north edge (drifting door)
    /// -> seam bridges -> Witness platform z 22..30 -> exit bridge -> tower landing z 36..42.
    /// </summary>
    public static class Level06Builder
    {
        const string F_Reached = "L06.nurseryReached";
        const string F_Shapes = "L06.shapesAligned";
        const string F_Heard = "L06.lullabyHeard";
        const string F_Lullaby = "L06.lullabyComplete";
        const string F_Anchored = "L06.doorAnchored";
        const string F_Approach = "L06.witnessApproach";
        const string F_Witness = "L06.witnessHeard";
        const string F_Zero = "L06.zeroTaken";
        const string F_Exit = "L06.returnedToTower";
        const string K_Zero = "L06_zero";

        static readonly Color Warm = new Color(1f, 0.76f, 0.5f);
        static readonly Color Cold = new Color(0.5f, 0.78f, 0.9f);
        static readonly Color Cyan = new Color(0.3f, 0.9f, 1f);
        static readonly Color Magenta = new Color(1f, 0.3f, 0.85f);
        static readonly Color Violet = new Color(0.6f, 0.4f, 1f);
        static readonly Color TextWarm = new Color(1f, 0.9f, 0.7f);
        static readonly Color TextCold = new Color(0.8f, 0.92f, 1f);
        static readonly Color TextHolo = new Color(1f, 0.6f, 0.95f);

        public static void Build(Transform root)
        {
            var d = Systems(root, 5, TimeMask.All, TimeState.Present, out var visuals);
            TuneVisuals(visuals);

            d.storySetup = "A nursery that appears only when details from all three states align. It is not a fourth era - it is a seam, stitched together from the PAST, the PRESENT and the FUTURE at once.";
            d.introLines.Add(L("Arin", "This isn't the Past, or the Present, or the Future. It's all three at once."));
            d.introLines.Add(L("Arin", "A bridge in pieces... one piece in each state."));
            d.introLines.Add(L("The Witness", "Look closely, Arin. Nothing here belongs to one time alone."));

            AddObjective(d, "Cross the seam into the nursery", F_Reached, "The plank exists in the PAST, the grate in the PRESENT, the light-bridge in the FUTURE. Shift only while standing on the stone steps.");
            AddObjective(d, "Turn the hanging shapes to face their state marks", F_Shapes, "Each shape hangs in only one state. Its mark - a floor arrow beneath it - is painted in another state, so look at the echo under the shape and turn the shape's arrow to match.");
            AddObjective(d, "Mend the three music boxes with your hum", F_Lullaby, "The boxes sit on the north ledges behind the furniture, one per state. Wind each in its own state, then hum (H / HUM) beside it. Keep the drifting door in view - if it slips away, the room forgets the boxes you mended.");
            AddObjective(d, "Reach the drifting door - keep it in view", F_Anchored, "The door drifts between the west, east and south ledges while nobody watches it. Walk to it with it on screen.");
            AddObjective(d, "Hear The Witness", F_Witness, "Cross the seam bridge to the figure of light.");
            AddObjective(d, "Take the carved zero", F_Zero, "It rests on the black plinth on the Witness platform.");
            AddObjective(d, "Return to the tower before the final bell", F_Exit, "Follow the bridge north toward the clock tower.");

            d.payoffFlag = F_Witness;
            d.payoffClueTitle = "An Attempted Rescue";
            d.payoffClueText = "The Bell Catastrophe may not have been an attack, an accident or a punishment. The founders rang the bell to carry Veyra's children out of time - into the nursery with no address. The first ring opened the way. The second ring, the one that brings the city down, was never part of the plan.";

            Player(root, new Vector3(0f, 0.05f, -22.4f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0f, 0f, -22.4f), new Vector3(6f, 3f, 3f), 0f, true);
            Checkpoint("CP_Nursery", root, new Vector3(0f, 0f, 1.6f), new Vector3(5f, 3f, 2.4f), 0f);
            Checkpoint("CP_Climax", root, new Vector3(0f, 0f, 12.2f), new Vector3(4f, 3f, 2f), 0f).climax = true;
            Checkpoint("CP_Witness", root, new Vector3(0f, 0f, 23.4f), new Vector3(8f, 3f, 2.4f), 0f);
            KillPlane(root, new Vector3(0f, -25f, 10f), new Vector3(400f, 4f, 400f), "You fall through the seam - and wake where you last stood.");

            var logic = Group("Logic", root);
            var shared = Group("Shared", root);

            BuildArrival(root, logic, shared);
            BuildNurseryShell(root, logic, shared);
            BuildMobile(root, logic, shared);
            BuildLullaby(root, logic, shared);
            BuildDriftingDoor(root, logic, shared);
            BuildWitness(root, logic, shared);
            BuildExit(root, logic, shared);
            BuildVoid(root, shared);
            BuildCupboard(root, logic, shared);
            BuildFragments(root, shared);
            BuildNpcs(root, logic);
            BuildAudio(root);
        }

        static void TuneVisuals(TemporalVisuals v)
        {
            v.echoesAlwaysOn = true;
            v.past.fogColor = new Color(0.28f, 0.17f, 0.2f); v.past.fogDensity = 0.012f; v.past.skyExposure = 0.55f; v.past.sunIntensity = 0.9f;
            v.past.bloomIntensity = 1.4f; v.past.vignette = 0.38f;
            v.present.fogColor = new Color(0.1f, 0.15f, 0.22f); v.present.fogDensity = 0.014f; v.present.skyExposure = 0.35f;
            v.present.bloomIntensity = 1.5f; v.present.vignette = 0.4f;
            v.future.fogColor = new Color(0.1f, 0.05f, 0.2f); v.future.fogDensity = 0.012f;
            v.future.bloomIntensity = 2.6f; v.future.vignette = 0.42f;
        }

        // =====================================================================
        // Helpers
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

        static void MakeHolo(NPCMotor m, string key)
        {
            foreach (var r in m.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                r.sharedMaterial = AZMaterials.Hologram(key);
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
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
                    new Vector3(((float)rng.NextDouble() - 0.5f) * 40f, (float)rng.NextDouble() * 90f, ((float)rng.NextDouble() - 0.5f) * 40f), false);
                c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (light) PointLight("CrystalGlow", g, ground + Vector3.up * 1.2f * scale, Violet, 2f, 6f * scale);
        }

        static FlagReactor Gate(string name, Transform logic, Transform visualParent, Vector3 ground, Vector3 size, string mat, Vector3 openBy, string toast, params string[] flags)
        {
            var g = Group(name, visualParent);
            var mover = Group("Mover", g);
            mover.position = ground;
            Box("Panel", mover, ground + Vector3.up * size.y * 0.5f, size, mat);
            Box("Trim", mover, ground + Vector3.up * (size.y + 0.05f), new Vector3(size.x + 0.2f, 0.1f, size.z + 0.2f), "neon_cyan", false);
            NotStatic(mover.gameObject);
            var r = Reactor(name + " Reactor", logic, flags);
            r.moveTarget = mover;
            r.moveBy = openBy;
            r.duration = 1.8f;
            r.announce = true;
            r.toast = toast;
            r.reversible = false;
            return r;
        }

        /// <summary>Floating fragment that slowly bobs and turns (void dressing).</summary>
        static GameObject Drift(string name, Transform parent, Vector3 pos, Vector3 size, string mat, int seed)
        {
            var go = TiltBox(name, parent, pos, size, mat, new Vector3(seed * 17f % 40f, seed * 53f % 360f, seed * 29f % 30f));
            NotStatic(go);
            var b = go.AddComponent<FloatBob>();
            b.amplitude = 0.25f + (seed % 5) * 0.08f;
            b.frequency = 0.15f + (seed % 4) * 0.05f;
            go.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 3f + seed % 7, 1.5f);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // =====================================================================
        // Arrival landing + the state-segment bridge (echo overlap introduced safely)
        // =====================================================================

        static void BuildArrival(Transform root, Transform logic, Transform shared)
        {
            var a = Group("Arrival", shared);
            Floor("Landing", a, new Vector3(0f, 0f, -20.5f), new Vector2(10f, 7f), "stone_warm", 1.2f);
            Box("LandingUnderside", a, new Vector3(0f, -2.2f, -20.5f), new Vector3(7f, 2.4f, 5f), "ruin_dark");
            Wall("LandingRailW", a, new Vector3(-5f, 0f, -24f), new Vector3(-5f, 0f, -17f), 1.1f, 0.25f, "wood_dark");
            Wall("LandingRailE", a, new Vector3(5f, 0f, -24f), new Vector3(5f, 0f, -17f), 1.1f, 0.25f, "wood_dark");
            Wall("LandingRailNW", a, new Vector3(-5f, 0f, -17.1f), new Vector3(-1.3f, 0f, -17.1f), 1.1f, 0.25f, "wood_dark");
            Wall("LandingRailNE", a, new Vector3(1.3f, 0f, -17.1f), new Vector3(5f, 0f, -17.1f), 1.1f, 0.25f, "wood_dark");

            // The seam Arin came through (back to the Drowned District) - decorative, sealed.
            Box("SeamPostL", a, new Vector3(-0.95f, 1.45f, -23.8f), new Vector3(0.25f, 2.9f, 0.3f), "wood_dark");
            Box("SeamPostR", a, new Vector3(0.95f, 1.45f, -23.8f), new Vector3(0.25f, 2.9f, 0.3f), "wood_dark");
            Box("SeamLintel", a, new Vector3(0f, 3.0f, -23.8f), new Vector3(2.15f, 0.3f, 0.3f), "wood_dark");
            Hologram("SeamBehind", a, new Vector3(0f, 1.4f, -23.8f), new Vector3(1.6f, 2.6f, 0.04f), PrimitiveType.Cube, "hologram_magenta", false, false);
            Label("SeamText", a, new Vector3(0f, 3.5f, -23.6f), 180f, "THE DROWNED DISTRICT", 0.2f, TextHolo);

            // Bridge: PAST plank, stone, PRESENT grate, stone, FUTURE light-bridge.
            var plank = StateGroup("Past Plank", root, TimeMask.Past, true);
            Floor("Plank", plank, new Vector3(0f, 0f, -14.5f), new Vector2(2f, 5f), "wood", 0.25f);
            for (int i = 0; i < 4; i++) Box("Batten", plank, new Vector3(0f, 0.02f, -16.5f + i * 1.3f), new Vector3(2.05f, 0.05f, 0.12f), "wood_dark", false);
            PointLight("PlankLantern", plank, new Vector3(1.2f, 1.2f, -14.5f), Warm, 1.6f, 5f);
            Floor("Stone1", a, new Vector3(0f, 0f, -11.25f), new Vector2(2.6f, 1.5f), "stone_warm", 0.8f);
            var grate = StateGroup("Present Grate", root, TimeMask.Present, true);
            Floor("Grate", grate, new Vector3(0f, 0f, -8.25f), new Vector2(2f, 4.5f), "metal_dark", 0.2f);
            Box("GrateRailL", grate, new Vector3(-1.05f, 0.5f, -8.25f), new Vector3(0.06f, 1.0f, 4.5f), "iron_rust");
            Box("GrateRailR", grate, new Vector3(1.05f, 0.5f, -8.25f), new Vector3(0.06f, 1.0f, 4.5f), "iron_rust");
            PointLight("GrateSodium", grate, new Vector3(-1.2f, 1.6f, -8.25f), new Color(1f, 0.6f, 0.3f), 1.6f, 5f);
            Floor("Stone2", a, new Vector3(0f, 0f, -5.25f), new Vector2(2.6f, 1.5f), "stone_warm", 0.8f);
            var light = StateGroup("Future Light Bridge", root, TimeMask.Future, true);
            Floor("LightBridge", light, new Vector3(0f, 0f, -2.25f), new Vector2(2f, 4.5f), "hologram", 0.12f);
            Box("EdgeL", light, new Vector3(-1.02f, 0.03f, -2.25f), new Vector3(0.05f, 0.05f, 4.5f), "neon_cyan", false);
            Box("EdgeR", light, new Vector3(1.02f, 0.03f, -2.25f), new Vector3(0.05f, 0.05f, 4.5f), "neon_cyan", false);
            PointLight("BridgeGlow", light, new Vector3(0f, 0.6f, -2.25f), Cyan, 1.8f, 5f);

            // Readable by glyph, not colour.
            Label("GlyphPlank", a, new Vector3(0f, 1.9f, -16.9f), 0f, "<< PAST", 0.3f, TextWarm);
            Label("GlyphGrate", a, new Vector3(0f, 1.9f, -10.6f), 0f, "[ PRESENT ]", 0.3f, TextCold);
            Label("GlyphLight", a, new Vector3(0f, 1.9f, -4.6f), 0f, "FUTURE >>", 0.3f, TextHolo);
            Label("BridgeRule", a, new Vector3(0f, 2.6f, -17.2f), 0f, "shift only while standing on stone", 0.16f, Color.white);

            var hint = Zone("Bridge Hint", logic, new Vector3(0f, 1f, -18.2f), new Vector3(4f, 2f, 1.6f), "L06.bridgeHint", "Each piece of the bridge exists in only one state. Shift while standing on stone.");
            hint.lines.Add(L("Arin", "The plank is only there in the PAST. Then the grate, then the light..."));

            var reached = Zone("Nursery Reached", logic, new Vector3(0f, 1f, 1.6f), new Vector3(5f, 2f, 2.4f), F_Reached);
            reached.lines.Add(L("Arin", "A nursery. A cot from the Past, wallpaper from the Present, a mobile made of light."));
            reached.lines.Add(L("Arin", "None of it should be in the same room."));
        }

        // =====================================================================
        // Nursery shell: shared floor/rails, one wall per state, state marks
        // =====================================================================

        static void BuildNurseryShell(Transform root, Transform logic, Transform shared)
        {
            var n = Group("Nursery", shared);
            Floor("NurseryFloor", n, new Vector3(0f, 0f, 8f), new Vector2(16f, 16f), "wood", 0.5f);
            var under = Prim(PrimitiveType.Sphere, "IslandUnderside", n, new Vector3(0f, -4.5f, 8f), new Vector3(17f, 9f, 17f), "ruin_dark", false);
            under.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box("Rug", n, new Vector3(0f, 0.012f, 6.5f), new Vector3(6.5f, 0.02f, 6f), "fabric_teal", false);
            Box("RugBorder", n, new Vector3(0f, 0.008f, 6.5f), new Vector3(7f, 0.01f, 6.5f), "fabric_gold", false);

            // Shared balustrade (the room's skeleton exists in all states).
            // Gaps: west/east side ledges and a south-west ledge (drifting-door anchors), east cupboard,
            // south entrance, three north gaps (music boxes behind the furniture).
            Wall("RailW_S", n, new Vector3(-8f, 0f, 0f), new Vector3(-8f, 0f, 9.1f), 1.1f, 0.25f, "wood_dark");
            Wall("RailW_N", n, new Vector3(-8f, 0f, 10.9f), new Vector3(-8f, 0f, 16f), 1.1f, 0.25f, "wood_dark");
            Wall("RailE_S", n, new Vector3(8f, 0f, 0f), new Vector3(8f, 0f, 2f), 1.1f, 0.25f, "wood_dark");
            Wall("RailE_M", n, new Vector3(8f, 0f, 4f), new Vector3(8f, 0f, 9.1f), 1.1f, 0.25f, "wood_dark");
            Wall("RailE_N", n, new Vector3(8f, 0f, 10.9f), new Vector3(8f, 0f, 16f), 1.1f, 0.25f, "wood_dark");
            Wall("RailSW_W", n, new Vector3(-8f, 0f, 0f), new Vector3(-5.9f, 0f, 0f), 1.1f, 0.25f, "wood_dark");
            Wall("RailSW_E", n, new Vector3(-4.1f, 0f, 0f), new Vector3(-1.2f, 0f, 0f), 1.1f, 0.25f, "wood_dark");
            Floor("LedgeWest", n, new Vector3(-8.95f, 0f, 10f), new Vector2(1.9f, 1.8f), "wood_dark", 0.4f);
            Floor("LedgeEast", n, new Vector3(8.95f, 0f, 10f), new Vector2(1.9f, 1.8f), "wood_dark", 0.4f);
            Floor("LedgeSouth", n, new Vector3(-5f, 0f, -0.85f), new Vector2(1.8f, 1.7f), "wood_dark", 0.4f);
            Wall("RailSE", n, new Vector3(1.2f, 0f, 0f), new Vector3(8f, 0f, 0f), 1.1f, 0.25f, "wood_dark");
            float[] seg = { -8f, -5.9f, -4.1f, -0.9f, 0.9f, 4.1f, 5.9f, 8f };
            for (int i = 0; i < 4; i++)
                Wall("RailN" + i, n, new Vector3(seg[i * 2], 0f, 16f), new Vector3(seg[i * 2 + 1], 0f, 16f), 1.1f, 0.25f, "wood_dark");
            // Corner posts + ceiling beams (shared frame).
            foreach (var c in new[] { new Vector3(-8f, 0f, 0f), new Vector3(8f, 0f, 0f), new Vector3(-8f, 0f, 16f), new Vector3(8f, 0f, 16f) })
                Box("CornerPost", n, c + Vector3.up * 2f, new Vector3(0.35f, 4f, 0.35f), "wood_dark");
            Box("BeamNS", n, new Vector3(0f, 4.1f, 8f), new Vector3(0.3f, 0.3f, 16.4f), "wood_dark", false);
            Box("BeamEW", n, new Vector3(0f, 4.1f, 8f), new Vector3(16.4f, 0.3f, 0.3f), "wood_dark", false);
            Box("BeamS", n, new Vector3(0f, 4.1f, 0f), new Vector3(16.4f, 0.3f, 0.3f), "wood_dark", false);
            Box("BeamN", n, new Vector3(0f, 4.1f, 16f), new Vector3(16.4f, 0.3f, 0.3f), "wood_dark", false);
            // Ledges beyond the three north gaps (the music boxes).
            foreach (float x in new[] { -5f, 0f, 5f })
                Floor("Ledge", n, new Vector3(x, 0f, 16.85f), new Vector2(1.8f, 1.7f), "wood_dark", 0.4f);
            PointLight("NurseryKey", n, new Vector3(0f, 3.6f, 7f), new Color(1f, 0.85f, 0.75f), 2.2f, 14f, true);
            ReflectionProbe(n, new Vector3(0f, 2f, 8f), new Vector3(17f, 5f, 18f));

            // ---- PAST wall (west): wood panelling, a window full of festival evening ----
            var pw = StateGroup("Past Wall", root, TimeMask.Past, true);
            Box("PanellingS", pw, new Vector3(-8.3f, 2f, 4.55f), new Vector3(0.2f, 4f, 9.1f), "wood");
            Box("PanellingN", pw, new Vector3(-8.3f, 2f, 13.45f), new Vector3(0.2f, 4f, 5.1f), "wood");
            Box("PanelLintel", pw, new Vector3(-8.3f, 3.5f, 10f), new Vector3(0.2f, 1f, 1.8f), "wood");
            Box("Wainscot", pw, new Vector3(-8.18f, 1.2f, 4.55f), new Vector3(0.06f, 0.08f, 9.1f), "brass", false);
            Box("Window", pw, new Vector3(-8.18f, 2.4f, 13.5f), new Vector3(0.05f, 1.4f, 1.8f), "window_warm", false);
            PointLight("WindowGlow", pw, new Vector3(-7.4f, 2.4f, 13.5f), Warm, 2.2f, 7f);
            var cot = Group("Cot", pw);
            Box("CotBase", cot, new Vector3(-6f, 0.45f, 3.2f), new Vector3(1.6f, 0.5f, 0.8f), "wood");
            for (int i = 0; i < 7; i++) Box("Spindle", cot, new Vector3(-6.7f + i * 0.23f, 0.85f, 2.82f), new Vector3(0.04f, 0.5f, 0.04f), "wood_dark", false);
            Box("CotRail", cot, new Vector3(-6f, 1.1f, 2.82f), new Vector3(1.6f, 0.06f, 0.06f), "wood_dark", false);
            Clue("CotCarving", pw, new Vector3(-6f, 1.15f, 2.8f), new Vector3(0.6f, 0.06f, 0.05f), "wood_dark", "Read the carving on the cot rail", "L06.notePast",
                "clue.L06.notePast", "Carved into the cot rail", "Small careful letters, cut with a pen-knife:\n\n'the SECOND note is 3'", true, TimeMask.Past);
            Box("RockingChair", pw, new Vector3(-6.4f, 0.5f, 12.6f), new Vector3(0.7f, 1.0f, 0.7f), "wood");
            // The children's cots (PAST): fourteen children were hidden here; cots 1-7 line the south wall.
            var cots = StateGroup("Past Cots", root, TimeMask.Past, false);
            for (int i = 0; i < 7; i++)
            {
                float x = 1.75f + i * 0.92f;
                Box("Cot" + (i + 1), cots, new Vector3(x, 0.35f, 1.05f), new Vector3(0.8f, 0.5f, 1.3f), "wood");
                Box("CotRail" + (i + 1), cots, new Vector3(x, 0.75f, 1.7f), new Vector3(0.8f, 0.3f, 0.04f), "wood_dark", false);
                Label("CotNo" + (i + 1), cots, new Vector3(x, 1.05f, 1.75f), 180f, (i + 1).ToString(), 0.25f, TextWarm);
            }
            var cot7 = StateGroup("Past Cot Seven", root, TimeMask.Past, true);
            Clue("CotSevenCarving", cot7, new Vector3(1.75f + 6 * 0.92f, 0.8f, 1.62f), new Vector3(0.6f, 0.25f, 0.06f), "wood_dark", "Look inside cot 7", "L06.cot7",
                "clue.L06.cot7", "Inside cot 7", "Carved inside the rail by a child's hand, the letters uneven:\n\nARIN\n\nUnderneath, smaller, in another child's hand: 'and MAREN, who had the lantern'.", true, TimeMask.Past);

            // ---- PRESENT wall (north): wet wallpaper between the gaps ----
            var nw = StateGroup("Present Wall", root, TimeMask.Present, true);
            for (int i = 0; i < 4; i++)
            {
                float x0 = seg[i * 2], x1 = seg[i * 2 + 1];
                Box("Wallpaper" + i, nw, new Vector3((x0 + x1) * 0.5f, 2f, 16.3f), new Vector3(x1 - x0, 4f, 0.1f), "plaster_cold");
                Box("DampStain" + i, nw, new Vector3((x0 + x1) * 0.5f + 0.3f, 0.6f, 16.24f), new Vector3((x1 - x0) * 0.6f, 0.9f, 0.02f), "mud", false);
            }
            Box("WallpaperLintel", nw, new Vector3(0f, 3.6f, 16.3f), new Vector3(16f, 0.8f, 0.1f), "plaster_cold");
            Clue("PencilNote", nw, new Vector3(-2.5f, 1.65f, 16.2f), new Vector3(0.45f, 0.3f, 0.03f), "paper", "Read the pencil marks on the wallpaper", "L06.notePresent",
                "clue.L06.notePresent", "Pencil on wet wallpaper", "Child-height, pressed hard so it would not wash away:\n\n'three is 5. dont forget'", true, TimeMask.Present);
            var drip = Particles("Drips", nw, new Vector3(0f, 3.9f, 15.6f), new Vector3(14f, 0.1f, 0.6f), "rain");
            var dem = drip.emission; dem.rateOverTime = 90f;
            Box("Puddle", nw, new Vector3(3.5f, 0.015f, 14.2f), new Vector3(3f, 0.01f, 1.4f), "water_clear", false);
            PointLight("WetLight", nw, new Vector3(0f, 3.2f, 14.5f), Cold, 1.6f, 7f);

            // ---- FUTURE wall (east): holographic lattice, neon seams ----
            var fw = StateGroup("Future Wall", root, TimeMask.Future, true);
            Box("LatticeS", fw, new Vector3(8.3f, 2f, 1f), new Vector3(0.08f, 4f, 2f), "hologram");
            Box("LatticeM", fw, new Vector3(8.3f, 2f, 6.55f), new Vector3(0.08f, 4f, 5.1f), "hologram");
            Box("LatticeN", fw, new Vector3(8.3f, 2f, 13.45f), new Vector3(0.08f, 4f, 5.1f), "hologram");
            for (int i = 0; i < 5; i++) Box("NeonRib" + i, fw, new Vector3(8.2f, 2f, 0.5f + i * 3.75f), new Vector3(0.05f, 4f, 0.05f), i % 2 == 0 ? "neon_cyan" : "neon_magenta", false);
            Box("NeonBase", fw, new Vector3(8.2f, 0.05f, 8f), new Vector3(0.05f, 0.05f, 16f), "neon_violet", false);
            var cap = Clue("HoloCaption", fw, new Vector3(8.02f, 1.75f, 12.6f), new Vector3(0.03f, 0.4f, 0.7f), "hologram", "Read the flickering caption", "L06.noteFuture",
                "clue.L06.noteFuture", "Flickering caption", "A caption that repeats on the lattice, as if someone stored it there on purpose:\n\nNOTE 4 = 4", true, TimeMask.Future);
            Label("CaptionText", fw, new Vector3(7.98f, 1.75f, 12.6f), 90f, "NOTE 4 = 4", 0.16f, Color.white);
            Crystals("CornerCrystals", fw, new Vector3(6.8f, 0f, 1.4f), 0.8f, 66, true);
            PointLight("LatticeGlow", fw, new Vector3(7f, 2.5f, 6f), Cyan, 1.8f, 8f);

            // Walls are named by word only; the shapes' marks are painted on the floor in other states.
            Label("WordPast", n, new Vector3(-7.8f, 3.2f, 6f), -90f, "<< PAST", 0.35f, TextWarm);
            Label("WordPresent", n, new Vector3(-2.5f, 3.3f, 15.8f), 0f, "[ PRESENT ]", 0.35f, TextCold);
            Label("WordFuture", n, new Vector3(7.8f, 3.2f, 6f), 90f, "FUTURE >>", 0.35f, TextHolo);
        }

        // =====================================================================
        // Beat: match hanging shapes to state marks
        // =====================================================================

        static void BuildMobile(Transform root, Transform logic, Transform shared)
        {
            var m = Group("Mobile Frame", shared);
            var ring = Prim(PrimitiveType.Cylinder, "MobileRing", m, new Vector3(0f, 3.75f, 8.6f), new Vector3(4f, 0.03f, 4f), "brass", false);
            Prim(PrimitiveType.Cylinder, "MobileHub", m, new Vector3(0f, 3.95f, 8.6f), new Vector3(0.3f, 0.2f, 0.3f), "brass", false);
            ring.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // index 0 = arrow north (+z), 1 = east, 2 = south, 3 = west.
            var shapes = new List<RotatableElement>();
            // Each shape's mark (a floor arrow) is painted in a DIFFERENT state, so in the shape's own state
            // the solution is visible only as an echo (echoes are always on in the nursery).
            shapes.Add(Shape(root, m, "Past", TimeMask.Past, new Vector3(-1.8f, 2.2f, 7.8f), "wood", "<<", 0, 2, "a carved wooden star", "Future", TimeMask.Future));
            shapes.Add(Shape(root, m, "Present", TimeMask.Present, new Vector3(0f, 2.2f, 10.4f), "iron_rust", "[ ]", 3, 1, "a dented tin moon", "Past", TimeMask.Past));
            shapes.Add(Shape(root, m, "Future", TimeMask.Future, new Vector3(1.8f, 2.2f, 7.8f), "hologram", ">>", 1, 3, "a cube of light", "Present", TimeMask.Present));

            var pz = new GameObject("Mobile Alignment").AddComponent<AlignmentPuzzle>();
            pz.transform.SetParent(logic, false);
            pz.elements = shapes;
            pz.solvedFlag = F_Shapes;
            pz.solvedToast = "The mobile hangs true. Below it, the music box clicks.";
        }

        static RotatableElement Shape(Transform root, Transform frame, string state, TimeMask mask, Vector3 c, string mat, string glyph, int start, int correct, string what, string markState, TimeMask markMask)
        {
            // The mark: a painted floor arrow beneath the shape, existing only in markState (echo visible elsewhere).
            var mk = StateGroup(state + " Shape Mark (" + markState + ")", root, markMask, true);
            var arrowRoot = Group("Arrow", mk);
            arrowRoot.position = new Vector3(c.x, 0.02f, c.z);
            Box("Shaft", arrowRoot, arrowRoot.position + new Vector3(0f, 0f, 0.25f), new Vector3(0.14f, 0.02f, 0.9f), "neon_amber", false);
            TiltBox("HeadL", arrowRoot, arrowRoot.position + new Vector3(-0.14f, 0f, 0.62f), new Vector3(0.12f, 0.02f, 0.45f), "neon_amber", new Vector3(0f, 35f, 0f));
            TiltBox("HeadR", arrowRoot, arrowRoot.position + new Vector3(0.14f, 0f, 0.62f), new Vector3(0.12f, 0.02f, 0.45f), "neon_amber", new Vector3(0f, -35f, 0f));
            Box("Ring", arrowRoot, arrowRoot.position, new Vector3(0.5f, 0.02f, 0.5f), "neon_violet", false);
            arrowRoot.rotation = Quaternion.Euler(0f, correct * 90f, 0f);
            glyph = "";
            // Rotating shapes: no echo (an echo would freeze the wrong orientation). A shared hook label says where it hangs.
            var g = StateGroup(state + " Shape", root, mask, false);
            var r = Rotatable(state + "Shape", g, c, new Vector3(0.55f, 0.55f, 0.55f), mat, 4, start, correct, glyph);
            r.prompt = "Turn " + what;
            r.usableIn = mask;
            Cylinder("String", g, c + Vector3.up * 0.95f, 0.01f, 1.2f, "wood_dark", false);
            if (mask == TimeMask.Future) PointLight("ShapeGlow", g, c, Cyan, 1.2f, 3f);
            Hologram(state + "Hook", frame, c + Vector3.up * 1.55f, new Vector3(0.12f, 0.12f, 0.12f), PrimitiveType.Sphere, mask == TimeMask.Future ? "hologram" : "hologram_amber", false, false);
            Label(state + "HookLabel", frame, c + Vector3.up * 1.32f, 0f, "hangs in the " + state.ToUpper() + " - its mark is painted in the " + markState.ToUpper(), 0.11f, Color.white);
            return r;
        }

        // =====================================================================
        // Beat: the lullaby (music box with missing notes, toy-block notes)
        // =====================================================================

        static void BuildLullaby(Transform root, Transform logic, Transform shared)
        {
            var g = Group("Lullaby", shared);
            Vector3 box = new Vector3(0f, 0f, 6.2f);

            // The lullaby: four notes, 1 3 5 4. The toy blocks are an instrument to test notes; the music
            // boxes stand on the north ledges BEHIND the furniture, one per state, each missing a different
            // note. Wind a box in its state, then HUM the whole song beside it to mend it. If the drifting
            // door slips away while you work, the room forgets every mended box (L06RoomForgets).
            var tune = new GameObject("Lullaby").AddComponent<SequencePuzzle>();
            tune.transform.SetParent(logic, false);
            tune.transform.position = box + Vector3.up * 1.1f;
            tune.solution = new List<int> { 0, 2, 4, 3 };
            tune.demoNoteGap = 0.7f;
            tune.solvedFlag = "L06.blocksPlayed";

            string[] state = { "Past", "Present", "Future" };
            TimeMask[] masks = { TimeMask.Past, TimeMask.Present, TimeMask.Future };
            string[] mats = { "brass", "iron_rust", "hologram" };
            float[] xs = { -5f, 0f, 5f };
            for (int i = 0; i < 3; i++)
            {
                Vector3 bp = new Vector3(xs[i], 0f, 17.1f);
                var sg = StateGroup(state[i] + " Music Box", root, masks[i], true);
                Box("Stool", sg, bp + Vector3.up * 0.4f, new Vector3(0.5f, 0.8f, 0.5f), "wood_dark");
                var mbGo = Prim(PrimitiveType.Cube, "MusicBox", sg, bp + Vector3.up * 0.97f, new Vector3(0.5f, 0.32f, 0.4f), mats[i], true, false);
                var mb = mbGo.AddComponent<MelodyListener>();
                mb.prompt = "Wind the music box (" + state[i].ToUpper() + ")";
                mb.usableIn = masks[i];
                mb.puzzle = tune;
                mb.missingNotes = new List<int> { i + 1 };
                mb.heardFlag = "L06.heard" + state[i];
                mb.requiredFlags.Add(F_Shapes);
                mb.lockedMessage = "The music box is wound, but it will not play while the mobile hangs out of true.";
                Box("Lid", sg, bp + new Vector3(0f, 1.25f, 0.18f), new Vector3(0.5f, 0.3f, 0.03f), i == 2 ? "neon_cyan" : "wood", false);
                Label("Tag", sg, bp + new Vector3(0f, 1.65f, 0f), 0f, "MUSIC BOX  " + (i == 0 ? "<<" : i == 1 ? "[ ]" : ">>"), 0.14f, i == 0 ? TextWarm : i == 1 ? TextCold : TextHolo);
                var hr = mbGo.AddComponent<HumReceiver>();
                hr.radius = 2.4f;
                hr.activeIn = masks[i];
                hr.oneShot = false;
                hr.requiredFlags.Add("L06.heard" + state[i]);
                hr.setsFlag = "L06.mended" + state[i];
                hr.lines.Add(L("Arin", "(The " + state[i].ToUpper() + " box catches the hum - its missing note rings true.)", 3f));
            }
            var mended = Reactor("Lullaby Mended", logic, "L06.mendedPast", "L06.mendedPresent", "L06.mendedFuture");
            mended.setFlagWhenTrue = F_Lullaby;
            mended.reversible = false;
            Box("SheetStand", g, new Vector3(1.4f, 0.45f, 6.2f), new Vector3(0.3f, 0.9f, 0.3f), "wood_dark");
            Clue("LullabySheet", g, new Vector3(1.4f, 0.95f, 6.2f), new Vector3(0.45f, 0.04f, 0.36f), "paper", "Read the lullaby sheet", "L06.sheetRead",
                "clue.L06.sheet", "The nursery lullaby",
                "Four notes, written large for small eyes:\n\n   1   _   _   _\n\nUnderneath: 'Each room that made this one forgets a different note. The boxes wait behind the furniture, one in each state. Wind each one where it lives - then sing it the whole song.'\n\n(Hum: H / HUM. The toy blocks let you try notes.)");
            Label("SheetGlyph", g, new Vector3(1.4f, 1.5f, 6.2f), 0f, "1 _ _ _", 0.2f, Color.white);

            // Toy blocks 1-6 (notes 0-5); two are distractors.
            string[] bmats = { "fabric_red", "fabric_gold", "fabric_teal", "wood", "coat_white", "cup_blue" };
            string[] shapes = { "o", "#", "[ ]", "<<", ">>", "0" };
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3(-3.75f + i * 1.5f, 0.25f, 3.9f + Mathf.Abs(i - 2.5f) * 0.25f);
                var b = Prim(PrimitiveType.Cube, "ToyBlock" + (i + 1), g, p, new Vector3(0.5f, 0.5f, 0.5f), bmats[i], true, false);
                var sb = b.AddComponent<SequenceButton>();
                sb.puzzle = tune;
                sb.noteIndex = i;
                sb.pressVisual = b.transform;
                sb.prompt = "Strike block " + (i + 1);
                Label("Num", g, p + new Vector3(0f, 0.05f, -0.27f), 0f, (i + 1).ToString(), 0.5f, Color.white);
                Label("Top", g, p + new Vector3(0f, 0.45f, 0f), 0f, (i + 1) + " " + shapes[i], 0.16f, Color.white);
            }

            var done = Reactor("Lullaby Whole", logic, F_Lullaby);
            done.announce = true;
            done.cue = Cue.Revelation;
            done.toast = "The lullaby is whole - the room will not forget it now. Reach the drifting door.";
            done.reversible = false;

            // L05 choice echoes: Maren's music box (keepsake) or a page of the families' register (records).
            var keep = Group("Marens Music Box (keepsake)", g);
            Box("Shelf", keep, new Vector3(-1.5f, 0.45f, 6.2f), new Vector3(0.5f, 0.9f, 0.4f), "wood");
            Box("MarensBox", keep, new Vector3(-1.5f, 1.02f, 6.2f), new Vector3(0.42f, 0.24f, 0.32f), "brass");
            Box("MarensLid", keep, new Vector3(-1.5f, 1.16f, 6.35f), new Vector3(0.42f, 0.22f, 0.03f), "wood", false);
            Label("MarensTag", keep, new Vector3(-1.5f, 1.6f, 6.2f), 0f, "MAREN'S MUSIC BOX", 0.14f, TextWarm);
            PointLight("MarensGlow", keep, new Vector3(-1.5f, 1.5f, 5.8f), Warm, 1.2f, 3f);
            var km = keep.gameObject.AddComponent<L06KeepsakeMelody>();
            km.puzzle = tune;
            km.whenAll = new List<string> { "choice.L05.keepsake", F_Lullaby };
            km.playedFlag = "L06.keepsakePlayed";
            km.toast = "Maren's music box plays the whole song. No note missing - for the first time.";
            keep.gameObject.SetActive(false);
            var rk = Reactor("Keepsake Present", logic, "choice.L05.keepsake");
            rk.enableWhenTrue.Add(keep.gameObject);

            var page = Group("Register Page (records)", g);
            Box("Page", page, new Vector3(-6.8f, 1.6f, 2.8f), new Vector3(0.5f, 0.65f, 0.02f), "paper", false);
            Box("Easel", page, new Vector3(-6.8f, 0.8f, 2.84f), new Vector3(0.06f, 1.6f, 0.06f), "wood_dark", false);
            Label("PageText", page, new Vector3(-6.8f, 1.6f, 2.78f), 0f, "FAMILIES' REGISTER\nOSKEL, MAREN - 7\nstill counted", 0.11f, new Color(0.3f, 0.2f, 0.15f));
            page.gameObject.SetActive(false);
            var rr = Reactor("Register Page Present", logic, "choice.L05.records");
            rr.enableWhenTrue.Add(page.gameObject);
        }

        // =====================================================================
        // Beat: keep the exit visible while moving furniture
        // =====================================================================

        static void BuildDriftingDoor(Transform root, Transform logic, Transform shared)
        {
            var furn = Group("Furniture (pushable)", shared);
            var cradle = Pushable("Cradle", furn, new Vector3(-5f, 0.5f, 14.85f), new Vector3(2.0f, 1.0f, 0.9f), "wood", 55f);
            var wardrobe = Pushable("Wardrobe", furn, new Vector3(0f, 1.1f, 14.9f), new Vector3(2.0f, 2.2f, 0.8f), "wood_dark", 80f);
            var chest = Pushable("ToyChest", furn, new Vector3(5f, 0.4f, 14.9f), new Vector3(1.9f, 0.8f, 0.8f), "fabric_red", 50f);
            Label("FurnitureHint", shared, new Vector3(0f, 2.45f, 14.45f), 0f, "push from the side", 0.14f, Color.white);

            var holder = Group("Drifting Door Holder", shared);
            // The door is in the room from the start and drifts between the side and south ledges whenever
            // nobody watches it. Its front (local -z) faces into the room at every anchor.
            var anchors = new List<Transform>
            {
                Marker("DoorAnchor_West", logic, new Vector3(-9.35f, 0f, 10f), -90f),
                Marker("DoorAnchor_East", logic, new Vector3(9.35f, 0f, 10f), 90f),
                Marker("DoorAnchor_South", logic, new Vector3(-5f, 0f, -1.15f), 180f),
            };
            var door = Group("Drifting Door", holder);
            door.position = anchors[0].position;
            Vector3 p = door.position;   // built facing north (identity), rotated onto its anchor below
            Box("PostL", door, p + new Vector3(-0.85f, 1.4f, 0f), new Vector3(0.18f, 2.8f, 0.25f), "wood_dark");
            Box("PostR", door, p + new Vector3(0.85f, 1.4f, 0f), new Vector3(0.18f, 2.8f, 0.25f), "wood_dark");
            Box("Lintel", door, p + new Vector3(0f, 2.9f, 0f), new Vector3(1.9f, 0.2f, 0.25f), "wood_dark");
            var mover = Group("PanelMover", door);
            mover.position = p;
            var panel = Box("Panel", mover, p + new Vector3(0f, 1.3f, 0f), new Vector3(1.5f, 2.6f, 0.1f), "plaster_warm");
            Label("DoorZero", mover, p + new Vector3(0f, 1.6f, -0.07f), 0f, "0", 0.6f, new Color(0.35f, 0.25f, 0.3f));
            Hologram("DoorAura", door, p + new Vector3(0f, 1.4f, 0.1f), new Vector3(1.7f, 2.8f, 0.02f), PrimitiveType.Cube, "hologram_magenta", false, false);
            PointLight("DoorLight", door, p + new Vector3(0f, 2.2f, -0.8f), Magenta, 2.2f, 6f);
            Label("DoorTag", door, p + new Vector3(0f, 3.3f, -0.1f), 0f, "THE WAY OUT  -  KEEP IT IN VIEW", 0.15f, TextHolo);
            NotStatic(door.gameObject);

            var ox = door.gameObject.AddComponent<ObservedExit>();
            ox.anchors = anchors;
            ox.graceTime = 3f;
            ox.anchoredFlag = F_Anchored;
            ox.doorRenderer = panel.GetComponent<Renderer>();

            var touch = TriggerBox("ReachDoor", door, p + new Vector3(0f, 1f, -0.55f), new Vector3(1.5f, 2f, 0.9f)).gameObject.AddComponent<TriggerFlag>();
            touch.setFlag = F_Anchored;
            touch.requiredFlags.Add(F_Lullaby);
            touch.toast = "The door holds still under your hand.";
            door.rotation = anchors[0].rotation;

            var forget = logic.gameObject.AddComponent<L06RoomForgets>();
            forget.door = door;
            forget.forgetFlags = new List<string> { "L06.mendedPast", "L06.mendedPresent", "L06.mendedFuture" };
            forget.lockFlag = F_Lullaby;

            var open = Reactor("Door Opens", logic, F_Anchored);
            open.moveTarget = mover;
            open.moveBy = new Vector3(0f, 2.7f, 0f);
            open.duration = 1.6f;
            open.announce = true;
            open.reversible = false;
            open.toast = "The door opens onto the seam. A bridge of light unfolds toward a waiting figure.";

            // Seam bridges from every door ledge to the Witness platform (appear once the door is anchored).
            var bridges = Group("Seam Bridges", shared);
            void Seg(Vector3 from, Vector3 to)
            {
                var b = Ramp("SeamBridge", bridges, from, to, 1.6f, "hologram_magenta", 0.2f);
                b.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Seg(new Vector3(-9.8f, 0f, 10f), new Vector3(-11f, 0f, 11f));
            Seg(new Vector3(-11f, 0f, 10.2f), new Vector3(-11f, 0f, 20.5f));
            Seg(new Vector3(-11.6f, 0f, 20f), new Vector3(-4.4f, 0f, 23f));
            Seg(new Vector3(9.8f, 0f, 10f), new Vector3(11f, 0f, 11f));
            Seg(new Vector3(11f, 0f, 10.2f), new Vector3(11f, 0f, 20.5f));
            Seg(new Vector3(11.6f, 0f, 20f), new Vector3(4.4f, 0f, 23f));
            Seg(new Vector3(-5f, 0f, -1.6f), new Vector3(-11.6f, 0f, -1.6f));
            Seg(new Vector3(-11f, 0f, -2.2f), new Vector3(-11f, 0f, 10.8f));
            PointLight("BridgeGlow", bridges, new Vector3(-11f, 1f, 16f), Magenta, 2f, 9f);
            bridges.gameObject.SetActive(false);
            open.enableWhenTrue.Add(bridges.gameObject);
        }

        // =====================================================================
        // Beat: hear The Witness; take the carved zero
        // =====================================================================

        static NPCMotor witness;

        static void BuildWitness(Transform root, Transform logic, Transform shared)
        {
            var w = Group("Witness Platform", shared);
            Floor("Platform", w, new Vector3(0f, 0f, 26f), new Vector2(10f, 8f), "obsidian", 0.6f);
            var under = Prim(PrimitiveType.Sphere, "PlatformUnderside", w, new Vector3(0f, -2.4f, 26f), new Vector3(10f, 4.5f, 8f), "ruin_dark", false);
            under.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box("EdgeW", w, new Vector3(-5f, 0.03f, 26f), new Vector3(0.06f, 0.06f, 8f), "neon_cyan", false);
            Box("EdgeE", w, new Vector3(5f, 0.03f, 26f), new Vector3(0.06f, 0.06f, 8f), "neon_cyan", false);
            Box("EdgeN", w, new Vector3(0f, 0.03f, 30f), new Vector3(10f, 0.06f, 0.06f), "neon_magenta", false);
            Box("EdgeS", w, new Vector3(0f, 0.03f, 22f), new Vector3(10f, 0.06f, 0.06f), "neon_magenta", false);
            Wall("RailW", w, new Vector3(-5f, 0f, 24.6f), new Vector3(-5f, 0f, 30f), 1.0f, 0.15f, "obsidian");
            Wall("RailE", w, new Vector3(5f, 0f, 24.6f), new Vector3(5f, 0f, 30f), 1.0f, 0.15f, "obsidian");
            var ring = Hologram("WitnessRing", w, new Vector3(0f, 0.05f, 27f), new Vector3(4.5f, 0.01f, 4.5f), PrimitiveType.Cylinder, "hologram_magenta", false, true);
            PointLight("PlatformGlow", w, new Vector3(0f, 2.5f, 26.5f), Magenta, 2.6f, 10f);
            PointLight("PlatformFill", w, new Vector3(0f, 1.2f, 23f), Cyan, 1.4f, 7f);

            // The Witness: a named hologram figure that exists in every state.
            witness = Npc("The Witness", w, new Vector3(0f, 0f, 27.2f), 180f, "coat_white", true, 99);
            MakeHolo(witness, "hologram_magenta");

            // The apparition shown during the memory.
            var big = Group("Witness Apparition", w);
            var body = Hologram("ApparitionBody", big, new Vector3(0f, 2.4f, 29.2f), new Vector3(1.4f, 2.0f, 0.9f), PrimitiveType.Capsule, "hologram_magenta", true, false);
            Hologram("ApparitionHead", big, new Vector3(0f, 4.8f, 29.2f), new Vector3(0.8f, 0.8f, 0.8f), PrimitiveType.Sphere, "hologram_magenta", true, false);
            Hologram("ApparitionHalo", big, new Vector3(0f, 5.6f, 29.2f), new Vector3(1.8f, 0.02f, 1.8f), PrimitiveType.Cylinder, "hologram", true, true);
            Label("ApparitionSeal", big, new Vector3(0f, 3.4f, 28.6f), 180f, "(0)", 0.8f, Color.white);
            PointLight("ApparitionLight", big, new Vector3(0f, 3f, 27.5f), Magenta, 4f, 12f);

            var approach = Zone("Witness Approach", logic, new Vector3(0f, 1f, 23.6f), new Vector3(10f, 2f, 2.4f), F_Approach);
            approach.requiredFlags.Add(F_Anchored);
            var ms = new GameObject("The Witness (memory)").AddComponent<MemorySequence>();
            ms.transform.SetParent(logic, false);
            ms.triggerFlag = F_Approach;
            ms.completeFlag = F_Witness;
            ms.showDuring = big.gameObject;
            ms.lines.Add(L("The Witness", "You found it. The room with no address.", 4f));
            ms.lines.Add(L("The Witness", "Every family in the basin marked tomorrow. They were not afraid of the bell. They were waiting for it.", 5f));
            ms.lines.Add(L("The Witness", "The Founders saw a larger collapse coming - every state falling into one. So they hid the children between the states. Fourteen of us. Here.", 6f));
            ms.lines.Add(L("The Witness", "I was small when they hid us here. I had a lantern. You hummed in the dark so the little ones would sleep.", 5.5f));
            ms.lines.Add(L("Arin", "...Maren? The girl in the laundry.", 3.5f));
            ms.lines.Add(L("The Witness", "Maren Oskel. Grown, in a place where nothing grows. The rescue failed, Arin. The door closed with me inside.", 6f));
            ms.lines.Add(L("The Witness", "I stayed so someone would be here when you came back.", 4.5f));
            ms.lines.Add(L("The Witness", "Your name is in cot seven. You carved it the first night. Take the zero - it was Ilse Marrow's. She left it when she went to stop the tower.", 7f));
            ms.lines.Add(L("The Witness", "Carry it back before the final bell.", 3.5f));

            var isWitness = Reactor("Maren Is The Witness", logic, F_Witness);
            isWitness.setFlagWhenTrue = "maren.isWitness";
            isWitness.reversible = false;
            var whum = witness.gameObject.AddComponent<HumReceiver>();
            whum.radius = 6f;
            whum.setsFlag = "L06.witnessHummed";
            whum.requiredFlags.Add(F_Witness);
            whum.lines.Add(L("The Witness", "You still hum it. All four notes. You always had the whole song - that's why they gave you cot seven, by the door.", 5f));

            // Carved zero on a black plinth (appears after the Witness speaks).
            Box("ZeroPlinth", w, new Vector3(0f, 0.5f, 28.6f), new Vector3(0.6f, 1.0f, 0.6f), "obsidian");
            Box("PlinthSeam", w, new Vector3(0f, 1.01f, 28.6f), new Vector3(0.64f, 0.02f, 0.64f), "neon_cyan", false);
            var zeroHolder = Group("Carved Zero Holder", w);
            var zero = new GameObject("CarvedZero");
            zero.transform.SetParent(zeroHolder, false);
            zero.transform.position = new Vector3(0f, 1.27f, 28.6f);
            var col = zero.AddComponent<BoxCollider>();
            col.size = new Vector3(0.36f, 0.5f, 0.12f);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                var seg = Prim(PrimitiveType.Cube, "Carve", zero.transform, zero.transform.position + new Vector3(Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.2f, 0f), new Vector3(0.07f, 0.09f, 0.07f), "wood", false, false);
                seg.transform.rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            var glow = PointLight("ZeroGlow", zero.transform, zero.transform.position + new Vector3(0f, 0f, -0.2f), new Color(0.9f, 0.95f, 1f), 1.4f, 3f);
            var rb = zero.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var gz = zero.AddComponent<Grabbable>();
            gz.key = K_Zero;
            gz.displayName = "Ilse's carved zero";
            gz.prompt = "Take Ilse's carved zero";
            gz.carryAcrossStates = true;
            gz.resetOnLoop = false;
            gz.pickedUpFlag = F_Zero;
            Hologram("ZeroBeacon", zeroHolder, new Vector3(0f, 1.9f, 28.6f), new Vector3(0.25f, 0.25f, 0.25f), PrimitiveType.Sphere, "hologram", true, true);
            Label("ZeroTag", zeroHolder, new Vector3(0f, 2.25f, 28.6f), 180f, "0", 0.4f, Color.white);
            Label("ZeroTagS", zeroHolder, new Vector3(0f, 2.25f, 28.6f), 0f, "0", 0.4f, Color.white);
            zeroHolder.gameObject.SetActive(false);
            var reveal = Reactor("Zero Revealed", logic, F_Witness);
            reveal.enableWhenTrue.Add(zeroHolder.gameObject);
            reveal.announce = true;
            reveal.cue = Cue.Revelation;
            reveal.reversible = false;
        }

        // =====================================================================
        // Exit: back toward the tower before the final bell
        // =====================================================================

        static void BuildExit(Transform root, Transform logic, Transform shared)
        {
            var e = Group("Tower Approach", shared);
            var bridge = Group("Exit Bridge", e);
            Ramp("ExitBridge", bridge, new Vector3(0f, 0f, 29.9f), new Vector3(0f, 0f, 36.1f), 2f, "hologram", 0.2f);
            Box("ExitEdgeL", bridge, new Vector3(-1.02f, 0.03f, 33f), new Vector3(0.05f, 0.05f, 6.2f), "neon_cyan", false);
            Box("ExitEdgeR", bridge, new Vector3(1.02f, 0.03f, 33f), new Vector3(0.05f, 0.05f, 6.2f), "neon_cyan", false);
            PointLight("ExitBridgeGlow", bridge, new Vector3(0f, 0.8f, 33f), Cyan, 2f, 8f);
            bridge.gameObject.SetActive(false);

            // Blocks the north edge of the Witness platform until the bridge exists.
            var edge = Box("NorthRail", e, new Vector3(0f, 0.5f, 29.95f), new Vector3(10f, 1.0f, 0.15f), "obsidian");
            var r = Reactor("Exit Bridge Unfolds", logic, F_Zero);
            r.enableWhenTrue.Add(bridge.gameObject);
            r.disableWhenTrue.Add(edge);
            r.announce = true;
            r.cue = Cue.Bell;
            r.toast = "Far off, the tower bell gathers itself for its final ring. Go back - now.";
            r.reversible = false;

            Floor("TowerLanding", e, new Vector3(0f, 0f, 39f), new Vector2(8f, 6f), "stone_warm", 1f);
            Wall("LandingRailW", e, new Vector3(-4f, 0f, 36f), new Vector3(-4f, 0f, 42f), 1.1f, 0.25f, "wood_dark");
            Wall("LandingRailE", e, new Vector3(4f, 0f, 36f), new Vector3(4f, 0f, 42f), 1.1f, 0.25f, "wood_dark");
            Box("ArchL", e, new Vector3(-1.3f, 1.6f, 40.2f), new Vector3(0.4f, 3.2f, 0.4f), "brass");
            Box("ArchR", e, new Vector3(1.3f, 1.6f, 40.2f), new Vector3(0.4f, 3.2f, 0.4f), "brass");
            Box("ArchTop", e, new Vector3(0f, 3.35f, 40.2f), new Vector3(3f, 0.3f, 0.4f), "brass");
            Label("ArchText", e, new Vector3(0f, 3.85f, 40.0f), 0f, "TO THE TOWER", 0.3f, TextWarm);
            Gate("Tower Gate", logic, e, new Vector3(0f, 0f, 40.2f), new Vector3(2.2f, 3.0f, 0.2f), "metal_dark", new Vector3(0f, 3.2f, 0f), "", F_Zero);
            Exit("TowerExit", logic, new Vector3(0f, 0f, 41.2f), new Vector3(2.2f, 2.6f, 1.2f),
                "The way back is sealed. The Witness said to take the carved zero.", F_Witness, F_Zero);

            // The clock tower across the void - visible from the moment Arin arrives.
            var t = Group("Distant Clock Tower", shared);
            Box("Shaft", t, new Vector3(0f, 10f, 120f), new Vector3(12f, 70f, 12f), "stone_warm");
            Box("Belfry", t, new Vector3(0f, 47f, 120f), new Vector3(15f, 8f, 15f), "brass");
            Box("Spire", t, new Vector3(0f, 55f, 120f), new Vector3(5f, 8f, 5f), "wood_dark");
            var face = Prim(PrimitiveType.Cylinder, "ClockFace", t, new Vector3(0f, 38f, 113.8f), new Vector3(9f, 0.1f, 9f), "zero_glow", false);
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("TowerSeal", t, new Vector3(0f, 38f, 113.6f), 0f, "(0)", 4f, new Color(0.25f, 0.3f, 0.4f));
            PointLight("TowerLight", t, new Vector3(0f, 38f, 108f), new Color(0.9f, 0.95f, 1f), 4f, 24f);
            var bell = Prim(PrimitiveType.Sphere, "Bell", t, new Vector3(0f, 46f, 120f), new Vector3(5f, 4f, 5f), "brass", false);
            bell.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // =====================================================================
        // Optional side area: the cupboard of lost things (objects from every state)
        // =====================================================================

        static void BuildCupboard(Transform root, Transform logic, Transform shared)
        {
            var c = Group("Cupboard of Lost Things (optional)", shared);
            Floor("CupboardFloor", c, new Vector3(9.6f, 0f, 3f), new Vector2(3.2f, 4f), "wood_dark", 0.5f);
            Wall("CupS", c, new Vector3(8f, 0f, 1f), new Vector3(11.2f, 0f, 1f), 2.7f, 0.2f, "wood_dark");
            Wall("CupE", c, new Vector3(11.2f, 0f, 1f), new Vector3(11.2f, 0f, 5f), 2.7f, 0.2f, "wood_dark");
            Wall("CupN", c, new Vector3(11.2f, 0f, 5f), new Vector3(8f, 0f, 5f), 2.7f, 0.2f, "wood_dark");
            Box("CupCeiling", c, new Vector3(9.6f, 2.8f, 3f), new Vector3(3.4f, 0.2f, 4.2f), "wood_dark");
            Label("CupSign", c, new Vector3(7.85f, 2.5f, 3f), 90f, "CUPBOARD OF\nLOST THINGS", 0.2f, TextWarm);
            for (int i = 0; i < 3; i++) Box("Shelf" + i, c, new Vector3(10.85f, 0.6f + i * 0.65f, 3f), new Vector3(0.5f, 0.05f, 3.6f), "wood");
            PointLight("CupLamp", c, new Vector3(9.6f, 2.4f, 3f), new Color(1f, 0.85f, 0.6f), 1.6f, 5f);

            var past = StateGroup("Past Lost Things", root, TimeMask.Past, true);
            Sphere("ToyBell", past, new Vector3(10.85f, 0.75f, 2f), 0.12f, "brass", false);
            Box("FestivalRibbon", past, new Vector3(10.85f, 0.66f, 3f), new Vector3(0.3f, 0.02f, 0.6f), "fabric_red", false);
            Box("ChildShoe", past, new Vector3(10.85f, 0.7f, 4f), new Vector3(0.12f, 0.1f, 0.22f), "wood_dark", false);
            var pres = StateGroup("Present Lost Things", root, TimeMask.Present, true);
            Box("TramTicket", pres, new Vector3(10.85f, 1.27f, 2.2f), new Vector3(0.2f, 0.01f, 0.12f), "paper", false);
            Box("WetSatchel", pres, new Vector3(10.85f, 1.4f, 3.3f), new Vector3(0.3f, 0.25f, 0.45f), "coat_ochre", false);
            Box("LanternGlass", pres, new Vector3(10.85f, 1.38f, 4.2f), new Vector3(0.15f, 0.22f, 0.15f), "glass", false);
            var fut = StateGroup("Future Lost Things", root, TimeMask.Future, true);
            Hologram("ToyOfLight", fut, new Vector3(10.85f, 2.05f, 2.4f), new Vector3(0.2f, 0.2f, 0.2f), PrimitiveType.Cube, "hologram", true, true);
            Box("CrystalMarble", fut, new Vector3(10.85f, 1.98f, 3.6f), new Vector3(0.1f, 0.1f, 0.1f), "mineral_trace", false);

            Clue("LostTags", c, new Vector3(8.6f, 1.0f, 4.7f), new Vector3(0.4f, 0.3f, 0.03f), "paper", "Read the lost-property tags", "L06.lostThings",
                "clue.L06.lost", "Lost-property tags",
                "Tags on string, all in the same tidy hand:\n\n- one festival ribbon, one toy bell (PAST)\n- one tram ticket, Line 4, one wet satchel (PRESENT)\n- one toy made of light (FUTURE)\n- one blue cup - CLAIMED\n\nThe last tag is crossed out and rewritten: 'one carved zero - lent, not lost. I. M.'");
        }

        static void BuildFragments(Transform root, Transform shared)
        {
            var g = Group("Memory Fragments", shared);
            Fragment(g, new Vector3(10.6f, 2.35f, 4.4f), "L06.1", "A tram ticket",
                "Line 4, Basin to Tower, single. Punched nine times by a conductor who wrote on the back: 'no charge - children'. Signed I. Marrow.");

            var past = StateGroup("Past Side Step", root, TimeMask.Past, true);
            Floor("SideStep", past, new Vector3(-2.5f, 0f, -11.25f), new Vector2(1.8f, 1.8f), "wood", 0.25f);
            Fragment(past, new Vector3(-2.5f, 1.2f, -11.25f), "L06.2", "The nursemaid's rota",
                "Oriel, nights. Expected: forty-one children, Bell Day eve. Bring every blanket in the ward. If the bell rings twice, keep singing - whatever happens, keep singing.");

            var fut = StateGroup("Future Side Step", root, TimeMask.Future, true);
            Floor("SideStep", fut, new Vector3(2.5f, 0f, -5.25f), new Vector2(1.8f, 1.8f), "hologram", 0.12f);
            Fragment(fut, new Vector3(2.5f, 1.2f, -5.25f), "L06.3", "Seam log",
                "POCKET STABLE. CYCLE 0 ... 1 ... 112 ... 113. OCCUPANT COUNT: 1 ADULT. NAME WITHHELD AT OCCUPANT'S REQUEST. NOTE: OCCUPANT WAS ADMITTED AS A CHILD.");
        }

        // =====================================================================
        // The void: fragments of all three states floating past
        // =====================================================================

        static void BuildVoid(Transform root, Transform shared)
        {
            var v = Group("Void", shared);
            Particles("VoidMotes", v, new Vector3(0f, 3f, 8f), new Vector3(60f, 16f, 70f), "motes");
            Particles("VoidMistBelow", v, new Vector3(0f, -8f, 8f), new Vector3(90f, 4f, 90f), "mist");
            Particles("VoidMistAround", v, new Vector3(0f, 1f, 8f), new Vector3(50f, 3f, 60f), "mist");

            var pastBits = StateGroup("Past Fragments", root, TimeMask.Past, false);
            var presBits = StateGroup("Present Fragments", root, TimeMask.Present, false);
            var futBits = StateGroup("Future Fragments", root, TimeMask.Future, false);
            var rng = new System.Random(606);
            for (int i = 0; i < 36; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 16f + (float)rng.NextDouble() * 30f;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, -6f + (float)rng.NextDouble() * 18f, 8f + Mathf.Sin(a) * r);
                int kind = i % 3;
                if (kind == 0)
                {
                    Drift("Beam" + i, pastBits, p, new Vector3(0.3f, 0.3f, 2f + (float)rng.NextDouble() * 3f), "wood", i);
                    if (i % 6 == 0) Drift("Bunting" + i, pastBits, p + Vector3.up, new Vector3(0.6f, 0.5f, 0.02f), "fabric_red", i + 1);
                }
                else if (kind == 1)
                {
                    Drift("Slab" + i, presBits, p, new Vector3(1.5f + (float)rng.NextDouble() * 2f, 0.25f, 1f + (float)rng.NextDouble() * 2f), "stone_wet", i);
                    if (i % 4 == 1) Drift("Puddle" + i, presBits, p + Vector3.up * 0.14f, new Vector3(1.2f, 0.02f, 0.9f), "water", i);
                }
                else
                {
                    var s = Drift("Shard" + i, futBits, p, new Vector3(0.4f, 1.5f + (float)rng.NextDouble() * 2f, 0.4f), i % 2 == 0 ? "crystal" : "obsidian", i);
                    if (i % 5 == 2) Drift("NeonShard" + i, futBits, p + Vector3.right * 0.6f, new Vector3(0.05f, 1.2f, 0.05f), "neon_magenta", i + 2);
                }
            }
            // A few big fragments with lights (reads as other rooms adrift).
            Lamp("DriftingGasLamp", pastBits, new Vector3(-14f, -1f, 2f), "gas", 3f);
            Lamp("DriftingSodiumLamp", presBits, new Vector3(13f, -1.5f, 12f), "sodium", 3.4f);
            Lamp("DriftingNeonLamp", futBits, new Vector3(-12f, -0.5f, 18f), "neon", 3.2f);
            var holo = Hologram("FloatingSign", futBits, new Vector3(11f, 5f, 2f), new Vector3(4f, 1f, 0.02f), PrimitiveType.Cube, "hologram", true, false);
            holo.transform.rotation = Quaternion.Euler(0f, -50f, 0f);
            Label("FloatingSignText", futBits, new Vector3(10.95f, 5f, 1.95f), -50f, "ADDRESS NOT FOUND", 0.3f, Color.white).transform.rotation = Quaternion.Euler(0f, -50f, 0f);
            Particles("PastDust", pastBits, new Vector3(0f, 2f, 8f), new Vector3(18f, 4f, 18f), "dust");
            var rain = Particles("PresentRain", presBits, new Vector3(0f, 14f, 8f), new Vector3(40f, 1f, 50f), "rain");
            var rem = rain.emission; rem.rateOverTime = 900f;
            PointLight("VoidUnderglowPast", pastBits, new Vector3(0f, -4f, 8f), Warm, 3f, 18f);
            PointLight("VoidUnderglowPresent", presBits, new Vector3(0f, -4f, 8f), Cold, 3f, 18f);
            PointLight("VoidUnderglowFuture", futBits, new Vector3(0f, -4f, 8f), Violet, 3.5f, 20f);
        }

        // =====================================================================
        // Named NPCs (echo residents) + background echoes
        // =====================================================================

        static void BuildNpcs(Transform root, Transform logic)
        {
            var pastPeople = StateGroup("Past Nursery Echoes", root, TimeMask.Past, false);
            var presPeople = StateGroup("Present Nursery Echoes", root, TimeMask.Present, false);

            // ---- Nanny Oriel (PAST): the nursemaid ----
            var oriel = Npc("Nanny Oriel", pastPeople, new Vector3(-5f, 0f, 5f), 90f, "coat_white", true, 61, "fabric_gold");
            Route(oriel, false,
                (new Vector3(-5.3f, 0f, 4.3f), 5f, NPCActivity.Work, "Hush now. Hush."),
                (new Vector3(-6.2f, 0f, 8.8f), 4f, NPCActivity.Carry, "Blankets for the little ones."),
                (new Vector3(-3.8f, 0f, 12.2f), 4f, NPCActivity.Look, "The door was here a moment ago..."));
            oriel.changedFlag = F_Lullaby;
            Route(oriel, true,
                (new Vector3(-5.6f, 0f, 9.4f), 6f, NPCActivity.Sit, "There it is. The whole song."),
                (new Vector3(-3.2f, 0f, 7f), 4f, NPCActivity.Idle, ""),
                (new Vector3(-5.3f, 0f, 4.3f), 4f, NPCActivity.Work, ""));
            var po = oriel.GetComponent<NPCPersona>();
            po.task = "Nursemaid of the nursery with no address: rocking the cot and folding blankets for children who have not arrived yet.";
            po.approachBarks.AddRange(new[] { "Hush - she's only just asleep.", "Mind the cot, love. Wipe your feet." });
            po.talkLines.Add(L("Nanny Oriel", "I sing the same lullaby every night. Some nights the notes go missing."));
            po.talkLines.Add(L("Nanny Oriel", "The children are coming tomorrow. All of them, from every house in the basin."));
            po.talkLines.Add(L("Nanny Oriel", "Forty-one expected, the rota says. I have counted the blankets twice."));
            po.talkLines.Add(L("Nanny Oriel", "The door of this room doesn't stay put. You have to keep your eyes on it, like a sleeping child."));
            po.talkLines.Add(L("Nanny Oriel", "Ilse Marrow brought a little carved zero. She said it would keep the room from forgetting itself."));
            po.conditional.Add(new ConditionalLines { whenHolding = K_Zero, lines = { L("Nanny Oriel", "The little zero. They carved one for every child they could not count."), L("Nanny Oriel", "Keep it close. Somebody has to.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "L06.heardPast", lines = { L("Nanny Oriel", "You heard it too? The holes in the song."), L("Nanny Oriel", "I carved the second note into the cot rail, so I'd never lose it.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "choice.L05.keepsake", lines = { L("Nanny Oriel", "That brass music box... I know that song. Every mother in the basin sang it."), L("Nanny Oriel", "Whoever owns it was loved. You can hear it in the gears.") } });
            po.changedLines.Add(L("Nanny Oriel", "There it is - the whole song. I'd forgotten how it ends."));
            po.changedLines.Add(L("Nanny Oriel", "Go on. The door won't wait for anyone who looks away."));
            po.conditional.Insert(1, new ConditionalLines { whenFlag = F_Lullaby, lines = { L("Nanny Oriel", "There it is - the whole song. I'd forgotten how it ends."), L("Nanny Oriel", "Go on. The door won't wait for anyone who looks away.") } });

            // ---- Little Wren (PRESENT): a child's echo ----
            var wren = Npc("Little Wren", presPeople, new Vector3(2.4f, 0f, 3.3f), 180f, "coat_ochre", true, 62);
            wren.transform.localScale = Vector3.one * 0.7f;
            Route(wren, false,
                (new Vector3(2.6f, 0f, 3.2f), 5f, NPCActivity.Work, "Stack, stack, stack..."),
                (new Vector3(3.6f, 0f, 9.6f), 4f, NPCActivity.Look, "Is that my mobile?"),
                (new Vector3(-1.6f, 0f, 5.2f), 4f, NPCActivity.Idle, "Mama said the bell would bring us here."));
            wren.changedFlag = F_Lullaby;
            Route(wren, true,
                (new Vector3(-0.8f, 0f, 7.6f), 5f, NPCActivity.Look, "Again! Again!"),
                (new Vector3(1.6f, 0f, 3.2f), 4f, NPCActivity.Work, ""),
                (new Vector3(2.8f, 0f, 12.8f), 4f, NPCActivity.Look, "Where does the door go?"));
            var pw = wren.GetComponent<NPCPersona>();
            pw.task = "A child's echo, stacking toy blocks into the notes of the lullaby she still remembers.";
            pw.approachBarks.AddRange(new[] { "Shh! The song is broken.", "Are you a grown-up from the water?" });
            pw.talkLines.Add(L("Little Wren", "There are only four notes. In my room the third one drowned."));
            pw.talkLines.Add(L("Little Wren", "Nanny Oriel says the door is shy. It only moves when you're not looking."));
            pw.talkLines.Add(L("Little Wren", "Maren had the lantern. Maren always has the lantern."));
            pw.talkLines.Add(L("Little Wren", "When the bell rings we all hold hands. Two by two. That's the rule."));
            pw.talkLines.Add(L("Little Wren", "The pencil on the wall is mine. Three is five. I wrote it so I wouldn't forget."));
            pw.conditional.Add(new ConditionalLines { whenHolding = K_Zero, lines = { L("Little Wren", "The zero! Is it for me?"), L("Little Wren", "...No. It's for the one nobody counted. Take it home for them.") } });
            pw.conditional.Add(new ConditionalLines { whenFlag = F_Lullaby, lines = { L("Little Wren", "You finished it! That's how Mama sang it."), L("Little Wren", "Where does the door go? Can I come? ...I'm not allowed, am I.") } });
            pw.conditional.Add(new ConditionalLines { whenFlag = F_Shapes, lines = { L("Little Wren", "You fixed my mobile! Now the music box will sing."), L("Little Wren", "Listen for the holes. The holes are the important part.") } });
            pw.conditional.Add(new ConditionalLines { whenFlag = "choice.L05.records", lines = { L("Little Wren", "Is my name in the big book? Is it written properly?"), L("Little Wren", "Good. Then I'm still counted.") } });
            pw.changedLines.Add(L("Little Wren", "You finished it! That's how Mama sang it."));
            pw.changedLines.Add(L("Little Wren", "Where does the door go? Can I come? ...I'm not allowed, am I."));

            // ---- The Witness (all states): keeper of the seam ----
            Route(witness, false,
                (new Vector3(-2.4f, 0f, 27.4f), 5f, NPCActivity.Look, "So many cycles. So few who look."),
                (new Vector3(2.4f, 0f, 27.8f), 5f, NPCActivity.Idle, ""),
                (new Vector3(0f, 0f, 25.4f), 4f, NPCActivity.Work, "Recording. Always recording."));
            witness.changedFlag = F_Witness;
            Route(witness, true,
                (new Vector3(0.8f, 0f, 29.2f), 6f, NPCActivity.Look, "Go back before the final bell."),
                (new Vector3(-3f, 0f, 28f), 4f, NPCActivity.Idle, ""),
                (new Vector3(3f, 0f, 28f), 4f, NPCActivity.Idle, ""));
            var pwi = witness.GetComponent<NPCPersona>();
            pwi.task = "Keeps watch over the seam between states, recording what the city chose to forget.";
            pwi.approachBarks.AddRange(new[] { "You came through the door with no address.", "I have waited many cycles for someone to look." });
            pwi.talkLines.Add(L("The Witness", "I am what remains when everyone else forgets."));
            pwi.talkLines.Add(L("The Witness", "Stand in the ring of light, and I will show you what I saw."));
            pwi.talkLines.Add(L("The Witness", "I write letters, sometimes. To people who have not arrived yet."));
            pwi.talkLines.Add(L("The Witness", "The children called this room the nursery. The Founders called it the pocket."));
            pwi.talkLines.Add(L("The Witness", "Do not ask my name. Names were the first thing they took from us."));
            pwi.conditional.Add(new ConditionalLines { whenHolding = K_Zero, lines = { L("The Witness", "Carry it carefully. It is the only proof of a child the city never counted."), L("The Witness", "The tower will want it back. Do not let it have it cheaply.") } });
            pwi.conditional.Add(new ConditionalLines { whenFlag = "L06.cot7", lines = { L("The Witness", "You found your name. Cot seven. I slept in cot eight, with the lantern."), L("The Witness", "You used to tell me the watch would start ticking when it was time to go home.") } });
            pwi.conditional.Add(new ConditionalLines { whenFlag = "choice.L05.keepsake", lines = { L("The Witness", "You carried a music box out of the flood once. A small brass one."), L("The Witness", "...Someone told me. Someone who never stopped being grateful.") } });
            pwi.conditional.Add(new ConditionalLines { whenFlag = "choice.L05.records", lines = { L("The Witness", "You saved the register. Every name the Founders struck out."), L("The Witness", "Odile Fenwick will know what to do with it. She always did.") } });
            pwi.changedLines.Add(L("The Witness", "Return to the tower, Arin. Before the final bell."));
            pwi.changedLines.Add(L("The Witness", "What you do with the truth is yours."));

            // Background echoes on the arrival landing: residents waiting for tomorrow.
            string[] murmurs = { "...tomorrow...", "...take the children up at first light...", "...did the door move?..." };
            var waiting = Crowd("Waiting Echoes West", logic, new Vector3(-3.3f, 0f, -20.5f), new Vector2(1.4f, 4f), 2, 6600, murmurs, "fabric_gold");
            waiting.AddRange(Crowd("Waiting Echoes East", logic, new Vector3(3.4f, 0f, -20.5f), new Vector2(1.4f, 4f), 1, 6601, murmurs, "fabric_gold"));
            foreach (var m in waiting) MakeHolo(m, "hologram");
        }

        static void BuildAudio(Transform root)
        {
            var a = Group("Audio", root);
            Ambient("NurseryHum", a, new Vector3(0f, 2f, 8f), AmbientKind.Nursery, 0.45f, TimeMask.All, 1f, 22f);
            Ambient("VoidDrone", a, new Vector3(0f, 2f, 8f), AmbientKind.FutureDrone, 0.28f, TimeMask.All, 0f, 60f);
            Ambient("SeamWind", a, new Vector3(0f, 1f, -10f), AmbientKind.Wind, 0.35f, TimeMask.All, 1f, 16f);
            Ambient("WetWall", a, new Vector3(0f, 1.5f, 15.5f), AmbientKind.Water, 0.18f, TimeMask.Present, 1f, 8f);
            Ambient("PastMurmur", a, new Vector3(-5f, 1.5f, 6f), AmbientKind.Crowd, 0.08f, TimeMask.Past, 1f, 10f);
            Ambient("WitnessDrone", a, new Vector3(0f, 2f, 26f), AmbientKind.FutureDrone, 0.35f, TimeMask.All, 1f, 14f);
            Ambient("TowerWind", a, new Vector3(0f, 2f, 39f), AmbientKind.Wind, 0.3f, TimeMask.All, 1f, 14f);
        }
    }
}
