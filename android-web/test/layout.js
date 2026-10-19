// Layout / overlap check: loads the title menu and a chapter in headless Chromium, resizes the SAME page through a list of
// phone + tablet viewports in portrait and landscape (live reflow, no reload - like rotating the device), forces the
// worst-case HUD (long objective, timer, loop bar, detection, toast, prompt, carried item, two-line subtitle) and
// opens every menu / panel. Fails on any overlap between touch controls and HUD text boxes, anything outside the
// safe area, touch targets under 44 px, or panels that do not fit / scroll.
// usage: node test/layout.js [--shots=dir] [--level=L01] [--quick] [--only=name,name]
const { chromium } = require("../tools/node_modules/playwright-core");
const path = require("path"), fs = require("fs");
const args = process.argv.slice(2);
const opt = k => { const a = args.find(x => x.startsWith("--" + k + "=")); return a ? a.slice(k.length + 3) : null; };
const shots = opt("shots") || path.join(__dirname, "shots", "layout");
const level = opt("level") || "L01";
fs.mkdirSync(shots, { recursive: true });

// name, width, height, safe insets [top, right, bottom, left], left-handed
const ALL = [
  ["phone-360x640-portrait", 360, 640], ["phone-390x844-portrait", 390, 844], ["phone-412x915-portrait", 412, 915], ["phone-430x932-portrait", 430, 932],
  ["phone-640x360-landscape", 640, 360], ["phone-844x390-landscape", 844, 390], ["phone-915x412-landscape", 915, 412], ["phone-932x430-landscape", 932, 430],
  ["phone-390x844-portrait-notch", 390, 844, [47, 0, 34, 0]], ["phone-844x390-landscape-notch", 844, 390, [0, 47, 21, 47]],
  ["phone-390x844-portrait-lefty", 390, 844, null, true], ["phone-844x390-landscape-lefty", 844, 390, null, true],
  ["tablet-800x1280-portrait", 800, 1280], ["tablet-1280x800-landscape", 1280, 800], ["tablet-768x1024-portrait", 768, 1024], ["tablet-1024x768-landscape", 1024, 768],
];
let SIZES = args.includes("--quick") ? ALL.filter(s => /360x640|390x844-portrait$|844x390-landscape$|640x360|1280x800/.test(s[0])) : ALL;
if (opt("only")) SIZES = ALL.filter(s => opt("only").split(",").some(o => s[0].includes(o)));
const SHOT_PANELS = /360x640|640x360|390x844-portrait$|844x390-landscape$|800x1280|1280x800/;

const failures = [];
const fail = (where, msg) => { failures.push(where + ": " + msg); };

// ---- in-page helpers (stringified) ----
const PAGE_HELPERS = () => {
  window.__lt = {
    vis(e) {
      if (!e) return false;
      for (let n = e; n && n !== document.body; n = n.parentElement) { const cs = getComputedStyle(n); if (cs.display === "none" || cs.visibility === "hidden" || +cs.opacity < 0.05) return false; }
      const r = e.getBoundingClientRect(); return r.width > 0.5 && r.height > 0.5;
    },
    rect(e) { const r = e.getBoundingClientRect(), br = parseFloat(getComputedStyle(e).borderTopLeftRadius) || 0; return { x: r.left, y: r.top, w: r.width, h: r.height, r: r.right, b: r.bottom, round: Math.abs(r.width - r.height) < 1 && br >= r.width / 2 - 1 }; },
    collect(sel) { return [...document.querySelectorAll(sel)].filter(e => this.vis(e)).map(e => Object.assign({ id: e.id || (e.className + ":" + (e.dataset.s || e.textContent.trim().slice(0, 14))) }, this.rect(e))); },
    safe() { const p = document.getElementById("safeprobe"); if (!p) return { t: 0, r: 0, b: 0, l: 0 }; const cs = getComputedStyle(p), v = k => parseFloat(cs[k]) || 0; return { t: v("paddingTop"), r: v("paddingRight"), b: v("paddingBottom"), l: v("paddingLeft") }; },
  };
};

// round buttons are compared as circles (diagonal neighbours on an arc have overlapping boxes but not overlapping discs)
function overlaps(a, b, tol = 0.5) {
  if (!(Math.min(a.r, b.r) - Math.max(a.x, b.x) > tol && Math.min(a.b, b.b) - Math.max(a.y, b.y) > tol)) return false;
  const circ = o => ({ x: o.x + o.w / 2, y: o.y + o.h / 2, r: o.w / 2 });
  if (a.round && b.round) { const p = circ(a), q = circ(b); return Math.hypot(p.x - q.x, p.y - q.y) < p.r + q.r - tol; }
  if (a.round || b.round) { const c = circ(a.round ? a : b), o = a.round ? b : a; const nx = Math.max(o.x, Math.min(c.x, o.r)), ny = Math.max(o.y, Math.min(c.y, o.b)); return Math.hypot(c.x - nx, c.y - ny) < c.r - tol; }
  return true;
}
function checkSet(where, items, W, H, safe, opts = {}) {
  for (let i = 0; i < items.length; i++) {
    const a = items[i];
    if (a.x < safe.l - 0.5 || a.y < safe.t - 0.5 || a.r > W - safe.r + 0.5 || a.b > H - safe.b + 0.5) fail(where, `${a.id} outside safe viewport [${a.x | 0},${a.y | 0},${a.r | 0},${a.b | 0}]`);
    for (let j = i + 1; j < items.length; j++) { const b = items[j]; if (overlaps(a, b)) fail(where, `${a.id} overlaps ${b.id}  (${a.x | 0},${a.y | 0} ${a.w | 0}x${a.h | 0} / ${b.x | 0},${b.y | 0} ${b.w | 0}x${b.h | 0})`); }
    if (opts.minTouch && a.touch && (a.w < opts.minTouch - 0.5 || a.h < opts.minTouch - 0.5)) fail(where, `${a.id} touch target ${a.w | 0}x${a.h | 0} < ${opts.minTouch}px`);
  }
}

(async () => {
  const browser = await chromium.launch({ executablePath: "/opt/pw-browsers/chromium_headless_shell-1194/chrome-linux/headless_shell", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--autoplay-policy=no-user-gesture-required", "--allow-file-access-from-files"] });
  const page = await browser.newPage({ viewport: { width: SIZES[0][1], height: SIZES[0][2] }, deviceScaleFactor: 1, hasTouch: true });
  await page.addInitScript(() => { try { const s = JSON.parse(localStorage.getItem("az1_settings") || "{}"); s.quality = 0; s.left = false; localStorage.setItem("az1_settings", JSON.stringify(s)); } catch (e) { } });
  await page.addInitScript(PAGE_HELPERS);
  const errors = [];
  page.on("pageerror", e => errors.push("[pageerror] " + e.message));
  page.on("console", m => { if (m.type() === "error" && !/GPU stall|WebGL|swiftshader/i.test(m.text())) errors.push("[console] " + m.text().slice(0, 200)); });
  const file = "file://" + path.resolve(__dirname, "../assets/index.html");
  const frames = n => page.evaluate(n2 => new Promise(r => { let k = 0; const f = () => (++k >= n2 ? r() : requestAnimationFrame(f)); requestAnimationFrame(f); }), n);

  async function setSize(s) {
    const [name, w, h, inset, lefty] = s;
    await page.setViewportSize({ width: w, height: h });
    await page.evaluate(([ins, lf]) => {
      const st = document.documentElement.style;
      ["--st", "--sr", "--sb", "--sl"].forEach((k, i) => { if (ins) st.setProperty(k, ins[i] + "px"); else st.removeProperty(k); });
      if (window.AZ && AZ.settings) { AZ.settings.left = !!lf; AZ.emit("settings"); }
      window.dispatchEvent(new Event("resize"));
    }, [inset || null, !!lefty]);
    await frames(3);
    await page.waitForTimeout(120);
  }
  const ctx = s => ({ W: s[1], H: s[2] });

  // ======================= title menu =======================
  await page.goto(file);
  await page.waitForFunction("document.querySelector('#pMenu.show .mbtns .btn')", { timeout: 120000 });
  for (const s of SIZES) {
    await setSize(s);
    const where = "menu@" + s[0];
    const r = await page.evaluate(() => {
      const L = __lt; AZ.ui.showChapters(true);
      const menu = L.collect("#pMenu .eyebrow, #pMenu h1, #pMenu .tag, #pMenu .mbtns .btn, #pMenu .diff");
      const ch = document.querySelector("#pMenu .chapters"), chr = L.rect(ch);
      // chapter buttons that are inside the scroll viewport
      const chap = L.collect("#pMenu .chapters .btn").filter(b => b.b > chr.y + 1 && b.y < chr.b - 1).map(b => Object.assign(b, { y: Math.max(b.y, chr.y), b: Math.min(b.b, chr.b), touch: true }));
      menu.forEach(m => { if (/btn/.test(m.id)) m.touch = true; });
      return { items: menu.concat(chap), chScroll: ch.scrollHeight > ch.clientHeight + 1, chOverflow: getComputedStyle(ch).overflowY, nChap: ch.children.length, safe: L.safe(), menuBox: L.rect(document.querySelector("#pMenu .menu")) };
    });
    const { W, H } = ctx(s);
    checkSet(where, r.items, W, H, r.safe, { minTouch: 44 });
    if (r.chScroll && !/auto|scroll/.test(r.chOverflow)) fail(where, "chapter list overflows without scrolling");
    if (r.menuBox.b > H + 1 || r.menuBox.y < -1) fail(where, "menu block does not fit vertically (" + (r.menuBox.y | 0) + ".." + (r.menuBox.b | 0) + ")");
    await page.screenshot({ path: path.join(shots, "menu_" + s[0] + ".png") });
    await page.evaluate(() => AZ.ui.showChapters(false));
    if (SHOT_PANELS.test(s[0])) await page.screenshot({ path: path.join(shots, "menu-closed_" + s[0] + ".png") });
    // settings from the menu
    await page.evaluate(() => AZ.ui.openSettings());
    await frames(2);
    await checkPanel("settings-menu@" + s[0], "#pSettings", s, SHOT_PANELS.test(s[0]) ? "settings_" + s[0] : null);
    await page.evaluate(() => document.querySelector("#pSettings [data-a=back]").click());
  }

  async function checkPanel(where, sel, s, shot) {
    const r = await page.evaluate(sel2 => {
      const L = __lt, p = document.querySelector(sel2);
      const box = p.querySelector(".box, .paper"), br = L.rect(box);
      const cs = getComputedStyle(box), scrolls = /auto|scroll/.test(cs.overflowY);
      const fits = box.scrollHeight <= box.clientHeight + 1, hfits = box.scrollWidth <= box.clientWidth + 1;
      // interactive + text children: must not overlap each other (within the box's scroll content)
      // children are clipped to their scroll containers (rows scrolled out of view are not on screen)
      const clip = (e, r) => { for (let n = e.parentElement; n && n !== p; n = n.parentElement) { if (getComputedStyle(n).overflowY !== "visible") { const c = n.getBoundingClientRect(); r = Object.assign({}, r, { x: Math.max(r.x, c.left), y: Math.max(r.y, c.top), r: Math.min(r.r, c.right), b: Math.min(r.b, c.bottom) }); } } r.w = r.r - r.x; r.h = r.b - r.y; return r; };
      const kids = [...box.querySelectorAll(".btn, .lbl, .val, h2, h3, .body")].filter(e => L.vis(e)).map(e => { const r0 = L.rect(e); return Object.assign({ id: (e.className || e.tagName) + ":" + e.textContent.trim().slice(0, 16), el: e, w0: r0.w, h0: r0.h }, clip(e, r0), { touch: e.classList.contains("btn") }); }).filter(k => k.w > 1 && k.h > 1);
      // every button whose centre is on screen must actually receive the touch (not be covered by another layer)
      const covered = kids.filter(k => k.touch && k.h > k.h0 * 0.6).map(k => { const e = k.el, x = k.x + k.w / 2, y = k.y + k.h / 2;
        if (x < 0 || y < 0 || x > innerWidth || y > innerHeight) return null; const hit = document.elementFromPoint(x, y); return hit && (hit === e || e.contains(hit)) ? null : e.textContent.trim().slice(0, 16) + " covered by " + (hit ? hit.id || hit.className : "nothing"); }).filter(Boolean);
      kids.forEach(k => { delete k.el; });
      return { br, scrolls, fits, hfits, kids, covered, safe: L.safe() };
    }, sel);
    const { W, H } = ctx(s);
    const sf = r.safe;
    if (r.br.x < sf.l - 0.5 || r.br.y < sf.t - 0.5 || r.br.r > W - sf.r + 0.5 || r.br.b > H - sf.b + 0.5) fail(where, `panel box outside safe viewport [${r.br.x | 0},${r.br.y | 0},${r.br.r | 0},${r.br.b | 0}]`);
    if (!r.fits && !r.scrolls) fail(where, "panel content taller than the box and not scrollable");
    if (!r.hfits) fail(where, "panel content wider than the box (horizontal overflow)");
    r.covered.forEach(c => fail(where, "button " + c));
    for (let i = 0; i < r.kids.length; i++) for (let j = i + 1; j < r.kids.length; j++) {
      const a = r.kids[i], b = r.kids[j];
      if (a.id.startsWith("body") || b.id.startsWith("body")) continue;
      if (overlaps(a, b)) fail(where, `${a.id} overlaps ${b.id}`);
    }
    r.kids.filter(k => k.touch && (k.h0 < 43.5 || k.w0 < 43.5)).forEach(k => fail(where, `${k.id} touch target ${k.w0 | 0}x${k.h0 | 0} < 44px`));
    if (shot) await page.screenshot({ path: path.join(shots, shot + ".png") });
  }

  // ======================= in a chapter =======================
  await page.goto(file + "?level=" + level + "&skipstory=1");
  await page.waitForFunction("window.AZ && AZ.game && AZ.game.world() && AZ.player.avatar", { timeout: 180000 });
  await page.waitForTimeout(3600); // chapter title card
  await page.evaluate(() => {
    const UI = AZ.ui;
    UI.say([{ speaker: "", text: "", dur: 0.01 }]); // clear any intro line
    UI.objective("Find the second recurrence point and stop the cart before the bell rings twice");
    UI.timer(479, "SECOND BELL IN");
    UI.loopBar({ elapsed: 30, duration: 90, count: 2 });
    UI.detection(0.6);
    UI.toast("Something occupies this space in the FUTURE. Step aside and try again.", 9999);
    UI.prompt("Talk to Ilse Marrow");
    document.getElementById("held").textContent = "Carrying: Brass valve handle";
    // keep the forced HUD while the simulation keeps running (camera framing per orientation stays live)
    ["timer", "loopBar", "detection", "prompt", "toast", "objective", "refreshHeld"].forEach(k => { UI["_" + k] = UI[k]; UI[k] = () => { }; });
    AZ.ui.say = () => { }; AZ.ui.bark = () => { };
  });
  await page.waitForTimeout(400); // let the cleared subtitle queue finish before forcing a two-line subtitle
  await page.evaluate(() => {
    const s = document.getElementById("sub");
    s.innerHTML = "<b>ILSE MARROW</b>Three drives under the bell. One of them is listening to something that isn't me - don't touch them blind.";
    s.classList.add("show");
  });
  for (const s of SIZES) {
    await setSize(s);
    const where = "hud@" + s[0];
    await frames(2);
    const r = await page.evaluate(() => {
      const L = __lt;
      const texts = L.collect("#obj, #held, #timer, #loopbar, #detect, #toast, #prompt, #sub, #states");
      const ctl = L.collect("#bUse, #bJump, #bCrouch, #bEcho, #bHum, .tb.state, #bJournal, #bPause").map(c => Object.assign(c, { touch: true }));
      const blocked = ctl.map(c => { const e = document.getElementById(c.id) || null; if (!e) return null; const hit = document.elementFromPoint(c.x + c.w / 2, c.y + c.h / 2); return hit && (hit === e || e.contains(hit)) ? null : c.id + " covered by " + (hit ? hit.id || hit.className : "nothing"); }).filter(Boolean);
      const cv = document.getElementById("view");
      return { blocked, items: texts.concat(ctl), nCtl: ctl.length, safe: L.safe(), canvas: [cv.clientWidth, cv.clientHeight], aspect: AZ.render.camera.aspect, fov: AZ.render.fovFor ? AZ.render.fovFor(60) : AZ.render.camera.fov, lefty: document.body.classList.contains("lefty"),
        use: L.rect(document.getElementById("bUse")) };
    });
    const { W, H } = ctx(s);
    if (r.nCtl !== 10) fail(where, "expected 10 visible touch controls, found " + r.nCtl);
    checkSet(where, r.items, W, H, r.safe, { minTouch: 44 });
    r.blocked.forEach(b => fail(where, "control " + b));
    if (r.canvas[0] !== W || r.canvas[1] !== H) fail(where, "canvas not resized: " + r.canvas.join("x"));
    if (Math.abs(r.aspect - W / H) > 0.01) fail(where, "camera aspect " + r.aspect.toFixed(3) + " != " + (W / H).toFixed(3));
    const hfov = 2 * Math.atan(Math.tan(r.fov * Math.PI / 360) * W / H) * 180 / Math.PI;
    if (hfov < 52) fail(where, "horizontal FOV too narrow: " + hfov.toFixed(1) + " deg (vertical " + r.fov.toFixed(1) + ")");
    if (s[4] && r.use.x > W / 2) fail(where, "left-handed layout not mirrored (USE on the right)");
    if (!s[4] && r.use.x < W / 2) fail(where, "right-handed layout has USE on the left");
    console.log(`   ${s[0]}: vfov ${r.fov.toFixed(1)} hfov ${hfov.toFixed(1)}`);
    await page.screenshot({ path: path.join(shots, "hud_" + s[0] + ".png") });
  }
  // ---- panels in game ----
  for (const s of SIZES) {
    await setSize(s);
    const sh = SHOT_PANELS.test(s[0]);
    await page.evaluate(() => AZ.ui.pause(true)); await frames(1);
    await checkPanel("pause@" + s[0], "#pPause", s, sh ? "pause_" + s[0] : null);
    await page.evaluate(() => AZ.ui.openSettings()); await frames(1);
    await checkPanel("settings@" + s[0], "#pSettings", s, null);
    await page.evaluate(() => { document.querySelector("#pSettings [data-a=back]").click(); AZ.ui.pause(false); });
    await page.evaluate(() => {
      AZ.journal.length = 0;
      for (let i = 0; i < 6; i++) AZ.journal.push({ title: "Evidence " + (i + 1) + " - The cart on Lantern Lane", body: "The cart comes down the lane at the same moment every recurrence. The cup on Mirela's table jumps; her hands are dry. Someone is resetting the city - and they missed one thing." });
      AZ.ui.openJournal();
    }); await frames(1);
    await checkPanel("journal@" + s[0], "#pJournal", s, sh ? "journal_" + s[0] : null);
    await page.evaluate(() => document.querySelector("#pJournal .btn").click());
    await page.evaluate(() => { window.__choice = AZ.ui.choose("Ilse holds out the brass key. What do you tell her?", [{ text: "\"Keep it. If the bell rings twice, you'll need it more than I will.\"" }, { text: "\"Give it to me - I've seen what happens when you hold the tower alone.\"" }, { text: "\"I don't know what it opens. Tell me about cot seven first.\"" }, { text: "(Say nothing. Shift to the Past and look at the key there.)", disabled: true }]); }); await frames(1);
    await checkPanel("choice@" + s[0], "#pChoice", s, sh ? "choice_" + s[0] : null);
    await page.evaluate(() => document.querySelector("#pChoice .opt").click());
    await page.evaluate(() => AZ.ui.document("NOTICE FROM THE WARD OFFICE", "To all residents of the Lantern Ward:\n\nThe second bell is a fault of the old mechanism and nothing more. ".repeat(1) + "Do not gather at the tower. Do not speak to the Keepers. Do not repeat the Nursery song to children.\n\nIf you find a watch that does not tick, bring it to the ward office. It is not yours. It was never yours.\n\n".repeat(3) + "- Founder Halden")); await frames(1);
    await checkPanel("document@" + s[0], "#pDoc", s, sh ? "document_" + s[0] : null);
    await page.evaluate(() => document.querySelector("#pDoc .btn").click());
  }
  // ---- title / revelation cards ----
  for (const s of SIZES.filter(x => SHOT_PANELS.test(x[0]))) {
    await setSize(s);
    await page.evaluate(() => { const c = document.getElementById("card"); c.querySelector("small").textContent = "CHAPTER 7"; c.querySelector("strong").textContent = "THE SECOND BELL"; c.querySelector("em").textContent = "Clock Tower, Upper Veyra"; c.style.transition = "none"; c.classList.add("show"); });
    await frames(2);
    const r = await page.evaluate(() => { const L = __lt; return L.collect("#card small, #card strong, #card em"); });
    r.forEach(e => { if (e.x < -0.5 || e.r > s[1] + 0.5) fail("card@" + s[0], e.id + " wider than screen"); });
    await page.screenshot({ path: path.join(shots, "card_" + s[0] + ".png") });
    await page.evaluate(() => document.getElementById("card").classList.remove("show"));
  }
  await browser.close();
  errors.forEach(e => console.log("   " + e));
  if (errors.length) failures.push("page errors: " + errors.length);
  console.log(failures.length ? "LAYOUT FAIL (" + failures.length + ")\n  " + failures.join("\n  ") : "LAYOUT OK - " + SIZES.length + " sizes, no overlaps");
  console.log("screenshots: " + shots);
  process.exit(failures.length ? 1 : 0);
})().catch(e => { console.error(e); process.exit(2); });
