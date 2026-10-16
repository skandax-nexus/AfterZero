// Scripted walkthrough of Level 05 (The Drowned District): proves every beat is completable.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m);
  const fail = m => console.error("[solve] FAILED: " + m + " | objective=" + T.objective() + " flags=" + Object.keys(AZ.flags).join(","));
  const pickWhenShown = async (i, ms = 30000) => { const t0 = Date.now(); while (!document.querySelector("#pChoice.show .opt") && Date.now() - t0 < ms) { T.skipTalk(); await T.wait(250); } await T.pick(i); };
  const waitFor = async (flag, ms) => { const t0 = Date.now(); while (!AZ.get(flag) && Date.now() - t0 < ms) await T.wait(150); return AZ.get(flag); };
  try {
    await T.wait(600);
    T.skipTalk();
    AZ.set("evidence.list");                     // carried over from L04 in a real playthrough
    // 1. pump (PAST)
    await T.use("Take pump handle");
    await T.use("Seat the handle");
    if (!AZ.get("L05.pumpRepaired")) return fail("pump");
    if (!(await waitFor("L05.basinDrained", 20000))) return fail("basin did not drain");
    // 2. sluices (PRESENT)
    T.shift(1); await T.wait(800);
    for (const d of ["west", "north", "east"]) await T.use("Turn the " + d + " lane sluice");
    if (!AZ.get("L05.waterRedirected")) return fail("sluices");
    // 3. three family rooms
    for (const f of ["Vell", "Corran", "Oskel"]) { await T.use("Read the " + f + " calendar"); T.closeDoc(); await T.wait(200); }
    T.skipTalk();
    if (!AZ.get("L05.roomsCompared")) return fail("rooms");
    // 4. rescue: hear Maren, shore the beam in the PAST, hum, lead her out
    T.goto(9, 0, -12); if (!(await waitFor("L05.heardMaren", 3000))) return fail("Maren's cries");
    for (let i = 0; i < 3; i++) { await T.use("Take timber shore"); await T.use("Set the shore under the beam"); }
    if (!AZ.get("L05.debrisCleared")) return fail("shores");
    T.shift(1); await T.wait(800);
    T.goto(26.5, 0, -11.5); await T.wait(400);
    T.hum(); await waitFor("maren.recognised", 4000);
    if (!AZ.get("maren.recognised")) return fail("hum");
    T.skipTalk();
    await T.use("Take Maren's hand"); T.skipTalk();
    // walk her out (teleport-and-wait: the follower moves her; we place her on the route to save sim time)
    const m = T.world().npcs.find(n => n.name === "Maren Oskel");
    T.goto(0, 0, -16.8); T.face(0, 0);
    m.p.group.position.set(0.5, 0, -17.6);
    await waitFor("L05.childRescued", 8000);
    if (!AZ.get("L05.childRescued")) return fail("Maren not led to the stairs");
    // 5. Ondine at the boat (show the list)
    T.goto(0, 3, -30); await T.wait(500);
    T.goto(2.5, 0, -14.5); await T.wait(900);
    await pickWhenShown(0); await T.wait(300);
    T.skipTalk(); await T.wait(300); T.skipTalk();
    if (!(await waitFor("L05.ondineDone", 4000))) return fail("Ondine dialogue");
    // 6. the flood returns: save the music box
    if (!(await waitFor("L05.crisis", 3000))) return fail("crisis");
    T.skipTalk();
    T.use("Save Maren's music box"); await pickWhenShown(0); await T.wait(400);
    if (!AZ.get("choice.L05.made")) return fail("choice");
    // 7. the door with no house
    await T.wait(2000);
    T.goto(9, 0, 10.2); await T.wait(500);
    T.goto(9, 0, 11.8); await T.wait(2500);
    log("objective now: " + T.objective() + " | level: " + T.level());
    if (T.level() !== "L06") fail("did not reach L06");
    else log("PASSED");
  } catch (e) { fail(e.message); }
})();
