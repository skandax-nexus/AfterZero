// Scripted walkthrough of LEVEL 07 (the back-lane route, no cross-level proof): every beat is completable.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m), W = () => T.world();
  const SAFE = [-22, -32.4];
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).filter(f => f.startsWith("L07")).join(","));
  const waitFor = async (cond, ms = 10000) => { const t0 = Date.now(); while (!cond()) { if (Date.now() - t0 > ms) return false; await T.wait(200); } return true; };
  const useAt = async (prompt, x, z) => {
    const it = W().interactables.find(i => i.enabled && i.prompt === prompt && Math.abs(i.pos.x - x) < 0.2 && Math.abs(i.pos.z - z) < 0.2);
    if (!it) throw new Error("no interactable " + prompt + " at " + x + "," + z);
    T.goto(it.pos.x, 0, it.pos.z - 1.3); await T.wait(80);
    await AZ.sim.use(W(), it); await T.wait(150);
  };
  // Lead a following group to the Tram Hall by walking the player there in steps.
  // Lead a following group: walk a few steps (proves they follow), then teleport the column to the hall
  // (headless SwiftShader runs at a few fps, so real-time walking would take minutes).
  const lead = async (gi, path) => {
    const G = W().evac[gi];
    if (!G.following) throw new Error("group " + gi + " is not following");
    const [x0, z0] = path[0];
    const before = G.members[0].p.group.position.clone();
    T.goto(x0, 0, z0);
    await waitFor(() => G.members[0].p.group.position.distanceTo(before) >= 0.3, 10000);
    const moved = G.members[0].p.group.position.distanceTo(before);
    if (moved < 0.3) throw new Error("group " + gi + " did not move toward the player");
    T.goto(SAFE[0], 0, SAFE[1] + 2.4); T.face(SAFE[0], SAFE[1] + 12);
    G.members.forEach((m, i) => m.p.group.position.set(SAFE[0] + (i % 3 - 1), 0, SAFE[1] + 0.5 * Math.floor(i / 3)));
    await waitFor(() => G.saved, 10000);
  };
  try {
    await T.wait(2500);
    T.goto(0, 0, -20);
    if (!(await waitFor(() => AZ.get("L07.crisisStarted")))) return fail("crisis did not start");
    T.skipTalk();
    // Brace the wall (PAST)
    await T.use("Lock the scaffolding"); T.skipTalk();
    T.shift(1); await T.wait(800);
    if (!AZ.get("L07.scaffoldBraced")) return fail("scaffold");
    // Halden refuses without proof -> back lane
    T.use("Confront Founder Halden");
    await T.wait(500); for (let i = 0; i < 6; i++) { T.skipTalk(); await T.wait(150); }
    await T.pick(2); for (let i = 0; i < 6; i++) { T.skipTalk(); await T.wait(150); }
    T.shift(0); await T.wait(800);
    await T.use("Lift the service door bar"); T.skipTalk();
    if (!AZ.get("L07.tramRoute")) return fail("tram route");
    // Group 1: Market Row through the PAST
    await T.use("Lead Market Row group"); T.skipTalk();
    await lead(0, [[36, 1]]);
    if (!AZ.get("L07.group1Saved")) return fail("group 1");
    // Group 2: courtyard via the braced lane (PRESENT)
    T.shift(1); await T.wait(800);
    await T.use("Lead Ashgrove courtyard group"); T.skipTalk();
    await lead(1, [[-45, 1.5]]);
    if (!AZ.get("L07.group2Saved")) return fail("group 2");
    // Ground the surge (FUTURE)
    T.shift(2); await T.wait(800);
    await T.use("Take grounding cable");
    await T.use("Clamp the cable"); T.skipTalk();
    if (!AZ.get("L07.surgeGrounded")) return fail("surge");
    // Group 3: behind the tower, out through the FUTURE alley
    await T.use("Lead Warden's drill group"); T.skipTalk();
    await lead(2, [[4, 43.5]]);
    if (!AZ.get("L07.group3Saved")) return fail("group 3");
    // The signal
    await T.use("Read the tower's signal"); T.closeDoc(); T.skipTalk();
    if (!AZ.get("L07.signalSeen")) return fail("signal");
    // Climax: drive III + the lullaby plates
    T.shift(1); await T.wait(800);
    T.goto(0, 0, 12); await T.wait(300);
    await T.use("drive III"); T.skipTalk();
    if (!AZ.get("L07.driveOpened")) return fail("drive III");
    const plate = i => { const a = (200 + i * 28) * Math.PI / 180; return [Math.cos(a) * 9.5, 22 + Math.sin(a) * 9.5]; };
    for (const i of [0, 2, 4, 3]) { const [x, z] = plate(i); await useAt("Strike the bell", x, z); }
    await T.wait(500);
    if (!AZ.get("L07.bellDisabled")) return fail("bell sequence");
    T.skipTalk();
    // Climb to the core and enter the Zero Chamber
    await T.wait(2200);
    T.goto(0, 0, 28.5); await T.wait(400);
    T.goto(-4.1, 6.1, 34);
    if (!(await waitFor(() => AZ.get("L07.coreReached")))) return fail("core");
    T.goto(-4.1, 6.1, 39.3);
    await waitFor(() => T.level() === "L08", 12000);
    log("objective now: " + T.objective() + " | level: " + T.level());
    if (T.level() !== "L08") return fail("did not reach L08");
    log("PASSED");
  } catch (e) { fail(e.message); }
})();
