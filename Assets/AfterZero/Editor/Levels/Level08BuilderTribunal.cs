using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    public static partial class Level08Builder
    {
        // =====================================================================
        // Phase 2 pressure: chamber segments collapse on a telegraphed rhythm
        // =====================================================================

        static void BuildCollapse(Transform shared, Transform logic)
        {
            var g = Group("Chamber Collapse", shared);
            var spots = new[] { Polar(14f, 0f), Polar(14f, 90f), Polar(14f, 180f), Polar(9f, 45f), Polar(9f, 135f), Polar(9.5f, 330f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var h = Masonry("FallingSegment" + i, g, spots[i], spots[i] + Vector3.up * 11f, i * 0.9f, TimeMask.All, F_Seq, "obsidian");
                h.idleTime = 3.2f; h.warnTime = 1.5f; h.activeTime = 1.2f;
                h.deathMessage = "A ceiling segment fell. Red light and falling grit mark the next one -- cross between the falls.";
            }
            g.gameObject.SetActive(false);
            var wake = Reactor("ChamberBreaks", logic, F_Zero);
            wake.enableWhenTrue.Add(g.gameObject);
            wake.reversible = false;
            var calm = Reactor("ChamberSettles", logic, F_Seq);
            calm.toast = "The falling stops. The chamber holds its breath -- and figures gather around the niche.";
        }

        // =====================================================================
        // Phase 3: the Tribunal (chained DialogueChoices + four echo NPCs)
        // =====================================================================

        static NPCMotor TribunalEcho(Transform g, string name, Vector3 pos, string holo, string coat, int seed)
        {
            var m = Npc(name, g, pos, FaceYaw(Vector3.zero, pos) + 180f, coat, true, seed);
            HoloBody(m, holo);
            return m;
        }

        static void BuildTribunal(Transform shared, Transform logic)
        {
            var g = Group("Tribunal", shared);
            var echoes = new List<GameObject>();

            // ---- Founder Aurel Halden (echo) ----
            var halden = TribunalEcho(g, "Echo of Founder Aurel Halden", new Vector3(-5.6f, 0f, 3.6f), "hologram_amber", "coat_white", 91);
            Route(halden, false,
                (new Vector3(-5.6f, 0f, 3.6f), 4f, NPCActivity.Work, "Every name, in my own hand."),
                (new Vector3(-6.8f, 0f, 5.2f), 3f, NPCActivity.Look, ""),
                (new Vector3(-4.8f, 0f, 5.6f), 3f, NPCActivity.Idle, ""));
            Route(halden, true,
                (new Vector3(-9f, 0f, 7f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-11f, 0f, 8f), 99999f, NPCActivity.Idle, "Choose. I already did, once."));
            var hp = Persona(halden, "An echo of the Founder, writing the evacuation list again and again in a ledger of light.",
                new[] { "The anomaly reaches the Engine at last.", "Stand where I stood." },
                new[]
                {
                    L("Aurel Halden", "I ordered the evacuation, and I ordered the silence. Both were for the same children."),
                    L("Aurel Halden", "Fourteen of them, nine days early. You were the eighth name on the list."),
                    L("Aurel Halden", "The rescue failed in your branch. In this one, the loop caught it. Nobody here has died for good since."),
                    L("Aurel Halden", "That is what Stability means. Not a lie -- a mercy with a bad memory."),
                    L("Aurel Halden", "Give them the memory back, a little each night, and keep the mercy. That is my case."),
                },
                "L07.haldenYielded", "", new[] { L("Aurel Halden", "On the catastrophe night you made me open my gate. I was glad. I never told Ondine."), L("Aurel Halden", "Being glad did not make me wrong.") },
                F_TribunalDone, new[] { L("Aurel Halden", "I have said my piece. The Engine listens to you, not to me.") });
            AddCond(hp, "evidence.list", "", L("Aurel Halden", "You carry my list. 'VALE, A., age 8. Guardian: I. Marrow.'"), L("Aurel Halden", "I wrote that line. I remember the ink was cold."));

            // ---- Mother Ondine Halden (echo) ----
            var ondine = TribunalEcho(g, "Echo of Mother Ondine Halden", new Vector3(-6.6f, 0f, -1f), "hologram_magenta", "coat_grey", 92);
            Route(ondine, false,
                (new Vector3(-6.6f, 0f, -1f), 4f, NPCActivity.Work, "Lanterns lit. Hour kept."),
                (new Vector3(-8.2f, 0f, 0.5f), 3f, NPCActivity.Look, ""),
                (new Vector3(-7.6f, 0f, -2.6f), 3f, NPCActivity.Idle, ""));
            Route(ondine, true,
                (new Vector3(-10f, 0f, -1f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-12f, 0f, -2f), 99999f, NPCActivity.Look, "Whatever you choose, the Keepers will answer."));
            var onp = Persona(ondine, "An echo of the Keepers' mother, trimming a sight-lantern that never runs out of oil.",
                new[] { "You came all the way down. Of course you did.", "The hour is kept. Mind it." },
                new[]
                {
                    L("Ondine Halden", "Inside the cycle, nobody dies for good. Not one child on that list. Not one person in the square."),
                    L("Ondine Halden", "Break it, and tomorrow comes. Tomorrow has no room for the loop-born."),
                    L("Ondine Halden", "Lise Ashgrove was born inside the repeat. At dawn she simply... stops."),
                    L("Ondine Halden", "The Keepers lock doors and burn records because the truth is the only thing that can end us."),
                    L("Ondine Halden", "Choose Stability, and the Keepers will answer to you. Every lantern. Every hour."),
                },
                "L07.ondineShaken", "", new[] { L("Ondine Halden", "You told me my father admitted it. On the bell night."), L("Ondine Halden", "I still choose the loop. But I choose it knowing, now.") },
                F_TribunalDone, new[] { L("Ondine Halden", "Choose, keeper. Or whatever you are now.") });
            AddCond(onp, "L07.ondineDefied", "", L("Ondine Halden", "You told me nobody lives for good either. I have thought about it every night since."), L("Ondine Halden", "Every one of those nights was the same night. Perhaps you had a point."));

            // ---- Odile Fenwick (echo) ----
            var odile = TribunalEcho(g, "Echo of Odile Fenwick", new Vector3(5.6f, 0f, 3.6f), "hologram", "coat_grey", 93);
            Route(odile, false,
                (new Vector3(5.6f, 0f, 3.6f), 4f, NPCActivity.Work, "Name. Street. Age."),
                (new Vector3(6.8f, 0f, 5.2f), 3f, NPCActivity.Look, ""),
                (new Vector3(4.8f, 0f, 5.6f), 3f, NPCActivity.Idle, ""));
            Route(odile, true,
                (new Vector3(9f, 0f, 7f), 0f, NPCActivity.Walk, ""),
                (new Vector3(11f, 0f, 8f), 99999f, NPCActivity.Work, "Whatever you choose, I'll write it down."));
            var odp = Persona(odile, "An echo of the head archivist, copying the struck names from the evacuation list into a clean book.",
                new[] { "Arin. You found the bottom of it.", "Every name gets written. Every one." },
                new[]
                {
                    L("Odile Fenwick", "Forty years of struck names. I have counted them every night: the same number, every night."),
                    L("Odile Fenwick", "A city that cannot remember cannot heal. It can only repeat."),
                    L("Odile Fenwick", "Publish the names. Let the states separate. Let the horizon open and let us find out what is out there."),
                    L("Odile Fenwick", "Yes, the loop-born fade. I have done that counting too. I will not pretend it is small."),
                    L("Odile Fenwick", "But a mercy nobody consented to is just a cage with kind walls. That is my case."),
                },
                "choice.L05.records", "", new[] { L("Odile Fenwick", "You saved the class register from the water. I would have kissed you for it."), L("Odile Fenwick", "Every one of those children gets a line in daylight, if you let them.") },
                F_TribunalDone, new[] { L("Odile Fenwick", "Choose. I'll keep the record either way.") });
            AddCond(odp, "evidence.record", "", L("Odile Fenwick", "Maren's record. Alive and dead, countersigned in your hand."), L("Odile Fenwick", "That is what the loop does to the truth. It makes it say both things at once."));

            // ---- Pell Ashgrove (echo) ----
            var pell = TribunalEcho(g, "Echo of Pell Ashgrove", new Vector3(6.6f, 0f, -1f), "hologram", "coat_navy", 94);
            Route(pell, false,
                (new Vector3(6.6f, 0f, -1f), 4f, NPCActivity.Carry, "One more letter. Always one more."),
                (new Vector3(8.2f, 0f, 0.5f), 3f, NPCActivity.Look, ""),
                (new Vector3(7.6f, 0f, -2.6f), 3f, NPCActivity.Idle, ""));
            Route(pell, true,
                (new Vector3(10f, 0f, -1f), 0f, NPCActivity.Walk, ""),
                (new Vector3(12f, 0f, -2f), 99999f, NPCActivity.Idle, "Lise is asleep. Don't wake her yet."));
            var plp = Persona(pell, "An echo of the courier, still carrying the letter dated tomorrow in his satchel.",
                new[] { "Arin. Again. Last time, maybe.", "I brought the letter. I always bring the letter." },
                new[]
                {
                    L("Pell Ashgrove", "I want the truth out. I've carried your letter more times than there are days -- I want it to arrive."),
                    L("Pell Ashgrove", "And Lise is the truth too. She's real. She's nine. She only exists because the night repeats."),
                    L("Pell Ashgrove", "If you break the loop, she fades at dawn. If you don't, we all stay in one night forever."),
                    L("Pell Ashgrove", "Odile says freedom. Ondine says mercy. I just say Lise."),
                    L("Pell Ashgrove", "Decide for both of us. I can't. That's my whole case: I can't."),
                },
                "L07.group2Saved", "", new[] { L("Pell Ashgrove", "You got Lise out of the courtyard on the bell night."), L("Pell Ashgrove", "Whatever you choose, you already saved her once. Remember that.") },
                F_TribunalDone, new[] { L("Pell Ashgrove", "Go on. Choose. I'll hold her hand either way.") });
            AddCond(plp, "arin.watch", "", L("Pell Ashgrove", "That watch. You had it the first time I delivered the letter."), L("Pell Ashgrove", "It's never ticked. Not once, in all those mornings."));

            echoes.Add(halden.gameObject); echoes.Add(ondine.gameObject); echoes.Add(odile.gameObject); echoes.Add(pell.gameObject);
            foreach (var e in echoes) e.SetActive(false);
            var gather = Reactor("TribunalGathers", logic, F_Seq);
            gather.enableWhenTrue.AddRange(echoes);
            gather.announce = true;
            gather.reversible = false;

            // ---- The chained hearing ----
            DialogueChoice Hearing(string name, string speaker, string trigger, string done, string question)
            {
                var host = new GameObject("Tribunal_" + name);
                host.transform.SetParent(logic, false);
                var d = Dialogue(host, speaker, question, trigger);
                d.doneFlag = done;
                return d;
            }
            var t1 = Hearing("Halden", "Echo of Founder Aurel Halden", F_Seq, "L08.tribunal.t1", "Halden argues for STABILITY. Your answer?");
            t1.opening.Add(L("Aurel Halden", "Before you choose, the Tribunal sits. Hear the Founders first. Hear me.", 4f));
            t1.opening.Add(L("Aurel Halden", "Stability. Keep the night turning, but soften it. Every soul wakes each morning alive -- and remembering a little more.", 5.5f));
            t1.opening.Add(L("Aurel Halden", "The silence killed no one in my plan. The truth would have.", 3.5f));
            t1.options.Add(Option("Alive is not the same as living.", "L08.tribunal.halden.refuted", L("Aurel Halden", "Perhaps. But the dead do neither.", 3f)));
            t1.options.Add(Option("You kept them alive. That counts.", "L08.tribunal.halden.heard", L("Aurel Halden", "Then let it count for something, when you choose.", 3.5f)));

            var t2 = Hearing("Ondine", "Echo of Mother Ondine Halden", "L08.tribunal.t1", "L08.tribunal.t2", "Ondine argues for STABILITY. Your answer?");
            t2.opening.Add(L("Ondine Halden", "In the loop nobody dies for good. Not the children on the list. Not Lise Ashgrove. Not you.", 4.5f));
            t2.opening.Add(L("Ondine Halden", "Choose Evidence, and the loop-born fade at dawn. Pell's daughter first.", 4f));
            t2.options.Add(Option("Lise deserves a real tomorrow, not a borrowed one.", "L08.tribunal.ondine.refuted", L("Ondine Halden", "A real tomorrow she won't be in.", 3f)));
            t2.options.Add(Option("I won't pretend that cost is small.", "L08.tribunal.ondine.heard", L("Ondine Halden", "Good. Then you'll choose with your eyes open.", 3.5f)));

            var t3 = Hearing("Odile", "Echo of Odile Fenwick", "L08.tribunal.t2", "L08.tribunal.t3", "Odile argues for EVIDENCE. Your answer?");
            t3.opening.Add(L("Odile Fenwick", "I keep the record honest. Forty years of struck names, and every one of them still waiting.", 4.5f));
            t3.opening.Add(L("Odile Fenwick", "Publish them. Let the states separate. Let the horizon open. A city that cannot remember cannot heal.", 5f));
            t3.options.Add(Option("Even if people fade at dawn?", "L08.tribunal.odile.pressed", L("Odile Fenwick", "Even then. I'm sorry. I've done the counting, and I'd do it again.", 4f)));
            t3.options.Add(Option("The names deserve daylight.", "L08.tribunal.odile.heard", L("Odile Fenwick", "They do. So do the people who'll have to read them.", 3.5f)));

            var t4 = Hearing("Pell", "Echo of Pell Ashgrove", "L08.tribunal.t3", "L08.tribunal.t4", "Pell argues for EVIDENCE -- and for Lise. Your answer?");
            t4.opening.Add(L("Pell Ashgrove", "I want the truth out. Lise is the truth too.", 3f));
            t4.opening.Add(L("Pell Ashgrove", "Break it and she goes. Keep it and we all stay nine years inside one night. Decide for both of us -- I can't.", 5.5f));
            t4.options.Add(Option("I'll carry it, Pell. Whatever I choose.", "L08.tribunal.pell.heard", L("Pell Ashgrove", "Then I'll carry the letter one last time.", 3f)));
            t4.options.Add(Option("You can't ask me to choose your daughter.", "L08.tribunal.pell.pressed", L("Pell Ashgrove", "I'm not asking you to choose her. I'm asking you to choose.", 3.5f)));

            var t5 = Hearing("Maren", "The Witness", "L08.tribunal.t4", F_TribunalDone, "Maren -- The Witness -- argues for EVIDENCE. Your answer?");
            t5.opening.Add(L("The Witness", "I'm Maren Oskel. You pulled me out of the water once. I stayed in the seam so someone would be here when you came back.", 5.5f));
            t5.opening.Add(L("The Witness", "Keep the loop, and I stay. Break it, and I can finally leave -- into a tomorrow that might not have room for me.", 5f));
            t5.opening.Add(L("The Witness", "I want to leave, Arin. I've wanted it for forty years. But not if it costs you everything.", 4.5f));
            t5.options.Add(Option("Come with me into tomorrow.", "L08.tribunal.maren.leave", L("The Witness", "Then hold the door for me when it opens.", 3f)));
            t5.options.Add(Option("Stay, and keep watch with me.", "L08.tribunal.maren.stay", L("The Witness", "I'd keep watch with you for ever. I nearly have.", 3.5f)));
            var after = Reactor("TribunalAdjourns", logic, F_TribunalDone);
            after.announce = true;
            after.toast = "The Tribunal falls silent. The chamber fills with a lullaby -- and the floor tilts into memory.";
        }

        // =====================================================================
        // Phase 4: the playable memory -- child Arin walks the Nursery with Ilse
        // =====================================================================

        static void BuildMemory(Transform shared, Transform logic)
        {
            var c = new Vector3(150f, 0f, -120f);
            var g = Group("Memory Nursery", shared);
            Floor("NurseryFloor", g, c, new Vector2(16f, 12f), "wood");
            Wall("NW", g, c + new Vector3(-8f, 0f, 6f), c + new Vector3(8f, 0f, 6f), 4f, 0.4f, "plaster_warm");
            Wall("SW", g, c + new Vector3(-8f, 0f, -6f), c + new Vector3(8f, 0f, -6f), 4f, 0.4f, "plaster_warm");
            Wall("WW", g, c + new Vector3(-8f, 0f, -6f), c + new Vector3(-8f, 0f, 6f), 4f, 0.4f, "plaster_warm");
            Wall("EW", g, c + new Vector3(8f, 0f, -6f), c + new Vector3(8f, 0f, 6f), 4f, 0.4f, "plaster_warm");
            Box("NurseryCeiling", g, c + Vector3.up * 4.2f, new Vector3(16.4f, 0.4f, 12.4f), "plaster_warm");
            for (int i = 0; i < 3; i++)
                NoShadow(Box("NightWindow", g, c + new Vector3(-5f + i * 5f, 2.4f, 5.78f), new Vector3(1.6f, 1.2f, 0.06f), "window_cold", false));
            string[] blankets = { "fabric_teal", "fabric_red", "fabric_gold" };
            for (int row = 0; row < 2; row++)
            for (int i = 0; i < 7; i++)
            {
                int n = row * 7 + i + 1;
                var p = c + new Vector3(-6f + i * 2f, 0f, row == 0 ? -3f : 3f);
                Box("Cot" + n, g, p + Vector3.up * 0.3f, new Vector3(1f, 0.6f, 1.6f), "wood");
                NoShadow(Box("Blanket", g, p + Vector3.up * 0.62f, new Vector3(0.9f, 0.05f, 1.2f), blankets[n % 3], false));
                if (n == 7 || n == 3 || n == 1 || n == 14)
                    Label("CotNumber", g, p + new Vector3(0f, 1.0f, row == 0 ? 0.85f : -0.85f), row == 0 ? 180f : 0f, n == 7 ? "7   A." : n == 3 ? "3   M." : n.ToString(), 0.3f, new Color(0.3f, 0.2f, 0.1f));
            }
            Box("MusicBoxTable", g, c + new Vector3(-7f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f), "wood_dark");
            Box("MusicBox", g, c + new Vector3(-7f, 0.92f, 0f), new Vector3(0.4f, 0.25f, 0.3f), "brass", false);
            Box("IlseTable", g, c + new Vector3(7f, 0.4f, -3f), new Vector3(0.9f, 0.8f, 0.9f), "wood_dark");
            PointLight("NightLamp", g, c + new Vector3(-6f, 2.8f, 0f), new Color(1f, 0.75f, 0.45f), 3f, 12f);
            PointLight("IlseLamp", g, c + new Vector3(6.5f, 2.6f, -2.5f), new Color(1f, 0.8f, 0.5f), 2.5f, 8f);
            Particles("NurseryDust", g, c + Vector3.up * 2f, new Vector3(14f, 3f, 10f), "dust");
            Ambient("Lullaby", g, c + new Vector3(-7f, 1f, 0f), AmbientKind.Nursery, 0.45f, TimeMask.All, 1f, 20f);

            var spawn = Marker("MemorySpawn", g, c + new Vector3(-7f, 0.05f, -1.8f), 90f);
            var back = Marker("ReturnSpawn", g, new Vector3(0f, 0.05f, -7.5f), 0f);

            // Ilse, as she was (not an echo here: this is Arin's memory of her).
            var ilse = Npc("Ilse Marrow (memory)", g, c + new Vector3(-4f, 0f, 0f), 90f, "coat_olive", true, 95);
            Route(ilse, false,
                (c + new Vector3(-4f, 0f, 0f), 3f, NPCActivity.Work, "Tuck in, number two."),
                (c + new Vector3(1f, 0f, 0f), 3f, NPCActivity.Work, "Maren, that's the wrong note and you know it."),
                (c + new Vector3(5.5f, 0f, -1.2f), 4f, NPCActivity.Look, "Come on, little A. One more round of the cots."));
            Route(ilse, true,
                (c + new Vector3(6.2f, 0f, -2.2f), 99999f, NPCActivity.Idle, "Keep time for me, little A."));
            var ip = Persona(ilse, "The clockkeeper, as Arin remembers her: making the last round of the Nursery cots before lights out.",
                new[] { "There you are. Bare feet again.", "Shh. Half of them are asleep." },
                new[]
                {
                    L("Ilse Marrow", "Fourteen cots, fourteen of you. Nine days early, and nobody outside is allowed to know you're here."),
                    L("Ilse Marrow", "Founder Halden says it's safer this way. I say it's quieter. They aren't the same thing."),
                    L("Ilse Marrow", "You hum all four notes of the song. The others only ever manage three. Don't stop."),
                    L("Ilse Marrow", "If the bell rings twice, I'll be in the tower. Holding it. That's my job."),
                    L("Ilse Marrow", "Go and look at your cot. Number seven. I have something for you on my table."),
                },
                "arin.listedChild", "", new[] { L("Ilse Marrow", "VALE, A., age eight. That's what the list says. Guardian: me."), L("Ilse Marrow", "I signed it with my left hand so it would look different from Halden's. Silly. It's still my name.") },
                F_Watch, new[] { L("Ilse Marrow", "There. Now you keep time for me."), L("Ilse Marrow", "Whatever happens to the night, little A, it's yours to keep.") });
            AddCond(ip, "maren.recognised", "", L("Ilse Marrow", "Maren's in cot three, humming your song wrong on purpose."), L("Ilse Marrow", "She'll wait for you, that one. She's stubborn enough to wait for ever."));

            var walk = Zone("CotSeven", logic, c + new Vector3(6f, 1f, -3f), new Vector3(2.4f, 2f, 3f), "L08.memoryWalked");
            walk.lines.Add(L("Arin (child)", "Cot seven. Somebody scratched an A inside the rail.", 3.5f));
            walk.lines.Add(L("Ilse Marrow", "Somebody. I pretended not to see.", 3f));
            var hum = Marker("NurseryHum", g, c + new Vector3(0f, 1f, 0f));
            var hr = hum.gameObject.AddComponent<HumReceiver>();
            hr.radius = 9f;
            hr.setsFlag = "L08.memoryHum";
            hr.lines.Add(L("Ilse Marrow", "All four notes. You're the only one who knows all four.", 3.5f));

            var watch = Use("IlsesWatch", g, c + new Vector3(7f, 0.9f, -3f), new Vector3(0.4f, 0.12f, 0.4f), "brass", "Take the watch from Ilse", F_Watch, TimeMask.All, PrimitiveType.Cylinder);
            watch.requiredFlags.Add("L08.memoryWalked");
            watch.lockedMessage = "Ilse shakes her head: 'Your cot first, little A. Number seven.'";
            watch.cue = Cue.Memory;
            watch.lines.Add(L("Ilse Marrow", "Hold out your hand. Brass, a bit dented -- it was my father's.", 3.5f));
            watch.lines.Add(L("Ilse Marrow", "I've had it engraved. 'For A. -- keep time for me. I.M.'", 3.5f));
            watch.lines.Add(L("Arin (child)", "It doesn't tick.", 2f));
            watch.lines.Add(L("Ilse Marrow", "It will, when it's needed.", 2.5f));
            watch.disableAfterUse = watch.gameObject;

            var mw = new GameObject("MemoryWalk").AddComponent<L08MemoryWalk>();
            mw.transform.SetParent(logic, false);
            mw.startFlag = F_TribunalDone;
            mw.endFlag = F_Watch;
            mw.completeFlag = F_Memory;
            mw.memorySpawn = spawn;
            mw.returnSpawn = back;
            mw.endDelay = 11f;
            mw.enterLines.Add(L("Arin (child)", "The Nursery. I'm small. The cots are taller than me.", 3.5f));
            mw.enterLines.Add(L("Ilse Marrow", "Come on, little A. One more round of the cots before the lights go out.", 4f));
        }
    }
}
