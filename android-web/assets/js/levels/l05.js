/* LEVEL 05 — The Drowned District (Old Veyra Flood Basin).
   Port of the Unity Level05Builder (Design v2). Learn: the pump in the PAST drains the PRESENT basin.
   Combine: sluices + three family rooms; shore the laundry beam in the PAST while Maren is held in the
   PRESENT and the water rises. Confrontation: Mother Ondine's boat (evidence.list option).
   Climax: the flood returns - save the register or the music box - then the door with no house. */
AZ.registerLevel({
  id: "L05", order: 5, chapter: "LEVEL 05", title: "The Drowned District", location: "Old Veyra Flood Basin",
  start: 1, allowed: 7,
  build(L) {
    const { line } = L, S = L.shared, PA = L.past, PR = L.present, FU = L.future, W = L.W;
    const PI = Math.PI;
    const F = {
      pump: "L05.pumpRepaired", sW: "L05.sluiceWest", sN: "L05.sluiceNorth", sE: "L05.sluiceEast", redirected: "L05.waterRedirected",
      vell: "L05.roomVell", corran: "L05.roomCorran", oskel: "L05.roomOskel", rooms: "L05.roomsCompared",
      heard: "L05.heardMaren", debris: "L05.debrisCleared", child: "L05.childRescued", recog: "maren.recognised",
      ondine: "L05.ondineDone", yielded: "L05.ondineYielded", hunt: "L05.keepersHunt", crisis: "L05.crisis",
      made: "choice.L05.made", records: "choice.L05.records", keepsake: "choice.L05.keepsake",
    };
    const TW = "#ffe6b0", TC = "#cfe8ff", TH = "#ff9ae8";

    L.look({ fog: 0.85, past: { density: 0.01 }, present: { density: 0.026 }, future: { density: 0.02 } });
    L.interlude("The Lift Down", [
      line("Ignatz Marrow (echo)", "Mind the gap, mind the gap... The lift goes down to the basin. Nobody rides it back up.", 4.5),
      line("Ignatz Marrow (echo)", "Forty trains in nine days. They told me it was a drill. Children, mostly. Children and their mothers.", 5),
      line("Arin", "Who told you to run them?", 3),
      line("Ignatz Marrow (echo)", "My sister knew. Ilse always knew more than she said. She always carried a carved zero - turned it in her fingers like a coin.", 6),
      line("Ignatz Marrow (echo)", "If you see her, tell her I kept the trains running. Tell her I never asked why.", 4.5),
      line("Arin", "The lift shudders to a stop. Below: rain, black water, and rooftops.", 4),
    ]);
    L.intro(line("Arin", "The whole district is under water. Doors, lanes... it's up to the windows."),
      line("Hesk Lowry", "Mind the rail, stranger. The basin took everything that wasn't nailed to the sky."),
      line("Arin", "In the PAST these lanes were dry. Something kept the water out. Something I can fix."));

    L.objective("Repair the communal hand-pump in the PAST", F.pump, "Tobin the pump mender lost the handle. In the PAST, look up the north lane by the Corran house.");
    L.objective("Open the three lane sluices in the PRESENT", F.redirected, "With the basin drained, wade to the Sluice House (south-west corner) and turn all three wheels.");
    L.objective("Compare the three family rooms", F.rooms, "Read the calendar in the Vell (west), Corran (north) and Oskel (east) houses in the PRESENT.");
    L.objective("Rescue Maren from the flooded laundry", F.child, "Three timber shores lie around the PAST square. Set them under the sagging yard beam in the PAST; each clears a rubble bank in the PRESENT. HUM so Maren trusts you, then lead her to the Weir Stairs.");
    L.objective("Face Mother Ondine at the Keeper boat", F.ondine, "Her boat waits near the Weir Stairs. If you carry the evacuation list, show it to her.");
    L.objective("The flood is returning: save the register or the music box", F.made, "Both lie in the Ward Office, north-west side of the square. You can only carry one.");
    L.objective("Enter the impossible nursery", "L05.nurseryEntered", "The door with no house stands in the north-east corner of the square.");
    L.payoff(F.rooms, "Every Calendar Marks Tomorrow", "Three families struck from the record by Founder Aurel Halden's order. Three calendars in three different hands - and every one marks tomorrow as the day of the bell. They were not surprised by the catastrophe. They were preparing for it.");

    // ================= shell: basin, terrace, stairs, terraces =================
    L.floor(S, 0, 0, 76, 72, 0, "stone");
    L.floor(S, 0, -4, 27.6, 39.6, 0.02, "stoneWet", 0.1, { solid: false });
    L.box(S, 24, 3, 12, "stone", 0, 1.5, -30);                       // Weir Terrace (top y=3)
    L.stairs(S, [0, 0, -19.5], [0, 3, -24], 4, "stone", { echo: true });
    L.wall(S, -2.2, -19.5, -2.2, -24, 3.9, "stone", 0.4);
    L.wall(S, 2.2, -19.5, 2.2, -24, 3.9, "stone", 0.4);
    L.wall(S, -12, -24.1, -2.2, -24.1, 1.05, "stone", 0.25, 3);
    L.wall(S, 2.2, -24.1, 12, -24.1, 1.05, "stone", 0.25, 3);
    L.wall(S, -12, -36, 12, -36, 4, "brick", 0.5, 3);
    L.wall(S, -12, -36, -12, -24, 1.05, "stone", 0.4, 3);
    L.wall(S, 12, -36, 12, -24, 1.05, "stone", 0.4, 3);
    // name board spanning the stair walls (was buried inside the steps, facing the terrace face)
    L.box(S, 4.8, 0.55, 0.14, "woodDark", 0, 4.17, -19.7, { solid: false });
    L.label(S, "OLD VEYRA FLOOD BASIN  -  WEIR STAIRS", 0, 4.17, -19.62, 0, 0.3, TW, "rgba(10,10,14,.8)");
    L.label(S, "OLD VEYRA FLOOD BASIN  -  WEIR STAIRS", 0, 4.17, -19.78, PI, 0.3, TW, "rgba(10,10,14,.8)");
    // Corner terraces (solid masses, windows on lanes/square)
    const B = (x, z, w, h, d, m, s) => L.building(S, x, z, w, h, d, m, "windowCold", s);
    B(-21, -20, 14, 8, 8, "plaster", 1); B(-25, -12, 6, 8, 8, "brick", 2); B(-21, -5, 14, 7, 6, "plaster", 3);
    B(-21, 4, 14, 9, 4, "brick", 4); B(-25, 10, 6, 9, 8, "plaster", 5); B(-21, 15, 14, 9, 2, "brick", 6);
    B(-15, 23, 26, 10, 14, "plaster", 7); B(15, 23, 26, 10, 14, "brick", 8);
    B(27, 9, 6, 9, 14, "plaster", 9); L.box(S, 10, 9, 2, "brick", 19, 4.5, 3); L.box(S, 10, 9, 2, "brick", 19, 4.5, 15);
    B(21, -5, 14, 8, 6, "brick", 10); L.box(S, 10, 8, 2.5, "plaster", 19, 4, -9.25); L.box(S, 10, 8, 2.5, "plaster", 19, 4, -14.75); B(21, -20, 14, 8, 8, "brick", 11);
    L.box(S, 6, 8, 16, "plaster", 33, 4, -12); L.box(S, 16, 11, 14, "plaster", -20, 5.5, -31); L.box(S, 16, 11, 14, "brick", 20, 5.5, -31);
    L.box(S, 30, 12, 6, "brick", 0, 6, -40);
    L.skyline(S, 0, 0, 55, 80, 10, "plasterCold", 5);
    L.box(S, 9, 52, 9, "stone", 0, 26, 92, { solid: false, noEcho: true });
    L.box(S, 11, 6, 11, "brass", 0, 55, 92, { solid: false, noEcho: true });
    L.clockFace(S, 0, 47, 87.5, 3.5);
    L.label(S, "(0)", 0, 47, 87.2, PI, 3, "#34445a");
    L.light(S, 0xe8f0ff, 4, 30, 0, 47, 84);

    // ================= Beat 1: the pump =================
    L.box(S, 2.2, 0.6, 2.2, "stone", 0, 0.3, -2); L.box(S, 3.2, 0.2, 3.2, "stone", 0, 0.1, -2);
    L.box(S, 2.2, 0.7, 0.9, "stoneWet", 0, 0.35, -4.1);
    L.cyl(PA, 0.13, 1.6, "rust", 0, 1.4, -2, { solid: true });
    L.sphere(PA, 0.18, "brass", 0, 2.25, -2);
    L.label(PA, "COMMUNAL PUMP\nHANDLE MISSING - T. ASH", 0, 1.15, -3.15, PI, 0.18, TW);
    const handle = L.item({ group: PA, at: [1.1, 0.12, 26.6], key: "handle", name: "pump handle", mesh: L.box(PA, 0.14, 0.14, 1.1, "rust", 1.1, 0.12, 26.6, { solid: false, dynamic: true }) });
    for (let i = 0; i < 6; i++) L.box(PA, 0.8, 0.02, 0.8, "paper", -0.6 + (i % 2) * 0.9, 0.02, 20 + i, { solid: false, noEcho: true });
    L.label(PA, "1 2 3 ... 0", 0, 0.6, 25.6, PI, 0.25, "#ffffff");
    L.socket({ group: PA, at: [0.85, 0.62, -2], radius: 0.4, accepts: "handle", flag: F.pump, prompt: "Seat the handle in the pump yoke", wrong: "That will not seat in the pump yoke.",
      onPlace: h => { h.mesh.position.set(0.32, 2.0, -1.6); } });
    const gush = L.particles(PA, "drips", [0.3, 1, 0.3], [0, 1.1, -2.9], 120); gush.visible = false;
    L.cyl(PR, 0.13, 1.6, "rust", 0, 1.4, -2, { solid: true });
    const noHandle = L.label(PR, "no handle - bolt holes worn smooth", 0, 2.6, -2.3, PI, 0.14, TC);
    const outflow = L.particles(PR, "drips", [0.5, 1, 0.5], [0, 0.6, -4.6], 160); outflow.visible = false;
    L.crystals(FU, 4, 0, -2, 2, 2, 0.6);
    L.holoMesh(FU, "box", "amber", 0, 3, -2, 1.6, 0.5, 0.02, { bob: 0.05 });
    const plaque = L.label(FU, "SUMP SEALED - CYCLE 1", 0, 3, -2.05, PI, 0.2, "#ffffff");
    L.when([F.pump], () => {
      gush.visible = true; outflow.visible = true; noHandle.visible = false; plaque.visible = false;
      L.label(FU, "SUMP OPEN SINCE THE FIRST CYCLE", 0, 3.5, -2.05, PI, 0.18, "#7ff0ff");
      L.sfx("rule"); L.toast("The mended pump keeps the old sump open. In the PRESENT, the basin drains.", 4);
    });
    // PRESENT basin flood: windows-high until the pump is mended in the PAST.
    L.water({ group: PR, at: [-2.9, -2.5], size: [38.6, 37], high: 2.2, low: 0.35, lowWhen: [F.pump], mask: 2, speed: 1.4, drainedFlag: "L05.basinDrained" });
    L.water({ group: PR, at: [19, 9], size: [9.6, 9.6], high: 2.2, low: 0.35, lowWhen: [F.pump], mask: 2, speed: 0.8 });   // schoolhouse
    L.label(PR, "- FLOOD LINE 2.2 M -", -13.75, 2.25, -18, PI / 2, 0.18, TC);
    L.label(PR, "- FLOOD LINE 2.2 M -", 13.75, 2.25, -18, -PI / 2, 0.18, TC);

    // ================= Beat 2: sluices + lanes =================
    L.room(S, -18, -12, 8, 8, 4.6, "stoneWet", null, [1], "woodDark");
    L.label(S, "SLUICE HOUSE", -13.75, 3.3, -12, PI / 2, 0.4, TW);
    L.box(PR, 0.15, 2.6, 6.4, "copper", -21.6, 1.8, -12);
    L.label(PR, "LANE SLUICES - TURN TO SEND EACH LANE TO THE CULVERT", -21.45, 3.4, -12, PI / 2, 0.16, TC);
    const lanes = [["WEST LANE (VELL) <<", F.sW, "west"], ["NORTH LANE (CORRAN) [ ]", F.sN, "north"], ["EAST LANE (OSKEL) >>", F.sE, "east"]];
    lanes.forEach((ln, i) => {
      const z = -14 + i * 2;
      const wheel = L.mesh(PR, "torus", "rust", -21.2, 1.5, z, 0.9, 0.9, 0.9); wheel.rotation.y = PI / 2; wheel.userData.dynamic = true;
      L.label(PR, ln[0], -21.4, 2.35, z, PI / 2, 0.13, TC);
      let spin = 0;
      L.update(dt => { if (spin > 0) { spin -= dt; wheel.rotation.z += dt * 4; } });
      L.interact({ at: [-21.2, 1.5, z], mesh: wheel, mask: 2, prompt: "Turn the " + ln[2] + " lane sluice", requires: [F.pump], once: true,
        locked: "The wheel will not move against a basin full of water. Something in the PAST must drain it first.",
        onUse: () => { spin = 2.2; L.sfx("clang"); L.set(ln[1]); } });
    });
    L.light(PR, 0xff9a48, 1.6, 9, -18, 4, -12);
    L.label(PA, "CULVERT GATES OPEN\nAS LONG AS THE PUMP RUNS", -21.45, 2.2, -12, PI / 2, 0.2, TW);
    L.light(PA, 0xffb469, 1.4, 8, -18, 3.8, -12);
    L.crystals(FU, 3, -20, -14.5, 2, 2);
    // lanes: [name, flag, water centre, water size, gate pos, gate is along x?, crystal pos, house door pos]
    const laneDefs = [
      ["West", F.sW, [-22.2, 0], [15.6, 4], [-14.2, 0], true, [-15.2, 0], [-29.8, 0]],
      ["East", F.sE, [22.2, 0], [15.6, 4], [14.2, 0], true, [15.2, 0], [29.8, 0]],
      ["North", F.sN, [0, 23.1], [4, 13.8], [0, 16.2], false, [0, 17.2], [0, 29.8]],
    ];
    laneDefs.forEach(d => {
      const [name, flag, wc, ws, gp, alongX, cp, dp] = d;
      L.water({ group: PR, at: wc, size: ws, high: 1.9, low: -0.4, lowWhen: [flag], mask: 2, speed: 1.4, drainedFlag: "L05.laneDrained" + name });
      L.door(PR, alongX ? 0.3 : 4.2, 2.8, alongX ? 4.2 : 0.3, "rust", gp[0], 0, gp[1], [flag], [0, -2.9, 0], { toast: "The " + name.toLowerCase() + " lane drains into the culvert." });
      L.label(PR, "SLUICE - " + name.toUpperCase() + " LANE", gp[0] + (alongX ? (gp[0] > 0 ? -0.3 : 0.3) : 0), 3.3, gp[1] - (alongX ? 0 : 0.3), alongX ? (gp[0] > 0 ? -PI / 2 : PI / 2) : PI, 0.22, TC);
      L.box(FU, alongX ? 0.6 : 4, 3.2, alongX ? 4 : 0.6, "crystal", cp[0], 1.6, cp[1]);
      L.crystals(FU, 4, cp[0], cp[1], alongX ? 1 : 3.5, alongX ? 3.5 : 1);
      L.box(PA, alongX ? 0.15 : 2, 2.6, alongX ? 2 : 0.15, "woodDark", dp[0], 1.3, dp[1]);
      L.label(PA, "LATCHED\nsupper is on - come back after Bell Day", dp[0] + (alongX ? (dp[0] > 0 ? -0.12 : 0.12) : 0), 1.7, dp[1] - (alongX ? 0 : 0.12), alongX ? (dp[0] > 0 ? -PI / 2 : PI / 2) : PI, 0.13, TW);
    });
    L.when([F.sW, F.sN, F.sE], () => { L.set(F.redirected); L.sfx("solved"); L.toast("All three lanes run to the culvert. The family houses are open.", 4); });

    // ================= Beat 3: the three family rooms =================
    const house = (family, cx, cz, door, tdx, tdz, flag, title, text, echoLine, fate) => {
      L.room(S, cx, cz, 8, 8, 4.6, "plaster", null, [door], "woodDark");
      L.box(S, 7.6, 0.03, 7.6, "woodDark", cx, 0.015, cz, { solid: false });
      L.box(S, 8.4, 3.6, 8.4, "brick", cx, 6.6, cz, { solid: false });
      const fx = cx + tdx * 4.3, fz = cz + tdz * 4.3, ryOut = Math.atan2(-tdx, -tdz) + PI;
      L.label(S, family.toUpperCase(), fx, 3.1, fz, Math.atan2(tdx, tdz), 0.3, TW); // faces the street
      const bx = cx - tdx * 3.65, bz = cz - tdz * 3.65, rx = tdz, rz = -tdx;   // back wall + its tangent
      const ryIn = Math.atan2(tdx, tdz);                                        // read by someone facing the back wall
      const pw = [bx + rx * -1.8 + tdx * 0.1, bz + rz * -1.8 + tdz * 0.1], cal = [bx + rx * 1.8 + tdx * 0.12, bz + rz * 1.8 + tdz * 0.12];
      const flat = Math.abs(tdx) > 0.5;
      // PAST: supper, an intact portrait, a resident at home.
      L.box(PA, 2.2, 0.1, 1.2, "wood", cx, 0.75, cz); L.box(PA, 1.9, 0.7, 0.9, "woodDark", cx, 0.37, cz);
      L.box(PA, flat ? 0.06 : 1.6, 1.2, flat ? 1.6 : 0.06, "brass", pw[0], 2.1, pw[1], { solid: false });
      for (let i = 0; i < 4; i++) L.sphere(PA, i === 3 ? 0.08 : 0.11, "skin", pw[0] + rx * (-0.5 + i * 0.33) + tdx * 0.05, 2.15, pw[1] + rz * (-0.5 + i * 0.33) + tdz * 0.05);
      L.label(PA, "THE " + family.toUpperCase() + " FAMILY", pw[0] + tdx * 0.08, 1.35, pw[1] + tdz * 0.08, ryIn, 0.15, TW);
      L.label(PA, "TOMORROW\nBELL DAY", cal[0], 1.7, cal[1], ryIn, 0.14, "#a83020");
      L.light(PA, 0xffb469, 2.2, 9, cx, 3.2, cz);
      L.npc({ group: PA, name: "", named: false, at: [cx + rx * 1.2 + tdx * 1.4, cz + rz * 1.2 + tdz * 1.4], coat: "burgundy",
        route: [[cx + rx * 1.2 + tdx * 1.4, cz + rz * 1.2 + tdz * 1.4, 6, "work"], [cx + rx * -1.5, cz + rz * -1.5, 4, "carry"], [cx - tdx * 2, cz - tdz * 2, 4, "look"]] });
      // PRESENT: scratched faces, the calendar clue.
      L.box(PR, 2.2, 0.1, 1.2, "woodDark", cx + 0.4, 0.6, cz + 0.2, { ry: 0.4 });
      L.box(PR, 6.5, 0.02, 6.5, "puddle", cx, 0.03, cz, { solid: false });
      L.box(PR, flat ? 0.06 : 1.6, 1.2, flat ? 1.6 : 0.06, "rust", pw[0], 2.1, pw[1], { solid: false });
      for (let i = 0; i < 4; i++) {
        const spared = family === "Oskel" && i === 3;
        const px = pw[0] + rx * (-0.5 + i * 0.33) + tdx * 0.05, pz = pw[1] + rz * (-0.5 + i * 0.33) + tdz * 0.05;
        if (spared) L.sphere(PR, 0.08, "skin", px, 1.97, pz); else L.box(PR, flat ? 0.02 : 0.24, 0.24, flat ? 0.24 : 0.02, "ruin", px, 2.15, pz, { solid: false });
      }
      L.label(PR, "faces scratched away", pw[0] + tdx * 0.1, 1.35, pw[1] + tdz * 0.1, ryIn, 0.14, TC);
      L.clue({ group: PR, at: [cal[0], 1.7, cal[1]], size: flat ? [0.08, 0.9, 0.7] : [0.7, 0.9, 0.08], mat: "paper", prompt: "Read the " + family + " calendar", flag, id: "L05.calendar." + family.toLowerCase(), title, text });
      L.label(PR, "TOMORROW (O)", cal[0] + tdx * 0.06, 2.25, cal[1] + tdz * 0.06, ryIn, 0.15, "#ff6050");
      L.light(PR, 0x7fc0d8, 1.4, 8, cx + tdx * 2, 3.6, cz + tdz * 2);
      // FUTURE: crystal ruin, holographic family echo, and what became of them.
      L.crystals(FU, 5, cx, cz, 6, 6);
      for (let i = 0; i < 3; i++) {
        const h = i === 2 ? 1 : 1.65, ex = cx + rx * (-1.2 + i * 1.2) - tdx * 0.6, ez = cz + rz * (-1.2 + i * 1.2) - tdz * 0.6;
        L.holoMesh(FU, "cyl", "magenta", ex, h * 0.5, ez, 0.45, h, 0.3, { bob: 0.04 });
        L.holoMesh(FU, "sphere", "magenta", ex, h + 0.15, ez, 0.26, 0.26, 0.26, { bob: 0.04 });
      }
      L.label(FU, family.toUpperCase() + " - RECORD EXPUNGED\n" + fate, cx, 3.2, cz, ryIn, 0.2, TH);
      L.light(FU, 0xff40c8, 1.6, 7, cx, 2.2, cz);
      L.zone({ at: [cx, 1, cz], size: [6, 2, 6], mask: 4, flag: "L05.echo" + family, lines: [line(family + " echo", echoLine)] });
    };
    house("Vell", -34, 0, 1, 1, 0, F.vell, "The Vell family - wall calendar",
      "Swollen with flood water, but the ink held. Every day is crossed out up to today. Tomorrow is circled three times: 'BELL DAY. Children to the tower at first light.'\n\nBeside it, the family portrait. Every face has been scratched away.",
      "Pack the good linen. We leave for the tower before dawn.", "EVACUATED - ARRIVED");
    house("Oskel", 34, 0, 3, -1, 0, F.oskel, "The Oskel family - calendar above the cot",
      "Tomorrow is marked with a little carved paper zero. Under it, a mother's note: 'Maren - if we are separated, wait where the water cannot follow. We will come for you.' A newer slip is pinned over the family name: STRUCK BY ORDER OF FOUNDER A. HALDEN.\n\nThe portrait's faces are scratched out. Only the smallest was left - a girl with a lantern.",
      "Maren, hold the lantern high. We'll find you by the light.", "ONE CHILD LEFT BEHIND");
    house("Corran", 0, 34, 2, 0, -1, F.corran, "The Corran family - kitchen calendar",
      "Tomorrow ringed in red pencil. In a child's hand underneath: 'the bell will carry us'. Someone scraped the Corran name off the door plate and the portrait frame - but missed this.",
      "The bell will carry us. Everyone says so.", "LOST IN THE SECOND RING");
    L.when([F.vell, F.corran, F.oskel], () => L.set(F.rooms));

    // ================= Beat 4: the laundry rescue =================
    L.room(S, 27, -12, 6, 8, 4.2, "plasterCold", null, [3], "woodDark");
    L.box(S, 5.6, 0.03, 7.6, "tile", 27, 0.015, -12, { solid: false });
    L.label(S, "LAUNDRY ALLEY", 14.1, 3.4, -9.9, PI / 2, 0.3, TW);
    // PAST yard: fence, sagging beam, three shore sockets.
    L.box(PA, 0.15, 2.4, 3, "wood", 19.3, 1.2, -12);
    L.label(PA, "LAUNDRY CLOSED\nFOR BELL DAY", 19.2, 1.9, -12, -PI / 2, 0.2, TW);
    const beam = L.box(PA, 3.4, 0.35, 0.35, "woodDark", 17.5, 3.3, -12, { solid: false }); beam.rotation.z = -0.1;
    L.label(PA, "the yard beam sags - shore it up", 15.2, 3, -12, PI / 2, 0.16, TW);
    L.cyl(PA, 0.55, 1, "brass", 28.6, 0.5, -14.5); L.cyl(PA, 0.55, 1, "brass", 28.6, 0.5, -12.1);
    L.light(PA, 0xffb469, 1.8, 8, 27, 3.6, -12);
    // FUTURE: crystal seam.
    L.box(FU, 0.6, 3.2, 3, "crystal", 15, 1.6, -12); L.crystals(FU, 4, 15, -12, 1, 3);
    L.crystals(FU, 4, 28.5, -14.5, 2, 2);
    L.holoMesh(FU, "cyl", "magenta", 27, 0.7, -12, 0.35, 1.1, 0.25, { bob: 0.05 });
    L.label(FU, "UNCOUNTED: 1 CHILD", 27, 1.8, -12, -PI / 2, 0.18, TH);
    // PRESENT: three rubble banks; each vanishes when its shore is set in the PAST.
    const shoreFlags = ["L05.shore1", "L05.shore2", "L05.shore3"];
    const spawns = [[7.6, -8.6, "by Tobin's bench"], [-10.6, -4.6, "at the market stall"], [-20.6, -9.6, "in the sluice house"]];
    shoreFlags.forEach((fl, i) => {
      const x = 16.5 + i;
      const bank = L.box(PR, 0.95, 2.6, 3, i === 1 ? "brick" : "concrete", x, 1.3, -12, { dynamic: true });
      const spill = L.box(PR, 1, 0.7, 1.2, "ruin", x - 0.2, 0.35, -11.2, { dynamic: true, solid: false });
      const tag = L.label(PR, "RUBBLE " + (i + 1), x - 0.5, 2.2, -12, -PI / 2, 0.16, TC);
      L.socket({ group: PA, at: [x, 0.03, -12], radius: 0.4, accepts: "shore", flag: fl, prompt: "Set the shore under the beam (" + (i + 1) + ")", wrong: "Only a timber shore fits under the beam.",
        onPlace: h => { h.mesh.position.y = 0.72; } });
      L.label(PA, "SHORE " + (i + 1), x - 0.45, 1.7, -12, -PI / 2, 0.14, TW);
      const sp = spawns[i];
      L.item({ group: PA, at: [sp[0], 0.7, sp[1]], key: "shore", name: "timber shore", mesh: L.box(PA, 0.22, 1.4, 0.22, "wood", sp[0], 0.7, sp[1], { solid: false, dynamic: true }) });
      L.label(PA, "TIMBER SHORE\n" + sp[2], sp[0], 1.75, sp[1], PI, 0.12, TW);
      L.when([fl], () => {
        bank.visible = false; spill.visible = false; tag.visible = false; bank.userData.collider.on = false;
        L.sfx("hazard", 0.5); L.toast("In the PRESENT, rubble bank " + (i + 1) + " slides away - the beam came down differently.", 3.5);
      });
    });
    L.when(shoreFlags, () => { L.set(F.debris); L.sfx("solved"); L.toast("The alley is open in the PRESENT. Go to Maren - now.", 4); });
    L.zone({ at: [9, 1, -12], size: [8, 2.5, 8], mask: 2, requires: [F.pump], flag: F.heard,
      lines: [line("Maren (far off)", "Help! Is somebody out there? The water's coming under the door!"), line("Arin", "A child. In the laundry alley.")] });
    L.timer({ duration: 170, startFlag: F.heard, successFlag: F.child, label: "LAUNDRY FLOODS IN",
      startToast: "The laundry is flooding. Shore the beam in the PAST - three shores - and get Maren out in the PRESENT.",
      failMessage: "The water reached the laundry ceiling. The bell rings again - and the basin forgets Maren too." });
    L.water({ group: PR, at: [21, -12], size: [14, 3], high: -0.6, low: -0.6, riseWhen: [F.heard], risen: 1.0, speed: 1.6 / 170, wadeable: true, mask: 2 });
    L.label(PR, "- WATER RISING -", 14.25, 1, -10.6, -PI / 2, 0.14, TC);
    // Loose slates: telegraphed hazard (PRESENT).
    L.hazard({ group: PR, at: [21.8, 0, -12], size: [2, 2.5, 3], mask: 2, idle: 3.2, warn: 1.5, active: 1, msg: "The slates came down. Wait for the dust and the red light to pass, then run." });
    L.label(PR, "LOOSE SLATES\nwait for the dust, then run", 21.8, 3, -10.6, PI, 0.16, "#ffa070");
    L.particles(PR, "debris", [2, 1, 3], [21.8, 4.2, -12], 60);
    L.checkpoint(19.8, 0, -12, -PI / 2, { size: [0.8, 2.5, 3] });
    // Maren (PRESENT)
    L.box(PR, 1.2, 0.8, 1, "iron", 28.8, 0.4, -14.8); L.box(PR, 0.9, 1.2, 1.2, "rust", 28.9, 0.6, -9.4);
    for (let i = 0; i < 4; i++) L.box(PR, 0.8, 1.1, 0.02, "white", 25.4 + i, 2.2, -12.4 + (i % 2) * 0.9, { solid: false });
    const lanternMesh = L.box(PR, 0.4, 0.5, 0.4, "warm", 28.4, 0.3, -13.4, { solid: false });
    L.light(PR, 0xffb060, 2.2, 7, 28.4, 0.9, -13.4);
    const maren = L.npc({ group: PR, name: "Maren Oskel", at: [27.4, -12.6], yaw: -PI / 2, coat: "ochre", scale: 0.72,
      task: "Seven years old, trapped in the old laundry. Keeping her family's lantern lit and waiting, as her mother told her, 'where the water cannot follow'.",
      route: [[27.8, -14.2, 5, "work"], [28.4, -10.4, 4, "look"], [25.6, -12, 3, "idle"]],
      approach: ["Is someone there? Please!", "Don't let the water in!"],
      talk: [line("Maren Oskel", "I don't go with strangers. Mama hums when I'm scared. Only the right people know the song."),
        line("Maren Oskel", "Mama said wait where the water can't follow. So I waited."),
        line("Maren Oskel", "Everyone went to the tower for Bell Day. They said they'd come back for me."),
        line("Maren Oskel", "I'm seven. I counted the drips. Lots of hundreds."),
        line("Maren Oskel", "I left my music box in the ward office. It plays Mama's song, but one note is always missing.")],
      cond: [{ flag: F.keepsake, lines: [line("Maren Oskel", "My music box! You brought it!"), line("Maren Oskel", "Listen - it still skips the note. Mama says we'll find it one day, in the right room.")] },
        { flag: F.child, lines: [line("Maren Oskel", "It's so high up here. The water can't follow."), line("Maren Oskel", "Will you find Mama? She was going to the tower. I think she's still going.")] },
        { flag: F.recog, lines: [line("Maren Oskel", "You're the one who hums. You hummed it in the nursery, when the lights went out."), line("Maren Oskel", "You had a watch that didn't tick. You said it was waiting.")] },
        { flag: F.oskel, lines: [line("Maren Oskel", "You saw our calendar? Mama cut the little zero for tomorrow."), line("Maren Oskel", "She said tomorrow the bell would carry us somewhere safe. Somewhere with no address.")] }],
      changedFlag: F.child, changedRoute: [[1.9, -26.8, 6, "look"], [0.2, -28.6, 4, "idle"], [2.6, -28.2, 4, "talk"]],
      changedTalk: [line("Maren Oskel", "It's so high up here. The water can't follow."), line("Maren Oskel", "Will you find Mama? She was going to the tower. I think she's still going.")] });
    const marenChanged = maren.o.changedRoute; maren.o.changedRoute = null;   // restored once she has climbed to the terrace
    L.onHum([27, 1, -12], 6, () => {
      L.set(F.recog);
      L.say([line("Maren Oskel", "...That's Mama's song. All of it - even the note that's always missing."), line("Maren Oskel", "You're the one who hums. From the nursery. I know you."),
        line("Arin", "(The list said VALE, A., age 8. Was I one of them? Did I hum this to her?)")], false);
    }, { mask: 2 });
    let following = false, climb = -1;
    const climbPath = [[0, 0, -19.4], [0, 3, -24.05], [1.8, 3, -26.2]];
    const lead = L.interact({ at: [28.4, 0.4, -13.4], mesh: lanternMesh, mask: 2, prompt: "Take Maren's hand - lead her to the Weir Stairs", requires: [F.debris, F.recog], once: true,
      locked: "Maren shrinks back: 'I don't go with strangers. Mama said the one who hums would come.'  (HUM)",
      onUse: () => { following = true; maren.route = []; L.say([line("Maren Oskel", "You came! Mama said someone would come where the water can't follow."), line("Arin", "Stay close. When the red light flickers, we wait.")]); } });
    L.holoMesh(S, "torus", "amber", 0, 0.08, -18.2, 5.6, 5.6, 1, { spin: 0.4 }).rotation.x = PI / 2;
    const mp = maren.p.group.position, P = AZ.player;
    L.update(dt => {
      if (W.state !== 1) return;
      if (following) {
        const tx = P.pos.x + Math.sin(P.yaw) * 1.8, tz = P.pos.z + Math.cos(P.yaw) * 1.8;
        const dx = tx - mp.x, dz = tz - mp.z, d = Math.hypot(dx, dz);
        if (d > 0.4) { const s = Math.min(d, 3.6 * Math.min(1.6, d / 3) * dt); mp.x += dx / d * s; mp.z += dz / d * s; maren.p.group.rotation.y = Math.atan2(dx, dz); }
        const g = AZ.sim.groundAt(W, mp.x, mp.y + 0.3, mp.z); if (isFinite(g.y)) mp.y += (g.y - mp.y) * Math.min(1, dt * 8);
        if (Math.hypot(mp.x, mp.z + 18.2) < 3) { following = false; climb = 0; L.set(F.child); L.sfx("objective"); L.toast("Maren is safe at the foot of the Weir Stairs.", 3); }
      } else if (climb >= 0 && climb < climbPath.length) {
        const c = climbPath[climb], dx = c[0] - mp.x, dy = c[1] - mp.y, dz = c[2] - mp.z, d = Math.hypot(dx, dy, dz);
        if (d < 0.05) { climb++; if (climb >= climbPath.length) maren.o.changedRoute = marenChanged; return; }
        const s = Math.min(d, 1.8 * dt); mp.x += dx / d * s; mp.y += dy / d * s; mp.z += dz / d * s;
      }
    });

    // ================= Confrontation: Mother Ondine =================
    const boat = new THREE.Group(); PR.add(boat); boat.userData.dynamic = true; boat.visible = false;
    L.box(boat, 1.6, 0.5, 4.2, "woodDark", 4.2, 0.25, -12.5, { solid: false, ry: 0.26 });
    L.box(boat, 0.5, 0.4, 0.5, "obsidian", 4.75, 0.45, -10.5, { solid: false });
    L.label(boat, "KEEPERS OF THE HOUR", 3.3, 0.7, -12.5, -PI / 2, 0.14, "#ffd8a0");
    const boatLight = L.light(PR, 0xffd88c, 0, 9, 4.2, 1.8, -10.9);
    const ondine = L.npc({ group: PR, name: "Mother Ondine Halden", at: [3.2, -14.6], yaw: 3.5, coat: "navy",
      task: "Leader of the Keepers of the Hour, Founder Halden's daughter. Patrols the drowned basin by boat, hunting 'anomalies' who remember the repeat.",
      route: [[3.2, -14.6, 6, "look"], [5.6, -10.8, 4, "work"], [1.8, -12.4, 4, "look"]],
      approach: ["Stay where my lantern can see you.", "You again. The one who ran from me in the archive."],
      talk: [line("Mother Ondine Halden", "You ran from me in the archive. You took a record that should have burned."),
        line("Mother Ondine Halden", "Inside the cycle, nobody dies for good. Every morning the basin is dry again. Why break that?"),
        line("Mother Ondine Halden", "My father kept this city alive with silence. I keep it alive with the loop."),
        line("Mother Ondine Halden", "The Keepers lock doors so people don't wander into tomorrow. It's a kindness."),
        line("Mother Ondine Halden", "Whatever you carry out of the flood belongs to the night, not to you.")],
      cond: [{ flag: F.yielded, lines: [line("Mother Ondine Halden", "Forty-one households, in his hand. I was told they were criminals."), line("Mother Ondine Halden", "Go on. I need to think - and I don't want my Keepers seeing me do it.")] },
        { flag: F.hunt, lines: [line("Mother Ondine Halden", "Run, anomaly. The lanterns will find you."), line("Mother Ondine Halden", "Every time you're caught, the water comes faster. That's the loop being merciful.")] },
        { flag: F.child, lines: [line("Mother Ondine Halden", "A child. Alive, in the drowned ward. That shouldn't be possible."), line("Mother Ondine Halden", "Which of you is the anomaly - you, or her?")] }],
      changedFlag: F.ondine, changedRoute: [[4.6, -12.2, 6, "idle"], [6.2, -14.8, 4, "look"], [3.6, -10.6, 4, "work"]],
      changedTalk: [line("Mother Ondine Halden", "You have what you came for. So do I."), line("Mother Ondine Halden", "We'll meet at the tower. Everyone does, on Bell Day.")] });
    ondine.mask = 0; if (ondine.it) ondine.it.mask = 0;
    L.when([F.child], () => { ondine.mask = 2; if (ondine.it) ondine.it.mask = 2; boat.visible = true; boatLight.intensity = 3; L.sfx("alert"); L.toast("A Keeper boat glides in across the square. Its lantern finds you.", 3.5); });
    const hunters = [
      L.keeper({ name: "Keeper Lantern-bearer", mode: "hunter", group: PR, at: [-9, 2], path: [[-9, 2], [-10, 13], [-4, 9], [-6, -3]], mask: 0, alarmFlag: "L05.keepersAlarm", caught: "The Keepers dragged you out of the lantern light. The water rose while they held you." }),
      L.keeper({ name: "Keeper Boatman", mode: "hunter", group: PR, at: [6, 6], yaw: PI, path: [[6, 6], [-2, 12], [-8, 6], [2, -4]], mask: 0, alarmFlag: "L05.keepersAlarm", caught: "The Keepers dragged you out of the lantern light. The water rose while they held you." }),
    ];
    hunters.forEach(k => { k.o.alarm = ["There! The one who hums!", "Anomaly in the ward office!"]; });
    L.zone({ at: [2.5, 1, -14.5], size: [12, 3, 9], mask: 2, requires: [F.child, F.rooms], onEnter: async () => {
      await L.dialogue({ speaker: "Mother Ondine Halden",
        opening: [line("Mother Ondine Halden", "Stop there. Lanterns on the anomaly.", 3), line("Mother Ondine Halden", "You pulled a child out of a house that doesn't exist. Now you want the ward office. Why?", 5)],
        question: "Mother Ondine Halden blocks the way to the Ward Office. What do you do?",
        options: [
          { text: "Show her the evacuation list", requires: ["evidence.list"], missing: "(you don't have the evacuation list)", sets: F.yielded,
            lines: [line("Arin", "Your father's evacuation list. Forty-one households. Nine days before the catastrophe.", 4.5), line("Mother Ondine Halden", "...That's his hand. His seal.", 3),
              line("Mother Ondine Halden", "He told me they were criminals. That the erasure was a sentence.", 4), line("Mother Ondine Halden", "Keepers - lanterns down. Let this one pass. Tonight, only tonight.", 4)] },
          { text: "Lie: \"I'm a salvager. There's nothing down here.\"", sets: F.hunt,
            lines: [line("Arin", "I'm a salvager. I go where the water goes. There's nothing down here.", 4), line("Mother Ondine Halden", "A salvager who hums the nursery song. I don't think so.", 4), line("Mother Ondine Halden", "Keepers! Lanterns up. Bring me the one who remembers.", 4)] },
          { text: "\"Your father erased these families.\"", sets: F.hunt,
            lines: [line("Arin", "Founder Halden struck these families from the record. You're guarding his silence.", 4.5), line("Mother Ondine Halden", "My father saved this city with silence. You'd drown it with talk.", 4), line("Mother Ondine Halden", "Keepers - take them before the water does.", 3.5)] },
        ], doneFlag: F.ondine });
    } });
    L.when([F.hunt], () => { hunters.forEach(k => { k.mask = 2; }); L.sfx("alert"); });
    AZ.on("respawn", () => { if (AZ.get(F.hunt)) L.penalize(10); });
    L.checkpoint(6, 0, -16.5, PI, { climax: true, size: [3, 2.5, 3] });

    // ================= Climax: the flood returns; register or music box =================
    L.room(S, -18, 10, 8, 8, 4.6, "stone", null, [1], "woodDark");
    L.label(S, "WARD OFFICE\nRECORDS OF OLD VEYRA", -13.75, 3.3, 10, PI / 2, 0.3, TW);
    L.box(S, 0.6, 1.1, 4.2, "woodDark", -17.2, 0.55, 10);
    for (let i = 0; i < 4; i++) L.box(PA, 0.6, 0.4, 1.6, i % 2 ? "teal" : "red", -21.5, 0.6 + i * 0.5, 7.5 + i * 0.2);
    L.light(PA, 0xffb469, 1.8, 8, -18, 3.8, 10);
    L.npc({ group: PA, name: "", named: false, at: [-19.6, 11.4], coat: "navy", route: [[-20.8, 8, 5, "work"], [-19.6, 11.6, 4, "carry"], [-17.9, 9, 3, "work"]] });
    L.crystals(FU, 4, -21, 12.5, 2, 2);
    L.holoMesh(FU, "box", "magenta", -18, 2.6, 10, 0.02, 0.6, 1.8, { bob: 0.05 });
    L.label(FU, "ARCHIVE PURGED", -17.95, 2.6, 10, -PI / 2, 0.24, "#ffffff");
    L.box(PR, 0.9, 0.8, 0.9, "stoneWet", -20.6, 0.4, 8.2); L.box(PR, 0.9, 0.8, 0.9, "stoneWet", -20.6, 0.4, 11.8);
    L.label(PR, "- - - FLOOD LINE - - -", -21.75, 2.2, 10, PI / 2, 0.18, TC);
    L.light(PR, 0x7fc0d8, 1.4, 8, -18, 3.8, 10);
    const alarm = L.light(PR, 0xff2a1a, 0, 9, -15, 3.8, 10);
    const regMesh = L.box(PR, 0.75, 0.5, 0.55, "wood", -20.6, 1.05, 8.2, { solid: false, dynamic: true });
    const regLabel = L.label(PR, "THE FAMILIES' REGISTER\nevery erased household, names intact", -20.5, 1.85, 8.2, -PI / 2, 0.14, TW);
    const boxMesh = L.box(PR, 0.45, 0.4, 0.42, "brass", -20.6, 1.0, 11.8, { solid: false, dynamic: true });
    const boxLabel = L.label(PR, "MAREN'S MUSIC BOX\nher mother's lullaby - one note missing", -20.5, 1.85, 11.8, -PI / 2, 0.14, TW);
    const keyMesh = L.box(PR, 0.4, 0.4, 0.4, "brass", -20.1, 1.0, 10.9, { solid: false, dynamic: true });
    const windKey = L.interact({ at: [-20.1, 1.0, 10.9], mesh: keyMesh, mask: 2, prompt: "Wind Maren's music box", onUse: async () => {
      const p = [1, 1.26, 1.498]; for (let i = 0; i < 4; i++) { if (i < 3) AZ.note(p[i]); await new Promise(r => setTimeout(r, 650)); }
      L.toast("Four notes - and the fourth never comes.", 3); L.set("L05.musicBoxHeard"); } });
    L.clue({ group: PR, at: [-21.7, 1.7, 10], size: [0.05, 0.7, 0.5], mat: "paper", prompt: "Read the pinned notice", flag: "L05.haldenOrder", id: "L05.haldenNotice", title: "By order of Founder Aurel Halden",
      text: "WARD ZERO - NOTICE OF RECORD WITHDRAWAL\n\nBy order of Founder Aurel Halden, the following households are struck from the public record, effective Bell Day:\nVELL. CORRAN. OSKEL. (and thirty-eight more)\n\nDoor plates are to be scraped. Names are not to be spoken in the ward. This is for their protection.\n\n- sealed (0)" });
    let regIt, boxIt;
    const choose = async (records) => {
      if (AZ.get(F.made)) return;
      const q = records ? "Carry out the families' register? It proves the erased households existed - names, births, addresses. Maren's music box will be lost to the flood."
        : "Carry out Maren's music box? The last thing her mother gave her. The families' register - the only proof these households existed - will be lost.";
      const i = await L.choice(q, [{ text: records ? "Take the register" : "Take the music box" }, { text: "Not yet" }]);
      if (i !== 0 || AZ.get(F.made)) return;
      L.set(records ? F.records : F.keepsake); L.set(F.made); L.sfx("rule");
      regMesh.visible = boxMesh.visible = keyMesh.visible = false; regLabel.visible = boxLabel.visible = false;
      regIt.enabled = boxIt.enabled = windKey.enabled = false;
      L.say(records ? [line("Arin", "Every name in the basin. Someone has to remember them now."), line("Arin", "The head archivist - Odile Fenwick. She'll know what to do with this.")]
        : [line("Arin", "Records can be rewritten. This can't."), line("Arin", "Four notes, one missing. Maybe someday she'll hear the whole song.")]);
    };
    const notYet = "Not yet. Someone is still trapped out there - and the water is quiet for now.";
    regIt = L.interact({ at: [-20.6, 1.05, 8.2], mesh: regMesh, mask: 2, prompt: "Save the families' register", requires: [F.crisis], locked: notYet, onUse: () => choose(true) });
    boxIt = L.interact({ at: [-20.6, 1.0, 11.8], mesh: boxMesh, mask: 2, prompt: "Save Maren's music box", requires: [F.crisis], locked: notYet, onUse: () => choose(false) });
    L.when([F.child, F.rooms, F.ondine], () => {
      L.set(F.crisis);
      L.timer({ duration: 80, successFlag: F.made, label: "FLOOD RETURNS IN", failMessage: "The flood closes over the ward office. The bell rings again - and the basin forgets you too." });
      L.sfx("bell"); L.toast("The culverts are failing - the flood is coming back. Save what you can from the Ward Office.", 4.5);
      alarm.intensity = 3;
      L.say([line("Hesk Lowry", "The culverts are going! The whole basin is coming back!"), line("Hesk Lowry", "Arin - the ward office! The families' register and the Oskel girl's music box. You'll only carry one!")], false);
      if (AZ.get(F.hunt)) setTimeout(() => { L.penalize(25); L.toast("The Keepers are in the basin. The flood will not wait for you to hide.", 4); }, 300);
    });
    L.update((dt, t) => { if (AZ.get(F.crisis) && !AZ.get(F.made)) alarm.intensity = 2 + Math.sin(t * 9) * 1.5; });
    L.water({ group: PR, at: [0, -2], size: [60, 46], high: -0.6, low: -0.6, riseWhen: [F.crisis], risen: 1.15, speed: 1.75 / 80, wadeable: true, mask: 2 });

    // ================= Exit: the door with no house =================
    const dx = 9, dz = 11;
    L.box(S, 0.25, 2.9, 0.3, "woodDark", dx - 0.95, 1.45, dz); L.box(S, 0.25, 2.9, 0.3, "woodDark", dx + 0.95, 1.45, dz);
    L.box(S, 2.15, 0.3, 0.3, "woodDark", dx, 3, dz);
    L.label(S, "NO ADDRESS", dx, 3.5, dz - 0.2, PI, 0.24, "#ffc8f0");
    L.label(S, "(0)", dx, 2.55, dz - 0.2, PI, 0.35, "#ffffff");
    L.box(S, 2.2, 2.9, 0.1, "paper", dx, 1.45, dz + 1.9); L.box(S, 0.1, 2.9, 1.9, "paper", dx - 1.1, 1.45, dz + 0.95); L.box(S, 0.1, 2.9, 1.9, "paper", dx + 1.1, 1.45, dz + 0.95);
    L.box(S, 2.3, 0.1, 2, "woodDark", dx, 2.95, dz + 0.95, { solid: false });
    L.box(S, 1, 0.5, 0.6, "wood", dx + 0.4, 0.45, dz + 1.4);
    L.holoMesh(S, "box", "magenta", dx + 0.4, 2.3, dz + 1.4, 0.3, 0.3, 0.3, { spin: 1, bob: 0.06 });
    L.light(S, 0xff8ce8, 2.4, 6, dx, 2.2, dz + 1);
    L.door(S, 1.65, 2.6, 0.12, "plaster", dx, 0, dz, [F.made], [0, 2.75, 0], { toast: "A door with no house has opened in the north-east corner of the square." });
    L.zone({ at: [dx, 1, dz - 0.9], size: [2, 2, 1], requires: [F.made], flag: "L05.nurseryEntered" });
    L.exit({ group: S, at: [dx, 0, dz + 0.5], size: [2, 2.5, 2.2], requires: [F.rooms, F.child, F.made],
      locked: "The door with no house is shut. It opens only after the families are remembered, Maren is safe, and you have chosen what to save." });

    // ================= Optional: the flooded schoolhouse =================
    L.room(S, 19, 9, 10, 10, 4.6, "brick", null, [3], "woodDark");
    L.box(S, 9.6, 0.03, 9.6, "wood", 19, 0.015, 9, { solid: false });
    L.label(S, "WARD ZERO INFANTS SCHOOL", 13.75, 3.4, 9, -PI / 2, 0.3, TW);
    L.box(S, 0.08, 1.6, 3.6, "ruin", 23.7, 2, 9);
    for (let r = 0; r < 3; r++) for (let k = 0; k < 2; k++) L.box(PA, 0.9, 0.76, 1.4, "wood", 16.2 + r * 2, 0.38, 6.6 + k * 4);
    L.label(PA, "BELL DAY TOMORROW\nwarm coats - hold hands - no running\ntwo by two up to the tower", 23.62, 2.1, 9, -PI / 2, 0.16, "#ffffff");
    L.npc({ group: PA, name: "", named: false, at: [16.9, 7], scale: 0.7, coat: "olive", route: [[16.9, 7, 6, "work"], [21, 7.5, 3, "look"], [16.9, 7, 5, "work"]] });
    L.light(PA, 0xffb469, 1.8, 10, 19, 3.8, 9);
    for (let i = 0; i < 4; i++) L.box(PR, 0.9, 0.76, 1.4, "woodDark", 16 + (i % 2) * 3, 0.4, 6 + Math.floor(i / 2) * 4, { ry: i * 0.7 });
    L.label(PR, "BELL DAY TOM...\n(the rest washed away)", 23.62, 2.1, 9, -PI / 2, 0.16, "#d8e8e8");
    L.box(PR, 1, 0.9, 2, "woodDark", 22, 0.45, 9);
    L.clue({ group: PR, at: [22, 0.95, 9], size: [0.5, 0.06, 0.4], prompt: "Read the class register", flag: "L05.classRegister", id: "L05.classRegister", title: "Class register - Ward Zero Infants",
      text: "Autumn term. Twelve names in a careful hand:\n\nAsh, Tilde - Corran, Bram - Corran, Wil - Fenn, Ada - Oskel, Maren (7) - Vell, Ilya - and six more.\n\nEvery name has been ruled through in red ink. In the margin, a different hand: 'struck by order - A.H.'\n\nUnder Maren's name someone has written, very small: 'still here?'" });
    L.crystals(FU, 5, 19, 9, 8, 8);
    L.holoMesh(FU, "box", "cyan", 23.3, 2.2, 9, 0.02, 0.8, 2.4, { bob: 0.04 });
    L.label(FU, "WARD ZERO INFANTS\nPUPILS PRESENT: 0", 23.25, 2.2, 9, -PI / 2, 0.2, "#ffffff");

    // ================= Named NPCs + background life =================
    L.npc({ group: PA, name: "Edda Oskel", at: [-3.4, -4.6], yaw: PI / 2, coat: "burgundy",
      task: "Washing the family's festival linens for tomorrow's Bell Day at the communal pump - dry since its handle went missing.",
      route: [[-2.4, -4.4, 5, "work"], [-8, 3, 4, "carry"], [12.6, 0.4, 3, "look"]],
      approach: ["Mind the tub, stranger.", "You're not from the basin, are you?"],
      talk: [line("Edda Oskel", "Bell Day tomorrow and not a drop from the pump. Tobin says the handle walked off."),
        line("Edda Oskel", "The Corran boys had it for their chalk game, up the north lane. I'd bet my good linen on it."),
        line("Edda Oskel", "We're all going up to the tower at first light. The Founders' men came round with the list."),
        line("Edda Oskel", "They said not to talk about it. Said it would only frighten people who aren't on the list."),
        line("Edda Oskel", "My Maren is seven. She thinks it's a festival trip. I let her think so.")],
      cond: [{ holding: "handle", lines: [line("Edda Oskel", "That's it - that's the handle!"), line("Edda Oskel", "Seat it in the yoke on top of the column. Tobin will weep, I swear.")] },
        { flag: F.oskel, lines: [line("Edda Oskel", "You've been in our house? Then you saw the calendar."), line("Edda Oskel", "I cut the little zero for Maren. If we're parted, she's to wait where the water can't follow.")] }],
      changedFlag: F.pump, changedRoute: [[-1.9, -3.9, 6, "work"], [-8, 3, 4, "carry"], [-3.6, -5.2, 4, "work"]],
      changedTalk: [line("Edda Oskel", "The water's sweet again. Clean linen for the tower tomorrow."), line("Edda Oskel", "Every family on the list goes up at first light. Even the babies. The bell will carry us.")] });
    L.npc({ group: PA, name: "Tobin Ash", at: [1.8, -0.6], yaw: -PI / 2, coat: "olive",
      task: "Pump mender of Old Veyra: keeping the communal pump - and the sump beneath it that keeps the basin dry - working before the festival.",
      route: [[1.9, -1.0, 5, "work"], [6.2, -7.9, 4, "work"], [0.4, 13.4, 4, "look"]],
      approach: ["Careful, that plinth is slick.", "If you're here about the pump - I know, I know."],
      talk: [line("Tobin Ash", "No pump, no sump. No sump, and the basin floods every spring."),
        line("Tobin Ash", "The handle's iron, about so long. The children took it to play. Find it and I owe you a pint."),
        line("Tobin Ash", "Founders' surveyors were down here last week, measuring the sump. Wouldn't say what for."),
        line("Tobin Ash", "Half the basin's on some list for Bell Day. Not me. Somebody has to keep the pump turning."),
        line("Tobin Ash", "And if the yard beam at the laundry ever goes, shore it - three timbers, never two.")],
      cond: [{ holding: "handle", lines: [line("Tobin Ash", "Bless you. Set it in the yoke - the bracket at the top of the column."), line("Tobin Ash", "Pump keeps the sump open, the sump keeps the basin dry. Forever, if we're careful.")] },
        { holding: "shore", lines: [line("Tobin Ash", "One of my shores. Under the laundry beam, is it? Good. Plumb and tight."), line("Tobin Ash", "Three of them and that beam will hold - or fall where you want it to.")] }],
      changedFlag: F.pump, changedRoute: [[1.9, -1.0, 6, "work"], [5.6, -6.4, 4, "idle"], [-1.2, -7, 4, "look"]],
      changedTalk: [line("Tobin Ash", "She draws clean. Hear that? This basin will never drown while that pump turns."), line("Arin", "(It will drown. But not as deep - not anymore.)")] });
    L.npc({ group: PR, name: "Hesk Lowry", at: [-6, -25.6], y: 3, coat: "grey",
      task: "The basin's last resident. Keeps a rescue lamp, a rope and a dry blanket on the Weir Terrace, watching the flood line.",
      route: [[-6, -25, 5, "look"], [8, -30.4, 5, "work"], [-10, -33, 4, "carry"]],
      approach: ["Mind the rail. Slick as glass.", "Another one come to stare at the drowned district?"],
      talk: [line("Hesk Lowry", "Thirty years I've watched this basin. Families lived down there - I'm sure of it. Nobody else is."),
        line("Hesk Lowry", "Their names were struck from the public record. Door plates scraped bare. As if they never were."),
        line("Hesk Lowry", "The order came from the tower. Founder Halden's seal on it. Nobody argued with a Founder."),
        line("Hesk Lowry", "Some nights, when the rain stops, I hear a child calling from the laundry."),
        line("Hesk Lowry", "The old pump used to keep the sump open. Nobody's mended it since before I can remember.")],
      cond: [{ flag: F.child, lines: [line("Hesk Lowry", "You brought her up. An Oskel child, alive."), line("Hesk Lowry", "Go. Whatever you came to the basin for - finish it.")] },
        { flag: F.hunt, lines: [line("Hesk Lowry", "Keeper lanterns in the basin. Ondine's people."), line("Hesk Lowry", "Keep to the dark and shift away from them. They only walk the PRESENT.")] },
        { flag: F.rooms, lines: [line("Hesk Lowry", "Tomorrow? On every calendar?"), line("Hesk Lowry", "I had one too. I marked it. I can't for the life of me remember why.")] },
        { flag: F.pump, lines: [line("Hesk Lowry", "The water dropped. Just like that - as if it had never been so high."), line("Hesk Lowry", "If you're going down, the sluice house wheels send the lanes to the culvert.")] }],
      changedFlag: F.child, changedRoute: [[1, -27.2, 6, "talk"], [-1.5, -27.8, 4, "idle"], [3.6, -28.4, 4, "look"]],
      changedTalk: [line("Hesk Lowry", "You brought her up. An Oskel child, alive."), line("Hesk Lowry", "Go. Whatever you came to the basin for - finish it.")] });
    L.crowd(PA, 0, -9, 18, 10, 5);
    L.crowd(PR, 9, -31.5, 6, 3, 2, { y: 3 });
    L.crowd(FU, 0, -7, 16, 12, 3, { holo: "cyan" });

    // ================= Dressing per state =================
    [[-11, -18], [11, -18], [-11, -4], [11, -4], [-11, 10], [11, 10]].forEach((p, i) => { if (i % 2 === 0) L.lamp(PA, p[0], p[1], "gas"); else { L.cyl(PA, 0.07, 4.2, "brass", p[0], 2.1, p[1], { seg: 8 }); } });
    L.bunting(PA, -11, 11, -11); L.bunting(PA, -11, 11, 3);
    L.cyl(PA, 0.08, 6, "brass", -7, 3, 8); L.cyl(PA, 0.08, 6, "brass", 7, 3, 8);
    L.box(PA, 13.6, 1.2, 0.05, "red", 0, 5.2, 8, { solid: false });
    L.label(PA, "BELL DAY - TOMORROW AT FIRST LIGHT", 0, 5.2, 7.95, PI, 0.42, "#ffd870");
    for (let i = 0; i < 3; i++) { L.box(PA, 1.4, 0.9, 2.4, "wood", -9.5, 0.45, -12 + i * 5); L.box(PA, 1.8, 0.05, 2.8, i % 2 ? "teal" : "red", -9.5, 2.4, -12 + i * 5, { solid: false }); }
    L.box(PA, 1.8, 0.9, 0.8, "woodDark", 6.6, 0.45, -8.8);
    L.cyl(PA, 0.5, 0.6, "wood", -2.4, 0.3, -5.2, { solid: true });
    L.particles(PA, "dust", [30, 6, 40], [0, 3, -4], 350);
    L.lamp(PR, -11, -14, "sodium", 4.6); L.lamp(PR, 11, 6, "sodium", 4.6); L.lamp(PR, -8, -25, "sodium", 3.6, 3);
    L.particles(PR, "rain", [60, 22, 60], [0, 8, -6], 1400);
    L.box(PR, 1.4, 0.6, 3.6, "woodDark", 9.5, 3.35, -31.5, { ry: 0.35 });
    L.box(PR, 1.2, 0.06, 0.9, "teal", 1.2, 3.06, -27.6, { solid: false });
    for (let i = 0; i < 5; i++) L.box(PR, 2.2, 0.01, 1.4, "puddle", -10 + i * 5, 3.01, -29 + (i % 3) * 2.2, { solid: false });
    L.lamp(FU, -11, -10, "neon", 5); L.lamp(FU, 11, 8, "neon", 5);
    L.crystals(FU, 12, 0, -4, 24, 34);
    for (let i = 0; i < 8; i++) { const a = i * PI / 4 + 0.17; L.box(FU, 0.08, 0.04, 8, i % 2 ? "neonC" : "neonV", Math.sin(a) * 7, 0.05, -2 + Math.cos(a) * 7, { solid: false, ry: a }); }
    L.debris(FU, 10, 0, 6, -4, 26, 6, 36);
    L.holoMesh(FU, "box", "cyan", 0, 7.5, -12, 6.4, 1.1, 0.02, { bob: 0.1 });
    L.label(FU, "OLD VEYRA BASIN - WARD 0\n3 FAMILIES EXPUNGED", 0, 7.5, -12.05, PI, 0.3, "#ffffff");
    L.particles(FU, "motes", [32, 8, 42], [0, 4, -4], 450);

    L.ambient("rain", 0.3, 2); L.ambient("water", 0.25, 2); L.ambient("crowd", 0.25, 1); L.ambient("machine", 0.08, 1);
    L.ambient("wind", 0.22, 4); L.ambient("drone", 0.25, 4);

    // ================= spawn, checkpoints, fragments =================
    L.player(0, 3, -31, PI);
    L.checkpoint(0, 3, -31, PI, { size: [6, 2.5, 4] });
    L.checkpoint(0, 0, -16.5, PI, { size: [6, 2.5, 3] });
    L.fragment("L05.1", [23, 1.3, 13], "A lesson plan", "Ward Zero Infants, Bell Day eve. 'Tomorrow we practise lining up quietly. Two by two, holding hands, all the way up to the tower. No one is to be left behind.' Pencilled beside it: Maren O. to hold the lantern.", S);
    L.fragment("L05.2", [-11.2, 4.3, -35.1], "A surveyor's note", "Founders' Survey, Old Veyra Basin. 'Depth at the sump: six metres. Projected flooding after the event: total. Recommend the listed households be moved before Bell Day - quietly.' Countersigned for Founder A. Halden.", S);
    L.crystals(FU, 4, -12.4, 13.4, 2, 2);
    L.fragment("L05.3", [-12.4, 2.6, 13.4], "Record 0-3-OSKEL", "Archive hologram, corrupted. 'MAREN OSKEL, AGE 7. STATUS: EVACUATED. STATUS: DECEASED. STATUS: - . Conflict unresolved. Flagged for review by O. Fenwick, head archivist.'", FU);
  },
});
