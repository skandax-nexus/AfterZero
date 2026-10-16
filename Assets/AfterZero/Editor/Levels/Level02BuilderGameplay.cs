using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>LEVEL 02: clockwork, per-state dressing, puzzle flow and NPCs.</summary>
    public static partial class Level02Builder
    {
        // =================================================================
        // Clockwork (gears, shaft, bell)
        // =================================================================

        static void BuildClockwork()
        {
            var axisX = new Vector3(0f, 0f, 90f);   // disc in the Y-Z plane (on east/west walls)
            var axisZ = new Vector3(90f, 0f, 0f);   // disc in the X-Y plane (on north/south walls)

            // PAST: the great drive gear + pinion, seized by the wedge until it is pulled.
            var pastGears = Group("Clockwork_Past", pastPlay);
            Gear("GreatGear", pastGears, new Vector3(-10.8f, 1.3f, 2f), 3.5f, axisX, "brass", 11, F_Wedge, 14f);
            Gear("Pinion", pastGears, new Vector3(-10.8f, -0.4f, 6.55f), 1.1f, axisX, "brass", 6, F_Wedge, -44f);
            Box("PinionBracket", pastGears, new Vector3(-11.4f, -1.5f, 6.55f), new Vector3(1.0f, 2f, 0.6f), "metal_dark");
            Box("GearAxle", pastGears, new Vector3(-11.5f, 1.3f, 2f), new Vector3(1.2f, 0.5f, 0.5f), "metal_dark");
            Cylinder("DriveShaft", pastGears, new Vector3(-11.3f, 14f, 2f), 0.22f, 24f, "brass", false);

            var pastUpper = Group("UpperClockwork_Past", pastLook);
            Gear("Esc1", pastUpper, new Vector3(11.4f, 18f, -5f), 2.6f, axisX, "brass", 9, F_Wedge, 10f);
            Gear("Esc2", pastUpper, new Vector3(11.4f, 21.6f, -0.6f), 1.6f, axisX, "brass", 6, F_Wedge, -16f);
            Gear("Esc3", pastUpper, new Vector3(11.4f, 18.6f, 4.4f), 2.2f, axisX, "brass", 8, F_Wedge, 12f);
            Gear("NorthGear1", pastUpper, new Vector3(2f, 20f, 11.4f), 3f, axisZ, "brass", 10, F_Wedge, -8f);
            Gear("NorthGear2", pastUpper, new Vector3(-3.6f, 23.8f, 11.4f), 1.8f, axisZ, "brass", 6, F_Wedge, 13f);
            Gear("GalleryEscapement", pastUpper, new Vector3(11.5f, 9.6f, 6f), 1.2f, axisX, "brass", 6, F_Wedge, 30f);

            // PRESENT: the same gears, rusted; the drive gear turns once the pit drains.
            var presGears = Group("Clockwork_Present", presPlay);
            Gear("GreatGear", presGears, new Vector3(-10.8f, 1.3f, 2f), 3.5f, axisX, "iron_rust", 11, F_Drained, 6f);
            Gear("Pinion", presGears, new Vector3(-10.8f, -0.4f, 6.55f), 1.1f, axisX, "iron_rust", 6, F_Drained, -19f);
            Box("PinionBracket", presGears, new Vector3(-11.4f, -1.5f, 6.55f), new Vector3(1.0f, 2f, 0.6f), "metal_dark");
            Cylinder("DriveShaft", presGears, new Vector3(-11.3f, 9f, 2f), 0.22f, 14f, "iron_rust", false);
            var presUpper = Group("UpperClockwork_Present", presLook);
            Gear("Esc1", presUpper, new Vector3(11.4f, 18f, -5f), 2.6f, axisX, "iron_rust", 9, null, 0f);
            Gear("NorthGear1", presUpper, new Vector3(2f, 20f, 11.4f), 3f, axisZ, "copper_verdigris", 10, null, 0f);
            Gear("GalleryEscapement", presUpper, new Vector3(11.5f, 9.6f, 6f), 1.2f, axisX, "iron_rust", 6, null, 0f);

            // FUTURE: shattered clockwork drifting in the open shaft.
            var futGears = Group("Clockwork_Future", futLook);
            var f1 = Gear("DriftGear1", futGears, new Vector3(-9.5f, 2f, 2f), 3.5f, new Vector3(12f, 0f, 80f), "copper_verdigris", 11, null, 4f);
            f1.gameObject.AddComponent<FloatBob>().amplitude = 0.25f;
            var f2 = Gear("DriftGear2", futGears, new Vector3(3f, 19f, 3f), 2.2f, new Vector3(30f, 20f, 70f), "brass", 8, null, -6f);
            f2.gameObject.AddComponent<FloatBob>().amplitude = 0.4f;
            var f3 = Gear("DriftGear3", futGears, new Vector3(-1f, 24f, -3f), 1.6f, new Vector3(70f, 40f, 10f), "copper_verdigris", 6, null, 9f);
            f3.gameObject.AddComponent<FloatBob>().amplitude = 0.3f;

            // The bell.
            BellAt(pastLook, new Vector3(2.5f, 24f, 0f), "brass", false);
            BellAt(presLook, new Vector3(2.5f, 24f, 0f), "iron_rust", true);
            var shardRng = new System.Random(3);
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(2.5f + ((float)shardRng.NextDouble() - 0.5f) * 5f, 22f + (float)shardRng.NextDouble() * 5f, ((float)shardRng.NextDouble() - 0.5f) * 5f);
                var s = Box("BellShard", futLook, p, new Vector3(0.8f, 0.6f, 0.15f), "brass", false, (float)shardRng.NextDouble() * 360f);
                s.transform.rotation = Random3(shardRng);
                s.AddComponent<FloatBob>().amplitude = 0.3f;
                s.AddComponent<Spinner>().degreesPerSecond = new Vector3(5f, 12f, 3f);
            }
        }

        static Quaternion Random3(System.Random r) => Quaternion.Euler((float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f);

        static void BellAt(Transform g, Vector3 p, string mat, bool cracked)
        {
            Cylinder("BellBody", g, p + Vector3.down * 1.1f, 1.5f, 2.2f, mat, false);
            Sphere("BellCrown", g, p, 1.35f, mat, false);
            Cylinder("BellLip", g, p + Vector3.down * 2.25f, 1.75f, 0.25f, mat, false);
            Box("BellYoke", g, p + Vector3.up * 1.4f, new Vector3(9.5f, 0.5f, 0.6f), "wood_dark", false);
            if (cracked)
            {
                var c = Box("Crack", g, p + new Vector3(0f, -1.2f, -1.52f), new Vector3(0.08f, 1.8f, 0.04f), "metal_dark", false);
                c.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
            }
        }

        // =================================================================
        // State dressing
        // =================================================================

        static void BuildPastDressing()
        {
            var g = pastLook;
            // Founders' banners.
            foreach (var z in new[] { -6f, 6f })
            {
                Deco("FounderBanner", g, new Vector3(-11.9f, 9f, z), new Vector3(0.05f, 5f, 2f), "fabric_red");
                Deco("FounderBannerTrim", g, new Vector3(-11.85f, 9f, z), new Vector3(0.05f, 4.4f, 0.3f), "fabric_gold");
                Label("FounderGlyph", g, new Vector3(-11.8f, 10.5f, z), -90f, "(0)", 0.6f, new Color(1f, 0.85f, 0.4f));
            }
            // Workbench + tools on the landing.
            Box("Workbench", g, new Vector3(-3.6f, 1.95f, -11.3f), new Vector3(2.6f, 0.9f, 1f), "wood");
            Deco("OilCans", g, new Vector3(-4.2f, 2.55f, -11.3f), new Vector3(0.6f, 0.3f, 0.3f), "brass");
            Deco("Ledger", g, new Vector3(-3.0f, 2.43f, -11.2f), new Vector3(0.5f, 0.06f, 0.35f), "paper");
            Deco("ToolRack", g, new Vector3(-3.6f, 3.4f, -11.95f), new Vector3(2.6f, 1.2f, 0.1f), "wood_dark");
            Box("BrassPump", g, new Vector3(5f, 2.1f, -7.6f), new Vector3(0.5f, 1.2f, 0.5f), "brass");
            // Keeper's chamber (Past): desk, shelves, chair.
            Deco("Bookshelf", g, new Vector3(-11.6f, 15f, 8f), new Vector3(0.6f, 3.6f, 4f), "wood_dark");
            for (int i = 0; i < 4; i++) Deco("Books", g, new Vector3(-11.3f, 13.8f + i * 0.85f, 8f), new Vector3(0.3f, 0.5f, 3.6f), i % 2 == 0 ? "fabric_red" : "fabric_teal");
            Deco("Chair", g, new Vector3(-9.6f, 13.45f, 10.5f), new Vector3(0.6f, 0.9f, 0.6f), "wood");
            // Lamps + lights.
            Lamp("LandingLamp", g, new Vector3(-10.8f, 1.5f, -11.2f), "gas", 3f);
            Lamp("LandingLamp", g, new Vector3(10.8f, 1.5f, -11.2f), "gas", 3f);
            PointLight("PitGlow", g, new Vector3(-6f, 1f, 2f), Warm, 3f, 11f);
            PointLight("GalleryLamp", g, new Vector3(9.5f, 9.5f, -5f), Warm, 2.2f, 8f);
            PointLight("GalleryLamp", g, new Vector3(9.5f, 9.5f, 6f), Warm, 2.2f, 8f);
            PointLight("BellFloorLamp", g, new Vector3(-7f, 16f, -8f), Warm, 2.4f, 9f);
            PointLight("ChamberLamp", g, new Vector3(-8f, 17f, 8f), Warm, 2.4f, 8f);
            PointLight("FaceBacklight", g, new Vector3(0f, 22f, -10.2f), Warm, 3f, 9f);
            SpotLight("SkylightShaft", g, new Vector3(0f, 29.5f, 0f), new Vector3(90f, 0f, 0f), new Color(1f, 0.85f, 0.6f), 6f, 40f, 40f, true);
            Particles("Dust", g, new Vector3(0f, 10f, 0f), new Vector3(20f, 22f, 20f), "dust");
            Particles("PitDust", g, new Vector3(0f, -1f, 2f), new Vector3(20f, 2f, 16f), "dust");
            Ambient("Amb_GearsPast", shared, new Vector3(-8f, 0f, 3f), AmbientKind.Machine, 0.45f, TimeMask.Past, 1f, 28f);
            Ambient("Amb_GearsPastUpper", shared, new Vector3(9f, 18f, 0f), AmbientKind.Machine, 0.35f, TimeMask.Past, 1f, 24f);
            ReflectionProbe(shared, new Vector3(0f, 10f, 0f), new Vector3(24f, 30f, 24f));
        }

        static void BuildPresentDressing()
        {
            var g = presLook;
            // Rusted pipes along the walls, drips, chains.
            for (int i = 0; i < 3; i++)
            {
                var p = Cylinder("Pipe", g, new Vector3(-11.7f, 4f + i * 0.6f, 0f), 0.15f, 24f, "iron_rust", false);
                p.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var pe = Cylinder("PipeE", g, new Vector3(11.75f, 8.4f, 0f), 0.2f, 24f, "iron_rust", false);
            pe.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 4; i++)
                Deco("Chain", g, new Vector3(-4f + i * 3f, 20f, -1f + i), new Vector3(0.06f, 12f, 0.06f), "metal_dark");
            // Debris in the pit (revealed when drained).
            var rng = new System.Random(22);
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3(-9f + (float)rng.NextDouble() * 16f, -2.3f, -4f + (float)rng.NextDouble() * 12f);
                if (Mathf.Abs(p.x) < 2.2f && p.z < 0f) continue; // keep the stair foot clear
                Deco("Crate", g, p, new Vector3(0.7f, 0.4f, 0.7f), i % 2 == 0 ? "wood_dark" : "iron_rust", (float)rng.NextDouble() * 90f);
            }
            Deco("Silt", g, new Vector3(0f, -2.49f, 3f), new Vector3(22f, 0.02f, 16f), "mud");
            // Buckets and puddles on the landing.
            Deco("Puddle", g, new Vector3(-3f, 1.51f, -9.5f), new Vector3(3f, 0.02f, 2f), "water");
            Deco("Puddle", g, new Vector3(6f, 1.51f, -10.5f), new Vector3(2f, 0.02f, 1.5f), "water");
            for (int i = 0; i < 3; i++) Cylinder("Bucket", g, new Vector3(8.5f + i * 0.6f, 1.75f, -11.4f), 0.2f, 0.45f, "iron_rust", false);
            Label("FloodNotice", g, new Vector3(0f, 3.2f, -11.95f), 180f, "CLOCKWORKS CLOSED - FLOOD SINCE THE QUAKE", 0.3f, new Color(1f, 0.85f, 0.6f));
            // Lights.
            Lamp("WorkLamp", g, new Vector3(-10.8f, 1.5f, -11.2f), "sodium", 3f);
            Lamp("WorkLamp", g, new Vector3(10.8f, 1.5f, -11.2f), "sodium", 3f);
            PointLight("UnderwaterGlow", g, new Vector3(0f, -1.6f, 3f), Teal, 3.5f, 14f);
            PointLight("GalleryWork", g, new Vector3(9.5f, 9.3f, -6f), Sodium, 2.6f, 8f);
            PointLight("BellFloorCold", g, new Vector3(-7f, 16f, -8f), new Color(0.6f, 0.75f, 1f), 2.2f, 9f);
            PointLight("ChamberCold", g, new Vector3(-8f, 17f, 8f), new Color(0.6f, 0.75f, 1f), 1.8f, 8f);
            PointLight("FaceCold", g, new Vector3(0f, 22f, -10.2f), new Color(0.6f, 0.8f, 1f), 2.5f, 9f);
            SpotLight("RoofHoleShaft", g, new Vector3(0f, 29.5f, 0f), new Vector3(90f, 0f, 0f), new Color(0.6f, 0.75f, 0.95f), 5f, 40f, 35f, true);
            Particles("RoofRain", g, new Vector3(0f, 29f, 0f), new Vector3(6f, 1f, 6f), "rain");
            Particles("FloodMist", g, new Vector3(0f, 0.8f, 2.5f), new Vector3(22f, 1f, 17f), "mist");
            Particles("PipeSteam", g, new Vector3(-11.4f, 4.6f, -5f), new Vector3(0.3f, 0.1f, 0.3f), "steam");
            Ambient("Amb_Flood", shared, new Vector3(0f, 0f, 2f), AmbientKind.Water, 0.45f, TimeMask.Present, 1f, 26f);
            Ambient("Amb_RainHole", shared, new Vector3(0f, 18f, 0f), AmbientKind.Rain, 0.3f, TimeMask.Present, 1f, 24f);
        }

        static void BuildFutureDressing()
        {
            var g = futLook;
            // Hologram of the clock face where the real one used to be.
            var face = Hologram("HoloClockFace", g, new Vector3(0f, 22f, -10.5f), new Vector3(10f, 0.02f, 10f), PrimitiveType.Cylinder, "hologram", false, false);
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ring = Hologram("HoloClockRing", g, new Vector3(0f, 22f, -10.3f), new Vector3(11.5f, 0.02f, 11.5f), PrimitiveType.Cylinder, "hologram_magenta", false, false);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 12f, 0f);
            var hand = Hologram("HoloHand", g, new Vector3(0f, 23.8f, -10.2f), new Vector3(0.25f, 3.6f, 0.02f), PrimitiveType.Cube, "hologram", false, false);
            Label("HoloZero", g, new Vector3(0f, 22f, -10.1f), 180f, "0", 3f, new Color(0.5f, 1f, 1f));
            Label("HoloTime", g, new Vector3(0f, 16.5f, -10.6f), 180f, "VEYRA STANDARD TIME  00:00:00", 0.45f, new Color(1f, 0.5f, 0.9f));
            _ = hand;
            // Neon seams on the pit edge, landing and chamber.
            Deco("PitSeam", g, new Vector3(0f, -2.48f, 11.8f), new Vector3(24f, 0.04f, 0.1f), "neon_magenta");
            Deco("PitSeamW", g, new Vector3(-11.8f, -2.48f, 2.5f), new Vector3(0.1f, 0.04f, 19f), "neon_cyan");
            Deco("ChamberSeam", g, new Vector3(-7.5f, 13.02f, 11.8f), new Vector3(9f, 0.04f, 0.1f), "neon_violet");
            Deco("BellSeam", g, new Vector3(-7f, 13.02f, -11.8f), new Vector3(10f, 0.04f, 0.1f), "neon_cyan");
            // Crystals.
            var rng = new System.Random(77);
            Vector3[] clusters = { new Vector3(-10f, -2.5f, -5f), new Vector3(10f, -2.5f, 10f), new Vector3(-11f, 13f, -11f), new Vector3(-11f, 13f, 11f), new Vector3(11f, 7f, -11f) };
            foreach (var c in clusters)
                for (int i = 0; i < 4; i++)
                {
                    var s = Box("Crystal", g, c + new Vector3(((float)rng.NextDouble() - 0.5f) * 1.2f, 0.5f, ((float)rng.NextDouble() - 0.5f) * 1.2f),
                                new Vector3(0.25f, 0.8f + (float)rng.NextDouble() * 1.4f, 0.25f), i % 2 == 0 ? "crystal" : "neon_violet", false);
                    s.transform.rotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 40f, (float)rng.NextDouble() * 360f, ((float)rng.NextDouble() - 0.5f) * 40f);
                }
            // Floating debris in the shaft and beyond the split.
            for (int i = 0; i < 16; i++)
            {
                bool outside = i >= 9;
                var p = outside
                    ? new Vector3(-20f + (float)rng.NextDouble() * 6f, 14f + (float)rng.NextDouble() * 14f, -14f + (float)rng.NextDouble() * 28f)
                    : new Vector3(-1f + (float)rng.NextDouble() * 7f, 15f + (float)rng.NextDouble() * 12f, -6f + (float)rng.NextDouble() * 12f);
                var s = Box("Debris", g, p, new Vector3(0.6f + (float)rng.NextDouble() * 1.8f, 0.3f + (float)rng.NextDouble() * 0.8f, 0.6f + (float)rng.NextDouble() * 1.4f), i % 4 == 0 ? "brick_warm" : "ruin_dark", false);
                s.transform.rotation = Random3(rng);
                var fb = s.AddComponent<FloatBob>(); fb.amplitude = 0.2f + (float)rng.NextDouble() * 0.3f; fb.frequency = 0.2f + (float)rng.NextDouble() * 0.3f;
                if (i % 3 == 0) Box("DebrisSeam", s.transform.parent, p + Vector3.up * 0.02f, new Vector3(0.08f, 0.08f, 1f), "neon_cyan", false).transform.rotation = s.transform.rotation;
            }
            // Floating hologram signage.
            var sign = Hologram("HoloSign", g, new Vector3(9.5f, 10f, 0f), new Vector3(0.05f, 1f, 3f), PrimitiveType.Cube, "hologram_magenta", true, false);
            Label("HoloSignText", g, new Vector3(9.4f, 10f, 0f), 90f, "ESCAPEMENT - OFFLINE", 0.3f, new Color(1f, 0.6f, 0.95f));
            _ = sign;
            Hologram("HoloRing1", g, new Vector3(-7.5f, 15.5f, 8f), new Vector3(2f, 0.02f, 2f), PrimitiveType.Cylinder, "hologram", true, true);
            // Lights.
            PointLight("NeonPit", g, new Vector3(-5f, -1f, 3f), Cyan, 3f, 12f);
            PointLight("NeonGallery", g, new Vector3(9.5f, 9f, 0f), Violet, 3f, 12f);
            PointLight("NeonBell", g, new Vector3(-7f, 15.5f, -8f), Cyan, 2.6f, 10f);
            PointLight("NeonChamber", g, new Vector3(-8f, 16f, 8f), Magenta, 2.4f, 9f);
            PointLight("HoloFaceLight", g, new Vector3(0f, 22f, -9f), Cyan, 3.5f, 12f);
            Particles("Motes", g, new Vector3(0f, 12f, 0f), new Vector3(22f, 24f, 22f), "motes");
            Particles("VoidMist", g, new Vector3(0f, -1.5f, 2f), new Vector3(22f, 1f, 18f), "mist");
            Ambient("Amb_FutureDrone", shared, new Vector3(0f, 8f, 0f), AmbientKind.FutureDrone, 0.4f, TimeMask.Future, 0.3f, 40f);
            Ambient("Amb_FutureWind", shared, new Vector3(-8f, 16f, 0f), AmbientKind.Wind, 0.4f, TimeMask.Future, 1f, 22f);

            // Skyline seen through the split (Future only).
            var sky = Group("FutureSkyline", g);
            var srng = new System.Random(404);
            string[] signs = { "ZERO", null, "VEYRA 0", null, "AFTER", null, "NO SECOND BELL", null };
            for (int i = 0; i < 5; i++)
            {
                float ang = (i / 5f) * Mathf.PI * 2f + 0.3f;
                float r = 42f + (float)srng.NextDouble() * 18f;
                var p = new Vector3(Mathf.Cos(ang) * r, -6f, Mathf.Sin(ang) * r);
                Building("FutTower" + i, sky, p, new Vector3(10f, 30f + (float)srng.NextDouble() * 35f, 10f), "obsidian", i % 2 == 0 ? "neon_cyan" : "window_cold", "neon_magenta", 500 + i, signs[i], i % 2 == 0 ? "neon_magenta" : "neon_cyan");
            }
        }

        // =================================================================
        // Puzzle flow
        // =================================================================

        static void BuildGameplay()
        {
            // --- The wedge (PAST) ---
            var wedgeHolder = Group("Wedge_Past", pastPlay);
            var wedge = Clue("GearWedge", wedgeHolder, new Vector3(-10.35f, 0.05f, 5.4f), new Vector3(0.35f, 0.5f, 0.9f), "wood_dark",
                "Pull the wedge free", F_Wedge, "L02_wedge", "The Keeper's Wedge",
                "A hardwood wedge, hammered deliberately between the great gear and its pinion - not dropped, not fallen. " +
                "The blows are clean. Cut into its face: the initials I.M. and the clock-tower seal (0).", true, TimeMask.Past);
            wedge.cue = Cue.Clang;
            wedge.disableAfterUse = wedge.gameObject;
            // Once pulled, the wedge lies on the pit floor and can be carried (object-specific line from Ida).
            var looseWedge = Grab("KeeperWedge", wedgeHolder, new Vector3(-9.3f, -2.3f, 5.0f), new Vector3(0.9f, 0.3f, 0.35f), "wood_dark", "keeper_wedge", "keeper's wedge");
            looseWedge.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Past;
            looseWedge.resetOnLoop = false;
            Label("WedgeMark", wedgeHolder, new Vector3(-10.1f, 0.65f, 5.4f), -90f, "I.M. (0)", 0.16f, new Color(1f, 0.9f, 0.6f));
            Label("GearTag", wedgeHolder, new Vector3(-10.4f, 5.4f, 2f), -90f, "GREAT DRIVE GEAR", 0.3f, new Color(1f, 0.9f, 0.6f));

            var freed = Reactor("React_GearFreed", logic, F_Wedge);
            freed.reversible = false;
            freed.announce = true;
            freed.cue = Cue.RuleChange;
            freed.toast = "The great gear turns. In every state, the works below the water are no longer jammed.";
            freed.enableWhenTrue.Add(looseWedge.gameObject);
            var wedgeMarker = wedgeHolder.Find("GearWedge_Marker");
            if (wedgeMarker != null) freed.disableWhenTrue.Add(wedgeMarker.gameObject);

            // --- The drain valve (PRESENT) ---
            var valveHolder = Group("Valve_Present", presPlay);
            Box("ValveColumn", valveHolder, new Vector3(5f, 2.1f, -7.6f), new Vector3(0.45f, 1.2f, 0.45f), "iron_rust");
            var valve = Use("DrainValve", valveHolder, new Vector3(5f, 2.55f, -7.3f), new Vector3(0.9f, 0.06f, 0.9f), "copper_verdigris",
                "Open the drain valve", F_Valve, TimeMask.Present, PrimitiveType.Cylinder);
            valve.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            valve.requiredFlags.Add(F_Wedge);
            valve.lockedMessage = "The valve wheel will not turn. Something deep in the works is jammed - something older than the flood.";
            valve.animateTarget = valve.transform;
            valve.rotateBy = new Vector3(0f, 540f, 0f);
            valve.animateDuration = 2.5f;
            valve.cue = Cue.Scrape;
            Label("ValveTag", valveHolder, new Vector3(5f, 3.3f, -7.3f), 0f, "DRAIN", 0.3f, Color.white);

            valve.oneShot = false; // the flood can return: the valve must be usable again
            valve.usedPrompt = "Open the drain valve again";

            // --- Flood water: own controller (drain, refill countdown, deep-water wall) ---
            var surf = Box("FloodSurface", presPlay, new Vector3(0f, 0.6f, 2.55f), new Vector3(24f, 0.05f, 18.9f), "water", false);
            surf.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(surf, 0);
            // Tall invisible wall (top 4.5) so the player cannot step up onto it and walk over the water.
            var blocker = Box("FloodBlocker", logic, new Vector3(0f, 1.0f, 2.55f), new Vector3(24f, 7f, 18.9f), "concrete");
            Strip(blocker);
            var flood = Group("Flood", logic).gameObject.AddComponent<L02Flood>();
            flood.surface = surf.transform;
            flood.blocker = blocker.GetComponent<Collider>();
            flood.highY = 0.6f; flood.lowY = -2.45f; flood.speed = 0.45f; flood.blockAboveY = -2f;
            flood.blockIn = TimeMask.Present;
            flood.lowWhenAll.Add(F_Wedge); flood.lowWhenAll.Add(F_Valve);
            flood.drainedFlag = F_Drained;
            flood.refillSeconds = 75f;
            flood.stopFlag = F_Gallery;
            flood.clearOnRefill.Add(F_Valve); // water rises again; L02.chamberDrained stays set (objective done once)
            flood.sweepArea = new Bounds(new Vector3(0f, 1.5f, 2.5f), new Vector3(24f, 9f, 19.5f));
            flood.refillMessage = "The flood is coming back! Get up to the gallery - the PAST gears are the only way.";
            flood.sweptMessage = "The water came back. Drain it again and climb faster: ramp, mid platform, then the PAST gears.";

            var drained = Reactor("React_Drained", logic, F_Drained);
            drained.reversible = false;
            drained.toast = "The pit is dry - but the sluice is already leaking. The water will return. Climb!";

            // --- Future unlock (safe: the drained pit floor exists in every state) ---
            var unlock = Group("StateUnlock_Future", logic).gameObject.AddComponent<StateUnlock>();
            unlock.whenFlag = F_Drained;
            unlock.unlock = TimeMask.Future;
            unlock.toast = "NEW STATE: FUTURE >>.  Press [3] (or [R]) to shift to the FUTURE, where the tower is split open. Try it here, on solid ground.";
            var teach = Group("Teach_Future", logic).gameObject.AddComponent<MemorySequence>();
            teach.triggerFlag = F_Drained;
            teach.lockControls = false;
            teach.startCue = Cue.RuleChange;
            teach.lines.Add(L("The Witness", "Now the third state, Arin. What the tower becomes after the second bell.", 4f));
            teach.lines.Add(L("The Witness", "Don't be afraid of it. I have stood in those ruins more times than you have.", 4f));
            teach.lines.Add(L("Arin", "Past, Present, Future. Same stones, three different towers.", 3.5f));
            teach.lines.Add(L("The Witness", "Look for what still spans the gaps. Ruins keep their bridges in strange places.", 4f));

            // --- Steam vents on the gallery (PRESENT, telegraphed) ---
            Vent("VentA", 3f, 0f);
            Vent("VentB", -3.5f, 2.2f);

            // --- Optional objects for the NPCs' object-specific lines ---
            var oil = Grab("OilCan", presPlay, new Vector3(-10f, 1.68f, -9.2f), new Vector3(0.25f, 0.35f, 0.25f), "brass", "oil_can", "old oil can", PrimitiveType.Cylinder);
            oil.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Present;
            oil.resetOnLoop = false;
            Label("OilCanTag", presPlay, new Vector3(-10f, 2.2f, -9.2f), 0f, "I.M.", 0.15f, new Color(1f, 0.9f, 0.6f));
            var shard = Grab("BellShardPickup", futPlay, new Vector3(-5f, 13.2f, -11.2f), new Vector3(0.5f, 0.3f, 0.12f), "brass", "bell_shard", "bell shard");
            shard.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Future;
            shard.resetOnLoop = false;
            Hologram("BellShardRing", futPlay, new Vector3(-5f, 13.6f, -11.2f), new Vector3(0.6f, 0.02f, 0.6f), PrimitiveType.Cylinder, "hologram_amber", true, true);

            // --- Zones, checkpoints ---
            Zone("Zone_Gallery", logic, new Vector3(9.5f, 8.5f, -1.8f), new Vector3(5f, 3f, 20.4f), F_Gallery, "The gallery. The flood can't reach you here.");
            Zone("Zone_Chamber", logic, new Vector3(-7.5f, 14.5f, 7.5f), new Vector3(8f, 3f, 5f), F_Beam, "The keeper's chamber.");
            Zone("Zone_Archive", logic, new Vector3(-7.5f, 14.5f, 14f), new Vector3(4.6f, 3f, 1.2f), F_Archive);
            Checkpoint("CP_Valve", logic, new Vector3(5f, 1.5f, -10f), new Vector3(4f, 3f, 3f), 0f);
            Checkpoint("CP_Gallery", logic, new Vector3(9.5f, 7f, -1f), new Vector3(4f, 3f, 3f), 180f);
            Checkpoint("CP_BellFloor", logic, new Vector3(-7f, 13f, -9.5f), new Vector3(6f, 3f, 3f), 0f);
            Checkpoint("CP_Chamber", logic, new Vector3(-7.5f, 13f, 7.5f), new Vector3(6f, 3f, 4f), 0f);

            // --- The clockkeeper's recording ---
            Solid("KeeperDesk", new Vector3(-10.4f, 13.45f, 9.6f), new Vector3(2.2f, 0.9f, 1.1f), "wood", "wood_dark", "obsidian");
            // The recording cylinder sits in the bell console on the bell floor, behind Calder's shutter.
            Solid("BellConsole", new Vector3(-10.6f, 13.45f, -10.6f), new Vector3(1.0f, 0.9f, 0.8f), "brass", "iron_rust", "obsidian");
            var rec = Use("KeeperRecording", shared, new Vector3(-10.6f, 14.05f, -10.6f), new Vector3(0.55f, 0.2f, 0.55f), "brass",
                "Play the clockkeeper's recording", F_RecPlayed, TimeMask.All, PrimitiveType.Cylinder);
            rec.cue = Cue.Click;
            rec.usedPrompt = "Replay the recording";
            rec.lines.Add(L("Ilse Marrow", "(the cylinder crackles)", 1.5f));
            var horn = Cylinder("PhonographHorn", pastLook, new Vector3(-10.1f, 14.6f, 9.4f), 0.28f, 0.8f, "brass", false);
            horn.transform.rotation = Quaternion.Euler(0f, 0f, -50f);
            var hornP = Cylinder("PhonographHorn", presLook, new Vector3(-10.1f, 14.6f, 9.4f), 0.28f, 0.8f, "copper_verdigris", false);
            hornP.transform.rotation = Quaternion.Euler(0f, 0f, -50f);
            Hologram("RecordingHolo", futLook, new Vector3(-10.6f, 14.5f, -10.6f), new Vector3(0.7f, 0.02f, 0.7f), PrimitiveType.Cylinder, "hologram_amber", true, true);
            Label("RecordingTag", shared, new Vector3(-10.6f, 15.6f, -10.0f), 0f, "RECORDING - I. MARROW", 0.18f, new Color(1f, 0.95f, 0.75f));
            PointLight("RecordingGlow", shared, new Vector3(-10f, 15.2f, -9.6f), new Color(1f, 0.8f, 0.5f), 1.5f, 4f);
            var shutter = Door("CaldersShutter", shared, new Vector3(-10.6f, 13f, -10.6f), new Vector3(1.6f, 2.3f, 1.5f), "iron_rust", new Vector3(0f, 2.5f, 0f), 0f, F_GateUnlocked);
            Unstatic(shutter.transform);
            shutter.toast = "Calder's shutter rattles up from the bell console.";
            Label("ShutterTag", shared, new Vector3(-10.6f, 14.4f, -9.8f), 0f, "LOCKED - BY ORDER OF THE KEEPERS", 0.14f, new Color(1f, 0.6f, 0.4f));

            var projection = Group("KeeperProjection", logic);
            Hologram("KeeperFigure", projection, new Vector3(-8.4f, 14.1f, -9f), new Vector3(0.55f, 0.85f, 0.55f), PrimitiveType.Capsule, "hologram_amber", true, false);
            Hologram("KeeperHead", projection, new Vector3(-8.4f, 15.25f, -9f), new Vector3(0.3f, 0.3f, 0.3f), PrimitiveType.Sphere, "hologram_amber", true, false);
            Hologram("KeeperRing", projection, new Vector3(-8.4f, 13.1f, -9f), new Vector3(1.4f, 0.02f, 1.4f), PrimitiveType.Cylinder, "hologram", false, true);
            Label("KeeperName", projection, new Vector3(-8.4f, 15.8f, -9f), 0f, "ILSE MARROW - CLOCKKEEPER", 0.2f, new Color(1f, 0.85f, 0.6f));
            PointLight("ProjectionLight", projection, new Vector3(-8.4f, 14.6f, -9.5f), new Color(1f, 0.7f, 0.35f), 2.5f, 6f);

            var mem = Group("Memory_Recording", logic).gameObject.AddComponent<MemorySequence>();
            mem.triggerFlag = F_RecPlayed;
            mem.completeFlag = F_RecHeard;
            mem.showDuring = projection.gameObject;
            mem.lockControls = true;
            mem.lines.Add(L("Ilse Marrow", "This is Ilse Marrow, clockkeeper to the Founders. If this cylinder is turning, someone has climbed up through the flood.", 5f));
            mem.lines.Add(L("Ilse Marrow", "Hear me plainly. There was no earthquake. Whatever they carve on the plaque, there was no earthquake.", 4.5f));
            mem.lines.Add(L("Ilse Marrow", "The rescue is failing. The children are still in the fold between the states, and the second bell would close it on them.", 5.5f));
            mem.lines.Add(L("Ilse Marrow", "So I stopped the tower. From inside. My own wedge, in the great gear, between the first stroke and the second.", 5f));
            mem.lines.Add(L("Ilse Marrow", "Founder Halden has ordered silence. He will flood the pit and call it a quake. Let him. Every hour this tower stands still is an hour for the children.", 6f));
            mem.lines.Add(L("Ilse Marrow", "The rest is written in the rain. The Archive keeps every night, and every night is the same. Go. Read the rain.", 5f));
            mem.lines.Add(L("Ilse Marrow", "If the little ones come home, tell them I kept the hour as long as I could.", 4.5f));
            mem.lines.Add(L("Ilse Marrow", "And you, if it is you... keep time for me, my little A.", 4.5f));
            mem.lines.Add(L("Arin", "My little A. The watch in my coat: 'For A. - keep time for me. I.M.' Ilse Marrow.", 5f));
            mem.lines.Add(L("The Witness", "She bought them time, Arin. Stopped from inside - not destroyed.", 3.5f));
            mem.lines.Add(L("The Witness", "Time is what you are spending now. Spend it well.", 3f));

            // --- Keeper's door -> balcony -> exit ---
            var door = Door("KeeperDoor", shared, new Vector3(-7.5f, 13f, 12.5f), new Vector3(2.5f, 2.6f, 0.5f), "wood_dark", new Vector3(0f, 2.7f, 0f), 0f, F_RecHeard);
            door.toast = "The keeper's door unbolts. The archive bridge lies beyond.";
            Label("KeeperDoorSeal", shared, new Vector3(-7.5f, 14.6f, 12.2f), 0f, "(0)", 0.6f, new Color(1f, 0.85f, 0.5f));
            door.disableWhenTrue.Add(shared.Find("KeeperDoorSeal").gameObject);
            Exit("Exit_RainArchive", logic, new Vector3(-7.5f, 13f, 15.8f), new Vector3(4f, 3f, 1.6f),
                 "The keeper's door is bolted. Hear the clockkeeper's recording first.", F_RecHeard);

            BuildGearStairs();
            BuildClimax();
        }

        static void Vent(string name, float z, float offset)
        {
            var root = Group(name, logic);
            root.position = new Vector3(9.5f, 7f, z);
            Box("Grate", root, new Vector3(9.5f, 7.02f, z), new Vector3(4.6f, 0.04f, 1.2f), "metal_dark", false);
            var pipe = Cylinder("VentPipe", root, new Vector3(11.7f, 8.5f, z), 0.3f, 3f, "iron_rust", false);
            Label("VentWarn", root, new Vector3(11.9f, 9.6f, z), 90f, "STEAM VENT\nWAIT FOR THE RED LIGHT", 0.22f, new Color(1f, 0.8f, 0.6f));
            Box("VentStripe", root, new Vector3(9.5f, 7.03f, z - 0.75f), new Vector3(4.6f, 0.03f, 0.12f), "warning_red", false);
            Box("VentStripe", root, new Vector3(9.5f, 7.03f, z + 0.75f), new Vector3(4.6f, 0.03f, 0.12f), "warning_red", false);

            var h = root.gameObject.AddComponent<TelegraphedHazard>();
            h.idleTime = 2.6f; h.warnTime = 1.5f; h.activeTime = 1.6f; h.startOffset = offset;
            h.activeIn = TimeMask.Present;
            h.warnLight = PointLight("WarnLight", root, new Vector3(9.5f, 8.2f, z), new Color(1f, 0.15f, 0.1f), 4f, 6f);
            var warn = Particles("WarnSteam", root, new Vector3(9.5f, 7.1f, z), new Vector3(4f, 0.1f, 0.8f), "steam");
            var wm = warn.main; wm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); wm.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            var we = warn.emission; we.rateOverTime = 14f;
            h.warnParticles = warn;
            var act = Particles("BlastSteam", root, new Vector3(9.5f, 7.1f, z), new Vector3(4f, 0.1f, 0.9f), "steam");
            var am = act.main; am.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f); am.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f); am.startLifetime = 1.2f;
            var ae = act.emission; ae.rateOverTime = 160f;
            h.activeParticles = act;
            var dz = TriggerBox("DamageZone", root, new Vector3(9.5f, 8.2f, z), new Vector3(5f, 2.4f, 1.4f));
            h.damageZone = dz;
            h.shakeTarget = pipe.transform;
            h.deathMessage = "Scalded. The vent glows red and hisses before it blows - cross while it is dark.";
            _ = am; _ = ae;
        }

        // =================================================================
        // NPCs
        // =================================================================

        static NPCPersona Persona(NPCMotor m, string task, string changedFlag, string[] approach, SubtitleLine[] talk, ConditionalLines[] cond, SubtitleLine[] changedLines)
        {
            var p = m.GetComponent<NPCPersona>();
            p.task = task;
            p.approachBarks.AddRange(approach);
            p.talkLines.AddRange(talk);
            p.conditional.AddRange(cond);
            p.changedLines.AddRange(changedLines);
            m.changedFlag = changedFlag;
            return p;
        }

        static ConditionalLines WhenFlag(string flag, params SubtitleLine[] lines) =>
            new ConditionalLines { whenFlag = flag, lines = new List<SubtitleLine>(lines) };

        static ConditionalLines WhenHolding(string key, params SubtitleLine[] lines) =>
            new ConditionalLines { whenHolding = key, lines = new List<SubtitleLine>(lines) };

        static void MakeHologram(NPCMotor m, string key = "hologram")
        {
            foreach (var r in m.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                r.sharedMaterial = AZMaterials.Hologram(key);
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void BuildNpcs()
        {
            // --- Ida Kell, Ilse Marrow's apprentice (PAST, gear pit) ---
            var ida = Npc("Ida Kell", pastPlay, new Vector3(-8.3f, -2.5f, 3f), -90f, "coat_ochre", true, 21, "brass");
            Route(ida, false,
                (new Vector3(-8.3f, -2.5f, 3f), 6f, NPCActivity.Work, "Oil on the drive teeth, oil on the pinion..."),
                (new Vector3(-4f, -2.5f, -2f), 4f, NPCActivity.Look, ""),
                (new Vector3(-8.6f, -2.5f, 7.6f), 5f, NPCActivity.Work, "Who jammed this? It wasn't here at dawn."));
            Route(ida, true,
                (new Vector3(-7.4f, -2.5f, 2f), 5f, NPCActivity.Look, "It turns! Listen to it sing."),
                (new Vector3(-3f, -2.5f, 4f), 5f, NPCActivity.Work, ""),
                (new Vector3(-7f, -2.5f, -3f), 5f, NPCActivity.Carry, ""));
            Persona(ida, "Ilse Marrow's apprentice: oils the great drive gear and keeps the works turning before the festival bell.", F_Wedge,
                new[] { "Mind your sleeves near the teeth!", "Are you from the guild?", "The keeper sent everyone up to the square." },
                new[]
                {
                    L("Ida", "You shouldn't be down in the pit during the festival. Keeper Marrow sent everyone up to the square."),
                    L("Ida", "Everyone but herself. She stayed down here alone, with the door to her quarters locked. And now the great gear won't turn.", 5f),
                    L("Ida", "I've oiled it twice. Something's caught between the gear and the pinion, down by the west wall. I can't reach it with my arm in this sling.", 5f),
                    L("Ida", "She's been strange for weeks. Writing in her logbook at night. Asking the Founders' runners how many children were on the list.", 4.5f),
                    L("Ida", "Founder Halden came down yesterday. They argued by the pinion. I only heard one word: 'silence'."),
                    L("Ida", "If you see the keeper, tell her the bell is meant to ring once tonight. Once. She'll know what I mean."),
                },
                new[]
                {
                    WhenHolding("keeper_wedge",
                        L("Ida", "That's the keeper's own wedge. She cuts her mark into all her tools - I.M., and the (0)."),
                        L("Ida", "She jammed it in herself? Then the tower didn't fail. She stopped it. On purpose.", 4f)),
                    WhenHolding("carved_zero",
                        L("Ida", "Her carved zero! She never goes anywhere without it. She says it's 'the shape of a promise'."),
                        L("Ida", "If she left it behind in her quarters, then she isn't planning to come back up.", 4f)),
                },
                new[]
                {
                    L("Ida", "Listen to it! The whole tower breathing again."),
                    L("Ida", "Whatever the keeper was afraid of... I hope you know what you've started."),
                });

            // --- Bram Kettle, maintenance worker (PRESENT, landing) ---
            var bram = Npc("Bram Kettle", presPlay, new Vector3(7f, 1.5f, -8.2f), 0f, "coat_olive", true, 22, "iron_rust");
            Route(bram, false,
                (new Vector3(7f, 1.5f, -8.2f), 4f, NPCActivity.Carry, "Another bucket for the river."),
                (new Vector3(10.4f, 1.5f, -10.8f), 4f, NPCActivity.Work, "Pump's primed. Not that it matters."),
                (new Vector3(-4f, 1.5f, -8.3f), 5f, NPCActivity.Look, "Same height every night. Every single night."));
            Route(bram, true,
                (new Vector3(-1.8f, 1.5f, -7.7f), 8f, NPCActivity.Look, "Dry. The pit is dry. I can see the floor tiles."),
                (new Vector3(-9.5f, 1.5f, -10.2f), 6f, NPCActivity.Sit, ""),
                (new Vector3(5.5f, 1.5f, -10.5f), 4f, NPCActivity.Idle, ""));
            Persona(bram, "Maintenance worker: bails floodwater with a bucket and keeps the hand pump primed, every night.", F_Drained,
                new[] { "Careful, the landing's slick.", "Clockworks are closed, friend.", "Water's cold enough to stop a heart." },
                new[]
                {
                    L("Bram", "Clockworks are closed. Flooded to the stair since the quake."),
                    L("Bram", "That's what they told us. An earthquake split the tower. Funny kind of quake that only floods one room.", 4.5f),
                    L("Bram", "The drain valve's right there. Won't budge. Something deep down is locking the whole works."),
                    L("Bram", "My father bailed this pit before me. Same bucket, same mark on the wall. The water never rises and it never falls.", 4.5f),
                    L("Bram", "There's a little room behind the landing - the old keeper's quarters. Nobody goes in. The Founders' office sealed it, and then forgot to.", 5f),
                    L("Bram", "Heard the bell rang only once down in the Arrival Quarter tonight. Mirela's cup, still in one piece. Was that you?", 4.5f),
                    L("Bram", "Ilse Marrow, the last keeper. They say she went mad and broke the clock. I say she was the only sane one in the building.", 5f),
                },
                new[]
                {
                    WhenHolding("oil_can",
                        L("Bram", "Where'd you find that? That's old Marrow's oil can - her initials on the lid."),
                        L("Bram", "She never let anyone else oil the drive gear. Said the tower would stop the day she did. Then it stopped anyway.", 4.5f)),
                    WhenHolding("nameplate",
                        L("Bram", "Her nameplate. 'I. MARROW - KEEPER'. They pried that off the door the week after the quake."),
                        L("Bram", "Funny thing to bother hiding, a name. Unless the name is the thing you want people to forget.", 4.5f)),
                },
                new[]
                {
                    L("Bram", "You drained it! Thirty years of buckets and you just... turned it."),
                    L("Bram", "The maintenance stair goes up to the escapement gallery. Mind the steam pipes - they glow and hiss before they blow.", 4.5f),
                });

            // --- Echo of Ilse Marrow (FUTURE, bell floor) ---
            var echo = Npc("Echo of Ilse Marrow", futPlay, new Vector3(-9f, 13f, -10f), 0f, "coat_white", true, 23);
            MakeHologram(echo, "hologram_amber");
            Route(echo, false,
                (new Vector3(-9f, 13f, -10f), 5f, NPCActivity.Work, "Second stroke. It must not come."),
                (new Vector3(-5f, 13f, -6f), 4f, NPCActivity.Look, ""),
                (new Vector3(-10.5f, 13f, -6.5f), 5f, NPCActivity.Work, "Hold on, little ones. Hold on."));
            Route(echo, true,
                (new Vector3(-7.5f, 13f, -5f), 20f, NPCActivity.Look, "You heard it. Then I can stop walking."),
                (new Vector3(-9f, 13f, -8f), 10f, NPCActivity.Idle, ""),
                (new Vector3(-6f, 13f, -10f), 10f, NPCActivity.Sit, ""));
            Persona(echo, "A looping projection of the clockkeeper Ilse Marrow, still walking her last rounds of the bell floor.", F_RecHeard,
                new[] { "...the bell must not ring twice...", "Who walks my floor?", "Second stroke... second stroke..." },
                new[]
                {
                    L("Echo of Ilse Marrow", "...the bell must not ring twice... the bell must not ring twice..."),
                    L("Echo of Ilse Marrow", "My recording is in my chamber, across the gap. Only the fallen beam still reaches it.", 4f),
                    L("Echo of Ilse Marrow", "Halden wanted silence. Silence is easy. Silence is what the bell makes after the second stroke.", 4.5f),
                    L("Echo of Ilse Marrow", "There were names on the list. Small names. I read them every night so that someone would.", 4f),
                    L("Echo of Ilse Marrow", "My brother runs the trains. He thinks they are festival specials. Do not tell him what they are for.", 4.5f),
                },
                new[]
                {
                    WhenHolding("bell_shard",
                        L("Echo of Ilse Marrow", "A shard of my bell. It cracked on the first stroke. I heard it crack, and I knew the second must never come.", 5f)),
                    WhenHolding("zero_echo",
                        L("Echo of Ilse Marrow", "My zero... even here it keeps its shape. I carved it the night they gave me the keys."),
                        L("Echo of Ilse Marrow", "Nothing, held open. That is all a rescue is, in the end.", 3.5f)),
                },
                new[]
                {
                    L("Echo of Ilse Marrow", "Now someone knows. Take it to the Archive."),
                    L("Echo of Ilse Marrow", "The rain remembers what the Founders erased."),
                });

            // --- Background life ---
            Crowd("ClockCrew_Past", pastLook, new Vector3(0f, -2.5f, 1f), new Vector2(12f, 8f), 4, 211,
                  new[] { "Did the gear just move?", "Back to work - the bell's at sundown.", "Who's that?" }, "wood");
            Crowd("GalleryCrew_Past", pastLook, new Vector3(9.5f, 7f, 0f), new Vector2(2f, 16f), 2, 212,
                  new[] { "Escapement's ticking.", "Hand me that spanner." }, "brass");
            Crowd("LandingCrew_Past", pastLook, new Vector3(5f, 1.5f, -10.2f), new Vector2(8f, 2f), 2, 216,
                  new[] { "Festival specials leave at dusk, they say.", "The keeper's in a mood." }, "paper");
            Crowd("Workers_Present", presLook, new Vector3(2f, 1.5f, -9.9f), new Vector2(14f, 2f), 2, 213,
                  new[] { "Did you hear that?", "The pump's never sounded like that." }, "iron_rust");
            var echoes = Crowd("Echoes_Future", futLook, new Vector3(0f, -2.5f, 2f), new Vector2(12f, 8f), 3, 214,
                  new[] { "...sundown...", "...the gear...", "...again..." }, "wood");
            foreach (var e in echoes) MakeHologram(e);
            var echoes2 = Crowd("GalleryEchoes_Future", futLook, new Vector3(9.5f, 7f, 0f), new Vector2(2f, 14f), 1, 215,
                  new[] { "...escapement..." }, "brass");
            foreach (var e in echoes2) MakeHologram(e, "hologram_magenta");
        }

        // =================================================================
        // Optional side area: the clockkeeper's quarters (off the entry landing, south)
        // =================================================================

        static void BuildQuarters()
        {
            // Room x -11..-3, z -19..-13, floor 1.5; doorway in the tower's south wall at x -8..-6.
            Solid("QuartersFloor", new Vector3(-7f, -0.75f, -16.2f), new Vector3(8.8f, 4.5f, 6.4f), "wood", "stone_wet", "obsidian");
            Solid("QuartersWallW", new Vector3(-11.2f, 3.6f, -16.2f), new Vector3(0.4f, 4.2f, 6.4f), "plaster_warm", "plaster_cold", "obsidian");
            Solid("QuartersWallE", new Vector3(-2.8f, 3.6f, -16.2f), new Vector3(0.4f, 4.2f, 6.4f), "plaster_warm", "plaster_cold", "obsidian");
            Solid("QuartersWallS", new Vector3(-7f, 3.6f, -19.2f), new Vector3(8.8f, 4.2f, 0.4f), "plaster_warm", "plaster_cold", "obsidian");
            Solid("QuartersCeiling", new Vector3(-7f, 5.9f, -16.2f), new Vector3(8.8f, 0.4f, 6.4f), "wood_dark", "concrete", null);
            Label("QuartersSign", shared, new Vector3(-7f, 4.45f, -11.95f), 180f, "KEEPER'S QUARTERS", 0.25f, Color.white);
            Zone("Zone_Quarters", logic, new Vector3(-7f, 3f, -16f), new Vector3(7.5f, 3f, 5f), "L02.quartersVisited", "The clockkeeper's quarters.")
                .lines.Add(L("Arin", "Her rooms. Small. A bed, a desk, and a window painted shut."));

            // Furniture (shared collider, per-state look).
            Solid("Bed", new Vector3(-9.9f, 1.85f, -17.6f), new Vector3(1.8f, 0.7f, 2.4f), "fabric_red", "mud", "ruin_dark");
            Solid("Desk", new Vector3(-4.2f, 1.95f, -18.3f), new Vector3(2f, 0.9f, 1f), "wood", "wood_dark", "obsidian");
            Solid("Shelf", new Vector3(-3.3f, 3f, -15f), new Vector3(0.6f, 3f, 2.4f), "wood_dark", "iron_rust", "obsidian");
            Deco("Window", pastLook, new Vector3(-7f, 3.4f, -18.98f), new Vector3(1.6f, 1.2f, 0.05f), "window_warm");
            Deco("Window", presLook, new Vector3(-7f, 3.4f, -18.98f), new Vector3(1.6f, 1.2f, 0.05f), "window_cold");
            Deco("Rug", pastLook, new Vector3(-7f, 1.51f, -16f), new Vector3(3f, 0.02f, 2f), "fabric_teal");
            Deco("WaterStain", presLook, new Vector3(-6f, 1.51f, -16.5f), new Vector3(3.5f, 0.02f, 2.5f), "water");
            Deco("FallenBooks", presLook, new Vector3(-4f, 1.6f, -15.2f), new Vector3(1.2f, 0.2f, 0.8f), "paper", 20f);
            Hologram("HoloLog", futLook, new Vector3(-4.2f, 3f, -18.3f), new Vector3(1.4f, 0.9f, 0.02f), PrimitiveType.Cube, "hologram_amber", true, false);
            for (int i = 0; i < 3; i++)
                Box("Crystal", futLook, new Vector3(-10.5f + i * 0.4f, 2f, -13.8f), new Vector3(0.22f, 1f + i * 0.4f, 0.22f), "crystal", false, i * 30f);
            PointLight("QuartersLamp", pastLook, new Vector3(-7f, 4.8f, -16f), Warm, 2.2f, 8f);
            PointLight("QuartersCold", presLook, new Vector3(-7f, 4.8f, -16f), new Color(0.6f, 0.75f, 1f), 1.6f, 8f);
            Ambient("Amb_QuartersDrip", shared, new Vector3(-7f, 3f, -16f), AmbientKind.Water, 0.18f, TimeMask.Present, 1f, 8f);

            // Logbook pages (readable in every state).
            Clue("LogPage1", shared, new Vector3(-4.8f, 2.42f, -18.2f), new Vector3(0.45f, 0.05f, 0.34f), "paper", "Read the logbook page", "L02.log1",
                "L02_log1", "Keeper's Logbook - the Grinding",
                "Day 1. The three states rub at the seams. The Engine sings a quarter-tone flat and the escapement shivers at the second stroke.\n\n" +
                "Founder Halden came down himself. He asked me to write nothing. I am writing this.\n\n- I.M.");
            Clue("LogPage2", shared, new Vector3(-9.6f, 2.23f, -17.2f), new Vector3(0.45f, 0.05f, 0.34f), "paper", "Read the logbook page", "L02.log2",
                "L02_log2", "Keeper's Logbook - the List",
                "Day 31. The evacuation list is drawn up, nine days before the date they will give the public. Families first, quietly. The children before anyone.\n\n" +
                "They are to be kept in a place with no address until the states settle. Ignatz is to run the trains. He thinks they are festival specials.\n\n- I.M.");
            Clue("LogPage3", shared, new Vector3(-3.8f, 2.42f, -18.25f), new Vector3(0.34f, 0.05f, 0.45f), "paper", "Read the last logbook page", "L02.log3",
                "L02_log3", "Keeper's Logbook - Last Entry",
                "The rescue is failing. The pocket is folding. If the bell strikes twice tonight they will be lost between the states.\n\n" +
                "I will stop the tower myself, between the strokes. Halden orders silence. Let him call it an earthquake.\n\n" +
                "If anyone reads this: I am sorry for the flood. The flood is his. The silence is his. The wedge is mine.\n\n" +
                "My little A. is on the list too. I gave A. my watch to hold - 'keep time for me' - and promised to come for it.\n\n- Ilse Marrow");

            // Object-specific keepsakes, one per state.
            var zero = Grab("CarvedZero", pastPlay, new Vector3(-9.6f, 2.3f, -18.3f), new Vector3(0.4f, 0.06f, 0.4f), "brass", "carved_zero", "carved zero", PrimitiveType.Cylinder);
            zero.resetOnLoop = false;
            var plate = Grab("Nameplate", presPlay, new Vector3(-7.6f, 1.6f, -13.6f), new Vector3(0.6f, 0.04f, 0.2f), "brass", "nameplate", "keeper's nameplate");
            plate.resetOnLoop = false;
            Label("NameplateTag", presPlay, new Vector3(-7.6f, 1.9f, -13.6f), 0f, "I. MARROW - KEEPER", 0.12f, new Color(1f, 0.9f, 0.6f));
            var zf = Grab("ZeroEcho", futPlay, new Vector3(-4.3f, 2.5f, -18.5f), new Vector3(0.4f, 0.06f, 0.4f), "neon_cyan", "zero_echo", "glowing zero", PrimitiveType.Cylinder);
            zf.resetOnLoop = false;
        }

        // =================================================================
        // Memory fragments (3, off the main path)
        // =================================================================

        static void BuildFragments()
        {
            var g = Group("MemoryFragments", logic);
            Fragment(g, new Vector3(-10.2f, 2.6f, -14f), "L02.1", "The Keeper's Coin",
                "My mother cleaned the keeper's lamps. The night of the festival Keeper Marrow pressed a coin into her hand and said: " +
                "take your children to the station, not the square. Mother laughed. We went to the square.");
            Fragment(g, new Vector3(10.6f, -1.4f, 10.4f), "L02.2", "The Mark on the Wall",
                "Thirty years I've bailed this pit. Every morning the water sits at the same chalk mark. Water doesn't remember. This water does. " +
                "Some mornings I find a child's shoe on the stair, and every morning it is the same shoe.");
            Fragment(g, new Vector3(11.2f, 8.2f, -11.3f), "L02.3", "Two Strokes",
                "I counted the bell from the gallery. One. Then the gears screamed and the second stroke never came - " +
                "and still the city fell, slower, like a held breath. Someone above us was counting too. Someone with a zero on their palm.");
        }
    }
}
