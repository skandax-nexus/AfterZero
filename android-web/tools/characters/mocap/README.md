# AfterZero motion (motion capture -> our rig)

Every clip in `assets/models/anims.glb` is real human motion: CMU motion capture retargeted onto our own game rig
by `build_anims.py`, which runs Blender as a Python module. The characters themselves are still ours, built by
`../build_humans.py`.

    export MH_DATA=/path/to/makehuman/makehuman/data          # reference body for the rig (same as build_humans.py)
    ../venv/bin/python build_anims.py out                      # ONLY=walk,run for a subset; ~10 s for all clips
    npx @gltf-transform/cli meshopt out/anims.glb ../../../assets/models/anims.glb

`build_humans.py` still writes its own `anims.glb`, which holds the old keyframed clips. Do not install that one.
`assets/models/anims.glb` comes from this folder.

## Source data

The motion data comes from the CMU Graphics Lab Motion Capture Database, http://mocap.cs.cmu.edu. The database was
created with funding from NSF EIA-0196217. It is free to use, including in commercial products. The data is read
from the BVH conversion by B. Hahne (cgspeed), mirrored at github.com/una-dinosauria/cmu-mocap. The raw `.bvh` files
download into `cache/` (or `$MOCAP_CACHE`), which git ignores. Only the baked clips ship. The dataset is never
redistributed.

| clip | CMU trial | what it is | frames used (120 fps) |
|---|---|---|---|
| idle | 137_28 "normal wait" | relaxed standing, weight shifts | 3446-3722 loop |
| walk | 35_01 "walk" | normal walk, 1.17 m/s | 129-264 (1 cycle) |
| walk_slow | 07_04 "slow walk" | 0.92 m/s | 83-256 |
| jog | 35_17 "run/jog" | 2.9 m/s | 62-153 |
| run | 127_07 "run" | 4.3 m/s | 61-141 |
| sprint | 143_01 "run" | 6.4 m/s | 7-93 |
| crouch | 136_09 "walk crouched" | sneak, 0.84 m/s | 900-1045 |
| talk | 18_08 "conversation, explain with hand gestures" | calm stretch: head tilts, small hand gestures | 1730-2070 loop |
| nod | 18_08 | the listener, with soft head nods | 456-939 loop |
| sad | 137_28 | idle with a slumped posture, slowed 1.35x | 3446-3722 loop |
| look | 137_28 "normal wait" | hands on hips, looking around | 2284-2704 loop |
| work | 62_07 "hammering a nail" | hands busy at a bench, head down | 460-822 loop |
| fall | 143_05 "jumping distances" | airborne, arms out (ping-pong, in place) | 172-186 |
| sit | 86_15 "sitting, hand motions" | seated, upright, hands on thighs (IK), feet on floor | 2175-2599 loop |

`build_anims.py` prints the exact frames, speed, stride and contact checks for each clip. It also writes them to
`out/anims.json`.

## Method

1. **Read.** `bvh.py` parses the BVH and runs forward kinematics. The result goes to Blender space: Z up, actor
   facing -Y, which is +Z in game.
2. **Retarget** onto the reference rig. That rig is the average MakeHuman body, rigged by `build_humans.rig_points`.
   - **Limbs:** clavicle, arm, hand, leg, foot and toe are matched by direction. A constant rest alignment turns our
     A-pose bone onto the CMU T-pose segment. Twist comes from an anatomical axis: forward for arms and legs, up for
     feet. Each bone's direction then follows the actor exactly.
   - **Clavicles:** CMU shoulder joints sit high, which reads as a 20-35° shrug on our rig. Each clavicle keeps 20% of
     the take's mean offset from rest (relative to the chest) and 60% of its motion around that mean. It then drops 5°,
     for relaxed shoulders under hanging arms. The upper arms keep their world rotations.
   - **Torso and head:** they take the source's world rotation change, so our own spine curve is kept.
   - **Pelvis:** it follows the mid-point between the hip joints, scaled by leg length. Stride therefore matches our
     proportions.
3. **Heading.** Every clip faces forward. Locomotion runs in place: the linear travel of one gait cycle is removed,
   and vertical bob, sway and pelvis rotation stay. Any turn over the cycle is straightened. Speed is
   stride ÷ cycle time and is stored per clip.
4. **Loops.** The best-matching pair of frames is found from pose and velocity, and the seam is cross-faded. Every
   gait starts on the same footfall, with the left foot forward, so the game can swap gaits in phase.
5. **Feet.** The floor is found from the soles: heel, ball and toe tip. Two-bone leg IK keeps every planted foot on
   the floor, so feet do not sink or float. A foot lock pins each planted sole point while it stays down. Seated clips
   put the hip joints 8.5 cm above the 0.466 m bench, and IK places the feet on the floor and the hands on the thighs.
6. **Bake.** Clips are resampled to 30 fps and keyed on the rig. The rig node's extras carry `az_clips` (JSON with
   speed, cycle and seat per clip).

## At runtime

This happens in `assets/js/humans.js`.

- **Clip transfer.** Each body gets every clip through a world-space retarget. Pelvis travel scales with
  hip height.
- **Floor correction.** Frame by frame, the pelvis height is adjusted so this body's soles touch the floor exactly
  when the reference body's do.
- **Seated clips.** Each body runs leg IK so its feet reach the floor. A child keeps dangling feet, because a foot may
  reach down at most 6 cm.
- **`h.move(speed, scale, {crouch})`.** Picks walk_slow, walk, jog, run or sprint, whichever clip's natural speed for
  this body is nearest, with hysteresis. It sets `timeScale = speed / clipSpeed`, so feet do not slide. Changing gait
  keeps the gait phase.
- **Per-person variety.** Each person gets their own phase and a ±7 % tempo on idle-type clips.

`assets/js/world.js` drives the clips:

- **Who moves.** NPCs, keepers, pursuers, scripted people and Arin go through `h.move` with their measured speed.
- **Route activities.** `work` and `carry` map to `work`, `look` to `look`, `talk` to `talk` and `sit` to `sit`. A
  named NPC that the player stands next to plays `nod`.
- **Arin.** Arin plays `fall` when airborne and `sad` when dying.
