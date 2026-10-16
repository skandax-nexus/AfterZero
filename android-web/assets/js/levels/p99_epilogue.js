/* EPILOGUE — After Zero. Plays the ending earned by the player's choices, then credits. */
AZ.registerLevel({
  id: "E99", order: 9, chapter: "EPILOGUE", title: "After Zero", location: "Veyra, the morning after",
  start: 1, allowed: 2,
  build(L) {
    const { line } = L, S = L.shared;
    const evidence = L.get("choice.L08.evidence");
    L.look({ fog: 0.35, present: evidence
      ? { sky: 0x2a1d48, fog: 0x3a2a60, sun: 0x80ffb0, sunI: 0.8, hemiS: 0x6a5aa8, tint: [0.98, 1.0, 1.06], sat: 1.15 }
      : { sky: 0xe0b080, fog: 0xd8a878, sun: 0xffd8a0, sunI: 1.8, hemiS: 0xffe0b8, tint: [1.06, 1.0, 0.9], sat: 1.0 } });
    L.floor(S, 0, 0, 40, 40, 0, "stone");
    L.box(S, 40, 1, 0.6, "stone", 0, 0.5, 14);
    for (let i = 0; i < 5; i++) L.lamp(S, -12 + i * 6, 12, "gas");
    L.player(0, 0, -6, Math.PI);
    L.block(0, 16, 40, 1, 0, 3);
    L.ambient("wind", 0.25);
    if (!evidence) {
      // Stability: the festival again, but people remember.
      for (let i = 0; i < 4; i++) { L.box(S, 2.6, 0.12, 1.4, "wood", -9 + i * 6, 1, 4, { solid: false }); L.box(S, 2.8, 0.08, 1.8, ["red", "gold", "teal", "red"][i], -9 + i * 6, 2.6, 4, { solid: false }); }
      L.cyl(S, 0.08, 0.16, "cup", -9, 1.14, 4);
      L.bunting(S, -16, 16, 6, 6); L.bunting(S, -16, 16, -2, 6.5);
      L.crowd(S, 0, 4, 22, 10, 14);
      L.box(S, 9, 50, 9, "plaster", 0, 25, 40, { solid: false });
      L.cyl(S, 3.2, 0.3, "warm", 0, 40, 35.4).rotation.x = Math.PI / 2;
      L.particles(S, "dust", [36, 8, 30], [0, 0, 0], 600);
      L.ambient("crowd", 0.35);
    } else {
      // Evidence: the horizon opens on regions with different physical laws.
      for (let i = 0; i < 9; i++) {
        const isle = L.mesh(S, "cone", i % 2 ? "crystal" : "obsidian", -60 + i * 15, 18 + (i % 3) * 9, 70 + (i % 2) * 20, 9, 7, 9);
        isle.rotation.x = Math.PI; isle.userData.dynamic = true;
        const by = isle.position.y; L.update((dt, t) => { isle.position.y = by + Math.sin(t * 0.3 + i) * 1.2; });
      }
      const ring = L.holoMesh(S, "torus", "cyan", 0, 30, 110, 60, 60, 4, { spin: 0.05 }); ring.rotation.x = Math.PI / 2;
      L.light(S, 0x60ff90, 8, 140, -50, 35, 90);
      L.particles(S, "motes", [40, 30, 40], [30, 0, 70], 600);
      L.label(S, "VEYRA RECORD OFFICE - THE NAMES OF THE ERASED", 0, 3.5, 13.6, Math.PI, 0.35, "#ffe8c0", "rgba(20,14,10,.85)");
      L.ambient("drone", 0.3);
    }
  },
  async onStart(L) {
    const { line } = L, UI = AZ.ui;
    const evidence = L.get("choice.L08.evidence");
    const lines = evidence ? [
      line("Arin", "The record is public. Every name the Founders erased is back on the wall."),
      line("Pell Ashgrove", "Lise faded at dawn. She was born in the loop - she only ever had the one night."),
      line("Pell Ashgrove", "I'd choose it again. She'd want the names said out loud."),
      line("The Witness", "The states are letting go of each other. Look at the horizon, Arin."),
      line("Arin", "Islands falling upward. A green sun. Rain going the wrong way."),
      line("Maren", "Come on, Arin. For once, let's be late."),
    ] : [
      line("Arin", "The bell rang once this morning. Only once."),
      line("Pell Ashgrove", "Letter for Arin Vale. Dated today, for a change. Welcome back."),
      line("The Witness", "The city still repeats. But now it remembers a little more every time. That's how wounds close."),
      line("Mother Ondine Halden", "The Keepers answer to you now, clockkeeper. Keep time for us."),
      line("Arin", "(the watch ticks)  I'll keep it, Ilse. Every night."),
    ];
    if (L.get("choice.L05.records")) lines.push(line("Odile Fenwick", "I've published the families' register. All of them. Someone has to say the names out loud."));
    else if (L.get("choice.L05.keepsake")) lines.push(line("Maren", "You saved my music box. Listen - all four notes."));
    const groups = ["L07.group1Saved", "L07.group2Saved", "L07.group3Saved"].filter(f => L.get(f)).length;
    lines.push(groups >= 3 ? line("Warden Roe", "Every group made the tram stairs. Not one name lost that night.") : line("Warden Roe", "We didn't get everyone out of the square. I'll carry their names. So should you."));
    const frags = AZ.journal.filter(j => j.id.startsWith("frag.")).length;
    if (frags >= 27) lines.push(line("Ilse Marrow", "(an echo winds the watch)  You found every piece of us, my little A. Thank you for remembering."));
    await new Promise(r => setTimeout(r, 1200));
    AZ.sfx("watch"); setTimeout(() => AZ.sfx("watch"), 1000);
    await UI.sayAndWait(lines);
    UI.revelation(evidence ? "The states begin to separate" : "The cycle holds, and remembers",
      "AFTERZERO - GAME 01: THE FIRST CYCLE.  Thank you for playing. Memory fragments found: " + frags + "/27. The story continues beyond the horizon.");
    await new Promise(r => setTimeout(r, 7000));
    AZ.save.clear();
    AZ.game.menu();
  },
});
