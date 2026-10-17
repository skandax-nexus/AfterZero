/* AFTERZERO — game flow: load chapters, shift states, die/respawn, save, main loop. */
(function () {
  "use strict";
  const AZ = window.AZ, R = AZ.render, UI = AZ.ui, P = AZ.player;
  const G = (AZ.game = {});
  let W = null, L = null, current = -1, levelStart = null, focus = null, last = 0, t0 = 0, loading = false, hintShown = false, objSince = 0, lastObj = null;
  const params = new URLSearchParams(location.search);

  G.world = () => W;
  G.state = () => (W ? W.state : 1);
  G.inLevel = () => !!W;

  // ---------------- loading ----------------
  G.load = async (index, opts = {}) => {
    if (loading) return;
    loading = true;
    UI.menu(false);
    await UI.fade(1, 500);
    unload();
    current = Math.max(0, Math.min(AZ.levels.length - 1, index));
    const def = AZ.levels[current];
    levelStart = AZ.snapshot();
    R.LOOK = JSON.parse(JSON.stringify(baseLook));
    R.fogMul = 1;
    L = AZ.createLevel(def);
    W = L.W;
    try { def.build(L); } catch (e) { console.error("Level build failed", def.id, e); }
    W.groups.forEach((g, i) => { R.mergeStatic(g); const e = R.buildEcho(g); W.echoes[i] = e; W.root.add(e); });
    R.mergeStatic(W.shared);
    R.scene.add(W.root);
    W.state = def.start !== undefined ? def.start : 1;
    W.spawn.state = W.state;
    W.checkpoint = { pos: W.spawn.pos.clone(), yaw: W.spawn.yaw, state: W.state };
    teleport(W.spawn.pos, W.spawn.yaw);
    applyState(true);
    AZ.save.write(current);
    UI.resetLocks(); UI.refreshHeld(); UI.refreshStates(); UI.timer(-1); UI.loopBar(null);
    hintShown = false; objSince = performance.now(); lastObj = null;
    document.body.classList.add("ingame");
    await UI.fade(0, 700);
    loading = false;
    // story beats
    if (W.interlude && !opts.skipStory) {
      UI.lock(true); R.memory = 1;
      await UI.titleCard("INTERLUDE", W.interlude.title, "");
      await UI.sayAndWait(W.interlude.lines);
      R.memory = 0; UI.lock(false);
    }
    await UI.titleCard(def.chapter, def.title.toUpperCase(), def.location);
    if (W.intro.length) UI.say(W.intro);
    if (def.onStart) def.onStart(L);
  };
  const baseLook = JSON.parse(JSON.stringify(R.LOOK));
  function unload() {
    if (W) { R.scene.remove(W.root); W.root.traverse(o => { if (o.geometry && o.userData.merged) o.geometry.dispose(); }); }
    AZ.stopAmbient(); AZ.clearLevelHandlers(); R.clearLights();
    W = null; L = null; focus = null;
    AZ.sim.hideAvatar();
    document.body.classList.remove("ingame");
  }
  G.complete = async () => {
    const def = AZ.levels[current];
    if (def.onComplete) def.onComplete(L);
    if (current + 1 >= AZ.levels.length) { AZ.save.clear(); G.menu(); return; }
    AZ.save.write(current + 1);
    G.load(current + 1);
  };
  G.restartLevel = () => { if (levelStart) AZ.restore(levelStart); G.load(current, { skipStory: true }); };
  G.menu = async () => { await UI.fade(1, 400); unload(); G.loadMenuScene(); UI.menu(true); await UI.fade(0, 600); };
  G.newGame = () => { AZ.save.clear(); AZ.restore({}); G.load(0); };
  G.continueGame = () => { const s = AZ.save.load(); if (!s) return G.newGame(); AZ.restore(s.world); G.load(s.level || 0); };
  G.saveNow = () => { if (W) AZ.save.write(current); };

  // ---------------- states ----------------
  function applyState(instant) {
    W.groups.forEach((g, i) => { g.visible = i === W.state; });
    if (instant) R.weights = [0, 1, 2].map(i => (i === W.state ? 1 : 0));
    UI.refreshStates();
    AZ.emit("state", W.state);
  }
  let lastShift = 0;
  G.shift = s => {
    if (!W || UI.locked() || P.dying) return;
    if (s === W.state) return;
    if (!(W.allowed & (1 << s))) { UI.toast(s === 0 ? "The Past is out of reach here." : s === 2 ? "The Future is out of reach here." : "Not here.", 2); AZ.sfx("blocked"); return; }
    if (performance.now() - lastShift < 700) return;
    if (AZ.sim.overlapsSolid(W, P.pos, P.height, P.radius, 1 << s)) { UI.toast("Something occupies this space in the " + AZ.STATE_NAMES[s] + ".", 2.2); AZ.sfx("blocked"); R.ripple(0.25); AZ.vibrate(40); return; }
    const from = W.state;
    if (R.transition) R.transition();
    W.state = s; lastShift = performance.now();
    applyState(false);
    R.ripple(1); AZ.vibrate(25);
    AZ.sfx(["past", "present", "future"][s]);
    AZ.emit("shift", from, s);
  };
  G.hum = () => {
    if (!W || UI.locked()) return;
    if (G._humT && performance.now() - G._humT < 3000) return;
    G._humT = performance.now();
    UI.say([{ speaker: "Arin", text: "(humming)  mm - mm - mm - mm", dur: 2.4 }], false);
    [1, 1.26, 1.498, 1.335].forEach((p, i) => setTimeout(() => AZ.note(p * 0.5), i * 500));
    setTimeout(() => AZ.emit("hum", P.pos.clone()), 2000);
    AZ.emit("noise", P.pos.clone(), 0.3);
  };

  // ---------------- interaction ----------------
  G.useFocus = () => { if (!W || UI.locked()) { if (UI.speaking()) UI.skipLine(); return; } if (focus) AZ.sim.use(W, focus); else if (UI.speaking()) UI.skipLine(); };
  G.tap = (x, y) => {
    if (!W || UI.locked()) return;
    if (UI.speaking() && !focus) { UI.skipLine(); return; }
    const it = AZ.sim.pick(W, (x / innerWidth) * 2 - 1, -(y / innerHeight) * 2 + 1);
    if (it) { const d = it.pos.distanceTo(AZ.sim.eye()); if (d <= it.radius + 1.2) AZ.sim.use(W, it); else UI.toast("Move closer.", 1.4); }
  };

  // ---------------- death / respawn ----------------
  function teleport(pos, yaw) { P.pos.copy(pos); P.vy = 0; P.yaw = yaw; P.pitch = -0.12; P.face = yaw + Math.PI; P.pivotY = null; P.camDist = 3; }
  G.die = async msg => {
    if (!W || P.dying) return;
    P.dying = true;
    AZ.sfx("hurt"); AZ.vibrate(120);
    if (msg) UI.toast(msg, 3.5);
    await UI.fade(1, 450);
    const cp = W.checkpoint;
    teleport(cp.pos, cp.yaw);
    if (cp.state !== W.state) { W.state = cp.state; applyState(true); }
    AZ.emit("respawn");
    await new Promise(r => setTimeout(r, 200));
    P.dying = false;
    await UI.fade(0, 500);
  };

  // ---------------- objectives / payoff ----------------
  AZ.on("flag", AZ.persistent((f, v) => {
    if (!W || !v) return;
    const cur = W.objectives.find(o => !AZ.get(o.flag));
    if (lastObj && f === lastObj.flag) AZ.sfx("objective");
    if (W.payoff && f === W.payoff.flag) { AZ.addJournal("payoff." + AZ.levels[current].id, W.payoff.title, W.payoff.text); setTimeout(() => UI.revelation(W.payoff.title, W.payoff.text), 600); }
    refreshObjective(cur);
  }));
  function refreshObjective(cur) {
    cur = cur === undefined ? W.objectives.find(o => !AZ.get(o.flag)) : cur;
    if (cur !== lastObj) { lastObj = cur; objSince = performance.now(); hintShown = false; UI.objective(cur ? cur.text : ""); }
  }

  // ---------------- main loop ----------------
  function frame(now) {
    requestAnimationFrame(frame);
    const dt = Math.min(0.05, (now - last) / 1000 || 0.016); last = now;
    const t = (now - t0) / 1000;
    if (W && !UI.paused) {
      UI.keyMove && UI.keyMove();
      AZ.sim.step(W, dt, t);
      W.t += dt;
      focus = UI.locked() ? null : AZ.sim.focus(W);
      UI.prompt(focus ? focus.prompt : "");
      R.holoUniforms.uReveal.value += ((AZ.sim.input.echo || W.def.echoes ? 1 : 0) - R.holoUniforms.uReveal.value) * Math.min(1, dt * 5);
      W.echoes.forEach((e, i) => { if (e) e.visible = R.holoUniforms.uReveal.value > 0.02 && i !== W.state; });
      if (lastObj === null && W.objectives.length) refreshObjective();
      if (lastObj && lastObj.hint && !hintShown && (now - objSince) / 1000 > AZ.diff.hintDelay()) { hintShown = true; UI.toast("Hint: " + lastObj.hint, 7); }
      AZ.updateAmbient(1 << W.state);
      R.frame(dt, t, W.state, P.pos);
    } else if (menuScene) {
      menuScene(dt, t);
      R.fogMul = 0.35;
      R.frame(dt, t, 2, new THREE.Vector3(0, 0, 0));
    } else R.frame(dt, t, W ? W.state : 1, P.pos);
    R.adapt(dt);
    R.render();
  }

  // ---------------- title screen scene ----------------
  let menuScene = null, menuRoot = null;
  G.loadMenuScene = () => {
    if (menuRoot) R.scene.remove(menuRoot);
    menuRoot = new THREE.Group(); R.scene.add(menuRoot);
    const M = R.MAT;
    R.mesh(menuRoot, "box", M.obsidian, 0, -0.2, 0, 400, 0.4, 400);
    let s = 7; const rnd = () => ((s = (s * 16807) % 2147483647) / 2147483647);
    for (let i = 0; i < 60; i++) { const a = rnd() * Math.PI * 2, r = 30 + rnd() * 110, h = 10 + rnd() * 45; R.mesh(menuRoot, "box", M.ruin, Math.cos(a) * r, h / 2, Math.sin(a) * r, 8 + rnd() * 10, h, 8 + rnd() * 10, rnd() * 3); if (i % 3 === 0) R.mesh(menuRoot, "box", rnd() < 0.5 ? M.neonM : M.neonC, Math.cos(a) * r, h * (0.3 + rnd() * 0.6), Math.sin(a) * r, 9, 0.12, 0.12, rnd() * 3); }
    R.mesh(menuRoot, "box", M.ruin, 0, 20, 0, 10, 40, 10);
    const l = R.mesh(menuRoot, "box", M.ruin, -3, 48, 0, 4, 16, 10); l.rotation.z = 0.15;
    const r2 = R.mesh(menuRoot, "box", M.ruin, 3.5, 47, 0, 4, 14, 10); r2.rotation.z = -0.18;
    const rings = [0, 1, 2].map(i => { const m = R.mesh(menuRoot, "torus", i === 1 ? M.holoM : M.holoC, 0, 40 + i * 3, 0, 16 + i * 5, 16 + i * 5, 2); m.rotation.x = Math.PI / 2 + i * 0.2; return m; });
    R.mesh(menuRoot, "cyl", M.holoC, 0, 70, 0, 1.2, 140, 1.2);
    R.clearLights(); R.virtualLight(menuRoot, 0x7fe9ff, 6, 90, 0, 44, 0);
    R.points(menuRoot, 900, 0x7ff0ff, 0.12, [120, 50, 120], [0, 0, 0], { rise: 0.4 });
    menuScene = (dt, t) => {
      rings.forEach((m, i) => { m.rotation.z += dt * (0.1 + i * 0.07) * (i % 2 ? -1 : 1); });
      R.camera.position.set(Math.sin(t * 0.03) * 70, 26 + Math.sin(t * 0.1) * 3, Math.cos(t * 0.03) * 70);
      R.camera.lookAt(0, 30, 0);
      R.animateParticles(menuRoot, dt, t);
    };
    AZ.stopAmbient(); AZ.ambient("drone", 0.35, 7);
  };
  const origLoad = G.load;
  G.load = async (i, o) => { menuScene = null; if (menuRoot) { R.scene.remove(menuRoot); menuRoot = null; } return origLoad(i, o); };

  // ---------------- test hooks (used by test/solve/*.js; harmless in the app) ----------------
  AZ.test = {
    goto(x, y, z) { P.pos.set(x, y, z); P.vy = 0; },
    face(x, z) { P.yaw = Math.atan2(-(x - P.pos.x), -(z - P.pos.z)); P.face = P.yaw + Math.PI; P.pitch = -0.15; },
    near(filter) { const W2 = W; return W2.interactables.filter(it => it.enabled && (!filter || (it.prompt || "").toLowerCase().includes(filter.toLowerCase()))).map(it => ({ prompt: it.prompt, pos: it.pos.toArray().map(v => +v.toFixed(2)), mask: it.mask })); },
    /** Walk-free use: teleport next to the first enabled interactable whose prompt contains text, shift to its state, use it. */
    async use(text, opts = {}) {
      const it = W.interactables.find(i => i.enabled && (i.prompt || "").toLowerCase().includes(text.toLowerCase()) && (!opts.mask || (i.mask & opts.mask)));
      if (!it) throw new Error("No interactable matching '" + text + "'");
      if (!(it.mask & (1 << W.state))) { const s = [0, 1, 2].find(k => it.mask & (1 << k)); W.state = s; W.allowed |= 1 << s; applyState(true); }
      const g = AZ.sim.groundAt(W, it.pos.x, it.pos.y + 1, it.pos.z + 1.2);
      P.pos.set(it.pos.x, isFinite(g.y) ? g.y : it.pos.y - 1, it.pos.z + 1.2);
      await AZ.sim.use(W, it);
      await AZ.test.wait(opts.wait || 300);
      return it.prompt;
    },
    async pick(i) { let b = null; for (let k = 0; k < 60 && !b; k++) { await AZ.test.wait(200); b = document.querySelectorAll("#pChoice.show .opt")[i]; if (!b) UI.skipLine(); } if (!b) throw new Error("No choice panel/option " + i); b.click(); await AZ.test.wait(200); },
    closeDoc() { const b = document.querySelector("#pDoc.show .btn"); if (b) b.click(); },
    skipTalk() { for (let i = 0; i < 40; i++) UI.skipLine(); },
    shift(s) { W.allowed |= 1 << s; G.shift(s); },
    hum() { G._humT = 0; G.hum(); },
    wait: ms => new Promise(r => setTimeout(r, ms)),
    world: () => W, level: () => (W ? W.def.id : null),
    objective: () => { const o = W && W.objectives.find(o2 => !AZ.get(o2.flag)); return o ? o.text : null; },
  };

  // ---------------- boot ----------------
  window.addEventListener("load", async () => {
    R.init(document.getElementById("view"));
    const boot = document.getElementById("boot"); if (boot) boot.textContent = "AFTERZERO  ·  LOADING VEYRA";
    await Promise.all([AZ.humans.load(), AZ.props ? AZ.props.load() : null]);
    UI.build();
    addEventListener("resize", () => R.resize());
    document.addEventListener("visibilitychange", () => { if (document.hidden) window.azPause(); });
    t0 = performance.now(); last = t0;
    requestAnimationFrame(frame);
    // Test / deep-link: index.html?level=3 jumps straight into a chapter.
    const lv = params.get("level");
    if (lv !== null) { const i = isNaN(+lv) ? AZ.levels.findIndex(l => l.id === lv) : +lv; G.load(Math.max(0, i), { skipStory: params.has("skipstory") }); }
    else { G.loadMenuScene(); UI.menu(true); }
    document.getElementById("boot").remove();
  });
})();
