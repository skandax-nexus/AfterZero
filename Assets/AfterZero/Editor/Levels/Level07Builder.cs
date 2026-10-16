using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 07 — The Bell Catastrophe (design v2, "the hardest action"). Founders' Square and the Clock Tower.
    /// Layout (y = 0 square, +z = north):
    ///   Warden's Terrace (start, y 3, safe observation) at the south edge, stairs down into the square.
    ///   Tram Hall (safe zone, inside) in the south-west corner. Founder Halden blocks its front gate;
    ///   the alternate route is the Keepers' back lane from the arcade courtyard to its service door.
    ///   Group 1 (Mirela) in Market Row, east: fire in the PRESENT, route through the PAST.
    ///   Group 2 (Pell + Lise) in the courtyard behind the cracked arcade wall: brace it with PAST scaffolding.
    ///   Group 3 (Warden Roe) behind the tower, reached through Keeper's Alley: ground the FUTURE surge.
    ///   Climax at the tower forecourt: open drive III (named by the signal) and play the lullaby on its
    ///   striking plates under falling masonry. Ilse walks into the tower. Climb to the core, Zero Chamber.
    /// </summary>
    public static partial class Level07Builder
    {
        const string F_Start = "L07.crisisStarted";
        const string F_G1 = "L07.group1Saved";
        const string F_G2 = "L07.group2Saved";
        const string F_G3 = "L07.group3Saved";
        const string F_All = "L07.allSaved";
        const string F_Braced = "L07.scaffoldBraced";
        const string F_Grounded = "L07.surgeGrounded";
        const string F_Signal = "L07.signalSeen";
        const string F_DriveOpen = "L07.driveOpened";
        const string F_Bell = "L07.bellDisabled";
        const string F_Core = "L07.coreReached";
        const string F_Notes = "clue.L07.keeperNotes";
        const string F_TowerOpen = "L07.towerOpen";
        const string F_Halden = "clue.L07.halden";
        const string F_Plan = "clue.L07.plan";
        const string F_HYield = "L07.haldenYielded";
        const string F_HTalked = "L07.haldenTalked";
        const string F_BackDoor = "L07.backDoorOpen";
        const string F_TramRoute = "L07.tramRoute";

        static readonly Vector3 SafePos = new Vector3(-22f, 0f, -32.4f);

        public static void Build(Transform root)
        {
            var d = Systems(root, 6, TimeMask.All, TimeState.Present, out var visuals);
            d.storySetup = "The catastrophe night, lived. The bell has rung once; before it rings twice, three groups must reach the Tram Hall by three different routes -- past a Founder who would rather they didn't -- and the bell must be stopped.";
            d.interludeTitle = "Before the Second Bell";
            d.interludeLines.Add(L("The Witness", "Arin. Before you go down there -- listen.", 3.5f));
            d.interludeLines.Add(L("The Witness", "You've done this before. You chose differently.", 4f));
            d.interludeLines.Add(L("Arin", "How many times?", 2.5f));
            d.interludeLines.Add(L("The Witness", "Enough that the stones remember your footsteps. Not enough that you do.", 4.5f));
            d.interludeLines.Add(L("The Witness", "Tonight, save who you can. Then follow the signal to whoever is sending it.", 4.5f));
            d.interludeLines.Add(L("Arin", "And if it's someone I know?", 3f));
            d.introLines.Add(L("Arin", "Founders' Square. Festival, rain and ruin -- all three at once now, stacked on the same stones."));
            d.introLines.Add(L("Arin", "From up here I can see all of it. Once I step down, the bell rings, and the clock starts."));
            d.introLines.Add(L("Odile Fenwick", "Arin! You came back. When it starts: the Tram Hall, south-west. But Founder Halden is standing at its gate."));
            d.introLines.Add(L("The Witness", "It has always rung from somewhere further on. Listen for where.", 4f));

            AddObjective(d, "Step down into the square", F_Start, "Take the terrace stairs down. Odile waits at the bottom.");
            AddObjective(d, "Brace the cracked arcade wall with the PAST scaffolding", F_Braced, "The west lane is blocked by a collapse in the PRESENT. In the PAST the same wall has scaffolding -- lock it in place.");
            AddObjective(d, "Open the Tram Hall: face Founder Halden, or take the back lane", F_TramRoute, "Halden at the gate yields only to proof (the list, the record, the register). Otherwise: arcade courtyard, south door, the Keepers' back lane -- shift to the PAST to slip past their lanterns -- and lift the service door bar.");
            AddObjective(d, "Lead Mirela's market group out through the PAST", "L07.g1Resolved", "Market Row (east) is on fire in the PRESENT. In the PAST the lane is open: shift, then lead them out and on to the Tram Hall.");
            AddObjective(d, "Lead Pell and Lise's group out of the courtyard", "L07.g2Resolved", "Through the braced arcade lane, then to the Tram Hall.");
            AddObjective(d, "Ground the surge in the FUTURE", F_Grounded, "Keeper's Alley (east of the tower) is rubble in the PRESENT and locked in the PAST. In the FUTURE it is open but arcing: carry the grounding cable from the market pylon to the earth spike.");
            AddObjective(d, "Lead Warden Roe's group out from behind the tower", "L07.g3Resolved", "Warden Roe's drill group is trapped behind the tower, at the end of Keeper's Alley. Lead them out in the FUTURE.");
            AddObjective(d, "Read the signal from the tower's future ruins", F_Signal, "The relay at the end of Keeper's Alley (FUTURE) names the drive that strikes the second toll.");
            AddObjective(d, "Open the drive the signal names", F_DriveOpen, "Three drives on the tower forecourt (PRESENT): I /\\, II (0), III [#]. A wrong drive costs time.");
            AddObjective(d, "Play the lullaby on the drive's striking plates", F_Bell, "Six plates, numbered 1-6, under falling masonry. The Nursery song: 1 3 5 4. Hum [H] near the drive if you forget.");
            AddObjective(d, "Climb the tower to the core", F_Core, "The tower door opens once the bell is stopped.");
            AddObjective(d, "Enter the Zero Chamber", "L07.enteredChamber", "The glowing door at the top of the core platform.");

            d.payoffFlag = F_Bell;
            d.payoffClueTitle = "The Signal From After";
            d.payoffClueText = "Drive III never answered a hand in Veyra. It answered a signal broadcast from the clock tower's own future ruins -- signed with a zero drawn in one stroke, closing on a small hook.\n\nArin's zero. Arin's hand. Someone in the future is ringing the bell backwards through time, and they write exactly like you.";

            // ---------------- Look ----------------
            visuals.past.fogDensity = 0.009f;
            visuals.present.fogDensity = 0.02f;
            visuals.present.colorFilter = new Color(0.92f, 0.95f, 1f);
            visuals.present.bloomIntensity = 1.3f;
            visuals.future.fogDensity = 0.016f;
            visuals.future.bloomIntensity = 2.6f;
            visuals.future.chromatic = 0.25f;

            Player(root, new Vector3(0f, 3.05f, -34f), 0f);
            Checkpoint("CP_Terrace", root, new Vector3(0f, 3f, -34f), new Vector3(6f, 3f, 4f), 0f, true);
            Checkpoint("CP_StairFoot", root, new Vector3(0f, 0f, -20f), new Vector3(8f, 3f, 3f), 0f);
            Checkpoint("CP_Courtyard", root, new Vector3(-48f, 0f, 2f), new Vector3(10f, 3f, 16f), 90f);
            Checkpoint("CP_BackLane", root, new Vector3(-48f, 0f, -10f), new Vector3(4f, 3f, 2f), 180f);
            Checkpoint("CP_Alley", root, new Vector3(11.5f, 0f, 24f), new Vector3(7f, 3f, 2f), 0f);
            Checkpoint("CP_Climax", root, new Vector3(0f, 0f, 12f), new Vector3(12f, 3f, 3f), 0f).climax = true;
            Checkpoint("CP_Tower", root, new Vector3(0f, 0f, 29f), new Vector3(4f, 3f, 2.5f), 0f);

            var shared = Group("Shared", root);
            var past = StateGroup("Past Dressing", root, TimeMask.Past, false);
            var present = StateGroup("Present Dressing", root, TimeMask.Present, false);
            var future = StateGroup("Future Dressing", root, TimeMask.Future, false);
            var pastPresent = StateGroup("Windows Lit", root, TimeMask.Past | TimeMask.Present, false);
            var logic = Group("Logic", root);

            BuildSquare(root, shared, past, present, future, pastPresent, logic);
            BuildTower(root, shared, past, present, future, pastPresent, logic);
            BuildSkyline(shared, future);
            var timer = BuildCrisis(root, shared, logic);
            BuildEvacuation(root, shared, past, present, future, logic);
            BuildHalden(root, shared, logic);
            BuildArcade(root, shared, present, future, logic);
            BuildSurge(root, shared, future, logic);
            BuildDrives(root, shared, past, present, future, logic, timer);
            BuildAudio(root);
            BuildFragments(shared);
        }

        // =====================================================================
        // Square, buildings, terrace, Tram Hall, Market Row fire, back lane, alley blockers
        // =====================================================================

        static void BuildSquare(Transform root, Transform shared, Transform past, Transform present, Transform future, Transform lit, Transform logic)
        {
            var g = Group("Square", shared);
            Floor("SquareFloor", g, new Vector3(0f, 0f, 10f), new Vector2(64f, 72f), "stone_wet");
            Floor("LaneFloor", g, new Vector3(-37f, 0f, 1.5f), new Vector2(10f, 5f), "stone_wet");
            Floor("CourtyardFloor", g, new Vector3(-48f, 0f, 2f), new Vector2(12f, 20f), "stone_wet");
            Floor("EastLaneFloor", g, new Vector3(38f, 0f, 1f), new Vector2(12f, 6f), "stone_wet");
            Floor("BackLaneNS", g, new Vector3(-48f, 0f, -21f), new Vector2(4f, 26f), "stone_wet");
            Floor("BackLaneEW", g, new Vector3(-41f, 0f, -31f), new Vector2(18f, 6f), "stone_wet");
            void Overlay(Transform parent, string mat)
            {
                var o = Group("Surface", parent);
                NoShadow(Box("Square", o, new Vector3(0f, 0.006f, 10f), new Vector3(63.8f, 0.01f, 71.8f), mat, false));
                NoShadow(Box("Lane", o, new Vector3(-37f, 0.006f, 1.5f), new Vector3(10f, 0.01f, 5f), mat, false));
                NoShadow(Box("Courtyard", o, new Vector3(-48f, 0.006f, 2f), new Vector3(12f, 0.01f, 20f), mat, false));
            }
            Overlay(past, "stone_warm");
            Overlay(future, "obsidian");
            NoShadow(Cylinder("CompassRing", g, new Vector3(0f, 0.012f, 2f), 7f, 0.02f, "brass", false));
            NoShadow(Cylinder("CompassInner", g, new Vector3(0f, 0.016f, 2f), 6.4f, 0.02f, "stone_warm", false));
            Label("CompassZero", g, new Vector3(0f, 0.03f, 2f), 0f, "0", 6f, new Color(0.9f, 0.8f, 0.5f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // ---- Perimeter buildings ----
            Facade("West A (Chandlers)", g, lit, future, new Vector3(-37f, 0f, -13.5f), new Vector3(10f, 14f, 25f), Vector3.right, "plaster_warm", "window_warm", 1);
            Facade("West B (Founders' Arcade)", g, lit, future, new Vector3(-37f, 0f, 25f), new Vector3(10f, 16f, 42f), Vector3.right, "brick_warm", "window_warm", 2);
            Facade("East A (Guild House)", g, lit, future, new Vector3(37f, 0f, -14f), new Vector3(10f, 13f, 24f), Vector3.left, "stone_warm", "window_warm", 3);
            Facade("East B (Weavers)", g, lit, future, new Vector3(37f, 0f, 25f), new Vector3(10f, 15f, 42f), Vector3.left, "plaster_warm", "window_warm", 4);
            BuildFoundersHall(g, past, present, future, lit);
            Facade("Clockkeeper's House", g, lit, future, new Vector3(24f, 0f, 36f), new Vector3(16f, 12f, 20f), Vector3.back, "brick_warm", "window_warm", 6);
            Facade("South East (Post House)", g, lit, future, new Vector3(20f, 0f, -32f), new Vector3(24f, 12f, 12f), Vector3.forward, "stone_warm", "window_warm", 8);
            Wall("GapSW", g, new Vector3(-12f, 0f, -26.2f), new Vector3(-8f, 0f, -26.2f), 10f, 0.6f, "stone_warm");
            Wall("AlleyEnd", g, new Vector3(-7f, 0f, 46f), new Vector3(16f, 0f, 46f), 12f, 0.8f, "brick_warm");
            Wall("EastLaneBarricade", g, new Vector3(44f, 0f, -2f), new Vector3(44f, 0f, 4f), 5f, 0.6f, "wood_dark");
            Label("EastLaneSign", g, new Vector3(43.6f, 2.6f, 1f), 90f, "MARKET ROW", 0.8f, new Color(1f, 0.85f, 0.6f));
            // Courtyard enclosure (south door leads to the back lane).
            Wall("CourtW", g, new Vector3(-54f, 0f, -8f), new Vector3(-54f, 0f, 12f), 9f, 0.6f, "brick_warm");
            Wall("CourtN", g, new Vector3(-54f, 0f, 12f), new Vector3(-42f, 0f, 12f), 9f, 0.6f, "brick_warm");
            WallWithDoor("CourtS", g, new Vector3(-54f, 0f, -8f), new Vector3(-42f, 0f, -8f), 9f, 0.6f, "brick_warm", 2.4f, 3f, 0.5f);
            Label("BackLaneSign", g, new Vector3(-48f, 3.6f, -7.65f), 180f, "SERVICE LANE -- TRAM HALL", 0.35f, new Color(1f, 0.85f, 0.6f));

            // ---- Warden's Terrace (safe observation spot) ----
            var t = Group("Warden's Terrace", g);
            Box("TerraceBlock", t, new Vector3(0f, 1.5f, -33f), new Vector3(16f, 3f, 10f), "stone_warm");
            Stairs("TerraceStairs", t, new Vector3(0f, 0f, -23.5f), 180f, 15, 0.2f, 0.3f, 4f, "stone_warm");
            Wall("RailW", t, new Vector3(-8f, 3f, -28.15f), new Vector3(-2f, 3f, -28.15f), 1.05f, 0.25f, "brass");
            Wall("RailE", t, new Vector3(2f, 3f, -28.15f), new Vector3(8f, 3f, -28.15f), 1.05f, 0.25f, "brass");
            Wall("TerraceBack", t, new Vector3(-8f, 0f, -38.2f), new Vector3(8f, 0f, -38.2f), 9f, 0.6f, "stone_warm");
            Wall("TerraceSideW", t, new Vector3(-8f, 3f, -38f), new Vector3(-8f, 3f, -28f), 1.05f, 0.25f, "brass");
            Wall("TerraceSideE", t, new Vector3(8f, 3f, -38f), new Vector3(8f, 3f, -28f), 1.05f, 0.25f, "brass");
            Label("TerracePlaque", t, new Vector3(0f, 4.6f, -37.8f), 180f, "WARDEN'S TERRACE", 0.9f, new Color(1f, 0.85f, 0.6f));
            Use("Spyglass", t, new Vector3(-4f, 3.6f, -28.7f), new Vector3(0.4f, 0.6f, 0.4f), "brass", "Look through the spyglass", "L07.observed", TimeMask.All, PrimitiveType.Cylinder)
                .lines.AddRange(new[]
                {
                    L("Arin", "East: Market Row is burning. In the PAST that lane is just stalls and bunting."),
                    L("Arin", "West: the arcade lane, that wall is leaning hard. And behind the tower -- people, behind the rubble in Keeper's Alley."),
                    L("Arin", "South-west, the Tram Hall. A man in Founders' robes is standing in its gate."),
                    L("Arin", "And the tower... in the FUTURE something at the top is still transmitting."),
                });

            // ---- Tram Hall (safe zone inside) ----
            var th = Group("Tram Hall", g);
            const float hx0 = -32f, hx1 = -12f, hz0 = -38f, hz1 = -26f;
            WallWithDoor("HallFront", th, new Vector3(hx0, 0f, hz1), new Vector3(hx1, 0f, hz1), 12f, 0.6f, "plaster_warm", 4f, 3.6f, 0.5f);
            WallWithDoor("HallWest", th, new Vector3(hx0, 0f, hz0), new Vector3(hx0, 0f, hz1), 12f, 0.6f, "plaster_warm", 2.4f, 3f, 0.583f);
            Wall("HallBack", th, new Vector3(hx0, 0f, hz0), new Vector3(hx1, 0f, hz0), 12f, 0.6f, "plaster_warm");
            Wall("HallEast", th, new Vector3(hx1, 0f, hz0), new Vector3(hx1, 0f, hz1), 12f, 0.6f, "plaster_warm");
            Box("HallRoof", th, new Vector3(-22f, 12.2f, -32f), new Vector3(20.6f, 0.4f, 12.6f), "stone_warm");
            Box("HallInnerCeiling", th, new Vector3(-22f, 6.2f, -32f), new Vector3(20f, 0.3f, 12f), "wood_dark");
            Box("Platform", th, new Vector3(-22f, 0.5f, -36.6f), new Vector3(18f, 1f, 2.6f), "concrete");
            Stairs("TramStairs", th, new Vector3(-22f, 0f, -33.8f), 180f, 5, 0.2f, 0.3f, 6f, "concrete");
            Box("TramCar", th, new Vector3(-22f, 2.6f, -37.1f), new Vector3(12f, 3f, 1.4f), "metal_dark", false);
            NoShadow(Box("TramWindows", th, new Vector3(-22f, 3.1f, -36.38f), new Vector3(11f, 0.8f, 0.04f), "window_warm", false));
            Label("TramCarSign", th, new Vector3(-22f, 4.4f, -36.35f), 0f, "TRAM 0 -- LAST CAR", 0.5f, new Color(1f, 0.85f, 0.5f));
            NoShadow(Cylinder("SafeCircle", th, SafePos + Vector3.up * 0.014f, 3.5f, 0.02f, "neon_amber", false));
            Label("SafeFloorText", th, SafePos + new Vector3(0f, 0.04f, 2.2f), 0f, "GATHER HERE", 1.6f, new Color(1f, 0.8f, 0.4f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            PointLight("SafeLight", th, SafePos + Vector3.up * 4.5f, new Color(1f, 0.75f, 0.4f), 3f, 14f);
            Label("HallSign", th, new Vector3(-22f, 4.6f, hz1 - 0.35f), 0f, "TRAM HALL", 0.9f, new Color(1f, 0.85f, 0.6f));
            var wgT = Group("TramHall Windows", lit);
            for (int c = 0; c < 6; c++)
                if (c != 2 && c != 3) NoShadow(Box("Window", wgT, new Vector3(hx0 + 2f + c * 3.2f, 8.4f, hz1 - 0.34f), new Vector3(1.1f, 1.7f, 0.08f), "window_warm", false));
            // Front gate (Halden) and service door (back lane).
            var gate = Door("TramGate", th, new Vector3(-22f, 0f, hz1), new Vector3(4f, 3.6f, 0.3f), "iron_rust", new Vector3(0f, 3.7f, 0f), 0f, F_HYield);
            gate.toast = "Founder Halden steps aside. The Tram Hall gate rises.";
            var service = Door("ServiceDoor", th, new Vector3(hx0, 0f, -31f), new Vector3(2.4f, 3f, 0.3f), "wood_dark", new Vector3(0f, 3.1f, 0f), 90f, F_BackDoor);
            service.toast = "The service door swings up. The Tram Hall is open from the back.";
            var bar = Use("ServiceBar", th, new Vector3(hx0 - 0.55f, 1.3f, -29.4f), new Vector3(0.4f, 0.5f, 0.6f), "iron_rust", "Lift the service door bar", F_BackDoor);
            bar.cue = Cue.Clang;
            bar.lines.Add(L("Arin", "Bar's up. The back way into the Tram Hall is open -- now bring them through the lane."));
            var r1 = Reactor("TramRouteFront", logic, F_HYield); r1.setFlagWhenTrue = F_TramRoute; r1.reversible = false;
            var r2 = Reactor("TramRouteBack", logic, F_BackDoor); r2.setFlagWhenTrue = F_TramRoute; r2.reversible = false;

            // ---- Market Row: fire in the PRESENT (route through the PAST) ----
            var fire = Group("Market Row Fire", present);
            Box("BurningWreck", fire, new Vector3(33.4f, 1.3f, 1f), new Vector3(1.4f, 2.6f, 6f), "iron_rust");
            Tilted("CharredCanopy", fire, new Vector3(34.2f, 2.5f, -0.5f), new Vector3(2.6f, 0.1f, 2f), "ruin_dark", new Vector3(18f, 30f, 10f), false);
            for (int i = 0; i < 3; i++)
            {
                var f = Particles("Flames", fire, new Vector3(33.4f, 0.4f, -1.2f + i * 2.2f), new Vector3(1.2f, 0.2f, 1.2f), "fire");
                var fm = f.main; fm.playOnAwake = true;
            }
            Particles("Smoke", fire, new Vector3(33.6f, 3f, 1f), new Vector3(2f, 0.4f, 5f), "smoke");
            var fireLight = PointLight("FireGlow", fire, new Vector3(32.5f, 1.6f, 1f), new Color(1f, 0.5f, 0.2f), 5f, 10f);
            var flick = fire.gameObject.AddComponent<NeonFlicker>(); flick.targetLight = fireLight; flick.flickerChance = 0.2f; flick.hum = 0.25f;
            var pastLane = Group("Market Row (Past)", past);
            NoShadow(Box("LaneBunting", pastLane, new Vector3(38f, 4f, 1f), new Vector3(10f, 0.05f, 0.05f), "fabric_red", false));
            Label("PastLaneSign", pastLane, new Vector3(32.6f, 3.3f, 1f), -90f, "MARKET ROW -- FESTIVAL CLOTH", 0.35f, new Color(0.3f, 0.15f, 0.05f));
            var futLane = Group("Market Row (Future)", future);
            Tilted("CrystalWall", futLane, new Vector3(33.4f, 1.6f, 1f), new Vector3(1.2f, 3.2f, 6f), "crystal", new Vector3(0f, 0f, 6f));

            // ---- The Keepers' back lane (alternate route to the Tram Hall) ----
            var bl = Group("Back Lane", g);
            Wall("BLWest", bl, new Vector3(-50f, 0f, -34f), new Vector3(-50f, 0f, -8f), 8f, 0.5f, "brick_warm");
            Wall("BLEast", bl, new Vector3(-46f, 0f, -28f), new Vector3(-46f, 0f, -8f), 8f, 0.5f, "brick_warm");
            Wall("BLSouth", bl, new Vector3(-50f, 0f, -34f), new Vector3(-32f, 0f, -34f), 8f, 0.5f, "brick_warm");
            Wall("BLNorth", bl, new Vector3(-46f, 0f, -28f), new Vector3(-32f, 0f, -28f), 8f, 0.5f, "brick_warm");
            Box("CrateCover1", bl, new Vector3(-49.2f, 0.6f, -21f), new Vector3(1.2f, 1.2f, 1.6f), "wood");
            Box("CrateCover2", bl, new Vector3(-46.9f, 0.6f, -14.5f), new Vector3(1.2f, 1.2f, 1.6f), "wood");
            Box("CrateCover3", bl, new Vector3(-43f, 0.6f, -33.1f), new Vector3(1.6f, 1.2f, 1.2f), "wood");
            Box("CrateCover4", bl, new Vector3(-37f, 0.6f, -28.9f), new Vector3(1.6f, 1.2f, 1.2f), "wood");
            var blPast = StateGroup("Back Lane (Past)", root, TimeMask.Past, true);
            Box("LaundryCart", blPast, new Vector3(-48f, 0.8f, -18.5f), new Vector3(3.8f, 1.6f, 1.2f), "wood");
            Box("CoalCart", blPast, new Vector3(-40.5f, 0.8f, -31f), new Vector3(1.2f, 1.6f, 5.8f), "wood_dark");
            NoShadow(Box("Washing", blPast, new Vector3(-48f, 3.2f, -24f), new Vector3(3.5f, 0.9f, 0.03f), "paper", false));
            Label("CartTag", blPast, new Vector3(-48f, 1.9f, -17.85f), 180f, "LAUNDRY -- FESTIVAL LINEN", 0.25f, new Color(0.3f, 0.15f, 0.05f));
            var k1 = Keeper("Lantern-Bearer", bl, new Vector3(-48f, 0f, -13f), 180f, KeeperPatrol.Mode.Hunter, TimeMask.Present, new Vector3(-48f, 0f, -13f), new Vector3(-48f, 0f, -26f));
            k1.viewDistance = 10f; k1.caughtMessage = "A Keeper's lantern found you. In the PAST the Keepers don't walk this lane -- but the carts do block it.";
            k1.spotBarks = new List<string> { "Someone in the lane?", "Anomaly scent..." };
            var k2 = Keeper("Gate-Keeper", bl, new Vector3(-36f, 0f, -31f), -90f, KeeperPatrol.Mode.Hunter, TimeMask.Present, new Vector3(-36f, 0f, -31f), new Vector3(-47.5f, 0f, -31f));
            k2.viewDistance = 10f; k2.caughtMessage = "The Gate-Keeper caught you. Wait behind the crates for the lantern to turn away.";
            k2.spotBarks = new List<string> { "Mother Ondine said one would come this way.", "Who goes there?" };
            Particles("LaneRain", present, new Vector3(-45f, 10f, -24f), new Vector3(12f, 1f, 22f), "rain");

            // ---- Keeper's Alley blockers (open only in the FUTURE) ----
            var ap = Group("Alley Rubble (Present)", present);
            Box("AlleyRubble", ap, new Vector3(11.5f, 1.8f, 29.5f), new Vector3(9f, 3.6f, 1.6f), "stone_wet");
            Tilted("AlleyBeam", ap, new Vector3(11f, 3.2f, 29.2f), new Vector3(0.3f, 0.3f, 8f), "wood_dark", new Vector3(0f, 80f, 18f), false);
            Label("AlleyRubbleSign", ap, new Vector3(11.5f, 2.6f, 28.6f), 0f, "VOICES BEHIND THE RUBBLE", 0.35f, new Color(1f, 0.6f, 0.4f));
            var apast = Group("Alley Gate (Past)", past);
            Wall("KeeperYardGate", apast, new Vector3(7f, 0f, 29.5f), new Vector3(16f, 0f, 29.5f), 3.4f, 0.3f, "iron_rust");
            Label("YardGateSign", apast, new Vector3(11.5f, 2.8f, 29.3f), 0f, "KEEPER'S YARD -- LOCKED", 0.35f, new Color(1f, 0.9f, 0.6f));

            ReflectionProbe(g, new Vector3(0f, 8f, 8f), new Vector3(70f, 24f, 80f));

            // ---- PAST: festival day ----
            var pf = Group("Festival", past);
            Bunting(pf, new Vector3(-31.5f, 9f, -18f), new Vector3(31.5f, 9f, -18f), 18);
            Bunting(pf, new Vector3(-31.5f, 9.5f, 6f), new Vector3(31.5f, 9.5f, 6f), 18);
            string[] bannerMats = { "fabric_red", "fabric_gold", "fabric_teal" };
            for (int i = 0; i < 3; i++)
            {
                NoShadow(Box("BannerW", pf, new Vector3(-31.8f, 7f, -16f + i * 12f), new Vector3(0.05f, 6f, 1.6f), bannerMats[i % 3], false));
                NoShadow(Box("BannerE", pf, new Vector3(31.8f, 7f, -16f + i * 12f), new Vector3(0.05f, 6f, 1.6f), bannerMats[(i + 1) % 3], false));
            }
            Lamp("GasLamp", pf, new Vector3(-24f, 0f, -8f), "gas", 4.2f);
            Lamp("GasLamp", pf, new Vector3(24f, 0f, -8f), "gas", 4.2f);
            Lamp("GasLamp", pf, new Vector3(0f, 0f, 8f), "gas", 4.2f);
            for (int i = 0; i < 3; i++)
                Stall(pf, new Vector3(-20f + i * 4.5f, 0f, -12f), bannerMats[i % 3], i);
            Label("FestivalSign", pf, new Vector3(0f, 7.5f, 25.5f), 0f, "FOUNDERS' FESTIVAL\nTHE BELL RINGS ONCE FOR PLENTY", 0.9f, new Color(1f, 0.85f, 0.55f));
            Particles("FestivalDust", pf, new Vector3(0f, 4f, 0f), new Vector3(60f, 8f, 50f), "dust");
            Crowd("Festival Crowd", pf, new Vector3(-6f, 0f, -6f), new Vector2(30f, 16f), 6, 701,
                new[] { "Did you hear that? Like a bell under the bell.", "Mind the scaffolding, love.", "Plenty! Plenty for the founders!" }, "fabric_gold");
            PointLight("PastFill", pf, new Vector3(0f, 10f, 0f), new Color(1f, 0.8f, 0.55f), 2.5f, 40f);

            // ---- PRESENT: rain, puddles, sodium lamps, the crisis ----
            var pr = Group("Rain", present);
            Particles("Rain", pr, new Vector3(0f, 18f, 6f), new Vector3(80f, 2f, 80f), "rain");
            Particles("RainMist", pr, new Vector3(0f, 1.5f, 6f), new Vector3(60f, 3f, 60f), "mist");
            var rng = new System.Random(77);
            for (int i = 0; i < 6; i++)
                NoShadow(Box("Puddle", pr, new Vector3(((float)rng.NextDouble() - 0.5f) * 56f, 0.012f, ((float)rng.NextDouble() - 0.5f) * 44f), new Vector3(2f + (float)rng.NextDouble() * 3f, 0.01f, 1.5f + (float)rng.NextDouble() * 2.5f), "water", false));
            Lamp("SodiumLamp", pr, new Vector3(-26f, 0f, -10f), "sodium", 4.6f);
            Lamp("SodiumLamp", pr, new Vector3(26f, 0f, -10f), "sodium", 4.6f);
            Lamp("SodiumLamp", pr, new Vector3(-14f, 0f, 14f), "sodium", 4.6f);
            for (int i = 0; i < 3; i++)
                Stall(pr, new Vector3(-20f + i * 4.5f, 0f, -12f), "iron_rust", 10 + i, true);
            NoShadow(Box("EvacBanner", pr, new Vector3(0f, 6f, 25.5f), new Vector3(12f, 1.4f, 0.1f), "warning_red", false));
            Label("EvacText", pr, new Vector3(0f, 6f, 25.4f), 0f, "EVACUATE -- TRAM HALL SOUTH-WEST", 0.75f, Color.white);

            // ---- FUTURE: split-open night ----
            var fu = Group("Ruins", future);
            Particles("Motes", fu, new Vector3(0f, 6f, 8f), new Vector3(70f, 12f, 70f), "motes");
            Lamp("NeonPylon", fu, new Vector3(-25f, 0f, -8f), "neon", 5f);
            Lamp("NeonPylon", fu, new Vector3(25f, 0f, -8f), "neon", 5f);
            Lamp("NeonPylon", fu, new Vector3(14f, 0f, 14f), "neon", 5f);
            var frng = new System.Random(91);
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new Vector3(((float)frng.NextDouble() - 0.5f) * 54f, 3f + (float)frng.NextDouble() * 9f, -18f + (float)frng.NextDouble() * 40f);
                if (Mathf.Abs(p.x) < 9f && p.z > 10f) p.x += 14f;
                var deb = Tilted("FloatingDebris", fu, p, new Vector3(0.6f + (float)frng.NextDouble() * 1.6f, 0.4f + (float)frng.NextDouble() * 0.8f, 0.6f + (float)frng.NextDouble() * 1.4f), i % 3 == 0 ? "crystal" : "ruin_dark", new Vector3(frng.Next(360), frng.Next(360), frng.Next(360)), false);
                var fb = deb.AddComponent<FloatBob>(); fb.amplitude = 0.3f; fb.frequency = 0.15f + (float)frng.NextDouble() * 0.2f; fb.drift = new Vector3(4f, 10f, 2f);
            }
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = new Vector3(((float)frng.NextDouble() - 0.5f) * 50f, 0f, -20f + (float)frng.NextDouble() * 30f);
                if (Mathf.Abs(p.x) < 8f) p.x += 12f;
                Tilted("Crystal", fu, p + Vector3.up * 0.8f, new Vector3(0.5f, 1.8f + (float)frng.NextDouble() * 1.5f, 0.5f), "crystal", new Vector3(frng.Next(-25, 25), frng.Next(360), frng.Next(-25, 25)), false);
            }
            Hologram("AdPanel", fu, new Vector3(-31f, 8f, -4f), new Vector3(0.1f, 3f, 5f), PrimitiveType.Cube, "hologram_magenta", true, false);
            Label("AdText", fu, new Vector3(-30.8f, 8f, -4f), -90f, "VEYRA\nREMEMBERS", 1.1f, new Color(1f, 0.6f, 0.95f));
            Hologram("AdPanel2", fu, new Vector3(31f, 9f, -8f), new Vector3(0.1f, 3f, 5f), PrimitiveType.Cube, "hologram", true, false);
            Label("AdText2", fu, new Vector3(30.8f, 9f, -8f), 90f, "TRAM 0\nNO SERVICE", 1.1f, new Color(0.6f, 0.95f, 1f));
            var halo = Hologram("SquareHalo", fu, new Vector3(0f, 0.2f, 2f), new Vector3(14f, 0.02f, 14f), PrimitiveType.Cylinder, "hologram", false, true);
            halo.GetComponent<Spinner>().degreesPerSecond = new Vector3(0f, 8f, 0f);
            PointLight("FutureFill", fu, new Vector3(0f, 9f, 0f), new Color(0.6f, 0.5f, 1f), 3f, 40f);
            Hologram("TramHolo", fu, new Vector3(-22f, 4.6f, -25.5f), new Vector3(4f, 1f, 0.05f), PrimitiveType.Cube, "hologram_amber", true, false);
        }

        static void Stall(Transform parent, Vector3 ground, string canopyMat, int seed, bool wrecked = false)
        {
            var s = Group("Stall", parent);
            Box("Counter", s, ground + new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 1.2f), "wood");
            for (int i = 0; i < 2; i++)
                Box("Post", s, ground + new Vector3(i == 0 ? -1.4f : 1.4f, 1.3f, 0.5f), new Vector3(0.12f, 2.6f, 0.12f), "wood_dark", false);
            var c = Box("Canopy", s, ground + new Vector3(0f, 2.6f, 0.1f), new Vector3(3.4f, 0.08f, 1.8f), canopyMat, false);
            c.transform.rotation = Quaternion.Euler(wrecked ? 14f : -8f, 0f, wrecked ? (seed % 2 == 0 ? 12f : -9f) : 0f);
            Box("Goods", s, ground + new Vector3(-0.6f, 1.15f, 0f), new Vector3(0.6f, 0.3f, 0.5f), wrecked ? "iron_rust" : "fabric_gold", false);
        }

        // =====================================================================
        // Skyline
        // =====================================================================

        static void BuildSkyline(Transform shared, Transform future)
        {
            var sky = Group("Skyline", shared);
            var rng = new System.Random(707);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f + 0.2f;
                float r = 75f + (float)rng.NextDouble() * 20f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, 10f + Mathf.Sin(a) * r);
                SkyTower("Roofline" + i, sky, p, new Vector3(16f + (float)rng.NextDouble() * 10f, 14f + (float)rng.NextDouble() * 12f, 12f), i % 2 == 0 ? "brick_warm" : "plaster_cold", "window_warm", 700 + i);
            }
            var fs = Group("Future Spires", future);
            string[] signs = { "AFTER/ZERO", null, "VEYRA 0", null, "THE BELL IS LISTENING", null };
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f + 0.5f;
                float r = 105f + (float)rng.NextDouble() * 30f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, 10f + Mathf.Sin(a) * r);
                SkyTower("Spire" + i, fs, p, new Vector3(14f + (float)rng.NextDouble() * 8f, 50f + (float)rng.NextDouble() * 50f, 14f), "ruin_dark", i % 2 == 0 ? "neon_cyan" : "neon_magenta", 800 + i, true, signs[i], i % 2 == 0 ? "neon_magenta" : "neon_violet");
            }
        }

        // =====================================================================
        // Audio
        // =====================================================================

        static void BuildAudio(Transform root)
        {
            var a = Group("Ambience", root);
            Ambient("Rain", a, new Vector3(0f, 4f, 0f), AmbientKind.Rain, 0.45f, TimeMask.Present, 0f);
            Ambient("FestivalCrowd", a, new Vector3(-8f, 2f, -8f), AmbientKind.Crowd, 0.4f, TimeMask.Past, 1f, 40f);
            Ambient("ShelterCrowd", a, SafePos + Vector3.up, AmbientKind.Crowd, 0.35f, TimeMask.Present, 1f, 16f);
            Ambient("MarketRowFire", a, new Vector3(36f, 1.5f, 1f), AmbientKind.Machine, 0.35f, TimeMask.Present, 1f, 14f).pitch = 0.6f;
            Ambient("TowerMachine", a, new Vector3(0f, 6f, 34f), AmbientKind.Machine, 0.5f, TimeMask.Past | TimeMask.Present, 1f, 30f);
            Ambient("FutureDrone", a, new Vector3(0f, 6f, 10f), AmbientKind.FutureDrone, 0.4f, TimeMask.Future, 0f);
            Ambient("TerraceWind", a, new Vector3(0f, 4f, -33f), AmbientKind.Wind, 0.35f, TimeMask.All, 1f, 16f);
            Ambient("Gutters", a, new Vector3(-37f, 1f, 1.5f), AmbientKind.Water, 0.3f, TimeMask.Present, 1f, 14f);
            Ambient("SurgeHum", a, new Vector3(11.5f, 2f, 34f), AmbientKind.Machine, 0.45f, TimeMask.Future, 1f, 16f).pitch = 1.6f;
            Ambient("BackLaneRain", a, new Vector3(-46f, 2f, -24f), AmbientKind.Water, 0.25f, TimeMask.Present, 1f, 12f);
        }
    }
}
