/* LEVEL 07 — The Bell Catastrophe (Design v2: the hardest action). Ported from the Unity Level07Builder.
   Layout (+z north): Warden's Terrace start (y 3, south), Tram Hall safe zone (south-west, inside, Halden at its gate),
   Market Row (east, fire in the PRESENT -> lead through the PAST), arcade lane + courtyard (west, brace the wall with
   PAST scaffolding), Keeper's back lane (courtyard -> Tram Hall service door, Keepers in the PRESENT), Keeper's Alley
   (east of the tower: open only in the FUTURE, surge + signal relay, Warden Roe's group behind the tower),
   the tower forecourt (three drives, six striking plates = the climax), the tower interior to the core. */
AZ.registerLevel({
  id: "L07", order: 7, chapter: "LEVEL 07", title: "The Bell Catastrophe", location: "Founders' Square and the Clock Tower",
  start: 1, allowed: 7,
  build(L) {
    const ln = L.line, S = L.shared, PA = L.past, PR = L.present, FU = L.future;
    const F = {
      start: "L07.crisisStarted", g1: "L07.group1Saved", g2: "L07.group2Saved", g3: "L07.group3Saved", all: "L07.allSaved",
      braced: "L07.scaffoldBraced", grounded: "L07.surgeGrounded", signal: "L07.signalSeen", driveOpen: "L07.driveOpened",
      bell: "L07.bellDisabled", core: "L07.coreReached", yield: "L07.haldenYielded", hTalked: "L07.haldenTalked",
      back: "L07.backDoorOpen", route: "L07.tramRoute", notes: "clue.L07.keeperNotes", halden: "clue.L07.halden", plan: "clue.L07.plan",
    };
    const SAFE = [-22, -32.4], GATE = [-22, -24.6], SW = [-12, -20];
    const face = (x, z, vx, vz) => Math.atan2(vx - x, vz - z);
    const hide = m => { m.visible = false; if (m.userData.collider) m.userData.collider.on = false; };
    const show = m => { m.visible = true; if (m.userData.collider) m.userData.collider.on = true; };

    // ---------------- look ----------------
    L.look({ fog: 0.85, present: { sunI: 0.55, bloom: 0.9 }, future: { bloom: 1.5 } });

    // ---------------- story ----------------
    L.interlude("Before the Second Bell", [
      ln("The Witness", "Arin. Before you go down there - listen.", 3.5),
      ln("The Witness", "You've done this before. You chose differently.", 4),
      ln("Arin", "How many times?", 2.5),
      ln("The Witness", "Enough that the stones remember your footsteps. Not enough that you do.", 4.5),
      ln("The Witness", "Tonight, save who you can. Then follow the signal to whoever is sending it.", 4.5),
      ln("Arin", "And if it's someone I know?", 3),
    ]);
    L.intro(ln("Arin", "Founders' Square. Festival, rain and ruin - all three at once now, stacked on the same stones."),
      ln("Arin", "From up here I can see all of it. Once I step down, the bell rings, and the clock starts."),
      ln("Odile Fenwick", "Arin! You came back. When it starts: the Tram Hall, south-west. But Founder Halden is standing at its gate."),
      ln("The Witness", "It has always rung from somewhere further on. Listen for where.", 4));
    L.objective("Step down into the square", F.start, "Take the terrace stairs down. Odile waits at the bottom.");
    L.objective("Brace the cracked arcade wall with the PAST scaffolding", F.braced, "The west lane is blocked by a collapse in the PRESENT. Shift to the PAST: the same wall has scaffolding - lock its clamp.");
    L.objective("Open the Tram Hall: face Founder Halden, or take the back lane", F.route, "Halden at the gate yields only to proof (the list, the record, the register). Otherwise: arcade courtyard, south door, the Keepers' back lane - shift to the PAST to slip past their lanterns - and lift the service door bar.");
    L.objective("Lead Mirela's market group out through the PAST", "L07.g1Resolved", "Market Row (east) is on fire in the PRESENT. In the PAST the lane is open: shift, then lead them out and on to the Tram Hall.");
    L.objective("Lead Pell and Lise's group out of the courtyard", "L07.g2Resolved", "Through the braced arcade lane, then to the Tram Hall.");
    L.objective("Ground the surge in the FUTURE", F.grounded, "Keeper's Alley (east of the tower) is rubble in the PRESENT and locked in the PAST. In the FUTURE it is open but arcing: carry the grounding cable from the market pylon to the earth spike.");
    L.objective("Lead Warden Roe's group out from behind the tower", "L07.g3Resolved", "Warden Roe's drill group is trapped behind the tower, at the end of Keeper's Alley. Lead them out in the FUTURE.");
    L.objective("Read the signal from the tower's future ruins", F.signal, "The relay at the end of Keeper's Alley (FUTURE) names the drive that strikes the second toll.");
    L.objective("Open the drive the signal names", F.driveOpen, "Three drives on the tower forecourt (PRESENT): I /\\, II (0), III [#]. A wrong drive costs time.");
    L.objective("Play the lullaby on the striking plates", F.bell, "Six plates numbered 1-6, under falling masonry. The Nursery song: 1 3 5 4. HUM near drive III if you forget.");
    L.objective("Climb the tower to the core", F.core, "The tower door opens once the bell is stopped.");
    L.objective("Enter the Zero Chamber", "L07.enteredChamber", "The glowing door at the top of the core platform.");
    L.payoff(F.bell, "The Signal From After", "Drive III never answered a hand in Veyra. It answered a signal broadcast from the clock tower's own future ruins - signed with a zero drawn in one stroke, closing on a small hook.\n\nArin's zero. Arin's hand. Someone in the future is ringing the bell backwards through time, and they write exactly like you.");

    // ---------------- square, floors, perimeter ----------------
    L.floor(S, 0, 10, 64, 72, 0, "stoneWet");
    L.floor(S, -37, 1.5, 10, 5, 0, "stoneWet");
    L.floor(S, -48, 2, 12, 20, 0, "stoneWet");
    L.floor(S, 38, 1, 12, 6, 0, "stoneWet");
    L.floor(S, -48, -21, 4, 26, 0, "stoneWet");
    L.floor(S, -41, -31, 18, 6, 0, "stoneWet");
    L.floor(S, -22, -32, 20, 12, 0, "tile");
    L.box(PA, 63.8, 0.02, 71.8, "stone", 0, 0.012, 10, { solid: false });
    L.box(FU, 63.8, 0.02, 71.8, "obsidian", 0, 0.012, 10, { solid: false });
    L.cyl(S, 7, 0.03, "brass", 0, 0.02, 2); L.cyl(S, 6.4, 0.04, "stone", 0, 0.03, 2);
    L.label(S, "0", 0, 0.06, 2, 0, 4, "#e8d090").rotation.x = -Math.PI / 2;

    const mass = (x, z, w, h, d, mat, winFace) => {
      L.box(S, w, h, d, mat, x, h / 2, z);
      L.box(S, w + 0.4, 0.4, d + 0.4, "woodDark", x, h + 0.2, z, { solid: false });
      // windows on the face toward the square (Past/Present lit), neon trim in the Future
      const [nx, nz] = winFace, along = nx !== 0 ? d : w, cols = Math.floor(along / 3.2);
      for (let f = 1; f < Math.floor((h - 2) / 3.4); f++) for (let c = 0; c < cols; c++) {
        if ((c * 7 + f * 3) % 5 === 0) continue;
        const o = -along / 2 + (c + 0.5) * along / cols, y = 2.2 + f * 3.4;
        const wx = nx !== 0 ? x + nx * (w / 2 + 0.04) : x + o, wz = nz !== 0 ? z + nz * (d / 2 + 0.04) : z + o;
        L.box(S, nx !== 0 ? 0.06 : 1.1, 1.6, nx !== 0 ? 1.1 : 0.06, "window", wx, y, wz, { solid: false, noEcho: true, shadow: false });
      }
      L.box(FU, nx !== 0 ? 0.1 : w, 0.15, nx !== 0 ? d : 0.1, nx + nz > 0 ? "neonC" : "neonM", nx !== 0 ? x + nx * (w / 2 + 0.06) : x, h - 0.5, nz !== 0 ? z + nz * (d / 2 + 0.06) : z, { solid: false, shadow: false });
    };
    mass(-37, -13.5, 10, 14, 25, "plaster", [1, 0]);
    mass(-37, 25, 10, 16, 42, "brick", [1, 0]);
    mass(37, -14, 10, 13, 24, "stone", [-1, 0]);
    mass(37, 25, 10, 15, 42, "plaster", [-1, 0]);
    mass(24, 36, 16, 12, 20, "brick", [0, -1]);
    mass(20, -32, 24, 12, 12, "stone", [0, 1]);
    L.wall(S, -12, -26.2, -8, -26.2, 10, "stone", 0.6);
    L.wall(S, -7, 46, 16, 46, 12, "brick", 0.8);
    L.wall(S, 44, -2, 44, 4, 5, "woodDark", 0.6);
    L.label(S, "MARKET ROW", 43.6, 2.6, 1, face(43.6, 1, 30, 1), 0.6, "#ffd8a0", "rgba(20,12,8,.85)");
    L.wall(S, -54, -8, -54, 12, 9, "brick", 0.6);
    L.wall(S, -54, 12, -42, 12, 9, "brick", 0.6);
    L.wallDoor(S, -54, -8, -42, -8, 9, "brick", 2.4, 3, 0.5, 0.6);
    L.label(S, "SERVICE LANE - TRAM HALL", -48, 3.6, -7.6, face(-48, -7.6, -48, 2), 0.3, "#ffd8a0", "rgba(20,12,8,.85)");
    L.skyline(S, 0, 10, 72, 95, 12, "plasterCold", 7);
    L.skyline(FU, 0, 10, 105, 135, 8, "ruin", 9);

    // ---------------- Warden's Terrace (safe observation) ----------------
    L.box(S, 16, 3, 10, "stone", 0, 1.5, -33);
    L.stairs(S, [0, 0, -23.5], [0, 3, -28], 4, "stone");
    L.wall(S, -8, -28.1, -2, -28.1, 0.9, "stone", 0.25, 3); L.wall(S, 2, -28.1, 8, -28.1, 0.9, "stone", 0.25, 3);
    L.wall(S, -8, -38, -8, -28, 0.9, "stone", 0.25, 3); L.wall(S, 8, -38, 8, -28, 0.9, "stone", 0.25, 3);
    L.wall(S, -8, -38.2, 8, -38.2, 9, "stone", 0.6);
    L.label(S, "WARDEN'S TERRACE", 0, 4.8, -37.8, face(0, -37.8, 0, -30), 0.6, "#ffd8a0");
    L.clue({ group: S, at: [-4, 3.8, -28.7], size: [0.45, 0.6, 0.45], mat: "brass", prompt: "Look through the spyglass", flag: "L07.observed", id: null, noMarker: true,
      lines: [ln("Arin", "East: Market Row is burning. In the PAST that lane is just stalls and bunting."),
        ln("Arin", "West: the arcade lane, that wall is leaning hard. And behind the tower - people, behind the rubble in Keeper's Alley."),
        ln("Arin", "South-west, the Tram Hall. A man in Founders' robes is standing in its gate."),
        ln("Arin", "And the tower... in the FUTURE something at the top is still transmitting.")] });
    L.player(0, 3, -34, Math.PI);
    L.checkpoint(0, 3, -34, Math.PI, { size: [6, 2.5, 4] });
    L.checkpoint(0, 0, -20, Math.PI, { size: [8, 2.5, 3] });

    // ---------------- Tram Hall (safe zone inside) ----------------
    L.wallDoor(S, -32, -26, -12, -26, 12, "plaster", 4, 3.6, 0.5, 0.6);
    L.wallDoor(S, -32, -38, -32, -26, 12, "plaster", 2.4, 3, 0.583, 0.6);
    L.wall(S, -32, -38, -12, -38, 12, "plaster", 0.6);
    L.wall(S, -12, -38, -12, -26, 12, "plaster", 0.6);
    L.box(S, 20.6, 0.4, 12.6, "stone", -22, 12.2, -32);
    L.box(S, 20, 0.3, 12, "woodDark", -22, 6.2, -32, { solid: false });
    L.box(S, 18, 1, 2.6, "concrete", -22, 0.5, -36.6);
    L.stairs(S, [-22, 0, -33.8], [-22, 1, -35.3], 6, "concrete");
    L.box(S, 12, 3, 1.4, "iron", -22, 2.6, -37.1, { solid: false });
    L.box(S, 11, 0.8, 0.05, "window", -22, 3.1, -36.38, { solid: false, shadow: false });
    L.label(S, "TRAM 0 - LAST CAR", -22, 4.4, -36.3, face(-22, -36.3, -22, -30), 0.4, "#ffd890");
    L.cyl(S, 3.5, 0.03, "neonA", SAFE[0], 0.02, SAFE[1]);
    L.label(S, "GATHER HERE", SAFE[0], 0.06, SAFE[1] + 2.2, 0, 1, "#ffd090").rotation.x = -Math.PI / 2;
    L.light(S, 0xffb468, 2.6, 14, SAFE[0], 4.5, SAFE[1]);
    L.label(S, "TRAM HALL", -22, 4.6, -25.65, face(-22, -25.65, -22, -18), 0.7, "#ffd8a0", "rgba(20,12,8,.85)");
    L.door(S, 4, 3.6, 0.3, "rust", -22, 0, -26, [F.yield], [0, 3.7, 0], { toast: "Founder Halden steps aside. The Tram Hall gate rises." });
    L.door(S, 0.3, 3, 2.4, "woodDark", -32, 0, -31, [F.back], [0, 3.1, 0], { toast: "The service door swings up. The Tram Hall is open from the back." });
    const bar = L.box(S, 0.4, 0.5, 0.6, "rust", -32.55, 1.3, -29.4, { solid: false });
    L.interact({ at: [-32.55, 1.3, -29.4], mesh: bar, prompt: "Lift the service door bar", once: true, onUse: () => {
      L.set(F.back); L.sfx("clang"); L.say([ln("Arin", "Bar's up. The back way into the Tram Hall is open - now bring them through the lane.")], false); } });
    L.when([F.yield], () => L.set(F.route)); L.when([F.back], () => L.set(F.route));

    // ---------------- Market Row: fire in the PRESENT, open in the PAST ----------------
    L.box(PR, 1.4, 2.6, 6, "rust", 33.4, 1.3, 1);
    L.box(PR, 2.6, 0.1, 2, "ruin", 34.2, 2.5, -0.5, { solid: false });
    L.particles(PR, "embers", [1.6, 3, 6], [33.2, 0.4, 1], 220);
    L.mesh(PR, "cone", "neonA", 33.2, 0.7, -0.6, 1, 1.4, 1).castShadow = false;
    L.mesh(PR, "cone", "neonA", 33.2, 0.8, 2.4, 1.2, 1.6, 1.2).castShadow = false;
    const fireL = L.light(PR, 0xff6a20, 3, 10, 32.5, 1.6, 1);
    L.update((dt, t) => { fireL.intensity = 2.6 + Math.sin(t * 17) * 0.5 + Math.sin(t * 5.3) * 0.4; });
    L.label(PA, "MARKET ROW - FESTIVAL CLOTH", 32.6, 3.3, 1, face(32.6, 1, 20, 1), 0.3, "#5a2a10", "rgba(240,220,180,.9)");
    L.bunting(PA, 33, 43, 1, 4);
    L.box(FU, 1.2, 3.2, 6, "crystal", 33.4, 1.6, 1);
    const stall = (g, x, z, canopy) => { L.box(g, 3, 1, 1.2, "wood", x, 0.5, z); L.box(g, 3.4, 0.08, 1.8, canopy, x, 2.6, z + 0.1, { solid: false }); L.cyl(g, 0.06, 2.6, "woodDark", x - 1.4, 1.3, z + 0.5); L.cyl(g, 0.06, 2.6, "woodDark", x + 1.4, 1.3, z + 0.5); };
    stall(S, 20, 12, "red"); stall(S, 25.5, 12, "teal"); stall(S, 22, -1, "gold"); stall(S, 41, -1.2, "red");
    for (let i = 0; i < 3; i++) stall(PA, -20 + i * 4.5, -12, ["red", "gold", "teal"][i]);
    for (let i = 0; i < 3; i++) stall(PR, -20 + i * 4.5, -12, "rust");

    // ---------------- the Keepers' back lane (alternate route) ----------------
    L.wall(S, -50, -34, -50, -8, 8, "brick", 0.5); L.wall(S, -46, -28, -46, -8, 8, "brick", 0.5);
    L.wall(S, -50, -34, -32, -34, 8, "brick", 0.5); L.wall(S, -46, -28, -32, -28, 8, "brick", 0.5);
    L.box(S, 1.2, 1.2, 1.6, "wood", -49.2, 0.6, -21); L.box(S, 1.2, 1.2, 1.6, "wood", -46.9, 0.6, -14.5);
    L.box(S, 1.6, 1.2, 1.2, "wood", -43, 0.6, -33.1); L.box(S, 1.6, 1.2, 1.2, "wood", -37, 0.6, -28.9);
    L.box(PA, 3.8, 1.6, 1.2, "wood", -48, 0.8, -18.5); L.box(PA, 1.2, 1.6, 5.8, "woodDark", -40.5, 0.8, -31);
    L.box(PA, 3.5, 0.9, 0.03, "paper", -48, 3.2, -24, { solid: false });
    L.label(PA, "LAUNDRY - FESTIVAL LINEN", -48, 1.9, -17.85, face(-48, -17.85, -48, -10), 0.22, "#5a2a10");
    L.keeper({ name: "Lantern-Bearer", mode: "hunter", group: PR, at: [-48, -13], yaw: Math.PI, path: [[-48, -13], [-48, -26]], view: 10,
      caught: "A Keeper's lantern found you. In the PAST the Keepers don't walk this lane - but the carts do block it.", spot: ["Someone in the lane?", "Anomaly scent..."] });
    L.keeper({ name: "Gate-Keeper", mode: "hunter", group: PR, at: [-36, -31], yaw: -Math.PI / 2, path: [[-36, -31], [-47.5, -31]], view: 10,
      caught: "The Gate-Keeper caught you. Wait behind the crates for the lantern to turn away.", spot: ["Mother Ondine said one would come this way.", "Who goes there?"] });
    L.checkpoint(-48, 0, -10, 0, { size: [4, 2.5, 2] });

    // ---------------- arcade lane: brace the PRESENT wall from the PAST ----------------
    const leaning = L.box(PR, 10, 9, 0.5, "brick", -37, 4.5, 3.2, { dynamic: true });
    leaning.rotation.x = 0.16;
    const rubble = [L.box(PR, 3, 3.6, 5.2, "stoneWet", -35.5, 1.8, 1.5, { dynamic: true }), L.box(PR, 2.2, 1.2, 1.6, "brick", -34, 0.6, 0.4, { dynamic: true, ry: 0.4 })];
    const rubbleSign = L.label(PR, "COLLAPSE - NO WAY THROUGH", -33.9, 2.4, 1, face(-33.9, 1, -20, 1), 0.3, "#ff9a80", "rgba(30,10,8,.85)");
    const bracedWall = L.box(PR, 10, 9, 0.5, "brick", -37, 4.5, 3.7, { dynamic: true, solid: false });
    const shores = [-40, -37, -34].map(x => { const m = L.box(PR, 0.25, 5.6, 0.25, "rust", x, 2.6, 2.55, { dynamic: true, solid: false }); m.rotation.x = -0.44; return m; });
    const shoreText = L.label(PR, "SHORED IN THE FESTIVAL YEAR", -37, 6.2, 3.42, face(-37, 3.42, -37, -5), 0.3, "#e8d0a0");
    [bracedWall, ...shores].forEach(m => { m.visible = false; }); shoreText.visible = false;
    L.when([F.braced], () => { hide(leaning); rubble.forEach(hide); rubbleSign.visible = false; [bracedWall, ...shores].forEach(m => { m.visible = true; }); shoreText.visible = true; L.sfx("rule");
      L.toast("In the PRESENT, the festival scaffolding has held the arcade wall for a hundred years. The lane to Pell and Lise is open.", 5); });
    L.hazard({ group: S, mask: 2, at: [-33.8, 0, 1.5], size: [3, 2.4, 3], idle: 4.5, warn: 1.8, active: 1.1, offset: 1, disabledBy: F.braced, msg: "Masonry. Watch for the red light and falling grit before it drops." });
    for (const x of [-41, -38.5, -36, -33.6]) L.box(PA, 0.18, 9, 0.18, "wood", x, 4.5, 3.4, { solid: false });
    for (const y of [3, 6]) L.box(PA, 8, 0.12, 0.9, "woodDark", -37.3, y, 3.25, { solid: false });
    const timberShores = [-40, -37, -34].map(x => { const m = L.box(PA, 0.25, 5.6, 0.25, "wood", x, 2.6, 2.55, { dynamic: true, solid: false }); m.rotation.x = -0.44; m.visible = false; return m; });
    L.wall(PA, -33, -1, -33, 2.9, 2.2, "wood", 0.25);
    L.label(PA, "WORKS - NO PASSAGE\nFESTIVAL REPAIRS", -32.8, 1.6, 1, face(-32.8, 1, -20, 1), 0.3, "#3a2010", "rgba(240,225,190,.95)");
    L.box(PA, 0.5, 1, 0.5, "brass", -32.4, 0.5, 3.3);
    const clamp = L.box(PA, 0.42, 0.9, 0.42, "brass", -32.4, 1.45, 3.3, { solid: false, dynamic: true });
    L.label(PA, "SCAFFOLD LOCK", -32.4, 2.4, 3.0, face(-32.4, 3, -20, 3), 0.25, "#ffe6b0");
    L.interact({ at: [-32.4, 1.45, 3.3], mesh: clamp, mask: 1, prompt: "Lock the scaffolding to the wall", once: true, onUse: () => {
      clamp.rotation.z = -1.2; timberShores.forEach(m => { m.visible = true; }); L.set(F.braced); L.sfx("clang");
      L.say([ln("Arin", "Locked. Shores against the brick... In a hundred years they'll be iron in the wall."), ln("Arin", "Back to the PRESENT. The lane should be standing.")], false); } });
    L.box(FU, 8, 5, 5.2, "ruin", -37, 2.5, 1.5);
    L.crystals(FU, 3, -36, 1, 3, 3, 4.8);
    L.label(FU, "SEALED SINCE THE SECOND TOLL", -32.9, 2.4, 1.5, face(-32.9, 1.5, -20, 1.5), 0.3, "#9ff0ff");
    L.lamp(S, -52, -4, "gas", 3.6);
    L.checkpoint(-48, 0, 2, 0, { size: [10, 2.5, 16] });

    // ---------------- Founders' Hall (optional side area) ----------------
    L.wallDoor(S, -32, 28, -7.4, 28, 14, "stone", 2.6, 3.2, 0.285, 0.6);
    L.wall(S, -32, 28, -32, 46, 14, "stone", 0.6); L.wall(S, -32, 46, -7.4, 46, 14, "stone", 0.6); L.wall(S, -7.4, 28, -7.4, 46, 14, "stone", 0.6);
    L.box(S, 25, 0.4, 18.6, "stone", -19.7, 14.2, 37); L.box(S, 24.6, 0.3, 18, "woodDark", -19.7, 7.2, 37, { solid: false });
    L.label(S, "FOUNDERS' HALL", -25, 4.2, 27.65, face(-25, 27.65, -25, 20), 0.55, "#ffd8a0");
    L.box(S, 6, 0.9, 3, "woodDark", -19.5, 0.45, 37); L.box(S, 5.6, 0.02, 2.6, "paper", -19.5, 0.91, 37, { solid: false });
    L.box(S, 0.4, 0.2, 0.4, "stone", -19.5, 1, 37.9, { solid: false }); L.box(S, 2.8, 0.01, 0.06, "red", -19.7, 0.93, 36.7, { solid: false });
    for (let i = 0; i < 8; i++) L.box(S, 0.6, 1, 0.6, "wood", -21.8 + (i % 4) * 1.5, 0.5, i < 4 ? 34.9 : 39.1);
    L.clue({ group: S, at: [-17.3, 0.96, 37.6], size: [0.6, 0.05, 0.45], prompt: "Read the Founders' evacuation plan", flag: "L07.planRead", id: "L07.plan", title: "The Founders' Evacuation Plan",
      text: "PHASE ONE (nine days before): the children of the listed families, quietly, by the tram stairs to the place with no address. Strike their names from the public record. No panic.\n\nPHASE TWO (the night): the square. The bell rings ONCE as the warning.\n\nIf the bell rings TWICE, the rescue has failed. Seal the tower. Call it an earthquake.\n\n- A. Halden, for the Founders" });
    L.box(S, 7, 3.6, 0.1, "woodDark", -19.5, 3.2, 45.6, { solid: false });
    L.label(S, "LISTED FAMILIES\nOSKEL (Lane) ........ STRUCK\nVEYRA (Row) ......... STRUCK\nFENWICK ............. STRUCK\nARCADE ROW .......... STRUCK\nMARROW .... remains (clockkeeper)", -19.5, 3.2, 45.5, face(-19.5, 45.5, -19.5, 37), 0.22, "#2a1a0a", "rgba(232,223,200,1)");
    L.light(S, 0xffc27a, 2.4, 14, -19.5, 5.5, 37);
    L.crowd(PA, -19.5, 34.4, 5, 1, 2);
    L.ambient("crowd", 0.25, 1);

    // ---------------- statues + Halden inscription ----------------
    [["SELA VEYRA", -6, 17], ["AUREL HALDEN", 0, 18.5], ["OREN VEYRA", 6, 17]].forEach(([n, x, z]) => {
      L.box(S, 1.8, 1.4, 1.8, "stone", x, 0.7, z);
      L.cyl(S, 0.45, 2.6, "copper", x, 2.7, z); L.sphere(S, 0.35, "copper", x, 4.3, z);
      L.label(S, n, x, 1.15, z - 0.92, Math.PI, 0.22, "#ffe6b0");
    });
    L.label(S, "(0)", 0, 5.6, 18.5, Math.PI, 1, "#cfefff");
    L.clue({ group: S, at: [0, 0.55, 17.55], size: [1.2, 0.5, 0.12], mat: "brass", prompt: "Read Founder Halden's inscription", flag: "L07.haldenRead", id: "L07.halden", title: "Plinth of Founder Aurel Halden",
      text: "AUREL HALDEN - FOUNDER, KEEPER OF THE SEAL\n\nWHAT WE HOLD TOGETHER, WE HOLD IN SILENCE.\n\nBelow, scratched into the bronze much later by an unsteady hand:\n'You held us in silence. We fell in silence. - a daughter of Oskel Lane'" });

    // ---------------- dressing per state ----------------
    L.bunting(PA, -31, 31, -18, 9); L.bunting(PA, -31, 31, 6, 9.5);
    L.lamp(PA, -24, -8, "gas"); L.lamp(PA, 24, -8, "gas"); L.lamp(PA, 0, 8, "gas");
    L.label(PA, "FOUNDERS' FESTIVAL\nTHE BELL RINGS ONCE FOR PLENTY", 0, 7.5, 25.5, Math.PI, 0.6, "#ffe0a0");
    L.particles(PA, "dust", [60, 8, 50], [0, 1, 0], 450);
    L.crowd(PA, -6, -6, 30, 16, 6);
    L.ambient("crowd", 0.4, 1);
    L.particles(PR, "rain", [70, 18, 70], [0, 0, 6], 1300);
    L.lamp(PR, -26, -10, "sodium"); L.lamp(PR, 26, -10, "sodium"); L.lamp(PR, -14, 14, "sodium");
    for (let i = 0; i < 6; i++) L.box(PR, 2 + (i % 3), 0.02, 1.6 + (i % 2), "puddle", -24 + i * 9.5, 0.02, -14 + (i * 13) % 36, { solid: false });
    L.box(PR, 12, 1.4, 0.1, "red2", 0, 6, 25.5, { solid: false });
    L.label(PR, "EVACUATE - TRAM HALL SOUTH-WEST", 0, 6, 25.4, Math.PI, 0.55, "#ffffff");
    L.ambient("rain", 0.4, 2); L.ambient("fire", 0.25, 2);
    L.particles(FU, "motes", [60, 12, 60], [0, 0, 8], 500);
    L.lamp(FU, -25, -8, "neon"); L.lamp(FU, 25, -8, "neon"); L.lamp(FU, 14, 14, "neon");
    L.debris(FU, 8, 0, 6, 0, 50, 6, 36);
    L.holoMesh(FU, "box", "magenta", -31, 8, -4, 0.1, 3, 5, { bob: 0.1 });
    L.label(FU, "VEYRA\nREMEMBERS", -30.8, 8, -4, Math.PI / 2, 0.8, "#ffa0f0");
    L.holoMesh(FU, "torus", "cyan", 0, 0.08, 2, 14, 14, 0.2, { spin: 0.15 }).rotation.x = Math.PI / 2;
    L.ambient("drone", 0.35, 4);

    // ---------------- clock tower ----------------
    L.wallDoor(S, -7, 27, 7, 27, 24, "stone", 3, 3.6, 0.5, 0.8);
    L.wall(S, -7, 41, 7, 41, 24, "stone", 0.8); L.wall(S, -7, 27, -7, 41, 24, "stone", 0.8); L.wall(S, 7, 27, 7, 41, 24, "stone", 0.8);
    L.label(S, "THE FOUNDERS' CLOCK", 0, 5, 26.5, Math.PI, 0.6, "#ffd8a0");
    L.door(S, 3, 3.6, 0.35, "woodDark", 0, 0, 27, [F.bell], [0, 3.7, 0], { toast: "The tower door unbars itself. The way up to the core is open." });
    L.stairs(S, [4.5, 0, 29], [4.5, 6, 38], 2.4, "wood");
    L.floor(S, 0, 39.3, 13.2, 2.6, 6, "brass");
    L.floor(S, -4.1, 33.5, 5, 9.6, 6, "brass");
    L.wall(S, -1.6, 28.7, -1.6, 38, 1, "brass", 0.15, 6);
    L.box(PA, 14, 0.4, 14, "woodDark", 0, 24.2, 34); L.box(PR, 14, 0.4, 14, "woodDark", 0, 24.2, 34);
    L.box(PA, 14, 14, 14, "stone", 0, 31, 34, { solid: false }); L.box(PR, 14, 14, 14, "stone", 0, 31, 34, { solid: false });
    L.cyl(PA, 4, 0.2, "paper", 0, 30, 26.85).rotation.x = Math.PI / 2; L.cyl(PR, 4, 0.2, "cold", 0, 30, 26.85).rotation.x = Math.PI / 2;
    L.mesh(PA, "cone4", "copper", 0, 42, 34, 12, 8, 12); L.mesh(PR, "cone4", "copper", 0, 42, 34, 12, 8, 12);
    L.sphere(PA, 2.1, "brass", 0, 17, 34); L.sphere(PR, 2.1, "brass", 0, 17, 34);
    const ruinW = L.box(FU, 5.5, 13, 14, "ruin", -4.2, 30, 34, { solid: false, dynamic: true }); ruinW.rotation.z = 0.19;
    const ruinE = L.box(FU, 5, 11, 14, "ruin", 4.6, 29, 34, { solid: false, dynamic: true }); ruinE.rotation.z = -0.26;
    L.holoMesh(FU, "cyl", "magenta", 0, 60, 34, 1.1, 36, 1.1);
    for (let i = 0; i < 3; i++) L.holoMesh(FU, "torus", i % 2 ? "cyan" : "magenta", 0, 36 + i * 6, 34, 7 + i * 2, 7 + i * 2, 1, { spin: 0.4 + i * 0.2 }).rotation.x = Math.PI / 2;
    L.holoMesh(FU, "cyl", "cyan", 0, 30, 26.4, 7, 0.05, 7).rotation.x = Math.PI / 2;
    L.label(FU, "[#]", 0, 30, 26.2, Math.PI, 5, "#ffa0f0");
    L.label(FU, "TRANSMITTING >> TOLL 2    SIGNED (0)", 0, 25.6, 26.2, Math.PI, 0.8, "#ffb0f0");
    L.holoMesh(FU, "sphere", "amber", 0, 17, 34, 4.2, 4.6, 4.2, { bob: 0.2 });
    L.light(FU, 0xff50c8, 6, 50, 0, 40, 34);
    L.light(S, 0xffc080, 2.4, 12, 0, 3, 30.5); L.light(S, 0xffc080, 2.4, 14, 0, 13, 33);
    for (let i = 0; i < 3; i++) { const gr = L.cyl(S, 1.4 - i * 0.25, 0.3, "brass", -6.3, 10 + i * 3.2, 30 + i * 4); gr.rotation.z = Math.PI / 2; gr.userData.dynamic = true; L.update(dt => { if (!L.get(F.bell)) gr.rotation.x += dt * (i % 2 ? -0.5 : 0.4); }); }
    // core + Zero Chamber door
    for (let i = 0; i < 3; i++) L.holoMesh(S, "torus", i === 1 ? "amber" : "cyan", -4.1, 8.2 + i * 0.2, 32.5, 3.2 - i * 0.7, 3.2 - i * 0.7, 1, { spin: 0.7 + i * 0.4 }).rotation.x = Math.PI / 2 + i * 0.3;
    L.label(S, "0", -4.1, 8.2, 32.5, Math.PI / 2, 2, "#eaf8ff");
    L.light(S, 0x9ae6ff, 4, 10, -4.1, 8.5, 33);
    L.box(S, 2.2, 2.6, 0.02, "zero", -4.1, 7.3, 40.55, { solid: false });
    L.label(S, "(0)  ZERO CHAMBER", -4.1, 9.3, 40.45, Math.PI, 0.35, "#dff4ff");
    L.door(S, 2.4, 2.8, 0.3, "obsidian", -4.1, 6, 40.3, [F.core], [0, 3, 0], { toast: "The Zero Chamber opens." });
    L.zone({ at: [-4.1, 7, 33.5], size: [4.8, 2, 9.6], flag: F.core, requires: [F.bell], lines: [ln("Arin", "The core. It's humming in all three states at once."), ln("The Witness", "Below the bell there is a room that was here before the city. Go in.", 4)] });
    L.zone({ at: [-4.1, 7, 39.3], size: [4.8, 2, 2.4], flag: F.core, requires: [F.bell] });
    L.zone({ at: [-4.1, 7.2, 39.6], size: [2.4, 2.4, 1.2], flag: "L07.enteredChamber", requires: [F.core] });
    L.exit({ group: S, at: [-4.1, 6, 39.3], size: [2.4, 2.5, 1.6], requires: [F.core], locked: "The Zero Chamber is sealed. Reach the core first - the bell must be stopped." });
    L.checkpoint(0, 0, 29, Math.PI, { size: [4, 2.5, 2.5] });
    L.ambient("machine", 0.35, 3);
    L.clue({ group: PA, at: [10.5, 0.95, 21.5], size: [0.6, 0.08, 0.45], prompt: "Read the clockkeeper's notes", flag: "L07.notesRead", id: "L07.keeperNotes", title: "Clockkeeper's Notes (Festival Year)",
      text: "Three drives sit under the bell. I know them by their housings, never by number:\n  /\\  the spire     (0)  the wheel     [#]  the cage\n\nOne of them strikes the second toll - and the tower chooses which. Some nights I hear it choosing.\n\nThe order comes from above. From a part of the tower that has not been built yet.\n\n- I. Marrow, clockkeeper" });
    L.box(PA, 2, 0.9, 1, "wood", 10.5, 0.45, 21.5);

    // ---------------- crisis: timer + tremors ----------------
    L.timer({ duration: 480, startFlag: F.start, successFlag: F.core, label: "SECOND BELL IN", startToast: "The bell has rung once. Get them out before it rings again.",
      failMessage: "The bell rings a second time. Founders' Square falls - and the cycle begins again." });
    L.zone({ at: [0, 1, 0], size: [62, 2, 46], flag: F.start, lines: [ln("Odile Fenwick", "That's the first toll! Everyone - to the Tram Hall, south-west!"), ln("Arin", "Three groups still out there. And the bell is winding up for the second strike.")] });
    const masonry = (x, z, cx, cy, cz, off, requires, idle, warn) => {
      const chunk = L.box(S, 1.6, 0.7, 1.2, "stoneWet", cx, cy, cz, { solid: false, dynamic: true });
      const h = L.hazard({ group: S, mask: 2, at: [x, 0, z], size: [3, 2.4, 3], idle: idle || 4.5, warn: warn || 1.8, active: 1.1, offset: off, requires, disabledBy: F.bell,
        visual: chunk, drop: cy - 0.4, msg: "Masonry. Watch for the red light and falling grit before it drops." });
      L.particles(S, "debris", [1.4, 1, 1.2], [cx, cy - 1.5, cz], 40);
      return h;
    };
    masonry(-29.5, -12, -31.4, 8, -12, 0, [F.start]);
    masonry(29.5, -14, 31.4, 8, -14, 1, [F.start]);
    masonry(-18, 25.5, -18, 10, 27.4, 3, [F.start]);

    // ---------------- Keeper's Alley: open only in the FUTURE ----------------
    L.box(PR, 9, 3.6, 1.6, "stoneWet", 11.5, 1.8, 29.5);
    L.label(PR, "VOICES BEHIND THE RUBBLE", 11.5, 2.6, 28.6, Math.PI, 0.3, "#ffa080");
    L.wall(PA, 7, 29.5, 16, 29.5, 3.4, "rust", 0.3);
    L.label(PA, "KEEPER'S YARD - LOCKED", 11.5, 2.8, 29.3, Math.PI, 0.3, "#ffe6b0");
    L.label(FU, "KEEPER'S ALLEY", 11.5, 6, 26.2, Math.PI, 0.6, "#9ff0ff");
    // grounding cable at the fallen market pylon
    const pylon = L.box(FU, 0.6, 0.6, 7, "ruin", 27, 0.6, 17.5, { solid: false, dynamic: true }); pylon.rotation.y = 0.6;
    L.label(FU, "GROUNDING CABLE", 25.5, 1.4, 15, face(25.5, 15, 15, 5), 0.25, "#a0ffd8");
    L.item({ group: FU, at: [25.5, 0.35, 15], size: 0.7, mat: "copper", key: "cable", name: "grounding cable", pickedFlag: "L07.cableTaken" });
    L.socket({ group: FU, at: [11.5, 0.05, 24.5], radius: 0.5, accepts: "cable", flag: F.grounded, prompt: "Clamp the cable to the earth spike", hint: "An earth spike. It wants a grounding cable (the fallen pylon in the market).", wrong: "The spike wants a grounding cable." });
    L.cyl(FU, 0.12, 1.8, "copper", 12.6, 0.9, 24.5);
    L.label(FU, "EARTH SPIKE\nGROUND THE SURGE", 12.6, 2.3, 24.4, Math.PI, 0.25, "#a0ffd8");
    L.when([F.grounded], () => { L.sfx("rule"); L.toast("The surge bleeds into the earth spike. Keeper's Alley goes quiet - and the relay wakes.", 4); });
    [31, 37].forEach((z, i) => {
      L.mesh(FU, "octa", "crystal", 7.8, 1.4, z, 0.8, 2.8, 0.8); L.mesh(FU, "octa", "crystal", 15.2, 1.4, z, 0.8, 2.8, 0.8);
      const arc = L.box(FU, 7.2, 0.14, 0.14, "neonM", 11.5, 1.4, z, { solid: false, dynamic: true, shadow: false });
      const glow = L.holoMesh(FU, "box", "magenta", 11.5, 1.1, z, 7.2, 1.6, 0.3);
      const h = L.hazard({ group: FU, mask: 4, at: [11.5, 0, z], size: [9, 2.6, 1.6], idle: 2.2, warn: 1.5, active: 2.2, offset: i * 1.6, disabledBy: F.grounded, color: 0xff30a0,
        msg: "The surge arcs across the alley. Pink light means it is about to fire - or ground it." });
      L.update(() => { const on = h.phase === "active" && !L.get(F.grounded); arc.visible = on; glow.visible = on; });
    });
    L.checkpoint(11.5, 0, 24, 0, { size: [7, 2.5, 2] });
    L.clue({ group: FU, at: [11.5, 1.1, 43.8], size: [2.4, 2.2, 0.6], mat: "obsidian", prompt: "Read the tower's signal", flag: F.signal, id: "L07.signal", title: "Relay: Outbound Signal",
      requires: [F.grounded], locked: "The relay is drowned in surge static. Ground the surge first - the earth spike at the alley mouth.",
      text: "SOURCE: CLOCK TOWER - AFTER\nTARGET: BELL DRIVE [#]  (III, THE CAGE)\nCOMMAND: STRIKE TOLL 2\nREPEAT: EVERY CYCLE\n\nThe transmission comes from the tower's own future ruins, and it is addressed to the bell's past.\n\nSIGNATURE: (0)\nThe zero is drawn in one stroke, starting at the bottom and closing with a small hook - exactly the way Arin writes it.",
      lines: [ln("Arin", "Drive III. The cage. And at the end... a zero, one stroke, a hook where it closes."), ln("Arin", "That's how I sign my name. Who in the future signs with my hand?", 4)] });
    const relayScreen = L.holoMesh(FU, "box", "magenta", 11.5, 3.6, 43.6, 3.2, 1.8, 0.04, { bob: 0.05 });
    const relayText = L.label(FU, "[#]   DRIVE III >> TOLL 2   -- (0)", 11.5, 3.6, 43.5, Math.PI, 0.45, "#ffe0ff");
    const stat = L.label(FU, "## STATIC ## OVERLOAD", 11.5, 3.6, 43.45, Math.PI, 0.45, "#ff6060");
    relayScreen.visible = relayText.visible = false;
    L.when([F.grounded], () => { relayScreen.visible = relayText.visible = true; stat.visible = false; });
    L.light(FU, 0x60d8ff, 2.4, 12, 11.5, 4, 40);
    L.ambient("machine", 0.3, 4);

    // ---------------- evacuation groups (follow the player, saved in the Tram Hall) ----------------
    const groups = [];
    const evac = (name, cx, cz, n, flag, coats, follow, toast) => {
      const members = [];
      for (let i = 0; i < n; i++) {
        const p = L.person(S, cx + (i % 3 - 1) * 1.0, cz + Math.floor(i / 3) * 1.1, coats[i % coats.length], i * 1.7, 0.95);
        members.push({ p, ox: (i % 3 - 1) * 0.9, oz: -Math.floor(i / 3) * 0.9, ph: i });
      }
      const marker = L.holoMesh(S, "torus", "amber", cx, 2.8, cz + 0.5, 0.6, 0.6, 0.6, { spin: 1.2, bob: 0.08 }); marker.rotation.x = Math.PI / 2;
      const tag = L.label(S, name.toUpperCase(), cx, 3.3, cz + 0.5, 0, 0.25, "#ffd890");
      const G = { name, members, following: false, saved: false, flag };
      const it = L.interact({ at: [cx, 1, cz + 0.5], radius: 3.4, prompt: "Lead " + name + " to the Tram Hall", onUse: () => {
        if (G.saved || G.following) return; G.following = true; it.enabled = false; L.say(follow, false); L.sfx("use"); } });
      G.it = it;
      L.update((dt, t) => {
        tag.rotation.y = Math.atan2(AZ.player.pos.x - cx, AZ.player.pos.z - (cz + 0.5));
        if (!G.following || G.saved) return;
        const P = AZ.player, fx = -Math.sin(P.yaw), fz = -Math.cos(P.yaw), rx = Math.cos(P.yaw), rz = -Math.sin(P.yaw);
        let inside = 0;
        for (const m of members) {
          const g = m.p.group;
          const tx = P.pos.x - fx * 2.4 + rx * m.ox + fx * m.oz, tz = P.pos.z - fz * 2.4 + rz * m.ox + fz * m.oz;
          const dx = tx - g.position.x, dz = tz - g.position.z, d = Math.hypot(dx, dz);
          if (d > 0.4) {
            const s = Math.min(d, 3.4 * Math.min(1.7, Math.max(0.45, d / 3)) * dt);
            g.position.x += dx / d * s; g.position.z += dz / d * s; g.rotation.y = Math.atan2(dx, dz);
            const sw = Math.sin(t * 8 + m.ph) * 0.6; m.p.armL.rotation.x = sw; m.p.armR.rotation.x = -sw;
          }
          if (Math.hypot(g.position.x - SAFE[0], g.position.z - SAFE[1]) < 3.5) inside++;
        }
        if (inside >= Math.ceil(members.length * 0.6)) {
          G.saved = true; G.following = false; L.set(flag); L.sfx("objective"); L.toast(toast, 3.5); marker.visible = false; tag.visible = false;
          members.forEach((m, i) => { m.p.group.position.set(SAFE[0] + (i % 3 - 1) * 1.1 + (groups.indexOf(G) - 1) * 3.3, 0, SAFE[1] + Math.floor(i / 3) * 1.1); m.p.armL.rotation.x = m.p.armR.rotation.x = 0; });
        }
      });
      groups.push(G);
      return G;
    };
    evac("Market Row group", 40.5, 1.2, 4, F.g1, ["ochre", "red", "teal", "grey"], [ln("Vendor", "Through the fire? - No, it's... gone? Where are we? Just follow, just follow!"), ln("Mirela Dovan", "Stay behind Arin. Don't look at the flames.")], "Mirela's market group is safe in the Tram Hall.");
    evac("Ashgrove courtyard group", -49, 4.5, 3, F.g2, ["navy", "olive", "white"], [ln("Resident", "The lane's open? The wall held? Then go - we're with you!"), ln("Pell Ashgrove", "Lise, hold my coat. We're going.")], "Pell, Lise and the courtyard group are safe in the Tram Hall.");
    evac("Warden's drill group", -1, 43.4, 4, F.g3, ["grey", "navy", "burgundy", "gold"], [ln("Drill volunteer", "The alley's clear? In this light it looks like ruins... go, go, we're behind you!"), ln("Warden Roe", "Single file! Follow Arin!")], "Warden Roe's group is safe in the Tram Hall.");
    L.W.evac = groups; // exposed for the scripted walkthrough (test/solve/L07.js)
    [F.g1, F.g2, F.g3].forEach((f, i) => { L.when([f], () => L.set("L07.g" + (i + 1) + "Resolved")); });
    L.when([F.signal], () => { ["L07.g1Resolved", "L07.g2Resolved", "L07.g3Resolved"].forEach(f => L.set(f)); });
    L.when([F.g1, F.g2, F.g3], () => { L.set(F.all); L.sfx("solved"); L.toast("Founders' Square is clear. All three groups are in the Tram Hall.", 4); });
    L.crowd(PR, SAFE[0], SAFE[1] + 0.5, 7, 2, 4);
    L.ambient("crowd", 0.3, 2);

    // ---------------- named NPCs ----------------
    L.npc({ name: "Mirela Dovan", at: [38.5, -0.5], yaw: -Math.PI / 2, coat: "ochre", task: "Cup vendor of Market Row. Packing her blue cups into a crate while the lane burns, refusing to leave her neighbours.",
      route: [[37.5, -0.6, 3, "carry"], [42.5, 2.6, 4, "work"], [39, 3, 3, "look"]], approach: ["Don't come through the fire!", "Arin? I know your face. From the festival."],
      talk: [ln("Mirela Dovan", "The whole lane caught at the first toll. The fire is between us and the square."), ln("Mirela Dovan", "My grandfather Hale built carts for this market. He used to say the lane is older than the fire."), ln("Mirela Dovan", "Older than the fire... unless he meant the lane was here before it burned."), ln("Mirela Dovan", "If there's another way through, it isn't tonight's way."), ln("Mirela Dovan", "Get my people out, Arin. I'll walk behind them.")],
      cond: [{ flag: "L01.cupSaved", lines: [ln("Mirela Dovan", "You caught my blue cup once. On a festival morning, before it hit the stones."), ln("Mirela Dovan", "I dreamed it, I thought. But the cup is still on my shelf, and it has never once been broken.")] },
        { flag: F.plan, lines: [ln("Mirela Dovan", "Market Row on the Founders' plan? Struck out nine days early?"), ln("Mirela Dovan", "They took the children from the lane and left us the cups.")] }],
      changedFlag: F.g1, changedRoute: [[36, 1, 0.1], [28, -4, 0.1], [SW[0], SW[1], 0.1], [GATE[0], GATE[1], 0.1], [SAFE[0] - 3, SAFE[1] + 1.5, 9999, "talk"]],
      changedTalk: [ln("Mirela Dovan", "Every one of them, safe. Through a lane that wasn't burning. How?"), ln("Mirela Dovan", "Never mind how. Thank you, Arin. Go stop that bell.")] });
    L.npc({ name: "Pell Ashgrove", at: [-47, 0], yaw: Math.PI / 2, coat: "navy", task: "Courier and Rememberer. Shoring the courtyard door with his satchel strap while keeping his daughter Lise in sight.",
      route: [[-51, -4, 4, "work"], [-45, 7, 3, "carry"], [-51.5, 8, 3, "look"]], approach: ["Arin. Again. I always find you again.", "Don't stand under that wall!"],
      talk: [ln("Pell Ashgrove", "I've delivered your letter more times than there are days. I know your face better than my own street."), ln("Pell Ashgrove", "Tonight there's no letter. Just the wall, and us behind it."), ln("Pell Ashgrove", "Odile keeps the tally of every night. I keep the tally of Lise. Every night she's nine. Every night she's here."), ln("Pell Ashgrove", "If the loop ever breaks... I don't know if she comes with us. Odile says she might not."), ln("Pell Ashgrove", "Brace that wall and get us out. I'll worry about dawn at dawn.")],
      cond: [{ flag: "evidence.list", lines: [ln("Pell Ashgrove", "The evacuation list. Ashgrove isn't on it. Lise isn't on anything."), ln("Pell Ashgrove", "She was born inside the repeat. The Founders never planned for anyone like her.")] },
        { flag: F.braced, lines: [ln("Pell Ashgrove", "The wall's standing straight? Those old poles rusted into the brick..."), ln("Pell Ashgrove", "Somebody locked them a century ago. You, I'd bet. You always come the long way.")] }],
      changedFlag: F.g2, changedRoute: [[-44, 1.5, 0.1], [-30, 1.5, 0.1], [SW[0], SW[1], 0.1], [GATE[0], GATE[1], 0.1], [SAFE[0] + 3, SAFE[1] + 1.5, 9999, "idle"]],
      changedTalk: [ln("Pell Ashgrove", "Out of the courtyard, both of us. Whatever happens at dawn - tonight you got her out."), ln("Pell Ashgrove", "Odile will want the truth published. I want it too. I just want Lise more.")] });
    L.npc({ name: "Lise Ashgrove", at: [-49.5, 7.5], yaw: 2, coat: "burgundy", scale: 0.72, task: "Pell's nine-year-old, born inside the loop. Drawing chalk zeros on the courtyard stones to stay brave.",
      route: [[-49.5, 7.5, 3, "look"], [-46, 9.5, 3, "work"], [-50.5, 2, 3, "idle"]], approach: ["Are you the one who catches cups?", "Papa says don't talk to people who remember."],
      talk: [ln("Lise Ashgrove", "I'm Lise. I'm nine. I've been nine for ever so long."), ln("Lise Ashgrove", "I draw zeros on the stones because they come back every morning. Nothing else I draw comes back."), ln("Lise Ashgrove", "At the festival I always run into the cup stall. Always. Even when I try not to."), ln("Lise Ashgrove", "Papa holds my hand very tight at night. Like I might go somewhere."), ln("Lise Ashgrove", "When the wall stops leaning, can we go? Papa says only when the wall stops leaning.")],
      cond: [{ flag: "arin.watch", lines: [ln("Lise Ashgrove", "Is that a watch? It isn't ticking."), ln("Lise Ashgrove", "Nothing here ticks. Papa says time only goes round, not forward.")] },
        { flag: "L01.cupSaved", lines: [ln("Lise Ashgrove", "You're the one who caught the cup when I bumped the stall!"), ln("Lise Ashgrove", "Mirela wasn't cross that time. She's always cross. That time she laughed.")] }],
      changedFlag: F.g2, changedRoute: [[-44, 2.5, 0.1], [-30, 2.5, 0.1], [SW[0], SW[1] + 1, 0.1], [GATE[0] + 1, GATE[1], 0.1], [SAFE[0] + 4, SAFE[1] + 2.2, 9999, "look"]],
      changedTalk: [ln("Lise Ashgrove", "We got out! I'm going to draw you a zero. A big one."), ln("Lise Ashgrove", "Will I still be here tomorrow? Papa won't say.")] });
    L.npc({ name: "Warden Roe", at: [-4, 43], yaw: Math.PI / 2, coat: "navy", task: "Square warden. Running the evacuation drill from a signal lantern behind the tower, where his drill group got cut off.",
      route: [[-4, 43, 4, "work"], [3, 44.6, 3, "look"], [5.5, 42.4, 3, "idle"]], approach: ["Arin! You found us!", "Mind the arcs - watch for the pink light!"],
      talk: [ln("Warden Roe", "Twenty years of drills. Tonight I led my volunteers the wrong way round the tower."), ln("Warden Roe", "Rubble came down at the first toll and sealed Keeper's Alley. In the old days there was a locked yard gate there instead."), ln("Warden Roe", "And when the light goes strange and violet, the alley's open - but it spits lightning."), ln("Warden Roe", "There's a cable by the fallen pylon in the market, if the market's even there in that light. Ground the sparks."), ln("Warden Roe", "Then come back for us. The Tram Hall, not the stairs in the open. Halden will be at the gate.")],
      cond: [{ flag: "L07.observed", lines: [ln("Warden Roe", "You saw us from the terrace? Then somebody was watching the drill after all.")] },
        { flag: F.halden, lines: [ln("Warden Roe", "Founder Halden's plinth? My grandfather called that motto a promise."), ln("Warden Roe", "Read the scratches under it and tell me it isn't an apology.")] }],
      changedFlag: F.g3, changedRoute: [[9, 43.4, 0.1], [11.5, 30.5, 0.1], [9, 22, 0.1], [SW[0], SW[1], 0.1], [GATE[0], GATE[1], 0.1], [SAFE[0], SAFE[1] + 2.6, 9999, "idle"]],
      changedTalk: [ln("Warden Roe", "Drill group, all present. In twenty years I never got everyone out. Tonight I nearly didn't."), ln("Warden Roe", "Thank you, Arin.")] });
    L.box(S, 0.2, 2, 0.2, "iron", -4.6, 1, 44.6); L.sphere(S, 0.25, "neonA", -4.6, 2.1, 44.6);
    L.box(S, 0.4, 1.8, 0.4, "iron", -4.5, 0.9, -19); L.box(S, 0.9, 0.7, 0.05, "paper", -4.5, 1.9, -19.25, { solid: false });
    L.npc({ name: "Odile Fenwick", at: [0, -20], yaw: Math.PI, coat: "grey", task: "Head archivist and Rememberer. Writing every evacuee's name on a tally board, so that this time nobody is struck from the record.",
      route: [[-3.8, -19.6, 4, "work"], [4, -19, 3, "look"], [0, -16.5, 3, "idle"]], approach: ["Arin. You remember me this time - I can see it.", "Tally first, panic later."],
      talk: [ln("Odile Fenwick", "I keep the record honest. Tonight that means writing down everyone who gets out - and everyone who doesn't."), ln("Odile Fenwick", "Founder Halden is at the Tram Hall gate. He'd rather people die quietly than live loudly."), ln("Odile Fenwick", "Show him proof and he may break. The list, the record, the register. Without them... there's the back lane."), ln("Odile Fenwick", "The back lane runs from the arcade courtyard to the hall's service door. Mother Ondine's Keepers walk it."), ln("Odile Fenwick", "The Keepers want this night to go on forever. The Rememberers want it to end and be known. Tonight, we just want it survived.")],
      talkedFlag: F.start,
      cond: [{ flag: "evidence.record", lines: [ln("Odile Fenwick", "You took Maren Oskel's record out of my archive. The one with your countersignature."), ln("Odile Fenwick", "Keep it close. Halden signed the order that made it necessary.")] },
        { flag: "choice.L05.records", lines: [ln("Odile Fenwick", "The class register - you brought it out of the Drowned District."), ln("Odile Fenwick", "Do you know how many names in it were struck? I do. I've counted them every night.")] }],
      changedFlag: F.route, changedRoute: [[-8, -22, 0.1], [GATE[0] - 1, GATE[1], 0.1], [SAFE[0] - 4, SAFE[1] - 1, 9999, "work"]],
      changedTalk: [ln("Odile Fenwick", "The hall's open. I'll take the names at the platform."), ln("Odile Fenwick", "Go get the others, Arin. I'm leaving room in the book.")] });
    L.npc({ name: "Ilse Marrow", at: [-3, 19.2], yaw: Math.PI, coat: "olive", task: "The Founders' clockkeeper. Fighting the three bell drives with a spanner, refusing to leave the foot of her tower.",
      route: [[-3, 19.2, 5, "work"], [3.2, 19.2, 4, "work"], [9.5, 21.5, 3, "look"]], approach: ["Stay back from the drives - they kick!", "You. You've got the look of someone who's seen this before."],
      talk: [ln("Ilse Marrow", "Ilse Marrow. Clockkeeper. That bell has not obeyed me since the first toll."), ln("Ilse Marrow", "Three drives under the bell. One of them is listening to something that isn't me."), ln("Ilse Marrow", "My brother Ignatz is running the trams tonight. If you see him, tell him I'm staying."), ln("Ilse Marrow", "Don't touch the drives blind. The wrong one throws the whole bell forward and the clock jumps."), ln("Ilse Marrow", "You have a face I raised once. That's impossible. Everything tonight is impossible - so help me anyway.")],
      cond: [{ flag: F.signal, lines: [ln("Ilse Marrow", "The relay showed [#] - and a mark? A zero in one stroke, with a hook..."), ln("Ilse Marrow", "That isn't a Founder's seal. It's a signature. It's your hand, Arin. How can it be your hand?")] },
        { flag: "arin.watch", lines: [ln("Ilse Marrow", "That watch. Brass, dented at the crown. 'For A.'"), ln("Ilse Marrow", "I haven't engraved it yet. I've been meaning to. For a child who hums all four notes.")] },
        { flag: F.notes, lines: [ln("Ilse Marrow", "My notes, from the festival year. I was younger than you are now."), ln("Ilse Marrow", "Even then I could hear it choosing. I just never knew who it was listening to.")] }],
      changedFlag: F.bell, changedRoute: [[0, 25.2, 0.1], [0, 28.8, 0.1], [2.4, 30.6, 9999, "work"]],
      changedTalk: [ln("Ilse Marrow", "The door's open. I'm going in - someone has to hold the engine while you climb."), ln("Ilse Marrow", "Keep time for me.")] });
    L.npc({ name: "Tobiah Venn", at: [8.5, 19.6], yaw: 3.4, coat: "burgundy", task: "Bell ringer. Oiling the drive linkages because it's the only thing his hands know how to do tonight.",
      route: [[8.6, 19.6, 4, "work"], [4, 15.5, 3, "look"], [10, 14, 3, "idle"]], approach: ["Don't touch the drives unless you know which!", "The bell's never rung on its own before."],
      talk: [ln("Tobiah Venn", "I didn't pull any rope. The bell rang itself."), ln("Tobiah Venn", "One of the three drives is striking on its own: the spire, the wheel or the cage."), ln("Tobiah Venn", "Disengage the wrong one and the whole bell lurches - you'll lose minutes we haven't got."), ln("Tobiah Venn", "Ilse says the drive that's wrong will want a tune to stop, not a lever. She won't say which tune."), ln("Tobiah Venn", "Something in the future is talking to my bell. Find out what it's saying.")],
      cond: [{ flag: "L06.lullabyComplete", lines: [ln("Tobiah Venn", "You know the Nursery song? All four notes?"), ln("Tobiah Venn", "Then you know the drive's tune. One, three, five, four - the founders built the bell around a lullaby.")] },
        { flag: F.halden, lines: [ln("Tobiah Venn", "'What we hold together, we hold in silence.' We sing that line every festival."), ln("Tobiah Venn", "I always thought it was about harmony. Tonight it sounds like a secret.")] }],
      changedFlag: F.bell, changedRoute: [[4, 10, 0.1], [SW[0], SW[1], 0.1], [GATE[0] + 1.5, GATE[1], 0.1], [SAFE[0] + 2, SAFE[1] - 1.8, 9999, "idle"]],
      changedTalk: [ln("Tobiah Venn", "It's stopped. Thirty years I rang that bell, and never feared it till tonight."), ln("Tobiah Venn", "Thank you, Arin.")] });
    L.when([F.bell], () => setTimeout(() => L.say([ln("Ilse Marrow", "The drive's caught - but it won't hold by itself. I'm going in to hold it by hand.", 4), ln("Arin", "Ilse, the tower could come down on you.", 3), ln("Ilse Marrow", "Then it comes down on someone who chose it. Climb, Arin.", 3.5), ln("Ilse Marrow", "Keep time for me.", 3)], false), 4500));

    // ---------------- Founder Aurel Halden at the gate ----------------
    L.npc({ name: "Founder Aurel Halden", at: [-22, -24.3], coat: "white", task: "Founder. Guarding the Tram Hall gate with the Founders' ledger, deciding who may be told what the Founders did.",
      route: [[-24.5, -24.1, 4, "idle"], [-19.9, -24.1, 3, "look"], [-22, -23.4, 4, "work"]], approach: ["Stop there. This gate is not for spectators.", "You have Ilse's way of standing. Curious."],
      talk: [ln("Founder Aurel Halden", "I am Aurel Halden. I signed the evacuation. I signed the erasures. I signed the word 'earthquake'."), ln("Founder Aurel Halden", "If the square sees these people go down into the Tram Hall, the square will ask why there was a plan."), ln("Founder Aurel Halden", "And then they will find the list. Fourteen children, nine days early. And the families we struck out."), ln("Founder Aurel Halden", "I knew the rescue might fail. I knew it when I signed. Silence was the only part I could guarantee."), ln("Founder Aurel Halden", "Bring me proof that silence has already failed, and I will open the gate myself.")],
      cond: [{ flag: "evidence.list", lines: [ln("Founder Aurel Halden", "Where did you find that list? Nine days early, in my own hand."), ln("Founder Aurel Halden", "VALE, A. Guardian: I. Marrow... You. You were one of them.")] },
        { flag: F.plan, lines: [ln("Founder Aurel Halden", "You read the plan in my hall. Then you've seen 'call it an earthquake' in my handwriting.")] }],
      changedFlag: F.yield, changedRoute: [[-22, -27, 0.1], [-26, -30, 9999, "work"]],
      changedTalk: [ln("Founder Aurel Halden", "The gate is open. Let the square watch."), ln("Founder Aurel Halden", "My daughter will never forgive me. Tell her I chose it, at least.")] });
    const ledger = L.box(S, 0.6, 1.2, 0.5, "woodDark", -19.2, 0.6, -24.6);
    L.box(S, 0.55, 0.06, 0.4, "paper", -19.2, 1.23, -24.6, { solid: false });
    L.label(S, "FOUNDERS' LEDGER", -19.2, 1.65, -24.6, Math.PI, 0.18, "#ffe6b0");
    const proofs = ["evidence.list", "evidence.record", "choice.L05.records"];
    L.talkPoint({ at: [-19.2, 1, -24.6], mesh: ledger, prompt: "Confront Founder Halden", radius: 3 }, {
      speaker: "Founder Aurel Halden", question: "Founder Halden bars the Tram Hall gate. What do you do?", doneFlag: F.hTalked,
      opening: [ln("Founder Aurel Halden", "Not through this gate. If they go down in front of the whole square, all of Veyra learns what the Founders did.", 4.5), ln("Arin", "They'll die out here if they don't.", 2.5),
        ln("Founder Aurel Halden", "I know. I knew forty years ago. I signed the evacuation knowing the rescue might fail - and I signed it anyway.", 5), ln("Founder Aurel Halden", "Silence saved more of them than the truth would have. Prove me wrong. Or go around.", 4)],
      options: [
        { text: "Show him the list, the record and the register.", requires: proofs, missing: "(needs the list, the record and the register)", sets: F.yield,
          lines: [ln("Founder Aurel Halden", "The list. The Oskel girl's record - countersigned. The school register, out of the water.", 4.5), ln("Founder Aurel Halden", "Then the silence is already broken, and I am only standing in a doorway. Go. Take them down.", 4.5)] },
        { text: "Lay out whatever proof you have.", sets: "L07.haldenEvidence", lines: [ln("Arin", "This is what I carried out of the archive, the tunnels and the water. Look at it.", 3.5)],
          action: () => { const n = proofs.filter(f => L.get(f)).length; if (n >= 2) { L.set(F.yield); L.toast("Two proofs are enough. Founder Halden steps aside.", 4); } else { L.set("L07.haldenRefused"); L.toast("Not enough proof. Halden won't move. Take the back lane: arcade courtyard, south door.", 5); } } },
        { text: "Then I'll find another way.", sets: "L07.haldenRefused", lines: [ln("Founder Aurel Halden", "The back lane is full of my daughter's Keepers. Ondine is thorough.", 3.5), ln("Arin", "So am I.", 2)],
          action: () => L.toast("The back lane: through the braced arcade to the courtyard, then the south door. Keepers patrol it in the PRESENT.", 5) },
      ] });

    // ---------------- Mother Ondine Halden (after the signal) ----------------
    const ondine = L.npc({ name: "Mother Ondine Halden", at: [-10, 21], yaw: 2, coat: "grey", hood: true, task: "Mother of the Keepers of the Hour, Aurel's daughter. Directing her lantern-bearers from the forecourt so the night is never broken.",
      route: [[-10, 21, 4, "work"], [-12, 18, 3, "look"], [-9.5, 15, 3, "idle"]], approach: ["The anomaly. Closer than I'd like.", "Don't touch my father's bell."],
      talk: [ln("Mother Ondine Halden", "I am Ondine Halden. The Keepers keep the hour, because the hour keeps us."), ln("Mother Ondine Halden", "Inside the cycle nobody dies for good. Every child on that list comes back every morning."), ln("Mother Ondine Halden", "You call it a prison. I call it the only mercy anyone in this city was ever given."), ln("Mother Ondine Halden", "My father bought it with silence. I keep it with lanterns."), ln("Mother Ondine Halden", "Stop the bell, and tomorrow comes. Have you asked who tomorrow doesn't have room for?")],
      cond: [{ flag: F.yield, lines: [ln("Mother Ondine Halden", "My father stepped aside? Then he's finally tired."), ln("Mother Ondine Halden", "I am not.")] },
        { flag: "maren.isWitness", lines: [ln("Mother Ondine Halden", "The Witness is the Oskel girl? Grown, in the seam?"), ln("Mother Ondine Halden", "Then even she chose to stay inside time's mercy. Ask her why.")] }],
      changedFlag: F.bell, changedRoute: [[-15, 22, 0.1], [-25, 26.5, 0.1], [-25, 30, 9999, "idle"]],
      changedTalk: [ln("Mother Ondine Halden", "You stopped it. Tonight. It will want to ring again, and I will be listening."), ln("Mother Ondine Halden", "Go on down. Whatever you find under the bell, remember I warned you.")] });
    ondine.p.group.visible = false; ondine.mask = 0; ondine.it.enabled = false;
    L.when([F.signal], () => { ondine.mask = 7; ondine.it.mask = 7; ondine.it.enabled = true; });
    L.zone({ at: [11.5, 1, 25.5], size: [9, 2.5, 2], requires: [F.signal], onEnter: () => {
      L.dialogue({ speaker: "Mother Ondine Halden", question: "Mother Ondine speaks through the Keepers' horns.",
        opening: [ln("Mother Ondine Halden", "So you've read the signal. The Keepers' horns carry into every state, anomaly. I hear you.", 4.5), ln("Mother Ondine Halden", "Inside the cycle no one dies for good. My father bought that with silence. Don't you dare sell it.", 4.5)],
        options: [{ text: "Nobody lives for good either. The bell stops tonight.", sets: "L07.ondineDefied", lines: [ln("Mother Ondine Halden", "Then I'll make the tower fight you for every note.", 3.5)] },
          { text: "Your father admitted the rescue might fail. He told me himself.", requires: [F.hTalked], missing: "(speak to Founder Halden first)", sets: "L07.ondineShaken", lines: [ln("Mother Ondine Halden", "...He said that? To you?", 3), ln("Mother Ondine Halden", "Go, then. I won't add a single stone. But I won't help you either.", 4)] }] });
    } });

    // ---------------- forecourt: drives + the climax ----------------
    const xs = [-6, 0, 6], names = ["I", "II", "III"], glyphs = ["/\\", "(0)", "[#]"], shapes = ["the spire", "the wheel", "the cage"];
    const locked = "Three drives, one toll. Which one strikes the second? Find the signal (FUTURE >>, Keeper's Alley, east of the tower).";
    xs.forEach((x, i) => {
      L.box(S, 2.4, 0.5, 2.4, "stone", x, 0.25, 22);
      L.label(S, "DRIVE " + names[i] + "   " + glyphs[i], x, 0.32, 20.78, Math.PI, 0.25, "#ffe6b0");
      L.box(S, 0.15, 11, 0.15, "rust", x, 8, 26.45, { solid: false });
      if (i === 0) { const sp = L.mesh(PR, "octa", "iron", x, 1.6, 22, 1.4, 1.6, 1.4); }
      else if (i === 1) { const w = L.cyl(PR, 1, 0.4, "iron", x, 1.6, 22, { solid: true }); w.rotation.x = Math.PI / 2; w.userData.dynamic = true; L.update(dt => { if (!L.get(F.bell)) w.rotation.y += dt * 1.5; }); }
      else { L.box(PR, 1, 1.4, 1, "iron", x, 1.2, 22); for (const [cx, cz] of [[-0.7, -0.7], [0.7, -0.7], [-0.7, 0.7], [0.7, 0.7]]) L.box(PR, 0.1, 2, 0.1, "rust", x + cx, 1.5, 22 + cz, { solid: false }); L.box(PR, 1.6, 0.1, 1.6, "rust", x, 2.55, 22, { solid: false }); }
      L.label(PR, names[i] + "\n" + glyphs[i], x, 3.6, 22, Math.PI, 0.5, "#ffffff");
      L.mesh(PA, i === 1 ? "cyl" : i === 0 ? "octa" : "box", "brass", x, 1.5, 22, 1.2, 1.4, 1.2);
      L.box(FU, 1.4, 1.2, 1.2, "ruin", x, 1, 22, { solid: false }); L.label(FU, glyphs[i], x, 3.2, 22, Math.PI, 0.6, i === 2 ? "#ffa0f0" : "#7a8a9a");
      const lever = L.box(PR, 0.42, 0.9, 0.42, "brass", x + 1, 1, 21, { solid: false, dynamic: true });
      if (i === 2) L.interact({ at: [x + 1, 1, 21], mesh: lever, mask: 2, prompt: "Open the striking frame of drive III [#]", requires: [F.signal], locked, once: true, onUse: () => {
        lever.rotation.x = 1.2; L.set(F.driveOpen); L.sfx("rule");
        L.say([ln("Arin", "The cage opens - and six striking plates fold out around the forecourt."), ln("Ilse Marrow", "It won't stop for a lever. It stops for a tune. The same one that sang the children to sleep.", 4), ln("Ilse Marrow", "One, three, five, four. Strike them in order - and mind the stones.", 3.5)], false); } });
      else L.interact({ at: [x + 1, 1, 21], mesh: lever, mask: 2, prompt: "Open the striking frame of drive " + names[i] + " " + glyphs[i], requires: [F.signal], locked, onUse: () => {
        lever.rotation.x = 0.6; setTimeout(() => { lever.rotation.x = 0; }, 600); L.sfx("clang"); L.sfx("alert"); AZ.vibrate(80);
        L.penalize(30); L.toast("Wrong drive - " + shapes[i] + " only winds the hours. The bell lurches; the clock jumps ahead.", 3.5); } });
    });
    L.light(PR, 0xffa060, 2.4, 16, 0, 4.5, 18);
    const plates = [];
    for (let i = 0; i < 6; i++) {
      const a = (200 + i * 28) * Math.PI / 180, x = Math.cos(a) * 9.5, z = 22 + Math.sin(a) * 9.5;
      const post = L.box(PR, 0.6, 1.1, 0.6, "iron", x, 0.55, z);
      const disc = L.cyl(PR, 0.42, 0.08, "brass", x, 1.15, z);
      const cover = L.box(PR, 0.7, 0.12, 0.7, "rust", x, 1.22, z, { solid: false, dynamic: true });
      L.label(PR, String(i + 1), x, 1.9, z, face(x, z, 0, 8), 0.8, "#ffffff");
      plates.push({ at: [x, 1.15, z], note: i, group: PR, mesh: disc, cover });
    }
    L.when([F.driveOpen], () => { plates.forEach(p => { p.cover.visible = false; }); L.toast("Six striking plates unfold around the forecourt. Play the lullaby: 1, 3, 5, 4.", 4.5); });
    const seq = L.sequence({ buttons: plates.map(p => ({ at: p.at, note: p.note, group: p.group, mesh: p.mesh })), solution: [0, 2, 4, 3], flag: F.bell, bell: true, requires: [F.driveOpen], lockedMsg: "A folded striking plate. It opens with the right drive." });
    L.interact({ at: [7.4, 0.9, 20.6], mesh: L.box(PR, 0.5, 0.6, 0.5, "brass", 7.4, 0.9, 20.6, { solid: false }), mask: 2, prompt: "Listen to drive III's broken tune", requires: [F.driveOpen], locked: "Silent until the striking frame is open.", onUse: () => seq.demo() });
    L.onHum([6, 1, 20], 7, () => L.say([ln("Ilse Marrow", "That's it - that's the song. You always hummed all four notes, even when you were small.", 4), ln("Ilse Marrow", "One, three, five, four. Now strike it.", 3)], false), { requires: [F.driveOpen] });
    [1, 3, 5].forEach((k, j) => { const p = plates[k].at; masonry(p[0], p[2], p[0], 9, p[2] + 0.8, j * 1.6, [F.driveOpen], 2.6, 1.4); });
    const ex = plates[0].at;
    const extra = masonry(ex[0], ex[2], ex[0], 9, ex[2] + 0.8, 0.8, [F.driveOpen, "L07.ondineDefied"], 3, 1.3);
    L.checkpoint(0, 0, 12, Math.PI, { climax: true, size: [12, 2.5, 3] });
    L.when([F.bell], () => { L.sfx("reveal"); L.toast("The last note lands. The bell is caught mid-swing; the tremors stop.", 4); });

    // ---------------- memory fragments ----------------
    L.fragment("L07.1", [-30.5, 1.3, 44.5], "The Ninth Chair", "PAST. A Founders' Council minute, in a clerk's hand: 'Founder Halden asked that a ninth chair be left empty at the table, for whoever comes after us to judge what we did. The chair was left. Nobody sat in it.'", S);
    L.fragment("L07.2", [-7, 4.3, -37.2], "The Warden's Terrace", "PRESENT. A tally scratched into the terrace rail: '412 festivals. 412 first tolls. I have watched the square empty from up here every time. I have never once seen the second toll from the other side.'", S);
    L.fragment("L07.3", [-49, 1.3, -33], "The Back Lane", "FUTURE. A note wedged in the lane wall: 'If you are reading this in the back lane, you came the long way, like me. The signal is not a weapon. It is a hand on the door, holding it shut. - (0)'", S);
    L.ambient("wind", 0.2, 7);
  },
});
