# AFTERZERO — Game 01: The First Cycle (Unity 6 / URP)

First-person narrative puzzle game. Arin moves between three coexisting states of the city of Veyra —
PAST, PRESENT, FUTURE — to stop a repeating catastrophe. Design source: `Docs/GAME_01_THE_FIRST_CYCLE_DETAILED_LEVELS.docx`.

## How the project is built
- **Levels are generated from code.** `Assets/AfterZero/Editor/Levels/Level0NBuilder.cs` builds each scene;
  menu **AfterZero ▸ Build All Levels** (or `AfterZero.EditorTools.AZBuild.BuildAllBatch` in batch mode) regenerates
  `Assets/AfterZero/Scenes/*.unity`. Scenes and `Assets/AfterZero/Generated/` are build outputs — edit the builders,
  not the scenes, unless you intentionally fork a scene by hand (then stop rebuilding that level).
- Shared building toolkit: `Editor/AZKit.cs` (geometry, lights, particles, NPCs, interactables) and
  `Editor/AZMaterials.cs` (material keys → generated URP materials).
- Runtime code: `Assets/AfterZero/Scripts/` — namespace `AfterZero`. One MonoBehaviour per file, file name == class name.

## Core rules of the simulation
- `TemporalStateManager` owns the current state; `TemporalPresence` makes a hierarchy exist only in some states
  (it toggles components, so `SetActive` stays free for gameplay). Absent objects show hologram echoes (hold F).
- `WorldState` flags are the causal memory (`L0N.camelCase`; `loop.*` flags clear on each recurrence;
  `choice.*` flags are cross-level). `FlagReactor` makes consequences visible in other states.
- `LoopManager` + `LoopScheduledEvent` implement the recurrence in Level 01.
- `LevelDirector` holds each level's ordered objectives (one per playable beat) and the clue/payoff revelation.
- Look per state: `TemporalVisuals` (URP Volumes + sun/fog/sky blend) and the `AfterZero/Hologram` shader.
- Audio is procedural (`ProceduralAudio`, `AudioCues`, `AmbientZone`); drop real clips into
  `Assets/AfterZero/Resources/AfterZero/Audio/<CueName>` or `Ambient_<Kind>` to override.

## Working through Unity MCP (MCP for Unity)
- Rebuild after changing a builder: run menu item `AfterZero/Build All Levels` (or `AfterZero/Build Level/0N ...`).
- Then open a scene (`Assets/AfterZero/Scenes/AZ_L0N_*.unity`), enter Play mode, read the Console for errors.
- Prefer changing builders/runtime scripts and rebuilding over hand-editing generated scenes.
- Keep design-doc requirements per level: named NPC checklist (routine, task, approach reaction, object-specific
  line, changed state), background NPC life, local ambience, distinct rule-change cues, safe observation area,
  telegraphed hazards, checkpoints, never colour-only clues, readable exit.
