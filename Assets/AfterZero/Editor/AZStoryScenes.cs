using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>Prologue (the last train into Veyra) and Epilogue (two endings). See Docs/STORY.md.</summary>
    public static class AZStoryScenes
    {
        // =====================================================================
        // PROLOGUE — The Last Train
        // =====================================================================
        public static void BuildPrologue(Transform root)
        {
            var d = Systems(root, 0, TimeMask.Present, TimeState.Present, out var visuals);
            d.levelIndex = LevelCatalog.PrologueIndex;
            d.levelTitle = "The Last Train";
            d.location = "Veyra Line, night service";
            d.storySetup = "Arin wakes on the last train into Veyra with no memory and a letter dated tomorrow.";
            visuals.present.fogDensity = 0.012f;
            visuals.present.sunIntensity = 0.2f;

            d.introLines.Add(L("Arin", "...Wake up. You were asleep. You don't remember getting on."));
            d.introLines.Add(L("Arin", "Coat too heavy for spring. Ticket with no departure station. And an envelope with my name."));
            d.introLines.Add(L("Conductor", "Veyra, last stop. Everyone off before the bell, please."));

            AddObjective(d, "Check your coat pocket", "arin.watch", "Your coat is on the seat across the aisle. Something heavy is in the pocket.");
            AddObjective(d, "Read the letter on the seat beside you", "P.letterRead", "It's the envelope on the seat, sealed with a ring and a zero.");
            AddObjective(d, "Ask the conductor where this train came from", "P.talkedConductor", "The conductor is walking the aisle.");
            AddObjective(d, "Step off the train", "P.offTrain", "The doors at the front of the carriage are open.");
            AddObjective(d, "Walk through the arrival gate into the city", "P.gate", "Follow the platform lamps to the gate.");
            d.payoffFlag = "P.letterRead";
            d.payoffClueTitle = "A letter dated tomorrow";
            d.payoffClueText = "\"Arin. Don't trust the first bell. Save the blue cup. You have done this before.\" Sealed with the clock-tower seal: (0).";

            // --- Carriage (interior 3 m wide, 22 m long, along +Z) ---
            var train = Group("Carriage", root);
            Floor("Floor", train, new Vector3(0, 0, 0), new Vector2(3.2f, 22f), "wood_dark");
            Box("Roof", train, new Vector3(0, 2.75f, 0), new Vector3(3.4f, 0.2f, 22f), "metal_dark");
            Box("WallL_Low", train, new Vector3(-1.7f, 0.5f, 0), new Vector3(0.15f, 1f, 22f), "wood");
            Box("WallR_Low", train, new Vector3(1.7f, 0.5f, 0), new Vector3(0.15f, 1f, 22f), "wood");
            Box("WallL_High", train, new Vector3(-1.7f, 2.45f, 0), new Vector3(0.15f, 0.5f, 22f), "wood");
            Box("WallR_High", train, new Vector3(1.7f, 2.45f, 0), new Vector3(0.15f, 0.5f, 22f), "wood");
            Box("GlassL", train, new Vector3(-1.72f, 1.6f, 0), new Vector3(0.04f, 1.2f, 22f), "glass", true);
            Box("GlassR", train, new Vector3(1.72f, 1.6f, 0), new Vector3(0.04f, 1.2f, 22f), "glass", true);
            Box("BackWall", train, new Vector3(0, 1.4f, -11f), new Vector3(3.4f, 2.8f, 0.2f), "wood");
            var front = WallWithDoor("FrontWall", train, new Vector3(-1.7f, 0, 11f), new Vector3(1.7f, 0, 11f), 2.8f, 0.2f, "wood", 1.4f, 2.4f);
            for (int i = 0; i < 9; i++)
            {
                float z = -9f + i * 2.2f;
                foreach (float x in new[] { -1.05f, 1.05f })
                {
                    Box("Seat", train, new Vector3(x, 0.45f, z), new Vector3(1.1f, 0.12f, 0.9f), "fabric_teal");
                    Box("SeatBack", train, new Vector3(x, 0.9f, z - 0.4f), new Vector3(1.1f, 0.9f, 0.12f), "fabric_teal");
                }
                PointLight("CabinLamp", train, new Vector3(0, 2.55f, z), new Color(1f, 0.8f, 0.55f), 0.9f, 4.5f);
                Box("LampStrip", train, new Vector3(0, 2.63f, z), new Vector3(0.6f, 0.04f, 0.2f), "lamp_warm", false);
            }
            Label("LineSign", train, new Vector3(0, 2.2f, 10.85f), 180f, "VEYRA LINE  -  LAST STOP", 0.35f, new Color(1f, 0.75f, 0.4f));
            Ambient("TrainHum", root, new Vector3(0, 1.5f, 0), AmbientKind.Machine, 0.3f, TimeMask.All, 0.4f);
            Ambient("RainOnRoof", root, new Vector3(0, 3f, 0), AmbientKind.Rain, 0.25f, TimeMask.All, 0.3f);

            Player(root, new Vector3(0f, 0.05f, -7.9f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0, 0, -7.9f), new Vector3(1.2f, 2.5f, 1.2f), 0f, true);

            Clue("Letter", train, new Vector3(1.05f, 0.53f, -6.6f), new Vector3(0.25f, 0.02f, 0.18f), "paper",
                "Read the letter", "P.letterRead", "prologue.letter", "A letter dated tomorrow",
                "Arin,\n\nYou won't remember this, and that's all right. Don't trust the first bell. When the cup breaks, look at what stays broken.\n\nSave the blue cup. Then come to the tower.\n\nYou have done this before.\n\n                                   (0)\n\nDated: tomorrow.");

            // Arin's stopped watch: the personal thread of Design v2.
            Box("Coat", train, new Vector3(-1.05f, 0.56f, -9f), new Vector3(0.8f, 0.1f, 0.6f), "arin_coat", false);
            var watch = Clue("PocketWatch", train, new Vector3(-1.05f, 0.66f, -9f), new Vector3(0.4f, 0.1f, 0.4f), "brass",
                "Check your coat pocket", "arin.watch", "arin.watch", "A stopped pocket watch",
                "Brass, scratched, heavier than it looks. The hands are stuck at a minute to midnight.\n\nEngraved inside the lid:\n\n\"For A. - keep time for me.  I.M.\"", true);
            watch.lines.Add(L("Arin", "It doesn't tick. I don't know why I'm sure it used to."));
            watch.lines.Add(L("The Witness", "(over the intercom, faint)  Don't be afraid of the bell, Arin.", 4f));

            // Optional: a child passenger who knows the lullaby Arin hums (teaches HUM).
            var kid = Npc("Sleeping child", train, new Vector3(-1.05f, 0.45f, 3.6f), 0f, "fabric_gold", false, 31);
            kid.transform.localScale = Vector3.one * 0.6f;
            var hum = kid.gameObject.AddComponent<HumReceiver>();
            hum.radius = 4f;
            hum.setsFlag = "P.humHeard";
            hum.lines.Add(L("Child", "(waking)  That song... how do you know the Nursery song?", 3.5f));
            hum.lines.Add(L("Arin", "I don't. It was just... in my head."));
            Zone("HumHint", root, new Vector3(-0.3f, 1.2f, 3.6f), new Vector3(2.5f, 2.5f, 2f), "P.humHint",
                "A tune is stuck in your head. Hum it: H key, or the HUM button.");

            // Passengers and the conductor.
            var people = Group("People", root);
            var conductor = Npc("Conductor Sefa", people, new Vector3(0, 0, 6f), 180f, "coat_navy", true, 11);
            Route(conductor, false,
                (new Vector3(0, 0, 6f), 4f, NPCActivity.Look, "Tickets, please. Veyra, last stop."),
                (new Vector3(0, 0, -3f), 3f, NPCActivity.Work, ""),
                (new Vector3(0, 0, 9.5f), 5f, NPCActivity.Idle, "Mind the gap, mind the bell."));
            conductor.changedFlag = "P.offTrain";
            Route(conductor, true, (new Vector3(0f, 0f, 10f), 999f, NPCActivity.Look, ""));
            var cp = conductor.GetComponent<NPCPersona>();
            cp.task = "Walks the last service, checking tickets and counting passengers off before the bell.";
            cp.approachBarks.Add("Last stop. You'll want to be off before the bell.");
            cp.approachBarks.Add("You again? No... I'd remember that coat.");
            cp.talkLines.Add(L("Conductor Sefa", "Where from? This is the only train, friend. It goes to Veyra and it stops.", 4f));
            cp.talkLines.Add(L("Conductor Sefa", "Funny thing. I count one more passenger off than I count on. Every night.", 4.5f));
            cp.talkLines.Add(L("Arin", "Every night?"));
            cp.talkLines.Add(L("Conductor Sefa", "Did I say that? Long shift. Off you go, the festival's starting.", 4f));
            cp.talkedFlag = "P.talkedConductor";
            cp.conditional.Add(new ConditionalLines
            {
                whenFlag = "P.letterRead",
                setsFlag = "P.talkedConductor",
                lines =
                {
                    L("Arin", "Does this seal mean anything to you? A ring with a zero."),
                    L("Conductor Sefa", "That's the tower's seal. Only the clockkeeper uses it. And she hasn't written to anyone in forty years.", 5f),
                    L("Conductor Sefa", "The train came from the end of the line. Don't ask me where the end of the line is.", 4.5f),
                }
            });
            cp.changedLines.Add(L("Conductor Sefa", "Go on. The bell rings twice tonight. It always does.", 4f));

            var crowd = Crowd("Passengers", people, new Vector3(0, 0, -2f), new Vector2(1.2f, 14f), 4, 5, new[] { "Is that the tower?", "Nearly there.", "Did you hear a bell?" }, "fabric_red");
            foreach (var m in crowd) m.walkSpeed = 0.6f;

            Zone("OffTrain", root, new Vector3(0, 1.2f, 12.5f), new Vector3(3f, 2.5f, 1.5f), "P.offTrain", "Veyra. The air smells of rain and festival smoke.");

            // --- Platform and arrival gate ---
            var plat = Group("Platform", root);
            Floor("PlatformFloor", plat, new Vector3(-2.5f, 0.6f, 31f), new Vector2(8f, 38f), "stone_wet");
            Floor("Track", plat, new Vector3(4f, -0.6f, 30f), new Vector2(5f, 60f), "concrete");
            Ramp("StepDown", plat, new Vector3(0, 0f, 11.2f), new Vector3(0, 0.6f, 12.6f), 1.4f, "metal_dark");
            Floor("Landing", plat, new Vector3(-0.6f, 0.6f, 13.6f), new Vector2(3f, 2.4f), "stone_wet");
            for (int i = 0; i < 6; i++) Lamp("PlatformLamp" + i, plat, new Vector3(-5.5f, 0.6f, 14f + i * 6f), "sodium");
            Box("Canopy", plat, new Vector3(-2.5f, 5.2f, 30f), new Vector3(8.5f, 0.2f, 40f), "metal_dark");
            Particles("Rain", root, new Vector3(2f, 14f, 30f), new Vector3(18f, 1f, 50f), "rain");
            Particles("Steam", plat, new Vector3(2.2f, 0f, 9f), new Vector3(1f, 0.2f, 1f), "steam");
            Label("StationSign", plat, new Vector3(-6.3f, 3.6f, 26f), 90f, "VEYRA  ARRIVALS", 0.6f, new Color(0.8f, 0.95f, 1f));
            Building("StationHall", plat, new Vector3(-2.5f, 0.6f, 58f), new Vector3(14f, 12f, 6f), "plaster_cold", "window_warm", "metal_dark", 3, "ARRIVAL QUARTER", "neon_amber");
            Floor("Concourse", plat, new Vector3(-2.5f, 0.6f, 52.5f), new Vector2(8f, 5f), "tile_cold");
            Box("HallWallL", plat, new Vector3(-9f, 3.6f, 50f), new Vector3(0.4f, 6f, 1f), "plaster_cold");
            var gate = Door("ArrivalGate", plat, new Vector3(-2.5f, 0.6f, 49.6f), new Vector3(3f, 3f, 0.2f), "iron_rust", new Vector3(0, 3.1f, 0), 0f, "P.talkedConductor", "P.letterRead");
            gate.toast = "The arrival gate grinds open.";
            Exit("ToTheFestival", root, new Vector3(-2.5f, 0.6f, 51f), new Vector3(3f, 3f, 1.5f), "The gate is shut. Read the letter and talk to the conductor first.", "P.letterRead", "P.talkedConductor");
            Zone("GateReached", root, new Vector3(-2.5f, 1.8f, 48.5f), new Vector3(3f, 2.5f, 1f), "P.gate");
            Checkpoint("CP_Platform", root, new Vector3(-1f, 0.6f, 16f), new Vector3(4, 3, 3), 180f);

            // The city beyond the tracks, with the tower at the end of everything.
            var city = Group("CityBeyond", root);
            for (int i = 0; i < 16; i++)
                Building("B" + i, city, new Vector3(14f + (i % 3) * 9f, 0f, 2f + i * 6f), new Vector3(7f, 10f + (i * 7 % 13), 6f), "plaster_cold", "window_cold", "metal_dark", i);
            Box("TowerFar", city, new Vector3(30f, 25f, 120f), new Vector3(9f, 50f, 9f), "plaster_cold");
            var face = Hologram("TowerFace", city, new Vector3(30f, 42f, 115.4f), new Vector3(6f, 0.1f, 6f), PrimitiveType.Cylinder, "hologram_amber", false, false);
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            PointLight("TowerGlow", city, new Vector3(30f, 42f, 112f), new Color(1f, 0.8f, 0.5f), 6f, 30f);

            // Optional memory fragments.
            Fragment(train, new Vector3(-1.05f, 1.0f, 8.5f), "P.1", "A ticket stub", "Veyra Line. Single. The departure station has been scratched out so many times the paper is thin.");
            Fragment(plat, new Vector3(-6f, 1.4f, 40f), "P.2", "Chalk tally", "Behind the lamp post: tally marks in chalk, hundreds of them. The newest one is still wet.");
            Fragment(plat, new Vector3(1.2f, 1.2f, 46f), "P.3", "A child's drawing", "A tower with two bells and a blue cup. Signed 'M.O.'");
        }

        // =====================================================================
        // EPILOGUE
        // =====================================================================
        public static void BuildEpilogue(Transform root)
        {
            var d = Systems(root, 0, TimeMask.All, TimeState.Present, out var visuals);
            d.levelIndex = LevelCatalog.EpilogueIndex;
            d.levelTitle = "After Zero";
            d.location = "Veyra, the morning after";
            visuals.present.sunIntensity = 1.4f;
            visuals.present.sunColor = new Color(1f, 0.85f, 0.65f);
            visuals.present.fogDensity = 0.008f;
            visuals.present.saturation = 5f;
            visuals.present.temperature = 10f;

            Player(root, new Vector3(0, 0.05f, -8f), 0f);
            Checkpoint("CP_Start", root, new Vector3(0, 0, -8f), new Vector3(4, 3, 4), 0f, true);
            Ambient("Morning", root, Vector3.zero, AmbientKind.Wind, 0.25f, TimeMask.All, 0f);

            var ground = Group("Ground", root);
            Floor("Terrace", ground, new Vector3(0, 0, 0), new Vector2(30f, 30f), "stone_warm");
            for (int i = 0; i < 4; i++) Lamp("Lamp" + i, ground, new Vector3(-10f + i * 6.6f, 0, 10f), "gas");

            // Stability ending: the festival again, but people remember.
            var stability = Group("StabilityEnding", root);
            Crowd("Festival", stability, new Vector3(0, 0, 4f), new Vector2(20f, 10f), 12, 21, new[] { "Have we met?", "I remember you.", "Morning, Arin." }, "fabric_gold");
            Box("Stall", stability, new Vector3(-4f, 0.5f, 3f), new Vector3(2.4f, 1f, 1.2f), "wood");
            Cylinder("BlueCup", stability, new Vector3(-4f, 1.08f, 3f), 0.08f, 0.16f, "cup_blue");
            Particles("Dust", stability, new Vector3(0, 3f, 0), new Vector3(30f, 6f, 30f), "dust");
            Box("TowerEast", stability, new Vector3(0, 25f, 40f), new Vector3(9f, 50f, 9f), "plaster_warm");

            // Evidence ending: the horizon opens on regions with different laws.
            var evidence = Group("EvidenceEnding", root);
            for (int i = 0; i < 9; i++)
            {
                var isle = Box("FloatingIsle" + i, evidence, new Vector3(-60f + i * 15f, 18f + (i % 3) * 9f, 70f + (i % 2) * 20f), new Vector3(8f, 3f, 8f), i % 2 == 0 ? "crystal" : "obsidian", false, i * 23f);
                isle.AddComponent<FloatBob>().amplitude = 1.2f;
            }
            Hologram("HorizonZero", evidence, new Vector3(0, 30f, 110f), new Vector3(40f, 0.3f, 40f), PrimitiveType.Cylinder, "hologram", false, true).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            PointLight("GreenSun", evidence, new Vector3(-50f, 35f, 90f), new Color(0.4f, 1f, 0.6f), 8f, 120f);
            Particles("RisingRain", evidence, new Vector3(30f, 0f, 70f), new Vector3(20f, 1f, 20f), "motes");

            var epi = new GameObject("Epilogue").AddComponent<EpilogueSequence>();
            epi.transform.SetParent(root, false);
            epi.stabilityScene = stability.gameObject;
            epi.evidenceScene = evidence.gameObject;
            epi.stabilityLines.Add(L("Arin", "The bell rang once this morning. Only once."));
            epi.stabilityLines.Add(L("Pell", "Letter for Arin Vale. Dated today, for a change. Welcome back.", 4f));
            epi.stabilityLines.Add(L("The Witness", "The city still repeats. But now it remembers a little more every time. That's how wounds close.", 5f));
            epi.stabilityLines.Add(L("Arin", "Then I'll be here when it's ready to stop.", 3.5f));
            epi.stabilityLines.Add(L("Mother Ondine Halden", "The Keepers answer to you now, clockkeeper. Keep time for us.", 4.5f));
            epi.stabilityLines.Add(L("Arin", "(the watch ticks)  I'll keep it, Ilse. Every night.", 4f));
            epi.evidenceLines.Add(L("Arin", "The record is public. Every name the Founders erased is back on the wall.", 4f));
            epi.evidenceLines.Add(L("The Witness", "And the states are letting go of each other. Look at the horizon, Arin.", 4.5f));
            epi.evidenceLines.Add(L("Arin", "Islands falling upward. A green sun. Rain going the wrong way.", 4f));
            epi.evidenceLines.Add(L("Pell Ashgrove", "Lise faded at dawn. She was born in the loop - she only ever had the one night.", 5f));
            epi.evidenceLines.Add(L("Pell Ashgrove", "I'd choose it again. She'd want the names said out loud.", 4f));
            epi.evidenceLines.Add(L("The Witness", "The first cycle is over. The world outside Veyra was never held together the way the city was. Go and see it.", 5.5f));
            epi.evidenceLines.Add(L("Maren", "Come on, Arin. For once, let's be late.", 3.5f));
            epi.recordsLines.Add(L("Odile Fenwick", "I've published the families' names. All of them. Someone has to say them out loud.", 4.5f));
            epi.keepsakeLines.Add(L("The Witness", "You saved my music box. Listen. All four notes.", 4f));
            epi.allFragmentsLines.Add(L("Ilse Marrow", "(an echo winds the watch)  You found every piece of us, my little A. Thank you for remembering.", 5f));
            epi.groupsLostLines.Add(L("Warden Roe", "We didn't get everyone out of the square. I'll carry their names. So should you.", 4.5f));
            epi.allGroupsLines.Add(L("Warden Roe", "Every group made the tram stairs. Forty years and not one name lost tonight.", 4.5f));
            epi.credits = "AFTERZERO  -  GAME 01: THE FIRST CYCLE\n\nThank you for playing.\n\nThe story continues beyond the horizon.";

            d.objectives.Add(new Objective { text = "Watch the morning come", completeFlag = "E.done" });
        }
    }
}
