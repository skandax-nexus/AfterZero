using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 08 — The First Zero. The Zero Chamber under the clock-tower core (final level).
    /// Layout (y = 0, +z = north):
    ///   Entry hall (start, safe observation) at z -42..-30 with the carved zero on a pedestal; corridor north into
    ///   the rotunda (r 18.5) with the central niche + dais at the origin, concentric hologram rings overhead.
    ///   Three state wings off the rotunda: WEST = PAST bells, NORTH = PRESENT bells, EAST = FUTURE bells.
    ///   Two choice consoles flank the dais. The north wing's back wall slides apart at the end, exposing a corridor
    ///   to a balcony (z 55..63) facing a horizon of regions with different physical laws.
    /// </summary>
    public static partial class Level08Builder
    {
        const string F_ZeroTaken = "L08.zeroTaken";
        const string F_Zero = "L08.zeroPlaced";
        const string F_Heard = "L08.melodyHeard";
        const string F_Seq = "L08.sequenceRestored";
        const string F_Made = "choice.L08.made";
        const string F_Stab = "choice.L08.stability";
        const string F_Evid = "choice.L08.evidence";
        const string F_Memory = "L08.memoryPlayed";
        const string F_Sign = "L08.signatureFound";
        const string F_Horizon = "L08.horizonSeen";
        const string F_TribunalDone = "L08.tribunal.done";
        const string F_OtherSpoke = "L08.otherArinSpoke";
        const string F_Watch = "L08.watchGiven";
        const float WingR = 23.6f;

        public static void Build(Transform root)
        {
            var d = Systems(root, 7, TimeMask.All, TimeState.Present, out var visuals);
            d.storySetup = "Beneath the bell lies the Zero Chamber, older than Veyra. Here Arin learns that the cycle was restarted on purpose -- to prevent a far larger collapse.";
            d.introLines.Add(L("Arin", "Under the bell. It's bigger than the tower. Bigger than the square."));
            d.introLines.Add(L("Arin", "Rings of light, three wings -- one for each state. And a niche in the middle, shaped like a zero."));
            d.introLines.Add(L("The Witness", "Everything began here. Place what you carry, and it will remember for you.", 4f));

            AddObjective(d, "Place the carved zero in the central niche", F_Zero, "The carved zero rests on the pedestal in the entry hall. Carry it to the niche on the central dais.");
            AddObjective(d, "Rebuild the bell sequence while the chamber breaks apart", F_Seq, "Read the inscription around the niche, or listen to the chamber. Each bell hangs in one state's wing: << PAST west, [ PRESENT ] north, FUTURE >> east. Falling segments flash red and shed grit first.");
            AddObjective(d, "Answer the Tribunal", F_TribunalDone, "Halden and Ondine argue for Stability; Odile, Pell and Maren for Evidence. Answer each of them.");
            AddObjective(d, "Walk the memory", F_Memory, "Walk the Nursery with Ilse. She has something for you.");
            AddObjective(d, "Choose: STABILITY or EVIDENCE", F_Made, "The two consoles beside the dais. Only one can be chosen, and both cost something.");
            AddObjective(d, "Read the intervention order", F_Sign, "The document risen from the niche.");
            AddObjective(d, "Walk the newly exposed corridor to the horizon", F_Horizon, "The north wing's back wall has opened.");

            d.payoffFlag = F_Sign;
            d.payoffClueTitle = "Signed in Your Own Hand";
            d.payoffClueText = "The order that restarted the cycle -- that folded Veyra back to the festival morning, again and again -- is signed in Arin's handwriting.\n\nNot this Arin. An Arin from another branch, who stood in this chamber, saw the larger collapse coming, and chose a smaller catastrophe that repeats.";

            // ---------- Look: deep, awe-inspiring, strong bloom, fog thin enough for the horizon ----------
            visuals.past.fogDensity = 0.006f; visuals.past.bloomIntensity = 1.4f;
            visuals.present.fogDensity = 0.007f; visuals.present.bloomIntensity = 1.7f;
            visuals.future.fogDensity = 0.006f; visuals.future.bloomIntensity = 3.0f; visuals.future.bloomThreshold = 0.7f;
            visuals.present.sunEuler = new Vector3(70f, 160f, 0f);
            visuals.past.sunEuler = new Vector3(65f, -40f, 0f);
            visuals.future.sunEuler = new Vector3(75f, 20f, 0f);

            Player(root, new Vector3(0f, 0.05f, -39f), 0f);
            Checkpoint("CP_Entry", root, new Vector3(0f, 0f, -39f), new Vector3(8f, 3f, 4f), 0f, true);
            Checkpoint("CP_Rotunda", root, new Vector3(0f, 0f, -16f), new Vector3(5f, 3f, 2f), 0f);
            Checkpoint("CP_Dais", root, new Vector3(0f, 0f, -6.5f), new Vector3(6f, 3f, 2f), 0f).climax = true;
            Checkpoint("CP_Corridor", root, new Vector3(0f, 0f, 32f), new Vector3(4.6f, 3f, 2f), 0f);

            var shared = Group("Shared", root);
            var past = StateGroup("Past Dressing", root, TimeMask.Past, false);
            var present = StateGroup("Present Dressing", root, TimeMask.Present, false);
            var future = StateGroup("Future Dressing", root, TimeMask.Future, false);
            var logic = Group("Logic", root);

            BuildChamber(shared, past, present, future, logic);
            var puzzle = BuildBells(root, shared, logic);
            BuildZero(root, shared, logic, puzzle);
            BuildChoice(root, shared, logic);
            BuildNpcs(shared);
            BuildCorridorAndHorizon(root, shared, logic);
            BuildCollapse(shared, logic);
            BuildTribunal(shared, logic);
            BuildMemory(shared, logic);
            BuildAudio(root);
            BuildArchive(shared, logic);
        }

        static Vector3 Polar(float r, float deg, float y = 0f) =>
            new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad) * r, y, Mathf.Sin(deg * Mathf.Deg2Rad) * r);

        // =====================================================================
        // Chamber shell: entry hall, rotunda, wings, ceiling, rings
        // =====================================================================

        static void BuildChamber(Transform shared, Transform past, Transform present, Transform future, Transform logic)
        {
            var g = Group("Zero Chamber", shared);
            Floor("ChamberFloor", g, new Vector3(0f, 0f, -4f), new Vector2(64f, 80f), "obsidian");
            NoShadow(Cylinder("RotundaInlay", g, new Vector3(0f, 0.008f, 0f), 18f, 0.01f, "ruin_dark", false));
            for (int i = 0; i < 4; i++)
                NoShadow(Cylinder("InlayRing", g, new Vector3(0f, 0.012f + i * 0.002f, 0f), 6f + i * 3f, 0.01f, i % 2 == 0 ? "rail_strip" : "ruin_dark", false));

            // Rotunda wall: 24 chord segments with gaps at E (0), N (90), W (180), S (270).
            float r = 18.5f;
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f;
                if (i % 6 == 0) continue;
                var w = Wall("RotundaWall", g, Polar(r, a - 7.5f), Polar(r, a + 7.5f), 16f, 0.8f, "obsidian");
                if (i % 2 == 1)
                {
                    var rib = Box("Rib", g, Polar(r - 0.5f, a - 7.5f, 8f), new Vector3(0.5f, 16f, 0.5f), "metal_dark", false);
                    rib.transform.rotation = Quaternion.Euler(0f, -a, 0f);
                    NoShadow(Box("RibNeon", g, Polar(r - 0.78f, a - 7.5f, 8f), new Vector3(0.08f, 15f, 0.08f), "neon_cyan", false));
                }
            }
            // Ceiling with an oculus (the tower core shaft).
            Box("CeilingN", g, new Vector3(0f, 24.2f, 15f), new Vector3(40f, 0.4f, 20f), "obsidian", false);
            Box("CeilingS", g, new Vector3(0f, 24.2f, -15f), new Vector3(40f, 0.4f, 20f), "obsidian", false);
            Box("CeilingW", g, new Vector3(-12.5f, 24.2f, 0f), new Vector3(15f, 0.4f, 10f), "obsidian", false);
            Box("CeilingE", g, new Vector3(12.5f, 24.2f, 0f), new Vector3(15f, 0.4f, 10f), "obsidian", false);
            Box("ShaftCap", g, new Vector3(0f, 42f, 0f), new Vector3(12f, 0.5f, 12f), "obsidian", false);
            for (int s = 0; s < 4; s++)
            {
                float a = s * 90f;
                var p = Polar(5.3f, a, 33f);
                var sw = Box("ShaftWall", g, p, new Vector3(0.4f, 18f, 10.6f), "metal_dark", false);
                sw.transform.rotation = Quaternion.Euler(0f, -a, 0f);
            }
            // Fill the rotunda gaps above the wing mouths (12 m) and the entry corridor (5 m).
            for (int i = 0; i < 24; i += 6)
            {
                float y0 = i == 18 ? 5f : 12f;
                Wall("GapFill", g, Polar(r, i * 15f - 7.5f, y0), Polar(r, i * 15f + 7.5f, y0), 16f - y0, 0.8f, "obsidian");
            }
            // Upper wall band between the rotunda (16 m) and the ceiling (24 m).
            for (int i = 0; i < 24; i++)
                Wall("UpperWall", g, Polar(r, i * 15f - 7.5f, 16f), Polar(r, i * 15f + 7.5f, 16f), 8f, 0.8f, "obsidian");

            // Entry hall + corridor.
            var e = Group("Entry", g);
            WallWithDoor("HallW", e, new Vector3(-5f, 0f, -42f), new Vector3(-5f, 0f, -30f), 7f, 0.6f, "obsidian", 2.4f, 3f, 0.5f);
            Wall("HallE", e, new Vector3(5f, 0f, -42f), new Vector3(5f, 0f, -30f), 7f, 0.6f, "obsidian");
            Wall("HallS", e, new Vector3(-5f, 0f, -42f), new Vector3(5f, 0f, -42f), 7f, 0.6f, "obsidian");
            Wall("HallNW", e, new Vector3(-5f, 0f, -30f), new Vector3(-2.4f, 0f, -30f), 7f, 0.6f, "obsidian");
            Wall("HallNE", e, new Vector3(2.4f, 0f, -30f), new Vector3(5f, 0f, -30f), 7f, 0.6f, "obsidian");
            Box("HallCeiling", e, new Vector3(0f, 7.2f, -36f), new Vector3(10.6f, 0.4f, 12.6f), "obsidian");
            Wall("CorridorW", e, new Vector3(-2.4f, 0f, -30f), new Vector3(-2.4f, 0f, -18.2f), 5f, 0.5f, "metal_dark");
            Wall("CorridorE", e, new Vector3(2.4f, 0f, -30f), new Vector3(2.4f, 0f, -18.2f), 5f, 0.5f, "metal_dark");
            Box("CorridorCeiling", e, new Vector3(0f, 5.2f, -24f), new Vector3(5.4f, 0.4f, 12f), "obsidian");
            for (int i = 0; i < 4; i++)
            {
                NoShadow(Box("FloorStrip", e, new Vector3(0f, 0.01f, -29f + i * 3f), new Vector3(3f, 0.02f, 0.15f), "rail_strip", false));
                NoShadow(Box("WallNeonW", e, new Vector3(-2.12f, 2.6f, -29f + i * 3f), new Vector3(0.05f, 2.4f, 0.12f), "neon_cyan", false));
                NoShadow(Box("WallNeonE", e, new Vector3(2.12f, 2.6f, -29f + i * 3f), new Vector3(0.05f, 2.4f, 0.12f), "neon_cyan", false));
            }
            Label("EntryGlyph", e, new Vector3(0f, 5.6f, -30.35f), 0f, "(0)", 2f, new Color(0.85f, 0.95f, 1f));
            Label("EntryText", e, new Vector3(0f, 6.4f, -41.6f), 180f, "ZERO CHAMBER", 0.8f, new Color(0.6f, 0.9f, 1f));
            PointLight("HallLight", e, new Vector3(0f, 5f, -37f), new Color(0.6f, 0.85f, 1f), 2.5f, 12f);
            PointLight("CorridorLight", e, new Vector3(0f, 4f, -24f), new Color(0.3f, 0.9f, 1f), 2f, 10f);

            // Wings (shared shells).
            string[] wingName = { "FUTURE >>", "[ PRESENT ]", "<< PAST" };
            float[] wingDeg = { 0f, 90f, 180f };
            for (int w = 0; w < 3; w++)
            {
                float a = wingDeg[w];
                Vector3 c = Polar(WingR, a);
                Vector3 radial = Polar(1f, a), tang = Polar(1f, a + 90f);
                var wg = Group("Wing " + wingName[w], g);
                Vector3 inner = c - radial * 5f, outer = c + radial * 5f;
                Wall("InnerL", wg, inner - tang * 5f, inner - tang * 2.4f, 12f, 0.6f, "obsidian");
                Wall("InnerR", wg, inner + tang * 2.4f, inner + tang * 5f, 12f, 0.6f, "obsidian");
                Wall("Lintel", wg, inner - tang * 2.4f + Vector3.up * 6f, inner + tang * 2.4f + Vector3.up * 6f, 6f, 0.6f, "obsidian");
                Wall("SideL", wg, inner - tang * 5f, outer - tang * 5f, 12f, 0.6f, "obsidian");
                Wall("SideR", wg, inner + tang * 5f, outer + tang * 5f, 12f, 0.6f, "obsidian");
                if (w != 1) Wall("Back", wg, outer - tang * 5f, outer + tang * 5f, 12f, 0.6f, "obsidian");
                Box("WingCeiling", wg, c + Vector3.up * 12.2f, new Vector3(10.6f, 0.4f, 10.6f), "obsidian", false);
                var lbl = Label("WingLabel", wg, Polar(18.0f, a, 6.6f), FaceYaw(Polar(18f, a), Vector3.zero), wingName[w], 1.4f, w == 0 ? new Color(0.6f, 0.95f, 1f) : w == 1 ? new Color(0.8f, 0.9f, 1f) : new Color(1f, 0.85f, 0.55f));
                NoShadow(Box("MouthTrim", wg, inner + Vector3.up * 6f - radial * 0.35f, new Vector3(Mathf.Abs(tang.x), 0f, Mathf.Abs(tang.z)) * 4.8f + Vector3.up * 0.12f + new Vector3(Mathf.Abs(radial.x), 0f, Mathf.Abs(radial.z)) * 0.1f, w == 0 ? "neon_magenta" : w == 1 ? "lamp_cold" : "lamp_warm", false));
            }
            // North wing back wall: two fixed pieces; the centre is the sliding door (see corridor).
            Vector3 nOuter = Polar(WingR + 5f, 90f);
            Wall("BackL", g, nOuter + new Vector3(-5f, 0f, 0f), nOuter + new Vector3(-2.4f, 0f, 0f), 12f, 0.6f, "obsidian");
            Wall("BackR", g, nOuter + new Vector3(2.4f, 0f, 0f), nOuter + new Vector3(5f, 0f, 0f), 12f, 0.6f, "obsidian");
            Wall("BackLintel", g, nOuter + new Vector3(-2.4f, 3f, 0f), nOuter + new Vector3(2.4f, 3f, 0f), 9f, 0.6f, "obsidian");

            // Concentric hologram rings (Spinners) + the zero of light.
            var rings = Group("Rings", g);
            for (int i = 0; i < 5; i++)
            {
                float rr = 5f + i * 3f;
                var ring = Hologram("HoloRing" + i, rings, new Vector3(0f, 9f + i * 2.6f, 0f), new Vector3(rr * 2f, 0.03f, rr * 2f), PrimitiveType.Cylinder, i % 3 == 0 ? "hologram" : i % 3 == 1 ? "hologram_amber" : "hologram_magenta", false, false);
                ring.transform.rotation = Quaternion.Euler(i % 2 == 0 ? 8f + i * 3f : -6f - i * 2f, i * 30f, i % 2 == 0 ? 4f : -9f);
                var sp = ring.AddComponent<Spinner>();
                sp.degreesPerSecond = new Vector3(0f, (i % 2 == 0 ? 1f : -1f) * (6f + i * 3f), 0f);
            }
            // Glyph rim: 36 small markers on the outermost ring height.
            for (int i = 0; i < 24; i++)
            {
                var m = Box("RingGlyph", rings, Polar(16.5f, i * 15f, 22f), new Vector3(0.6f, 0.6f, 0.06f), i % 4 == 0 ? "zero_glow" : "neon_cyan", false);
                m.transform.rotation = Quaternion.Euler(0f, -i * 15f + 90f, 0f);
                NoShadow(m);
            }
            Hologram("CoreShaftBeam", rings, new Vector3(0f, 26f, 0f), new Vector3(2.2f, 16f, 2.2f), PrimitiveType.Cylinder, "hologram", false, false);
            var bigZero = Label("ZeroOfLight", rings, new Vector3(0f, 14f, 0f), 0f, "0", 30f, new Color(0.9f, 0.97f, 1f));
            bigZero.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 12f, 0f);
            Particles("ChamberMotes", g, new Vector3(0f, 8f, 0f), new Vector3(34f, 14f, 34f), "motes");
            Particles("DeepFog", g, new Vector3(0f, 1f, 0f), new Vector3(34f, 2f, 34f), "mist");
            Particles("DeepFogWings", g, new Vector3(0f, 1f, 0f), new Vector3(56f, 2f, 56f), "mist");
            PointLight("RingLightCyan", g, new Vector3(-8f, 12f, -6f), new Color(0.3f, 0.85f, 1f), 3f, 26f);
            PointLight("RingLightMagenta", g, new Vector3(9f, 14f, 6f), new Color(1f, 0.3f, 0.85f), 2.5f, 26f);
            SpotLight("OculusShaft", g, new Vector3(0f, 23f, 0f), new Vector3(90f, 0f, 0f), new Color(0.75f, 0.9f, 1f), 8f, 30f, 35f, false);
            ReflectionProbe(g, new Vector3(0f, 8f, 0f), new Vector3(40f, 24f, 40f));

            // ---------- Per-state dressing ----------
            var pd = Group("Brass Age", past);
            for (int i = 0; i < 8; i++)
            {
                float a = 22.5f + i * 45f;
                Cylinder("Candelabrum", pd, Polar(16.5f, a, 1f), 0.12f, 2f, "brass", false);
                NoShadow(Sphere("Flame", pd, Polar(16.5f, a, 2.15f), 0.14f, "lamp_warm", false));
                var banner = Box("Banner", pd, Polar(17.6f, a, 8f), new Vector3(1.4f, 7f, 0.05f), i % 2 == 0 ? "fabric_red" : "fabric_gold", false);
                banner.transform.rotation = Quaternion.Euler(0f, -a + 90f, 0f);
                NoShadow(banner);
            }
            PointLight("CandleGlowW", pd, new Vector3(-12f, 3f, -6f), new Color(1f, 0.72f, 0.4f), 3f, 18f);
            Particles("Dust", pd, new Vector3(0f, 6f, 0f), new Vector3(34f, 10f, 34f), "dust");
            var brassRing = Cylinder("BrassFloorRing", pd, new Vector3(0f, 0.02f, 0f), 12f, 0.01f, "brass", false);
            NoShadow(brassRing);

            var prd = Group("Seepage", present);
            Particles("OculusRain", prd, new Vector3(0f, 23f, 0f), new Vector3(9f, 1f, 9f), "rain");
            for (int i = 0; i < 6; i++)
                NoShadow(Box("Puddle", prd, Polar(9f + (i % 3) * 2.5f, i * 60f + 20f, 0.016f), new Vector3(2.5f, 0.01f, 1.8f), "water", false));
            for (int i = 0; i < 2; i++)
            {
                float a = 45f + i * 180f;
                Lamp("WorkLamp", prd, Polar(15.5f, a), "sodium", 3.4f);
            }
            var drips = Particles("Drips", prd, new Vector3(0f, 15f, 0f), new Vector3(30f, 1f, 30f), "rain");
            var dem = drips.emission; dem.rateOverTime = 150f;

            var fd = Group("Neon Ruin", future);
            var rng = new System.Random(808);
            for (int i = 0; i < 12; i++)
            {
                var p = Polar(11f + (float)rng.NextDouble() * 6f, i * 30f + 10f, 0f);
                Tilted("Crystal", fd, p + Vector3.up * 1.2f, new Vector3(0.6f, 2.4f + (float)rng.NextDouble() * 2f, 0.6f), "crystal", new Vector3(rng.Next(-25, 25), rng.Next(360), rng.Next(-25, 25)), false);
            }
            for (int i = 0; i < 10; i++)
            {
                var p = Polar(6f + (float)rng.NextDouble() * 10f, rng.Next(360), 4f + (float)rng.NextDouble() * 10f);
                var deb = Tilted("FloatingShard", fd, p, new Vector3(0.8f + (float)rng.NextDouble(), 0.3f, 0.6f + (float)rng.NextDouble()), "ruin_dark", new Vector3(rng.Next(360), rng.Next(360), rng.Next(360)), false);
                var fb = deb.AddComponent<FloatBob>(); fb.amplitude = 0.4f; fb.frequency = 0.12f; fb.drift = new Vector3(3f, 12f, 5f);
            }
            for (int i = 0; i < 3; i++)
            {
                Hologram("DataPillar", fd, Polar(14f, 45f + i * 120f, 5f), new Vector3(0.8f, 5f, 0.8f), PrimitiveType.Cylinder, "hologram", true, true);
            }
            PointLight("NeonFloorCyan", fd, new Vector3(-10f, 1.5f, 8f), new Color(0.3f, 0.9f, 1f), 3f, 14f);
        }

        // =====================================================================
        // Bells: the sequence puzzle across the three state wings
        // =====================================================================

        // glyph, note, state, wing angle, lateral offset
        static readonly (string glyph, string word, int note, TimeMask state, float deg, float side)[] Bells =
        {
            ("/\\", "spire", 0, TimeMask.Past, 180f, -2.5f),
            ("~",   "wave",  2, TimeMask.Past, 180f, 2.5f),
            ("(0)", "zero",  4, TimeMask.Present, 90f, -2.5f),
            ("||",  "gate",  6, TimeMask.Present, 90f, 2.5f),
            ("[#]", "cage",  5, TimeMask.Future, 0f, -2.5f),
            ("<>",  "eye",   1, TimeMask.Future, 0f, 2.5f),
        };

        static SequencePuzzle BuildBells(Transform root, Transform shared, Transform logic)
        {
            var pgo = new GameObject("BellSequence");
            pgo.transform.SetParent(logic, false);
            pgo.transform.position = new Vector3(0f, 2f, 0f);
            var puzzle = pgo.AddComponent<SequencePuzzle>();
            puzzle.solution = new List<int> { 4, 0, 5, 2, 4 }; // (0) /\ [#] ~ (0)
            puzzle.solvedFlag = F_Seq;
            puzzle.requiredFlags.Add(F_Zero);
            puzzle.demoNoteGap = 0.8f;

            var groups = new Dictionary<TimeMask, Transform>
            {
                { TimeMask.Past, StateGroup("Bells (Past)", root, TimeMask.Past, true) },
                { TimeMask.Present, StateGroup("Bells (Present)", root, TimeMask.Present, true) },
                { TimeMask.Future, StateGroup("Bells (Future)", root, TimeMask.Future, true) },
            };
            foreach (var b in Bells)
            {
                var gp = groups[b.state];
                Vector3 c = Polar(WingR, b.deg);
                Vector3 tang = Polar(1f, b.deg + 90f);
                Vector3 p = c + tang * b.side + Vector3.up * 1.9f;
                string body = b.state == TimeMask.Past ? "brass" : b.state == TimeMask.Present ? "copper_verdigris" : "crystal";
                var frame = Box("BellFrame", gp, p + Vector3.up * 1.5f, new Vector3(0.2f, 0.2f, 0.2f), "metal_dark", false);
                Box("BellChain", gp, p + Vector3.up * 3.4f, new Vector3(0.06f, 3.6f, 0.06f), "metal_dark", false);
                var bellGo = Prim(PrimitiveType.Sphere, "Bell " + b.glyph, gp, p, new Vector3(1.1f, 1.3f, 1.1f), body, true, false);
                var lip = Cylinder("BellLip", bellGo.transform, p + Vector3.down * 0.55f, 0.62f, 0.12f, body, false);
                var btn = bellGo.AddComponent<SequenceButton>();
                btn.puzzle = puzzle;
                btn.noteIndex = b.note;
                btn.bellLike = true;
                btn.pressVisual = bellGo.transform;
                btn.prompt = "Strike the " + b.word + " bell " + b.glyph;
                btn.usableIn = b.state;
                // Readable by shape + text: glyph plate under the bell, facing the wing mouth.
                Vector3 plate = p + Vector3.down * 1.2f - Polar(1f, b.deg) * 0.8f;
                Label("BellGlyph", gp, plate + Vector3.up * 2.9f, FaceYaw(plate, Vector3.zero), b.glyph, 1.4f, Color.white);
                Label("BellWord", gp, plate + Vector3.up * 2.35f, FaceYaw(plate, Vector3.zero), b.word.ToUpperInvariant(), 0.35f, new Color(0.85f, 0.95f, 1f));
                Box("BellPlinth", gp, new Vector3(p.x, 0.25f, p.z), new Vector3(1.4f, 0.5f, 1.4f), "metal_dark");
                if (b.state == TimeMask.Future)
                    Hologram("BellHalo", gp, p + Vector3.up * 0.9f, new Vector3(1.6f, 0.02f, 1.6f), PrimitiveType.Cylinder, "hologram_magenta", false, true);
            }
            // Per-wing lights (exist only in their state).
            PointLight("PastWingLight", groups[TimeMask.Past], Polar(WingR, 180f, 5f), new Color(1f, 0.75f, 0.45f), 3.5f, 13f);
            PointLight("PresentWingLight", groups[TimeMask.Present], Polar(WingR, 90f, 5f), new Color(0.7f, 0.85f, 1f), 3f, 13f);
            PointLight("FutureWingLight", groups[TimeMask.Future], Polar(WingR, 0f, 5f), new Color(1f, 0.35f, 0.9f), 3.5f, 13f);

            // Wing dressing per state.
            Particles("WingDust", groups[TimeMask.Past], Polar(WingR, 180f, 4f), new Vector3(9f, 6f, 9f), "dust");
            Particles("WingSteam", groups[TimeMask.Present], Polar(WingR, 90f, 0.2f), new Vector3(3f, 0.2f, 3f), "steam");
            Particles("WingMotes", groups[TimeMask.Future], Polar(WingR, 0f, 4f), new Vector3(9f, 6f, 9f), "motes");

            // Future wing: the branch log (clue for the other Arin).
            var fwing = groups[TimeMask.Future];
            Clue("BranchLog", fwing, Polar(WingR + 3.5f, 0f, 1.1f), new Vector3(0.5f, 1.4f, 1.6f), "obsidian", "Read the branch log", "L08.branchLogRead", "L08.branchLog",
                "Branch Log // Observer: ARIN",
                "CYCLES OBSERVED: 0, 1, 2, 3 ... (count unstable)\n\nWithout the loop, the collapse does not stop at Veyra. It spreads outward: regions where gravity forgets its direction, where water forgets to fall, where the sun is the wrong colour.\n\nA small catastrophe, repeating, holds the big one shut.\n-- A.",
                true, TimeMask.Future);
            return puzzle;
        }

        // =====================================================================
        // The zero, the niche, the inscription, the melody
        // =====================================================================

        static void BuildZero(Transform root, Transform shared, Transform logic, SequencePuzzle puzzle)
        {
            var g = Group("Niche", shared);
            // Entry pedestal with the carved zero.
            Box("Pedestal", g, new Vector3(0f, 0.5f, -33f), new Vector3(0.9f, 1f, 0.9f), "stone_warm");
            NoShadow(Box("PedestalTrim", g, new Vector3(0f, 1.02f, -33f), new Vector3(1f, 0.04f, 1f), "brass", false));
            // Uniformly scaled grab root (so children never skew), visuals as children.
            var zero = Grab("CarvedZero", g, new Vector3(0f, 1.3f, -33f), Vector3.one * 0.5f, "stone_warm", "zero", "carved zero", PrimitiveType.Cube, true);
            Object.DestroyImmediate(zero.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(zero.GetComponent<MeshFilter>());
            zero.pickedUpFlag = F_ZeroTaken;
            zero.resetOnLoop = false;
            zero.GetComponent<Rigidbody>().isKinematic = true;
            var disc = Prim(PrimitiveType.Cylinder, "ZeroDisc", zero.transform, new Vector3(0f, 1.3f, -33f), new Vector3(1.2f, 0.08f, 1.2f), "stone_warm", false, false);
            disc.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var hole = Prim(PrimitiveType.Cylinder, "ZeroHole", zero.transform, new Vector3(0f, 1.3f, -33f), new Vector3(0.5f, 0.1f, 0.85f), "zero_glow", false, false);
            hole.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("ZeroGlyph", zero.transform, new Vector3(0f, 1.3f, -33.06f), 0f, "0", 2.4f, new Color(1f, 0.95f, 0.8f));
            Label("ZeroGlyphBack", zero.transform, new Vector3(0f, 1.3f, -32.94f), 180f, "0", 2.4f, new Color(1f, 0.95f, 0.8f));
            PointLight("PedestalLight", g, new Vector3(0f, 2.6f, -33f), new Color(1f, 0.9f, 0.7f), 2f, 5f);
            Label("PedestalText", g, new Vector3(0f, 0.6f, -33.47f), 0f, "WHAT ARIN CARRIED", 0.3f, new Color(1f, 0.9f, 0.7f));

            var carried = Zone("CarriedFromNursery", logic, new Vector3(0f, 1f, -36f), new Vector3(9f, 2f, 4f), "L08.carriedLine");
            carried.requiredFlags.Add("L06.zeroTaken");
            carried.lines.Add(L("Arin", "The zero I took from the nursery. I set it down for one breath and it's already warm -- like it knows this room."));
            var pickLines = Zone("ZeroSeen", logic, new Vector3(0f, 1f, -33f), new Vector3(3f, 2f, 3f), "L08.zeroSeen");
            pickLines.lines.Add(L("Arin", "Carved by the same hand as the zero in the nursery. It belongs in the middle of this room."));

            // Central dais + niche.
            var dais = Cylinder("Dais", g, new Vector3(0f, 0.15f, 0f), 3.2f, 0.3f, "metal_dark", false);
            var col = dais.AddComponent<BoxCollider>();
            NoShadow(Cylinder("DaisRim", g, new Vector3(0f, 0.31f, 0f), 3.3f, 0.02f, "rail_strip", false));
            Box("NichePillar", g, new Vector3(0f, 0.8f, 0f), new Vector3(1f, 1.0f, 1f), "obsidian");
            var socket = Socket("ZeroNiche", g, new Vector3(0f, 1.32f, 0f), "zero", F_Zero, "Set", 0.4f);
            socket.wrongItemMessage = "The niche is shaped like a zero.";
            Label("NicheGlyph", g, new Vector3(0f, 0.8f, -0.52f), 0f, "( 0 )", 0.5f, new Color(0.85f, 0.95f, 1f));

            // Powered-up state (after the zero is placed).
            var power = Group("PoweredUp", g);
            for (int i = 0; i < 24; i++)
            {
                var seg = Box("GlowRing", power, Polar(3.6f, i * 15f, 0.04f), new Vector3(0.9f, 0.04f, 0.18f), "zero_glow", false);
                seg.transform.rotation = Quaternion.Euler(0f, -i * 15f + 90f, 0f);
                NoShadow(seg);
            }
            Hologram("NicheBeam", power, new Vector3(0f, 12f, 0f), new Vector3(0.6f, 11f, 0.6f), PrimitiveType.Cylinder, "hologram", false, false);
            PointLight("NicheLight", power, new Vector3(0f, 3f, 0f), new Color(0.85f, 0.95f, 1f), 6f, 16f);
            // Inscription: the bell order, by glyph and number (never colour-only).
            string[] order = { "1  (0)", "2  /\\", "3  [#]", "4  ~", "5  (0)" };
            for (int i = 0; i < order.Length; i++)
            {
                float a = 250f + i * 10f;
                var pos = Polar(3.9f, a, 0.9f);
                Label("Inscription" + (i + 1), power, pos, FaceYaw(pos, Polar(10f, a)), order[i], 0.55f, new Color(0.95f, 0.97f, 1f));
            }
            power.gameObject.SetActive(false);
            var powerReact = Reactor("ChamberPowersUp", logic, F_Zero);
            powerReact.enableWhenTrue.Add(power.gameObject);
            powerReact.announce = true;
            powerReact.toast = "The chamber wakes. An inscription lights around the niche, and the bells in every wing can answer now.";
            powerReact.reversible = false;

            var insc = Clue("Inscription", g, Polar(4.2f, 270f, 0.35f), new Vector3(1.6f, 0.5f, 0.3f), "metal_dark", "Read the inscription", "L08.inscriptionRead", "L08.inscription",
                "Inscription Around the Niche",
                "RING THEM AS THE FIRST CYCLE RANG:\n\n1  (0)  the zero      -- [ PRESENT ], north\n2  /\\   the spire     -- << PAST, west\n3  [#]  the cage      -- FUTURE >>, east\n4  ~    the wave      -- << PAST, west\n5  (0)  the zero      -- [ PRESENT ], north\n\nA bell lives in only one state. Shift to where it hangs.",
                true);
            insc.requiredFlags.Add(F_Zero);
            insc.lockedMessage = "The inscription is dark. The niche is still empty.";
            var listen = Prim(PrimitiveType.Cube, "ChamberMemoryHorn", g, Polar(4.2f, 290f, 0.6f), new Vector3(0.5f, 1.2f, 0.5f), "brass", true, false);
            var ml = listen.AddComponent<MelodyListener>();
            ml.puzzle = puzzle;
            ml.bellLike = true;
            ml.heardFlag = F_Heard;
            ml.prompt = "Listen to the chamber's memory of the bell";
            ml.requiredFlags.Add(F_Zero);
            ml.lockedMessage = "Silent. The chamber has nothing to remember yet.";
            Label("HornTag", g, Polar(4.2f, 290f, 1.5f), FaceYaw(Polar(4.2f, 290f), Polar(10f, 290f)), "LISTEN", 0.3f, new Color(1f, 0.9f, 0.6f));

            var solved = Reactor("SequenceRestored", logic, F_Seq);
            solved.announce = true;
            solved.cue = Cue.PuzzleSolved;
            solved.toast = "The bell sequence is whole again. Two consoles rise beside the niche.";
        }

        // =====================================================================
        // The choice, the memory, the signature
        // =====================================================================

        static void BuildChoice(Transform root, Transform shared, Transform logic)
        {
            var g = Group("Choice", shared);
            GameObject Console(string name, Vector3 pos, string mat, string holo, string title, string sub)
            {
                var cg = Group(name, g);
                var body = Box("ConsoleBody", cg, pos + Vector3.up * 0.55f, new Vector3(1.4f, 1.1f, 0.8f), "obsidian");
                NoShadow(Box("ConsoleTop", cg, pos + new Vector3(0f, 1.12f, 0f), new Vector3(1.4f, 0.04f, 0.8f), mat, false));
                Hologram("ConsoleHolo", cg, pos + Vector3.up * 2.2f, new Vector3(0.9f, 0.9f, 0.9f), PrimitiveType.Sphere, holo, true, true);
                Label("ConsoleTitle", cg, pos + new Vector3(0f, 3.1f, 0f), FaceYaw(pos, new Vector3(pos.x, 0f, -14f)), title, 0.7f, Color.white);
                Label("ConsoleSub", cg, pos + new Vector3(0f, 2.75f, 0f), FaceYaw(pos, new Vector3(pos.x, 0f, -14f)), sub, 0.28f, new Color(0.85f, 0.9f, 1f));
                return cg.gameObject;
            }
            var stabPos = new Vector3(-6.5f, 0f, -6f);
            var evidPos = new Vector3(6.5f, 0f, -6f);
            var stab = Console("Console STABILITY", stabPos, "neon_cyan", "hologram", "STABILITY  [=]", "The loop goes on, gentler: everyone remembers a little more each night. Cost: you stay as its keeper; Maren stays in the seam.");
            var evid = Console("Console EVIDENCE", evidPos, "neon_amber", "hologram_amber", "EVIDENCE  [?]", "Publish the names. The states separate; the horizon opens. Cost: the loop-born, like Lise, fade at dawn.");

            ChoiceInteractable MakeChoice(GameObject c, string option, string confirm, GameObject other, SubtitleLine[] lines)
            {
                var ci = c.transform.Find("ConsoleBody").gameObject.AddComponent<ChoiceInteractable>();
                ci.groupFlag = F_Made;
                ci.optionFlag = option;
                ci.confirmText = confirm;
                ci.alternatives.Add(other);
                ci.lines.AddRange(lines);
                ci.prompt = option == F_Stab ? "Choose STABILITY" : "Choose EVIDENCE";
                ci.requiredFlags.Add(F_OtherSpoke);
                ci.lockedMessage = "The console is dark. The Tribunal and the memory come first.";
                return ci;
            }
            MakeChoice(stab, F_Stab, "Choose STABILITY? No one in Veyra will ever die for good, and each night they will remember a little more. But you will never leave: you become the city's keeper of time, as Ilse was, and Maren stays in the seam with you. The evidence console goes dark forever.", evid,
                new[] { L("Arin", "Stability. Let the night keep turning -- but loosen its grip."), L("Arin", "Let them wake up each morning remembering one more thing. The cup. A name. A face at the tram stairs."), L("The Witness", "Then the festival comes again, and someone will say 'welcome back' -- and mean it.", 4.5f), L("Arin", "And I stay. Someone has to keep time. Ilse did.", 3.5f), L("The Witness", "Then I'll stay too. The seam has room for two.", 3.5f) });
            MakeChoice(evid, F_Evid, "Choose EVIDENCE? The names are published, the Founders' secret is out, the states separate and tomorrow finally comes. But the loop-born -- Lise Ashgrove among them -- fade at dawn. The stability console goes dark forever.", stab,
                new[] { L("Arin", "Keep it. Every cycle, every list, every struck name -- on record."), L("Arin", "Let the cycle crack. If the states have to come apart for the truth to come out, let them."), L("The Witness", "Then the horizon opens, and the world out there stops waiting behind a wall.", 4.5f), L("Arin", "Lise. Pell. I'm sorry. Dawn is coming anyway.", 3.5f) });
            // Consoles are dim until the sequence is restored.
            var dim = Group("ConsolesDark", g);
            foreach (var p in new[] { stabPos, evidPos })
                Hologram("DarkVeil", dim, p + Vector3.up * 1.3f, new Vector3(1.8f, 2.6f, 1.2f), PrimitiveType.Cube, "hologram_magenta", false, false);
            var lit = Reactor("ConsolesLit", logic, F_OtherSpoke);
            lit.disableWhenTrue.Add(dim.gameObject);

            // Earlier choices echo here (Level 05).
            var rec = Group("L05 Records Echo", g);
            for (int i = 0; i < 5; i++)
                Hologram("FloatingRecord", rec, evidPos + new Vector3(-0.8f + i * 0.4f, 3.8f + (i % 2) * 0.3f, 0.6f), new Vector3(0.3f, 0.4f, 0.02f), PrimitiveType.Cube, "hologram_amber", true, true);
            Label("RecordsNote", rec, evidPos + new Vector3(0f, 4.6f, 0.6f), FaceYaw(evidPos, new Vector3(evidPos.x, 0, -14f)), "THE DROWNED RECORDS YOU SAVED", 0.25f, new Color(1f, 0.85f, 0.55f));
            rec.gameObject.SetActive(false);
            var recR = Reactor("L05RecordsShown", logic, "choice.L05.records");
            recR.enableWhenTrue.Add(rec.gameObject);
            var keep = Group("L05 Keepsake Echo", g);
            Hologram("Keepsake", keep, stabPos + new Vector3(0f, 3.9f, 0.6f), new Vector3(0.35f, 0.35f, 0.35f), PrimitiveType.Sphere, "hologram", true, true);
            Label("KeepsakeNote", keep, stabPos + new Vector3(0f, 4.6f, 0.6f), FaceYaw(stabPos, new Vector3(stabPos.x, 0, -14f)), "THE KEEPSAKE YOU KEPT", 0.25f, new Color(0.7f, 0.95f, 1f));
            keep.gameObject.SetActive(false);
            var keepR = Reactor("L05KeepsakeShown", logic, "choice.L05.keepsake");
            keepR.enableWhenTrue.Add(keep.gameObject);

            // Visible consequences of this choice.
            var calm = Group("StableField", g);
            var calmRing = Hologram("CalmRing", calm, new Vector3(0f, 0.5f, 0f), new Vector3(30f, 0.02f, 30f), PrimitiveType.Cylinder, "hologram", false, true);
            calmRing.GetComponent<Spinner>().degreesPerSecond = new Vector3(0f, 4f, 0f);
            PointLight("CalmLight", calm, new Vector3(0f, 6f, 0f), new Color(0.6f, 0.95f, 1f), 3f, 25f);
            calm.gameObject.SetActive(false);
            var stabR = Reactor("StabilityChosen", logic, F_Stab);
            stabR.enableWhenTrue.Add(calm.gameObject);
            stabR.announce = true;
            stabR.toast = "The rings settle into one gentle orbit. The cycle holds -- and loosens.";
            var archive = Group("ArchiveField", g);
            for (int i = 0; i < 8; i++)
            {
                var col = Hologram("RecordColumn", archive, Polar(11f, i * 45f + 22.5f, 3f), new Vector3(0.7f, 6f, 0.7f), PrimitiveType.Cube, "hologram_amber", true, false);
                col.GetComponent<FloatBob>().drift = new Vector3(0f, 15f, 0f);
            }
            PointLight("ArchiveLight", archive, new Vector3(0f, 6f, 0f), new Color(1f, 0.75f, 0.4f), 3f, 25f);
            archive.gameObject.SetActive(false);
            var evidR = Reactor("EvidenceChosen", logic, F_Evid);
            evidR.enableWhenTrue.Add(archive.gameObject);
            evidR.announce = true;
            evidR.toast = "Columns of record rise around the chamber. Far above, the states begin to drift apart.";

            // ---- The memory (Arin, another branch) ----
            var tableau = Group("MemoryTableau", g);
            var ghost = Npc("Arin (memory)", tableau, new Vector3(0f, 0.3f, 2.2f), 180f, "arin_coat", false, 88);
            HoloBody(ghost, "hologram_amber");
            Object.DestroyImmediate(ghost.GetComponent<CapsuleCollider>());
            Hologram("MemoryDesk", tableau, new Vector3(0f, 1.0f, 1.3f), new Vector3(1.4f, 0.08f, 0.7f), PrimitiveType.Cube, "hologram_amber", false, false);
            Hologram("MemoryPage", tableau, new Vector3(0f, 1.08f, 1.3f), new Vector3(0.4f, 0.01f, 0.5f), PrimitiveType.Cube, "hologram", false, false);
            var other = new GameObject("OtherBranchSpeaks").AddComponent<MemorySequence>();
            other.transform.SetParent(logic, false);
            other.triggerFlag = F_Memory;
            other.completeFlag = F_OtherSpoke;
            other.showDuring = tableau.gameObject;
            other.lines.Add(L("Arin (other branch)", "In my branch the bell rang twice and nothing came back. All three states fell into one. I was the only child they pulled out.", 5.5f));
            other.lines.Add(L("Arin (other branch)", "I grew up in the wreck. I found the Zero Engine under the bell. It couldn't undo the fall -- but it could start the night again.", 5.5f));
            other.lines.Add(L("Arin (other branch)", "One bad night, repeating, instead of the larger collapse. I signed the order, and I sent a younger me back in to check on it.", 5.5f));
            other.lines.Add(L("Arin (other branch)", "I restarted the night. You are the version of me that gets to choose.", 5f));
            other.lines.Add(L("Arin", "...My watch. It's ticking.", 3f));
            var tick = new GameObject("ArinsWatch").AddComponent<L08WatchTick>();
            tick.transform.SetParent(logic, false);
            tick.startFlag = F_OtherSpoke;
            tick.toast = "Your watch -- 'For A. - keep time for me. I.M.' -- has started ticking.";

            // ---- The intervention order rises from the niche ----
            var order = Group("InterventionOrder", g);
            order.position = new Vector3(0f, 0f, 0f);
            var doc = Clue("OrderDocument", order, new Vector3(0f, 2.1f, -0.2f), new Vector3(0.6f, 0.8f, 0.04f), "paper", "Read the intervention order", F_Sign, "L08.order",
                "Intervention Order (0)",
                "BY THE AUTHORITY OF THE ZERO CHAMBER:\nRestart the Veyra cycle at the festival morning. Let the bell ring once, then twice. Repeat until a branch is found in which the larger collapse does not follow.\n\nResponsibility for every repetition is mine.\n\n                        -- Arin\n        (branch unrecorded. the handwriting is yours.)",
                true);
            Label("OrderSignature", order, new Vector3(0f, 1.85f, -0.25f), 0f, "-- Arin", 0.35f, new Color(0.1f, 0.1f, 0.15f));
            PointLight("OrderLight", order, new Vector3(0f, 2.8f, -1f), new Color(1f, 0.95f, 0.85f), 3f, 5f);
            order.gameObject.SetActive(false);
            var orderR = Reactor("OrderRises", logic, F_Made);
            orderR.enableWhenTrue.Add(order.gameObject);
            orderR.announce = true;
            orderR.cue = Cue.Revelation;
            orderR.toast = "A document rises from the niche.";
            orderR.reversible = false;
        }

        static void HoloBody(NPCMotor m, string holoKey)
        {
            var mat = AZMaterials.Hologram(holoKey);
            foreach (var r in m.body.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
