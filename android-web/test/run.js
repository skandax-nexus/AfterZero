// Headless play-test: loads chapters in Chromium (SwiftShader WebGL), drives scripted input, reports errors + screenshots.
// usage: node test/run.js [levelId|index ...] [--seconds=8] [--shots=dir] [--script=file.js]
const { chromium } = require("../tools/node_modules/playwright-core");
const path = require("path"), fs = require("fs");
const args = process.argv.slice(2);
const opt = k => { const a = args.find(x => x.startsWith("--" + k + "=")); return a ? a.split("=")[1] : null; };
const levels = args.filter(a => !a.startsWith("--"));
const seconds = +(opt("seconds") || 8), shots = opt("shots") || path.join(__dirname, "shots"), script = opt("script");
fs.mkdirSync(shots, { recursive: true });
(async () => {
  const browser = await chromium.launch({ executablePath: "/opt/pw-browsers/chromium_headless_shell-1194/chrome-linux/headless_shell", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--autoplay-policy=no-user-gesture-required", "--allow-file-access-from-files"] });
  const page = await browser.newPage({ viewport: { width: 960, height: 540 }, deviceScaleFactor: 1, hasTouch: true, isMobile: false });
  // --quality=0|1|2 pins the graphics preset (walkthroughs run faster on Low under SwiftShader)
  if (opt("quality") !== null) await page.addInitScript(q => { try { const s = JSON.parse(localStorage.getItem("az1_settings") || "{}"); s.quality = q; localStorage.setItem("az1_settings", JSON.stringify(s)); } catch (e) { } }, +opt("quality"));
  const errors = [];
  page.on("console", m => { if (m.type() === "error" || m.type() === "warning") errors.push("[" + m.type() + "] " + m.text()); });
  page.on("pageerror", e => errors.push("[pageerror] " + e.message + "\n" + (e.stack || "").split("\n").slice(0, 4).join("\n")));
  const file = "file://" + path.resolve(__dirname, "../assets/index.html");
  const list = levels.length ? levels : ["menu"];
  let bad = 0;
  for (const lv of list) {
    errors.length = 0;
    await page.goto(lv === "menu" ? file : file + "?level=" + lv + "&skipstory=1");
    await page.waitForTimeout(1500);
    if (script) { const code = fs.readFileSync(script, "utf8"); await page.evaluate(code).catch(e => errors.push("[script] " + e.message)); }
    await page.waitForTimeout(seconds * 1000);
    const info = await page.evaluate(() => { const W = AZ.game.world(); return W ? { id: W.def.id, objective: (W.objectives.find(o => !AZ.get(o.flag)) || {}).text, colliders: W.colliders.length, interactables: W.interactables.length, npcs: W.npcs.length, keepers: W.keepers.length, pos: AZ.player.pos.toArray().map(v => +v.toFixed(2)), calls: AZ.render.renderer.info.render.calls, tris: AZ.render.renderer.info.render.triangles, flags: Object.keys(AZ.flags).length } : { menu: true }; }).catch(e => ({ err: e.message }));
    await page.screenshot({ path: path.join(shots, lv + ".png") });
    const real = errors.filter(e => !/GPU stall|WebGL: INVALID|swiftshader|Automatic fallback/i.test(e));
    console.log("== " + lv + " " + JSON.stringify(info));
    real.slice(0, 12).forEach(e => console.log("   " + e));
    if (real.length) bad++;
  }
  await browser.close();
  process.exit(bad ? 1 : 0);
})();
