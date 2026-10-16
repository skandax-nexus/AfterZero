using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 01 - The Bell That Rings Twice (Veyra Arrival Quarter).
    /// Tutorial for observation, the 30 s recurrence and causal persistence (Past -> Present).
    ///
    /// Layout (street runs along +Z):
    ///   z -16..-6  raised arrival terrace (spawn, safe observation area) with stairs down
    ///   z  -6..48  festival street: postbox (z12), cup stall (left, z22), side lane + cart landing (right, z17..28)
    ///   z  48      festival gate (opens when the cup is saved)
    ///   z  48..62  tower plaza;  z 62+ clock tower (sealed door, exit vestibule)
    /// </summary>
    public static partial class Level01Builder
    {
        // State groups used by the helpers.
        static Transform shared, pastLook, presLook, pastPlay, presPlay;

        // Flags
        const string F_LetterPosted = "loop.L01.letterPosted";
        const string F_CartRolled = "loop.L01.cartRolled";
        const string F_LetterFound = "L01.letterFound";
        const string F_CrashSeen = "L01.crashSeen";
        const string F_VendorTalked = "L01.vendorTalked";
        const string F_Observed = "L01.routinesObserved";
        const string F_CartMoved = "L01.cartMovedPast";
        const string F_CupSaved = "L01.cupSaved";
        const string F_Inspected = "L01.streetInspected";
        const string F_LetterTruth = "L01.letterTruth";
        const string F_Entered = "L01.enteredClockworks";
        const string Clue_Letter = "clue.L01_letter";
        const string F_CartChocked = "loop.L01.cartChocked";
        const string F_LiseStopped = "loop.L01.liseStopped";
        const string F_PellDiverted = "loop.L01.pellDiverted";
        const string F_Perfect = "loop.L01.perfectLoop";
        const string F_LiseSeen = "L01.liseRanSeen";
        const string Clue_Truth = "clue.L01_letterTruth";

        static readonly Color Warm = new Color(1f, 0.74f, 0.42f);
        static readonly Color Sodium = new Color(1f, 0.6f, 0.28f);
        static readonly Color Cold = new Color(0.55f, 0.75f, 1f);

        public static void Build(Transform root)
        {
            loopEvents.Clear();
            var d = Systems(root, 0, TimeMask.Present, TimeState.Present, out var visuals, 30f, F_CupSaved);
            // Sister Vossel's reports tighten the loop (30 -> 24 -> 18 s); the timeline rescales every scheduled moment.
            var esc = d.gameObject.AddComponent<LoopEscalation>();
            esc.anomaliesPerStep = 3; esc.step = 6f; esc.minimumDuration = 18f;
            Tune(visuals);

            d.storySetup = "A festival street repeats thirty seconds, but a broken blue cup remains broken.";
            d.introLines.Add(L("Arin", "Veyra. Bunting in the rain, music under the awnings... and that bell."));
            d.introLines.Add(L("Arin", "The letter from the train said it plainly: don't trust the first bell. Save the blue cup.", 4f));
            d.introLines.Add(L("The Witness", "Watch the street, Arin. Everything here comes back every thirty seconds. Nearly everything."));
            d.introLines.Add(L("Arin", "A courier with a letter. A cup vendor. A cart on the slope. Let me watch first."));
            d.introLines.Add(L("The Witness", "Look for the thing the loop cannot mend.", 3f));

            AddObjective(d, "Follow the courier and read the letter he posts", F_LetterFound, "The courier with the satchel walks from the terrace stairs to the postbox on the left. Wait for him there.");
            AddObjective(d, "Watch the street's routine and talk to the cup vendor", F_Observed, "Talk to Mirela at the blue-cup stall, and watch what happens when the bell rings twice.");
            AddObjective(d, "Shift to the PAST and push the festival cart into the bay", F_CartMoved, "Press [1] to shift to the PAST. Climb the side lane on the right and push the cart into the stone bay marked CART BAY. Keep out of Sister Vossel's lantern when you shift.");
            AddObjective(d, "The cup still breaks. Watch the stall again: what else hits it?", F_LiseSeen, "Stay in the PRESENT near the stall around 0:14 of the loop. Watch the lane mouth.");
            AddObjective(d, "THE PERFECT LOOP: park the cart, stop Lise, divert Pell - all in one loop", F_CupSaved, "Talk to Pell early so he takes the long way. Push the cart into the bay in the PAST. Be back in the PRESENT at the lane mouth before 0:14 - talk to Lise or stand in her path. Avoid Vossel's lantern or the loop gets shorter.");
            AddObjective(d, "Inspect the changed street beyond the festival gate", F_Inspected, "The festival gate at the end of the street has opened. Walk into the tower plaza.");
            AddObjective(d, "Read the letter at the tower door", F_LetterTruth, "A letter waits on the step of the clock-tower door.");
            AddObjective(d, "Enter the clockworks", F_Entered, "Walk through the open tower door.");

            d.payoffFlag = F_LetterTruth;
            d.payoffClueTitle = "Dated Tomorrow";
            d.payoffClueText = "The letter is dated tomorrow and pressed with the clock-tower seal (0). And the handwriting is Arin's own - older, shakier, " +
                               "with the same long-tailed A as the engraving in the stopped watch: 'For A. - keep time for me. I.M.' Arin wrote it, from a tomorrow Arin cannot remember.";

            Player(root, new Vector3(0f, 3.05f, -13f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0f, 3f, -13f), new Vector3(6f, 3f, 5f), 0f, true);

            shared = Group("Shared", root);
            pastLook = StateGroup("PastDressing", root, TimeMask.Past, false);
            presLook = StateGroup("PresentDressing", root, TimeMask.Present, false);
            pastPlay = StateGroup("PastGameplay", root, TimeMask.Past, true);
            presPlay = StateGroup("PresentGameplay", root, TimeMask.Present, true);
            var logic = Group("Logic", root);

            BuildTerrace();
            BuildStreet();
            BuildLane();
            BuildGateAndPlaza(logic);
            BuildSkyline();
            BuildAtmosphere();

            var cup = BuildCupStall(logic);
            BuildCart(logic);
            BuildLetter(logic);
            BuildChangedStreet(logic, cup);
            BuildNpcs();
            BuildFlow(logic);
            BuildKeepersAndClimax(logic);

            var timeline = d.gameObject.AddComponent<L01LoopTimeline>();
            timeline.events.AddRange(loopEvents);
        }

        // =================================================================
        // Look
        // =================================================================

        static void Tune(TemporalVisuals v)
        {
            v.past.fogDensity = 0.009f;
            v.past.sunIntensity = 1.7f;
            v.past.bloomIntensity = 0.9f;
            v.present.fogDensity = 0.02f;
            v.present.bloomIntensity = 1.25f;
            v.present.sunIntensity = 0.45f;
        }

        // =================================================================
        // Geometry helpers (shared invisible collider + per-state visuals)
        // =================================================================

        static void Strip(GameObject go)
        {
            var mr = go.GetComponent<MeshRenderer>(); if (mr != null) Object.DestroyImmediate(mr);
            var mf = go.GetComponent<MeshFilter>(); if (mf != null) Object.DestroyImmediate(mf);
        }

        static void StripCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
        }

        /// <summary>Solid in both states: invisible collider in Shared, look per state (null = invisible in that state).</summary>
        static void Solid(string name, Vector3 c, Vector3 s, string pastMat, string presMat, float yaw = 0f)
        {
            var col = Box(name + "_Col", shared, c, s, "concrete", true, yaw);
            Strip(col);
            if (pastMat != null) Box(name, pastLook, c, s, pastMat, false, yaw);
            if (presMat != null) Box(name, presLook, c, s, presMat, false, yaw);
        }

        /// <summary>Purely visual box in one look group.</summary>
        static GameObject Deco(string name, Transform g, Vector3 c, Vector3 s, string mat, float yaw = 0f) => Box(name, g, c, s, mat, false, yaw);

        static void DualRamp(string name, Vector3 a, Vector3 b, float width, string pastMat, string presMat)
        {
            var col = Ramp(name + "_Col", shared, a, b, width, "concrete"); Strip(col);
            StripCollider(Ramp(name, pastLook, a, b, width, pastMat));
            StripCollider(Ramp(name, presLook, a, b, width, presMat));
        }

        static void DualStairs(string name, Vector3 start, float yaw, int steps, float width, string pastMat, string presMat)
        {
            var s = Stairs(name + "_Col", shared, start, yaw, steps, 0.2f, 0.3f, width, "concrete");
            foreach (Transform t in s) Strip(t.gameObject);
            var p = Stairs(name, pastLook, start, yaw, steps, 0.2f, 0.3f, width, pastMat);
            foreach (Transform t in p) StripCollider(t.gameObject);
            var n = Stairs(name, presLook, start, yaw, steps, 0.2f, 0.3f, width, presMat);
            foreach (Transform t in n) StripCollider(t.gameObject);
        }

        /// <summary>Street facade block. faceX = x of the street-facing face; side = -1 (left block, faces +x) or +1 (right block, faces -x).</summary>
        static void Facade(string name, float faceX, int side, float z0, float z1, float height, int seed, string sign = null)
        {
            float depth = 5f;
            float cx = faceX + side * depth * 0.5f;
            float len = z1 - z0;
            float cz = (z0 + z1) * 0.5f;
            var rng = new System.Random(seed);
            string pastWall = rng.NextDouble() < 0.5 ? "plaster_warm" : "brick_warm";
            string presWall = rng.NextDouble() < 0.5 ? "plaster_cold" : "stone_wet";
            Solid(name, new Vector3(cx, height * 0.5f, cz), new Vector3(depth, height, len), pastWall, presWall);
            // Cornice + ground-floor band.
            Deco(name + "_Cornice", pastLook, new Vector3(faceX - side * 0.15f, height + 0.15f, cz), new Vector3(0.6f, 0.3f, len), "stone_warm");
            Deco(name + "_Cornice", presLook, new Vector3(faceX - side * 0.15f, height + 0.15f, cz), new Vector3(0.6f, 0.3f, len), "metal_dark");
            Deco(name + "_Band", pastLook, new Vector3(faceX - side * 0.05f, 3.3f, cz), new Vector3(0.25f, 0.25f, len), "wood_dark");
            Deco(name + "_Band", presLook, new Vector3(faceX - side * 0.05f, 3.3f, cz), new Vector3(0.25f, 0.25f, len), "iron_rust");

            int floors = Mathf.Max(1, Mathf.FloorToInt((height - 4f) / 3f));
            int cols = Mathf.Max(1, Mathf.FloorToInt(len / 2.8f));
            for (int f = 0; f < floors; f++)
            for (int c = 0; c < cols; c++)
            {
                double r = rng.NextDouble();
                if (r < 0.4) continue;
                float z = z0 + (c + 0.5f) * len / cols;
                float y = 5f + f * 3f;
                var pos = new Vector3(faceX - side * 0.04f, y, z);
                Deco("Win", pastLook, pos, new Vector3(0.08f, 1.5f, 1.1f), r < 0.7 ? "window_warm" : "glass");
                Deco("Win", presLook, pos, new Vector3(0.08f, 1.5f, 1.1f), r < 0.55 ? "window_cold" : (r < 0.8 ? "window_warm" : "metal_dark"));
            }
            // Shopfront window at street level.
            Deco(name + "_Shop", pastLook, new Vector3(faceX - side * 0.04f, 1.6f, cz), new Vector3(0.08f, 1.8f, Mathf.Min(4f, len - 2f)), "window_warm");
            Deco(name + "_Shop", presLook, new Vector3(faceX - side * 0.04f, 1.6f, cz), new Vector3(0.08f, 1.8f, Mathf.Min(4f, len - 2f)), rng.NextDouble() < 0.5 ? "window_warm" : "window_cold");
            // Awning.
            var aw = new Vector3(faceX - side * 0.8f, 2.9f, cz);
            Deco(name + "_Awning", pastLook, aw, new Vector3(1.6f, 0.08f, Mathf.Min(5f, len - 1f)), rng.NextDouble() < 0.5 ? "fabric_red" : "fabric_teal");
            Deco(name + "_Awning", presLook, aw, new Vector3(1.6f, 0.08f, Mathf.Min(5f, len - 1f)), "iron_rust");

            if (!string.IsNullOrEmpty(sign))
            {
                float yaw = side < 0 ? -90f : 90f;
                var lp = new Vector3(faceX - side * 0.12f, 3.75f, cz);
                Label(name + "_Sign", pastLook, lp, yaw, sign, 0.38f, new Color(0.25f, 0.15f, 0.05f));
                Deco(name + "_SignBoard", pastLook, lp + new Vector3(side * 0.05f, 0f, 0f), new Vector3(0.05f, 0.6f, sign.Length * 0.24f + 0.6f), "fabric_gold");
                Label(name + "_Sign", presLook, lp, yaw, sign, 0.38f, new Color(1f, 0.85f, 0.6f));
                Deco(name + "_SignBoard", presLook, lp + new Vector3(side * 0.05f, 0f, 0f), new Vector3(0.05f, 0.6f, sign.Length * 0.24f + 0.6f), rng.NextDouble() < 0.5 ? "neon_magenta" : "neon_cyan");
            }
        }

        // =================================================================
        // Terrace (spawn / observation)
        // =================================================================

        static void BuildTerrace()
        {
            Solid("Terrace", new Vector3(0f, 1.5f, -11f), new Vector3(14f, 3f, 10f), "stone_warm", "stone_wet");
            Deco("TerraceTiles", pastLook, new Vector3(0f, 3.01f, -11f), new Vector3(13.6f, 0.02f, 9.6f), "plaster_warm");
            Deco("TerracePuddle", presLook, new Vector3(-2.5f, 3.01f, -10f), new Vector3(3f, 0.02f, 2f), "water");
            Deco("TerracePuddle2", presLook, new Vector3(3f, 3.01f, -13f), new Vector3(2f, 0.02f, 1.4f), "water");
            DualStairs("TerraceStairs", new Vector3(0f, 0f, -1.5f), 180f, 15, 3f, "stone_warm", "stone_wet");

            // Railings (front edge with stair gap, and the sides).
            Solid("RailFrontL", new Vector3(-4.25f, 3.5f, -6.2f), new Vector3(5.5f, 1f, 0.15f), "brass", "metal_dark");
            Solid("RailFrontR", new Vector3(4.25f, 3.5f, -6.2f), new Vector3(5.5f, 1f, 0.15f), "brass", "metal_dark");
            Solid("RailSideL", new Vector3(-6.9f, 3.5f, -11f), new Vector3(0.15f, 1f, 10f), "brass", "metal_dark");
            Solid("RailSideR", new Vector3(6.9f, 3.5f, -11f), new Vector3(0.15f, 1f, 10f), "brass", "metal_dark");
            // Arrival station facade behind.
            Solid("ArrivalHall", new Vector3(0f, 7f, -17f), new Vector3(18f, 14f, 2f), "stone_warm", "concrete");
            Deco("ArrivalArch", pastLook, new Vector3(0f, 4.5f, -15.95f), new Vector3(5f, 3f, 0.1f), "window_warm");
            Deco("ArrivalArch", presLook, new Vector3(0f, 4.5f, -15.95f), new Vector3(5f, 3f, 0.1f), "window_cold");
            Label("ArrivalSign", pastLook, new Vector3(0f, 8f, -15.9f), 180f, "VEYRA ARRIVALS", 0.8f, new Color(0.35f, 0.2f, 0.05f));
            Label("ArrivalSign", presLook, new Vector3(0f, 8f, -15.9f), 180f, "VEYRA ARRIVALS", 0.8f, new Color(0.6f, 0.9f, 1f));
            Deco("ArrivalSignNeon", presLook, new Vector3(0f, 8f, -15.92f), new Vector3(7f, 0.9f, 0.05f), "neon_cyan");

            // Benches and the observation telescope (shape-coded "watch here").
            Solid("Bench1", new Vector3(-4.5f, 3.25f, -9f), new Vector3(2f, 0.5f, 0.6f), "wood", "wood_dark");
            Solid("Bench2", new Vector3(4.5f, 3.25f, -9f), new Vector3(2f, 0.5f, 0.6f), "wood", "wood_dark");
            Cylinder("ScopePost", shared, new Vector3(2.2f, 3.6f, -7f), 0.06f, 1.2f, "brass");
            var scope = Cylinder("Scope", shared, new Vector3(2.2f, 4.25f, -7f), 0.09f, 0.7f, "brass", false);
            scope.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
            Label("ScopeTag", shared, new Vector3(2.2f, 4.7f, -7.2f), 0f, "WATCH THE STREET", 0.22f, Color.white);

            Lamp("TerraceLampL", pastLook, new Vector3(-6.3f, 3f, -7f), "gas", 3.6f);
            Lamp("TerraceLampL", presLook, new Vector3(-6.3f, 3f, -7f), "sodium", 3.6f);
            Ambient("Amb_TerraceWind", shared, new Vector3(0f, 4f, -11f), AmbientKind.Wind, 0.18f, TimeMask.All, 1f, 18f);
        }

        // =================================================================
        // Street
        // =================================================================

        static void BuildStreet()
        {
            // Ground (street + plaza) - shared collider, per-state surface.
            Solid("StreetGround", new Vector3(0f, -0.25f, 22f), new Vector3(16f, 0.5f, 56f), "stone_warm", "stone_wet");
            Deco("Gutter", presLook, new Vector3(-7.2f, 0.01f, 22f), new Vector3(0.6f, 0.02f, 54f), "water");
            Deco("Gutter", presLook, new Vector3(7.2f, 0.01f, 22f), new Vector3(0.6f, 0.02f, 54f), "water");
            var rng = new System.Random(101);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(((float)rng.NextDouble() - 0.5f) * 11f, 0.012f, -3f + i * 5.6f);
                Deco("Puddle", presLook, p, new Vector3(1.2f + (float)rng.NextDouble() * 2.2f, 0.02f, 0.8f + (float)rng.NextDouble() * 1.6f), "water", (float)rng.NextDouble() * 90f);
            }
            // Past: a woven festival runner down the middle.
            Deco("FestivalRunner", pastLook, new Vector3(0f, 0.012f, 21f), new Vector3(2.2f, 0.02f, 52f), "fabric_red");
            Deco("FestivalRunnerTrimL", pastLook, new Vector3(-1.15f, 0.014f, 21f), new Vector3(0.12f, 0.02f, 52f), "fabric_gold");
            Deco("FestivalRunnerTrimR", pastLook, new Vector3(1.15f, 0.014f, 21f), new Vector3(0.12f, 0.02f, 52f), "fabric_gold");

            // Facades. Left (faces +x at x=-8).
            Facade("L_A", -8f, -1, -16f, -6f, 13f, 1);
            Facade("L_B", -8f, -1, -6f, 3f, 11f, 2, "BAKERY");
            Facade("L_C", -8f, -1, 3f, 11f, 14f, 3);
            Facade("L_D", -8f, -1, 11f, 18f, 10f, 4, "POST");
            Facade("L_E", -8f, -1, 18f, 27f, 12f, 5, "BLUE CUPS");
            Facade("L_F", -8f, -1, 27f, 35f, 15f, 6);
            Facade("L_G", -8f, -1, 35f, 42f, 11f, 7, "LANTERNS");
            Facade("L_H", -8f, -1, 42f, 47.5f, 16f, 8);
            // Right (faces -x at x=8), gap for the side lane at z 17..28.
            Facade("R_A", 8f, 1, -16f, -6f, 12f, 11);
            Facade("R_B", 8f, 1, -6f, 3f, 14f, 12, "TEA HOUSE");
            Facade("R_C", 8f, 1, 3f, 10f, 10f, 13);
            Facade("R_D", 8f, 1, 10f, 17f, 13f, 14, "CARTWRIGHT");
            Facade("R_E", 8f, 1, 28f, 35f, 12f, 15);
            Facade("R_F", 8f, 1, 35f, 42f, 15f, 16, "MUSIC");
            Facade("R_G", 8f, 1, 42f, 47.5f, 11f, 17);

            // Street lamps (gas in the Past, sodium in the Present).
            float[] lz = { 2f, 15f, 34f };
            foreach (float z in lz)
            {
                foreach (float x in new[] { -6.9f, 6.9f })
                {
                    if (x > 0 && z > 16f && z < 29f) continue;
                    Lamp("Lamp", pastLook, new Vector3(x, 0f, z), "gas", 4.2f);
                    Lamp("Lamp", presLook, new Vector3(x, 0f, z), "sodium", 4.2f);
                }
            }

            // Past bunting: triangular-ish pennants on strings across the street.
            float[] bz = { 2f, 10f, 18f, 30f, 38f, 45f };
            for (int b = 0; b < bz.Length; b++)
            {
                Deco("BuntingLine", pastLook, new Vector3(0f, 6.2f, bz[b]), new Vector3(16f, 0.03f, 0.03f), "wood_dark");
                for (int i = 0; i < 9; i++)
                {
                    float x = -7f + i * 1.75f;
                    float sag = -0.5f * (1f - Mathf.Pow((x / 8f), 2f));
                    Deco("Pennant", pastLook, new Vector3(x, 5.95f + sag, bz[b]), new Vector3(0.5f, 0.45f, 0.03f), (i + b) % 3 == 0 ? "fabric_gold" : ((i + b) % 3 == 1 ? "fabric_red" : "fabric_teal"), 0f)
                        .transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                }
            }
            // Present: torn, sodden remains of the same bunting + overhead cables.
            for (int b = 0; b < bz.Length; b += 2)
            {
                Deco("Cable", presLook, new Vector3(0f, 6.4f, bz[b]), new Vector3(16f, 0.04f, 0.04f), "metal_dark");
                for (int i = 1; i < 9; i += 3)
                    Deco("TornPennant", presLook, new Vector3(-7f + i * 1.75f, 6.05f, bz[b]), new Vector3(0.35f, 0.6f, 0.02f), "fabric_teal");
            }

            // Market stalls (shared bodies, per-state canopies).
            Stall("StallBread", new Vector3(5.6f, 0f, 4f), "fabric_gold", "Bread");
            Stall("StallRibbons", new Vector3(5.6f, 0f, 34f), "fabric_red", "Ribbons");
            Stall("StallMasks", new Vector3(-5.6f, 0f, 38f), "fabric_teal", "Masks");

            // Postbox (where the courier posts the letter).
            Solid("Postbox", new Vector3(-7.3f, 0.65f, 12f), new Vector3(0.6f, 1.3f, 0.6f), "fabric_red", "iron_rust");
            Deco("PostboxCap", pastLook, new Vector3(-7.3f, 1.33f, 12f), new Vector3(0.7f, 0.06f, 0.7f), "brass");
            Deco("PostboxCap", presLook, new Vector3(-7.3f, 1.33f, 12f), new Vector3(0.7f, 0.06f, 0.7f), "metal_dark");
            Label("PostboxTag", shared, new Vector3(-6.95f, 0.95f, 12f), -90f, "POST", 0.2f, Color.white);

            ReflectionProbe(shared, new Vector3(0f, 5f, 18f), new Vector3(18f, 14f, 64f));

            // Ambience.
            Ambient("Amb_RainStreet", shared, new Vector3(0f, 3f, 12f), AmbientKind.Rain, 0.4f, TimeMask.Present, 0.6f, 45f);
            Ambient("Amb_RainStreet2", shared, new Vector3(0f, 3f, 36f), AmbientKind.Rain, 0.4f, TimeMask.Present, 0.6f, 45f);
            Ambient("Amb_CrowdPast", shared, new Vector3(0f, 2f, 20f), AmbientKind.Crowd, 0.5f, TimeMask.Past, 0.5f, 45f);
            Ambient("Amb_CrowdPresent", shared, new Vector3(0f, 2f, 24f), AmbientKind.Crowd, 0.18f, TimeMask.Present, 0.8f, 25f);
        }

        static void Stall(string name, Vector3 g, string canopyPast, string goods)
        {
            Solid(name + "_Table", g + new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 2.8f), "wood", "wood_dark");
            foreach (var o in new[] { new Vector3(-0.55f, 0f, -1.35f), new Vector3(0.55f, 0f, -1.35f), new Vector3(-0.55f, 0f, 1.35f), new Vector3(0.55f, 0f, 1.35f) })
                Solid(name + "_Post", g + o + new Vector3(0f, 1.25f, 0f), new Vector3(0.08f, 2.5f, 0.08f), "wood_dark", "metal_dark");
            Deco(name + "_Canopy", pastLook, g + new Vector3(0f, 2.55f, 0f), new Vector3(1.6f, 0.08f, 3.2f), canopyPast);
            Deco(name + "_Canopy", presLook, g + new Vector3(0f, 2.55f, 0f), new Vector3(1.6f, 0.08f, 3.2f), "fabric_teal");
            Deco(name + "_Goods", pastLook, g + new Vector3(0f, 1.0f, 0f), new Vector3(0.8f, 0.2f, 2.2f), "fabric_gold");
            Deco(name + "_Goods", presLook, g + new Vector3(0f, 1.0f, 0f), new Vector3(0.8f, 0.2f, 1.2f), "paper");
            Label(name + "_Tag", shared, g + new Vector3(0f, 2.2f, -1.62f), 0f, goods, 0.25f, Color.white);
        }

        // =================================================================
        // Side lane + cart landing + alcove
        // =================================================================

        static void BuildLane()
        {
            // Landing block (top 2.5) and ramp from the street.
            Solid("Landing", new Vector3(17f, 1.25f, 22.5f), new Vector3(6f, 2.5f, 11f), "stone_warm", "stone_wet");
            DualRamp("LaneRamp", new Vector3(8f, 0f, 22.5f), new Vector3(14f, 2.5f, 22.5f), 11f, "stone_warm", "stone_wet");
            // Walls.
            Solid("LaneWallS", new Vector3(16.5f, 4f, 16.75f), new Vector3(7f, 8f, 0.5f), "brick_warm", "plaster_cold");
            // East wall with a doorway (z 18..20, y 2.5..5.1) into the workshop / backstage side area.
            Solid("LaneWallE_S", new Vector3(20.25f, 4f, 17.25f), new Vector3(0.5f, 8f, 1.5f), "brick_warm", "plaster_cold");
            Solid("LaneWallE_N", new Vector3(20.25f, 4f, 24.25f), new Vector3(0.5f, 8f, 8.5f), "brick_warm", "plaster_cold");
            Solid("LaneWallE_Top", new Vector3(20.25f, 6.55f, 19f), new Vector3(0.5f, 2.9f, 2f), "brick_warm", "plaster_cold");
            Solid("LaneWallN_W", new Vector3(14.25f, 4f, 28.25f), new Vector3(2.5f, 8f, 0.5f), "brick_warm", "plaster_cold");
            Solid("LaneWallN_E", new Vector3(19.35f, 4f, 28.25f), new Vector3(1.7f, 8f, 0.5f), "brick_warm", "plaster_cold");
            Solid("LaneWallN_Top", new Vector3(17f, 6.6f, 28.25f), new Vector3(3f, 2.8f, 0.5f), "brick_warm", "plaster_cold");
            // Alcove (cart bay): floor block, side walls, back, roof.
            Solid("BayFloor", new Vector3(17f, 1.25f, 29.9f), new Vector3(3f, 2.5f, 3.3f), "stone_warm", "stone_wet");
            Solid("BayWallW", new Vector3(15.3f, 4f, 29.9f), new Vector3(0.4f, 8f, 3.3f), "brick_warm", "plaster_cold");
            Solid("BayWallE", new Vector3(18.7f, 4f, 29.9f), new Vector3(0.4f, 8f, 3.3f), "brick_warm", "plaster_cold");
            Solid("BayBack", new Vector3(17f, 4f, 31.7f), new Vector3(3.8f, 8f, 0.4f), "brick_warm", "plaster_cold");
            Label("BaySign", shared, new Vector3(17f, 5.6f, 28.0f), 0f, "CART BAY", 0.45f, Color.white);
            Deco("BaySignBoard", pastLook, new Vector3(17f, 5.6f, 28.02f), new Vector3(2.6f, 0.7f, 0.04f), "wood_dark");
            Deco("BaySignBoard", presLook, new Vector3(17f, 5.6f, 28.02f), new Vector3(2.6f, 0.7f, 0.04f), "metal_dark");
            // Painted chevrons on the landing pointing into the bay (shape, not colour).
            for (int i = 0; i < 3; i++)
                Deco("Chevron", pastLook, new Vector3(17f, 2.515f, 24.6f + i * 1.1f), new Vector3(0.9f, 0.02f, 0.25f), "fabric_gold", 0f).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            Label("SlopeWarn", pastLook, new Vector3(14.3f, 3.6f, 17.1f), 180f, "STEEP SLOPE - CHOCK ALL CARTS", 0.22f, new Color(0.3f, 0.15f, 0.05f));
            Label("SlopeWarn", presLook, new Vector3(14.3f, 3.6f, 17.1f), 180f, "STEEP SLOPE", 0.22f, new Color(1f, 0.8f, 0.5f));

            Lamp("LaneLamp", pastLook, new Vector3(19.6f, 2.5f, 18f), "gas", 3.4f);
            Lamp("LaneLamp", presLook, new Vector3(19.6f, 2.5f, 18f), "sodium", 3.4f);
            PointLight("BayGlow", presLook, new Vector3(17f, 4.5f, 30f), Cold, 1.2f, 6f);
            Particles("LaneSteam", presLook, new Vector3(19.5f, 2.6f, 26.5f), new Vector3(0.5f, 0.1f, 0.5f), "steam");
        }

        // =================================================================
        // Gate, plaza, tower
        // =================================================================

        static void BuildGateAndPlaza(Transform logic)
        {
            // Gate pillars + lintel.
            Solid("GatePillarL", new Vector3(-5.5f, 3f, 48f), new Vector3(5f, 6f, 1f), "stone_warm", "concrete");
            Solid("GatePillarR", new Vector3(5.5f, 3f, 48f), new Vector3(5f, 6f, 1f), "stone_warm", "concrete");
            Solid("GateLintel", new Vector3(0f, 5.4f, 48f), new Vector3(6f, 1.2f, 1f), "stone_warm", "concrete");
            Label("GateSign", shared, new Vector3(0f, 5.4f, 47.45f), 0f, "FESTIVAL GATE", 0.5f, Color.white);
            Label("GateSign2", presLook, new Vector3(0f, 3.6f, 47.65f), 0f, "CLOSED UNTIL THE BELL IS QUIET", 0.22f, new Color(1f, 0.85f, 0.6f));
            var gate = Door("FestivalGate", shared, new Vector3(0f, 0f, 48f), new Vector3(6f, 4.8f, 0.3f), "iron_rust", new Vector3(0f, 4.4f, 0f), 0f, F_CupSaved);
            gate.toast = "The festival gate grinds open.";

            // Plaza ground and walls.
            Solid("PlazaGround", new Vector3(0f, -0.25f, 57f), new Vector3(30f, 0.5f, 18f), "stone_warm", "stone_wet");
            Deco("PlazaMosaic", pastLook, new Vector3(0f, 0.012f, 55f), new Vector3(7f, 0.02f, 7f), "brass", 45f);
            Deco("PlazaPool", presLook, new Vector3(0f, 0.012f, 55f), new Vector3(7f, 0.02f, 7f), "water", 45f);
            Label("PlazaZero", shared, new Vector3(0f, 0.05f, 55f), 0f, "0", 3f, new Color(0.9f, 0.95f, 1f, 0.6f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Solid("PlazaWallL", new Vector3(-12f, 6f, 57f), new Vector3(6f, 12f, 16f), "plaster_warm", "plaster_cold");
            Solid("PlazaWallR", new Vector3(12f, 6f, 57f), new Vector3(6f, 12f, 16f), "brick_warm", "stone_wet");
            Solid("PlazaFillL", new Vector3(-11.5f, 6f, 48f), new Vector3(7f, 12f, 1f), "plaster_warm", "plaster_cold");
            Solid("PlazaFillR", new Vector3(11.5f, 6f, 48f), new Vector3(7f, 12f, 1f), "brick_warm", "stone_wet");

            Solid("PlazaBackL", new Vector3(-8.5f, 6f, 68f), new Vector3(3f, 12f, 8f), "plaster_warm", "plaster_cold");
            Solid("PlazaBackR", new Vector3(8.5f, 6f, 68f), new Vector3(3f, 12f, 8f), "brick_warm", "stone_wet");

            // Clock tower: body, front walls with door, vestibule.
            Solid("TowerBody", new Vector3(0f, 22f, 69.5f), new Vector3(14f, 44f, 5f), "stone_warm", "stone_wet");
            Solid("TowerFrontL", new Vector3(-4.1f, 22f, 62.5f), new Vector3(5.8f, 44f, 1f), "stone_warm", "stone_wet");
            Solid("TowerFrontR", new Vector3(4.1f, 22f, 62.5f), new Vector3(5.8f, 44f, 1f), "stone_warm", "stone_wet");
            Solid("TowerFrontTop", new Vector3(0f, 23.7f, 62.5f), new Vector3(2.4f, 40.6f, 1f), "stone_warm", "stone_wet");
            Solid("VestibuleL", new Vector3(-4.5f, 22f, 65f), new Vector3(5f, 44f, 4f), "stone_warm", "stone_wet");
            Solid("VestibuleR", new Vector3(4.5f, 22f, 65f), new Vector3(5f, 44f, 4f), "stone_warm", "stone_wet");
            Solid("VestibuleRoof", new Vector3(0f, 24f, 65f), new Vector3(4f, 40f, 4f), "stone_warm", "stone_wet");
            Solid("VestibuleFloor", new Vector3(0f, -0.25f, 65f), new Vector3(4f, 0.5f, 4f), "brass", "metal_dark");
            PointLight("ClockworksGlow", shared, new Vector3(0f, 2.6f, 65.5f), new Color(1f, 0.7f, 0.35f), 2.2f, 5f);
            Ambient("Amb_Clockworks", shared, new Vector3(0f, 2f, 65f), AmbientKind.Machine, 0.35f, TimeMask.All, 1f, 16f);
            // Door with the seal.
            var td = Door("TowerDoor", shared, new Vector3(0f, 0f, 62.3f), new Vector3(2.4f, 3.4f, 0.3f), "wood_dark", new Vector3(2.5f, 0f, 0f), 0f, F_LetterTruth);
            td.toast = "The seal (0) on the tower door unlocks.";
            var seal = Label("DoorSeal", shared, new Vector3(0f, 2.2f, 62.1f), 0f, "(0)", 0.9f, new Color(1f, 0.85f, 0.5f));
            td.disableWhenTrue.Add(seal);
            Label("DoorSealNote", shared, new Vector3(0f, 3.9f, 61.95f), 0f, "FOUNDERS' CLOCKWORKS", 0.28f, Color.white);

            // Clock face (shape + glyph).
            var face = Cylinder("ClockFace", shared, new Vector3(0f, 32f, 61.9f), 4f, 0.2f, "paper", false);
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var faceGlow = Cylinder("ClockGlow", presLook, new Vector3(0f, 32f, 61.72f), 3.7f, 0.06f, "lamp_cold", false);
            faceGlow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var faceGold = Cylinder("ClockRing", pastLook, new Vector3(0f, 32f, 61.95f), 4.3f, 0.12f, "brass", false);
            faceGold.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("ClockZero", shared, new Vector3(0f, 35f, 61.6f), 0f, "0", 1.6f, new Color(0.1f, 0.1f, 0.12f));
            Box("HourHand", shared, new Vector3(0f, 33.2f, 61.55f), new Vector3(0.25f, 2.4f, 0.08f), "metal_dark", false);
            var hand2 = Box("MinuteHand", shared, new Vector3(1.6f, 32f, 61.5f), new Vector3(3.2f, 0.18f, 0.08f), "metal_dark", false);
            hand2.transform.RotateAround(new Vector3(0f, 32f, 61.5f), Vector3.forward, -2f);
            // Belfry openings + bell.
            Deco("Belfry", presLook, new Vector3(0f, 39.5f, 61.95f), new Vector3(5f, 3f, 0.1f), "window_cold");
            Deco("Belfry", pastLook, new Vector3(0f, 39.5f, 61.95f), new Vector3(5f, 3f, 0.1f), "window_warm");
            SpotLight("TowerWash", presLook, new Vector3(0f, 1f, 54f), new Vector3(-62f, 0f, 0f), new Color(0.6f, 0.8f, 1f), 9f, 50f, 30f, false);
            SpotLight("TowerWash", pastLook, new Vector3(0f, 1f, 54f), new Vector3(-62f, 0f, 0f), Warm, 5f, 50f, 30f, false);
            PointLight("BelfryLight", shared, new Vector3(0f, 39f, 60.5f), Warm, 3f, 10f);
            Cylinder("SpireTop", shared, new Vector3(0f, 47f, 67f), 2.5f, 6f, "copper_verdigris", false);

            // Plaza lamps (always) - sealed plaza is dark until changed.
            Lamp("PlazaLamp", pastLook, new Vector3(-7f, 0f, 58f), "gas", 4.5f);
            Lamp("PlazaLamp", presLook, new Vector3(-7f, 0f, 58f), "sodium", 4.5f);
        }

        // =================================================================
        // Skyline
        // =================================================================

        static void BuildSkyline()
        {
            var sky = Group("Skyline", shared);
            var rng = new System.Random(7);
            for (int i = 0; i < 3; i++)
            {
                float z = -6f + i * 26f;
                float h1 = 18f + (float)rng.NextDouble() * 18f;
                float h2 = 16f + (float)rng.NextDouble() * 20f;
                Building("SkyL" + i, sky, new Vector3(-21f - (float)rng.NextDouble() * 6f, 0f, z), new Vector3(9f, h1, 10f), i % 2 == 0 ? "plaster_cold" : "brick_warm", "window_warm", "metal_dark", 30 + i, i == 2 ? "HOTEL" : null, "neon_magenta");
                Building("SkyR" + i, sky, new Vector3(37f + (float)rng.NextDouble() * 5f, 0f, z), new Vector3(9f, h2, 10f), i % 2 == 1 ? "plaster_cold" : "stone_wet", "window_cold", "metal_dark", 60 + i, i == 4 ? "THEATRE" : null, "neon_cyan");
            }
            // Distant towers behind the clock tower.
        }

        // =================================================================
        // Atmosphere
        // =================================================================

        static void BuildAtmosphere()
        {
            Particles("Rain", presLook, new Vector3(0f, 20f, 22f), new Vector3(26f, 1f, 64f), "rain").gameObject.name = "Rain_Main";
            Particles("RainPlaza", presLook, new Vector3(0f, 20f, 56f), new Vector3(26f, 1f, 16f), "rain");
            Particles("Mist", presLook, new Vector3(0f, 1f, 22f), new Vector3(14f, 1f, 50f), "mist");
            Particles("Dust", pastLook, new Vector3(0f, 3f, 14f), new Vector3(14f, 4f, 30f), "dust");
            Particles("Dust2", pastLook, new Vector3(0f, 3f, 38f), new Vector3(14f, 4f, 20f), "dust");
            Particles("DustTerrace", pastLook, new Vector3(0f, 5f, -11f), new Vector3(12f, 3f, 8f), "dust");
        }
    }
}
