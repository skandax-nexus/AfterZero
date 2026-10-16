/* LEVEL 08 — The First Zero (final). Ported from the Unity Level08Builder (Design v2).
   Phase 1: place the zero.  Phase 2: rebuild the bell sequence across the three state wings while ceiling segments
   fall on a telegraphed rhythm.  Phase 3: the Tribunal (Halden, Ondine argue Stability; Odile, Pell, Maren argue
   Evidence).  Phase 4: a playable memory (child Arin walks the Nursery with Ilse, who gives the watch).
   Then the other-branch Arin, the watch starts ticking, the Stability / Evidence choice, the signed intervention
   order, and the corridor to the horizon (exit -> Epilogue).
   Layout (+z north): entry hall z -42..-30 (archive alcove to the west), rotunda r 18.5 at the origin with the dais,
   wings W = PAST bells, N = PRESENT bells, E = FUTURE bells; the Nursery memory lies sealed below (y -40). */
AZ.registerLevel({
  id: "L08", order: 8, chapter: "LEVEL 08", title: "The First Zero", location: "The Zero Chamber, beneath the bell",
  start: 1, allowed: 7, killY: -70,
  build(L) {
    const ln = L.line, S = L.shared, PA = L.past, PR = L.present, FU = L.future, R = AZ.render;
    const F = {
      zeroTaken: "L08.zeroTaken", zero: "L08.zeroPlaced", seq: "L08.sequenceRestored", trib: "L08.tribunal.done", memory: "L08.memoryPlayed",
      watch: "L08.watchGiven", walked: "L08.memoryWalked", other: "L08.otherArinSpoke", made: "choice.L08.made", stab: "choice.L08.stability", evid: "choice.L08.evidence",
      sign: "L08.signatureFound", horizon: "L08.horizonSeen",
    };
    const WR = 23.6;
    const pol = (r, deg) => [Math.cos(deg * Math.PI / 180) * r, Math.sin(deg * Math.PI / 180) * r];
    const face = (x, z, vx, vz) => Math.atan2(vx - x, vz - z);

    L.look({ fog: 0.45, past: { bloom: 0.7, hemiI: 1.0 }, present: { bloom: 0.9, sunI: 0.4, hemiI: 0.9, exp: 1.15 }, future: { bloom: 1.6, hemiI: 0.8 } });
    L.intro(ln("Arin", "Under the bell. It's bigger than the tower. Bigger than the square."),
      ln("Arin", "Rings of light, three wings - one for each state. And a niche in the middle, shaped like a zero."),
      ln("The Witness", "Everything began here. Place what you carry, and it will remember for you.", 4));
    L.objective("Place the carved zero in the central niche", F.zero, "The carved zero rests on the pedestal in the entry hall. Carry it to the niche on the central dais.");
    L.objective("Rebuild the bell sequence while the chamber breaks apart", F.seq, "Read the inscription around the niche, or listen to the chamber. Each bell hangs in one state's wing: << PAST west, [ PRESENT ] north, FUTURE >> east. Falling segments flash red first.");
    L.objective("Answer the Tribunal", F.trib, "Halden and Ondine argue for Stability; Odile, Pell and Maren for Evidence. Answer each of them.");
    L.objective("Walk the memory", F.memory, "Walk the Nursery with Ilse: your cot is number seven. She has something for you on her table.");
    L.objective("Choose: STABILITY or EVIDENCE", F.made, "The two consoles beside the dais. Only one can be chosen, and both cost something.");
    L.objective("Read the intervention order", F.sign, "The document risen from the niche.");
    L.objective("Walk the newly exposed corridor to the horizon", F.horizon, "The north wing's back wall has opened.");
    L.payoff(F.sign, "Signed in Your Own Hand", "The order that restarted the cycle - that folded Veyra back to the festival morning, again and again - is signed in Arin's handwriting.\n\nNot this Arin. An Arin from another branch, who stood in this chamber after the larger collapse, and chose a smaller catastrophe that repeats. You are the version of that Arin who gets to choose again.");

    // ---------------- chamber shell ----------------
    L.floor(S, 0, -4, 64, 80, 0, "obsidian");
    L.cyl(S, 18, 0.02, "ruin", 0, 0.012, 0);
    for (let i = 0; i < 4; i++) L.cyl(S, 6 + i * 3, 0.02, i % 2 ? "ruin" : "neonC", 0, 0.016 + i * 0.002, 0);
    const r = 18.5;
    for (let i = 0; i < 24; i++) {
      const a = i * 15;
      const [x1, z1] = pol(r, a - 7.5), [x2, z2] = pol(r, a + 7.5);
      if (i % 6 !== 0) L.wall(S, x1, z1, x2, z2, 16, "obsidian", 0.8);
      else L.wall(S, x1, z1, x2, z2, 16 - (i === 18 ? 5 : 12), "obsidian", 0.8, i === 18 ? 5 : 12);
      L.wall(S, x1, z1, x2, z2, 8, "obsidian", 0.8, 16);
      if (i % 2 === 1) { const [rx, rz] = pol(r - 0.7, a - 7.5); L.box(S, 0.08, 15, 0.08, "neonC", rx, 8, rz, { solid: false, shadow: false }); }
    }
    L.box(S, 40, 0.4, 20, "obsidian", 0, 24.2, 15, { solid: false }); L.box(S, 40, 0.4, 20, "obsidian", 0, 24.2, -15, { solid: false });
    L.box(S, 15, 0.4, 10, "obsidian", -12.5, 24.2, 0, { solid: false }); L.box(S, 15, 0.4, 10, "obsidian", 12.5, 24.2, 0, { solid: false });
    // concentric hologram rings + the zero of light
    for (let i = 0; i < 5; i++) { const m = L.holoMesh(S, "torus", ["cyan", "amber", "magenta"][i % 3], 0, 9 + i * 2.6, 0, (5 + i * 3) * 2, (5 + i * 3) * 2, 2, { spin: (i % 2 ? -1 : 1) * (0.1 + i * 0.05) }); m.rotation.x = Math.PI / 2 + (i % 2 ? -0.12 : 0.14); }
    L.holoMesh(S, "cyl", "cyan", 0, 26, 0, 2.2, 16, 2.2);
    const bigZero = L.label(S, "0", 0, 14, 0, 0, 9, "#eefaff"); bigZero.userData.dynamic = true; L.update(dt => { bigZero.rotation.y += dt * 0.2; });
    L.particles(S, "motes", [34, 14, 34], [0, 1, 0], 600);
    L.particles(S, "dreams", [30, 2, 30], [0, 0, 0], 250);
    L.light(S, 0x50d8ff, 3, 26, -8, 12, -6); L.light(S, 0xff50d0, 2.5, 26, 9, 14, 6); L.light(S, 0xbfe6ff, 4, 30, 0, 22, 0);

    // entry hall + corridor
    L.wallDoor(S, -5, -42, -5, -30, 7, "obsidian", 2.4, 3, 0.5, 0.6);
    L.wall(S, 5, -42, 5, -30, 7, "obsidian", 0.6); L.wall(S, -5, -42, 5, -42, 7, "obsidian", 0.6);
    L.wall(S, -5, -30, -2.4, -30, 7, "obsidian", 0.6); L.wall(S, 2.4, -30, 5, -30, 7, "obsidian", 0.6);
    L.box(S, 10.6, 0.4, 12.6, "obsidian", 0, 7.2, -36);
    L.wall(S, -2.4, -30, -2.4, -18.2, 5, "iron", 0.5); L.wall(S, 2.4, -30, 2.4, -18.2, 5, "iron", 0.5);
    L.box(S, 5.4, 0.4, 12, "obsidian", 0, 5.2, -24);
    for (let i = 0; i < 4; i++) { L.box(S, 0.05, 2.4, 0.12, "neonC", -2.12, 2.6, -29 + i * 3, { solid: false, shadow: false }); L.box(S, 0.05, 2.4, 0.12, "neonC", 2.12, 2.6, -29 + i * 3, { solid: false, shadow: false }); }
    L.label(S, "(0)", 0, 5.6, -30.35, 0, 1.2, "#dff4ff");
    L.label(S, "ZERO CHAMBER", 0, 6.2, -41.6, Math.PI, 0.6, "#9fe6ff");
    L.light(S, 0x9fd8ff, 2.2, 12, 0, 5, -37); L.light(S, 0x50e8ff, 1.8, 10, 0, 4, -24);
    L.player(0, 0, -39, Math.PI);
    L.checkpoint(0, 0, -39, Math.PI, { size: [8, 2.5, 4] });
    L.checkpoint(0, 0, -16, Math.PI, { size: [5, 2.5, 2] });
    L.checkpoint(0, 0, -6.5, Math.PI, { climax: true, size: [6, 2.5, 2] });

    // wings
    const wingNames = { 0: "FUTURE >>", 90: "[ PRESENT ]", 180: "<< PAST" };
    [0, 90, 180].forEach(a => {
      const [cx, cz] = pol(WR, a), [rx, rz] = pol(1, a), [tx, tz] = pol(1, a + 90);
      const ix = cx - rx * 5, iz = cz - rz * 5, ox = cx + rx * 5, oz = cz + rz * 5;
      L.wall(S, ix - tx * 5, iz - tz * 5, ix - tx * 2.4, iz - tz * 2.4, 12, "obsidian", 0.6);
      L.wall(S, ix + tx * 2.4, iz + tz * 2.4, ix + tx * 5, iz + tz * 5, 12, "obsidian", 0.6);
      L.wall(S, ix - tx * 2.4, iz - tz * 2.4, ix + tx * 2.4, iz + tz * 2.4, 6, "obsidian", 0.6, 6);
      L.wall(S, ix - tx * 5, iz - tz * 5, ox - tx * 5, oz - tz * 5, 12, "obsidian", 0.6);
      L.wall(S, ix + tx * 5, iz + tz * 5, ox + tx * 5, oz + tz * 5, 12, "obsidian", 0.6);
      if (a !== 90) L.wall(S, ox - tx * 5, oz - tz * 5, ox + tx * 5, oz + tz * 5, 12, "obsidian", 0.6);
      L.box(S, 10.6, 0.4, 10.6, "obsidian", cx, 12.2, cz, { solid: false });
      const [lx, lz] = pol(18, a);
      L.label(S, wingNames[a], lx, 6.6, lz, face(lx, lz, 0, 0), 1, a === 0 ? "#9ff0ff" : a === 90 ? "#dfeaff" : "#ffd890");
    });
    // north wing back wall: fixed pieces + the sliding halves (open on the signature)
    const zb = WR + 5;
    L.wall(S, -5, zb, -2.4, zb, 12, "obsidian", 0.6); L.wall(S, 2.4, zb, 5, zb, 12, "obsidian", 0.6); L.wall(S, -2.4, zb, 2.4, zb, 9, "obsidian", 0.6, 3);
    L.door(S, 2.4, 3, 0.4, "obsidian", -1.2, 0, zb, [F.sign], [-2.5, 0, 0], { toast: "The north wall slides apart. Behind it: a corridor, and wind." });
    L.door(S, 2.4, 3, 0.4, "obsidian", 1.2, 0, zb, [F.sign], [2.5, 0, 0]);
    L.label(S, "(0  0)\nSEALED UNTIL THE ORDER IS READ", 0, 3.7, zb - 0.35, Math.PI, 0.3, "#bfe8ff");

    // state dressing
    for (let i = 0; i < 8; i++) { const [x, z] = pol(16.5, 22.5 + i * 45); L.cyl(PA, 0.12, 2, "brass", x, 1, z); L.sphere(PA, 0.14, "warm", x, 2.15, z); const [bx, bz] = pol(17.6, 22.5 + i * 45); L.box(PA, 1.4, 7, 0.05, i % 2 ? "gold" : "red", bx, 8, bz, { solid: false, ry: face(bx, bz, 0, 0) }); }
    L.light(PA, 0xffb060, 2.6, 18, -12, 3, -6);
    L.particles(PA, "dust", [34, 10, 34], [0, 1, 0], 400);
    L.cyl(PA, 12, 0.02, "brass", 0, 0.02, 0);
    L.particles(PR, "rain", [9, 22, 9], [0, 0, 0], 500);
    L.particles(PR, "drips", [30, 12, 30], [0, 2, 0], 200);
    for (let i = 0; i < 6; i++) { const [x, z] = pol(9 + (i % 3) * 2.5, i * 60 + 20); L.box(PR, 2.5, 0.02, 1.8, "puddle", x, 0.02, z, { solid: false }); }
    L.lamp(PR, ...pol(15.5, 45), "sodium", 3.4); L.lamp(PR, ...pol(15.5, 225), "sodium", 3.4);
    L.crystals(FU, 10, 0, 0, 30, 30);
    L.debris(FU, 8, 0, 4, 0, 24, 10, 24);
    for (let i = 0; i < 3; i++) { const [x, z] = pol(14, 45 + i * 120); L.holoMesh(FU, "cyl", "cyan", x, 5, z, 0.8, 5, 0.8, { spin: 0.6, bob: 0.1 }); }
    L.light(FU, 0x50d8ff, 2.6, 14, -10, 1.5, 8);
    L.ambient("drone", 0.32, 7); L.ambient("machine", 0.25, 3); L.ambient("water", 0.25, 2); L.ambient("rain", 0.2, 2);

    // ---------------- Phase 1: the zero ----------------
    L.box(S, 0.9, 1, 0.9, "stone", 0, 0.5, -33);
    L.label(S, "WHAT ARIN CARRIED", 0, 0.6, -33.47, Math.PI, 0.22, "#ffe6b0");
    L.light(S, 0xffe0b0, 1.8, 5, 0, 2.6, -33);
    const zeroMesh = new THREE.Group(); zeroMesh.position.set(0, 1.3, -33); S.add(zeroMesh); zeroMesh.userData.dynamic = true;
    const zt = L.mesh(zeroMesh, "torus", "stone", 0, 0, 0, 0.9, 0.9, 3); zt.userData.dynamic = true;
    L.mesh(zeroMesh, "torus", "zero", 0, 0, 0.02, 0.6, 0.6, 1);
    L.update(dt => { zeroMesh.rotation.y += dt * 0.8; });
    L.item({ group: S, at: [0, 1.3, -33], mesh: zeroMesh, key: "zero", name: "carved zero", pickedFlag: F.zeroTaken });
    L.zone({ at: [0, 1, -36], size: [9, 2.5, 4], requires: ["L06.zeroTaken"], lines: [ln("Arin", "The zero I took from the nursery. I set it down for one breath and it's already warm - like it knows this room.")] });
    L.zone({ at: [0, 1, -33], size: [3, 2.5, 3], lines: [ln("Arin", "Carved by the same hand as the zero in the nursery. It belongs in the middle of this room.")] });
    L.cyl(S, 3.2, 0.3, "iron", 0, 0.15, 0, { solid: true });
    L.cyl(S, 3.3, 0.02, "neonC", 0, 0.31, 0);
    L.box(S, 1, 1, 1, "obsidian", 0, 0.8, 0);
    L.label(S, "( 0 )", 0, 0.8, -0.52, Math.PI, 0.35, "#dff4ff");
    L.socket({ group: S, at: [0, 1.32, 0], radius: 0.45, accepts: "zero", flag: F.zero, prompt: "Set the carved zero in the niche", hint: "The niche is shaped like a zero. The carved zero waits on the pedestal in the entry hall.", wrong: "The niche is shaped like a zero." });
    const power = [];
    for (let i = 0; i < 24; i++) { const [x, z] = pol(3.6, i * 15); const m = L.box(S, 0.9, 0.04, 0.18, "zero", x, 0.34, z, { solid: false, dynamic: true, ry: -i * 15 * Math.PI / 180 + Math.PI / 2, shadow: false }); m.visible = false; power.push(m); }
    const beam = L.holoMesh(S, "cyl", "cyan", 0, 12, 0, 0.6, 22, 0.6); beam.visible = false; power.push(beam);
    const order = ["1  (0)", "2  /\\", "3  [#]", "4  ~", "5  (0)"];
    order.forEach((t, i) => { const a = 250 + i * 10, [x, z] = pol(3.9, a); const m = L.label(S, t, x, 0.9, z, face(x, z, ...pol(10, a)), 0.4, "#f2f8ff"); m.visible = false; power.push(m); });
    const nicheLight = L.light(S, 0xd8f0ff, 0, 16, 0, 3, 0);
    L.when([F.zero], () => { power.forEach(m => { m.visible = true; }); nicheLight.intensity = 5; L.sfx("rule"); R.ripple(1);
      L.toast("The chamber wakes - and starts to break apart. An inscription lights around the niche; the bells can answer now.", 5); });
    L.clue({ group: S, at: [...[0], 0.35, -4.2], size: [1.6, 0.5, 0.3], mat: "iron", prompt: "Read the inscription", flag: "L08.inscriptionRead", id: "L08.inscription", title: "Inscription Around the Niche",
      requires: [F.zero], locked: "The inscription is dark. The niche is still empty.",
      text: "RING THEM AS THE FIRST CYCLE RANG:\n\n1  (0)  the zero      - [ PRESENT ], north\n2  /\\   the spire     - << PAST, west\n3  [#]  the cage      - FUTURE >>, east\n4  ~    the wave      - << PAST, west\n5  (0)  the zero      - [ PRESENT ], north\n\nA bell lives in only one state. Shift to where it hangs." });
    const cup = L.cyl(S, 0.2, 0.25, "cup", 1.4, 0.45, -1.2);
    L.interact({ at: [1.4, 0.45, -1.2], mesh: cup, prompt: "Look at the blue cup", once: true, onUse: () => L.say([ln("Arin", "The blue cup. Intact. The first thing I saved, the first morning."), ln("Arin", "Every cycle it falls and breaks. Here, it never has.")], false) });

    // ---------------- Phase 2: the bells (+ collapsing segments) ----------------
    const bells = [["/\\", "spire", 0, PA, 180, -2.5, "brass"], ["~", "wave", 2, PA, 180, 2.5, "brass"], ["(0)", "zero", 4, PR, 90, -2.5, "copper"], ["||", "gate", 6, PR, 90, 2.5, "copper"], ["[#]", "cage", 5, FU, 0, -2.5, "crystal"], ["<>", "eye", 1, FU, 0, 2.5, "crystal"]];
    const buttons = bells.map(([glyph, word, note, g, a, side, mat]) => {
      const [cx, cz] = pol(WR, a), [tx, tz] = pol(1, a + 90), [rx, rz] = pol(1, a);
      const x = cx + tx * side, z = cz + tz * side;
      L.box(g, 1.4, 0.5, 1.4, "iron", x, 0.25, z);
      L.box(g, 0.06, 3.6, 0.06, "iron", x, 4.4, z, { solid: false });
      const bell = L.mesh(g, "cone", mat, x, 1.9, z, 1.1, 1.3, 1.1);
      L.label(g, glyph, x - rx * 0.8, 3.4, z - rz * 0.8, face(x, z, 0, 0), 1, "#ffffff");
      L.label(g, word.toUpperCase(), x - rx * 0.8, 2.85, z - rz * 0.8, face(x, z, 0, 0), 0.28, "#dff0ff");
      if (g === FU) L.holoMesh(FU, "torus", "magenta", x, 2.8, z, 1.6, 1.6, 1, { spin: 1 }).rotation.x = Math.PI / 2;
      return { at: [x, 1.9, z], note, group: g, mesh: bell };
    });
    L.light(PA, 0xffb060, 3, 13, ...[pol(WR, 180)[0], 5, pol(WR, 180)[1]]);
    L.light(PR, 0xbcd8ff, 2.6, 13, 0, 5, WR);
    L.light(FU, 0xff50e0, 3, 13, WR, 5, 0);
    const seq = L.sequence({ buttons, solution: [4, 0, 5, 2, 4], flag: F.seq, bell: true, requires: [F.zero], lockedMsg: "Nothing answers yet. The niche is empty." });
    const horn = L.box(S, 0.5, 1.2, 0.5, "brass", ...[pol(4.2, 290)[0], 0.6, pol(4.2, 290)[1]]);
    L.interact({ at: [pol(4.2, 290)[0], 0.6, pol(4.2, 290)[1]], mesh: horn, prompt: "Listen to the chamber's memory of the bell", requires: [F.zero], locked: "Silent. The chamber has nothing to remember yet.", onUse: () => { seq.demo(); L.set("L08.melodyHeard"); } });
    L.clue({ group: FU, at: [WR + 3.5, 1.1, 0], size: [0.5, 1.4, 1.6], mat: "obsidian", prompt: "Read the branch log", flag: "L08.branchLogRead", id: "L08.branchLog", title: "Branch Log // Observer: ARIN",
      text: "CYCLES OBSERVED: 0, 1, 2, 3 ... (count unstable)\n\nWithout the loop, the collapse does not stop at Veyra. It spreads outward: regions where gravity forgets its direction, where water forgets to fall, where the sun is the wrong colour.\n\nA small catastrophe, repeating, holds the big one shut.\n- A." });
    [[14, 0], [14, 90], [14, 180], [9, 45], [9, 135], [9.5, 330]].forEach(([rr, a], i) => {
      const [x, z] = pol(rr, a);
      const chunk = L.box(S, 1.8, 0.8, 1.4, "obsidian", x, 11, z, { solid: false, dynamic: true });
      L.hazard({ group: S, at: [x, 0, z], size: [3, 2.4, 3], idle: 3.2, warn: 1.5, active: 1.2, offset: i * 0.9, requires: [F.zero], disabledBy: F.seq, visual: chunk, drop: 10.6,
        msg: "A ceiling segment fell. Red light and falling grit mark the next one - cross between the falls." });
      L.particles(S, "debris", [1.6, 1, 1.4], [x, 9.5, z], 30);
    });
    L.when([F.seq], () => { L.sfx("solved"); L.toast("The falling stops. The chamber holds its breath - and figures gather around the niche.", 4.5); });

    // ---------------- echoes (named NPCs) ----------------
    L.box(S, 1.2, 1.1, 0.7, "brass", -9.6, 0.55, 7); L.box(S, 1.1, 0.03, 0.6, "neonA", -9.6, 1.12, 7, { solid: false });
    L.box(S, 1.2, 1.1, 0.7, "obsidian", 8.6, 0.55, -12.2); L.box(S, 1.1, 0.03, 0.6, "neonC", 8.6, 1.12, -12.2, { solid: false });
    L.npc({ name: "Echo of Ilse Marrow", at: [-8.6, 6.2], coat: "olive", holo: "amber", task: "Echo of the clockkeeper who stopped the tower from inside. She still calibrates the chamber's rings at the brass ring console.",
      route: [[-8.8, 6.1, 5, "work"], [-11, -3, 3, "look"], [-6, 10.5, 3, "idle"]], approach: ["Another visitor. Or the same one, again.", "Mind the rings - they hold more than light."],
      talk: [ln("Ilse Marrow", "You climbed past me on the stairs, the night it happened. I held the engine with both hands."), ln("Ilse Marrow", "It stopped. They called it an earthquake. I stayed down here, between one tick and the next."), ln("Ilse Marrow", "I always carried a carved zero. I cut it from the old bell's clapper block, the morning of the first festival."), ln("Ilse Marrow", "The niche in the middle has been empty ever since. Every cycle, empty."), ln("Ilse Marrow", "The bells answer only in their own time. West for the old, north for the now, east for what comes after.")],
      cond: [{ holding: "zero", lines: [ln("Ilse Marrow", "That zero... my own chisel marks. You carried it all the way down here."), ln("Ilse Marrow", "Set it in the niche. The chamber has been waiting longer than I have.")] },
        { flag: "choice.L05.records", lines: [ln("Ilse Marrow", "You saved the drowned register. Odile will want it - she always kept the record honest.")] },
        { flag: "choice.L05.keepsake", lines: [ln("Ilse Marrow", "You kept the child's music box and let the register drown."), ln("Ilse Marrow", "A kindness to one person, at the price of the truth. Remember that, at the consoles.")] }],
      changedFlag: F.seq, changedRoute: [[-5.5, -1, 0.1], [-4.2, -4, 9999, "idle"]],
      changedTalk: [ln("Ilse Marrow", "The sequence is whole. I can let go of the rings now."), ln("Ilse Marrow", "Whatever you choose next - choose it as yourself, not as the one who came before.")] });
    L.npc({ name: "Arin (other branch)", at: [14, -4], coat: "navy", holo: "cyan", task: "An echo of Arin from another branch, carrying the records of every cycle from the FUTURE wing to the archive console.",
      route: [[14, -4, 2.5, "carry"], [8.6, -11.3, 4, "work"], [12.5, 7, 3, "look"]], approach: ["Don't look at me like that. You'll have this face too, eventually.", "You're early. Or I'm late."],
      talk: [ln("Arin (other branch)", "In my branch the bell rang twice and nothing came back. One collapsed city, all three states crushed into it."), ln("Arin (other branch)", "The Founders were right about the larger collapse. They were only wrong about how to stop it."), ln("Arin (other branch)", "So I restarted the night. One bad night, repeating, instead of no nights at all."), ln("Arin (other branch)", "The signal from the tower's ruins? That's me, holding the door shut from the far side. It's in my handwriting because it's my hand."), ln("Arin (other branch)", "And you're the next of us. The fail-safe. Sent in to see whether Veyra is ready to stop repeating.")],
      cond: [{ flag: "clue.L08.branchLog", lines: [ln("Arin (other branch)", "You read my log. So you know the collapse doesn't end at Veyra."), ln("Arin (other branch)", "I did this. I'm sorry. And I'm not.")] },
        { flag: "clue.L08.halden", lines: [ln("Arin (other branch)", "Halden's recording. He chose silence to stop a panic."), ln("Arin (other branch)", "I chose repetition to stop an ending. Neither of us asked the city. You still can.")] }],
      changedFlag: F.made, changedRoute: [[9, 9, 0.1], [0, 15, 9999, "idle"]],
      changedTalk: [ln("Arin (other branch)", "You chose. I never could - I just kept the loop running and called it waiting."), ln("Arin (other branch)", "Go read what I signed. Then go see what I was protecting.")] });
    L.npc({ name: "The Witness", at: [-2.6, -5.2], coat: "white", holo: "magenta", task: "The Witness. Tracing the inscription around the niche, glyph by glyph, so the order is never lost between cycles.",
      route: [[-2.6, -5.2, 4, "work"], [4.8, 2.4, 4, "work"], [-4.8, 2.4, 3, "look"]], approach: ["You hear me at last, close up.", "I have watched you arrive here before."],
      talk: [ln("The Witness", "I am what remains when a cycle forgets itself. I watch, so that something does."), ln("The Witness", "Once I had a name. It was on a list nine days early, and in the archive twice: alive, and dead."), ln("The Witness", "Maren Oskel. The girl in the Drowned District. The water was at my chin when you reached me."), ln("The Witness", "I grew up outside of time, in the seam between the states. I wrote you the letter dated tomorrow - copied from your own hand."), ln("The Witness", "The niche first. Then the bells. Then a choice nobody can make for you. Not even me.")],
      cond: [{ flag: "clue.L08.inscription", lines: [ln("The Witness", "You read it. Ring them as the first cycle rang."), ln("The Witness", "A bell lives in only one state. Shift to where it hangs - the order survives the shift.")] },
        { flag: "choice.L05.keepsake", lines: [ln("The Witness", "You saved my music box, that night. I still have it. It plays three notes and skips the fourth."), ln("The Witness", "Whatever you choose, I'll play it for you at the end.")] },
        { flag: "choice.L05.records", lines: [ln("The Witness", "You saved the register instead of my music box. I was angry for a very long time."), ln("The Witness", "Then I read it, and found my family's name. You gave me back the proof that I existed.")] }],
      changedFlag: F.sign, changedRoute: [[-1.5, 16, 0.1], [0, 18.5, 9999, "look"]],
      changedTalk: [ln("The Witness", "Now you know whose hand restarted the world."), ln("The Witness", "The north wall is open. Go and see what it was holding back.")] });
    L.crowd(S, -12, -11, 6, 5, 4, { holo: "cyan" });
    L.crowd(S, 12.5, 10, 5, 5, 4, { holo: "amber" });

    // ---------------- Phase 3: the Tribunal ----------------
    const tribunal = [];
    const echo = (name, x, z, holo, coat, task, talk, cond) => {
      const n = L.npc({ name, at: [x, z], yaw: face(x, z, 0, 0), coat, holo, task, route: [[x, z, 4, "work"], [x * 1.2, z + 1.6, 3, "look"], [x * 0.85, z + 2, 3, "idle"]], approach: ["Stand where I stood.", "The Tribunal sits."], talk, cond,
        changedFlag: F.trib, changedRoute: [[x * 1.6, z * 1.6 + 2, 9999, "idle"]], changedTalk: [ln(name.replace("Echo of ", ""), "I have said my piece. The Engine listens to you, not to me.")] });
      n.mask = 0; n.it.enabled = false; tribunal.push(n);
      return n;
    };
    echo("Echo of Founder Aurel Halden", -5.6, 3.6, "amber", "white", "An echo of the Founder, writing the evacuation list again and again in a ledger of light.",
      [ln("Aurel Halden", "I ordered the evacuation, and I ordered the silence. Both were for the same children."), ln("Aurel Halden", "Fourteen of them, nine days early. You were the eighth name on the list."), ln("Aurel Halden", "The rescue failed in your branch. In this one, the loop caught it. Nobody here has died for good since."), ln("Aurel Halden", "That is what Stability means. Not a lie - a mercy with a bad memory."), ln("Aurel Halden", "Give them the memory back, a little each night, and keep the mercy. That is my case.")],
      [{ flag: "L07.haldenYielded", lines: [ln("Aurel Halden", "On the catastrophe night you made me open my gate. I was glad. I never told Ondine.")] }, { flag: "evidence.list", lines: [ln("Aurel Halden", "You carry my list. 'VALE, A., age 8. Guardian: I. Marrow.' I remember the ink was cold.")] }]);
    echo("Echo of Mother Ondine Halden", -6.6, -1, "magenta", "grey", "An echo of the Keepers' mother, trimming a sight-lantern that never runs out of oil.",
      [ln("Ondine Halden", "Inside the cycle, nobody dies for good. Not one child on that list. Not one person in the square."), ln("Ondine Halden", "Break it, and tomorrow comes. Tomorrow has no room for the loop-born."), ln("Ondine Halden", "Lise Ashgrove was born inside the repeat. At dawn she simply... stops."), ln("Ondine Halden", "The Keepers lock doors and burn records because the truth is the only thing that can end us."), ln("Ondine Halden", "Choose Stability, and the Keepers will answer to you. Every lantern. Every hour.")],
      [{ flag: "L07.ondineShaken", lines: [ln("Ondine Halden", "You told me my father admitted it. I still choose the loop. But I choose it knowing, now.")] }, { flag: "L07.ondineDefied", lines: [ln("Ondine Halden", "You told me nobody lives for good either. Every one of those nights was the same night. Perhaps you had a point.")] }]);
    echo("Echo of Odile Fenwick", 5.6, 3.6, "cyan", "grey", "An echo of the head archivist, copying the struck names from the evacuation list into a clean book.",
      [ln("Odile Fenwick", "Forty years of struck names. I have counted them every night: the same number, every night."), ln("Odile Fenwick", "A city that cannot remember cannot heal. It can only repeat."), ln("Odile Fenwick", "Publish the names. Let the states separate. Let the horizon open and let us find out what is out there."), ln("Odile Fenwick", "Yes, the loop-born fade. I have done that counting too. I will not pretend it is small."), ln("Odile Fenwick", "But a mercy nobody consented to is just a cage with kind walls. That is my case.")],
      [{ flag: "choice.L05.records", lines: [ln("Odile Fenwick", "You saved the class register from the water. Every one of those children gets a line in daylight, if you let them.")] }, { flag: "evidence.record", lines: [ln("Odile Fenwick", "Maren's record. Alive and dead, countersigned in your hand. That is what the loop does to the truth.")] }]);
    echo("Echo of Pell Ashgrove", 6.6, -1, "cyan", "navy", "An echo of the courier, still carrying the letter dated tomorrow in his satchel.",
      [ln("Pell Ashgrove", "I want the truth out. I've carried your letter more times than there are days - I want it to arrive."), ln("Pell Ashgrove", "And Lise is the truth too. She's real. She's nine. She only exists because the night repeats."), ln("Pell Ashgrove", "If you break the loop, she fades at dawn. If you don't, we all stay in one night forever."), ln("Pell Ashgrove", "Odile says freedom. Ondine says mercy. I just say Lise."), ln("Pell Ashgrove", "Decide for both of us. I can't. That's my whole case: I can't.")],
      [{ flag: "L07.group2Saved", lines: [ln("Pell Ashgrove", "You got Lise out of the courtyard on the bell night. Whatever you choose, you already saved her once.")] }, { flag: "arin.watch", lines: [ln("Pell Ashgrove", "That watch. You had it the first time I delivered the letter. It's never ticked. Not once.")] }]);
    const hearing = async () => {
      tribunal.forEach(n => { n.mask = 7; n.it.mask = 7; n.it.enabled = true; });
      L.sfx("rule"); R.ripple(0.8);
      await new Promise(res => setTimeout(res, 1500));
      await L.dialogue({ question: "Halden argues for STABILITY. Your answer?", opening: [ln("Aurel Halden", "Before you choose, the Tribunal sits. Hear the Founders first. Hear me.", 4), ln("Aurel Halden", "Stability. Keep the night turning, but soften it. Every soul wakes each morning alive - and remembering a little more.", 5.5), ln("Aurel Halden", "The silence killed no one in my plan. The truth would have.", 3.5)],
        options: [{ text: "Alive is not the same as living.", sets: "L08.tribunal.halden.refuted", lines: [ln("Aurel Halden", "Perhaps. But the dead do neither.", 3)] }, { text: "You kept them alive. That counts.", sets: "L08.tribunal.halden.heard", lines: [ln("Aurel Halden", "Then let it count for something, when you choose.", 3.5)] }], doneFlag: "L08.tribunal.t1" });
      await L.dialogue({ question: "Ondine argues for STABILITY. Your answer?", opening: [ln("Ondine Halden", "In the loop nobody dies for good. Not the children on the list. Not Lise Ashgrove. Not you.", 4.5), ln("Ondine Halden", "Choose Evidence, and the loop-born fade at dawn. Pell's daughter first.", 4)],
        options: [{ text: "Lise deserves a real tomorrow, not a borrowed one.", sets: "L08.tribunal.ondine.refuted", lines: [ln("Ondine Halden", "A real tomorrow she won't be in.", 3)] }, { text: "I won't pretend that cost is small.", sets: "L08.tribunal.ondine.heard", lines: [ln("Ondine Halden", "Good. Then you'll choose with your eyes open.", 3.5)] }], doneFlag: "L08.tribunal.t2" });
      await L.dialogue({ question: "Odile argues for EVIDENCE. Your answer?", opening: [ln("Odile Fenwick", "I keep the record honest. Forty years of struck names, and every one of them still waiting.", 4.5), ln("Odile Fenwick", "Publish them. Let the states separate. Let the horizon open. A city that cannot remember cannot heal.", 5)],
        options: [{ text: "Even if people fade at dawn?", sets: "L08.tribunal.odile.pressed", lines: [ln("Odile Fenwick", "Even then. I'm sorry. I've done the counting, and I'd do it again.", 4)] }, { text: "The names deserve daylight.", sets: "L08.tribunal.odile.heard", lines: [ln("Odile Fenwick", "They do. So do the people who'll have to read them.", 3.5)] }], doneFlag: "L08.tribunal.t3" });
      await L.dialogue({ question: "Pell argues for EVIDENCE - and for Lise. Your answer?", opening: [ln("Pell Ashgrove", "I want the truth out. Lise is the truth too.", 3), ln("Pell Ashgrove", "Break it and she goes. Keep it and we all stay nine years inside one night. Decide for both of us - I can't.", 5.5)],
        options: [{ text: "I'll carry it, Pell. Whatever I choose.", sets: "L08.tribunal.pell.heard", lines: [ln("Pell Ashgrove", "Then I'll carry the letter one last time.", 3)] }, { text: "You can't ask me to choose your daughter.", sets: "L08.tribunal.pell.pressed", lines: [ln("Pell Ashgrove", "I'm not asking you to choose her. I'm asking you to choose.", 3.5)] }], doneFlag: "L08.tribunal.t4" });
      await L.dialogue({ question: "Maren - The Witness - argues for EVIDENCE. Your answer?", opening: [ln("The Witness", "I'm Maren Oskel. You pulled me out of the water once. I stayed in the seam so someone would be here when you came back.", 5.5), ln("The Witness", "Keep the loop, and I stay. Break it, and I can finally leave - into a tomorrow that might not have room for me.", 5), ln("The Witness", "I want to leave, Arin. I've wanted it for forty years. But not if it costs you everything.", 4.5)],
        options: [{ text: "Come with me into tomorrow.", sets: "L08.tribunal.maren.leave", lines: [ln("The Witness", "Then hold the door for me when it opens.", 3)] }, { text: "Stay, and keep watch with me.", sets: "L08.tribunal.maren.stay", lines: [ln("The Witness", "I'd keep watch with you for ever. I nearly have.", 3.5)] }], doneFlag: F.trib });
      L.toast("The Tribunal falls silent. The chamber fills with a lullaby - and the floor tilts into memory.", 4);
    };
    L.when([F.seq], () => { setTimeout(hearing, 2500); });

    // ---------------- Phase 4: the playable memory (the Nursery, sealed below the chamber) ----------------
    const NY = -40, NX = 0, NZ = 0;
    L.floor(S, NX, NZ, 16, 12, NY, "wood");
    L.wall(S, NX - 8, NZ + 6, NX + 8, NZ + 6, 4, "plaster", 0.4, NY); L.wall(S, NX - 8, NZ - 6, NX + 8, NZ - 6, 4, "plaster", 0.4, NY);
    L.wall(S, NX - 8, NZ - 6, NX - 8, NZ + 6, 4, "plaster", 0.4, NY); L.wall(S, NX + 8, NZ - 6, NX + 8, NZ + 6, 4, "plaster", 0.4, NY);
    L.box(S, 16.4, 0.4, 12.4, "plaster", NX, NY + 4.2, NZ);
    for (let i = 0; i < 3; i++) L.box(S, 1.6, 1.2, 0.06, "windowCold", NX - 5 + i * 5, NY + 2.4, NZ + 5.78, { solid: false, shadow: false });
    for (let row = 0; row < 2; row++) for (let i = 0; i < 7; i++) {
      const n = row * 7 + i + 1, x = NX - 6 + i * 2, z = NZ + (row ? 3 : -3);
      L.box(S, 1, 0.6, 1.6, "wood", x, NY + 0.3, z);
      L.box(S, 0.9, 0.05, 1.2, ["teal", "red", "gold"][n % 3], x, NY + 0.62, z, { solid: false });
      if (n === 7 || n === 3 || n === 1 || n === 14) L.label(S, n === 7 ? "7   A." : n === 3 ? "3   M." : String(n), x, NY + 1.0, z + (row ? -0.85 : 0.85), row ? Math.PI : 0, 0.25, "#4a3018");
    }
    L.box(S, 0.8, 0.8, 0.8, "woodDark", NX - 7, NY + 0.4, NZ); L.box(S, 0.4, 0.25, 0.3, "brass", NX - 7, NY + 0.92, NZ, { solid: false });
    L.box(S, 0.9, 0.8, 0.9, "woodDark", NX + 7, NY + 0.4, NZ - 3);
    L.light(S, 0xffb468, 2.6, 12, NX - 6, NY + 2.8, NZ); L.light(S, 0xffc480, 2.2, 8, NX + 6.5, NY + 2.6, NZ - 2.5);
    L.particles(S, "dust", [14, 3, 10], [NX, NY + 0.5, NZ], 200);
    L.npc({ name: "Ilse Marrow (memory)", at: [NX - 4, NZ], y: NY, coat: "olive", task: "The clockkeeper, as Arin remembers her: making the last round of the Nursery cots before lights out.",
      route: [[NX - 4, NZ, 3, "work"], [NX + 1, NZ, 3, "work"], [NX + 5.5, NZ - 1.2, 4, "look"]], approach: ["There you are. Bare feet again.", "Shh. Half of them are asleep."],
      talk: [ln("Ilse Marrow", "Fourteen cots, fourteen of you. Nine days early, and nobody outside is allowed to know you're here."), ln("Ilse Marrow", "Founder Halden says it's safer this way. I say it's quieter. They aren't the same thing."), ln("Ilse Marrow", "You hum all four notes of the song. The others only ever manage three. Don't stop."), ln("Ilse Marrow", "If the bell rings twice, I'll be in the tower. Holding it. That's my job."), ln("Ilse Marrow", "Go and look at your cot. Number seven. I have something for you on my table.")],
      cond: [{ flag: "arin.listedChild", lines: [ln("Ilse Marrow", "VALE, A., age eight. That's what the list says. Guardian: me.")] }, { flag: "maren.recognised", lines: [ln("Ilse Marrow", "Maren's in cot three, humming your song wrong on purpose. She'll wait for you, that one.")] }],
      changedFlag: F.watch, changedRoute: [[NX + 6.2, NZ - 2.2, 9999, "idle"]], changedTalk: [ln("Ilse Marrow", "There. Now you keep time for me."), ln("Ilse Marrow", "Whatever happens to the night, little A, it's yours to keep.")] });
    L.zone({ at: [NX + 6, NY + 1, NZ - 3], size: [2.4, 2.5, 3], flag: F.walked, lines: [ln("Arin (child)", "Cot seven. Somebody scratched an A inside the rail.", 3.5), ln("Ilse Marrow", "Somebody. I pretended not to see.", 3)] });
    L.onHum([NX, NY + 1, NZ], 9, () => L.say([ln("Ilse Marrow", "All four notes. You're the only one who knows all four.", 3.5)], false));
    const watchMesh = L.cyl(S, 0.22, 0.12, "brass", NX + 7, NY + 0.9, NZ - 3);
    let inMemory = false;
    L.interact({ at: [NX + 7, NY + 0.9, NZ - 3], mesh: watchMesh, prompt: "Take the watch from Ilse", requires: [F.walked], locked: "Ilse shakes her head: 'Your cot first, little A. Number seven.'", once: true, onUse: async () => {
      watchMesh.visible = false; L.set(F.watch); AZ.sfx("memory");
      await AZ.ui.sayAndWait([ln("Ilse Marrow", "Hold out your hand. Brass, a bit dented - it was my father's.", 3.5), ln("Ilse Marrow", "I've had it engraved. 'For A. - keep time for me. I.M.'", 3.5), ln("Arin (child)", "It doesn't tick.", 2), ln("Ilse Marrow", "It will, when it's needed.", 2.5)]);
      await AZ.ui.fade(1, 900);
      AZ.player.pos.set(0, 0, -7.5); AZ.player.vy = 0; AZ.player.yaw = Math.PI;
      AZ.sim.input.crouch = false; R.memory = 0; inMemory = false;
      AZ.game.world().checkpoint = { pos: new THREE.Vector3(0, 0, -7.5), yaw: Math.PI, state: AZ.game.state() };
      await AZ.ui.fade(0, 900);
      L.set(F.memory);
    } });
    L.when([F.trib], async () => {
      await new Promise(res => setTimeout(res, 1800));
      await AZ.ui.fade(1, 900);
      AZ.player.pos.set(NX - 7, NY, NZ - 1.8); AZ.player.vy = 0; AZ.player.yaw = -Math.PI / 2;
      AZ.game.world().checkpoint = { pos: new THREE.Vector3(NX - 7, NY, NZ - 1.8), yaw: -Math.PI / 2, state: AZ.game.state() };
      inMemory = true; R.memory = 1; AZ.sim.input.crouch = true; AZ.sfx("memory");
      await AZ.ui.fade(0, 900);
      L.say([ln("Arin (child)", "The Nursery. I'm small. The cots are taller than me.", 3.5), ln("Ilse Marrow", "Come on, little A. One more round of the cots before the lights go out.", 4)], false);
    });
    L.update(() => { if (inMemory) { AZ.sim.input.crouch = true; R.memory = 1; } });
    L.ambient("nursery", 0.3, 7);

    // ---------------- the other-branch Arin, the watch, the choice ----------------
    const ghost = L.person(S, 0, 2.2, R.MAT.holoA, Math.PI, 1); ghost.group.visible = false;
    L.when([F.memory], async () => {
      ghost.group.visible = true;
      await L.memory([
        ln("Arin (other branch)", "In my branch the bell rang twice and nothing came back. All three states fell into one. I was the only child they pulled out.", 5.5),
        ln("Arin (other branch)", "I grew up in the wreck. I found the Zero Engine under the bell. It couldn't undo the fall - but it could start the night again.", 5.5),
        ln("Arin (other branch)", "One bad night, repeating, instead of the larger collapse. I signed the order, and I sent a younger me back in to check on it.", 5.5),
        ln("Arin (other branch)", "I restarted the night. You are the version of me that gets to choose.", 5),
        ln("Arin", "...My watch. It's ticking.", 3),
      ], F.other);
      ghost.group.visible = false;
      L.toast("Your watch - 'For A. - keep time for me. I.M.' - has started ticking.", 4.5); AZ.vibrate(60); L.set("arin.watchTicking");
    });
    let tickT = 0;
    L.update(dt => { if (!L.get(F.other)) return; tickT += dt; if (tickT >= 1) { tickT = 0; AZ.sfx("watch", 0.35); } });

    const consoles = {};
    const consoleAt = (key, x, mat, holo, title, sub) => {
      const body = L.box(S, 1.4, 1.1, 0.8, "obsidian", x, 0.55, -6, { dynamic: true });
      const top = L.box(S, 1.4, 0.04, 0.8, mat, x, 1.12, -6, { solid: false, dynamic: true });
      const orb = L.holoMesh(S, "sphere", holo, x, 2.2, -6, 0.9, 0.9, 0.9, { spin: 0.6, bob: 0.08 });
      const t1 = L.label(S, title, x, 3.1, -6, Math.PI, 0.5, "#ffffff");
      const t2 = L.label(S, sub, x, 2.75, -6, Math.PI, 0.2, "#d8e8ff");
      const veil = L.holoMesh(S, "box", "magenta", x, 1.3, -6, 1.8, 2.6, 1.2);
      consoles[key] = { parts: [body, top, orb, t1, t2], veil };
      return body;
    };
    consoleAt("stab", -6.5, "neonC", "cyan", "STABILITY  [=]", "The loop goes on, gentler: everyone remembers a little more each night.\nCost: you stay as its keeper; Maren stays in the seam.");
    consoleAt("evid", 6.5, "neonA", "amber", "EVIDENCE  [?]", "Publish the names. The states separate; the horizon opens.\nCost: the loop-born, like Lise, fade at dawn.");
    L.when([F.other], () => { consoles.stab.veil.visible = false; consoles.evid.veil.visible = false; });
    if (L.get("choice.L05.records")) { for (let i = 0; i < 5; i++) L.holoMesh(S, "box", "amber", 5.7 + i * 0.4, 3.8 + (i % 2) * 0.3, -5.4, 0.3, 0.4, 0.02, { bob: 0.05 }); L.label(S, "THE DROWNED REGISTER YOU SAVED", 6.5, 4.6, -5.4, Math.PI, 0.2, "#ffd890"); }
    if (L.get("choice.L05.keepsake")) { L.holoMesh(S, "box", "cyan", -6.5, 3.9, -5.4, 0.4, 0.3, 0.3, { spin: 0.8, bob: 0.05 }); L.label(S, "THE MUSIC BOX YOU KEPT", -6.5, 4.6, -5.4, Math.PI, 0.2, "#bff0ff"); }
    const choose = async (key) => {
      if (L.get(F.made)) return;
      const stab = key === "stab";
      const pick = await L.choice(stab
        ? "Choose STABILITY? No one in Veyra will ever die for good, and each night they will remember a little more. But you will never leave: you become the city's keeper of time, as Ilse was, and Maren stays in the seam with you. The other console goes dark forever."
        : "Choose EVIDENCE? The names are published, the Founders' secret is out, the states separate and tomorrow finally comes. But the loop-born - Lise Ashgrove among them - fade at dawn. The other console goes dark forever.",
        [{ text: stab ? "Choose STABILITY" : "Choose EVIDENCE" }, { text: "Not yet" }]);
      if (pick !== 0) return;
      L.set(stab ? F.stab : F.evid); L.set(F.made); L.sfx("rule"); R.ripple(1.2);
      consoles[stab ? "evid" : "stab"].parts.forEach(m => { m.visible = false; if (m.userData.collider) m.userData.collider.on = false; });
      if (stab) {
        for (let i = 0; i < 1; i++) L.holoMesh(S, "torus", "cyan", 0, 0.5, 0, 30, 30, 1, { spin: 0.07 }).rotation.x = Math.PI / 2;
        L.toast("The rings settle into one gentle orbit. The cycle holds - and loosens.", 4);
        await AZ.ui.sayAndWait([ln("Arin", "Stability. Let the night keep turning - but loosen its grip."), ln("Arin", "Let them wake up each morning remembering one more thing. The cup. A name. A face at the tram stairs."), ln("The Witness", "Then the festival comes again, and someone will say 'welcome back' - and mean it.", 4.5), ln("Arin", "And I stay. Someone has to keep time. Ilse did.", 3.5), ln("The Witness", "Then I'll stay too. The seam has room for two.", 3.5)]);
      } else {
        for (let i = 0; i < 8; i++) { const [x, z] = pol(11, i * 45 + 22.5); L.holoMesh(S, "box", "amber", x, 3, z, 0.7, 6, 0.7, { spin: 0.3, bob: 0.2 }); }
        L.toast("Columns of record rise around the chamber. Far above, the states begin to drift apart.", 4);
        await AZ.ui.sayAndWait([ln("Arin", "Keep it. Every cycle, every list, every struck name - on record."), ln("Arin", "Let the cycle crack. If the states have to come apart for the truth to come out, let them."), ln("The Witness", "Then the horizon opens, and the world out there stops waiting behind a wall.", 4.5), ln("Arin", "Lise. Pell. I'm sorry. Dawn is coming anyway.", 3.5)]);
      }
    };
    L.interact({ at: [-6.5, 1.1, -6], mesh: consoles.stab.parts[0], prompt: "Choose STABILITY", requires: [F.other], locked: "The console is dark. The Tribunal and the memory come first.", onUse: () => choose("stab") });
    L.interact({ at: [6.5, 1.1, -6], mesh: consoles.evid.parts[0], prompt: "Choose EVIDENCE", requires: [F.other], locked: "The console is dark. The Tribunal and the memory come first.", onUse: () => choose("evid") });

    // ---------------- the intervention order (payoff) ----------------
    const doc = L.box(S, 0.6, 0.8, 0.06, "paper", 0, 2.1, -0.6, { solid: false, dynamic: true }); doc.visible = false;
    const docIt = L.clue({ group: S, at: [0, 2.1, -0.6], mesh: doc, prompt: "Read the intervention order", flag: F.sign, id: "L08.order", title: "Intervention Order (0)", requires: [F.made], locked: "Nothing to read yet.",
      text: "BY THE AUTHORITY OF THE ZERO CHAMBER:\nRestart the Veyra cycle at the festival morning. Let the bell ring once, then twice. Repeat until a branch is found in which the larger collapse does not follow.\n\nResponsibility for every repetition is mine.\n\n                        - Arin\n        (branch unrecorded. the handwriting is yours.)" });
    docIt.enabled = false;
    L.when([F.made], () => { doc.visible = true; docIt.enabled = true; L.light(S, 0xfff0d8, 3, 5, 0, 2.8, -1.2); L.sfx("reveal"); L.toast("A document rises from the niche.", 3); });

    // ---------------- archive alcove of every cycle's letter (optional side area) ----------------
    L.wall(S, -15, -41, -15, -31, 5, "obsidian", 0.5); L.wall(S, -15, -31, -5, -31, 5, "obsidian", 0.5); L.wall(S, -15, -41, -5, -41, 5, "obsidian", 0.5);
    L.box(S, 10.4, 0.4, 10.4, "obsidian", -10, 5.2, -36);
    L.floor(S, -10, -36, 10, 10, 0, "woodDark");
    L.label(S, "ARCHIVE OF CYCLES", -4.65, 3.5, -36, -Math.PI / 2, 0.35, "#ffd8a0");
    const shelves = [[-14.5, -39.3, Math.PI / 2], [-14.5, -37.1, Math.PI / 2], [-14.5, -34.9, Math.PI / 2], [-14.5, -32.7, Math.PI / 2], [-12.6, -31.5, Math.PI], [-10.2, -31.5, Math.PI], [-7.8, -31.5, Math.PI], [-12.6, -40.5, 0], [-10.2, -40.5, 0], [-7.8, -40.5, 0]];
    shelves.forEach(([x, z, ry], i) => {
      const fx = Math.sin(ry), fz = Math.cos(ry);
      L.box(S, 2, 3.2, 0.5, "woodDark", x, 1.6, z, { ry });
      for (let k = 0; k < 4; k++) L.box(S, 1.8, 0.4, 0.06, "paper", x + fx * 0.27, 0.45 + k * 0.75, z + fz * 0.27, { solid: false, ry });
      L.label(S, "CYCLES " + String(i * 40 + 1).padStart(4, "0") + "-" + String(i * 40 + 40).padStart(4, "0"), x + fx * 0.3, 3.4, z + fz * 0.3, ry, 0.2, "#ffe6b0");
    });
    L.box(S, 2.2, 0.9, 1.1, "wood", -10, 0.45, -36);
    L.light(S, 0xffc480, 2.2, 8, -10.5, 2.2, -36);
    L.clue({ group: S, at: [-9.9, 0.94, -36.2], size: [0.45, 0.05, 0.34], prompt: "Read the unsent letter, cycle 0413", flag: "L08.letterRead", id: "L08.letter", title: "Letter, Cycle 0413 (unsent)",
      text: "To Arin. Dated tomorrow.\n\nYou will arrive on the last train. You will not remember me. Take the blue cup before it falls. Follow the zero.\n\nIn the margins, a page of practice zeros - dozens of them, each one closer to a single stroke with a small hook where it closes. Your stroke.\n\n- M." });
    L.box(S, 0.9, 0.8, 0.9, "woodDark", -13.6, 0.4, -36);
    const phono = L.cyl(S, 0.3, 0.12, "brass", -13.6, 0.86, -36);
    L.clue({ group: S, at: [-13.6, 0.95, -36], mesh: phono, prompt: "Play Founder Halden's recording", flag: "L08.haldenHeard", id: "L08.halden", title: "Recording: Founder Aurel Halden",
      text: "Halden, for the Founders: the three states will fall into one. The children move first, nine days early, names struck from the record. The bell rings once as the warning; twice means failure. 'I chose silence. If you are hearing this, silence was not enough.'",
      lines: [ln("Founder Aurel Halden", "This is Aurel Halden, for the Founders - for whoever finds the Zero Chamber after us.", 4.5), ln("Founder Aurel Halden", "The three states are grinding together. Our numbers say they will fall into one. Every era. Every person.", 5),
        ln("Founder Aurel Halden", "We move the children first, nine days early, to the pocket between the states. Their names come off the record, so the city does not panic.", 5.5), ln("Founder Aurel Halden", "On the night the bell rings once and the square empties. If it rings twice... we have failed.", 4.5),
        ln("Founder Aurel Halden", "I chose silence. If you are hearing this, silence was not enough.", 4), ln("Founder Aurel Halden", "Forgive us, or don't. But finish it.", 3.5)] });
    L.label(S, "A. HALDEN", -13.6, 1.9, -36, -Math.PI / 2, 0.22, "#ffe6b0");

    // ---------------- corridor, balcony, the horizon ----------------
    const z0 = zb + 0.3, z1 = 56;
    L.floor(S, 0, (z0 + z1) / 2 + 1, 6, z1 - z0 + 2, 0, "obsidian");
    L.wall(S, -2.4, z0, -2.4, z1, 5, "iron", 0.5); L.wall(S, 2.4, z0, 2.4, z1, 5, "iron", 0.5);
    L.box(S, 5.4, 0.4, z1 - z0, "obsidian", 0, 5.2, (z0 + z1) / 2);
    for (let i = 0; i < 8; i++) L.box(S, 4.6, 0.1, 0.12, i % 2 ? "neonV" : "neonC", 0, 4.9, z0 + 2 + i * 3.2, { solid: false, shadow: false });
    L.light(S, 0x80d8ff, 2.2, 12, 0, 4, 36); L.light(S, 0xa080ff, 2.2, 12, 0, 4, 48);
    L.checkpoint(0, 0, 32, Math.PI, { size: [4.6, 2.5, 2] });
    L.floor(S, 0, 59.5, 16, 7, 0, "stone");
    L.wall(S, -8, 63, 8, 63, 1.1, "brass", 0.25); L.wall(S, -8, 56, -8, 63, 1.1, "brass", 0.25); L.wall(S, 8, 56, 8, 63, 1.1, "brass", 0.25);
    L.block(0, 63.3, 16.5, 0.4, 0, 4); L.block(-8.3, 59.5, 0.4, 7.5, 0, 4); L.block(8.3, 59.5, 0.4, 7.5, 0, 4);
    L.wall(S, -8, 56, -2.4, 56, 9, "ruin", 0.8); L.wall(S, 2.4, 56, 8, 56, 9, "ruin", 0.8); L.wall(S, -2.4, 56, 2.4, 56, 4, "ruin", 0.8, 5);
    L.zone({ at: [0, 1, 57.5], size: [15, 2.5, 3], flag: F.horizon, lines: [ln("Arin", "So that's what the loop was holding shut."), ln("Arin", "Islands falling upward. A waterfall that forgot how to fall. A sun the wrong colour."), ln("The Witness", "The first cycle is over, Arin. The others are waiting out there.", 4.5)] });
    L.exit({ group: S, at: [0, 0, 61.6], size: [14, 2.5, 1.6], requires: [F.sign], locked: "The horizon can wait until you've read the order." });
    // horizon: inverted islands, a frozen waterfall, a green sun, upward rain, a zero on the horizon
    L.box(S, 900, 1, 600, "obsidian", 0, -45, 250, { solid: false, noEcho: true });
    [[-60, 28, 150, 22], [45, 44, 185, 30], [-15, 62, 240, 26], [95, 22, 130, 16], [-120, 40, 215, 20]].forEach(([x, y, z, s], i) => {
      const isle = new THREE.Group(); isle.position.set(x, y, z); S.add(isle); isle.userData.dynamic = true; isle.userData.noEcho = true;
      const rock = L.mesh(isle, "cone", "ruin", 0, s * 0.25, 0, s, s * 0.7, s); rock.userData.dynamic = true;
      L.mesh(isle, "cyl", "olive", 0, -s * 0.08, 0, s * 0.95, s * 0.1, s * 0.95);
      for (let k = 0; k < 4; k++) { const a = k * 1.7 + i, rr = s * 0.25; L.mesh(isle, "cyl8", "woodDark", Math.cos(a) * rr, -s * 0.2, Math.sin(a) * rr, 0.6, s * 0.2, 0.6); L.mesh(isle, "sphere", "olive", Math.cos(a) * rr, -s * 0.32, Math.sin(a) * rr, s * 0.16, s * 0.16, s * 0.16); }
      L.holoMesh(isle, "box", "cyan", s * 0.42, s * 0.6, 0, s * 0.08, s * 1.2, s * 0.08);
      L.update((dt, t) => { isle.position.y = y + Math.sin(t * 0.2 + i) * 1.5; isle.rotation.y += dt * 0.02 * (i % 2 ? 1 : -1); });
    });
    L.box(S, 45, 130, 30, "ruin", -95, 5, 265, { solid: false, noEcho: true });
    L.holoMesh(S, "box", "cyan", -95, 18, 248, 11, 100, 1.2);
    for (let i = 0; i < 14; i++) L.holoMesh(S, "sphere", "cyan", -95 + ((i * 37) % 26) - 13, -30 + (i * 13) % 22, 244 - (i * 7) % 12, 1 + (i % 3), 1 + (i % 3), 1 + (i % 3));
    L.light(S, 0x80e8ff, 5, 90, -95, 20, 236);
    L.mesh(S, "sphere", "green", 165, 70, 340, 52, 52, 52).userData.noEcho = true;
    L.light(S, 0x70ff98, 7, 260, 150, 45, 300);
    for (let i = 0; i < 7; i++) L.mesh(S, "octa", "crystal", 120 + i * 11, -10, 250 + (i * 23) % 70, 8, 40 + (i * 9) % 30, 8).userData.noEcho = true;
    L.particles(S, "motes", [40, 40, 40], [55, -20, 120], 400);
    const hz = L.holoMesh(S, "torus", "cyan", 0, 90, 420, 220, 220, 6, { spin: 0.05 }); hz.rotation.x = 0;
    L.ambient("wind", 0.4, 7);

    // ---------------- memory fragments ----------------
    L.fragment("L08.1", [-14, 1.3, -40.2], "Letter 0001", "The first letter in the archive, in a child's careful capitals: 'TO WHOEVER COMES NEXT. THE BELL RANG TWICE. I AM STILL HERE. I WILL WRITE AGAIN TOMORROW. - MAREN, AGE 9.'", S);
    L.fragment("L08.2", [-27.6, 1.3, -4.2], "The Clapper Block", "PAST. Ilse Marrow's workbook: 'Cut the old clapper block into a zero this morning. Halden says the engine needs a key nobody would think to steal. Who steals a toy?'", S);
    L.fragment("L08.3", [27.6, 1.3, -4.2], "Branch Weather", "FUTURE. A survey slate from beyond the wall: 'Region 4: rain falls upward on Tuesdays. Region 7: the waterfall stopped mid-air in cycle 12 and has not moved since. Region 9: green sun, no shadows. Recommend: do not open the wall yet.'", S);
  },
});
