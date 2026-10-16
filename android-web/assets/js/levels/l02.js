/* LEVEL 02 - Under the Clock Skin (Founders' Clockworks, inside the clock tower).
   Port of the Unity Level02Builder (Design v2): wedge (PAST) -> drain (PRESENT) -> refill countdown while climbing the
   PAST gear wheel -> steam gallery -> Brother Calder cuts the power and seals the bell stairs -> restart the PAST
   clockwork -> PAST counterweight lift -> FUTURE beam in telegraphed gusts -> draw Calder's bolt in the PAST ->
   Ilse Marrow's recording ("Keep time for me, my little A.") -> the Rain Archive.
   Tower interior x/z -12..12. Landing z<-7 (y 1.5), pit y -2.5, gallery x 7..12 (y 7), bell floor / chamber y 13. */
AZ.registerLevel({
  id: "L02", order: 2, chapter: "LEVEL 02", title: "Under the Clock Skin", location: "Founders' Clockworks",
  start: 1, allowed: 3,
  build(L) {
    const { line, THREE } = L, S = L.shared, PA = L.past, PR = L.present, FU = L.future;
    const F = {
      wedge: "L02.wedgeRemoved", valve: "L02.valveOpened", drained: "L02.chamberDrained", gallery: "L02.galleryReached",
      bell: "L02.bellFloorReached", cut: "L02.calderCut", powerOut: "L02.powerOut", lever: "L02.leverPulled", restarted: "L02.clockworkRestarted",
      beam: "L02.beamCrossed", bolt: "L02.calderBoltDrawn", recPlayed: "L02.recordingPlayed", rec: "L02.recordingHeard", archive: "L02.toArchive",
    };
    const dynGroup = parent => { const g = new THREE.Group(); g.userData.dynamic = true; parent.add(g); return g; };

    L.look({ fog: 0.55, past: { density: 0.01, hemiI: 1.1, exp: 1.15 }, present: { density: 0.018, hemiI: 1.15, exp: 1.3 }, future: { density: 0.014, hemiI: 0.9, exp: 1.25 } });

    L.intro(line("Arin", "The clockworks. Flooded right up to the stair."),
      line("The Witness", "In the PAST this tower was whole. In the PRESENT it drowns. There is a third state, Arin. Not yet."),
      line("Arin", "Then I start where it was whole. Something down in that pit is holding the water."));
    L.objective("Remove the wedge from the great gear in the PAST", F.wedge, "Shift to the PAST. The pit is dry there - go down the stairs to the great gear on the west wall.");
    L.objective("Drain the flooded chamber in the PRESENT", F.drained, "With the gear free, return to the PRESENT and open the drain valve on the landing.");
    L.objective("Climb before the flood returns: ramp, then the PAST gear wheel", F.gallery, "In the PRESENT take the broken ramp to the mid platform. Shift to the PAST and step onto a gear-wheel platform when it comes level - ride it up to the gallery.");
    L.objective("Climb past the steam vents to the bell stairs", F.bell, "The vents hiss and glow red before they blow. Cross while they are dark - or cross in a state where they do not exist.");
    L.objective("Calder cut the power: restart the PAST clockwork", F.restarted, "Back at the north end of the gallery, in the PAST, pull the clockwork engagement lever.");
    L.objective("Ride the PAST lift, then cross the gusting beam in the FUTURE", F.beam, "Ride the counterweight lift to the escapement ledge. Shift to the FUTURE and cross between gusts - banners whip and the wind howls first. Wait at the shelter.");
    L.objective("Draw Calder's bolt from the keeper's chamber in the PAST", F.bolt, "Inside the chamber, shift to the PAST: the bolt winch is only intact there.");
    L.objective("Recover the clockkeeper's recording", F.rec, "The recording is in the bell console on the bell floor. Cross the gap on the FUTURE beam.");
    L.objective("Reach the Rain Archive", F.archive, "The keeper's door in the north wall of the chamber leads out to the archive bridge.");
    L.payoff(F.rec, "Stopped From Inside", "There was no earthquake. Ilse Marrow, the Founders' clockkeeper, stopped the tower herself - her own wedge in the great gear, between the bell's first and second stroke - to buy time for the children caught between the states. Founder Halden ordered silence, flooded the pit and called it a quake. And she called someone 'my little A.'");

    L.player(0, 1.5, -10.5, Math.PI);
    L.checkpoint(0, 1.5, -10.5, Math.PI, { size: [6, 2.5, 3] });

    // ---------------- shell ----------------
    const wall = (x, y, z, w, h, d, futTop) => {
      L.box(PA, w, h, d, "brick", x, y, z); L.box(PR, w, h, d, "stoneWet", x, y, z);
      const b = y - h / 2, top = Math.min(y + h / 2, futTop);
      L.box(FU, w, top - b, d, "obsidian", x, (b + top) / 2, z);
      L.box(FU, w > d ? w : d + 0.1, 0.06, w > d ? d + 0.1 : d, "neonC", x, top + 0.04, z, { solid: false, noEcho: true });
    };
    wall(-10.5, 13.5, -12.5, 5, 33, 1, 18); wall(3.5, 13.5, -12.5, 19, 33, 1, 18); wall(-7, -0.75, -12.5, 2, 4.5, 1, 18); wall(-7, 17.05, -12.5, 2, 25.9, 1, 18);
    wall(12.5, 13.5, 0, 1, 33, 24, 23); wall(-12.5, 13.5, 0, 1, 33, 24, 17);
    wall(-10.875, 13.5, 12.5, 4.25, 33, 1, 19); wall(3.375, 13.5, 12.5, 19.25, 33, 1, 20); wall(-7.5, 5, 12.5, 2.5, 16, 1, 19); wall(-7.5, 22.8, 12.5, 2.5, 14.4, 1, 19);
    L.box(PA, 26, 0.5, 26, "woodDark", 0, 30.2, 0, { solid: false }); L.box(PA, 6, 0.1, 6, "window", 0, 29.9, 0, { solid: false });
    L.box(PR, 26, 0.5, 10, "iron", 0, 30.2, 8, { solid: false }); L.box(PR, 26, 0.5, 10, "iron", 0, 30.2, -8, { solid: false });
    for (let i = 0; i < 3; i++) { L.box(PA, 0.1, 5, 2, "window", 11.95, 21, -6 + i * 6, { solid: false }); L.box(PR, 0.1, 5, 2, "windowCold", 11.95, 21, -6 + i * 6, { solid: false }); }
    const faceP = L.cyl(PA, 5, 0.15, "window", 0, 22, -11.9); faceP.rotation.x = Math.PI / 2;
    const faceN = L.cyl(PR, 5, 0.15, "windowCold", 0, 22, -11.9); faceN.rotation.x = Math.PI / 2;
    const faceF = L.holoMesh(FU, "cyl", "cyan", 0, 22, -10.5, 10, 0.05, 10); faceF.rotation.x = Math.PI / 2;
    const ringF = L.holoMesh(FU, "torus", "magenta", 0, 22, -10.3, 11.5, 11.5, 1); L.update(dt => { ringF.rotation.z += dt * 0.2; });
    L.label(S, "0", 0, 25.5, -11.7, 0, 1.6, "#2a1a0a");
    L.label(FU, "VEYRA STANDARD TIME  00:00:00", 0, 16.5, -10.6, 0, 0.45, "#ff90e0");

    // ---------------- floors ----------------
    L.floor(S, 0, 2.5, 24, 19, -2.5, "stone");
    L.box(PR, 22, 0.02, 16, "puddle", 0, -2.48, 3, { solid: false });
    L.box(S, 24, 4.5, 5, "stone", 0, -0.75, -9.5);
    L.box(PR, 24, 0.04, 0.3, "neonA", 0, 1.52, -7.15, { solid: false }); L.box(FU, 24, 0.04, 0.3, "neonC", 0, 1.52, -7.15, { solid: false });
    L.stairs(S, [0, 1.5, -7], [0, -2.5, -1], 3, "wood");
    L.box(S, 10.4, 1, 0.12, "brass", -6.8, 2, -7.1); L.box(S, 10.4, 1, 0.12, "brass", 6.8, 2, -7.1);
    // gallery (east), rail gap at z 5..7 where the PAST gear wheel arrives
    L.floor(S, 9.5, -1.8, 5, 20.4, 7, "wood");
    L.box(S, 0.12, 1, 14, "brass", 7.05, 7.5, -2.2); L.box(S, 0.12, 1, 1.4, "brass", 7.05, 7.5, 7.7);
    for (let i = 0; i < 4; i++) L.box(S, 0.4, 8.6, 0.4, "woodDark", 7.4, 2.2, -6 + i * 4.6);
    L.floor(S, 6.45, 6, 1.1, 1.8, 7, "brass");
    // escapement ledge (y 13) above the gallery, reached only by the PAST lift
    L.floor(S, 9.5, 6.5, 5, 3.8, 13, "wood");
    L.box(S, 5, 1, 0.12, "brass", 9.5, 13.5, 4.65); L.box(S, 0.12, 1, 1.25, "brass", 7.05, 13.5, 5.27); L.box(S, 0.12, 1, 1.25, "brass", 7.05, 13.5, 7.73);
    L.label(S, "ESCAPEMENT LEDGE", 11.9, 14.6, 6.5, -Math.PI / 2, 0.25, "#ffffff");
    // broken maintenance ramp (PRESENT + FUTURE) to the mid platform
    L.ramp(PR, [-9, -2.5, 10.5], [-1, 2.6, 10.5], 2.5, "iron");
    L.ramp(FU, [-9, -2.5, 10.5], [-1, 2.6, 10.5], 2.5, "ruin");
    L.floor(S, 1.45, 8.4, 4.9, 7.2, 2.6, "woodDark");
    L.box(S, 4.9, 1, 0.12, "brass", 1.45, 3.1, 11.95);
    L.label(PR, "MAINTENANCE STAIR - BROKEN ABOVE", -8.5, -0.6, 9.1, Math.PI, 0.25, "#ffd8a0");
    L.label(S, "THE OLD WAY UP WAS THE GEARS", 1.5, 4.4, 11.9, Math.PI, 0.22, "#ffe8c0");
    // bell stairs (south) gallery -> bell floor
    L.stairs(S, [7, 7, -10.5], [-2, 13, -10.5], 2.5, "wood");
    L.floor(S, -7, -8, 10, 8, 13, "wood");
    L.box(S, 0.12, 1, 5, "brass", -2.05, 13.5, -6.6);
    L.floor(S, -7.5, 8, 9, 8, 13, "woodDark");
    L.box(S, 0.6, 19.5, 0.6, "woodDark", -2.4, 3, -4.4); L.box(S, 0.6, 19.5, 0.6, "woodDark", -3.4, 3, 4.4);
    // the gap: PAST floor, FUTURE beam, PRESENT nothing
    L.floor(PA, -7, 0, 10, 8, 13, "wood");
    L.floor(FU, -7.5, 0, 0.9, 8.4, 13, "obsidian");
    L.box(FU, 0.05, 0.06, 8.4, "neonC", -7.95, 13.02, 0, { solid: false }); L.box(FU, 0.05, 0.06, 8.4, "neonC", -7.05, 13.02, 0, { solid: false });
    L.light(FU, 0x40d8ff, 2.5, 8, -7.5, 14.5, 0);
    L.label(PR, "FLOOR GONE", -7, 14.2, -4.1, Math.PI, 0.35, "#ff9a70");
    L.zone({ at: [-7, 10, 0], size: [10, 1, 8], onEnter: () => AZ.game.die("The floor is not there in this state. Find a state where something spans the gap."), once: false });
    // keeper's chamber walls (south door x -8.75..-6.25, east door z 5.4..7.6)
    L.box(S, 3.25, 6, 0.4, "brick", -10.375, 16, 4.2); L.box(S, 2.75, 6, 0.4, "brick", -4.875, 16, 4.2); L.box(S, 2.5, 3.4, 0.4, "brick", -7.5, 17.3, 4.2);
    L.box(S, 0.4, 6, 1, "brick", -3.3, 16, 4.9); L.box(S, 0.4, 6, 4.4, "brick", -3.3, 16, 9.8); L.box(S, 0.4, 3.4, 2.2, "brick", -3.3, 17.3, 6.5);
    L.box(PA, 9.4, 0.4, 8.4, "woodDark", -7.5, 19.2, 8, { solid: false });
    L.label(S, "KEEPER", -7.5, 16.2, 3.95, Math.PI, 0.35, "#ffffff");
    L.box(PA, 2.5, 2.6, 0.3, "woodDark", -7.5, 14.3, 4.2);
    L.label(PA, "LOCKED FROM INSIDE", -7.5, 14.8, 4.0, Math.PI, 0.2, "#ffe8c0");
    // balcony + archive bridge
    L.floor(S, -7.5, 15, 5, 4, 13, "stone");
    L.box(S, 0.12, 1, 4, "brass", -10, 13.5, 15); L.box(S, 0.12, 1, 4, "brass", -5, 13.5, 15);
    L.block(-7.5, 17.1, 5, 0.2, 13, 16, 7);
    L.box(S, 3, 0.4, 26, "concrete", -7.5, 12.6, 30, { solid: false });
    L.building(S, -7.5, 50, 18, 22, 12, "stoneWet", "windowCold", 202, "RAIN ARCHIVE", "#9ef0ff");
    L.label(S, "TO THE RAIN ARCHIVE >>", -7.5, 15.8, 16.9, Math.PI, 0.3, "#bff6ff");

    // ---------------- clockwork ----------------
    const gear = (g, x, y, z, r, axis, mat, speed, teeth) => {
      const grp = dynGroup(g); grp.position.set(x, y, z);
      if (axis === "x") grp.rotation.z = Math.PI / 2; else if (axis === "z") grp.rotation.x = Math.PI / 2;
      const inner = new THREE.Group(); grp.add(inner);
      L.cyl(inner, r, 0.4, mat, 0, 0, 0); L.cyl(inner, r * 0.25, 0.5, "iron", 0, 0, 0);
      for (let i = 0; i < teeth; i++) { const a = i * Math.PI * 2 / teeth; L.box(inner, 0.3, 0.36, 0.3, mat, Math.cos(a) * (r + 0.1), 0, Math.sin(a) * (r + 0.1), { solid: false, ry: -a }); }
      for (let i = 0; i < 3; i++) L.box(inner, r * 1.8, 0.42, 0.18, "iron", 0, 0, 0, { solid: false, ry: i * Math.PI / 3 });
      if (speed) L.update(dt => { if (AZ.get(F.wedge) && !AZ.get(F.powerOut)) inner.rotation.y += dt * speed; });
      return inner;
    };
    gear(PA, -10.8, 1.3, 2, 3.5, "x", "brass", 0.25, 12); gear(PA, -10.8, -0.4, 6.55, 1.1, "x", "brass", -0.8, 7);
    gear(PA, 11.4, 18, -5, 2.6, "x", "brass", 0.18, 9); gear(PA, 11.4, 18.6, 4.4, 2.2, "x", "brass", -0.22, 8); gear(PA, 2, 20, 11.4, 3, "z", "brass", 0.15, 10);
    L.cyl(PA, 0.22, 24, "brass", -11.3, 14, 2);
    gear(PR, -10.8, 1.3, 2, 3.5, "x", "rust", 0, 12); gear(PR, 11.4, 18, -5, 2.6, "x", "rust", 0, 9); gear(PR, 2, 20, 11.4, 3, "z", "copper", 0, 10);
    const drift = gear(FU, -9.5, 2, 2, 3.5, "x", "copper", 0.05, 10);
    L.update((dt, t) => { drift.parent.position.y = 2 + Math.sin(t * 0.4) * 0.3; });
    const bellAt = (g, mat) => { L.cyl(g, 1.5, 2.2, mat, 2.5, 22.9, 0); L.sphere(g, 1.35, mat, 2.5, 24, 0); L.cyl(g, 1.75, 0.25, mat, 2.5, 21.75, 0); L.box(g, 9.5, 0.5, 0.6, "woodDark", 2.5, 25.4, 0, { solid: false }); };
    bellAt(PA, "brass"); bellAt(PR, "rust");
    L.debris(FU, 14, 1, 18, 0, 10, 10, 10);
    L.debris(FU, 8, -20, 16, 0, 6, 14, 26);

    // ---------------- state dressing + light ----------------
    L.light(PA, 0xffc27a, 2.6, 12, -6, 1, 2); L.light(PA, 0xffc27a, 2.2, 9, 9.5, 9.5, -2); L.light(PA, 0xffc27a, 2.2, 9, -7, 16, -8); L.light(PA, 0xffc27a, 2.2, 9, -8, 17, 8);
    L.spot(PA, 0xffd8a0, 4, 40, 0, 29.5, 0, 0, 5, 0);
    L.lamp(PA, -10.8, -11.2, "gas", 3, 1.5); L.lamp(PR, -10.8, -11.2, "sodium", 3, 1.5); L.lamp(PR, 10.8, -11.2, "sodium", 3, 1.5);
    L.light(PR, 0x58c8d8, 3, 14, 0, -1.6, 3); L.light(PR, 0x8fb8e0, 3.2, 20, 0, 6, 1); L.light(PR, 0xff9a48, 1.1, 9, 0, 5, -8); L.light(PR, 0x8fb8e0, 2.6, 16, 0, 18, 0);
    L.light(PA, 0xffc27a, 3, 18, 0, 6, 1); L.light(PA, 0xffc27a, 1.1, 9, 0, 5, -8); L.light(FU, 0x9a60ff, 3, 18, 0, 6, 1); L.light(FU, 0x40d8ff, 1.1, 9, 0, 5, -8); L.light(PR, 0xff9a48, 2.2, 8, 9.5, 9.3, -6); L.light(PR, 0x9ec0ff, 2, 9, -7, 16, -8); L.light(PR, 0x9ec0ff, 1.6, 8, -8, 17, 8);
    L.light(FU, 0x40d8ff, 2.6, 12, -5, -1, 3); L.light(FU, 0x9a60ff, 2.6, 12, 9.5, 9, 0); L.light(FU, 0x40d8ff, 2.2, 10, -7, 15.5, -8); L.light(FU, 0xff40c8, 2.2, 9, -8, 16, 8); L.light(FU, 0x40d8ff, 3, 12, 0, 22, -9);
    L.particles(PA, "dust", [20, 22, 20], [0, 10, 0], 500);
    L.particles(PR, "rain", [6, 30, 6], [0, 14, 0], 400);
    L.particles(PR, "drips", [20, 10, 20], [0, 8, 0], 200);
    L.particles(FU, "motes", [22, 24, 22], [0, 12, 0], 600);
    L.crystals(FU, 6, -9, -9, 4, 4, 13); L.crystals(FU, 5, 9, 9, 5, 5, -2.5);
    L.skyline(FU, 0, 0, 40, 70, 10, "ruin", 9);
    for (const z of [-6, 6]) { L.box(PA, 0.05, 5, 2, "red", -11.9, 9, z, { solid: false }); L.label(PA, "(0)", -11.8, 10.5, z, Math.PI / 2, 0.6, "#ffd890"); }
    L.label(PR, "CLOCKWORKS CLOSED - FLOOD SINCE THE QUAKE", 0, 3.2, -11.95, 0, 0.3, "#ffd8a0");
    L.ambient("machine", 0.4, 1); L.ambient("water", 0.35, 2); L.ambient("rain", 0.15, 2); L.ambient("drone", 0.35, 4); L.ambient("wind", 0.3, 4);

    // ---------------- 1. the wedge (PAST) ----------------
    const wedgeG = dynGroup(PA);
    const wedgeMesh = L.box(wedgeG, 0.45, 0.6, 0.9, "woodDark", -10.3, 0.05, 5.4, { solid: false });
    L.label(wedgeG, "I.M. (0)", -10.0, 0.7, 5.4, Math.PI / 2, 0.16, "#ffe8c0");
    L.label(PA, "GREAT DRIVE GEAR", -10.4, 5.4, 2, Math.PI / 2, 0.3, "#ffe8c0");
    const looseWedge = L.item({ group: PA, key: "keeper_wedge", name: "keeper's wedge", at: [-9.3, -2.25, 5], size: 0.5, mat: "woodDark" });
    looseWedge.enabled = false; looseWedge.mesh.visible = false;
    L.clue({ group: wedgeG, mesh: wedgeMesh, at: [-10.3, 0.05, 5.4], prompt: "Pull the wedge free", flag: F.wedge, id: "L02_wedge", title: "The Keeper's Wedge",
      text: "A hardwood wedge, hammered deliberately between the great gear and its pinion - not dropped, not fallen. The blows are clean. Cut into its face: the initials I.M. and the clock-tower seal (0).",
      onUse: it => { it.enabled = false; wedgeG.visible = false; looseWedge.enabled = true; looseWedge.mesh.visible = true; L.sfx("clang"); L.toast("The great gear turns. Below the water, the works are no longer jammed.", 3.5); } });

    // ---------------- 2. the valve + the flood (PRESENT) ----------------
    L.box(PR, 0.45, 1.2, 0.45, "rust", 5, 2.1, -7.6);
    const wheel = L.cyl(PR, 0.45, 0.08, "copper", 5, 2.6, -7.3); wheel.rotation.x = Math.PI / 2; wheel.userData.dynamic = true;
    L.label(PR, "DRAIN", 5, 3.3, -7.3, Math.PI, 0.3, "#ffffff");
    let spin = 0;
    L.interact({ at: [5, 2.6, -7.3], mesh: wheel, mask: 2, prompt: "Open the drain valve", requires: [F.wedge], locked: "The valve wheel will not turn. Something deep in the works is jammed - something older than the flood.",
      onUse: () => { if (AZ.get(F.valve)) { L.toast("The valve is open. The water is going down.", 2); return; } spin = 2.5; L.set(F.valve); L.sfx("scrape"); } });
    L.update(dt => { if (spin > 0) { spin -= dt; wheel.rotation.y += dt * 4; } });
    const water = L.water({ group: PR, at: [0, 2.55], size: [24, 18.9], high: 0.6, low: -2.45, speed: 1.0, lowWhen: [F.wedge, F.valve], drainedFlag: F.drained, mask: 2 });
    // Refill countdown (re-arms every time the pit is drained again).
    let refill = -1, warned = false, wasLow = false;
    L.update(dt => {
      const low = water.level() <= -2.4;
      if (low && !wasLow && !AZ.get(F.gallery)) { refill = 75 * AZ.diff.timer(); warned = false; L.toast("The pit is dry - but the sluice is already leaking. The water will return. Climb!", 4); }
      wasLow = low;
      if (refill < 0) return;
      if (AZ.get(F.gallery)) { refill = -1; AZ.ui.timer(-1); return; }
      const before = Math.ceil(refill);
      refill -= dt;
      AZ.ui.timer(refill, "FLOOD RETURNS IN");
      if (refill < 10 && Math.ceil(refill) !== before) L.sfx("tick");
      if (!warned && refill < 15) { warned = true; L.toast("The flood is coming back! The PAST gear wheel is the only way up.", 3); L.sfx("water"); AZ.vibrate(60); }
      if (refill <= 0) {
        refill = -1; AZ.ui.timer(-1); L.set(F.valve, false); L.sfx("water");
        const P = AZ.player.pos;
        if (L.W.state === 1 && P.y < 6 && P.z > -7.2) AZ.game.die("The water came back. Drain it again and climb faster: ramp, mid platform, then the PAST gear wheel.");
        else L.toast("The flood returns to the pit.", 3);
      }
    });
    L.when([F.drained], () => {
      L.unlockStates(4, "NEW STATE: FUTURE >>. Tap >> to shift to the FUTURE, where the tower is split open. Try it here, on the dry pit floor.");
      L.say([line("The Witness", "Now the third state, Arin. What the tower becomes after the second bell."), line("The Witness", "Don't be afraid of it. I have stood in those ruins more times than you have."), line("Arin", "Past, Present, Future. Same stones, three different towers.")]);
    });

    // ---------------- 3. the PAST gear wheel: mid platform (2.6) -> gallery stub (7) ----------------
    const WC = { x: 5, y: 4.65, z: 6, r: 2.2 }, plats = [];
    const wheelG = dynGroup(PA);
    const disc = L.cyl(wheelG, 2.6, 0.3, "brass", WC.x, WC.y, WC.z + 1.1); disc.rotation.x = Math.PI / 2;
    for (let i = 0; i < 4; i++) {
      const m = L.box(PA, 1.8, 0.3, 1.8, "brass", WC.x, WC.y - WC.r, WC.z, { dynamic: true });
      const a0 = -Math.PI / 2 - i * Math.PI / 2;
      L.mover(m, (t, dt) => {
        const run = AZ.get(F.wedge) && !AZ.get(F.powerOut);
        m.userData.a = (m.userData.a === undefined ? a0 : m.userData.a) - (run ? 0.35 : 0) * dt;
        const a = m.userData.a; m.position.set(WC.x + WC.r * Math.cos(a), WC.y + WC.r * Math.sin(a) - 0.15, WC.z); disc.rotation.z = a;
      });
      plats.push(m);
    }
    L.label(PA, "RIDE THE WHEEL - STEP ON WHEN IT IS LEVEL", 1.5, 3.9, 11.9, Math.PI, 0.2, "#ffe8c0");
    L.light(PA, 0xffc27a, 2.4, 10, 3.5, 7, 6);
    L.zone({ at: [9.5, 8.2, -1.8], size: [5, 2.5, 20.4], flag: F.gallery, toast: "The gallery. The flood can't reach you here." });

    // ---------------- steam vents (PRESENT) ----------------
    for (const [z, off] of [[3, 0], [-3.5, 2.2]]) {
      L.box(S, 4.6, 0.04, 1.2, "iron", 9.5, 7.02, z, { solid: false });
      L.box(S, 4.6, 0.03, 0.12, "red2", 9.5, 7.03, z - 0.75, { solid: false }); L.box(S, 4.6, 0.03, 0.12, "red2", 9.5, 7.03, z + 0.75, { solid: false });
      L.label(S, "STEAM VENT - WAIT FOR THE RED LIGHT TO GO OUT", 11.9, 9.4, z, -Math.PI / 2, 0.18, "#ffd8b0");
      const steam = L.particles(PR, "drips", [4, 2.4, 1.2], [9.5, 8.2, z], 120);
      L.hazard({ group: PR, at: [9.5, 7, z], size: [5, 2.4, 1.4], idle: 2.6, warn: 1.5, active: 1.6, offset: off, mask: 2, msg: "Scalded. The vent glows red and hisses before it blows - cross while it is dark." });
      void steam;
    }

    // ---------------- NPCs ----------------
    L.npc({ name: "Ida Kell", group: PA, at: [-8.3, 3], y: -2.5, yaw: -Math.PI / 2, coat: "ochre", task: "Ilse Marrow's apprentice: oils the great drive gear and keeps the works turning before the festival bell.",
      route: [[-8.3, 3, 6, "work"], [-4, -2, 4, "look"], [-8.6, 7.6, 5, "work"]], approach: ["Mind your sleeves near the teeth!", "Are you from the guild?", "The keeper sent everyone up to the square."],
      talk: [line("Ida", "You shouldn't be down in the pit during the festival. Keeper Marrow sent everyone up to the square."),
        line("Ida", "Everyone but herself. She stayed down here alone, with her quarters locked. And now the great gear won't turn."),
        line("Ida", "Something's caught between the gear and the pinion, down by the west wall. I can't reach it with my arm in this sling."),
        line("Ida", "She's been writing in her logbook at night. Asking the Founders' runners how many children were on the list."),
        line("Ida", "Founder Halden came down yesterday. They argued by the pinion. I only heard one word: 'silence'.")],
      cond: [{ holding: "keeper_wedge", lines: [line("Ida", "That's the keeper's own wedge - I.M., and the (0). She jammed it in herself? Then the tower didn't fail. She stopped it.")] },
        { holding: "carved_zero", lines: [line("Ida", "Her carved zero! She never goes anywhere without it. If she left it in her quarters, she isn't planning to come back up.")] }],
      changedFlag: F.wedge, changedRoute: [[-7.4, 2, 5, "look"], [-3, 4, 5, "work"], [-7, -3, 5, "carry"]],
      changedTalk: [line("Ida", "Listen to it! The whole tower breathing again."), line("Ida", "Whatever the keeper was afraid of... I hope you know what you've started.")] });
    L.npc({ name: "Bram Kettle", group: PR, at: [7, -8.2], y: 1.5, coat: "olive", task: "Maintenance worker: bails floodwater with a bucket and keeps the hand pump primed, every night.",
      route: [[7, -8.2, 4, "carry"], [10.4, -10.8, 4, "work"], [-4, -8.3, 5, "look"]], approach: ["Careful, the landing's slick.", "Clockworks are closed, friend."],
      talk: [line("Bram", "Clockworks are closed. Flooded to the stair since the quake."), line("Bram", "An earthquake split the tower, they told us. Funny kind of quake that only floods one room."),
        line("Bram", "The drain valve's right there. Won't budge. Something deep down is locking the whole works."),
        line("Bram", "Heard the bell rang only once down in the Arrival Quarter tonight. Mirela's cup, still in one piece. Was that you?"),
        line("Bram", "There's a little room behind the landing - the old keeper's quarters. Ilse Marrow. They say she went mad and broke the clock.")],
      cond: [{ holding: "oil_can", lines: [line("Bram", "That's old Marrow's oil can - her initials on the lid. She never let anyone else oil the drive gear.")] },
        { holding: "nameplate", lines: [line("Bram", "Her nameplate. They pried that off the door the week after the quake. Funny thing to hide - unless the name is what you want forgotten.")] }],
      changedFlag: F.drained, changedRoute: [[-1.8, -7.7, 8, "look"], [-9.5, -10.2, 6, "idle"], [5.5, -10.5, 4, "idle"]],
      changedTalk: [line("Bram", "You drained it! Thirty years of buckets and you just... turned it."), line("Bram", "Mind the steam pipes on the gallery - they glow and hiss before they blow.")] });
    L.npc({ name: "Echo of Ilse Marrow", group: FU, at: [-9, -10], y: 13, coat: "white", holo: "amber", task: "A looping projection of the clockkeeper Ilse Marrow, still walking her last rounds of the bell floor.",
      route: [[-9, -10, 5, "work"], [-5, -6, 4, "look"], [-10.5, -6.5, 5, "work"]], approach: ["...the bell must not ring twice...", "Who walks my floor?"],
      talk: [line("Echo of Ilse Marrow", "...the bell must not ring twice... the bell must not ring twice..."),
        line("Echo of Ilse Marrow", "My recording is in the bell console. Only the fallen beam still reaches it."),
        line("Echo of Ilse Marrow", "Halden wanted silence. Silence is what the bell makes after the second stroke."),
        line("Echo of Ilse Marrow", "There were names on the list. Small names. I read them every night so that someone would."),
        line("Echo of Ilse Marrow", "My brother runs the trains. He thinks they are festival specials.")],
      cond: [{ holding: "zero_echo", lines: [line("Echo of Ilse Marrow", "My zero... even here it keeps its shape. Nothing, held open. That is all a rescue is.")] },
        { holding: "bell_shard", lines: [line("Echo of Ilse Marrow", "A shard of my bell. It cracked on the first stroke, and I knew the second must never come.")] }],
      changedFlag: F.rec, changedRoute: [[-7.5, -5, 20, "look"], [-9, -8, 10, "idle"]],
      changedTalk: [line("Echo of Ilse Marrow", "Now someone knows. Take it to the Archive."), line("Echo of Ilse Marrow", "The rain remembers what the Founders erased.")] });
    L.crowd(PA, 0, 1, 10, 7, 4, { y: -2.5 }); L.crowd(PA, 4, -10.2, 8, 2, 2, { y: 1.5 });
    L.crowd(FU, 0, 2, 10, 7, 3, { y: -2.5, holo: "cyan" });
    L.item({ group: PR, key: "oil_can", name: "old oil can", at: [-10, 1.75, -9.2], size: 0.45, mat: "brass" });
    L.item({ group: FU, key: "bell_shard", name: "bell shard", at: [-5, 13.25, -11.2], size: 0.5, mat: "brass" });

    // ---------------- 4. Brother Calder: ambush at the top of the bell stairs ----------------
    const calder = L.npc({ name: "Brother Calder", group: S, at: [-6, -10], y: 13, yaw: Math.PI / 2, coat: "obsidian", hood: true, task: "Keeper enforcer: guards the bell floor so the clockwork never runs and the night never ends.",
      route: [[-6, -10, 5, "work"], [-9.5, -6, 4, "look"], [-4, -7, 4, "idle"]], approach: ["Back down the stairs.", "The Keepers see you."],
      talk: [line("Calder", "Far enough, anomaly."), line("Calder", "The Keepers of the Hour keep this tower. Inside the hour, nobody dies for good."),
        line("Calder", "Mother Ondine says you are a crack in the glass. Cracks spread."), line("Calder", "Ilse Marrow stopped this clock once. We have kept it stopped ever since."),
        line("Calder", "Go home. Oh - you don't have one. None of you lot do.")],
      cond: [{ holding: "nameplate", lines: [line("Calder", "Her nameplate. We took that down for a reason.")] },
        { flag: "clue.L02_log3", lines: [line("Calder", "You've been reading her logbook. 'The wedge is mine.' Brave words for a woman who drowned the pit.")] }],
      changedFlag: F.cut, changedRoute: [[-7.5, -5, 3, "look"], [-7.5, -4.6, 60, "idle"]],
      changedTalk: [line("Calder", "Run along the gears, then. The hour is patient."), line("Calder", "Next time it won't be a power lever. Next time it will be the lift cable.")] });
    L.when([F.beam], () => { calder.p.group.position.y = -80; AZ.ui.toast("Brother Calder is gone. Somewhere below, a Keeper's lantern goes out.", 3.5); });
    const gate = L.box(S, 0.3, 2.6, 2.9, "rust", -2.25, 13 + 1.3 + 2.8, -10.6, { dynamic: true });
    const gateSign = L.label(S, "SEALED BY THE KEEPERS", -2.05, 15.9, -10.6, Math.PI / 2, 0.22, "#ff9a70"); gateSign.visible = false; gateSign.userData.dynamic = true;
    L.update(dt => {
      const closed = AZ.get(F.cut) && !AZ.get(F.bolt), ty = 13 + 1.3 + (closed ? 0 : 2.8);
      gate.position.y += Math.sign(ty - gate.position.y) * Math.min(Math.abs(ty - gate.position.y), dt * 2.5);
      gateSign.visible = closed;
    });
    L.zone({ at: [0.8, 13, -10.5], size: [2.6, 3.4, 2.5], flag: F.bell, onEnter: () => {
      L.dialogue({ speaker: "Brother Calder", question: "Brother Calder bars the bell floor. What do you say?", doneFlag: F.cut,
        opening: [line("Calder", "Far enough, anomaly. Sister Vossel said you'd climb the tower after the street."),
          line("Calder", "Do you know what the Keepers keep? The hour. Inside it nobody dies for good. Not the children, not the old, not even you."),
          line("Calder", "Ilse Marrow stopped this clock once. We have kept it stopped ever since.")],
        options: [
          { text: "Let me through. I only want the truth.", sets: "L02.calderTruth", lines: [line("Calder", "Truth is a flood, Arin. Ask Bram Kettle about floods."), line("Calder", "There. The power's cut, and the bell floor is sealed. Your PAST gears can rot.")] },
          { text: "Sister Vossel. You Keepers were watching me on the festival street.", requires: ["L01.vosselScene"], missing: "(you never faced the Keepers on the street)", sets: "L02.calderVossel",
            lines: [line("Calder", "Vossel watches. I lock. Mother Ondine decides."), line("Calder", "And right now I'm locking you out. Enjoy the dark.")] },
          { text: "Ilse stopped it to save the children - not to keep you safe.", requires: ["clue.L02_log3"], missing: "(you have not read her last logbook page)", sets: "L02.calderIlse",
            lines: [line("Calder", "...She knew what she was doing."), line("Calder", "So do I. Lights out, anomaly.")] },
        ],
        options_note: "" }).then(() => { L.set(F.powerOut); L.sfx("rule"); L.toast("The clockwork winds down. Calder has cut the power and sealed the bell floor.", 4); });
    } });

    // ---------------- 5. restart the PAST clockwork (gallery north end) ----------------
    L.box(PA, 0.7, 0.5, 0.7, "iron", 11.5, 7.25, 7.8);
    const leverM = L.box(PA, 0.45, 1.2, 0.45, "brass", 11.5, 8.0, 7.8, { solid: false, dynamic: true });
    L.label(PA, "ENGAGE CLOCKWORK", 11.5, 9.1, 7.8, -Math.PI / 2, 0.22, "#ffe8c0");
    L.label(PR, "LEVER (PAST ONLY)", 11.5, 9.1, 7.8, -Math.PI / 2, 0.2, "#ffd8a0");
    L.interact({ at: [11.5, 8, 7.8], mesh: leverM, mask: 1, prompt: "Engage the clockwork", requires: [F.cut], locked: "The clockwork is running. No need to touch the engagement lever.", once: true,
      onUse: () => { leverM.rotation.x = 0.9; L.set(F.lever); L.set(F.powerOut, false); L.set(F.restarted); L.sfx("solved"); L.toast("The PAST clockwork turns again. The counterweight lift is moving.", 3.5);
        L.say([line("The Witness", "Good. The clock remembers how to turn. Ride it up."), line("The Witness", "In the FUTURE the wind comes through the split. Listen for the howl, watch the banners, and wait.")]); } });
    L.checkpoint(9.5, 7, 6, 0, { size: [4, 2.5, 3] });

    // ---------------- 6. PAST counterweight lift: gallery (7) <-> ledge (13) ----------------
    const lift = L.box(PA, 3.6, 0.3, 3.2, "brass", 9.5, 6.85, 10.3, { dynamic: true });
    for (const x of [7.7, 11.3]) L.box(PA, 0.08, 22, 0.08, "iron", x, 19, 10.3, { solid: false });
    L.label(S, "COUNTERWEIGHT LIFT", 9.5, 9.4, 11.9, Math.PI, 0.25, "#ffffff");
    let liftT = 0, liftDir = 1, liftWait = 0;
    L.mover(lift, (t, dt) => {
      if (!AZ.get(F.restarted) || AZ.get(F.powerOut)) return;
      if (liftWait > 0) { liftWait -= dt; return; }
      liftT += liftDir * dt * 0.9 / 6;
      if (liftT >= 1) { liftT = 1; liftDir = -1; liftWait = 3.5; L.sfxAt("clang", lift.position, 0.5); }
      if (liftT <= 0) { liftT = 0; liftDir = 1; liftWait = 3.5; L.sfxAt("clang", lift.position, 0.5); }
      lift.position.y = 6.85 + 6 * liftT * liftT * (3 - 2 * liftT);
    });

    // ---------------- 7. FUTURE gust beam: ledge -> chamber east door ----------------
    L.floor(FU, 1.97, 6.5, 10.15, 0.9, 13, "obsidian");
    L.box(FU, 10.15, 0.05, 0.05, "neonC", 1.97, 13.01, 6.95, { solid: false }); L.box(FU, 10.15, 0.05, 0.05, "neonC", 1.97, 13.01, 6.05, { solid: false });
    L.floor(FU, 2, 6.5, 2.2, 2.2, 13, "obsidian");
    L.box(FU, 2.2, 2.4, 0.3, "ruin", 2, 14.2, 5.3);
    L.label(FU, "SHELTER", 2, 15.6, 5.47, 0, 0.3, "#9ef0ff");
    L.light(FU, 0x40d8ff, 2, 7, 2, 14.8, 6.6);
    const gusts = [];
    for (const [cx, w, off] of [[5.07, 3.95, 0], [-1.1, 4.0, 1.6]]) {
      const h = L.hazard({ group: FU, at: [cx, 13, 6.5], size: [w, 2.8, 1.8], idle: 3.0, warn: 1.6, active: 1.5, offset: off, mask: 4, color: 0xb0f0ff,
        msg: "A gust tore you off the beam. When the banners whip and the wind howls, hold at the shelter." });
      L.box(FU, 0.1, 2.8, 0.1, "obsidian", cx, 14.4, 7.3, { solid: false });
      const banner = L.box(FU, 1, 0.6, 0.03, "neonM", cx + 0.5, 15.3, 7.3, { solid: false, dynamic: true });
      gusts.push({ h, banner });
    }
    L.update((dt, t) => gusts.forEach(g => { const k = g.h.phase === "warn" ? 1 : g.h.phase === "active" ? 2 : 0; g.banner.rotation.y = Math.sin(t * (4 + k * 10)) * 0.25 * (1 + k); g.banner.rotation.z = k * 0.4 * Math.sin(t * 13); }));
    L.zone({ at: [1.9, 10, 6.5], size: [9.9, 1, 3.5], once: false, onEnter: () => AZ.game.die("You fell into the shaft. Cross the beam only in the FUTURE, between gusts.") });
    L.checkpoint(10.3, 13, 6.5, Math.PI / 2, { climax: true, size: [3, 2.5, 3] });
    L.zone({ at: [-7.5, 14.5, 7.5], size: [8, 3, 5], flag: F.beam, toast: "The keeper's chamber." });
    L.checkpoint(-7.5, 13, 7.5, 0, { size: [6, 2.5, 4] });

    // ---------------- 8. Calder's bolt winch (PAST, inside the chamber) ----------------
    const winch = L.cyl(PA, 0.4, 1.2, "brass", -4.3, 13.6, 10.6, { solid: true });
    L.label(PA, "CALDER'S BOLT - WINCH", -4.3, 14.7, 10.0, Math.PI, 0.18, "#ffe8c0");
    L.box(PR, 0.7, 1, 0.7, "rust", -4.3, 13.5, 10.6); L.box(FU, 0.7, 1, 0.7, "ruin", -4.3, 13.5, 10.6);
    L.label(PR, "BOLT WINCH - SEIZED (intact only in the PAST)", -4.3, 14.4, 10.0, Math.PI, 0.14, "#ffd8a0");
    L.label(FU, "BOLT WINCH - SEIZED (intact only in the PAST)", -4.3, 14.4, 10.0, Math.PI, 0.14, "#9ef0ff");
    L.interact({ at: [-4.3, 13.6, 10.6], mesh: winch, mask: 1, prompt: "Draw Calder's bolt", requires: [F.cut], locked: "The bolt is not drawn across anything yet.", once: true,
      onUse: () => { L.set(F.bolt); L.sfx("solved"); L.toast("Calder's seal falls open - on the bell stairs and on the bell console.", 4); L.say([line("Arin", "Ilse's own bolt. Calder only borrowed it.")]); } });
    L.box(S, 2.2, 0.9, 1.1, "wood", -10.4, 13.45, 9.6);
    const horn = L.cyl(PA, 0.28, 0.8, "brass", -10.1, 14.6, 9.4); horn.rotation.z = -0.9;

    // ---------------- 9. the recording (bell console, behind Calder's shutter) ----------------
    L.box(S, 1, 0.9, 0.8, "brass", -10.6, 13.45, -10.6);
    const recM = L.cyl(S, 0.3, 0.2, "brass", -10.6, 14.0, -10.6);
    L.label(S, "RECORDING - I. MARROW", -10.6, 15.6, -9.8, 0, 0.18, "#fff0c0");
    L.light(S, 0xffc080, 1.5, 4, -10, 15.2, -9.6);
    const shutter = L.box(S, 1.6, 2.3, 1.5, "rust", -10.6, 14.15, -10.6, { solid: false, dynamic: true });
    L.label(S, "LOCKED - BY ORDER OF THE KEEPERS", -10.6, 14.4, -9.82, 0, 0.14, "#ff9a70");
    L.when([F.bolt], () => { shutter.visible = false; });
    const projection = dynGroup(S);
    L.holoMesh(projection, "cyl", "amber", -8.4, 14.1, -9, 0.55, 1.6, 0.55); L.holoMesh(projection, "sphere", "amber", -8.4, 15.25, -9, 0.3, 0.3, 0.3);
    L.label(projection, "ILSE MARROW - CLOCKKEEPER", -8.4, 15.8, -9, 0, 0.2, "#ffd8a0");
    projection.visible = false;
    L.interact({ at: [-10.6, 14, -10.6], mesh: recM, prompt: "Play the clockkeeper's recording", requires: [F.bolt], locked: "Calder's shutter is locked over the console.", once: true,
      onUse: async () => {
        L.set(F.recPlayed); projection.visible = true;
        await L.memory([
          line("Ilse Marrow", "This is Ilse Marrow, clockkeeper to the Founders. If this cylinder is turning, someone has climbed up through the flood."),
          line("Ilse Marrow", "Hear me plainly. There was no earthquake. I stopped it."),
          line("Ilse Marrow", "The rescue is failing. The children are still in the fold between the states, and the second bell would close it on them."),
          line("Ilse Marrow", "So I stopped the tower. From inside. My own wedge, in the great gear, between the first stroke and the second."),
          line("Ilse Marrow", "Founder Halden has ordered silence. He will flood the pit and call it a quake. Let him. Every hour this tower stands still is an hour for the children."),
          line("Ilse Marrow", "The rest is written in the rain. The Archive keeps every night. Go. Read the rain."),
          line("Ilse Marrow", "And you, if it is you... keep time for me, my little A."),
          line("Arin", "My little A. The watch in my coat: 'For A. - keep time for me. I.M.' Ilse Marrow."),
          line("The Witness", "She bought them time, Arin. Time is what you are spending now. Spend it well."),
        ], F.rec);
        projection.visible = false;
      } });
    L.door(S, 2.5, 2.6, 0.5, "woodDark", -7.5, 13, 12.5, [F.rec], [0, 2.7, 0], { toast: "The keeper's door unbolts. The archive bridge lies beyond." });
    L.zone({ at: [-7.5, 14.5, 14], size: [4.6, 3, 1.2], flag: F.archive });
    L.exit({ group: S, at: [-7.5, 13, 15.8], size: [4, 2.5, 1.6], requires: [F.rec], locked: "The keeper's door is bolted. Hear the clockkeeper's recording first." });
    L.checkpoint(-7, 13, -9.5, 0, { size: [6, 2.5, 3] });
    L.checkpoint(5, 1.5, -10, 0, { size: [4, 2.5, 3] });

    // ---------------- optional: the keeper's quarters (through the south wall of the landing) ----------------
    L.floor(S, -7, -16.2, 8.8, 6.4, 1.5, "wood");
    L.box(S, 0.4, 4.2, 6.4, "plaster", -11.2, 3.6, -16.2); L.box(S, 0.4, 4.2, 6.4, "plaster", -2.8, 3.6, -16.2); L.box(S, 8.8, 4.2, 0.4, "plaster", -7, 3.6, -19.2);
    L.box(S, 8.8, 0.4, 6.4, "woodDark", -7, 5.9, -16.2);
    L.label(S, "KEEPER'S QUARTERS", -7, 4.45, -11.95, 0, 0.25, "#ffffff");
    L.zone({ at: [-7, 3, -16], size: [7.5, 3, 5], flag: "L02.quartersVisited", toast: "The clockkeeper's quarters.", lines: [line("Arin", "Her rooms. Small. A bed, a desk, and a window painted shut.")] });
    L.box(S, 1.8, 0.7, 2.4, "red", -9.9, 1.85, -17.6); L.box(S, 2, 0.9, 1, "wood", -4.2, 1.95, -18.3); L.box(S, 0.6, 3, 2.4, "woodDark", -3.3, 3, -15);
    L.box(PA, 1.6, 1.2, 0.05, "window", -7, 3.4, -18.98, { solid: false }); L.box(PR, 1.6, 1.2, 0.05, "windowCold", -7, 3.4, -18.98, { solid: false });
    L.light(PA, 0xffc27a, 2, 8, -7, 4.8, -16); L.light(PR, 0x9ec0ff, 1.4, 8, -7, 4.8, -16); L.light(FU, 0xff40c8, 1.6, 8, -7, 4.8, -16);
    L.clue({ group: S, at: [-4.8, 2.43, -18.2], size: [0.45, 0.05, 0.34], prompt: "Read the logbook page", flag: "L02.log1", id: "L02_log1", title: "Keeper's Logbook - the Grinding",
      text: "Day 1. The three states rub at the seams. The Engine sings a quarter-tone flat and the escapement shivers at the second stroke.\n\nFounder Halden came down himself. He asked me to write nothing. I am writing this.\n\n- I.M." });
    L.clue({ group: S, at: [-9.6, 2.23, -17.2], size: [0.45, 0.05, 0.34], prompt: "Read the logbook page", flag: "L02.log2", id: "L02_log2", title: "Keeper's Logbook - the List",
      text: "Day 31. The evacuation list is drawn up, nine days before the date they will give the public. Families first, quietly. The children before anyone.\n\nThey are to be kept in a place with no address until the states settle. Ignatz is to run the trains. He thinks they are festival specials.\n\n- I.M." });
    L.clue({ group: S, at: [-3.8, 2.43, -18.25], size: [0.34, 0.05, 0.45], prompt: "Read the last logbook page", flag: "L02.log3", id: "L02_log3", title: "Keeper's Logbook - Last Entry",
      text: "The rescue is failing. The pocket is folding. If the bell strikes twice tonight they will be lost between the states.\n\nI will stop the tower myself, between the strokes. Halden orders silence. Let him call it an earthquake.\n\nI am sorry for the flood. The flood is his. The silence is his. The wedge is mine.\n\nMy little A. is on the list too. I gave A. my watch to hold - 'keep time for me' - and promised to come for it.\n\n- Ilse Marrow" });
    L.item({ group: PA, key: "carved_zero", name: "carved zero", at: [-9.6, 2.35, -18.3], size: 0.42, mat: "brass" });
    L.item({ group: PR, key: "nameplate", name: "keeper's nameplate", at: [-7.6, 1.75, -13.6], size: 0.5, mat: "brass" });
    L.item({ group: FU, key: "zero_echo", name: "glowing zero", at: [-5.2, 2.6, -18.5], size: 0.42, mat: "neonC" });

    // ---------------- memory fragments ----------------
    L.fragment("L02.1", [-10.2, 2.6, -14], "The Keeper's Coin", "My mother cleaned the keeper's lamps. The night of the festival Keeper Marrow pressed a coin into her hand and said: take your children to the station, not the square. Mother laughed. We went to the square.", S);
    L.fragment("L02.2", [10.6, -1.4, 10.4], "The Mark on the Wall", "Thirty years I've bailed this pit. Every morning the water sits at the same chalk mark. Water doesn't remember. This water does. Some mornings I find a child's shoe on the stair, and every morning it is the same shoe.", S);
    L.fragment("L02.3", [11.2, 8.2, -11.3], "Two Strokes", "I counted the bell from the gallery. One. Then the gears screamed and the second stroke never came - and still the city fell, slower, like a held breath. Someone above us was counting too. Someone with a zero on their palm.", S);
  },
});
