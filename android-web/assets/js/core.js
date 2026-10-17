/* AFTERZERO: The First Cycle — core: namespace, flags, save, settings, difficulty, audio, haptics. */
(function () {
  "use strict";
  const AZ = (window.AZ = window.AZ || {});
  AZ.version = "1.0.0";
  AZ.levels = [];
  AZ.registerLevel = def => { AZ.levels.push(def); AZ.levels.sort((a, b) => a.order - b.order); };

  // ---------------- events ----------------
  const handlers = {};
  AZ.on = (ev, fn) => { (handlers[ev] = handlers[ev] || []).push(fn); return () => AZ.off(ev, fn); };
  AZ.off = (ev, fn) => { const h = handlers[ev]; if (h) { const i = h.indexOf(fn); if (i >= 0) h.splice(i, 1); } };
  AZ.emit = (ev, ...a) => { (handlers[ev] || []).slice().forEach(fn => { try { fn(...a); } catch (e) { console.error(e); } }); };
  AZ.clearLevelHandlers = () => { for (const k in handlers) handlers[k] = handlers[k].filter(f => f.persistent); };
  AZ.persistent = fn => { fn.persistent = true; return fn; };

  // ---------------- world state (causal memory) ----------------
  AZ.flags = {};
  AZ.journal = [];
  AZ.get = f => !!(f && AZ.flags[f]);
  AZ.all = list => !list || list.every(f => !f || AZ.flags[f]);
  AZ.set = (f, v = true) => {
    if (!f) return;
    if (!!AZ.flags[f] === !!v) return;
    if (v) AZ.flags[f] = true; else delete AZ.flags[f];
    AZ.emit("flag", f, !!v);
  };
  AZ.clearLoopFlags = () => Object.keys(AZ.flags).forEach(f => { if (f.startsWith("loop.")) AZ.set(f, false); });
  AZ.addJournal = (id, title, body) => {
    if (!id || AZ.journal.some(j => j.id === id)) return;
    AZ.journal.push({ id, title, body });
    AZ.set("clue." + id);
    AZ.emit("journal", { id, title, body });
  };
  AZ.snapshot = () => ({ flags: Object.assign({}, AZ.flags), journal: AZ.journal.slice() });
  AZ.restore = s => {
    AZ.flags = Object.assign({}, (s && s.flags) || {});
    AZ.journal = ((s && s.journal) || []).slice();
  };

  // ---------------- storage (WebView localStorage, guarded) ----------------
  const store = {
    get(k) { try { const v = localStorage.getItem(k); return v ? JSON.parse(v) : null; } catch (e) { return null; } },
    set(k, v) { try { localStorage.setItem(k, JSON.stringify(v)); } catch (e) { } },
    del(k) { try { localStorage.removeItem(k); } catch (e) { } },
  };
  AZ.save = {
    has: () => !!store.get("az1_save"),
    load: () => store.get("az1_save"),
    write: levelIndex => store.set("az1_save", { level: levelIndex, world: AZ.snapshot(), at: Date.now() }),
    clear: () => store.del("az1_save"),
  };

  // ---------------- settings + difficulty ----------------
  const defaults = { difficulty: 1, sens: 1, invert: false, quality: -1, left: false, vibe: true, sub: 1, volume: 0.9, cam: 1, arin: "f" };
  AZ.settings = Object.assign({}, defaults, store.get("az1_settings") || {});
  AZ.saveSettings = () => { store.set("az1_settings", AZ.settings); AZ.emit("settings"); };
  AZ.diff = {
    label: d => ["STORY - generous timers, early hints", "NORMAL - the intended challenge", "HARD - no hints, short timers, sharp enemies"][d],
    hintDelay: () => [45, 120, Infinity][AZ.settings.difficulty],
    timer: () => [1.5, 1, 0.75][AZ.settings.difficulty],
    perception: () => [0.6, 1, 1.35][AZ.settings.difficulty],
    climaxCheckpoints: () => AZ.settings.difficulty !== 2,
    decoys: () => [Infinity, 3, 2][AZ.settings.difficulty],
  };

  // ---------------- haptics ----------------
  let lastVibe = 0;
  AZ.vibrate = (ms = 30) => {
    if (!AZ.settings.vibe) return;
    const now = performance.now();
    if (now - lastVibe < 200) return;
    lastVibe = now;
    try {
      if (window.AZNative && AZNative.vibrate) AZNative.vibrate(ms);
      else if (navigator.vibrate) navigator.vibrate(ms);
    } catch (e) { }
  };

  // ---------------- procedural audio ----------------
  let ctx = null, master = null;
  const buffers = {};
  AZ.audioUnlock = () => {
    if (!ctx) {
      try {
        ctx = new (window.AudioContext || window.webkitAudioContext)();
        master = ctx.createGain();
        master.gain.value = AZ.settings.volume;
        master.connect(ctx.destination);
      } catch (e) { ctx = null; }
    }
    if (ctx && ctx.state === "suspended") ctx.resume();
  };
  AZ.on("settings", AZ.persistent(() => { if (master) master.gain.value = AZ.settings.volume; }));
  AZ.audioPause = on => { if (ctx) { if (on) ctx.suspend(); else ctx.resume(); } };

  const RATE = 22050;
  function makeBuffer(seconds, fill) {
    const n = Math.max(1, Math.floor(seconds * RATE));
    const d = new Float32Array(n);
    fill(d, n);
    let peak = 1e-4; for (let i = 0; i < n; i++) peak = Math.max(peak, Math.abs(d[i]));
    const g = 0.9 / peak; for (let i = 0; i < n; i++) d[i] *= g;
    const b = ctx.createBuffer(1, n, RATE);
    b.copyToChannel ? b.copyToChannel(d, 0) : b.getChannelData(0).set(d);
    return b;
  }
  const env = (t, a, dcy) => (t < a ? t / a : Math.exp(-(t - a) / dcy));
  let seed = 1337;
  const noise = () => { seed = (seed * 16807) % 2147483647; return seed / 1073741823.5 - 1; };
  const TAU = Math.PI * 2;
  const SYN = {
    bell: (base = 196, sec = 5) => makeBuffer(sec, (d, n) => {
      const p = [0.5, 1, 1.183, 1.506, 2, 2.514, 2.662, 3.011, 4.166], a = [0.6, 1, 0.6, 0.5, 0.45, 0.3, 0.25, 0.2, 0.12];
      for (let i = 0; i < n; i++) { const t = i / RATE; let s = 0; for (let k = 0; k < p.length; k++) s += a[k] * Math.sin(TAU * base * p[k] * t) * Math.exp(-t * (0.6 + k * 0.45)); d[i] = s * env(t, 0.002, 10); }
    }),
    chime: (notes, gap, dcy, fm = 0) => makeBuffer(gap * notes.length + dcy * 3, (d, n) => {
      notes.forEach((hz, k) => { const st = Math.floor(k * gap * RATE); for (let i = st; i < n; i++) { const t = (i - st) / RATE; const m = fm ? Math.sin(TAU * hz * 2 * t) * fm * Math.exp(-t * 3) : 0; d[i] += Math.sin(TAU * hz * t + m) * env(t, 0.004, dcy) * 0.6 + Math.sin(TAU * hz * 2.01 * t) * env(t, 0.002, dcy * 0.4) * 0.2; } });
    }),
    sweep: (f0, f1, sec, nz, glitch = 0) => makeBuffer(sec, (d, n) => {
      let ph = 0; for (let i = 0; i < n; i++) { const t = i / RATE, k = t / sec; ph += TAU * (f0 + (f1 - f0) * k * k) / RATE; let s = Math.sin(ph) + 0.35 * Math.sin(ph * 1.5); if (glitch && ((i / 550) | 0) % 5 === 0) s = Math.sign(s) * glitch; s = s * (1 - nz) + noise() * nz; d[i] = s * Math.sin(Math.PI * k); }
    }),
    burst: (sec, lp, dcy, thump = 0) => makeBuffer(sec, (d, n) => {
      let y = 0; for (let i = 0; i < n; i++) { const t = i / RATE; y += lp * (noise() - y); let s = y; if (thump) s += Math.sin(TAU * thump * t * (1 - t)) * 1.5 * Math.exp(-t * 18); d[i] = s * env(t, 0.002, dcy); }
    }),
    tone: (hz, sec, square, dcy) => makeBuffer(sec, (d, n) => { for (let i = 0; i < n; i++) { const t = i / RATE; let s = Math.sin(TAU * hz * t); if (square) s = Math.sign(s) * 0.6; d[i] = s * env(t, 0.003, dcy); } }),
    pad: (chord, sec) => makeBuffer(sec, (d, n) => { for (let i = 0; i < n; i++) { const t = i / RATE, k = t / sec; let s = 0; chord.forEach(hz => { s += Math.sin(TAU * hz * t + 0.3 * Math.sin(TAU * 0.25 * t)) * 0.4 + Math.sin(TAU * hz * 1.003 * t) * 0.3; }); d[i] = s * Math.sin(Math.PI * k); } }),
    shatter: () => makeBuffer(1.2, (d, n) => { for (let b = 0; b < 9; b++) { const st = Math.floor((b ? 0.02 + Math.random() * 0.5 : 0) * RATE), hz = 2500 + Math.random() * 4500; for (let i = st; i < n; i++) { const t = (i - st) / RATE; d[i] += (noise() * 0.5 + Math.sin(TAU * hz * t)) * env(t, 0.001, b ? 0.05 : 0.08) * (b ? 0.5 : 1); } } }),
    metal: (base, sec) => makeBuffer(sec, (d, n) => { const p = [1, 2.32, 4.25, 6.63, 9.38]; for (let i = 0; i < n; i++) { const t = i / RATE; let s = 0; p.forEach((q, k) => { s += Math.sin(TAU * base * q * t) * Math.exp(-t * (3 + k * 2)) / (k + 1); }); s += noise() * Math.exp(-t * 60) * 0.6; d[i] = s; } }),
    murmur: sec => makeBuffer(sec, (d, n) => { let ph = 0; for (let i = 0; i < n; i++) { const t = i / RATE; const syl = 0.5 + 0.5 * Math.sin(TAU * 4.3 * t + Math.sin(t * 1.7) * 2); ph += TAU * (140 + 30 * Math.sin(t * 2.1) + 15 * Math.sin(t * 5.3)) / RATE; d[i] = (Math.sin(ph) + 0.5 * Math.sin(ph * 2) + 0.25 * Math.sin(ph * 3)) * syl * syl * 0.5; } }),
    ambient: (kind, sec = 6) => makeBuffer(sec, (d, n) => {
      let lp = 0, lp2 = 0;
      for (let i = 0; i < n; i++) {
        const t = i / RATE, k = t / sec, z = noise(); let s = 0;
        if (kind === "rain") { lp += 0.45 * (z - lp); s = lp * 0.6 + (Math.random() < 0.0018 ? noise() * 2.5 : 0); }
        else if (kind === "crowd") { lp += 0.06 * (z - lp); s = lp * 1.6 * (0.7 + 0.3 * Math.sin(TAU * 3 * k)) + Math.sin(TAU * (180 + 40 * Math.sin(TAU * 7 * k)) * t) * 0.05 * (0.5 + 0.5 * Math.sin(TAU * 11 * k)); }
        else if (kind === "machine") { s = Math.sin(TAU * 55 * t) * 0.5 + Math.sin(TAU * 110 * t) * 0.25 + Math.sin(TAU * 165 * t) * 0.1; lp += 0.03 * (z - lp); s += lp * 0.8; if ((t * 2) % 1 < 0.02) s += z * 1.2; }
        else if (kind === "water") { lp += 0.1 * (z - lp); lp2 += 0.015 * (lp - lp2); s = lp2 * 6 * (0.6 + 0.4 * Math.sin(TAU * 2 * k)); }
        else if (kind === "wind") { lp += (0.015 + 0.03 * (0.5 + 0.5 * Math.sin(TAU * 2 * k))) * (z - lp); s = lp * 5; }
        else if (kind === "drone") { s = Math.sin(TAU * 41.2 * t) * 0.5 + Math.sin(TAU * 61.7 * t + Math.sin(TAU * k) * 3) * 0.3 + Math.sin(TAU * 329.6 * t) * 0.04 * (0.5 + 0.5 * Math.sin(TAU * 4 * k)); lp += 0.04 * (z - lp); s += lp * 0.3; }
        else if (kind === "nursery") { s = Math.sin(TAU * 523.25 * t) * 0.05 * (0.5 + 0.5 * Math.sin(TAU * 2 * k)) + Math.sin(TAU * 659.25 * t) * 0.04 * (0.5 + 0.5 * Math.sin(TAU * 3 * k)) + Math.sin(TAU * 130.8 * t) * 0.1; }
        else if (kind === "fire") { lp += 0.25 * (z - lp); s = lp * (0.6 + 0.4 * Math.random()) + (Math.random() < 0.003 ? z * 2 : 0); }
        d[i] = s;
      }
      const fade = RATE / 10; for (let i = 0; i < fade; i++) { const w = i / fade; d[i] = d[i] * w + d[n - fade + i] * (1 - w); }
    }),
  };
  const CUES = {
    use: () => SYN.chime([880], 0.05, 0.08), denied: () => SYN.chime([220, 196], 0.09, 0.08), pickup: () => SYN.chime([660, 990], 0.05, 0.06),
    place: () => SYN.metal(320, 0.5), click: () => SYN.burst(0.06, 0.9, 0.01), clang: () => SYN.metal(180, 1.4), scrape: () => SYN.burst(0.5, 0.15, 0.25),
    shatter: () => SYN.shatter(), step: () => SYN.burst(0.12, 0.25, 0.03, 90), rule: () => SYN.chime([523.25, 783.99, 1046.5, 1567.98], 0.07, 0.5, 0.6),
    past: () => SYN.chime([587.33, 440, 349.23], 0.08, 0.6), present: () => SYN.chime([392, 523.25], 0.06, 0.4, 0.3), future: () => SYN.sweep(180, 1400, 0.55, 0.15, 0.6),
    blocked: () => SYN.tone(110, 0.35, true, 0.12), loop: () => SYN.sweep(900, 60, 1.2, 0.35), objective: () => SYN.chime([659.25, 830.61, 987.77], 0.1, 0.7),
    solved: () => SYN.chime([523.25, 659.25, 783.99, 1046.5], 0.12, 0.9, 0.4), checkpoint: () => SYN.chime([783.99, 1174.66], 0.08, 0.5),
    exit: () => SYN.pad([261.63, 392, 523.25], 2.5), hurt: () => SYN.burst(0.6, 0.08, 0.2, 60), warn: () => SYN.sweep(300, 900, 0.8, 0.05),
    hazard: () => SYN.burst(1, 0.3, 0.35, 50), alert: () => SYN.chime([1200, 900, 1200, 900], 0.09, 0.06, 1.5), water: () => SYN.burst(2.5, 0.06, 1.2),
    bell: () => SYN.bell(196, 6), tick: () => SYN.burst(0.04, 0.95, 0.006), memory: () => SYN.pad([220, 277.18, 329.63, 415.3], 3.5),
    murmur: () => SYN.murmur(1.4), reveal: () => SYN.pad([146.83, 220, 293.66, 349.23, 440], 4), note: () => SYN.chime([523.25], 0.01, 0.6, 0.2),
    bellnote: () => SYN.bell(261.63, 3), watch: () => SYN.burst(0.03, 0.95, 0.004),
  };
  AZ.sfx = (name, vol = 1, rate = 1) => {
    if (!ctx) return;
    try {
      if (!buffers[name]) buffers[name] = CUES[name] ? CUES[name]() : null;
      if (!buffers[name]) return;
      const src = ctx.createBufferSource(); src.buffer = buffers[name]; src.playbackRate.value = rate;
      const g = ctx.createGain(); g.gain.value = vol; src.connect(g); g.connect(master); src.start();
    } catch (e) { }
  };
  /** Positional-ish: volume falls off with distance from the listener. */
  AZ.sfxAt = (name, pos, vol = 1, rate = 1) => {
    const p = AZ.player && AZ.player.pos;
    if (!p || !pos) return AZ.sfx(name, vol, rate);
    const d = Math.hypot(p.x - pos.x, p.y - pos.y, p.z - pos.z);
    const k = Math.max(0, 1 - d / 32);
    if (k > 0.02) AZ.sfx(name, vol * k, rate);
  };
  AZ.note = (pitch, bell) => AZ.sfx(bell ? "bellnote" : "note", 0.9, pitch);

  // Looping ambiences, each audible in a set of states, cross-faded on shift.
  const ambs = [];
  AZ.ambient = (kind, vol = 0.3, mask = 7) => {
    const a = { kind, vol, mask, gain: null, src: null };
    ambs.push(a);
    startAmb(a);
    return a;
  };
  function startAmb(a) {
    if (!ctx || a.src) return;
    const key = "amb_" + a.kind;
    if (!buffers[key]) buffers[key] = SYN.ambient(a.kind);
    a.src = ctx.createBufferSource(); a.src.buffer = buffers[key]; a.src.loop = true;
    a.gain = ctx.createGain(); a.gain.gain.value = 0;
    a.src.connect(a.gain); a.gain.connect(master);
    a.src.start(0, Math.random() * 4);
  }
  AZ.updateAmbient = stateBit => {
    ambs.forEach(a => {
      if (!a.src) startAmb(a);
      if (!a.gain) return;
      const target = (a.mask & stateBit) ? a.vol : 0;
      a.gain.gain.value += (target - a.gain.gain.value) * 0.05;
    });
  };
  AZ.stopAmbient = () => { ambs.forEach(a => { try { a.src && a.src.stop(); } catch (e) { } }); ambs.length = 0; };
})();
