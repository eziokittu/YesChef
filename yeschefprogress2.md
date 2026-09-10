# YesChef Progress 2

> Historical handoff: the former rodent visitor described below was removed in Progress 6 after the design changed.

Last updated: 2026-09-10  
Base revision: `95f2aa5f1047ec6221f6ff704fe23250652aaad4`  
Progress 2 state: implemented and verified locally, but not committed or pushed  
Repository: <https://github.com/eziokittu/YesChef>

## Status verdict

All twelve implementation requests recorded in `issues.txt` have been addressed in the runtime code, editable scene builder, generated scene, and Blender asset pipeline.

The isolated Unity validator and Windows development build pass. Runtime probes confirm the refrigerator close fix and the east-edge idle camera reveal without `NullReferenceException` or `MissingReferenceException` errors.

The following boundaries remain:

- The final music and sound-effect files do not exist yet. Their mechanics and Inspector slots are ready.
- A normal visible Play Mode review is still required for subjective art, animation, layout, and tuning approval.
- Automated Windows captures produced black images because the test player was required to run hidden. They are not visual proof.
- A full three-minute match and the multi-resolution manual regression list remain pending.
- Progress 2 is not committed or pushed.

## Requirements and decisions to preserve

- Use Unity `6000.3.11f1` or newer and C#.
- Do not add Asset Store art packs or third-party plugins.
- Keep the existing project-owner overrides: one customer initially, staggered arrivals, and randomized return timing.
- Preserve the three-minute match, one-held-item limit, independent two-stove capacity, duplicate orders, and existing scoring formula.
- Update `YesChefSceneBuilder.cs` whenever serialized hierarchy or references change.
- Do not push unless the project owner explicitly requests it.

## Progress 2 implementation checklist

### 1. Pause, quit, results, and credits UI

- [x] Pause and quit confirmation use translucent full-screen overlays so the kitchen remains visible beneath them.
- [x] Credits were moved outside the central pause and quit cards to the lower screen area.
- [x] Developer and source-code credits are separate rows.
- [x] The Glitchbong logo is inside the full clickable developer row.
- [x] A code-native GitHub badge is inside the full clickable source row.
- [x] Credits are also present on the starting instructions and service-over screens.
- [x] Developer link opens <https://glitchbong.com/contact>.
- [x] Source link opens <https://github.com/eziokittu/YesChef>.

### 2. Refrigerator interaction fix

- [x] Pressing `E` closes an open refrigerator catalogue even when the chef is already holding an ingredient.
- [x] The refrigerator prompt changes to `[E] Close fridge` while open.
- [x] Door open and close events have separate sound-effect hooks.
- [x] Runtime evidence logged `YES_CHEF_FRIDGE_E_CLOSE_WITH_HELD_ITEM: True`.

### 3. World-label readability

- [x] Station labels remain compact but use stronger distant opacity and darker backdrops.
- [x] Labels sit higher above their stations.
- [x] Labels fade more strongly when the chef approaches.
- [x] Occupied chopping/stove labels remain suppressible.

### 4. Marsh animals, butterflies, camera, and water

- [x] Frog, fish, and snake Blender models contain additional recognizable details.
- [x] Fish follow smooth routes and perform a predictable water-breach arc.
- [x] Fish movement exposes optional spatial splash/paddle sound slots.
- [x] The frog follows smooth parabolic hops between lotus locations.
- [x] The marsh includes low-poly lotus leaves beneath the flowers.
- [x] The snake waits for a randomized delay, crosses the marsh once, performs a hiss motion/sound event, and disappears.
- [x] Four small butterflies fly along smooth repeatable paths near the garden flowers.
- [x] The marsh surface is divided into moving low-poly facets with phase-offset vertical waves.
- [x] When the chef is idle near the east side, the camera shifts right and widens instead of zooming inward.
- [x] Runtime evidence recorded the chef at screen X `672.89` and delayed east-side FOV `57.91`.

### 5. Garden layout and wind

- [x] Trees and flowers use intentionally uneven positions, gaps, rotations, and scales.
- [x] Bushes and grass clumps were added.
- [x] Trees, flowers, grass, bushes, lotus flowers, and lotus leaves have lightweight wind sway.

### 6. Kitchen navigation, windows, floor, and exterior light

- [x] The refrigerator, chopping table, and stoves have wider navigation gaps.
- [x] South-facing garden windows and east-facing marsh windows were added.
- [x] Window panes use a transparent Standard-material configuration.
- [x] Warm exterior spot lights create conical light entering from the garden and marsh directions.
- [x] The kitchen floor includes a subtle alternating accent-tile pattern.

### 7. Music and sound mechanics

- [x] `AudioDirector` persists through scene restarts with `DontDestroyOnLoad`.
- [x] Two optional background tracks play sequentially and loop as a playlist.
- [x] Music continues while gameplay is paused and while pause/quit screens are open.
- [x] Restarting a match does not restart the persistent music.
- [x] Music and sound effects have separate pause-menu toggles.
- [x] Preferences persist with `PlayerPrefs`.
- [x] Re-enabling music within three seconds resumes its previous position.
- [x] Re-enabling it after more than three seconds restarts the current track.
- [x] Optional SFX hooks exist for chopping, cooking, fridge open, fridge close, prepared food, new orders, received order items, and trash.
- [x] Garden ambience has three proximity-faded loop slots for crickets, birds, and wind.
- [x] Marsh ambience has four proximity-faded loop slots for water, paddling, frogs, and additional marsh ambience.
- [x] Fish breaches and the snake visit expose optional spatial event clips.
- [ ] Assign and mix final audio clips when they are supplied.

#### Where to assign future audio

In the generated `Kitchen` scene:

- Select `AUDIO - DROP CLIPS HERE`.
- On `AudioDirector`, assign the two music tracks and the named kitchen SFX fields.
- Select `Garden Ambience - Crickets Birds Wind` and assign its three child `Optional Loop` AudioSources.
- Select `Marsh Ambience - Water Paddle Frog Hiss` and assign its four child `Optional Loop` AudioSources.
- Assign optional event clips on the two jumping-fish `WildlifeMover` components and the rare snake `WildlifeMover` component.

Keep clip references serialized in the scene builder as well if the audio assets become permanent project files; otherwise rebuilding the scene will remove manual scene-only assignments.

### 8. Customer dialogue and order cards

- [x] Dialogue-cloud pieces are fully opaque, preventing dark overlap lines inside the cloud.
- [x] Dialogue remains visible for six seconds rather than disappearing after 3.5 seconds.
- [x] Order cards use smaller dimensions and type to reduce visual obstruction.

### 9. Credits on every major screen

- [x] Instructions screen.
- [x] Pause screen.
- [x] Quit confirmation screen.
- [x] Service-over/results screen.

### 10. Chef and customer animation

- [x] Existing breathing and body bobbing remain.
- [x] Named arm and leg meshes now receive subtle alternating idle motion.
- [x] Chef movement increases the limb swing rate and range.
- [x] Customer limb motion uses randomized phase offsets so all customers do not animate identically.
- [x] Movement-facing and Blender axis-correction transforms remain separate.

These are optimized procedural animations for the current separated low-poly mesh parts. They do not require an Animator Controller or humanoid skeleton.

### 11. Trash, spills, insects, and the former visitor

- [x] The Blender trash model now contains an authored `TrashLidHinge`.
- [x] Throwing away an item triggers a timed lid-open/lid-close animation.
- [x] The first discarded item schedules red-ant and fly effects after five to six seconds.
- [x] Further trash interactions do not stack unbounded insect coroutines.
- [x] Taking cheese or meat has a 50% spill chance.
- [x] Cheese and meat use distinct spill colors.
- Superseded in Progress 6: the former visitor behavior and model were removed.
- [x] Chopping creates green floor particles that stop emitting when preparation completes and disappear through bounded particle lifetimes.
- [x] Cooking creates red meat particles while at least one stove is active.
- [x] Shared cooking effects use an active-stove counter so one finishing stove does not stop effects for another active stove.
- [x] Particle counts and lifetimes are bounded; effects do not instantiate debris every frame.

### 12. Starting instructions presentation

- [x] The instructions card is larger and better structured.
- [x] Rules are grouped as Fridge, Prepare, Serve, and Clean Up.
- [x] Each group has a distinct color-coded UI icon.
- [x] Controls are displayed separately and concisely.
- [x] Credits remain accessible without competing with the primary Start Cooking action.

## Main files changed

| Area | Files |
|---|---|
| Runtime probes | `Assets/Scripts/Core/RuntimeVerifier.cs` |
| Refrigerator | `RefrigeratorStation.cs`, `RefrigeratorAnimator.cs`, `FridgeMenuController.cs` |
| Stations and trash | `ChoppingTableStation.cs`, `StoveStation.cs`, `TrashStation.cs` |
| Customers and labels | `CustomerWindow.cs`, `CharacterIdleMotion.cs`, `WorldLabelFader.cs` |
| Camera and wildlife | `AdaptiveCinemachineCamera.cs`, `WildlifeMover.cs` |
| Audio | `Assets/Scripts/Audio/AudioDirector.cs`, `AmbientAudioZone.cs` |
| Exterior motion | `ButterflyFlight.cs`, `FrogHopper.cs`, `WindSway.cs`, `WaterSurfaceAnimator.cs` |
| Kitchen micro-effects | `KitchenActivityEffects.cs`, `TrashLidAnimator.cs` |
| Scene generation | `Assets/Editor/YesChefSceneBuilder.cs` |
| Validation/build | `Assets/Editor/YesChefProjectValidator.cs` |
| Blender pipeline | `Tools/Blender/generate_low_poly_assets.py`, `ArtSource/YesChef_LowPoly.blend` |
| Generated scene | `Assets/Scenes/Kitchen.unity` |

New generated models include:

- `Assets/Art/Models/Bush.fbx`

Updated detailed/animated models include:

- `Fish.fbx`
- `Frog.fbx`
- `Snake.fbx`
- `TrashBin.fbx`

## Verification evidence

### Blender

- Blender `5.2.1 LTS` completed without generation errors.
- Generated 23 low-poly FBX assets.
- Updated `YesChef_LowPoly.blend` and preview image.

### Unity scene validation

- Editor version: `6000.3.11f1`.
- Validation ran in an isolated temporary project copy.
- Result: `YES_CHEF_VALIDATION_COMPLETE`.
- The expanded validator checks:
  - persistent audio director and two ambience zones;
  - two pause-menu audio toggles;
  - east-edge camera reveal settings;
  - wind and water-motion component counts;
  - butterflies and frog hopper;
  - kitchen activity effects and trash-lid animation;
  - two exterior spot lights;
  - bush model import;
  - all previous gameplay, recipe, customer, refrigerator, camera, UI, and scoring requirements.

### Windows build

- Development build result: Success.
- Reported total build size: `150830570` bytes.
- The temporary build was used only for verification and was not copied into source control.

### Runtime probes

- Refrigerator probe exit code: `0`.
- Refrigerator close evidence: `YES_CHEF_FRIDGE_E_CLOSE_WITH_HELD_ITEM: True`.
- Exterior camera probe exit code: `0`.
- East-edge delayed idle FOV: `57.91`.
- Normal delayed idle FOV remained approximately `47.45` during its transition.
- No runtime null-reference or missing-reference exceptions were found.

### Visual verification limitation

- Hidden Windows-player screenshots were entirely black even though runtime state and renderer logs were healthy.
- The Computer Use native helper was unavailable, so the already-open Unity Game view could not be inspected automatically.
- Do not claim visual approval from these captures. Perform a visible Play Mode review.

## Repository state and preservation notes

- Branch: `main`.
- Base commit: `95f2aa5 Polish camera characters customer flow and menus`.
- Progress 2 is uncommitted.
- Nothing was pushed.
- `.gitignore` was already locally modified to ignore `/tmp/`; it was preserved.
- `issues.txt` and the progress handoff documents are currently untracked unless committed later.
- The prior local `Kitchen.unity` diff was inspected before the scene was regenerated. It consisted mainly of Unity/TMP reserialization and camera transform values. The scene was then intentionally replaced by the updated scene-builder output.
- Unity-generated scene YAML contains normal trailing spaces on empty serialized values; source-code files pass `git diff --check` when the generated scene is excluded.

## Required manual review

Before calling Progress 2 fully presentation-approved:

1. Open `Assets/Scenes/Kitchen.unity` and accept/reload the externally updated scene if the already-open Unity Editor asks.
2. Check instructions, pause, quit confirmation, and results at 16:9.
3. Confirm every credit row is entirely clickable, including its icon area.
4. Pick an ingredient, reopen the fridge if needed, and verify `E` closes it while the chef is holding the item.
5. Walk around every station and confirm labels are readable at distance and fade near the chef.
6. Watch a complete frog route, both fish routes, the delayed one-time snake visit, butterflies, wind sway, and water facets.
7. Stand idle near the east wall and confirm the camera reveals the marsh without losing the chef.
8. Trigger chopping, both stoves, fridge spills, trash lid, ants, and flies.
9. Complete a full three-minute match with two-item, three-item, and duplicate orders.
10. Check 16:10, ultrawide, and a smaller resizable window.
11. Assign temporary test clips if available and verify playlist sequencing, all SFX hooks, ambience fades, toggles, and the three-second music rule.

## Known remaining risks

- Visual density and animation quality are subjective and may need tuning after the visible review.
- The generated scene is large because all environment and UI objects are serialized explicitly.
- Final audio balance cannot be evaluated until clips are supplied.
- Manual audio assignments made only in the scene will be lost when the scene builder runs; permanent clip references should also be added to the builder after the asset paths are known.
- Extended gameplay pacing and responsive-layout checks remain manual.

## How to continue safely

1. Read this file, `issues.txt`, `docs/ARCHITECTURE.md`, and `git status`.
2. Preserve unrelated working-tree changes.
3. Perform the visible manual checklist before adding further polish.
4. Record new reproducible issues in `issues.txt`.
5. Make the smallest responsible runtime change.
6. Mirror serialized hierarchy/reference changes in `YesChefSceneBuilder.cs`.
7. Regenerate Blender assets only when Blender source changes.
8. Run `YesChefProjectValidator.BuildAndValidate` in an isolated copy.
9. Produce a Windows build and distinguish build proof from runtime/visual proof.
10. Update this handoff before committing.
11. Do not include builds, temporary captures, Unity cache directories, the assignment PDF, or scratch tooling in a commit.
12. Do not push without explicit permission.

## Prompt for the next chat

```text
Continue development of the YesChef Unity project using yeschefprogress2.md as
the primary handoff. First read yeschefprogress2.md, issues.txt,
docs/ARCHITECTURE.md, and git status. Progress 2 implements all twelve listed
issues, but the changes are not committed or pushed. Preserve unrelated local
changes and keep the documented one-customer/random-arrival overrides.

Begin with the Required manual review in yeschefprogress2.md. Distinguish code,
scene validation, Windows build, runtime behavior, and visible presentation
proof. Final audio clips are not available yet, but the two-track music,
kitchen SFX, ambience zones, wildlife event audio, toggles, persistence, and
three-second resume/restart mechanics are ready. If hierarchy or serialized
references change, update YesChefSceneBuilder.cs too. Validate in an isolated
Unity copy and build Windows after changes. Do not add third-party plugins or
art packs. Do not commit or push unless I explicitly request it.
```
