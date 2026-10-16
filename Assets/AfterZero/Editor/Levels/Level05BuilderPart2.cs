using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    public static partial class Level05Builder
    {
        // =====================================================================
        // Beat 4: rescue Maren from the laundry (SE alley)
        // =====================================================================

        static NPCMotor pell;
        static EvacueeGroup pellGroup;

        static void BuildLaundryAndRescue(Transform root, Transform logic, Transform past, Transform present, Transform future)
        {
            var lg = Group("Laundry", root);
            Room("Shell", lg, new Vector3(27f, 0f, -12f), new Vector2(6f, 8f), 4.2f, "plaster_cold", null, "wood_dark", 3);
            Box("Tiles", lg, new Vector3(27f, 0.015f, -12f), new Vector3(5.6f, 0.03f, 7.6f), "tile_cold", false);
            Box("Gutter", lg, new Vector3(19f, 4.6f, -10.9f), new Vector3(10f, 0.25f, 0.6f), "iron_rust");
            Box("GutterS", lg, new Vector3(19f, 4.6f, -13.1f), new Vector3(10f, 0.25f, 0.6f), "iron_rust");
            Label("AlleyName", lg, new Vector3(14.1f, 3.4f, -9.9f), -90f, "LAUNDRY ALLEY", 0.3f, TextWarm);

            // PAST: the laundry yard is open to the alley, but its roof beam sags. Three timber shores
            // set under it in the PAST change how it falls in the PRESENT - one rubble bank per shore.
            var pg = StateGroup("Past Laundry Yard", root, TimeMask.Past, true);
            Box("YardFence", pg, new Vector3(19.3f, 1.2f, -12f), new Vector3(0.15f, 2.4f, 3f), "wood");
            Label("FenceNote", pg, new Vector3(19.2f, 1.9f, -12f), 90f, "LAUNDRY CLOSED\nFOR BELL DAY", 0.2f, TextWarm);
            TiltBox("SaggingBeam", pg, new Vector3(17.5f, 3.3f, -12f), new Vector3(3.4f, 0.35f, 0.35f), "wood_dark", new Vector3(0f, 0f, -6f));
            Label("BeamNote", pg, new Vector3(15.2f, 3.0f, -12f), 90f, "the yard beam sags - shore it up", 0.16f, TextWarm);
            var pastLaundry = StateGroup("Past Laundry", root, TimeMask.Past, false);
            for (int i = 0; i < 2; i++)
                Cylinder("Copper" + i, pastLaundry, new Vector3(28.6f, 0.5f, -14.5f + i * 2.4f), 0.55f, 1f, "brass");
            Particles("CopperSteam", pastLaundry, new Vector3(28.6f, 1.1f, -13.3f), new Vector3(0.6f, 0.1f, 2.6f), "steam");
            PointLight("LaundryGas", pastLaundry, new Vector3(27f, 3.6f, -12f), Gas, 2f, 8f);

            // FUTURE: the alley is a crystal seam.
            var fg = StateGroup("Future Alley Seal", root, TimeMask.Future, true);
            Box("CrystalWall", fg, new Vector3(15f, 1.6f, -12f), new Vector3(0.6f, 3.2f, 3f), "crystal");
            Crystals("AlleyCrystals", fg, new Vector3(15f, 0f, -12f), 1.4f, 404);
            var futLaundry = StateGroup("Future Laundry", root, TimeMask.Future, false);
            Crystals("LaundryCrystals", futLaundry, new Vector3(28.5f, 0f, -14.5f), 1.0f, 405);
            Hologram("LostChild", futLaundry, new Vector3(27f, 0.7f, -12f), new Vector3(0.35f, 0.55f, 0.25f), PrimitiveType.Capsule, "hologram_magenta", true, false);
            Label("LostChildText", futLaundry, new Vector3(27f, 1.8f, -12f), 90f, "UNCOUNTED: 1 CHILD", 0.18f, new Color(1f, 0.5f, 0.9f));

            // PRESENT: three banks of rubble from the collapsed beam fill the alley; Maren is held beyond.
            var debris = StateGroup("Present Alley Debris", root, TimeMask.Present, true);
            string[] shoreFlags = { "L05.shore1", "L05.shore2", "L05.shore3" };
            Vector3[] shoreSpawns = { new Vector3(7.6f, 0.75f, -8.6f), new Vector3(-10.6f, 0.75f, -4.6f), new Vector3(-20.6f, 0.75f, -9.6f) };
            string[] shoreWhere = { "by Tobin's bench", "at the market stall", "in the sluice house" };
            var shoresPast = StateGroup("Past Shores", root, TimeMask.Past, true);
            for (int i = 0; i < 3; i++)
            {
                float x = 16.5f + i * 1.0f;
                var bank = Group("RubbleBank" + (i + 1), debris);
                Box("Block", bank, new Vector3(x, 1.3f, -12f), new Vector3(0.95f, 2.6f, 3f), i == 1 ? "brick_warm" : "concrete");
                TiltBox("Spill", bank, new Vector3(x - 0.2f, 0.35f, -11.2f), new Vector3(1.0f, 0.7f, 1.2f), "ruin_dark", new Vector3(0f, 25f * i, 12f));
                Label("BankNo", bank, new Vector3(x - 0.5f, 2.2f, -12f), 90f, "RUBBLE " + (i + 1), 0.16f, TextCold);
                var dust = Particles("Collapse" + i, logic, new Vector3(x, 1.5f, -12f), new Vector3(1f, 1f, 3f), "debris");
                var r = Reactor("Shore " + (i + 1) + " Holds", logic, shoreFlags[i]);
                r.disableWhenTrue.Add(bank.gameObject);
                r.announce = true;
                r.toast = "In the PRESENT, rubble bank " + (i + 1) + " slides away - the beam came down differently.";
                r.reversible = false;
                dust.gameObject.SetActive(false);
                r.enableWhenTrue.Add(dust.gameObject);

                var s = Socket("ShoreSocket" + (i + 1), shoresPast, new Vector3(x, 0.03f, -12f), "L05_shore", shoreFlags[i], "Set the shore under the beam", 0.35f);
                s.usableIn = TimeMask.Past;
                s.anchor.position = new Vector3(x, 0.72f, -12f);
                Label("SocketNo", shoresPast, new Vector3(x - 0.45f, 1.7f, -12f), 90f, "SHORE " + (i + 1), 0.14f, TextWarm);
                var shore = Grab("TimberShore" + (i + 1), shoresPast, shoreSpawns[i], new Vector3(0.2f, 1.4f, 0.2f), "wood", "L05_shore", "timber shore");
                shore.usableIn = TimeMask.Past;
                Label("ShoreTag", shoresPast, shoreSpawns[i] + Vector3.up * 1.0f, 0f, "TIMBER SHORE\n" + shoreWhere[i], 0.12f, TextWarm);
            }
            var cleared = Reactor("Alley Cleared", logic, shoreFlags);
            cleared.setFlagWhenTrue = F_Debris;
            cleared.announce = true;
            cleared.cue = Cue.PuzzleSolved;
            cleared.toast = "The alley is open in the PRESENT. Go to Maren - now.";
            cleared.reversible = false;

            // Rescue pressure: the laundry floods while Maren is held there (telegraphed by rising water + timer).
            var rescueTimer = new GameObject("Laundry Floods (CrisisTimer)").AddComponent<CrisisTimer>();
            rescueTimer.transform.SetParent(logic, false);
            rescueTimer.duration = 170f;
            rescueTimer.startFlag = "L05.heardMaren";
            rescueTimer.successFlag = F_Child;
            rescueTimer.startMessage = "The laundry is flooding. Shore the beam in the PAST - three shores - and get Maren out in the PRESENT.";
            rescueTimer.timerLabel = "LAUNDRY FLOODS IN";
            rescueTimer.failMessage = "The water reached the laundry ceiling. The bell rings again - and the basin forgets Maren too.";
            var alleyRise = Flood("Alley Flood", logic, new Vector3(21f, 0f, -12f), new Vector2(14f, 3f), Vector2.zero, -0.6f, 1.0f, 99f, false, new[] { "L05.heardMaren" });
            alleyRise.speed = 1.6f / 170f;
            Label("AlleyFloodMark", debris, new Vector3(14.2f, 1.0f, -10.55f), -90f, "- WATER RISING -", 0.14f, TextCold);

            // Maren's cries once the basin is passable (PRESENT).
            var cries = Zone("Maren Cries", logic, new Vector3(9f, 1f, -12f), new Vector3(8f, 2.5f, 8f), "L05.heardMaren", "", TimeMask.Present);
            cries.requiredFlags.Add(F_Pump);
            cries.lines.Add(L("Maren (far off)", "Help! Is somebody out there? The water's coming under the door!"));
            cries.lines.Add(L("Arin", "A child. In the laundry alley."));

            // Telegraphed hazard: loose slates fall from the gutter (PRESENT). Light/particles live with the
            // hazard in shared logic (it only drives them in the PRESENT); signs and slates are Present-only.
            var hz = StateGroup("Present Slates Hazard", root, TimeMask.Present, true);
            var hzGo = new GameObject("Loose Slates");
            hzGo.transform.SetParent(logic, false);
            hzGo.transform.position = new Vector3(21.8f, 0f, -12f);
            var h = hzGo.AddComponent<TelegraphedHazard>();
            h.idleTime = 3.2f; h.warnTime = 1.5f; h.activeTime = 1.0f;
            h.activeIn = TimeMask.Present;
            h.warnLight = SpotLight("SlateWarn", hzGo.transform, new Vector3(21.8f, 4.3f, -12f), new Vector3(90f, 0f, 0f), new Color(1f, 0.2f, 0.12f), 6f, 7f, 60f, false);
            h.warnParticles = Particles("SlateDust", hzGo.transform, new Vector3(21.8f, 4.5f, -12f), new Vector3(2.4f, 0.2f, 2.8f), "debris");
            h.activeParticles = Particles("SlateFall", hzGo.transform, new Vector3(21.8f, 3.6f, -12f), new Vector3(2.4f, 0.2f, 2.8f), "debris");
            var slates = Group("FallingSlates", hz);
            for (int i = 0; i < 5; i++)
                TiltBox("Slate", slates, new Vector3(21.0f + i * 0.4f, 0.4f + (i % 3) * 0.5f, -12.9f + i * 0.45f), new Vector3(0.45f, 0.04f, 0.3f), "ruin_dark", new Vector3(i * 23f, i * 47f, i * 11f));
            h.activeVisual = slates.gameObject;
            var dz = TriggerBox("SlateDamage", hz, new Vector3(21.8f, 1.25f, -12f), new Vector3(2.0f, 2.5f, 3f));
            h.damageZone = dz;
            h.deathMessage = "The slates came down. Wait for the dust and the red light to pass, then run.";
            Label("SlateWarningN", hz, new Vector3(21.8f, 3.0f, -10.6f), 180f, "LOOSE SLATES\nwait for the dust, then run", 0.16f, new Color(1f, 0.6f, 0.4f));
            Label("SlateWarningS", hz, new Vector3(21.8f, 3.0f, -13.4f), 0f, "LOOSE SLATES\nwait for the dust, then run", 0.16f, new Color(1f, 0.6f, 0.4f));

            // PRESENT laundry interior: dry, lantern-lit; Maren waits here.
            var pl = StateGroup("Present Laundry", root, TimeMask.Present, false);
            Box("TubA", pl, new Vector3(28.8f, 0.4f, -14.8f), new Vector3(1.2f, 0.8f, 1.0f), "metal_dark");
            Box("Mangle", pl, new Vector3(28.9f, 0.6f, -9.4f), new Vector3(0.9f, 1.2f, 1.2f), "iron_rust");
            for (int i = 0; i < 4; i++)
                Box("Sheet" + i, pl, new Vector3(25.4f + i * 1.0f, 2.2f, -12.4f + (i % 2) * 0.9f), new Vector3(0.8f, 1.1f, 0.02f), "coat_white", false);
            Box("Line", pl, new Vector3(27f, 2.8f, -12f), new Vector3(5.6f, 0.02f, 0.02f), "wood_dark", false);
            Box("DoorSill", pl, new Vector3(24.2f, 0.08f, -12f), new Vector3(0.4f, 0.16f, 2f), "concrete");

            pell = Npc("Maren Oskel", pl, new Vector3(27.4f, 0f, -12.6f), -90f, "coat_ochre", true, 7);
            pell.transform.localScale = Vector3.one * 0.72f;
            pell.walkSpeed = 1.1f;

            var lantern = Prim(PrimitiveType.Cube, "MarensLantern", pl, new Vector3(28.4f, 0.3f, -13.4f), new Vector3(0.28f, 0.45f, 0.28f), "lamp_warm", true, false);
            PointLight("LanternLight", pl, new Vector3(28.4f, 0.9f, -13.4f), new Color(1f, 0.7f, 0.35f), 2.4f, 7f);
            pellGroup = lantern.AddComponent<EvacueeGroup>();
            pellGroup.groupName = "Maren";
            pellGroup.prompt = "Take Maren's hand - lead her to the Weir Stairs";
            pellGroup.usableIn = TimeMask.Present;
            pellGroup.requiredFlags.Add(F_Debris);
            pellGroup.requiredFlags.Add("maren.recognised");
            pellGroup.lockedMessage = "Maren shrinks back: 'I don't go with strangers. Mama said the one who hums would come.'  (Hum: H / HUM)";
            pellGroup.members.Add(pell.transform);
            pellGroup.safeZone = Marker("MarenSafeZone", logic, new Vector3(0f, 0f, -18.2f));
            pellGroup.safeRadius = 3f;
            pellGroup.followDistance = 2.0f;
            pellGroup.speed = 3.6f;
            pellGroup.savedFlag = F_Child;
            pellGroup.savedToast = "Maren is safe at the foot of the Weir Stairs.";
            pellGroup.onFollow.Add(L("Maren Oskel", "You came! Mama said someone would come where the water can't follow."));
            var hum = pell.gameObject.AddComponent<HumReceiver>();
            hum.radius = 6f;
            hum.activeIn = TimeMask.Present;
            hum.setsFlag = "maren.recognised";
            hum.lines.Add(L("Maren Oskel", "...That's Mama's song. All of it - even the note that's always missing."));
            hum.lines.Add(L("Maren Oskel", "You're the one who hums. From the nursery. I know you."));
            hum.lines.Add(L("Arin", "(The list said VALE, A., age 8. Was I one of them? Did I hum this to her?)"));
            pellGroup.onFollow.Add(L("Arin", "Stay close. When the red light flickers, we wait."));
            Hologram("SafeZoneRing", logic, new Vector3(0f, 0.08f, -18.2f), new Vector3(5.6f, 0.01f, 5.6f), PrimitiveType.Cylinder, "hologram_amber", false, true);

            var climb = pell.gameObject.AddComponent<L05ChildRescue>();
            climb.group = pellGroup;
            climb.motor = pell;
            climb.climbPath.Add(Marker("Climb0", logic, new Vector3(0f, 0f, -19.4f)));
            climb.climbPath.Add(Marker("Climb1", logic, new Vector3(0f, 3f, -24.05f)));
            climb.climbPath.Add(Marker("Climb2", logic, new Vector3(1.8f, 3f, -26.2f)));
        }

        // =====================================================================
        // Beat 5: the choice under pressure (Ward Office, NW)
        // =====================================================================

        static void BuildWardOfficeAndChoice(Transform root, Transform logic, Transform past, Transform present, Transform future)
        {
            var wo = Group("Ward Office", root);
            Room("Shell", wo, new Vector3(-18f, 0f, 10f), new Vector2(8f, 8f), 4.6f, "stone_warm", null, "wood_dark", 1);
            Label("WardSign", wo, new Vector3(-13.75f, 3.3f, 10f), -90f, "WARD OFFICE\nRECORDS OF OLD VEYRA", 0.3f, TextWarm);
            Box("Counter", wo, new Vector3(-17.2f, 0.55f, 10f), new Vector3(0.6f, 1.1f, 4.2f), "wood_dark");

            // PAST: a clerk at work, ledgers stacked high.
            var pw = StateGroup("Past Ward Office", root, TimeMask.Past, false);
            for (int i = 0; i < 4; i++) Box("Ledgers" + i, pw, new Vector3(-21.5f, 0.6f + i * 0.5f, 7.5f + i * 0.2f), new Vector3(0.6f, 0.4f, 1.6f), i % 2 == 0 ? "fabric_red" : "fabric_teal");
            PointLight("WardGas", pw, new Vector3(-18f, 3.8f, 10f), Gas, 2f, 8f);
            var clerk = Npc("Ward Clerk", pw, new Vector3(-19.6f, 0f, 11.4f), 90f, "coat_navy", false, 41, "paper");
            Route(clerk, false, (new Vector3(-20.8f, 0f, 8f), 5f, NPCActivity.Work, ""), (new Vector3(-19.6f, 0f, 11.6f), 4f, NPCActivity.Carry, ""), (new Vector3(-17.9f, 0f, 9f), 3f, NPCActivity.Work, ""));
            clerk.reactionBarks.Add("Every family in the ward, signed and sealed.");

            // FUTURE: purged archive.
            var fw = StateGroup("Future Ward Office", root, TimeMask.Future, false);
            Crystals("WardCrystals", fw, new Vector3(-21f, 0f, 12.5f), 0.9f, 606);
            Hologram("PurgeSign", fw, new Vector3(-18f, 2.6f, 10f), new Vector3(1.8f, 0.6f, 0.02f), PrimitiveType.Cube, "hologram_magenta", true, false).transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Label("PurgeText", fw, new Vector3(-17.95f, 2.6f, 10f), -90f, "ARCHIVE PURGED", 0.24f, Color.white);

            // PRESENT: the two things worth saving.
            var pr = StateGroup("Present Ward Office", root, TimeMask.Present, true);
            Box("PlinthR", pr, new Vector3(-20.6f, 0.4f, 8.2f), new Vector3(0.9f, 0.8f, 0.9f), "stone_wet");
            Box("PlinthK", pr, new Vector3(-20.6f, 0.4f, 11.8f), new Vector3(0.9f, 0.8f, 0.9f), "stone_wet");
            Label("FloodLine", pr, new Vector3(-21.75f, 2.2f, 10f), -90f, "- - - FLOOD LINE - - -", 0.18f, TextCold);
            PointLight("WardCold", pr, new Vector3(-18f, 3.8f, 10f), ColdTeal, 1.4f, 8f);

            var records = Prim(PrimitiveType.Cube, "FamiliesRegister", pr, new Vector3(-20.6f, 1.05f, 8.2f), new Vector3(0.75f, 0.5f, 0.55f), "wood", true, false);
            var recLabel = Label("RecordsLabel", pr, new Vector3(-20.5f, 1.85f, 8.2f), -90f, "THE FAMILIES' REGISTER\nevery erased household, names intact", 0.14f, TextWarm);
            var keepsake = Prim(PrimitiveType.Cube, "MarensMusicBox", pr, new Vector3(-20.6f, 0.97f, 11.8f), new Vector3(0.42f, 0.26f, 0.32f), "brass", true, false);
            Box("MusicBoxLid", pr, new Vector3(-20.6f, 1.12f, 11.95f), new Vector3(0.42f, 0.24f, 0.03f), "wood", false);
            var keepLabel = Label("KeepsakeLabel", pr, new Vector3(-20.5f, 1.85f, 11.8f), -90f, "MAREN'S MUSIC BOX\nher mother's lullaby - one note missing", 0.14f, TextWarm);
            // The music box can be heard before choosing: four notes, the last one silent.
            var tune = new GameObject("Maren's Lullaby").AddComponent<SequencePuzzle>();
            tune.transform.SetParent(logic, false);
            tune.transform.position = new Vector3(-20.6f, 1.1f, 11.8f);
            tune.solution = new List<int> { 0, 2, 4, 3 };
            tune.demoNoteGap = 0.65f;
            var crank = Prim(PrimitiveType.Cube, "MusicBoxKey", pr, new Vector3(-20.15f, 1.0f, 11.8f), new Vector3(0.12f, 0.12f, 0.06f), "brass", true, false);
            var listen = crank.AddComponent<MelodyListener>();
            listen.prompt = "Wind Maren's music box";
            listen.usableIn = TimeMask.Present;
            listen.puzzle = tune;
            listen.missingNotes = new List<int> { 3 };
            listen.heardFlag = "L05.musicBoxHeard";
            // Founder Halden's order (optional evidence).
            Clue("HaldenNotice", pr, new Vector3(-21.7f, 1.7f, 10f), new Vector3(0.05f, 0.7f, 0.5f), "paper", "Read the pinned notice", "L05.haldenOrder",
                "clue.L05.halden", "By order of Founder Aurel Halden",
                "WARD ZERO - NOTICE OF RECORD WITHDRAWAL\n\nBy order of Founder Aurel Halden, the following households are struck from the public record, effective Bell Day:\nVELL. CORRAN. OSKEL. (and thirty-eight more)\n\nDoor plates are to be scraped. Names are not to be spoken in the ward. This is for their protection.\n\n- sealed (0)", true, TimeMask.Present);

            var cr = records.AddComponent<ChoiceInteractable>();
            cr.prompt = "Save the families' register";
            cr.usableIn = TimeMask.Present;
            cr.requiredFlags.Add(F_Crisis);
            cr.lockedMessage = "Not yet. Someone is still trapped out there - and the water is quiet for now.";
            cr.groupFlag = F_Choice;
            cr.optionFlag = F_Records;
            cr.confirmText = "Carry out the families' register? It proves the erased households existed - names, births, addresses. Maren's music box will be lost to the flood.";
            cr.lines.Add(L("Arin", "Every name in the basin. Someone has to remember them now."));
            cr.lines.Add(L("Arin", "The head archivist - Odile Fenwick. She'll know what to do with this."));

            var ck = keepsake.AddComponent<ChoiceInteractable>();
            ck.prompt = "Save Maren's music box";
            ck.usableIn = TimeMask.Present;
            ck.requiredFlags.Add(F_Crisis);
            ck.lockedMessage = cr.lockedMessage;
            ck.groupFlag = F_Choice;
            ck.optionFlag = F_Keepsake;
            ck.confirmText = "Carry out Maren's music box? The last thing her mother gave her. The families' register - the only proof these households existed - will be lost.";
            ck.lines.Add(L("Arin", "Records can be rewritten. This can't."));
            ck.lines.Add(L("Arin", "Four notes, one missing. Maybe someday she'll hear the whole song."));

            cr.alternatives.Add(keepsake);
            cr.alternatives.Add(keepLabel);
            cr.alternatives.Add(pr.Find("MusicBoxLid").gameObject);
            cr.alternatives.Add(crank);
            ck.alternatives.Add(records);
            ck.alternatives.Add(recLabel);

            var takenR = Reactor("Register Taken", logic, F_Records);
            takenR.disableWhenTrue.Add(records);
            takenR.disableWhenTrue.Add(recLabel);
            takenR.reversible = false;
            var takenK = Reactor("Keepsake Taken", logic, F_Keepsake);
            takenK.disableWhenTrue.Add(keepsake);
            takenK.disableWhenTrue.Add(keepLabel);
            takenK.disableWhenTrue.Add(pr.Find("MusicBoxLid").gameObject);
            takenK.disableWhenTrue.Add(crank);
            takenK.reversible = false;

            // Crisis: once Maren is safe and the rooms are understood, the culverts fail.
            var crisis = Reactor("Crisis Begins", logic, F_Child, F_Rooms, "L05.ondineDone");
            crisis.setFlagWhenTrue = F_Crisis;
            crisis.announce = true;
            crisis.cue = Cue.Alert;
            crisis.reversible = false;

            var alarm = Group("Crisis Alarm", pr);
            for (int i = 0; i < 1; i++)
            {
                var l = PointLight("Alarm" + i, alarm, new Vector3(-15f, 3.8f, 7.5f + i * 5f), new Color(1f, 0.15f, 0.1f), 3f, 8f);
                var f = l.gameObject.AddComponent<NeonFlicker>();
                f.targetLight = l; f.flickerChance = 0.25f; f.hum = 0.4f;
            }
            alarm.gameObject.SetActive(false);
            crisis.enableWhenTrue.Add(alarm.gameObject);

            var call = Zone("Crisis Call", logic, new Vector3(0f, 2f, -8f), new Vector3(90f, 8f, 70f), "L05.crisisCall", "", TimeMask.All);
            call.requiredFlags.Add(F_Crisis);
            call.lines.Add(L("Hesk Lowry", "The culverts are going! The whole basin is coming back!"));
            call.lines.Add(L("Hesk Lowry", "Arin - the ward office! The families' register and the Oskel girl's music box. You'll only carry one!"));
            call.gameObject.SetActive(false);
            crisis.enableWhenTrue.Add(call.gameObject);

            var timer = new GameObject("Flood Returns (CrisisTimer)");
            timer.transform.SetParent(logic, false);
            var ct = timer.AddComponent<CrisisTimer>();
            ct.duration = 80f;
            ct.startFlag = F_Crisis;
            ct.successFlag = F_Choice;
            ct.startMessage = "The water is coming back. Save the records or the music box - there is time for one.";
            ct.timerLabel = "FLOOD RETURNS IN";
            ct.failMessage = "The flood closes over the ward office. The bell rings again - and the basin forgets you too.";
            ct.startMessage = "The culverts are failing - the flood is coming back. Save what you can from the Ward Office.";
            ct.timerLabel = "FLOOD RETURNS IN";
            timer.SetActive(false);
            crisis.enableWhenTrue.Add(timer);
            BuildOndine(root, logic, ct);

            // Rising water (PRESENT): telegraphs the pressure by climbing toward the windows.
            var rise = Flood("Rising Flood", logic, new Vector3(0f, 0f, -2f), new Vector2(60f, 46f), Vector2.zero, -0.6f, 1.15f, 99f, false, new[] { F_Crisis });
            rise.speed = 1.75f / 80f;
            rise.emitCues = true;
        }

        // =====================================================================
        // Confrontation: Mother Ondine Halden arrives by boat (PRESENT)
        // =====================================================================

        static void BuildOndine(Transform root, Transform logic, CrisisTimer crisisTimer)
        {
            var arrival = Group("Ondine Arrival (Present)", root);
            var grp = StateGroup("Present Ondine", arrival, TimeMask.Present, false);
            Vector3 boatPos = new Vector3(4.2f, 0f, -12.5f);
            var boat = Group("KeeperBoat", grp);
            Box("Hull", boat, boatPos + Vector3.up * 0.25f, new Vector3(1.6f, 0.5f, 4.2f), "wood_dark", false, 15f);
            Box("Prow", boat, boatPos + new Vector3(0.55f, 0.45f, 2.0f), new Vector3(0.5f, 0.4f, 0.5f), "obsidian", false, 15f);
            var lamp = PointLight("BoatLantern", boat, boatPos + new Vector3(0f, 1.8f, 1.6f), new Color(1f, 0.85f, 0.55f), 3f, 9f);
            Label("BoatMark", boat, boatPos + new Vector3(-0.9f, 0.6f, 0f), -90f, "KEEPERS OF THE HOUR", 0.14f, new Color(1f, 0.85f, 0.6f));

            var ondine = Npc("Mother Ondine Halden", grp, new Vector3(3.2f, 0f, -14.6f), 200f, "coat_navy", true, 44);
            Route(ondine, false,
                (new Vector3(3.2f, 0f, -14.6f), 6f, NPCActivity.Look, "Lanterns low. It's here somewhere."),
                (new Vector3(5.6f, 0f, -10.8f), 4f, NPCActivity.Work, "Mark the water line. The loop keeps it, we keep the loop."),
                (new Vector3(1.8f, 0f, -12.4f), 4f, NPCActivity.Look, "Where is the one who hums?"));
            ondine.changedFlag = "L05.ondineDone";
            Route(ondine, true,
                (new Vector3(4.6f, 0f, -12.2f), 6f, NPCActivity.Idle, ""),
                (new Vector3(6.2f, 0f, -14.8f), 4f, NPCActivity.Look, "The water will take this place again tomorrow. It always does."),
                (new Vector3(3.6f, 0f, -10.6f), 4f, NPCActivity.Work, ""));
            var po = ondine.GetComponent<NPCPersona>();
            po.task = "Leader of the Keepers of the Hour, Founder Halden's daughter. Patrols the drowned basin by boat, hunting 'anomalies' who remember the repeat.";
            po.approachBarks.AddRange(new[] { "Stay where my lantern can see you.", "You again. The one who ran from me in the archive." });
            po.talkLines.Add(L("Mother Ondine Halden", "You ran from me in the archive. You took a record that should have burned."));
            po.talkLines.Add(L("Mother Ondine Halden", "Inside the cycle, nobody dies for good. Every morning the basin is dry again. Why break that?"));
            po.talkLines.Add(L("Mother Ondine Halden", "My father kept this city alive with silence. I keep it alive with the loop."));
            po.talkLines.Add(L("Mother Ondine Halden", "The Keepers lock doors so people don't wander into tomorrow. It's a kindness."));
            po.talkLines.Add(L("Mother Ondine Halden", "Whatever you're carrying out of the flood - it belongs to the night, not to you."));
            po.conditional.Add(new ConditionalLines { whenFlag = "L05.ondineYielded", lines = { L("Mother Ondine Halden", "Forty-one households, in his hand. I was told they were criminals."), L("Mother Ondine Halden", "Go on. I need to think - and I don't want my Keepers seeing me do it.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "L05.keepersHunt", lines = { L("Mother Ondine Halden", "Run, anomaly. The lanterns will find you."), L("Mother Ondine Halden", "Every time you're caught, the water comes faster. That's the loop being merciful.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = F_Child, lines = { L("Mother Ondine Halden", "A child. Alive, in the drowned ward. That shouldn't be possible."), L("Mother Ondine Halden", "Which of you is the anomaly - you, or her?") } });
            po.changedLines.Add(L("Mother Ondine Halden", "You have what you came for. So do I."));
            po.changedLines.Add(L("Mother Ondine Halden", "We'll meet at the tower. Everyone does, on Bell Day."));
            grp.gameObject.SetActive(false);
            var arrive = Reactor("Ondine Arrives", logic, F_Child);
            arrive.enableWhenTrue.Add(grp.gameObject);
            arrive.announce = true;
            arrive.cue = Cue.Alert;
            arrive.toast = "A Keeper boat glides in across the square. Its lantern finds you.";
            arrive.reversible = false;

            // The dialogue scene (own object; triggered when Arin comes to the boat with the rooms understood).
            var meet = Zone("Ondine Confrontation Zone", logic, new Vector3(2.5f, 1f, -14.5f), new Vector3(12f, 3f, 9f), "L05.ondineConfronts", "", TimeMask.Present);
            meet.requiredFlags.Add(F_Child);
            meet.requiredFlags.Add(F_Rooms);
            var host = new GameObject("Ondine Dialogue");
            host.transform.SetParent(logic, false);
            host.transform.position = new Vector3(3.2f, 1f, -14.6f);
            var dc = Dialogue(host, "Mother Ondine Halden", "Mother Ondine Halden blocks the way to the Ward Office. What do you do?", "L05.ondineConfronts");
            dc.opening.Add(L("Mother Ondine Halden", "Stop there. Lanterns on the anomaly.", 3f));
            dc.opening.Add(L("Mother Ondine Halden", "You pulled a child out of a house that doesn't exist. Now you want the ward office. Why?", 5f));
            var show = Option("Show her the evacuation list", "L05.ondineYielded",
                L("Arin", "Your father's evacuation list. Forty-one households. Nine days before the catastrophe.", 4.5f),
                L("Mother Ondine Halden", "...That's his hand. His seal.", 3f),
                L("Mother Ondine Halden", "He told me they were criminals. That the erasure was a sentence.", 4f),
                L("Mother Ondine Halden", "Keepers - lanterns down. Let this one pass. Tonight, only tonight.", 4f));
            show.requiresFlags.Add("evidence.list");
            show.missingText = "(you don't have the evacuation list)";
            dc.options.Add(show);
            dc.options.Add(Option("Lie: \"I'm a salvager. There's nothing down here.\"", "L05.keepersHunt",
                L("Arin", "I'm a salvager. I go where the water goes. There's nothing down here.", 4f),
                L("Mother Ondine Halden", "A salvager who hums the nursery song. I don't think so.", 4f),
                L("Mother Ondine Halden", "Keepers! Lanterns up. Bring me the one who remembers.", 4f)));
            dc.options.Add(Option("\"Your father erased these families.\"", "L05.keepersHunt",
                L("Arin", "Founder Halden struck these families from the record. You're guarding his silence.", 4.5f),
                L("Mother Ondine Halden", "My father saved this city with silence. You'd drown it with talk.", 4f),
                L("Mother Ondine Halden", "Keepers - take them before the water does.", 3.5f)));
            dc.doneFlag = "L05.ondineDone";

            // Keeper hunters (only if Arin lied or accused): sight-cone lanterns in the PRESENT around the ward office.
            var hunters = StateGroup("Present Keeper Hunters", arrival, TimeMask.Present, false);
            var k1 = Keeper("Keeper Lantern-bearer", hunters, new Vector3(-9f, 0f, 2f), 0f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(-9f, 0f, 2f), new Vector3(-10f, 0f, 13f), new Vector3(-4f, 0f, 9f), new Vector3(-6f, 0f, -3f));
            var k2 = Keeper("Keeper Boatman", hunters, new Vector3(6f, 0f, 6f), 180f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(6f, 0f, 6f), new Vector3(-2f, 0f, 12f), new Vector3(-8f, 0f, 6f), new Vector3(2f, 0f, -4f));
            foreach (var k in new[] { k1, k2 })
            {
                k.alarmFlag = "L05.keepersAlarm";
                k.caughtMessage = "The Keepers dragged you out of the lantern light. The water rose while they held you.";
                k.alarmBarks = new List<string> { "There! The one who hums!", "Anomaly in the ward office!" };
            }
            hunters.gameObject.SetActive(false);
            var hunt = Reactor("Keepers Hunt", logic, "L05.keepersHunt");
            hunt.enableWhenTrue.Add(hunters.gameObject);
            hunt.announce = true;
            hunt.cue = Cue.Alert;
            hunt.reversible = false;
            var pen = logic.gameObject.AddComponent<L05CrisisPenalty>();
            pen.timer = crisisTimer;
        }

        // =====================================================================
        // Exit: the door with no house
        // =====================================================================

        static void BuildNurseryDoor(Transform root, Transform logic, Transform shared)
        {
            var g = Group("Nursery Door (no address)", shared);
            Vector3 p = new Vector3(9f, 0f, 11f);
            Box("PostL", g, p + new Vector3(-0.95f, 1.45f, 0f), new Vector3(0.25f, 2.9f, 0.3f), "wood_dark");
            Box("PostR", g, p + new Vector3(0.95f, 1.45f, 0f), new Vector3(0.25f, 2.9f, 0.3f), "wood_dark");
            Box("Lintel", g, p + new Vector3(0f, 3.0f, 0f), new Vector3(2.15f, 0.3f, 0.3f), "wood_dark");
            Box("Step", g, p + new Vector3(0f, 0.08f, -0.3f), new Vector3(2.0f, 0.16f, 0.6f), "stone_warm");
            Label("NoAddress", g, p + new Vector3(0f, 3.5f, -0.2f), 0f, "NO ADDRESS", 0.24f, new Color(1f, 0.75f, 0.95f));
            Label("Seal", g, p + new Vector3(0f, 2.55f, -0.2f), 0f, "(0)", 0.35f, Color.white);
            // The impossible nursery glimpsed through the door (a small lit room that is not part of any house).
            var room = Group("NurseryGlimpse", g);
            Box("BackWall", room, p + new Vector3(0f, 1.45f, 1.9f), new Vector3(2.2f, 2.9f, 0.1f), "paper");
            Box("SideL", room, p + new Vector3(-1.1f, 1.45f, 0.95f), new Vector3(0.1f, 2.9f, 1.9f), "paper");
            Box("SideR", room, p + new Vector3(1.1f, 1.45f, 0.95f), new Vector3(0.1f, 2.9f, 1.9f), "paper");
            Box("Roof", room, p + new Vector3(0f, 2.95f, 0.95f), new Vector3(2.3f, 0.1f, 2.0f), "wood_dark");
            Box("Cot", room, p + new Vector3(0.4f, 0.45f, 1.4f), new Vector3(1.0f, 0.5f, 0.6f), "wood");
            var mobile = Hologram("Mobile", room, p + new Vector3(0.4f, 2.3f, 1.4f), new Vector3(0.3f, 0.3f, 0.3f), PrimitiveType.Cube, "hologram_magenta", true, true);
            PointLight("NurseryGlow", room, p + new Vector3(0f, 2.2f, 1.0f), new Color(1f, 0.55f, 0.9f), 2.6f, 6f);

            // The door panel: shut until the district's last choice is made.
            var panelGo = Box("DoorPanel", g, p + new Vector3(0f, 1.3f, 0f), new Vector3(1.65f, 2.6f, 0.12f), "plaster_warm");
            NotStatic(panelGo);
            var portal = Hologram("OpenSeam", g, p + new Vector3(0f, 1.3f, 0.02f), new Vector3(1.6f, 2.55f, 0.02f), PrimitiveType.Cube, "hologram_magenta", false, false);
            portal.SetActive(false);
            var open = Reactor("Nursery Door Opens", logic, F_Choice);
            open.disableWhenTrue.Add(panelGo);
            open.enableWhenTrue.Add(portal);
            open.announce = true;
            open.cue = Cue.Revelation;
            open.toast = "A door with no house has opened in the north-east corner of the square.";
            open.reversible = false;

            Exit("NurseryExit", logic, p + new Vector3(0f, 0f, 0.4f), new Vector3(2.0f, 2.6f, 2.4f),
                "The door with no house is shut. It opens only after the district's families are remembered, Maren is safe, and you have chosen what to save.",
                F_Rooms, F_Child, F_Choice);
        }

        // =====================================================================
        // Named NPCs + background life
        // =====================================================================

        static void BuildNpcs(Transform root, Transform past, Transform present, Transform future)
        {
            var pastPeople = StateGroup("Past Residents", root, TimeMask.Past, false);
            var presPeople = StateGroup("Present Residents", root, TimeMask.Present, false);
            var futPeople = StateGroup("Future Echoes", root, TimeMask.Future, false);

            // ---- Edda Oskel (PAST): Maren's mother, doing the festival laundry at the pump ----
            var edda = Npc("Edda Oskel", pastPeople, new Vector3(-3.4f, 0f, -4.6f), 90f, "coat_burgundy", true, 1, "coat_white");
            Route(edda, false,
                (new Vector3(-2.4f, 0f, -4.4f), 5f, NPCActivity.Work, "This tub won't fill itself."),
                (new Vector3(-8f, 0f, 3f), 4f, NPCActivity.Carry, ""),
                (new Vector3(12.6f, 0f, 0.4f), 3f, NPCActivity.Look, "Ours is the Oskel house, down the east lane."));
            edda.changedFlag = F_Pump;
            Route(edda, true,
                (new Vector3(-1.9f, 0f, -3.9f), 6f, NPCActivity.Work, "Clean water! Smells like rain on brass."),
                (new Vector3(-8f, 0f, 3f), 4f, NPCActivity.Carry, ""),
                (new Vector3(-3.6f, 0f, -5.2f), 4f, NPCActivity.Work, ""));
            var pe = edda.GetComponent<NPCPersona>();
            pe.task = "Washing the family's festival linens for tomorrow's Bell Day at the communal pump - which has been dry since its handle went missing.";
            pe.approachBarks.AddRange(new[] { "Mind the tub, stranger.", "You're not from the basin, are you?" });
            pe.talkLines.Add(L("Edda Oskel", "Bell Day tomorrow and not a drop from the pump. Tobin says the handle walked off."));
            pe.talkLines.Add(L("Edda Oskel", "The Corran boys had it for their chalk game, up the north lane. I'd bet my good linen on it."));
            pe.talkLines.Add(L("Edda Oskel", "We're all going up to the tower at first light. The Founders' men came round with the list."));
            pe.talkLines.Add(L("Edda Oskel", "They said not to talk about it. Said it would only frighten people who aren't on the list."));
            pe.talkLines.Add(L("Edda Oskel", "My Maren is seven. She thinks it's a festival trip. I let her think so."));
            pe.talkLines.Add(L("Arin", "I'll find the handle."));
            pe.conditional.Add(new ConditionalLines { whenHolding = K_Handle, lines = { L("Edda Oskel", "That's it - that's the handle!"), L("Edda Oskel", "Seat it in the yoke on top of the column. Tobin will weep, I swear.") } });
            pe.conditional.Add(new ConditionalLines { whenFlag = F_RoomOskel, lines = { L("Edda Oskel", "You've been in our house? Then you saw the calendar."), L("Edda Oskel", "I cut the little zero for Maren. If we're parted, she's to wait where the water can't follow.") } });
            pe.changedLines.Add(L("Edda Oskel", "The water's sweet again. Clean linen for the tower tomorrow."));
            pe.changedLines.Add(L("Edda Oskel", "Every family on the list goes up at first light. Even the babies. The bell will carry us."));

            // ---- Tobin Ash (PAST): the pump mender ----
            var tobin = Npc("Tobin Ash", pastPeople, new Vector3(1.8f, 0f, -0.6f), -90f, "coat_olive", true, 2, "iron_rust");
            Route(tobin, false,
                (new Vector3(1.7f, 0f, -1.0f), 5f, NPCActivity.Work, "Yoke's sound. Spindle's sound. No handle."),
                (new Vector3(6.2f, 0f, -7.9f), 4f, NPCActivity.Work, "Spare pins, spare washers... no spare handle."),
                (new Vector3(0.4f, 0f, 13.4f), 4f, NPCActivity.Look, "Where did those boys put it?"));
            tobin.changedFlag = F_Pump;
            Route(tobin, true,
                (new Vector3(1.7f, 0f, -1.0f), 6f, NPCActivity.Work, "Listen to her draw. Sweetest pump in Veyra."),
                (new Vector3(5.6f, 0f, -6.4f), 4f, NPCActivity.Idle, ""),
                (new Vector3(-1.2f, 0f, -7f), 4f, NPCActivity.Look, "Keep the sump open and the basin stays dry. Simple."));
            var pt = tobin.GetComponent<NPCPersona>();
            pt.task = "Pump mender of Old Veyra: keeping the communal pump - and the sump beneath it that keeps the basin dry - working before the festival.";
            pt.approachBarks.AddRange(new[] { "Careful, that plinth is slick.", "If you're here about the pump - I know, I know." });
            pt.talkLines.Add(L("Tobin Ash", "No pump, no sump. No sump, and the basin floods every spring."));
            pt.talkLines.Add(L("Tobin Ash", "The handle's iron, about so long. The children took it to play. Find it and I owe you a pint."));
            pt.talkLines.Add(L("Tobin Ash", "Founders' surveyors were down here last week, measuring the sump. Wouldn't say what for."));
            pt.talkLines.Add(L("Tobin Ash", "Half the basin's on some list for Bell Day. Not me. Somebody has to keep the pump turning."));
            pt.talkLines.Add(L("Tobin Ash", "Forty years I've mended this thing. It's never once let the water win."));
            pt.conditional.Add(new ConditionalLines { whenHolding = K_Handle, lines = { L("Tobin Ash", "Bless you. Set it in the yoke - the bracket at the top of the column."), L("Tobin Ash", "Pump keeps the sump open, the sump keeps the basin dry. Forever, if we're careful.") } });
            pt.conditional.Add(new ConditionalLines { whenFlag = F_Redirected, lines = { L("Tobin Ash", "You've a look about you. Like you've seen the lanes under water."), L("Tobin Ash", "Don't tell me. If the sluices ever have to be turned, turn all three. Never just one.") } });
            pt.changedLines.Add(L("Tobin Ash", "She draws clean. Hear that? This basin will never drown while that pump turns."));
            pt.changedLines.Add(L("Arin", "(It will drown. But not as deep - not anymore.)"));

            // ---- Hesk Lowry (PRESENT): the old neighbour who remembers ----
            var hesk = Npc("Hesk Lowry", presPeople, new Vector3(-6f, 3f, -25.6f), 0f, "coat_grey", true, 3, "fabric_teal");
            Route(hesk, false,
                (new Vector3(-6f, 3f, -25.0f), 5f, NPCActivity.Look, "Water's climbing again."),
                (new Vector3(8f, 3f, -30.4f), 5f, NPCActivity.Work, "Lamp oil, rope, a dry blanket. Always ready."),
                (new Vector3(-11f, 3f, -33f), 4f, NPCActivity.Carry, "Nothing left down there to save. Is there?"));
            hesk.changedFlag = F_Child;
            Route(hesk, true,
                (new Vector3(1.0f, 3f, -27.2f), 6f, NPCActivity.Talk, "There, little one. The blanket's dry."),
                (new Vector3(-1.5f, 3f, -27.8f), 4f, NPCActivity.Idle, ""),
                (new Vector3(3.6f, 3f, -28.4f), 4f, NPCActivity.Look, "Never thought I'd see an Oskel again."));
            var ph = hesk.GetComponent<NPCPersona>();
            ph.task = "The basin's last resident. Keeps a rescue lamp, a rope and a dry blanket on the Weir Terrace, watching the flood line.";
            ph.approachBarks.AddRange(new[] { "Mind the rail. Slick as glass.", "Another one come to stare at the drowned district?" });
            ph.talkLines.Add(L("Hesk Lowry", "Thirty years I've watched this basin. Families lived down there - I'm sure of it. Nobody else is."));
            ph.talkLines.Add(L("Hesk Lowry", "Their names were struck from the public record. Door plates scraped bare. As if they never were."));
            ph.talkLines.Add(L("Hesk Lowry", "The order came from the tower. Founder Halden's seal on it. Nobody argued with a Founder."));
            ph.talkLines.Add(L("Hesk Lowry", "Some nights, when the rain stops, I hear a child calling from the laundry."));
            ph.talkLines.Add(L("Hesk Lowry", "The old pump used to keep the sump open. Nobody's mended it since... since before I can remember."));
            ph.conditional.Add(new ConditionalLines { whenFlag = F_Child, lines = { L("Hesk Lowry", "You brought her up. An Oskel child, alive."), L("Hesk Lowry", "Go. Whatever you came to the basin for - finish it.") } });
            ph.conditional.Add(new ConditionalLines { whenFlag = F_Rooms, lines = { L("Hesk Lowry", "Tomorrow? On every calendar?"), L("Hesk Lowry", "I had one too. I marked it. I can't for the life of me remember why.") } });
            ph.conditional.Add(new ConditionalLines { whenFlag = "L05.haldenOrder", lines = { L("Hesk Lowry", "So you found Halden's notice. Forty-one households, struck out in one stroke."), L("Hesk Lowry", "They weren't criminals. They were on a list. That's all anybody would say.") } });
            ph.conditional.Add(new ConditionalLines { whenFlag = F_Pump, lines = { L("Hesk Lowry", "The water dropped. Just like that - as if it had never been so high."), L("Hesk Lowry", "If you're going down, the sluice house wheels send the lanes to the culvert.") } });
            ph.conditional.Insert(1, new ConditionalLines { whenFlag = "L05.keepersHunt", lines = { L("Hesk Lowry", "Keeper lanterns in the basin. Ondine's people."), L("Hesk Lowry", "Keep to the dark water and shift away from them if you can. They only walk the PRESENT.") } });
            ph.changedLines.Add(L("Hesk Lowry", "You brought her up. An Oskel child, alive."));
            ph.changedLines.Add(L("Hesk Lowry", "Go. Whatever you came to the basin for - finish it."));

            // ---- Maren Oskel, age 7 (PRESENT): the trapped child (motor built with the laundry) ----
            Route(pell, false,
                (new Vector3(27.8f, 0f, -14.2f), 5f, NPCActivity.Work, "Keep the lantern lit. Keep it lit."),
                (new Vector3(28.4f, 0f, -10.4f), 4f, NPCActivity.Look, "Mama said wait where the water can't follow..."),
                (new Vector3(25.6f, 0f, -12f), 3f, NPCActivity.Idle, "Hello? HELLO?"));
            pell.changedFlag = F_Child;
            Route(pell, true,
                (new Vector3(1.9f, 3f, -26.8f), 6f, NPCActivity.Look, "It's so high up here. The water can't follow."),
                (new Vector3(0.2f, 3f, -28.6f), 4f, NPCActivity.Idle, ""),
                (new Vector3(2.6f, 3f, -28.2f), 4f, NPCActivity.Talk, ""));
            pell.conversePartner = hesk.transform;
            var pp = pell.GetComponent<NPCPersona>();
            pp.task = "Seven years old, trapped in the old laundry. Keeping her family's lantern lit and waiting, as her mother told her, 'where the water cannot follow'.";
            pp.approachBarks.AddRange(new[] { "Is someone there? Please!", "Don't let the water in!" });
            pp.talkLines.Add(L("Maren Oskel", "I don't go with strangers. Mama hums when I'm scared. Only the right people know the song."));
            pp.talkLines.Add(L("Maren Oskel", "Mama said wait where the water can't follow. So I waited."));
            pp.talkLines.Add(L("Maren Oskel", "Everyone went to the tower for Bell Day. They said they'd come back for me."));
            pp.talkLines.Add(L("Maren Oskel", "I'm seven. I can count to a hundred. I counted the drips. Lots of hundreds."));
            pp.talkLines.Add(L("Maren Oskel", "I left my music box in the ward office. It plays Mama's song, but one note is always missing."));
            pp.talkLines.Add(L("Maren Oskel", "Are you a grown-up from the tower? You look like you're from somewhere else."));
            pp.conditional.Add(new ConditionalLines { whenFlag = F_Child, lines = { L("Maren Oskel", "It's so high up here. The water can't follow."), L("Maren Oskel", "Will you find Mama? She was going to the tower. I think she's still going.") } });
            pp.conditional.Add(new ConditionalLines { whenFlag = F_RoomOskel, lines = { L("Maren Oskel", "You saw our calendar? Mama cut the little zero for tomorrow."), L("Maren Oskel", "She said tomorrow the bell would carry us somewhere safe. Somewhere with no address.") } });
            pp.conditional.Add(new ConditionalLines { whenFlag = F_Keepsake, lines = { L("Maren Oskel", "My music box! You brought it!"), L("Maren Oskel", "Listen - it still skips the note. Mama says we'll find it, one day, in the right room.") } });
            pp.conditional.Add(new ConditionalLines { whenFlag = "maren.recognised", lines = { L("Maren Oskel", "You're the one who hums. You hummed it in the nursery, when the lights went out."), L("Maren Oskel", "You had a watch that didn't tick. You said it was waiting.") } });
            pp.changedLines.Add(L("Maren Oskel", "It's so high up here. The water can't follow."));
            pp.changedLines.Add(L("Maren Oskel", "Will you find Mama? She was going to the tower. I think she's still going."));
            // Changed state: once a choice is made, her lines reflect it (keepsake first, then the default changed lines).
            pp.conditional.Insert(0, pp.conditional[2]);
            pp.conditional.RemoveAt(3);

            // Background life.
            Crowd("Festival Crowd", pastPeople, new Vector3(0f, 0f, -9f), new Vector2(20f, 12f), 5, 5050,
                new[] { "Did you hear that?", "Bell Day tomorrow - first light!", "Funny, I thought I heard water." }, "fabric_gold");
            Crowd("Salvage Crew", presPeople, new Vector3(10f, 3f, -31.5f), new Vector2(8f, 4f), 2, 6060,
                new[] { "The basin just dropped!", "Rope's wet through." }, "iron_rust");
            var echoes = Crowd("Echo Residents", futPeople, new Vector3(0f, 0f, -7f), new Vector2(18f, 14f), 3, 7070,
                new[] { "...first light...", "...the bell will carry us..." }, "wood");
            foreach (var e in echoes) MakeHolo(e, "hologram");
        }

        // =====================================================================
        // Dressing per state
        // =====================================================================

        static void BuildDressing(Transform shared, Transform past, Transform present, Transform future)
        {
            // ---------------- PAST: golden festival eve ----------------
            Vector3[] gas = { new Vector3(-11f, 0f, -18f), new Vector3(11f, 0f, -18f), new Vector3(-11f, 0f, -4f), new Vector3(11f, 0f, -4f), new Vector3(-11f, 0f, 10f), new Vector3(11f, 0f, 10f) };
            for (int i = 0; i < gas.Length; i++) { var lp = Lamp("GasLamp" + i, past, gas[i], "gas"); if (i % 2 == 1) Object.DestroyImmediate(lp.Find("Light").gameObject); }
            Bunting(past, gas[0] + Vector3.up * 4f, gas[3] + Vector3.up * 4f, 8, 0);
            Bunting(past, gas[1] + Vector3.up * 4f, gas[2] + Vector3.up * 4f, 8, 1);
            Bunting(past, gas[2] + Vector3.up * 4f, gas[5] + Vector3.up * 4f, 8, 2);
                        // Festival banner: the clue made public in the PAST.
            Cylinder("BannerPoleL", past, new Vector3(-7f, 3f, 8f), 0.08f, 6f, "brass");
            Cylinder("BannerPoleR", past, new Vector3(7f, 3f, 8f), 0.08f, 6f, "brass");
            Box("Banner", past, new Vector3(0f, 5.2f, 8f), new Vector3(13.6f, 1.2f, 0.05f), "fabric_red", false);
            Label("BannerText", past, new Vector3(0f, 5.2f, 7.95f), 0f, "BELL DAY - TOMORROW AT FIRST LIGHT", 0.42f, new Color(1f, 0.85f, 0.4f));
            // Stalls.
            for (int i = 0; i < 3; i++)
            {
                Vector3 s = new Vector3(-9.5f, 0f, -12f + i * 5f);
                Box("StallTable" + i, past, s + Vector3.up * 0.45f, new Vector3(1.4f, 0.9f, 2.4f), "wood");
                TiltBox("StallAwning" + i, past, s + Vector3.up * 2.4f, new Vector3(1.8f, 0.05f, 2.8f), i % 2 == 0 ? "fabric_red" : "fabric_teal", new Vector3(0f, 0f, -12f));
                Box("Goods" + i, past, s + Vector3.up * 1.0f, new Vector3(1.0f, 0.2f, 1.8f), i == 1 ? "fabric_gold" : "paper", false);
            }
            Box("TobinBench", past, new Vector3(6.6f, 0.45f, -8.8f), new Vector3(1.8f, 0.9f, 0.8f), "wood_dark");
            Box("Tools", past, new Vector3(6.6f, 0.95f, -8.8f), new Vector3(1.2f, 0.1f, 0.4f), "brass", false);
            Cylinder("WashTub", past, new Vector3(-2.4f, 0.3f, -5.2f), 0.5f, 0.6f, "wood");
            // Washing lines across the W / E lanes.
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -20f : 20f;
                Box("WashLine" + i, past, new Vector3(x, 3.2f, 0f), new Vector3(0.02f, 0.02f, 4f), "wood_dark", false);
                for (int k = 0; k < 3; k++) Box("Washing", past, new Vector3(x, 2.75f, -1.2f + k * 1.2f), new Vector3(0.02f, 0.8f, 0.8f), k == 1 ? "fabric_teal" : "coat_white", false);
            }
            var dust = Particles("Dust", past, new Vector3(0f, 3f, -4f), new Vector3(30f, 6f, 40f), "dust");

            // ---------------- PRESENT: rain on a drowned ward ----------------
            Vector3[] sod = { new Vector3(-11f, 0f, -14f), new Vector3(11f, 0f, -14f), new Vector3(-11f, 0f, 6f), new Vector3(11f, 0f, 6f), new Vector3(-8f, 3f, -25f), new Vector3(8f, 3f, -25f) };
            for (int i = 0; i < sod.Length; i++) { var lp = Lamp("SodiumLamp" + i, present, sod[i], "sodium", i >= 4 ? 3.6f : 4.6f); if (i == 1 || i == 2 || i == 5) { Object.DestroyImmediate(lp.GetComponent<NeonFlicker>()); Object.DestroyImmediate(lp.Find("Light").gameObject); } }
            Particles("Rain", present, new Vector3(0f, 22f, -6f), new Vector3(72f, 1f, 72f), "rain");
            Particles("FloodMist", present, new Vector3(0f, 1.2f, -2f), new Vector3(28f, 2f, 36f), "mist");
            Label("FloodLineW", present, new Vector3(-13.75f, 2.25f, -18f), 90f, "- FLOOD LINE 2.2 M -", 0.18f, TextCold);
            Label("FloodLineE", present, new Vector3(13.75f, 2.25f, -18f), -90f, "- FLOOD LINE 2.2 M -", 0.18f, TextCold);
            // Terrace: Hesk's rescue kit.
            TiltBox("UpturnedSkiff", present, new Vector3(9.5f, 3.35f, -31.5f), new Vector3(1.4f, 0.6f, 3.6f), "wood_dark", new Vector3(0f, 20f, 180f), true);
            Box("Rope", present, new Vector3(7.6f, 3.1f, -30.8f), new Vector3(0.6f, 0.2f, 0.6f), "coat_ochre", false);
            Box("Blanket", present, new Vector3(1.2f, 3.06f, -27.6f), new Vector3(1.2f, 0.06f, 0.9f), "fabric_teal", false);
            for (int i = 0; i < 6; i++)
                Box("Puddle", present, new Vector3(-14f + i * 5.3f, 3.01f, -29f + (i % 3) * 2.2f), new Vector3(2.2f, 0.01f, 1.4f), "water_clear", false);
            for (int i = 0; i < 6; i++)
            {
                float x = -12f + i * 4.8f;
                TiltBox("Boards", present, new Vector3(x, 5.5f, 16.05f), new Vector3(1.2f, 1.4f, 0.06f), "wood_dark", new Vector3(0f, 0f, i * 7f - 15f));
            }

            // ---------------- FUTURE: split-open crystal crater ----------------
            Vector3[] neon = { new Vector3(-11f, 0f, -10f), new Vector3(11f, 0f, -10f), new Vector3(-11f, 0f, 8f), new Vector3(11f, 0f, 8f) };
            for (int i = 0; i < neon.Length; i++) { var lp = Lamp("NeonLamp" + i, future, neon[i], "neon", 5f); if (i % 2 == 1) { Object.DestroyImmediate(lp.GetComponent<NeonFlicker>()); Object.DestroyImmediate(lp.Find("Light").gameObject); } }
            Vector3[] cr = { new Vector3(-8f, 0f, -16f), new Vector3(7f, 0f, -14f), new Vector3(-6f, 0f, 4f), new Vector3(6.5f, 0f, 2f), new Vector3(-12f, 0f, 13f), new Vector3(4f, 0f, 13.5f), new Vector3(12.5f, 0f, -6f), new Vector3(-12.5f, 0f, -8f) };
            for (int i = 0; i < cr.Length; i++) Crystals("SquareCrystal" + i, future, cr[i], 0.9f + (i % 3) * 0.35f, 800 + i);
            // Glowing fracture seams radiating from the pump.
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 10f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                var seam = Box("FractureSeam" + i, future, new Vector3(0f, 0.05f, -2f) + dir * 7f, new Vector3(0.08f, 0.04f, 8f), i % 2 == 0 ? "neon_violet" : "neon_cyan", false);
                seam.transform.rotation = Quaternion.Euler(0f, a, 0f);
            }
            // Neon trims along the terraces facing the square.
            Box("TrimW", future, new Vector3(-13.85f, 7.0f, -4f), new Vector3(0.08f, 0.08f, 40f), "neon_cyan", false);
            Box("TrimE", future, new Vector3(13.85f, 7.0f, -4f), new Vector3(0.08f, 0.08f, 40f), "neon_magenta", false);
            Box("TrimN", future, new Vector3(0f, 9.0f, 15.95f), new Vector3(28f, 0.08f, 0.08f), "neon_violet", false);
            // Floating fractured debris.
            var rng = new System.Random(8080);
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = new Vector3(((float)rng.NextDouble() - 0.5f) * 30f, 5f + (float)rng.NextDouble() * 8f, -4f + ((float)rng.NextDouble() - 0.5f) * 40f);
                var chunk = TiltBox("FloatingDebris" + i, future, pos, new Vector3(0.6f + (float)rng.NextDouble() * 1.6f, 0.3f + (float)rng.NextDouble() * 0.8f, 0.6f + (float)rng.NextDouble() * 1.4f),
                    i % 4 == 0 ? "obsidian" : "ruin_dark", new Vector3((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 40f));
                NotStatic(chunk);
                chunk.AddComponent<FloatBob>().amplitude = 0.3f + (float)rng.NextDouble() * 0.3f;
                chunk.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 4f + (float)rng.NextDouble() * 10f, 2f);
                if (i % 5 == 0) Box("Glow", chunk.transform.parent, pos + Vector3.down * 0.3f, new Vector3(0.5f, 0.04f, 0.5f), "neon_cyan", false);
            }
            // Holographic signage.
            Hologram("BasinSign", future, new Vector3(0f, 7.5f, -12f), new Vector3(6.4f, 1.1f, 0.02f), PrimitiveType.Cube, "hologram", true, false);
            Label("BasinSignText", future, new Vector3(0f, 7.5f, -12.05f), 0f, "OLD VEYRA BASIN - WARD 0\n3 FAMILIES EXPUNGED", 0.3f, Color.white);
            Hologram("SealRing", future, new Vector3(0f, 9.5f, -2f), new Vector3(3f, 0.02f, 3f), PrimitiveType.Cylinder, "hologram_magenta", true, true).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Particles("DataMotes", future, new Vector3(0f, 4f, -4f), new Vector3(34f, 8f, 44f), "motes");
            Particles("CraterMist", future, new Vector3(0f, 0.8f, -4f), new Vector3(30f, 1.5f, 40f), "mist");
        }

        // =====================================================================
        // Optional side area: the flooded schoolhouse (environmental storytelling)
        // =====================================================================

        static void BuildSchoolhouse(Transform root, Transform logic)
        {
            var g = Group("Schoolhouse (optional)", root);
            Vector3 c = new Vector3(19f, 0f, 9f);
            Room("Shell", g, c, new Vector2(10f, 10f), 4.6f, "brick_warm", null, "wood_dark", 3);
            Box("Floorboards", g, c + Vector3.up * 0.015f, new Vector3(9.6f, 0.03f, 9.6f), "wood", false);
            Label("SchoolSign", g, new Vector3(13.75f, 3.4f, 9f), -90f, "WARD ZERO INFANTS SCHOOL", 0.3f, TextWarm);
            Box("Chalkboard", g, new Vector3(23.7f, 2.0f, 9f), new Vector3(0.08f, 1.6f, 3.6f), "ground_dark");

            // Own flood (PRESENT), tied to the same pump as the basin.
            Flood("Schoolhouse Flood", logic, c, new Vector2(9.6f, 9.6f), new Vector2(9.6f, 9.6f), 2.2f, 0.35f, 1.1f, true, new[] { F_Pump });

            // PAST: lessons on Bell Day eve.
            var past = StateGroup("Past Schoolroom", root, TimeMask.Past, false);
            for (int r = 0; r < 3; r++)
                for (int k = 0; k < 2; k++)
                    Box("Desk", past, new Vector3(16.2f + r * 2f, 0.38f, 6.2f + k * 2.6f), new Vector3(0.9f, 0.76f, 1.4f), "wood");
            Box("TeacherDesk", past, new Vector3(22f, 0.45f, 9f), new Vector3(1f, 0.9f, 2f), "wood_dark");
            Label("ChalkPast", past, new Vector3(23.62f, 2.1f, 9f), 90f, "BELL DAY TOMORROW\nwarm coats - hold hands - no running\ntwo by two up to the tower", 0.16f, Color.white);
            PointLight("SchoolGas", past, c + Vector3.up * 3.8f, Gas, 2.2f, 10f);
            for (int i = 0; i < 1; i++)
            {
                var kid = Npc("Pupil " + (i + 1), past, new Vector3(16.9f + i * 2f, 0f, 7.0f + i * 2.6f), 90f, i == 0 ? "coat_olive" : "coat_navy", false, 900 + i);
                kid.transform.localScale = Vector3.one * 0.7f;
                Route(kid, false, (kid.transform.position, 6f, NPCActivity.Work, ""), (new Vector3(21f, 0f, 7.5f + i * 3f), 3f, NPCActivity.Look, ""), (kid.transform.position, 5f, NPCActivity.Work, ""));
                kid.reactionBarks.Add(i == 0 ? "Miss! The water's back!" : "Is it Bell Day yet?");
            }

            // PRESENT: tumbled desks, the class register.
            var pres = StateGroup("Present Schoolroom", root, TimeMask.Present, true);
            for (int i = 0; i < 6; i++)
                TiltBox("TumbledDesk", pres, new Vector3(16f + (i % 3) * 2.2f, 0.4f, 6f + (i / 3) * 4f), new Vector3(0.9f, 0.76f, 1.4f), "wood_dark", new Vector3(i * 13f, i * 41f, i % 2 == 0 ? 70f : 0f), true);
            Box("TeacherDesk", pres, new Vector3(22f, 0.45f, 9f), new Vector3(1f, 0.9f, 2f), "wood_dark");
            Label("ChalkPresent", pres, new Vector3(23.62f, 2.1f, 9f), 90f, "BELL DAY TOM...\n(the rest washed away)", 0.16f, new Color(0.85f, 0.9f, 0.9f));
            Clue("ClassRegister", pres, new Vector3(22f, 0.95f, 9f), new Vector3(0.5f, 0.06f, 0.36f), "paper", "Read the class register", "L05.classRegister",
                "clue.L05.register", "Class register - Ward Zero Infants",
                "Autumn term. Twelve names in a careful hand:\n\nAsh, Tilde - Corran, Bram - Corran, Wil - Fenn, Ada - Oskel, Maren (7) - Vell, Ilya - and six more.\n\nEvery name has been ruled through in red ink. In the margin, a different hand: 'struck by order - A.H.'\n\nUnder Maren's name someone has written, very small: 'still here?'", true, TimeMask.Present);
            PointLight("SchoolCold", pres, c + Vector3.up * 3.8f, ColdTeal, 1.4f, 10f);

            // FUTURE: crystal-filled, a hologram roll call that counts nobody.
            var fut = StateGroup("Future Schoolroom", root, TimeMask.Future, false);
            Crystals("SchoolCrystalsA", fut, new Vector3(17f, 0f, 12f), 1.1f, 707);
            Crystals("SchoolCrystalsB", fut, new Vector3(21f, 0f, 5.6f), 0.9f, 708);
            Hologram("RollCall", fut, new Vector3(23.3f, 2.2f, 9f), new Vector3(0.02f, 0.8f, 2.4f), PrimitiveType.Cube, "hologram", true, false);
            Label("RollCallText", fut, new Vector3(23.25f, 2.2f, 9f), 90f, "WARD ZERO INFANTS\nPUPILS PRESENT: 0", 0.2f, Color.white);

            Ambient("SchoolDrip", g, c + Vector3.up * 1.5f, AmbientKind.Water, 0.2f, TimeMask.Present, 1f, 9f);
        }

        // =====================================================================
        // Memory fragments (optional, off the main path)
        // =====================================================================

        static void BuildFragments(Transform root)
        {
            var g = Group("Memory Fragments", root);
            Fragment(g, new Vector3(23f, 1.3f, 13f), "L05.1", "A lesson plan",
                "Ward Zero Infants, Bell Day eve. 'Tomorrow we practise lining up quietly. Two by two, holding hands, all the way up to the tower. No one is to be left behind.' Pencilled beside it: Maren O. to hold the lantern.");
            Fragment(g, new Vector3(-16.6f, 4.3f, -35.1f), "L05.2", "A surveyor's note",
                "Founders' Survey, Old Veyra Basin. 'Depth at the sump: six metres. Projected flooding after the event: total. Recommend the listed households be moved before Bell Day - quietly.' Countersigned for Founder A. Halden.");
            var fut = StateGroup("Future Fragment", root, TimeMask.Future, true);
            Crystals("FragmentCrystals", fut, new Vector3(-12.4f, 0f, 13.4f), 1.2f, 990);
            Fragment(fut, new Vector3(-12.4f, 2.6f, 13.4f), "L05.3", "Record 0-3-OSKEL",
                "Archive hologram, corrupted. 'MAREN OSKEL, AGE 7. STATUS: EVACUATED. STATUS: DECEASED. STATUS: - . Conflict unresolved. Flagged for review by O. Fenwick, head archivist.'");
        }

        static void BuildAudio(Transform root)
        {
            var a = Group("Audio", root);
            Ambient("RainTerrace", a, new Vector3(0f, 4f, -30f), AmbientKind.Rain, 0.45f, TimeMask.Present, 1f, 40f);
            Ambient("RainBasin", a, new Vector3(0f, 3f, -2f), AmbientKind.Rain, 0.4f, TimeMask.Present, 1f, 50f);
            Ambient("FloodWater", a, new Vector3(0f, 1f, 0f), AmbientKind.Water, 0.35f, TimeMask.Present, 1f, 40f);
            Ambient("SluiceWater", a, new Vector3(-18f, 1f, -12f), AmbientKind.Water, 0.3f, TimeMask.Present, 1f, 14f);
            Ambient("FestivalCrowd", a, new Vector3(0f, 1.5f, -6f), AmbientKind.Crowd, 0.4f, TimeMask.Past, 1f, 35f);
            Ambient("PumpWorks", a, new Vector3(0f, 1f, -2f), AmbientKind.Machine, 0.15f, TimeMask.Past, 1f, 10f);
            Ambient("LaundryCoppers", a, new Vector3(27f, 1f, -12f), AmbientKind.Machine, 0.2f, TimeMask.Past, 1f, 10f);
            Ambient("CraterWind", a, new Vector3(0f, 6f, -4f), AmbientKind.Wind, 0.35f, TimeMask.Future, 1f, 45f);
            Ambient("CraterDrone", a, new Vector3(0f, 3f, 0f), AmbientKind.FutureDrone, 0.4f, TimeMask.Future, 1f, 50f);
            Ambient("NurseryHum", a, new Vector3(9f, 1.5f, 11.8f), AmbientKind.Nursery, 0.32f, TimeMask.All, 1f, 9f);
        }
    }
}
