// Scripted walkthrough of LEVEL 02: wedge -> drain -> gallery -> Calder -> lever -> ledge -> chamber -> bolt -> recording -> exit.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m);
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).join(","));
  const until = async (fn, ms = 8000) => { const t0 = Date.now(); while (!fn() && Date.now() - t0 < ms) await T.wait(100); return fn(); };
  const talkOut = async n => { for (let i = 0; i < n; i++) { T.skipTalk(); await T.wait(250); } };
  try {
    await T.wait(600);
    await T.use("Pull the wedge"); T.closeDoc(); T.skipTalk();
    if (!AZ.get("L02.wedgeRemoved")) return fail("wedge");
    await T.use("Open the drain valve");
    T.shift(1); T.goto(0, 1.5, -10);
    if (!await until(() => AZ.get("L02.chamberDrained"), 90000)) return fail("not drained");
    T.skipTalk();
    // Climb: mid platform, then ride the PAST wheel (teleport onto the gallery stub for the test).
    T.goto(1.5, 2.6, 8); await T.wait(300);
    T.shift(0); await T.wait(800);
    T.goto(8.5, 7, 6); await T.wait(600);
    if (!AZ.get("L02.galleryReached")) return fail("gallery");
    // Bell stairs: Calder's ambush.
    T.shift(1); await T.wait(800);
    T.goto(0.8, 11.6, -10.5); await T.wait(1200);
    await talkOut(6); await T.pick(0); await talkOut(6);
    if (!await until(() => AZ.get("L02.calderCut") && AZ.get("L02.powerOut"), 6000)) return fail("Calder scene");
    await T.use("Engage the clockwork"); T.skipTalk();
    if (!AZ.get("L02.clockworkRestarted")) return fail("restart");
    // Ledge (climax checkpoint), then the FUTURE beam into the chamber.
    T.goto(10.3, 13, 6.5); await T.wait(600);
    T.shift(2); await T.wait(800);
    T.goto(-7.5, 13, 7.5); await T.wait(800);
    if (!AZ.get("L02.beamCrossed")) return fail("beam");
    await T.use("Draw Calder's bolt"); T.skipTalk();
    if (!AZ.get("L02.calderBoltDrawn")) return fail("bolt");
    await T.use("Play the clockkeeper's recording", { wait: 600 });
    await talkOut(14);
    if (!await until(() => AZ.get("L02.recordingHeard"), 10000)) return fail("recording");
    T.closeDoc(); await T.wait(4000);
    T.goto(-7.5, 13, 14); await T.wait(300);
    T.goto(-7.5, 13, 15.8); await T.wait(2500);
    log("objective now: " + T.objective() + " | level: " + T.level());
    if (T.level() !== "L03") fail("did not reach L03");
    else log("PASSED");
  } catch (e) { fail(e.message); }
})();
