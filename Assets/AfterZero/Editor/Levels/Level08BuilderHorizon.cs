using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    public static partial class Level08Builder
    {
        // =====================================================================
        // Named NPCs: the clockkeeper's echo, the other Arin, The Witness (+ echo crowds)
        // =====================================================================

        static void BuildNpcs(Transform shared)
        {
            var g = Group("Echoes", shared);

            // Workstations.
            Box("RingConsole", g, new Vector3(-9.6f, 0.55f, 7f), new Vector3(1.2f, 1.1f, 0.7f), "brass");
            NoShadow(Box("RingConsoleGlow", g, new Vector3(-9.6f, 1.12f, 7f), new Vector3(1.1f, 0.03f, 0.6f), "neon_amber", false));
            Box("ArchiveConsole", g, new Vector3(8.6f, 0.55f, -12.2f), new Vector3(1.2f, 1.1f, 0.7f), "obsidian");
            NoShadow(Box("ArchiveConsoleGlow", g, new Vector3(8.6f, 1.12f, -12.2f), new Vector3(1.1f, 0.03f, 0.6f), "neon_cyan", false));

            // ---- Echo of Ilse Marrow, the clockkeeper ----
            var ilse = Npc("Echo of Ilse Marrow", g, new Vector3(-8.6f, 0f, 6.2f), 120f, "coat_olive", true, 81);
            HoloBody(ilse, "hologram_amber");
            Route(ilse, false,
                (new Vector3(-8.8f, 0f, 6.1f), 5f, NPCActivity.Work, "Three degrees off. Always three degrees."),
                (new Vector3(-11f, 0f, -3f), 3f, NPCActivity.Look, ""),
                (new Vector3(-6f, 0f, 10.5f), 3f, NPCActivity.Idle, "The rings remember every cycle. I only keep them turning."));
            Route(ilse, true,
                (new Vector3(-5.5f, 0f, -1f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-3.6f, 0f, -4.6f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-4.2f, 0f, -4.0f), 99999f, NPCActivity.Idle, "Listen. The bell sounds whole again."));
            var ip = Persona(ilse, "Echo of the clockkeeper who stopped the tower from inside. She still calibrates the chamber's rings at the brass ring console.",
                new[] { "Another visitor. Or the same one, again.", "Mind the rings -- they hold more than light." },
                new[]
                {
                    L("Ilse Marrow", "You climbed past me on the stairs, the night it happened. I held the engine with both hands."),
                    L("Ilse Marrow", "It stopped. They called it an earthquake. I stayed down here, between one tick and the next."),
                    L("Ilse Marrow", "I always carried a carved zero. I carved it from the old bell's clapper block, the morning of the first festival."),
                    L("Ilse Marrow", "The niche in the middle has been empty ever since. Every cycle, empty."),
                    L("Ilse Marrow", "The bells answer only in their own time. West for the old, north for the now, east for what comes after."),
                },
                "", "zero", new[] { L("Ilse Marrow", "That zero... my own chisel marks. You carried it all the way down here."), L("Ilse Marrow", "Set it in the niche. The chamber has been waiting longer than I have.") },
                F_Seq, new[] { L("Ilse Marrow", "The sequence is whole. I can let go of the rings now."), L("Ilse Marrow", "Whatever you choose next -- choose it as yourself, not as the one who came before."), L("Ilse Marrow", "Tell Ignatz I stayed on purpose.") });
            AddCond(ip, "choice.L05.records", "", L("Ilse Marrow", "You saved the drowned records. Odile will want them -- she always kept the record honest."), L("Ilse Marrow", "The city keeps its proof in strange places. Now it keeps some in your hands."));
            AddCond(ip, "choice.L05.keepsake", "", L("Ilse Marrow", "You kept the child's keepsake and let the records drown."), L("Ilse Marrow", "A kindness to one person, at the price of the truth. Remember that, at the consoles."));

            // ---- Arin from another branch ----
            var other = Npc("Arin (other branch)", g, new Vector3(14f, 0f, -4f), 230f, "arin_coat", true, 82, "paper");
            HoloBody(other, "hologram");
            Route(other, false,
                (new Vector3(14f, 0f, -4f), 2.5f, NPCActivity.Carry, ""),
                (new Vector3(8.6f, 0f, -11.3f), 4f, NPCActivity.Work, "Cycle four hundred and... I've lost count again."),
                (new Vector3(12.5f, 0f, 7f), 3f, NPCActivity.Look, ""));
            Route(other, true,
                (new Vector3(9f, 0f, 9f), 0f, NPCActivity.Walk, ""),
                (new Vector3(3f, 0f, 14f), 0f, NPCActivity.Walk, ""),
                (new Vector3(0f, 0f, 15f), 99999f, NPCActivity.Idle, "Whatever you chose -- it's the choice I couldn't make."));
            var op = Persona(other, "An echo of Arin from another branch, carrying the records of every cycle from the FUTURE wing to the archive console.",
                new[] { "Don't look at me like that. You'll have this face too, eventually.", "You're early. Or I'm late." },
                new[]
                {
                    L("Arin (other branch)", "In my branch the bell rang twice and nothing came back. Not the festival, not the rain, not the ruins. One collapsed city, all three states crushed into it."),
                    L("Arin (other branch)", "The Founders were right about the larger collapse. They were only wrong about how to stop it."),
                    L("Arin (other branch)", "So I restarted the cycle. One bad night, repeating, instead of no nights at all."),
                    L("Arin (other branch)", "The signal from the tower's ruins? That's me, holding the door shut from the far side. It's in my handwriting because it's my hand."),
                    L("Arin (other branch)", "And you're the next of us. The fail-safe. Sent in to see whether Veyra is ready to stop repeating."),
                },
                "clue.L08.branchLog", "", new[] { L("Arin (other branch)", "You read my log. So you know the collapse doesn't end at Veyra."), L("Arin (other branch)", "I did this. I'm sorry. And I'm not.") },
                F_Made, new[] { L("Arin (other branch)", "You chose. I never could -- I just kept the loop running and called it waiting."), L("Arin (other branch)", "Go read what I signed. Then go see what I was protecting.") });
            AddCond(op, "clue.L08.halden", "", L("Arin (other branch)", "Halden's recording. He chose silence to stop a panic."), L("Arin (other branch)", "I chose repetition to stop an ending. Neither of us asked the city. You still can."));

            // ---- The Witness (Maren Oskel) ----
            var witness = Npc("The Witness", g, new Vector3(-2.6f, 0f, -5.2f), 0f, "coat_white", true, 83);
            HoloBody(witness, "hologram_magenta");
            Route(witness, false,
                (new Vector3(-2.6f, 0f, -5.2f), 4f, NPCActivity.Work, "Every glyph, once more."),
                (new Vector3(4.8f, 0f, 2.4f), 4f, NPCActivity.Work, ""),
                (new Vector3(-4.8f, 0f, 2.4f), 3f, NPCActivity.Look, ""));
            Route(witness, true,
                (new Vector3(-4.5f, 0f, 5f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-1.5f, 0f, 16f), 0f, NPCActivity.Walk, ""),
                (new Vector3(0f, 0f, 18.5f), 99999f, NPCActivity.Look, "The wall is open. Go on."));
            var wp = Persona(witness, "The Witness. Tracing the inscription around the niche, glyph by glyph, so the order is never lost between cycles.",
                new[] { "You hear me at last, close up.", "I have watched you arrive here before." },
                new[]
                {
                    L("The Witness", "I am what remains when a cycle forgets itself. I watch, so that something does."),
                    L("The Witness", "Once I had a name. It was on a list nine days early, and in the archive twice: alive, and dead."),
                    L("The Witness", "Maren Oskel. The girl in the Drowned District. The water was at my chin when you reached me."),
                    L("The Witness", "I grew up outside of time, in the seam between the states. I wrote you the letter dated tomorrow -- copied from your own hand."),
                    L("The Witness", "The niche first. Then the bells. Then a choice nobody can make for you. Not even me."),
                },
                "clue.L08.inscription", "", new[] { L("The Witness", "You read it. Ring them as the first cycle rang."), L("The Witness", "A bell lives in only one state. Shift to where it hangs -- the order survives the shift.") },
                F_Sign, new[] { L("The Witness", "Now you know whose hand restarted the world."), L("The Witness", "The north wall is open. Go and see what it was holding back.") });
            AddCond(wp, "choice.L05.keepsake", "", L("The Witness", "You saved my music box, that night. I still have it. It still plays three notes and skips the fourth."), L("The Witness", "Whatever you choose, I'll play it for you at the end."));
            AddCond(wp, "choice.L05.records", "", L("The Witness", "You saved the records instead of my music box. I was angry for a very long time."), L("The Witness", "Then I read them, and found my family's name. You gave me back the proof that I existed."));

            // Background echoes of past cycles, still going about the festival.
            var barks = new[] { "Is it the festival again?", "I remember this room. I don't remember why.", "Did the bell ring twice?" };
            foreach (var m in Crowd("Echo Crowd West", g, new Vector3(-12f, 0f, -11f), new Vector2(6f, 5f), 4, 8101, barks, "paper")) HoloBody(m, "hologram");
            foreach (var m in Crowd("Echo Crowd East", g, new Vector3(12.5f, 0f, 10f), new Vector2(5f, 5f), 4, 8102, barks, "fabric_gold")) HoloBody(m, "hologram_amber");
        }

        // =====================================================================
        // Newly exposed corridor, balcony, the horizon of different physical laws
        // =====================================================================

        static void BuildCorridorAndHorizon(Transform root, Transform shared, Transform logic)
        {
            var g = Group("Exit", shared);
            float z0 = WingR + 5f; // north wing back wall
            var dl = Door("ExitWallL", g, new Vector3(-1.2f, 0f, z0), new Vector3(2.4f, 3f, 0.4f), "obsidian", new Vector3(-2.5f, 0f, 0f), 0f, F_Sign);
            var dr = Door("ExitWallR", g, new Vector3(1.2f, 0f, z0), new Vector3(2.4f, 3f, 0.4f), "obsidian", new Vector3(2.5f, 0f, 0f), 0f, F_Sign);
            dl.toast = "The north wall slides apart. Behind it: a corridor, and wind.";
            var sealL = Label("SealGlyphL", dl.moveTarget, new Vector3(-1.2f, 1.8f, z0 - 0.22f), 0f, "(0", 1.4f, new Color(0.8f, 0.95f, 1f));
            var sealR = Label("SealGlyphR", dr.moveTarget, new Vector3(1.2f, 1.8f, z0 - 0.22f), 0f, "0)", 1.4f, new Color(0.8f, 0.95f, 1f));
            sealL.transform.localScale = new Vector3(1f / 2.4f, 1f / 3f, 1f / 0.4f);
            sealR.transform.localScale = new Vector3(1f / 2.4f, 1f / 3f, 1f / 0.4f);
            Label("SealText", g, new Vector3(0f, 3.6f, z0 - 0.35f), 0f, "SEALED UNTIL THE ORDER IS READ", 0.3f, new Color(0.7f, 0.9f, 1f));

            // Corridor.
            float zc0 = z0 + 0.3f, zc1 = 56f;
            Floor("CorridorFloor", g, new Vector3(0f, 0f, (zc0 + zc1) * 0.5f + 1f), new Vector2(6f, zc1 - zc0 + 2f), "obsidian");
            Wall("CorridorW", g, new Vector3(-2.4f, 0f, zc0), new Vector3(-2.4f, 0f, zc1), 5f, 0.5f, "metal_dark");
            Wall("CorridorE", g, new Vector3(2.4f, 0f, zc0), new Vector3(2.4f, 0f, zc1), 5f, 0.5f, "metal_dark");
            Box("CorridorCeiling", g, new Vector3(0f, 5.2f, (zc0 + zc1) * 0.5f), new Vector3(5.4f, 0.4f, zc1 - zc0), "obsidian");
            for (int i = 0; i < 8; i++)
            {
                float z = zc0 + 2f + i * 3.2f;
                NoShadow(Box("ArchNeon", g, new Vector3(0f, 4.9f, z), new Vector3(4.6f, 0.1f, 0.12f), i % 2 == 0 ? "neon_cyan" : "neon_violet", false));
                NoShadow(Box("FloorGuide", g, new Vector3(0f, 0.01f, z), new Vector3(0.12f, 0.02f, 1.6f), "rail_strip", false));
            }
            PointLight("CorridorLightA", g, new Vector3(0f, 4f, 36f), new Color(0.5f, 0.85f, 1f), 2.5f, 12f);

            // Balcony.
            Floor("BalconyFloor", g, new Vector3(0f, 0f, 59.5f), new Vector2(16f, 7f), "stone_warm");
            Box("BalconyUnder", g, new Vector3(0f, -6f, 59.5f), new Vector3(15f, 11f, 6.5f), "ruin_dark", false);
            Wall("RailN", g, new Vector3(-8f, 0f, 63f), new Vector3(8f, 0f, 63f), 1.1f, 0.25f, "brass");
            Wall("RailW", g, new Vector3(-8f, 0f, 56f), new Vector3(-8f, 0f, 63f), 1.1f, 0.25f, "brass");
            Wall("RailE", g, new Vector3(8f, 0f, 56f), new Vector3(8f, 0f, 63f), 1.1f, 0.25f, "brass");
            Wall("CliffW", g, new Vector3(-8f, 0f, 56f), new Vector3(-2.4f, 0f, 56f), 9f, 0.8f, "ruin_dark");
            Wall("CliffE", g, new Vector3(2.4f, 0f, 56f), new Vector3(8f, 0f, 56f), 9f, 0.8f, "ruin_dark");
            Wall("CliffLintel", g, new Vector3(-2.4f, 5f, 56f), new Vector3(2.4f, 5f, 56f), 4f, 0.8f, "ruin_dark");
            KillPlane(g, new Vector3(0f, -14f, 150f), new Vector3(500f, 2f, 300f), "Down there the laws are different. Not yet.");
            ReflectionProbe(g, new Vector3(0f, 3f, 50f), new Vector3(16f, 8f, 26f));

            var horizon = Zone("HorizonSeen", logic, new Vector3(0f, 1f, 57.5f), new Vector3(15f, 2f, 3f), F_Horizon);
            horizon.lines.Add(L("Arin", "So that's what the loop was holding shut."));
            horizon.lines.Add(L("Arin", "Islands falling upward. A waterfall that forgot how to fall. A sun the wrong colour."));
            horizon.lines.Add(L("The Witness", "The first cycle is over, Arin. The others are waiting out there.", 4.5f));
            Exit("Exit_Horizon", g, new Vector3(0f, 0f, 61.6f), new Vector3(14f, 3f, 1.6f), "The horizon can wait until you've read the order.", F_Sign);

            // ---------- The horizon ----------
            var v = Group("Horizon", shared);
            Box("MistSea", v, new Vector3(0f, -45f, 250f), new Vector3(900f, 1f, 600f), "obsidian", false);
            var sea = Particles("MistSea", v, new Vector3(0f, -28f, 200f), new Vector3(400f, 10f, 260f), "mist");
            var sm = sea.main; sm.maxParticles = 400; sm.startSize = new ParticleSystem.MinMaxCurve(20f, 45f); sm.startLifetime = 20f;
            var se = sea.emission; se.rateOverTime = 25f;

            // 1) Floating INVERTED islands: rock points up, grass and trees hang downward.
            Island(v, new Vector3(-60f, 28f, 150f), 22f, 1);
            Island(v, new Vector3(45f, 44f, 185f), 30f, 2);
            Island(v, new Vector3(-15f, 62f, 240f), 26f, 3);
            Island(v, new Vector3(95f, 22f, 130f), 16f, 4);
            Island(v, new Vector3(-120f, 40f, 215f), 20f, 5);
            PointLight("IslandLight", v, new Vector3(-20f, 60f, 170f), new Color(0.9f, 0.85f, 1f), 6f, 160f);

            // 2) The frozen waterfall.
            var wf = Group("FrozenWaterfall", v);
            Box("Cliff", wf, new Vector3(-95f, 5f, 265f), new Vector3(45f, 130f, 30f), "ruin_dark", false);
            Box("CliffLip", wf, new Vector3(-95f, 70f, 249f), new Vector3(16f, 4f, 6f), "ruin_dark", false);
            Hologram("FallSheet", wf, new Vector3(-95f, 18f, 248f), new Vector3(11f, 100f, 1.2f), PrimitiveType.Cube, "hologram", false, false);
            Hologram("FallCore", wf, new Vector3(-95f, 18f, 247.2f), new Vector3(5f, 100f, 0.6f), PrimitiveType.Cube, "hologram", false, false);
            var frng = new System.Random(55);
            for (int i = 0; i < 18; i++)
            {
                var p = new Vector3(-95f + ((float)frng.NextDouble() - 0.5f) * 26f, -30f + (float)frng.NextDouble() * 22f, 244f - (float)frng.NextDouble() * 12f);
                Hologram("FrozenSpray", wf, p, Vector3.one * (0.8f + (float)frng.NextDouble() * 2.2f), PrimitiveType.Sphere, "hologram", false, false);
            }
            for (int i = 0; i < 5; i++)
                Tilted("Icicle", wf, new Vector3(-101f + i * 3f, 64f, 248f), new Vector3(0.8f, 7f, 0.8f), "glass", new Vector3(0f, 0f, i * 4f - 8f), false);
            PointLight("FallLight", wf, new Vector3(-95f, 20f, 236f), new Color(0.5f, 0.9f, 1f), 5f, 90f);

            // 3) A region lit by a different-coloured sun.
            var gs = Group("GreenSunRegion", v);
            var sun = Sphere("GreenSun", gs, new Vector3(165f, 70f, 340f), 26f, "mineral_trace", false);
            NoShadow(sun);
            Hologram("SunCorona", gs, new Vector3(165f, 70f, 338f), new Vector3(80f, 80f, 0.5f), PrimitiveType.Sphere, "hologram", false, false);
            PointLight("GreenSunLight", gs, new Vector3(150f, 45f, 300f), new Color(0.45f, 1f, 0.6f), 8f, 260f);
            var grng = new System.Random(66);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(120f + (float)grng.NextDouble() * 80f, -25f, 250f + (float)grng.NextDouble() * 70f);
                float h = 25f + (float)grng.NextDouble() * 45f;
                Tilted("GreenSpire", gs, p + Vector3.up * h * 0.5f, new Vector3(4f + (float)grng.NextDouble() * 4f, h, 4f + (float)grng.NextDouble() * 4f), i % 2 == 0 ? "crystal" : "ruin_dark", new Vector3(grng.Next(-12, 12), grng.Next(360), grng.Next(-12, 12)), false);
            }
            var gm = Particles("GreenMotes", gs, new Vector3(150f, 10f, 290f), new Vector3(90f, 40f, 80f), "motes");
            var gmm = gm.main; gmm.startColor = new Color(0.5f, 1f, 0.6f, 0.9f); gmm.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);

            // 4) Rain that falls upward.
            var up = Particles("UpwardRain", v, new Vector3(55f, -20f, 120f), new Vector3(40f, 2f, 40f), "rain");
            var upv = up.velocityOverLifetime; upv.y = new ParticleSystem.MinMaxCurve(14f);
            var upm = up.main; upm.maxParticles = 1500; upm.startLifetime = 4f;
            var upe = up.emission; upe.rateOverTime = 350f;

            // 5) The zero on the horizon.
            var ring = Hologram("HorizonZero", v, new Vector3(0f, 90f, 420f), new Vector3(220f, 0.6f, 220f), PrimitiveType.Cylinder, "hologram", false, false);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var rs = ring.AddComponent<Spinner>(); rs.degreesPerSecond = new Vector3(0f, 3f, 0f);
        }

        static void Island(Transform parent, Vector3 pos, float s, int seed)
        {
            var g = Group("InvertedIsland" + seed, parent);
            g.position = pos;
            var rng = new System.Random(900 + seed);
            var rock = Sphere("RockCap", g, pos, s * 0.5f, "ruin_dark", false);
            rock.transform.localScale = new Vector3(s, s * 0.3f, s);
            Box("RockSpire1", g, pos + Vector3.up * s * 0.22f, new Vector3(s * 0.6f, s * 0.25f, s * 0.6f), "ruin_dark", false).transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            Box("RockSpire2", g, pos + Vector3.up * s * 0.42f, new Vector3(s * 0.3f, s * 0.25f, s * 0.3f), "ruin_dark", false).transform.rotation = Quaternion.Euler(0f, 50f, 0f);
            Box("RockSpire3", g, pos + Vector3.up * s * 0.6f, new Vector3(s * 0.12f, s * 0.2f, s * 0.12f), "crystal", false);
            var grass = Sphere("GrassUnderside", g, pos + Vector3.down * s * 0.1f, s * 0.48f, "grass_dry", false);
            grass.transform.localScale = new Vector3(s * 0.96f, s * 0.12f, s * 0.96f);
            for (int i = 0; i < 5; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * s * 0.32f;
                var tp = pos + new Vector3(Mathf.Cos(a) * r, -s * 0.15f, Mathf.Sin(a) * r);
                float th = s * (0.12f + (float)rng.NextDouble() * 0.12f);
                Cylinder("HangingTrunk", g, tp + Vector3.down * th * 0.5f, s * 0.015f + 0.2f, th, "wood_dark", false);
                Sphere("HangingCrown", g, tp + Vector3.down * th, s * 0.07f + 0.8f, i % 2 == 0 ? "coat_olive" : "grass_dry", false);
            }
            // Water pouring UP from the rim.
            var pour = Hologram("UpwardFall", g, pos + new Vector3(s * 0.42f, s * 0.6f, 0f), new Vector3(s * 0.08f, s * 1.2f, s * 0.08f), PrimitiveType.Cube, "hologram", false, false);
            var fb = g.gameObject.AddComponent<FloatBob>();
            fb.amplitude = 1.2f + seed * 0.2f; fb.frequency = 0.03f + seed * 0.005f; fb.drift = new Vector3(0f, 1.5f * (seed % 2 == 0 ? 1f : -1f), 0f);
        }

        // =====================================================================
        // Audio
        // =====================================================================

        static void BuildAudio(Transform root)
        {
            var a = Group("Ambience", root);
            Ambient("ChamberDrone", a, new Vector3(0f, 8f, 0f), AmbientKind.FutureDrone, 0.35f, TimeMask.All, 0f);
            Ambient("RingMachine", a, new Vector3(0f, 3f, 0f), AmbientKind.Machine, 0.35f, TimeMask.Past | TimeMask.Present, 1f, 20f);
            Ambient("Seepage", a, new Vector3(0f, 4f, 0f), AmbientKind.Water, 0.3f, TimeMask.Present, 1f, 22f);
            Ambient("OculusRain", a, new Vector3(0f, 10f, 0f), AmbientKind.Rain, 0.25f, TimeMask.Present, 1f, 18f);
            Ambient("EchoMurmurs", a, new Vector3(-12f, 2f, -11f), AmbientKind.Crowd, 0.18f, TimeMask.Past, 1f, 14f);
            Ambient("FutureWing", a, Polar(WingR, 0f, 3f), AmbientKind.FutureDrone, 0.4f, TimeMask.Future, 1f, 14f);
            Ambient("MemoryHum", a, new Vector3(0f, 2f, 1.5f), AmbientKind.Nursery, 0.18f, TimeMask.All, 1f, 9f);
            Ambient("BalconyWind", a, new Vector3(0f, 3f, 59f), AmbientKind.Wind, 0.55f, TimeMask.All, 1f, 22f);
        }
    }
}
