// Scripted walkthrough of Level 03: evidence, fire + water routing, deduction, Ondine (refuse) + chase, exit.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m);
  const until = async (f, ms = 6000) => { for (let i = 0; i < ms / 100 && !AZ.get(f); i++) await T.wait(100); return AZ.get(f); };
  const done = () => { T.closeDoc(); T.skipTalk(); };
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).filter(f => AZ.flags[f]).join(","));
  try {
    await T.wait(600); T.skipTalk();
    await T.use("Read the flooded register"); done();
    await T.use("Read the index board"); done();
    await T.use("Open drawer 05"); done();
    if (!(await until("L03.nameFoundPast"))) return fail("drawer");
    await T.use("Read the damp ticket"); done();
    await T.use("Press stamp <V>"); done();
    await T.wait(1800);
    for (let i = 1; i <= 4; i++) { await T.use("Trace the mineral letters " + i); done(); }
    if (!(await until("L03.nameReconstructed"))) return fail("traces");
    T.shift(1); await T.wait(300);
    const turns = [2, 2, 3, 3];
    for (let j = 0; j < 4; j++) for (let k = 0; k < turns[j]; k++) { await T.use("Turn junction #" + (j + 1), { wait: 150 }); }
    await T.wait(400);
    if (!(await until("L03.waterRouted"))) return fail("water routing");
    for (let i = 1; i <= 3; i++) { await T.use("Read clue card #" + i); done(); }
    T.use("Study the five records"); await T.pick(2); done();
    if (!(await until("L03.recordPicked"))) return fail("deduction");
    await T.use("Read Maren's record"); done();
    // Ondine arrives ~7 s later: skip her opening, refuse.
    for (let i = 0; i < 60 && !document.querySelector("#pChoice.show"); i++) { T.skipTalk(); await T.wait(250); }
    await T.pick(2);
    for (let i = 0; i < 8; i++) { T.skipTalk(); await T.wait(150); }
    if (!AZ.get("L03.refused") || !AZ.get("evidence.record")) return fail("ondine choice");
    await T.use("Turn the hatch release"); done();
    await T.wait(2300);
    if (!(await until("L03.hatchOpen"))) return fail("hatch");
    T.goto(0, -3, 52.4); await T.wait(500);
    T.goto(0, -3, 54.4); await T.wait(2500);
    log("objective now: " + T.objective() + " | level: " + T.level());
    if (T.level() !== "L04") fail("did not reach L04");
    else log("PASSED");
  } catch (e) { fail(e.message); }
})();
