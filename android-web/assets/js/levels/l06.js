/* LEVEL 06 — The Nursery With No Address (a spatial seam between states).
   Port of the Unity Level06Builder (Design v2, the hardest puzzle). Echoes are always visible here.
   Learn: a bridge built from one piece per state. Combine: hanging shapes whose marks are painted in
   OTHER states (the solution is only visible as an echo). Climax (interlocked): three music boxes behind
   the furniture, one per state, each missing a different note - wind each, then HUM the whole song beside
   it - while the drifting door (the way out) slips away whenever you stop watching it; each time it
   slips, the room forgets the boxes you mended. Reveal: The Witness is Maren, grown. Take Ilse's zero. */
AZ.registerLevel({
  id: "L06", order: 6, chapter: "LEVEL 06", title: "The Nursery With No Address", location: "Spatial seam between states",
  start: 1, allowed: 7, echoes: true, killY: -20,
  build(L) {
    const { line } = L, S = L.shared, PA = L.past, PR = L.present, FU = L.future, W = L.W, R = AZ.render, P = AZ.player;
    const PI = Math.PI, THREE = L.THREE;
    const F = { reached: "L06.nurseryReached", shapes: "L06.shapesAligned", lullaby: "L06.lullabyComplete", anchored: "L06.doorAnchored",
      witness: "L06.witnessHeard", zero: "L06.zeroTaken", isWitness: "maren.isWitness" };
    const TW = "#ffe6b0", TC = "#cfe8ff", TH = "#ff9ae8";
    const STATES = [["Past", PA, 1, TW, "<<"], ["Present", PR, 2, TC, "[ ]"], ["Future", FU, 4, TH, ">>"]];

    L.look({ fog: 0.55,
      past: { sky: 0x3a2030, fog: 0x4a2a34, density: 0.012, sunI: 0.9, hemiS: 0xffc8a0, bloom: 0.9 },
      present: { sky: 0x101c2c, fog: 0x18283a, density: 0.014, bloom: 1.0 },
      future: { sky: 0x0a0418, fog: 0x1a0a30, density: 0.012, bloom: 1.5 } });
    L.intro(line("Arin", "This isn't the Past, or the Present, or the Future. It's all three at once."),
      line("Arin", "A bridge in pieces... one piece in each state."),
      line("The Witness", "Look closely, Arin. Nothing here belongs to one time alone."));

    L.objective("Cross the seam into the nursery", F.reached, "The plank exists in the PAST, the grate in the PRESENT, the light-bridge in the FUTURE. Shift only while standing on the stone steps.");
    L.objective("Turn the hanging shapes to face their marks", F.shapes, "Each shape hangs in only one state. Its mark - a floor arrow beneath it - is painted in another state, so look at the ECHO under the shape and turn the shape's arrow to match.");
    L.objective("Mend the three music boxes with your hum", F.lullaby, "The boxes sit on the north ledges behind the furniture, one per state. Slide the furniture aside, wind each box in its own state, then HUM beside it. Keep the drifting door in view - if it slips away, the room forgets.");
    L.objective("Reach the drifting door - keep it in view", F.anchored, "The door drifts between the west, east and south ledges while nobody watches it. Walk to it with it on screen.");
    L.objective("Hear The Witness", F.witness, "Cross the seam bridge to the figure of light.");
    L.objective("Take Ilse's carved zero", F.zero, "It rests on the black plinth on the Witness platform.");
    L.objective("Return to the tower before the final bell", "L06.returnedToTower", "Follow the bridge north toward the clock tower.");
    L.payoff(F.witness, "An Attempted Rescue", "The Bell Catastrophe was not an attack, an accident or a punishment. The Founders rang the bell to carry Veyra's children out of time - fourteen of them - into the nursery with no address. The first ring opened the way. The second ring was never part of the plan. And one child stayed behind to wait for you: Maren Oskel.");

    // ================= arrival + the state-segment bridge =================
    L.floor(S, 0, -20.5, 10, 7, 0, "stone", 1.2);
    L.box(S, 7, 2.4, 5, "ruin", 0, -2.4, -20.5, { solid: false });
    L.wall(S, -5, -24, -5, -17, 1.1, "woodDark", 0.25); L.wall(S, 5, -24, 5, -17, 1.1, "woodDark", 0.25);
    L.wall(S, -5, -17.1, -1.3, -17.1, 1.1, "woodDark", 0.25); L.wall(S, 1.3, -17.1, 5, -17.1, 1.1, "woodDark", 0.25);
    L.box(S, 0.25, 2.9, 0.3, "woodDark", -0.95, 1.45, -23.8); L.box(S, 0.25, 2.9, 0.3, "woodDark", 0.95, 1.45, -23.8); L.box(S, 2.15, 0.3, 0.3, "woodDark", 0, 3, -23.8);
    L.holoMesh(S, "box", "magenta", 0, 1.4, -23.8, 1.6, 2.6, 0.04);
    L.label(S, "THE DROWNED DISTRICT", 0, 3.5, -23.6, 0, 0.2, TH);
    L.floor(PA, 0, -14.5, 2, 5, 0, "wood", 0.25);
    for (let i = 0; i < 4; i++) L.box(PA, 2.05, 0.05, 0.12, "woodDark", 0, 0.02, -16.5 + i * 1.3, { solid: false });
    L.light(PA, 0xffb469, 1.6, 6, 1.2, 1.2, -14.5);
    L.floor(S, 0, -11.25, 2.6, 1.5, 0, "stone", 0.8);
    L.floor(PR, 0, -8.25, 2, 4.5, 0, "iron", 0.2);
    L.box(PR, 0.06, 1, 4.5, "rust", -1.05, 0.5, -8.25); L.box(PR, 0.06, 1, 4.5, "rust", 1.05, 0.5, -8.25);
    L.light(PR, 0xff9a48, 1.6, 6, -1.2, 1.6, -8.25);
    L.floor(S, 0, -5.25, 2.6, 1.5, 0, "stone", 0.8);
    L.floor(FU, 0, -2.25, 2, 4.5, 0, "glass", 0.12);
    L.holoMesh(FU, "box", "cyan", 0, 0.01, -2.25, 2, 0.04, 4.5);
    L.light(FU, 0x40d8ff, 1.8, 6, 0, 0.6, -2.25);
    L.label(S, "<< PAST", 0, 1.9, -16.9, PI, 0.3, TW);
    L.label(S, "[ PRESENT ]", 0, 1.9, -10.6, PI, 0.3, TC);
    L.label(S, "FUTURE >>", 0, 1.9, -4.6, PI, 0.3, TH);
    L.label(S, "shift only while standing on stone", 0, 2.6, -17.2, PI, 0.16, "#ffffff");
    L.zone({ at: [0, 1, -18.2], size: [4, 2, 1.6], toast: "Each piece of the bridge exists in only one state. Shift while standing on stone.",
      lines: [line("Arin", "The plank is only there in the PAST. Then the grate, then the light...")] });
    L.zone({ at: [0, 1, 1.6], size: [5, 2, 2.4], flag: F.reached,
      lines: [line("Arin", "A nursery. A cot from the Past, wallpaper from the Present, a mobile made of light."), line("Arin", "None of it should be in the same room.")] });

    // ================= the nursery shell =================
    L.floor(S, 0, 8, 16, 16, 0, "wood", 0.5);
    L.mesh(S, "sphere", "ruin", 0, -4.5, 8, 17, 9, 17).userData.noEcho = true;
    L.box(S, 6.5, 0.02, 6, "teal", 0, 0.012, 6.5, { solid: false });
    const rail = (x1, z1, x2, z2) => L.wall(S, x1, z1, x2, z2, 1.1, "woodDark", 0.25);
    rail(-8, 0, -8, 9.1); rail(-8, 10.9, -8, 16);
    rail(8, 0, 8, 2); rail(8, 4, 8, 9.1); rail(8, 10.9, 8, 16);
    rail(-8, 0, -5.9, 0); rail(-4.1, 0, -1.2, 0); rail(1.2, 0, 8, 0);
    const seg = [-8, -5.9, -4.1, -0.9, 0.9, 4.1, 5.9, 8];
    for (let i = 0; i < 4; i++) rail(seg[i * 2], 16, seg[i * 2 + 1], 16);
    [[-8, 0], [8, 0], [-8, 16], [8, 16]].forEach(c => L.box(S, 0.35, 4, 0.35, "woodDark", c[0], 2, c[1]));
    L.box(S, 0.3, 0.3, 16.4, "woodDark", 0, 4.1, 8, { solid: false }); L.box(S, 16.4, 0.3, 0.3, "woodDark", 0, 4.1, 8, { solid: false });
    for (const x of [-5, 0, 5]) L.floor(S, x, 16.85, 1.8, 1.7, 0, "woodDark", 0.4);
    L.floor(S, -8.95, 10, 1.9, 1.8, 0, "woodDark", 0.4); L.floor(S, 8.95, 10, 1.9, 1.8, 0, "woodDark", 0.4); L.floor(S, -5, -0.85, 1.8, 1.7, 0, "woodDark", 0.4);
    L.light(S, 0xffd8c0, 2.2, 14, 0, 3.6, 7);
    // PAST wall (west): panelling, warm window, the cots
    L.box(PA, 0.2, 4, 9.1, "wood", -8.3, 2, 4.55); L.box(PA, 0.2, 4, 5.1, "wood", -8.3, 2, 13.45); L.box(PA, 0.2, 1, 1.8, "wood", -8.3, 3.5, 10);
    L.box(PA, 0.05, 1.4, 1.8, "window", -8.18, 2.4, 13.5, { solid: false });
    L.light(PA, 0xffb469, 2.2, 7, -7.4, 2.4, 13.5);
    L.box(PA, 1.6, 0.5, 0.8, "wood", -6, 0.45, 3.2);
    L.clue({ group: PA, at: [-6, 1.15, 2.78], size: [0.6, 0.1, 0.06], mat: "woodDark", prompt: "Read the carving on the nurse's cot", flag: "L06.notePast", id: "L06.notePast", title: "Carved into the cot rail",
      text: "Small careful letters, cut with a pen-knife:\n\n'the SECOND note is 3'" });
    L.box(PA, 0.7, 1, 0.7, "wood", -6.4, 0.5, 12.6);
    for (let i = 0; i < 7; i++) {
      const x = 1.75 + i * 0.92;
      L.box(PA, 0.8, 0.5, 1.3, "wood", x, 0.35, 1.05); L.box(PA, 0.8, 0.3, 0.04, "woodDark", x, 0.75, 1.7, { solid: false });
      L.label(PA, String(i + 1), x, 1.05, 1.75, PI, 0.25, TW);
    }
    L.clue({ group: PA, at: [7.27, 0.85, 1.62], size: [0.6, 0.25, 0.06], mat: "woodDark", prompt: "Look inside cot 7", flag: "L06.cot7", id: "L06.cot7", title: "Inside cot 7",
      text: "Carved inside the rail by a child's hand, the letters uneven:\n\nARIN\n\nUnderneath, smaller, in another child's hand: 'and MAREN, who had the lantern'.",
      lines: [line("Arin", "My name. I slept here. I was one of them.")] });
    // PRESENT wall (north): wet wallpaper between the gaps
    for (let i = 0; i < 4; i++) { const x0 = seg[i * 2], x1 = seg[i * 2 + 1]; L.box(PR, x1 - x0, 4, 0.1, "wallpaper", (x0 + x1) / 2, 2, 16.3); L.box(PR, (x1 - x0) * 0.6, 0.9, 0.02, "ruin", (x0 + x1) / 2 + 0.3, 0.6, 16.24, { solid: false }); }
    L.box(PR, 16, 0.8, 0.1, "wallpaper", 0, 3.6, 16.3);
    L.clue({ group: PR, at: [-2.5, 1.65, 16.2], size: [0.45, 0.3, 0.04], prompt: "Read the pencil marks on the wallpaper", flag: "L06.notePresent", id: "L06.notePresent", title: "Pencil on wet wallpaper",
      text: "Child-height, pressed hard so it would not wash away:\n\n'three is 5. dont forget'" });
    L.particles(PR, "drips", [14, 1, 0.6], [0, 3.6, 15.6], 120);
    L.box(PR, 3, 0.01, 1.4, "puddle", 3.5, 0.015, 13.4, { solid: false });
    L.light(PR, 0x9fc8e8, 1.6, 7, 0, 3.2, 14.5);
    // FUTURE wall (east): holographic lattice, neon ribs
    L.box(FU, 0.08, 4, 2, "glass", 8.3, 2, 1); L.box(FU, 0.08, 4, 5.1, "glass", 8.3, 2, 6.55); L.box(FU, 0.08, 4, 5.1, "glass", 8.3, 2, 13.45);
    for (let i = 0; i < 5; i++) L.box(FU, 0.05, 4, 0.05, i % 2 ? "neonM" : "neonC", 8.2, 2, 0.5 + i * 3.75, { solid: false });
    L.clue({ group: FU, at: [8.02, 1.75, 12.6], size: [0.04, 0.4, 0.7], mat: "glass", prompt: "Read the flickering caption", flag: "L06.noteFuture", id: "L06.noteFuture", title: "Flickering caption",
      text: "A caption that repeats on the lattice, as if someone stored it there on purpose:\n\nNOTE 4 = 4" });
    L.label(FU, "NOTE 4 = 4", 7.98, 1.75, 12.6, PI / 2, 0.16, "#ffffff");
    L.crystals(FU, 3, 6.8, 1.4, 1.5, 1.5);
    L.light(FU, 0x40d8ff, 1.8, 8, 7, 2.5, 6);
    L.label(S, "<< PAST", -7.8, 3.2, 6, -PI / 2, 0.35, TW);
    L.label(S, "[ PRESENT ]", -2.5, 3.3, 15.8, 0, 0.35, TC);
    L.label(S, "FUTURE >>", 7.8, 3.2, 6, PI / 2, 0.35, TH);

    // ================= hanging shapes (marks painted in OTHER states) =================
    L.holoMesh(S, "torus", "amber", 0, 3.75, 8.6, 4, 4, 1, { spin: 0.2 }).rotation.x = PI / 2;
    const shapeDefs = [
      // [state idx, position, material, start, correct, what, mark state idx]
      [0, [-1.8, 2.2, 7.8], "wood", 0, 2, "the carved wooden star", 2],
      [1, [0, 2.2, 10.4], "rust", 3, 1, "the dented tin moon", 0],
      [2, [1.8, 2.2, 7.8], "crystal", 1, 3, "the cube of light", 1],
    ];
    const rots = shapeDefs.map(d => {
      const [si, at, mat, start, correct, what, mi] = d;
      const st = STATES[si], ms = STATES[mi];
      const e = L.rotator({ group: st[1], at, mat, size: [0.55, 0.55, 0.55], start, correct, prompt: "Turn " + what });
      e.grp.traverse(o => { o.userData.noEcho = true; });      // an echo would freeze the wrong orientation
      L.cyl(st[1], 0.01, 1.2, "woodDark", at[0], at[1] + 0.95, at[2]);
      // the mark: a painted floor arrow, existing only in the mark state (visible elsewhere as an echo)
      const mk = new THREE.Group(); mk.position.set(at[0], 0.02, at[2]); mk.rotation.y = correct * PI / 2; ms[1].add(mk);
      L.mesh(mk, "box", "gold", 0, 0, 0.25, 0.14, 0.03, 0.9);
      L.mesh(mk, "cone4", "gold", 0, 0, 0.8, 0.35, 0.4, 0.35).rotation.x = PI / 2;
      L.mesh(mk, "cyl", "brass", 0, 0, 0, 0.55, 0.02, 0.55);
      L.holoMesh(S, "sphere", si === 2 ? "cyan" : "amber", at[0], at[1] + 1.55, at[2], 0.12, 0.12, 0.12);
      L.label(S, "hangs in the " + st[0].toUpperCase() + " - its mark is painted in the " + ms[0].toUpperCase(), at[0], at[1] + 1.3, at[2], PI, 0.11, "#ffffff");
      return e;
    });
    L.alignment(rots, F.shapes, { toast: "The mobile hangs true. On the north ledges, the music boxes click." });

    // ================= the music boxes + the hum =================
    const notes = [1, 1.26, 1.498, 1.335];   // 1 3 5 4
    const playTune = async skip => { for (let i = 0; i < 4; i++) { if (i !== skip) AZ.note(notes[i]); await new Promise(r => setTimeout(r, 700)); } };
    const furniture = [["cradle", -5, 0.5, 2.0, 1.0, 0.9, "wood", 2.0], ["wardrobe", 0, 1.1, 2.0, 2.2, 0.8, "woodDark", 2.2], ["toy chest", 5, 0.4, 1.9, 0.8, 0.8, "red", -2.0]];
    furniture.forEach(f => {
      const [name, x, y, w, h, d, mat, dx] = f;
      const m = L.box(S, w, h, d, mat, x, y, 14.85, { dynamic: true });
      let aside = false, t = 1, from = x, to = x;
      L.interact({ at: [x, 1, 14.4], mesh: m, prompt: "Slide the " + name + " aside", onUse: it => {
        aside = !aside; from = m.position.x; to = aside ? x + dx : x; t = 0; L.sfxAt("scrape", it.pos);
        it.prompt = aside ? "Slide the " + name + " back" : "Slide the " + name + " aside"; } });
      L.update(dt => { if (t >= 1) return; t = Math.min(1, t + dt / 1.1); const e = t * t * (3 - 2 * t); m.position.x = from + (to - from) * e; });
    });
    L.label(S, "slide the furniture to reach the boxes", 0, 2.6, 14.4, PI, 0.14, "#ffffff");
    const mendFlags = ["L06.mendedPast", "L06.mendedPresent", "L06.mendedFuture"];
    STATES.forEach((st, i) => {
      const x = [-5, 0, 5][i], g = st[1];
      L.box(g, 0.5, 0.8, 0.5, "woodDark", x, 0.4, 17.1);
      const mb = L.box(g, 0.55, 0.4, 0.45, ["brass", "rust", "crystal"][i], x, 0.98, 17.1, { solid: false });
      L.label(g, "MUSIC BOX  " + st[4], x, 1.6, 17.1, PI, 0.14, st[3]);
      const heard = "L06.heard" + st[0];
      L.interact({ at: [x, 1, 17.1], mesh: mb, mask: st[2], prompt: "Wind the music box (" + st[0].toUpperCase() + ")", requires: [F.shapes],
        locked: "The music box is wound, but it will not play while the mobile hangs out of true.",
        onUse: async () => { await playTune(i + 1); L.set(heard); L.toast("The " + st[0].toUpperCase() + " box skips note " + (i + 2) + ". Hum it the whole song.", 3.5); } });
      L.onHum([x, 1, 17.1], 2.6, () => {
        if (AZ.get(F.lullaby)) return;
        L.set(mendFlags[i]); L.sfx("rule");
        L.say([line("Arin", "(The " + st[0].toUpperCase() + " box catches the hum - its missing note rings true.)", 3)], false);
      }, { mask: st[2], requires: [heard], once: false });
    });
    L.when(mendFlags, () => { L.set(F.lullaby); L.sfx("solved"); L.toast("The lullaby is whole - the room will not forget it now. Reach the drifting door.", 4); });
    // the toy blocks: an instrument to try notes
    L.sequence({ buttons: [0, 1, 2, 3, 4, 5].map(n => ({ group: S, at: [-3.75 + n * 1.5, 0.3, 3.9 + Math.abs(n - 2.5) * 0.25], note: n, label: String(n + 1) })), solution: [0, 2, 4, 3], flag: "L06.blocksPlayed" });
    L.box(S, 0.3, 0.9, 0.3, "woodDark", 1.4, 0.45, 6.2);
    L.clue({ group: S, at: [1.4, 0.95, 6.2], size: [0.5, 0.05, 0.4], prompt: "Read the lullaby sheet", flag: "L06.sheetRead", id: "L06.sheet", title: "The nursery lullaby",
      text: "Four notes, written large for small eyes:\n\n   1   _   _   _\n\nUnderneath: 'Each room that made this one forgets a different note. The boxes wait behind the furniture, one in each state. Wind each one where it lives - then sing it the whole song.'\n\n(HUM. The toy blocks let you try notes.)" });
    L.label(S, "1 _ _ _", 1.4, 1.5, 6.2, PI, 0.2, "#ffffff");
    // L05 choice echoes
    const keep = new THREE.Group(); keep.userData.dynamic = true; keep.visible = false; S.add(keep);
    L.box(keep, 0.5, 0.9, 0.4, "wood", -1.5, 0.45, 6.2, { solid: false }); L.box(keep, 0.45, 0.25, 0.32, "brass", -1.5, 1.02, 6.2, { solid: false });
    L.label(keep, "MAREN'S MUSIC BOX", -1.5, 1.6, 6.2, PI, 0.14, TW);
    L.when(["choice.L05.keepsake"], () => { keep.visible = true; });
    L.when(["choice.L05.keepsake", F.lullaby], () => { setTimeout(() => { L.toast("Maren's music box plays the whole song. No note missing - for the first time.", 4); playTune(-1); L.set("L06.keepsakePlayed"); }, 1200); });
    const page = new THREE.Group(); page.userData.dynamic = true; page.visible = false; S.add(page);
    L.box(page, 0.5, 0.65, 0.02, "paper", -6.8, 1.6, 2.8, { solid: false });
    L.label(page, "FAMILIES' REGISTER\nOSKEL, MAREN - 7\nstill counted", -6.8, 1.6, 2.78, PI, 0.11, "#4a3020");
    L.when(["choice.L05.records"], () => { page.visible = true; });

    // ================= the drifting door (keep it in view) =================
    const anchors = [{ x: -9.35, z: 10, ry: PI / 2 }, { x: 9.35, z: 10, ry: -PI / 2 }, { x: -5, z: -1.15, ry: 0 }];
    const door = new THREE.Group(); door.userData.dynamic = true; S.add(door);
    L.mesh(door, "box", "woodDark", -0.85, 1.4, 0, 0.18, 2.8, 0.25); L.mesh(door, "box", "woodDark", 0.85, 1.4, 0, 0.18, 2.8, 0.25); L.mesh(door, "box", "woodDark", 0, 2.9, 0, 1.9, 0.2, 0.25);
    const panel = L.mesh(door, "box", "plaster", 0, 1.3, 0, 1.5, 2.6, 0.1);
    L.label(door, "0", 0, 1.6, 0.07, 0, 0.6, "#5a4050");
    L.label(door, "THE WAY OUT - KEEP IT IN VIEW", 0, 3.3, 0.1, 0, 0.15, TH);
    L.holoMesh(door, "box", "magenta", 0, 1.4, -0.1, 1.7, 2.8, 0.02);
    const doorLight = L.light(S, 0xff40c8, 2.2, 7, anchors[0].x, 2.2, anchors[0].z);
    let ai = 0, unseen = 0, panelT = 0;
    const placeDoor = () => { const a = anchors[ai]; door.position.set(a.x, 0, a.z); door.rotation.y = a.ry; doorLight.position.set(a.x + Math.sin(a.ry) * 0.8, 2.2, a.z + Math.cos(a.ry) * 0.8); };
    placeDoor();
    const grace = [4, 3, 2.4][AZ.settings.difficulty] || 3;
    const vC = new THREE.Vector3(), vP = new THREE.Vector3(), fwd = new THREE.Vector3();
    W.L06 = { door, anchors };
    L.update(dt => {
      if (AZ.get(F.anchored)) { if (panelT < 1) { panelT = Math.min(1, panelT + dt / 1.6); panel.position.y = 1.3 + 2.7 * panelT * panelT * (3 - 2 * panelT); } return; }
      const cam = R.camera;
      vC.set(door.position.x, 1.3, door.position.z);
      vP.copy(vC).project(cam);
      fwd.set(0, 0, -1).applyQuaternion(cam.quaternion);
      const toX = vC.x - cam.position.x, toY = vC.y - cam.position.y, toZ = vC.z - cam.position.z;
      const inFront = fwd.x * toX + fwd.y * toY + fwd.z * toZ > 0;
      const seen = inFront && Math.abs(vP.x) < 1 && Math.abs(vP.y) < 1 && !AZ.sim.losBlocked(W, cam.position, vC);
      if (seen) unseen = 0; else unseen += dt;
      if (unseen > grace * 0.6 && unseen - dt <= grace * 0.6) L.toast("The door is drifting. Keep it in view.", 1.2);
      if (unseen > grace) {
        unseen = 0; ai = (ai + 1) % anchors.length; placeDoor(); L.sfx("rule", 0.6); R.ripple(0.4);
        if (!AZ.get(F.lullaby) && mendFlags.some(f => AZ.get(f))) { mendFlags.forEach(f => L.set(f, false)); L.toast("The door slipped away - and the room forgot the song.", 3); L.sfx("loop", 0.6); AZ.vibrate(60); }
      }
      // reaching it (only once the song is whole)
      const fx = door.position.x + Math.sin(door.rotation.y) * 0.7, fz = door.position.z + Math.cos(door.rotation.y) * 0.7;
      if (AZ.get(F.lullaby) && Math.hypot(P.pos.x - fx, P.pos.z - fz) < 1.2 && Math.abs(P.pos.y) < 1) { L.set(F.anchored); L.toast("The door holds still under your hand.", 3); L.sfx("rule"); }
    });
    // seam bridges from the ledges to the Witness platform (appear once the door is anchored)
    const bridges = [];
    const seg2 = (a, b) => { const m = L.ramp(S, a, b, 1.6, "glass"); m.userData.dynamic = true; m.visible = false; const c = W.colliders[W.colliders.length - 1]; c.on = false; bridges.push([m, c]); };
    seg2([-9.8, 0, 10], [-11, 0, 11]); seg2([-11, 0, 10.2], [-11, 0, 20.5]); seg2([-11.6, 0, 20], [-4.4, 0, 23]);
    seg2([9.8, 0, 10], [11, 0, 11]); seg2([11, 0, 10.2], [11, 0, 20.5]); seg2([11.6, 0, 20], [4.4, 0, 23]);
    seg2([-5, 0, -1.6], [-11.6, 0, -1.6]); seg2([-11, 0, -2.2], [-11, 0, 10.8]);
    const bridgeGlow = L.light(S, 0xff40c8, 0, 9, -11, 1, 16);
    L.when([F.anchored], () => { bridges.forEach(([m, c]) => { m.visible = true; c.on = true; }); bridgeGlow.intensity = 2; L.toast("The door opens onto the seam. A bridge of light unfolds toward a waiting figure.", 4); });

    // ================= the Witness platform =================
    L.floor(S, 0, 26, 10, 8, 0, "obsidian", 0.6);
    L.mesh(S, "sphere", "ruin", 0, -2.4, 26, 10, 4.5, 8).userData.noEcho = true;
    [[-5, 26, 0.06, 8], [5, 26, 0.06, 8], [0, 22, 10, 0.06]].forEach(e => L.box(S, e[2], 0.06, e[3], "neonC", e[0], 0.03, e[1], { solid: false }));
    L.wall(S, -5, 24.6, -5, 30, 1, "obsidian", 0.15); L.wall(S, 5, 24.6, 5, 30, 1, "obsidian", 0.15);
    const northRail = L.box(S, 10, 1, 0.15, "obsidian", 0, 0.5, 29.95, { dynamic: true });
    L.holoMesh(S, "torus", "magenta", 0, 0.05, 27, 4.5, 4.5, 1, { spin: 0.3 }).rotation.x = PI / 2;
    L.light(S, 0xff40c8, 2.6, 10, 0, 2.5, 26.5); L.light(S, 0x40d8ff, 1.4, 7, 0, 1.2, 23);
    const witness = L.npc({ group: S, name: "The Witness", at: [0, 27.2], yaw: PI, coat: "white", holo: "magenta",
      task: "Keeps watch over the seam between states, recording what the city chose to forget.",
      route: [[-2.4, 27.4, 5, "look"], [2.4, 27.8, 5, "idle"], [0, 25.4, 4, "work"]],
      approach: ["You came through the door with no address.", "I have waited many cycles for someone to look."],
      talk: [line("The Witness", "I am what remains when everyone else forgets."), line("The Witness", "Stand in the ring of light, and I will show you what I saw."),
        line("The Witness", "I write letters, sometimes. To people who have not arrived yet."), line("The Witness", "The children called this room the nursery. The Founders called it the pocket."),
        line("The Witness", "Do not ask my name. Names were the first thing they took from us.")],
      cond: [{ holding: "L06_zero", lines: [line("The Witness", "Carry it carefully. It was Ilse's - and it is the only proof of a child the city never counted."), line("The Witness", "The tower will want it back. Do not let it have it cheaply.")] },
        { flag: "L06.cot7", lines: [line("The Witness", "You found your name. Cot seven. I slept in cot eight, with the lantern."), line("The Witness", "You used to say the watch would start ticking when it was time to go home.")] },
        { flag: "choice.L05.keepsake", lines: [line("The Witness", "You carried a music box out of the flood once. A small brass one."), line("The Witness", "...Someone told me. Someone who never stopped being grateful.")] },
        { flag: "choice.L05.records", lines: [line("The Witness", "You saved the register. Every name the Founders struck out."), line("The Witness", "Odile Fenwick will know what to do with it. She always did.")] }],
      changedFlag: F.witness, changedRoute: [[0.8, 29.2, 6, "look"], [-3, 28, 4, "idle"], [3, 28, 4, "idle"]],
      changedTalk: [line("Maren Oskel", "Return to the tower, Arin. Before the final bell."), line("Maren Oskel", "What you do with the truth is yours.")] });
    L.onHum([0, 1, 27], 6, () => L.say([line("The Witness", "You still hum it. All four notes. You always had the whole song - that's why they gave you cot seven, by the door.", 5)], false), { requires: [F.witness] });
    const apparition = new THREE.Group(); apparition.userData.dynamic = true; apparition.visible = false; S.add(apparition);
    L.holoMesh(apparition, "cyl", "magenta", 0, 2.4, 29.2, 1.4, 4, 0.9, { bob: 0.1 });
    L.holoMesh(apparition, "sphere", "magenta", 0, 4.8, 29.2, 0.8, 0.8, 0.8, { bob: 0.1 });
    L.holoMesh(apparition, "torus", "cyan", 0, 5.6, 29.2, 1.8, 1.8, 1, { spin: 0.8 }).rotation.x = PI / 2;
    L.zone({ at: [0, 1, 23.6], size: [10, 2, 2.4], requires: [F.anchored], onEnter: async () => {
      apparition.visible = true;
      await L.memory([
        line("The Witness", "You found it. The room with no address.", 4),
        line("The Witness", "Every family in the basin marked tomorrow. They were not afraid of the bell. They were waiting for it.", 5),
        line("The Witness", "The Founders saw a larger collapse coming - every state falling into one. So they hid the children between the states. Fourteen of us. Here.", 6),
        line("The Witness", "I was small when they hid us here. I had a lantern. You hummed in the dark so the little ones would sleep.", 5.5),
        line("Arin", "...Maren? The girl in the laundry.", 3.5),
        line("The Witness", "Maren Oskel. Grown, in a place where nothing grows. The rescue failed, Arin. The door closed with me inside.", 6),
        line("The Witness", "I stayed so someone would be here when you came back.", 4.5),
        line("The Witness", "Your name is in cot seven. You carved it the first night. Take the zero - it was Ilse Marrow's. She left it when she went to stop the tower.", 7),
        line("The Witness", "Carry it back before the final bell.", 3.5),
      ], F.witness);
      apparition.visible = false; L.set(F.isWitness);
      witness.name = "Maren Oskel"; if (witness.tag) witness.tag.visible = false;
    } });
    // Ilse's carved zero
    L.box(S, 0.6, 1, 0.6, "obsidian", 0, 0.5, 28.6);
    L.box(S, 0.64, 0.02, 0.64, "neonC", 0, 1.01, 28.6, { solid: false });
    const zeroMesh = L.mesh(S, "torus", "brass", 0, 1.32, 28.6, 0.42, 0.55, 0.42);
    const zero = L.item({ group: S, at: [0, 1.32, 28.6], key: "L06_zero", name: "Ilse's carved zero", pickedFlag: F.zero, mesh: zeroMesh });
    zero.prompt = "Take Ilse's carved zero";
    const zeroGlow = L.light(S, 0xf0f8ff, 0, 4, 0, 1.6, 28.2);
    zeroMesh.visible = false; zero.enabled = false;
    L.when([F.witness], () => { zeroMesh.visible = true; zero.enabled = true; zeroGlow.intensity = 1.6; L.sfx("reveal"); });

    // ================= exit: back toward the tower =================
    const exitBridge = L.ramp(S, [0, 0, 29.9], [0, 0, 36.1], 2, "glass"); exitBridge.userData.dynamic = true; exitBridge.visible = false;
    const ebc = W.colliders[W.colliders.length - 1]; ebc.on = false;
    L.when([F.zero], () => {
      exitBridge.visible = true; ebc.on = true; northRail.visible = false; northRail.userData.collider.on = false;
      L.sfx("bell"); L.toast("Far off, the tower bell gathers itself for its final ring. Go back - now.", 4);
    });
    L.floor(S, 0, 39, 8, 6, 0, "stone", 1);
    L.wall(S, -4, 36, -4, 42, 1.1, "woodDark", 0.25); L.wall(S, 4, 36, 4, 42, 1.1, "woodDark", 0.25);
    L.box(S, 0.4, 3.2, 0.4, "brass", -1.3, 1.6, 40.2); L.box(S, 0.4, 3.2, 0.4, "brass", 1.3, 1.6, 40.2); L.box(S, 3, 0.3, 0.4, "brass", 0, 3.35, 40.2);
    L.label(S, "TO THE TOWER", 0, 3.85, 40, PI, 0.3, TW);
    L.door(S, 2.2, 3, 0.2, "iron", 0, 0, 40.2, [F.zero], [0, 3.2, 0]);
    L.exit({ group: S, at: [0, 0, 41.3], size: [2.2, 2.5, 1.2], requires: [F.witness, F.zero], locked: "The way back is sealed. The Witness said to take the carved zero." });
    L.box(S, 12, 70, 12, "stone", 0, 10, 120, { solid: false, noEcho: true });
    L.box(S, 15, 8, 15, "brass", 0, 47, 120, { solid: false, noEcho: true });
    L.holoMesh(S, "cyl", "amber", 0, 38, 113.8, 9, 0.1, 9).rotation.x = PI / 2;
    L.label(S, "(0)", 0, 38, 113.6, PI, 4, "#34445a");
    L.light(S, 0xe8f0ff, 4, 30, 0, 38, 108);

    // ================= optional: the cupboard of lost things =================
    L.floor(S, 9.6, 3, 3.2, 4, 0, "woodDark", 0.5);
    L.wall(S, 8, 1, 11.2, 1, 2.7, "woodDark", 0.2); L.wall(S, 11.2, 1, 11.2, 5, 2.7, "woodDark", 0.2); L.wall(S, 11.2, 5, 8, 5, 2.7, "woodDark", 0.2);
    L.box(S, 3.4, 0.2, 4.2, "woodDark", 9.6, 2.8, 3, { solid: false });
    L.label(S, "CUPBOARD OF\nLOST THINGS", 7.85, 2.5, 3, -PI / 2, 0.2, TW);
    for (let i = 0; i < 3; i++) L.box(S, 0.5, 0.05, 3.6, "wood", 10.85, 0.6 + i * 0.65, 3, { solid: false });
    L.light(S, 0xffd8a0, 1.6, 5, 9.6, 2.4, 3);
    L.sphere(PA, 0.12, "brass", 10.85, 0.75, 2); L.box(PA, 0.3, 0.02, 0.6, "red", 10.85, 0.66, 3, { solid: false }); L.box(PA, 0.12, 0.1, 0.22, "woodDark", 10.85, 0.7, 4, { solid: false });
    L.box(PR, 0.2, 0.02, 0.12, "paper", 10.85, 1.27, 2.2, { solid: false }); L.box(PR, 0.3, 0.25, 0.45, "ochre", 10.85, 1.4, 3.3, { solid: false }); L.box(PR, 0.15, 0.22, 0.15, "glass", 10.85, 1.38, 4.2, { solid: false });
    L.holoMesh(FU, "box", "cyan", 10.85, 2.05, 2.4, 0.2, 0.2, 0.2, { spin: 1 }); L.box(FU, 0.1, 0.1, 0.1, "crystal", 10.85, 1.98, 3.6, { solid: false });
    L.clue({ group: S, at: [8.6, 1, 4.7], size: [0.4, 0.3, 0.04], prompt: "Read the lost-property tags", flag: "L06.lostThings", id: "L06.lostTags", title: "Lost-property tags",
      text: "Tags on string, all in the same tidy hand:\n\n- one festival ribbon, one toy bell (PAST)\n- one tram ticket, Line 4, one wet satchel (PRESENT)\n- one toy made of light (FUTURE)\n- one blue cup - CLAIMED\n\nThe last tag is crossed out and rewritten: 'one carved zero - lent, not lost. I. M.'" });

    // ================= NPCs =================
    L.npc({ group: PA, name: "Nanny Oriel", at: [-5, 5], yaw: PI / 2, coat: "white",
      task: "Nursemaid of the nursery with no address: rocking the cot and folding blankets for children who have not arrived yet.",
      route: [[-5.3, 4.3, 5, "work"], [-6.2, 8.4, 4, "carry"], [-3.8, 12.2, 4, "look"]],
      approach: ["Hush - she's only just asleep.", "Mind the cot, love. Wipe your feet."],
      talk: [line("Nanny Oriel", "I sing the same lullaby every night. Some nights the notes go missing."), line("Nanny Oriel", "The children are coming tomorrow. All of them, from every house in the basin."),
        line("Nanny Oriel", "Fourteen expected, the rota says. I have counted the blankets twice."), line("Nanny Oriel", "The door of this room doesn't stay put. You have to keep your eyes on it, like a sleeping child."),
        line("Nanny Oriel", "Ilse Marrow brought a little carved zero. She said it would keep the room from forgetting itself.")],
      cond: [{ holding: "L06_zero", lines: [line("Nanny Oriel", "The little zero. Ilse carved it for every child they could not count."), line("Nanny Oriel", "Keep it close. Somebody has to.")] },
        { flag: F.lullaby, lines: [line("Nanny Oriel", "There it is - the whole song. I'd forgotten how it ends."), line("Nanny Oriel", "Go on. The door won't wait for anyone who looks away.")] },
        { flag: "L06.heardPast", lines: [line("Nanny Oriel", "You heard it too? The holes in the song."), line("Nanny Oriel", "I carved the second note into the cot rail, so I'd never lose it.")] },
        { flag: "choice.L05.keepsake", lines: [line("Nanny Oriel", "That brass music box... I know that song. Every mother in the basin sang it."), line("Nanny Oriel", "Whoever owns it was loved. You can hear it in the gears.")] }],
      changedFlag: F.lullaby, changedRoute: [[-5.6, 9.4, 6, "idle"], [-3.2, 7, 4, "idle"], [-5.3, 4.3, 4, "work"]],
      changedTalk: [line("Nanny Oriel", "There it is - the whole song. I'd forgotten how it ends."), line("Nanny Oriel", "Go on. The door won't wait for anyone who looks away.")] });
    L.npc({ group: PR, name: "Little Wren", at: [2.4, 3.3], yaw: PI, coat: "ochre", scale: 0.7,
      task: "A child's echo, stacking toy blocks into the notes of the lullaby she still remembers.",
      route: [[2.6, 3.2, 5, "work"], [3.6, 9.6, 4, "look"], [-1.6, 5.2, 4, "idle"]],
      approach: ["Shh! The song is broken.", "Are you a grown-up from the water?"],
      talk: [line("Little Wren", "There are only four notes. In my room the third one drowned."), line("Little Wren", "Nanny Oriel says the door is shy. It only moves when you're not looking."),
        line("Little Wren", "Maren had the lantern. Maren always has the lantern."), line("Little Wren", "When the bell rings we all hold hands. Two by two. That's the rule."),
        line("Little Wren", "The pencil on the wall is mine. Three is five. I wrote it so I wouldn't forget.")],
      cond: [{ holding: "L06_zero", lines: [line("Little Wren", "The zero! Is it for me?"), line("Little Wren", "...No. It's for the one nobody counted. Take it home for them.")] },
        { flag: F.lullaby, lines: [line("Little Wren", "You finished it! That's how Mama sang it."), line("Little Wren", "Where does the door go? Can I come? ...I'm not allowed, am I.")] },
        { flag: F.shapes, lines: [line("Little Wren", "You fixed my mobile! Now the music boxes will sing."), line("Little Wren", "Listen for the holes. The holes are the important part.")] },
        { flag: "choice.L05.records", lines: [line("Little Wren", "Is my name in the big book? Is it written properly?"), line("Little Wren", "Good. Then I'm still counted.")] }],
      changedFlag: F.lullaby, changedRoute: [[-0.8, 7.6, 5, "look"], [1.6, 3.2, 4, "work"], [2.8, 12.8, 4, "look"]],
      changedTalk: [line("Little Wren", "You finished it! That's how Mama sang it."), line("Little Wren", "Where does the door go? Can I come? ...I'm not allowed, am I.")] });
    L.crowd(S, -3, -20.5, 1.6, 4, 2, { holo: "cyan" });
    L.crowd(S, 3.4, -20.5, 1.4, 4, 1, { holo: "cyan" });

    // ================= the void =================
    L.particles(S, "dreams", [60, 16, 70], [0, 3, 8], 500);
    L.particles(S, "motes", [30, 6, 34], [0, 2, 8], 300);
    L.particles(PA, "dust", [18, 4, 18], [0, 2, 8], 200);
    L.particles(PR, "rain", [40, 14, 50], [0, 8, 8], 500);
    L.debris(FU, 12, 0, -2, 10, 70, 14, 70);
    for (let i = 0; i < 10; i++) { const a = i * 0.63, r = 18 + (i % 4) * 6; L.box(i % 2 ? PA : PR, 0.3 + (i % 3), 0.3, 2 + (i % 3), i % 2 ? "wood" : "stoneWet", Math.cos(a) * r, -4 + (i % 5) * 3, 8 + Math.sin(a) * r, { solid: false, noEcho: true, ry: a }); }
    L.lamp(PA, -14, 2, "gas", 3, -1); L.lamp(PR, 13, 12, "sodium", 3.4, -1.5); L.lamp(FU, -12, 18, "neon", 3.2, -0.5);
    L.holoMesh(FU, "box", "cyan", 11, 5, 2, 4, 1, 0.02, { bob: 0.2, ry: -0.9 });
    L.label(FU, "ADDRESS NOT FOUND", 10.95, 5, 1.95, -0.9 + PI, 0.3, "#ffffff");
    L.light(PA, 0xffb469, 3, 18, 0, -4, 8); L.light(PR, 0x9fc8e8, 3, 18, 0, -4, 8); L.light(FU, 0x9a60ff, 3.5, 20, 0, -4, 8);
    L.ambient("nursery", 0.4); L.ambient("drone", 0.22); L.ambient("wind", 0.18); L.ambient("water", 0.12, 2);

    // ================= spawn, checkpoints, fragments =================
    L.player(0, 0, -22.4, PI);
    L.checkpoint(0, 0, -22.4, PI, { size: [6, 2.5, 3] });
    L.checkpoint(0, 0, 1.6, PI, { size: [5, 2.5, 2.4] });
    L.checkpoint(0, 0, 12.2, PI, { climax: true, size: [4, 2.5, 2] });
    L.checkpoint(0, 0, 23.4, PI, { size: [8, 2.5, 2.4] });
    L.fragment("L06.1", [10.6, 2.35, 4.4], "A tram ticket", "Line 4, Basin to Tower, single. Punched nine times by a conductor who wrote on the back: 'no charge - children'. Signed I. Marrow.", S);
    L.floor(PA, -2.5, -11.25, 1.8, 1.8, 0, "wood", 0.25);
    L.fragment("L06.2", [-2.5, 1.2, -11.25], "The nursemaid's rota", "Oriel, nights. Expected: fourteen children, Bell Day eve. Bring every blanket in the ward. If the bell rings twice, keep singing - whatever happens, keep singing.", PA);
    L.floor(FU, 2.5, -5.25, 1.8, 1.8, 0, "glass", 0.12);
    L.fragment("L06.3", [2.5, 1.2, -5.25], "Seam log", "POCKET STABLE. CYCLE 0 ... 1 ... 112 ... 113. OCCUPANT COUNT: 1 ADULT. NAME WITHHELD AT OCCUPANT'S REQUEST. NOTE: OCCUPANT WAS ADMITTED AS A CHILD.", FU);
  },
});
