using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    public static partial class Level07Builder
    {
        // =====================================================================
        // Clock tower: shell, interior climb, core, Zero Chamber door
        // =====================================================================

        static void BuildTower(Transform root, Transform shared, Transform past, Transform present, Transform future, Transform pastPresent, Transform logic)
        {
            var t = Group("Clock Tower", shared);
            WallWithDoor("FrontWall", t, new Vector3(-7f, 0f, 27f), new Vector3(7f, 0f, 27f), 24f, 0.8f, "stone_warm", 3f, 3.6f);
            Wall("BackWall", t, new Vector3(-7f, 0f, 41f), new Vector3(7f, 0f, 41f), 24f, 0.8f, "stone_warm");
            Wall("WestWall", t, new Vector3(-7f, 0f, 27f), new Vector3(-7f, 0f, 41f), 24f, 0.8f, "stone_warm");
            Wall("EastWall", t, new Vector3(7f, 0f, 27f), new Vector3(7f, 0f, 41f), 24f, 0.8f, "stone_warm");
            Box("Buttress", t, new Vector3(-7.4f, 6f, 26.6f), new Vector3(1.2f, 12f, 1.2f), "stone_warm");
            Box("Buttress", t, new Vector3(7.4f, 6f, 26.6f), new Vector3(1.2f, 12f, 1.2f), "stone_warm");
            Box("DoorArch", t, new Vector3(0f, 3.9f, 26.45f), new Vector3(4.2f, 0.6f, 0.3f), "brass", false);
            Label("TowerName", t, new Vector3(0f, 5f, 26.5f), 0f, "THE FOUNDERS' CLOCK", 0.9f, new Color(1f, 0.85f, 0.6f));

            // Door: opens when the bell is stopped and the square is clear.
            var door = Door("TowerDoor", t, new Vector3(0f, 0f, 27f), new Vector3(3f, 3.6f, 0.35f), "wood_dark", new Vector3(0f, 3.7f, 0f), 0f, F_Bell);
            door.toast = "The tower door unbars itself. The way up to the core is open.";
            door.setFlagWhenTrue = F_TowerOpen;
            var latch = Use("TowerDoorLatch", t, new Vector3(2.1f, 1.3f, 26.4f), new Vector3(0.45f, 0.6f, 0.3f), "brass", "Try the tower door", "L07.doorTried");
            latch.requiredFlags.Add(F_Bell);
            latch.lockedMessage = "Barred from inside while the bell drive is engaged. Stop the drive the signal names.";
            latch.lines.Add(L("Arin", "Open. Up to the core, before the second toll."));

            var ilseIn = new GameObject("IlseGoesIn").AddComponent<MemorySequence>();
            ilseIn.transform.SetParent(logic, false);
            ilseIn.triggerFlag = F_TowerOpen;
            ilseIn.lockControls = false;
            ilseIn.completeFlag = "L07.ilseInside";
            ilseIn.lines.Add(L("Ilse Marrow", "The drive's caught -- but it won't hold by itself. I'm going in to hold it by hand.", 4f));
            ilseIn.lines.Add(L("Arin", "Ilse, the tower could come down on you.", 3f));
            ilseIn.lines.Add(L("Ilse Marrow", "Then it comes down on someone who chose it. Climb, Arin.", 3.5f));
            ilseIn.lines.Add(L("Ilse Marrow", "Keep time for me.", 3f));

            // ---- Interior ----
            var inside = Group("Interior", t);
            Stairs("Flight1", inside, new Vector3(4.5f, 0f, 29f), 0f, 20, 0.2f, 0.3f, 2.4f, "wood");
            Floor("Landing", inside, new Vector3(4f, 4f, 37.75f), new Vector2(5.2f, 5.5f), "wood");
            Stairs("Flight2", inside, new Vector3(1.4f, 4f, 38.5f), -90f, 10, 0.2f, 0.3f, 2.4f, "wood");
            Floor("CorePlatform", inside, new Vector3(-4.1f, 6f, 34f), new Vector2(5f, 13f), "brass");
            Wall("PlatformRail", inside, new Vector3(-1.6f, 6f, 27.5f), new Vector3(-1.6f, 6f, 37.2f), 1f, 0.15f, "brass");
            Wall("LandingRail", inside, new Vector3(1.4f, 4f, 35f), new Vector3(1.4f, 4f, 37.2f), 1f, 0.15f, "brass");
            for (int i = 0; i < 3; i++)
            {
                var gear = Cylinder("Gear", inside, new Vector3(-6.3f, 10f + i * 3.2f, 30f + i * 4f), 1.4f - i * 0.25f, 0.3f, "brass", false);
                gear.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                var sp = gear.AddComponent<Spinner>(); sp.degreesPerSecond = new Vector3(0f, i % 2 == 0 ? 24f : -32f, 0f); sp.stopWhenFlag = F_Bell;
                var gear2 = Cylinder("Gear", inside, new Vector3(6.3f, 8f + i * 3.5f, 31f + i * 3.5f), 1.1f, 0.3f, "copper_verdigris", false);
                gear2.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                var sp2 = gear2.AddComponent<Spinner>(); sp2.degreesPerSecond = new Vector3(0f, i % 2 == 0 ? -20f : 28f, 0f); sp2.stopWhenFlag = F_Bell;
            }
            Box("Pendulum", inside, new Vector3(0f, 11f, 39.8f), new Vector3(0.2f, 10f, 0.2f), "brass", false);
            PointLight("TowerLamp1", inside, new Vector3(0f, 3f, 30.5f), new Color(1f, 0.75f, 0.45f), 3f, 10f);
            PointLight("BellLight", inside, new Vector3(0f, 13f, 33f), new Color(1f, 0.8f, 0.5f), 3f, 14f);
            ReflectionProbe(inside, new Vector3(0f, 10f, 34f), new Vector3(13f, 22f, 13f));

            // Core: rings of light over the platform, the zero at its heart.
            var core = Group("Core", inside);
            for (int i = 0; i < 3; i++)
            {
                var ring = Hologram("CoreRing", core, new Vector3(-4.1f, 8.2f + i * 0.2f, 33f), new Vector3(3.2f - i * 0.7f, 0.02f, 3.2f - i * 0.7f), PrimitiveType.Cylinder, i == 1 ? "hologram_amber" : "hologram", false, false);
                ring.transform.rotation = Quaternion.Euler(i * 25f, 0f, i * 15f);
                ring.AddComponent<Spinner>().degreesPerSecond = new Vector3(i * 10f, 40f + i * 25f, 0f);
            }
            var zero = Label("CoreZero", core, new Vector3(-4.1f, 8.2f, 33f), 90f, "0", 9f, new Color(0.9f, 0.97f, 1f));
            zero.AddComponent<FloatBob>().amplitude = 0.1f;
            PointLight("CoreGlow", core, new Vector3(-4.1f, 8.5f, 33f), new Color(0.6f, 0.9f, 1f), 5f, 10f);
            var coreZone = Zone("CoreZone", logic, new Vector3(-4.1f, 7f, 34f), new Vector3(4.8f, 2f, 12.5f), F_Core, "You reached the core. The second toll cannot fall now.");
            coreZone.requiredFlags.Add(F_Bell);
            coreZone.lines.Add(L("Arin", "The core. It's humming in all three states at once."));
            coreZone.lines.Add(L("The Witness", "Below the bell there is a room that was here before the city. Go in.", 4f));

            // Zero Chamber door in the back wall at platform height.
            NoShadow(Box("ChamberGlow", inside, new Vector3(-4.1f, 7.3f, 40.58f), new Vector3(2.2f, 2.6f, 0.02f), "zero_glow", false));
            Label("ChamberGlyph", inside, new Vector3(-4.1f, 9.3f, 40.5f), 0f, "(0)  ZERO CHAMBER", 0.5f, new Color(0.85f, 0.95f, 1f));
            var cdoor = Door("ZeroChamberDoor", inside, new Vector3(-4.1f, 6f, 40.3f), new Vector3(2.4f, 2.8f, 0.3f), "obsidian", new Vector3(0f, 3f, 0f), 0f, F_Core);
            cdoor.toast = "The Zero Chamber opens.";
            Zone("ChamberEnter", logic, new Vector3(-4.1f, 7.2f, 39.6f), new Vector3(2.4f, 2.4f, 1.2f), "L07.enteredChamber").requiredFlags.Add(F_Core);
            Exit("Exit_ZeroChamber", inside, new Vector3(-4.1f, 6f, 39.4f), new Vector3(2.4f, 2.5f, 1.4f),
                "The Zero Chamber is sealed. Reach the core first -- the bell must be stopped.", F_Core);

            // ---- Upper tower per state ----
            var up = Group("UpperTower", pastPresent);
            Box("Ceiling", up, new Vector3(0f, 24.2f, 34f), new Vector3(14f, 0.4f, 14f), "wood_dark");
            Box("Belfry", up, new Vector3(0f, 31f, 34f), new Vector3(14f, 14f, 14f), "stone_warm");
            Box("SpireBase", up, new Vector3(0f, 39f, 34f), new Vector3(10f, 2f, 10f), "copper_verdigris");
            Box("SpireMid", up, new Vector3(0f, 42f, 34f), new Vector3(6f, 4f, 6f), "copper_verdigris");
            Box("SpireTop", up, new Vector3(0f, 46f, 34f), new Vector3(2.5f, 4f, 2.5f), "copper_verdigris");
            Cylinder("Finial", up, new Vector3(0f, 50f, 34f), 0.2f, 4f, "brass", false);
            var bell = Group("Bell", up);
            Sphere("BellBody", bell, new Vector3(0f, 18f, 34f), 2.1f, "brass", false).transform.localScale = new Vector3(4.2f, 4.6f, 4.2f);
            Cylinder("BellLip", bell, new Vector3(0f, 16f, 34f), 2.4f, 0.5f, "brass", false);
            Box("Yoke", bell, new Vector3(0f, 20.8f, 34f), new Vector3(9f, 0.6f, 0.6f), "wood_dark", false);
            Sphere("Clapper", bell, new Vector3(0f, 15.4f, 34f), 0.45f, "iron_rust", false);

            var pastFace = Group("PastClock", past);
            ClockFace(pastFace, "paper", "wood_dark");
            var presentFace = Group("PresentClock", present);
            ClockFace(presentFace, "lamp_cold", "metal_dark");
            SpotLight("TowerFloodlight", presentFace, new Vector3(0f, 1f, 14f), new Vector3(-55f, 0f, 0f), new Color(0.75f, 0.85f, 1f), 6f, 40f, 40f, false);

            // FUTURE: split open, a beam and the signal rings.
            var fu = Group("TowerRuin", future);
            Tilted("SplitWest", fu, new Vector3(-4.2f, 30f, 34f), new Vector3(5.5f, 13f, 14f), "ruin_dark", new Vector3(0f, 0f, 11f), false);
            Tilted("SplitEast", fu, new Vector3(4.6f, 29f, 34f), new Vector3(5f, 11f, 14f), "ruin_dark", new Vector3(0f, 0f, -15f), false);
            NoShadow(Tilted("SeamWest", fu, new Vector3(-1.6f, 30f, 26.9f), new Vector3(0.18f, 12f, 0.18f), "neon_cyan", new Vector3(0f, 0f, 11f), false));
            NoShadow(Tilted("SeamEast", fu, new Vector3(2.2f, 29f, 26.9f), new Vector3(0.18f, 10f, 0.18f), "neon_magenta", new Vector3(0f, 0f, -15f), false));
            Hologram("SignalBeam", fu, new Vector3(0f, 60f, 34f), new Vector3(1.1f, 36f, 1.1f), PrimitiveType.Cylinder, "hologram_magenta", false, false);
            for (int i = 0; i < 4; i++)
            {
                var r = Hologram("SignalRing", fu, new Vector3(0f, 36f + i * 6f, 34f), new Vector3(6f + i * 2f, 0.04f, 6f + i * 2f), PrimitiveType.Cylinder, i % 2 == 0 ? "hologram_magenta" : "hologram", false, false);
                var sp = r.AddComponent<Spinner>(); sp.degreesPerSecond = new Vector3(0f, 30f + i * 12f, 0f); sp.stopWhenFlag = F_Bell;
                r.AddComponent<FloatBob>().amplitude = 0.6f;
            }
            var face = Hologram("HoloClockFace", fu, new Vector3(0f, 30f, 26.4f), new Vector3(7f, 0.05f, 7f), PrimitiveType.Cylinder, "hologram", false, false);
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("SignalGlyph", fu, new Vector3(0f, 30f, 26.2f), 0f, "[#]", 9f, new Color(1f, 0.55f, 0.95f));
            Label("SignalCaption", fu, new Vector3(0f, 25.6f, 26.2f), 0f, "TRANSMITTING >> TOLL 2    SIGNED (0)", 1.2f, new Color(1f, 0.6f, 0.95f));
            var cracked = Hologram("CrackedBell", fu, new Vector3(0f, 18f, 34f), new Vector3(4.2f, 4.6f, 4.2f), PrimitiveType.Sphere, "hologram_amber", true, false);
            cracked.GetComponent<FloatBob>().drift = new Vector3(0f, 6f, 0f);
            PointLight("BeamLight", fu, new Vector3(0f, 40f, 34f), new Color(1f, 0.3f, 0.8f), 8f, 50f);
        }

        static void ClockFace(Transform g, string faceMat, string handMat)
        {
            var disc = Cylinder("ClockFace", g, new Vector3(0f, 30f, 26.85f), 4f, 0.2f, faceMat, false);
            disc.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var rim = Cylinder("ClockRim", g, new Vector3(0f, 30f, 26.9f), 4.3f, 0.12f, "brass", false);
            rim.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Tilted("HourHand", g, new Vector3(0.6f, 30.9f, 26.7f), new Vector3(0.25f, 2.2f, 0.06f), handMat, new Vector3(0f, 0f, -33f), false);
            Tilted("MinuteHand", g, new Vector3(-0.2f, 31.6f, 26.68f), new Vector3(0.16f, 3.3f, 0.06f), handMat, new Vector3(0f, 0f, 6f), false);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                NoShadow(Box("Tick", g, new Vector3(Mathf.Sin(a) * 3.5f, 30f + Mathf.Cos(a) * 3.5f, 26.72f), new Vector3(0.18f, 0.18f, 0.05f), handMat, false));
            }
        }

        // =====================================================================
        // Crisis: timer, tremors (falling masonry)
        // =====================================================================

        static CrisisTimer BuildCrisis(Transform root, Transform shared, Transform logic)
        {
            var go = new GameObject("CrisisTimer");
            go.transform.SetParent(logic, false);
            var timer = go.AddComponent<CrisisTimer>();
            timer.duration = 480f;
            timer.startFlag = F_Start;
            timer.successFlag = F_Core;
            timer.failMessage = "The bell rings a second time. Founders' Square falls -- and the cycle begins again.";

            var start = Zone("CrisisStartZone", logic, new Vector3(0f, 1f, 0f), new Vector3(62f, 2f, 46f), F_Start);
            start.lines.Add(L("Odile Fenwick", "That's the first toll! Everyone -- to the Tram Hall, south-west!"));
            start.lines.Add(L("Arin", "Three groups still out there. And the bell is winding up for the second strike."));

            var tremors = Group("Tremors (after first toll)", shared);
            Masonry("Masonry_WA", tremors, new Vector3(-29.5f, 0f, -12f), new Vector3(-31.4f, 8f, -12f), 0f, TimeMask.Present, F_Bell);
            Masonry("Masonry_EA", tremors, new Vector3(29.5f, 0f, -14f), new Vector3(31.4f, 8f, -14f), 1.0f, TimeMask.Present, F_Bell);
            Masonry("Masonry_Hall", tremors, new Vector3(-18f, 0f, 25.5f), new Vector3(-18f, 10f, 27.6f), 4.0f, TimeMask.Present, F_Bell);

            var begin = Reactor("CrisisBegins", logic, F_Start);
            begin.enableWhenTrue.Add(tremors.gameObject);
            begin.announce = true;
            begin.reversible = false;
            return timer;
        }

        // =====================================================================
        // Evacuation: three groups, named leaders, warden, sheltering crowd
        // =====================================================================

        static EvacueeGroup Evac(string name, Transform parent, Vector3 center, int count, string flag, Transform safe, int seed, SubtitleLine[] follow, string toast, string[] barks)
        {
            var g = Group("Evacuees - " + name, parent);
            g.position = center;
            var e = g.gameObject.AddComponent<EvacueeGroup>();
            e.groupName = name;
            e.safeZone = safe;
            e.safeRadius = 3.5f;
            e.savedFlag = flag;
            e.prompt = "Lead " + name + " to the Tram Hall";
            e.usableIn = TimeMask.All;
            e.savedToast = toast;
            e.onFollow.AddRange(follow);
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var p = center + new Vector3((i % 3 - 1) * 1.0f, 0f, (i / 3) * 1.1f);
                var m = Npc(name + " " + (i + 1), g, p, rng.Next(360), null, false, seed + i);
                m.reactionBarks.AddRange(barks);
                Route(m, false, (p, 99999f, NPCActivity.Look, ""));
                e.members.Add(m.transform);
            }
            // Readable marker over the huddle.
            var mk = Hologram("GroupMarker", g, center + new Vector3(0f, 2.8f, 0.5f), new Vector3(0.5f, 0.05f, 0.5f), PrimitiveType.Cylinder, "hologram_amber", true, true);
            Label("GroupLabel", g, center + new Vector3(0f, 3.2f, 0.5f), 0f, name.ToUpperInvariant(), 0.35f, new Color(1f, 0.85f, 0.5f));
            var hide = Reactor("HideMarker_" + name, parent, flag);
            hide.disableWhenTrue.Add(mk);
            return e;
        }

        // =====================================================================
        // West arcade: brace a Present wall with Past scaffolding
        // =====================================================================

        static void BuildArcade(Transform root, Transform shared, Transform present, Transform future, Transform logic)
        {
            // PRESENT: leaning wall + rubble (until braced) / straight braced wall (after).
            var pr = Group("Arcade Lane (Present)", present);
            var leaning = Tilted("LeaningFacade", pr, new Vector3(-37f, 4.5f, 3.3f), new Vector3(10f, 9f, 0.5f), "brick_warm", new Vector3(-9f, 0f, 0f));
            var rubble = Group("Collapse", pr);
            Box("RubbleCore", rubble, new Vector3(-35.5f, 1.8f, 1.5f), new Vector3(3f, 3.6f, 5.2f), "stone_wet");
            Tilted("RubbleSlab", rubble, new Vector3(-34f, 0.8f, 0.5f), new Vector3(2.2f, 1.2f, 1.6f), "brick_warm", new Vector3(12f, 25f, 8f));
            Tilted("RubbleBeam", rubble, new Vector3(-35f, 2.9f, 2.5f), new Vector3(0.3f, 0.3f, 4.5f), "wood_dark", new Vector3(30f, 70f, 0f), false);
            Label("CollapseSign", rubble, new Vector3(-33.9f, 2.2f, 0f), -90f, "COLLAPSE\nNO WAY THROUGH", 0.45f, new Color(1f, 0.5f, 0.4f));
            Particles("CollapseDust", rubble, new Vector3(-35f, 4f, 2f), new Vector3(3f, 1f, 3f), "dust");
            var braced = Group("BracedWall", pr);
            Box("StraightFacade", braced, new Vector3(-37f, 4.5f, 3.7f), new Vector3(10f, 9f, 0.5f), "brick_warm");
            for (int i = 0; i < 3; i++)
            {
                float x = -40f + i * 3f;
                Tilted("IronShore", braced, new Vector3(x, 2.6f, 2.55f), new Vector3(0.25f, 5.6f, 0.25f), "iron_rust", new Vector3(25f, 0f, 0f), false);
                Box("ShorePlate", braced, new Vector3(x, 5f, 3.42f), new Vector3(0.7f, 0.7f, 0.06f), "iron_rust", false);
            }
            Label("ShoreText", braced, new Vector3(-37f, 6.2f, 3.4f), 0f, "SHORED IN THE FESTIVAL YEAR", 0.4f, new Color(0.9f, 0.8f, 0.6f));
            var react = Reactor("ArcadeBraced", logic, F_Braced);
            react.enableWhenTrue.Add(braced.gameObject);
            react.disableWhenTrue.Add(leaning);
            react.disableWhenTrue.Add(rubble.gameObject);
            react.announce = true;
            react.toast = "In the PRESENT, the festival scaffolding has held the arcade wall for a hundred years. The lane to Pell and Lise is open.";
            Masonry("ArcadeCollapse", shared, new Vector3(-33.8f, 0f, 1.5f), new Vector3(-34.5f, 7.5f, 3f), 1f, TimeMask.Present, F_Braced, "brick_warm");

            // PAST: scaffolding on the same wall, works barrier across the lane, the clamp.
            var pastGame = StateGroup("Arcade Scaffolding (Past)", root, TimeMask.Past, true);
            var scaffold = Group("Scaffold", pastGame);
            float[] poles = { -41f, -38.5f, -36f, -33.6f };
            foreach (var x in poles) Box("Pole", scaffold, new Vector3(x, 4.5f, 3.4f), new Vector3(0.18f, 9f, 0.18f), "wood", false);
            for (int k = 1; k <= 2; k++) Box("Plank", scaffold, new Vector3(-37.3f, k * 3f, 3.25f), new Vector3(8f, 0.12f, 0.9f), "wood_dark", false);
            var shores = Group("ShoresLocked", pastGame);
            for (int i = 0; i < 3; i++)
                Tilted("TimberShore", shores, new Vector3(-40f + i * 3f, 2.6f, 2.55f), new Vector3(0.25f, 5.6f, 0.25f), "wood", new Vector3(25f, 0f, 0f), false);
            var pastReact = Reactor("PastShores", logic, F_Braced);
            pastReact.enableWhenTrue.Add(shores.gameObject);
            Wall("WorksBarrier", pastGame, new Vector3(-33f, 0f, -1f), new Vector3(-33f, 0f, 2.9f), 2.2f, 0.25f, "wood");
            Label("WorksSign", pastGame, new Vector3(-32.8f, 1.6f, 1f), -90f, "WORKS -- NO PASSAGE\nFESTIVAL REPAIRS", 0.4f, new Color(0.25f, 0.15f, 0.05f));
            NoShadow(Box("WorksBoard", pastGame, new Vector3(-32.85f, 1.6f, 1f), new Vector3(0.04f, 0.8f, 2.6f), "paper", false));
            Box("ClampBase", pastGame, new Vector3(-32.4f, 0.5f, 3.3f), new Vector3(0.5f, 1f, 0.5f), "brass");
            var clamp = Use("ScaffoldClamp", pastGame, new Vector3(-32.4f, 1.45f, 3.3f), new Vector3(0.4f, 0.9f, 0.4f), "brass", "Lock the scaffolding to the wall", F_Braced, TimeMask.Past);
            clamp.animateTarget = clamp.transform;
            clamp.rotateBy = new Vector3(0f, 0f, -70f);
            clamp.cue = Cue.Clang;
            clamp.usedPrompt = "";
            clamp.lines.Add(L("Arin", "Locked. Shores against the brick... In a hundred years they'll be iron in the wall."));
            clamp.lines.Add(L("Arin", "Back to the PRESENT. The lane should be standing."));
            Label("ClampTag", pastGame, new Vector3(-32.4f, 2.3f, 3.0f), -90f, "SCAFFOLD LOCK", 0.3f, new Color(1f, 0.9f, 0.6f));

            // FUTURE: the lane is long collapsed.
            var fu = Group("Arcade Ruin (Future)", future);
            Box("FallenArcade", fu, new Vector3(-37f, 2.5f, 1.5f), new Vector3(8f, 5f, 5.2f), "ruin_dark");
            for (int i = 0; i < 3; i++)
                Tilted("RuinCrystal", fu, new Vector3(-35f - i * 1.6f, 5.5f, 0.5f + i), new Vector3(0.5f, 2.4f, 0.5f), "crystal", new Vector3(15f * i - 15f, i * 40f, 20f), false);
            Label("RuinTag", fu, new Vector3(-32.9f, 2.4f, 1.5f), -90f, "SEALED SINCE THE SECOND TOLL", 0.4f, new Color(0.6f, 0.95f, 1f));
        }

        // =====================================================================
        // FUTURE surge in Keeper's Alley + the signal relay
        // =====================================================================

        static void BuildSurge(Transform root, Transform shared, Transform future, Transform logic)
        {
            var game = StateGroup("Surge Gameplay (Future)", root, TimeMask.Future, true);
            // Fallen pylon with the grounding cable (market).
            Tilted("FallenPylon", game, new Vector3(27f, 0.6f, 17.5f), new Vector3(0.6f, 0.6f, 7f), "ruin_dark", new Vector3(0f, 35f, 8f));
            NoShadow(Tilted("PylonNeon", game, new Vector3(27f, 1f, 17.5f), new Vector3(0.12f, 0.12f, 6.6f), "neon_cyan", new Vector3(0f, 35f, 8f), false));
            var cable = Grab("GroundingCable", game, new Vector3(25.5f, 0.3f, 15f), new Vector3(0.7f, 0.22f, 0.7f), "copper_verdigris", "cable", "grounding cable", PrimitiveType.Cylinder);
            cable.pickedUpFlag = "L07.cableTaken";
            // No physics body: while the FUTURE is hidden its collider is off and a body would fall through the floor.
            Object.DestroyImmediate(cable.GetComponent<Rigidbody>());
            Label("CableTag", game, new Vector3(25.5f, 1.4f, 15f), FaceYaw(new Vector3(25.5f, 0f, 15f), new Vector3(10f, 0f, 5f)), "GROUNDING CABLE", 0.3f, new Color(0.6f, 1f, 0.85f));

            // Earth spike at the alley mouth.
            var spike = Socket("EarthSpike", game, new Vector3(11.5f, 0.03f, 24.5f), "cable", F_Grounded, "Clamp", 0.45f);
            spike.wrongItemMessage = "The spike wants a grounding cable.";
            Cylinder("SpikePost", game, new Vector3(12.6f, 0.9f, 24.5f), 0.12f, 1.8f, "copper_verdigris");
            Label("SpikeTag", game, new Vector3(12.6f, 2.2f, 24.4f), 0f, "EARTH SPIKE\nGROUND THE SURGE", 0.3f, new Color(0.6f, 1f, 0.85f));

            // Two surge arcs across the alley.
            var arcs = Group("Surges", shared);
            for (int i = 0; i < 2; i++)
            {
                float z = 31f + i * 6f;
                Tilted("Emitter", game, new Vector3(7.8f, 1.4f, z), new Vector3(0.6f, 2.8f, 0.6f), "crystal", new Vector3(0f, 45f, 0f));
                Tilted("Emitter", game, new Vector3(15.2f, 1.4f, z), new Vector3(0.6f, 2.8f, 0.6f), "crystal", new Vector3(0f, 45f, 0f));
                NoShadow(Box("EmitterCap", game, new Vector3(7.8f, 2.95f, z), new Vector3(0.35f, 0.3f, 0.35f), "neon_magenta", false));
                NoShadow(Box("EmitterCap", game, new Vector3(15.2f, 2.95f, z), new Vector3(0.35f, 0.3f, 0.35f), "neon_magenta", false));
                NoShadow(Box("FloorBurn", game, new Vector3(11.5f, 0.015f, z), new Vector3(7f, 0.01f, 1.2f), "ruin_dark", false));

                var hz = Group("Surge" + i, arcs);
                hz.position = new Vector3(11.5f, 0f, z);
                var arc = Group("Arc", hz);
                NoShadow(Box("ArcHigh", arc, new Vector3(11.5f, 1.5f, z), new Vector3(7.2f, 0.12f, 0.12f), "neon_magenta", false));
                NoShadow(Tilted("ArcLow", arc, new Vector3(11.5f, 0.8f, z), new Vector3(7.2f, 0.08f, 0.08f), "neon_cyan", new Vector3(0f, 0f, 4f), false));
                Hologram("ArcGlow", arc, new Vector3(11.5f, 1.1f, z), new Vector3(7.2f, 1.6f, 0.3f), PrimitiveType.Cube, "hologram_magenta", false, false);
                arc.gameObject.SetActive(false);
                var warn = PointLight("WarnLight", hz, new Vector3(11.5f, 2.6f, z), new Color(1f, 0.2f, 0.6f), 7f, 8f);
                var sparksW = Particles("WarnSparks", hz, new Vector3(7.8f, 2.9f, z), Vector3.one * 0.3f, "sparks");
                var sparksA = Particles("ArcSparks", hz, new Vector3(11.5f, 1.2f, z), Vector3.one * 0.3f, "sparks");
                var zoneGo = new GameObject("SurgeHazard");
                zoneGo.transform.SetParent(hz, false);
                zoneGo.transform.position = new Vector3(11.5f, 1.3f, z);
                var bc = zoneGo.AddComponent<BoxCollider>();
                bc.size = new Vector3(9f, 2.6f, 1.6f);
                bc.isTrigger = true;
                var h = zoneGo.AddComponent<TelegraphedHazard>();
                h.idleTime = 2.2f; h.warnTime = 1.5f; h.activeTime = 2.2f; h.startOffset = i * 1.6f;
                h.activeIn = TimeMask.Future; h.disabledByFlag = F_Grounded;
                h.warnLight = warn; h.warnParticles = sparksW; h.activeParticles = sparksA; h.activeVisual = arc.gameObject; h.damageZone = bc;
                h.deathMessage = "The surge arcs across the alley. Pink light and sparks mean it is about to fire -- or ground it.";
            }
            var grounded = Reactor("SurgeGrounded", logic, F_Grounded);
            grounded.announce = true;
            grounded.toast = "The surge bleeds into the earth spike. Keeper's Alley goes quiet -- and the relay wakes.";

            // Signal relay at the end of the alley.
            var relay = Clue("SignalRelay", game, new Vector3(11.5f, 1.1f, 43.8f), new Vector3(2.4f, 2.2f, 0.6f), "obsidian", "Read the tower's signal", F_Signal, "L07.signal",
                "Relay: Outbound Signal",
                "SOURCE: CLOCK TOWER -- AFTER\nTARGET: BELL DRIVE [#]  (III, THE CAGE)\nCOMMAND: STRIKE TOLL 2\nREPEAT: EVERY CYCLE\n\nThe transmission comes from the tower's own future ruins, and it is addressed to the bell's past.\n\nSIGNATURE: (0)\nThe zero is drawn in one stroke, starting at the bottom and closing with a small hook -- exactly the way Arin writes it.",
                true, TimeMask.Future);
            relay.requiredFlags.Add(F_Grounded);
            relay.lines.Add(L("Arin", "Drive III. The cage. And at the end... a zero, one stroke, a hook where it closes."));
            relay.lines.Add(L("Arin", "That's how I sign my name. Who in the future signs with my hand?", 4f));
            relay.lockedMessage = "The relay is drowned in surge static. Ground the surge first -- the earth spike at the alley mouth.";
            NoShadow(Box("RelayTrim", game, new Vector3(11.5f, 2.25f, 43.45f), new Vector3(2.5f, 0.08f, 0.08f), "neon_cyan", false));
            var screen = Group("RelayDisplay", game);
            Hologram("Screen", screen, new Vector3(11.5f, 3.6f, 43.6f), new Vector3(3.2f, 1.8f, 0.04f), PrimitiveType.Cube, "hologram_magenta", true, false);
            Label("ScreenGlyph", screen, new Vector3(11.5f, 3.8f, 43.5f), 0f, "[#]", 3f, Color.white);
            Label("ScreenText", screen, new Vector3(11.5f, 3.05f, 43.5f), 0f, "DRIVE III  >>  TOLL 2", 0.45f, new Color(1f, 0.8f, 1f));
            Label("ScreenSignature", screen, new Vector3(13.0f, 2.6f, 43.5f), 0f, "-- (0)", 0.5f, new Color(1f, 0.95f, 0.8f));
            Hologram("GlyphCube", screen, new Vector3(11.5f, 5.3f, 43.6f), new Vector3(0.8f, 0.8f, 0.8f), PrimitiveType.Cube, "hologram_magenta", true, true);
            var stat = Group("RelayStatic", game);
            Label("Static", stat, new Vector3(11.5f, 3.4f, 43.5f), 0f, "## STATIC ##\nOVERLOAD", 0.6f, new Color(1f, 0.4f, 0.4f));
            var relayReact = Reactor("RelayOnline", logic, F_Grounded);
            relayReact.enableWhenTrue.Add(screen.gameObject);
            relayReact.disableWhenTrue.Add(stat.gameObject);
            PointLight("AlleyNeon", game, new Vector3(11.5f, 4f, 40f), new Color(0.4f, 0.85f, 1f), 3f, 12f);
            Label("AlleyName", future, new Vector3(11.5f, 6f, 26.2f), 0f, "KEEPER'S ALLEY", 0.8f, new Color(0.6f, 0.95f, 1f));
        }

        // =====================================================================
        // Bell drives I /\  II (0)  III [#]
        // =====================================================================

        static void BuildDrives(Transform root, Transform shared, Transform past, Transform present, Transform future, Transform logic, CrisisTimer timer)
        {
            var fore = Group("Forecourt", shared);
            float[] xs = { -6f, 0f, 6f };
            string[] names = { "I", "II", "III" };
            string[] glyphs = { "/\\", "(0)", "[#]" };
            string[] shapes = { "the spire", "the wheel", "the cage" };
            const string locked = "Three drives, one toll. Which one strikes the second? Find the signal (FUTURE >>, Keeper's Alley, east of the tower).";
            var game = StateGroup("Bell Drives (Present)", root, TimeMask.Present, true);
            for (int i = 0; i < 3; i++)
            {
                var b = new Vector3(xs[i], 0f, 22f);
                Box("DrivePlinth", fore, b + Vector3.up * 0.25f, new Vector3(2.4f, 0.5f, 2.4f), "stone_warm");
                Label("DriveNumber", fore, b + new Vector3(0f, 0.3f, -1.22f), 0f, "DRIVE " + names[i] + "   " + glyphs[i], 0.35f, new Color(1f, 0.9f, 0.7f));
                Box("Linkage", fore, b + new Vector3(0f, 2.6f, 2.4f), new Vector3(0.15f, 0.15f, 4.4f), "iron_rust", false);
                Box("Rod", fore, new Vector3(xs[i], 8f, 26.45f), new Vector3(0.15f, 11f, 0.15f), "iron_rust", false);

                // PRESENT housings (the playable ones).
                DriveHousing(game, b, i, "metal_dark", "iron_rust");
                Label("DriveGlyph", game, b + new Vector3(0f, 3.6f, 0f), 0f, names[i] + "\n" + glyphs[i], 0.7f, Color.white);
                var leverPos = b + new Vector3(1.0f, 1.0f, -1.0f);
                if (i == 2)
                {
                    var right = Use("DriveLever" + names[i], game, leverPos, new Vector3(0.4f, 0.9f, 0.4f), "brass", "Open the striking frame of drive " + names[i] + " " + glyphs[i], F_DriveOpen, TimeMask.Present);
                    right.requiredFlags.Add(F_Signal);
                    right.lockedMessage = locked;
                    right.animateTarget = right.transform;
                    right.rotateBy = new Vector3(70f, 0f, 0f);
                    right.cue = Cue.RuleChange;
                    right.lines.Add(L("Arin", "The cage opens -- and six striking plates fold out around the forecourt."));
                    right.lines.Add(L("Ilse Marrow", "It won't stop for a lever. It stops for a tune. The same one that sang the children to sleep.", 4f));
                    right.lines.Add(L("Ilse Marrow", "One, three, five, four. Strike them in order -- and mind the stones.", 3.5f));
                }
                else
                {
                    var lever = Prim(PrimitiveType.Cube, "DriveLever" + names[i], game, leverPos, new Vector3(0.4f, 0.9f, 0.4f), "brass", true, false);
                    var w = lever.AddComponent<L07WrongMechanism>();
                    w.prompt = "Open the striking frame of drive " + names[i] + " " + glyphs[i];
                    w.usableIn = TimeMask.Present;
                    w.requiredFlags.Add(F_Signal);
                    w.lockedMessage = locked;
                    w.timer = timer;
                    w.penaltySeconds = 30f;
                    w.warning = "Wrong drive -- " + shapes[i] + " only winds the hours. The bell lurches; the clock jumps ahead.";
                    w.lever = lever.transform;
                    w.sparks = Particles("WrongSparks", game, b + new Vector3(0f, 2.2f, 0f), Vector3.one * 0.3f, "sparks");
                }
            }
            PointLight("ForecourtLamp", game, new Vector3(0f, 4.5f, 18f), new Color(1f, 0.65f, 0.35f), 3f, 16f);

            // ---- CLIMAX: the lullaby on six striking plates, under falling masonry ----
            var ppg = new GameObject("DriveSequence");
            ppg.transform.SetParent(logic, false);
            ppg.transform.position = new Vector3(6f, 1.5f, 22f);
            var seq = ppg.AddComponent<SequencePuzzle>();
            seq.solution = new List<int> { 0, 2, 4, 3 }; // the Nursery lullaby: 1 3 5 4
            seq.solvedFlag = F_Bell;
            seq.requiredFlags.Add(F_DriveOpen);
            seq.demoNoteGap = 0.7f;
            var plates = StateGroup("Striking Plates (Present)", root, TimeMask.Present, true);
            var plateFolded = new List<GameObject>();
            var platePos = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (200f + i * 28f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 9.5f, 0f, 22f + Mathf.Sin(a) * 9.5f);
                platePos[i] = p;
                var post = Prim(PrimitiveType.Cube, "StrikingPlate " + (i + 1), plates, p + Vector3.up * 0.55f, new Vector3(0.6f, 1.1f, 0.6f), "metal_dark", true, false);
                var disc = Cylinder("PlateFace", plates, p + Vector3.up * 1.15f, 0.42f, 0.08f, "brass", false);
                var btn = post.AddComponent<SequenceButton>();
                btn.puzzle = seq;
                btn.noteIndex = i;
                btn.bellLike = true;
                btn.pressVisual = disc.transform;
                btn.prompt = "Strike plate " + (i + 1);
                btn.usableIn = TimeMask.Present;
                btn.requiredFlags.Add(F_DriveOpen);
                btn.lockedMessage = "A folded striking plate. It opens with the right drive.";
                var lbl = Label("PlateNumber", plates, p + Vector3.up * 1.9f, FaceYaw(p, new Vector3(0f, 0f, 8f)), (i + 1).ToString(), 1.4f, Color.white);
                lbl.AddComponent<FloatBob>().amplitude = 0.05f;
                var cover = Box("FoldedCover", plates, p + Vector3.up * 1.2f, new Vector3(0.7f, 0.12f, 0.7f), "iron_rust", false);
                plateFolded.Add(cover);
            }
            var unfold = Reactor("PlatesUnfold", logic, F_DriveOpen);
            unfold.disableWhenTrue.AddRange(plateFolded);
            unfold.announce = true;
            unfold.toast = "Six striking plates unfold around the forecourt. Play the lullaby: 1, 3, 5, 4.";
            var listen = Prim(PrimitiveType.Cube, "DriveIIIEar", plates, new Vector3(7.4f, 0.9f, 20.6f), new Vector3(0.5f, 0.6f, 0.5f), "brass", true, false);
            var ml = listen.AddComponent<MelodyListener>();
            ml.puzzle = seq; ml.bellLike = true; ml.heardFlag = "L07.driveTuneHeard";
            ml.prompt = "Listen to drive III's broken tune";
            ml.usableIn = TimeMask.Present;
            ml.requiredFlags.Add(F_DriveOpen);
            ml.lockedMessage = "Silent until the striking frame is open.";
            var hum = new GameObject("DriveHumReceiver");
            hum.transform.SetParent(logic, false);
            hum.transform.position = new Vector3(6f, 1f, 20f);
            var hr = hum.AddComponent<HumReceiver>();
            hr.radius = 7f;
            hr.requiredFlags.Add(F_DriveOpen);
            hr.setsFlag = "L07.hummedDrive";
            hr.lines.Add(L("Ilse Marrow", "That's it -- that's the song. You always hummed all four notes, even when you were small.", 4f));
            hr.lines.Add(L("Ilse Marrow", "One, three, five, four. Now strike it.", 3f));

            var falling = Group("Climax Masonry", shared);
            int[] cover3 = { 1, 3, 5 };
            for (int k = 0; k < 3; k++)
            {
                var p = platePos[cover3[k]];
                var h = Masonry("ClimaxMasonry" + k, falling, p, p + new Vector3(0f, 9f, 0.8f), k * 1.6f, TimeMask.Present, F_Bell);
                h.idleTime = 2.6f; h.warnTime = 1.4f; h.activeTime = 1.0f;
                h.deathMessage = "The tower is shedding stone. Strike a plate between the falls -- red light and grit mean move.";
            }
            var extra = Masonry("OndineMasonry", falling, platePos[0], platePos[0] + new Vector3(0f, 9f, 0.8f), 0.8f, TimeMask.Present, F_Bell);
            extra.idleTime = 3f; extra.warnTime = 1.3f;
            extra.gameObject.SetActive(false);
            var defy = Reactor("OndineDefiedMasonry", logic, "L07.ondineDefied", F_DriveOpen);
            defy.enableWhenTrue.Add(extra.gameObject);
            falling.gameObject.SetActive(false);
            var climax = Reactor("ClimaxBegins", logic, F_DriveOpen);
            climax.enableWhenTrue.Add(falling.gameObject);
            climax.announce = true;
            climax.reversible = false;

            var stop = Reactor("BellStopped", logic, F_Bell);
            stop.announce = true;
            stop.cue = Cue.Revelation;
            stop.toast = "The last note lands. The bell is caught mid-swing; the tremors stop.";

            // PAST: polished brass drives + the clockkeeper's notes.
            var pg = Group("Drives (Past)", past);
            for (int i = 0; i < 3; i++) DriveHousing(pg, new Vector3(xs[i], 0f, 22f), i, "brass", "wood_dark");
            Box("KeeperBench", pg, new Vector3(10.5f, 0.45f, 21.5f), new Vector3(2f, 0.9f, 1f), "wood");
            var notesGroup = StateGroup("Keeper's Notes (Past)", root, TimeMask.Past, true);
            Clue("KeeperNotes", notesGroup, new Vector3(10.5f, 0.95f, 21.5f), new Vector3(0.6f, 0.06f, 0.45f), "paper", "Read the clockkeeper's notes", "L07.notesRead", "L07.keeperNotes",
                "Clockkeeper's Notes (Festival Year)",
                "Three drives sit under the bell. I know them by their housings, never by number:\n  /\\  the spire     (0)  the wheel     [#]  the cage\n\nOne of them strikes the second toll -- and the tower chooses which. Some nights I hear it choosing.\n\nThe order comes from above. From a part of the tower that has not been built yet.\n\n-- I. Marrow, clockkeeper",
                true, TimeMask.Past);

            // FUTURE: ruined drives, the cage still glowing.
            var fg = Group("Drives (Future)", future);
            for (int i = 0; i < 3; i++)
            {
                var b = new Vector3(xs[i], 0f, 22f);
                Tilted("DriveRuin", fg, b + Vector3.up * 1.0f, new Vector3(1.4f, 1.2f, 1.2f), "ruin_dark", new Vector3(10f, i * 30f, 15f), false);
                Tilted("DriveCrystal", fg, b + new Vector3(0.6f, 1.8f, 0.3f), new Vector3(0.35f, 1.6f, 0.35f), "crystal", new Vector3(-12f, 0f, 18f), false);
                Label("RuinGlyph", fg, b + new Vector3(0f, 3.2f, 0f), 0f, glyphs[i], 0.8f, i == 2 ? new Color(1f, 0.5f, 0.95f) : new Color(0.5f, 0.6f, 0.7f));
            }
            NoShadow(Box("CageTrace", fg, new Vector3(6f, 0.02f, 24f), new Vector3(0.2f, 0.01f, 4f), "neon_magenta", false));
        }

        static void DriveHousing(Transform g, Vector3 b, int kind, string mat, string trim)
        {
            switch (kind)
            {
                case 0: // spire /\
                    var sp = Box("SpireHousing", g, b + Vector3.up * 1.55f, new Vector3(1.15f, 1.15f, 1.15f), mat);
                    sp.transform.rotation = Quaternion.Euler(45f, 0f, 35.26f);
                    Cylinder("SpireNeedle", g, b + Vector3.up * 2.6f, 0.08f, 1.2f, trim, false);
                    break;
                case 1: // wheel (0)
                    var wheel = Cylinder("WheelHousing", g, b + Vector3.up * 1.6f, 1.0f, 0.4f, mat);
                    wheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    var hub = Cylinder("WheelHub", g, b + new Vector3(0f, 1.6f, -0.25f), 0.3f, 0.2f, trim, false);
                    hub.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    var spin = hub.AddComponent<Spinner>(); spin.degreesPerSecond = new Vector3(0f, 90f, 0f); spin.stopWhenFlag = F_Bell;
                    break;
                default: // cage [#]
                    Box("CageCore", g, b + Vector3.up * 1.5f, new Vector3(1.0f, 1.4f, 1.0f), mat);
                    for (int c = 0; c < 4; c++)
                    {
                        float cx = c % 2 == 0 ? -0.7f : 0.7f, cz = c < 2 ? -0.7f : 0.7f;
                        Box("CageBar", g, b + new Vector3(cx, 1.5f, cz), new Vector3(0.1f, 2f, 0.1f), trim, false);
                    }
                    Box("CageTop", g, b + Vector3.up * 2.55f, new Vector3(1.6f, 0.1f, 1.6f), trim, false);
                    break;
            }
        }
    }
}
