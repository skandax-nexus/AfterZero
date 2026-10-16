// Scripted walkthrough of LEVEL 08: zero, bells across the states, Tribunal, memory, choice, order, horizon -> Epilogue.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m), W = () => T.world();
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).filter(f => f.startsWith("L08") || f.startsWith("choice")).join(","));
  const pol = (r, deg) => [Math.cos(deg * Math.PI / 180) * r, Math.sin(deg * Math.PI / 180) * r];
  const waitFor = async (cond, ms = 30000) => { const t0 = Date.now(); while (!cond()) { if (Date.now() - t0 > ms) return false; T.skipTalk(); await T.wait(250); } return true; };
  const choicePanel = () => !!document.querySelector("#pChoice.show");
  try {
    await T.wait(2500);
    await T.use("Take carved zero");
    await T.use("Set the carved zero"); T.skipTalk();
    if (!AZ.get("L08.zeroPlaced")) return fail("zero");
    // bells: note -> [state, wing angle, side]
    const bells = { 0: [0, 180, -2.5], 2: [0, 180, 2.5], 4: [1, 90, -2.5], 6: [1, 90, 2.5], 5: [2, 0, -2.5], 1: [2, 0, 2.5] };
    for (const note of [4, 0, 5, 2, 4]) {
      const [s, a, side] = bells[note], [cx, cz] = pol(23.6, a), [tx, tz] = pol(1, a + 90);
      const x = cx + tx * side, z = cz + tz * side;
      if (W().state !== s) { T.goto(x - pol(1, a)[0] * 2, 0, z - pol(1, a)[1] * 2); await T.wait(300); T.shift(s); await T.wait(900); }
      const it = W().interactables.find(i => i.enabled && i.prompt === "Strike the bell" && Math.abs(i.pos.x - x) < 0.2 && Math.abs(i.pos.z - z) < 0.2);
      if (!it) return fail("no bell for note " + note);
      T.goto(x - pol(1, a)[0] * 2, 0, z - pol(1, a)[1] * 2); await T.wait(100);
      await AZ.sim.use(W(), it); await T.wait(300);
    }
    if (!AZ.get("L08.sequenceRestored")) return fail("bell sequence");
    T.shift(1); T.goto(0, 0, -8); await T.wait(300);
    // the Tribunal: five hearings
    for (let k = 0; k < 5; k++) {
      if (!(await waitFor(choicePanel, 40000))) return fail("tribunal hearing " + (k + 1) + " never asked");
      await T.pick(k % 2);
    }
    if (!(await waitFor(() => AZ.get("L08.tribunal.done")))) return fail("tribunal");
    // the memory
    if (!(await waitFor(() => AZ.player.pos.y < -30, 20000))) return fail("memory teleport");
    T.goto(6, -40, -3);
    if (!(await waitFor(() => AZ.get("L08.memoryWalked"), 8000))) return fail("cot seven");
    T.use("Take the watch");
    if (!(await waitFor(() => AZ.get("L08.memoryPlayed"), 30000))) return fail("memory");
    if (!(await waitFor(() => AZ.get("L08.otherArinSpoke"), 40000))) return fail("other-branch Arin");
    // the choice
    T.use("Choose EVIDENCE");
    if (!(await waitFor(choicePanel, 8000))) return fail("choice confirm");
    await T.pick(0);
    if (!(await waitFor(() => AZ.get("choice.L08.made")))) return fail("choice");
    await waitFor(() => !AZ.ui.speaking || !AZ.ui.speaking(), 30000);
    await T.wait(500);
    await T.use("Read the intervention order"); T.closeDoc(); T.skipTalk();
    if (!AZ.get("L08.signatureFound")) return fail("order");
    await T.wait(2500);
    T.goto(0, 0, 40); await T.wait(600);
    T.goto(0, 0, 57.5);
    if (!(await waitFor(() => AZ.get("L08.horizonSeen"), 8000))) return fail("horizon");
    T.goto(0, 0, 61.6);
    await waitFor(() => T.level() === "E99", 10000);
    log("level: " + T.level());
    if (T.level() !== "E99") return fail("did not reach the Epilogue");
    log("PASSED");
  } catch (e) { fail(e.message); }
})();
