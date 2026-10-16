/* AFTERZERO — world simulation + the level-building API ("L") used by every level file.
   States: 0 PAST, 1 PRESENT, 2 FUTURE. Masks: 1 PAST, 2 PRESENT, 4 FUTURE, 7 ALL. */
(function () {
  "use strict";
  const AZ = window.AZ, R = AZ.render;
  const V3 = (x, y, z) => new THREE.Vector3(x, y, z);
  const BIT = s => 1 << s;
  AZ.MASK = { PAST: 1, PRESENT: 2, FUTURE: 4, ALL: 7 };
  AZ.STATE_NAMES = ["PAST", "PRESENT", "FUTURE"];

  // =====================================================================
  // Level context
  // =====================================================================
  AZ.createLevel = def => {
    const root = new THREE.Group();
    const shared = new THREE.Group(), groups = [new THREE.Group(), new THREE.Group(), new THREE.Group()];
    root.add(shared, ...groups);
    groups.forEach((g, i) => { g.userData.mask = BIT(i); });
    shared.userData.mask = 7;

    const W = {
      def, root, shared, groups,
      colliders: [], interactables: [], zones: [], npcs: [], keepers: [], pursuers: [], hazards: [], fires: [],
      updaters: [], resetters: [], movers: [], objectives: [], echoes: [],
      state: def.start !== undefined ? def.start : 1, allowed: def.allowed || 7,
      spawn: { pos: V3(0, 0, 0), yaw: 0, state: 1 }, checkpoint: null, loop: null, timer: null,
      held: null, payoff: null, intro: [], interlude: null, exitDone: false, t: 0,
    };

    const maskOf = g => (g && g.userData.mask !== undefined ? g.userData.mask : maskOf(g && g.parent)) || 7;
    const g0 = g => g || shared;

    // ---------------- colliders ----------------
    // Box: oriented in yaw only. Top surface is walkable; sides block.
    function addCollider(c) { c.on = c.on !== false; W.colliders.push(c); return c; }
    const L = {
      W, shared, past: groups[0], present: groups[1], future: groups[2], groups, root,
      MAT: R.MAT, holo: R.holo, THREE,
      PAST: 1, PRESENT: 2, FUTURE: 4, ALL: 7,
    };

    /** Box mesh (+ solid collider unless solid:false). (x,y,z) is the box CENTER. opts: {solid, ry, mask, echo, dynamic} */
    L.box = (g, w, h, d, mat, x, y, z, opts = {}) => {
      g = g0(g);
      const m = R.mesh(g, "box", mat, x, y, z, w, h, d, opts.ry || 0);
      if (opts.noEcho) m.userData.noEcho = true;
      if (opts.dynamic) m.userData.dynamic = true;
      if (opts.shadow === false) m.castShadow = false;
      if (opts.solid !== false) m.userData.collider = addCollider({ x, z, hw: w / 2, hd: d / 2, yaw: opts.ry || 0, y0: y - h / 2, y1: y + h / 2, mask: opts.mask || maskOf(g), mesh: opts.dynamic ? m : null, w, h, d });
      return m;
    };
    /** Floor slab: top surface at y. */
    L.floor = (g, x, z, w, d, y, mat, thick = 0.4, opts = {}) => L.box(g, w, thick, d, mat, x, y - thick / 2, z, opts);
    /** Wall from (x1,z1) to (x2,z2), standing on y. */
    L.wall = (g, x1, z1, x2, z2, h, mat, t = 0.4, y = 0, opts = {}) => {
      const dx = x2 - x1, dz = z2 - z1, len = Math.hypot(dx, dz);
      return L.box(g, t, h, len, mat, (x1 + x2) / 2, y + h / 2, (z1 + z2) / 2, Object.assign({ ry: Math.atan2(dx, dz) }, opts));
    };
    /** Wall with a doorway gap (width dw, height dh) centred at fraction f along it. */
    L.wallDoor = (g, x1, z1, x2, z2, h, mat, dw = 1.8, dh = 2.6, f = 0.5, t = 0.4, y = 0) => {
      const dx = x2 - x1, dz = z2 - z1, len = Math.hypot(dx, dz), ux = dx / len, uz = dz / len;
      const c = Math.min(Math.max(f * len, dw / 2), len - dw / 2);
      const a = c - dw / 2, b = c + dw / 2;
      if (a > 0.05) L.wall(g, x1, z1, x1 + ux * a, z1 + uz * a, h, mat, t, y);
      if (len - b > 0.05) L.wall(g, x1 + ux * b, z1 + uz * b, x2, z2, h, mat, t, y);
      if (h > dh) L.wall(g, x1 + ux * a, z1 + uz * a, x1 + ux * b, z1 + uz * b, h - dh, mat, t, y + dh);
    };
    /** Four walls + floor (+ceiling). doors: array of side indices 0=N(+z) 1=E(+x) 2=S(-z) 3=W(-x). */
    L.room = (g, x, z, w, d, h, wallMat, floorMat, doors = [], ceilMat = null, y = 0) => {
      if (floorMat) L.floor(g, x, z, w, d, y, floorMat);
      const hx = w / 2, hz = d / 2;
      const sides = [[x - hx, z + hz, x + hx, z + hz], [x + hx, z + hz, x + hx, z - hz], [x + hx, z - hz, x - hx, z - hz], [x - hx, z - hz, x - hx, z + hz]];
      sides.forEach((s, i) => (doors.includes(i) ? L.wallDoor(g, ...s, h, wallMat, 1.8, 2.6, 0.5, 0.4, y) : L.wall(g, ...s, h, wallMat, 0.4, y)));
      if (ceilMat) L.box(g, w, 0.3, d, ceilMat, x, y + h + 0.15, z);
    };
    /** Walkable ramp from point a to b (top surface), width w. Use for stairs too (stepped visuals optional). */
    L.ramp = (g, a, b, w, mat, opts = {}) => {
      g = g0(g);
      const dx = b[0] - a[0], dz = b[2] - a[2], len = Math.hypot(dx, dz), yaw = Math.atan2(dx, dz);
      const slope = Math.atan2(b[1] - a[1], len), L3 = Math.hypot(len, b[1] - a[1]);
      const m = R.mesh(g, "box", mat, (a[0] + b[0]) / 2, (a[1] + b[1]) / 2 - 0.12, (a[2] + b[2]) / 2, w, 0.24, L3);
      m.rotation.set(-slope, yaw, 0, "YXZ");
      addCollider({ ramp: true, x: (a[0] + b[0]) / 2, z: (a[2] + b[2]) / 2, hw: w / 2, hd: len / 2, yaw, ya: a[1], yb: b[1], y0: Math.min(a[1], b[1]) - 0.3, y1: Math.max(a[1], b[1]), mask: opts.mask || maskOf(g) });
      return m;
    };
    L.stairs = (g, a, b, w, mat, opts = {}) => {
      g = g0(g);
      const steps = Math.max(2, Math.round(Math.abs(b[1] - a[1]) / 0.2));
      const dx = b[0] - a[0], dz = b[2] - a[2], yaw = Math.atan2(dx, dz), len = Math.hypot(dx, dz);
      for (let i = 0; i < steps; i++) {
        const t = (i + 0.5) / steps, top = a[1] + (b[1] - a[1]) * (i + 1) / steps;
        const s = R.mesh(g, "box", mat, a[0] + dx * t, (top + Math.min(a[1], b[1])) / 2, a[2] + dz * t, w, Math.max(0.05, top - Math.min(a[1], b[1])), len / steps, yaw);
        s.userData.noEcho = !opts.echo;
      }
      addCollider({ ramp: true, x: (a[0] + b[0]) / 2, z: (a[2] + b[2]) / 2, hw: w / 2, hd: len / 2, yaw, ya: a[1], yb: b[1], y0: Math.min(a[1], b[1]) - 0.3, y1: Math.max(a[1], b[1]), mask: opts.mask || maskOf(g) });
    };
    L.cyl = (g, r, h, mat, x, y, z, opts = {}) => {
      g = g0(g);
      const m = R.mesh(g, opts.seg === 8 ? "cyl8" : "cyl", mat, x, y, z, r * 2, h, r * 2);
      if (opts.solid) m.userData.collider = addCollider({ x, z, hw: r * 0.85, hd: r * 0.85, yaw: 0, y0: y - h / 2, y1: y + h / 2, mask: opts.mask || maskOf(g) });
      return m;
    };
    L.sphere = (g, r, mat, x, y, z) => R.mesh(g0(g), "sphere", mat, x, y, z, r * 2, r * 2, r * 2);
    L.mesh = (g, geo, mat, x, y, z, sx, sy, sz, ry) => R.mesh(g0(g), geo, mat, x, y, z, sx, sy, sz, ry);
    /** Invisible blocker (e.g. deep water edge, void edge). */
    L.block = (x, z, w, d, y0, y1, mask = 7, opts = {}) => addCollider(Object.assign({ x, z, hw: w / 2, hd: d / 2, yaw: opts.ry || 0, y0, y1, mask }, opts));
    L.label = (g, text, x, y, z, ry = 0, size = 0.5, color = "#ffffff", bg = null) => R.label(g0(g), text, x, y, z, ry, size, color, bg);
    /** Point light (pooled: only the nearest few render). Returns {intensity, color, position, visible}. */
    L.light = (g, color, intensity, range, x, y, z) => R.virtualLight(g0(g), color, intensity, range, x, y, z);
    /** Spot-ish light: pooled point light placed between source and target. */
    L.spot = (g, color, intensity, range, x, y, z, tx, ty, tz) => R.virtualLight(g0(g), color, intensity, range, (x + tx) / 2, (y + ty) / 2, (z + tz) / 2);
    L.holoMesh = (g, geo, color, x, y, z, sx = 1, sy = 1, sz = 1, opts = {}) => {
      const mat = color === "magenta" ? R.MAT.holoM : color === "amber" ? R.MAT.holoA : R.MAT.holoC;
      const m = R.mesh(g0(g), geo, mat, x, y, z, sx, sy, sz, opts.ry || 0);
      m.castShadow = false; m.userData.noMerge = true;
      if (opts.spin) L.update(dt => { m.rotation.y += dt * opts.spin; });
      if (opts.bob) { const by = y, ph = Math.random() * 6; L.update((dt, t) => { m.position.y = by + Math.sin(t * 0.8 + ph) * opts.bob; }); }
      return m;
    };
    L.particles = (g, kind, area, center, count) => {
      g = g0(g);
      if (kind === "rain") return R.rain(g, area, center, count || 1400);
      const k = {
        dust: [0xffd59a, 0.05, count || 500, { drift: true }], motes: [0x7ff0ff, 0.08, count || 700, { rise: 0.35 }],
        embers: [0xff9a40, 0.07, count || 300, { rise: 0.8 }], snow: [0xdfe8ff, 0.06, count || 600, { fall: 0.8 }],
        drips: [0xa8d0ff, 0.05, count || 300, { fall: 6 }], dreams: [0xbfa0ff, 0.07, count || 700, { rise: 0.2 }],
        debris: [0xb0a090, 0.08, count || 250, { fall: 3 }],
      }[kind] || [0xffffff, 0.05, count || 300, {}];
      return R.points(g, k[2], k[0], k[1], area, center, k[3]);
    };
    L.ambient = (kind, vol = 0.3, mask = 7) => AZ.ambient(kind, vol, mask);

    // ---------------- dressing kits ----------------
    L.lamp = (g, x, z, style = "sodium", h = 4.2, y = 0) => {
      g = g0(g);
      const post = style === "gas" ? "brass" : style === "neon" ? "obsidian" : "iron";
      L.cyl(g, 0.07, h, post, x, y + h / 2, z, { seg: 8 });
      const head = style === "gas" ? "warm" : style === "neon" ? "neonC" : "neonA";
      const hm = L.mesh(g, "sphere", head, x, y + h, z, 0.4, 0.4, 0.4); hm.castShadow = false;
      return L.light(g, style === "gas" ? 0xffb469 : style === "neon" ? 0x40d8ff : 0xff9a48, style === "neon" ? 1.6 : 2.0, 12, x, y + h - 0.2, z);
    };
    L.building = (g, x, z, w, h, d, wallMat = "plaster", winMat = "window", seed = 1, sign = null, signColor = "#ffd28a") => {
      g = g0(g);
      L.box(g, w, h, d, wallMat, x, h / 2, z);
      L.box(g, w + 0.5, 0.4, d + 0.5, "woodDark", x, h + 0.2, z, { solid: false });
      let s = seed * 9301 + 49297;
      const rnd = () => ((s = (s * 9301 + 49297) % 233280) / 233280);
      const floors = Math.max(1, Math.floor((h - 1.5) / 3)), cols = Math.max(1, Math.floor(w / 2.4));
      for (let f = 0; f < floors; f++) for (let c = 0; c < cols; c++) {
        if (rnd() < 0.35) continue;
        const wx = x - w / 2 + (c + 0.5) * w / cols, wy = 2 + f * 3;
        L.box(g, 1.1, 1.4, 0.06, winMat, wx, wy, z - d / 2 - 0.03, { solid: false, noEcho: true, shadow: false });
        L.box(g, 1.1, 1.4, 0.06, winMat, wx, wy, z + d / 2 + 0.03, { solid: false, noEcho: true, shadow: false });
      }
      if (sign) L.label(g, sign, x, Math.min(h - 1, 4.2), z - d / 2 - 0.08, Math.PI, 0.55, signColor, "rgba(10,12,18,0.85)");
    };
    L.skyline = (g, cx, cz, r0, r1, n, mat = "ruin", seed = 3) => {
      let s = seed;
      const rnd = () => ((s = (s * 16807) % 2147483647) / 2147483647);
      for (let i = 0; i < n; i++) {
        const a = rnd() * Math.PI * 2, r = r0 + rnd() * (r1 - r0), h = 15 + rnd() * 50;
        const m = L.box(g, 8 + rnd() * 12, h, 8 + rnd() * 12, mat, cx + Math.cos(a) * r, h / 2, cz + Math.sin(a) * r, { solid: false, ry: rnd() * 3, noEcho: true, shadow: false });
      }
    };
    L.crystals = (g, n, cx, cz, w, d, y = 0) => {
      for (let i = 0; i < n; i++) {
        const c = L.mesh(g, "octa", "crystal", cx + (Math.random() - 0.5) * w, y + 0.5, cz + (Math.random() - 0.5) * d, 0.6 + Math.random(), 1.6 + Math.random() * 2, 0.6 + Math.random());
        c.rotation.y = Math.random() * 3;
      }
    };
    L.debris = (g, n, cx, cy, cz, w, h, d) => {
      for (let i = 0; i < n; i++) {
        const s = 0.4 + Math.random() * 1.4;
        const m = L.box(g, s, s * (0.5 + Math.random()), s, Math.random() < 0.3 ? "crystal" : "obsidian", cx + (Math.random() - 0.5) * w, cy + Math.random() * h, cz + (Math.random() - 0.5) * d, { solid: false, dynamic: true, noEcho: true });
        const by = m.position.y, ph = Math.random() * 6, sp = 0.2 + Math.random() * 0.5;
        L.update((dt, t) => { m.position.y = by + Math.sin(t * sp + ph) * 0.5; m.rotation.x += dt * sp * 0.3; m.rotation.y += dt * sp * 0.2; });
      }
    };
    L.bunting = (g, x1, x2, z, h = 7) => {
      g = g0(g);
      const pts = []; for (let i = 0; i <= 16; i++) { const t = i / 16; pts.push(V3(x1 + (x2 - x1) * t, h - Math.sin(t * Math.PI) * 1.2, z)); }
      const line = new THREE.Line(new THREE.BufferGeometry().setFromPoints(pts), new THREE.LineBasicMaterial({ color: 0x3a2a1a }));
      line.userData.noEcho = true; g.add(line);
      const mats = ["red", "gold", "teal"];
      for (let i = 1; i < 16; i += 2) { const f = L.mesh(g, "cone4", mats[i % 3], pts[i].x, pts[i].y - 0.35, pts[i].z, 0.5, 0.6, 0.5); f.rotation.x = Math.PI; f.userData.noEcho = true; }
    };

    /** Stylised person mesh (body, head, visor band, arms). Returns {group, armL, armR, head, body}. */
    L.person = (g, x, z, coat = "navy", yaw = 0, scale = 1, opts = {}) => {
      g = g0(g);
      const grp = new THREE.Group(); grp.position.set(x, opts.y || 0, z); grp.rotation.y = yaw; grp.scale.setScalar(scale);
      grp.userData.dynamic = true; grp.userData.noEcho = !opts.echo;
      const body = new THREE.Group(); grp.add(body);
      const mat = typeof coat === "string" && R.MAT[coat] ? R.MAT[coat] : coat;
      R.mesh(body, "cyl", mat, 0, 1.0, 0, 0.48, 1.05, 0.34);
      R.mesh(body, "box", R.MAT.woodDark, 0, 0.42, 0, 0.34, 0.84, 0.22);
      const head = new THREE.Group(); head.position.y = 1.68; body.add(head);
      R.mesh(head, "sphere", opts.skin || R.MAT.skin, 0, 0, 0, 0.3, 0.32, 0.3);
      if (opts.visor !== false) { const v = R.mesh(head, "box", R.MAT.neonC, 0, 0.02, 0.14, 0.22, 0.05, 0.04); v.castShadow = false; }
      if (opts.hood) R.mesh(head, "cone", R.MAT.obsidian, 0, 0.12, 0, 0.4, 0.45, 0.4);
      const arm = side => { const p = new THREE.Group(); p.position.set(side * 0.3, 1.42, 0); body.add(p); R.mesh(p, "cyl8", mat, 0, -0.34, 0, 0.12, 0.66, 0.12); return p; };
      const armL = arm(-1), armR = arm(1);
      g.add(grp);
      return { group: grp, body, head, armL, armR };
    };

    // ---------------- player / objectives / story ----------------
    L.player = (x, y, z, yaw = 0) => { W.spawn = { pos: V3(x, y, z), yaw, state: W.state }; };
    L.objective = (text, flag, hint = "") => W.objectives.push({ text, flag, hint });
    L.intro = (...lines) => { W.intro.push(...lines); };
    L.interlude = (title, lines) => { W.interlude = { title, lines }; };
    L.payoff = (flag, title, text) => { W.payoff = { flag, title, text }; };
    L.line = (speaker, text, dur) => ({ speaker, text, dur: dur || Math.max(2.4, Math.min(6, text.length * 0.06)) });
    L.say = (lines, interrupt = true) => AZ.ui.say(Array.isArray(lines) ? lines : [lines], interrupt);
    L.toast = (msg, sec) => AZ.ui.toast(msg, sec);
    L.set = AZ.set; L.get = AZ.get; L.all = AZ.all;
    L.journal = AZ.addJournal;
    L.sfx = AZ.sfx; L.sfxAt = AZ.sfxAt;
    L.look = (tweaks = {}) => { if (tweaks.fog) R.fogMul = tweaks.fog; ["past", "present", "future"].forEach((k, i) => { if (tweaks[k]) Object.assign(R.LOOK[i], tweaks[k]); }); };
    L.update = fn => { W.updaters.push(fn); return fn; };
    L.onReset = fn => { W.resetters.push(fn); return fn; };
    /** Call fn once when all flags are set (also immediately at start if already set). */
    L.when = (flags, fn, opts = {}) => {
      flags = Array.isArray(flags) ? flags : [flags];
      let done = false;
      const check = () => { if (!done && AZ.all(flags)) { done = !opts.repeat; fn(); } };
      AZ.on("flag", (f, v) => { if (v && flags.includes(f)) check(); });
      W.updaters.push(() => { if (!W.whenChecked) { } });
      setTimeout(check, 0);
    };
    L.onFlag = (flag, fn) => AZ.on("flag", (f, v) => { if (f === flag) fn(v); });
    L.unlockStates = (mask, toast) => { W.allowed |= mask; AZ.sfx("rule"); R.ripple(0.6); if (toast) AZ.ui.toast(toast, 5); AZ.ui.refreshStates(); };

    // ---------------- interaction ----------------
    /** Generic usable thing. opts: {at:[x,y,z], radius, prompt, mask, requires, locked, once, mesh, onUse(), visible fn, usedPrompt} */
    L.interact = opts => {
      const it = Object.assign({ radius: 2.6, prompt: "Use", mask: 7, requires: [], once: false, used: false, enabled: true }, opts);
      it.pos = Array.isArray(it.at) ? V3(...it.at) : it.at;
      if (it.mesh) { it.mesh.userData.interactive = true; it.mesh.traverse(o => { o.userData.interactive = true; o.userData.interact = it; }); }
      W.interactables.push(it);
      return it;
    };
    /** Readable clue/document: sets flag, adds a journal entry, shows a document panel. */
    L.clue = o => {
      const mesh = o.mesh || L.box(o.group, o.size ? o.size[0] : 0.5, o.size ? o.size[1] : 0.06, o.size ? o.size[2] : 0.4, o.mat || "paper", o.at[0], o.at[1], o.at[2], { solid: false });
      if (!o.noMarker) L.holoMesh(o.group, "torus", "amber", o.at[0], o.at[1] + 0.45, o.at[2], 0.5, 0.5, 0.5, { spin: 1.2, bob: 0.05 }).rotation.x = Math.PI / 2;
      return L.interact({
        at: o.at, mesh, mask: o.mask || maskOf(o.group), prompt: o.prompt || "Read", requires: o.requires || [], locked: o.locked,
        onUse: it => {
          AZ.set(o.flag);
          if (o.id !== null) AZ.addJournal(o.id || o.flag, o.title, o.text);
          if (o.text) AZ.ui.document(o.title, o.text);
          if (o.lines) setTimeout(() => AZ.ui.say(o.lines), 400);
          if (o.onUse) o.onUse(it);
          AZ.sfx("use");
          it.prompt = "Read again";
        },
      });
    };
    /** Carryable item (one at a time). */
    L.item = o => {
      const mesh = o.mesh || L.box(o.group, o.size || 0.45, o.size || 0.45, o.size || 0.45, o.mat || "brass", o.at[0], o.at[1], o.at[2], { solid: false, dynamic: true });
      mesh.userData.dynamic = true;
      const home = mesh.position.clone(), homeParent = mesh.parent;
      const it = L.interact({
        at: o.at, mesh, mask: o.mask || maskOf(o.group), prompt: "Take " + o.name, requires: o.requires || [],
        onUse: () => {
          if (W.held) { AZ.ui.toast("You're already carrying the " + W.held.name + ".", 2.5); return; }
          W.held = { key: o.key, name: o.name, mesh, item: it };
          mesh.visible = false; it.enabled = false;
          AZ.set(o.pickedFlag); AZ.sfx("pickup"); AZ.ui.refreshHeld();
          if (o.onPick) o.onPick();
        },
      });
      it.returnHome = () => { homeParent.add(mesh); mesh.position.copy(home); mesh.visible = true; it.enabled = true; };
      L.onReset(() => { if (o.resetOnLoop && W.held && W.held.item === it) { W.held = null; AZ.ui.refreshHeld(); it.returnHome(); } });
      return it;
    };
    /** Socket that accepts a held item with matching key. */
    L.socket = o => {
      const base = L.cyl(o.group, o.radius || 0.4, 0.06, "iron", o.at[0], o.at[1], o.at[2]);
      const ring = L.holoMesh(o.group, "torus", "cyan", o.at[0], o.at[1] + 0.06, o.at[2], (o.radius || 0.4) * 2.2, (o.radius || 0.4) * 2.2, 1, { spin: 0.8 });
      ring.rotation.x = Math.PI / 2;
      const it = L.interact({
        at: o.at, mesh: base, mask: o.mask || maskOf(o.group), prompt: o.prompt || "Place", requires: o.requires || [],
        visibleIf: () => !!W.held,
        onUse: () => {
          if (!W.held) { AZ.ui.toast(o.hint || "Something belongs here.", 2.5); return; }
          if (o.accepts && W.held.key !== o.accepts) { AZ.ui.toast(o.wrong || "That doesn't fit here.", 2.5); AZ.sfx("denied"); return; }
          const h = W.held; W.held = null; AZ.ui.refreshHeld();
          h.mesh.visible = true; base.parent.add(h.mesh);
          h.mesh.position.set(o.at[0], o.at[1] + 0.3, o.at[2]);
          it.enabled = false; ring.visible = false;
          AZ.sfxAt("place", it.pos); AZ.set(o.flag);
          if (o.onPlace) o.onPlace(h);
        },
      });
      return it;
    };
    /** Door that opens (slides by offset) when flags are set. Returns {mesh, open()}. */
    L.door = (g, w, h, d, mat, x, y, z, flags, offset = [0, h + 0.1, 0], opts = {}) => {
      const m = L.box(g, w, h, d, mat, x, y + h / 2, z, Object.assign({ dynamic: true }, opts));
      L.box(g, w + 0.2, 0.1, d + 0.2, "neonC", x, y + h + 0.05, z, { solid: false, ry: opts.ry || 0, shadow: false, noEcho: true });
      const start = m.position.clone(); let t = 0, open = false;
      const doOpen = () => { if (open) return; open = true; AZ.sfxAt("rule", start); if (opts.toast) AZ.ui.toast(opts.toast, 3); };
      if (flags && flags.length) L.when(flags, doOpen);
      L.update(dt => { if (!open || t >= 1) return; t = Math.min(1, t + dt / (opts.time || 1.6)); const e = t * t * (3 - 2 * t); m.position.set(start.x + offset[0] * e, start.y + offset[1] * e, start.z + offset[2] * e); });
      return { mesh: m, open: doOpen };
    };
    /** Zone: box trigger. opts: {at:[x,y,z], size:[w,h,d], mask, flag, once, onEnter, toast, lines, requires} */
    L.zone = o => {
      const z = Object.assign({ once: true, mask: 7, fired: false, inside: false, requires: [] }, o);
      z.min = V3(o.at[0] - o.size[0] / 2, o.at[1] - o.size[1] / 2, o.at[2] - o.size[2] / 2);
      z.max = V3(o.at[0] + o.size[0] / 2, o.at[1] + o.size[1] / 2, o.at[2] + o.size[2] / 2);
      W.zones.push(z);
      return z;
    };
    L.checkpoint = (x, y, z, yaw = 0, opts = {}) => {
      if (opts.climax && !AZ.diff.climaxCheckpoints()) return null;
      return L.zone({ at: [x, y + 1, z], size: opts.size || [4, 2.5, 4], once: true, onEnter: () => {
        W.checkpoint = { pos: V3(x, y, z), yaw, state: AZ.game.state() };
        AZ.sfx("checkpoint"); AZ.ui.toast("Checkpoint", 1.4);
        AZ.save.write(def.order);
      } });
    };
    L.exit = o => {
      const at = o.at;
      L.holoMesh(o.group, "sphere", "cyan", at[0], at[1] + 3, at[2], 0.4, 0.4, 0.4, { spin: 1, bob: 0.1 });
      L.light(o.group, 0x7fe9ff, 2.2, 8, at[0], at[1] + 2, at[2]);
      return L.zone({ at: [at[0], at[1] + 1.2, at[2]], size: o.size || [3, 2.5, 3], once: false, mask: o.mask || 7, onEnter: () => {
        if (W.exitDone) return;
        if (!AZ.all(o.requires || [])) { AZ.ui.toast(o.locked || "The way forward is still sealed.", 3); AZ.sfx("denied"); return; }
        W.exitDone = true; AZ.sfx("exit"); AZ.game.complete();
      } });
    };
    L.fragment = (id, at, title, text, g) => {
      if (AZ.get("frag." + id)) return;
      const m = L.holoMesh(g, "octa", "amber", at[0], at[1], at[2], 0.35, 0.55, 0.35, { spin: 1.5, bob: 0.12 });
      const l = L.light(g, 0xffb060, 1.2, 4, at[0], at[1], at[2]);
      const it = L.interact({ at, mesh: m, mask: maskOf(g), prompt: "Touch the memory fragment", onUse: () => {
        AZ.addJournal("frag." + id, "Memory fragment: " + title, text); AZ.set("frag." + id);
        m.visible = false; l.visible = false; it.enabled = false;
        AZ.sfx("memory", 0.7); R.ripple(0.35);
        const n = AZ.journal.filter(j => j.id.startsWith("frag.")).length;
        AZ.ui.toast("Memory fragment " + n + "/27: " + title, 4);
      } });
    };

    // ---------------- NPCs ----------------
    /** Named or background NPC. o: {name, at:[x,z], y, yaw, coat, mask, group, route:[[x,z,wait,activity]], task, talk:[lines], approach:[strings],
     *  cond:[{flag|holding, lines, sets}], changedFlag, changedRoute, changedTalk, scale, named(default true), barks(world reactions)} */
    L.npc = o => {
      const g = o.group || shared;
      const p = L.person(g, o.at[0], o.at[1], o.coat || "navy", o.yaw || 0, o.scale || 1, { y: o.y || 0, hood: o.hood, skin: o.skin, visor: o.visor });
      if (o.holo) p.group.traverse(m => { if (m.isMesh) { m.material = o.holo === "magenta" ? R.MAT.holoM : o.holo === "amber" ? R.MAT.holoA : R.MAT.holoC; m.castShadow = false; } });
      const n = { o, p, name: o.name || "Resident", pos: p.group.position, route: o.route || [], idx: 0, wait: 0, phase: Math.random() * 6, talkIdx: 0, approached: false, mask: o.mask || maskOf(g), y: o.y || 0 };
      const start = p.group.position.clone(), startYaw = o.yaw || 0;
      n.reset = () => { p.group.position.copy(start); p.group.rotation.y = startYaw; n.idx = 0; n.wait = 0; };
      L.onReset(n.reset);
      if (o.named !== false) {
        const tag = L.label(p.group, o.name, 0, 2.15, 0, 0, 0.22, "#cfeeff");
        n.tag = tag;
        n.it = L.interact({ at: p.group.position, mesh: p.group, mask: n.mask, prompt: "Talk to " + o.name, radius: 2.8, onUse: () => talk(n) });
        n.it.npc = n;
      }
      W.npcs.push(n);
      return n;
    };
    function talk(n) {
      const o = n.o;
      const held = W.held && W.held.key;
      for (const c of o.cond || []) {
        if ((c.holding && c.holding === held) || (c.flag && !c.holding && AZ.get(c.flag))) {
          if (c.once && AZ.get("talked." + n.name + "." + (c.flag || c.holding))) continue;
          AZ.ui.say(c.lines); AZ.set(c.sets); AZ.set("talked." + n.name + "." + (c.flag || c.holding));
          return;
        }
      }
      if (o.changedFlag && AZ.get(o.changedFlag) && o.changedTalk && o.changedTalk.length) { AZ.ui.say(o.changedTalk); AZ.set(o.talkedFlag); return; }
      const talkSets = o.talk || [];
      if (talkSets.length && Array.isArray(talkSets[0])) { AZ.ui.say(talkSets[n.talkIdx % talkSets.length]); n.talkIdx++; }
      else AZ.ui.say(talkSets);
      AZ.set(o.talkedFlag);
      if (o.onTalk) o.onTalk(n);
    }
    L.crowd = (g, cx, cz, w, d, count, opts = {}) => {
      const coats = ["navy", "ochre", "olive", "burgundy", "grey", "white", "teal", "red"];
      const out = [];
      for (let i = 0; i < count; i++) {
        const x = cx + (Math.random() - 0.5) * w, z = cz + (Math.random() - 0.5) * d;
        const route = [[x, z, 2 + Math.random() * 4, i % 3 === 0 ? "work" : "idle"], [cx + (Math.random() - 0.5) * w, cz + (Math.random() - 0.5) * d, 2 + Math.random() * 4, i % 4 === 1 ? "talk" : "look"]];
        out.push(L.npc({ group: g, name: "", named: false, at: [x, z], y: opts.y || 0, yaw: Math.random() * 6, coat: opts.holo ? "navy" : coats[i % coats.length], holo: opts.holo, route, scale: 0.9 + Math.random() * 0.2, barks: opts.barks }));
      }
      return out;
    };

    // ---------------- Keepers (sight + hearing) ----------------
    /** o: {name, mode:'hunter'|'watcher', group, at:[x,z], path:[[x,z]], speed, chase, view, angle, mask, disabledBy, alarmFlag, caught} */
    L.keeper = o => {
      const g = o.group || shared;
      const p = L.person(g, o.at[0], o.at[1], "obsidian", o.yaw || 0, 1.05, { hood: true, visor: false, y: o.y || 0 });
      const lantern = L.mesh(p.group, "sphere", "warm", 0.3, 1.2, 0.35, 0.18, 0.22, 0.18); lantern.castShadow = false;
      const coneLen = (o.view || 11) * (o.mode === "watcher" ? 0.8 : 1);
      const coneGeo = new THREE.ConeGeometry(Math.tan(((o.angle || 70) / 2) * Math.PI / 180) * coneLen, coneLen, 20, 1, true);
      coneGeo.translate(0, -coneLen / 2, 0); coneGeo.rotateX(-Math.PI / 2);
      const cone = new THREE.Mesh(coneGeo, new THREE.MeshBasicMaterial({ color: 0xffc070, transparent: true, opacity: 0.09, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide }));
      cone.position.set(0, 1.2, 0.2); cone.rotation.x = 0.12; cone.userData.noEcho = true; p.group.add(cone);
      const glow = L.light(p.group, 0xffb060, 1.4, 6, 0.3, 1.3, 0.6);
      const k = { o, p, cone, glow, name: o.name || "Keeper", mode: o.mode || "hunter", path: o.path || [], idx: 0, wait: 0, det: 0, chasing: false, lost: 0, inv: null, barkCd: 0,
        mask: o.mask || maskOf(g), view: (o.view || 11) * (0.6 + 0.4 * AZ.diff.perception()), angle: o.angle || 70, speed: o.speed || 1.6, chaseSpeed: (o.chase || 4.4) * (0.9 + 0.1 * AZ.diff.perception()),
        hear: (o.hear || 9) * AZ.diff.perception(), detectTime: (o.detect || 1.3) / AZ.diff.perception(), start: p.group.position.clone(), startYaw: o.yaw || 0 };
      k.reset = () => { p.group.position.copy(k.start); p.group.rotation.y = k.startYaw; k.det = 0; k.chasing = false; k.inv = null; k.idx = 0; };
      AZ.on("respawn", k.reset);
      AZ.on("noise", (pos, loud) => { if (k.mode === "hunter" && !k.chasing && activeMask(k.mask) && !AZ.get(o.disabledBy) && pos.distanceTo(p.group.position) < k.hear * Math.min(1, loud + 0.2)) k.inv = pos.clone(); });
      W.keepers.push(k);
      return k;
    };
    /** Scripted chaser along a lane. o: {name, group, at:[x,z], lane:[[x,z]], startFlag, stopFlag, speed, caught} */
    L.pursuer = o => {
      const p = L.person(o.group || shared, o.at[0], o.at[1], "obsidian", o.yaw || 0, 1.15, { hood: true, y: o.y || 0 });
      const c = { o, p, lane: o.lane || [], idx: 0, running: false, delay: 0, start: p.group.position.clone(), speed: (o.speed || 4.6) * (0.85 + 0.15 * AZ.diff.perception()), bark: 0, mask: o.mask || 7 };
      L.when([o.startFlag], () => { if (!AZ.get(o.stopFlag)) { c.running = true; c.delay = o.delay || 1.5; AZ.sfx("alert"); AZ.vibrate(60); } });
      if (o.stopFlag) L.onFlag(o.stopFlag, v => { if (v) c.running = false; });
      AZ.on("respawn", () => { p.group.position.copy(c.start); c.idx = 0; if (c.running) c.delay = (o.delay || 1.5) + 1; });
      W.pursuers.push(c);
      return c;
    };

    // ---------------- hazards / fire / water / movers ----------------
    /** Telegraphed hazard: idle -> warn (blinking light + sound) -> active (damage zone). */
    L.hazard = o => {
      const h = Object.assign({ idle: 3, warn: 1.4, active: 1.2, offset: 0, mask: 7, phase: "idle", timer: 0, msg: "Watch for the warning before it strikes." }, o);
      h.timer = h.idle - h.offset;
      h.min = V3(o.at[0] - o.size[0] / 2, o.at[1], o.at[2] - o.size[2] / 2);
      h.max = V3(o.at[0] + o.size[0] / 2, o.at[1] + o.size[1], o.at[2] + o.size[2] / 2);
      h.light = L.light(o.group, o.color || 0xff3020, 0, 9, o.at[0], o.at[1] + o.size[1] + 0.5, o.at[2]);
      h.marker = L.box(o.group, o.size[0], 0.04, o.size[2], "red2", o.at[0], o.at[1] + 0.03, o.at[2], { solid: false, noEcho: true, dynamic: true, shadow: false });
      h.marker.material = new THREE.MeshBasicMaterial({ color: 0xff3020, transparent: true, opacity: 0, depthWrite: false });
      if (o.visual) { o.visual.userData.dynamic = true; h.visualBase = o.visual.position.clone(); }
      W.hazards.push(h);
      return h;
    };
    /** Spreading fire along nodes; each node can be put out by a flag. */
    L.fire = o => {
      const f = Object.assign({ interval: 10, telegraph: 3, nodes: [], running: false, timer: 0, front: 0 }, o);
      f.interval *= AZ.diff.timer();
      f.nodes = o.nodes.map(n => {
        const pts = L.particles(o.group, "embers", [1.4, 2.2, 1.4], [n.at[0], n.at[1], n.at[2]], 160);
        const l = L.light(o.group, 0xff6a20, 0, 8, n.at[0], n.at[1] + 1, n.at[2]);
        const base = L.mesh(o.group, "cone", "neonA", n.at[0], n.at[1] + 0.6, n.at[2], 1, 1.2, 1); base.castShadow = false; base.userData.noEcho = true;
        pts.visible = false; base.visible = false;
        return Object.assign({ burning: false, pts, light: l, base }, n);
      });
      const setNode = (n, on) => { n.burning = on; n.pts.visible = on; n.base.visible = on; n.light.intensity = on ? 3 : 0; };
      f.ignite = i => { const n = f.nodes[i]; if (!n || AZ.get(n.out)) return; setNode(n, true); AZ.sfxAt("hazard", V3(...n.at), 0.7); if (i === f.nodes.length - 1) { AZ.set(o.lostFlag); AZ.game.die(o.lostMessage || "The fire reached the records."); } };
      f.begin = () => { if (f.running) return; f.running = true; f.front = 0; f.timer = 0; f.ignite(0); AZ.ui.toast(o.startToast || "Fire!", 3.5); };
      f.reset = () => { if (!f.running) return; f.nodes.forEach(n => setNode(n, false)); f.front = 0; f.timer = 0; f.ignite(0); };
      AZ.on("respawn", f.reset);
      AZ.on("flag", (fl, v) => { if (!v) return; f.nodes.forEach(n => { if (n.burning && fl === n.out) { setNode(n, false); AZ.sfxAt("water", V3(...n.at), 0.8); } }); if (f.running && f.nodes.every(n => !n.out || AZ.get(n.out))) { f.running = false; f.nodes.forEach(n => setNode(n, false)); AZ.set(o.allOutFlag); AZ.sfx("solved"); } });
      if (o.startFlag) L.when([o.startFlag], f.begin);
      f.setNode = setNode;
      W.fires.push(f);
      return f;
    };
    /** Water surface whose height follows flags; blocks wading while high. */
    L.water = o => {
      const m = L.box(o.group, o.size[0], 0.05, o.size[1], "water", o.at[0], o.high, o.at[1], { solid: false, dynamic: true, noEcho: true, shadow: false });
      const blocker = L.block(o.at[0], o.at[1], o.size[0], o.size[1], -50, o.high - 0.6, o.mask || 7, { water: true });
      let y = o.high;
      const target = () => (o.lowWhen && AZ.all(o.lowWhen) ? o.low : o.riseWhen && AZ.all(o.riseWhen) ? (o.risen !== undefined ? o.risen : o.high) : o.high);
      y = target(); m.position.y = y;
      L.update(dt => {
        const t = target();
        if (Math.abs(t - y) > 0.001) { y += Math.sign(t - y) * Math.min(Math.abs(t - y), (o.speed || 0.6) * dt); m.position.y = y; if (Math.abs(t - y) < 0.01 && t === o.low) AZ.set(o.drainedFlag); }
        blocker.y1 = y - (o.deep || 0.9);
        blocker.on = !o.wadeable && y > (o.low + 0.4);
      });
      return { mesh: m, level: () => y };
    };
    /** Moving object; fn(t, dt, mesh) positions it. platform:true lets the player ride it. */
    L.mover = (mesh, fn, opts = {}) => {
      mesh.userData.dynamic = true;
      const c = mesh.userData.collider;
      const mv = { mesh, fn, platform: opts.platform !== false, collider: c };
      W.movers.push(mv);
      return mv;
    };

    // ---------------- loop / timer ----------------
    /** Recurrence loop. o: {duration, breakFlag, events:[{t, fire(), reset()}]} */
    L.loop = o => {
      W.loop = Object.assign({ elapsed: 0, count: 0, running: true, fired: {} }, o);
      W.loop.base = o.duration;
      return W.loop;
    };
    L.timer = o => {
      W.timer = Object.assign({ running: false, remaining: o.duration * AZ.diff.timer(), label: o.label || "SECOND BELL IN", finished: false }, o);
      W.timer.duration = o.duration * AZ.diff.timer();
      W.timer.remaining = W.timer.duration;
      if (o.startFlag) L.when([o.startFlag], () => { W.timer.running = true; if (o.startToast) AZ.ui.toast(o.startToast, 4.5); AZ.sfx("bell"); });
      else W.timer.running = true;
      if (o.successFlag) L.when([o.successFlag], () => { W.timer.running = false; W.timer.finished = true; AZ.ui.timer(-1); });
      return W.timer;
    };
    L.penalize = s => { if (W.timer && W.timer.running) { W.timer.remaining = Math.max(1, W.timer.remaining - s); AZ.sfx("tick"); AZ.ui.toast("-" + s + "s", 1.5); } };

    // ---------------- puzzles ----------------
    /** Rotating element (steps), correct index; glyph label above. Returns element with .index. */
    L.rotator = o => {
      const grp = new THREE.Group(); grp.position.set(...o.at); grp.userData.dynamic = true; g0(o.group).add(grp);
      const body = o.mesh || R.mesh(grp, "box", o.mat || "brass", 0, 0, 0, o.size ? o.size[0] : 1, o.size ? o.size[1] : 0.3, o.size ? o.size[2] : 1);
      if (!o.mesh) { const arrow = R.mesh(body, "cone4", R.MAT.neonA, 0, 0.6, 0.35, 0.25, 0.6, 0.25); arrow.rotation.x = Math.PI / 2; arrow.castShadow = false; }
      if (o.glyph) L.label(o.group, o.glyph, o.at[0], o.at[1] + 1.1, o.at[2], 0, 0.45, "#ffffff");
      const e = { index: o.start || 0, steps: o.steps || 4, correct: o.correct || 0, grp, vis: (o.start || 0) * Math.PI * 2 / (o.steps || 4) };
      grp.rotation.y = e.vis;
      e.it = L.interact({ at: o.at, mesh: grp, mask: o.mask || maskOf(o.group), prompt: o.prompt || "Turn", requires: o.requires || [], onUse: () => { e.index = (e.index + 1) % e.steps; AZ.sfxAt("click", e.it.pos); if (o.onTurn) o.onTurn(e); AZ.emit("rotated", e); } });
      L.update(dt => { const tgt = e.index * Math.PI * 2 / e.steps; let d = tgt - e.vis; if (d < -0.01) d += Math.PI * 2; e.vis += Math.min(d, dt * 5); if (e.vis > Math.PI * 2) e.vis -= Math.PI * 2; grp.rotation.y = e.vis; });
      e.ok = () => e.index === e.correct;
      return e;
    };
    L.alignment = (elements, flag, opts = {}) => {
      let solved = false;
      const check = () => { if (solved) return; if (elements.every(e => e.ok()) && AZ.all(opts.requires || [])) { solved = true; AZ.set(flag); AZ.sfx("solved"); elements.forEach(e => { e.it.enabled = false; }); if (opts.toast) AZ.ui.toast(opts.toast, 3); } };
      AZ.on("rotated", check); AZ.on("flag", check);
    };
    /** Ordered notes puzzle. o: {buttons:[{at, note, mask, group, label}], solution:[notes], flag, bell, requires} */
    L.sequence = o => {
      const pitches = [1, 1.122, 1.26, 1.335, 1.498, 1.682, 1.888, 2];
      let entered = [], solved = false;
      const s = { pitches, demo: async (skip = []) => { for (let i = 0; i < o.solution.length; i++) { if (!skip.includes(i)) AZ.note(pitches[o.solution[i]], o.bell); await new Promise(r => setTimeout(r, 550)); } } };
      o.buttons.forEach(b => {
        const mesh = b.mesh || (o.bell ? L.mesh(b.group, "cone", "brass", b.at[0], b.at[1], b.at[2], 0.9, 1, 0.9) : L.box(b.group, 0.6, 0.6, 0.6, ["red", "gold", "teal", "navy", "olive", "white"][b.note % 6], b.at[0], b.at[1], b.at[2], { solid: false }));
        if (b.label) L.label(b.group, b.label, b.at[0], b.at[1] + 0.8, b.at[2], b.ry || 0, 0.3, "#ffffff");
        L.interact({ at: b.at, mesh, mask: b.mask || maskOf(b.group), prompt: o.bell ? "Strike the bell" : "Strike", onUse: () => {
          if (solved) return;
          if (!AZ.all(o.requires || [])) { AZ.ui.toast(o.lockedMsg || "Nothing answers yet.", 2); return; }
          AZ.note(pitches[b.note], o.bell);
          entered.push(b.note);
          const i = entered.length - 1;
          if (entered[i] !== o.solution[i]) { entered = b.note === o.solution[0] ? [b.note] : []; if (!entered.length) AZ.sfx("denied", 0.4); if (o.onWrong) o.onWrong(); return; }
          if (entered.length === o.solution.length) { solved = true; AZ.set(o.flag); AZ.sfx("solved"); }
        } });
      });
      return s;
    };
    /** Deduction board: choose the right candidate; penalties scale with difficulty. */
    L.deduction = o => {
      let solved = false;
      return L.interact({ at: o.at, mesh: o.mesh, mask: o.mask || 7, prompt: o.prompt || "Study the evidence", requires: o.requires || [], locked: o.locked, onUse: async it => {
        if (solved) return;
        const pick = await AZ.ui.choose(o.question, o.candidates.map(c => ({ text: c })));
        if (pick === o.correct) { solved = true; it.enabled = false; AZ.set(o.flag); AZ.sfx("solved"); if (o.lines) AZ.ui.say(o.lines); return; }
        AZ.sfx("denied"); AZ.vibrate(80);
        const d = AZ.settings.difficulty;
        if (d === 0) AZ.ui.toast(o.wrong || "That doesn't fit the evidence.", 3);
        else if (d === 1) { AZ.ui.toast((o.wrong || "That doesn't fit the evidence.") + (W.timer ? " (-10s)" : ""), 3); L.penalize(10); AZ.emit("noise", it.pos.clone(), 1.2); }
        else { AZ.ui.toast((o.wrong || "Wrong.") + " The board resets - check the clues again.", 4); (o.resetFlags || []).forEach(f => AZ.set(f, false)); L.penalize(10); }
      } });
    };
    /** Branching conversation. o: {speaker, opening:[lines], question, options:[{text, requires:[], missing, sets, lines, action()}], doneFlag} */
    L.dialogue = async o => {
      if (o.opening && o.opening.length) await AZ.ui.sayAndWait(o.opening);
      const opts = o.options.map(op => ({ text: op.text + (AZ.all(op.requires) ? "" : "  " + (op.missing || "(you have no proof)")), disabled: !AZ.all(op.requires) }));
      const i = await AZ.ui.choose(o.question || o.speaker, opts);
      const op = o.options[i];
      AZ.set(op.sets);
      if (op.lines && op.lines.length) await AZ.ui.sayAndWait(op.lines);
      if (op.action) op.action();
      AZ.set(o.doneFlag);
      return i;
    };
    /** Talk point that starts a dialogue (on an NPC mesh or an object). */
    L.talkPoint = (o, dialogue) => {
      let done = false;
      return L.interact({ at: o.at, mesh: o.mesh, mask: o.mask || 7, radius: o.radius || 3, prompt: o.prompt || "Talk", requires: o.requires || [], onUse: async it => { if (done || it.busy) return; it.busy = true; await L.dialogue(dialogue); done = !o.repeat; it.busy = false; if (done) it.enabled = false; } });
    };
    /** Arin's hum reaches things within radius. */
    L.onHum = (at, radius, fn, opts = {}) => {
      let fired = false;
      AZ.on("hum", pos => { if (fired && opts.once !== false) return; if (opts.mask && !(opts.mask & BIT(W.state))) return; if (pos.distanceTo(V3(...at)) > radius || !AZ.all(opts.requires || [])) return; fired = true; fn(); });
    };
    /** Cinematic memory: locks controls, tints the world, plays lines. */
    L.memory = async (lines, flag) => {
      AZ.ui.lock(true); R.memory = 1; AZ.sfx("memory");
      await AZ.ui.sayAndWait(lines);
      R.memory = 0; AZ.ui.lock(false); AZ.set(flag);
    };
    L.choice = (q, options) => AZ.ui.choose(q, options);

    // ---------------- state helpers ----------------
    const activeMask = m => (m & BIT(W.state)) !== 0;
    W.activeMask = activeMask;
    return L;
  };

  // =====================================================================
  // Simulation
  // =====================================================================
  const S = (AZ.sim = {});
  const P = (AZ.player = { pos: new THREE.Vector3(), vy: 0, yaw: 0, pitch: 0, height: 1.7, radius: 0.32, grounded: false, crouch: false, sprint: false, stepT: 0, ground: null, dying: false });
  S.input = { mx: 0, my: 0, lookX: 0, lookY: 0, jump: false, crouch: false, echo: false };

  function insideRect(c, x, z, pad) {
    const dx = x - c.x, dz = z - c.z, cs = Math.cos(-c.yaw), sn = Math.sin(-c.yaw);
    const lx = dx * cs + dz * sn * -1 * -1, lz = -dx * sn + dz * cs;
    // rotate world->local: yaw rotates local z toward world (sin yaw, cos yaw)
    const ux = Math.cos(c.yaw), uz = -Math.sin(c.yaw); // local x axis
    const vx = Math.sin(c.yaw), vz = Math.cos(c.yaw);  // local z axis
    const px = dx * ux + dz * uz, pz = dx * vx + dz * vz;
    return { inside: Math.abs(px) <= c.hw + pad && Math.abs(pz) <= c.hd + pad, px, pz, ux, uz, vx, vz };
  }
  function topAt(c, r) { if (!c.ramp) return c.y1; const t = (r.pz + c.hd) / (2 * c.hd); return c.ya + (c.yb - c.ya) * Math.min(1, Math.max(0, t)); }

  S.groundAt = (W, x, y, z, step = 0.45) => {
    let best = -Infinity, bestC = null;
    for (const c of W.colliders) {
      if (!c.on || !W.activeMask(c.mask)) continue;
      const r = insideRect(c, x, z, 0.1);
      if (!r.inside) continue;
      const top = topAt(c, r);
      if (top <= y + step && top > best) { best = top; bestC = c; }
    }
    return { y: best, c: bestC };
  };
  S.overlapsSolid = (W, pos, height, radius, stateMask) => {
    for (const c of W.colliders) {
      if (!c.on || c.ramp || !(c.mask & stateMask)) continue;
      if (c.y1 <= pos.y + 0.45 || c.y0 >= pos.y + height) continue;
      const r = insideRect(c, pos.x, pos.z, radius * 0.8);
      if (r.inside) return true;
    }
    return false;
  };
  function pushOut(W, pos, height, radius) {
    for (let pass = 0; pass < 2; pass++) for (const c of W.colliders) {
      if (!c.on || c.ramp || !W.activeMask(c.mask)) continue;
      if (c.y1 <= pos.y + 0.45 || c.y0 >= pos.y + height - 0.1) continue;
      const r = insideRect(c, pos.x, pos.z, radius);
      if (!r.inside) continue;
      // closest point on rect
      const cx = Math.max(-c.hw, Math.min(c.hw, r.px)), cz = Math.max(-c.hd, Math.min(c.hd, r.pz));
      let nx = r.px - cx, nz = r.pz - cz, d = Math.hypot(nx, nz);
      if (d < 1e-5) { // centre inside: push along shallowest axis
        const ox = c.hw - Math.abs(r.px), oz = c.hd - Math.abs(r.pz);
        if (ox < oz) { nx = Math.sign(r.px) || 1; nz = 0; d = -ox; } else { nx = 0; nz = Math.sign(r.pz) || 1; d = -oz; }
      } else { nx /= d; nz /= d; }
      const push = radius - d;
      if (push <= 0) continue;
      pos.x += (nx * r.ux + nz * r.vx) * push;
      pos.z += (nx * r.uz + nz * r.vz) * push;
    }
  }
  S.losBlocked = (W, a, b) => {
    const steps = Math.ceil(a.distanceTo(b) / 0.4);
    for (let i = 1; i < steps; i++) {
      const t = i / steps, x = a.x + (b.x - a.x) * t, y = a.y + (b.y - a.y) * t, z = a.z + (b.z - a.z) * t;
      for (const c of W.colliders) {
        if (!c.on || c.ramp || c.water || !W.activeMask(c.mask) || y < c.y0 || y > c.y1) continue;
        if (insideRect(c, x, z, 0).inside) return true;
      }
    }
    return false;
  };

  S.step = (W, dt, t) => {
    const inp = S.input, locked = AZ.ui.locked();
    // ---- movers (record collider deltas for riding) ----
    W.movers.forEach(mv => {
      const before = mv.mesh.position.clone(), byaw = mv.mesh.rotation.y;
      mv.fn(t, dt, mv.mesh);
      mv.delta = mv.mesh.position.clone().sub(before); mv.dyaw = mv.mesh.rotation.y - byaw;
      if (mv.collider) { const wp = new THREE.Vector3(); mv.mesh.getWorldPosition(wp); mv.collider.x = wp.x; mv.collider.z = wp.z; const h = mv.collider.y1 - mv.collider.y0; mv.collider.y0 = wp.y - h / 2; mv.collider.y1 = wp.y + h / 2; mv.collider.yaw = mv.mesh.rotation.y; }
    });
    // dynamic colliders follow their meshes (doors)
    W.colliders.forEach(c => { if (c.mesh && !c.moverManaged) { const wp = new THREE.Vector3(); c.mesh.getWorldPosition(wp); c.x = wp.x; c.z = wp.z; const h = c.y1 - c.y0; c.y0 = wp.y - h / 2; c.y1 = wp.y + h / 2; } });

    // ---- player ----
    if (!P.dying) {
      if (!locked) {
        const s = AZ.settings.sens;
        P.yaw -= inp.lookX * 0.0042 * s; P.pitch -= inp.lookY * 0.0042 * s * (AZ.settings.invert ? -1 : 1);
        P.pitch = Math.max(-1.35, Math.min(1.35, P.pitch));
      }
      inp.lookX = inp.lookY = 0;
      let mx = locked ? 0 : inp.mx, my = locked ? 0 : inp.my;
      const mag = Math.hypot(mx, my);
      P.crouch = !locked && inp.crouch;
      P.sprint = !P.crouch && mag > 0.93 && my > 0.3;
      const speed = P.crouch ? 1.8 : P.sprint ? 6.0 : 3.6 * Math.min(1, mag / 0.93 + 0.05);
      if (mag > 1) { mx /= mag; my /= mag; }
      const fx = -Math.sin(P.yaw), fz = -Math.cos(P.yaw), rx = Math.cos(P.yaw), rz = -Math.sin(P.yaw);
      const nm = Math.min(1, mag);
      const dirx = (fx * my + rx * mx), dirz = (fz * my + rz * mx);
      const dl = Math.hypot(dirx, dirz) || 1;
      P.pos.x += dirx / dl * speed * nm * dt; P.pos.z += dirz / dl * speed * nm * dt;
      P.height += ((P.crouch ? 1.1 : 1.7) - P.height) * Math.min(1, dt * 8);
      // ride platforms
      if (P.grounded && P.ground) { const mv = W.movers.find(m => m.collider === P.ground); if (mv && mv.platform && mv.delta) { P.pos.add(mv.delta); P.yaw += mv.dyaw || 0; } }
      pushOut(W, P.pos, P.height, P.radius);
      // gravity + ground
      const g = S.groundAt(W, P.pos.x, P.pos.y, P.pos.z);
      if (inp.jump && P.grounded && !locked) { P.vy = 4.6; P.grounded = false; }
      inp.jump = false;
      P.vy -= 16 * dt;
      P.pos.y += P.vy * dt;
      if (g.y > -Infinity && P.pos.y <= g.y) { P.pos.y = g.y; P.vy = 0; P.grounded = true; P.ground = g.c; }
      else if (g.y > -Infinity && P.grounded && P.pos.y - g.y < 0.5 && P.vy <= 0) { P.pos.y = g.y; P.vy = 0; P.ground = g.c; }
      else { P.grounded = false; P.ground = null; }
      if (P.pos.y < (W.def.killY !== undefined ? W.def.killY : -25)) AZ.game.die("You fell between the states.");
      // footsteps -> noise
      if (P.grounded && nm > 0.1) {
        P.stepT -= dt * speed / 3.6;
        if (P.stepT <= 0) { P.stepT = 0.5; AZ.sfx("step", P.crouch ? 0.08 : P.sprint ? 0.35 : 0.2, 0.9 + Math.random() * 0.2); AZ.emit("noise", P.pos.clone(), P.crouch ? 0.1 : P.sprint ? 1 : 0.45); }
      }
    }
    // camera
    const cam = R.camera;
    const bob = P.grounded ? Math.sin(t * 10) * 0.025 * Math.min(1, Math.hypot(inp.mx, inp.my)) : 0;
    cam.position.set(P.pos.x, P.pos.y + P.height - 0.12 + bob, P.pos.z);
    cam.rotation.set(P.pitch, P.yaw, 0);
    const targetFov = P.sprint ? 78 : 70;
    if (Math.abs(cam.fov - targetFov) > 0.1) { cam.fov += (targetFov - cam.fov) * Math.min(1, dt * 6); cam.updateProjectionMatrix(); }

    // ---- zones ----
    W.zones.forEach(z => {
      const p = P.pos, inside = W.activeMask(z.mask) && p.x >= z.min.x && p.x <= z.max.x && p.z >= z.min.z && p.z <= z.max.z && p.y + 0.5 >= z.min.y && p.y <= z.max.y;
      if (inside && !z.inside && !(z.once && z.fired) && AZ.all(z.requires)) {
        z.fired = true; AZ.set(z.flag);
        if (z.toast) AZ.ui.toast(z.toast, 3.5);
        if (z.lines) AZ.ui.say(z.lines, false);
        if (z.onEnter) z.onEnter(z);
      }
      if (!inside && z.inside && z.onExit) z.onExit(z);
      z.inside = inside;
    });

    // ---- NPCs ----
    W.npcs.forEach(n => {
      const on = W.activeMask(n.mask);
      n.p.group.visible = on;
      if (!on) return;
      const o = n.o, grp = n.p.group;
      const route = o.changedFlag && AZ.get(o.changedFlag) && o.changedRoute ? o.changedRoute : n.route;
      if (route !== n.lastRoute) { n.lastRoute = route; n.idx = 0; n.wait = 0; }
      const dP = Math.hypot(P.pos.x - grp.position.x, P.pos.z - grp.position.z);
      let walking = false, act = "idle";
      if (o.named !== false && dP < 3) {
        const a = Math.atan2(P.pos.x - grp.position.x, P.pos.z - grp.position.z);
        grp.rotation.y += angDiff(a, grp.rotation.y) * Math.min(1, dt * 5);
        if (!n.approached && o.approach && o.approach.length) { n.approached = true; AZ.ui.bark(n.name, o.approach[Math.floor(Math.random() * o.approach.length)]); }
      } else if (route.length) {
        const s = route[n.idx % route.length];
        const tx = s[0], tz = s[1], dx = tx - grp.position.x, dz = tz - grp.position.z, d = Math.hypot(dx, dz);
        if (d > 0.15) {
          const sp = (o.speed || 1.3) * dt;
          grp.position.x += dx / d * Math.min(sp, d); grp.position.z += dz / d * Math.min(sp, d);
          grp.rotation.y += angDiff(Math.atan2(dx, dz), grp.rotation.y) * Math.min(1, dt * 6);
          walking = true;
        } else {
          act = s[3] || "idle";
          n.wait += dt;
          if (n.wait > (s[2] || 3)) { n.wait = 0; n.idx++; }
        }
      }
      if (dP > 7) n.approached = false;
      // procedural animation
      n.phase += dt;
      const sw = walking ? Math.sin(n.phase * 7) * 0.55 : 0;
      let la = sw, ra = -sw;
      if (act === "work") { ra = -1 + Math.sin(n.phase * 9) * 0.6; la = -0.3; }
      else if (act === "talk") ra = -0.4 + Math.sin(n.phase * 2.3) * 0.3;
      else if (act === "carry") { la = ra = -1.2; }
      n.p.armL.rotation.x += (la - n.p.armL.rotation.x) * Math.min(1, dt * 10);
      n.p.armR.rotation.x += (ra - n.p.armR.rotation.x) * Math.min(1, dt * 10);
      n.p.body.position.y = walking ? Math.abs(Math.sin(n.phase * 7)) * 0.04 : Math.sin(n.phase * 1.6) * 0.008;
      if (act === "look") n.p.head.rotation.y = Math.sin(n.phase * 0.7) * 0.8;
      if (n.tag) n.tag.rotation.y = Math.atan2(P.pos.x - grp.position.x, P.pos.z - grp.position.z) - grp.rotation.y;
    });

    // ---- keepers ----
    let maxDet = 0;
    W.keepers.forEach(k => {
      const on = W.activeMask(k.mask) && !AZ.get(k.o.disabledBy);
      k.p.group.visible = on;
      if (!on) { k.det = 0; return; }
      const grp = k.p.group;
      const eye = grp.position.clone().add(V3(0, 1.5, 0));
      const tgt = P.pos.clone().add(V3(0, P.crouch ? 0.8 : 1.4, 0));
      const to = tgt.clone().sub(eye), dist = to.length();
      const range = k.view * (P.crouch ? 0.55 : 1);
      const fwd = V3(Math.sin(grp.rotation.y), 0, Math.cos(grp.rotation.y));
      const flat = V3(to.x, 0, to.z).normalize();
      const sees = !P.dying && dist < range && fwd.angleTo(flat) < (k.angle * Math.PI / 360) && !S.losBlocked(W, eye, tgt);
      k.cone.material.opacity = 0.07 + k.det * 0.12;
      k.cone.material.color.setHex(k.chasing ? 0xff4030 : k.det > 0.3 ? 0xffa050 : 0xffc070);
      k.barkCd -= dt;
      const bark = list => { if (k.barkCd > 0) return; k.barkCd = 6; AZ.ui.bark(k.name, list[Math.floor(Math.random() * list.length)]); };
      if (k.mode === "watcher") {
        if (sees && !k.wasSeeing) { k.sawAt = t; }
        k.wasSeeing = sees; k.seesNow = sees;
      } else {
        if (sees) k.det = Math.min(1, k.det + dt / k.detectTime); else if (!k.chasing) k.det = Math.max(0, k.det - dt * 0.35);
        if (sees && k.det > 0.35 && k.det < 1) { k.inv = P.pos.clone(); bark(k.o.spot || ["Who's there?", "Something moved."]); }
        if (k.det >= 1 && !k.chasing) { k.chasing = true; k.lost = 0; bark(k.o.alarm || ["Anomaly! Stop!", "The one who remembers!"]); AZ.sfx("alert"); AZ.vibrate(80); AZ.set(k.o.alarmFlag); AZ.emit("alarm", k); }
      }
      maxDet = Math.max(maxDet, k.chasing ? 1 : k.det);
      const moveTo = (x, z, sp) => { const dx = x - grp.position.x, dz = z - grp.position.z, d = Math.hypot(dx, dz); if (d < 0.3) return true; grp.position.x += dx / d * Math.min(sp * dt, d); grp.position.z += dz / d * Math.min(sp * dt, d); grp.rotation.y += angDiff(Math.atan2(dx, dz), grp.rotation.y) * Math.min(1, dt * 5); return false; };
      if (k.chasing) {
        k.lost = sees ? 0 : k.lost + dt;
        if (k.lost > 4) { k.chasing = false; k.det = 0.5; k.inv = P.pos.clone(); }
        else { moveTo(P.pos.x, P.pos.z, k.chaseSpeed); if (Math.hypot(P.pos.x - grp.position.x, P.pos.z - grp.position.z) < 1.2) { AZ.game.die(k.o.caught || "The Keepers caught you. Stay out of the lantern light - crouch to shrink their sight."); } }
      } else if (k.inv) { if (moveTo(k.inv.x, k.inv.z, k.speed * 1.4)) { k.inv = null; k.wait = 2.5; } }
      else if (k.path.length) {
        if (k.wait > 0) { k.wait -= dt; grp.rotation.y += Math.sin(t * 0.9) * dt * 0.8; }
        else if (moveTo(k.path[k.idx][0], k.path[k.idx][1], k.speed)) { k.idx = (k.idx + 1) % k.path.length; k.wait = k.o.pause || 2; }
      }
      // animate legs/arms
      const sw = Math.sin(t * 7) * 0.5; k.p.armL.rotation.x = sw; k.p.armR.rotation.x = -0.9;
    });
    AZ.ui.detection(maxDet);

    // ---- pursuers ----
    W.pursuers.forEach(c => {
      c.p.group.visible = W.activeMask(c.mask);
      if (!c.running || P.dying) return;
      if (c.delay > 0) { c.delay -= dt; return; }
      const grp = c.p.group, d = Math.hypot(P.pos.x - grp.position.x, P.pos.z - grp.position.z);
      const s = (d > 14 ? c.speed * 1.4 : c.speed) * dt;
      const tgt = c.idx < c.lane.length ? c.lane[c.idx] : [P.pos.x, P.pos.z];
      const dx = tgt[0] - grp.position.x, dz = tgt[1] - grp.position.z, dd = Math.hypot(dx, dz);
      if (dd < 0.4 && c.idx < c.lane.length) c.idx++;
      else if (dd > 0.01) { grp.position.x += dx / dd * Math.min(s, dd); grp.position.z += dz / dd * Math.min(s, dd); grp.rotation.y = Math.atan2(dx, dz); }
      c.p.armL.rotation.x = Math.sin(t * 9) * 0.7; c.p.armR.rotation.x = -Math.sin(t * 9) * 0.7;
      c.bark -= dt; if (c.bark <= 0 && c.o.barks) { c.bark = 7; AZ.ui.bark(c.o.name, c.o.barks[Math.floor(Math.random() * c.o.barks.length)]); }
      if (d < 1.3 && W.activeMask(c.mask)) AZ.game.die(c.o.caught || (c.o.name + " caught you."));
    });

    // ---- hazards ----
    W.hazards.forEach(h => {
      const on = W.activeMask(h.mask) && !AZ.get(h.disabledBy) && AZ.all(h.requires || []);
      if (!on) { h.phase = "idle"; h.light.intensity = 0; h.marker.material.opacity = 0; return; }
      h.timer -= dt;
      if (h.timer <= 0) {
        if (h.phase === "idle") { h.phase = "warn"; h.timer = h.warn; AZ.sfxAt("warn", V3(...h.at), 0.8); }
        else if (h.phase === "warn") { h.phase = "active"; h.timer = h.active; AZ.sfxAt("hazard", V3(...h.at), 0.9); if (h.visual) h.visual.position.y = h.visualBase.y - (h.drop || 0); }
        else { h.phase = "idle"; h.timer = h.idle; if (h.visual) h.visual.position.copy(h.visualBase); }
      }
      if (h.phase === "warn") { const k = 1 - h.timer / h.warn; h.light.intensity = 3 * (0.5 + 0.5 * Math.sin(t * (8 + k * 22))); h.marker.material.opacity = 0.15 + 0.25 * k; if (h.visual) h.visual.position.x = h.visualBase.x + (Math.random() - 0.5) * 0.04 * k; }
      else if (h.phase === "active") { h.light.intensity = 4; h.marker.material.opacity = 0.5; const p = P.pos; if (p.x > h.min.x && p.x < h.max.x && p.z > h.min.z && p.z < h.max.z && p.y + 1 > h.min.y && p.y < h.max.y) AZ.game.die(h.msg); }
      else { h.light.intensity = 0; h.marker.material.opacity = 0; }
    });

    // ---- fires ----
    W.fires.forEach(f => {
      if (!f.running) return;
      f.timer += dt;
      const next = f.front + 1;
      if (next < f.nodes.length) {
        const n = f.nodes[next], until = f.interval - f.timer;
        if (until < f.telegraph && !AZ.get(n.out)) n.light.intensity = (1 - until / f.telegraph) * 1.5 * (0.7 + 0.3 * Math.sin(t * 20));
        if (f.timer >= f.interval) { f.timer = 0; f.front = next; f.ignite(next); }
      }
      f.nodes.forEach(n => { if (n.burning && Math.hypot(P.pos.x - n.at[0], P.pos.z - n.at[2]) < 1.5 && Math.abs(P.pos.y - n.at[1]) < 2) AZ.game.die("Caught by the fire. Watch the smoke and glow before a shelf ignites."); });
    });

    // ---- loop ----
    const lp = W.loop;
    if (lp && lp.running) {
      if (lp.breakFlag && AZ.get(lp.breakFlag)) lp.running = false;
      else {
        lp.elapsed += dt;
        (lp.events || []).forEach((e, i) => { if (!lp.fired[i] && lp.elapsed >= e.t) { lp.fired[i] = true; e.fire && e.fire(lp); } });
        if (lp.elapsed >= lp.duration) {
          lp.elapsed = 0; lp.count++; lp.fired = {};
          AZ.clearLoopFlags();
          (lp.events || []).forEach(e => e.reset && e.reset(lp));
          W.resetters.forEach(fn => fn());
          AZ.sfx("loop"); R.ripple(1.4); AZ.emit("loopReset", lp.count);
        }
      }
    }
    AZ.ui.loopBar(lp && lp.running ? lp : null);

    // ---- timer ----
    const tm = W.timer;
    if (tm && tm.running && !tm.finished) {
      const before = Math.ceil(tm.remaining);
      tm.remaining -= dt;
      if (tm.remaining < 30 && Math.ceil(tm.remaining) !== before) AZ.sfx("tick");
      AZ.ui.timer(tm.remaining, tm.label);
      if (tm.remaining <= 0) { tm.finished = true; tm.running = false; AZ.ui.timer(-1); AZ.sfx("bell"); R.ripple(2); AZ.ui.toast(tm.failMessage || "The bell rings a second time. The cycle begins again.", 4); setTimeout(() => AZ.game.restartLevel(), 3200); }
    }

    // ---- custom updaters ----
    for (const fn of W.updaters) fn(dt, t);
    R.animateParticles(W.root, dt, t);
  };

  function angDiff(a, b) { let d = a - b; while (d > Math.PI) d -= Math.PI * 2; while (d < -Math.PI) d += Math.PI * 2; return d; }
  AZ.angDiff = angDiff;

  /** Interactable in focus: nearest usable within reach, roughly in front of the camera. */
  S.focus = W => {
    let best = null, bestScore = Infinity;
    const cam = R.camera, fwd = new THREE.Vector3(0, 0, -1).applyQuaternion(cam.quaternion);
    for (const it of W.interactables) {
      if (!it.enabled || !W.activeMask(it.mask) || (it.visibleIf && !it.visibleIf())) continue;
      if (it.mesh && !isVisible(it.mesh)) continue;
      const to = it.pos.clone().sub(cam.position);
      const d = to.length();
      if (d > it.radius + 0.6) continue;
      const ang = fwd.angleTo(to.normalize());
      if (ang > 0.9 && d > 1.2) continue;
      const score = d + ang * 3;
      if (score < bestScore) { bestScore = score; best = it; }
    }
    return best;
  };
  function isVisible(o) { while (o) { if (!o.visible) return false; o = o.parent; } return true; }
  S.use = async (W, it) => {
    if (!it) return;
    if (!AZ.all(it.requires)) { AZ.ui.toast(it.locked || "Not yet.", 2.5); AZ.sfx("denied"); return; }
    if (it.once && it.used) return;
    it.used = true;
    try { await it.onUse(it); } catch (e) { console.error(e); }
    AZ.emit("interacted", it);
    if (it.once) it.enabled = false;
  };
  /** Tap-to-use: raycast from a screen point to interactive meshes. */
  const ray = new THREE.Raycaster();
  S.pick = (W, nx, ny) => {
    ray.setFromCamera({ x: nx, y: ny }, R.camera);
    const meshes = [];
    W.interactables.forEach(it => { if (it.mesh && it.enabled && W.activeMask(it.mask)) it.mesh.traverse(o => { if (o.isMesh) meshes.push(o); }); });
    const hits = ray.intersectObjects(meshes, false);
    for (const h of hits) { const it = h.object.userData.interact || findIt(W, h.object); if (it && h.distance < it.radius + 2.5 && isVisible(h.object)) return it; }
    return null;
  };
  function findIt(W, o) { while (o) { const it = W.interactables.find(i => i.mesh === o); if (it) return it; o = o.parent; } return null; }
})();
