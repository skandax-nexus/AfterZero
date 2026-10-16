using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// Level 04 design v2: Keeper hunters with lanterns on top of the vibration machine, limited decoys,
    /// Founder Halden's echo, and the climax lift chase with Brother Calder.
    /// </summary>
    public static partial class Level04Builder
    {
        static void BuildV2(Transform root, Transform shared, Transform logic)
        {
            BuildCover(shared);
            BuildHunters(root);
            BuildExtraDecoys(root);
            BuildHaldenEcho(root, logic);
            BuildCalderClimax(root, shared, logic);

            var e1 = Reactor("ListIsEvidence", logic, "L04.listRecovered"); e1.setFlagWhenTrue = "evidence.list";
            var e2 = Reactor("ArinListed", logic, "L04.listRecovered"); e2.setFlagWhenTrue = "arin.listedChild";
        }

        // Crates and pillars: crouch behind them to break the Keepers' line of sight.
        static void BuildCover(Transform shared)
        {
            var g = Group("Cover", shared);
            var crates = new[]
            {
                new Vector3(-9f, 0f, 19f), new Vector3(3f, 0f, 21f), new Vector3(12.3f, 0f, 11f), new Vector3(-4f, 0f, 27f),
                new Vector3(-10.5f, 0f, 61f), new Vector3(-10.5f, 0f, 75f), new Vector3(0.5f, 0f, 62.5f), new Vector3(-4.5f, 0f, 70.5f), new Vector3(1.6f, 0f, 77.3f),
            };
            foreach (var c in crates)
            {
                Box("CoverCrate", g, c + Vector3.up * 0.65f, new Vector3(1.4f, 1.3f, 1.2f), "wood_dark");
                Box("CrateBand", g, c + Vector3.up * 0.9f, new Vector3(1.45f, 0.08f, 1.25f), "iron_rust", false);
            }
            foreach (var p in new[] { new Vector3(-2.5f, 0f, 57f), new Vector3(-2.5f, 0f, 62f), new Vector3(1.8f, 0f, 70f), new Vector3(-8f, 0f, 9f), new Vector3(12.5f, 0f, 20f) })
                Box("Pillar", g, p + Vector3.up * 4f, new Vector3(0.8f, 8f, 0.8f), "tile_cold");
        }

        static void BuildHunters(Transform root)
        {
            var g = StateGroup("PresentKeepers", root, TimeMask.Present, false);
            var depot = KeeperFixed("Keeper", g, new Vector3(10f, 0f, 8f), 0f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(10f, 0f, 8f), new Vector3(10f, 0f, 27f), new Vector3(4f, 0f, 27f), new Vector3(4f, 0f, 14f));
            var st1 = KeeperFixed("Keeper", g, new Vector3(-5f, 0f, 57f), 0f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(-5f, 0f, 57f), new Vector3(-6.5f, 0f, 74f), new Vector3(-12f, 0f, 76.5f), new Vector3(-6.5f, 0f, 74f));
            var st2 = KeeperFixed("Keeper", g, new Vector3(3.5f, 0f, 80f), 180f, KeeperPatrol.Mode.Hunter, TimeMask.Present,
                new Vector3(3.5f, 0f, 80f), new Vector3(3.5f, 0f, 56f));
            foreach (var k in new[] { depot, st1, st2 })
            {
                k.disabledByFlag = "L04.climax";
                k.spotBarks = new List<string> { "Lantern up - something on the rails.", "Who's down here?" };
                k.alarmBarks = new List<string> { "Anomaly! Under the street!", "There - stop them!" };
                k.caughtMessage = "A Keeper caught you. The machine hears, the Keepers see: crouch behind cover, or shift to the PAST to hide from both.";
            }
        }

        static void BuildExtraDecoys(Transform root)
        {
            var g = StateGroup("ExtraDecoys", root, TimeMask.Present, true);
            // Depot: a loose rail clamp by the stairs.
            var a = Prim(PrimitiveType.Cube, "DepotClamp", g, new Vector3(-9f, 0.5f, 11.5f), new Vector3(0.5f, 1f, 0.5f), "warning_red", true, false);
            var da = a.AddComponent<NoiseDecoy>();
            da.prompt = "Kick the loose rail clamp (noise at the centre rails)";
            da.usableIn = TimeMask.Present; da.supplyGroup = "L04.pipes"; da.usedFlag = "L04.falseVibration";
            da.loudness = 4f; da.delay = 1f; da.cooldown = 10f;
            da.noisePoint = Marker("DepotNoisePoint", g, new Vector3(2f, 0f, 19f));
            Label("ClampTag", g, new Vector3(-9f, 1.4f, 11.15f), 0f, "LOOSE CLAMP\n(decoy)", 0.2f, new Color(1f, 0.6f, 0.5f));
            // Station: a vent grille by the cable junction, rattles toward the south concourse.
            var b = Prim(PrimitiveType.Cube, "VentGrille", g, new Vector3(-13.6f, 1.2f, 72.8f), new Vector3(0.3f, 0.8f, 0.8f), "warning_red", true, false);
            var db = b.AddComponent<NoiseDecoy>();
            db.prompt = "Rattle the vent grille (noise at the south concourse)";
            db.usableIn = TimeMask.Present; db.supplyGroup = "L04.pipes"; db.usedFlag = "L04.falseVibration";
            db.loudness = 4f; db.delay = 1.2f; db.cooldown = 10f;
            db.noisePoint = Marker("VentNoisePoint", g, new Vector3(-2f, 0f, 54f));
            Label("VentTag", g, new Vector3(-13.35f, 1.9f, 72.8f), 90f, "VENT\n(decoy)", 0.2f, new Color(1f, 0.6f, 0.5f));
            var tip = Zone("DecoyTip", root, new Vector3(-10.5f, 1.5f, 12.5f), new Vector3(3f, 3f, 2f), "L04.decoyTip",
                "Decoys are limited: every pipe, clamp and vent comes from the same supply. Use them when it matters.", TimeMask.Present);
        }

        // ---------------------------------------------------------------- Halden
        static void BuildHaldenEcho(Transform root, Transform logic)
        {
            var g = StateGroup("HaldenEcho", root, TimeMask.Future, true);
            var pos = new Vector3(2.6f, 0f, 73.5f);
            var fig = Group("HaldenFigure", g);
            Hologram("Body", fig, pos + Vector3.up * 1f, new Vector3(0.55f, 0.75f, 0.4f), PrimitiveType.Capsule, "hologram_amber", false, false);
            Hologram("Head", fig, pos + Vector3.up * 1.95f, new Vector3(0.3f, 0.3f, 0.3f), PrimitiveType.Sphere, "hologram_amber", false, false);
            Hologram("Plinth", fig, pos + Vector3.up * 0.05f, new Vector3(1.2f, 0.02f, 1.2f), PrimitiveType.Cylinder, "hologram", false, true);
            Label("HaldenTag", fig, pos + Vector3.up * 2.45f, 180f, "FOUNDER AUREL HALDEN (echo)", 0.25f, new Color(1f, 0.85f, 0.55f));
            var talk = Box("HaldenTalk", g, pos + Vector3.up * 1f, new Vector3(0.9f, 2f, 0.9f), "hologram", true);
            Object.DestroyImmediate(talk.GetComponent<MeshRenderer>());
            var dlg = Dialogue(talk, "Founder Aurel Halden", "The Founder's echo waits for an answer.");
            dlg.usableIn = TimeMask.Future;
            dlg.requiredFlags.Add("L04.listRecovered");
            dlg.lockedMessage = "The echo stutters, repeating one word: 'list... list...'";
            dlg.prompt = "Face Founder Halden's echo";
            dlg.opening.Add(L("Founder Aurel Halden", "You found my list. Then you know what I knew: the three states were grinding into one.", 4.5f));
            dlg.opening.Add(L("Founder Aurel Halden", "Two hundred and fourteen seats. A city of forty thousand. What would you have announced?", 4.5f));
            dlg.opening.Add(L("Founder Aurel Halden", "Panic would have killed more than the collapse. I chose silence. Silence saved the ones it could.", 5f));
            dlg.options.Add(Option("You decided who lived.", "L04.haldenAccused",
                L("Founder Aurel Halden", "I decided that anyone lived. Somebody had to hold the pen."),
                L("Arin", "And then you erased their families so nobody would ask.")));
            dlg.options.Add(Option("Why hide the families afterwards?", "L04.haldenAsked",
                L("Founder Aurel Halden", "Grief asks questions. Questions open doors. Doors let the collapse in."),
                L("Founder Aurel Halden", "My daughter keeps the night repeating for the same reason. Ask Ondine.")));
            var listed = Option("My name is on your list.", "L04.haldenNamed",
                L("Founder Aurel Halden", "...Vale. Ilse's ward. Then you were one I chose."),
                L("Founder Aurel Halden", "Don't make me regret it."),
                L("Arin", "You don't get to regret me."));
            listed.requiresFlags.Add("arin.listedChild");
            dlg.options.Add(listed);
            dlg.doneFlag = "L04.haldenDone";
            var hint = Reactor("HaldenHint", logic, "L04.listRecovered");
            hint.toast = "In the FUTURE, an echo of Founder Halden waits by the lift.";
        }

        // ---------------------------------------------------------------- climax
        static void BuildCalderClimax(Transform root, Transform shared, Transform logic)
        {
            var zone = Zone("ClimaxZone", logic, new Vector3(-3f, 1.5f, 76.6f), new Vector3(3.4f, 3f, 1.2f), "L04.climax");
            zone.requiredFlags.AddRange(new[] { "L04.liftPowered", "L04.listRecovered", "L04.haldenDone" });

            var dGo = new GameObject("CalderDialogue");
            dGo.transform.SetParent(logic, false);
            var dlg = Dialogue(dGo, "Brother Calder", "Brother Calder has the lift cable in his fist.", "L04.climax");
            dlg.opening.Add(L("Brother Calder", "Remember me? The clock tower. You slipped out of a door I'd locked.", 4f));
            dlg.opening.Add(L("Brother Calder", "Mother Ondine says the night is mercy. I say it's the only order this city has.", 4.5f));
            dlg.opening.Add(L("Brother Calder", "That list stays down here. And so do you.", 3.5f));
            dlg.options.Add(Option("Let me through, Calder.", "L04.calderAsked",
                L("Brother Calder", "Through? There's no through. There's only round again."),
                L("Arin", "Then I'll make one.")));
            dlg.options.Add(Option("Ilse Marrow stopped the tower. You knew.", "L04.calderIlse",
                L("Brother Calder", "The old clockkeeper broke the bell for a handful of children. We fixed what she broke."),
                L("Arin", "You mean you made it ring again.")));
            var named = Option("My name is on Halden's list.", "L04.calderNamed",
                L("Brother Calder", "...Then you're the one she hid. Mother will want you alive. I won't."),
                L("Arin", "Good to know where we stand."));
            named.requiresFlags.Add("arin.listedChild");
            dlg.options.Add(named);
            dlg.doneFlag = "L04.calderTalked";

            // Calder rips the cable out: the circuit must be held with two breakers.
            var cut = Group("CableCut", shared);
            var sp = Particles("CutSparks", cut, new Vector3(-7.2f, 1.3f, 76.5f), new Vector3(0.2f, 0.2f, 0.2f), "sparks");
            var spm = sp.main; spm.playOnAwake = true;
            Label("CutText", cut, new Vector3(-7.2f, 2.2f, 76.05f), 0f, "CIRCUIT BROKEN\nthrow breakers A + B", 0.22f, new Color(1f, 0.45f, 0.35f));
            cut.gameObject.SetActive(false);
            var cutR = Reactor("CalderCutsCable", logic, "L04.calderTalked");
            cutR.enableWhenTrue.Add(cut.gameObject);
            foreach (var rope in root.GetComponentsInChildren<L04CableRope>(true)) cutR.disableWhenTrue.Add(rope.gameObject);
            cutR.announce = true; cutR.cue = Cue.Alert;
            cutR.toast = "Calder tears the cable loose! Throw breakers A and B to hold the lift circuit.";

            var bg = StateGroup("Breakers", root, TimeMask.Present, true);
            Breaker(bg, "A", new Vector3(-13.55f, 1.2f, 75f), -90f, "L04.breakerA");
            Breaker(bg, "B", new Vector3(4.55f, 1.2f, 74f), 90f, "L04.breakerB");
            var circuit = Reactor("CircuitHeld", logic, "L04.breakerA", "L04.breakerB");
            circuit.setFlagWhenTrue = "L04.circuitHeld";
            circuit.announce = true;
            circuit.toast = "Circuit held. Get in the lift - DESCEND!";

            // Brother Calder: the pursuer.
            var m = Npc("Brother Calder", shared, new Vector3(-7f, 0f, 66.5f), 90f, "obsidian", false, 41);
            var c = m.transform;
            Object.DestroyImmediate(m);
            c.name = "Brother Calder (Pursuer)";
            Prim(PrimitiveType.Cylinder, "Hood", c, new Vector3(-7f, 1.75f, 66.5f), new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            Box("Crowbar", c, new Vector3(-6.7f, 1.1f, 66.8f), new Vector3(0.05f, 0.9f, 0.05f), "iron_rust", false);
            Label("CalderTag", c, new Vector3(-7f, 2.3f, 66.5f), 0f, "BROTHER CALDER", 0.28f, new Color(1f, 0.45f, 0.4f));
            SpotLight("CalderLantern", c, new Vector3(-6.7f, 1.2f, 66.5f), new Vector3(10f, 90f, 0f), new Color(1f, 0.3f, 0.2f), 4f, 10f, 60f, false);
            Dyn(c.gameObject);
            var pur = c.gameObject.AddComponent<Pursuer>();
            pur.displayName = "Brother Calder";
            pur.startFlag = "L04.calderTalked";
            pur.stopFlag = "L04.descending";
            pur.speed = 4.4f; pur.catchUpSpeed = 6.2f; pur.startDelay = 2.5f;
            pur.barks = new List<string> { "Round again, anomaly!", "The hour keeps everyone!", "I'll put you back on the list - the other one." };
            pur.caughtMessage = "Calder caught you. Keep moving between the breakers - and don't sprint past the machine.";
            var lane = Group("CalderLane", logic);
            pur.lane.Add(Marker("C0", lane, new Vector3(-5.5f, 0f, 70f)));
            c.gameObject.SetActive(false);
            var show = Reactor("CalderAppears", logic, "L04.climax");
            show.enableWhenTrue.Add(c.gameObject);
            var hide = Reactor("CalderLeft", logic, "L04.liftArrived");
            hide.disableWhenTrue.Add(c.gameObject);
        }

        static void Breaker(Transform parent, string id, Vector3 pos, float yaw, string flag)
        {
            Box("BreakerBox" + id, parent, pos, new Vector3(0.3f, 0.9f, 0.7f), "metal_dark");
            var lever = Use("Breaker" + id, parent, pos + Quaternion.Euler(0f, yaw, 0f) * Vector3.back * 0.25f, new Vector3(0.45f, 0.5f, 0.45f), "warning_red", "Throw breaker " + id, flag, TimeMask.Present);
            lever.requiredFlags.Add("L04.calderTalked");
            lever.lockedMessage = "Breaker " + id + ". The lift circuit is fine for now.";
            lever.cue = Cue.Clang;
            lever.animateTarget = lever.transform; lever.moveBy = new Vector3(0f, -0.25f, 0f);
            Label("BreakerTag", parent, pos + Quaternion.Euler(0f, yaw, 0f) * Vector3.back * 0.3f + Vector3.up * 0.75f, yaw, "BREAKER " + id, 0.28f, new Color(1f, 0.85f, 0.45f));
        }

        static void ShaftDebris(Transform parent, Vector3 ground, float offset)
        {
            var g = Group("Debris", parent);
            g.position = ground;
            var fallen = Group("Falling", g);
            Box("Chunk", fallen, ground + new Vector3(0f, 1.2f, 0.4f), new Vector3(1f, 0.6f, 0.8f), "concrete", false).transform.rotation = Quaternion.Euler(20f, 30f, 10f);
            Box("Chunk", fallen, ground + new Vector3(0.3f, 0.6f, -0.8f), new Vector3(0.7f, 0.5f, 0.7f), "iron_rust", false).transform.rotation = Quaternion.Euler(-10f, 60f, 25f);
            fallen.gameObject.SetActive(false);
            var warn = PointLight("DebrisWarn", g, ground + Vector3.up * 2.6f, Red, 4f, 4.5f);
            var dust = Particles("DebrisDust", g, ground + Vector3.up * 3.5f, new Vector3(2.2f, 0.2f, 5f), "debris");
            Box("HalfMarker", g, ground + Vector3.up * 0.02f, new Vector3(2.4f, 0.02f, 5.2f), "warning_red", false);
            var dz = TriggerBox("DamageZone", g, ground + Vector3.up * 1f, new Vector3(2.4f, 2f, 5.4f));
            var h = g.gameObject.AddComponent<TelegraphedHazard>();
            h.idleTime = 2.2f; h.warnTime = 1.3f; h.activeTime = 0.7f; h.startOffset = offset;
            h.warnLight = warn; h.warnParticles = dust; h.activeVisual = fallen.gameObject; h.damageZone = dz;
            h.deathMessage = "Calder's debris hit you. The red light shows which half of the car is about to be struck.";
        }

        /// <summary>Same as AZKit.Keeper, but keeps the body transform before removing the NPC motor.</summary>
        static KeeperPatrol KeeperFixed(string displayName, Transform parent, Vector3 ground, float yaw, KeeperPatrol.Mode mode, TimeMask activeIn, params Vector3[] patrol)
        {
            var m = Npc(displayName, parent, ground, yaw, "coat_grey", false, displayName.Length + (int)ground.z);
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
            l.range = 13f;
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
            var route = Group(displayName + " Patrol " + ground.z, parent);
            foreach (var p in patrol) k.patrol.Add(Marker("P", route, p));
            Dyn(root.gameObject);
            return k;
        }
    }
}
