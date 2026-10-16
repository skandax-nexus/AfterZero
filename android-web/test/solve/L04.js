// Scripted walkthrough of Level 04: pulses, crossing, cable, decoy, list (VALE reveal), Halden, Calder climax, lift ride, exit.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m);
  const until = async (f, ms = 6000) => { for (let i = 0; i < ms / 100 && !AZ.get(f); i++) await T.wait(100); return AZ.get(f); };
  const done = () => { T.closeDoc(); T.skipTalk(); };
  const fail = m => console.error("[solve] FAILED: " + m + " t=" + (T.world() && T.world().t) + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).filter(f => AZ.flags[f]).join(","));
  const choose = async i => { for (let k = 0; k < 80 && !document.querySelector("#pChoice.show"); k++) { T.skipTalk(); await T.wait(150); } await T.pick(i); for (let k = 0; k < 10; k++) { T.skipTalk(); await T.wait(120); } };
  try {
    await T.wait(600); T.skipTalk();
    await T.use("Read the pulse monitor"); done();
    T.goto(0, 0, 51); await T.wait(400);
    if (!(await until("L04.crossedTracks"))) return fail("crossing");
    T.shift(1); await T.wait(300);
    await T.use("Take live cable end");
    await T.use("Connect");
    if (!(await until("L04.liftPowered"))) return fail("cable");
    await T.use("Pull the gantry release"); await T.wait(1700);
    if (!(await until("L04.falseVibration"))) return fail("decoy");
    await T.use("Take the evacuation list"); done();
    if (!AZ.get("arin.listedChild") || !AZ.get("evidence.list")) return fail("list flags");
    T.use("Face Founder Halden"); await choose(2);
    if (!(await until("L04.haldenDone"))) return fail("halden");
    T.shift(1); await T.wait(800);
    T.goto(0, 0, 53.5); await T.wait(300); T.goto(-3, 0, 74.4); await T.wait(400);
    T.goto(-3, 0, 76.6); await T.wait(300);
    await choose(0);
    if (!(await until("L04.calderTalked"))) return fail("calder");
    await T.use("Throw breaker A");
    await T.use("Throw breaker B");
    if (!(await until("L04.circuitHeld"))) return fail("breakers");
    for (let tries = 0; tries < 3 && !AZ.get("L04.liftArrived"); tries++) {
      await T.use("Descend to the Drowned District"); T.skipTalk();
      const L4 = T.world().l04;
      for (let k = 0; k < 600 && L4.riding(); k++) { T.goto(L4.safeX(), L4.top(), 81); await T.wait(50); }
      await T.wait(600);
    }
    if (!(await until("L04.liftArrived"))) return fail("lift ride");
    T.goto(-3, -6, 90); await T.wait(300);
    T.goto(-3, -6, 93.5); await T.wait(2500);
    log("level: " + T.level());
    if (T.level() !== "L05") fail("did not reach L05");
    else log("PASSED");
  } catch (e) { fail(e.message); }
})();
