/* LEVEL 03 — The Rain Archive (Municipal Records Hall). Port of Level03Builder (Design v2).
   Layout (+z north): foyer z -10..0, grand hall x -18..18 z 0..48, Restricted Stacks x 18..26 z 33..45,
   floor hatch x -1.5..1.5 z 40..44.5 -> service stair -> Line 4 tunnel (y -3). */
AZ.registerLevel({
  id: "L03", order: 3, chapter: "LEVEL 03", title: "The Rain Archive", location: "Municipal Records Hall",
  start: 1, allowed: 3, killY: -12,
  build(L) {
    const { line } = L, S = L.shared, PA = L.past, PR = L.present, FU = L.future, W = L.W, T3 = L.THREE;
    const dyn = (parent, visible = true) => { const g = new T3.Group(); g.userData.dynamic = true; g.visible = visible; (parent || S).add(g); return g; };
    const MINT = "#9dffd9", WARM = "#ffd79a", COLD = "#bfe2ff";

    L.look({ fog: 0.6, past: { density: 0.008, exp: 1.08 }, present: { density: 0.02, sunI: 0.35 }, future: { density: 0.016, bloom: 1.5 } });
    L.interlude("Pell's Route", [
      line("Pell Ashgrove", "Records Hall's just past the fountain. I'll walk you. Couriers know the dry way."),
      line("Arin", "You keep looking at me like you're checking a delivery."),
      line("Pell Ashgrove", "Because I am. That letter in your coat. I've handed it to you before."),
      line("Pell Ashgrove", "More times than there are days, Arin. Same seal, same date. Tomorrow."),
      line("Arin", "And every time I say I don't remember you."),
      line("Pell Ashgrove", "Every time. But this time you asked where the records are kept. That's new."),
    ]);
    L.intro(line("Arin", "The Records Hall. Rain still finds its way in, even through the stone."),
      line("Arin", "The flooded register on the desk. Someone here is filed twice: once living, once dead."),
      line("Arin", "If the Present has smudged it, the Past still has it clean. << PAST for the old Hall."));

    L.objective("Read the flooded register in the foyer", "L03.ledgerRead", "The open ledger on the reception desk in the foyer.");
    L.objective("Find resident 0417 in the Past card drawers", "L03.nameFoundPast", "Shift to the PAST. Read the index board by the west cabinet first.");
    L.objective("Match the Present transit stamp on the damp ticket", "L03.stampMatched", "Lost-property tray at the east transit desk, then the stamp with the same mark.");
    L.objective("Reconstruct the deleted name from Future mineral traces", "L03.nameReconstructed", "In the FUTURE, four glowing fragments sit on the collapsed shelves.");
    L.objective("Route the rain channels to douse the burning stacks", "L03.waterRouted", "Past drainage plan: junction I east, II north, III east, IV north. Each routed channel puts out an aisle.");
    L.objective("Compare the clue cards and pick Maren's real record", "L03.recordPicked", "Clue cards west of the basin, five records on the board east of it.");
    L.objective("Read Maren's record", "L03.payoff", "On the record board beside the basin.");
    L.objective("Face Mother Ondine", "L03.ondineDone", "She is waiting at the basin.");
    L.objective("Reach Line 4 under the floor hatch", "L03.exit", "If the Keepers sealed the hatch, the release valve is in Odile's Restricted Stacks behind the east door.");
    L.payoff("L03.payoff", "A record in my own hand", "Death certificate, resident 0417, MAREN OSKEL, aged 7 - and stamped TRANSFERRED TO THE NURSERY REGISTER. Filed by Odile Fenwick, hidden in the survey basin. Countersigned: ARIN. It is my handwriting.");

    // ================================================================ shell
    const hallFloor = [[0, 20, 36, 40], [-9.75, 44, 16.5, 8], [9.75, 44, 16.5, 8], [0, 46.25, 3, 3.5]];
    hallFloor.forEach(f => L.floor(S, f[0], f[1], f[2], f[3], 0, "stone"));
    hallFloor.forEach(f => { L.box(PA, f[2], 0.02, f[3], "wood", f[0], 0.01, f[1], { solid: false }); L.box(PR, f[2], 0.02, f[3], "stoneWet", f[0], 0.01, f[1], { solid: false }); L.box(FU, f[2], 0.02, f[3], "obsidian", f[0], 0.01, f[1], { solid: false }); });
    L.wallDoor(S, -18, 0, 18, 0, 5, "stone", 4, 4);
    L.wall(S, -18, 0, -18, 48, 5, "stone", 0.6);
    L.wallDoor(S, 18, 0, 18, 48, 5, "stone", 2, 3, 39 / 48, 0.6);
    L.wall(S, -18, 48, 18, 48, 5, "stone", 0.6);
    for (let i = 0; i < 4; i++) for (const x of [-7, 7]) { L.cyl(S, 0.45, 14, "stone", x, 7, 6 + i * 10, { solid: true }); L.box(S, 1.3, 0.6, 1.3, "stone", x, 0.3, 6 + i * 10); }
    // Past + Present upper walls and roof with skylights; Future roofless ruin.
    [[PA, "plaster", "woodDark", true], [PR, "plasterCold", "iron", false]].forEach(([g, wm, rm, glass]) => {
      L.wall(g, -18, 0, 18, 0, 9, wm, 0.6, 5); L.wall(g, -18, 0, -18, 48, 9, wm, 0.6, 5); L.wall(g, 18, 0, 18, 48, 9, wm, 0.6, 5); L.wall(g, -18, 48, 18, 48, 9, wm, 0.6, 5);
      for (let i = 0; i < 5; i++) L.box(g, 36.6, 0.4, 8, rm, 0, 14.2, 4 + i * 10, { solid: false });
      for (let i = 0; i < 4; i++) { if (glass) L.box(g, 36, 0.08, 2, "glass", 0, 14.2, 9 + i * 10, { solid: false }); else { L.box(g, 9, 0.08, 2, "glass", -10, 14.2, 9 + i * 10, { solid: false }); L.box(g, 7, 0.08, 2, "glass", 9, 14.2, 9 + i * 10, { solid: false }); } }
      L.box(g, 35, 0.3, 0.4, glass ? "brass" : "rust", 0, 5.1, 0.45, { solid: false });
    });
    for (let i = 0; i < 16; i++) { const t = i / 15, h = 1 + (i * 37 % 6); L.box(FU, 0.6, h, 1.8, "ruin", -18, 5 + h / 2, t * 48, { solid: false }); L.box(FU, 0.6, h * 1.3, 1.6, "ruin", 18, 5 + h * 0.65, (1 - t) * 48, { solid: false }); }
    L.box(FU, 0.06, 0.06, 48, "neonC", -17.65, 5.02, 24, { solid: false }); L.box(FU, 0.06, 0.06, 48, "neonM", 17.65, 5.02, 24, { solid: false });
    L.box(FU, 4.5, 0.6, 2.5, "ruin", -4, 0.7, 20, { ry: 0.6 }); L.box(FU, 4.5, 0.6, 2.5, "ruin", 5, 0.7, 9, { ry: -0.4 }); L.box(FU, 4.5, 0.6, 2.5, "ruin", 11, 0.7, 33, { ry: 0.9 });
    L.debris(FU, 10, 0, 10, 24, 28, 6, 40);
    L.crystals(FU, 10, -16.5, 24, 1.5, 44); L.crystals(FU, 8, 16.5, 24, 1.5, 44);
    L.skyline(FU, 0, 24, 45, 75, 14, "obsidian", 33);
    L.holoMesh(FU, "torus", "magenta", 0, 22, 24, 22, 22, 1, { spin: 0.2 }).rotation.x = Math.PI / 2;
    L.label(FU, "VEYRA CIVIL MEMORY", 0, 30, 70, Math.PI, 3, "#66ffff", "rgba(6,4,18,.85)");

    // Shelves (shared) + per-state dressing.
    const shelves = [[-12.5, 10, 1.4, 6], [-12.5, 20, 1.4, 6], [12.5, 10, 1.4, 6], [12.5, 20, 1.4, 6], [-11, 42, 8, 1.2], [11, 42, 8, 1.2]];
    const bookM = ["red", "teal", "gold", "wood", "paper"];
    shelves.forEach((s, si) => {
      const alongZ = s[3] > s[2];
      L.box(S, alongZ ? 0.5 : s[2], 6, alongZ ? s[3] : 0.5, "woodDark", s[0], 3, s[1]);
      for (let lv = 0; lv < 4; lv++) {
        const y = 0.55 + lv * 1.4;
        for (const side of [-1, 1]) {
          const x = s[0] + (alongZ ? side * 0.45 : 0), z = s[1] + (alongZ ? 0 : side * 0.45), w = alongZ ? 0.35 : s[2] - 0.3, d = alongZ ? s[3] - 0.3 : 0.35;
          L.box(PA, w, 0.42, d, bookM[(si + lv) % 5], x, y, z, { solid: false });
          if ((lv + si) % 2 === 0) L.box(PR, w, 0.28, d, "plasterCold", x, y - 0.06, z, { solid: false });
          L.box(FU, w * 1.05, 0.38, d * 1.05, lv === 2 && si % 2 === 0 ? "neonC" : "ruin", x, y, z, { solid: false });
        }
      }
    });
    for (let i = 0; i < 6; i++) { const x = -10 + i * 4, z = 6 + (i % 3) * 9; L.cyl(PR, 0.22, 0.4, "iron", x, 0.2, z); L.box(PR, 0.5, 0.24, 0.4, "paper", x + 0.8, 0.12, z + 0.3, { solid: false }); }

    // ================================================================ foyer
    L.floor(S, 0, -5, 16, 10, 0, "stone");
    L.wall(S, -8, -10, -8, 0, 6, "stone"); L.wall(S, 8, -10, 8, 0, 6, "stone"); L.wall(S, -8, -10, 8, -10, 6, "stone");
    L.box(S, 16.5, 0.4, 10.5, "stone", 0, 6.2, -5, { solid: false });
    L.box(S, 4.5, 1.1, 1, "woodDark", 0, 0.55, -4); L.box(S, 4.7, 0.06, 1.2, "brass", 0, 1.13, -4, { solid: false });
    L.label(S, "MUNICIPAL RECORDS HALL", 0, 4.6, -0.4, Math.PI, 0.6, WARM, "rgba(30,20,10,.9)");
    L.label(S, "(0)  REGISTER OF THE LIVING AND THE DEAD  (0)", 0, 4.4, 0.4, 0, 0.4, WARM, "rgba(30,20,10,.9)");
    L.light(S, 0xf0e0c0, 1.4, 14, 0, 5, -5);
    L.clue({ group: PR, at: [0, 1.2, -4], size: [0.9, 0.08, 0.6], prompt: "Read the flooded register", flag: "L03.ledgerRead", id: "L03.ledger", title: "The flooded register",
      text: "REGISTER OF THE LIVING - Resident No. 0417, Lantern Row, aged 7. Status: ALIVE. Margin stamp: TRANSFERRED TO THE NURSERY REGISTER.\n\nREGISTER OF THE DEAD - Resident No. 0417, aged 7. Died in the catastrophe, at the second bell. Name: (erased - the ink has run off the page).\n\nSame number, both books. Cards for residents are kept in the card drawers by hundreds. The Past will have the card before the rain got to it.",
      lines: [line("Arin", "Alive in one book, dead in the other. And the name in the death book has washed away.")] });
    L.box(PR, 3, 0.02, 2, "puddle", 1.5, 0.02, -6, { solid: false });
    L.holoMesh(FU, "box", "cyan", 0, 2.2, -4, 1.6, 0.9, 0.02, { bob: 0.05 });
    L.label(FU, "0417 : ALIVE / DEAD\n[ RECORD CONFLICT ]", 0, 2.2, -4.05, Math.PI, 0.22, "#80ffff");

    // ================================================================ beat 1: Past drawers
    L.box(PA, 0.8, 2.6, 7.6, "wood", -17.3, 1.3, 10);
    L.label(PA, "CARD CATALOGUE OF RESIDENTS", -16.85, 3.2, 10, Math.PI / 2, 0.35, WARM, "rgba(30,20,10,.9)");
    L.box(PA, 0.1, 2.6, 2.4, "woodDark", -17.55, 2.3, 16.2);
    L.label(PA, "INDEX - DRAWERS BY HUNDREDS\n0000-0099 : 01\n0100-0199 : 02\n0200-0299 : 03\n0300-0399 : 04\n0400-0499 : 05\n0500-0599 : 06\n...\n1100-1199 : 12", -17.45, 2.3, 16.2, Math.PI / 2, 0.17, WARM);
    L.interact({ at: [-17.2, 1.4, 16.2], mesh: L.box(PA, 0.1, 0.5, 2.2, "brass", -17.42, 0.9, 16.2, { solid: false }), mask: 1, prompt: "Read the index board",
      onUse: it => { L.set("L03.indexRead"); L.say([line("Arin", "Drawers by hundreds. 0417 is in the four hundreds: drawer 05.")]); it.prompt = "Read the index board again"; } });
    for (let n = 1; n <= 12; n++) {
      const row = Math.floor((n - 1) / 4), col = (n - 1) % 4, z = 7.15 + col * 1.9, y = 2.05 - row * 0.72;
      const g = dyn(PA);
      const m = L.box(g, 0.14, 0.6, 1.7, "woodDark", -16.86, y, z, { solid: false });
      L.box(g, 0.06, 0.05, 0.35, "brass", -16.77, y - 0.1, z, { solid: false });
      L.label(g, (n < 10 ? "0" : "") + n, -16.76, y + 0.12, z, Math.PI / 2, 0.22, n === 5 ? "#ffe680" : WARM);
      let open = false;
      const lo = (n - 1) * 100, rng = String(lo).padStart(4, "0") + "-" + String(lo + 99).padStart(4, "0");
      L.interact({ at: [-16.6, y, z], mesh: m, mask: 1, radius: 2.4, prompt: "Open drawer " + (n < 10 ? "0" : "") + n, requires: ["L03.indexRead"], locked: "Twelve drawers, thousands of cards. Read the index board beside the cabinet first.",
        onUse: () => {
          open = !open; g.position.x = open ? 0.45 : 0;
          if (n === 5) {
            L.set("L03.nameFoundPast"); L.journal("L03.card", "Registry card 0417", "MAREN OSKEL, aged 7, Lantern Row. Daughter of Tamsin and Joss Oskel. LIVING. Red stamp: TRANSFERRED TO THE NURSERY REGISTER - by Founders' order. No address given.");
            AZ.ui.document("Registry card 0417", "RESIDENT No. 0417 - MAREN OSKEL, aged 7.\nLantern Row, upper flat. Daughter of Tamsin and Joss Oskel, weavers at the festival looms.\nStatus: LIVING. Registered by O. Fenwick, Head Archivist.\n\nFresh red stamp across the corner: TRANSFERRED TO THE NURSERY REGISTER - by Founders' order. No address given.\n\nThere is no death on this card. Not even a pencil mark.");
            L.say([line("Arin", "Maren Oskel. Seven years old. Alive, and moved to a register with no address.")]);
          } else { L.sfx("clang", 0.6); L.say([line("Arin", "Cards " + rng + ". Not the range for 0417.")]); }
        } });
    }
    L.light(PA, 0xffc890, 2, 8, -15.5, 3.2, 11);
    // Past drainage plan.
    L.box(PA, 0.7, 1.1, 0.6, "wood", -8.5, 0.55, 31);
    L.label(PA, "SURVEY OF RAIN-COURSES", -8.5, 2.3, 31.4, Math.PI, 0.3, WARM, "rgba(30,20,10,.9)");
    L.clue({ group: PA, at: [-8.5, 1.18, 31], size: [0.8, 0.05, 0.6], prompt: "Study the drainage plan", flag: "L03.planRead", id: "L03.plan", title: "Drainage plan of the Records Hall",
      text: "The Hall was built over the old rain-courses. Floor channel junctions turn by hand.\n\nTo carry roof water into the Survey Basin:\n  Junction I   - turn to EAST\n  Junction II  - turn to NORTH\n  Junction III - turn to EAST\n  Junction IV  - turn to NORTH, into the basin.\n\nThe Basin shows the city's survey map when filled. - O. Fenwick" });

    // ================================================================ beat 2: Present stamp
    L.box(PR, 1, 1.1, 6, "woodDark", 15.6, 0.55, 28);
    L.label(PR, "TRANSIT BUREAU - LINE STAMPS", 17.3, 3.2, 28, -Math.PI / 2, 0.35, COLD, "rgba(8,14,22,.9)");
    L.label(PR, "LOST PROPERTY", 15, 1.5, 25.6, -Math.PI / 2, 0.2, COLD);
    L.clue({ group: PR, at: [15.5, 1.18, 25.6], size: [0.5, 0.04, 0.4], prompt: "Read the damp ticket", flag: "L03.ticketRead", id: "L03.ticket", title: "Damp transit ticket",
      text: "LINE 4 - SUBSURFACE. CHILD FARE. Holder: resident No. 0417.\nPunched for travel on the 3rd day AFTER the date in the Register of the Dead.\n\nThe bureau mark is half-dissolved but the shape is clear: a V between angle brackets:  <V>\n\nIf a stamp on the rack makes the same mark, the ticket is genuine.",
      lines: [line("Arin", "A dead child bought a ticket. Let's see if the bureau really stamped it.")] });
    ["O", "#", "<V>", "X", "<>"].forEach((mk, i) => {
      const z = 26.9 + i * 0.6;
      const m = L.cyl(PR, 0.2, 0.3, mk === "<V>" ? "brass" : "rust", 15.6, 1.27, z);
      L.label(PR, mk, 15.3, 1.75, z, -Math.PI / 2, 0.25, "#ffffff");
      L.interact({ at: [15.6, 1.3, z], mesh: m, mask: 2, radius: 2.4, prompt: "Press stamp " + mk, requires: ["L03.ticketRead"], locked: "Find something to compare these stamps with.",
        onUse: () => {
          if (mk !== "<V>") { L.sfx("clang", 0.6); L.say([line("Arin", "A '" + mk + "' mark. That's not what is on the ticket.")]); return; }
          L.set("L03.stampMatched");
          L.journal("L03.stamp", "The stamp matches", "The <V> stamp of LINE 4 - SUBSURFACE matches the ticket. Its date wheel is set three days after resident 0417's recorded death - on a line the Past clerks say has not opened.");
          AZ.ui.document("The stamp matches", "The <V> stamp leaves exactly the mark on the ticket. The bureau's date wheel is set three days after resident 0417's recorded death.\n\nSomeone who was 'dead' travelled on a line that had not even opened yet.");
          L.say([line("Arin", "Same mark. The ticket's real. The stamp's date hasn't happened yet.")]);
        } });
    });
    L.when(["L03.stampMatched"], () => setTimeout(() => L.unlockStates(4, "The stamp is dated in a time that has not happened. FUTURE >> is now reachable."), 1500));

    // ================================================================ beat 3: Future mineral traces
    const frags = [[-11.7, 2.6, 20, "(1)  MA-", Math.PI / 2], [-12, 2.0, 41.3, "(2)  -REN", Math.PI], [10, 3.2, 41.3, "(3)  OS-", Math.PI], [11.7, 2.2, 20, "(4)  -KEL", -Math.PI / 2]];
    frags.forEach((f, i) => {
      const along = Math.abs(f[4]) === Math.PI / 2;
      const m = L.box(FU, along ? 0.12 : 1.2, 0.8, along ? 1.2 : 0.12, "neonC", f[0], f[1], f[2], { solid: false });
      const off = along ? [f[0] > 0 ? -0.15 : 0.15, 0] : [0, -0.15];
      L.label(FU, f[3], f[0] + off[0], f[1] + 0.75, f[2] + off[1], f[4], 0.45, MINT);
      if (i % 2) L.light(FU, 0x8cffd0, 1.8, 6, f[0] + off[0] * 5, f[1], f[2] + off[1] * 5);
      L.interact({ at: [f[0] + off[0] * 3, f[1], f[2] + off[1] * 3], mesh: m, mask: 4, once: true, prompt: "Trace the mineral letters " + (i + 1),
        onUse: () => { L.set("L03.trace" + (i + 1)); L.sfx("memory", 0.6); L.say([line("Arin", "Ink dissolved, minerals stayed. Fragment " + (i + 1) + " of 4: " + f[3].slice(5).trim())]); } });
    });
    const nameG = dyn(FU, false);
    L.label(nameG, "MAREN OSKEL\n( erased from the Register of the Dead )", 0, 6.5, 41, Math.PI, 0.8, MINT);
    L.holoMesh(nameG, "box", "cyan", 0, 6.5, 41.1, 9, 2.2, 0.02);
    L.when(["L03.trace1", "L03.trace2", "L03.trace3", "L03.trace4"], () => {
      nameG.visible = true; L.set("L03.nameReconstructed"); L.sfx("reveal");
      L.journal("L03.name", "The erased name", "Mineral traces in the Future spell the name erased from the Register of the Dead: M-A-R-E-N  O-S-K-E-L.");
      L.toast("Reconstructed: MAREN OSKEL - the name deleted from the Register of the Dead.", 4.5);
    });
    L.zone({ at: [0, 1.5, 24], size: [30, 3, 40], mask: 4, lines: [line("The Witness", "Water forgets ink. Stone does not.")] });
    L.zone({ at: [0, 1.5, 2.5], size: [6, 3, 2], lines: [line("The Witness", "A name written twice is a name someone wanted kept.")] });

    // ================================================================ water routing + fire
    L.cyl(S, 1.7, 0.7, "copper", 0, 0.35, 36.5, { solid: true });
    L.label(S, "SURVEY BASIN (0)", 0, 0.45, 34.75, Math.PI, 0.25, "#d0ffe8");
    L.cyl(PR, 0.9, 0.4, "rust", -12, 0.2, 31, { solid: true });
    L.label(PR, "ROOF RAIN", -12, 1, 29.95, Math.PI, 0.22, COLD);
    L.particles(PR, "rain", [1.2, 13, 1.2], [-12, 0.5, 31], 150);
    const J = [[-12, 26], [-6, 26], [-6, 32], [0, 32]];
    const path = [[-12, 31, -12, 26], [-12, 26, -6, 26], [-6, 26, -6, 32], [-6, 32, 0, 32], [0, 32, 0, 36.5]];
    const flow = dyn(PR, false);
    path.forEach(([x1, z1, x2, z2]) => {
      const cx = (x1 + x2) / 2, cz = (z1 + z2) / 2, w = Math.abs(x2 - x1) || 0.5, d = Math.abs(z2 - z1) || 0.5;
      L.box(PR, w + 0.2, 0.03, d + 0.2, "iron", cx, 0.035, cz, { solid: false });
      L.box(flow, w, 0.03, d, "water", cx, 0.06, cz, { solid: false });
    });
    L.cyl(flow, 1.42, 0.05, "water", 0, 0.73, 36.5);
    const ROMAN = ["I", "II", "III", "IV"];
    const rots = [[3, 1], [2, 0], [2, 1], [1, 0]].map((sc, i) => L.rotator({ group: PR, at: [J[i][0], 0.15, J[i][1]], size: [1.2, 0.2, 1.2], mat: "copper", steps: 4, start: sc[0], correct: sc[1], glyph: ROMAN[i], prompt: "Turn junction #" + (i + 1) }));
    L.label(PR, "N ^", -6, 0.07, 29, 0, 0.4, COLD).rotation.x = -Math.PI / 2;
    const channels = () => { let up = true; rots.forEach((r, i) => { up = up && r.ok(); const f = "L03.channel" + (i + 1); if (up && !L.get(f)) { L.toast("Water runs past junction " + ROMAN[i] + ".", 2); L.sfx("water", 0.5); } L.set(f, up); }); };
    AZ.on("rotated", channels);
    L.alignment(rots, "L03.waterRouted", { toast: "The rain runs the old courses into the Survey Basin." });

    const fireG = L.present;
    const fireNodes = [[-12.5, 0, 24, "L03.channel1", "AISLE I"], [-9, 0, 22.5, "L03.channel2", "AISLE II"], [-9.6, 0, 35, "L03.channel3", "AISLE III"], [-4, 0, 40.2, "L03.channel4", "AISLE IV"], [-9, 0, 40.6, "L03.waterRouted", "EVIDENCE STACKS"]];
    fireNodes.forEach(n => { L.box(PR, 1.2, 0.9, 0.6, "woodDark", n[0], 0.45, n[2], { solid: false }); L.label(PR, n[4], n[0], 1.6, n[2] - 0.35, Math.PI, 0.2, "#ffc080"); });
    L.fire({ group: fireG, nodes: fireNodes.map(n => ({ at: [n[0], n[1], n[2]], out: n[3] })), interval: 11, telegraph: 4, startFlag: "L03.nameReconstructed", lostFlag: "L03.recordsLost", allOutFlag: "L03.fireOut",
      lostMessage: "The fire reached the evidence stacks. Route the channels faster: each junction puts out the aisle beside it.", startToast: "Fire! The Keepers are burning the records." });
    L.zone({ at: [0, 1.5, 24], size: [34, 3, 46], mask: 2, requires: ["L03.nameReconstructed"],
      lines: [line("Arin", "Smoke. The Keepers are burning the stacks - working toward the evidence shelves."), line("Arin", "The channels run past every aisle. Route the water and it'll drown the fire.")] });

    // ================================================================ map, clue cards, deduction board
    const map = dyn(PR, false);
    L.holoMesh(map, "cyl", "cyan", 0, 1.9, 36.5, 3, 0.01, 3, { spin: 0.3 });
    L.holoMesh(map, "box", "amber", 0, 1.95, 37.1, 0.08, 0.02, 2.2);
    L.holoMesh(map, "sphere", "magenta", 0, 2.05, 36.8, 0.2, 0.2, 0.2);
    L.label(map, "SURVEY: LINE 4 - SUBSURFACE\nservice stair under the floor hatch  >>  north", 0, 2.9, 36.5, Math.PI, 0.25, "#9fffff");
    const recG = dyn(PR, false);
    L.box(recG, 0.7, 1, 2, "copper", -2.7, 0.5, 36.5);
    L.label(recG, "CLUE CARDS", -2.75, 1.9, 36.5, Math.PI / 2, 0.25, WARM);
    const cards = [["L03.nameFoundPast", "Clue card 1 - the Past card", "From drawer 05: resident 0417 is SEVEN years old and lives on LANTERN ROW."],
      ["L03.stampMatched", "Clue card 2 - the Present stamp", "From the transit bureau: the genuine ticket carries the Line 4 mark  <V>  - no other mark."],
      ["L03.nameReconstructed", "Clue card 3 - the Future traces", "From the mineral traces: the erased name is spelled  M-A-R-E-N  O-S-K-E-L.  Not Marin. Not Oskal."]];
    cards.forEach((c, i) => {
      const z = 35.85 + i * 0.65;
      L.label(recG, String(i + 1), -2.6, 1.3, z, Math.PI / 2, 0.25, "#ffffff");
      L.clue({ group: recG, at: [-2.6, 1.05, z], size: [0.45, 0.06, 0.5], prompt: "Read clue card #" + (i + 1), flag: "L03.card" + (i + 1), id: "L03.cardclue" + (i + 1), title: c[1], text: c[2], requires: [c[0]], locked: "This card is blank - you haven't found that piece of evidence yet.", noMarker: true });
    });
    const cand = ["A   MAREN OSKEL   age 7   Lantern Row    mark <O>", "B   MARIN OSKEL   age 7   Lantern Row    mark <V>", "C   MAREN OSKEL   age 7   Lantern Row    mark <V>", "D   MAREN OSKEL   age 9   Lantern Row    mark <V>", "E   MAREN OSKAL   age 7   Weavers Lane   mark <V>"];
    const board = L.box(recG, 0.8, 1.1, 2.2, "copper", 3, 0.55, 36.5);
    L.box(recG, 0.05, 1.6, 3.4, "woodDark", 3.45, 2.1, 36.5, { solid: false });
    L.label(recG, "FIVE RECORDS, ONE REAL\n\n" + cand.join("\n"), 3.4, 2.1, 36.5, -Math.PI / 2, 0.14, "#f2e6c0");
    L.deduction({ at: [3, 1.1, 36.5], mesh: board, mask: 2, prompt: "Study the five records", question: "Which record is Maren's?", candidates: cand, correct: 2, flag: "L03.recordPicked",
      requires: ["L03.card1", "L03.card2", "L03.card3"], locked: "Lay out all three clue cards first (west side of the basin).", resetFlags: ["L03.card1", "L03.card2", "L03.card3"],
      wrong: "Wrong record - and the clatter carried through the Hall.", lines: [line("Arin", "Seven. Lantern Row. The V mark. Spelled with an E. This one.")] });
    const realG = dyn(recG, false);
    const realRec = L.clue({ group: realG, at: [3, 1.18, 36.5], size: [0.5, 0.05, 0.45], prompt: "Read Maren's record", flag: "L03.payoff", id: "L03.record", title: "The contradictory record", requires: ["L03.recordPicked"],
      text: "CERTIFICATE OF DEATH - Resident No. 0417, MAREN OSKEL, aged 7, Lantern Row.\nCause: 'the second bell'.\nStamped across it: TRANSFERRED TO THE NURSERY REGISTER.\n\nCertified: O. Fenwick, Head Archivist.\nCountersigned by the witness of record:\n\n        ARIN\n\nPinned to it, in Odile's hand: 'Hidden in the survey basin. She is not dead. I will not let them file her twice.'",
      lines: [line("Arin", "Dead and transferred. On the same page. And that's my signature."), line("The Witness", "Some names are hidden to keep them safe. Follow the water down, Arin.")] });
    realRec.enabled = false;
    L.when(["L03.waterRouted"], () => { flow.visible = true; map.visible = true; recG.visible = true; L.sfx("water"); L.toast("A map rises from the basin. Five sodden records have surfaced beside it - only one is Maren's.", 4.5); });
    L.when(["L03.recordPicked"], () => { realG.visible = true; realRec.enabled = true; });
    let mapT = 0; L.update(dt => { if (map.visible && mapT < 1) { mapT = Math.min(1, mapT + dt / 3); map.position.y = -1.3 + 1.3 * mapT; } });
    map.position.y = -1.3;

    // ================================================================ hatch + tunnel + exit
    L.box(S, 3.4, 0.08, 0.2, "brass", 0, 0.04, 39.9, { solid: false }); L.box(S, 3.4, 0.08, 0.2, "brass", 0, 0.04, 44.6, { solid: false });
    const hatch = L.box(S, 3, 0.2, 4.5, "rust", 0, -0.1, 42.25, { dynamic: true });
    L.label(S, "SERVICE STAIR - LINE 4 SUBSURFACE", 0, 2.6, 47.6, Math.PI, 0.35, COLD, "rgba(8,14,22,.9)");
    L.light(S, 0xff9050, 1.6, 8, 0, 2.2, 46.5);
    let hatchT = 0, hatchOpen = false;
    L.when(["L03.hatchOpen"], () => { hatchOpen = true; hatch.userData.collider.on = false; L.sfx("rule"); L.toast("The floor hatch unlatches. The subsurface tram route is open.", 3.5); });
    L.update(dt => { if (hatchOpen && hatchT < 1) { hatchT = Math.min(1, hatchT + dt / 2); hatch.rotation.z = hatchT * 1.9; hatch.position.set(-1.5 + Math.cos(hatchT * 1.9) * 1.5, -0.1 + Math.sin(hatchT * 1.9) * 1.5, 42.25); } });
    L.stairs(S, [0, 0, 40], [0, -3, 44.5], 3, "concrete");
    L.wall(S, -1.6, 40, -1.6, 44.5, 3, "concrete", 0.2, -3).position.x += 0.012; L.wall(S, 1.6, 40, 1.6, 44.5, 3, "concrete", 0.2, -3).position.x -= 0.012; // meshes off the floor-slab edges
    L.floor(S, 0, 52.25, 6, 15.5, -3, "concrete");
    L.wall(S, -3, 44.5, -3, 60, 2.6, "tile", 0.4, -3); L.wall(S, 3, 44.5, 3, 60, 2.6, "tile", 0.4, -3); L.wall(S, -3, 60, 3, 60, 2.6, "tile", 0.4, -3);
    L.box(S, 6.4, 0.4, 12, "concrete", 0, -0.2, 54, { solid: false });
    for (const x of [-0.75, 0.75]) L.box(S, 0.08, 0.14, 12, "rail", x, -2.93, 54, { solid: false });
    L.box(S, 2.6, 2.3, 4.4, "iron", 0, -1.75, 57.6); L.box(S, 1.2, 1.9, 0.06, "cold", 0, -1.95, 55.38, { solid: false }); L.box(S, 2.6, 0.12, 0.04, "neonC", 0, -1.2, 55.37, { solid: false });
    L.label(S, "LINE 4", 0, -0.85, 55.3, Math.PI, 0.3, "#9fffff");
    L.light(S, 0x60c0ff, 1.8, 9, 0, -1, 53); L.light(S, 0xff9a48, 1.4, 8, -2.4, -0.8, 48);
    L.zone({ at: [0, -1.5, 52.4], size: [5.4, 3, 1.2], flag: "L03.exit" });
    L.exit({ group: S, at: [0, -3, 54.4], size: [5, 2.5, 1.4], requires: ["L03.hatchOpen"], locked: "The hatch is shut. Fill the Survey Basin, find Maren's record - and get past whoever comes for it." });
    L.ambient("machine", 0.12);

    // ================================================================ Keepers + Sister Vossel
    const kp = L.keeper({ name: "Keeper", mode: "hunter", group: PR, at: [-3, 6], path: [[-3, 6], [-3, 27], [3, 27], [3, 6]], disabledBy: "L03.payoff", caught: "A Keeper caught you. Shelves block the lantern - and the PAST hides you." });
    L.keeper({ name: "Keeper", mode: "hunter", group: PR, at: [9, 8], path: [[9, 8], [9, 33], [13.6, 33.5], [10, 22]], disabledBy: "L03.payoff", caught: kp.o.caught });
    const voss = L.keeper({ name: "Sister Vossel", mode: "watcher", group: PR, at: [-6, 3.5], yaw: Math.PI / 2, path: [[-6, 3.5], [6, 3.5]], disabledBy: "L03.payoff" });
    L.label(voss.p.group, "Sister Vossel", 0, 2.25, 0, 0, 0.22, "#ffd0c0");
    let anomalies = 0;
    AZ.on("shift", () => { if (voss.seesNow && W.state !== 0) { anomalies++; AZ.ui.bark("Sister Vossel", anomalies > 1 ? "Again. Mother Ondine will hear of this." : "The cup-saver. Shifting again, are we?"); } });
    L.talkPoint({ at: voss.p.group.position, mesh: voss.p.group, mask: 2, prompt: "Talk to Sister Vossel" }, { speaker: "Sister Vossel", question: "Sister Vossel blocks the aisle.",
      opening: [line("Sister Vossel", "The one from the festival street. You saved a blue cup that was supposed to break."), line("Sister Vossel", "Things that should break keep the night whole. You keep un-breaking them.")],
      options: [{ text: "I'm only here to read.", sets: "L03.vosselRead", lines: [line("Sister Vossel", "Then read quickly. Paper is very flammable tonight."), line("Arin", "Was that a threat?"), line("Sister Vossel", "A forecast.")] },
        { text: "What do the Keepers want?", sets: "L03.vosselAsked", lines: [line("Sister Vossel", "For tonight to never end. Inside a night that repeats, nobody dies for good."), line("Sister Vossel", "Mother Ondine will explain it better. She's on her way.")] },
        { text: "Who signed the dead child's record?", requires: ["L03.ledgerRead"], missing: "(read the register first)", sets: "L03.vosselPressed", lines: [line("Sister Vossel", "Records are lies the city tells itself. We burn the ones that hurt."), line("Arin", "You mean the ones that prove something.")] }],
      doneFlag: "L03.vosselTalked" });

    // ================================================================ NPCs
    L.npc({ name: "Odile Fenwick", group: PA, at: [-15, 9], coat: "burgundy", task: "Head Archivist: files resident cards and keeps the rain-courses survey.",
      route: [[-15.4, 9, 6, "work"], [-8.5, 30, 5, "look"], [-2, 14, 5, "work"], [-15.4, 14, 4, "look"]],
      approach: ["Quietly, please. The Hall is listening.", "You look like someone hunting for a name."],
      talk: [line("Odile Fenwick", "Every resident of Veyra has a card. Every card has a drawer. The drawers go by hundreds."), line("Odile Fenwick", "The index board by the cabinet will tell you which one."),
        line("Odile Fenwick", "A record is a promise. Write a name once and the city owes that person a place."), line("Odile Fenwick", "Lately the Founders' office sends me stamps I have never seen. 'Transferred.' Transferred where?"),
        line("Odile Fenwick", "My own office is through the east door. Restricted. I keep the key, and the questions.")],
      cond: [{ flag: "L03.odileNote", lines: [line("Odile Fenwick", "You read my desk. Then you know I hid something."), line("Odile Fenwick", "If the Founders want a child filed as dead, they will have to find the paper first.")] },
        { flag: "L03.nameFoundPast", lines: [line("Odile Fenwick", "Maren Oskel? Seven. Hums four notes over and over and swears there is a fifth."), line("Odile Fenwick", "Dead? Her mother brought her in this morning to be stamped for the Nursery register.")] },
        { flag: "L03.ledgerRead", lines: [line("Odile Fenwick", "0417? Four hundreds - drawer five. Mind the brass handle, it sticks.")] }],
      changedFlag: "L03.nameFoundPast", changedRoute: [[-1, -3, 6, "work"], [-15.4, 10, 6, "look"], [-8.5, 29.5, 4, "idle"]],
      changedTalk: [line("Odile Fenwick", "I re-read card 0417 three times. There is no death on it. Who told you there would be?")] });
    L.npc({ name: "Tobin Reyes", group: PA, at: [14, 27], coat: "olive", task: "Records clerk: stamps transit dockets and carries them to the catalogue.",
      route: [[14.4, 27.5, 5, "work"], [5, 22, 3, "carry"], [-15.4, 12, 4, "work"], [4, 8, 3, "carry"]], approach: ["Docket? Ticket? Complaint?", "Mind the ink, it's still wet."],
      talk: [line("Tobin Reyes", "Every line has its own stamp. Circle for the river line, hash for the market loop."), line("Tobin Reyes", "Nine days ago the Founders' office ordered a whole new box of Line Four tickets. Child fares."),
        line("Tobin Reyes", "Mistress Fenwick doesn't like it. She reads every docket twice now."), line("Tobin Reyes", "Cards don't lie. People with stamps do."), line("Tobin Reyes", "The V in brackets is Line Four - the subsurface. It isn't even open.")],
      cond: [{ flag: "L03.ticketRead", lines: [line("Tobin Reyes", "A Line Four ticket with the <V> mark? We only keep that stamp for the opening day."), line("Tobin Reyes", "Which hasn't come yet. Where did you find it?")] },
        { flag: "L03.nameFoundPast", lines: [line("Tobin Reyes", "Oskel? I stamped a child's ticket for an Oskel last week. Founders' account.")] }],
      changedFlag: "L03.stampMatched", changedRoute: [[14.4, 28.5, 8, "look"], [10, 30, 4, "idle"], [14.4, 26, 6, "work"]],
      changedTalk: [line("Tobin Reyes", "Somebody used the Line Four stamp. The date wheel is set to days that haven't happened.")] });
    L.npc({ name: "Bram Hollis", group: PR, at: [-10, 29], coat: "navy", task: "Night caretaker (a Rememberer): carries rain buckets so the last dry records survive.",
      route: [[-10.8, 30, 4, "carry"], [6, 12, 4, "work"], [10, 33, 3, "carry"], [-2, 4, 3, "idle"]], approach: ["Careful, floor's slick.", "Hall's closed. Not that anyone's left to close it for."],
      talk: [line("Bram Hollis", "The rain comes in where the skylights broke. I empty, it fills. That's the job."), line("Bram Hollis", "Forty years of nights here. Since the catastrophe. Since the 'earthquake'."),
        line("Bram Hollis", "Odd thing about the Register of the Dead. Lot of children in it. Lot of children nobody ever mourned."), line("Bram Hollis", "Old Fenwick's office is behind the east door. I leave a lamp on."),
        line("Bram Hollis", "There are channels in the floor. Turnable junctions, numbered. Nobody remembers which way they go.")],
      cond: [{ flag: "L03.ondineDone", lines: [line("Bram Hollis", "Mother Ondine herself. Pell said she'd come if anyone found that record.")] },
        { flag: "L03.vosselTalked", lines: [line("Bram Hollis", "Vossel. The Keepers' watcher. Pell warned us - she was on the festival street too.")] },
        { flag: "L03.nameReconstructed", lines: [line("Bram Hollis", "Maren Oskel. The Oskels left on the trams nine days before the bell. They never came back up.")] },
        { flag: "L03.planRead", lines: [line("Bram Hollis", "A drainage plan from the old days? Then turn them, friend. I'll hold the bucket.")] }],
      changedFlag: "L03.waterRouted", changedRoute: [[2.4, 33.6, 10, "look"], [-3, 34, 6, "idle"], [-10.8, 30, 4, "work"]],
      changedTalk: [line("Bram Hollis", "Look at that map. There's a stair under the hatch I've mopped around for forty years.")] });
    L.crowd(PA, 0, 25.5, 10, 6, 7);
    for (let i = 0; i < 3; i++) { const z = 8 + i * 5; L.box(PA, 5, 0.08, 1.4, "wood", 0, 0.75, z, { solid: false }); L.box(PA, 4.6, 0.74, 1.1, "woodDark", 0, 0.37, z); L.sphere(PA, 0.16, "warm", 0, 1.3, z); }

    // ================================================================ Restricted Stacks (side area)
    L.floor(S, 22.3, 39, 8.6, 12, 0, "woodDark");
    L.wall(S, 18.3, 33, 26.6, 33, 4.2, "stone"); L.wall(S, 18.3, 45, 26.6, 45, 4.2, "stone"); L.wall(S, 26.6, 33, 26.6, 45, 4.2, "stone");
    L.box(PA, 8.8, 0.4, 12.4, "plaster", 22.3, 4.4, 39, { solid: false }); L.box(PR, 8.8, 0.4, 12.4, "plasterCold", 22.3, 4.4, 39, { solid: false });
    L.label(S, "RESTRICTED STACKS - O. FENWICK", 17.65, 3.5, 39, -Math.PI / 2, 0.25, WARM, "rgba(30,20,10,.9)");
    L.box(PA, 0.2, 3, 2, "wood", 18, 1.5, 39); L.label(PA, "LOCKED\nkey with O. Fenwick", 17.85, 1.8, 39, -Math.PI / 2, 0.2, WARM);
    L.box(S, 1.6, 0.8, 1, "wood", 24.5, 0.4, 41.5);
    for (let i = 0; i < 3; i++) { L.box(S, 1.8, 3.2, 0.8, "woodDark", 20 + i * 2.4, 1.6, 33.7); L.label(S, i === 1 ? "NURSERY REGISTER\nSEALED BY FOUNDERS' ORDER" : "TRANSFERS " + (i ? "M-Z" : "A-L"), 20 + i * 2.4, 2.9, 34.15, 0, 0.16, WARM); }
    L.box(S, 1.4, 0.8, 1, "wood", 21, 0.4, 43.8); L.label(S, "LINE 4 - CHILD FARES - 214", 21, 0.6, 43.25, Math.PI, 0.18, "#e8dcc0");
    L.box(S, 0.03, 0.6, 0.8, "paper", 26.35, 1.6, 38, { solid: false }); L.label(S, "( 0 )\nme and the\nnice lady\n- M.", 26.3, 1.6, 38, -Math.PI / 2, 0.14, "#4a3020");
    L.clue({ group: S, mask: 6, at: [24.5, 0.85, 41.5], size: [0.5, 0.04, 0.4], prompt: "Read the note on Odile's desk", flag: "L03.odileNote", id: "L03.odile", title: "Odile Fenwick's desk note",
      text: "They want her filed twice. Living, then transferred to a register that has no address. Then dead, so no one will look.\n\nI have kept the certificate. It has a countersignature I recognise from the visitors' book, the same careful hand every festival morning: ARIN.\n\nI have put it where only water will find it - the survey basin.\n\n- O.F.", lines: [line("Arin", "She hid it on purpose. And she knew my handwriting before I did.")] });
    L.clue({ group: S, mask: 6, at: [23.4, 0.85, 41.5], size: [0.45, 0.04, 0.4], prompt: "Take Odile's forged copy", flag: "L03.forgedCopy", id: "L03.forged", title: "Odile's forged copy",
      text: "A second certificate for resident 0417, perfect in every line - except the countersignature is a careful imitation, and the paper is new.\n\nOdile's pencil note: 'For the Keepers, when they come. Let them burn this one.'", lines: [line("Arin", "A decoy. Odile planned for the Keepers.")] });
    L.light(PR, 0xffc070, 1.4, 7, 24, 1.4, 41.2);
    L.holoMesh(FU, "box", "magenta", 22.5, 2.2, 39, 1.4, 1, 0.02, { bob: 0.05 }); L.label(FU, "NURSERY REGISTER\nADDRESS: (none)", 22.5, 2.2, 38.95, Math.PI, 0.18, "#ffb8f0");
    const valve = L.cyl(S, 0.45, 0.25, "brass", 25.9, 1.3, 36); valve.rotation.z = Math.PI / 2;
    L.label(S, "HATCH RELEASE", 25.6, 2.1, 36, -Math.PI / 2, 0.22, WARM);
    L.interact({ at: [25.6, 1.3, 36], mesh: valve, prompt: "Turn the hatch release", requires: ["L03.refused"], locked: "An old hatch release. The hatch isn't sealed - nothing to release.", once: true,
      onUse: () => { L.set("L03.hatchReleased"); L.sfx("clang"); L.say([line("Arin", "Chains off. Now back to the hatch - fast.")]); } });

    // ================================================================ climax: Mother Ondine
    L.checkpoint(-1.5, 0, 33.2, 0, { climax: true, size: [4, 2.5, 2] });
    const ond = L.npc({ name: "Mother Ondine Halden", group: PR, at: [-1.6, 31.5], yaw: 0.3, coat: "white", task: "Mother of the Keepers of the Hour: came to burn the record that proves the night can be broken.",
      route: [[-1.4, 34, 25, "look"], [-3.5, 33, 6, "idle"]], approach: ["Careful, Arin. Water conducts more than sound.", "My father kept lists. I keep nights."],
      talk: [line("Mother Ondine Halden", "My father, Founder Halden, chose who would be saved. He chose silence. Silence worked."), line("Mother Ondine Halden", "Inside the loop no one dies for good. Every morning, alive."),
        line("Mother Ondine Halden", "You call it a cage. I call it the only mercy this city ever got."), line("Mother Ondine Halden", "Odile wants the names published. Do you know how many that would kill at dawn?"),
        line("Mother Ondine Halden", "Sister Vossel says you un-break things. That's also how the world ends.")],
      cond: [{ flag: "L03.dealAccepted", lines: [line("Mother Ondine Halden", "You chose well. You'll wake tomorrow, and nobody you love will be gone.")] },
        { flag: "evidence.record", lines: [line("Mother Ondine Halden", "Keep your paper, then. Paper burns, Arin. We'll meet again in the water.")] }],
      changedFlag: "L03.ondineDone", changedRoute: [[-2, 20, 2, "idle"], [0, 4, 3, "look"], [0, -6, 999, "idle"]], changedTalk: [line("Mother Ondine Halden", "We're done here. The Hall will keep what it keeps.")] });
    ond.mask = 0; ond.it.mask = 0;
    L.when(["L03.payoff"], () => setTimeout(() => L.set("L03.ondineArrives"), 7000));
    const chase = () => L.set("L03.chase");
    L.when(["L03.ondineArrives"], async () => {
      ond.mask = 2; ond.it.mask = 2; L.sfx("alert"); L.toast("Footsteps on the wet stone. Lanterns at the basin.", 3);
      await L.dialogue({ speaker: "Mother Ondine Halden", question: "Mother Ondine holds out her hand for the record.",
        opening: [line("Mother Ondine Halden", "So you're the anomaly Sister Vossel keeps losing on the festival street."), line("Mother Ondine Halden", "My father signed lists to keep this city alive. I keep the night repeating for the same reason."), line("Mother Ondine Halden", "Give me that record and the loop will keep you safe. Forever. Nobody inside it dies for good.")],
        options: [
          { text: "Hand over the record.", sets: "L03.dealAccepted", lines: [line("Mother Ondine Halden", "Wise. Sleep well, Arin. You'll wake up here again."), line("Arin", "...I already have. Haven't I.")], action: () => L.set("L03.hatchOpen") },
          { text: "Give her Odile's forged copy instead.", requires: ["L03.forgedCopy"], missing: "(you have nothing to swap)", sets: "L03.decoyGiven", lines: [line("Mother Ondine Halden", "Good. Sister - burn it."), line("Arin", "Odile's forgery. Walk to the hatch. Don't run. Not yet.")],
            action: () => { L.set("evidence.record"); L.set("L03.hatchOpen"); setTimeout(() => { if (!L.get("L03.exit")) { L.toast("Mother Ondine: 'This is a forgery! After them!'", 3.5); chase(); } }, 9000); } },
          { text: "Refuse.", sets: "L03.refused", lines: [line("Mother Ondine Halden", "Then the Hall keeps you instead. Seal the hatch! Open the cisterns!"), line("Arin", "The hatch release - Odile's office. Through the east door. Go!")], action: () => { L.set("evidence.record"); chase(); } }],
        doneFlag: "L03.ondineDone" });
    });
    L.when(["L03.hatchReleased"], () => L.set("L03.hatchOpen"));
    const seal = dyn(S, false);
    L.box(seal, 3.4, 0.12, 0.15, "rust", 0, 0.15, 42.25, { solid: false, ry: 0.6 }); L.box(seal, 3.4, 0.12, 0.15, "rust", 0, 0.15, 42.25, { solid: false, ry: -0.6 });
    L.label(seal, "SEALED BY THE KEEPERS\nrelease: Restricted Stacks", 0, 0.6, 39.6, Math.PI, 0.22, "#ff8070");
    L.when(["L03.refused"], () => { if (!L.get("L03.hatchReleased")) { seal.visible = true; L.sfx("denied"); } });
    L.when(["L03.hatchReleased"], () => { seal.visible = false; });
    L.pursuer({ name: "Keeper Enforcer", group: S, at: [-4, 33], lane: [[-1, 34]], startFlag: "L03.chase", stopFlag: "L03.exit", speed: 4.5, delay: 2,
      barks: ["Mother wants the paper!", "You can't outrun the hour!"], caught: "The enforcer caught you. Sprint, and use the shelves to break the line." });
    const flood = dyn(S, false);
    const fw = L.box(flood, 36, 0.04, 26, "water", 4, -0.06, 25, { solid: false });
    let floodT = 0;
    L.when(["L03.chase"], () => { flood.visible = true; L.sfx("water"); L.toast("The cisterns are open - the stacks are flooding!", 3.5); });
    L.update(dt => { if (flood.visible && floodT < 1) { floodT = Math.min(1, floodT + dt / 25); fw.position.y = -0.06 + 0.3 * floodT; } });
    [[9, 37.5, 0], [14.5, 38.8, 1.3], [4.5, 40.5, 0.7]].forEach(([x, z, off]) => {
      const vis = L.box(flood, 2.6, 0.5, 0.5, "woodDark", x, 3.5, z, { solid: false, ry: 0.5 });
      L.hazard({ group: S, at: [x, 0, z], size: [2.2, 2.2, 2.2], idle: 2, warn: 1.2, active: 1, offset: off, requires: ["L03.chase"], visual: vis, drop: 3, color: 0xff9040, msg: "A shelf came down. Watch for the creak and the warning glow, then run." });
    });

    // ================================================================ atmosphere
    for (let i = 0; i < 3; i++) { const z = 8 + i * 14; L.cyl(PA, 1.2, 0.08, "brass", 0, 9, z); L.light(PA, 0xffc488, 2.8, 22, 0, 8.7, z); for (let k = 0; k < 6; k++) L.sphere(PA, 0.09, "warm", Math.cos(k) * 1.2, 9.15, z + Math.sin(k) * 1.2); }
    L.particles(PA, "dust", [30, 9, 40], [0, 1, 22], 300);
    for (let i = 0; i < 4; i++) L.box(PA, 0.05, 3.4, 1.6, i % 2 ? "gold" : "red", i % 2 ? 17.6 : -17.6, 9, 10 + Math.floor(i / 2) * 20, { solid: false });
    L.ambient("crowd", 0.18, 1);
    [[-4, 9], [3, 19], [8, 29]].forEach(([x, z]) => { L.particles(PR, "rain", [3, 13, 1.6], [x, 0.5, z], 220); L.box(PR, 3.6, 0.02, 2.4, "puddle", x, 0.025, z, { solid: false }); });
    L.light(PR, 0xa8c8ff, 1.8, 26, 0, 10, 14); L.light(PR, 0xa8c8ff, 1.6, 24, 0, 10, 34);
    L.lamp(PR, -17, 4, "sodium", 3.4); L.lamp(PR, 17, 36, "sodium", 3.4);
    L.ambient("rain", 0.4, 2); L.ambient("water", 0.15, 2); L.ambient("fire", 0.1, 2);
    L.particles(FU, "motes", [34, 8, 46], [0, 0.5, 24], 400);
    L.light(FU, 0x40d8ff, 2.2, 22, -14, 4, 24); L.light(FU, 0xff40c8, 2.2, 22, 14, 4, 24); L.light(FU, 0x9a60ff, 1.8, 30, 0, 12, 24);
    L.holoMesh(FU, "torus", "cyan", 0, 9, 20, 10, 10, 1, { spin: 0.4 }).rotation.x = Math.PI / 2;
    for (let i = 0; i < 6; i++) L.holoMesh(FU, "box", i % 2 ? "amber" : "cyan", -8 + (i % 3) * 8, 3 + (i % 2) * 2.5, 12 + (i % 3) * 8, 0.6, 0.4, 0.02, { bob: 0.15, spin: 0.5 + i * 0.1 });
    L.label(FU, "RECORD 0417 : CONFLICT UNRESOLVED", 0, 4.2, 47.55, Math.PI, 0.4, "#ff80e0", "rgba(20,4,20,.85)");
    L.ambient("drone", 0.3, 4); L.ambient("wind", 0.15, 4);

    // ================================================================ player, checkpoints, fragments
    L.player(0, 0, -8, Math.PI);
    L.checkpoint(0, 0, -8, Math.PI, { size: [5, 2.5, 3] });
    L.checkpoint(0, 0, 3, Math.PI, { size: [5, 2.5, 2] });
    L.checkpoint(0, 0, 29, Math.PI, { size: [6, 2.5, 2] });
    L.fragment("L03.1", [25.6, 1.3, 35.2], "The stamp clerk", "PAST. Tobin, stamping: 'Two hundred and fourteen child fares, all on the Founders' account. I asked Mistress Fenwick if there was a school trip. She took the box away and locked her door.'", S);
    L.fragment("L03.2", [-16.4, 1.2, 2.2], "A mother in the queue", "PAST. Tamsin Oskel, holding a small hand: 'They say the Nursery is just for the festival week. Maren, don't hum in the Hall. The lady with the cards is kind, but she listens to everything.'", S);
    L.fragment("L03.3", [16.6, 1.2, 46.6], "The night of the second bell", "PRESENT. Bram, forty years ago: 'The bell rang twice and the archive shook. In the morning the Register of the Dead had grown by a hundred children. Not one family came to claim a grave.'", S);
  },
});
