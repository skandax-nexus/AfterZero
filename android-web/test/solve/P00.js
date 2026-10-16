// Scripted walkthrough of the Prologue: proves every beat is completable.
(async () => {
  const T = AZ.test, log = m => console.log("[solve] " + m);
  await T.wait(500);
  await T.use("coat"); T.closeDoc(); T.skipTalk();
  await T.use("Read the letter"); T.closeDoc(); T.skipTalk();
  await T.use("Talk to Conductor"); T.skipTalk();
  T.goto(0, 0.6, 12.3); await T.wait(400);
  T.goto(-2.5, 0.6, 48.4); await T.wait(2200);
  T.goto(-2.5, 0.6, 52); await T.wait(1500);
  log("objective now: " + T.objective() + " | level: " + T.level());
  if (T.level() !== "L01") console.error("[solve] FAILED: did not reach L01, flags=" + Object.keys(AZ.flags).join(","));
  else log("PASSED");
})();
