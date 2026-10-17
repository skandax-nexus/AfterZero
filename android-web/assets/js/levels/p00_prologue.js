/* PROLOGUE — The Last Train. Reference chapter for the level API (see js/world.js).
   The carriage is a baked prop (tools/props/build_train.py -> props/train_carriage.glb, driven by js/props.js): the
   train runs through a rainy night until Arin has spoken to the conductor, then brakes, the landscape dissolves into
   Veyra's terminus and the end doors slide open. Colliders below follow the prop's layout (scene extras). */
AZ.registerLevel({
  id: "P00", order: 0, chapter: "PROLOGUE", title: "The Last Train", location: "Veyra Line, night service",
  start: 1, allowed: 2,
  build(L) {
    const { line } = L, S = L.shared, R = AZ.render;
    L.look({ fog: 0.5, present: { sunI: 0.05, hemiI: 0.34, hemiS: 0x50688a, hemiG: 0x161210, env: 0.55, exp: 1.0, bloom: 0.7, tint: [0.97, 1.0, 1.04], sat: 0.95, con: 1.06 } });
    L.intro(line("Arin", "...You were asleep. You don't remember getting on."), line("Arin", "A coat too heavy for spring. A ticket with no departure station."), line("Conductor Sefa", "Veyra, last stop. Everyone off before the bell, please."));
    L.objective("Check your coat on the seat across the aisle", "arin.watch", "The dark coat on the seat behind you. Tap it.");
    L.objective("Read the letter on the seat", "P.letterRead", "An envelope with a ring-and-zero seal, two rows ahead.");
    L.objective("Ask the conductor where this train came from", "P.talkedConductor", "Conductor Sefa walks the aisle.");
    L.objective("Step off the train", "P.offTrain", "The doors at the front of the carriage are open.");
    L.objective("Walk through the arrival gate into the city", "P.gate", "Follow the platform lamps to the gate.");
    L.payoff("P.letterRead", "A letter dated tomorrow", "\"Don't trust the first bell. Save the blue cup. You have done this before.\" Sealed with the clock-tower seal: (0).");

    // ---------------------------------------------------------------- the carriage (22 m, along +z; exit door at +z)
    const T = AZ.props && AZ.props.train ? AZ.props.train(L, S) : null;
    const lay = T ? T.layout : { W: 1.6, A: 0.52, ZB: -10.9, ZF: 10.85, ZFO: 11.1, bays: [-8, -6, -4, -2, 0, 2, 4, 6, 8], partitions: [-9, -7, -5, -3, -1, 1, 3, 5, 7, 9], door: { hw: 0.7, h: 2.3 }, coat: [-0.95, 0.47, -8.61], letter: [0.95, 0.469, -4.6] };
    const W = lay.W, A = lay.A, ZB = lay.ZB, ZF = lay.ZF, ZFO = lay.ZFO, D = lay.door;
    if (!T) fallbackCarriage(L, S, lay);
    // colliders: shell, ceiling (keeps the camera inside), benches (backs + seats), window tables, racks, luggage
    L.block(0, (ZB + ZF) / 2, 2 * W + 0.4, ZF - ZB + 0.3, -0.4, 0);
    for (const s of [-1, 1]) L.block(s * (W + 0.1), (ZB + ZFO) / 2, 0.2, ZFO - ZB + 0.6, 0, 3.2);
    L.block(0, ZB - 0.1, 2 * W + 0.4, 0.2, 0, 3.2);
    for (const s of [-1, 1]) L.block(s * (D.hw + (W + 0.2 - D.hw) / 2), (ZF + ZFO) / 2, W + 0.2 - D.hw, ZFO - ZF, 0, 3.2);
    L.block(0, (ZF + ZFO) / 2, 2 * D.hw, ZFO - ZF, D.h, 3.2);
    const doorway = L.block(0, (ZF + ZFO) / 2, 2 * D.hw, ZFO - ZF, 0, D.h);
    L.block(0, (ZB + ZF) / 2, 2 * W, ZF - ZB, 2.5, 3.4);
    const midW = (W - A) / 2 + A, nP = lay.partitions.length;
    for (const s of [-1, 1]) {
      lay.partitions.forEach((zp, i) => {
        L.block(s * midW, zp, W - A, 0.42, 0, 1.33);
        for (const d of [-1, 1]) if ((d > 0 && i < nP - 1) || (d < 0 && i > 0)) L.block(s * midW, zp + d * 0.43, W - A, 0.47, 0, 0.47);
      });
      lay.bays.forEach(zb => L.block(s * (W - 0.17), zb, 0.34, 0.52, 0, 0.73));
      L.block(s * (W - 0.21), 0, 0.42, 19.4, 1.98, 2.12);
    }
    L.block(1.25, ZB + 0.32, 0.62, 0.62, 0, 0.72);
    L.block(1.27, ZF - 0.21, 0.56, 0.4, 0, 0.62);

    L.player(0, 0, -7.9, Math.PI);
    L.checkpoint(0, 0, -7.9, Math.PI, { size: [1.4, 2.5, 1.4] });

    // Arin's coat with the stopped watch (Design v2 personal thread)
    const coat = lay.coat;
    L.clue({ group: S, mesh: T ? T.nodes.coat : null, at: [coat[0], 0.62, coat[2]], size: [0.44, 0.1, 0.38], mat: "navy", prompt: "Check the coat pocket", flag: "arin.watch", id: "arin.watch", title: "A stopped pocket watch",
      text: "Brass, scratched, heavier than it looks. The hands are stuck at a minute to midnight.\n\nEngraved inside the lid:\n\n\"For A. - keep time for me.  I.M.\"",
      lines: [line("Arin", "It doesn't tick. I don't know why I'm sure it used to."), line("The Witness", "(faint, over the intercom)  Don't be afraid of the bell, Arin.", 4)] });
    const lt = lay.letter;
    L.clue({ group: S, mesh: T ? T.nodes.letter : null, at: [lt[0], lt[1] + 0.03, lt[2]], size: [0.23, 0.01, 0.13], prompt: "Read the letter", flag: "P.letterRead", id: "prologue.letter", title: "A letter dated tomorrow",
      text: "Arin,\n\nYou won't remember this, and that's all right. Don't trust the first bell. When the cup breaks, look at what stays broken.\n\nSave the blue cup. Then come to the tower.\n\nYou have done this before.\n\n                                   (0)\n\nDated: tomorrow.",
      lines: [line("Arin", "My name. My... handwriting? No. Older. Shakier.")] });

    // ---------------------------------------------------------------- conductor + passengers (seated)
    L.npc({ name: "Conductor Sefa", at: [0, 6], yaw: Math.PI, coat: "navy", task: "Walks the last service, counting passengers off before the bell.",
      route: [[0, 6, 4, "look"], [0, -3, 3, "work"], [0, 9.5, 5, "idle"]], approach: ["Last stop. Off before the bell.", "You again? No... I'd remember that coat."],
      talk: [line("Conductor Sefa", "Where from? This is the only train, friend. It goes to Veyra and it stops."), line("Conductor Sefa", "Funny thing. I count one more passenger off than I count on. Every night."), line("Arin", "Every night?"), line("Conductor Sefa", "Did I say that? Long shift. Off you go, the festival's starting.")],
      cond: [{ flag: "P.letterRead", sets: "P.talkedConductor", lines: [line("Arin", "Does this seal mean anything? A ring with a zero."), line("Conductor Sefa", "That's the tower's seal. Only the clockkeeper uses it. And she hasn't written to anyone in forty years."), line("Conductor Sefa", "The train came from the end of the line. Don't ask me where the end of the line is.")] }],
      talkedFlag: "P.talkedConductor", changedFlag: "P.offTrain", changedRoute: [[0, 9.6, 999, "look"]], changedTalk: [line("Conductor Sefa", "Go on. The bell rings twice tonight. It always does.")] });
    // A passenger sits on the bench backed on partition zp, facing d (+1 = toward the exit). Until the cast has a
    // 'sit' clip they stand in the bay instead (never inside the bench).
    const seated = (o, s, zp, d, aisle) => {
      const x = s * (aisle ? 0.80 : 1.31), z = zp + d * 0.34, yaw = d > 0 ? 0 : Math.PI;
      const n = L.npc(Object.assign({ name: "", named: false, at: [x, z], yaw, route: [[x, z, 999, "sit"]] }, o));
      const h = n.p.human;
      if (!h || !h.actions || !h.actions.sit) {
        const sz = zp + d * 0.95, sx = s * (aisle ? 0.62 : 0.95);
        n.p.group.position.set(sx, n.p.group.position.y, sz); n.p.group.rotation.y = s > 0 ? Math.PI / 2 : -Math.PI / 2;
        n.route = [[sx, sz, 999, "look"]]; n.o.route = n.route;
        const st = n.p.group.position.clone(), sy = n.p.group.rotation.y;
        n.reset = () => { n.p.group.position.copy(st); n.p.group.rotation.y = sy; n.idx = 0; n.wait = 0; };
      }
      return n;
    };
    seated({ coat: "olive" }, 1, -9, 1, false);              // asleep against the window, behind Arin
    seated({ coat: "burgundy" }, -1, -3, -1, false);         // reading by the window
    seated({ coat: "grey" }, 1, -1, 1, true);                // an old man dozing on the aisle seat
    seated({ coat: "red" }, -1, 5, -1, false);               // the child's mother, facing her
    seated({ coat: "ochre", scale: 0.6 }, -1, 3, 1, false);  // the child, asleep
    seated({ coat: "gold" }, 1, 7, -1, false);
    L.onHum([-1.05, 0.5, 3.6], 4, () => { L.set("P.humHeard"); L.say([line("Child", "(waking)  That song... how do you know the Nursery song?"), line("Arin", "I don't. It was just... in my head.")]); });
    L.zone({ at: [-0.3, 1.2, 3.6], size: [2.5, 2.5, 2], toast: "A tune is stuck in your head. Press HUM." });
    L.zone({ at: [0, 1.2, 12.3], size: [3, 2.5, 1.4], flag: "P.offTrain", toast: "Veyra. The air smells of rain and festival smoke." });
    L.fragment("P.1", [-1.05, 1.0, 8.5], "A ticket stub", "Veyra Line. Single. The departure station has been scratched out so many times the paper is thin.", S);

    // ---------------------------------------------------------------- the terminus (hidden while the train runs)
    const PL = new THREE.Group(); S.add(PL);
    L.ramp(PL, [0, 0, 11.1], [0, 0.6, 12.6], 1.4, "iron");
    L.floor(PL, -2.5, 31, 8, 38, 0.6, "stoneWet");
    L.floor(PL, -2.5, 52.5, 8, 5, 0.6, "tile");
    L.box(PL, 5, 0.4, 60, "concrete", 4, -0.8, 30, { noEcho: true });
    L.block(1.4, 31, 0.2, 38, 0.6, 3, 7);
    L.block(-6.6, 36, 0.2, 48, 0.6, 3, 7);
    // concourse edge railings either side of the gangway (no stepping off onto the tracks)
    for (const [x0, x1] of [[-6.5, -0.75], [0.75, 1.5]]) {
      L.box(PL, x1 - x0, 0.05, 0.05, "iron", (x0 + x1) / 2, 1.62, 12.05, { solid: false });
      L.box(PL, x1 - x0, 0.04, 0.04, "iron", (x0 + x1) / 2, 1.1, 12.05, { solid: false });
      for (let x = x0 + 0.05; x <= x1; x += 1.1) L.box(PL, 0.06, 1.05, 0.06, "iron", x, 1.12, 12.05, { solid: false });
      L.block((x0 + x1) / 2, 12.05, x1 - x0, 0.2, 0.6, 3);
    }
    // buffer stop, track and the arrival platform alongside the carriage (seen through the windows once stopped)
    L.box(PL, 3.0, 0.32, 0.4, "red", 0, -0.14, 11.95, { solid: false });
    for (const bx of [-0.95, 0.95]) L.cyl(PL, 0.17, 0.3, "iron", bx, -0.14, 11.7).rotation.x = Math.PI / 2;
    L.box(PL, 3.4, 0.3, 54, "concrete", 0, -1.35, -15, { solid: false, noEcho: true });
    for (const rx of [-0.72, 0.72]) L.box(PL, 0.07, 0.14, 54, "rail", rx, -1.13, -15, { solid: false, noEcho: true });
    for (let z = -40; z < 11.5; z += 0.7) L.box(PL, 2.4, 0.1, 0.22, "woodDark", 0, -1.22, z, { solid: false, noEcho: true, shadow: false });
    L.floor(PL, -4.6, -6, 5.4, 36, -0.05, "stoneWet", 1.2);
    L.block(-1.92, -6, 0.1, 36, -1.3, 3);
    for (let i = 0; i < 6; i++) { const z = -20 + i * 6; L.lamp(PL, -4.2, z, "sodium", 3.6, -0.05); L.cyl(PL, 0.12, 4.6, "iron", -6.6, 2.25, z + 3, { seg: 8 }); }
    L.box(PL, 6, 0.2, 38, "iron", -4.4, 4.75, -6, { solid: false });
    L.box(PL, 0.3, 4.8, 36, "brick", -7.4, 2.35, -6, { solid: false });
    L.label(PL, "VEYRA  CENTRAL", -7.22, 2.6, -4, Math.PI / 2, 0.42, "#f2e6c8", "rgba(14,20,34,.92)");
    L.label(PL, "VEYRA  CENTRAL", -7.22, 2.6, -16, Math.PI / 2, 0.42, "#f2e6c8", "rgba(14,20,34,.92)");
    for (const z of [-12, 0]) { L.box(PL, 0.5, 0.06, 1.8, "woodDark", -6.6, 0.42, z, { solid: false }); L.box(PL, 0.08, 0.45, 1.8, "woodDark", -6.85, 0.62, z, { solid: false }); }
    for (let i = 0; i < 6; i++) L.lamp(PL, -5.5, 14 + i * 6, "sodium", 4, 0.6);
    L.box(PL, 8.5, 0.2, 40, "iron", -2.5, 5.2, 30, { solid: false });
    L.label(PL, "VEYRA  ARRIVALS", -6.3, 3.6, 26, Math.PI / 2, 0.5, "#cfeeff", "rgba(8,14,22,.9)");
    L.building(PL, -2.5, 58, 14, 12, 6, "plasterCold", "window", 3, "ARRIVAL QUARTER");
    L.door(PL, 3, 3, 0.2, "rust", -2.5, 0.6, 49.6, ["P.talkedConductor", "P.letterRead"], [0, 3.1, 0], { toast: "The arrival gate grinds open." });
    L.wall(PL, -9, 49.6, -4, 49.6, 6, "plasterCold", 0.4, 0.6);
    L.wall(PL, -1, 49.6, 4, 49.6, 6, "plasterCold", 0.4, 0.6);
    L.zone({ at: [-2.5, 1.8, 48.4], size: [3, 2.5, 1], flag: "P.gate" });
    L.exit({ group: PL, at: [-2.5, 0.6, 52], requires: ["P.letterRead", "P.talkedConductor"], locked: "The gate is shut. Read the letter and talk to the conductor first." });
    L.checkpoint(-1, 0.6, 16, 0);
    L.particles(PL, "rain", [16, 14, 44], [-2, 0, 32], 1200);
    L.particles(PL, "rain", [7, 12, 30], [-4.5, -0.5, -6], 500);
    for (let i = 0; i < 14; i++) L.building(PL, 14 + (i % 3) * 9, 2 + i * 6, 7, 10 + (i * 7) % 13, 6, "plasterCold", "windowCold", i);
    L.box(PL, 9, 50, 9, "plasterCold", 30, 25, 120, { solid: false, noEcho: true });
    L.holoMesh(PL, "cyl", "amber", 30, 42, 115.4, 6, 0.1, 6).rotation.x = Math.PI / 2;
    L.light(PL, 0xffd090, 5, 30, 30, 42, 112);
    L.fragment("P.2", [-6, 1.6, 40], "Chalk tally", "Behind the lamp post: tally marks in chalk, hundreds of them. The newest one is still wet.", PL);
    L.fragment("P.3", [0.8, 1.6, 46], "A child's drawing", "A tower with two bells and a blue cup. Signed 'M.O.'", PL);
    R.mergeStatic(PL); PL.userData.noMerge = true;   // batched on its own so it can be hidden while the train runs

    // ---------------------------------------------------------------- the run, the stop, the doors
    const mach = L.ambient("machine", 0.28); L.ambient("rain", 0.22);
    let stopping = false, arrived = false;
    const arrive = () => {
      if (arrived) return;
      arrived = true; PL.visible = true; doorway.on = false;
      if (T) { T.fadeOut = true; T.openDoors(); }
      AZ.sfx("scrape", 0.45, 0.7);
      setTimeout(() => L.toast("The doors slide open onto the terminus.", 3.5), 300);
    };
    L.when(["P.talkedConductor"], () => {
      if (!T) return arrive();
      if (stopping) return;
      stopping = true;
      setTimeout(() => { if (!arrived) L.toast("The brakes catch. The carriage shudders and slows.", 3.5); }, 600);
      T.brake(7).then(() => setTimeout(arrive, 900));
    });
    PL.visible = !T;
    L.update(() => {
      const p = AZ.player.pos, inside = Math.abs(p.x) < W + 0.25 && p.z > ZB - 0.3 && p.z < ZF + 0.15 && p.y < 3;
      if (!inside && !arrived) { if (T) { T.speed = T.target = 0; if (T.onStop) { const f = T.onStop; T.onStop = null; f(); } } arrive(); }
      if (T) {
        T.setInside(inside);
        const k = T.speed / 24;
        if (mach) { mach.vol = 0.06 + 0.24 * k; if (mach.src && mach.src.playbackRate) mach.src.playbackRate.value = 0.55 + 0.45 * k; }
      }
    });
  },
});

/** Box-built carriage (used only if the baked prop could not load, e.g. a browser that blocks file:// fetches). */
function fallbackCarriage(L, S, lay) {
  const W = lay.W, A = lay.A;
  L.floor(S, 0, 0, 2 * W, 22, 0, "woodDark");
  L.box(S, 2 * W, 0.2, 22, "wood", 0, 2.8, 0, { solid: false });
  for (const s of [-1, 1]) { L.box(S, 0.15, 1, 22, "wood", s * (W + 0.07), 0.5, 0, { solid: false }); L.box(S, 0.15, 0.5, 22, "wood", s * (W + 0.07), 2.45, 0, { solid: false }); L.box(S, 0.04, 0.95, 22, "glass", s * (W + 0.07), 1.47, 0, { solid: false }); }
  L.box(S, 2 * W + 0.3, 2.9, 0.2, "wood", 0, 1.45, lay.ZB - 0.1, { solid: false });
  for (const s of [-1, 1]) L.box(S, W - lay.door.hw + 0.15, 2.9, 0.25, "wood", s * (lay.door.hw + (W - lay.door.hw) / 2), 1.45, (lay.ZF + lay.ZFO) / 2, { solid: false });
  const mid = (W - A) / 2 + A;
  for (const s of [-1, 1]) lay.partitions.forEach(zp => {
    L.box(S, W - A, 1.3, 0.12, "woodDark", s * mid, 0.65, zp, { solid: false });
    for (const d of [-1, 1]) { L.box(S, W - A, 0.12, 0.5, "burgundy", s * mid, 0.41, zp + d * 0.4, { solid: false }); L.box(S, W - A, 0.62, 0.1, "burgundy", s * mid, 0.82, zp + d * 0.11, { solid: false }); L.box(S, W - A, 0.35, 0.5, "woodDark", s * mid, 0.17, zp + d * 0.4, { solid: false }); }
  });
  lay.bays.forEach(zb => { L.light(S, 0xffc27a, 1.1, 5, 0, 2.45, zb); L.mesh(S, "sphere", "warm", 0, 2.6, zb, 0.24, 0.2, 0.24).castShadow = false; });
}
