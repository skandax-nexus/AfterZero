# AfterZero: The First Cycle — Android (touch)

Installable Android game (APK) of AfterZero: The First Cycle, built for touch screens. The game runs on a three.js
engine inside a full-screen, landscape Android WebView: Prologue, 8 chapters, Epilogue, all from Design v2.

- Build the APK: `./tools/build_apk.sh` -> `dist/AfterZero_FirstCycle.apk` (needs Java 17+; Apktool and
  uber-apk-signer are in `tools/`).
- Play in a desktop browser for testing: `cd assets && python3 -m http.server 8000`, then open http://localhost:8000
  (WASD/mouse/E also work). Opening the file directly works too, but browsers block loading the character models
  from `file://`, so you'd see the stylised fallback figures.
- Characters: skinned, animated humans (`assets/models`, see `CREDITS.txt`) driven by `js/humans.js`; third-person
  over-the-shoulder camera with wall collision (Settings: camera distance, Arin's look).
- Automated play-tests: `node test/run.js P00 L01 ... --script=test/solve/L01.js` (headless Chromium).

The full-fidelity Unity 6 version lives in the separate `AfterZero_TheFirstCycle` project.
