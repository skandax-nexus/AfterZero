using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>LEVEL 01 gameplay: the cup, the cart, the letter, the changed street, NPCs and flow.</summary>
    public static partial class Level01Builder
    {
        class CupRefs
        {
            public GameObject intactHolder, brokenHolder, savedCup, intact, shards;
        }

        static NPCMotor courier, lise;
        static readonly List<LoopScheduledEvent> loopEvents = new List<LoopScheduledEvent>();

        static void Unstatic(Transform t)
        {
            foreach (var tr in t.GetComponentsInChildren<Transform>(true))
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
        }

        // =================================================================
        // The blue cup stall + the loop events
        // =================================================================

        static CupRefs BuildCupStall(Transform logic)
        {
            var r = new CupRefs();
            Vector3 table = new Vector3(-5f, 0.45f, 22.5f);
            Solid("CupTable", table, new Vector3(1.2f, 0.9f, 3.2f), "wood", "wood_dark");
            foreach (var o in new[] { new Vector3(-0.55f, 0f, -1.5f), new Vector3(0.55f, 0f, -1.5f), new Vector3(-0.55f, 0f, 1.5f), new Vector3(0.55f, 0f, 1.5f) })
                Solid("CupStallPost", new Vector3(table.x + o.x, 1.3f, table.z + o.z), new Vector3(0.08f, 2.6f, 0.08f), "wood_dark", "metal_dark");
            Deco("CupCanopy", pastLook, new Vector3(-5f, 2.65f, 22.5f), new Vector3(1.7f, 0.08f, 3.5f), "fabric_teal");
            Deco("CupCanopy", presLook, new Vector3(-5f, 2.65f, 22.5f), new Vector3(1.7f, 0.08f, 3.5f), "fabric_red");
            Label("CupSign", shared, new Vector3(-4.35f, 2.25f, 22.5f), -90f, "MIRELA'S BLUE CUPS", 0.25f, Color.white);
            PointLight("StallLamp", pastLook, new Vector3(-5f, 2.3f, 22.5f), Warm, 1.6f, 5f);
            PointLight("StallLamp", presLook, new Vector3(-5f, 2.3f, 22.5f), Sodium, 2.2f, 6f);

            // Past: a full shelf of brand-new blue cups.
            var pastStall = Group("CupStall_Past", pastPlay);
            for (int i = 0; i < 6; i++)
                Cylinder("NewCup", pastStall, new Vector3(-5.1f + (i % 2) * 0.3f, 0.97f, 21.4f + i * 0.42f), 0.07f, 0.13f, "cup_blue", false);

            // Present: the one cup.
            var stall = Group("CupStall_Present", presPlay);
            for (int i = 0; i < 3; i++)
                Cylinder("PlainCup", stall, new Vector3(-5.2f, 0.97f, 23.3f + i * 0.35f), 0.07f, 0.13f, "plaster_cold", false);
            r.intactHolder = Group("IntactHolder", stall).gameObject;
            r.intact = Cylinder("BlueCup", r.intactHolder.transform, new Vector3(-4.75f, 0.97f, 22.2f), 0.08f, 0.14f, "cup_blue", false);
            r.brokenHolder = Group("BrokenHolder", stall).gameObject;
            var shards = Group("BlueCupShards", r.brokenHolder.transform);
            r.shards = shards.gameObject;
            var rng = new System.Random(5);
            for (int i = 0; i < 7; i++)
            {
                var p = i < 3 ? new Vector3(-4.8f + (float)rng.NextDouble() * 0.3f, 0.915f, 22f + (float)rng.NextDouble() * 0.4f)
                              : new Vector3(-3.9f + (float)rng.NextDouble() * 1.2f, 0.02f, 21.6f + (float)rng.NextDouble() * 1.4f);
                Box("Shard", shards, p, new Vector3(0.06f + (float)rng.NextDouble() * 0.05f, 0.025f, 0.05f), "cup_blue", false, (float)rng.NextDouble() * 180f);
            }
            Label("ShardTag", shards, new Vector3(-4.7f, 1.25f, 22.2f), -90f, "broken", 0.18f, new Color(0.8f, 0.9f, 1f));
            r.savedCup = Group("SavedCup", stall).gameObject;
            Cylinder("BlueCupWhole", r.savedCup.transform, new Vector3(-4.75f, 0.97f, 22.2f), 0.08f, 0.14f, "cup_blue", false);
            Label("SavedTag", r.savedCup.transform, new Vector3(-4.7f, 1.3f, 22.2f), -90f, "whole", 0.18f, new Color(1f, 0.95f, 0.7f));
            Hologram("SavedHalo", r.savedCup.transform, new Vector3(-4.75f, 1.12f, 22.2f), new Vector3(0.3f, 0.01f, 0.3f), PrimitiveType.Cylinder, "hologram_amber", true, true);

            // Loop events (shared, so they keep time in every state).
            var bellPoint = Marker("BellPoint", logic, new Vector3(0f, 39f, 61f));
            var bell1 = Group("Event_Bell1", logic).gameObject.AddComponent<LoopScheduledEvent>();
            bell1.fireAt = 16.2f; bell1.happensIn = TimeMask.All; bell1.cue = Cue.Bell; bell1.cuePoint = bellPoint; bell1.noise = 0f;
            var bell2 = Group("Event_Bell2", logic).gameObject.AddComponent<LoopScheduledEvent>();
            bell2.fireAt = 17.2f; bell2.happensIn = TimeMask.All; bell2.cue = Cue.Bell; bell2.cuePoint = bellPoint; bell2.noise = 0f;

            var cupEvt = Group("Event_CupBreaks", logic).gameObject.AddComponent<LoopScheduledEvent>();
            cupEvt.fireAt = 18.6f;
            cupEvt.happensIn = TimeMask.Present;
            cupEvt.preventedBy.Add(F_Perfect); // only the Perfect Loop saves it
            cupEvt.enableOnFire.Add(r.shards);
            cupEvt.disableOnFire.Add(r.intact);
            cupEvt.cue = Cue.Shatter;
            cupEvt.cuePoint = r.intact.transform;
            cupEvt.persistsAcrossLoops = true;
            cupEvt.startFired = true; // the anomaly: it is already broken when Arin arrives
            cupEvt.preventedFlag = F_CupSaved;
            cupEvt.preventedToast = "The perfect loop. The cart stays, Lise stops, Pell takes the long way - and the blue cup is still whole.";
            loopEvents.Add(bell1); loopEvents.Add(bell2); loopEvents.Add(cupEvt);
            return r;
        }

        // =================================================================
        // The festival cart (Pushable in the Past, rolling hazard in the Present)
        // =================================================================

        static void BuildCart(Transform logic)
        {
            Vector3 cartPos = new Vector3(17f, 2.5f + 0.57f, 22.5f);
            Vector3 cartSize = new Vector3(2.4f, 1.1f, 1.6f);

            // PAST: the brand-new parade cart; push it into the bay.
            var pastLane = Group("Lane_Past", pastPlay);
            var push = Pushable("FestivalCart_Past", pastLane, cartPos, cartSize, "wood", 60f);
            push.resetOnLoop = true; // a stuck cart rewinds with the loop
            push.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Past;
            CartDressing(push.transform, cartPos, cartSize, "fabric_gold", "brass", true);
            Unstatic(push.transform);
            Label("CartTag", pastLane, cartPos + new Vector3(0f, 1.6f, 0f), 0f, "PARADE CART - PUSH", 0.22f, new Color(1f, 0.95f, 0.8f));
            // Curb along the top of the ramp so the cart cannot run away in the Past (player can step over it).
            Box("Curb", pastLane, new Vector3(14.15f, 2.65f, 22.5f), new Vector3(0.3f, 0.3f, 11f), "stone_warm");
            var pp = new Vector3(17f, 3.07f, 29.7f);

            // PRESENT: the old cart waiting on the slope.
            var presLane = Group("Lane_Present", presPlay);
            var slopeHolder = Group("CartOnSlope", presLane);
            var pivot = Group("CartPivot", slopeHolder);
            pivot.position = cartPos;
            Box("CartBody", pivot, cartPos, cartSize, "iron_rust");
            CartDressing(pivot, cartPos, cartSize, "fabric_teal", "metal_dark", false);
            Label("CartTagP", pivot, cartPos + new Vector3(0f, 1.5f, 0f), 0f, "NO BRAKE", 0.2f, new Color(1f, 0.6f, 0.4f));
            Unstatic(pivot);
            var presParked = Group("ParkedCart_Present", presLane).gameObject;
            Box("ParkedBody", presParked.transform, pp, new Vector3(1.6f, 1.1f, 2.4f), "iron_rust");
            CartDressing(presParked.transform, pp, new Vector3(1.6f, 1.1f, 2.4f), "fabric_teal", "metal_dark", false);
            Label("ParkedTag", presParked.transform, pp + new Vector3(0f, 1.3f, -1.3f), 0f, "PARKED - THIS LOOP", 0.18f, new Color(1f, 0.9f, 0.7f));

            // Cart rolls at the second bell (non-persistent: it rewinds with the loop).
            var roll = Group("Event_CartRolls", logic).gameObject.AddComponent<LoopScheduledEvent>();
            roll.fireAt = 18.0f;
            roll.happensIn = TimeMask.Present;
            roll.preventedBy.Add(F_CartChocked);
            roll.setFlagOnFire = F_CartRolled;
            roll.cue = Cue.Clang;
            roll.cuePoint = Marker("CrashPoint", logic, new Vector3(-4f, 0.8f, 22.5f));
            roll.noise = 1f;
            roll.preventedFlag = "L01.cartStayed";
            loopEvents.Add(roll);

            var rollR = Reactor("React_CartRolling", logic, F_CartRolled);
            rollR.moveTarget = pivot;
            rollR.moveBy = new Vector3(-20.0f, -2.5f, 0f);
            rollR.rotateBy = new Vector3(0f, -18f, 0f);
            rollR.duration = 1.1f;
            rollR.reversible = true; // the loop visibly winds the cart back up the lane

            // Causality, per loop: the Past cart is pushed back to the slope every loop (Pushable.resetOnLoop),
            // so parking it in the bay only protects THIS loop (loop.L01.cartChocked). The first time also completes
            // the "learn" objective (L01.cartMovedPast).
            var bay = Zone("BayTrigger", logic, new Vector3(17f, 3.3f, 29.9f), new Vector3(2.6f, 1.6f, 2.2f), F_CartChocked,
                           "The cart rests in the bay - for this loop. In the PRESENT it will not reach the slope.", TimeMask.Past);
            bay.requireObjectNamed = "FestivalCart_Past";
            bay.oneShot = false;

            var moved = Reactor("React_CartParked", logic, F_CartChocked);
            moved.reversible = true;
            moved.disableWhenTrue.Add(slopeHolder.gameObject);
            moved.enableWhenTrue.Add(presParked);
            moved.announce = true;
            moved.cue = Cue.RuleChange;
            moved.setFlagWhenTrue = F_CartMoved;

            var crash = Reactor("React_CrashSeen", logic, F_CartRolled);
            crash.setFlagWhenTrue = F_CrashSeen;
            var crashToast = Reactor("React_CrashToast", logic, F_CrashSeen);
            crashToast.reversible = false;
            crashToast.toast = "The bell rang twice and the cart rolled into the stall. The cup was already broken.";
        }

        static void CartDressing(Transform parent, Vector3 c, Vector3 size, string canopy, string trim, bool attachToBody)
        {
            var tmp = Group("CartDress", parent.parent != null ? parent.parent : parent);
            bool alongX = size.x > size.z;
            float halfLen = (alongX ? size.x : size.z) * 0.5f - 0.45f;
            float halfW = (alongX ? size.z : size.x) * 0.5f + 0.06f;
            for (int i = 0; i < 4; i++)
            {
                float a = (i < 2 ? -1f : 1f) * halfLen;
                float b = (i % 2 == 0 ? -1f : 1f) * halfW;
                Vector3 off = alongX ? new Vector3(a, 0f, b) : new Vector3(b, 0f, a);
                var w = Cylinder("Wheel", tmp, c + off + new Vector3(0f, -size.y * 0.5f + 0.35f, 0f), 0.35f, 0.1f, trim, false);
                w.transform.rotation = Quaternion.Euler(alongX ? 90f : 0f, 0f, alongX ? 0f : 90f);
            }
            Box("Canopy", tmp, c + new Vector3(0f, size.y * 0.5f + 0.75f, 0f), new Vector3(size.x * 0.9f, 0.08f, size.z * 0.9f), canopy, false);
            foreach (var sx in new[] { -1f, 1f })
                Box("CanopyPole", tmp, c + new Vector3(sx * size.x * 0.4f, size.y * 0.5f + 0.37f, 0f), new Vector3(0.06f, 0.75f, 0.06f), trim, false);
            Box("Goods", tmp, c + new Vector3(0f, size.y * 0.5f + 0.15f, 0f), new Vector3(size.x * 0.6f, 0.3f, size.z * 0.6f), canopy == "fabric_gold" ? "fabric_red" : "wood_dark", false);
            foreach (Transform t in new List<Transform>(GetChildren(tmp))) t.SetParent(parent, true);
            Object.DestroyImmediate(tmp.gameObject);
        }

        static IEnumerable<Transform> GetChildren(Transform t)
        {
            var list = new List<Transform>();
            foreach (Transform c in t) list.Add(c);
            return list;
        }

        // =================================================================
        // The letter (posted every loop)
        // =================================================================

        static void BuildLetter(Transform logic)
        {
            var holder = Group("LetterHolder", presPlay);
            var letter = Clue("PostedLetter", holder, new Vector3(-7.3f, 1.4f, 12f), new Vector3(0.45f, 0.05f, 0.34f), "paper",
                "Read the posted letter", F_LetterFound, "L01_letter", "A Rain-Smeared Letter",
                "Addressed to: ARIN, at the Arrival Quarter.\n\nThe ink has run in the rain. Fragments remain:\n\n" +
                "'...you will arrive with the festival... the bell will ring twice... look for the blue cup, it is the one thing the loop cannot mend... " +
                "go in through the clockworks...'\n\nThe date and the wax seal are hidden under a thumbprint of mud. In a different street, it might read differently.");
            letter.usableIn = TimeMask.Present;
            Label("LetterTag", holder, new Vector3(-6.95f, 1.75f, 12f), -90f, "LETTER", 0.18f, new Color(1f, 0.95f, 0.75f));

            var show = Reactor("React_LetterPosted", logic, F_LetterPosted);
            show.enableWhenTrue.Add(holder.gameObject);
            show.cue = Cue.Place;
            show.announce = true;
        }

        // =================================================================
        // The changed street (after the cup is saved)
        // =================================================================

        static void BuildChangedStreet(Transform logic, CupRefs cup)
        {
            var changed = Group("ChangedStreet", presLook);
            // Lantern strings over the street + plaza.
            float[] lz = { 6f, 14f, 22f, 32f, 42f, 52f, 57f };
            for (int i = 0; i < lz.Length; i++)
            {
                float z = lz[i];
                float half = z > 48f ? 11f : 7.5f;
                Box("LanternWire", changed, new Vector3(0f, 6.6f, z), new Vector3(half * 2f, 0.03f, 0.03f), "metal_dark", false);
                for (int k = 0; k < 7; k++)
                {
                    float x = -half + (k + 0.5f) * (half * 2f / 7f);
                    var s = Sphere("Lantern", changed, new Vector3(x, 6.25f - 0.4f * (1f - Mathf.Pow(x / half, 2f)), z), 0.17f, k % 2 == 0 ? "lamp_warm" : "neon_amber", false);
                    s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                if (i % 3 == 0) PointLight("LanternLight", changed, new Vector3(0f, 5.8f, z), Warm, 2.6f, 11f);
                // Fresh banners hanging between lanterns.
                if (i < 5)
                    for (int k = 0; k < 3; k++)
                        Box("Banner", changed, new Vector3(-5f + k * 5f, 4.9f, z + 0.1f), new Vector3(0.8f, 1.6f, 0.03f), k == 1 ? "fabric_gold" : "fabric_red", false);
            }
            Lamp("PlazaLampLit", changed, new Vector3(7f, 0f, 58f), "gas", 4.5f);
            Lamp("PlazaLampLit2", changed, new Vector3(-3f, 0f, 60.5f), "gas", 3.2f);
            PointLight("TowerStepGlow", changed, new Vector3(-1.8f, 1.6f, 60.5f), new Color(1f, 0.85f, 0.55f), 2f, 5f);
            Label("FestivalSign", changed, new Vector3(0f, 7.6f, 47.4f), 0f, "WELCOME TO THE FESTIVAL OF THE ZERO", 0.32f, new Color(1f, 0.9f, 0.6f));
            Crowd("Crowd_Festival", changed, new Vector3(0f, 0f, 53f), new Vector2(16f, 8f), 4, 777,
                  new[] { "The lamps are lit!", "Did the bell stop?", "Listen... no second bell." }, "fabric_gold");
            var ambFest = Ambient("Amb_FestivalReturns", changed, new Vector3(0f, 2f, 52f), AmbientKind.Crowd, 0.45f, TimeMask.Present, 0.6f, 40f);

            var saved = Reactor("React_CupSaved", logic, F_CupSaved);
            saved.reversible = false;
            saved.disableWhenTrue.Add(cup.intactHolder);
            saved.disableWhenTrue.Add(cup.brokenHolder);
            saved.enableWhenTrue.Add(cup.savedCup);
            saved.enableWhenTrue.Add(changed.gameObject);
            var rainMain = presLook.Find("Rain_Main");
            if (rainMain != null) saved.disableWhenTrue.Add(rainMain.gameObject);
            var rainPlaza = presLook.Find("RainPlaza");
            if (rainPlaza != null) saved.disableWhenTrue.Add(rainPlaza.gameObject);
            foreach (var n in new[] { "Amb_RainStreet", "Amb_RainStreet2" })
            {
                var a = shared.Find(n);
                if (a != null) saved.disableWhenTrue.Add(a.gameObject);
            }
            saved.announce = true;
            saved.cue = Cue.PuzzleSolved;
            saved.toast = "The loop is broken. The rain stops - the street is changing.";
            _ = ambFest;

            // The letter waiting at the tower step (payoff).
            var stepHolder = Group("TowerLetterHolder", presPlay);
            Box("Lectern", stepHolder, new Vector3(-1.8f, 0.5f, 60.8f), new Vector3(0.6f, 1f, 0.5f), "wood_dark");
            var truth = Clue("TowerLetter", stepHolder, new Vector3(-1.8f, 1.03f, 60.8f), new Vector3(0.45f, 0.05f, 0.34f), "paper",
                "Read Arin's letter", F_LetterTruth, "L01_letterTruth", "Arin's Letter",
                "To Arin, at the Arrival Quarter.\n\nYou saved the blue cup. Good. Now you know the rule: what you change, stays changed.\n\n" +
                "The first bell is a warning. The second is the end, and the beginning again. The tower was stopped once, from inside. " +
                "Go in through the clockworks and find out who stopped it.\n\nYou have done this before. You will not remember. I do.\n\n   - (0)\n\n" +
                "DATED: tomorrow - the day after the festival.\nSEALED: in wax, with the clock-tower seal (0).\n\n" +
                "The handwriting is yours - older, shakier, but yours. The long tail of the A is the same as the engraving inside your stopped watch:\n'For A. - keep time for me. I.M.'");
            truth.lines.Add(L("Arin", "This is my hand. Older. Shaking. The same A as the one on the watch.", 4f));
            truth.lines.Add(L("The Witness", "You wrote to yourself, Arin. From a tomorrow you haven't lived yet.", 4f));
            truth.usableIn = TimeMask.Present;
            truth.requiredFlags.Add(F_Inspected);
            truth.lockedMessage = "Look around the changed plaza first.";
            Label("TowerLetterTag", stepHolder, new Vector3(-1.8f, 1.6f, 60.6f), 0f, "LETTER - SEALED (0)", 0.18f, new Color(1f, 0.95f, 0.75f));
            var showStep = Reactor("React_TowerLetter", logic, F_CupSaved);
            showStep.enableWhenTrue.Add(stepHolder.gameObject);
        }

        // =================================================================
        // NPCs
        // =================================================================

        static NPCPersona Persona(NPCMotor m, string task, string changedFlag, string[] approach, SubtitleLine[] talk, ConditionalLines[] cond, SubtitleLine[] changedLines, string talkedFlag = "")
        {
            var p = m.GetComponent<NPCPersona>();
            p.task = task;
            p.approachBarks.AddRange(approach);
            p.talkLines.AddRange(talk);
            p.conditional.AddRange(cond);
            p.changedLines.AddRange(changedLines);
            p.talkedFlag = talkedFlag;
            m.changedFlag = changedFlag;
            return p;
        }

        static ConditionalLines WhenHolding(string key, params SubtitleLine[] lines) =>
            new ConditionalLines { whenHolding = key, lines = new List<SubtitleLine>(lines) };

        static ConditionalLines WhenFlag(string flag, params SubtitleLine[] lines) =>
            new ConditionalLines { whenFlag = flag, lines = new List<SubtitleLine>(lines) };

        static void BuildNpcs()
        {
            // --- Mirela Dovan, the cup vendor (Present) ---
            var mirela = Npc("Mirela Dovan", presPlay, new Vector3(-6.5f, 0f, 22.5f), 90f, "coat_burgundy", true, 1, "paper");
            Route(mirela, false,
                (new Vector3(-6.5f, 0f, 22.5f), 6f, NPCActivity.Work, "Blue cups! Fresh from the kiln... well. Fresh once."),
                (new Vector3(-6.6f, 0f, 20.6f), 4f, NPCActivity.Look, ""),
                (new Vector3(-6.4f, 0f, 24.4f), 5f, NPCActivity.Work, "Mind the lane when the bell goes, everyone."));
            Route(mirela, true,
                (new Vector3(-6.5f, 0f, 22.5f), 6f, NPCActivity.Work, "Whole. I had forgotten the glaze has stars in it."),
                (new Vector3(-3.2f, 0f, 26f), 5f, NPCActivity.Talk, ""),
                (new Vector3(-6.4f, 0f, 21f), 5f, NPCActivity.Carry, ""));
            Persona(mirela, "Sells blue glazed cups at the festival stall - and sweeps up the same broken blue cup every loop.", F_CupSaved,
                new[] { "Careful - glass and cups.", "Buying, or just watching the rain?", "Stand clear of the lane when the bell goes." },
                new[]
                {
                    L("Mirela", "That cup? Broken. It's always broken."),
                    L("Mirela", "The bell rings twice, the cart comes down the lane, my table jumps... and then it's raining again and my hands are dry, as if nothing happened.", 5f),
                    L("Mirela", "Everything comes back. The music, the crowd, the courier and his letter. Never the cup."),
                    L("Mirela", "It's never just the cart. Little Lise comes tearing across at the same moment, and Pell stands in the lane like a post.", 4.5f),
                    L("Mirela", "My father built the parade cart, years ago. He always said that slope would be the death of something.", 4f),
                    L("Mirela", "The first bell is the festival bell. The second... nobody here will tell you what the second one is for.", 4f),
                    L("Mirela", "Half the stalls on this street used to belong to families on Lantern Row. Nobody can say where they went. Nobody tries.", 4.5f),
                    L("The Witness", "The cart was set rolling long before tonight, Arin. Some causes are older than the loop.", 4.5f),
                },
                new[]
                {
                    WhenFlag(Clue_Truth,
                        L("Mirela", "Dated tomorrow? There hasn't been a tomorrow in this street for as long as I can remember."),
                        L("Mirela", "If someone in that tower can write from tomorrow... ask them to send me a cup that never breaks.", 4.5f)),
                    WhenHolding("cup_shard",
                        L("Mirela", "Careful, that edge bites. That's a piece of my blue cup."),
                        L("Mirela", "Strange - it's warm. Like it broke a moment ago. Like it breaks every moment.", 4f)),
                    WhenHolding("missing_poster",
                        L("Mirela", "The Oskels. Maren used to buy a cup from me every festival and drop it before she reached the corner.", 4.5f),
                        L("Mirela", "Who took that poster down? Who put it up? I can't remember either, and that frightens me more.", 4.5f)),
                },
                new[]
                {
                    L("Mirela", "You did something, didn't you? The cart never came. The cup is whole... and the rain stopped."),
                    L("Mirela", "The plaza gate is open. Osric is lighting the lamps by the tower. Go and look."),
                },
                F_VendorTalked);

            // --- Pell Ashgrove, the courier (Present) ---
            courier = Npc("Pell Ashgrove", presPlay, new Vector3(1.5f, 0f, 0.3f), 0f, "coat_navy", true, 2, "paper");
            courier.walkSpeed = 1.45f;
            Route(courier, false,
                (new Vector3(2.5f, 0f, 6f), 1.5f, NPCActivity.Carry, "Letter for the postbox! Mind the puddles."),
                (new Vector3(-2f, 0f, 10f), 0.5f, NPCActivity.Carry, ""),
                (new Vector3(-6.6f, 0f, 12f), 6f, NPCActivity.Work, "There. Posted. Same as always... same as always?"),
                (new Vector3(1f, 0f, 21f), 30f, NPCActivity.Look, "Now where's my Lise got to..."));   // stands right in the escape lane
            // Final routine (after the cup is saved): built first, then moved into the switch.
            Route(courier, true,
                (new Vector3(2f, 0f, 30f), 1f, NPCActivity.Carry, ""),
                (new Vector3(1f, 0f, 58.4f), 2f, NPCActivity.Carry, "One letter for the tower. Funny - it was always addressed to you."),
                (new Vector3(3f, 0f, 59.5f), 10f, NPCActivity.Look, ""),
                (new Vector3(5f, 0f, 54f), 8f, NPCActivity.Idle, ""));
            var finalRoute = new List<RoutineStep>(courier.changedRoutine);
            courier.changedRoutine.Clear();
            // Per-loop diversion: talk to Pell and he takes the long way along the shopfronts, clear of the lane.
            Route(courier, true,
                (new Vector3(-6.8f, 0f, 4f), 0.5f, NPCActivity.Carry, "The long way round it is."),
                (new Vector3(-7f, 0f, 9f), 0.5f, NPCActivity.Carry, ""),
                (new Vector3(-6.6f, 0f, 12f), 6f, NPCActivity.Work, "Posted. And out of everyone's way."),
                (new Vector3(-7f, 0f, 16f), 30f, NPCActivity.Look, ""));
            var pellPersona = Persona(courier, "Delivers the festival post: every loop he carries the same sealed letter from the arrival stairs to the postbox.", F_PellDiverted,
                new[] { "Letter for the postbox - mind the satchel!", "Walk with me if you like. The postbox is on the left.", "Oh - it's you. Again. I mean... hello." },
                new[]
                {
                    L("Pell", "Post for the festival! Well - one letter. Always the same one."),
                    L("Pell", "I take it from the arrivals, walk it down the street, post it. Then the bell, and I'm back at the stairs with it in my satchel.", 5f),
                    L("Pell", "It's addressed to an Arin. Funny - that's you, isn't it? Read it when I've posted it, if you like."),
                    L("Pell", "Have we met? You've got a face I keep almost remembering. Like a word on the tip of my tongue."),
                    L("Pell", "I've carried this letter to you more times than there are days. Don't ask me how I know. My feet know it.", 4.5f),
                    L("Pell", "Every time, you read it, and you look up at the tower, and you go a little pale. You're doing it now.", 4.5f),
                    L("Pell", "Take the long way, you say? Along the shopfronts, out of the lane? ...Alright. For you. This loop.", 4.5f),
                    L("Pell", "If you see my Lise running for Mirela's stall, catch her. She never looks where she's going.", 4f),
                },
                new[]
                {
                    WhenFlag(Clue_Truth,
                        L("Pell", "Dated tomorrow, with the tower's seal? I've been carrying tomorrow's post all this time."),
                        L("Pell", "And the hand... it's yours, near enough. Like someone copied you, letter by letter, from very far away.", 4.5f)),
                    WhenHolding("missing_poster",
                        L("Pell", "The Oskels. I had a parcel for them once. Lantern Row, number nine. I knocked and the door wasn't there.", 4.5f),
                        L("Pell", "Not locked - not there. A wall with a little zero scratched where the knocker should be.", 4f)),
                },
                new[]
                {
                    L("Pell", "Long way round, as promised. The lane's all yours."),
                    L("Pell", "Funny. I've walked the middle of that street more times than there are days."),
                }, F_PellDiverted);
            var pellSwitch = courier.gameObject.AddComponent<L01PellSwitch>();
            pellSwitch.motor = courier;
            pellSwitch.persona = pellPersona;
            pellSwitch.finalFlag = F_CupSaved;
            pellSwitch.finalRoutine.AddRange(finalRoute);
            pellSwitch.finalLines.Add(L("Pell", "No bell, no going back to the stairs. I don't know what to do with the rest of the night."));
            pellSwitch.finalLines.Add(L("Pell", "I took the letter up to the tower door. Felt like that's where it belonged."));
            pellSwitch.finalLines.Add(L("Pell", "If I see you again tomorrow - a real tomorrow - I'll say 'welcome back'. And I'll mean it."));

            // --- Lise Ashgrove, Pell's daughter (Present, loop-born) ---
            lise = Npc("Lise Ashgrove", presPlay, new Vector3(6.4f, 0f, 25f), -90f, "coat_ochre", true, 5);
            lise.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            Route(lise, false,
                (new Vector3(6.4f, 0f, 25f), 3f, NPCActivity.Work, "Hop, skip... zero!"),
                (new Vector3(6.6f, 0f, 27.2f), 3f, NPCActivity.Look, ""),
                (new Vector3(5.8f, 0f, 23.6f), 3f, NPCActivity.Idle, "When the bell goes I'm getting a blue cup."));
            Route(lise, true,
                (new Vector3(2f, 0f, 55f), 6f, NPCActivity.Talk, "Papa! The lamps are on!"),
                (new Vector3(4f, 0f, 57f), 5f, NPCActivity.Look, ""),
                (new Vector3(5f, 0f, 54.5f), 6f, NPCActivity.Idle, ""));
            Persona(lise, "Pell's nine-year-old: plays hopscotch at the lane mouth - and at 0:14 of every loop sprints for Mirela's blue cups.", F_CupSaved,
                new[] { "Hello! Are you new? You look new. Again.", "Don't step on the zero!", "Papa's the postman. The only postman." },
                new[]
                {
                    L("Lise", "I'm Lise. Lise Ashgrove. I'm nine. I've been nine for ages."),
                    L("Lise", "When the bell rings I run to Mirela's for a blue cup. I always trip on her table. Always.", 4f),
                    L("Lise", "Papa says I was born on festival night. Every festival night. That's a lot of birthdays and no cake."),
                    L("Lise", "Wait for you? Alright. I'll stay right here and count the bunting. You're funny."),
                    L("Lise", "There's a lady with a lantern who walks up and down and writes things in a little book. She doesn't blink."),
                },
                new[]
                {
                    WhenHolding("cup_shard",
                        L("Lise", "That's from the cup I break! I'm sorry, I'm sorry, I don't mean to - my feet just go.", 4f)),
                    WhenHolding("missing_poster",
                        L("Lise", "Maren! I play with Maren... in dreams, I think. She hums a song with a hole in it."),
                        L("Lise", "You hum it right, though. All four notes. How do you know it?", 3.5f)),
                },
                new[]
                {
                    L("Lise", "Nobody ran! Nobody tripped! Is it tomorrow now?"),
                    L("Lise", "Papa's taking a letter to the tower. Can I come? Can you come?"),
                }, F_LiseStopped);

            // --- Osric Vane, warden of the festival gate (Present) ---
            var osric = Npc("Osric Vane", presPlay, new Vector3(3.5f, 0f, 45.6f), 180f, "coat_grey", true, 3);
            Solid("GateWinch", new Vector3(5.2f, 0.6f, 46.7f), new Vector3(0.8f, 1.2f, 0.8f), "brass", "iron_rust");
            Route(osric, false,
                (new Vector3(4.6f, 0f, 46f), 6f, NPCActivity.Work, "Crank holds. Gate holds. Bell... rings."),
                (new Vector3(-3.5f, 0f, 45.5f), 4f, NPCActivity.Look, ""),
                (new Vector3(0f, 0f, 44f), 5f, NPCActivity.Idle, "Plaza's closed till the bell is quiet. Tower's orders."));
            Route(osric, true,
                (new Vector3(0f, 0f, 52f), 4f, NPCActivity.Look, "Never seen the plaza lamps lit, not in all the rain-years."),
                (new Vector3(-6f, 0f, 57.5f), 6f, NPCActivity.Work, ""),
                (new Vector3(5f, 0f, 55f), 5f, NPCActivity.Idle, ""));
            Persona(osric, "Warden of the festival gate: winds the gate chain and keeps the tower plaza sealed while the bell loops.", F_CupSaved,
                new[] { "Plaza's closed, friend.", "Gate stays down while the bell rings twice.", "Hands off the winch." },
                new[]
                {
                    L("Osric", "Plaza's closed. Tower's orders, since before I took the job."),
                    L("Osric", "Every time the bell rings twice, I'm back at this chain with my hands on the crank. Funny, that."),
                    L("Osric", "Only the tower seals in wax - a ring with a zero in it. If you see that seal, it's the clockkeeper's business."),
                    L("Osric", "The orders came signed by a Founder. Halden. You don't argue with that signature. You don't even read it twice.", 4.5f),
                    L("Osric", "First bell's a warning. Second bell's the... well. Nobody's ever finished that sentence for me.", 4f),
                    L("Osric", "The gate opens when the bell is quiet. In my whole time here, the bell has never once been quiet."),
                },
                new[]
                {
                    WhenFlag(Clue_Truth,
                        L("Osric", "The (0) seal, in fresh wax. That's the clockkeeper's mark - and the keeper's been gone longer than I've been warden.", 4.5f),
                        L("Osric", "If the tower's writing letters again, then someone's awake in the clockworks. Go carefully.")),
                    WhenHolding("cup_shard",
                        L("Osric", "A bit of Mirela's cup. Every night I hear it break from here. Clear as the bell."),
                        L("Osric", "You'd think a man would stop flinching. I never do.")),
                },
                new[]
                {
                    L("Osric", "Gate's up! First time in... I can't count it. The bell let go of us."),
                    L("Osric", "Go on. There's something on the tower step. It wasn't there before."),
                });

            // --- Hale Dovan, the cartwright (Past) ---
            var hale = Npc("Hale Dovan", pastPlay, new Vector3(19.4f, 2.5f, 20.5f), -90f, "coat_ochre", true, 4);
            Route(hale, false,
                (new Vector3(19.4f, 2.5f, 21f), 6f, NPCActivity.Work, "Fresh paint for the parade cart."),
                (new Vector3(19.4f, 2.5f, 26f), 5f, NPCActivity.Work, ""),
                (new Vector3(19.4f, 2.5f, 23f), 4f, NPCActivity.Look, "Lovely slope. Terrible brake."));
            Route(hale, true,
                (new Vector3(19.4f, 2.5f, 27f), 6f, NPCActivity.Look, "Snug in the bay. Should have built her there from the start."),
                (new Vector3(19.4f, 2.5f, 21f), 6f, NPCActivity.Work, ""),
                (new Vector3(19.4f, 2.5f, 24f), 4f, NPCActivity.Idle, ""));
            Persona(hale, "Builds and paints the new parade cart and greases its wheels for the festival.", F_CartMoved,
                new[] { "Afternoon! Mind the wet paint.", "Come to see the parade cart?", "Careful on the slope." },
                new[]
                {
                    L("Hale", "Mind the paint! She's for the parade."),
                    L("Hale", "The brake's a beauty, but that slope is steep. If she ever got loose she'd roll clean across the street.", 4.5f),
                    L("Hale", "There's a cart bay in the wall there. Never bother using it myself. Push her in if you like - she rolls easy."),
                    L("Hale", "My workshop's through the door behind me. Chocks, wheels, paint. Help yourself, just don't touch the gold."),
                    L("Hale", "The Founders want the cart at the station at dawn, not the square. 'Festival specials', they say. Odd place for a parade.", 4.5f),
                    L("Hale", "My girl Mirela wants a stall of her own one day. Blue cups. Mark my words."),
                },
                new[]
                {
                    WhenHolding("hale_brush",
                        L("Hale", "My brush! Been looking for that. Gold for the trim, blue for the wheels - blue like Mirela's cups.", 4.5f)),
                    WhenHolding("cart_chock",
                        L("Hale", "A wheel chock. I've told myself a hundred times to chock her on that slope."),
                        L("Hale", "A hundred times... funny, it does feel like a hundred. Push her into the bay, would you? Better than any chock.", 4.5f)),
                },
                new[]
                {
                    L("Hale", "Tucked away in the bay, safe as a sleeping cat. You've a careful hand."),
                    L("Hale", "Funny. I feel like I've just saved somebody a great deal of sweeping."),
                });

            var brush = Grab("HaleBrush", pastPlay, new Vector3(15f, 2.58f, 18.2f), new Vector3(0.16f, 0.16f, 0.55f), "fabric_gold", "hale_brush", "paint brush");
            brush.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Past;
            var shard = Grab("LooseCupShard", presPlay, new Vector3(-3.6f, 0.05f, 21.2f), new Vector3(0.4f, 0.05f, 0.3f), "cup_blue", "cup_shard", "blue cup shard");
            shard.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Present;

            // --- Background life ---
            Crowd("Crowd_Past", pastLook, new Vector3(0f, 0f, 20f), new Vector2(11f, 40f), 8, 501,
                  new[] { "Did you hear something?", "The bell sounds lovely today.", "Mind the cart!", "What a festival." }, "fabric_gold");
            Crowd("Crowd_Present", presLook, new Vector3(0f, 0f, 24f), new Vector2(10f, 36f), 7, 502,
                  new[] { "Again?", "Did the bell just...?", "Rain. Always rain.", "Felt that one in my teeth." }, "fabric_teal");
            Crowd("Crowd_PresentTerraceSteps", presLook, new Vector3(0f, 0f, 3f), new Vector2(10f, 4f), 2, 503,
                  new[] { "Train's late again.", "Didn't we just arrive?" }, "paper");
            // Past musician on the corner.
            var musician = Npc("Busker", pastLook, new Vector3(5.8f, 0f, 12f), -90f, "coat_olive", false, 9, "brass");
            musician.reactionBarks.Add("Another song for the bell!");
            Route(musician, false,
                (new Vector3(5.8f, 0f, 12f), 8f, NPCActivity.Work, ""),
                (new Vector3(5.6f, 0f, 13.5f), 3f, NPCActivity.Talk, ""),
                (new Vector3(5.8f, 0f, 12.2f), 6f, NPCActivity.Carry, ""));
        }

        // =================================================================
        // Optional side area: Hale's workshop (Past) / festival backstage (Present), east of the cart landing
        // =================================================================

        static void BuildBackstage(Transform logic)
        {
            // Room x 20.5..30.5, z 17.5..27.5, floor 2.5; doorway from the landing at z 18..20.
            Solid("BackstageSill", new Vector3(20.25f, 1.25f, 19f), new Vector3(0.5f, 2.5f, 2f), "stone_warm", "stone_wet");
            Solid("BackstageFloor", new Vector3(25.5f, 1.25f, 22.5f), new Vector3(10f, 2.5f, 10f), "wood", "stone_wet");
            Solid("BackstageWallE", new Vector3(30.75f, 4.7f, 22.5f), new Vector3(0.5f, 4.4f, 11f), "brick_warm", "plaster_cold");
            Solid("BackstageWallS", new Vector3(25.5f, 4.7f, 17.25f), new Vector3(10.5f, 4.4f, 0.5f), "brick_warm", "plaster_cold");
            Solid("BackstageWallN", new Vector3(25.5f, 4.7f, 27.75f), new Vector3(10.5f, 4.4f, 0.5f), "brick_warm", "plaster_cold");
            Solid("BackstageRoof", new Vector3(25.5f, 7.1f, 22.5f), new Vector3(10.5f, 0.4f, 11f), "wood_dark", "metal_dark");
            Label("BackstageSign", shared, new Vector3(19.95f, 5.5f, 19f), 90f, "CARTWRIGHT - BACKSTAGE", 0.22f, Color.white);
            Zone("Zone_Backstage", logic, new Vector3(25.5f, 4f, 22.5f), new Vector3(9f, 3f, 9f), "L01.backstageVisited", "Festival backstage.")
                .lines.Add(L("Arin", "Behind the parade, the props. Behind the props... a notice board nobody reads."));

            // PAST: Hale's workshop.
            Solid("WorkBench", new Vector3(27.5f, 2.95f, 18.5f), new Vector3(3f, 0.9f, 1f), "wood", "wood_dark");
            for (int i = 0; i < 3; i++)
            {
                var w = Cylinder("SpareWheel", pastLook, new Vector3(30.2f, 3.2f, 20f + i * 1.2f), 0.6f, 0.12f, "wood", false);
                w.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Deco("PaintPots", pastLook, new Vector3(27f, 3.55f, 18.4f), new Vector3(1.2f, 0.3f, 0.4f), "fabric_gold");
            Deco("RolledBanners", pastLook, new Vector3(23f, 2.8f, 26.8f), new Vector3(4f, 0.5f, 0.5f), "fabric_red");
            Deco("Masks", pastLook, new Vector3(30.45f, 4.6f, 24.5f), new Vector3(0.05f, 1f, 2.5f), "fabric_teal");
            PointLight("WorkshopLamp", pastLook, new Vector3(25.5f, 6.2f, 22.5f), Warm, 2.6f, 10f);
            Particles("WorkshopDust", pastLook, new Vector3(25.5f, 4.5f, 22.5f), new Vector3(8f, 3f, 8f), "dust");
            var programme = Clue("FestivalProgramme", pastPlay, new Vector3(28.3f, 3.42f, 18.5f), new Vector3(0.45f, 0.05f, 0.34f), "paper",
                "Read the festival programme", "L01.programmeRead", "L01_programme", "Festival of the Zero - Programme",
                "FESTIVAL OF THE ZERO\n\nDusk: the great bell rings ONCE to open the festival.\nNight: parade from the Arrival Quarter.\n" +
                "Dawn: festival specials depart the station for honoured families (by Founders' invitation only).\n\n" +
                "Pencilled in the margin, in a hand you almost know: 'not once. twice. tell the cartwright to chock his cart.'", true, TimeMask.Past);
            _ = programme;
            var chock = Grab("CartChock", pastPlay, new Vector3(24f, 2.7f, 19f), new Vector3(0.4f, 0.25f, 0.3f), "wood_dark", "cart_chock", "wheel chock");
            chock.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Past;
            Crowd("WorkshopHands_Past", pastLook, new Vector3(25f, 2.5f, 23f), new Vector2(6f, 5f), 1, 601,
                  new[] { "Mind the varnish!", "Hale will have our heads." }, "wood");

            // PRESENT: derelict backstage with the notice board of missing posters.
            Deco("NoticeBoard", presLook, new Vector3(25.5f, 4.4f, 27.45f), new Vector3(5f, 2.4f, 0.1f), "wood_dark");
            Label("BoardTitle", presLook, new Vector3(25.5f, 5.85f, 27.35f), 0f, "MISSING", 0.5f, new Color(1f, 0.85f, 0.6f));
            Deco("BrokenProps", presLook, new Vector3(22.5f, 2.8f, 21f), new Vector3(2f, 0.6f, 1.2f), "iron_rust", 20f);
            Deco("Tarp", presLook, new Vector3(28.5f, 2.9f, 23f), new Vector3(2.4f, 0.8f, 2f), "fabric_teal", -10f);
            Deco("Puddle", presLook, new Vector3(24f, 2.51f, 23.5f), new Vector3(2.5f, 0.02f, 1.6f), "water");
            PointLight("BackstageBulb", presLook, new Vector3(25.5f, 6.2f, 25.5f), new Color(0.95f, 0.8f, 0.6f), 2.2f, 8f);
            Particles("RoofDrip", presLook, new Vector3(23f, 6.7f, 22f), new Vector3(1f, 0.1f, 1f), "rain");
            Ambient("Amb_BackstageDrip", shared, new Vector3(25.5f, 4f, 22.5f), AmbientKind.Water, 0.2f, TimeMask.Present, 1f, 10f);

            var board = Group("MissingPosters", presPlay);
            Clue("Poster_Oskel", board, new Vector3(24f, 4.6f, 27.36f), new Vector3(0.7f, 0.9f, 0.03f), "paper", "Read the poster", "L01.posterOskel",
                "L01_posterOskel", "MISSING: The Oskel Family",
                "MISSING - The Oskel family of Lantern Row, number nine: Teodor, Sanne, and their daughter MAREN, aged seven.\n\n" +
                "Last seen the night of the festival. Their house still stands. Their neighbours cannot remember their faces.\n\n" +
                "Someone has scratched a small 0 in the corner.", true, TimeMask.Present);
            Clue("Poster_School", board, new Vector3(25.5f, 4.3f, 27.36f), new Vector3(0.7f, 0.9f, 0.03f), "paper", "Read the poster", "L01.posterSchool",
                "L01_posterSchool", "MISSING: The Founders' School Excursion",
                "Fourteen children of the Founders' School left before dawn on a 'festival excursion' and did not return.\n\n" +
                "The school says there was no excursion. The school says there were never fourteen.", true, TimeMask.Present);
            Clue("Poster_Order", board, new Vector3(27f, 4.6f, 27.36f), new Vector3(0.7f, 0.9f, 0.03f), "paper", "Read the notice", "L01.posterOrder",
                "L01_posterOrder", "Notice, By Order",
                "BY ORDER OF FOUNDER AUREL HALDEN:\n\nEnquiries regarding relocated families are to cease. Posters are to be removed.\n" +
                "The festival will proceed.\n\n(This one was not removed. It has been pinned up again, more than once.)", true, TimeMask.Present);
            var loose = Grab("LoosePoster", presPlay, new Vector3(23.5f, 2.55f, 26.4f), new Vector3(0.6f, 0.02f, 0.8f), "paper", "missing_poster", "missing poster");
            loose.gameObject.AddComponent<L01CartPhysics>().existsIn = TimeMask.Present;
            Label("LoosePosterTag", presPlay, new Vector3(23.5f, 2.9f, 26.4f), 0f, "MISSING", 0.15f, new Color(1f, 0.85f, 0.6f));
        }

        // =================================================================
        // Memory fragments (3, off the main path)
        // =================================================================

        static void BuildFragments(Transform logic)
        {
            var g = Group("MemoryFragments", logic);
            Fragment(g, new Vector3(29.5f, 3.6f, 26.5f), "L01.1", "The Excursion",
                "They said the school was going on a festival excursion. Fourteen of us in our best coats, at the station before dawn. " +
                "I had a fever and stayed home. When I went back to school there were fourteen empty pegs, and nobody would look at them.");
            Fragment(g, new Vector3(8f, 1.2f, 63f), "L01.2", "The Second Bell",
                "Grandfather said the bell rings once at dusk for the festival. Only once. When I heard it ring twice I looked up at him, " +
                "and he was already looking at the tower, the way you look at a door you have heard open before.");
            Fragment(g, new Vector3(-7.3f, 1.2f, -5.3f), "L01.3", "Names on the Door",
                "We painted our family name on the door every spring. This spring the paint was there, but nobody could read it - " +
                "as if the letters had forgotten how to be a name. Where the name had been, someone had carved a small 0.");
        }

        // =================================================================
        // Flow: observation -> Past unlock -> inspect -> payoff -> exit
        // =================================================================

        static void BuildFlow(Transform logic)
        {
            // Courier posts the letter (fallback at t=24 so the beat never stalls).
            var post = Marker("PostboxPoint", logic, new Vector3(-6.6f, 0f, 12f)).gameObject.AddComponent<L01ProximityFlag>();
            post.target = courier.transform;
            post.radius = 1.6f;
            post.setFlag = F_LetterPosted;
            post.fallbackLoopTime = 24f;

            var obs = Reactor("React_RoutinesObserved", logic, F_CrashSeen, F_VendorTalked);
            obs.reversible = false;
            obs.setFlagWhenTrue = F_Observed;

            var unlock = Group("StateUnlock_Past", logic).gameObject.AddComponent<StateUnlock>();
            unlock.whenFlag = F_Observed;
            unlock.unlock = TimeMask.Past;
            unlock.toast = "NEW STATE: << PAST.  Press [1] (or [Q]) to shift to the PAST, [2] to return to the [ PRESENT ]. Hold [F] to see echoes of the other state.";

            var teach = Group("Teach_Past", logic).gameObject.AddComponent<MemorySequence>();
            teach.triggerFlag = F_Observed;
            teach.lockControls = false;
            teach.startCue = Cue.RuleChange;
            teach.lines.Add(L("The Witness", "You have seen the routine. Now step back, Arin - into the PAST, before the rain.", 4f));
            teach.lines.Add(L("The Witness", "Same street. Same stones. Earlier. What you move there stays moved.", 3.5f));
            teach.lines.Add(L("Arin", "And the loop will remember it. Even if nobody else does.", 3.5f));
            teach.lines.Add(L("The Witness", "Look for whatever sets the cart rolling.", 3f));

            var savedTalk = Group("Witness_CupSaved", logic).gameObject.AddComponent<MemorySequence>();
            savedTalk.triggerFlag = F_CupSaved;
            savedTalk.lockControls = false;
            savedTalk.startCue = Cue.Revelation;
            savedTalk.lines.Add(L("The Witness", "There. One thing kept. The night can be taught, Arin.", 3.5f));
            savedTalk.lines.Add(L("Arin", "Who are you? How do you know my name?", 3f));
            savedTalk.lines.Add(L("The Witness", "Someone who has read all your letters. Go and see what the street becomes when nobody waits for the bell.", 5f));

            var insp = Zone("Zone_ChangedPlaza", logic, new Vector3(0f, 1.5f, 53f), new Vector3(22f, 3f, 6f), F_Inspected, "", TimeMask.Present);
            insp.requiredFlags.Add(F_CupSaved);
            insp.lines.Add(L("Arin", "The rain has stopped. The lanterns are lit. Nobody is waiting for the bell."));
            insp.lines.Add(L("Arin", "And there's a letter on the tower step - in my handwriting."));

            Zone("Zone_EnterClockworks", logic, new Vector3(0f, 1.5f, 63.4f), new Vector3(2.4f, 3f, 0.8f), F_Entered);
            Exit("Exit_Clockworks", logic, new Vector3(0f, 0f, 65.6f), new Vector3(3.6f, 3f, 2.4f),
                 "The tower door is sealed with the clock-tower seal (0). Save the cup, then read the letter on the step.", F_LetterTruth);
            Checkpoint("CP_Plaza", logic, new Vector3(0f, 0f, 51f), new Vector3(6f, 3f, 3f), 0f);

            BuildBackstage(logic);
            BuildFragments(logic);
        }
    }
}
