using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 02 v2: the PAST gear stairs (combine phase, under the refill timer) and Brother Calder's climax:
    /// power cut + sealed bell stairs -> restart the PAST clockwork -> PAST counterweight lift -> FUTURE gust beam
    /// -> draw Calder's bolt in the PAST from inside the keeper's chamber -> recording on the bell floor.
    /// </summary>
    public static partial class Level02Builder
    {
        // =================================================================
        // PAST gear stairs: mid platform (y 2.6) -> gallery (y 7)
        // =================================================================

        static void BuildGearStairs()
        {
            var g = Group("GearStairs_Past", pastPlay);
            // 4 m apart, 0.8 m higher each, alternating direction, slow (9 deg/s). Teeth are 1.3 m wide.
            Vector3[] centers =
            {
                new Vector3(4.9f, 3.2f, 9.8f),
                new Vector3(2.07f, 4.0f, 6.97f),
                new Vector3(4.9f, 4.8f, 4.14f),
                new Vector3(2.07f, 5.6f, 1.31f),
                new Vector3(4.9f, 6.4f, -1.52f),
            };
            for (int i = 0; i < centers.Length; i++)
                RideGear("StairGear" + i, g, centers[i], 1.6f, i % 2 == 0 ? 9f : -9f);
            Label("GearStairsSign", g, new Vector3(4.9f, 4.6f, 11.9f), 0f, "MIND THE TEETH - RIDE, DON'T RUN", 0.2f, new Color(1f, 0.9f, 0.6f));
            PointLight("GearStairsLight", g, new Vector3(3.5f, 7f, 4f), Warm, 2.6f, 10f);
        }

        static Transform RideGear(string name, Transform parent, Vector3 top, float radius, float speed)
        {
            var g = Group(name, parent);
            g.position = top;
            var disc = Prim(PrimitiveType.Cylinder, "Disc", g, top + Vector3.down * 0.2f, new Vector3(radius * 2f, 0.2f, radius * 2f), "brass", true, false);
            Prim(PrimitiveType.Cylinder, "Hub", g, top + Vector3.up * 0.02f, new Vector3(radius * 0.7f, 0.03f, radius * 0.7f), "metal_dark", false, false);
            int teeth = 6;
            for (int i = 0; i < teeth; i++)
            {
                float a = i * Mathf.PI * 2f / teeth;
                Vector3 local = new Vector3(Mathf.Cos(a) * (radius + 0.2f), -0.2f, Mathf.Sin(a) * (radius + 0.2f));
                var t = Prim(PrimitiveType.Cube, "Tooth", g, top + local, new Vector3(0.5f, 0.4f, 1.3f), "brass", true, false);
                t.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            // Axle down to the pit (visual only).
            Prim(PrimitiveType.Cylinder, "Axle", parent, new Vector3(top.x, (top.y - 2.9f) * 0.5f, top.z), new Vector3(0.3f, (top.y + 2.1f) * 0.5f, 0.3f), "metal_dark", false, false);
            var sp = g.gameObject.AddComponent<Spinner>();
            sp.degreesPerSecond = new Vector3(0f, speed, 0f);
            sp.startWhenFlag = F_Wedge;
            Ride(g.gameObject);
            _ = disc;
            return g;
        }

        // =================================================================
        // Climax: Brother Calder
        // =================================================================

        static void BuildClimax()
        {
            // --- Brother Calder (Keeper enforcer) on the bell floor ---
            var calder = Npc("Brother Calder", shared, new Vector3(-6f, 13f, -10f), 90f, "coat_grey", true, 31);
            Prim(PrimitiveType.Cylinder, "Hood", calder.body, new Vector3(-6f, 13f + 1.72f, -10f), new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            Route(calder, false,
                (new Vector3(-6f, 13f, -10f), 5f, NPCActivity.Work, "The hour holds."),
                (new Vector3(-9.5f, 13f, -6f), 4f, NPCActivity.Look, ""),
                (new Vector3(-4f, 13f, -7f), 4f, NPCActivity.Idle, "Nothing turns while I keep this floor."));
            Route(calder, true,
                (new Vector3(-7.5f, 13f, -5f), 3f, NPCActivity.Look, "Catch me in the Archive, anomaly."),
                (new Vector3(-7.5f, 13f, -4.6f), 60f, NPCActivity.Idle, ""),
                (new Vector3(-8.5f, 13f, -6f), 5f, NPCActivity.Idle, ""));
            Persona(calder, "Keeper enforcer: guards the bell floor so the clockwork never runs and the night never ends.", F_CalderCut,
                new[] { "Back down the stairs.", "The Keepers see you.", "One more step and the lights go out." },
                new[]
                {
                    L("Calder", "Far enough, anomaly."),
                    L("Calder", "The Keepers of the Hour keep this tower. Inside the hour, nobody dies for good."),
                    L("Calder", "Mother Ondine says you are a crack in the glass. Cracks spread."),
                    L("Calder", "Ilse Marrow stopped this clock once. We have kept it stopped ever since. You're welcome."),
                    L("Calder", "Go home. Oh - you don't have one. None of you lot do."),
                },
                new[]
                {
                    WhenHolding("nameplate",
                        L("Calder", "Her nameplate. We took that down for a reason. Put it back in the dark where it belongs.")),
                    WhenFlag("clue.L02_log3",
                        L("Calder", "You've been reading her logbook. 'The wedge is mine.' Brave words for a woman who drowned the pit.", 4.5f),
                        L("Calder", "She bought time. We keep it. Same work, Arin.")),
                },
                new[]
                {
                    L("Calder", "Run along the gears, then. The hour is patient."),
                    L("Calder", "Next time it won't be a power lever. Next time it will be the lift cable."),
                });
            var calderGone = Reactor("React_CalderEscapes", logic, F_Beam);
            calderGone.reversible = false;
            calderGone.disableWhenTrue.Add(calder.gameObject);
            calderGone.toast = "Brother Calder is gone. Somewhere below, a Keeper's lantern goes out.";

            // --- The ambush at the top of the bell stairs ---
            Zone("Zone_CalderAmbush", logic, new Vector3(0.8f, 13f, -10.5f), new Vector3(2.6f, 3.4f, 2.5f), F_BellFloor);
            var host = Group("Dialogue_Calder", logic);
            host.position = new Vector3(-3f, 14f, -10.5f);
            var dlg = Dialogue(host.gameObject, "Brother Calder", "Brother Calder bars the bell floor. What do you say?", F_BellFloor);
            dlg.opening.Add(L("Calder", "Far enough, anomaly. Sister Vossel said you'd climb the tower after the street.", 4f));
            dlg.opening.Add(L("Calder", "Do you know what the Keepers keep? The hour. Inside it nobody dies for good. Not the children, not the old, not even you.", 5f));
            dlg.opening.Add(L("Calder", "Ilse Marrow stopped this clock once. We have kept it stopped ever since.", 3.5f));
            var o1 = Option("Let me through. I only want the truth.", "L02.calderTruth",
                L("Calder", "Truth is a flood, Arin. Ask Bram Kettle about floods."),
                L("Calder", "There. The power's cut, and the bell floor is sealed. Your PAST gears can rot.", 4f));
            var o2 = Option("Sister Vossel. You Keepers were watching me on the festival street.", "L02.calderVossel",
                L("Calder", "Vossel watches. I lock. Mother Ondine decides."),
                L("Calder", "And right now I'm locking you out. Enjoy the dark.", 3.5f));
            o2.requiresFlags.Add("L01.vosselScene");
            o2.missingText = "(you never faced the Keepers on the street)";
            var o3 = Option("Ilse stopped it to save the children - not to keep you safe.", "L02.calderIlse",
                L("Calder", "...She knew what she was doing."),
                L("Calder", "So do I. Lights out, anomaly.", 3f));
            o3.requiresFlags.Add("clue.L02_log3");
            o3.missingText = "(you have not read her last logbook page)";
            dlg.options.Add(o1); dlg.options.Add(o2); dlg.options.Add(o3);
            dlg.doneFlag = F_CalderCut;

            // Calder's seal on the bell stairs: drops when he cuts the power, lifts when his bolt is drawn.
            var gatePanel = Box("CaldersGate", shared, new Vector3(-2.25f, 13f + 1.3f + 2.8f, -10.6f), new Vector3(0.3f, 2.6f, 2.9f), "iron_rust");
            Unstatic(gatePanel.transform);
            var gate = Reactor("React_CaldersGate", logic, F_CalderCut);
            gate.whenNone.Add(F_GateUnlocked);
            gate.moveTarget = gatePanel.transform;
            gate.moveBy = new Vector3(0f, -2.8f, 0f);
            gate.duration = 1.2f;
            gate.announce = true;
            gate.cue = Cue.Clang;
            var gateSign = Label("CaldersGateSign", shared, new Vector3(-2.05f, 15.9f, -10.6f), 90f, "SEALED BY THE KEEPERS", 0.22f, new Color(1f, 0.6f, 0.4f));
            gate.enableWhenTrue.Add(gateSign);

            // Power relay: cut on F_CalderCut, restored by the PAST lever.
            var relay = Group("PowerRelay", logic).gameObject.AddComponent<L02PowerRelay>();
            relay.cutFlag = F_CalderCut;
            relay.restartFlag = F_Lever;
            relay.powerOutFlag = F_PowerOut;
            relay.restartedFlag = F_Restarted;

            // --- Restart the PAST clockwork: engagement lever at the gallery's north end ---
            var leverHolder = Group("ClockworkLever_Past", pastPlay);
            Box("LeverBase", leverHolder, new Vector3(11.5f, 7.25f, 7.8f), new Vector3(0.7f, 0.5f, 0.7f), "metal_dark");
            var lever = Use("ClockworkLever", leverHolder, new Vector3(11.5f, 8.0f, 7.8f), new Vector3(0.45f, 1.2f, 0.45f), "brass", "Engage the clockwork", F_Lever, TimeMask.Past);
            lever.requiredFlags.Add(F_CalderCut);
            lever.lockedMessage = "The clockwork is running. No need to touch the engagement lever.";
            lever.animateTarget = lever.transform;
            lever.rotateBy = new Vector3(55f, 0f, 0f);
            lever.cue = Cue.Clang;
            Label("LeverTag", leverHolder, new Vector3(11.5f, 9.1f, 7.8f), 90f, "ENGAGE CLOCKWORK", 0.22f, new Color(1f, 0.9f, 0.6f));
            Label("LeverTagNow", presLook, new Vector3(11.5f, 9.1f, 7.8f), 90f, "LEVER (PAST ONLY)", 0.2f, new Color(1f, 0.85f, 0.6f));
            Checkpoint("CP_Lever", logic, new Vector3(9.5f, 7f, 6f), new Vector3(4f, 3f, 3f), 0f);

            // --- PAST counterweight lift: gallery (7) <-> escapement ledge (13) ---
            var lift = Prim(PrimitiveType.Cube, "CounterweightLift", pastPlay, new Vector3(9.5f, 6.85f, 10.3f), new Vector3(4f, 0.3f, 3.4f), "brass", true, false);
            var lm = lift.AddComponent<L02Lift>();
            lm.rise = 6f; lm.speed = 0.9f; lm.pause = 3.5f;
            lm.runFlag = F_Restarted; lm.stopFlag = F_PowerOut;
            Ride(lift);
            foreach (var x in new[] { 7.7f, 11.3f })
                Deco("LiftChain", pastLook, new Vector3(x, 19f, 10.3f), new Vector3(0.08f, 22f, 0.08f), "metal_dark");
            Label("LiftSign", shared, new Vector3(9.5f, 9.4f, 11.9f), 180f, "COUNTERWEIGHT LIFT", 0.25f, Color.white);
            Label("LiftSignNote", presLook, new Vector3(9.5f, 8.9f, 11.9f), 180f, "(the lift exists only in the PAST)", 0.16f, new Color(1f, 0.85f, 0.6f));

            // --- FUTURE gust beam: escapement ledge -> keeper's chamber east door ---
            var fb = Group("GustBeam_Future", futPlay);
            Beam("GustBeam", fb, new Vector3(7.05f, 13f, 6.5f), new Vector3(-3.1f, 13f, 6.5f), 0.8f, "obsidian");
            Box("GustTrimN", fb, new Vector3(1.97f, 13.01f, 6.92f), new Vector3(10.15f, 0.05f, 0.05f), "neon_cyan", false);
            Box("GustTrimS", fb, new Vector3(1.97f, 13.01f, 6.08f), new Vector3(10.15f, 0.05f, 0.05f), "neon_cyan", false);
            Box("Shelter", fb, new Vector3(2f, 12.75f, 6.5f), new Vector3(2.2f, 0.5f, 2.2f), "obsidian");
            Box("Windbreak", fb, new Vector3(2f, 14.2f, 5.3f), new Vector3(2.2f, 2.4f, 0.3f), "ruin_dark");
            Label("ShelterTag", fb, new Vector3(2f, 15.6f, 5.4f), 180f, "SHELTER", 0.3f, new Color(0.6f, 1f, 1f));
            PointLight("ShelterLight", fb, new Vector3(2f, 14.8f, 6.6f), Cyan, 2.2f, 7f);
            Ambient("Amb_GustHowl", shared, new Vector3(2f, 14f, 6.5f), AmbientKind.Wind, 0.6f, TimeMask.Future, 1f, 18f);
            Gust("GustEast", new Vector3(5.07f, 14.3f, 6.5f), new Vector3(3.95f, 2.8f, 1.8f), 0f);
            Gust("GustWest", new Vector3(-1.1f, 14.3f, 6.5f), new Vector3(4.0f, 2.8f, 1.8f), 1.6f);
            KillPlane(logic, new Vector3(1.9f, 10f, 6.5f), new Vector3(9.9f, 1f, 3.5f), "You fell into the shaft. Cross the beam only in the FUTURE, between gusts.");
            var cpc = Checkpoint("CP_Climax", logic, new Vector3(10.3f, 13f, 6.5f), new Vector3(3f, 3f, 3f), -90f);
            cpc.climax = true;

            // --- Calder's bolt winch inside the keeper's chamber (intact only in the PAST) ---
            var winchHolder = Group("BoltWinch_Past", pastPlay);
            var winch = Use("BoltWinch", winchHolder, new Vector3(-4.3f, 13.6f, 10.6f), new Vector3(0.7f, 1.2f, 0.7f), "brass", "Draw Calder's bolt", F_GateUnlocked, TimeMask.Past, PrimitiveType.Cylinder);
            winch.requiredFlags.Add(F_CalderCut);
            winch.lockedMessage = "The bolt is already drawn.";
            winch.cue = Cue.Scrape;
            winch.lines.Add(L("Arin", "Ilse's own bolt. Calder only borrowed it."));
            Label("WinchTag", winchHolder, new Vector3(-4.3f, 14.7f, 10.0f), 0f, "CALDER'S BOLT - WINCH", 0.18f, new Color(1f, 0.9f, 0.6f));
            Deco("SeizedWinch", presLook, new Vector3(-4.3f, 13.5f, 10.6f), new Vector3(0.7f, 1f, 0.7f), "iron_rust");
            Deco("SeizedWinch", futLook, new Vector3(-4.3f, 13.5f, 10.6f), new Vector3(0.7f, 1f, 0.7f), "ruin_dark");
            Label("WinchSeized", presLook, new Vector3(-4.3f, 14.4f, 10.0f), 0f, "BOLT WINCH - SEIZED (intact only in the PAST)", 0.14f, new Color(1f, 0.85f, 0.6f));
            Label("WinchSeized", futLook, new Vector3(-4.3f, 14.4f, 10.0f), 0f, "BOLT WINCH - SEIZED (intact only in the PAST)", 0.14f, new Color(0.6f, 1f, 1f));
            var bolt = Reactor("React_BoltDrawn", logic, F_GateUnlocked);
            bolt.reversible = false;
            bolt.announce = true;
            bolt.cue = Cue.PuzzleSolved;
            bolt.toast = "Calder's seal falls open - on the bell stairs and on the bell console. Cross the gap to the bell floor.";

            // Witness at the turn of the climax.
            var w = Group("Witness_Restart", logic).gameObject.AddComponent<MemorySequence>();
            w.triggerFlag = F_Restarted;
            w.lockControls = false;
            w.startCue = Cue.Memory;
            w.lines.Add(L("The Witness", "Good. The clock remembers how to turn. Ride it up.", 3.5f));
            w.lines.Add(L("The Witness", "In the FUTURE the wind comes through the split. Listen for the howl, watch the banners, and wait.", 4.5f));
        }

        static void Gust(string name, Vector3 center, Vector3 size, float offset)
        {
            var root = Group(name, logic);
            root.position = center;
            var h = root.gameObject.AddComponent<TelegraphedHazard>();
            h.idleTime = 3.0f; h.warnTime = 1.6f; h.activeTime = 1.5f; h.startOffset = offset;
            h.activeIn = TimeMask.Future;
            h.warnCue = Cue.HazardWarn;
            h.activeCue = Cue.HazardActive;
            h.warnLight = PointLight("GustWarnLight", root, center + Vector3.up * 1.2f, new Color(0.7f, 0.95f, 1f), 3.5f, 7f);
            var warn = Particles("GustWarnMist", root, center + Vector3.down * 0.8f, new Vector3(size.x, 0.2f, 0.5f), "mist");
            var wm = warn.main; wm.startLifetime = 1.5f; wm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            var we = warn.emission; we.rateOverTime = 40f;
            h.warnParticles = warn;
            var act = Particles("GustBlast", root, center + new Vector3(0f, -0.3f, -1.2f), new Vector3(size.x, 1f, 0.2f), "steam");
            act.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // blow across the beam (+z)
            var am = act.main; am.startSpeed = new ParticleSystem.MinMaxCurve(6f, 9f); am.startLifetime = 0.8f; am.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            var ae = act.emission; ae.rateOverTime = 120f;
            h.activeParticles = act;
            h.damageZone = TriggerBox("GustZone", root, center, size);
            // Banner on a pole: whips during the warning (shape cue, not just colour).
            var pole = Box("BannerPole", futLook, new Vector3(center.x, 14.4f, 7.3f), new Vector3(0.1f, 2.8f, 0.1f), "obsidian", false);
            var banner = Box("GustBanner", futLook, new Vector3(center.x + 0.5f, 15.3f, 7.3f), new Vector3(1f, 0.6f, 0.03f), "neon_magenta", false);
            Unstatic(banner.transform);
            h.shakeTarget = banner.transform;
            h.deathMessage = "A gust tore you off the beam. When the banners whip and the wind howls, hold at the shelter.";
            _ = pole; _ = wm; _ = we; _ = am; _ = ae;
        }
    }
}
