// Scripted walkthrough of LEVEL 01: letter -> routine -> PAST cart -> Lise seen -> Perfect Loop -> plaza -> Vossel -> letter -> exit.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m), W = () => T.world();
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).join(","));
  const until = async (fn, ms = 8000) => { const t0 = Date.now(); while (!fn() && Date.now() - t0 < ms) await T.wait(100); return fn(); };
  const loopTo = s => { W().loop.elapsed = s * (W().loop.duration / 30); };
  try {
    await T.wait(600);
    // 1. The courier posts the letter (fallback at 0:24).
    loopTo(23.8);
    if (!await until(() => AZ.get("loop.L01.letterPosted"), 40000)) return fail("letter never posted");
    await T.use("Read the posted letter"); T.closeDoc(); T.skipTalk();
    // 2. Routine: talk to Mirela and watch the cart crash at the second bell.
    await T.use("Talk to Mirela"); T.skipTalk();
    T.goto(0, 0, 10); loopTo(17.8);
    if (!await until(() => AZ.get("L01.routinesObserved"), 40000)) return fail("routine not observed");
    T.skipTalk();
    // 3. PAST: push the cart into the bay.
    await T.use("Push the cart");
    if (!await until(() => AZ.get("L01.cartMovedPast"), 40000)) return fail("cart not parked");
    // 4. Watch Lise run into the stall.
    T.shift(1); T.goto(-6, 0, 30); loopTo(13.8);
    if (!await until(() => AZ.get("L01.liseRanSeen"), 40000)) return fail("Lise run not seen");
    T.skipTalk();
    // 5. The Perfect Loop: start a fresh loop, divert Pell, stop Lise, park the cart - then the cup moment.
    { const c0 = W().loop.count; loopTo(29.95); await until(() => W().loop.count > c0, 40000); await T.wait(300); }
    await T.use("Talk to Pell"); T.skipTalk();
    await T.use("Talk to Lise"); T.skipTalk();
    await T.use("Push the cart");
    await until(() => AZ.get("loop.L01.cartChocked"), 40000);
    T.shift(1); T.goto(0, 0, 40);
    if (!AZ.get("loop.L01.perfectLoop")) return fail("perfect loop flag missing");
    loopTo(18.5);
    if (!await until(() => AZ.get("L01.cupSaved"), 40000)) return fail("cup not saved");
    T.skipTalk();
    // 6. The changed plaza, Vossel's scene, the letter, the clockworks.
    T.goto(0, 0, 53); await T.wait(600); T.skipTalk();
    T.goto(0, 0, 58); await T.wait(900);
    for (let i = 0; i < 6; i++) { T.skipTalk(); await T.wait(300); }
    await T.pick(1); await T.wait(300);
    for (let i = 0; i < 6; i++) { T.skipTalk(); await T.wait(300); }
    if (!AZ.get("L01.vosselScene")) return fail("Vossel scene not done");
    await T.use("Read Arin's letter"); T.closeDoc(); T.skipTalk();
    await T.wait(6000);
    T.goto(0, 0, 63.4); await T.wait(300);
    T.goto(0, 0, 65.6); await T.wait(2500);
    log("objective now: " + T.objective() + " | level: " + T.level());
    if (T.level() !== "L02") fail("did not reach L02");
    else log("PASSED");
  } catch (e) { fail(e.message); }
})();
