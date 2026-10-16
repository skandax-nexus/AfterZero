using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// LEVEL 01 v2: Sister Vossel (Watcher Keeper) + the Perfect Loop climax.
    /// In ONE loop: park the Past cart in the bay (loop.L01.cartChocked), stop Lise before 0:14
    /// (loop.L01.liseStopped, spoiled by loop.L01.liseRan), divert Pell (loop.L01.pellDiverted).
    /// Only then is the cup event prevented (loop.L01.perfectLoop -> L01.cupSaved).
    /// </summary>
    public static partial class Level01Builder
    {
        /// <summary>
        /// Same as AZKit.Keeper, but grabs the body's GameObject before removing its NPCMotor
        /// (AZKit.Keeper reads m.transform after DestroyImmediate(m), which throws in the editor).
        /// </summary>
        static KeeperPatrol SafeKeeper(string displayName, Transform parent, Vector3 ground, float yaw, KeeperPatrol.Mode mode, TimeMask activeIn, params Vector3[] patrol)
        {
            var m = Npc(displayName, parent, ground, yaw, "coat_grey", false, displayName.Length);
            var bodyGo = m.gameObject;
            Object.DestroyImmediate(m);
            var root = Group("Keeper " + displayName, parent);
            root.position = ground;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            bodyGo.transform.SetParent(root, true);
            bodyGo.name = "Body";
            Prim(PrimitiveType.Cylinder, "Hood", bodyGo.transform, ground + Vector3.up * 1.72f, new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
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
            Unstatic(root);
            return k;
        }

        static void BuildKeepersAndClimax(Transform logic)
        {
            // --- Sister Vossel: watches the street, reports shifts/tampering she sees (LoopEscalation on _Systems) ---
            var vossel = SafeKeeper("Sister Vossel", shared, new Vector3(0f, 0f, 6f), 0f, KeeperPatrol.Mode.Watcher, TimeMask.Present,
                new Vector3(0f, 0f, 6f), new Vector3(2.5f, 0f, 20f), new Vector3(-2f, 0f, 34f), new Vector3(1f, 0f, 14f));
            vossel.walkSpeed = 1.1f;
            vossel.pauseAtPoint = 3f;
            vossel.disabledByFlag = F_CupSaved;
            vossel.spotBarks.Clear();
            vossel.spotBarks.AddRange(new[] { "Anomaly.", "I saw that.", "The hour notices you.", "Noted. Again." });
            // The Keeper's body and lantern belong to the PRESENT; the patrol logic keeps running so it sees shifts.
            foreach (var n in new[] { "Body", "Lantern" })
            {
                var t = vossel.transform.Find(n);
                if (t == null) continue;
                var tp = t.gameObject.AddComponent<TemporalPresence>();
                tp.existsIn = TimeMask.Present;
                tp.showEcho = false;
            }
            var vBody = vossel.transform.Find("Body");
            Label("VosselTag", vBody != null ? vBody : vossel.transform, new Vector3(0f, 2.2f, 6f), 0f, "SISTER VOSSEL", 0.25f, new Color(1f, 0.8f, 0.55f));

            // After the cup is saved she stops patrolling and waits in the plaza.
            var vNpc = Npc("Sister Vossel", presPlay, new Vector3(3f, 0f, 57.5f), 180f, "coat_grey", true, 41);
            Prim(PrimitiveType.Cylinder, "Hood", vNpc.body, new Vector3(3f, 1.72f, 57.5f), new Vector3(0.36f, 0.2f, 0.36f), "obsidian", false, false);
            Route(vNpc, false,
                (new Vector3(3f, 0f, 57.5f), 6f, NPCActivity.Look, "Thirty seconds. It was a perfect thirty seconds."),
                (new Vector3(5.5f, 0f, 55f), 5f, NPCActivity.Work, ""),
                (new Vector3(1.5f, 0f, 59f), 5f, NPCActivity.Look, "Mother Ondine will want to hear about you."));
            Route(vNpc, true,
                (new Vector3(8f, 0f, 61f), 4f, NPCActivity.Look, "Go on, then. Climb your tower."),
                (new Vector3(8f, 0f, 63.5f), 30f, NPCActivity.Idle, ""),
                (new Vector3(7.5f, 0f, 60f), 10f, NPCActivity.Work, ""));
            Persona(vNpc, "Keeper watcher: walks the festival street counting what changes, and reports every anomaly to Mother Ondine.", "L01.vosselScene",
                new[] { "I have your count, anomaly.", "Stand still. I'm writing.", "The hour sees you." },
                new[]
                {
                    L("Vossel", "Sister Vossel, of the Keepers of the Hour. I count what changes."),
                    L("Vossel", "Thirty seconds of festival, forever. Nobody ever dies in it for good. Do you know how rare that is?", 4.5f),
                    L("Vossel", "Every time you shifted in my lantern, I wrote it down. Every time, the hour got shorter. That was us.", 4.5f),
                    L("Vossel", "The Founders built the cage. We only keep it locked."),
                    L("Vossel", "Brother Calder keeps the tower. He is less patient than I am."),
                },
                new[]
                {
                    WhenHolding("missing_poster",
                        L("Vossel", "Put that down. Those posters come down every night, and every night somebody pins them back up."),
                        L("Vossel", "Some names are safer forgotten. Ask the archive - while it still stands.", 4f)),
                    WhenFlag(Clue_Truth,
                        L("Vossel", "You read the letter. Your own hand, from a tomorrow that should not exist."),
                        L("Vossel", "Mother Ondine said one of you would come back. She did not say it would be you.", 4f)),
                },
                new[]
                {
                    L("Vossel", "You have made your choice for tonight. The hour will remember it. So will I."),
                    L("Vossel", "Calder is waiting in the tower. Mind the lights."),
                });
            var swap = Reactor("React_VosselStandsDown", logic, F_CupSaved);
            swap.reversible = false;
            swap.disableWhenTrue.Add(vossel.gameObject);
            swap.enableWhenTrue.Add(vNpc.gameObject);

            // Her scene: in the plaza, near the tower step.
            var vz = Zone("Zone_VosselScene", logic, new Vector3(0f, 1.5f, 58f), new Vector3(10f, 3f, 3f), "L01.vosselTrigger", "", TimeMask.Present);
            vz.requiredFlags.Add(F_Inspected);
            var host = Group("Dialogue_Vossel", logic);
            host.position = new Vector3(3f, 1.6f, 57.5f);
            var dlg = Dialogue(host.gameObject, "Sister Vossel", "Sister Vossel steps out of the lamplight. What do you say?", "L01.vosselTrigger");
            dlg.opening.Add(L("Vossel", "So. You are the anomaly I counted. Shifts. Tampering. A cart in a bay that was never used.", 4.5f));
            dlg.opening.Add(L("Vossel", "Thirty seconds, Arin. Thirty seconds of a perfect festival, kept forever. You broke it for a cup.", 4.5f));
            var o1 = Option("Nobody dies in your loop. Nobody lives in it either.", "L01.vosselDefied",
                L("Vossel", "Brave. Mother Ondine collects brave people. She keeps them, too."));
            var o2 = Option("Who are the Keepers?", "L01.vosselAsked",
                L("Vossel", "We keep the hour. The Founders built the cage; we only lock it. Brother Calder will show you the lock.", 4.5f));
            var o3 = Option("What happened to the Oskel family?", "L01.vosselPressed",
                L("Vossel", "...Lantern Row is none of your business. Some names are safer forgotten.", 3.5f),
                L("Vossel", "Ask the archive, if you must. If it is still standing when you get there.", 3.5f));
            o3.requiresFlags.Add("clue.L01_posterOskel");
            o3.missingText = "(you have not seen the missing posters)";
            dlg.options.Add(o1); dlg.options.Add(o2); dlg.options.Add(o3);
            dlg.doneFlag = "L01.vosselScene";

            // --- Lise's run at 0:14 ---
            var liseRun = Group("LiseRun", logic).gameObject.AddComponent<L01LiseRun>();
            liseRun.lise = lise;
            liseRun.target = Marker("LiseTarget", logic, new Vector3(-3.9f, 0f, 22.4f));
            liseRun.stoppedFlag = F_LiseStopped;
            liseRun.ranFlag = "loop.L01.liseRan";
            liseRun.seenFlag = F_LiseSeen;
            liseRun.seenRequiresFlag = F_CartMoved;
            liseRun.disabledByFlag = F_CupSaved;
            var seen = Reactor("React_LiseSeen", logic, F_LiseSeen);
            seen.reversible = false;
            seen.toast = "Lise crashed into the stall. The cart is only one cause. Pell stood in the lane too.";
            var seenTalk = Group("Witness_PerfectLoop", logic).gameObject.AddComponent<MemorySequence>();
            seenTalk.triggerFlag = F_LiseSeen;
            seenTalk.lockControls = false;
            seenTalk.startCue = Cue.Memory;
            seenTalk.lines.Add(L("Arin", "The cart, Lise, and Pell standing in the lane. Three causes, all in thirty seconds.", 4f));
            seenTalk.lines.Add(L("The Witness", "Then you need one perfect loop. Plan it before you run it.", 3.5f));
            seenTalk.lines.Add(L("The Witness", "And stay out of Vossel's lantern. Every time she sees you, the night gets shorter.", 4f));

            // --- The Perfect Loop ---
            var perfect = Group("PerfectLoop", logic).gameObject.AddComponent<L01PerfectLoop>();
            perfect.required.AddRange(new[] { F_CartChocked, F_LiseStopped, F_PellDiverted });
            perfect.labels.AddRange(new[] { "the cart is parked in the PAST", "Lise is stopped", "Pell takes the long way" });
            perfect.forbidden.Add("loop.L01.liseRan");
            perfect.perfectFlag = F_Perfect;
            perfect.activeAfterFlag = F_LiseSeen;
            perfect.doneFlag = F_CupSaved;
            perfect.baseReportTime = 18.3f;

            // Checkpoints: normal before the combine phase, climax (removed on Hard) at the lane mouth.
            Checkpoint("CP_Street", logic, new Vector3(0f, 0f, 9f), new Vector3(8f, 3f, 2f), 0f);
            var cpc = Checkpoint("CP_PerfectLoop", logic, new Vector3(5f, 0f, 22.5f), new Vector3(3f, 3f, 4f), -90f);
            cpc.climax = true;

            // Make moving doors render correctly in builds (no static batching on moving panels).
            foreach (var r in logic.root.GetComponentsInChildren<FlagReactor>(true))
                if (r.moveTarget != null) Unstatic(r.moveTarget);
        }
    }
}
