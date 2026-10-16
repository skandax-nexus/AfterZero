using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 02 - Under the Clock Skin (Founders' Clockworks, inside the clock tower).
    /// Three-state vertical traversal: wedge (Past) -> drain (Present) -> steam gallery -> beam (Future) -> recording.
    ///
    /// Tower interior x/z -12..12, walls to y 30.
    ///   Entry landing   z -12..-7, top y 1.5 (spawn, safe observation over the flood)
    ///   Gear pit        y -2.5 (dry in the Past, flooded in the Present)
    ///   Maintenance ramp (Present/Future) north side, pit -> gallery
    ///   Escapement gallery  east side x 7..12, top y 7 (steam vents in the Present)
    ///   Stairs (south) gallery -> bell floor  x -12..-2, z -12..-4, top y 13
    ///   Gap z -4..4: floor in the Past only, a beam in the Future only, nothing in the Present (kill volume below)
    ///   Keeper's chamber  x -12..-3, z 4..12, top y 13; keeper's door north -> balcony -> exit to the Rain Archive
    /// </summary>
    public static partial class Level02Builder
    {
        static Transform shared, pastLook, presLook, futLook, pastPlay, presPlay, futPlay, logic;

        const string F_Wedge = "L02.wedgeRemoved";
        const string F_Valve = "L02.valveOpened";
        const string F_Drained = "L02.chamberDrained";
        const string F_BellFloor = "L02.bellFloorReached";
        const string F_Beam = "L02.beamCrossed";
        const string F_RecPlayed = "L02.recordingPlayed";
        const string F_RecHeard = "L02.recordingHeard";
        const string F_Archive = "L02.toArchive";
        const string Clue_Wedge = "clue.L02_wedge";
        const string F_Gallery = "L02.galleryReached";
        const string F_CalderCut = "L02.calderCut";
        const string F_Lever = "L02.leverPulled";
        const string F_Restarted = "L02.clockworkRestarted";
        const string F_PowerOut = "L02.powerOut";
        const string F_GateUnlocked = "L02.calderBoltDrawn";

        static readonly Color Warm = new Color(1f, 0.72f, 0.4f);
        static readonly Color Sodium = new Color(1f, 0.6f, 0.28f);
        static readonly Color Teal = new Color(0.35f, 0.8f, 0.85f);
        static readonly Color Cyan = new Color(0.3f, 0.9f, 1f);
        static readonly Color Magenta = new Color(1f, 0.3f, 0.85f);
        static readonly Color Violet = new Color(0.6f, 0.4f, 1f);

        public static void Build(Transform root)
        {
            var d = Systems(root, 1, TimeMask.Past | TimeMask.Present, TimeState.Present, out var visuals);
            Tune(visuals);

            d.storySetup = "The tower is intact in Past, flooded in Present, and split open in Future.";
            d.introLines.Add(L("Arin", "The clockworks. Flooded right up to the stair."));
            d.introLines.Add(L("The Witness", "In the PAST this tower was whole. In the PRESENT it drowns. There is a third state, Arin. Not yet.", 4.5f));
            d.introLines.Add(L("Arin", "Then I start where it was whole. Something down in that pit is holding the water."));


            AddObjective(d, "Remove the wedge from the great gear in the PAST", F_Wedge, "Shift to the PAST [1]. The pit is dry there - go down the stairs to the great gear on the west wall.");
            AddObjective(d, "Drain the flooded chamber in the PRESENT", F_Drained, "With the gear free, return to the PRESENT [2] and open the drain valve on the landing.");
            AddObjective(d, "Climb before the flood returns: ramp, then the PAST gears", F_Gallery, "In the PRESENT take the broken maintenance ramp to the mid platform, then shift to the PAST and ride the turning gears up to the gallery.");
            AddObjective(d, "Climb past the steam vents to the bell stairs", F_BellFloor, "The vents hiss and glow red before they blow. Cross while they are dark - or cross where they do not exist.");
            AddObjective(d, "Calder cut the power: restart the PAST clockwork", F_Restarted, "Go back down to the north end of the gallery. In the PAST, pull the clockwork engagement lever.");
            AddObjective(d, "Ride the PAST lift, then cross the gusting beam in the FUTURE", F_Beam, "Ride the counterweight lift up to the escapement ledge. Shift to the FUTURE and cross between gusts: banners whip and the wind howls before each one. Wait at the shelter halfway.");
            AddObjective(d, "Draw Calder's bolt from the keeper's chamber in the PAST", F_GateUnlocked, "Inside the chamber, shift to the PAST: the bolt winch is only intact there.");
            AddObjective(d, "Recover the clockkeeper's recording", F_RecHeard, "The recording is in the bell console on the bell floor. Cross the gap on the FUTURE beam.");
            AddObjective(d, "Reach the Rain Archive", F_Archive, "The keeper's door in the north wall of the chamber leads out to the archive bridge.");

            d.payoffFlag = F_RecHeard;
            d.payoffClueTitle = "Stopped From Inside";
            d.payoffClueText = "There was no earthquake. Ilse Marrow, the Founders' clockkeeper, stopped the tower herself, driving her own wedge into the great gear " +
                               "between the bell's first and second stroke - to buy time for the children still caught between the states. " +
                               "Founder Halden ordered silence, flooded the pit and called it a quake. The tower was stopped from inside.";

            Player(root, new Vector3(0f, 1.55f, -10.5f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0f, 1.5f, -10.5f), new Vector3(6f, 3f, 3f), 0f, true);

            shared = Group("Shared", root);
            pastLook = StateGroup("PastDressing", root, TimeMask.Past, false);
            presLook = StateGroup("PresentDressing", root, TimeMask.Present, false);
            futLook = StateGroup("FutureDressing", root, TimeMask.Future, false);
            pastPlay = StateGroup("PastGameplay", root, TimeMask.Past, true);
            presPlay = StateGroup("PresentGameplay", root, TimeMask.Present, true);
            futPlay = StateGroup("FutureGameplay", root, TimeMask.Future, true);
            logic = Group("Logic", root);

            BuildShell();
            BuildFloors();
            BuildClockwork();
            BuildPastDressing();
            BuildPresentDressing();
            BuildFutureDressing();
            BuildGameplay();
            BuildNpcs();
            BuildQuarters();
            BuildFragments();

            // Calder's power cut stops every PAST gear that the wedge set turning.
            foreach (var g in new[] { pastPlay, pastLook })
                foreach (var sp in g.GetComponentsInChildren<Spinner>(true))
                    if (sp.startWhenFlag == F_Wedge) sp.stopWhenFlag = F_PowerOut;
        }

        static void Unstatic(Transform t)
        {
            foreach (var tr in t.GetComponentsInChildren<Transform>(true))
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
        }

        static void Tune(TemporalVisuals v)
        {
            v.past.fogDensity = 0.008f;
            v.past.sunIntensity = 0.9f;
            v.past.bloomIntensity = 1.0f;
            v.present.fogDensity = 0.016f;
            v.present.sunIntensity = 0.3f;
            v.present.bloomIntensity = 1.2f;
            v.future.fogDensity = 0.011f;
            v.future.bloomIntensity = 2.4f;
            v.future.sunIntensity = 0.5f;
        }

        // =================================================================
        // Helpers
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

        /// <summary>Collider shared by all states; one visual per state (null = no visual in that state).</summary>
        static void Solid(string name, Vector3 c, Vector3 s, string pastMat, string presMat, string futMat, float yaw = 0f)
        {
            var col = Box(name + "_Col", shared, c, s, "concrete", true, yaw);
            Strip(col);
            if (pastMat != null) Box(name, pastLook, c, s, pastMat, false, yaw);
            if (presMat != null) Box(name, presLook, c, s, presMat, false, yaw);
            if (futMat != null) Box(name, futLook, c, s, futMat, false, yaw);
        }

        static GameObject Deco(string name, Transform g, Vector3 c, Vector3 s, string mat, float yaw = 0f) => Box(name, g, c, s, mat, false, yaw);

        static void SharedStairs(string name, Vector3 start, float yaw, int steps, float width, string pastMat, string presMat, string futMat)
        {
            var s = Stairs(name + "_Col", shared, start, yaw, steps, 0.2f, 0.3f, width, "concrete");
            foreach (Transform t in s) Strip(t.gameObject);
            foreach (var (g, m) in new[] { (pastLook, pastMat), (presLook, presMat), (futLook, futMat) })
            {
                var v = Stairs(name, g, start, yaw, steps, 0.2f, 0.3f, width, m);
                foreach (Transform t in v) StripCollider(t.gameObject);
            }
        }

        /// <summary>A gear: disc + teeth, rotating about its own axis. axisEuler orients the cylinder's Y axis.</summary>
        static Transform Gear(string name, Transform parent, Vector3 center, float radius, Vector3 axisEuler, string mat, int teeth, string startFlag, float speed, float thickness = 0.4f)
        {
            var g = Group(name, parent);
            g.position = center;
            g.rotation = Quaternion.Euler(axisEuler);
            var disc = Prim(PrimitiveType.Cylinder, "Disc", g, center, new Vector3(radius * 2f, thickness * 0.5f, radius * 2f), mat, false, false);
            disc.transform.rotation = g.rotation;
            var hub = Prim(PrimitiveType.Cylinder, "Hub", g, center, new Vector3(radius * 0.5f, thickness * 0.8f, radius * 0.5f), "metal_dark", false, false);
            hub.transform.rotation = g.rotation;
            for (int i = 0; i < teeth; i++)
            {
                float a = i * Mathf.PI * 2f / teeth;
                Vector3 local = new Vector3(Mathf.Cos(a) * (radius + 0.12f), 0f, Mathf.Sin(a) * (radius + 0.12f));
                var t = Prim(PrimitiveType.Cube, "Tooth", g, g.TransformPoint(local), new Vector3(0.3f, thickness * 0.9f, 0.28f), mat, false, false);
                t.transform.rotation = g.rotation * Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            // Spokes (read as a gear even at a distance).
            for (int i = 0; i < 3; i++)
            {
                var sp = Prim(PrimitiveType.Cube, "Spoke", g, center, new Vector3(radius * 1.8f, thickness * 1.05f, 0.18f), "metal_dark", false, false);
                sp.transform.rotation = g.rotation * Quaternion.Euler(0f, i * 60f, 0f);
            }
            if (speed != 0f)
            {
                var sp = g.gameObject.AddComponent<Spinner>();
                sp.degreesPerSecond = new Vector3(0f, speed, 0f);
                sp.startWhenFlag = startFlag ?? "";
            }
            return g;
        }

        static void Rail(string name, Vector3 a, Vector3 b, string pastMat, string presMat, string futMat)
        {
            Vector3 c = (a + b) * 0.5f + Vector3.up * 0.5f;
            Vector3 d = b - a;
            Vector3 size = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Abs(d.x), 1f, 0.12f) : new Vector3(0.12f, 1f, Mathf.Abs(d.z));
            Solid(name, c, size, pastMat, presMat, futMat);
        }

        // =================================================================
        // Tower shell
        // =================================================================

        static void BuildShell()
        {
            // Colliders cover the full height in every state; visuals differ (Future: split open).
            void WallPiece(string n, Vector3 c, Vector3 s, float futTop)
            {
                var col = Box(n + "_Col", shared, c, s, "concrete"); Strip(col);
                Deco(n, pastLook, c, s, "brick_warm");
                Deco(n, presLook, c, s, "stone_wet");
                float bottom = c.y - s.y * 0.5f;
                float top = Mathf.Min(c.y + s.y * 0.5f, futTop);
                if (top > bottom)
                {
                    var fc = new Vector3(c.x, (bottom + top) * 0.5f, c.z);
                    var fs = new Vector3(s.x, top - bottom, s.z);
                    Deco(n, futLook, fc, fs, "obsidian");
                    // Jagged broken crown + neon seam along the break.
                    var rng = new System.Random(n.GetHashCode());
                    bool alongX = s.x > s.z;
                    float len = alongX ? s.x : s.z;
                    int chunks = Mathf.Max(2, Mathf.FloorToInt(len / 3f));
                    for (int i = 0; i < chunks; i++)
                    {
                        float t = -len * 0.5f + (i + 0.5f) * len / chunks;
                        float h = 0.5f + (float)rng.NextDouble() * 3.5f;
                        Vector3 p = new Vector3(alongX ? c.x + t : c.x, top + h * 0.5f, alongX ? c.z : c.z + t);
                        Deco("Jag", futLook, p, alongX ? new Vector3(len / chunks * 0.8f, h, s.z) : new Vector3(s.x, h, len / chunks * 0.8f), "ruin_dark", (float)rng.NextDouble() * 8f);
                    }
                    Deco(n + "_Seam", futLook, new Vector3(c.x, top + 0.04f, c.z), alongX ? new Vector3(s.x, 0.06f, s.z + 0.1f) : new Vector3(s.x + 0.1f, 0.06f, s.z), "neon_cyan");
                }
            }

            // South wall, with a doorway (x -8..-6, y 1.5..4.1) from the landing into the keeper's quarters.
            WallPiece("WallS_L", new Vector3(-10.5f, 13.5f, -12.5f), new Vector3(5f, 33f, 1f), 18f);
            WallPiece("WallS_R", new Vector3(3.5f, 13.5f, -12.5f), new Vector3(19f, 33f, 1f), 18f);
            WallPiece("WallS_Low", new Vector3(-7f, -0.75f, -12.5f), new Vector3(2f, 4.5f, 1f), 18f);
            WallPiece("WallS_High", new Vector3(-7f, 17.05f, -12.5f), new Vector3(2f, 25.9f, 1f), 18f);
            WallPiece("WallE", new Vector3(12.5f, 13.5f, 0f), new Vector3(1f, 33f, 24f), 23f);
            WallPiece("WallW", new Vector3(-12.5f, 13.5f, 0f), new Vector3(1f, 33f, 24f), 17f);
            // North wall with the keeper's door (x -8.75..-6.25, y 13..15.6).
            WallPiece("WallN_L", new Vector3(-10.875f, 13.5f, 12.5f), new Vector3(4.25f, 33f, 1f), 19f);
            WallPiece("WallN_R", new Vector3(3.375f, 13.5f, 12.5f), new Vector3(19.25f, 33f, 1f), 20f);
            WallPiece("WallN_Low", new Vector3(-7.5f, 5f, 12.5f), new Vector3(2.5f, 16f, 1f), 19f);
            WallPiece("WallN_High", new Vector3(-7.5f, 22.8f, 12.5f), new Vector3(2.5f, 14.4f, 1f), 19f);

            // Roofs: Past whole with a skylight, Present holed (rain falls in), Future none (night sky).
            Deco("Roof", pastLook, new Vector3(0f, 30.2f, 0f), new Vector3(26f, 0.5f, 26f), "wood_dark");
            Deco("Skylight", pastLook, new Vector3(0f, 29.9f, 0f), new Vector3(6f, 0.1f, 6f), "window_warm");
            Deco("RoofN", presLook, new Vector3(0f, 30.2f, 8f), new Vector3(26f, 0.5f, 10f), "metal_dark");
            Deco("RoofS", presLook, new Vector3(0f, 30.2f, -8f), new Vector3(26f, 0.5f, 10f), "metal_dark");
            Deco("RoofW", presLook, new Vector3(-8f, 30.2f, 0f), new Vector3(10f, 0.5f, 6f), "metal_dark");
            Deco("RoofE", presLook, new Vector3(8f, 30.2f, 0f), new Vector3(10f, 0.5f, 6f), "metal_dark");

            // Interior arched windows (east wall).
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(11.95f, 21f, -6f + i * 6f);
                Deco("Window", pastLook, p, new Vector3(0.1f, 5f, 2f), "window_warm");
                Deco("Window", presLook, p, new Vector3(0.1f, 5f, 2f), "window_cold");
            }

            // Inner clock face on the south wall (backlit).
            var faceP = Cylinder("ClockFaceBack", pastLook, new Vector3(0f, 22f, -11.9f), 5f, 0.15f, "window_warm", false);
            faceP.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var faceN = Cylinder("ClockFaceBack", presLook, new Vector3(0f, 22f, -11.9f), 5f, 0.15f, "window_cold", false);
            faceN.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("FaceZero", pastLook, new Vector3(0f, 25.5f, -11.7f), 180f, "0", 1.6f, new Color(0.25f, 0.15f, 0.05f));
            Label("FaceZero", presLook, new Vector3(0f, 25.5f, -11.7f), 180f, "0", 1.6f, new Color(0.1f, 0.15f, 0.2f));
            foreach (var g in new[] { pastLook, presLook })
            {
                var h = Box("FaceHand", g, new Vector3(0f, 23.6f, -11.75f), new Vector3(0.3f, 3.4f, 0.08f), "metal_dark", false);
                h.transform.RotateAround(new Vector3(0f, 22f, -11.75f), Vector3.forward, 20f);
            }
        }

        // =================================================================
        // Floors, stairs, ramp, beam
        // =================================================================

        static void BuildFloors()
        {
            Solid("PitFloor", new Vector3(0f, -2.75f, 2.5f), new Vector3(24f, 0.5f, 19f), "stone_warm", "tile_cold", "obsidian");
            Solid("EntryLanding", new Vector3(0f, -0.75f, -9.5f), new Vector3(24f, 4.5f, 5f), "stone_warm", "stone_wet", "ruin_dark");
            Deco("LandingEdge", pastLook, new Vector3(0f, 1.52f, -7.15f), new Vector3(24f, 0.04f, 0.3f), "brass");
            Deco("LandingEdge", presLook, new Vector3(0f, 1.52f, -7.15f), new Vector3(24f, 0.04f, 0.3f), "neon_amber");
            Deco("LandingEdge", futLook, new Vector3(0f, 1.52f, -7.15f), new Vector3(24f, 0.04f, 0.3f), "neon_cyan");
            SharedStairs("PitStairs", new Vector3(0f, -2.5f, -1f), 180f, 20, 3f, "wood", "iron_rust", "obsidian");
            Rail("LandingRailW", new Vector3(-12f, 1.5f, -7.1f), new Vector3(-1.6f, 1.5f, -7.1f), "brass", "iron_rust", "obsidian");
            Rail("LandingRailE", new Vector3(1.6f, 1.5f, -7.1f), new Vector3(12f, 1.5f, -7.1f), "brass", "iron_rust", "obsidian");

            // Escapement gallery (east), all states.
            // (z -12..8.4; the north end is the counterweight lift shaft, the gap in the rail at z -2..0.4 is where the PAST gears arrive)
            Solid("Gallery", new Vector3(9.5f, 6.75f, -1.8f), new Vector3(5f, 0.5f, 20.4f), "wood", "metal_dark", "obsidian");
            Rail("GalleryRailS", new Vector3(7.05f, 7f, -9.2f), new Vector3(7.05f, 7f, -2.0f), "brass", "iron_rust", "obsidian");
            Rail("GalleryRailN", new Vector3(7.05f, 7f, 0.4f), new Vector3(7.05f, 7f, 8.4f), "brass", "iron_rust", "obsidian");
            Deco("GalleryTrim", futLook, new Vector3(7.05f, 7.02f, -1.8f), new Vector3(0.1f, 0.05f, 20.4f), "neon_magenta");
            for (int i = 0; i < 4; i++)
                Solid("GallerySupport", new Vector3(7.4f, 2.2f, -6f + i * 4.6f), new Vector3(0.4f, 8.6f, 0.4f), "wood_dark", "iron_rust", "obsidian");

            // Escapement ledge (y 13) above the gallery: reached only by the PAST counterweight lift. The gust beam starts here.
            Solid("EscapementLedge", new Vector3(9.5f, 12.75f, 6.5f), new Vector3(5f, 0.5f, 3.8f), "wood", "metal_dark", "obsidian");
            Rail("LedgeRailS", new Vector3(7f, 13f, 4.65f), new Vector3(12f, 13f, 4.65f), "brass", "iron_rust", "obsidian");
            Rail("LedgeRailW1", new Vector3(7.05f, 13f, 4.65f), new Vector3(7.05f, 13f, 5.9f), "brass", "iron_rust", "obsidian");
            Rail("LedgeRailW2", new Vector3(7.05f, 13f, 7.1f), new Vector3(7.05f, 13f, 8.35f), "brass", "iron_rust", "obsidian");
            Label("LedgeSign", shared, new Vector3(11.9f, 14.6f, 6.5f), 90f, "ESCAPEMENT LEDGE", 0.25f, Color.white);

            // Maintenance ramp (Present + Future only - it did not exist in the Past).
            // It now ends at a broken mid platform (y 2.6): above it only the PAST gears climb on to the gallery.
            var a = new Vector3(-9f, -2.5f, 10.5f);
            var b = new Vector3(-1f, 2.6f, 10.5f);
            float rampLen = (b - a).magnitude;
            Ramp("MaintenanceRamp", presPlay, a, b, 2.5f, "metal_dark");
            Ramp("MaintenanceRamp", futPlay, a, b, 2.5f, "ruin_dark");
            Deco("RampRailP", presLook, (a + b) * 0.5f + new Vector3(0f, 0.55f, -1.25f), new Vector3(0.06f, 0.06f, rampLen), "iron_rust").transform.rotation = Quaternion.LookRotation((b - a).normalized);
            Deco("RampTrimF", futLook, (a + b) * 0.5f + new Vector3(0f, 0.05f, -1.25f), new Vector3(0.06f, 0.06f, rampLen), "neon_cyan").transform.rotation = Quaternion.LookRotation((b - a).normalized);
            Solid("MidPlatform", new Vector3(1f, 2.35f, 10.5f), new Vector3(4f, 0.5f, 3f), "wood_dark", "metal_dark", "ruin_dark");
            Solid("MidPlatformPost", new Vector3(2.8f, -0.1f, 11.8f), new Vector3(0.3f, 4.4f, 0.3f), "wood_dark", "iron_rust", "obsidian");
            Rail("MidRailN", new Vector3(-1f, 2.6f, 11.9f), new Vector3(3f, 2.6f, 11.9f), "brass", "iron_rust", "obsidian");
            Deco("BrokenStairStub", presLook, new Vector3(3.6f, 3.1f, 10.5f), new Vector3(1.2f, 0.2f, 2.2f), "iron_rust", 0f).transform.rotation = Quaternion.Euler(0f, 0f, -25f);
            Label("RampSign", presLook, new Vector3(-8.5f, -0.6f, 9.1f), 0f, "MAINTENANCE STAIR - BROKEN ABOVE", 0.25f, new Color(1f, 0.85f, 0.6f));
            Label("MidSign", shared, new Vector3(1f, 4.2f, 11.9f), 0f, "THE OLD WAY UP WAS THE GEARS", 0.2f, new Color(1f, 0.9f, 0.7f));

            // Stairs gallery -> bell floor along the south wall.
            SharedStairs("BellStairs", new Vector3(7f, 7f, -10.5f), -90f, 30, 2.5f, "wood", "iron_rust", "obsidian");

            // Bell floor (south-west) and keeper's chamber floor (north-west).
            Solid("BellFloor", new Vector3(-7f, 12.75f, -8f), new Vector3(10f, 0.5f, 8f), "wood", "metal_dark", "obsidian");
            Rail("BellRailE", new Vector3(-2.05f, 13f, -9.1f), new Vector3(-2.05f, 13f, -4.1f), "brass", "iron_rust", "obsidian");
            Solid("ChamberFloor", new Vector3(-7.5f, 12.75f, 8f), new Vector3(9f, 0.5f, 8f), "wood_dark", "stone_wet", "obsidian");
            Solid("BellFloorSupport", new Vector3(-2.4f, 3f, -4.4f), new Vector3(0.6f, 19.5f, 0.6f), "wood_dark", "iron_rust", "obsidian");
            Solid("ChamberSupport", new Vector3(-3.4f, 3f, 4.4f), new Vector3(0.6f, 19.5f, 0.6f), "wood_dark", "iron_rust", "obsidian");

            // The gap. PAST: the floor is whole. FUTURE: one fallen beam. PRESENT: nothing.
            Box("GapFloor", pastPlay, new Vector3(-7f, 12.75f, 0f), new Vector3(10f, 0.5f, 8f), "wood");
            Beam("FutureBeam", futPlay, new Vector3(-7.5f, 13f, -4.2f), new Vector3(-7.5f, 13f, 4.2f), 0.75f, "obsidian");
            Box("BeamTrimL", futPlay, new Vector3(-7.89f, 13f, 0f), new Vector3(0.05f, 0.06f, 8.4f), "neon_cyan", false);
            Box("BeamTrimR", futPlay, new Vector3(-7.11f, 13f, 0f), new Vector3(0.05f, 0.06f, 8.4f), "neon_cyan", false);
            for (int i = 0; i < 4; i++)
                Hologram("BeamChevron", futPlay, new Vector3(-7.5f, 13.6f, -3f + i * 2f), new Vector3(0.4f, 0.03f, 0.4f), PrimitiveType.Cylinder, "hologram", true, true);
            PointLight("BeamLight", futPlay, new Vector3(-7.5f, 14.5f, 0f), Cyan, 3f, 8f);
            // Present: broken joist stubs + a warning sign.
            for (int i = 0; i < 4; i++)
                Deco("Joist", presLook, new Vector3(-11f + i * 2.6f, 12.6f, -3.3f), new Vector3(0.3f, 0.3f, 1.4f), "wood_dark", (i % 2 == 0 ? 8f : -6f));
            Label("GapWarn", presLook, new Vector3(-7f, 14.2f, -4.1f), 0f, "FLOOR GONE", 0.35f, new Color(1f, 0.6f, 0.4f));
            Label("GapWarnF", futLook, new Vector3(-9.5f, 14.2f, -4.1f), 0f, "BEAM >> KEEPER", 0.3f, new Color(0.6f, 1f, 1f));
            KillPlane(logic, new Vector3(-7f, 10.2f, 0f), new Vector3(10f, 1f, 8f), "The floor is not there in this state. Find a state where something spans the gap.");

            // Keeper's chamber walls.
            Solid("ChamberWallS_L", new Vector3(-10.375f, 16f, 4.2f), new Vector3(3.25f, 6f, 0.4f), "brick_warm", "stone_wet", "obsidian");
            Solid("ChamberWallS_R", new Vector3(-4.875f, 16f, 4.2f), new Vector3(2.75f, 6f, 0.4f), "brick_warm", "stone_wet", "obsidian");
            Solid("ChamberWallS_Top", new Vector3(-7.5f, 17.3f, 4.2f), new Vector3(2.5f, 3.4f, 0.4f), "brick_warm", "stone_wet", null);
            // East wall with a doorway (z 5.4..7.6) where the gust beam from the escapement ledge arrives.
            Solid("ChamberWallE_S", new Vector3(-3.3f, 16f, 4.9f), new Vector3(0.4f, 6f, 1.0f), "brick_warm", "stone_wet", "obsidian");
            Solid("ChamberWallE_N", new Vector3(-3.3f, 16f, 9.8f), new Vector3(0.4f, 6f, 4.4f), "brick_warm", "stone_wet", "obsidian");
            Solid("ChamberWallE_Top", new Vector3(-3.3f, 17.3f, 6.5f), new Vector3(0.4f, 3.4f, 2.2f), "brick_warm", "stone_wet", null);
            Deco("ChamberCeiling", pastLook, new Vector3(-7.5f, 19.2f, 8f), new Vector3(9.4f, 0.4f, 8.4f), "wood_dark");
            Deco("ChamberCeiling", presLook, new Vector3(-7.5f, 19.2f, 8f), new Vector3(9.4f, 0.4f, 8.4f), "concrete");
            Label("ChamberSign", shared, new Vector3(-7.5f, 16.2f, 3.95f), 0f, "KEEPER", 0.35f, Color.white);
            // Past: the keeper's door is locked from inside.
            Box("KeeperDoorPast", pastPlay, new Vector3(-7.5f, 14.3f, 4.2f), new Vector3(2.5f, 2.6f, 0.3f), "wood_dark");
            Label("KeeperDoorNote", pastPlay, new Vector3(-7.5f, 14.6f, 4.02f), 0f, "LOCKED FROM INSIDE\nDO NOT DISTURB", 0.2f, new Color(1f, 0.9f, 0.7f));

            // Balcony outside the keeper's door + archive bridge.
            Solid("Balcony", new Vector3(-7.5f, 12.75f, 15f), new Vector3(5f, 0.5f, 4f), "stone_warm", "stone_wet", "obsidian");
            Rail("BalconyRailW", new Vector3(-10f, 13f, 13.1f), new Vector3(-10f, 13f, 17f), "brass", "iron_rust", "obsidian");
            Rail("BalconyRailE", new Vector3(-5f, 13f, 13.1f), new Vector3(-5f, 13f, 17f), "brass", "iron_rust", "obsidian");
            Solid("BalconyEnd", new Vector3(-7.5f, 14.5f, 17.1f), new Vector3(5f, 3f, 0.2f), null, null, null);
            Deco("ArchiveBridge", shared, new Vector3(-7.5f, 12.6f, 30f), new Vector3(3f, 0.4f, 26f), "concrete");
            Deco("ArchiveBridgeTrim", futLook, new Vector3(-7.5f, 12.82f, 30f), new Vector3(3.1f, 0.05f, 26f), "neon_violet");
            Building("RainArchive", shared, new Vector3(-7.5f, 0f, 50f), new Vector3(18f, 22f, 12f), "stone_wet", "window_cold", "copper_verdigris", 202, "RAIN ARCHIVE", "neon_cyan");
            Label("ArchiveArrow", shared, new Vector3(-7.5f, 15.8f, 16.9f), 0f, "TO THE RAIN ARCHIVE >>", 0.3f, new Color(0.7f, 0.95f, 1f));
        }
    }
}
