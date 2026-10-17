/* AFTERZERO — touch controls, HUD, dialogue, menus. Touch-first: everything is reachable with thumbs. */
(function () {
  "use strict";
  const AZ = window.AZ;
  const UI = (AZ.ui = {});
  const $ = id => document.getElementById(id);
  const el = (tag, cls, html, parent) => { const e = document.createElement(tag); if (cls) e.className = cls; if (html !== undefined) e.innerHTML = html; (parent || document.body).appendChild(e); return e; };
  const esc = s => String(s).replace(/[&<>]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;" }[c]));

  let lockCount = 0;
  UI.lock = on => { lockCount = Math.max(0, lockCount + (on ? 1 : -1)); document.body.classList.toggle("locked", lockCount > 0); };
  UI.locked = () => lockCount > 0 || UI.paused;
  UI.resetLocks = () => { lockCount = 0; document.body.classList.remove("locked"); };

  // =====================================================================
  // HUD
  // =====================================================================
  UI.build = () => {
    const hud = $("hud");
    hud.innerHTML = `
      <div id="obj"></div>
      <div id="states"><span data-s="0">&lt;&lt; PAST</span><span data-s="1">[ PRESENT ]</span><span data-s="2">FUTURE &gt;&gt;</span></div>
      <div id="timer"></div>
      <div id="detect"><i></i><b></b></div>
      <div id="toast"></div>
      <div id="prompt"></div>
      <div id="held"></div>
      <div id="sub"></div>
      <div id="loopbar"><i></i><span></span></div>
      <div id="card"><small></small><strong></strong><em></em></div>
      <div id="reveal"><small>REVELATION</small><strong></strong><p></p></div>
    `;
    buildTouch();
    buildPanels();
  };

  UI.objective = text => { $("obj").innerHTML = text ? "OBJECTIVE<b>" + esc(text) + "</b>" : ""; };
  UI.refreshStates = () => {
    const W = AZ.game.world(); if (!W) return;
    document.querySelectorAll("#states span").forEach(s => {
      const i = +s.dataset.s, allowed = (W.allowed & (1 << i)) !== 0;
      s.classList.toggle("on", W.state === i); s.classList.toggle("off", !allowed);
    });
    document.querySelectorAll(".tb.state").forEach(b => {
      const i = +b.dataset.s, allowed = (W.allowed & (1 << i)) !== 0;
      b.classList.toggle("on", W.state === i); b.classList.toggle("off", !allowed);
    });
  };
  let toastT = 0;
  UI.toast = (msg, sec = 3) => { const t = $("toast"); t.textContent = msg; t.classList.add("show"); clearTimeout(toastT); toastT = setTimeout(() => t.classList.remove("show"), sec * 1000); };
  UI.prompt = text => { $("prompt").textContent = text || ""; $("bUse").classList.toggle("ready", !!text); };
  UI.refreshHeld = () => { const W = AZ.game.world(); $("held").textContent = W && W.held ? "Carrying: " + W.held.name : ""; };
  UI.timer = (sec, label) => {
    const t = $("timer");
    if (sec < 0) { t.textContent = ""; return; }
    const s = Math.max(0, Math.ceil(sec));
    t.textContent = (label || "SECOND BELL IN") + "  " + Math.floor(s / 60) + ":" + String(s % 60).padStart(2, "0");
    t.classList.toggle("urgent", sec < 30);
  };
  UI.detection = d => {
    const e = $("detect");
    e.style.display = d > 0.01 ? "block" : "none";
    e.firstElementChild.style.width = Math.round(d * 100) + "%";
    e.classList.toggle("seen", d >= 1);
    e.lastElementChild.textContent = d >= 1 ? "SEEN - BREAK LINE OF SIGHT" : d > 0.35 ? "SUSPICIOUS" : "NOTICED";
  };
  UI.loopBar = lp => {
    const b = $("loopbar");
    if (!lp) { b.style.display = "none"; return; }
    b.style.display = "block";
    b.firstElementChild.style.width = (lp.elapsed / lp.duration * 100).toFixed(1) + "%";
    b.lastElementChild.textContent = "RECURRENCE " + String(lp.count + 1).padStart(2, "0") + "  -  resets in " + Math.ceil(lp.duration - lp.elapsed) + "s";
  };
  UI.titleCard = (a, b, c) => new Promise(res => {
    const card = $("card");
    card.querySelector("small").textContent = a; card.querySelector("strong").textContent = b; card.querySelector("em").textContent = c;
    card.classList.add("show");
    setTimeout(() => { card.classList.remove("show"); res(); }, 3000);
  });
  UI.revelation = (title, text) => {
    const r = $("reveal");
    r.querySelector("strong").textContent = title; r.querySelector("p").textContent = text;
    r.classList.add("show"); AZ.sfx("reveal");
    setTimeout(() => r.classList.remove("show"), 6500);
  };

  // ---------------- subtitles ----------------
  const queue = [];
  let speaking = false, skipFn = null;
  UI.say = (lines, interrupt = true) => {
    if (interrupt) { queue.length = 0; if (skipFn) skipFn(true); }
    lines.forEach(l => { if (l) queue.push(typeof l === "string" ? { speaker: "", text: l, dur: 3 } : l); });
    if (!speaking) pump();
    if (lines.length) AZ.sfx("murmur", 0.12, 0.9 + Math.random() * 0.2);
  };
  UI.sayAndWait = lines => new Promise(res => { UI.say(lines.concat([{ speaker: "", text: "", dur: 0.01, done: res }])); });
  UI.bark = (speaker, text) => { if (!speaking) UI.say([{ speaker, text, dur: 2.6 }], false); };
  function pump() {
    const s = $("sub");
    const l = queue.shift();
    if (!l) { speaking = false; s.innerHTML = ""; s.classList.remove("show"); return; }
    speaking = true;
    if (l.done) { l.done(); return pump(); }
    s.innerHTML = (l.speaker ? "<b>" + esc(l.speaker.toUpperCase()) + "</b>" : "") + esc(l.text);
    s.classList.add("show");
    const dur = (l.dur || Math.max(2.4, Math.min(6.5, l.text.length * 0.06))) * 1000;
    let done = false;
    const next = cleared => { if (done) return; done = true; clearTimeout(timer); skipFn = null; if (!cleared) pump(); else { speaking = false; } };
    const timer = setTimeout(() => next(false), dur);
    skipFn = next;
  }
  UI.skipLine = () => { if (skipFn) { const f = skipFn; f(false); } };
  UI.speaking = () => speaking;

  // =====================================================================
  // Touch controls
  // =====================================================================
  const S = () => AZ.sim.input;
  function buildTouch() {
    const t = $("touch");
    t.innerHTML = `
      <div id="lookpad"></div>
      <div id="stick"><i></i></div>
      <button class="tb" id="bUse">USE</button>
      <button class="tb small" id="bJump">JUMP</button>
      <button class="tb small" id="bCrouch">CROUCH</button>
      <button class="tb small" id="bEcho">ECHO</button>
      <button class="tb small" id="bHum">HUM</button>
      <div id="statebar">
        <button class="tb state" data-s="0">&lt;&lt;<br>PAST</button>
        <button class="tb state" data-s="1">[ ]<br>NOW</button>
        <button class="tb state" data-s="2">&gt;&gt;<br>FUTURE</button>
      </div>
      <button class="tb sys" id="bJournal">JOURNAL</button>
      <button class="tb sys" id="bPause">II</button>
    `;
    const look = $("lookpad"), stick = $("stick"), knob = stick.firstElementChild;
    let stickId = null, sx = 0, sy = 0, lookId = null, lx = 0, ly = 0, tapStart = 0, tapX = 0, tapY = 0, moved = 0;
    const R = 62;
    // Dynamic joystick: appears where the left thumb lands (left 45% of the screen).
    look.addEventListener("pointerdown", e => {
      AZ.audioUnlock();
      const leftZone = AZ.settings.left ? e.clientX > innerWidth * 0.55 : e.clientX < innerWidth * 0.45;
      if (leftZone && stickId === null) {
        stickId = e.pointerId; sx = e.clientX; sy = e.clientY;
        stick.style.left = (sx - 75) + "px"; stick.style.top = (sy - 75) + "px"; stick.classList.add("on");
      } else if (lookId === null) {
        lookId = e.pointerId; lx = e.clientX; ly = e.clientY; tapStart = performance.now(); tapX = lx; tapY = ly; moved = 0;
      }
      try { look.setPointerCapture(e.pointerId); } catch (err) { }
    });
    look.addEventListener("pointermove", e => {
      if (e.pointerId === stickId) {
        let dx = e.clientX - sx, dy = e.clientY - sy; const m = Math.hypot(dx, dy);
        if (m > R) { dx = dx / m * R; dy = dy / m * R; }
        knob.style.transform = `translate(${dx}px,${dy}px)`;
        S().mx = dx / R; S().my = -dy / R;
      } else if (e.pointerId === lookId) {
        const dx = e.clientX - lx, dy = e.clientY - ly;
        // gentle acceleration: small drags precise, fast swipes turn quickly
        const sp = Math.hypot(dx, dy), acc = 1 + Math.min(1.2, sp / 40);
        S().lookX += dx * acc * (900 / Math.max(500, innerHeight)); S().lookY += dy * acc * (900 / Math.max(500, innerHeight));
        lx = e.clientX; ly = e.clientY; moved += sp;
      }
    });
    const end = e => {
      if (e.pointerId === stickId) { stickId = null; S().mx = S().my = 0; knob.style.transform = ""; stick.classList.remove("on"); }
      else if (e.pointerId === lookId) {
        lookId = null;
        if (performance.now() - tapStart < 260 && moved < 14) AZ.game.tap(e.clientX, e.clientY);
      }
    };
    look.addEventListener("pointerup", end); look.addEventListener("pointercancel", end);

    const hold = (id, on, off) => {
      const b = $(id);
      b.addEventListener("pointerdown", e => { e.preventDefault(); e.stopPropagation(); AZ.audioUnlock(); b.classList.add("down"); on(); });
      const up = e => { b.classList.remove("down"); off && off(); };
      b.addEventListener("pointerup", up); b.addEventListener("pointercancel", up); b.addEventListener("pointerleave", up);
    };
    hold("bUse", () => AZ.game.useFocus());
    hold("bJump", () => { S().jump = true; });
    hold("bCrouch", () => { S().crouch = !S().crouch; $("bCrouch").classList.toggle("toggled", S().crouch); });
    hold("bEcho", () => { S().echo = true; }, () => { S().echo = false; });
    hold("bHum", () => AZ.game.hum());
    document.querySelectorAll(".tb.state").forEach(b => hold(null || b.id || (b.id = "bState" + b.dataset.s), () => AZ.game.shift(+b.dataset.s)));
    hold("bJournal", () => UI.openJournal());
    hold("bPause", () => UI.pause(true));
    UI.applyLayout();
    AZ.on("settings", AZ.persistent(UI.applyLayout));
    // Desktop testing helpers (keyboard) — the game is designed for touch.
    const keys = {};
    addEventListener("keydown", e => {
      keys[e.code] = true; AZ.audioUnlock();
      if (e.code === "KeyE") AZ.game.useFocus(); if (e.code === "Space") S().jump = true; if (e.code === "KeyC") S().crouch = !S().crouch;
      if (e.code === "Digit1") AZ.game.shift(0); if (e.code === "Digit2") AZ.game.shift(1); if (e.code === "Digit3") AZ.game.shift(2);
      if (e.code === "KeyH") AZ.game.hum(); if (e.code === "KeyF") S().echo = true; if (e.code === "Escape") window.azBack();
      if (e.code === "Enter") UI.skipLine();
    });
    addEventListener("keyup", e => { keys[e.code] = false; if (e.code === "KeyF") S().echo = false; });
    UI.keyMove = () => {
      if (stickId !== null) return;
      const x = (keys.KeyD ? 1 : 0) - (keys.KeyA ? 1 : 0), y = (keys.KeyW ? 1 : 0) - (keys.KeyS ? 1 : 0);
      if (x || y || UI._kb) { UI._kb = !!(x || y); S().mx = x * (keys.ShiftLeft ? 1 : 0.9); S().my = y * (keys.ShiftLeft ? 1 : 0.9); }
      if (keys.ArrowLeft) S().lookX -= 12; if (keys.ArrowRight) S().lookX += 12; if (keys.ArrowUp) S().lookY -= 8; if (keys.ArrowDown) S().lookY += 8;
    };
  }
  UI.applyLayout = () => { document.body.classList.toggle("lefty", !!AZ.settings.left); document.documentElement.style.setProperty("--sub-scale", AZ.settings.sub); };

  // =====================================================================
  // Panels: document, choices, journal, pause, settings, menu
  // =====================================================================
  function buildPanels() {
    const p = $("panels");
    p.innerHTML = `
      <div class="panel" id="pDoc"><div class="paper"><h2></h2><div class="body"></div><button class="btn">Close</button></div></div>
      <div class="panel" id="pChoice"><div class="box"><h3></h3><div class="opts"></div></div></div>
      <div class="panel" id="pJournal"><div class="box wide"><h3>CASE JOURNAL - ARIN</h3><div class="body"></div><button class="btn">Close</button></div></div>
      <div class="panel" id="pPause"><div class="box"><h3>PAUSED</h3>
        <button class="btn" data-a="resume">Resume</button><button class="btn" data-a="journal">Journal</button>
        <button class="btn" data-a="settings">Settings</button><button class="btn" data-a="restart">Restart chapter</button><button class="btn" data-a="menu">Main menu</button></div></div>
      <div class="panel" id="pSettings"><div class="box wide"><h3>SETTINGS</h3><div class="rows"></div><button class="btn" data-a="back">Back</button></div></div>
      <div class="panel" id="pMenu"><div class="menu">
        <div class="eyebrow">AFTERZERO - GAME 01</div><h1>THE FIRST CYCLE</h1>
        <p class="tag">A city repeats a catastrophe. A watch that doesn't tick. A letter in your own handwriting.</p>
        <div class="mbtns"></div><p class="diff"></p></div>
        <div class="chapters"></div></div>
      <div id="fade"></div>
    `;
    $("pDoc").querySelector(".btn").onclick = () => closeDoc();
    $("pDoc").onclick = e => { if (e.target.id === "pDoc") closeDoc(); };
    $("pJournal").querySelector(".btn").onclick = () => { show("pJournal", false); UI.lock(false); };
    $("pPause").querySelectorAll(".btn").forEach(b => b.onclick = () => {
      const a = b.dataset.a;
      if (a === "resume") UI.pause(false);
      if (a === "journal") { UI.pause(false); UI.openJournal(); }
      if (a === "settings") UI.openSettings();
      if (a === "restart") { UI.pause(false); AZ.game.restartLevel(); }
      if (a === "menu") { UI.pause(false); AZ.game.menu(); }
    });
    $("pSettings").querySelector("[data-a=back]").onclick = () => show("pSettings", false);
    buildSettings();
  }
  const show = (id, on) => $(id).classList.toggle("show", on);
  let docOpen = false;
  UI.document = (title, text) => {
    const d = $("pDoc");
    d.querySelector("h2").textContent = title || "";
    d.querySelector(".body").textContent = text || "";
    show("pDoc", true); if (!docOpen) UI.lock(true); docOpen = true;
  };
  function closeDoc() { if (!docOpen) return; docOpen = false; show("pDoc", false); UI.lock(false); }

  UI.choose = (question, options) => new Promise(res => {
    const box = $("pChoice");
    box.querySelector("h3").textContent = question || "";
    const opts = box.querySelector(".opts"); opts.innerHTML = "";
    options.forEach((o, i) => {
      const b = el("button", "btn opt" + (o.disabled ? " disabled" : ""), (i + 1) + ".  " + esc(o.text), opts);
      b.onclick = () => { if (o.disabled) { AZ.sfx("denied"); return; } show("pChoice", false); UI.lock(false); AZ.sfx("use"); res(i); };
    });
    UI.lock(true); show("pChoice", true);
  });

  UI.openJournal = () => {
    const j = $("pJournal").querySelector(".body");
    j.innerHTML = AZ.journal.length ? AZ.journal.slice().reverse().map(e => "<h4>" + esc(e.title) + "</h4><p>" + esc(e.body) + "</p>").join("") : "<p><i>No evidence yet. Everything that repeats is a clue; everything that doesn't is a bigger one.</i></p>";
    UI.lock(true); show("pJournal", true);
  };
  UI.paused = false;
  UI.pause = on => {
    if (UI.paused === on) return;
    UI.paused = on; show("pPause", on); if (!on) show("pSettings", false);
    AZ.audioPause(on);
  };
  window.azBack = () => {
    if ($("pSettings").classList.contains("show")) { show("pSettings", false); return; }
    if (docOpen) { closeDoc(); return; }
    if ($("pJournal").classList.contains("show")) { show("pJournal", false); UI.lock(false); return; }
    if ($("pMenu").classList.contains("show")) return;
    UI.pause(!UI.paused);
  };
  window.azPause = () => { if (AZ.game.inLevel()) UI.pause(true); AZ.game.saveNow(); };

  // ---------------- settings ----------------
  const ROWS = [
    ["Difficulty", () => AZ.diff.label(AZ.settings.difficulty), d => { AZ.settings.difficulty = (AZ.settings.difficulty + d + 3) % 3; }],
    ["Look sensitivity", () => AZ.settings.sens.toFixed(1) + "x", d => { AZ.settings.sens = Math.min(3, Math.max(0.3, +(AZ.settings.sens + d * 0.1).toFixed(1))); }],
    ["Invert look", () => (AZ.settings.invert ? "ON" : "OFF"), () => { AZ.settings.invert = !AZ.settings.invert; }],
    ["Camera distance", () => ({ 0.8: "NEAR", 1: "NORMAL", 1.25: "FAR" })[AZ.settings.cam] || "NORMAL", d => { const c = [0.8, 1, 1.25]; AZ.settings.cam = c[(c.indexOf(AZ.settings.cam) + d + 3) % 3]; }],
    ["Arin's coat", () => ({ crimson: "CRIMSON", charcoal: "CHARCOAL" })[AZ.settings.arin] || "SEA-GREEN (original)", d => { const o = ["teal", "crimson", "charcoal"]; AZ.settings.arin = o[(Math.max(0, o.indexOf(AZ.settings.arin)) + d + 3) % 3]; AZ.sim.rebuildAvatar(); }],
    ["Graphics", () => ["AUTO (adapts to keep it smooth)", "LOW - 30 fps battery saver", "MEDIUM", "HIGH - shadows"][AZ.settings.quality + 1], d => { AZ.settings.quality = ((AZ.settings.quality + 1 + d + 4) % 4) - 1; }],
    ["Thumb layout", () => (AZ.settings.left ? "LEFT-HANDED (look on left)" : "RIGHT-HANDED (move on left)"), () => { AZ.settings.left = !AZ.settings.left; }],
    ["Vibration", () => (AZ.settings.vibe ? "ON" : "OFF"), () => { AZ.settings.vibe = !AZ.settings.vibe; }],
    ["Subtitle size", () => Math.round(AZ.settings.sub * 100) + "%", d => { AZ.settings.sub = Math.min(1.6, Math.max(0.8, +(AZ.settings.sub + d * 0.1).toFixed(1))); }],
    ["Volume", () => Math.round(AZ.settings.volume * 100) + "%", d => { AZ.settings.volume = Math.min(1, Math.max(0, +(AZ.settings.volume + d * 0.1).toFixed(1))); }],
  ];
  function buildSettings() {
    const rows = $("pSettings").querySelector(".rows"); rows.innerHTML = "";
    ROWS.forEach(r => {
      const row = el("div", "row", "", rows);
      el("span", "lbl", r[0], row);
      const minus = el("button", "btn sq", "&lt;", row);
      const val = el("span", "val", r[1](), row);
      const plus = el("button", "btn sq", "&gt;", row);
      const ch = d => { r[2](d); AZ.saveSettings(); val.textContent = r[1](); AZ.sfx("click"); };
      minus.onclick = () => ch(-1); plus.onclick = () => ch(1);
      r.refresh = () => { val.textContent = r[1](); };
    });
  }
  UI.openSettings = () => { ROWS.forEach(r => r.refresh && r.refresh()); show("pSettings", true); };

  // ---------------- main menu ----------------
  UI.menu = on => {
    show("pMenu", on);
    document.body.classList.toggle("inmenu", on);
    if (!on) return;
    const m = $("pMenu"), btns = m.querySelector(".mbtns"); btns.innerHTML = "";
    const add = (label, fn) => { const b = el("button", "btn big", label, btns); b.onclick = () => { AZ.audioUnlock(); AZ.sfx("use"); fn(); }; };
    if (AZ.save.has()) add("Continue", () => AZ.game.continueGame());
    add("New game", () => AZ.game.newGame());
    add("Chapters", () => m.querySelector(".chapters").classList.toggle("show"));
    add("Settings", () => UI.openSettings());
    if (window.AZNative && AZNative.quit) add("Quit", () => AZNative.quit());
    m.querySelector(".diff").textContent = "Difficulty: " + AZ.diff.label(AZ.settings.difficulty) + "  (change in Settings)";
    const ch = m.querySelector(".chapters"); ch.innerHTML = "";
    AZ.levels.forEach((L, i) => { const b = el("button", "btn chap", "<small>" + esc(L.chapter) + "</small>" + esc(L.title), ch); b.onclick = () => { AZ.audioUnlock(); AZ.game.load(i); }; });
  };
  AZ.on("settings", AZ.persistent(() => { const d = document.querySelector("#pMenu .diff"); if (d) d.textContent = "Difficulty: " + AZ.diff.label(AZ.settings.difficulty) + "  (change in Settings)"; }));

  // ---------------- fade ----------------
  UI.fade = (to, ms = 600) => new Promise(res => { const f = $("fade"); f.style.transition = "opacity " + ms + "ms"; f.style.opacity = to; setTimeout(res, ms); });
})();
