/* LEVEL 04 — The Street Beneath the Street (Abandoned Tram Line). Port of Level04Builder (Design v2).
   Layout (+z north): maintenance gallery (start, y 4) over the Depot (z 5..30) -> Crossing hall, three live
   tracks (z 30..52) -> Station (z 52..84, office, lift shaft x -6..0 z 78..84) -> deep corridor (y -6). */
AZ.registerLevel({
  id: "L04", order: 4, chapter: "LEVEL 04", title: "The Street Beneath the Street", location: "Abandoned Tram Line",
  start: 1, allowed: 7, killY: -12,
  build(L) {
    const { line } = L, S = L.shared, PA = L.past, PR = L.present, FU = L.future, W = L.W, T3 = L.THREE, P = AZ.player;
    const dyn = (parent, visible = true) => { const g = new T3.Group(); g.userData.dynamic = true; g.visible = visible; (parent || S).add(g); return g; };
    const V = (x, y, z) => new T3.Vector3(x, y, z);
    const WARM = "#ffd79a", COLD = "#cfe6ff", HALF = Math.PI / 2;

    L.look({ fog: 0.7, past: { density: 0.01, sunI: 0.4 }, present: { density: 0.028, sunI: 0.15, hemiI: 0.45 }, future: { density: 0.02, bloom: 1.5 } });
    L.intro(line("Arin", "A maintenance gallery. Good - from up here I can watch it before it hears me."),
      line("Arin", "It has no eyes. It listens to the rails. Every footstep is a knock on its door."),
      line("Arin", "The Keepers are down there too, with lanterns. The machine hears; the Keepers see. CROUCH beats one, cover beats the other."));
    L.objective("Watch the machine and read the rail pulses", "L04.pulsesRead", "Use the pulse monitor on the gallery railing.");
    L.objective("Cross the tram lines between the trains", "L04.crossedTracks", "The PRESENT crossing is buried. In the PAST trams run - wait on the refuges for GO. The FUTURE has a crystal bridge with its own rhythm.");
    L.objective("Reroute the loose cable to power the lift", "L04.liftPowered", "PRESENT: the sparking cable end lies by the dead junction (north-west corner); plug it into the lift coupling.");
    L.objective("Create a false vibration to lure the machine away", "L04.falseVibration", "Pull the gantry release by the platform edge, or use a clamp or vent. Decoys are limited.");
    L.objective("Recover the evacuation list from the station office", "L04.listRecovered", "The office is only open in the PRESENT. Go in while the machine is busy elsewhere.");
    L.objective("Confront Founder Halden's echo (FUTURE, by the lift)", "L04.haldenDone", "Shift to the FUTURE; the echo flickers south-east of the lift gate.");
    L.objective("Hold the lift circuit: throw both breakers while Calder hunts you", "L04.circuitHeld", "PRESENT: breaker A on the west wall, breaker B at the platform edge.");
    L.objective("Ride the lift down through the collapsing shaft", "L04.liftArrived", "Press DESCEND, then watch the red warning light: debris falls on one half of the car at a time.");
    L.payoff("L04.listRecovered", "My name on the list", "Evacuation list, Line 4, signed by Founder Aurel Halden nine days BEFORE the official catastrophe. Car 3: the Oskel family - Tamsin, Joss and Maren, age 7. And: VALE, A., age 8. Guardian: I. Marrow. I was one of the children.");

    // ================================================================ helpers
    const noiseMeter = { last: V(0, 0, 0), speed: 0 };
    L.update(dt => { if (dt > 0) { noiseMeter.speed = Math.hypot(P.pos.x - noiseMeter.last.x, P.pos.z - noiseMeter.last.z) / dt; noiseMeter.last.copy(P.pos); } });
    const sentinels = [];
    /** The blind vibration machine (Present only): patrols, investigates loud vibrations, catches noisy movers. */
    const sentinel = (name, path, hear, thr) => {
      const g = dyn(PR);
      g.position.set(path[0][0], 0, path[0][1]);
      L.mesh(g, "box", "obsidian", 0, 1.1, 0, 1.5, 0.9, 2.1);
      L.mesh(g, "box", "iron", 0, 1.6, -0.2, 1.2, 0.2, 1.4);
      for (const ex of [-0.4, 0, 0.4]) L.mesh(g, "box", "red2", ex, 1.2, 1.07, 0.18, 0.08, 0.04);
      for (let i = 0; i < 4; i++) L.mesh(g, "box", "iron", (i % 2 ? 1 : -1) * 0.95, 0.45, (i < 2 ? -1 : 1) * 0.8, 0.12, 0.9, 0.12);
      const scan = new T3.Group(); scan.position.y = 1.85; g.add(scan);
      L.mesh(scan, "cyl", "iron", 0, 0, 0, 1.4, 0.12, 1.4);
      for (let i = 0; i < 4; i++) L.mesh(scan, "box", "red2", Math.cos(i * HALF) * 0.68, 0.04, Math.sin(i * HALF) * 0.68, 0.14, 0.08, 0.14);
      L.label(g, "MAINTENANCE UNIT M-0", 0, 2.6, 0, 0, 0.2, "#ff9080");
      const eye = L.light(g, 0xff2010, 2.5, 9, 0, 1.3, 1.6);
      const s = { g, path, idx: 0, inv: null, search: 0, hear: hear * AZ.diff.perception(), thr, start: g.position.clone(), alertCd: 0 };
      AZ.on("noise", (pos, loud) => {
        if (W.state !== 1) return;
        const d = Math.hypot(pos.x - g.position.x, pos.z - g.position.z) + Math.abs(pos.y - g.position.y) * 0.5;
        if (d > s.hear || loud * (1 - d / s.hear) < s.thr) return;
        s.inv = pos.clone(); s.search = 0;
        if (s.alertCd <= 0) { s.alertCd = 2; L.sfxAt("alert", g.position, 0.6); }
      });
      AZ.on("respawn", () => { g.position.copy(s.start); s.idx = 0; s.inv = null; s.search = 0; });
      const mv = (x, z, sp, dt) => { const dx = x - g.position.x, dz = z - g.position.z, d = Math.hypot(dx, dz); if (d < 0.3) return true; g.position.x += dx / d * Math.min(sp * dt, d); g.position.z += dz / d * Math.min(sp * dt, d); g.rotation.y += AZ.angDiff(Math.atan2(dx, dz), g.rotation.y) * Math.min(1, dt * 5); return false; };
      L.update((dt, t) => {
        s.alertCd -= dt;
        if (W.state !== 1) return;
        const busy = s.inv || s.search > 0;
        scan.rotation.y += dt * (busy ? 4 : 1);
        eye.intensity = 2 + 1.4 * Math.sin(t * (busy ? 30 : 7));
        if (s.search > 0) { s.search -= dt; g.rotation.y += dt * 1.6; }
        else if (s.inv) { if (mv(s.inv.x, s.inv.z, 4.2, dt)) { s.inv = null; s.search = 3.5; } }
        else if (mv(path[s.idx][0], path[s.idx][1], 2.2, dt)) s.idx = (s.idx + 1) % path.length;
        if (P.dying) return;
        const d = Math.hypot(P.pos.x - g.position.x, P.pos.z - g.position.z);
        const quiet = P.crouch || noiseMeter.speed < 0.6;
        if (Math.abs(P.pos.y - g.position.y) < 2.5 && d < 2.2 && (!quiet || d < 1.0)) AZ.game.die("The machine felt your steps through the rails. CROUCH to move quietly, and read the pulse strips.");
      });
      sentinels.push(s);
      return s;
    };
    /** Rail pulse strip: glows brighter and taller the closer a (present) sentinel is. */
    const strips = [];
    const pulseStrip = (x, y, z, alongX) => { const mat = new T3.MeshBasicMaterial({ color: 0x103038 }); const m = L.mesh(dyn(S), "box", mat, x, y + 0.06, z, alongX ? 2.4 : 0.12, 0.05, alongX ? 0.12 : 2.4); m.castShadow = false; strips.push({ m, mat, x, z }); };
    L.update((dt, t) => {
      for (const st of strips) {
        let near = 1e9;
        if (W.state === 1) for (const s of sentinels) near = Math.min(near, Math.hypot(s.g.position.x - st.x, s.g.position.z - st.z));
        const k = near < 20 ? 1 - near / 20 : 0, pulse = k * (0.5 + 0.5 * Math.sin(t * (1 + k * 10) * Math.PI));
        st.mat.color.setRGB(0.06 + pulse * 0.3, 0.2 + pulse * 0.75, 0.24 + pulse * 0.8);
        st.m.scale.y = 0.05 * (1 + pulse * 3);
      }
    });
    /** Limited decoys (difficulty): all share one supply. */
    let decoysUsed = 0;
    const decoy = (g, at, size, prompt, noiseAt, tag, tagRy) => {
      const m = L.box(g, size[0], size[1], size[2], "red2", at[0], at[1], at[2], { solid: false });
      L.label(g, tag, at[0], at[1] + 0.7, at[2], tagRy, 0.18, "#ffa090");
      let cd = 0; L.update(dt => { cd -= dt; });
      L.interact({ at, mesh: m, mask: 2, prompt, onUse: () => {
        if (cd > 0) { L.toast("Wait for the echo to settle.", 1.5); return; }
        const limit = AZ.diff.decoys();
        if (decoysUsed >= limit) { L.toast("No decoys left. Find another way past.", 3); L.sfx("denied"); return; }
        decoysUsed++; cd = 8; L.sfx("click");
        if (limit !== Infinity) L.toast("Decoys left: " + (limit - decoysUsed), 2.5);
        setTimeout(() => { L.sfxAt("clang", V(...noiseAt), 1); AZ.emit("noise", V(...noiseAt), 4); L.set("L04.falseVibration"); }, 1300);
      } });
    };
    const track = (g, x1, z1, x2, z2, y = 0) => {
      const len = Math.hypot(x2 - x1, z2 - z1), ry = Math.atan2(x2 - x1, z2 - z1), ux = (z2 - z1) / len, uz = -(x2 - x1) / len;
      for (const s of [-0.72, 0.72]) L.box(g, 0.08, 0.14, len, "rail", (x1 + x2) / 2 + ux * s, y + 0.07, (z1 + z2) / 2 + uz * s, { solid: false, ry });
      for (let i = 0; i < len; i += 3.2) L.box(g, 2, 0.04, 0.25, "woodDark", x1 + (x2 - x1) * i / len, y + 0.02, z1 + (z2 - z1) * i / len, { solid: false, ry, noEcho: true });
    };
    /** Past tram that ping-pongs between a and b (along x or z). Kills on contact in the Past. */
    const tram = (g, a, b, y, speed, pause, len, coat, label) => {
      const t = dyn(g); t.position.set(a[0], y, a[1]);
      const alongX = Math.abs(b[0] - a[0]) > Math.abs(b[1] - a[1]);
      const bw = alongX ? len : 2.4, bd = alongX ? 2.4 : len;
      L.mesh(t, "box", coat, 0, 1.7, 0, bw, 2.8, bd); L.mesh(t, "box", "iron", 0, 3.2, 0, bw - 0.3, 0.2, bd - 0.3);
      L.mesh(t, "box", "window", alongX ? 0 : 1.22, 2.1, alongX ? 1.22 : 0, alongX ? len - 1.5 : 0.04, 0.8, alongX ? 0.04 : len - 1.5);
      L.mesh(t, "box", "window", alongX ? 0 : -1.22, 2.1, alongX ? -1.22 : 0, alongX ? len - 1.5 : 0.04, 0.8, alongX ? 0.04 : len - 1.5);
      for (const sgn of [-1, 1]) { L.mesh(t, "box", "warm", alongX ? sgn * (len / 2 + 0.02) : 0, 1, alongX ? 0 : sgn * (len / 2 + 0.02), alongX ? 0.04 : 1.4, 0.25, alongX ? 1.4 : 0.04); L.label(t, label, alongX ? sgn * (len / 2 + 0.05) : 0, 2.7, alongX ? 0 : sgn * (len / 2 + 0.05), alongX ? sgn * HALF : (sgn > 0 ? 0 : Math.PI), 0.3, WARM); }
      const head = L.light(t, 0xfff0c0, 3.5, 16, 0, 1.2, 0);
      const st = { t, a, b, dir: 1, pause: 0, alongX, len, hornCd: 0 };
      L.update(dt => {
        if (st.pause > 0) { st.pause -= dt; return; }
        const tgt = st.dir > 0 ? b : a, dx = tgt[0] - t.position.x, dz = tgt[1] - t.position.z, d = Math.hypot(dx, dz);
        if (d < 0.05) { st.dir = -st.dir; st.pause = pause; return; }
        t.position.x += dx / d * Math.min(speed * dt, d); t.position.z += dz / d * Math.min(speed * dt, d);
        head.local.set(alongX ? Math.sign(dx) * (len / 2 + 1) : 0, 1.2, alongX ? 0 : Math.sign(dz) * (len / 2 + 1));
        st.hornCd -= dt;
        if (W.state !== 0 || P.dying) return;
        const px = Math.abs(P.pos.x - t.position.x), pz = Math.abs(P.pos.z - t.position.z), py = P.pos.y - y;
        if (st.hornCd <= 0 && px + pz < 24) { st.hornCd = 4; L.sfxAt("warn", t.position, 1); }
        if (py < 3 && py > -0.5 && px < bw / 2 + 0.7 && pz < bd / 2 + 0.7) AZ.game.die("Hit by a tram. Wait on a refuge for the signal to read GO - the headlight and horn come first.");
      });
      return st;
    };

    // ================================================================ gallery + depot
    L.floor(S, 0, 14, 28, 32, 0, "concrete");
    L.box(S, 28, 4, 7, "concrete", 0, 2, 1.5); L.box(S, 28, 0.1, 7.04, "iron", 0, 4.02, 1.5, { solid: false }); // plate overhangs the edge (was coplanar)
    L.wall(S, -14, -2, 14, -2, 9, "tile"); L.wall(S, -14, -2, -14, 30, 9, "tile");
    L.wallDoor(S, 14, -2, 14, 30, 9, "tile", 1.8, 2.6, 24 / 32);
    L.wallDoor(S, -14, 30, 14, 30, 9, "tile", 4, 3.4);
    L.stairs(S, [-10.5, 0, 11], [-10.5, 4, 5], 3, "iron");
    L.block(2.5, 5, 23, 0.2, 4, 5.1); L.block(-13.5, 5, 1, 0.2, 4, 5.1);
    L.box(S, 23, 0.06, 0.12, "rail", 2.5, 5.1, 5, { solid: false }); L.box(S, 23, 0.04, 0.06, "rail", 2.5, 4.6, 5, { solid: false });
    for (let x = -9; x <= 14; x += 2.3) L.box(S, 0.06, 1.1, 0.06, "iron", x, 4.55, 5, { solid: false });
    L.label(S, "DEPOT 4 - MAINTENANCE", 0, 7.6, 29.7, Math.PI, 0.5, COLD, "rgba(8,12,18,.9)");
    track(S, -2, 5.5, -2, 29.5); track(S, 6, 5.5, 6, 29.5);
    L.clue({ group: S, at: [4, 4.6, 4.7], size: [1.2, 0.7, 0.2], mat: "iron", prompt: "Read the pulse monitor", flag: "L04.pulsesRead", id: "L04.pulses", title: "Rail pulse monitor",
      text: "LINE 4 MAINTENANCE UNIT - VIBRATION TRACKING.\n\nThe unit is blind. It follows vibration through the rails.\n- Walking: heard at about ten paces.\n- Running: heard across a hall.\n- Crouching: below its threshold.\n- Standing still: silent.\n\nThe cyan strips along the rails pulse faster and brighter the closer the unit is. The Keepers' lanterns are another matter: they SEE. Break their line of sight behind crates.",
      lines: [line("Arin", "Brighter and faster means closer. Crouch, and I'm just a rumour to it.")] });
    L.label(S, "PULSE MONITOR", 4, 5.15, 4.55, Math.PI, 0.2, "#7ff0ff");
    L.zone({ at: [-10.5, 5, 4], size: [3, 2, 2], toast: "CROUCH to move quietly. The machine hears footsteps through the rails." });
    for (let i = 0; i < 6; i++) { pulseStrip(-2, 0, 7 + i * 4); pulseStrip(6, 0, 7 + i * 4); }
    for (let i = 0; i < 4; i++) pulseStrip(-7 + i * 5, 5.1, 5, true);
    sentinel("Depot", [[-8, 13], [8, 13], [8, 26], [-8, 26]], 12, 0.18);
    // Past depot: lamps, shunting tram, workers.
    L.box(PA, 28.5, 0.4, 32.5, "tile", 0, 9.2, 14, { solid: false });
    L.light(PA, 0xffc890, 3, 20, -6, 8, 9); L.light(PA, 0xffc890, 3, 20, 6, 8, 21);
    L.label(PA, "VEYRA MUNICIPAL TRAMWAYS", 13.65, 5.5, 16, -HALF, 0.55, WARM, "rgba(30,20,10,.9)");
    tram(PA, [-2, 8.5], [-2, 24], 0, 4.5, 4, 7, "ochre", "SHUNTING");
    L.crowd(PA, 9, 16, 5, 14, 4);
    L.ambient("machine", 0.22, 1);
    // Present depot: puddles, sodium lamp, wreck.
    L.box(PR, 28.5, 0.4, 32.5, "concrete", 0, 9.2, 14, { solid: false });
    [[-4, 16], [9, 21], [2, 27], [-10, 22]].forEach(([x, z]) => L.box(PR, 3.5, 0.02, 2.6, "puddle", x, 0.02, z, { solid: false }));
    L.lamp(PR, -13.3, 14, "sodium", 5);
    L.light(PR, 0x6a9ac8, 2.2, 22, 0, 7, 16); L.light(PR, 0xff9a48, 1.6, 12, 10, 2, 24); L.light(PR, 0x5ad0e0, 1.2, 10, -2, 0.6, 12);
    L.box(PR, 2.4, 2.6, 8, "rust", 6, 1.3, 18); L.box(PR, 0.04, 0.7, 6.8, "windowCold", 4.78, 1.8, 18, { solid: false });
    L.particles(PR, "drips", [10, 8, 8], [2, 0.5, 18], 200);
    L.ambient("water", 0.18, 2); L.ambient("machine", 0.12, 2);
    // Future depot: split ceiling, crystals, crystal plug sealing the north door.
    L.box(FU, 11.5, 0.4, 32.5, "ruin", -8.5, 9.2, 14, { solid: false }); L.box(FU, 9.5, 0.4, 32.5, "ruin", 9.5, 9.2, 14, { solid: false });
    L.box(FU, 0.08, 0.08, 32, "neonC", -2.7, 9, 14, { solid: false }); L.box(FU, 0.08, 0.08, 32, "neonM", 4.7, 9, 14, { solid: false });
    L.crystals(FU, 8, 0, 16, 24, 20);
    L.box(FU, 4.2, 3.4, 1.4, "crystal", 0, 1.7, 29.4);
    L.label(FU, "SEALED BY CRYSTAL\n(try another state)", 0, 3.9, 28.2, Math.PI, 0.28, "#d0b8ff");
    L.light(FU, 0x9a60ff, 3, 10, 0, 2, 27); L.light(FU, 0xff40c8, 2, 18, 8, 5, 18);
    L.particles(FU, "motes", [26, 7, 26], [0, 0.5, 16], 300);
    L.holoMesh(FU, "box", "cyan", 0, 6.5, 16, 6, 1.4, 0.02, { bob: 0.08 }); L.label(FU, "DEPOT 4 // LAST SERVICE: 0", 0, 6.5, 15.95, Math.PI, 0.35, "#9fffff");
    L.ambient("drone", 0.28, 4);
    decoy(PR, [-9, 0.5, 11.5], [0.5, 1, 0.5], "Kick the loose rail clamp (decoy)", [2, 0, 19], "LOOSE CLAMP\n(decoy)", Math.PI);

    // ================================================================ crossing hall
    L.floor(S, 0, 31.75, 28, 3.5, 0, "concrete");
    L.floor(S, 0, 51, 28, 2, 0, "concrete");
    L.wallDoor(S, -14, 52, 14, 52, 7, "tile", 4, 3.4);
    L.box(S, 28.5, 0.4, 3.5, "concrete", 0, 7.2, 31.75, { solid: false });
    L.label(S, "LIVE LINES - USE THE REFUGES", 0, 4.4, 29.7, Math.PI, 0.45, "#ffd070", "rgba(10,10,10,.9)");
    L.label(S, "LINE 4 STATION  >>", 0, 4.2, 51.7, Math.PI, 0.45, COLD, "rgba(8,12,18,.9)");
    const TZ = [36, 42, 48];
    for (const x of [-14, 14]) {
      const seg = [[30, 34.2], [37.8, 40.2], [43.8, 46.2], [49.8, 52]];
      seg.forEach(([a, b]) => L.wall(S, x, a, x, b, 7, "tile"));
      TZ.forEach(z => { L.wall(S, x, z - 1.8, x, z + 1.8, 3, "tile", 0.4, 4); L.box(S, 0.3, 4, 3.6, "obsidian", x + Math.sign(x) * 0.4, 2, z, { solid: false }); L.block(x, z, 0.5, 3.6, 0, 4); L.label(S, "DANGER - LIVE LINE", x - Math.sign(x) * 0.3, 4.4, z, x < 0 ? HALF : -HALF, 0.22, "#ffd070"); });
    }
    [PA, PR].forEach(g => { L.floor(g, 0, 41.75, 28, 16.5, 0, "concrete"); L.box(g, 28.5, 0.4, 17, "tile", 0, 7.2, 42, { solid: false }); TZ.forEach(z => track(g, -14, z, 14, z)); [39, 45].forEach(z => { L.box(g, 27, 0.16, 1.8, "concrete", 0, 0.08, z); L.box(g, 27, 0.02, 0.1, "neonA", 0, 0.17, z - 0.85, { solid: false }); L.box(g, 27, 0.02, 0.1, "neonA", 0, 0.17, z + 0.85, { solid: false }); for (let k = 0; k < 3; k++) L.label(g, "REFUGE", -8 + k * 8, 0.19, z, 0, 0.4, "#ffd070").rotation.x = -HALF; }); });
    // Past: trams + STOP/GO signals.
    const trams = [[9, 2.5, false], [12, 3.5, true], [10, 1.5, false]].map(([sp, pa, rev], i) => tram(PA, rev ? [24, TZ[i]] : [-24, TZ[i]], rev ? [-24, TZ[i]] : [24, TZ[i]], 0, sp, pa, 10, i === 1 ? "red" : "ochre", "LINE " + (i + 2)));
    trams.forEach((tr, i) => [-12.5, 12.5].forEach(sx => {
      const z = TZ[i] - 2.4;
      L.cyl(PA, 0.07, 2.8, "iron", sx, 1.4, z, { seg: 8 });
      const stop = dyn(PA), go = dyn(PA, false);
      L.label(stop, "STOP", sx, 3.4, z, Math.PI, 0.4, "#ff6050"); L.mesh(stop, "box", "red2", sx + 0.45, 3, z, 0.1, 0.8, 0.06);
      L.label(go, "GO", sx, 3.4, z, Math.PI, 0.4, "#70ff9a"); L.mesh(go, "box", "green", sx + 0.65, 2.75, z, 0.8, 0.1, 0.06);
      L.label(PA, "TRACK " + (i + 1), sx, 2.3, z, Math.PI, 0.2, "#ffffff");
      L.update(() => { const near = Math.abs(tr.t.position.x - sx * 0) < 20; stop.visible = near; go.visible = !near; });
    }));
    L.light(PA, 0xffc890, 3, 26, 0, 6.6, 42);
    L.ambient("machine", 0.35, 1);
    // Present: collapse buries the crossing.
    L.box(PR, 28, 4.4, 1, "ruin", 0, 2.2, 33);
    for (let i = 0; i < 9; i++) L.box(PR, 2, 1.4, 1.6, "concrete", -12 + i * 3, 0.7 + (i % 3), 33.9, { solid: false, ry: i * 0.7 });
    L.label(PR, "COLLAPSE - LINE CLOSED", 0, 2.5, 32.4, Math.PI, 0.45, "#ffb070");
    L.lamp(PR, -12, 31.5, "sodium", 4);
    // Future: chasm + crystal bridge + resonance bursts.
    L.floor(FU, 10, 41.75, 2, 16.5, 0, "crystal");
    L.box(FU, 0.06, 0.06, 16.5, "neonC", 9, 0.03, 41.75, { solid: false }); L.box(FU, 0.06, 0.06, 16.5, "neonC", 11, 0.03, 41.75, { solid: false });
    L.box(FU, 28, 1, 17, "obsidian", 0, -14, 42, { solid: false });
    L.crystals(FU, 10, 0, 42, 26, 14, -13.5);
    L.light(FU, 0xff40c8, 4, 20, -5, -8, 42);
    L.particles(FU, "motes", [26, 8, 15], [0, -4, 42], 300);
    L.holoMesh(FU, "box", "magenta", 10, 3.2, 33.8, 3.6, 0.9, 0.02, { bob: 0.05 });
    L.label(FU, "RESONANCE: CROSS WHEN THE GLOW FADES", 10, 3.2, 33.75, Math.PI, 0.2, "#ffb0ff");
    [[38.5, 0], [45, 1.7]].forEach(([z, off]) => L.hazard({ group: FU, mask: 4, at: [10, 0, z], size: [2.2, 2.4, 3], idle: 2.4, warn: 1.3, active: 1.1, offset: off, color: 0xff40c8, msg: "The crystal rang. Wait for the magenta glow to fade, then cross." }));
    L.ambient("drone", 0.3, 4); L.ambient("wind", 0.15, 4);
    L.zone({ at: [0, 1.5, 51], size: [28, 3, 1.8], flag: "L04.crossedTracks", toast: "Across. The station is ahead.", lines: [line("The Witness", "The trains ran on time, every one of them. Only their passengers were late.")] });

    // ================================================================ station
    L.floor(S, -4.5, 65, 19, 26, 0, "tile"); L.floor(S, -10, 81, 8, 6, 0, "tile"); L.floor(S, 2.5, 81, 5, 6, 0, "tile");
    L.floor(S, 9.5, 76, 9, 48, -1.2, "concrete"); L.box(S, 0.3, 1.2, 32, "concrete", 5, -0.6, 68).position.y -= 0.012; // mesh under the tile edge (z-fighting) L.box(S, 0.4, 0.02, 32, "neonA", 4.6, 0.01, 68, { solid: false });
    track(S, 9.5, 52.5, 9.5, 100, -1.2);
    L.wall(S, -14, 52, -14, 84, 8, "tile"); L.wall(S, 14, 52, 14, 84, 9.2, "tile", 0.4, -1.2);
    L.wall(S, -14, 84, 7, 84, 8, "tile"); L.wall(S, 12, 84, 14, 84, 9.2, "tile", 0.4, -1.2); L.wall(S, 7, 84, 12, 84, 4.9, "tile", 0.4, 3.3);
    L.wall(S, 7, 84, 7, 104, 4.6, "concrete", 0.4, -1.2); L.wall(S, 12, 84, 12, 104, 4.6, "concrete", 0.4, -1.2); L.box(S, 5.6, 0.4, 20, "concrete", 9.5, 3.5, 94, { solid: false });
    L.label(S, "LINE 4  -  VEYRA DEEP", 13.7, 4, 68, -HALF, 0.8, "#f0f0ff", "rgba(8,12,18,.9)");
    // office
    L.floor(S, -11, 67, 6, 10, 0.02, "woodDark", 0.05);
    L.wallDoor(S, -8, 62, -8, 72, 3.2, "plasterCold", 1.6, 2.5); L.wall(S, -14, 62, -8, 62, 3.2, "plasterCold", 0.3); L.wall(S, -14, 72, -8, 72, 3.2, "plasterCold", 0.3);
    L.box(S, 6.3, 0.3, 10.3, "plasterCold", -11, 3.35, 67, { solid: false });
    L.box(S, 1.2, 0.8, 2.2, "wood", -12.2, 0.4, 67); L.box(S, 0.6, 1.6, 1, "iron", -13.5, 0.8, 63.2);
    L.label(S, "STATIONMASTER", -7.8, 2.85, 67, HALF, 0.25, WARM, "rgba(30,20,10,.9)");
    L.light(S, 0xe8dcc0, 1.3, 7, -11.5, 2.8, 67);
    L.box(PA, 0.12, 2.5, 1.6, "wood", -8, 1.25, 67); L.label(PA, "STAFF ONLY\nSTATIONMASTER", -7.9, 1.9, 67, HALF, 0.22, WARM);
    L.box(FU, 0.9, 2.6, 1.8, "crystal", -8, 1.3, 67);
    // booth + benches + cover
    L.box(S, 3, 1.1, 0.8, "woodDark", -11, 0.55, 57.5); L.box(S, 3, 2.8, 0.2, "woodDark", -11, 1.4, 55); L.box(S, 3.4, 0.2, 3, "woodDark", -11, 2.9, 56.3, { solid: false });
    L.label(S, "TICKETS", -11, 3.3, 57.9, 0, 0.35, WARM, "rgba(30,20,10,.9)");
    for (let i = 0; i < 3; i++) L.box(S, 0.6, 0.5, 3, "wood", -1, 0.25, 58 + i * 7);
    [[-9, 19], [3, 21], [12.3, 11], [-4, 27], [-10.5, 61], [-10.5, 75], [0.5, 62.5], [-4.5, 70.5], [1.6, 77.3]].forEach(([x, z]) => { L.box(S, 1.4, 1.3, 1.2, "woodDark", x, 0.65, z); L.box(S, 1.45, 0.08, 1.25, "rust", x, 0.9, z, { solid: false }); });
    [[-2.5, 57], [-2.5, 62], [1.8, 70], [-8, 9], [12.5, 20]].forEach(([x, z]) => L.box(S, 0.8, 8, 0.8, "tile", x, 4, z));
    for (let i = 0; i < 6; i++) pulseStrip(9.5, -1.2, 56 + i * 5);
    for (let i = 0; i < 4; i++) pulseStrip(4.6, 0.02, 58 + i * 6);
    sentinel("Station", [[-2, 58], [2, 64], [-5, 67], [-11, 67], [-5, 67], [-2, 72], [3, 72], [2, 60]], 30, 0.3);
    L.zone({ at: [0, 1.5, 53.2], size: [4, 3, 1.2], mask: 2, toast: "The machine is on this platform. CROUCH - the edge strips show how close it is." });
    L.checkpoint(0, 0, 53.5, Math.PI, { size: [5, 2.5, 2] });
    L.checkpoint(0, 0, 31, Math.PI, { size: [8, 2.5, 2] });

    // Present gameplay: the list, the cable, the decoys.
    L.clue({ group: PR, at: [-12.2, 0.86, 67], size: [0.45, 0.06, 0.6], prompt: "Take the evacuation list", flag: "L04.listRecovered", id: "L04.list", title: "Evacuation list - Line 4",
      text: "LINE 4 - PRIORITY EVACUATION, BY ORDER OF THE FOUNDERS' OFFICE.\nCars 1-6, 214 names. Children to the Nursery register. Board at the first bell. Do not announce.\n\nCar 3: ... OSKEL, Tamsin. OSKEL, Joss. OSKEL, Maren, age 7. ...\nCar 3: ... VALE, A., age 8. Guardian: I. Marrow. ...\n\nSigned: AUREL HALDEN, Founder.\nDated: 9 days BEFORE the date on every memorial in Veyra.",
      lines: [line("Arin", "Nine days early. Halden's signature. Maren Oskel, Car 3..."), line("Arin", "VALE, A. Age eight. Guardian: I. Marrow.", 4), line("Arin", "That's me. I was on this list. I was one of the children they hid.", 4.5),
        line("Arin", "Ilse Marrow was my guardian. 'Keep time for me.' The watch... it was hers to give.", 5), line("The Witness", "You were in Car 3, Arin. So was I. You hummed the whole way down.", 4.5)] });
    L.when(["L04.listRecovered"], () => { L.set("evidence.list"); L.set("arin.listedChild"); setTimeout(() => L.toast("In the FUTURE, an echo of Founder Halden waits by the lift.", 4), 8000); });
    L.box(PR, 0.6, 1.6, 1.2, "rust", -13.6, 1.3, 80); L.label(PR, "JUNCTION 4-B (DEAD)", -13.25, 2.3, 80, HALF, 0.18, "#ffb070");
    const cable = L.item({ group: PR, at: [-12.6, 0.25, 79.6], key: "cable_end", name: "live cable end", size: 0.45, mat: "copper" });
    const rope = new T3.Line(new T3.BufferGeometry().setFromPoints(Array.from({ length: 13 }, () => V(0, 0, 0))), new T3.LineBasicMaterial({ color: 0x202226 }));
    rope.userData.noEcho = true; rope.frustumCulled = false; PR.add(rope);
    const ropePts = rope.geometry.attributes.position, anchor = V(-13.25, 1.1, 80), endP = V(0, 0, 0);
    L.update(() => {
      if (!rope.visible) return;
      if (W.held && W.held.key === "cable_end") endP.set(P.pos.x, P.pos.y + 1, P.pos.z); else endP.copy(cable.mesh ? cable.mesh.position : cable.pos);
      for (let i = 0; i <= 12; i++) { const k = i / 12; ropePts.setXYZ(i, anchor.x + (endP.x - anchor.x) * k, anchor.y + (endP.y - anchor.y) * k - Math.sin(k * Math.PI) * 0.5, anchor.z + (endP.z - anchor.z) * k); }
      ropePts.needsUpdate = true;
    });
    L.box(PR, 0.8, 1, 0.8, "iron", -7.2, 0.5, 76.5);
    L.socket({ group: PR, at: [-7.2, 1.02, 76.5], accepts: "cable_end", flag: "L04.liftPowered", prompt: "Connect", radius: 0.4, hint: "The lift coupling takes the heavy cable plug." });
    L.label(PR, "LIFT POWER COUPLING\n[ plug here ]", -7.2, 1.8, 76.05, Math.PI, 0.2, "#ffd070");
    decoy(PR, [4.4, 1.35, 58], [0.4, 0.5, 0.4], "Pull the gantry release (pipe drops on the far rail)", [9.5, -1.1, 80], "GANTRY RELEASE\nfar rail >> north", -HALF);
    decoy(PR, [-13.6, 1.2, 72.8], [0.3, 0.8, 0.8], "Rattle the vent grille (decoy)", [-2, 0, 54], "VENT\n(decoy)", HALF);
    L.box(PR, 9, 0.3, 0.3, "rust", 9.5, 3.2, 80, { solid: false }); L.cyl(PR, 0.15, 3, "rail", 9.5, 2.6, 80).rotation.z = HALF;
    L.label(PR, "FAR RAIL", 9.5, 0.4, 80, -HALF, 0.25, "#ffc070");
    L.zone({ at: [-10.5, 1.5, 12.5], size: [3, 3, 2], mask: 2, toast: "Decoys are limited: every pipe, clamp and vent comes from the same supply." });
    // Keepers (hunters with lanterns).
    const keeperOpts = { mode: "hunter", group: PR, disabledBy: "L04.climax", caught: "A Keeper caught you. The machine hears, the Keepers see: crouch behind cover, or shift to the PAST to hide from both.", spot: ["Lantern up - something on the rails.", "Who's down here?"], alarm: ["Anomaly! Under the street!", "There - stop them!"] };
    L.keeper(Object.assign({ name: "Keeper", at: [10, 8], path: [[10, 8], [10, 27], [4, 27], [4, 14]] }, keeperOpts));
    L.keeper(Object.assign({ name: "Keeper", at: [-5, 57], path: [[-5, 57], [-6.5, 74], [-12, 76.5], [-6.5, 74]] }, keeperOpts));
    L.keeper(Object.assign({ name: "Keeper", at: [3.5, 80], yaw: Math.PI, path: [[3.5, 80], [3.5, 56]] }, keeperOpts));
    // Present dressing.
    L.box(PR, 28.5, 0.4, 32.5, "concrete", 0, 8.2, 68, { solid: false }); L.box(PR, 8.8, 0.04, 32, "water", 9.5, -0.9, 68, { solid: false });
    [[-5, 60], [0, 70], [-9, 76]].forEach(([x, z]) => L.box(PR, 3, 0.02, 2.2, "puddle", x, 0.02, z, { solid: false }));
    L.lamp(PR, -13.3, 60, "sodium", 5);
    L.particles(PR, "drips", [12, 8, 20], [-3, 0.5, 70], 200);
    L.ambient("water", 0.2, 2);
    // Past dressing.
    L.box(PA, 28.5, 0.4, 32.5, "tile", 0, 8.2, 68, { solid: false });
    L.light(PA, 0xffc890, 3, 20, -6, 7, 58); L.light(PA, 0xffc890, 3, 20, 2, 7, 72);
    for (let i = 0; i < 4; i++) { L.box(PA, 0.04, 1.4, 1, i % 2 ? "teal" : "red", -13.7, 2.2, 60 + i * 5, { solid: false }); L.label(PA, i % 2 ? "LINE 4\nOPENS SOON" : "FESTIVAL\nSPECIALS", -13.65, 2.2, 60 + i * 5, HALF, 0.16, "#fff0d0"); }
    tram(PA, [9.5, 98], [9.5, 66], -1.2, 7, 7, 10, "teal", "LINE 4");
    L.crowd(PA, -2.5, 64, 9, 14, 8);
    L.ambient("crowd", 0.3, 1);
    // Future dressing.
    L.box(FU, 12.5, 0.4, 32.5, "ruin", -8, 8.2, 68, { solid: false }); L.box(FU, 8.5, 0.4, 32.5, "ruin", 10, 8.2, 68, { solid: false });
    L.crystals(FU, 6, -6, 66, 14, 26);
    L.holoMesh(FU, "box", "cyan", 0, 4.5, 66, 5, 2.2, 0.02, { bob: 0.06 });
    L.label(FU, "LINE 4 // DEPARTURES\nCAR 1-6 ..... EVACUATED\nALL OTHERS ..... 0", 0, 4.5, 65.95, Math.PI, 0.28, "#9fffff");
    L.holoMesh(FU, "torus", "magenta", -3, 6, 81, 5, 5, 1, { spin: 0.4 }).rotation.x = HALF;
    L.label(FU, "TERMINUS : DROWNED DISTRICT", -3, 6.6, 78.4, 0, 0.3, "#ffa0e8");
    L.light(FU, 0x40d8ff, 2.4, 20, -6, 5, 64);
    L.particles(FU, "motes", [26, 7, 30], [0, 0.5, 68], 300);
    L.ambient("drone", 0.3, 4);

    // ================================================================ Founder Halden's echo (Future)
    const hal = L.npc({ name: "", named: false, group: FU, at: [2.6, 73.5], yaw: Math.PI, holo: "amber", route: [[2.6, 73.5, 999, "talk"]] });
    L.label(FU, "FOUNDER AUREL HALDEN (echo)", 2.6, 2.45, 73.5, Math.PI, 0.22, WARM);
    L.holoMesh(FU, "cyl", "cyan", 2.6, 0.05, 73.5, 1.2, 0.02, 1.2, { spin: 0.8 });
    L.talkPoint({ at: [2.6, 1, 73.5], mesh: hal.p.group, mask: 4, prompt: "Face Founder Halden's echo", requires: ["L04.listRecovered"] }, { speaker: "Founder Aurel Halden", question: "The Founder's echo waits for an answer.",
      opening: [line("Founder Aurel Halden", "You found my list. Then you know what I knew: the three states were grinding into one.", 4.5), line("Founder Aurel Halden", "Two hundred and fourteen seats. A city of forty thousand. What would you have announced?", 4.5), line("Founder Aurel Halden", "Panic would have killed more than the collapse. I chose silence. Silence saved the ones it could.", 5)],
      options: [{ text: "You decided who lived.", sets: "L04.haldenAccused", lines: [line("Founder Aurel Halden", "I decided that anyone lived. Somebody had to hold the pen."), line("Arin", "And then you erased their families so nobody would ask.")] },
        { text: "Why hide the families afterwards?", sets: "L04.haldenAsked", lines: [line("Founder Aurel Halden", "Grief asks questions. Questions open doors. Doors let the collapse in."), line("Founder Aurel Halden", "My daughter keeps the night repeating for the same reason. Ask Ondine.")] },
        { text: "My name is on your list.", requires: ["arin.listedChild"], sets: "L04.haldenNamed", lines: [line("Founder Aurel Halden", "...Vale. Ilse's ward. Then you were one I chose."), line("Founder Aurel Halden", "Don't make me regret it."), line("Arin", "You don't get to regret me.")] }],
      doneFlag: "L04.haldenDone" });

    // ================================================================ lift shaft + climax
    L.wall(S, -6, 78, -6, 84, 7, "iron", 0.3); L.wall(S, 0, 78, 0, 84, 7, "iron", 0.3); L.wallDoor(S, -6, 78, 0, 78, 7, "iron", 3.4, 2.7, 0.5, 0.3);
    [L.wall(S, -6, 78, -6, 84, 6.5, "concrete", 0.3, -6.5), L.wall(S, 0, 78, 0, 84, 6.5, "concrete", 0.3, -6.5), L.wall(S, -6, 78, 0, 78, 6.5, "concrete", 0.3, -6.5)]
      .forEach(m => { m.position.y -= 0.012; }); // shaft wall tops were coplanar with the tiled floor (mesh only)
    L.wallDoor(S, -6, 84, 0, 84, 6, "concrete", 3, 2.8, 0.5, 0.3, -6);
    L.label(S, "LIFT - DEEP LEVEL", -3, 3.4, 77.8, Math.PI, 0.35, COLD, "rgba(8,12,18,.9)");
    L.door(S, 3.4, 2.7, 0.12, "rust", -3, 0, 78, ["L04.liftPowered"], [0, 2.75, 0], { toast: "Power hums through the coupling. The lift gate grinds open." });
    L.checkpoint(-3, 0, 74.4, Math.PI, { climax: true, size: [4, 2.5, 1.2] });
    let liftY = 0, riding = false;
    const deck = L.box(S, 5.6, 0.4, 5.6, "iron", -3, -0.2, 81, { dynamic: true });
    L.mover(deck, (t, dt, m) => { m.position.y = -0.2 - liftY; });
    const car = dyn(S);
    for (const [x, z, w, d] of [[-5.6, 81, 0.08, 5.4], [-0.4, 81, 0.08, 5.4], [-3, 83.6, 5.4, 0.08]]) L.mesh(car, "box", "rail", x, 0.55, z, w, 1.1, d);
    L.mesh(car, "box", "neonA", -3, 0.01, 78.5, 5.4, 0.02, 0.3);
    const ctrlMesh = L.mesh(car, "box", "iron", -1.2, 0.6, 79.4, 0.5, 1.2, 0.5);
    L.label(car, "DESCEND\n[ USE ]", -1.2, 1.45, 79.7, 0, 0.2, "#9fffff");
    const ctrlPos = V(-1.2, 0.6, 79.4);
    // debris halves on the car (warn light + plate), alternating
    const halves = [-4.4, -1.6].map((x, i) => {
      const plate = new T3.MeshBasicMaterial({ color: 0xff3020, transparent: true, opacity: 0, depthWrite: false });
      L.mesh(car, "box", plate, x, 0.03, 81, 2.6, 0.02, 5.2);
      const rock = L.mesh(car, "box", "concrete", x, 3, 81, 1.2, 0.7, 1);
      rock.visible = false;
      return { x, plate, rock, light: L.light(car, 0xff2010, 0, 6, x, 2.6, 81), phase: "idle", timer: 2.2 - i * 2.1 };
    });
    L.update((dt, t) => {
      car.position.y = -liftY; ctrlPos.y = 0.6 - liftY;
      if (riding) {
        liftY = Math.min(6, liftY + dt * 6 / 10);
        halves.forEach(h => {
          h.timer -= dt;
          if (h.timer <= 0) { if (h.phase === "idle") { h.phase = "warn"; h.timer = 1.3; L.sfx("warn", 0.7); } else if (h.phase === "warn") { h.phase = "active"; h.timer = 0.7; L.sfx("hazard", 0.8); } else { h.phase = "idle"; h.timer = 2.2; } }
          h.light.intensity = h.phase === "warn" ? 3 * (0.5 + 0.5 * Math.sin(t * 24)) : h.phase === "active" ? 4 : 0;
          h.plate.opacity = h.phase === "warn" ? 0.35 : h.phase === "active" ? 0.6 : 0;
          h.rock.visible = h.phase === "active"; h.rock.position.y = 0.6;
          if (h.phase === "active" && !P.dying && Math.abs(P.pos.x - h.x) < 1.5 && P.pos.z > 78 && P.pos.z < 84) AZ.game.die("Calder's debris hit you. The red light shows which half of the car is about to be struck.");
        });
        if (liftY >= 6) { riding = false; halves.forEach(h => { h.light.intensity = 0; h.plate.opacity = 0; h.rock.visible = false; }); L.set("L04.liftArrived"); L.sfx("clang"); L.toast("The lift slams to a stop. The air smells of floodwater.", 3); }
      }
    });
    W.l04 = { safeX: () => { const h = halves.find(q => q.phase !== "idle"); return h ? (h.x < -3 ? -1.6 : -4.4) : P.pos.x; }, top: () => -liftY, riding: () => riding };
    L.interact({ at: ctrlPos, mesh: ctrlMesh, prompt: "Descend to the Drowned District", requires: ["L04.liftPowered", "L04.listRecovered", "L04.circuitHeld"], locked: "The lift won't move. It needs power, the evacuation list - and both circuit breakers thrown.",
      onUse: () => { if (riding || L.get("L04.liftArrived")) return; riding = true; L.set("L04.descending"); L.sfx("rule"); L.toast("Calder is wrecking the shaft! Watch the red light - move to the safe half of the car.", 4);
        L.say([line("Arin", "Down, then. Wherever they sent the people who weren't on the list."), line("The Witness", "The water keeps what the city forgot. Go gently.")]); } });
    AZ.on("respawn", () => { if (riding || (L.get("L04.descending") && !L.get("L04.liftArrived"))) { riding = false; liftY = 0; L.set("L04.descending", false); halves.forEach(h => { h.phase = "idle"; h.timer = 2.2; h.rock.visible = false; h.plate.opacity = 0; h.light.intensity = 0; }); } });

    L.zone({ at: [-3, 1.5, 76.6], size: [3.4, 3, 1.2], flag: "L04.climax", requires: ["L04.liftPowered", "L04.listRecovered", "L04.haldenDone"] });
    const cut = dyn(S, false);
    L.label(cut, "CIRCUIT BROKEN\nthrow breakers A + B", -7.2, 2.2, 76.05, Math.PI, 0.2, "#ff7060");
    L.particles(cut, "embers", [0.4, 0.6, 0.4], [-7.2, 1.1, 76.5], 60);
    const calder = L.pursuer({ name: "Brother Calder", group: S, at: [-7, 66.5], lane: [[-5.5, 70]], startFlag: "L04.calderTalked", stopFlag: "L04.descending", speed: 4.4, delay: 2.5,
      barks: ["Round again, anomaly!", "The hour keeps everyone!", "I'll put you back on the list - the other one."], caught: "Calder caught you. Keep moving between the breakers - and don't sprint past the machine." });
    L.label(calder.p.group, "BROTHER CALDER", 0, 2.4, 0, 0, 0.24, "#ff8070");
    calder.mask = 0;
    L.when(["L04.climax"], async () => {
      calder.mask = 7;
      await L.dialogue({ speaker: "Brother Calder", question: "Brother Calder has the lift cable in his fist.",
        opening: [line("Brother Calder", "Remember me? The clock tower. You slipped out of a door I'd locked.", 4), line("Brother Calder", "Mother Ondine says the night is mercy. I say it's the only order this city has.", 4.5), line("Brother Calder", "That list stays down here. And so do you.", 3.5)],
        options: [{ text: "Let me through, Calder.", sets: "L04.calderAsked", lines: [line("Brother Calder", "Through? There's no through. There's only round again."), line("Arin", "Then I'll make one.")] },
          { text: "Ilse Marrow stopped the tower. You knew.", sets: "L04.calderIlse", lines: [line("Brother Calder", "The old clockkeeper broke the bell for a handful of children. We fixed what she broke."), line("Arin", "You mean you made it ring again.")] },
          { text: "My name is on Halden's list.", requires: ["arin.listedChild"], sets: "L04.calderNamed", lines: [line("Brother Calder", "...Then you're the one she hid. Mother will want you alive. I won't."), line("Arin", "Good to know where we stand.")] }],
        doneFlag: "L04.calderTalked" });
    });
    L.when(["L04.calderTalked"], () => { rope.visible = false; cut.visible = true; L.sfx("alert"); L.toast("Calder tears the cable loose! Throw breakers A and B to hold the lift circuit.", 4); });
    L.when(["L04.liftArrived"], () => { calder.mask = 0; });
    [["A", -13.55, 75, HALF, "L04.breakerA"], ["B", 4.55, 74, -HALF, "L04.breakerB"]].forEach(([id, x, z, ry, flag]) => {
      L.box(PR, 0.3, 0.9, 0.7, "iron", x, 1.2, z);
      const lx = x + Math.sin(ry) * 0.25, lz = z + Math.cos(ry) * 0.25;
      const lev = L.box(dyn(PR), 0.45, 0.5, 0.45, "red2", lx, 1.2, lz, { solid: false });
      L.label(PR, "BREAKER " + id, lx + Math.sin(ry) * 0.05, 1.95, lz, ry, 0.25, "#ffd070");
      L.interact({ at: [lx, 1.2, lz], mesh: lev, mask: 2, once: true, prompt: "Throw breaker " + id, requires: ["L04.calderTalked"], locked: "Breaker " + id + ". The lift circuit is fine for now.",
        onUse: () => { lev.position.y -= 0.25; L.set(flag); L.sfx("clang"); } });
    });
    L.when(["L04.breakerA", "L04.breakerB"], () => { L.set("L04.circuitHeld"); L.sfx("rule"); L.toast("Circuit held. Get in the lift - DESCEND!", 3.5); });

    // ================================================================ deep corridor + exit
    L.floor(S, -3, 81, 6, 6, -6.4, "concrete");
    L.floor(S, -3, 90, 4.4, 12, -6, "concrete");
    L.wall(S, -5.2, 84, -5.2, 96, 3.5, "tile", 0.3, -6); L.wall(S, -0.8, 84, -0.8, 96, 3.5, "tile", 0.3, -6); L.box(S, 4.8, 0.4, 12, "tile", -3, -2.3, 90, { solid: false });
    L.box(S, 4.4, 3.5, 0.3, "glass", -3, -4.25, 96.2);
    L.box(S, 30, 0.05, 16, "water", -3, -6.25, 104, { solid: false }); L.box(S, 30, 0.4, 16, "ground", -3, -8, 104, { solid: false });
    L.box(S, 8, 10, 6, "stoneWet", -12, -3, 108, { solid: false }); L.box(S, 9, 12, 6, "stoneWet", 6, -2, 110, { solid: false });
    L.label(S, "OLD VEYRA FLOOD BASIN", -3, -3.2, 95.9, Math.PI, 0.35, "#a0f0ff");
    L.light(S, 0x50d8e8, 3, 18, -3, -4, 100); L.light(S, 0xc0e0ff, 1.5, 9, -3, -3, 88);
    L.exit({ group: S, at: [-3, -6, 93.5], size: [4, 2.5, 1.5], requires: ["L04.liftPowered", "L04.listRecovered", "L04.liftArrived"], locked: "The way down is closed until the lift is powered and the evacuation list is recovered." });

    // ================================================================ signal box (side area)
    L.floor(S, 18.1, 22, 7.7, 10, 0, "woodDark");
    L.wall(S, 14.25, 17, 22, 17, 3.6, "brick", 0.3); L.wall(S, 14.25, 27, 22, 27, 3.6, "brick", 0.3); L.wall(S, 22, 17, 22, 27, 3.6, "brick", 0.3);
    L.box(S, 8, 0.3, 10.3, "woodDark", 18.1, 3.75, 22, { solid: false });
    L.label(S, "SIGNAL BOX 4", 13.7, 3.2, 22, -HALF, 0.3, WARM, "rgba(30,20,10,.9)");
    L.box(S, 0.8, 0.8, 6, "iron", 21.2, 0.4, 22);
    ["DEPOT", "XING 1", "XING 2", "XING 3", "PLATFORM", "DEEP LIFT", "SPARE", "SPARE"].forEach((n, i) => { L.box(S, 0.08, 0.9, 0.08, i === 5 ? "red2" : "rail", 21, 1.2, 19.4 + i * 0.75, { solid: false }); L.label(S, (i + 1) + "\n" + n, 20.75, 0.9, 19.4 + i * 0.75, -HALF, 0.1, "#ffecc0"); });
    L.box(S, 0.05, 1, 5, "paper", 21.82, 2.4, 22, { solid: false });
    L.label(S, "DEPOT == XING (3) == STATION == DEEP LIFT\nSIX CARS HELD AT DEEP LIFT - FOUNDERS' ORDER", 21.75, 2.4, 22, -HALF, 0.14, "#302010");
    L.box(S, 2, 0.8, 0.9, "wood", 16.5, 0.4, 25.6);
    L.clue({ group: S, at: [16.5, 0.85, 25.6], size: [0.5, 0.08, 0.45], mat: "red", prompt: "Read Ignatz's logbook", flag: "L04.logbook", id: "L04.logbook", title: "Conductor's logbook - I. Marrow",
      text: "Day -9. Order under Founder Halden's seal: hold six cars at the deep lift every night. No passengers. 'Practice.'\n\nDay -6. Season passes printed for 214. Children's fares mostly. Wren asked me why. I said I don't ask.\n\nDay -2. Ilse came down from the tower, still with that carved zero in her pocket. Made me promise: whatever happens, keep the deep lift powered.\n\nDay -1. Cars full by the first bell. I drove them down. I didn't look at the faces. I wish I had.",
      lines: [line("Arin", "He drove the evacuation trains and never knew what they were for.")] });
    L.light(PA, 0xffc890, 2, 9, 18, 3, 22);
    L.box(PR, 3, 0.02, 2, "puddle", 18, 0.02, 20, { solid: false });
    L.crystals(FU, 3, 18, 22, 6, 8);
    L.holoMesh(FU, "box", "amber", 18, 2, 22, 1.6, 0.9, 0.02, { bob: 0.05 }); L.label(FU, "CARS 1-6 : DEEP LIFT\nDEPART : FIRST BELL", 18, 2, 21.95, Math.PI, 0.18, "#ffd090");

    // ================================================================ NPCs
    L.npc({ name: "Ignatz Marrow", group: PA, at: [3.5, 64], yaw: HALF, coat: "navy", task: "Conductor (Ilse's brother): waves the Line 4 tram in and out and keeps passengers back from the edge.",
      route: [[3.8, 64, 6, "look"], [3.8, 74, 4, "work"], [-4, 54, 3, "talk"], [3.8, 58, 4, "work"]], approach: ["Ticket ready? No? Then mind the edge.", "You look like you walked here from a funeral."],
      talk: [line("Ignatz Marrow", "Thirty years on these rails. You feel a tram coming in your boots before you hear it."), line("Ignatz Marrow", "My sister Ilse keeps the clock tower. I keep the trams. Between us, Veyra is never late."),
        line("Ignatz Marrow", "Nine days back the Founders' office had me run six empty cars down to the deep lift. Every night. Practice, they said."), line("Ignatz Marrow", "Halden's seal on the order, and that's the end of asking."),
        line("Ignatz Marrow", "I write it all in my logbook in the signal box off the depot."), line("Ignatz Marrow", "On the crossing, watch the signals. STOP with the arm up means a car's close.")],
      cond: [{ flag: "arin.listedChild", lines: [line("Ignatz Marrow", "Vale? Ilse's ward is a Vale. Quiet little thing. Hums when it's frightened."), line("Ignatz Marrow", "She had a watch engraved for that child. 'For A. - keep time for me.' ...Why are you looking at me like that?")] },
        { flag: "L04.logbook", lines: [line("Ignatz Marrow", "You read my log? Six cars, the first bell, and no passengers yet."), line("Ignatz Marrow", "Ilse made me promise the deep lift stays powered. Wouldn't say why.")] },
        { flag: "L04.pulsesRead", lines: [line("Ignatz Marrow", "Reading the rails? The maintenance unit does the same. Blind as a bell, ears like a cat.")] }],
      changedFlag: "L04.listRecovered", changedRoute: [[-7, 67, 9, "look"], [-4, 70, 4, "idle"], [3.8, 66, 6, "look"]], changedTalk: [line("Ignatz Marrow", "I keep looking at that office door. Nine days. You said nine days.")] });
    L.npc({ name: "Wren Castell", group: PA, at: [-11, 56.4], coat: "ochre", task: "Ticket seller: sells festival passes and carries the day's dockets to the stationmaster.",
      route: [[-11, 56.4, 7, "work"], [-12.5, 61, 3, "carry"], [-9, 66, 3, "carry"], [-11, 56.4, 5, "work"]], approach: ["Single or return?", "Festival passes, half price before the bell!"],
      talk: [line("Wren Castell", "The office? Staff only. The stationmaster keeps the rosters locked up tight."), line("Wren Castell", "Two hundred and fourteen season passes came down from the Founders' office. Not one paid for."),
        line("Wren Castell", "Little Maren Oskel came through with her mother yesterday. Humming. She asked if the deep lift goes to the sea."), line("Wren Castell", "Ignatz says I worry too much. Ignatz also talks to his trams."),
        line("Wren Castell", "He's been odd lately. Counting seats on Line Four, which hasn't even opened.")],
      cond: [{ flag: "L04.listRecovered", lines: [line("Wren Castell", "Two hundred and fourteen names, sorted by car. Founders' friends - and children."), line("Wren Castell", "Where's the list for everyone else?")] },
        { flag: "evidence.record", lines: [line("Wren Castell", "Maren Oskel, dead? I sold her mother a child's fare for Car 3. She's on a list, not in a grave.")] },
        { flag: "L04.crossedTracks", lines: [line("Wren Castell", "You came over the crossing on foot? Ignatz will have your ears.")] }],
      changedFlag: "L04.listRecovered", changedRoute: [[-3, 76.5, 8, "look"], [-11, 58.5, 5, "idle"], [-6, 70, 4, "look"]], changedTalk: [line("Wren Castell", "I closed the booth. I can't sell anyone a ticket knowing there's a list.")] });
    L.npc({ name: "Dace Dunmore", group: PR, at: [-13, 53.5], yaw: 0.8, coat: "olive", speed: 0.9, task: "Line-walker (a Rememberer): strips copper from the dead line while hiding from the machine.",
      route: [[-13, 53.5, 8, "look"], [-12.6, 59, 5, "work"], [-13, 54.5, 6, "carry"]], approach: ["Get down! Low, low - it hears you.", "Don't stamp. It'll feel you."],
      talk: [line("Dace Dunmore", "That machine tracks the rails. Crouch, keep slow, and it'll pass you like a ghost."), line("Dace Dunmore", "Name's Dace. I strip copper down here. Nobody else is stupid enough."),
        line("Dace Dunmore", "My gran said the trams ran full the week before the bell. Then empty forever after."), line("Dace Dunmore", "The machine checks the stationmaster's office every round, like it's guarding something."),
        line("Dace Dunmore", "The gantry by the platform edge drops a pipe on the far rail. Bang! It runs to look. Don't waste them.")],
      cond: [{ holding: "cable_end", lines: [line("Dace Dunmore", "Careful - that end's still live! The lift coupling's on the pedestal by the shaft. North end.")] },
        { flag: "L04.listRecovered", lines: [line("Dace Dunmore", "Halden's list? My gran was on the platform that day. She wasn't on any list."), line("Dace Dunmore", "So that's what the Founders did. Picked. And the rest of us got an earthquake.")] },
        { flag: "evidence.record", lines: [line("Dace Dunmore", "You got Odile's record past Ondine? Pell owes me a bottle, then.")] }],
      changedFlag: "L04.falseVibration", changedRoute: [[3.6, 60, 8, "look"], [0, 56, 5, "work"], [-13, 53.5, 5, "idle"]], changedTalk: [line("Dace Dunmore", "You fooled it once. It'll come back. They always come back to the route.")] });

    // ================================================================ player + fragments
    L.player(2, 4, 1, Math.PI);
    L.checkpoint(2, 4, 1, Math.PI, { size: [6, 2.5, 3] });
    L.fragment("L04.1", [15.3, 1.2, 18], "The signalman's tea", "PAST. A signalman, to Ignatz: 'Six cars held at the deep lift again. You think the Founders know something we don't?' Ignatz: 'I think the Founders know everything we don't. Pour the tea.'", S);
    L.fragment("L04.2", [-12.2, 1.2, 56], "Season pass", "PAST. Wren, counting: 'Two hundred and fourteen. Halden, Oskel, Vale, Lund... Lantern Row gets twelve. Our street gets none.'", S);
    L.fragment("L04.3", [-13, 1.2, 29], "Last car", "PRESENT. A depot worker, old now: 'I saw the last car go down full of children, every one holding a ticket. I was told it was a festival outing.'", S);
  },
});
