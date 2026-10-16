using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 03 - The Rain Archive (Municipal Records Hall).
    /// Evidence triangulation (Past drawer / Present transit stamp / Future mineral traces) plus
    /// Present water routing that raises a hologram map of the subsurface tram route.
    /// Layout (+Z = north): foyer z -10..0, grand hall z 0..48 (x -18..18), floor hatch at z 40..44.5,
    /// service stair down to the Line 4 tram tunnel (y -3, z 44..60) where the exit is.
    /// </summary>
    public static partial class Level03Builder
    {
        static List<RotatableElement> junctionList;
        static Transform recGroup;
        static readonly Color Mint = new Color(0.6f, 1f, 0.85f);
        static readonly Color Warm = new Color(1f, 0.78f, 0.48f);
        static readonly Color Cold = new Color(0.62f, 0.8f, 1f);
        static readonly Color Ink = new Color(0.15f, 0.1f, 0.06f);

        public static void Build(Transform root)
        {
            var d = Systems(root, 2, TimeMask.Past | TimeMask.Present, TimeState.Present, out var visuals);
            TuneVisuals(visuals);

            d.storySetup = "The Municipal Records Hall lists one resident, No. 0417, as both alive and dead. Three states, three kinds of evidence: ink in the Past, rain in the Present, stone in the Future.";
            d.interludeTitle = "Pell's Route";
            d.interludeLines.Add(L("Pell Ashgrove", "Records Hall's just past the fountain. I'll walk you. Couriers know the dry way.", 4f));
            d.interludeLines.Add(L("Arin", "You keep looking at me like you're checking a delivery.", 3.5f));
            d.interludeLines.Add(L("Pell Ashgrove", "Because I am. That letter in your coat. I've handed it to you before.", 4f));
            d.interludeLines.Add(L("Pell Ashgrove", "More times than there are days, Arin. Same seal, same date. Tomorrow.", 4.5f));
            d.interludeLines.Add(L("Arin", "And every time I say I don't remember you.", 3.5f));
            d.interludeLines.Add(L("Pell Ashgrove", "Every time. But this time you asked where the records are kept. That's new.", 4.5f));
            d.introLines.Add(L("Arin", "The Records Hall. Rain still finds its way in, even through the stone."));
            d.introLines.Add(L("Arin", "The flooded register on the desk. Someone here is filed twice: once living, once dead."));
            d.introLines.Add(L("Arin", "If the Present has smudged it, the Past still has it clean. [1] for Past, [2] for Present."));

            AddObjective(d, "Read the flooded register in the foyer", "L03.ledgerRead", "The open ledger sits on the reception desk in the foyer.");
            AddObjective(d, "Find resident 0417 in the Past card drawers", "L03.nameFoundPast", "Shift to the PAST [1]. The index board by the west cabinet says which drawer holds the 0400s.");
            AddObjective(d, "Match the Present transit stamp on the damp ticket", "L03.stampMatched", "Read the ticket in the lost-property tray at the east transit desk, then pick the stamp with the same mark.");
            AddObjective(d, "Reconstruct the deleted name from Future mineral traces", "L03.nameReconstructed", "In the FUTURE [3], four glowing mineral fragments sit on the collapsed shelves. Trace all four.");
            AddObjective(d, "Route the rain channels to douse the burning stacks", "L03.waterRouted", "The Past drainage plan on the lectern: junction I east, II north, III east, IV north. Each routed channel puts out the aisle beside it.");
            AddObjective(d, "Compare the clue cards and pick Maren's real record", "L03.recordPicked", "Read the three clue cards west of the basin, then study the five records on the board east of it. Age, street, transit mark and spelling must all match.");
            AddObjective(d, "Read Maren's record", "L03.payoff", "It lies on the record board beside the basin.");
            AddObjective(d, "Face Mother Ondine", "L03.ondineDone", "She is waiting at the basin.");
            AddObjective(d, "Reach Line 4 under the floor hatch", "L03.exit", "If the Keepers sealed the hatch, its release valve is in Odile's Restricted Stacks behind the east door. Mind the collapsing shelves.");

            d.payoffFlag = "L03.payoff";
            d.payoffClueTitle = "A record in my own hand";
            d.payoffClueText = "Death certificate, resident 0417, MAREN OSKEL, aged 7. Cause: 'the second bell'. Filed by Odile Fenwick, then hidden in the survey basin so no clerk could file her twice. Countersigned: ARIN. It is my handwriting. I have never seen this paper before.";

            Player(root, new Vector3(0f, 0.05f, -8f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0f, 0f, -8f), new Vector3(6f, 3f, 3f), 0f, true);
            Checkpoint("CP_Hall", root, new Vector3(0f, 0f, 3f), new Vector3(5f, 3f, 2f), 0f);
            Checkpoint("CP_North", root, new Vector3(0f, 0f, 30f), new Vector3(6f, 3f, 3f), 0f);

            var shared = Group("Shared", root);
            var logic = Group("Logic", root);
            var past = StateGroup("Past", root, TimeMask.Past, false);
            var present = StateGroup("Present", root, TimeMask.Present, false);
            var future = StateGroup("Future", root, TimeMask.Future, false);
            var pastUpper = StateGroup("PastPresentShell", root, TimeMask.Past | TimeMask.Present, false);

            BuildShell(shared, past, present, future, pastUpper);
            BuildShelves(shared, past, present, future);
            BuildFoyer(shared, root);
            BuildPastEvidence(root, past);
            BuildPresentEvidence(root, logic);
            BuildFutureEvidence(root, logic);
            BuildWater(root, shared, logic);
            BuildHatchAndTunnel(shared, logic, root);
            BuildNpcs(root);
            BuildRestrictedStacks(root, shared, logic);
            BuildStoryExtras(root, logic);
            BuildV2(root, shared, logic);
            BuildAtmosphere(shared, past, present, future);
        }

        // =====================================================================
        static void TuneVisuals(TemporalVisuals v)
        {
            v.past.fogDensity = 0.008f; v.past.sunIntensity = 1.3f; v.past.bloomIntensity = 0.9f;
            v.past.sunEuler = new Vector3(62f, -30f, 0f);
            v.present.fogDensity = 0.018f; v.present.sunIntensity = 0.35f; v.present.bloomIntensity = 1.2f;
            v.present.ambientSky = new Color(0.22f, 0.3f, 0.38f);
            v.future.fogDensity = 0.016f; v.future.bloomIntensity = 2.4f;
        }

        // =====================================================================
        // Shell: floors, walls, roof per state
        // =====================================================================
        static readonly (Vector3 c, Vector2 s)[] hallFloor =
        {
            (new Vector3(0f, 0f, 20f), new Vector2(36f, 40f)),
            (new Vector3(-9.75f, 0f, 44f), new Vector2(16.5f, 8f)),
            (new Vector3(9.75f, 0f, 44f), new Vector2(16.5f, 8f)),
            (new Vector3(0f, 0f, 46.25f), new Vector2(3f, 3.5f)),
        };

        static void Overlay(Transform g, string mat)
        {
            foreach (var p in hallFloor)
                Box("FloorSkin", g, p.c + Vector3.up * 0.01f, new Vector3(p.s.x, 0.02f, p.s.y), mat, false);
        }

        static void BuildShell(Transform shared, Transform past, Transform present, Transform future, Transform pp)
        {
            var g = Group("Hall", shared);
            foreach (var p in hallFloor) Floor("HallFloor", g, p.c, p.s, "stone_warm");

            Overlay(Group("PastParquet", past), "wood");
            Overlay(Group("PresentWetStone", present), "stone_wet");
            Overlay(Group("FutureObsidian", future), "obsidian");

            // Lower walls (all states) 5 m tall.
            WallWithDoor("SouthWall", g, new Vector3(-18f, 0f, 0f), new Vector3(18f, 0f, 0f), 5f, 0.6f, "stone_warm", 4f, 4f);
            Wall("WestWall", g, new Vector3(-18f, 0f, 0f), new Vector3(-18f, 0f, 48f), 5f, 0.6f, "stone_warm");
            WallWithDoor("EastWall", g, new Vector3(18f, 0f, 0f), new Vector3(18f, 0f, 48f), 5f, 0.6f, "stone_warm", 2f, 3f, 39f / 48f);
            Wall("NorthWall", g, new Vector3(-18f, 0f, 48f), new Vector3(18f, 0f, 48f), 5f, 0.6f, "stone_warm");

            // Columns along the nave (shared, brass caps in Past).
            for (int i = 0; i < 4; i++)
            {
                float z = 6f + i * 10f;
                foreach (float x in new[] { -7f, 7f })
                {
                    Cylinder("Column", g, new Vector3(x, 7f, z), 0.45f, 14f, "stone_warm");
                    Box("Plinth", g, new Vector3(x, 0.3f, z), new Vector3(1.3f, 0.6f, 1.3f), "stone_warm");
                }
            }

            // Upper walls + roof: Past (warm plaster, glazed skylights) and Present (cold plaster, broken skylights).
            foreach (var (grp, wallMat, glass) in new[] { (Group("PastUpper", past), "plaster_warm", true), (Group("PresentUpper", present), "plaster_cold", false) })
            {
                Wall("UpS", grp, new Vector3(-18f, 5f, 0f), new Vector3(18f, 5f, 0f), 9f, 0.6f, wallMat);
                Wall("UpW", grp, new Vector3(-18f, 5f, 0f), new Vector3(-18f, 5f, 48f), 9f, 0.6f, wallMat);
                Wall("UpE", grp, new Vector3(18f, 5f, 0f), new Vector3(18f, 5f, 48f), 9f, 0.6f, wallMat);
                Wall("UpN", grp, new Vector3(-18f, 5f, 48f), new Vector3(18f, 5f, 48f), 9f, 0.6f, wallMat);
                for (int i = 0; i < 5; i++)
                    Box("RoofStrip", grp, new Vector3(0f, 14.2f, 4f + i * 10f), new Vector3(36.6f, 0.4f, 8f), glass ? "wood_dark" : "metal_dark");
                for (int i = 0; i < 4; i++)
                {
                    float z = 9f + i * 10f;
                    if (glass) Box("Skylight", grp, new Vector3(0f, 14.2f, z), new Vector3(36f, 0.08f, 2f), "glass", false);
                    else
                    {
                        // Broken panes: only fragments remain.
                        Box("SkylightShard", grp, new Vector3(-10f, 14.2f, z), new Vector3(9f, 0.08f, 2f), "glass", false);
                        Box("SkylightShard", grp, new Vector3(9f, 14.2f, z), new Vector3(7f, 0.08f, 2f), "glass", false);
                    }
                    Box("SkylightBeam", grp, new Vector3(0f, 13.9f, z), new Vector3(36f, 0.25f, 0.2f), glass ? "brass" : "iron_rust", false);
                }
                // Cornice
                Box("Cornice", grp, new Vector3(0f, 5.1f, 0.45f), new Vector3(35f, 0.3f, 0.4f), glass ? "brass" : "iron_rust", false);
            }

            // Future: the roof is gone, upper walls are jagged teeth with neon seams.
            var fz = Group("FutureRuin", future);
            var rng = new System.Random(303);
            for (int i = 0; i < 18; i++)
            {
                float h = 1f + (float)rng.NextDouble() * 6f;
                float t = i / 17f;
                Box("Tooth", fz, new Vector3(-18f, 5f + h * 0.5f, t * 48f), new Vector3(0.6f, h, 1.8f), "ruin_dark");
                Box("Tooth", fz, new Vector3(18f, 5f + h * 0.7f, (1f - t) * 48f), new Vector3(0.6f, h * 1.4f, 1.6f), "ruin_dark");
                if (i % 3 == 0) Box("Seam", fz, new Vector3(-17.6f, 5f + h * 0.5f, t * 48f), new Vector3(0.05f, h * 0.9f, 0.08f), i % 2 == 0 ? "neon_cyan" : "neon_magenta", false);
            }
            for (int i = 0; i < 14; i++)
            {
                float x = -16f + i * 2.4f;
                float h = 1f + (float)rng.NextDouble() * 4f;
                Box("ToothN", fz, new Vector3(x, 5f + h * 0.5f, 48f), new Vector3(2f, h, 0.6f), "ruin_dark");
            }
            Box("NeonRimW", fz, new Vector3(-17.65f, 5.02f, 24f), new Vector3(0.06f, 0.06f, 48f), "neon_cyan", false);
            Box("NeonRimE", fz, new Vector3(17.65f, 5.02f, 24f), new Vector3(0.06f, 0.06f, 48f), "neon_magenta", false);

            // Collapsed roof slabs on the floor (Future obstacles that leave clear paths).
            Rubble(fz, new Vector3(-4f, 0f, 20f), 1, rng);
            Rubble(fz, new Vector3(5f, 0f, 9f), 1, rng);
            Rubble(fz, new Vector3(11f, 0f, 33f), 1, rng);
            // Floating fractured debris + crystals.
            for (int i = 0; i < 12; i++)
            {
                var p = new Vector3(-14f + (float)rng.NextDouble() * 28f, 7f + (float)rng.NextDouble() * 8f, 4f + (float)rng.NextDouble() * 40f);
                var deb = Box("FloatingDebris", fz, p, new Vector3(0.8f + (float)rng.NextDouble() * 2f, 0.3f + (float)rng.NextDouble() * 0.5f, 0.6f + (float)rng.NextDouble() * 1.5f), "ruin_dark", false);
                Dyn(deb);
                deb.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 40f);
                var fb = deb.AddComponent<FloatBob>(); fb.amplitude = 0.25f; fb.frequency = 0.1f + (float)rng.NextDouble() * 0.15f; fb.drift = new Vector3(0f, 4f, 2f);
                if (i % 2 == 0) Box("Glow", deb.transform, p + Vector3.down * 0.2f, new Vector3(0.05f, 0.05f, 0.6f), "neon_violet", false);
            }
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector3(i % 2 == 0 ? -16.6f : 16.6f, 0f, 3f + i * 4.4f);
                Crystal(fz, p, 0.6f + (float)rng.NextDouble() * 1.4f, rng);
            }
            // Future skyline beyond the broken walls.
            var sky = Group("FutureSkyline", future);
            SkyTower("TowerW1", sky, new Vector3(-40f, 0f, 10f), new Vector3(12f, 38f, 12f), "obsidian", "neon_cyan", "neon_magenta", 31);
            SkyTower("TowerW2", sky, new Vector3(-36f, 0f, 40f), new Vector3(10f, 26f, 14f), "obsidian", "neon_violet", "neon_cyan", 32);
            SkyTower("TowerE1", sky, new Vector3(40f, 0f, 16f), new Vector3(14f, 44f, 12f), "obsidian", "neon_magenta", "neon_cyan", 33);
            SkyTower("TowerE2", sky, new Vector3(34f, 0f, 46f), new Vector3(10f, 30f, 10f), "obsidian", "neon_cyan", "neon_violet", 34);
            SkyTower("TowerN", sky, new Vector3(0f, 0f, 74f), new Vector3(30f, 52f, 12f), "obsidian", "neon_cyan", "neon_magenta", 35);
            Sign(sky, new Vector3(0f, 34f, 67.6f), 0f, "VEYRA CIVIL MEMORY", 3f, new Color(0.4f, 1f, 1f), "neon_cyan");
            Sign(sky, new Vector3(32.9f, 22f, 16f), 90f, "ARCHIVE 0", 2.4f, new Color(1f, 0.5f, 0.9f), "neon_magenta");
            Hologram("SkyRing", sky, new Vector3(0f, 26f, 30f), new Vector3(16f, 0.05f, 16f), PrimitiveType.Cylinder, "hologram_magenta", true, true);
        }

        static void Rubble(Transform g, Vector3 c, int n, System.Random rng)
        {
            var a = Box("FallenSlab", g, c + new Vector3(0f, 0.7f, 0f), new Vector3(4.5f, 0.6f, 2.5f), "ruin_dark");
            a.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 14f);
            var b = Box("Chunk", g, c + new Vector3(1.6f, 0.4f, 1.4f), new Vector3(1.1f, 0.8f, 1f), "ruin_dark");
            b.transform.rotation = Quaternion.Euler(10f, 30f, 6f);
            Box("SlabSeam", g, c + new Vector3(0f, 1.05f, 0f), new Vector3(4f, 0.04f, 0.06f), "neon_magenta", false).transform.rotation = a.transform.rotation;
        }

        static void Crystal(Transform g, Vector3 ground, float size, System.Random rng)
        {
            for (int k = 0; k < 3; k++)
            {
                var c = Box("Crystal", g, ground + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f, size * 0.5f, ((float)rng.NextDouble() - 0.5f) * 0.6f),
                    new Vector3(0.25f * size, size * (0.7f + 0.3f * k), 0.25f * size), "crystal", false);
                c.transform.rotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 40f, (float)rng.NextDouble() * 180f, ((float)rng.NextDouble() - 0.5f) * 40f);
            }
        }

        // =====================================================================
        // Shelves
        // =====================================================================
        static readonly (Vector3 c, Vector3 s)[] shelves =
        {
            (new Vector3(-12.5f, 0f, 10f), new Vector3(1.4f, 6f, 6f)),
            (new Vector3(-12.5f, 0f, 20f), new Vector3(1.4f, 6f, 6f)),
            (new Vector3(12.5f, 0f, 10f), new Vector3(1.4f, 6f, 6f)),
            (new Vector3(12.5f, 0f, 20f), new Vector3(1.4f, 6f, 6f)),
            (new Vector3(-11f, 0f, 42f), new Vector3(8f, 6f, 1.2f)),
            (new Vector3(11f, 0f, 42f), new Vector3(8f, 6f, 1.2f)),
        };

        static void BuildShelves(Transform shared, Transform past, Transform present, Transform future)
        {
            var g = Group("Shelves", shared);
            var pb = Group("PastBooks", past);
            var sb = Group("PresentSoggy", present);
            var fc = Group("FutureCrust", future);
            string[] bookMats = { "fabric_red", "fabric_teal", "fabric_gold", "wood", "paper" };
            var rng = new System.Random(77);
            int s = 0;
            foreach (var sh in shelves)
            {
                bool alongZ = sh.s.z > sh.s.x;
                Box("ShelfCase", g, sh.c + Vector3.up * sh.s.y * 0.5f, new Vector3(alongZ ? 0.5f : sh.s.x, sh.s.y, alongZ ? sh.s.z : 0.5f), "wood_dark");
                Box("ShelfTop", g, sh.c + Vector3.up * (sh.s.y + 0.1f), new Vector3(sh.s.x + 0.2f, 0.2f, sh.s.z + 0.2f), "wood_dark");
                for (int lvl = 0; lvl < 4; lvl++)
                {
                    float y = 0.3f + lvl * 1.4f;
                    Box("Board", g, sh.c + Vector3.up * y, new Vector3(sh.s.x, 0.06f, sh.s.z), "wood_dark", false);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector3 off = alongZ ? new Vector3(side * 0.45f, 0f, 0f) : new Vector3(0f, 0f, side * 0.45f);
                        Vector3 rowSize = alongZ ? new Vector3(0.35f, 0.42f, sh.s.z - 0.3f) : new Vector3(sh.s.x - 0.3f, 0.42f, 0.35f);
                        Vector3 pos = sh.c + Vector3.up * (y + 0.24f) + off;
                        Box("Books", pb, pos, rowSize, bookMats[(s + lvl) % bookMats.Length], false);
                        if ((lvl + s) % 2 == 0) Box("WetPulp", sb, pos + Vector3.down * 0.08f, Vector3.Scale(rowSize, new Vector3(1f, 0.6f, 1f)), "plaster_cold", false);
                        Box("MineralCrust", fc, pos, Vector3.Scale(rowSize, new Vector3(1.1f, 0.85f, 1f)), lvl == 2 && s % 2 == 0 ? "mineral_trace" : "ruin_dark", false);
                    }
                }
                Crystal(fc, sh.c + new Vector3(alongZ ? 0.9f : 0f, sh.s.y, alongZ ? 0f : 0.9f), 1.2f, rng);
                s++;
            }
            // Present: buckets and sodden stacks; Past: ladders.
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(-10f + i * 4f, 0f, 6f + (i % 3) * 9f);
                Cylinder("Bucket", sb, p + Vector3.up * 0.2f, 0.22f, 0.4f, "metal_dark", false);
                Cylinder("BucketWater", sb, p + Vector3.up * 0.36f, 0.19f, 0.02f, "water", false);
                Box("PaperStack", sb, p + new Vector3(0.8f, 0.12f, 0.3f), new Vector3(0.5f, 0.24f, 0.4f), "paper", false);
            }
            foreach (var x in new[] { -11.6f, 11.6f })
            {
                var lad = Box("Ladder", pb, new Vector3(x, 2.6f, 15f), new Vector3(0.08f, 5.2f, 0.6f), "brass", false);
                lad.transform.rotation = Quaternion.Euler(0f, 0f, x < 0 ? -8f : 8f);
            }
        }

        // =====================================================================
        // Foyer + the flooded register
        // =====================================================================
        static void BuildFoyer(Transform shared, Transform root)
        {
            var g = Group("Foyer", shared);
            Floor("FoyerFloor", g, new Vector3(0f, 0f, -5f), new Vector2(16f, 10f), "stone_warm");
            Wall("FoyerW", g, new Vector3(-8f, 0f, -10f), new Vector3(-8f, 0f, 0f), 6f, 0.5f, "stone_warm");
            Wall("FoyerE", g, new Vector3(8f, 0f, -10f), new Vector3(8f, 0f, 0f), 6f, 0.5f, "stone_warm");
            Wall("FoyerS", g, new Vector3(-8f, 0f, -10f), new Vector3(8f, 0f, -10f), 6f, 0.5f, "stone_warm");
            Box("FoyerCeiling", g, new Vector3(0f, 6.2f, -5f), new Vector3(16.5f, 0.4f, 10.5f), "stone_warm");
            Box("StreetDoor", g, new Vector3(0f, 1.4f, -9.7f), new Vector3(2.6f, 2.8f, 0.2f), "wood_dark");
            Sign(g, new Vector3(0f, 4.6f, -0.35f), 0f, "MUNICIPAL RECORDS HALL", 1.1f, new Color(1f, 0.85f, 0.55f), "brass");
            Sign(g, new Vector3(0f, 4.4f, 0.35f), 180f, "(0)  REGISTER OF THE LIVING AND THE DEAD  (0)", 0.7f, new Color(1f, 0.9f, 0.7f), "wood_dark");
            Box("Reception", g, new Vector3(0f, 0.55f, -4f), new Vector3(4.5f, 1.1f, 1f), "wood_dark");
            Box("ReceptionTop", g, new Vector3(0f, 1.12f, -4f), new Vector3(4.7f, 0.06f, 1.2f), "brass");
            Box("Bench", g, new Vector3(-6f, 0.25f, -6f), new Vector3(0.6f, 0.5f, 3f), "wood");
            Box("Bench", g, new Vector3(6f, 0.25f, -6f), new Vector3(0.6f, 0.5f, 3f), "wood");
            PointLight("FoyerLight", g, new Vector3(0f, 5f, -5f), new Color(0.9f, 0.85f, 0.75f), 1.6f, 14f);
            ReflectionProbe(g, new Vector3(0f, 3f, -5f), new Vector3(16f, 6f, 10f));

            var pres = StateGroup("FoyerPresent", root, TimeMask.Present, true);
            var ledger = Clue("FloodedRegister", pres, new Vector3(0f, 1.2f, -4f), new Vector3(0.9f, 0.08f, 0.6f), "paper", "Read the flooded register", "L03.ledgerRead", "L03.ledger",
                "The flooded register",
                "REGISTER OF THE LIVING - Resident No. 0417, Lantern Row, aged 7. Status: ALIVE. Margin stamp: TRANSFERRED TO THE NURSERY REGISTER.\n\nREGISTER OF THE DEAD - Resident No. 0417, aged 7. Died in the catastrophe, at the second bell. Name: (erased - the ink has run off the page).\n\nSame number, both books. Cards for residents are kept in the card drawers by hundreds. The Past will have the card before the rain got to it.");
            ledger.lines.Add(L("Arin", "Alive in one book, dead in the other. And the name in the death book has been washed away."));
            Box("Puddle", pres, new Vector3(1.5f, 0.015f, -6f), new Vector3(3f, 0.02f, 2f), "water", false);
            var rain = Particles("FoyerDrip", pres, new Vector3(1.5f, 5.8f, -6f), new Vector3(0.6f, 0.1f, 0.6f), "rain");
            var em = rain.emission; em.rateOverTime = 80f;
            var past = StateGroup("FoyerPast", root, TimeMask.Past, false);
            Box("ReceptionBell", past, new Vector3(1.6f, 1.22f, -4f), new Vector3(0.18f, 0.14f, 0.18f), "brass", false);
            Box("Visitors'Book", past, new Vector3(-1f, 1.17f, -4f), new Vector3(0.7f, 0.05f, 0.5f), "fabric_red", false);
            var fut = StateGroup("FoyerFuture", root, TimeMask.Future, false);
            Hologram("RegisterHolo", fut, new Vector3(0f, 2.2f, -4f), new Vector3(1.4f, 0.9f, 0.02f), PrimitiveType.Cube, "hologram", true, false);
            Label("RegisterHoloText", fut, new Vector3(0f, 2.2f, -4.05f), 0f, "0417 : ALIVE / DEAD\n[ RECORD CONFLICT ]", 0.25f, new Color(0.5f, 1f, 1f));
        }

        // =====================================================================
        // Beat 1: Past card drawers
        // =====================================================================
        static void BuildPastEvidence(Transform root, Transform pastDress)
        {
            var g = StateGroup("PastCatalogue", root, TimeMask.Past, true);
            Box("CardCabinet", g, new Vector3(-17.3f, 1.3f, 10f), new Vector3(0.8f, 2.6f, 7.6f), "wood");
            Box("CabinetCrown", g, new Vector3(-17.2f, 2.68f, 10f), new Vector3(1f, 0.16f, 7.8f), "brass", false);
            Sign(g, new Vector3(-16.85f, 3.2f, 10f), -90f, "CARD CATALOGUE OF RESIDENTS", 0.55f, new Color(1f, 0.88f, 0.6f), "wood_dark");

            string[] ranges = new string[12];
            for (int n = 0; n < 12; n++) ranges[n] = (n * 100).ToString("0000") + "-" + (n * 100 + 99).ToString("0000");
            for (int n = 1; n <= 12; n++)
            {
                int row = (n - 1) / 4, col = (n - 1) % 4;
                float z = 7.15f + col * 1.9f;
                float y = 2.05f - row * 0.72f;
                var pos = new Vector3(-16.86f, y, z);
                FlagInteractable fi;
                if (n == 5)
                {
                    fi = Clue("Drawer05", g, pos, new Vector3(0.12f, 0.6f, 1.7f), "wood_dark", "Open drawer 05", "L03.nameFoundPast", "L03.card",
                        "Registry card 0417",
                        "RESIDENT No. 0417 - MAREN OSKEL, aged 7.\nLantern Row, upper flat. Daughter of Tamsin and Joss Oskel, weavers at the festival looms.\nStatus: LIVING. Registered by O. Fenwick, Head Archivist.\n\nFresh red stamp across the corner: TRANSFERRED TO THE NURSERY REGISTER - by Founders' order. No address given.\n\nThere is no death on this card. Not even a pencil mark.", true, TimeMask.Past);
                    fi.lines.Add(L("Arin", "Maren Oskel. Seven years old. Alive, and moved to a register with no address."));
                    fi.usedPrompt = "Read card 0417 again";
                    fi.requiredFlags.Add("L03.indexRead");
                    fi.lockedMessage = "Twelve drawers, thousands of cards. Read the index board beside the cabinet first.";
                }
                else
                {
                    fi = Use("Drawer" + n.ToString("00"), g, pos, new Vector3(0.12f, 0.6f, 1.7f), "wood_dark", "Open drawer " + n.ToString("00"), "", TimeMask.Past);
                    fi.oneShot = false; fi.toggle = true; fi.cue = Cue.Clang;
                    fi.requiredFlags.Add("L03.indexRead");
                    fi.lockedMessage = "Twelve drawers, thousands of cards. Read the index board beside the cabinet first.";
                    fi.lines.Add(L("Arin", "Cards " + ranges[n - 1] + ". Not the range for 0417."));
                }
                fi.animateTarget = fi.transform;
                fi.moveBy = new Vector3(0.45f, 0f, 0f);
                Box("Handle", fi.transform, pos + new Vector3(0.09f, -0.1f, 0f), new Vector3(0.06f, 0.05f, 0.35f), "brass", false);
                Label("Num", fi.transform, pos + new Vector3(0.08f, 0.12f, 0f), -90f, n.ToString("00"), 0.35f, n == 5 ? new Color(1f, 0.9f, 0.5f) : new Color(0.95f, 0.85f, 0.65f));
            }
            // Index board (the clerk's hint, made readable).
            Box("IndexBoard", g, new Vector3(-17.55f, 2.3f, 16.2f), new Vector3(0.1f, 2.6f, 2.4f), "wood_dark");
            var idx = Use("IndexBoardRead", g, new Vector3(-17.42f, 1.2f, 16.2f), new Vector3(0.08f, 0.3f, 2.2f), "brass", "Read the index board", "L03.indexRead", TimeMask.Past);
            idx.usedPrompt = "Read the index board again";
            idx.lines.Add(L("Arin", "Drawers by hundreds. 0417 is in the four hundreds: drawer 05."));
            Label("IndexText", g, new Vector3(-17.45f, 2.3f, 16.2f), -90f,
                "INDEX - DRAWERS BY HUNDREDS\n0000-0099 : 01\n0100-0199 : 02\n0200-0299 : 03\n0300-0399 : 04\n0400-0499 : 05\n0500-0599 : 06\n...\n1100-1199 : 12", 0.17f, new Color(1f, 0.9f, 0.65f));
            PointLight("CatalogueLamp", g, new Vector3(-15.5f, 3.2f, 11f), Warm, 2.2f, 8f);

            // Past drainage plan on a lectern (the water puzzle's key).
            var lect = StateGroup("PastDrainagePlan", root, TimeMask.Past, true);
            Box("Lectern", lect, new Vector3(-8.5f, 0.55f, 31f), new Vector3(0.7f, 1.1f, 0.6f), "wood");
            var plan = Clue("DrainagePlan", lect, new Vector3(-8.5f, 1.18f, 31f), new Vector3(0.8f, 0.05f, 0.6f), "paper", "Study the drainage plan", "L03.planRead", "L03.plan",
                "Drainage plan of the Records Hall",
                "The Hall was built over the old rain-courses. Floor channel junctions turn by hand.\n\nTo carry roof water into the Survey Basin:\n  Junction I   - turn to EAST\n  Junction II  - turn to NORTH\n  Junction III - turn to EAST\n  Junction IV  - turn to NORTH, into the basin.\n\nThe Basin shows the city's survey map when filled. - O. Fenwick", true, TimeMask.Past);
            Sign(lect, new Vector3(-8.5f, 2.3f, 31.4f), 0f, "SURVEY OF RAIN-COURSES", 0.4f, new Color(1f, 0.85f, 0.55f), "brass");
        }

        // =====================================================================
        // Beat 2: Present transit stamp
        // =====================================================================
        static void BuildPresentEvidence(Transform root, Transform logic)
        {
            var g = StateGroup("PresentTransitDesk", root, TimeMask.Present, true);
            Box("TransitCounter", g, new Vector3(15.6f, 0.55f, 28f), new Vector3(1f, 1.1f, 6f), "wood_dark");
            Box("CounterTop", g, new Vector3(15.6f, 1.12f, 28f), new Vector3(1.2f, 0.05f, 6.2f), "iron_rust", false);
            Sign(g, new Vector3(17.3f, 3.2f, 28f), 90f, "TRANSIT BUREAU - LINE STAMPS", 0.5f, new Color(0.7f, 0.9f, 1f), "metal_dark");
            Box("LostTray", g, new Vector3(15.5f, 1.18f, 25.6f), new Vector3(0.7f, 0.08f, 0.6f), "metal_dark", false);
            var ticket = Clue("DampTicket", g, new Vector3(15.5f, 1.25f, 25.6f), new Vector3(0.5f, 0.04f, 0.4f), "paper", "Read the damp ticket", "L03.ticketRead", "L03.ticket",
                "Damp transit ticket",
                "LINE 4 - SUBSURFACE. CHILD FARE. Holder: resident No. 0417.\nPunched for travel on the 3rd day AFTER the date in the Register of the Dead.\n\nThe bureau mark is half-dissolved but the shape is clear: a V between angle brackets, like this:  <V>\n\nIf a stamp on the rack makes the same mark, the ticket is genuine.", true, TimeMask.Present);
            ticket.lines.Add(L("Arin", "A dead woman bought a ticket. Let's see if the bureau really stamped it."));
            Label("TrayTag", g, new Vector3(15.0f, 1.45f, 25.6f), 90f, "LOST PROPERTY", 0.22f, Cold);

            string[] marks = { "O", "#", "<V>", "X", "<>" };
            for (int i = 0; i < marks.Length; i++)
            {
                var p = new Vector3(15.6f, 1.3f, 26.9f + i * 0.6f);
                FlagInteractable fi;
                if (marks[i] == "<V>")
                {
                    fi = Clue("Stamp_V", g, p, new Vector3(0.4f, 0.3f, 0.4f), "brass", "Press stamp " + marks[i] + " beside the ticket", "L03.stampMatched", "L03.stamp",
                        "The stamp matches",
                        "The <V> stamp of LINE 4 - SUBSURFACE leaves exactly the mark on the ticket. The bureau's own date wheel is set three days after resident 0417's recorded death.\n\nSomeone who was 'dead' travelled on a line that, according to the Past clerks, had not even opened yet.", true, TimeMask.Present);
                    fi.requiredFlags.Add("L03.ticketRead");
                    fi.lockedMessage = "Read a ticket first - then you'll know which mark to match.";
                    fi.lines.Add(L("Arin", "Same mark. The ticket's real. The stamp's date hasn't happened yet."));
                }
                else
                {
                    fi = Use("Stamp_" + i, g, p, new Vector3(0.4f, 0.3f, 0.4f), "iron_rust", "Press stamp " + marks[i] + " beside the ticket", "", TimeMask.Present, PrimitiveType.Cylinder);
                    fi.oneShot = false; fi.cue = Cue.Clang;
                    fi.lines.Add(L("Arin", "A '" + marks[i] + "' mark. That's not what is on the ticket."));
                    fi.requiredFlags.Add("L03.ticketRead");
                    fi.lockedMessage = "Find something to compare these stamps with.";
                }
                Label("Mark", g, p + new Vector3(-0.15f, 0.42f, 0f), 90f, marks[i], 0.35f, Color.white);
            }

            // Introduce the Future once the stamp proves the date is still to come.
            var su = logic.gameObject.AddComponent<StateUnlock>();
            su.whenFlag = "L03.stampMatched";
            su.unlock = TimeMask.Future;
            su.toast = "The stamp is dated in a time that has not happened. FUTURE >> is now reachable [3].";
        }

        // =====================================================================
        // Beat 3: Future mineral traces
        // =====================================================================
        static void BuildFutureEvidence(Transform root, Transform logic)
        {
            var g = StateGroup("FutureTraces", root, TimeMask.Future, true);
            var frags = new (Vector3 pos, float yaw, string text, Vector3 face)[]
            {
                (new Vector3(-11.7f, 2.6f, 20f), 270f, "(1)  MA-", new Vector3(0.12f, 0.8f, 1.2f)),
                (new Vector3(-12f, 2.0f, 41.35f), 0f, "(2)  -REN", new Vector3(1.2f, 0.8f, 0.12f)),
                (new Vector3(10f, 3.2f, 41.35f), 0f, "(3)  OS-", new Vector3(1.2f, 0.8f, 0.12f)),
                (new Vector3(11.7f, 2.2f, 20f), 90f, "(4)  -KEL", new Vector3(0.12f, 0.8f, 1.2f)),
            };
            var flags = new List<string>();
            for (int i = 0; i < frags.Length; i++)
            {
                var f = frags[i];
                string flag = "L03.trace" + (i + 1);
                flags.Add(flag);
                var fi = Use("MineralTrace" + (i + 1), g, f.pos, f.face, "mineral_trace", "Trace the mineral letters", flag, TimeMask.Future);
                fi.oneShot = true; fi.usedPrompt = "Trace again";
                fi.cue = Cue.Memory;
                fi.lines.Add(L("Arin", "Ink dissolved, minerals stayed. Fragment " + (i + 1) + " of 4: " + f.text.Substring(5).Trim()));
                Vector3 viewBack = Quaternion.Euler(0f, f.yaw, 0f) * Vector3.back;
                Label("TraceText", g, f.pos + viewBack * 0.1f + Vector3.up * 0.75f, f.yaw, f.text, 0.6f, Mint);
                if (i % 2 == 1) PointLight("TraceGlow", g, f.pos + viewBack * 0.8f, Mint, 2.2f, 5f);
                var tm = Particles("TraceMotes", g, f.pos + viewBack * 0.4f, new Vector3(1.2f, 1.2f, 1.2f), "motes").emission; tm.rateOverTime = 18f;
                // Thin mineral veins around each fragment.
                for (int v = 0; v < 3; v++)
                    Box("Vein", g, f.pos + Vector3.up * (-0.5f + v * 0.5f) + viewBack * 0.02f, Vector3.Scale(f.face, new Vector3(1.3f, 0.06f, 1.3f)), "mineral_trace", false);
            }

            // Reconstructed name: hologram that assembles over the north shelves.
            var nameGroup = Group("ReconstructedName", g);
            var nameHolo = Label("NameHolo", nameGroup, new Vector3(0f, 6.5f, 41f), 0f, "MAREN OSKEL\n( erased from the Register of the Dead )", 0.9f, Mint);
            nameHolo.AddComponent<FloatBob>().amplitude = 0.12f;
            Hologram("NameFrame", nameGroup, new Vector3(0f, 6.5f, 41.1f), new Vector3(9f, 2.2f, 0.02f), PrimitiveType.Cube, "hologram", false, false);
            nameGroup.gameObject.SetActive(false);

            var r = Reactor("NameReconstructed", logic, flags.ToArray());
            r.announce = true;
            r.cue = Cue.Revelation;
            r.toast = "Reconstructed: MAREN OSKEL - the name deleted from the Register of the Dead.";
            r.setFlagWhenTrue = "L03.nameReconstructed";
            r.enableWhenTrue.Add(nameGroup.gameObject);

            var w = Zone("WitnessFuture", logic, new Vector3(0f, 1.5f, 24f), new Vector3(30f, 3f, 40f), "L03.witnessHeard", "", TimeMask.Future);
            w.lines.Add(L("The Witness", "Water forgets ink. Stone does not."));
        }

        // =====================================================================
        // Beat 4: water routing + map basin + payoff record
        // =====================================================================
        static void BuildWater(Transform root, Transform shared, Transform logic)
        {
            var basinG = Group("SurveyBasin", shared);
            Cylinder("BasinRim", basinG, new Vector3(0f, 0.35f, 36.5f), 1.7f, 0.7f, "copper_verdigris");
            Cylinder("BasinInner", basinG, new Vector3(0f, 0.68f, 36.5f), 1.45f, 0.04f, "stone_wet", false);
            Label("BasinPlate", basinG, new Vector3(0f, 0.45f, 34.75f), 0f, "SURVEY BASIN (0)", 0.3f, new Color(0.85f, 1f, 0.9f));

            var g = StateGroup("PresentChannels", root, TimeMask.Present, true);
            // Roof leak + collector.
            var leak = Particles("RoofLeakRain", g, new Vector3(-12f, 13.5f, 31f), new Vector3(1.2f, 0.1f, 1.2f), "rain");
            var lem = leak.emission; lem.rateOverTime = 600f;
            Cylinder("Collector", g, new Vector3(-12f, 0.2f, 31f), 0.9f, 0.4f, "iron_rust");
            Cylinder("CollectorWater", g, new Vector3(-12f, 0.41f, 31f), 0.8f, 0.02f, "water", false);
            Label("CollectorTag", g, new Vector3(-12f, 1.0f, 29.95f), 0f, "ROOF RAIN", 0.25f, Cold);

            Vector3 C = new Vector3(-12f, 0f, 31f);
            Vector3 J1 = new Vector3(-12f, 0f, 26f), J2 = new Vector3(-6f, 0f, 26f), J3 = new Vector3(-6f, 0f, 32f), J4 = new Vector3(0f, 0f, 32f), B = new Vector3(0f, 0f, 36.5f);
            var path = new[] { (C, J1), (J1, J2), (J2, J3), (J3, J4), (J4, B) };
            var flow = Group("FlowingWater", g);
            foreach (var (a, b) in path)
            {
                Vector3 mid = (a + b) * 0.5f; Vector3 dlt = b - a;
                Vector3 size = Mathf.Abs(dlt.x) > Mathf.Abs(dlt.z) ? new Vector3(Mathf.Abs(dlt.x), 0.03f, 0.5f) : new Vector3(0.5f, 0.03f, Mathf.Abs(dlt.z));
                Box("ChannelTrough", g, mid + Vector3.up * 0.025f, size + new Vector3(0.2f, 0f, 0.2f), "metal_dark", false);
                Box("ChannelWater", flow, mid + Vector3.up * 0.05f, size, "water_clear", false);
                // Flow arrows painted on the trough (shape, not colour).
                int n = Mathf.Max(1, Mathf.RoundToInt(dlt.magnitude / 2f));
                for (int k = 0; k < n; k++)
                {
                    var ap = a + dlt * ((k + 0.5f) / n) + Vector3.up * 0.07f;
                    var arrow = Label("FlowArrow", flow, ap, 0f, ">", 0.5f, new Color(0.7f, 0.95f, 1f));
                    arrow.transform.rotation = Quaternion.LookRotation(Vector3.down, dlt.normalized) * Quaternion.Euler(0f, 0f, 90f);
                }
            }
            Cylinder("BasinWater", flow, new Vector3(0f, 0.73f, 36.5f), 1.42f, 0.05f, "water_clear", false);
            var splash = Particles("BasinMist", flow, new Vector3(0f, 0.9f, 36.5f), new Vector3(2f, 0.2f, 2f), "mist");
            flow.gameObject.SetActive(false);

            // Junction tiles: arrow (shape) + roman numeral glyph. index 0=N,1=E,2=S,3=W.
            var j = new[]
            {
                Rotatable("JunctionI", g, J1 + Vector3.up * 0.1f, new Vector3(1.2f, 0.2f, 1.2f), "copper_verdigris", 4, 3, 1, "I"),
                Rotatable("JunctionII", g, J2 + Vector3.up * 0.1f, new Vector3(1.2f, 0.2f, 1.2f), "copper_verdigris", 4, 2, 0, "II"),
                Rotatable("JunctionIII", g, J3 + Vector3.up * 0.1f, new Vector3(1.2f, 0.2f, 1.2f), "copper_verdigris", 4, 2, 1, "III"),
                Rotatable("JunctionIV", g, J4 + Vector3.up * 0.1f, new Vector3(1.2f, 0.2f, 1.2f), "copper_verdigris", 4, 1, 0, "IV"),
            };
            foreach (var e in j) e.prompt = "Turn junction " + e.glyph;
            Label("CompassN", g, new Vector3(-6f, 0.06f, 29f), 0f, "N ^", 0.5f, Cold).transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var puzzle = logic.gameObject.AddComponent<AlignmentPuzzle>();
            puzzle.elements.AddRange(j);
            puzzle.solvedFlag = "L03.waterRouted";
            puzzle.solvedToast = "The rain runs the old courses into the Survey Basin.";
            junctionList = new List<RotatableElement>(j);

            // Hologram map that rises from the basin.
            var map = Group("SurveyMap", g);
            map.position = Vector3.zero;
            var mapRoot = new Vector3(0f, 0.6f, 36.5f);
            Hologram("MapDisc", map, mapRoot, new Vector3(3f, 0.01f, 3f), PrimitiveType.Cylinder, "hologram", false, true);
            Hologram("Line4", map, mapRoot + new Vector3(0f, 0.05f, 0.6f), new Vector3(0.08f, 0.02f, 2.2f), PrimitiveType.Cube, "hologram_amber", false, false);
            Hologram("LineHall", map, mapRoot + new Vector3(-0.6f, 0.05f, -0.4f), new Vector3(1.4f, 0.02f, 0.06f), PrimitiveType.Cube, "hologram", false, false);
            Hologram("HatchDot", map, mapRoot + new Vector3(0f, 0.12f, 0.3f), new Vector3(0.18f, 0.18f, 0.18f), PrimitiveType.Sphere, "hologram_magenta", false, false);
            Label("MapTitle", map, mapRoot + new Vector3(0f, 1.0f, 0f), 0f, "SURVEY: LINE 4 - SUBSURFACE\nservice stair under the floor hatch  >>  north", 0.28f, new Color(0.6f, 1f, 1f));
            var mapLight = PointLight("MapLight", map, mapRoot + Vector3.up * 0.6f, new Color(0.3f, 0.9f, 1f), 3f, 9f);

            // The contradictory record (payoff) surfaces with the water.
            var rec = Group("SurfacedRecord", g);
            Box("RecordStand", rec, new Vector3(2.4f, 0.5f, 36.5f), new Vector3(0.6f, 1f, 0.6f), "copper_verdigris");
            recGroup = rec;
            rec.gameObject.SetActive(false);

            var wr = Reactor("WaterFlows", logic, "L03.waterRouted");
            wr.announce = true; wr.cue = Cue.WaterRush;
            wr.enableWhenTrue.Add(flow.gameObject);
            wr.enableWhenTrue.Add(rec.gameObject);
            wr.moveTarget = map; wr.moveBy = new Vector3(0f, 1.3f, 0f); wr.duration = 3f;
            wr.toast = "A map rises from the basin. Five sodden records have surfaced beside it - only one is Maren's.";
            map.gameObject.SetActive(true);
            // Map is hidden (sunk) until the water arrives.
            var mapHide = Reactor("MapReveal", logic, "L03.waterRouted");
            foreach (Transform c in map) mapHide.enableWhenTrue.Add(c.gameObject);
            foreach (Transform c in map) c.gameObject.SetActive(false);
            Ambient("ChannelWater", g, new Vector3(-6f, 0.5f, 30f), AmbientKind.Water, 0.3f, TimeMask.Present, 1f, 16f);
        }

        // =====================================================================
        // Exit: floor hatch, service stair, Line 4 tunnel
        // =====================================================================
        static void BuildHatchAndTunnel(Transform shared, Transform logic, Transform root)
        {
            var g = Group("HatchAndTunnel", shared);
            // Hatch frame + hinged panel.
            Box("HatchFrameS", g, new Vector3(0f, 0.04f, 39.9f), new Vector3(3.4f, 0.08f, 0.2f), "brass", false);
            Box("HatchFrameN", g, new Vector3(0f, 0.04f, 44.6f), new Vector3(3.4f, 0.08f, 0.2f), "brass", false);
            var pivot = Group("HatchHinge", g);
            pivot.position = new Vector3(-1.5f, 0f, 42.25f);
            var panel = Box("HatchPanel", pivot, new Vector3(0f, -0.1f, 42.25f), new Vector3(3f, 0.2f, 4.5f), "iron_rust");
            Dyn(panel);
            var glyph = Label("HatchGlyph", pivot, new Vector3(0f, 0.02f, 42.25f), 0f, "(0)\nLINE 4", 0.6f, new Color(0.9f, 0.95f, 1f));
            glyph.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var hatchR = pivot.gameObject.AddComponent<FlagReactor>();
            hatchR.whenAll.Add("L03.hatchOpen");
            hatchR.moveTarget = pivot; hatchR.rotateBy = new Vector3(0f, 0f, 112f); hatchR.duration = 2.2f;
            hatchR.announce = true;
            hatchR.toast = "The floor hatch unlatches. The subsurface tram route is open.";
            Sign(g, new Vector3(0f, 2.6f, 47.6f), 0f, "SERVICE STAIR - LINE 4 SUBSURFACE", 0.5f, new Color(0.8f, 0.95f, 1f), "metal_dark");
            var warn = PointLight("HatchLight", g, new Vector3(0f, 2.2f, 46.5f), new Color(1f, 0.55f, 0.3f), 2f, 8f);

            // Stair + tunnel.
            Stairs("ServiceStair", g, new Vector3(0f, -3f, 44.5f), 180f, 15, 0.2f, 0.3f, 3f, "concrete");
            Wall("StairW", g, new Vector3(-1.6f, -3f, 40f), new Vector3(-1.6f, -3f, 44.5f), 3f, 0.2f, "concrete");
            Wall("StairE", g, new Vector3(1.6f, -3f, 40f), new Vector3(1.6f, -3f, 44.5f), 3f, 0.2f, "concrete");
            Floor("TunnelFloor", g, new Vector3(0f, -3f, 52.25f), new Vector2(6f, 15.5f), "concrete");
            Wall("TunnelW", g, new Vector3(-3f, -3f, 44.5f), new Vector3(-3f, -3f, 60f), 3f, 0.4f, "tile_cold");
            Wall("TunnelE", g, new Vector3(3f, -3f, 44.5f), new Vector3(3f, -3f, 60f), 3f, 0.4f, "tile_cold");
            Wall("TunnelN", g, new Vector3(-3f, -3f, 60f), new Vector3(3f, -3f, 60f), 3f, 0.4f, "tile_cold");
            Wall("TunnelSW", g, new Vector3(-3f, -3f, 44.5f), new Vector3(-1.6f, -3f, 44.5f), 2.5f, 0.3f, "tile_cold");
            Wall("TunnelSE", g, new Vector3(1.6f, -3f, 44.5f), new Vector3(3f, -3f, 44.5f), 2.5f, 0.3f, "tile_cold");
            Box("TunnelCeiling", g, new Vector3(0f, -0.2f, 54f), new Vector3(6.4f, 0.4f, 12f), "concrete");
            foreach (float x in new[] { -0.75f, 0.75f })
                Box("Rail", g, new Vector3(x, -2.92f, 54f), new Vector3(0.08f, 0.16f, 12f), "rail", false);
            // A waiting tram car (Line 4).
            var tram = Group("Line4Car", g);
            Box("CarBody", tram, new Vector3(0f, -1.75f, 57.6f), new Vector3(2.6f, 2.3f, 4.4f), "metal_dark");
            Box("CarDoor", tram, new Vector3(0f, -1.95f, 55.38f), new Vector3(1.2f, 1.9f, 0.06f), "lamp_cold", false);
            Box("CarStripe", tram, new Vector3(0f, -1.2f, 55.37f), new Vector3(2.6f, 0.12f, 0.04f), "neon_cyan", false);
            Sign(g, new Vector3(0f, -0.85f, 55.3f), 0f, "LINE 4", 0.5f, new Color(0.6f, 1f, 1f), "metal_dark");
            Lamp("TunnelLampA", g, new Vector3(-2.5f, -3f, 48f), "sodium", 2.3f);
            PointLight("TunnelGlow", g, new Vector3(0f, -1f, 53f), new Color(0.4f, 0.8f, 1f), 2f, 9f);
            ReflectionProbe(g, new Vector3(0f, -1.5f, 52f), new Vector3(6f, 3.6f, 16f));
            Ambient("TunnelHum", g, new Vector3(0f, -1.5f, 54f), AmbientKind.Machine, 0.25f, TimeMask.All, 1f, 14f);

            Exit("Exit_Line4", root, new Vector3(0f, -3f, 54.6f), new Vector3(5.4f, 3f, 1.4f),
                "The hatch is shut. Fill the Survey Basin, find Maren's real record - and get past whoever comes for it.", "L03.hatchOpen");
            Zone("ExitFlag", logic, new Vector3(0f, -1.5f, 52.8f), new Vector3(5.4f, 3f, 1.4f), "L03.exit");
            // Safety: anything that falls through the hatch gap before it opens.
            KillPlane(g, new Vector3(0f, -8f, 30f), new Vector3(240f, 2f, 240f), "Back to the hall.");
        }

        // =====================================================================
        // NPCs
        // =====================================================================
        static void BuildNpcs(Transform root)
        {
            var past = StateGroup("PastPeople", root, TimeMask.Past, false);
            var present = StateGroup("PresentPeople", root, TimeMask.Present, false);

            // Odile Fenwick - head archivist (Past).
            var odile = Npc("Odile Fenwick", past, new Vector3(-15f, 0f, 9f), 90f, "coat_burgundy", true, 1);
            Route(odile, false,
                (new Vector3(-15.4f, 0f, 9f), 6f, NPCActivity.Work, "Cards, cards, and more cards."),
                (new Vector3(-8.5f, 0f, 30f), 5f, NPCActivity.Look, "The rain-courses must be kept clear."),
                (new Vector3(-2f, 0f, 14f), 5f, NPCActivity.Work, ""),
                (new Vector3(-15.4f, 0f, 14f), 4f, NPCActivity.Look, ""));
            odile.changedFlag = "L03.nameFoundPast";
            Route(odile, true,
                (new Vector3(-1f, 0f, -3f), 6f, NPCActivity.Work, "Card 0417... alive. Then why does my hand shake?"),
                (new Vector3(-15.4f, 0f, 10f), 6f, NPCActivity.Look, ""),
                (new Vector3(-8.5f, 0f, 29.5f), 4f, NPCActivity.Idle, ""));
            var po = odile.GetComponent<NPCPersona>();
            po.task = "Head Archivist: files new resident cards into the catalogue and keeps the rain-courses survey.";
            po.approachBarks.AddRange(new[] { "Quietly, please. The Hall is listening.", "You look like someone hunting for a name." });
            po.talkLines.Add(L("Odile Fenwick", "Every resident of Veyra has a card. Every card has a drawer. The drawers go by hundreds."));
            po.talkLines.Add(L("Odile Fenwick", "The index board by the cabinet will tell you which one. Do not dog-ear anything."));
            po.talkLines.Add(L("Odile Fenwick", "A record is a promise, Arin. Write a name once and the city owes that person a place."));
            po.talkLines.Add(L("Odile Fenwick", "Lately the Founders' office sends me stamps I have never seen. 'Transferred.' Transferred where?"));
            po.talkLines.Add(L("Odile Fenwick", "My own office is through the east door. Restricted. I keep the key, and the questions."));
            po.conditional.Add(new ConditionalLines { whenFlag = "L03.odileNote", lines = { L("Odile Fenwick", "You read my desk. Then you know I hid something."), L("Odile Fenwick", "If the Founders want a child filed as dead, they will have to find the paper first.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "L03.payoff", lines = { L("Odile Fenwick", "A certificate signed by you? Then you have been in my Hall before. Long before today.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "L03.nameFoundPast", lines = { L("Odile Fenwick", "Maren Oskel? Seven. Hums four notes over and over and swears there is a fifth."), L("Odile Fenwick", "Dead? Nonsense. Her mother brought her in this morning to be stamped for the Nursery register.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "L03.ledgerRead", lines = { L("Odile Fenwick", "0417? Four hundreds - drawer five. Mind the brass handle, it sticks.") } });
            po.changedLines.Add(L("Odile Fenwick", "I re-read card 0417 three times. There is no death on it. Who told you there would be?"));

            // Tobin Reyes - records clerk (Past).
            var tobin = Npc("Tobin Reyes", past, new Vector3(14f, 0f, 27f), -90f, "coat_olive", true, 2, "paper");
            Route(tobin, false,
                (new Vector3(14.4f, 0f, 27.5f), 5f, NPCActivity.Work, "Line Four, stamped. Line Four, stamped."),
                (new Vector3(5f, 0f, 22f), 3f, NPCActivity.Carry, ""),
                (new Vector3(-15.4f, 0f, 12f), 4f, NPCActivity.Work, "Dockets for the catalogue."),
                (new Vector3(4f, 0f, 8f), 3f, NPCActivity.Carry, ""));
            tobin.changedFlag = "L03.stampMatched";
            Route(tobin, true,
                (new Vector3(14.4f, 0f, 28.5f), 8f, NPCActivity.Look, "Who has been using the V stamp?"),
                (new Vector3(10f, 0f, 30f), 4f, NPCActivity.Idle, ""),
                (new Vector3(14.4f, 0f, 26f), 6f, NPCActivity.Work, ""));
            var pt = tobin.GetComponent<NPCPersona>();
            pt.task = "Records clerk: stamps transit dockets at the bureau desk and carries them to the card catalogue.";
            pt.approachBarks.AddRange(new[] { "Docket? Ticket? Complaint?", "Mind the ink, it's still wet." });
            pt.talkLines.Add(L("Tobin Reyes", "Every line has its own stamp. Circle for the river line, hash for the market loop."));
            pt.talkLines.Add(L("Tobin Reyes", "Nine days ago the Founders' office ordered a whole new box of Line Four tickets. Child fares."));
            pt.talkLines.Add(L("Tobin Reyes", "Mistress Fenwick doesn't like it. She reads every docket twice now."));
            pt.talkLines.Add(L("Tobin Reyes", "If you're looking for someone, start with the cards. Cards don't lie. People with stamps do."));
            pt.talkLines.Add(L("Tobin Reyes", "And the V in brackets for Line Four - the subsurface. Not that anyone rides it. It isn't open."));
            pt.conditional.Add(new ConditionalLines { whenFlag = "L03.ticketRead", lines = { L("Tobin Reyes", "A Line Four ticket with the <V> mark? We only keep that stamp for the opening day."), L("Tobin Reyes", "Which hasn't come yet. Where did you find it?") } });
            pt.conditional.Add(new ConditionalLines { whenFlag = "L03.nameFoundPast", lines = { L("Tobin Reyes", "Oskel? I stamped a child's ticket for an Oskel last week. Founders' account. Never seen a Founder pay for a fare.") } });
            pt.conditional.Add(new ConditionalLines { whenFlag = "L03.ledgerRead", lines = { L("Tobin Reyes", "Resident numbers? Hundreds per drawer. The board by the cabinet lists them.") } });
            pt.changedLines.Add(L("Tobin Reyes", "Somebody used the Line Four stamp. The date wheel is set to days that haven't happened. I didn't touch it. I swear."));

            // Bram Hollis - Present night caretaker with a bucket.
            var bram = Npc("Bram Hollis", present, new Vector3(-10f, 0f, 29f), 0f, "coat_navy", true, 3, "metal_dark");
            Route(bram, false,
                (new Vector3(-10.8f, 0f, 30f), 4f, NPCActivity.Carry, "Another bucket. The roof never learns."),
                (new Vector3(6f, 0f, 12f), 4f, NPCActivity.Work, ""),
                (new Vector3(10f, 0f, 33f), 3f, NPCActivity.Carry, ""),
                (new Vector3(-2f, 0f, 4f), 3f, NPCActivity.Idle, "Keep the paper off the floor..."));
            bram.changedFlag = "L03.waterRouted";
            Route(bram, true,
                (new Vector3(2.4f, 0f, 33.6f), 10f, NPCActivity.Look, "Forty years of buckets, and it wanted to go there all along."),
                (new Vector3(-3f, 0f, 34f), 6f, NPCActivity.Idle, ""),
                (new Vector3(-10.8f, 0f, 30f), 4f, NPCActivity.Work, ""));
            var pb = bram.GetComponent<NPCPersona>();
            pb.task = "Night caretaker: carries rain buckets from under the roof leaks so the last dry records survive.";
            pb.approachBarks.AddRange(new[] { "Careful, floor's slick.", "Hall's closed. Not that anyone's left to close it for." });
            pb.talkLines.Add(L("Bram Hollis", "The rain comes in where the skylights broke. I empty, it fills. That's the job."));
            pb.talkLines.Add(L("Bram Hollis", "Forty years I've worked nights here. Since the catastrophe. Since the earthquake, they called it."));
            pb.talkLines.Add(L("Bram Hollis", "Odd thing about the Register of the Dead. Lot of children in it. Lot of children nobody ever mourned."));
            pb.talkLines.Add(L("Bram Hollis", "Old Fenwick's office is behind the east door. Nobody's gone in since her. I leave a lamp on."));
            pb.talkLines.Add(L("Bram Hollis", "There are old channels in the floor. Turnable junctions, numbered. Nobody remembers which way they go."));
            pb.conditional.Add(new ConditionalLines { whenFlag = "L03.ondineDone", lines = { L("Bram Hollis", "Mother Ondine herself. Pell said she'd come if anyone ever found that record. Odile will want to know you got out.") } });
            pb.conditional.Add(new ConditionalLines { whenFlag = "L03.vosselTalked", lines = { L("Bram Hollis", "Vossel. The Keepers' watcher. Pell warned us - she was on the festival street too. Never blinks.") } });
            pb.conditional.Add(new ConditionalLines { whenFlag = "L03.nameReconstructed", lines = { L("Bram Hollis", "Maren Oskel. I knew the Oskels. They left on the trams nine days before the bell. They never came back up.") } });
            pb.conditional.Add(new ConditionalLines { whenFlag = "L03.planRead", lines = { L("Bram Hollis", "A drainage plan? From the old days? Then turn them, friend. I'll hold the bucket.") } });
            pb.conditional.Add(new ConditionalLines { whenFlag = "L03.ticketRead", lines = { L("Bram Hollis", "That ticket floated up out of the floor drains last winter. Line Four. Under our feet, they say.") } });
            pb.changedLines.Add(L("Bram Hollis", "Look at that map. There's a stair under the hatch I've mopped around for forty years."));

            // Background readers and clerks (Past) - keep working when unobserved.
            var crowd = Crowd("Readers", past, new Vector3(0f, 0f, 25.5f), new Vector2(10f, 6f), 8, 303,
                new[] { "Shh.", "The census for Lantern Row is in drawer five.", "Festival week and I'm stuck in the stacks.", "Pardon me." }, "paper");
            // Reading desks with brass lamps.
            var desks = Group("ReadingDesks", Group("PastDesks", past));
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(0f, 0f, 8f + i * 5f);
                Box("Desk", desks, p + Vector3.up * 0.75f, new Vector3(5f, 0.08f, 1.4f), "wood", false);
                Box("DeskLegs", desks, p + Vector3.up * 0.37f, new Vector3(4.6f, 0.74f, 1.1f), "wood_dark", true);
                Cylinder("LampStem", desks, p + new Vector3(0f, 1.0f, 0f), 0.03f, 0.5f, "brass", false);
                Sphere("LampShade", desks, p + new Vector3(0f, 1.3f, 0f), 0.16f, "lamp_warm", false);
                Box("OpenBook", desks, p + new Vector3(-1.2f, 0.82f, 0f), new Vector3(0.5f, 0.04f, 0.35f), "paper", false);
            }
        }

        // =====================================================================
        // Atmosphere: lights, particles, holograms, audio, probes
        // =====================================================================
        static void BuildAtmosphere(Transform shared, Transform past, Transform present, Transform future)
        {
            ReflectionProbe(shared, new Vector3(0f, 6f, 24f), new Vector3(36f, 14f, 48f));

            // PAST: brass chandeliers, sunbeams through glazed skylights, dust.
            var pl = Group("PastLight", past);
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(0f, 9f, 8f + i * 14f);
                Cylinder("ChandelierChain", pl, p + Vector3.up * 2.5f, 0.03f, 5f, "brass", false);
                Cylinder("ChandelierRing", pl, p, 1.2f, 0.08f, "brass", false);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3f;
                    Sphere("Flame", pl, p + new Vector3(Mathf.Cos(a) * 1.2f, 0.15f, Mathf.Sin(a) * 1.2f), 0.09f, "lamp_warm", false);
                }
                PointLight("Chandelier", pl, p + Vector3.down * 0.3f, Warm, 3.4f, 22f, false);
            }
            SpotLight("SunShaftA", pl, new Vector3(-4f, 13.8f, 19f), new Vector3(70f, 20f, 0f), new Color(1f, 0.85f, 0.6f), 6f, 22f, 28f, true);
            Particles("PastDust", pl, new Vector3(0f, 5f, 22f), new Vector3(30f, 9f, 40f), "dust");
            for (int i = 0; i < 4; i++)
            {
                var banner = Box("Banner", pl, new Vector3(-17.6f + (i % 2) * 35.2f, 9f, 10f + (i / 2) * 20f), new Vector3(0.05f, 3.4f, 1.6f), i % 2 == 0 ? "fabric_red" : "fabric_gold", false);
                Label("BannerGlyph", pl, banner.transform.position + new Vector3(i % 2 == 0 ? 0.05f : -0.05f, 0.6f, 0f), i % 2 == 0 ? -90f : 90f, "(0)", 0.6f, new Color(1f, 0.9f, 0.6f));
            }
            Ambient("PastCrowd", pl, new Vector3(0f, 2f, 16f), AmbientKind.Crowd, 0.25f, TimeMask.Past, 0.6f, 30f);

            // PRESENT: rain columns through broken skylights, cold light, puddles, lightning flicker.
            var rl = Group("PresentLight", present);
            var rainPts = new[] { new Vector3(-4f, 13.6f, 9f), new Vector3(3f, 13.6f, 19f), new Vector3(8f, 13.6f, 29f), new Vector3(-9f, 13.6f, 39f) };
            foreach (var rp in rainPts)
            {
                var ps = Particles("RainColumn", rl, rp, new Vector3(3f, 0.1f, 1.6f), "rain");
                var em = ps.emission; em.rateOverTime = 500f;
                Box("Puddle", rl, new Vector3(rp.x, 0.025f, rp.z), new Vector3(3.6f, 0.02f, 2.4f), "water", false);
                Particles("Splash", rl, new Vector3(rp.x, 0.2f, rp.z), new Vector3(3f, 0.1f, 1.6f), "mist");
            }
            PointLight("ColdFill", rl, new Vector3(0f, 10f, 14f), Cold, 2.2f, 26f);
            PointLight("ColdFill2", rl, new Vector3(0f, 10f, 34f), Cold, 2f, 24f);
            foreach (var p in new[] { new Vector3(-17f, 0f, 4f), new Vector3(17f, 0f, 36f) })
                Lamp("EmergencyLamp", rl, p, "sodium", 3.4f);
            Ambient("PresentRain", rl, new Vector3(0f, 8f, 24f), AmbientKind.Rain, 0.5f, TimeMask.Present, 0.5f, 45f);
            Ambient("PresentRainFoyer", rl, new Vector3(0f, 3f, -5f), AmbientKind.Rain, 0.3f, TimeMask.Present, 1f, 14f);

            // FUTURE: neon, holograms, motes, mist, violet sky.
            var fl = Group("FutureLight", future);
            Particles("FutureMotes", fl, new Vector3(0f, 4f, 24f), new Vector3(34f, 8f, 46f), "motes");
            Particles("FutureMist", fl, new Vector3(0f, 0.6f, 24f), new Vector3(34f, 1f, 46f), "mist");
            PointLight("NeonFillW", fl, new Vector3(-14f, 4f, 24f), new Color(0.3f, 0.9f, 1f), 2.5f, 22f);
            PointLight("NeonFillE", fl, new Vector3(14f, 4f, 24f), new Color(1f, 0.3f, 0.8f), 2.5f, 22f);
            PointLight("VioletTop", fl, new Vector3(0f, 12f, 24f), new Color(0.55f, 0.35f, 1f), 2f, 30f);
            var ring = Hologram("ArchiveRing", fl, new Vector3(0f, 9f, 20f), new Vector3(10f, 0.04f, 10f), PrimitiveType.Cylinder, "hologram", true, true);
            Hologram("ArchiveRing2", fl, new Vector3(0f, 9.6f, 20f), new Vector3(7f, 0.04f, 7f), PrimitiveType.Cylinder, "hologram_magenta", true, true).GetComponent<Spinner>().degreesPerSecond = new Vector3(0f, -25f, 0f);
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3(-8f + (i % 4) * 5.3f, 3f + (i / 4) * 2.5f, 12f + (i % 3) * 8f);
                var card = Hologram("FloatingCard", fl, p, new Vector3(0.6f, 0.4f, 0.02f), PrimitiveType.Cube, i % 2 == 0 ? "hologram" : "hologram_amber", true, false);
                card.GetComponent<FloatBob>().drift = new Vector3(0f, 15f + i * 3f, 0f);
            }
            Sign(fl, new Vector3(0f, 4.2f, 47.55f), 0f, "RECORD 0417 : CONFLICT UNRESOLVED", 0.6f, new Color(1f, 0.45f, 0.85f), "neon_magenta");
            Ambient("FutureDrone", fl, new Vector3(0f, 4f, 24f), AmbientKind.FutureDrone, 0.35f, TimeMask.Future, 0.4f, 45f);
            Ambient("FutureWind", fl, new Vector3(0f, 8f, 30f), AmbientKind.Wind, 0.2f, TimeMask.Future, 0.3f, 45f);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>Readable sign: text faces a viewer looking along yaw; backing plate sits behind it.</summary>
        static GameObject Sign(Transform parent, Vector3 pos, float yaw, string text, float size, Color color, string backMat)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var t = Label("Sign_" + text, parent, pos, yaw, text, size, color);
            float w = Mathf.Min(40f, text.Length * size * 0.55f + 0.6f);
            var back = Box("SignBack", parent, pos + rot * Vector3.forward * 0.06f, new Vector3(w, size * 1.4f + 0.2f, 0.05f), backMat, false, yaw);
            back.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        /// <summary>Low-object-count skyline tower: mass, crown trim and emissive window bands (mobile friendly).</summary>
        static void SkyTower(string name, Transform parent, Vector3 ground, Vector3 size, string mat, string windowMat, string trimMat, int seed)
        {
            var g = Group(name, parent);
            Box("Mass", g, ground + Vector3.up * size.y * 0.5f, size, mat, false);
            Box("Crown", g, ground + Vector3.up * (size.y + 0.2f), new Vector3(size.x + 0.4f, 0.4f, size.z + 0.4f), trimMat, false);
            var rng = new System.Random(seed);
            int bands = Mathf.Clamp(Mathf.FloorToInt(size.y / 7f), 2, 6);
            for (int i = 0; i < bands; i++)
            {
                float y = 3f + i * (size.y - 4f) / bands;
                float w = size.x * (0.5f + (float)rng.NextDouble() * 0.4f);
                Box("WindowBand", g, ground + new Vector3(0f, y, -size.z * 0.5f - 0.04f), new Vector3(w, 0.5f, 0.06f), i % 2 == 0 ? windowMat : trimMat, false);
            }
        }

        /// <summary>Clears static flags so moving objects are not static-batched.</summary>
        static void Dyn(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }
        static void Dyn(Transform t) => Dyn(t.gameObject);
    }
}
