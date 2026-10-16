/* PROLOGUE — The Last Train. Reference chapter for the level API (see js/world.js). */
AZ.registerLevel({
  id: "P00", order: 0, chapter: "PROLOGUE", title: "The Last Train", location: "Veyra Line, night service",
  start: 1, allowed: 2,
  build(L) {
    const { line } = L, S = L.shared, M = L.MAT;
    L.look({ fog: 0.5, present: { sunI: 0.2 } });
    L.intro(line("Arin", "...You were asleep. You don't remember getting on."), line("Arin", "A coat too heavy for spring. A ticket with no departure station."), line("Conductor Sefa", "Veyra, last stop. Everyone off before the bell, please."));
    L.objective("Check your coat on the seat across the aisle", "arin.watch", "The dark coat on the seat behind you. Tap it.");
    L.objective("Read the letter on the seat", "P.letterRead", "An envelope with a ring-and-zero seal, two rows ahead.");
    L.objective("Ask the conductor where this train came from", "P.talkedConductor", "Conductor Sefa walks the aisle.");
    L.objective("Step off the train", "P.offTrain", "The doors at the front of the carriage are open.");
    L.objective("Walk through the arrival gate into the city", "P.gate", "Follow the platform lamps to the gate.");
    L.payoff("P.letterRead", "A letter dated tomorrow", "\"Don't trust the first bell. Save the blue cup. You have done this before.\" Sealed with the clock-tower seal: (0).");

    // Carriage (3.2 m wide, 22 m long, along +z)
    L.floor(S, 0, 0, 3.2, 22, 0, "woodDark");
    L.box(S, 3.4, 0.2, 22, "iron", 0, 2.85, 0);
    for (const x of [-1.7, 1.7]) { L.box(S, 0.15, 1, 22, "wood", x, 0.5, 0); L.box(S, 0.15, 0.5, 22, "wood", x, 2.5, 0); L.box(S, 0.04, 1.2, 22, "glass", x, 1.6, 0); }
    L.box(S, 3.4, 2.8, 0.2, "wood", 0, 1.4, -11);
    L.wallDoor(S, -1.7, 11, 1.7, 11, 2.8, "wood", 1.4, 2.4);
    for (let i = 0; i < 9; i++) {
      const z = -9 + i * 2.2;
      for (const x of [-1.05, 1.05]) { L.box(S, 1.1, 0.12, 0.9, "teal", x, 0.45, z); L.box(S, 1.1, 0.9, 0.12, "teal", x, 0.9, z - 0.4); L.box(S, 1, 0.4, 0.8, "woodDark", x, 0.2, z, { noEcho: true }); }
      L.light(S, 0xffc27a, 0.9, 5, 0, 2.55, z);
      L.box(S, 0.6, 0.04, 0.2, "warm", 0, 2.73, z, { solid: false });
    }
    L.label(S, "VEYRA LINE  -  LAST STOP", 0, 2.25, 10.85, Math.PI, 0.22, "#ffc070");
    // passing city outside the windows
    const city = new THREE.Group(); S.add(city);
    for (let i = 0; i < 36; i++) { const side = i % 2 ? 1 : -1, h = 4 + Math.random() * 16; const b = L.box(city, 5, h, 4, "plaster", side * (9 + Math.random() * 8), h / 2 - 3, -60 + i * 3.4, { solid: false, dynamic: true, noEcho: true }); }
    L.update(dt => city.children.forEach(b => { b.position.z += dt * 22; if (b.position.z > 62) b.position.z -= 124; }));
    L.particles(S, "rain", [30, 14, 60], [0, -2, 0], 900);
    L.ambient("machine", 0.28); L.ambient("rain", 0.22);

    L.player(0, 0, -7.9, Math.PI);
    L.checkpoint(0, 0, -7.9, Math.PI, { size: [1.4, 2.5, 1.4] });

    // Arin's stopped watch (Design v2 personal thread)
    L.box(S, 0.8, 0.1, 0.6, "navy", -1.05, 0.56, -9, { solid: false });
    L.clue({ group: S, at: [-1.05, 0.66, -9], size: [0.4, 0.1, 0.4], mat: "brass", prompt: "Check the coat pocket", flag: "arin.watch", id: "arin.watch", title: "A stopped pocket watch",
      text: "Brass, scratched, heavier than it looks. The hands are stuck at a minute to midnight.\n\nEngraved inside the lid:\n\n\"For A. - keep time for me.  I.M.\"",
      lines: [line("Arin", "It doesn't tick. I don't know why I'm sure it used to."), line("The Witness", "(faint, over the intercom)  Don't be afraid of the bell, Arin.", 4)] });
    L.clue({ group: S, at: [1.05, 0.53, -4.6], size: [0.3, 0.03, 0.22], prompt: "Read the letter", flag: "P.letterRead", id: "prologue.letter", title: "A letter dated tomorrow",
      text: "Arin,\n\nYou won't remember this, and that's all right. Don't trust the first bell. When the cup breaks, look at what stays broken.\n\nSave the blue cup. Then come to the tower.\n\nYou have done this before.\n\n                                   (0)\n\nDated: tomorrow.",
      lines: [line("Arin", "My name. My... handwriting? No. Older. Shakier.")] });

    // Conductor + passengers
    L.npc({ name: "Conductor Sefa", at: [0, 6], yaw: Math.PI, coat: "navy", task: "Walks the last service, counting passengers off before the bell.",
      route: [[0, 6, 4, "look"], [0, -3, 3, "work"], [0, 9.5, 5, "idle"]], approach: ["Last stop. Off before the bell.", "You again? No... I'd remember that coat."],
      talk: [line("Conductor Sefa", "Where from? This is the only train, friend. It goes to Veyra and it stops."), line("Conductor Sefa", "Funny thing. I count one more passenger off than I count on. Every night."), line("Arin", "Every night?"), line("Conductor Sefa", "Did I say that? Long shift. Off you go, the festival's starting.")],
      cond: [{ flag: "P.letterRead", sets: "P.talkedConductor", lines: [line("Arin", "Does this seal mean anything? A ring with a zero."), line("Conductor Sefa", "That's the tower's seal. Only the clockkeeper uses it. And she hasn't written to anyone in forty years."), line("Conductor Sefa", "The train came from the end of the line. Don't ask me where the end of the line is.")] }],
      talkedFlag: "P.talkedConductor", changedFlag: "P.offTrain", changedRoute: [[0, 9.6, 999, "look"]], changedTalk: [line("Conductor Sefa", "Go on. The bell rings twice tonight. It always does.")] });
    L.npc({ name: "", named: false, at: [-1.05, 2], coat: "red", route: [[-1.05, 2, 999, "idle"]] });
    L.npc({ name: "", named: false, at: [1.05, 6.4], coat: "gold", yaw: Math.PI, route: [[1.05, 6.4, 999, "look"]] });
    const kid = L.npc({ name: "", named: false, at: [-1.05, 3.6], coat: "ochre", scale: 0.6, y: 0.45, route: [[-1.05, 3.6, 999, "idle"]] });
    L.onHum([-1.05, 0.5, 3.6], 4, () => { L.set("P.humHeard"); L.say([line("Child", "(waking)  That song... how do you know the Nursery song?"), line("Arin", "I don't. It was just... in my head.")]); });
    L.zone({ at: [-0.3, 1.2, 3.6], size: [2.5, 2.5, 2], toast: "A tune is stuck in your head. Press HUM." });
    L.zone({ at: [0, 1.2, 12.3], size: [3, 2.5, 1.4], flag: "P.offTrain", toast: "Veyra. The air smells of rain and festival smoke." });

    // Platform
    const PL = L.shared;
    L.ramp(PL, [0, 0, 11.1], [0, 0.6, 12.6], 1.4, "iron");
    L.floor(PL, -2.5, 31, 8, 38, 0.6, "stoneWet");
    L.floor(PL, -2.5, 52.5, 8, 5, 0.6, "tile");
    L.box(PL, 5, 0.4, 60, "concrete", 4, -0.8, 30, { noEcho: true });
    L.block(1.4, 31, 0.2, 38, 0.6, 3, 7);
    L.block(-6.6, 36, 0.2, 48, 0.6, 3, 7);
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
    for (let i = 0; i < 14; i++) L.building(PL, 14 + (i % 3) * 9, 2 + i * 6, 7, 10 + (i * 7) % 13, 6, "plasterCold", "windowCold", i);
    L.box(PL, 9, 50, 9, "plasterCold", 30, 25, 120, { solid: false, noEcho: true });
    L.holoMesh(PL, "cyl", "amber", 30, 42, 115.4, 6, 0.1, 6).rotation.x = Math.PI / 2;
    L.light(PL, 0xffd090, 5, 30, 30, 42, 112);

    L.fragment("P.1", [-1.05, 1.0, 8.5], "A ticket stub", "Veyra Line. Single. The departure station has been scratched out so many times the paper is thin.", S);
    L.fragment("P.2", [-6, 1.6, 40], "Chalk tally", "Behind the lamp post: tally marks in chalk, hundreds of them. The newest one is still wet.", PL);
    L.fragment("P.3", [0.8, 1.6, 46], "A child's drawing", "A tower with two bells and a blue cup. Signed 'M.O.'", PL);
  },
});
