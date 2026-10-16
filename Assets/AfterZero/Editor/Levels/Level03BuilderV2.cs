using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// Level 03 design v2: Keepers burning the Present stacks (SpreadingFire put out by routed channels),
    /// Keeper hunters + Sister Vossel, the deduction board, and the Ondine climax (deal / forgery / refuse + chase).
    /// </summary>
    public static partial class Level03Builder
    {
        static void BuildV2(Transform root, Transform shared, Transform logic)
        {
            BuildFire(root, logic);
            BuildKeepers(root);
            BuildDeduction(logic);
            BuildOndine(root, shared, logic);
        }

        // ---------------------------------------------------------------- fire
        static void BuildFire(Transform root, Transform logic)
        {
            var cl = logic.gameObject.AddComponent<L03ChannelFlags>();
            cl.junctions.AddRange(junctionList);

            var g = StateGroup("PresentFire", root, TimeMask.Present, false);
            var fireGo = new GameObject("SpreadingFire");
            fireGo.transform.SetParent(logic, false);
            var fire = fireGo.AddComponent<SpreadingFire>();
            fire.startFlag = "L03.nameReconstructed";
            fire.spreadInterval = 11f;
            fire.telegraphTime = 4f;
            fire.damageRadius = 1.5f;
            fire.lostFlag = "L03.recordsLost";
            fire.allOutFlag = "L03.fireOut";
            fire.lostMessage = "The fire reached the evidence stacks. Route the channels faster: each junction puts out the aisle beside it.";
            var pts = new (Vector3 p, string flag, string tag)[]
            {
                (new Vector3(-12.5f, 0f, 24f), "L03.channel1", "AISLE I"),
                (new Vector3(-9f, 0f, 22.5f), "L03.channel2", "AISLE II"),
                (new Vector3(-9.6f, 0f, 35f), "L03.channel3", "AISLE III"),
                (new Vector3(-4f, 0f, 40.2f), "L03.channel4", "AISLE IV"),
                (new Vector3(-9f, 0f, 40.6f), "L03.waterRouted", "EVIDENCE STACKS"),
            };
            foreach (var t in pts)
            {
                fire.nodes.Add(FireNodeAt(g, t.p, t.flag));
                Box("BookCart", g, t.p + Vector3.up * 0.45f, new Vector3(1.2f, 0.9f, 0.6f), "wood_dark", false);
                Box("Pages", g, t.p + Vector3.up * 0.95f, new Vector3(1f, 0.15f, 0.5f), "paper", false);
                Label("AisleTag", g, t.p + new Vector3(0f, 1.6f, -0.35f), 0f, t.tag, 0.25f, new Color(1f, 0.75f, 0.45f));
            }
            Ambient("FireCrackle", g, new Vector3(-8f, 1f, 32f), AmbientKind.Machine, 0.12f, TimeMask.Present, 1f, 14f);
            var warn = Zone("FireWarning", logic, new Vector3(0f, 1.5f, 24f), new Vector3(34f, 3f, 46f), "L03.fireSeen", "", TimeMask.Present);
            warn.requiredFlags.Add("L03.nameReconstructed");
            warn.lines.Add(L("Arin", "Smoke. The Keepers are burning the stacks - working toward the evidence shelves."));
            warn.lines.Add(L("Arin", "The channels run past every aisle. Route the water and it'll drown the fire."));
        }

        // ---------------------------------------------------------------- keepers
        static void BuildKeepers(Transform root)
        {
            var g = StateGroup("PresentKeepers", root, TimeMask.Present, false);
            var a = KeeperFixed("Keeper", g, new Vector3(-3f, 0f, 6f), 0f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(-3f, 0f, 6f), new Vector3(-3f, 0f, 27f), new Vector3(3f, 0f, 27f), new Vector3(3f, 0f, 6f));
            a.disabledByFlag = "L03.payoff";
            a.spotBarks = new List<string> { "Who's in the stacks?", "Lantern this way." };
            a.caughtMessage = "A Keeper caught you. Stay out of the lantern light - shelves block it, and the PAST hides you.";
            var b = KeeperFixed("Keeper", g, new Vector3(9f, 0f, 8f), 0f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(9f, 0f, 8f), new Vector3(9f, 0f, 33f), new Vector3(13.6f, 0f, 33.5f), new Vector3(10f, 0f, 22f));
            b.disabledByFlag = "L03.payoff";
            b.caughtMessage = a.caughtMessage;

            // Sister Vossel: watcher near the hall entrance, remembers Arin from the festival street (L01).
            var v = KeeperFixed("Sister Vossel", g, new Vector3(-6f, 0f, 3.5f), 90f, KeeperPatrol.Mode.Watcher, TimeMask.Present,
                new Vector3(-6f, 0f, 3.5f), new Vector3(6f, 0f, 3.5f));
            v.disabledByFlag = "L03.payoff";
            v.spotBarks = new List<string> { "The cup-saver. Shifting again, are we?", "I saw that. Mother Ondine will hear of it." };
            var talk = Box("VosselTalk", v.transform, new Vector3(-6f, 1f, 3.5f), new Vector3(0.8f, 1.9f, 0.8f), "coat_grey", true);
            Object.DestroyImmediate(talk.GetComponent<MeshRenderer>());
            Label("NameTag", v.transform, new Vector3(-6f, 2.1f, 3.5f), 0f, "Sister Vossel", 0.25f, new Color(1f, 0.8f, 0.7f));
            var vd = Dialogue(talk, "Sister Vossel", "Sister Vossel blocks the aisle.");
            vd.lockControls = true;
            vd.opening.Add(L("Sister Vossel", "The one from the festival street. You saved a blue cup that was supposed to break."));
            vd.opening.Add(L("Sister Vossel", "Things that should break, Arin, keep the night whole. You keep un-breaking them."));
            vd.options.Add(Option("I'm only here to read.", "L03.vosselRead",
                L("Sister Vossel", "Then read quickly. Paper is very flammable tonight."),
                L("Arin", "Was that a threat?"), L("Sister Vossel", "A forecast.")));
            vd.options.Add(Option("What do the Keepers want?", "L03.vosselAsked",
                L("Sister Vossel", "For tonight to never end. Inside a night that repeats, nobody dies for good."),
                L("Sister Vossel", "Mother Ondine will explain it better. She's on her way.")));
            vd.options.Add(Option("Who signed the dead child's record?", "L03.vosselPressed",
                L("Sister Vossel", "Records are lies the city tells itself. We burn the ones that hurt."),
                L("Arin", "You mean the ones that prove something.")));
            vd.options[2].requiresFlags.Add("L03.ledgerRead");
            vd.options[2].missingText = "(read the register first)";
            vd.doneFlag = "L03.vosselTalked";
        }

        // ---------------------------------------------------------------- deduction
        static void BuildDeduction(Transform logic)
        {
            var rec = recGroup;
            // Clue cards (west of the basin): one per piece of evidence.
            Box("CardStand", rec, new Vector3(-2.7f, 0.5f, 36.5f), new Vector3(0.7f, 1f, 2f), "copper_verdigris");
            var cards = new (string flag, string req, string title, string text)[]
            {
                ("L03.card1", "L03.nameFoundPast", "Clue card 1 - the Past card",
                 "From drawer 05: resident 0417 is SEVEN years old and lives on LANTERN ROW."),
                ("L03.card2", "L03.stampMatched", "Clue card 2 - the Present stamp",
                 "From the transit bureau: the genuine ticket carries the Line 4 mark  <V>  - no other mark."),
                ("L03.card3", "L03.nameReconstructed", "Clue card 3 - the Future traces",
                 "From the mineral traces: the erased name is spelled  M-A-R-E-N  O-S-K-E-L.  Not Marin. Not Oskal."),
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i];
                var fi = Use("ClueCard" + (i + 1), rec, new Vector3(-2.6f, 1.05f, 35.85f + i * 0.65f), new Vector3(0.45f, 0.06f, 0.5f), "paper", "Read clue card " + (i + 1), c.flag, TimeMask.Present);
                fi.oneShot = false;
                fi.clueId = "L03.cardclue" + (i + 1); fi.clueTitle = c.title; fi.clueText = c.text; fi.showAsDocument = true;
                fi.requiredFlags.Add(c.req);
                fi.lockedMessage = "This card is blank - you haven't found that piece of evidence yet.";
                Label("CardNum", rec, new Vector3(-2.6f, 1.25f, 35.85f + i * 0.65f), 90f, (i + 1).ToString(), 0.3f, Color.white);
            }
            Label("CardsTitle", rec, new Vector3(-2.75f, 1.9f, 36.5f), 90f, "CLUE CARDS", 0.3f, new Color(1f, 0.85f, 0.55f));

            // The board with five near-identical records (east of the basin).
            var boardGo = Prim(PrimitiveType.Cube, "RecordBoard", rec, new Vector3(3f, 0.55f, 36.5f), new Vector3(0.8f, 1.1f, 2.2f), "copper_verdigris", true, false);
            var panel = Box("RecordPanel", rec, new Vector3(3.45f, 2.1f, 36.5f), new Vector3(0.05f, 1.6f, 3.2f), "wood_dark", false);
            string[] cand =
            {
                "A   MAREN OSKEL   age 7   Lantern Row    mark <O>",
                "B   MARIN OSKEL   age 7   Lantern Row    mark <V>",
                "C   MAREN OSKEL   age 7   Lantern Row    mark <V>",
                "D   MAREN OSKEL   age 9   Lantern Row    mark <V>",
                "E   MAREN OSKAL   age 7   Weavers Lane   mark <V>",
            };
            Label("RecordList", rec, new Vector3(3.4f, 2.1f, 36.5f), 90f, "FIVE RECORDS, ONE REAL\n\n" + string.Join("\n", cand), 0.17f, new Color(0.95f, 0.9f, 0.75f));
            for (int i = 0; i < 5; i++)
                Box("Slip", rec, new Vector3(3f, 1.12f, 35.7f + i * 0.4f), new Vector3(0.45f, 0.02f, 0.32f), "paper", false);
            var board = boardGo.AddComponent<DeductionBoard>();
            board.prompt = "Study the five records";
            board.usableIn = TimeMask.Present;
            board.question = "Which record is Maren's?";
            board.candidates.AddRange(cand);
            board.correctIndex = 2;
            board.solvedFlag = "L03.recordPicked";
            board.requiredFlags.AddRange(new[] { "L03.card1", "L03.card2", "L03.card3" });
            board.lockedMessage = "Lay out all three clue cards first (west side of the basin).";
            board.resetFlagsOnHard.AddRange(new[] { "L03.card1", "L03.card2", "L03.card3" });
            board.wrongMessage = "Wrong record - and the clatter carried through the Hall.";
            board.alarmOnWrong = true;
            board.solvedLines.Add(L("Arin", "Seven. Lantern Row. The V mark. Spelled with an E. This one."));

            // The real record appears once picked.
            var real = Group("RealRecord", rec);
            var record = Clue("ContradictoryRecord", real, new Vector3(3f, 1.2f, 36.5f), new Vector3(0.5f, 0.05f, 0.45f), "paper", "Read Maren's record", "L03.payoff", "L03.record",
                "The contradictory record",
                "CERTIFICATE OF DEATH - Resident No. 0417, MAREN OSKEL, aged 7, Lantern Row.\nCause: 'the second bell'.\nStamped across it: TRANSFERRED TO THE NURSERY REGISTER.\n\nCertified: O. Fenwick, Head Archivist.\nCountersigned by the witness of record:\n\n        ARIN\n\nPinned to it, in Odile's hand: 'Hidden in the survey basin. She is not dead. I will not let them file her twice.'", true, TimeMask.Present);
            record.lines.Add(L("Arin", "Dead and transferred. On the same page. And that's my signature."));
            record.lines.Add(L("The Witness", "Some names are hidden to keep them safe. Follow the water down, Arin."));
            real.gameObject.SetActive(false);
            var rr = Reactor("RecordPicked", logic, "L03.recordPicked");
            rr.enableWhenTrue.Add(real.gameObject);
            rr.announce = true; rr.cue = Cue.PuzzleSolved;
        }

        // ---------------------------------------------------------------- climax
        static void BuildOndine(Transform root, Transform shared, Transform logic)
        {
            Checkpoint("CP_Climax", root, new Vector3(-1.5f, 0f, 33.2f), new Vector3(4f, 3f, 2f), 0f).climax = true;

            // Ondine arrives a few seconds after the record is read (after the revelation).
            var arrive = logic.gameObject.AddComponent<L03DelayedFlag>();
            arrive.whenFlag = "L03.payoff"; arrive.delay = 7f; arrive.setFlag = "L03.ondineArrives"; arrive.cue = Cue.Alert;
            arrive.toast = "Footsteps on the wet stone. Lanterns at the basin.";

            var people = StateGroup("OndineGroup", root, TimeMask.Present, false);
            var ond = Npc("Mother Ondine Halden", people, new Vector3(-1.6f, 0f, 31.5f), 20f, "coat_white", true, 21);
            Route(ond, false,
                (new Vector3(-1.4f, 0f, 34f), 25f, NPCActivity.Look, "There you are."),
                (new Vector3(-3.5f, 0f, 33f), 6f, NPCActivity.Idle, "Paper remembers too much."),
                (new Vector3(-1.4f, 0f, 34f), 10f, NPCActivity.Look, ""));
            ond.changedFlag = "L03.ondineDone";
            Route(ond, true,
                (new Vector3(-2f, 0f, 20f), 3f, NPCActivity.Walk, "Seal it. All of it."),
                (new Vector3(0f, 0f, 4f), 4f, NPCActivity.Look, ""),
                (new Vector3(0f, 0f, -6f), 999f, NPCActivity.Idle, ""));
            var po = ond.GetComponent<NPCPersona>();
            po.task = "Mother of the Keepers of the Hour: came to burn the record that proves the night can be broken.";
            po.approachBarks.AddRange(new[] { "Careful, Arin. Water conducts more than sound.", "My father kept lists. I keep nights." });
            po.talkLines.Add(L("Mother Ondine Halden", "My father, Founder Halden, chose who would be saved. He chose silence. Silence worked."));
            po.talkLines.Add(L("Mother Ondine Halden", "Inside the loop no one dies for good. The baker, the bell ringer, the children. Every morning, alive."));
            po.talkLines.Add(L("Mother Ondine Halden", "You call it a cage. I call it the only mercy this city ever got."));
            po.talkLines.Add(L("Mother Ondine Halden", "Odile wants the names published. Do you know how many people that would kill at dawn?"));
            po.talkLines.Add(L("Mother Ondine Halden", "Sister Vossel says you un-break things. That's very kind. It's also how the world ends."));
            po.conditional.Add(new ConditionalLines { whenFlag = "L03.dealAccepted", lines = { L("Mother Ondine Halden", "You chose well. You'll wake tomorrow, and the day after, and nobody you love will be gone.") } });
            po.conditional.Add(new ConditionalLines { whenFlag = "evidence.record", lines = { L("Mother Ondine Halden", "Keep your paper, then. Paper burns, Arin. So do archives."), L("Mother Ondine Halden", "We'll meet again in the water.") } });
            po.changedLines.Add(L("Mother Ondine Halden", "We're done here. The Hall will keep what it keeps."));
            ond.gameObject.SetActive(false);
            var show = Reactor("OndineAppears", logic, "L03.ondineArrives");
            show.enableWhenTrue.Add(ond.gameObject);

            // The dialogue scene (flag-triggered, controls locked).
            var dGo = new GameObject("OndineDialogue");
            dGo.transform.SetParent(logic, false);
            var dlg = Dialogue(dGo, "Mother Ondine Halden", "Mother Ondine holds out her hand for the record.", "L03.ondineArrives");
            dlg.opening.Add(L("Mother Ondine Halden", "So you're the anomaly Sister Vossel keeps losing on the festival street.", 4f));
            dlg.opening.Add(L("Mother Ondine Halden", "My father signed lists to keep this city alive. I keep the night repeating for the same reason.", 4.5f));
            dlg.opening.Add(L("Mother Ondine Halden", "Give me that record and the loop will keep you safe. Forever. Nobody inside it dies for good.", 4.5f));
            dlg.options.Add(Option("Hand over the record.", "L03.dealAccepted",
                L("Mother Ondine Halden", "Wise. Sleep well, Arin. You'll wake up here again."),
                L("Arin", "...I already have. Haven't I.")));
            var forged = Option("Give her Odile's forged copy instead.", "L03.decoyGiven",
                L("Mother Ondine Halden", "Good. Sister - burn it."),
                L("Arin", "Odile's forgery. Walk to the hatch. Don't run. Not yet."));
            forged.requiresFlags.Add("L03.forgedCopy");
            forged.missingText = "(you have nothing to swap)";
            dlg.options.Add(forged);
            dlg.options.Add(Option("Refuse.", "L03.refused",
                L("Mother Ondine Halden", "Then the Hall keeps you instead. Seal the hatch! Open the cisterns!"),
                L("Arin", "The hatch release - Odile's office. Through the east door. Go!")));
            dlg.doneFlag = "L03.ondineDone";

            // Outcome wiring.
            Chain(logic, "DealOpensHatch", "L03.dealAccepted", "L03.hatchOpen");
            Chain(logic, "DecoyKeepsRecord", "L03.decoyGiven", "evidence.record");
            Chain(logic, "DecoyOpensHatch", "L03.decoyGiven", "L03.hatchOpen");
            Chain(logic, "RefuseKeepsRecord", "L03.refused", "evidence.record");
            Chain(logic, "RefuseStartsChase", "L03.refused", "L03.chase");
            Chain(logic, "ValveOpensHatch", "L03.hatchReleased", "L03.hatchOpen");
            var discover = logic.gameObject.AddComponent<L03DelayedFlag>();
            discover.whenFlag = "L03.decoyGiven"; discover.delay = 9f; discover.setFlag = "L03.chase";
            discover.toast = "Mother Ondine: 'This is a forgery! After them!'";

            // Sealed hatch (refuse path) + release valve in the Restricted Stacks.
            var seal = Group("HatchSeal", shared);
            Box("SealChainA", seal, new Vector3(0f, 0.15f, 42.25f), new Vector3(3.4f, 0.12f, 0.15f), "iron_rust", false).transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            Box("SealChainB", seal, new Vector3(0f, 0.15f, 42.25f), new Vector3(3.4f, 0.12f, 0.15f), "iron_rust", false).transform.rotation = Quaternion.Euler(0f, -35f, 0f);
            Label("SealText", seal, new Vector3(0f, 0.5f, 39.6f), 0f, "SEALED BY THE KEEPERS\nrelease: Restricted Stacks", 0.25f, new Color(1f, 0.5f, 0.4f));
            seal.gameObject.SetActive(false);
            var sealR = Reactor("HatchSealed", logic, "L03.refused");
            sealR.whenNone.Add("L03.hatchReleased");
            sealR.enableWhenTrue.Add(seal.gameObject);
            sealR.announce = true; sealR.cue = Cue.Denied;
            var valve = Use("HatchReleaseValve", shared, new Vector3(25.9f, 1.3f, 36f), new Vector3(0.5f, 0.9f, 0.9f), "brass", "Turn the hatch release", "L03.hatchReleased", TimeMask.All, PrimitiveType.Cylinder);
            valve.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            valve.requiredFlags.Add("L03.refused");
            valve.lockedMessage = "An old hatch release. The hatch isn't sealed - nothing to release.";
            valve.cue = Cue.Clang;
            valve.lines.Add(L("Arin", "Chains off. Now back to the hatch - fast."));
            Label("ValveTag", shared, new Vector3(25.6f, 2.1f, 36f), 90f, "HATCH RELEASE", 0.25f, new Color(1f, 0.85f, 0.5f));

            // The chase: a Keeper enforcer pursues; the stacks flood; shelves collapse on a rhythm.
            var chaser = Npc("Keeper Enforcer", shared, new Vector3(-4f, 0f, 33f), 90f, "obsidian", false, 31);
            var croot = chaser.transform;
            Object.DestroyImmediate(chaser);
            Prim(PrimitiveType.Cylinder, "Hood", croot, new Vector3(-4f, 1.75f, 33f), new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            Label("ChaserTag", croot, new Vector3(-4f, 2.25f, 33f), 0f, "KEEPER ENFORCER", 0.25f, new Color(1f, 0.45f, 0.4f));
            var lantern = SpotLight("ChaserLantern", croot, new Vector3(-3.7f, 1.2f, 33f), new Vector3(10f, 90f, 0f), new Color(1f, 0.3f, 0.2f), 4f, 10f, 60f, false);
            Dyn(croot);
            var pursuer = croot.gameObject.AddComponent<Pursuer>();
            pursuer.displayName = "Keeper Enforcer";
            pursuer.startFlag = "L03.chase";
            pursuer.stopFlag = "L03.exit";
            pursuer.speed = 4.5f;
            pursuer.catchUpSpeed = 6.3f;
            pursuer.startDelay = 2f;
            pursuer.barks = new List<string> { "Mother wants the paper!", "You can't outrun the hour!", "Stop the anomaly!" };
            pursuer.caughtMessage = "The enforcer caught you. Sprint, and use the shelves to break the line.";
            var lane = Group("ChaserLane", logic);
            pursuer.lane.Add(Marker("L0", lane, new Vector3(-1f, 0f, 34f)));

            var flood = Group("ChaseFlood", shared);
            var water = Box("RisingWater", flood, new Vector3(4f, -0.06f, 25f), new Vector3(36f, 0.04f, 26f), "water", false);
            Dyn(water);
            Particles("FloodMist", flood, new Vector3(6f, 0.4f, 37f), new Vector3(20f, 0.5f, 10f), "mist");
            CollapsingShelf(flood, new Vector3(9f, 0f, 37.5f), 0f);
            CollapsingShelf(flood, new Vector3(14.5f, 0f, 38.8f), 1.3f);
            CollapsingShelf(flood, new Vector3(4.5f, 0f, 40.5f), 0.7f);
            Ambient("FloodRush", flood, new Vector3(8f, 1f, 37f), AmbientKind.Water, 0.5f, TimeMask.All, 1f, 24f);
            flood.gameObject.SetActive(false);
            var fr = Reactor("FloodStarts", logic, "L03.chase");
            fr.enableWhenTrue.Add(flood.gameObject);
            fr.announce = true; fr.cue = Cue.WaterRush;
            fr.toast = "The cisterns are open - the stacks are flooding!";
            var rise = Reactor("FloodRises", logic, "L03.chase");
            rise.moveTarget = water.transform; rise.moveBy = new Vector3(0f, 0.3f, 0f); rise.duration = 25f;
        }

        /// <summary>Same as AZKit.Keeper, but keeps the body transform before removing the NPC motor.</summary>
        static KeeperPatrol KeeperFixed(string displayName, Transform parent, Vector3 ground, float yaw, KeeperPatrol.Mode mode, TimeMask activeIn, params Vector3[] patrol)
        {
            var m = Npc(displayName, parent, ground, yaw, "coat_grey", false, displayName.Length);
            var body = m.transform;
            Object.DestroyImmediate(m);
            var root = Group("Keeper " + displayName, parent);
            root.position = ground;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.SetParent(root, true);
            body.name = "Body";
            Prim(PrimitiveType.Cylinder, "Hood", body, ground + Vector3.up * 1.72f, new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            var eye = Marker("Eye", root, ground + Vector3.up * 1.6f + root.forward * 0.2f, yaw);
            var lanternGo = new GameObject("Lantern");
            lanternGo.transform.SetParent(root, false);
            lanternGo.transform.position = ground + Vector3.up * 1.2f + root.forward * 0.35f + root.right * 0.25f;
            lanternGo.transform.rotation = Quaternion.Euler(12f, yaw, 0f);
            var l = lanternGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(1f, 0.85f, 0.55f);
            l.intensity = 6f;
            l.range = mode == KeeperPatrol.Mode.Watcher ? 9f : 13f;
            l.spotAngle = 70f;
            l.shadows = LightShadows.None;
            Sphere("LanternGlass", lanternGo.transform, lanternGo.transform.position, 0.1f, "lamp_warm", false);
            var cone = Hologram("SightCone", lanternGo.transform, lanternGo.transform.position + lanternGo.transform.forward * 4f, new Vector3(5f, 4f, 0.01f), PrimitiveType.Quad, "hologram_amber", false, false);
            cone.transform.rotation = Quaternion.LookRotation(Vector3.up, lanternGo.transform.forward);
            cone.transform.localScale = new Vector3(5f, 8f, 1f);
            var k = root.gameObject.AddComponent<KeeperPatrol>();
            k.displayName = displayName;
            k.mode = mode;
            k.activeIn = activeIn;
            k.eye = eye;
            k.lantern = l;
            var route = Group(displayName + " Patrol", parent);
            foreach (var p in patrol) k.patrol.Add(Marker("P", route, p));
            Dyn(root.gameObject);
            return k;
        }

        static void Chain(Transform logic, string name, string when, string set)
        {
            var r = Reactor(name, logic, when);
            r.setFlagWhenTrue = set;
        }

        static void CollapsingShelf(Transform parent, Vector3 ground, float offset)
        {
            var g = Group("CollapsingShelf", parent);
            g.position = ground;
            var fallen = Group("Fallen", g);
            var slab = Box("FallenShelf", fallen, ground + new Vector3(0f, 0.6f, 0f), new Vector3(2.6f, 1.2f, 0.5f), "wood_dark", false);
            slab.transform.rotation = Quaternion.Euler(0f, 30f, 70f);
            fallen.gameObject.SetActive(false);
            var warn = PointLight("CreakWarn", g, ground + Vector3.up * 2.4f, new Color(1f, 0.55f, 0.25f), 3.5f, 5f);
            var dust = Particles("CreakDust", g, ground + Vector3.up * 3.5f, new Vector3(2f, 0.2f, 1.5f), "debris");
            Box("Marker", g, ground + Vector3.up * 0.32f, new Vector3(2.4f, 0.02f, 2.4f), "neon_amber", false);
            var dz = TriggerBox("DamageZone", g, ground + Vector3.up * 1f, new Vector3(2.2f, 2f, 2.2f));
            var h = g.gameObject.AddComponent<TelegraphedHazard>();
            h.idleTime = 2f; h.warnTime = 1.2f; h.activeTime = 1f; h.startOffset = offset;
            h.warnLight = warn; h.warnParticles = dust; h.activeVisual = fallen.gameObject; h.damageZone = dz;
            h.warnCue = Cue.HazardWarn;
            h.deathMessage = "A shelf came down. Watch for the creak and the falling dust, then run.";
            Dyn(g.gameObject);
        }
    }
}
