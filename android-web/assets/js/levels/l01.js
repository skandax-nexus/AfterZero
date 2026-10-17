/* LEVEL 01 - The Bell That Rings Twice (Veyra Arrival Quarter).
   Port of the Unity Level01Builder (Design v2): 30 s recurrence, first Past shift, Sister Vossel (watcher)
   tightening the loop, and the Perfect Loop climax (cart parked in the PAST + Lise stopped + Pell diverted,
   all in one loop). Street runs along +z: terrace z -16..-6, street to the festival gate at z 48, plaza, tower. */
AZ.registerLevel({
  id: "L01", order: 1, chapter: "LEVEL 01", title: "The Bell That Rings Twice", location: "Veyra Arrival Quarter",
  start: 1, allowed: 2,
  build(L) {
    const { line, THREE } = L, S = L.shared, PA = L.past, PR = L.present;
    const F = {
      posted: "loop.L01.letterPosted", rolled: "loop.L01.cartRolled", chocked: "loop.L01.cartChocked",
      liseStopped: "loop.L01.liseStopped", liseRan: "loop.L01.liseRan", pell: "loop.L01.pellDiverted", perfect: "loop.L01.perfectLoop",
      letter: "L01.letterFound", crash: "L01.crashSeen", vendor: "L01.vendorTalked", observed: "L01.routinesObserved",
      cartMoved: "L01.cartMovedPast", liseSeen: "L01.liseRanSeen", saved: "L01.cupSaved", inspected: "L01.streetInspected",
      truth: "L01.letterTruth", entered: "L01.enteredClockworks", vossel: "L01.vosselScene",
    };
    const CLUE_TRUTH = "clue.L01_letterTruth";
    const dynGroup = (parent) => { const g = new THREE.Group(); g.userData.dynamic = true; parent.add(g); return g; };
    const showGroup = (g, on) => { g.visible = on; g.traverse(o => { if (o.userData.collider) o.userData.collider.on = on; }); };

    L.look({ fog: 0.8, past: { density: 0.009 }, present: { density: 0.022 } });

    // ---------------- story ----------------
    L.intro(
      line("Arin", "Veyra. Bunting in the rain, music under the awnings... and that bell."),
      line("Arin", "The letter from the train said it plainly: don't trust the first bell. Save the blue cup."),
      line("The Witness", "Watch the street, Arin. Everything here comes back every thirty seconds. Nearly everything."),
      line("Arin", "A courier with a letter. A cup vendor. A cart on the slope. And a woman with a lantern, watching. Let me watch first."));
    L.objective("Follow the courier and read the letter he posts", F.letter, "Pell walks from the terrace stairs to the postbox on the left. Wait for him there.");
    L.objective("Watch the street's routine and talk to the cup vendor", F.observed, "Talk to Mirela at the blue-cup stall, and watch what happens when the bell rings twice.");
    L.objective("Shift to the PAST and push the festival cart into the bay", F.cartMoved, "Tap << PAST. Climb the side lane on the right and push the cart into the CART BAY. Stay out of Sister Vossel's lantern when you shift.");
    L.objective("The cup still breaks. Watch the stall again: what else hits it?", F.liseSeen, "Stay in the PRESENT near the stall around 0:14 of the loop. Watch the lane mouth.");
    L.objective("THE PERFECT LOOP: park the cart, stop Lise, divert Pell - all in one loop", F.saved, "Talk to Pell early so he takes the long way. Push the cart into the bay in the PAST. Be back at the lane mouth before 0:14 and talk to Lise or stand in her way. Every time Vossel sees you, the loop gets shorter.");
    L.objective("Inspect the changed street beyond the festival gate", F.inspected, "The festival gate at the end of the street has opened. Walk into the tower plaza.");
    L.objective("Read the letter at the tower door", F.truth, "A letter waits on a lectern by the clock-tower door.");
    L.objective("Enter the clockworks", F.entered, "Walk through the open tower door.");
    L.payoff(F.truth, "Dated Tomorrow", "The letter is dated tomorrow and pressed with the clock-tower seal (0). The handwriting is Arin's own - older, shakier - with the same long-tailed A as the engraving in the stopped watch: 'For A. - keep time for me. I.M.' Arin wrote it, from a tomorrow Arin cannot remember.");

    L.player(0, 3, -13, Math.PI);
    L.checkpoint(0, 3, -13, Math.PI, { size: [6, 2.5, 5] });

    // ---------------- terrace (safe observation area) ----------------
    L.box(S, 14, 3, 10, "stone", 0, 1.5, -11);
    L.box(PR, 13.6, 0.02, 9.6, "puddle", 0, 3.01, -11, { solid: false });
    L.stairs(S, [0, 3, -6], [0, 0, -1.5], 3, "stone");
    // terrace railings: same blocking volumes as before, drawn as wrought-iron balusters under a brass handrail
    const railing = (x, z, w, d) => {
      L.block(x, z, w, d, 3, 4);
      const along = w > d, len = Math.max(w, d), n = Math.round(len / 0.24);
      L.box(S, along ? len : 0.09, 0.07, along ? 0.09 : len, "brass", x, 3.97, z, { solid: false });
      L.box(S, along ? len : 0.05, 0.05, along ? 0.05 : len, "iron", x, 3.12, z, { solid: false });
      for (let i = 0; i <= n; i++) { const t = -len / 2 + (i * len) / n; L.cyl(S, 0.018, 0.86, "iron", along ? x + t : x, 3.5, along ? z : z + t, { seg: 8 }); }
    };
    for (const x of [-4.25, 4.25]) railing(x, -6.2, 5.5, 0.15);
    for (const x of [-6.9, 6.9]) railing(x, -11, 0.15, 10);
    L.box(S, 18, 14, 2, "stone", 0, 7, -17);
    L.box(PA, 5, 3, 0.1, "window", 0, 4.5, -15.95, { solid: false });
    L.box(PR, 5, 3, 0.1, "windowCold", 0, 4.5, -15.95, { solid: false });
    L.label(S, "VEYRA ARRIVALS", 0, 8, -15.9, 0, 0.8, "#cfeeff", "rgba(8,14,22,.85)");
    L.box(S, 2, 0.5, 0.6, "wood", -4.5, 3.25, -9); L.box(S, 2, 0.5, 0.6, "wood", 4.5, 3.25, -9);
    L.label(S, "WATCH THE STREET", 2.2, 4.4, -7.2, Math.PI, 0.22, "#ffffff");
    L.lamp(PA, -6.3, -7, "gas", 3.6, 3); L.lamp(PR, -6.3, -7, "sodium", 3.6, 3);

    // ---------------- street ----------------
    L.floor(S, 0, 22, 16, 56, 0, "stone");
    L.box(PR, 16, 0.02, 56, "stoneWet", 0, 0.012, 22, { solid: false });
    for (let i = 0; i < 8; i++) L.box(PR, 1.5 + (i % 3), 0.02, 1 + (i % 2), "puddle", -5 + (i * 3.7) % 10, 0.03, -3 + i * 6.2, { solid: false });
    L.box(PA, 2.2, 0.02, 52, "red", 0, 0.02, 21, { solid: false });
    const facade = (x0, side, z0, z1, h, seed, sign) => {
      const cx = x0 + side * 2.5, len = z1 - z0, cz = (z0 + z1) / 2;
      L.box(PA, 5, h, len, seed % 2 ? "plaster" : "brick", cx, h / 2, cz);
      L.box(PR, 5, h, len, seed % 2 ? "plasterCold" : "stoneWet", cx, h / 2, cz);
      let s = seed * 7 + 3; const rnd = () => ((s = (s * 9301 + 49297) % 233280) / 233280);
      const floors = Math.max(1, Math.floor((h - 4) / 3)), cols = Math.max(1, Math.floor(len / 2.8));
      for (let f = 0; f < floors; f++) for (let c = 0; c < cols; c++) {
        const r = rnd(); if (r < 0.35) continue;
        const z = z0 + (c + 0.5) * len / cols, y = 5 + f * 3, wx = x0 - side * 0.04;
        L.box(PA, 0.08, 1.4, 1.1, "window", wx, y, z, { solid: false, noEcho: true, shadow: false });
        L.box(PR, 0.08, 1.4, 1.1, r < 0.6 ? "windowCold" : "window", wx, y, z, { solid: false, noEcho: true, shadow: false });
      }
      L.box(PA, 1.6, 0.08, Math.min(5, len - 1), seed % 3 ? "red" : "teal", x0 - side * 0.8, 2.9, cz, { solid: false });
      L.box(PR, 1.6, 0.08, Math.min(5, len - 1), "rust", x0 - side * 0.8, 2.9, cz, { solid: false });
      if (sign) L.label(S, sign, x0 - side * 0.12, 3.8, cz, side < 0 ? Math.PI / 2 : -Math.PI / 2, 0.38, "#ffe0a8", "rgba(20,12,6,.85)");
    };
    facade(-8, -1, -16, -6, 13, 1); facade(-8, -1, -6, 3, 11, 2, "BAKERY"); facade(-8, -1, 3, 11, 14, 3); facade(-8, -1, 11, 18, 10, 4, "POST");
    facade(-8, -1, 18, 27, 12, 5, "BLUE CUPS"); facade(-8, -1, 27, 35, 15, 6); facade(-8, -1, 35, 42, 11, 7, "LANTERNS"); facade(-8, -1, 42, 47.5, 16, 8);
    facade(8, 1, -16, -6, 12, 11); facade(8, 1, -6, 3, 14, 12, "TEA HOUSE"); facade(8, 1, 3, 10, 10, 13); facade(8, 1, 10, 17, 13, 14, "CARTWRIGHT");
    facade(8, 1, 28, 35, 12, 15); facade(8, 1, 35, 42, 15, 16, "MUSIC"); facade(8, 1, 42, 47.5, 11, 17);
    for (const z of [2, 15, 34]) for (const x of [-6.9, 6.9]) { L.lamp(PA, x, z, "gas"); L.lamp(PR, x, z, "sodium"); }
    for (const z of [2, 10, 30, 38, 45]) L.bunting(PA, -8, 8, z, 6.5);
    for (const z of [6, 26]) L.box(PR, 16, 0.04, 0.04, "iron", 0, 6.4, z, { solid: false });
    // market stalls
    const stall = (x, z, canopy, goods) => {
      L.box(S, 1.2, 0.9, 2.8, "wood", x, 0.45, z);
      L.box(PA, 1.6, 0.08, 3.2, canopy, x, 2.55, z, { solid: false }); L.box(PR, 1.6, 0.08, 3.2, "teal", x, 2.55, z, { solid: false });
      for (const dz of [-1.35, 1.35]) L.box(S, 0.08, 2.5, 0.08, "woodDark", x + 0.55, 1.25, z + dz, { solid: false });
      L.label(S, goods, x, 2.2, z - 1.62, Math.PI, 0.25, "#ffffff");
    };
    stall(5.6, 4, "gold", "Bread"); stall(5.6, 34, "red", "Ribbons"); stall(-5.6, 38, "teal", "Masks");
    L.box(PA, 0.6, 1.3, 0.6, "red", -7.3, 0.65, 12); L.box(PR, 0.6, 1.3, 0.6, "rust", -7.3, 0.65, 12);
    L.label(S, "POST", -6.95, 0.95, 12, Math.PI / 2, 0.2, "#ffffff");
    L.particles(PR, "rain", [26, 16, 64], [0, 0, 22], 1300);
    L.particles(PA, "dust", [14, 5, 40], [0, 2, 22], 400);
    L.ambient("rain", 0.3, 2); L.ambient("crowd", 0.32, 1); L.ambient("crowd", 0.12, 2); L.ambient("wind", 0.08, 7);
    // skyline beyond the facades
    for (let i = 0; i < 3; i++) { L.building(S, -24, -6 + i * 26, 9, 22 + i * 6, 10, "plasterCold", "window", 30 + i); L.building(S, 38, -6 + i * 26, 9, 18 + i * 8, 10, "stoneWet", "windowCold", 60 + i, i === 1 ? "THEATRE" : null); }

    // ---------------- side lane, cart landing, bay ----------------
    L.ramp(S, [8, 0, 22.5], [14, 2.5, 22.5], 11, "stone");
    L.box(S, 6, 2.5, 11, "stone", 17, 1.25, 22.5);
    L.box(S, 7, 8, 0.5, "brick", 16.5, 4, 16.75);
    L.box(S, 0.5, 8, 1.5, "brick", 20.25, 4, 17.25); L.box(S, 0.5, 8, 8.5, "brick", 20.25, 4, 24.25); L.box(S, 0.5, 2.9, 2, "brick", 20.25, 6.55, 19);
    L.box(S, 2.5, 8, 0.5, "brick", 14.25, 4, 28.25); L.box(S, 1.7, 8, 0.5, "brick", 19.35, 4, 28.25); L.box(S, 3, 2.8, 0.5, "brick", 17, 6.6, 28.25);
    L.box(S, 3, 2.5, 3.3, "stone", 17, 1.25, 29.9);
    L.box(S, 0.4, 8, 3.3, "brick", 15.3, 4, 29.9); L.box(S, 0.4, 8, 3.3, "brick", 18.7, 4, 29.9); L.box(S, 3.8, 8, 0.4, "brick", 17, 4, 31.7);
    L.label(S, "CART BAY", 17, 5.6, 28.0, Math.PI, 0.45, "#ffffff", "rgba(30,20,10,.85)");
    L.label(PA, "STEEP SLOPE - CHOCK ALL CARTS", 14.3, 3.6, 17.1, 0, 0.22, "#ffd8a0");
    L.lamp(PA, 19.6, 18, "gas", 3.4, 2.5); L.lamp(PR, 19.6, 18, "sodium", 3.4, 2.5);
    L.light(PR, 0x9ec0ff, 1.2, 6, 17, 4.5, 30);

    const cartMesh = (g, mat, canopy) => {
      const grp = dynGroup(g);
      L.box(grp, 2.4, 1.1, 1.6, mat, 0, 0.62, 0, { solid: false });
      for (const dx of [-0.8, 0.8]) for (const dz of [-0.86, 0.86]) { const w = L.cyl(grp, 0.35, 0.1, "woodDark", dx, 0.35, dz); w.rotation.x = Math.PI / 2; }
      L.box(grp, 2.2, 0.08, 1.45, canopy, 0, 1.95, 0, { solid: false });
      for (const dx of [-0.95, 0.95]) L.box(grp, 0.06, 0.8, 0.06, "brass", dx, 1.55, 0, { solid: false });
      return grp;
    };
    // PAST cart: push it into the bay (resets each loop).
    const pastCart = cartMesh(PA, "wood", "gold"); pastCart.position.set(17, 2.5, 22.5);
    const pastCartCol = L.block(17, 22.5, 2.4, 1.6, 2.5, 3.6, 1);
    L.label(PA, "PARADE CART - PUSH", 17, 4.5, 22.5, Math.PI, 0.22, "#fff0cc");
    let pushT = -1;
    const pushIt = L.interact({ at: [17, 3.2, 21.6], mesh: pastCart, mask: 1, radius: 3, prompt: "Push the cart into the bay", onUse: it => {
      if (AZ.get(F.chocked)) return;
      pushT = 0; it.enabled = false; L.sfxAt("scrape", it.pos);
    } });
    L.update(dt => {
      if (pushT < 0) return;
      pushT = Math.min(1, pushT + dt / 1.6);
      const e = pushT * pushT * (3 - 2 * pushT);
      pastCart.position.set(17, 2.5, 22.5 + 7.2 * e); pastCartCol.z = pastCart.position.z;
      if (pushT >= 1) { pushT = -1; L.set(F.chocked); L.toast("The cart rests in the bay - for this loop. In the PRESENT it will not reach the slope.", 3.5); }
    });
    // PRESENT cart: rolls into the stall at the second bell unless parked this loop.
    const presCart = cartMesh(PR, "rust", "teal"); presCart.position.set(17, 2.5, 22.5);
    L.label(presCart, "NO BRAKE", 0, 2.4, 0, Math.PI, 0.2, "#ff9a70");
    const presParked = cartMesh(PR, "rust", "teal"); presParked.position.set(17, 2.5, 29.7); presParked.rotation.y = Math.PI / 2; presParked.visible = false;
    let rollT = -1;
    L.update(dt => {
      if (rollT < 0) return;
      rollT = Math.min(1, rollT + dt / 1.1);
      const e = rollT * rollT;
      presCart.position.set(17 - 20.2 * e, 2.5 - 2.5 * Math.min(1, e * 1.4), 22.5); presCart.rotation.y = -0.3 * e;
      if (rollT >= 1) { rollT = -1; L.sfxAt("clang", presCart.position, 1); }
    });
    L.onFlag(F.chocked, v => { presParked.visible = v; presCart.visible = !v; if (v) { L.sfx("rule"); L.set(F.cartMoved); } });

    // ---------------- blue cup stall ----------------
    L.box(S, 1.2, 0.9, 3.2, "wood", -5, 0.45, 22.5);
    L.box(PA, 1.7, 0.08, 3.5, "teal", -5, 2.65, 22.5, { solid: false }); L.box(PR, 1.7, 0.08, 3.5, "red", -5, 2.65, 22.5, { solid: false });
    L.label(S, "MIRELA'S BLUE CUPS", -4.35, 2.25, 22.5, Math.PI / 2, 0.25, "#ffffff", "rgba(10,20,40,.8)");
    L.light(PA, 0xffc27a, 1.4, 5, -5, 2.3, 22.5); L.light(PR, 0xff9a48, 1.8, 6, -5, 2.3, 22.5);
    for (let i = 0; i < 6; i++) L.cyl(PA, 0.07, 0.13, "cup", -5.1 + (i % 2) * 0.3, 0.97, 21.4 + i * 0.42);
    const cupWhole = dynGroup(PR); L.cyl(cupWhole, 0.1, 0.16, "cup", -4.75, 0.98, 22.2); L.label(cupWhole, "whole", -4.7, 1.35, 22.2, Math.PI / 2, 0.18, "#fff0c0"); cupWhole.visible = false;
    const shards = dynGroup(PR);
    for (let i = 0; i < 6; i++) L.box(shards, 0.08, 0.025, 0.06, "cup", i < 3 ? -4.8 + i * 0.12 : -3.9 + i * 0.25, i < 3 ? 0.915 : 0.02, 22 + (i % 3) * 0.3, { solid: false, ry: i });
    L.label(shards, "broken", -4.7, 1.25, 22.2, Math.PI / 2, 0.18, "#cfe0ff");
    L.item({ group: PR, key: "cup_shard", name: "blue cup shard", at: [-3.6, 0.06, 21.2], size: 0.4, mat: "cup", resetOnLoop: true });

    // ---------------- the letter ----------------
    const letterG = dynGroup(PR);
    const letter = L.clue({ group: letterG, at: [-7.3, 1.36, 12], size: [0.45, 0.05, 0.34], prompt: "Read the posted letter", flag: F.letter, id: "L01_letter", title: "A Rain-Smeared Letter",
      text: "Addressed to: ARIN, at the Arrival Quarter.\n\nThe ink has run in the rain. Fragments remain:\n\n'...you will arrive with the festival... the bell will ring twice... look for the blue cup, it is the one thing the loop cannot mend... go in through the clockworks...'\n\nThe date and the wax seal are hidden under a thumbprint of mud. In a different street, it might read differently." });
    letterG.visible = false; letter.enabled = false;
    L.onFlag(F.posted, v => { letterG.visible = v; letter.enabled = v; if (v) L.sfxAt("place", letter.pos, 0.6); });

    // ---------------- gate, plaza, tower ----------------
    L.box(S, 5, 6, 1, "stone", -5.5, 3, 48); L.box(S, 5, 6, 1, "stone", 5.5, 3, 48); L.box(S, 6, 1.2, 1, "stone", 0, 5.4, 48);
    L.label(S, "FESTIVAL GATE", 0, 5.4, 47.45, Math.PI, 0.5, "#ffffff");
    L.label(PR, "CLOSED UNTIL THE BELL IS QUIET", 0, 3.9, 47.6, Math.PI, 0.22, "#ffd8a0");
    L.door(S, 6, 4.8, 0.3, "rust", 0, 0, 48, [F.saved], [0, 4.4, 0], { toast: "The festival gate grinds open." });
    L.floor(S, 0, 57, 30, 18, 0, "stone");
    L.box(PR, 30, 0.02, 18, "stoneWet", 0, 0.012, 57, { solid: false });
    L.label(S, "0", 0, 0.05, 55, 0, 3, "#e8f4ff").rotation.x = -Math.PI / 2;
    L.box(S, 6, 12, 16, "plasterCold", -12, 6, 57); L.box(S, 6, 12, 16, "brick", 12, 6, 57);
    L.box(S, 7, 12, 1, "plasterCold", -11.5, 6, 48); L.box(S, 7, 12, 1, "brick", 11.5, 6, 48);
    L.box(S, 3, 12, 8, "plasterCold", -8.5, 6, 68); L.box(S, 3, 12, 8, "brick", 8.5, 6, 68);
    L.box(S, 14, 44, 5, "stone", 0, 22, 69.5);
    L.box(S, 5.8, 44, 1, "stone", -4.1, 22, 62.5); L.box(S, 5.8, 44, 1, "stone", 4.1, 22, 62.5); L.box(S, 2.4, 40.6, 1, "stone", 0, 23.7, 62.5);
    L.box(S, 5, 44, 4, "stone", -4.5, 22, 65); L.box(S, 5, 44, 4, "stone", 4.5, 22, 65); L.box(S, 4, 40, 4, "stone", 0, 24, 65);
    L.floor(S, 0, 65, 4, 4, 0, "brass");
    L.light(S, 0xffb060, 2, 5, 0, 2.6, 65.5);
    L.door(S, 2.4, 3.4, 0.3, "woodDark", 0, 0, 62.3, [F.truth], [2.5, 0, 0], { toast: "The seal (0) on the tower door unlocks." });
    L.label(S, "(0)", 0, 2.2, 62.1, Math.PI, 0.9, "#ffd890");
    L.label(S, "FOUNDERS' CLOCKWORKS", 0, 3.9, 61.95, Math.PI, 0.28, "#ffffff");
    const face = L.cyl(S, 4, 0.2, "paper", 0, 32, 61.9); face.rotation.x = Math.PI / 2;
    const glowFace = L.cyl(PR, 3.7, 0.06, "cold", 0, 32, 61.75); glowFace.rotation.x = Math.PI / 2;
    L.label(S, "0", 0, 35, 61.6, Math.PI, 1.6, "#202024");
    L.box(S, 0.25, 2.4, 0.08, "iron", 0, 33.2, 61.55, { solid: false }); L.box(S, 3.2, 0.18, 0.08, "iron", 1.6, 32, 61.5, { solid: false });
    L.box(PA, 5, 3, 0.1, "window", 0, 39.5, 61.95, { solid: false }); L.box(PR, 5, 3, 0.1, "windowCold", 0, 39.5, 61.95, { solid: false });
    L.light(S, 0xffc080, 3, 12, 0, 39, 60.5);
    L.spot(PR, 0x9ec0ff, 3, 40, 0, 1, 54, 0, 30, 62);
    L.lamp(PA, -7, 58, "gas", 4.5); L.lamp(PR, -7, 58, "sodium", 4.5);
    L.ambient("machine", 0.2, 7);

    // ---------------- the changed street (after the cup is saved) ----------------
    const changed = dynGroup(PR);
    [6, 14, 22, 32, 42, 52, 57].forEach((z, i) => {
      const half = z > 48 ? 11 : 7.5;
      L.box(changed, half * 2, 0.03, 0.03, "iron", 0, 6.6, z, { solid: false });
      for (let k = 0; k < 7; k++) { const x = -half + (k + 0.5) * (half * 2 / 7); L.mesh(changed, "sphere", k % 2 ? "neonA" : "warm", x, 6.25 - 0.4 * (1 - (x / half) * (x / half)), z, 0.34, 0.34, 0.34); }
      if (i % 3 === 0) L.light(changed, 0xffc27a, 2.4, 11, 0, 5.8, z);
      if (i < 5) for (let k = 0; k < 3; k++) L.box(changed, 0.8, 1.6, 0.03, k === 1 ? "gold" : "red", -5 + k * 5, 4.9, z + 0.1, { solid: false });
    });
    L.label(changed, "WELCOME TO THE FESTIVAL OF THE ZERO", 0, 7.6, 47.4, Math.PI, 0.32, "#ffe0a0");
    L.lamp(changed, 7, 58, "gas", 4.5);
    changed.visible = false;
    const stepG = dynGroup(PR);
    L.box(stepG, 0.6, 1, 0.5, "woodDark", -1.8, 0.5, 60.8, { solid: false });
    L.light(stepG, 0xffd890, 2, 5, -1.8, 1.6, 60.5);
    const truth = L.clue({ group: stepG, at: [-1.8, 1.03, 60.8], size: [0.45, 0.05, 0.34], prompt: "Read Arin's letter", flag: F.truth, id: "L01_letterTruth", title: "Arin's Letter", requires: [F.inspected], locked: "Look around the changed plaza first.",
      text: "To Arin, at the Arrival Quarter.\n\nYou saved the blue cup. Good. Now you know the rule: what you change, stays changed.\n\nThe first bell is a warning. The second is the end, and the beginning again. The tower was stopped once, from inside. Go in through the clockworks and find out who stopped it.\n\nYou have done this before. You will not remember. I do.\n\n   - (0)\n\nDATED: tomorrow - the day after the festival.\nSEALED: in wax, with the clock-tower seal (0).\n\nThe handwriting is yours - older, shakier, but yours. The long tail of the A is the same as the engraving inside your stopped watch:\n'For A. - keep time for me. I.M.'",
      lines: [line("Arin", "This is my hand. Older. Shaking. The same A as the one on the watch."), line("The Witness", "You wrote to yourself, Arin. From a tomorrow you haven't lived yet.")] });
    L.label(stepG, "LETTER - SEALED (0)", -1.8, 1.7, 60.6, Math.PI, 0.18, "#fff0c0");
    stepG.visible = false; truth.enabled = false;
    const rain = W => W; // (rain is a Present particle system; hidden when the rain stops)
    L.when([F.saved], () => {
      cupWhole.visible = true; shards.visible = false; changed.visible = true; stepG.visible = true; truth.enabled = true;
      L.root.traverse(o => { if (o.isPoints && o.parent === PR) o.visible = false; });
      L.sfx("solved"); L.toast("The loop is broken. The rain stops - the street is changing.", 4.5);
      setTimeout(() => L.say([line("The Witness", "There. One thing kept. The night can be taught, Arin."), line("Arin", "Who are you? How do you know my name?"), line("The Witness", "Someone who has read all your letters. Go and see what the street becomes when nobody waits for the bell.")]), 2500);
    });
    void rain;

    // ---------------- NPCs ----------------
    L.npc({ name: "Mirela Dovan", group: PR, at: [-6.5, 22.5], yaw: Math.PI / 2, coat: "burgundy", task: "Sells blue glazed cups at the festival stall - and sweeps up the same broken blue cup every loop.",
      route: [[-6.5, 22.5, 6, "work"], [-6.6, 20.6, 4, "look"], [-6.4, 24.4, 5, "work"]],
      approach: ["Careful - glass and cups.", "Buying, or just watching the rain?", "Stand clear of the lane when the bell goes."],
      talk: [line("Mirela", "That cup? Broken. It's always broken."), line("Mirela", "The bell rings twice, the cart comes down the lane, my table jumps... and then it's raining again and my hands are dry."),
        line("Mirela", "Everything comes back. The music, the crowd, the courier and his letter. Never the cup."),
        line("Mirela", "It's never just the cart. Little Lise comes tearing across at the same moment, and Pell stands in the lane like a post."),
        line("Mirela", "My grandfather built that parade cart, years ago. He always said the slope would be the death of something."),
        line("The Witness", "The cart was set rolling long before tonight, Arin. Some causes are older than the loop.")],
      cond: [{ flag: CLUE_TRUTH, lines: [line("Mirela", "Dated tomorrow? There hasn't been a tomorrow in this street for as long as I can remember.")] },
        { holding: "cup_shard", lines: [line("Mirela", "Careful, that edge bites. That's my blue cup. It's warm - like it breaks every moment.")] },
        { holding: "missing_poster", lines: [line("Mirela", "The Oskels. Maren used to buy a cup every festival and drop it before the corner. Who keeps pinning that up?")] }],
      talkedFlag: F.vendor, changedFlag: F.saved, changedRoute: [[-6.5, 22.5, 6, "work"], [-3.2, 26, 5, "talk"], [-6.4, 21, 5, "carry"]],
      changedTalk: [line("Mirela", "You did something, didn't you? The cart never came. The cup is whole... and the rain stopped."), line("Mirela", "The plaza gate is open. Osric is lighting the lamps by the tower. Go and look.")] });

    const pellRoute = [[2.5, 6, 1.5, "carry"], [-2, 10, 0.5, "carry"], [-6.6, 12, 6, "work"], [1, 21, 30, "look"]];
    const pellDivert = [[-6.8, 4, 0.5, "carry"], [-7, 9, 0.5, "carry"], [-6.6, 12, 6, "work"], [-7, 16, 30, "look"]];
    const pell = L.npc({ name: "Pell Ashgrove", group: PR, at: [1.5, 0.3], coat: "navy", speed: 1.45, task: "Delivers the festival post: every loop he carries the same sealed letter from the arrival stairs to the postbox.",
      route: pellRoute, approach: ["Letter for the postbox - mind the satchel!", "Oh - it's you. Again. I mean... hello.", "Walk with me. The postbox is on the left."],
      talk: [line("Pell", "Post for the festival! Well - one letter. Always the same one."), line("Pell", "It's addressed to an Arin. That's you, isn't it? Read it when I've posted it."),
        line("Pell", "I've carried this letter to you more times than there are days. Don't ask me how I know. My feet know it."),
        line("Pell", "Take the long way, you say? Along the shopfronts, out of the lane? ...Alright. For you. This loop."),
        line("Pell", "If you see my Lise running for Mirela's stall, catch her. She never looks where she's going.")],
      cond: [{ flag: CLUE_TRUTH, lines: [line("Pell", "Dated tomorrow, with the tower's seal? I've been carrying tomorrow's post all this time."), line("Pell", "And the hand... it's yours. Older. Like you wrote it from very far away.")] },
        { holding: "missing_poster", lines: [line("Pell", "The Oskels. Lantern Row, number nine. I knocked once and the door wasn't there - just a wall with a little zero scratched on it.")] }],
      talkedFlag: F.pell, changedFlag: F.pell, changedRoute: pellDivert,
      changedTalk: [line("Pell", "Long way round, as promised. The lane's all yours."), line("Pell", "Funny. I've walked the middle of that street more times than there are days.")] });
    L.when([F.saved], () => {
      pell.o.changedFlag = F.saved; pell.o.changedRoute = [[2, 30, 1, "carry"], [1, 58.4, 2, "carry"], [3, 59.5, 10, "look"], [5, 54, 8, "idle"]];
      pell.o.changedTalk = [line("Pell", "No bell, no going back to the stairs. I don't know what to do with the rest of the night."), line("Pell", "I took the letter up to the tower door. If I see you again tomorrow - a real tomorrow - I'll say 'welcome back'. And mean it.")];
    });
    let postFired = false;
    L.update(() => { if (!AZ.get(F.posted) && W().loop && (Math.hypot(pell.pos.x + 6.6, pell.pos.z - 12) < 1.6 || W().loop.elapsed > 24 * scale())) L.set(F.posted); });

    const liseHome = [[6.4, 25, 3, "work"], [6.6, 27.2, 3, "look"], [5.8, 23.6, 3, "idle"]];
    const lise = L.npc({ name: "Lise Ashgrove", group: PR, at: [6.4, 25], yaw: -Math.PI / 2, coat: "ochre", scale: 0.72, task: "Pell's nine-year-old: plays hopscotch at the lane mouth - and at 0:14 of every loop sprints for Mirela's blue cups.",
      route: liseHome, approach: ["Hello! Are you new? You look new. Again.", "Don't step on the zero!", "Papa's the postman. The only postman."],
      talk: [line("Lise", "I'm Lise. Lise Ashgrove. I'm nine. I've been nine for ages."), line("Lise", "When the bell rings I run to Mirela's for a blue cup. I always trip on her table. Always."),
        line("Lise", "Papa says I was born on festival night. Every festival night. That's a lot of birthdays and no cake."),
        line("Lise", "Wait for you? Alright. I'll stay right here and count the bunting."),
        line("Lise", "There's a lady with a lantern who walks up and down and writes things in a little book. She doesn't blink.")],
      cond: [{ holding: "cup_shard", lines: [line("Lise", "That's from the cup I break! I'm sorry - my feet just go.")] },
        { holding: "missing_poster", lines: [line("Lise", "Maren! I play with Maren... in dreams, I think. She hums a song with a hole in it."), line("Lise", "You hum it right, though. All four notes.")] }],
      talkedFlag: F.liseStopped, changedFlag: F.saved, changedRoute: [[2, 55, 6, "talk"], [4, 57, 5, "look"], [5, 54.5, 6, "idle"]],
      changedTalk: [line("Lise", "Nobody ran! Nobody tripped! Is it tomorrow now?"), line("Lise", "Papa's taking a letter to the tower. Can you come?")] });

    L.npc({ name: "Osric Vane", group: PR, at: [3.5, 45.6], yaw: Math.PI, coat: "grey", task: "Warden of the festival gate: winds the gate chain and keeps the tower plaza sealed while the bell loops.",
      route: [[4.6, 46, 6, "work"], [-3.5, 45.5, 4, "look"], [0, 44, 5, "idle"]], approach: ["Plaza's closed, friend.", "Gate stays down while the bell rings twice.", "Hands off the winch."],
      talk: [line("Osric", "Plaza's closed. Tower's orders, since before I took the job."), line("Osric", "Every time the bell rings twice, I'm back at this chain with my hands on the crank."),
        line("Osric", "Only the tower seals in wax - a ring with a zero in it. If you see that seal, it's the clockkeeper's business."),
        line("Osric", "The orders came signed by a Founder. Halden. You don't argue with that signature."),
        line("Osric", "First bell's a warning. Second bell's the... well. Nobody's ever finished that sentence for me.")],
      cond: [{ flag: CLUE_TRUTH, lines: [line("Osric", "The (0) seal, in fresh wax. The keeper's been gone longer than I've been warden. Someone's awake in the clockworks.")] },
        { holding: "cup_shard", lines: [line("Osric", "A bit of Mirela's cup. Every night I hear it break from here, clear as the bell.")] }],
      changedFlag: F.saved, changedRoute: [[0, 52, 4, "look"], [-6, 57.5, 6, "work"], [5, 55, 5, "idle"]],
      changedTalk: [line("Osric", "Gate's up! First time in... I can't count it. The bell let go of us."), line("Osric", "Go on. There's something on the tower step.")] });
    L.box(S, 0.8, 1.2, 0.8, "brass", 5.2, 0.6, 46.7);

    L.npc({ name: "Hale Dovan", group: PA, at: [19.4, 21], y: 2.5, yaw: -Math.PI / 2, coat: "ochre", task: "Builds and paints the new parade cart and greases its wheels for the festival.",
      route: [[19.4, 21, 6, "work"], [19.4, 26, 5, "work"], [19.4, 23, 4, "look"]], approach: ["Afternoon! Mind the wet paint.", "Come to see the parade cart?", "Careful on the slope."],
      talk: [line("Hale", "Mind the paint! She's for the parade."), line("Hale", "The brake's a beauty, but that slope is steep. If she ever got loose she'd roll clean across the street."),
        line("Hale", "There's a cart bay in the wall there. Push her in if you like - she rolls easy."),
        line("Hale", "My workshop's through the door behind me. Don't touch the gold."),
        line("Hale", "The Founders want the cart at the station at dawn, not the square. 'Festival specials', they say. Odd place for a parade.")],
      cond: [{ holding: "hale_brush", lines: [line("Hale", "My brush! Gold for the trim, blue for the wheels - blue like the cups my girl wants to sell one day.")] },
        { holding: "cart_chock", lines: [line("Hale", "A wheel chock. I've told myself a hundred times... funny, it does feel like a hundred. The bay is better than any chock.")] }],
      changedFlag: F.cartMoved, changedRoute: [[19.4, 27, 6, "look"], [19.4, 21, 6, "work"], [19.4, 24, 4, "idle"]],
      changedTalk: [line("Hale", "Tucked away in the bay, safe as a sleeping cat. You've a careful hand."), line("Hale", "Funny. I feel like I've just saved somebody a great deal of sweeping.")] });
    L.item({ group: PA, key: "hale_brush", name: "paint brush", at: [15, 2.75, 18.2], size: 0.45, mat: "gold" });

    L.crowd(PA, 0, 20, 11, 40, 8, { barks: ["The bell sounds lovely today."] });
    L.crowd(PR, 0, 24, 10, 36, 7, { barks: ["Again?"] });
    L.crowd(changed, 0, 53, 16, 8, 4);

    // ---------------- Sister Vossel (watcher) + loop escalation ----------------
    const vossel = L.keeper({ name: "Sister Vossel", mode: "watcher", group: PR, at: [0, 6], path: [[0, 6], [2.5, 20], [-2, 34], [1, 14]], speed: 1.1, view: 10, angle: 70, mask: 2, disabledBy: F.saved, pause: 3 });
    L.label(vossel.p.group, "SISTER VOSSEL", 0, 2.25, 0, 0, 0.24, "#ffd0a0");
    let anomalies = 0;
    const scale = () => (W().loop ? W().loop.duration / 30 : 1);
    function W() { return L.W; }
    const anomaly = () => {
      if (AZ.get(F.saved) || AZ.settings.difficulty === 0) return;
      anomalies++;
      L.sfx("alert", 0.6); AZ.vibrate(40);
      AZ.ui.bark("Sister Vossel", ["Anomaly.", "I saw that.", "The hour notices you.", "Noted. Again."][anomalies % 4]);
      L.toast("Sister Vossel noticed you (" + anomalies + "/3).", 2.2);
      const lp = W().loop;
      if (anomalies >= 3 && lp && lp.duration > 18) {
        anomalies = 0;
        lp.duration = Math.max(18, lp.duration - 6);
        const k = lp.duration / 30;
        lp.events.forEach(e => { e.t = e.base * k; });
        L.set("L01.loopTightened"); L.sfx("loop"); L.toast("The Keepers tighten the loop: " + Math.round(lp.duration) + " seconds.", 4);
      }
    };
    AZ.on("shift", from => { if (from === 1 && vossel.seesNow) anomaly(); });
    AZ.on("interacted", () => { if (L.W.state === 1 && vossel.seesNow) anomaly(); });
    AZ.on("loopReset", () => { anomalies = 0; });

    // Vossel after the cup is saved: waits in the plaza with a scene.
    const vNpc = L.npc({ name: "Sister Vossel", group: PR, at: [3, 57.5], y: -60, yaw: Math.PI, coat: "obsidian", hood: true, task: "Keeper watcher: walks the festival street counting what changes, and reports every anomaly to Mother Ondine.",
      route: [[3, 57.5, 6, "look"], [5.5, 55, 5, "work"], [1.5, 59, 5, "look"]], approach: ["I have your count, anomaly.", "Stand still. I'm writing."],
      talk: [line("Vossel", "Sister Vossel, of the Keepers of the Hour. I count what changes."), line("Vossel", "Thirty seconds of festival, forever. Nobody ever dies in it for good. Do you know how rare that is?"),
        line("Vossel", "Every time you shifted in my lantern, I wrote it down. Every time, the hour got shorter. That was us."),
        line("Vossel", "The Founders built the cage. We only keep it locked."), line("Vossel", "Brother Calder keeps the tower. He is less patient than I am.")],
      cond: [{ holding: "missing_poster", lines: [line("Vossel", "Put that down. Some names are safer forgotten. Ask the archive - while it still stands.")] },
        { flag: CLUE_TRUTH, lines: [line("Vossel", "Your own hand, from a tomorrow that should not exist. Mother Ondine said one of you would come back.")] }],
      changedFlag: F.vossel, changedRoute: [[8, 61, 4, "look"], [8, 63.5, 30, "idle"], [7.5, 60, 10, "work"]],
      changedTalk: [line("Vossel", "You have made your choice for tonight. The hour will remember it. So will I."), line("Vossel", "Calder is waiting in the tower. Mind the lights.")] });
    L.when([F.saved], () => { vNpc.p.group.position.set(3, 0, 57.5); });
    L.zone({ at: [0, 1.5, 58], size: [10, 3, 3], mask: 2, requires: [F.inspected], onEnter: () => {
      L.dialogue({ speaker: "Sister Vossel", question: "Sister Vossel steps out of the lamplight. What do you say?", doneFlag: F.vossel,
        opening: [line("Vossel", "So. You are the anomaly I counted. Shifts. Tampering. A cart in a bay that was never used."), line("Vossel", "Thirty seconds of a perfect festival, kept forever. You broke it for a cup.")],
        options: [
          { text: "Nobody dies in your loop. Nobody lives in it either.", sets: "L01.vosselDefied", lines: [line("Vossel", "Brave. Mother Ondine collects brave people. She keeps them, too.")] },
          { text: "Who are the Keepers?", sets: "L01.vosselAsked", lines: [line("Vossel", "We keep the hour. The Founders built the cage; we only lock it. Brother Calder will show you the lock.")] },
          { text: "What happened to the Oskel family?", requires: ["clue.L01_posterOskel"], missing: "(you have not seen the missing posters)", sets: "L01.vosselPressed",
            lines: [line("Vossel", "...Lantern Row is none of your business. Some names are safer forgotten."), line("Vossel", "Ask the archive, if you must. If it is still standing when you get there.")] },
        ] });
    } });

    // ---------------- the loop ----------------
    const LISE_TARGET = [-3.9, 22.4];
    let liseRunning = false;
    const ev = [
      { t: 14, fire: () => {
        if (AZ.get(F.saved) || AZ.get(F.liseStopped)) return;
        liseRunning = true; lise.route = [[LISE_TARGET[0], LISE_TARGET[1], 999, "idle"]]; lise.o.speed = 4.8;
        AZ.ui.bark("Lise Ashgrove", "Cup! Cup! Mirela, wait for me!");
      }, reset: () => { liseRunning = false; lise.route = liseHome; lise.o.speed = 1.3; } },
      { t: 16.2, fire: () => L.sfx("bell", 0.9) },
      { t: 17.2, fire: () => L.sfx("bell", 0.9) },
      { t: 18, fire: () => {
        if (AZ.get(F.chocked) || AZ.get(F.saved)) return;
        L.set(F.rolled); rollT = 0;
      }, reset: () => { rollT = -1; presCart.position.set(17, 2.5, 22.5); presCart.rotation.y = 0; } },
      { t: 18.3, fire: () => {
        if (AZ.get(F.saved) || !AZ.get(F.liseSeen)) return;
        if (AZ.get(F.perfect)) return;
        const miss = [];
        if (!AZ.get(F.chocked)) miss.push("the cart is parked in the PAST");
        if (!AZ.get(F.liseStopped) || AZ.get(F.liseRan)) miss.push("Lise is stopped");
        if (!AZ.get(F.pell)) miss.push("Pell takes the long way");
        L.toast("Not this loop. Still missing: " + miss.join(", ") + ".", 4.5);
      } },
      { t: 18.6, fire: () => {
        if (AZ.get(F.saved)) return;
        if (AZ.get(F.perfect)) { L.set(F.saved); L.toast("The perfect loop. The cart stays, Lise stops, Pell takes the long way - and the blue cup is still whole.", 5); }
        else if (AZ.get(F.rolled) || AZ.get(F.liseRan)) L.sfxAt("shatter", new THREE.Vector3(-4.7, 1, 22.2), 0.7);
      } },
    ];
    ev.forEach(e => { e.base = e.t; });
    L.loop({ duration: 30, breakFlag: F.saved, events: ev });
    L.onReset(() => { pushT = -1; pastCart.position.set(17, 2.5, 22.5); pastCartCol.z = 22.5; pushIt.enabled = true; });

    // Lise's run: standing in her path (or near her) stops her; reaching the stall spoils the loop.
    L.update(() => {
      if (!liseRunning) return;
      const P = AZ.player.pos;
      if (L.W.state === 1 && Math.hypot(P.x - lise.pos.x, P.z - lise.pos.z) < 2.2) {
        liseRunning = false; lise.route = [[lise.pos.x, lise.pos.z, 999, "idle"]]; lise.o.speed = 1.3;
        L.set(F.liseStopped); AZ.ui.bark("Lise Ashgrove", "Oh! Sorry - I nearly ran right into you."); return;
      }
      if (Math.hypot(lise.pos.x - LISE_TARGET[0], lise.pos.z - LISE_TARGET[1]) < 0.4) {
        liseRunning = false; L.set(F.liseRan); L.sfxAt("clang", lise.pos, 0.9);
        AZ.ui.bark("Lise Ashgrove", "Oops! Sorry, Mirela!");
        if (AZ.get(F.cartMoved)) L.set(F.liseSeen);
      }
    });
    // Perfect loop bookkeeping.
    const req = [F.chocked, F.liseStopped, F.pell], labels = ["the cart is parked in the PAST", "Lise is stopped", "Pell takes the long way"];
    AZ.on("flag", (f, v) => {
      if (!v || AZ.get(F.saved)) return;
      if (f === F.liseRan && AZ.get(F.liseSeen)) { L.toast("Lise reached the stall. This loop is spoiled.", 3); return; }
      const i = req.indexOf(f); if (i < 0) return;
      const n = req.filter(r => AZ.get(r)).length;
      if (n === 3 && !AZ.get(F.liseRan)) L.set(F.perfect);
      if (AZ.get(F.liseSeen)) { L.toast("Perfect loop " + n + "/3: " + labels[i], 2.5); L.sfx(n === 3 ? "solved" : "click"); }
    });

    // Observation -> PAST unlock (the first state shift ever: teach it clearly).
    L.onFlag(F.rolled, v => { if (v && !AZ.get(F.crash)) { L.set(F.crash); L.toast("The bell rang twice and the cart rolled into the stall. The cup was already broken.", 4); } });
    L.when([F.crash, F.vendor], () => {
      L.set(F.observed);
      L.unlockStates(1, "NEW STATE: << PAST. Tap the << button to shift to the PAST, [ ] to return to the PRESENT. Hold ECHO to see the other state.");
      L.say([line("The Witness", "You have seen the routine. Now step back, Arin - into the PAST, before the rain."), line("The Witness", "Same street. Same stones. Earlier. What you move there stays moved - for one loop at least."), line("Arin", "And the loop will remember it. Even if nobody else does.")]);
    });
    L.when([F.liseSeen], () => L.say([line("Arin", "The cart, Lise, and Pell standing in the lane. Three causes, all in thirty seconds."), line("The Witness", "Then you need one perfect loop. Plan it before you run it."), line("The Witness", "And stay out of Vossel's lantern. Every time she sees you, the night gets shorter.")]));

    L.zone({ at: [0, 1.5, 53], size: [22, 3, 6], flag: F.inspected, mask: 2, requires: [F.saved], lines: [line("Arin", "The rain has stopped. The lanterns are lit. Nobody is waiting for the bell."), line("Arin", "And there's a letter on the tower step - in my handwriting.")] });
    L.zone({ at: [0, 1.5, 63.4], size: [2.4, 3, 0.8], flag: F.entered });
    L.exit({ group: S, at: [0, 0, 65.6], size: [3.6, 2.5, 2.4], requires: [F.truth], locked: "The tower door is sealed with the clock-tower seal (0). Save the cup, then read the letter on the step." });
    L.checkpoint(0, 0, 9, 0, { size: [8, 2.5, 2] });
    L.checkpoint(5, 0, 22.5, -Math.PI / 2, { climax: true, size: [3, 2.5, 4] });
    L.checkpoint(0, 0, 51, 0, { size: [6, 2.5, 3] });

    // ---------------- optional: Hale's workshop / festival backstage ----------------
    L.box(S, 0.5, 2.5, 2, "stone", 20.25, 1.25, 19);
    L.box(S, 10, 2.5, 10, "wood", 25.5, 1.25, 22.5);
    L.box(S, 0.5, 4.4, 11, "brick", 30.75, 4.7, 22.5); L.box(S, 10.5, 4.4, 0.5, "brick", 25.5, 4.7, 17.25); L.box(S, 10.5, 4.4, 0.5, "brick", 25.5, 4.7, 27.75);
    L.box(S, 10.5, 0.4, 11, "woodDark", 25.5, 7.1, 22.5);
    L.label(S, "CARTWRIGHT - BACKSTAGE", 19.95, 5.5, 19, -Math.PI / 2, 0.22, "#ffffff");
    L.zone({ at: [25.5, 4, 22.5], size: [9, 3, 9], flag: "L01.backstageVisited", toast: "Festival backstage.", lines: [line("Arin", "Behind the parade, the props. Behind the props... a notice board nobody reads.")] });
    L.box(S, 3, 0.9, 1, "wood", 27.5, 2.95, 18.5);
    L.light(PA, 0xffc27a, 2.2, 10, 25.5, 6.2, 22.5); L.light(PR, 0xf0d0a0, 1.8, 8, 25.5, 6.2, 25.5);
    L.box(PA, 4, 0.5, 0.5, "red", 23, 2.8, 26.8, { solid: false });
    L.clue({ group: PA, at: [28.3, 3.43, 18.5], size: [0.45, 0.05, 0.34], prompt: "Read the festival programme", flag: "L01.programmeRead", id: "L01_programme", title: "Festival of the Zero - Programme",
      text: "FESTIVAL OF THE ZERO\n\nDusk: the great bell rings ONCE to open the festival.\nNight: parade from the Arrival Quarter.\nDawn: festival specials depart the station for honoured families (by Founders' invitation only).\n\nPencilled in the margin, in a hand you almost know: 'not once. twice. tell the cartwright to chock his cart.'" });
    L.item({ group: PA, key: "cart_chock", name: "wheel chock", at: [24, 2.75, 19], size: 0.45, mat: "woodDark" });
    L.box(PR, 5, 2.4, 0.1, "woodDark", 25.5, 4.4, 27.45, { solid: false });
    L.label(PR, "MISSING", 25.5, 5.85, 27.35, Math.PI, 0.5, "#ffd8a0");
    L.clue({ group: PR, at: [24, 4.5, 27.32], size: [0.7, 0.9, 0.05], prompt: "Read the poster", flag: "L01.posterOskel", id: "L01_posterOskel", title: "MISSING: The Oskel Family",
      text: "MISSING - The Oskel family of Lantern Row, number nine: Teodor, Edda, and their daughter MAREN, aged seven.\n\nLast seen the night of the festival. Their house still stands. Their neighbours cannot remember their faces.\n\nSomeone has scratched a small 0 in the corner.", noMarker: true });
    L.clue({ group: PR, at: [25.5, 4.2, 27.32], size: [0.7, 0.9, 0.05], prompt: "Read the poster", flag: "L01.posterSchool", id: "L01_posterSchool", title: "MISSING: The Founders' School Excursion",
      text: "Fourteen children of the Founders' School left before dawn on a 'festival excursion' and did not return.\n\nThe school says there was no excursion. The school says there were never fourteen.", noMarker: true });
    L.clue({ group: PR, at: [27, 4.5, 27.32], size: [0.7, 0.9, 0.05], prompt: "Read the notice", flag: "L01.posterOrder", id: "L01_posterOrder", title: "Notice, By Order",
      text: "BY ORDER OF FOUNDER AUREL HALDEN:\n\nEnquiries regarding relocated families are to cease. Posters are to be removed. The festival will proceed.\n\n(This one was not removed. It has been pinned up again, more than once.)", noMarker: true });
    L.item({ group: PR, key: "missing_poster", name: "missing poster", at: [23.5, 2.6, 26], size: 0.5, mat: "paper" });

    // ---------------- memory fragments (off the main path) ----------------
    L.fragment("L01.1", [29.5, 3.6, 26.5], "The Excursion", "They said the school was going on a festival excursion. Fourteen of us in our best coats, at the station before dawn. I had a fever and stayed home. When I went back there were fourteen empty pegs, and nobody would look at them.", S);
    L.fragment("L01.2", [8, 1.2, 63], "The Second Bell", "Grandfather said the bell rings once at dusk for the festival. Only once. When I heard it ring twice I looked up at him, and he was already looking at the tower, the way you look at a door you have heard open before.", S);
    L.fragment("L01.3", [-7.3, 1.2, -5.3], "Names on the Door", "We painted our family name on the door every spring. This spring the paint was there, but nobody could read it - as if the letters had forgotten how to be a name. Where the name had been, someone had carved a small 0.", S);
  },
});
