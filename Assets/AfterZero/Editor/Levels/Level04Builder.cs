using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 04 - The Street Beneath the Street (Abandoned Tram Line).
    /// Layout (+Z = north): raised maintenance gallery (start, y 4) over the Depot (z 5..30) ->
    /// Crossing hall with three live tram tracks (z 30..52) -> Station concourse + platform (z 52..84)
    /// with the stationmaster's office and the lift shaft -> lift bottom (y -10) -> Drowned District exit.
    /// PAST: working line, trams running, commuters. PRESENT: derelict, flooded, the vibration machine
    /// patrols. FUTURE: tunnels split open, crystal growth, holograms, an alternate crossing bridge.
    /// </summary>
    public static partial class Level04Builder
    {
        static readonly Color Warm = new Color(1f, 0.8f, 0.55f);
        static readonly Color Sodium = new Color(1f, 0.6f, 0.3f);
        static readonly Color Cyan = new Color(0.3f, 0.9f, 1f);
        static readonly Color Magenta = new Color(1f, 0.3f, 0.8f);
        static readonly Color Red = new Color(1f, 0.15f, 0.1f);

        public static void Build(Transform root)
        {
            var d = Systems(root, 3, TimeMask.All, TimeState.Present, out var visuals);
            Tune(visuals);

            d.storySetup = "Under Veyra runs the abandoned tram line. A blind maintenance machine still patrols it, tracking every vibration through the rails. Somewhere in the station office is a list of who was meant to be evacuated.";
            d.introLines.Add(L("Arin", "A maintenance gallery. Good - from up here I can watch it before it hears me."));
            d.introLines.Add(L("Arin", "It has no eyes. It listens to the rails. Every footstep is a knock on its door."));
            d.introLines.Add(L("Arin", "Crouch [C] and I should be nearly silent. Watch the rail strips: they pulse when it's close."));

            AddObjective(d, "Watch the machine and read the rail pulses", "L04.pulsesRead", "Use the pulse monitor on the gallery railing.");
            AddObjective(d, "Cross the tram lines between the trains", "L04.crossedTracks", "The PRESENT crossing is buried. In the PAST [1] trams run - wait on the refuge islands for the signals to read GO. The FUTURE [3] has a crystal bridge with its own rhythm.");
            AddObjective(d, "Reroute the loose cable to power the lift", "L04.liftPowered", "In the PRESENT, take the sparking cable end from the dead junction (north-west corner) to the lift power coupling beside the shaft.");
            AddObjective(d, "Create a false vibration to lure the machine away", "L04.falseVibration", "Pull the gantry release by the platform edge: the pipe falls on the far rail at the north end.");
            AddObjective(d, "Recover the evacuation list from the station office", "L04.listRecovered", "The office is only open in the PRESENT. Go in while the machine is busy at the far rail - and out of the Keepers' lantern light.");
            AddObjective(d, "Confront Founder Halden's echo (FUTURE, by the lift)", "L04.haldenDone", "Shift to the FUTURE [3]; the echo flickers on the platform south-east of the lift gate.");
            AddObjective(d, "Hold the lift circuit: throw both breakers while Calder hunts you", "L04.circuitHeld", "In the PRESENT: breaker A is on the west wall, breaker B at the platform edge. Sprint - Calder is faster when you hesitate.");
            AddObjective(d, "Ride the lift down through the collapsing shaft", "L04.liftArrived", "Press DESCEND, then watch the warning lights: debris falls on one half of the car at a time.");

            d.payoffFlag = "L04.listRecovered";
            d.payoffClueTitle = "My name on the list";
            d.payoffClueText = "Evacuation list, Line 4. 214 names, sorted by tram car. Car 3: the Oskel family - Tamsin, Joss and Maren, age 7. And: VALE, A., age 8. Guardian: I. Marrow. I was one of the children. Signed by Founder Aurel Halden. Dated nine days BEFORE the official date of the Bell Catastrophe. The Founders knew. They chose who would be saved, and told no one.";

            Player(root, new Vector3(2f, 4.05f, 1f), 0f);
            Checkpoint("CP_Start", root, new Vector3(2f, 4f, 1f), new Vector3(6f, 3f, 3f), 0f, true);
            Checkpoint("CP_Crossing", root, new Vector3(0f, 0f, 31f), new Vector3(10f, 3f, 2f), 0f);
            Checkpoint("CP_Station", root, new Vector3(0f, 0f, 53.5f), new Vector3(6f, 3f, 2f), 0f);
            Checkpoint("CP_Climax", root, new Vector3(-3f, 0f, 74.4f), new Vector3(4f, 3f, 1.2f), 0f).climax = true;

            var logic = Group("Logic", root);
            var shared = Group("Shared", root);

            BuildGalleryAndDepot(root, shared, logic);
            BuildCrossing(root, shared, logic);
            BuildStation(root, shared, logic);
            BuildLiftAndExit(root, shared, logic);
            BuildNpcs(root);
            BuildSignalBox(root, shared, logic);
            BuildFragments(root);
            BuildV2(root, shared, logic);
        }

        static void Tune(TemporalVisuals v)
        {
            v.past.fogDensity = 0.01f; v.past.sunIntensity = 0.4f; v.past.ambientSky = new Color(0.55f, 0.47f, 0.38f);
            v.present.fogDensity = 0.026f; v.present.sunIntensity = 0.15f;
            v.present.ambientSky = new Color(0.14f, 0.19f, 0.24f); v.present.ambientEquator = new Color(0.09f, 0.11f, 0.14f);
            v.future.fogDensity = 0.02f; v.future.bloomIntensity = 2.5f;
        }

        // =====================================================================
        // Gallery (safe observation area) + Depot
        // =====================================================================
        static void BuildGalleryAndDepot(Transform root, Transform shared, Transform logic)
        {
            var g = Group("Depot", shared);
            Floor("DepotFloor", g, new Vector3(0f, 0f, 14f), new Vector2(28f, 32f), "concrete");
            Box("GalleryBlock", g, new Vector3(0f, 2f, 1.5f), new Vector3(28f, 4f, 7f), "concrete");
            Box("GalleryDeck", g, new Vector3(0f, 3.97f, 1.5f), new Vector3(28f, 0.1f, 7f), "metal_dark");
            Wall("DepotS", g, new Vector3(-14f, 0f, -2f), new Vector3(14f, 0f, -2f), 9f, 0.5f, "tile_cold");
            Wall("DepotW", g, new Vector3(-14f, 0f, -2f), new Vector3(-14f, 0f, 30f), 9f, 0.5f, "tile_cold");
            WallWithDoor("DepotE", g, new Vector3(14f, 0f, -2f), new Vector3(14f, 0f, 30f), 9f, 0.5f, "tile_cold", 1.8f, 2.6f, 24f / 32f);
            WallWithDoor("DepotN", g, new Vector3(-14f, 0f, 30f), new Vector3(14f, 0f, 30f), 9f, 0.5f, "tile_cold", 4f, 3.4f);
            Stairs("GalleryStair", g, new Vector3(-10.5f, 0f, 11f), 180f, 20, 0.2f, 0.3f, 3f, "metal_dark");
            Box("StairRailW", g, new Vector3(-12.05f, 2.6f, 8f), new Vector3(0.08f, 1.1f, 6.2f), "metal_dark", false).transform.rotation = Quaternion.Euler(-33.7f, 0f, 0f);
            // Gallery railing (gap for the stair).
            Box("RailingE", g, new Vector3(2.5f, 4.55f, 5f), new Vector3(23f, 1.1f, 0.1f), "metal_dark");
            Box("RailingW", g, new Vector3(-13.5f, 4.55f, 5f), new Vector3(1f, 1.1f, 0.1f), "metal_dark");
            Box("RailCap", g, new Vector3(2.5f, 5.12f, 5f), new Vector3(23f, 0.06f, 0.2f), "rail", false);
            Box("GalleryWindowFrame", g, new Vector3(0f, 6.5f, -1.7f), new Vector3(10f, 2f, 0.1f), "window_cold", false);
            Sign(g, new Vector3(0f, 7.6f, 29.7f), 0f, "DEPOT 4 - MAINTENANCE", 0.7f, new Color(0.9f, 0.95f, 1f), "metal_dark");

            // Tracks
            foreach (float x in new[] { -2f, 6f }) Track(g, new Vector3(x, 0f, 5.5f), new Vector3(x, 0f, 29.5f));

            // Pulse monitor (beat 1) + rail pulse strips that react to the sentinel.
            var monitor = Clue("PulseMonitor", g, new Vector3(4f, 4.6f, 4.6f), new Vector3(1.2f, 0.7f, 0.2f), "metal_dark", "Read the pulse monitor", "L04.pulsesRead", "L04.pulses",
                "Rail pulse monitor",
                "LINE 4 MAINTENANCE UNIT - VIBRATION TRACKING.\n\nThe unit is blind. It follows vibration through the rails.\n- Walking: heard at about ten paces.\n- Running: heard across a hall.\n- Crouching [C]: below its threshold.\n- Standing still: silent.\n\nThe cyan strips along the rails pulse faster and brighter the closer the unit is to them. Read the strips; keep the unit in the dark.");
            monitor.lines.Add(L("Arin", "Brighter and faster means closer. Crouch, and I'm just a rumour to it."));
            Label("MonitorTitle", g, new Vector3(4f, 5.15f, 4.48f), 0f, "PULSE MONITOR", 0.22f, Cyan);
            var tz = Zone("CrouchTip", logic, new Vector3(-10.5f, 5f, 4f), new Vector3(3f, 2f, 2f), "L04.crouchTip", "Crouch [C] to move quietly. The machine hears footsteps through the rails.");

            var strips = new List<Vector3>();
            for (int i = 0; i < 6; i++) { strips.Add(new Vector3(-2f, 0f, 7f + i * 4f)); strips.Add(new Vector3(6f, 0f, 7f + i * 4f)); }
            for (int i = 0; i < strips.Count; i++) PulseStrip(g, strips[i], i == 4);
            for (int i = 0; i < 4; i++) PulseStrip(g, new Vector3(-7f + i * 5f, 5.18f, 5f), false, true);

            // Sentinel A patrols the depot floor (Present only).
            Sentinel("Sentinel_Depot", root, logic, new[] { new Vector3(-8f, 0f, 13f), new Vector3(8f, 0f, 13f), new Vector3(8f, 0f, 26f), new Vector3(-8f, 0f, 26f) }, 12f, 0.18f);

            // ---- PAST: working depot. Shunting tram on the west track, workers, warm lamps.
            var past = StateGroup("DepotPast", root, TimeMask.Past, false);
            Box("PastCeiling", past, new Vector3(0f, 9.2f, 14f), new Vector3(28.5f, 0.4f, 32.5f), "tile_cold");
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(i % 2 == 0 ? -6f : 6f, 8.4f, 9f + (i / 2) * 12f);
                Sphere("Globe", past, p, 0.3f, "lamp_warm", false);
                if (i < 2) PointLight("DepotLamp", past, p + Vector3.down * 0.4f, Warm, 3.2f, 20f);
            }
            for (int i = 0; i < 6; i++) Box("TileBand", past, new Vector3(i < 3 ? -13.7f : 13.7f, 2.4f, 4f + (i % 3) * 9f), new Vector3(0.05f, 0.3f, 7f), "brick_warm", false);
            Sign(past, new Vector3(13.65f, 5.5f, 16f), 90f, "VEYRA MUNICIPAL TRAMWAYS", 0.8f, new Color(1f, 0.85f, 0.55f), "wood_dark");
            Tram("ShuntingTram", past, new Vector3(-2f, 0f, 8.5f), new Vector3(-2f, 0f, 24f), 4.5f, 4f, 7f, "coat_ochre", "SHUNTING", out _);
            Ambient("DepotWork", past, new Vector3(4f, 2f, 16f), AmbientKind.Machine, 0.25f, TimeMask.Past, 1f, 22f);

            // ---- PRESENT: derelict, puddles, sodium emergency lamps, drips.
            var pres = StateGroup("DepotPresent", root, TimeMask.Present, false);
            Box("PresentCeiling", pres, new Vector3(0f, 9.2f, 14f), new Vector3(28.5f, 0.4f, 32.5f), "concrete");
            foreach (var p in new[] { new Vector3(-4f, 0f, 16f), new Vector3(9f, 0f, 21f), new Vector3(2f, 0f, 27f), new Vector3(-10f, 0f, 22f) })
                Box("Puddle", pres, p + Vector3.up * 0.02f, new Vector3(3.5f, 0.02f, 2.6f), "water", false);
            Lamp("SodiumA", pres, new Vector3(-13.3f, 0f, 14f), "sodium", 5f);
            var drip = Particles("CeilingDrip", pres, new Vector3(2f, 8.8f, 18f), new Vector3(10f, 0.1f, 8f), "rain");
            var em = drip.emission; em.rateOverTime = 120f;
            Box("WreckedCar", pres, new Vector3(6f, 1.3f, 18f), new Vector3(2.4f, 2.6f, 8f), "iron_rust");
            Box("WreckWindows", pres, new Vector3(4.78f, 1.8f, 18f), new Vector3(0.04f, 0.7f, 6.8f), "window_cold", false);
            Ambient("DepotDrip", pres, new Vector3(0f, 3f, 16f), AmbientKind.Water, 0.25f, TimeMask.Present, 1f, 20f);
            Ambient("DepotMachine", pres, new Vector3(0f, 2f, 20f), AmbientKind.Machine, 0.15f, TimeMask.Present, 0.8f, 24f);

            // ---- FUTURE: ceiling split open, crystals choke the north door (forces Past/Present through it).
            var fut = StateGroup("DepotFuture", root, TimeMask.Future, false);
            Box("CeilingHalfW", fut, new Vector3(-8.5f, 9.2f, 14f), new Vector3(11.5f, 0.4f, 32.5f), "ruin_dark");
            Box("CeilingHalfE", fut, new Vector3(9.5f, 9.2f, 14f), new Vector3(9.5f, 0.4f, 32.5f), "ruin_dark");
            Box("CrackSeamW", fut, new Vector3(-2.7f, 9f, 14f), new Vector3(0.08f, 0.08f, 32f), "neon_cyan", false);
            Box("CrackSeamE", fut, new Vector3(4.7f, 9f, 14f), new Vector3(0.08f, 0.08f, 32f), "neon_magenta", false);
            var rng = new System.Random(404);
            for (int i = 0; i < 8; i++) Crystal(fut, new Vector3(-12f + (i % 4) * 7.5f, 0f, 8f + (i / 4) * 14f + (float)rng.NextDouble() * 3f), 1.2f + (float)rng.NextDouble(), rng, false);
            // Crystal plug in the north doorway (solid).
            Box("CrystalPlug", fut, new Vector3(0f, 1.7f, 29.4f), new Vector3(4.2f, 3.4f, 1.4f), "crystal");
            Crystal(fut, new Vector3(0f, 0f, 28.4f), 2.5f, rng, false);
            Label("PlugNote", fut, new Vector3(0f, 3.9f, 28.2f), 0f, "SEALED BY CRYSTAL\n(try another state)", 0.3f, new Color(0.8f, 0.7f, 1f));
            PointLight("CrystalGlow", fut, new Vector3(0f, 2f, 27f), new Color(0.6f, 0.4f, 1f), 3f, 10f);
            PointLight("MagentaFill", fut, new Vector3(8f, 5f, 18f), Magenta, 2f, 18f);
            Particles("DepotMotes", fut, new Vector3(0f, 4f, 16f), new Vector3(26f, 7f, 26f), "motes");
            var holo = Hologram("DepotHoloSign", fut, new Vector3(0f, 6.5f, 16f), new Vector3(6f, 1.4f, 0.02f), PrimitiveType.Cube, "hologram", true, false);
            Label("DepotHoloText", fut, new Vector3(0f, 6.5f, 15.95f), 0f, "DEPOT 4 // LAST SERVICE: 0", 0.4f, new Color(0.6f, 1f, 1f));
            Ambient("DepotDrone", fut, new Vector3(0f, 4f, 16f), AmbientKind.FutureDrone, 0.3f, TimeMask.Future, 0.6f, 28f);

            ReflectionProbe(shared, new Vector3(0f, 4f, 14f), new Vector3(28f, 9f, 32f));
        }

        // =====================================================================
        // Crossing hall: three live tracks (beat 2)
        // =====================================================================
        static readonly float[] trackZ = { 36f, 42f, 48f };

        static void BuildCrossing(Transform root, Transform shared, Transform logic)
        {
            var g = Group("Crossing", shared);
            Floor("SouthStrip", g, new Vector3(0f, 0f, 31.75f), new Vector2(28f, 3.5f), "concrete");
            Floor("NorthStrip", g, new Vector3(0f, 0f, 51f), new Vector2(28f, 2f), "concrete");
            WallWithDoor("CrossN", g, new Vector3(-14f, 0f, 52f), new Vector3(14f, 0f, 52f), 7f, 0.5f, "tile_cold", 4f, 3.4f);
            Box("CrossCeilingN", g, new Vector3(0f, 7.2f, 51.25f), new Vector3(28.5f, 0.4f, 1.5f), "concrete");
            Box("CrossCeilingShared", g, new Vector3(0f, 7.2f, 31.75f), new Vector3(28.5f, 0.4f, 3.5f), "concrete");
            Sign(g, new Vector3(0f, 4.4f, 29.7f), 0f, "LIVE LINES - USE THE REFUGES", 0.6f, new Color(1f, 0.85f, 0.4f), "metal_dark");
            Sign(g, new Vector3(0f, 4.4f, 52.3f), 180f, "<< CROSSING - DEPOT", 0.6f, new Color(0.9f, 0.95f, 1f), "metal_dark");
            Sign(g, new Vector3(0f, 4.0f, 51.7f), 0f, "LINE 4 STATION  >>", 0.6f, new Color(0.9f, 0.95f, 1f), "metal_dark");

            // Side walls with portals per track.
            foreach (float x in new[] { -14f, 14f })
            {
                float[] solidFrom = { 30f, 37.8f, 43.8f, 49.8f };
                float[] solidTo = { 34.2f, 40.2f, 46.2f, 52f };
                for (int i = 0; i < 4; i++) Wall("CrossSide", g, new Vector3(x, 0f, solidFrom[i]), new Vector3(x, 0f, solidTo[i]), 7f, 0.5f, "tile_cold");
                foreach (float z in trackZ)
                {
                    var lintel = Wall("PortalLintel", g, new Vector3(x, 4f, z - 1.8f), new Vector3(x, 4f, z + 1.8f), 3f, 0.5f, "tile_cold");
                    Label("PortalMark", g, new Vector3(x - Mathf.Sign(x) * 0.3f, 4.4f, z), x < 0 ? -90f : 90f, "DANGER - LIVE LINE", 0.3f, new Color(1f, 0.8f, 0.3f));
                }
                // Hidden tunnels behind the portals (trams wait here, out of sight).
                float s = Mathf.Sign(x);
                Box("TunnelCeiling", g, new Vector3(x + s * 10f, 4.7f, 42f), new Vector3(20f, 0.4f, 17f), "concrete");
                Wall("TunnelEnd", g, new Vector3(x + s * 20f, 0f, 33.5f), new Vector3(x + s * 20f, 0f, 50.5f), 4.5f, 0.5f, "concrete");
                Wall("TunnelSideS", g, new Vector3(x, 0f, 33.5f), new Vector3(x + s * 20f, 0f, 33.5f), 4.5f, 0.5f, "concrete");
                Wall("TunnelSideN", g, new Vector3(x, 0f, 50.5f), new Vector3(x + s * 20f, 0f, 50.5f), 4.5f, 0.5f, "concrete");
            }

            // Past|Present track bed (the Future has a chasm here).
            var bed = StateGroup("TrackBed", root, TimeMask.Past | TimeMask.Present, true);
            Floor("TrackBedFloor", bed, new Vector3(0f, 0f, 41.75f), new Vector2(68f, 16.5f), "concrete");
            Box("CrossCeiling", bed, new Vector3(0f, 7.2f, 42f), new Vector3(28.5f, 0.4f, 17f), "tile_cold");
            foreach (float z in trackZ) Track(bed, new Vector3(-33f, 0f, z), new Vector3(33f, 0f, z));
            foreach (float z in new[] { 39f, 45f })
            {
                Box("RefugeIsland", bed, new Vector3(0f, 0.08f, z), new Vector3(27f, 0.16f, 1.8f), "concrete");
                Box("RefugeEdgeS", bed, new Vector3(0f, 0.17f, z - 0.85f), new Vector3(27f, 0.02f, 0.1f), "neon_amber", false);
                Box("RefugeEdgeN", bed, new Vector3(0f, 0.17f, z + 0.85f), new Vector3(27f, 0.02f, 0.1f), "neon_amber", false);
                for (int k = 0; k < 3; k++)
                    Label("RefugeText", bed, new Vector3(-8f + k * 8f, 0.19f, z), 0f, "REFUGE", 0.5f, new Color(1f, 0.85f, 0.4f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // Kerb lines at the safe strip edges (shape cue: hatched bars).
            for (int k = 0; k < 14; k++)
            {
                Box("HatchS", g, new Vector3(-13f + k * 2f, 0.02f, 33.3f), new Vector3(1f, 0.02f, 0.3f), "neon_amber", false);
                Box("HatchN", g, new Vector3(-13f + k * 2f, 0.02f, 50.2f), new Vector3(1f, 0.02f, 0.3f), "neon_amber", false);
            }

            // ---- PAST: trams run. Signals read STOP / GO.
            var past = StateGroup("CrossingPast", root, TimeMask.Past, false);
            var speeds = new[] { 9f, 12f, 10f };
            var pauses = new[] { 2.5f, 3.5f, 1.5f };
            for (int i = 0; i < 3; i++)
            {
                float z = trackZ[i];
                bool reverse = i == 1;
                var a = new Vector3(reverse ? 28f : -28f, 0f, z);
                var b = new Vector3(reverse ? -28f : 28f, 0f, z);
                var tram = Tram("LineTram" + (i + 1), past, a, b, speeds[i], pauses[i], 10f, i == 1 ? "fabric_red" : "coat_ochre", "LINE " + (i + 2), out var mh);
                Signal(past, new Vector3(-12.5f, 0f, z - 2.4f), tram.transform, "TRACK " + (i + 1));
                Signal(past, new Vector3(12.5f, 0f, z - 2.4f), tram.transform, "TRACK " + (i + 1));
            }
            for (int i = 0; i < 3; i++)
                if (i == 1) PointLight("CrossLamp", past, new Vector3(-8f + i * 8f, 6.6f, 42f), Warm, 3.2f, 26f);
            Ambient("TramRumble", past, new Vector3(0f, 2f, 42f), AmbientKind.Machine, 0.4f, TimeMask.Past, 0.7f, 30f);
            Ambient("PlatformCrowdFar", past, new Vector3(0f, 2f, 51f), AmbientKind.Crowd, 0.15f, TimeMask.Past, 1f, 14f);

            // ---- PRESENT: collapsed ceiling buries the crossing.
            var pres = StateGroup("CrossingPresent", root, TimeMask.Present, true);
            Box("RubbleWall", pres, new Vector3(0f, 2.2f, 33f), new Vector3(28f, 4.4f, 1f), "ruin_dark");
            var rng = new System.Random(4404);
            for (int i = 0; i < 9; i++)
            {
                var chunk = Box("RubbleChunk", pres, new Vector3(-12f + i * 3f, 0.6f + (float)rng.NextDouble() * 3f, 33.6f + (float)rng.NextDouble() * 1.4f), new Vector3(2f, 1.4f, 1.6f), "concrete");
                chunk.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 30f);
            }
            Label("RubbleNote", pres, new Vector3(0f, 2.5f, 32.4f), 0f, "COLLAPSE - LINE CLOSED", 0.5f, new Color(1f, 0.7f, 0.4f));
            Lamp("CrossSodium", pres, new Vector3(-12f, 0f, 31.5f), "sodium", 4f);
            Particles("CollapseDust", pres, new Vector3(0f, 3f, 32f), new Vector3(26f, 4f, 1.5f), "dust");

            // ---- FUTURE: chasm with a crystal bridge on the east side, crystal resonance bursts.
            var fut = StateGroup("CrossingFuture", root, TimeMask.Future, true);
            Floor("CrystalBridge", fut, new Vector3(10f, 0f, 41.75f), new Vector2(2f, 16.5f), "crystal", 0.4f);
            Box("BridgeEdgeW", fut, new Vector3(9f, 0.03f, 41.75f), new Vector3(0.06f, 0.06f, 16.5f), "neon_cyan", false);
            Box("BridgeEdgeE", fut, new Vector3(11f, 0.03f, 41.75f), new Vector3(0.06f, 0.06f, 16.5f), "neon_cyan", false);
            Box("ChasmFloor", fut, new Vector3(0f, -14f, 42f), new Vector3(28f, 1f, 17f), "obsidian", false);
            var crng = new System.Random(4405);
            for (int i = 0; i < 10; i++) Crystal(fut, new Vector3(-12f + i * 2.6f, -13.5f, 35f + (float)crng.NextDouble() * 14f), 2f + (float)crng.NextDouble() * 2f, crng, false);
            PointLight("ChasmGlowA", fut, new Vector3(-5f, -9f, 42f), Magenta, 4f, 18f);
            Box("SplitCeilW", fut, new Vector3(-8f, 7.2f, 42f), new Vector3(12f, 0.4f, 17f), "ruin_dark");
            Particles("ChasmMotes", fut, new Vector3(0f, -3f, 42f), new Vector3(26f, 8f, 15f), "motes");
            var holo = Hologram("BridgeHolo", fut, new Vector3(10f, 3.2f, 33.8f), new Vector3(3.6f, 0.9f, 0.02f), PrimitiveType.Cube, "hologram_magenta", true, false);
            Label("BridgeHoloText", fut, new Vector3(10f, 3.2f, 33.75f), 0f, "RESONANCE: CROSS WHEN THE GLOW FADES", 0.24f, new Color(1f, 0.7f, 1f));
            for (int i = 0; i < 2; i++) ResonanceHazard(fut, new Vector3(10f, 0f, 38.5f + i * 6.5f), i * 1.7f);
            KillPlane(g, new Vector3(0f, -8f, 42f), new Vector3(30f, 2f, 18f), "Between the states there is no floor. Use the crystal bridge.");
            Ambient("ChasmDrone", fut, new Vector3(0f, -2f, 42f), AmbientKind.FutureDrone, 0.35f, TimeMask.Future, 0.7f, 26f);
            Ambient("ChasmWind", fut, new Vector3(0f, 3f, 42f), AmbientKind.Wind, 0.2f, TimeMask.Future, 0.8f, 22f);

            Zone("CrossedZone", logic, new Vector3(0f, 1.5f, 51f), new Vector3(28f, 3f, 1.8f), "L04.crossedTracks", "Across. The station is ahead.").lines.Add(L("The Witness", "The trains ran on time, every one of them. Only their passengers were late."));
            ReflectionProbe(shared, new Vector3(0f, 3.5f, 42f), new Vector3(28f, 7f, 22f));
        }

        // =====================================================================
        // Station: platform, office (list), cable, decoy, second sentinel
        // =====================================================================
        static void BuildStation(Transform root, Transform shared, Transform logic)
        {
            var g = Group("Station", shared);
            Floor("PlatformMain", g, new Vector3(-4.5f, 0f, 65f), new Vector2(19f, 26f), "tile_cold");
            Floor("PlatformNW", g, new Vector3(-10f, 0f, 81f), new Vector2(8f, 6f), "tile_cold");
            Floor("PlatformNE", g, new Vector3(2.5f, 0f, 81f), new Vector2(5f, 6f), "tile_cold");
            Floor("StationTrackBed", g, new Vector3(9.5f, -1.2f, 76f), new Vector2(9f, 48f), "concrete");
            Box("PlatformEdge", g, new Vector3(5f, -0.6f, 68f), new Vector3(0.3f, 1.2f, 32f), "concrete");
            Box("EdgeLine", g, new Vector3(4.6f, 0.01f, 68f), new Vector3(0.4f, 0.02f, 32f), "neon_amber", false);
            Track(g, new Vector3(9.5f, -1.2f, 52.5f), new Vector3(9.5f, -1.2f, 100f));
            Wall("StationW", g, new Vector3(-14f, 0f, 52f), new Vector3(-14f, 0f, 84f), 8f, 0.5f, "tile_cold");
            Wall("StationE", g, new Vector3(14f, -1.2f, 52f), new Vector3(14f, -1.2f, 84f), 9.2f, 0.5f, "tile_cold");
            Wall("StationNW", g, new Vector3(-14f, 0f, 84f), new Vector3(7f, 0f, 84f), 8f, 0.5f, "tile_cold");
            Wall("StationNE", g, new Vector3(12f, -1.2f, 84f), new Vector3(14f, -1.2f, 84f), 9.2f, 0.5f, "tile_cold");
            Wall("PortalLintel", g, new Vector3(7f, 3.3f, 84f), new Vector3(12f, 3.3f, 84f), 4.9f, 0.5f, "tile_cold");
            Box("TrackBedSouthWall", g, new Vector3(9.5f, -0.6f, 52.2f), new Vector3(9f, 1.2f, 0.4f), "concrete");
            // Station tunnel beyond the north portal.
            Box("NTunnelCeiling", g, new Vector3(9.5f, 3.5f, 94f), new Vector3(5.6f, 0.4f, 20f), "concrete");
            Wall("NTunnelW", g, new Vector3(7f, -1.2f, 84f), new Vector3(7f, -1.2f, 104f), 4.6f, 0.4f, "concrete");
            Wall("NTunnelE", g, new Vector3(12f, -1.2f, 84f), new Vector3(12f, -1.2f, 104f), 4.6f, 0.4f, "concrete");
            Wall("NTunnelEnd", g, new Vector3(7f, -1.2f, 104f), new Vector3(12f, -1.2f, 104f), 4.6f, 0.4f, "concrete");
            Sign(g, new Vector3(13.7f, 4f, 68f), 90f, "LINE 4  -  VEYRA DEEP", 1f, new Color(0.95f, 0.95f, 1f), "metal_dark");

            // Office (x -14..-8, z 62..72), door on the east side at z 67.
            var off = Group("Office", g);
            Floor("OfficeFloor", off, new Vector3(-11f, 0.01f, 67f), new Vector2(6f, 10f), "wood_dark", 0.05f);
            WallWithDoor("OfficeE", off, new Vector3(-8f, 0f, 62f), new Vector3(-8f, 0f, 72f), 3.2f, 0.3f, "plaster_cold", 1.6f, 2.5f);
            Wall("OfficeS", off, new Vector3(-14f, 0f, 62f), new Vector3(-8f, 0f, 62f), 3.2f, 0.3f, "plaster_cold");
            Wall("OfficeN", off, new Vector3(-14f, 0f, 72f), new Vector3(-8f, 0f, 72f), 3.2f, 0.3f, "plaster_cold");
            Box("OfficeCeiling", off, new Vector3(-11f, 3.35f, 67f), new Vector3(6.3f, 0.3f, 10.3f), "plaster_cold");
            Box("OfficeDesk", off, new Vector3(-12.2f, 0.4f, 67f), new Vector3(1.2f, 0.8f, 2.2f), "wood");
            Box("FilingCabinet", off, new Vector3(-13.5f, 0.8f, 63.2f), new Vector3(0.6f, 1.6f, 1f), "metal_dark");
            Sign(off, new Vector3(-7.8f, 2.85f, 67f), -90f, "STATIONMASTER", 0.3f, new Color(1f, 0.9f, 0.7f), "wood_dark");
            PointLight("OfficeLamp", off, new Vector3(-11.5f, 2.8f, 67f), new Color(0.9f, 0.85f, 0.7f), 1.4f, 7f);

            // Ticket booth (shared shell).
            Box("BoothCounter", g, new Vector3(-11f, 0.55f, 57.5f), new Vector3(3f, 1.1f, 0.8f), "wood_dark");
            Box("BoothBack", g, new Vector3(-11f, 1.4f, 55f), new Vector3(3f, 2.8f, 0.2f), "wood_dark");
            Box("BoothRoof", g, new Vector3(-11f, 2.9f, 56.3f), new Vector3(3.4f, 0.2f, 3f), "wood_dark");
            Sign(g, new Vector3(-11f, 3.3f, 57.9f), 180f, "TICKETS", 0.4f, new Color(1f, 0.9f, 0.6f), "wood_dark");
            // Benches
            for (int i = 0; i < 3; i++) Box("Bench", g, new Vector3(-1f, 0.25f, 58f + i * 7f), new Vector3(0.6f, 0.5f, 3f), "wood");

            // Rail pulse strips along the station track and platform edge.
            for (int i = 0; i < 6; i++) PulseStrip(g, new Vector3(9.5f, -1.2f, 56f + i * 5f), i == 3);
            for (int i = 0; i < 4; i++) PulseStrip(g, new Vector3(4.6f, 0.02f, 58f + i * 6f), false, true);

            // Sentinel B: patrols the platform and through the office door (Present).
            var patrol = new[]
            {
                new Vector3(-2f, 0f, 58f), new Vector3(2f, 0f, 64f), new Vector3(-5f, 0f, 67f), new Vector3(-11f, 0f, 67f),
                new Vector3(-5f, 0f, 67f), new Vector3(-2f, 0f, 72f), new Vector3(3f, 0f, 72f), new Vector3(2f, 0f, 60f)
            };
            Sentinel("Sentinel_Station", root, logic, patrol, 30f, 0.3f);
            Zone("StationTip", logic, new Vector3(0f, 1.5f, 53.2f), new Vector3(4f, 3f, 1.2f), "L04.stationTip", "The machine is on this platform. Crouch [C] - the pulse strips at the edge show how close it is.", TimeMask.Present);

            // ---- PRESENT gameplay: evacuation list, cable, decoy.
            var pg = StateGroup("StationPresentGameplay", root, TimeMask.Present, true);
            var list = Clue("EvacuationList", pg, new Vector3(-12.2f, 0.86f, 67f), new Vector3(0.45f, 0.04f, 0.6f), "paper", "Take the evacuation list", "L04.listRecovered", "L04.list",
                "Evacuation list - Line 4",
                "LINE 4 - PRIORITY EVACUATION, BY ORDER OF THE FOUNDERS' OFFICE.\nCars 1-6, 214 names. Children to the Nursery register. Board at the first bell. Do not announce.\n\nCar 3: ... OSKEL, Tamsin. OSKEL, Joss. OSKEL, Maren, age 7. ...\nCar 3: ... VALE, A., age 8. Guardian: I. Marrow. ...\n\nSigned: AUREL HALDEN, Founder.\nDated: 9 days BEFORE the date on every memorial in Veyra.\n\nThey drew up the list of who would be saved before anything had happened.", true, TimeMask.Present);
            list.lines.Add(L("Arin", "Nine days early. Halden's signature. Maren Oskel, Car 3..."));
            list.lines.Add(L("Arin", "VALE, A. Age eight. Guardian: I. Marrow.", 4f));
            list.lines.Add(L("Arin", "That's me. I was on this list. I was one of the children they hid.", 4.5f));
            list.lines.Add(L("Arin", "Ilse Marrow was my guardian. 'Keep time for me.' The watch... it was hers to give.", 5f));
            list.lines.Add(L("The Witness", "You were in Car 3, Arin. So was I. You hummed the whole way down.", 4.5f));

            // Cable: dead junction in the NW corner -> lift power coupling.
            var jb = Box("DeadJunction", pg, new Vector3(-13.6f, 1.3f, 80f), new Vector3(0.6f, 1.6f, 1.2f), "iron_rust");
            Label("JunctionTag", pg, new Vector3(-13.25f, 2.3f, 80f), -90f, "JUNCTION 4-B (DEAD)", 0.22f, new Color(1f, 0.7f, 0.4f));
            var anchor = Marker("CableAnchor", pg, new Vector3(-13.25f, 1.1f, 80f));
            var plug = Grab("CableEnd", pg, new Vector3(-12.6f, 0.15f, 79.6f), new Vector3(0.4f, 0.3f, 0.45f), "copper_verdigris", "cable_end", "live cable end", PrimitiveType.Cube, false);
            plug.gameObject.AddComponent<L04FreezeWhenAbsent>().existsIn = TimeMask.Present;
            Box("PlugTip", plug.transform, plug.transform.position + new Vector3(0f, 0f, 0.24f), new Vector3(0.12f, 0.12f, 0.1f), "neon_amber", false);
            var sparks = Particles("PlugSparks", plug.transform, plug.transform.position + Vector3.up * 0.1f, new Vector3(0.1f, 0.1f, 0.1f), "sparks");
            var smain = sparks.main; smain.playOnAwake = true; smain.duration = 2.5f;
            var ropeGo = new GameObject("CableRope");
            ropeGo.transform.SetParent(pg, false);
            var lr = ropeGo.AddComponent<LineRenderer>();
            lr.sharedMaterial = AZMaterials.Get("metal_dark");
            lr.startWidth = 0.06f; lr.endWidth = 0.06f;
            lr.positionCount = 2;
            lr.SetPosition(0, anchor.position); lr.SetPosition(1, plug.transform.position);
            var rope = ropeGo.AddComponent<L04CableRope>();
            rope.anchor = anchor; rope.end = plug.transform; rope.sag = 0.5f;

            Box("CouplingPedestal", pg, new Vector3(-7.2f, 0.5f, 76.5f), new Vector3(0.8f, 1f, 0.8f), "metal_dark");
            var socket = Socket("LiftCoupling", pg, new Vector3(-7.2f, 1.02f, 76.5f), "cable_end", "L04.liftPowered", "Connect", 0.4f);
            socket.wrongItemMessage = "The coupling takes the heavy cable plug.";
            Label("CouplingTag", pg, new Vector3(-7.2f, 1.75f, 76.05f), 0f, "LIFT POWER COUPLING\n[ plug here ]", 0.22f, new Color(1f, 0.85f, 0.4f));

            // Decoy: gantry release by the platform edge; the pipe drops on the far rail (north end).
            Box("GantryPost", pg, new Vector3(4.4f, 0.6f, 58f), new Vector3(0.3f, 1.2f, 0.3f), "iron_rust");
            var lever = Prim(PrimitiveType.Cube, "GantryRelease", pg, new Vector3(4.4f, 1.35f, 58f), new Vector3(0.4f, 0.5f, 0.4f), "warning_red", true, false);
            var decoy = lever.AddComponent<NoiseDecoy>();
            decoy.prompt = "Pull the gantry release (pipe drops on the far rail)";
            decoy.usableIn = TimeMask.Present;
            decoy.usedFlag = "L04.falseVibration";
            decoy.supplyGroup = "L04.pipes";
            decoy.loudness = 4f; decoy.delay = 1.4f; decoy.cooldown = 12f;
            var np = Marker("FarRailNoisePoint", pg, new Vector3(9.5f, -1.1f, 80f));
            decoy.noisePoint = np;
            Label("ReleaseTag", pg, new Vector3(4.05f, 1.8f, 58f), 90f, "GANTRY RELEASE\nfar rail  >>  north", 0.2f, new Color(1f, 0.6f, 0.5f));
            Box("GantryCable", pg, new Vector3(5.2f, 3.2f, 69f), new Vector3(0.04f, 0.04f, 22f), "metal_dark", false);
            Box("GantryBeam", pg, new Vector3(9.5f, 3.2f, 80f), new Vector3(9f, 0.3f, 0.3f), "iron_rust");
            Cylinder("HangingPipe", pg, new Vector3(9.5f, 2.6f, 80f), 0.15f, 3f, "rail", false).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Hologram("FarRailMarker", pg, new Vector3(9.5f, -0.9f, 80f), new Vector3(1.2f, 0.02f, 1.2f), PrimitiveType.Cylinder, "hologram_amber", false, true);
            Label("FarRailTag", pg, new Vector3(9.5f, 0.4f, 80f), 90f, "FAR RAIL", 0.3f, new Color(1f, 0.8f, 0.4f));

            // ---- PRESENT dressing: dark, puddles, flicker.
            var pres = StateGroup("StationPresent", root, TimeMask.Present, false);
            Box("PresentCeiling", pres, new Vector3(0f, 8.2f, 68f), new Vector3(28.5f, 0.4f, 32.5f), "concrete");
            Box("TrackFlood", pres, new Vector3(9.5f, -0.9f, 68f), new Vector3(8.8f, 0.04f, 32f), "water", false);
            foreach (var p in new[] { new Vector3(-5f, 0f, 60f), new Vector3(0f, 0f, 70f), new Vector3(-9f, 0f, 76f) })
                Box("Puddle", pres, p + Vector3.up * 0.02f, new Vector3(3f, 0.02f, 2.2f), "water", false);
            Lamp("StationSodiumA", pres, new Vector3(-13.3f, 0f, 60f), "sodium", 5f);
            var drip = Particles("StationDrip", pres, new Vector3(-3f, 7.8f, 70f), new Vector3(12f, 0.1f, 20f), "rain");
            var dem = drip.emission; dem.rateOverTime = 150f;
            Particles("StationMist", pres, new Vector3(9.5f, -0.5f, 68f), new Vector3(8f, 0.5f, 30f), "mist");
            Ambient("StationWater", pres, new Vector3(9.5f, 0f, 68f), AmbientKind.Water, 0.3f, TimeMask.Present, 1f, 20f);

            // ---- PAST dressing: lit platform, staff-only office door, posters, station tram.
            var past = StateGroup("StationPast", root, TimeMask.Past, true);
            Box("PastCeiling", past, new Vector3(0f, 8.2f, 68f), new Vector3(28.5f, 0.4f, 32.5f), "tile_cold");
            Box("OfficeDoorPast", past, new Vector3(-8f, 1.25f, 67f), new Vector3(0.12f, 2.5f, 1.6f), "wood");
            Label("StaffOnly", past, new Vector3(-7.9f, 1.9f, 67f), -90f, "STAFF ONLY\nSTATIONMASTER", 0.25f, new Color(1f, 0.9f, 0.6f));
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(-6f + (i % 2) * 8f, 7.4f, 58f + (i / 2) * 14f);
                Sphere("Globe", past, p, 0.3f, "lamp_warm", false);
                if (i == 0 || i == 3) PointLight("PlatformLamp", past, p + Vector3.down * 0.4f, Warm, 3.2f, 20f);
            }
            for (int i = 0; i < 4; i++)
            {
                var poster = Box("Poster", past, new Vector3(-13.7f, 2.2f, 60f + i * 5f), new Vector3(0.04f, 1.4f, 1f), i % 2 == 0 ? "fabric_red" : "fabric_teal", false);
                Label("PosterText", past, new Vector3(-13.65f, 2.2f, 60f + i * 5f), -90f, i % 2 == 0 ? "FESTIVAL\nSPECIALS" : "LINE 4\nOPENS SOON", 0.18f, new Color(1f, 0.95f, 0.8f));
            }
            Tram("StationTram", past, new Vector3(9.5f, -1.2f, 98f), new Vector3(9.5f, -1.2f, 66f), 7f, 7f, 10f, "fabric_teal", "LINE 4", out _);
            Ambient("StationCrowd", past, new Vector3(-4f, 2f, 64f), AmbientKind.Crowd, 0.35f, TimeMask.Past, 0.7f, 26f);

            // ---- FUTURE dressing: split ceiling, crystals (one seals the office), holograms.
            var fut = StateGroup("StationFuture", root, TimeMask.Future, true);
            Box("FutCeilW", fut, new Vector3(-8f, 8.2f, 68f), new Vector3(12.5f, 0.4f, 32.5f), "ruin_dark");
            Box("FutCeilE", fut, new Vector3(10f, 8.2f, 68f), new Vector3(8.5f, 0.4f, 32.5f), "ruin_dark");
            Box("OfficeCrystal", fut, new Vector3(-8f, 1.3f, 67f), new Vector3(0.9f, 2.6f, 1.8f), "crystal");
            var frng = new System.Random(4406);
            Crystal(fut, new Vector3(-7.5f, 0f, 67f), 2.4f, frng, false);
            for (int i = 0; i < 6; i++) Crystal(fut, new Vector3(-12f + (i % 3) * 6f, 0f, 56f + (i / 3) * 18f), 1f + (float)frng.NextDouble() * 1.5f, frng, false);
            var tt = Hologram("TimetableHolo", fut, new Vector3(0f, 4.5f, 66f), new Vector3(5f, 2.2f, 0.02f), PrimitiveType.Cube, "hologram", true, false);
            Label("TimetableText", fut, new Vector3(0f, 4.5f, 65.95f), 0f, "LINE 4 // DEPARTURES\nCAR 1-6 ..... EVACUATED\nALL OTHERS ..... 0", 0.3f, new Color(0.6f, 1f, 1f));
            Hologram("TerminusRing", fut, new Vector3(-3f, 6f, 81f), new Vector3(5f, 0.04f, 5f), PrimitiveType.Cylinder, "hologram_magenta", true, true);
            Label("TerminusText", fut, new Vector3(-3f, 6.6f, 78.4f), 0f, "TERMINUS : DROWNED DISTRICT", 0.35f, new Color(1f, 0.6f, 0.9f));
            PointLight("FutCyan", fut, new Vector3(-6f, 5f, 64f), Cyan, 2.5f, 20f);
            Particles("StationMotes", fut, new Vector3(0f, 4f, 68f), new Vector3(26f, 7f, 30f), "motes");
            Box("NeonTrimW", fut, new Vector3(-13.7f, 0.3f, 68f), new Vector3(0.05f, 0.05f, 32f), "neon_cyan", false);
            Box("NeonTrimEdge", fut, new Vector3(4.85f, 0.05f, 68f), new Vector3(0.05f, 0.05f, 32f), "neon_magenta", false);
            Ambient("StationDrone", fut, new Vector3(0f, 4f, 68f), AmbientKind.FutureDrone, 0.35f, TimeMask.Future, 0.6f, 30f);

            ReflectionProbe(shared, new Vector3(0f, 3.5f, 68f), new Vector3(28f, 8f, 32f));
        }

        // =====================================================================
        // Lift + exit
        // =====================================================================
        static void BuildLiftAndExit(Transform root, Transform shared, Transform logic)
        {
            var g = Group("LiftShaft", shared);
            // Above platform.
            Wall("ShaftW", g, new Vector3(-6f, 0f, 78f), new Vector3(-6f, 0f, 84f), 7f, 0.3f, "metal_dark");
            Wall("ShaftE", g, new Vector3(0f, 0f, 78f), new Vector3(0f, 0f, 84f), 7f, 0.3f, "metal_dark");
            WallWithDoor("ShaftS", g, new Vector3(-6f, 0f, 78f), new Vector3(0f, 0f, 78f), 7f, 0.3f, "metal_dark", 3.4f, 2.7f);
            // Below platform.
            Wall("ShaftWLow", g, new Vector3(-6f, -10.5f, 78f), new Vector3(-6f, -10.5f, 84f), 10f, 0.3f, "concrete");
            Wall("ShaftELow", g, new Vector3(0f, -10.5f, 78f), new Vector3(0f, -10.5f, 84f), 10f, 0.3f, "concrete");
            Wall("ShaftSLow", g, new Vector3(-6f, -10.5f, 78f), new Vector3(0f, -10.5f, 78f), 10f, 0.3f, "concrete");
            WallWithDoor("ShaftNLow", g, new Vector3(-6f, -10f, 84f), new Vector3(0f, -10f, 84f), 9.5f, 0.3f, "concrete", 3f, 2.8f);
            Box("ShaftPit", g, new Vector3(-3f, -10.6f, 81f), new Vector3(6f, 0.4f, 6f), "concrete");
            Sign(g, new Vector3(-3f, 3.4f, 77.8f), 0f, "LIFT - DEEP LEVEL", 0.45f, new Color(0.9f, 0.95f, 1f), "metal_dark");
            for (int i = 0; i < 4; i++) Box("ShaftLight", g, new Vector3(-5.8f, -2f - i * 2.2f, 81f), new Vector3(0.05f, 0.1f, 4f), "lamp_cold", false);

            // Gate opens when powered.
            var gate = Door("LiftGate", g, new Vector3(-3f, 0f, 78f), new Vector3(3.4f, 2.7f, 0.12f), "iron_rust", new Vector3(0f, 2.75f, 0f), 0f, "L04.liftPowered");
            Dyn(gate.moveTarget.gameObject);
            gate.toast = "Power hums through the coupling. The lift gate grinds open.";
            Label("GateText", gate.transform, new Vector3(-3f, 1.5f, 77.9f), 0f, "NO POWER", 0.4f, new Color(1f, 0.4f, 0.3f)).transform.SetParent(gate.moveTarget, true);

            // Lift car.
            var car = Group("LiftCar", g);
            Ride(Box("CarDeck", car, new Vector3(-3f, -0.2f, 81f), new Vector3(5.6f, 0.4f, 5.6f), "metal_dark"));
            Box("CarRailW", car, new Vector3(-5.6f, 0.55f, 81f), new Vector3(0.08f, 1.1f, 5.4f), "rail");
            Box("CarRailE", car, new Vector3(-0.4f, 0.55f, 81f), new Vector3(0.08f, 1.1f, 5.4f), "rail");
            Box("CarRailN", car, new Vector3(-3f, 0.55f, 83.6f), new Vector3(5.4f, 1.1f, 0.08f), "rail");
            Box("CarStripe", car, new Vector3(-3f, 0.01f, 78.5f), new Vector3(5.4f, 0.02f, 0.3f), "neon_amber", false);
            var ctrl = Use("LiftControl", car, new Vector3(-1.2f, 0.6f, 83f), new Vector3(0.5f, 1.2f, 0.4f), "metal_dark", "Descend to the Drowned District", "L04.descending");
            ctrl.requiredFlags.AddRange(new[] { "L04.liftPowered", "L04.listRecovered", "L04.circuitHeld" });
            ctrl.lockedMessage = "The lift won't move. It needs power, the evacuation list - and both circuit breakers thrown.";
            ctrl.oneShot = false;
            ctrl.cue = Cue.Clang;
            ctrl.lines.Add(L("Arin", "Down, then. Wherever they sent the people who weren't on the list."));
            ctrl.lines.Add(L("The Witness", "The water keeps what the city forgot. Go gently."));
            Label("ControlText", car, new Vector3(-1.2f, 1.45f, 82.75f), 0f, "DESCEND\n[ E ]", 0.22f, new Color(0.7f, 1f, 1f));
            var carLight = PointLight("CarLight", car, new Vector3(-3f, 2.6f, 81f), new Color(0.75f, 0.9f, 1f), 2.2f, 8f);
            carLight.gameObject.SetActive(false);
            Dyn(car.gameObject);
            var power = Reactor("LiftPower", logic, "L04.liftPowered");
            power.enableWhenTrue.Add(carLight.gameObject);
            var hazards = Group("ShaftDebris", car);
            ShaftDebris(hazards, new Vector3(-4.4f, 0f, 81f), 0f);
            ShaftDebris(hazards, new Vector3(-1.6f, 0f, 81f), 2.1f);
            Dyn(hazards.gameObject);
            hazards.gameObject.SetActive(false);
            var ride = logic.gameObject.AddComponent<L04LiftRide>();
            ride.car = car; ride.drop = new Vector3(0f, -10f, 0f); ride.duration = 14f;
            ride.startFlag = "L04.descending"; ride.arrivedFlag = "L04.liftArrived"; ride.rideHazards = hazards.gameObject;
            var downR = Reactor("LiftDescend", logic, "L04.descending");
            downR.announce = true; downR.cue = Cue.RuleChange;
            downR.toast = "Calder is wrecking the shaft! Watch the warning lights - move to the safe half of the car.";
            Ambient("LiftHum", g, new Vector3(-3f, 1f, 81f), AmbientKind.Machine, 0.2f, TimeMask.All, 1f, 10f);

            // Bottom corridor -> Drowned District.
            var b = Group("DeepLevel", shared);
            Floor("CorridorFloor", b, new Vector3(-3f, -10f, 90f), new Vector2(4.4f, 12f), "concrete");
            Wall("CorrW", b, new Vector3(-5.2f, -10f, 84f), new Vector3(-5.2f, -10f, 96f), 3.5f, 0.3f, "tile_cold");
            Wall("CorrE", b, new Vector3(-0.8f, -10f, 84f), new Vector3(-0.8f, -10f, 96f), 3.5f, 0.3f, "tile_cold");
            Box("CorrCeil", b, new Vector3(-3f, -6.3f, 90f), new Vector3(4.8f, 0.4f, 12f), "tile_cold");
            Box("FloodWater", b, new Vector3(-3f, -10.25f, 104f), new Vector3(30f, 0.05f, 16f), "water_clear", false);
            Box("FloodBed", b, new Vector3(-3f, -12f, 104f), new Vector3(30f, 0.4f, 16f), "mud");
            Building("DrownedA", b, new Vector3(-12f, -12f, 108f), new Vector3(8f, 10f, 6f), "stone_wet", "window_cold", "iron_rust", 41);
            Building("DrownedB", b, new Vector3(6f, -12f, 110f), new Vector3(9f, 12f, 6f), "stone_wet", "window_cold", "iron_rust", 42);
            Sign(b, new Vector3(-3f, -7.2f, 95.7f), 0f, "OLD VEYRA FLOOD BASIN", 0.4f, new Color(0.6f, 0.95f, 1f), "metal_dark");
            PointLight("DeepTeal", b, new Vector3(-3f, -8f, 100f), new Color(0.3f, 0.85f, 0.9f), 3f, 18f);
            PointLight("CorrLight", b, new Vector3(-3f, -7f, 88f), new Color(0.75f, 0.9f, 1f), 1.6f, 9f);
            Particles("DeepMist", b, new Vector3(-3f, -9.6f, 102f), new Vector3(20f, 1f, 12f), "mist");
            Ambient("DeepWater", b, new Vector3(-3f, -9f, 100f), AmbientKind.Water, 0.35f, TimeMask.All, 1f, 20f);
            Box("CorrEnd", b, new Vector3(-3f, -8.25f, 96.2f), new Vector3(4.4f, 3.5f, 0.3f), "glass", false);
            Exit("Exit_Drowned", root, new Vector3(-3f, -10f, 93.5f), new Vector3(4.2f, 3f, 1.5f),
                "The way down is closed until the lift is powered and the evacuation list is recovered.", "L04.liftPowered", "L04.listRecovered", "L04.liftArrived");
            KillPlane(b, new Vector3(0f, -24f, 70f), new Vector3(200f, 2f, 200f), "Lost in the dark beneath the line.");
            ReflectionProbe(b, new Vector3(-3f, -8f, 92f), new Vector3(5f, 4f, 16f));
        }

        // =====================================================================
        // NPCs
        // =====================================================================
        static void BuildNpcs(Transform root)
        {
            var past = StateGroup("PastPeople", root, TimeMask.Past, false);
            var present = StateGroup("PresentPeople", root, TimeMask.Present, false);

            // Ignatz Marrow - tram conductor (Past).
            var ign = Npc("Ignatz Marrow", past, new Vector3(3.5f, 0f, 64f), 90f, "coat_navy", true, 11);
            Route(ign, false,
                (new Vector3(3.8f, 0f, 64f), 6f, NPCActivity.Look, "Line Four, stand clear of the edge!"),
                (new Vector3(3.8f, 0f, 74f), 4f, NPCActivity.Work, "Doors, doors, doors."),
                (new Vector3(-4f, 0f, 54f), 3f, NPCActivity.Talk, "Crossing's live. Wait for GO."),
                (new Vector3(3.8f, 0f, 58f), 4f, NPCActivity.Work, ""));
            ign.changedFlag = "L04.listRecovered";
            Route(ign, true,
                (new Vector3(-7f, 0f, 67f), 9f, NPCActivity.Look, "What does the stationmaster keep in there?"),
                (new Vector3(-4f, 0f, 70f), 4f, NPCActivity.Idle, ""),
                (new Vector3(3.8f, 0f, 66f), 6f, NPCActivity.Look, ""));
            var pi = ign.GetComponent<NPCPersona>();
            pi.task = "Conductor: waves the Line 4 tram in and out, checks the doors, and keeps passengers back from the edge.";
            pi.approachBarks.AddRange(new[] { "Ticket ready? No? Then mind the edge.", "Whoa - you look like you walked here from a funeral." });
            pi.talkLines.Add(L("Ignatz Marrow", "Thirty years on these rails. You learn to feel a tram coming in your boots before you hear it."));
            pi.talkLines.Add(L("Ignatz Marrow", "My sister Ilse keeps the clock tower. I keep the trams. Between us, Veyra is never late."));
            pi.talkLines.Add(L("Ignatz Marrow", "Nine days back the Founders' office had me run six empty cars down to the deep lift. Every night. Practice, they said."));
            pi.talkLines.Add(L("Ignatz Marrow", "I don't ask why. Halden's seal on the order and that's the end of asking."));
            pi.talkLines.Add(L("Ignatz Marrow", "I write it all in my logbook in the signal box off the depot. Somebody ought to remember the timetable."));
            pi.talkLines.Add(L("Ignatz Marrow", "On the crossing, watch the signals. STOP with the arm up means a car's close. Stand on the refuges."));
            pi.conditional.Add(new ConditionalLines { whenFlag = "arin.listedChild", lines = { L("Ignatz Marrow", "Vale? Ilse's ward is a Vale. Quiet little thing. Hums when it's frightened."), L("Ignatz Marrow", "She had a watch engraved for that child. 'For A. - keep time for me.' ...Why are you looking at me like that?") } });
            pi.conditional.Add(new ConditionalLines { whenFlag = "L04.listRecovered", lines = { L("Ignatz Marrow", "A list for Line Four, signed by Halden? Evacuating from what? It's festival week."), L("Ignatz Marrow", "Is Ilse's name on it? ...No. Of course not. She'd never leave the tower.") } });
            pi.conditional.Add(new ConditionalLines { whenFlag = "L04.logbook", lines = { L("Ignatz Marrow", "You read my log? Then you know as much as I do. Six cars, the first bell, and no passengers yet."), L("Ignatz Marrow", "Ilse came down last week. Made me promise the deep lift stays powered. Wouldn't say why.") } });
            pi.conditional.Add(new ConditionalLines { whenFlag = "L04.pulsesRead", lines = { L("Ignatz Marrow", "Reading the rails, are you? The maintenance unit does the same. Blind as a bell, ears like a cat.") } });
            pi.changedLines.Add(L("Ignatz Marrow", "I keep looking at that office door. Nine days. You said nine days."));

            // Wren Castell - ticket seller (Past).
            var wren = Npc("Wren Castell", past, new Vector3(-11f, 0f, 56.4f), 0f, "coat_ochre", true, 12, "paper");
            Route(wren, false,
                (new Vector3(-11f, 0f, 56.4f), 7f, NPCActivity.Work, "Singles, returns, festival passes!"),
                (new Vector3(-12.5f, 0f, 61f), 3f, NPCActivity.Carry, ""),
                (new Vector3(-9f, 0f, 66f), 3f, NPCActivity.Carry, "Dockets for the stationmaster."),
                (new Vector3(-11f, 0f, 56.4f), 5f, NPCActivity.Work, ""));
            wren.changedFlag = "L04.listRecovered";
            Route(wren, true,
                (new Vector3(-3f, 0f, 76.5f), 8f, NPCActivity.Look, "Where does the deep lift even go?"),
                (new Vector3(-11f, 0f, 58.5f), 5f, NPCActivity.Idle, "Booth's closed. I can't sell tickets today."),
                (new Vector3(-6f, 0f, 70f), 4f, NPCActivity.Look, ""));
            var pw = wren.GetComponent<NPCPersona>();
            pw.task = "Ticket seller: sells festival passes at the booth and carries the day's dockets to the stationmaster's office.";
            pw.approachBarks.AddRange(new[] { "Single or return?", "Festival passes, half price before the bell!" });
            pw.talkLines.Add(L("Wren Castell", "The office? Staff only. The stationmaster keeps the rosters and the lists locked up tight."));
            pw.talkLines.Add(L("Wren Castell", "Two hundred and fourteen season passes came down from the Founders' office. Not one of them paid for."));
            pw.talkLines.Add(L("Wren Castell", "Little Maren Oskel came through with her mother yesterday. Humming. She asked if the deep lift goes to the sea."));
            pw.talkLines.Add(L("Wren Castell", "Ignatz says I worry too much. Ignatz also talks to his trams."));
            pw.talkLines.Add(L("Wren Castell", "He's been odd lately. Counting seats on Line Four, which hasn't even opened."));
            pw.conditional.Add(new ConditionalLines { whenFlag = "L04.listRecovered", lines = { L("Wren Castell", "Two hundred and fourteen names, sorted by car. Those are season-ticket holders. Founders' friends."), L("Wren Castell", "Where's the list for everyone else?") } });
            pw.conditional.Add(new ConditionalLines { whenFlag = "clue.payoff.L3", lines = { L("Wren Castell", "Maren Oskel, dead? I sold her mother a child's fare for Car 3. She's on the list, not in a grave.") } });
            pw.conditional.Add(new ConditionalLines { whenFlag = "L04.crossedTracks", lines = { L("Wren Castell", "You came over the crossing on foot? Ignatz will have your ears.") } });
            pw.changedLines.Add(L("Wren Castell", "I closed the booth. I can't sell anyone a ticket knowing there's a list."));

            // Dace Dunmore - line-walker hiding from the machine (Present).
            var pell = Npc("Dace Dunmore", present, new Vector3(-13f, 0f, 53.5f), 45f, "coat_olive", true, 13, "iron_rust");
            Route(pell, false,
                (new Vector3(-13f, 0f, 53.5f), 8f, NPCActivity.Look, "Shh. It's on the platform."),
                (new Vector3(-12.6f, 0f, 59f), 5f, NPCActivity.Work, "Copper. Good copper."),
                (new Vector3(-13f, 0f, 54.5f), 6f, NPCActivity.Carry, ""));
            pell.changedFlag = "L04.falseVibration";
            pell.walkSpeed = 0.9f;
            Route(pell, true,
                (new Vector3(3.6f, 0f, 60f), 8f, NPCActivity.Look, "Ha! It went for the pipe. Like a dog for a bone."),
                (new Vector3(0f, 0f, 56f), 5f, NPCActivity.Work, ""),
                (new Vector3(-13f, 0f, 53.5f), 5f, NPCActivity.Idle, ""));
            var pp = pell.GetComponent<NPCPersona>();
            pp.task = "Line-walker: strips copper from the dead tram line while hiding from the maintenance unit in the refuge corner.";
            pp.approachBarks.AddRange(new[] { "Get down! Low, low - it hears you.", "Don't stamp. It'll feel you." });
            pp.talkLines.Add(L("Dace Dunmore", "That machine tracks the rails. Crouch, keep slow, and it'll pass you like a ghost."));
            pp.talkLines.Add(L("Dace Dunmore", "Name's Dace. I strip copper down here. Nobody else comes. Nobody else is stupid enough."));
            pp.talkLines.Add(L("Dace Dunmore", "My gran said the trams ran full the week before the bell. Then empty forever after."));
            pp.talkLines.Add(L("Dace Dunmore", "The office over there's the stationmaster's. The machine checks it every round, like it's guarding something."));
            pp.talkLines.Add(L("Dace Dunmore", "The gantry by the platform edge drops a pipe on the far rail. Bang! It runs to look. Gives you a breath."));
            pp.conditional.Add(new ConditionalLines { whenHolding = "cable_end", lines = { L("Dace Dunmore", "Careful - that end's still live! The lift coupling's on the pedestal by the shaft. North end.") } });
            pp.conditional.Add(new ConditionalLines { whenFlag = "L04.listRecovered", lines = { L("Dace Dunmore", "Halden's list? My gran was on the platform that day. She wasn't on any list."), L("Dace Dunmore", "So that's what the Founders did. Picked. And the rest of us got an earthquake.") } });
            pp.conditional.Add(new ConditionalLines { whenFlag = "evidence.record", lines = { L("Dace Dunmore", "You got Odile's record out of the Hall? Past Ondine? Pell owes me a bottle, then. I said you'd make it.") } });
            pp.conditional.Add(new ConditionalLines { whenFlag = "L04.logbook", lines = { L("Dace Dunmore", "A conductor's logbook? Marrow? There's a Marrow on the clock tower plaque. Keeper of something.") } });
            pp.changedLines.Add(L("Dace Dunmore", "You fooled it once. It'll come back, though. They always come back to the route."));

            // Background: commuters on the Past platform, depot workers (keep working unobserved).
            Crowd("Commuters", past, new Vector3(-2.5f, 0f, 64f), new Vector2(9f, 14f), 10, 404,
                new[] { "Is this the festival car?", "Mind your bag.", "Two stops, then the square.", "Did you hear the bell practice?" }, "fabric_red");
            Crowd("DepotWorkers", past, new Vector3(9f, 0f, 16f), new Vector2(5f, 14f), 4, 405,
                new[] { "Watch the shunter!", "Grease on the points, not on me.", "Shift ends at the bell." }, "metal_dark");
        }

        // =====================================================================
        // Builders for recurring pieces
        // =====================================================================

        static void Track(Transform parent, Vector3 a, Vector3 b)
        {
            Vector3 dir = (b - a); float len = dir.magnitude; dir /= len;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
            foreach (float s in new[] { -0.72f, 0.72f })
                Box("Rail", parent, (a + b) * 0.5f + side * s + Vector3.up * 0.07f, new Vector3(0.08f, 0.14f, len), "rail", false, yaw);
            int n = Mathf.FloorToInt(len / 1.6f);
            for (int i = 0; i < n; i += 1)
            {
                if (i % 2 == 1) continue; // keep object count down
                Box("Sleeper", parent, a + dir * (i * 1.6f + 0.8f) + Vector3.up * 0.02f, new Vector3(2f, 0.04f, 0.25f), "wood_dark", false, yaw);
            }
        }

        static void PulseStrip(Transform parent, Vector3 pos, bool withLight, bool alongX = false)
        {
            var strip = Box("RailPulseStrip", parent, pos + Vector3.up * 0.06f, alongX ? new Vector3(2.4f, 0.05f, 0.12f) : new Vector3(0.12f, 0.05f, 2.4f), "rail_strip", false);
            Dyn(strip);
            var rp = strip.AddComponent<RailPulseIndicator>();
            rp.strip = strip.GetComponent<Renderer>();
            rp.range = 20f;
            if (withLight) rp.glow = PointLight("PulseGlow", strip.transform, pos + Vector3.up * 0.4f, Cyan, 0f, 4f);
        }

        static void Sentinel(string name, Transform root, Transform logic, Vector3[] patrol, float hearing, float threshold)
        {
            var grp = StateGroup(name + "Body", root, TimeMask.Present, false);
            var s = Group(name, grp);
            s.position = patrol[0];
            Box("Hull", s, patrol[0] + new Vector3(0f, 1.1f, 0f), new Vector3(1.5f, 0.9f, 2.1f), "obsidian");
            Box("HullPlate", s, patrol[0] + new Vector3(0f, 1.6f, -0.2f), new Vector3(1.2f, 0.2f, 1.4f), "metal_dark", false);
            Box("Jaw", s, patrol[0] + new Vector3(0f, 0.75f, 1.05f), new Vector3(1.2f, 0.25f, 0.3f), "metal_dark", false);
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -0.8f : 0.8f, sz = i < 2 ? -0.8f : 0.8f;
                var leg = Box("Leg", s, patrol[0] + new Vector3(sx * 1.2f, 0.45f, sz), new Vector3(0.12f, 0.9f, 0.12f), "metal_dark", false);
                leg.transform.rotation = Quaternion.Euler(sz * 15f, 0f, -sx * 25f);
                Box("Foot", s, patrol[0] + new Vector3(sx * 1.45f, 0.05f, sz), new Vector3(0.3f, 0.1f, 0.3f), "iron_rust", false);
            }
            foreach (float ex in new[] { -0.4f, 0f, 0.4f })
                Box("Eye", s, patrol[0] + new Vector3(ex, 1.2f, 1.07f), new Vector3(0.18f, 0.08f, 0.04f), "warning_red", false);
            var scanner = Group("Scanner", s);
            scanner.position = patrol[0] + new Vector3(0f, 1.85f, 0f);
            Cylinder("ScannerDisc", scanner, scanner.position, 0.7f, 0.12f, "metal_dark", false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                Box("Emitter", scanner, scanner.position + new Vector3(Mathf.Cos(a) * 0.68f, 0.03f, Mathf.Sin(a) * 0.68f), new Vector3(0.14f, 0.08f, 0.14f), "warning_red", false);
            }
            Box("ScannerBeam", scanner, scanner.position + new Vector3(0f, 0.08f, 0.35f), new Vector3(0.05f, 0.03f, 0.7f), "warning_red", false);
            var light = SpotLight("SensorLight", s, patrol[0] + new Vector3(0f, 1.3f, 1.2f), new Vector3(25f, 0f, 0f), Red, 3f, 12f, 70f, false);
            Label("UnitTag", s, patrol[0] + new Vector3(0f, 2.5f, 0f), 0f, "MAINTENANCE UNIT M-0", 0.25f, new Color(1f, 0.5f, 0.4f));
            Particles("Exhaust", s, patrol[0] + new Vector3(0f, 1.4f, -1.1f), new Vector3(0.2f, 0.2f, 0.2f), "steam");
            Dyn(s.gameObject);

            var vs = s.gameObject.AddComponent<VibrationSentinel>();
            vs.activeIn = TimeMask.Present;
            vs.hearingRadius = hearing;
            vs.threshold = threshold;
            vs.sensorLight = light;
            vs.scanner = scanner;
            vs.caughtMessage = "The machine felt your steps through the rails. Crouch [C] to move quietly, and read the pulse strips.";
            var route = Group(name + " Patrol", logic);
            for (int i = 0; i < patrol.Length; i++) vs.patrol.Add(Marker("P" + i, route, patrol[i]));
        }

        static GameObject Tram(string name, Transform parent, Vector3 a, Vector3 b, float speed, float pause, float length, string bodyMat, string label, out MovingHazard mh)
        {
            var t = Group(name, parent);
            t.position = a;
            Vector3 dir = (b - a).normalized;
            t.rotation = Quaternion.LookRotation(dir);
            Vector3 TP(Vector3 local) => t.TransformPoint(local);
            var body = Box("Body", t, TP(new Vector3(0f, 1.7f, 0f)), new Vector3(2.4f, 2.8f, length), bodyMat);
            body.transform.rotation = t.rotation;
            var roof = Box("Roof", t, TP(new Vector3(0f, 3.2f, 0f)), new Vector3(2.2f, 0.2f, length - 0.4f), "metal_dark", false);
            roof.transform.rotation = t.rotation;
            foreach (float sx in new[] { -1.22f, 1.22f })
            {
                var w = Box("Windows", t, TP(new Vector3(sx, 2.1f, 0f)), new Vector3(0.04f, 0.8f, length - 1.5f), "window_warm", false);
                w.transform.rotation = t.rotation;
            }
            foreach (float sz in new[] { -1f, 1f })
            {
                var lampF = Box("HeadLamp", t, TP(new Vector3(0f, 1.0f, sz * (length * 0.5f + 0.02f))), new Vector3(1.4f, 0.25f, 0.04f), sz > 0 ? "lamp_warm" : "warning_red", false);
                lampF.transform.rotation = t.rotation;
                var sign = Label("RouteSign", t, TP(new Vector3(0f, 2.7f, sz * (length * 0.5f + 0.05f))), 0f, label, 0.35f, new Color(1f, 0.9f, 0.6f));
                sign.transform.rotation = t.rotation * Quaternion.Euler(0f, sz > 0 ? 180f : 0f, 0f);
            }
            var pole = Box("Pantograph", t, TP(new Vector3(0f, 3.8f, 0f)), new Vector3(0.08f, 1f, 0.08f), "metal_dark", false);
            pole.transform.rotation = t.rotation * Quaternion.Euler(30f, 0f, 0f);
            var head = SpotLight("Headlight", t, TP(new Vector3(0f, 1.2f, length * 0.5f + 0.3f)), Vector3.zero, new Color(1f, 0.92f, 0.75f), 6f, 30f, 55f, false);
            head.transform.rotation = t.rotation * Quaternion.Euler(8f, 0f, 0f);
            Dyn(t.gameObject);

            mh = t.gameObject.AddComponent<MovingHazard>();
            mh.activeIn = TimeMask.Past;
            mh.speed = speed;
            mh.pauseAtEnds = pause;
            mh.pingPong = true;
            mh.headlight = head;
            mh.warnDistance = 22f;
            mh.deathMessage = "Hit by a tram. Wait on a refuge for the signal to read GO - the headlight and horn come first.";
            var wp = Group(name + " Waypoints", parent.parent != null ? parent.parent : parent);
            mh.waypoints.Add(Marker("A", wp, a));
            mh.waypoints.Add(Marker("B", wp, b));
            return t.gameObject;
        }

        static void Signal(Transform parent, Vector3 ground, Transform tram, string tag)
        {
            var g = Group("Signal " + tag, parent);
            g.position = ground;
            Cylinder("Post", g, ground + Vector3.up * 1.4f, 0.07f, 2.8f, "metal_dark", false);
            var head = Box("Head", g, ground + Vector3.up * 2.9f, new Vector3(0.5f, 0.5f, 0.2f), "lamp_cold", false);
            Dyn(head);
            var stop = Group("Stop", g);
            Label("StopText", stop, ground + new Vector3(0f, 3.5f, -0.12f), 0f, "STOP", 0.45f, new Color(1f, 0.35f, 0.3f));
            Box("ArmUp", stop, ground + new Vector3(0.45f, 3.3f, 0f), new Vector3(0.1f, 0.8f, 0.06f), "warning_red", false);
            var go = Group("Go", g);
            Label("GoText", go, ground + new Vector3(0f, 3.5f, -0.12f), 0f, "GO", 0.45f, new Color(0.5f, 1f, 0.7f));
            var arm = Box("ArmDown", go, ground + new Vector3(0.65f, 2.75f, 0f), new Vector3(0.8f, 0.1f, 0.06f), "lamp_cold", false);
            Label("TrackTag", g, ground + new Vector3(0f, 2.3f, -0.12f), 0f, tag, 0.22f, Color.white);
            Light lamp = null;
            var sig = g.gameObject.AddComponent<L04TramSignal>();
            sig.tram = tram; sig.warnDistance = 20f; sig.activeIn = TimeMask.Past;
            sig.lamp = lamp; sig.lampRenderer = head.GetComponent<Renderer>();
            sig.stopVisual = stop.gameObject; sig.goVisual = go.gameObject;
        }

        static void ResonanceHazard(Transform parent, Vector3 ground, float offset)
        {
            var g = Group("Resonance", parent);
            g.position = ground;
            var spikes = Group("Spikes", g);
            var rng = new System.Random((int)(ground.z * 10f));
            for (int i = 0; i < 5; i++)
            {
                var sp = Box("Spike", spikes, ground + new Vector3(-0.7f + i * 0.35f, 1f, ((float)rng.NextDouble() - 0.5f) * 2f), new Vector3(0.18f, 2f, 0.18f), "crystal", false);
                sp.transform.rotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 30f, 0f, ((float)rng.NextDouble() - 0.5f) * 30f);
            }
            spikes.gameObject.SetActive(false);
            Box("RingMark", g, ground + Vector3.up * 0.03f, new Vector3(2f, 0.02f, 3f), "neon_magenta", false);
            Label("RingText", g, ground + new Vector3(0f, 0.06f, -1.7f), 0f, "/// RESONANCE ///", 0.3f, new Color(1f, 0.6f, 1f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var warn = PointLight("ResonanceWarn", g, ground + Vector3.up * 1.5f, Magenta, 5f, 6f);
            var ps = Particles("ResonanceSparks", g, ground + Vector3.up * 0.2f, new Vector3(1.6f, 0.1f, 2.6f), "sparks");
            var dz = TriggerBox("DamageZone", g, ground + Vector3.up * 1.2f, new Vector3(2.2f, 2.4f, 3f));
            var h = g.gameObject.AddComponent<TelegraphedHazard>();
            h.idleTime = 2.4f; h.warnTime = 1.3f; h.activeTime = 1.1f; h.startOffset = offset;
            h.activeIn = TimeMask.Future;
            h.warnLight = warn; h.warnParticles = ps; h.activeVisual = spikes.gameObject; h.damageZone = dz;
            h.deathMessage = "The crystal rang. Wait for the magenta glow to fade, then cross.";
            Dyn(g.gameObject);
        }

        static void Crystal(Transform g, Vector3 ground, float size, System.Random rng, bool collider)
        {
            for (int k = 0; k < 3; k++)
            {
                var c = Box("Crystal", g, ground + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f * size, size * 0.5f, ((float)rng.NextDouble() - 0.5f) * 0.6f * size),
                    new Vector3(0.3f * size, size * (0.7f + 0.3f * k), 0.3f * size), "crystal", collider);
                c.transform.rotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 40f, (float)rng.NextDouble() * 180f, ((float)rng.NextDouble() - 0.5f) * 40f);
            }
        }

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

        static void Dyn(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }
    }
}
