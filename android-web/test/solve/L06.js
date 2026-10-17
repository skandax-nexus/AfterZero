// Scripted walkthrough of Level 06 (The Nursery With No Address): proves every beat is completable.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m);
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).join(","));
  const waitFor = async (flag, ms) => { const t0 = Date.now(); while (!AZ.get(flag) && Date.now() - t0 < ms) await T.wait(150); return AZ.get(flag); };
  const W = () => T.world();
  const faceDoor = () => { const d = W().L06.door.position; T.face(d.x, d.z); };
  try {
    await T.wait(600); T.skipTalk();
    T.goto(0, 0, 1.6); await waitFor("L06.nurseryReached", 10000); T.skipTalk();
    if (!AZ.get("L06.nurseryReached")) return fail("nursery");
    // shapes: each needs two quarter-turns, in its own state
    for (const what of ["carved wooden star", "dented tin moon", "cube of light"]) for (let i = 0; i < 2; i++) await T.use(what);
    if (!AZ.get("L06.shapesAligned")) return fail("shapes");
    // slide the furniture away from the ledges
    for (const n of ["cradle", "wardrobe", "toy chest"]) await T.use("Slide the " + n + " aside");
    await T.wait(1300);
    // wind the three boxes (standing in the gaps, not on the void side)
    const states = ["PAST", "PRESENT", "FUTURE"], xs = [-5, 0, 5];
    for (let i = 0; i < 3; i++) {
      T.shift(i); await T.wait(750);
      T.goto(xs[i], 0, 16.3); T.face(xs[i], 17.1);
      const it = W().interactables.find(o => o.enabled && o.prompt === "Wind the music box (" + states[i] + ")");
      await AZ.sim.use(W(), it);
      if (!AZ.get("L06.heard" + states[i][0] + states[i].slice(1).toLowerCase())) return fail("wind " + states[i]);
    }
    // hum to each box while keeping the door in view; retry if the room forgets
    for (let attempt = 0; attempt < 3 && !AZ.get("L06.lullabyComplete"); attempt++) {
      for (let i = 0; i < 3 && !AZ.get("L06.lullabyComplete"); i++) {
        T.shift(i); await T.wait(750);
        T.goto(xs[i], 0, 16.3); faceDoor();
        T.hum(); await T.wait(2400); faceDoor(); T.skipTalk();
      }
    }
    if (!AZ.get("L06.lullabyComplete")) return fail("lullaby");
    // reach the drifting door
    await T.wait(300);
    const d = W().L06.door;
    T.goto(d.position.x + Math.sin(d.rotation.y) * 0.7, 0, d.position.z + Math.cos(d.rotation.y) * 0.7);
    if (!(await waitFor("L06.doorAnchored", 3000))) return fail("door");
    await T.wait(500);
    // the Witness
    T.goto(0, 0, 21.5); await T.wait(300);
    T.goto(0, 0, 23.6);
    for (let i = 0; i < 40 && !AZ.get("L06.witnessHeard"); i++) { await T.wait(400); T.skipTalk(); }
    if (!AZ.get("L06.witnessHeard") || !AZ.get("maren.isWitness")) return fail("witness");
    await T.wait(300);
    await T.use("Take Ilse's carved zero");
    if (!AZ.get("L06.zeroTaken")) return fail("zero");
    await T.wait(2200);
    T.goto(0, 0, 39); await T.wait(300);
    T.goto(0, 0, 41.3); await T.wait(2500);
    log("objective now: " + T.objective() + " | level: " + T.level());
    if (T.level() !== "L07") fail("did not reach L07");
    else log("PASSED");
  } catch (e) { fail(e.message); }
})();
