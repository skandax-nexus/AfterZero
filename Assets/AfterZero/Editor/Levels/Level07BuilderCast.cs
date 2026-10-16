using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    public static partial class Level07Builder
    {
        static readonly Vector3 GateFront = new Vector3(-22f, 0f, -24.6f);
        static readonly Vector3 SquareSW = new Vector3(-12f, 0f, -20f);

        // =====================================================================
        // Evacuation: three groups on three routes + the people of the square
        // =====================================================================

        static void BuildEvacuation(Transform root, Transform shared, Transform past, Transform present, Transform future, Transform logic)
        {
            var people = Group("People", shared);
            var safe = Marker("SafeZone (Tram Hall)", people, SafePos);

            // Shared stalls and the Founders' statues (Aurel Halden in the middle).
            var market = Group("Market", shared);
            Stall(market, new Vector3(20f, 0f, 12f), "fabric_red", 1);
            Stall(market, new Vector3(25.5f, 0f, 12f), "fabric_teal", 2);
            Stall(market, new Vector3(22f, 0f, -1f), "fabric_gold", 3);
            Stall(market, new Vector3(41f, 0f, -1.2f), "fabric_red", 4);
            Box("Crates", market, new Vector3(42.8f, 0.45f, 3.2f), new Vector3(1.2f, 0.9f, 1.2f), "wood");
            var statues = Group("Founders' Statues", shared);
            string[] founders = { "SELA VEYRA", "AUREL HALDEN", "OREN VEYRA" };
            float[] sx = { -6f, 0f, 6f };
            for (int i = 0; i < 3; i++)
            {
                var b = new Vector3(sx[i], 0f, i == 1 ? 18.5f : 17f);
                Box("Plinth", statues, b + Vector3.up * 0.7f, new Vector3(1.8f, 1.4f, 1.8f), "stone_warm");
                Prim(PrimitiveType.Capsule, "Founder", statues, b + Vector3.up * 2.7f, new Vector3(0.9f, 1.3f, 0.7f), "copper_verdigris");
                Sphere("Head", statues, b + Vector3.up * 4.3f, 0.35f, "copper_verdigris", false);
                Label("FounderName", statues, b + new Vector3(0f, 1.15f, -0.92f), 0f, founders[i], 0.3f, new Color(1f, 0.9f, 0.7f));
            }
            Label("StatueZero", statues, new Vector3(0f, 5.6f, 18.5f), 0f, "(0)", 1.4f, new Color(0.8f, 0.95f, 1f));
            Clue("HaldenInscription", statues, new Vector3(0f, 0.55f, 17.55f), new Vector3(1.2f, 0.5f, 0.12f), "brass", "Read Founder Halden's inscription", "L07.haldenRead", "L07.halden",
                "Plinth of Founder Aurel Halden",
                "AUREL HALDEN -- FOUNDER, KEEPER OF THE SEAL\n\nWHAT WE HOLD TOGETHER, WE HOLD IN SILENCE.\n\nBelow, scratched into the bronze much later by an unsteady hand:\n'You held us in silence. We fell in silence. -- a daughter of Oskel Lane'",
                true);

            // ---- Group 1: Mirela Dovan's market group (Market Row: fire in the PRESENT, out through the PAST) ----
            Evac("Market Row group", people, new Vector3(40.5f, 0f, 1.2f), 4, F_G1, safe, 7101,
                new[] { L("Vendor", "Through the fire? -- No, it's... gone? Where are we? Just follow, just follow!"), L("Mirela Dovan", "Stay behind Arin. Don't look at the flames.") },
                "Mirela's market group is safe in the Tram Hall.", new[] { "The stalls are burning!", "Where is Mirela?" });
            var mirela = Npc("Mirela Dovan", people, new Vector3(38.5f, 0f, -0.5f), 270f, "coat_ochre", true, 1, "cup_blue");
            Route(mirela, false,
                (new Vector3(37.5f, 0f, -0.6f), 3f, NPCActivity.Carry, "My cups -- leave the cups!"),
                (new Vector3(42.5f, 0f, 2.6f), 4f, NPCActivity.Work, ""),
                (new Vector3(39f, 0f, 3f), 3f, NPCActivity.Look, "The fire's between us and everything."));
            Route(mirela, true,
                (new Vector3(36f, 0f, 1f), 0f, NPCActivity.Walk, ""),
                (new Vector3(28f, 0f, -4f), 0f, NPCActivity.Walk, ""),
                (SquareSW, 0f, NPCActivity.Walk, ""),
                (GateFront, 0f, NPCActivity.Walk, ""),
                (SafePos + new Vector3(-3f, 0f, 1.5f), 99999f, NPCActivity.Talk, "Count heads. All here. All here."));
            var mp = Persona(mirela, "Cup vendor of Market Row. Packing her blue cups into a crate while the lane burns, refusing to leave her neighbours.",
                new[] { "Don't come through the fire!", "Arin? I know your face. From the festival." },
                new[]
                {
                    L("Mirela Dovan", "The whole lane caught at the first toll. The fire is between us and the square."),
                    L("Mirela Dovan", "My grandfather Hale built carts for this market. He used to say the lane is older than the fire."),
                    L("Mirela Dovan", "Older than the fire... I never understood that. Unless he meant the lane was here before it burned."),
                    L("Mirela Dovan", "If there's another way through, it isn't tonight's way."),
                    L("Mirela Dovan", "Get my people out, Arin. I'll walk behind them."),
                },
                "L01.cupSaved", "", new[] { L("Mirela Dovan", "You caught my blue cup once. On a festival morning, before it hit the stones."), L("Mirela Dovan", "I dreamed it, I thought. But the cup is still on my shelf, and it has never once been broken.") },
                F_G1, new[] { L("Mirela Dovan", "Every one of them, safe. Through a lane that wasn't burning. How?"), L("Mirela Dovan", "Never mind how. Thank you, Arin. Go stop that bell.") });
            AddCond(mp, F_Plan, "", L("Mirela Dovan", "Market Row on the Founders' plan? Struck out nine days early?"), L("Mirela Dovan", "They took the children from the lane and left us the cups."));

            // ---- Group 2: Pell and Lise Ashgrove (courtyard behind the cracked arcade wall) ----
            Evac("Ashgrove courtyard group", people, new Vector3(-49f, 0f, 4.5f), 3, F_G2, safe, 7201,
                new[] { L("Resident", "The lane's open? The wall held? Then go -- we're with you!"), L("Pell Ashgrove", "Lise, hold my coat. We're going.") },
                "Pell, Lise and the courtyard group are safe in the Tram Hall.", new[] { "The wall! Is the wall coming down?", "Pell, which way?" });
            var pell = Npc("Pell Ashgrove", people, new Vector3(-47f, 0f, 0f), 90f, "coat_navy", true, 2, "paper");
            Route(pell, false,
                (new Vector3(-51f, 0f, -4f), 4f, NPCActivity.Work, "Hold, door. Hold."),
                (new Vector3(-45f, 0f, 7f), 3f, NPCActivity.Carry, ""),
                (new Vector3(-51.5f, 0f, 8f), 3f, NPCActivity.Look, "Lise! Stay where I can see you."));
            Route(pell, true,
                (new Vector3(-44f, 0f, 1.5f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-30f, 0f, 1.5f), 0f, NPCActivity.Walk, ""),
                (SquareSW, 0f, NPCActivity.Walk, ""),
                (GateFront, 0f, NPCActivity.Walk, ""),
                (SafePos + new Vector3(3f, 0f, 1.5f), 99999f, NPCActivity.Idle, "We made it. Lise, we made it."));
            var pp = Persona(pell, "Courier and Rememberer. Shoring the courtyard door with his satchel strap while keeping his daughter Lise in sight.",
                new[] { "Arin. Again. I always find you again.", "Don't stand under that wall!" },
                new[]
                {
                    L("Pell Ashgrove", "I've delivered your letter more times than there are days. I know your face better than my own street."),
                    L("Pell Ashgrove", "Tonight there's no letter. Just the wall, and us behind it."),
                    L("Pell Ashgrove", "Odile keeps the tally of every night. I keep the tally of Lise. Every night she's nine. Every night she's here."),
                    L("Pell Ashgrove", "If the loop ever breaks... I don't know if she comes with us. Odile says she might not."),
                    L("Pell Ashgrove", "Brace that wall and get us out. I'll worry about dawn at dawn."),
                },
                "evidence.list", "", new[] { L("Pell Ashgrove", "The evacuation list. Ashgrove isn't on it. Lise isn't on anything."), L("Pell Ashgrove", "She was born inside the repeat. The Founders never planned for anyone like her.") },
                F_G2, new[] { L("Pell Ashgrove", "Out of the courtyard, both of us. Whatever happens at dawn -- tonight you got her out."), L("Pell Ashgrove", "Odile will want the truth published. I want it too. I just want Lise more.") });
            AddCond(pp, F_Braced, "", L("Pell Ashgrove", "The wall's standing straight? Those old poles rusted into the brick..."), L("Pell Ashgrove", "Somebody locked them a century ago. You, I'd bet. You always come the long way."));

            var lise = Npc("Lise Ashgrove", people, new Vector3(-49.5f, 0f, 7.5f), 120f, "coat_burgundy", true, 6);
            lise.transform.localScale = Vector3.one * 0.72f;
            Route(lise, false,
                (new Vector3(-49.5f, 0f, 7.5f), 3f, NPCActivity.Look, "Is it fireworks?"),
                (new Vector3(-46f, 0f, 9.5f), 3f, NPCActivity.Work, ""),
                (new Vector3(-50.5f, 0f, 2f), 3f, NPCActivity.Idle, "I'm counting the bells. One."));
            Route(lise, true,
                (new Vector3(-44f, 0f, 2.5f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-30f, 0f, 2.5f), 0f, NPCActivity.Walk, ""),
                (SquareSW + new Vector3(0f, 0f, 1f), 0f, NPCActivity.Walk, ""),
                (GateFront + new Vector3(1f, 0f, 0f), 0f, NPCActivity.Walk, ""),
                (SafePos + new Vector3(4f, 0f, 2.2f), 99999f, NPCActivity.Look, "Only one bell. Only one!"));
            var lp = Persona(lise, "Pell's nine-year-old, born inside the loop. Drawing chalk zeros on the courtyard stones to stay brave.",
                new[] { "Are you the one who catches cups?", "Papa says don't talk to people who remember." },
                new[]
                {
                    L("Lise Ashgrove", "I'm Lise. I'm nine. I've been nine for ever so long."),
                    L("Lise Ashgrove", "I draw zeros on the stones because they come back every morning. Nothing else I draw comes back."),
                    L("Lise Ashgrove", "At the festival I always run into the cup stall. Always. Even when I try not to."),
                    L("Lise Ashgrove", "Papa holds my hand very tight at night. Like I might go somewhere."),
                    L("Lise Ashgrove", "When the wall stops leaning, can we go? Papa says only when the wall stops leaning."),
                },
                "arin.watch", "", new[] { L("Lise Ashgrove", "Is that a watch? It isn't ticking."), L("Lise Ashgrove", "Nothing here ticks. Papa says time only goes round, not forward.") },
                F_G2, new[] { L("Lise Ashgrove", "We got out! I'm going to draw you a zero. A big one."), L("Lise Ashgrove", "Will I still be here tomorrow? Papa won't say.") });
            AddCond(lp, "L01.cupSaved", "", L("Lise Ashgrove", "You're the one who caught the cup when I bumped the stall!"), L("Lise Ashgrove", "Mirela wasn't cross that time. She's always cross. That time she laughed."));
            Lamp("CourtLamp", shared, new Vector3(-52f, 0f, -4f), "gas", 3.6f);

            // ---- Group 3: Warden Roe's drill group, trapped behind the tower (out through the FUTURE) ----
            Evac("Warden's drill group", people, new Vector3(-1f, 0f, 43.4f), 4, F_G3, safe, 7301,
                new[] { L("Drill volunteer", "The alley's clear? In this light it looks like ruins... go, go, we're behind you!"), L("Warden Roe", "Single file! Follow Arin!") },
                "Warden Roe's group is safe in the Tram Hall.", new[] { "We're trapped back here!", "Is that light coming from the tower?" });
            var warden = Npc("Warden Roe", people, new Vector3(-4f, 0f, 43f), 90f, "coat_navy", true, 4);
            Route(warden, false,
                (new Vector3(-4f, 0f, 43f), 4f, NPCActivity.Work, "Signal lantern's lit. Somebody will see it."),
                (new Vector3(3f, 0f, 44.6f), 3f, NPCActivity.Look, ""),
                (new Vector3(5.5f, 0f, 42.4f), 3f, NPCActivity.Idle, "Rubble on one side, a locked gate in my grandmother's day... and sparks in between."));
            Route(warden, true,
                (new Vector3(9f, 0f, 43.4f), 0f, NPCActivity.Walk, ""),
                (new Vector3(11.5f, 0f, 30.5f), 0f, NPCActivity.Walk, ""),
                (new Vector3(9f, 0f, 22f), 0f, NPCActivity.Walk, ""),
                (SquareSW, 0f, NPCActivity.Walk, ""),
                (GateFront, 0f, NPCActivity.Walk, ""),
                (SafePos + new Vector3(0f, 0f, 2.6f), 99999f, NPCActivity.Idle, "Drill group, all present."));
            NoShadow(Sphere("WardenLantern", shared, new Vector3(-4.6f, 2.1f, 44.6f), 0.25f, "neon_amber", false));
            Box("WardenLanternPost", shared, new Vector3(-4.6f, 1f, 44.6f), new Vector3(0.2f, 2f, 0.2f), "metal_dark");
            var wp = Persona(warden, "Square warden. Running the evacuation drill from a signal lantern behind the tower, where his drill group got cut off.",
                new[] { "Arin! You found us!", "Mind the arcs -- watch for the pink light!" },
                new[]
                {
                    L("Warden Roe", "Twenty years of drills. Tonight I led my volunteers the wrong way round the tower."),
                    L("Warden Roe", "Rubble came down at the first toll and sealed Keeper's Alley. In the old days there was a locked yard gate there instead."),
                    L("Warden Roe", "And when the light goes strange and violet, the alley's open -- but it spits lightning."),
                    L("Warden Roe", "There's a cable by the fallen pylon in the market, if the market's even there in that light. Ground the sparks."),
                    L("Warden Roe", "Then come back for us. The Tram Hall, not the stairs in the open. Halden will be at the gate."),
                },
                "L07.observed", "", new[] { L("Warden Roe", "You saw us from the terrace? Then somebody was watching the drill after all."), L("Warden Roe", "Good. Watching is half of a warden's job. The other half is counting.") },
                F_G3, new[] { L("Warden Roe", "Drill group, all present. In twenty years I never got everyone out. Tonight I nearly didn't."), L("Warden Roe", "Thank you, Arin.") });
            AddCond(wp, F_Halden, "", L("Warden Roe", "Founder Halden's plinth? My grandfather called that motto a promise."), L("Warden Roe", "Read the scratches under it and tell me it isn't an apology."));

            // ---- Odile Fenwick, tallying the evacuation ----
            var odile = Npc("Odile Fenwick", people, new Vector3(0f, 0f, -20f), 0f, "coat_grey", true, 7, "paper");
            Box("TallyPost", shared, new Vector3(-4.5f, 0.9f, -19f), new Vector3(0.4f, 1.8f, 0.4f), "metal_dark");
            NoShadow(Box("TallyBoard", shared, new Vector3(-4.5f, 1.9f, -19.25f), new Vector3(0.9f, 0.7f, 0.05f), "paper", false));
            Route(odile, false,
                (new Vector3(-3.8f, 0f, -19.6f), 4f, NPCActivity.Work, "Name, street, age. Every one of them."),
                (new Vector3(4f, 0f, -19f), 3f, NPCActivity.Look, "Tram Hall! South-west!"),
                (new Vector3(0f, 0f, -16.5f), 3f, NPCActivity.Idle, ""));
            Route(odile, true,
                (new Vector3(-8f, 0f, -22f), 0f, NPCActivity.Walk, ""),
                (GateFront + new Vector3(-1f, 0f, 0f), 0f, NPCActivity.Walk, ""),
                (SafePos + new Vector3(-4f, 0f, -1f), 99999f, NPCActivity.Work, "Every name goes in the book tonight. In ink."));
            var op = Persona(odile, "Head archivist and Rememberer. Writing every evacuee's name on a tally board, so that this time nobody is struck from the record.",
                new[] { "Arin. You remember me this time -- I can see it.", "Tally first, panic later." },
                new[]
                {
                    L("Odile Fenwick", "I keep the record honest. Tonight that means writing down everyone who gets out -- and everyone who doesn't."),
                    L("Odile Fenwick", "Founder Halden is at the Tram Hall gate. He'd rather people die quietly than live loudly."),
                    L("Odile Fenwick", "Show him proof and he may break. The list, the record, the register. Without them... there's the back lane."),
                    L("Odile Fenwick", "The back lane runs from the arcade courtyard to the hall's service door. Mother Ondine's Keepers walk it."),
                    L("Odile Fenwick", "The Keepers want this night to go on forever. The Rememberers want it to end and be known. Tonight, we just want it survived."),
                },
                "evidence.record", "", new[] { L("Odile Fenwick", "You took Maren Oskel's record out of my archive. The one with your countersignature."), L("Odile Fenwick", "Keep it close. Halden signed the order that made it necessary.") },
                F_TramRoute, new[] { L("Odile Fenwick", "The hall's open. I'll take the names at the platform."), L("Odile Fenwick", "Go get the others, Arin. I'm leaving room in the book.") },
                F_Start);
            AddCond(op, "choice.L05.records", "", L("Odile Fenwick", "The class register -- you brought it out of the Drowned District."), L("Odile Fenwick", "Do you know how many names in it were struck? I do. I've counted them every night."));

            // ---- Ilse Marrow, the Founders' clockkeeper ----
            var ilse = Npc("Ilse Marrow", people, new Vector3(-3f, 0f, 19.2f), 180f, "coat_olive", true, 5, "iron_rust");
            Route(ilse, false,
                (new Vector3(-3f, 0f, 19.2f), 5f, NPCActivity.Work, "Hold, you old engine. Hold."),
                (new Vector3(3.2f, 0f, 19.2f), 4f, NPCActivity.Work, ""),
                (new Vector3(9.5f, 0f, 21.5f), 3f, NPCActivity.Look, "Which of you is listening to it?"));
            Route(ilse, true,
                (new Vector3(0f, 0f, 25.2f), 0f, NPCActivity.Walk, ""),
                (new Vector3(0f, 0f, 28.8f), 0f, NPCActivity.Walk, ""),
                (new Vector3(2.4f, 0f, 30.6f), 99999f, NPCActivity.Work, "I've got it. Climb, Arin -- I'll hold it from here."));
            var ip = Persona(ilse, "The Founders' clockkeeper. Fighting the three bell drives with a spanner, refusing to leave the foot of her tower.",
                new[] { "Stay back from the drives -- they kick!", "You. You've got the look of someone who's seen this before." },
                new[]
                {
                    L("Ilse Marrow", "Ilse Marrow. Clockkeeper. That bell has not obeyed me since the first toll."),
                    L("Ilse Marrow", "Three drives under the bell. One of them is listening to something that isn't me."),
                    L("Ilse Marrow", "My brother Ignatz is running the trams tonight. If you see him, tell him I'm staying."),
                    L("Ilse Marrow", "Don't touch the drives blind. The wrong one throws the whole bell forward and the clock jumps."),
                    L("Ilse Marrow", "You have a face I raised once. That's impossible. Everything tonight is impossible -- so help me anyway."),
                },
                F_Signal, "", new[] { L("Ilse Marrow", "The relay showed [#] -- and a mark? A zero in one stroke, with a hook..."), L("Ilse Marrow", "That isn't a Founder's seal. It's a signature. It's your hand, Arin. How can it be your hand?") },
                F_TowerOpen, new[] { L("Ilse Marrow", "The door's open. I'm going in -- someone has to hold the engine while you climb."), L("Ilse Marrow", "Keep time for me.") });
            AddCond(ip, "arin.watch", "", L("Ilse Marrow", "That watch. Brass, dented at the crown. 'For A.'"), L("Ilse Marrow", "I haven't engraved it yet. I've been meaning to. For a child who hums all four notes."));
            AddCond(ip, F_Notes, "", L("Ilse Marrow", "My notes, from the festival year. I was younger than you are now."), L("Ilse Marrow", "Even then I could hear it choosing. I just never knew who it was listening to."));

            // ---- Tobiah Venn, bell ringer ----
            var tobiah = Npc("Tobiah Venn", people, new Vector3(8.5f, 0f, 19.6f), 200f, "coat_burgundy", true, 3, "iron_rust");
            Route(tobiah, false,
                (new Vector3(8.6f, 0f, 19.6f), 4f, NPCActivity.Work, "Oil won't help. It's not the gears that are wrong."),
                (new Vector3(4f, 0f, 15.5f), 3f, NPCActivity.Look, ""),
                (new Vector3(10f, 0f, 14f), 3f, NPCActivity.Idle, "I didn't pull any rope."));
            Route(tobiah, true,
                (new Vector3(4f, 0f, 10f), 0f, NPCActivity.Walk, ""),
                (SquareSW, 0f, NPCActivity.Walk, ""),
                (GateFront + new Vector3(1.5f, 0f, 0f), 0f, NPCActivity.Walk, ""),
                (SafePos + new Vector3(2f, 0f, -1.8f), 99999f, NPCActivity.Idle, "Thirty years, and I never once rang it twice."));
            var tp = Persona(tobiah, "Bell ringer. Oiling the drive linkages because it's the only thing his hands know how to do tonight.",
                new[] { "Don't touch the drives unless you know which!", "The bell's never rung on its own before." },
                new[]
                {
                    L("Tobiah Venn", "I didn't pull any rope. The bell rang itself."),
                    L("Tobiah Venn", "One of the three drives is striking on its own: the spire, the wheel or the cage."),
                    L("Tobiah Venn", "Disengage the wrong one and the whole bell lurches -- you'll lose minutes we haven't got."),
                    L("Tobiah Venn", "Ilse says the drive that's wrong will want a tune to stop, not a lever. She won't say which tune."),
                    L("Tobiah Venn", "Something in the future is talking to my bell. Find out what it's saying."),
                },
                "L06.lullabyComplete", "", new[] { L("Tobiah Venn", "You know the Nursery song? All four notes?"), L("Tobiah Venn", "Then you know the drive's tune. One, three, five, four -- the founders built the bell around a lullaby.") },
                F_Bell, new[] { L("Tobiah Venn", "It's stopped. Thirty years I rang that bell, and never feared it till tonight."), L("Tobiah Venn", "Thank you, Arin.") });
            AddCond(tp, F_Halden, "", L("Tobiah Venn", "'What we hold together, we hold in silence.' We sing that line every festival."), L("Tobiah Venn", "I always thought it was about harmony. Tonight it sounds like a secret."));

            Crowd("Sheltering", present, SafePos + new Vector3(0f, 0f, 0.5f), new Vector2(7f, 2f), 4, 7401,
                new[] { "Is everyone out?", "Hold my hand.", "Listen -- the tower's ticking louder." }, "fabric_teal");

            // Objective bookkeeping: a group objective resolves when the group is saved, or once the
            // signal is read (groups left behind still matter for the Epilogue via L07.groupNSaved).
            string[] saved = { F_G1, F_G2, F_G3 };
            for (int i = 0; i < 3; i++)
            {
                var a = Reactor("Group" + (i + 1) + "Resolved", logic, saved[i]); a.setFlagWhenTrue = "L07.g" + (i + 1) + "Resolved"; a.reversible = false;
                var b = Reactor("Group" + (i + 1) + "LeftBehind", logic, F_Signal); b.setFlagWhenTrue = "L07.g" + (i + 1) + "Resolved"; b.reversible = false;
            }
            var all = Reactor("AllGroupsSaved", logic, F_G1, F_G2, F_G3);
            all.setFlagWhenTrue = F_All;
            all.announce = true;
            all.cue = Cue.PuzzleSolved;
            all.toast = "Founders' Square is clear. All three groups are in the Tram Hall.";
            all.reversible = false;
        }

        // =====================================================================
        // Founder Aurel Halden at the Tram Hall gate; Mother Ondine Halden at the climax
        // =====================================================================

        static void BuildHalden(Transform root, Transform shared, Transform logic)
        {
            var g = Group("Antagonists", shared);

            // ---- Founder Aurel Halden ----
            var halden = Npc("Founder Aurel Halden", g, new Vector3(-22f, 0f, -24.3f), 0f, "coat_white", true, 11, "paper");
            Route(halden, false,
                (new Vector3(-24.5f, 0f, -24.3f), 4f, NPCActivity.Idle, "No one goes down in public."),
                (new Vector3(-19.8f, 0f, -24.3f), 3f, NPCActivity.Look, ""),
                (new Vector3(-22f, 0f, -23.4f), 4f, NPCActivity.Work, "Every name I write here, I wrote once before."));
            Route(halden, true,
                (new Vector3(-22f, 0f, -27f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-26f, 0f, -30f), 99999f, NPCActivity.Work, "Names, in public. I should have done this forty years ago."));
            var hp = Persona(halden, "Founder. Guarding the Tram Hall gate with the Founders' ledger, deciding who may be told what the Founders did.",
                new[] { "Stop there. This gate is not for spectators.", "You have Ilse's way of standing. Curious." },
                new[]
                {
                    L("Founder Aurel Halden", "I am Aurel Halden. I signed the evacuation. I signed the erasures. I signed the word 'earthquake'."),
                    L("Founder Aurel Halden", "If the square sees these people go down into the Tram Hall, the square will ask why there was a plan."),
                    L("Founder Aurel Halden", "And then they will find the list. Fourteen children, nine days early. And the families we struck out."),
                    L("Founder Aurel Halden", "I knew the rescue might fail. I knew it when I signed. Silence was the only part I could guarantee."),
                    L("Founder Aurel Halden", "Bring me proof that silence has already failed, and I will open the gate myself."),
                },
                "evidence.list", "", new[] { L("Founder Aurel Halden", "Where did you find that list? Nine days early, in my own hand."), L("Founder Aurel Halden", "VALE, A. Guardian: I. Marrow... You. You were one of them.") },
                F_HYield, new[] { L("Founder Aurel Halden", "The gate is open. Let the square watch."), L("Founder Aurel Halden", "My daughter will never forgive me. Tell her I chose it, at least.") });
            AddCond(hp, F_Plan, "", L("Founder Aurel Halden", "You read the plan in my hall. Then you've seen 'call it an earthquake' in my handwriting."), L("Founder Aurel Halden", "It was the easiest line I ever wrote. That should have frightened me."));
            var hst = Box("FoundersLedger", g, new Vector3(-19.2f, 0.6f, -24.8f), new Vector3(0.6f, 1.2f, 0.5f), "wood_dark");
            NoShadow(Box("LedgerBook", g, new Vector3(-19.2f, 1.23f, -24.8f), new Vector3(0.55f, 0.06f, 0.4f), "paper", false));
            Label("LedgerTag", g, new Vector3(-19.2f, 1.6f, -24.8f), 0f, "FOUNDERS' LEDGER", 0.22f, new Color(1f, 0.9f, 0.7f));
            var dlg = Dialogue(hst, "Founder Aurel Halden", "Founder Halden bars the Tram Hall gate. What do you do?");
            dlg.prompt = "Confront Founder Halden";
            dlg.doneFlag = F_HTalked;
            dlg.opening.Add(L("Founder Aurel Halden", "Not through this gate. If they go down in front of the whole square, all of Veyra learns what the Founders did.", 4.5f));
            dlg.opening.Add(L("Arin", "They'll die out here if they don't.", 2.5f));
            dlg.opening.Add(L("Founder Aurel Halden", "I know. I knew forty years ago. I signed the evacuation knowing the rescue might fail -- and I signed it anyway.", 5f));
            dlg.opening.Add(L("Founder Aurel Halden", "Silence saved more of them than the truth would have. Prove me wrong. Or go around.", 4f));
            var full = Option("Show him the list, the record and the register.", F_HYield,
                L("Founder Aurel Halden", "The list. The Oskel girl's record -- countersigned. The school register, out of the water.", 4.5f),
                L("Founder Aurel Halden", "Then the silence is already broken, and I am only standing in a doorway. Go. Take them down.", 4.5f));
            full.requiresFlags.AddRange(new[] { "evidence.list", "evidence.record", "choice.L05.records" });
            full.missingText = "(needs the list, the record and the register)";
            dlg.options.Add(full);
            dlg.options.Add(Option("Lay out whatever proof you have.", "L07.haldenEvidence",
                L("Arin", "This is what I carried out of the archive, the tunnels and the water. Look at it.", 3.5f)));
            dlg.options.Add(Option("Then I'll find another way.", "L07.haldenRefused",
                L("Founder Aurel Halden", "The back lane is full of my daughter's Keepers. Ondine is thorough.", 3.5f),
                L("Arin", "So am I.", 2f)));
            var check = new GameObject("HaldenEvidenceCheck").AddComponent<EvidenceCheck>();
            check.transform.SetParent(logic, false);
            check.triggerFlag = "L07.haldenEvidence";
            check.requiredFlags.AddRange(new[] { "evidence.list", "evidence.record", "choice.L05.records" });
            check.minimum = 2;
            check.passFlag = F_HYield;
            check.failFlag = "L07.haldenRefused";
            check.passToast = "Two proofs are enough. Founder Halden steps aside.";
            check.failToast = "Not enough proof. Halden won't move. Take the back lane: arcade courtyard, south door.";
            var refused = Reactor("HaldenRefused", logic, "L07.haldenRefused");
            refused.toast = "The back lane: through the braced arcade to the courtyard, then the south door. Keepers patrol it in the PRESENT.";

            // ---- Mother Ondine Halden (appears once the signal is read) ----
            var ondine = Npc("Mother Ondine Halden", g, new Vector3(-10f, 0f, 21f), 120f, "coat_grey", true, 12);
            Prim(PrimitiveType.Cylinder, "Hood", ondine.transform, new Vector3(-10f, 1.75f, 21f), new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            Route(ondine, false,
                (new Vector3(-10f, 0f, 21f), 4f, NPCActivity.Work, "Lanterns to the back lane. All of them."),
                (new Vector3(-12f, 0f, 18f), 3f, NPCActivity.Look, ""),
                (new Vector3(-9.5f, 0f, 15f), 3f, NPCActivity.Idle, "Inside the cycle, nobody dies for good."));
            Route(ondine, true,
                (new Vector3(-15f, 0f, 22f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-25f, 0f, 26.5f), 0f, NPCActivity.Walk, ""),
                (new Vector3(-25f, 0f, 30f), 99999f, NPCActivity.Idle, "Father's hall. Father's silence. Mine now."));
            var onp = Persona(ondine, "Mother of the Keepers of the Hour, Aurel's daughter. Directing her lantern-bearers from the forecourt so the night is never broken.",
                new[] { "The anomaly. Closer than I'd like.", "Don't touch my father's bell." },
                new[]
                {
                    L("Mother Ondine Halden", "I am Ondine Halden. The Keepers keep the hour, because the hour keeps us."),
                    L("Mother Ondine Halden", "Inside the cycle nobody dies for good. Every child on that list comes back every morning."),
                    L("Mother Ondine Halden", "You call it a prison. I call it the only mercy anyone in this city was ever given."),
                    L("Mother Ondine Halden", "My father bought it with silence. I keep it with lanterns."),
                    L("Mother Ondine Halden", "Stop the bell, and tomorrow comes. Have you asked who tomorrow doesn't have room for?"),
                },
                F_HYield, "", new[] { L("Mother Ondine Halden", "My father stepped aside? Then he's finally tired."), L("Mother Ondine Halden", "I am not.") },
                F_Bell, new[] { L("Mother Ondine Halden", "You stopped it. Tonight. It will want to ring again, and I will be listening."), L("Mother Ondine Halden", "Go on down. Whatever you find under the bell, remember I warned you.") });
            AddCond(onp, "maren.isWitness", "", L("Mother Ondine Halden", "The Witness is the Oskel girl? Grown, in the seam?"), L("Mother Ondine Halden", "Then even she chose to stay inside time's mercy. Ask her why."));
            var oHost = new GameObject("OndineHorn");
            oHost.transform.SetParent(g, false);
            var odlg = Dialogue(oHost, "Mother Ondine Halden", "Mother Ondine speaks through the Keepers' horns.", "L07.ondineCue");
            var cue = Zone("OndineCue", logic, new Vector3(11.5f, 1f, 25.5f), new Vector3(9f, 2f, 2f), "L07.ondineCue");
            cue.requiredFlags.Add(F_Signal);
            odlg.lockControls = true;
            odlg.opening.Add(L("Mother Ondine Halden", "So you've read the signal. The Keepers' horns carry into every state, anomaly. I hear you.", 4.5f));
            odlg.opening.Add(L("Mother Ondine Halden", "Inside the cycle no one dies for good. My father bought that with silence. Don't you dare sell it.", 4.5f));
            odlg.options.Add(Option("Nobody lives for good either. The bell stops tonight.", "L07.ondineDefied",
                L("Mother Ondine Halden", "Then I'll make the tower fight you for every note.", 3.5f)));
            var shaken = Option("Your father admitted the rescue might fail. He told me himself.", "L07.ondineShaken",
                L("Mother Ondine Halden", "...He said that? To you?", 3f),
                L("Mother Ondine Halden", "Go, then. I won't add a single stone. But I won't help you either.", 4f));
            shaken.requiresFlags.Add(F_HTalked);
            shaken.missingText = "(speak to Founder Halden first)";
            odlg.options.Add(shaken);
            ondine.gameObject.SetActive(false);
            var appear = Reactor("OndineArrives", logic, F_Signal);
            appear.enableWhenTrue.Add(ondine.gameObject);
            appear.reversible = false;
        }

        // =====================================================================
        // Memory fragments (optional, off the main path)
        // =====================================================================

        static void BuildFragments(Transform shared)
        {
            var g = Group("Memory Fragments", shared);
            Fragment(g, new Vector3(-30.5f, 1.3f, 44.5f), "L07.1", "The Ninth Chair",
                "PAST. A Founders' Council minute, in a clerk's hand: 'Founder Halden asked that a ninth chair be left empty at the table, for whoever comes after us to judge what we did. The chair was left. Nobody sat in it.'");
            Fragment(g, new Vector3(-7f, 4.3f, -37.2f), "L07.2", "The Warden's Terrace",
                "PRESENT. A tally scratched into the terrace rail: '412 festivals. 412 first tolls. I have watched the square empty from up here every time. I have never once seen the second toll from the other side.'");
            Fragment(g, new Vector3(-49f, 1.3f, -33f), "L07.3", "The Back Lane",
                "FUTURE. A note wedged in the lane wall: 'If you are reading this in the back lane, you came the long way, like me. The signal is not a weapon. It is a hand on the door, holding it shut. -- (0)'");
        }
    }
}
