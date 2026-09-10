# YesChef Progress 1

Last updated: 2026-09-10  
Current completed revision: uncommitted Progress 2 working tree (base `95f2aa5f1047ec6221f6ff704fe23250652aaad4`)  
Repository: <https://github.com/eziokittu/YesChef>

This is the primary continuation document for YesChef. Update the checklists and the session log at the bottom whenever the game changes.

## Current status

YesChef is a playable Unity 6000+ Windows prototype with the complete ingredient, preparation, order, scoring, timer, pause, restart, and high-score loop. The scene uses original low-poly Blender assets, TextMesh Pro UI, and Unity Package Manager Cinemachine. No Asset Store art packs or third-party plugins are used.

The Progress 2 issue implementation, latest Windows build, expanded automated scene validator, and runtime behavior probes passed. Final audio clips and a visible subjective presentation playtest remain; hidden-player screenshots were black in this Windows session and are not counted as visual proof.

## Requirement sources and priority

There are two sources of requirements:

1. **Assignment requirements** come from `New_Developer_Test.pdf`.
2. **Project-owner changes** are later instructions that intentionally expand or override parts of the assignment.

When they conflict, the later project-owner instruction is the current design decision. These differences must remain documented so they can be explained during review.

## Assignment requirement checklist

Checklist legend:

- `[x]` implemented and verified.
- `[~]` implemented differently because of a later project-owner decision.
- `[ ]` pending, unverified, or requiring a decision.

### Project constraints

- [x] Unity and C#.
- [x] Unity version 6000 or newer. Current editor version is `6000.3.11f1`.
- [x] Shareable source repository. The GitHub repository is public.
- [x] 3D game with an overhead view of the gameplay area.
- [x] Only Unity Package Manager/default Unity packages are used.
- [x] No third-party art package is used; low-poly assets were created locally in Blender.
- [x] Sound is optional and is not currently included.

### Kitchen and controls

- [x] Kitchen has four walls and recognizable gameplay stations.
- [x] Refrigerator supplies raw ingredients and never runs out.
- [x] Chopping table prepares vegetables.
- [x] Two cooking positions are available as two separate stove tables.
- [x] Four serving/customer positions exist along the left side.
- [x] Trash accepts raw or prepared held ingredients.
- [x] Player can move, interact, and visibly carry one ingredient.
- [x] Controls are shown before starting and remain available in the bottom help strip and pause screen.

### Match flow

- [x] Each match lasts three minutes.
- [x] Score and remaining time are visible during play.
- [x] High score persists between sessions through `PlayerPrefs`.
- [x] Match result acknowledges a new high score.
- [x] Play Again resets the match.
- [x] Pause and quit controls exist.
- [x] Quit uses a confirmation screen before closing the application.
- [x] Maximum simultaneous order capacity is four.
- [~] The PDF requests four open orders at match start. This was intentionally overridden: the current game starts with one customer and staggers later arrivals.
- [~] The PDF requests an exact five-second order respawn. This was intentionally overridden with a short randomized customer return interval for better pacing.

### Ingredients and preparation

- [x] Vegetable is worth 20 points and must be chopped.
- [x] Cheese is worth 10 points and can be served directly.
- [x] Meat is worth 30 points and must be cooked.
- [x] Player can hold only one ingredient at a time.
- [x] Chopping table accepts one vegetable and takes two seconds.
- [x] Stove capacity is two pieces of meat across two independent stove stations.
- [x] Meat takes six seconds and continues cooking while the player walks away.
- [x] Raw and prepared ingredients have distinct physical models.
- [x] Chopping and cooking progress UI appears only while a station is active.
- [x] Prepared ingredients can be collected and carried to a matching order.

### Orders and scoring

- [x] Each active table shows required ingredients and elapsed order time.
- [x] Invalid or unnecessary deliveries remain in the player's hand.
- [x] Completed-order score feedback appears near the table and fades.
- [x] Orders have a 50% chance of two ingredients and a 50% chance of three.
- [x] Every ingredient slot is independently randomized.
- [x] Duplicate ingredients, including three identical ingredients, are supported.
- [x] All 36 ordered two-item and three-item recipe sequences are validator-tested.
- [x] Score is ingredient value minus `floor(order seconds)`.
- [x] Negative order scores are supported.
- [x] The assignment example `Cheese + Meat at 14.99 seconds = 26` is validator-tested.

## Additional project-owner requirements

### Camera and orientation

- [x] Perspective Cinemachine camera follows the player.
- [x] Camera has a wider, visibly angled view from the garden side.
- [x] Chef remains upright during movement; movement rotates only the Y-up facing parent.
- [x] Customer route rotation is separated from Blender mesh-axis correction.
- [x] Camera stays wide while moving.
- [x] Idle zoom waits three seconds before beginning.
- [x] Idle zoom is gradual and returns wide when movement resumes.
- [x] Runtime evidence recorded FOV `52.00` before the delay and `47.44` after the delayed transition began.

### Interaction presentation

- [x] TextMesh Pro HUD and world-space text are readable at the tested 16:9 resolution.
- [x] Station labels are compact and positioned close to the top edge of their objects.
- [x] Station labels partially fade when the player approaches.
- [x] Stove and chopping labels disappear while their surfaces are occupied.
- [x] Empty progress-bar backgrounds are hidden.
- [x] Bottom help strip shows controls and contextual next-step guidance.
- [x] Held ingredient is shown through HUD state and a physical carried model.

### Refrigerator

- [x] Right-side scrollable ingredient catalogue.
- [x] Clickable Vegetable, Cheese, and Meat entries.
- [x] Direct `1`, `2`, and `3` quick-pick controls remain available nearby.
- [x] Refrigerator FBX has an authored `MainDoorHinge`.
- [x] Refrigerator contains visible stocked shelves and ingredient props.
- [x] Door opens smoothly with the catalogue and closes when leaving, closing, or taking an item.
- [x] Cool interior point light fades from intensity 0 to 1 while opening and back to 0 while closing.

### Customers

- [x] One customer is active when a match begins.
- [x] Other customers arrive gradually from alternating road directions.
- [x] Arrival and return delays are randomized and staggered.
- [x] Customers face the service counter after arriving.
- [x] Chef and customers have subtle procedural idle motion.
- [x] Customers use 50 predefined Indian and Western names.
- [x] Clothing, skin tone, hair colour, and hairstyle are randomized.
- [x] Dialogue changes according to order age and includes compliments, general conversation, thanks, and frustration.
- [x] Dialogue uses a rounded cloud above the customer's head.
- [x] Order card stays on the matching serving table.
- [x] Inactive tables do not display empty order cards.

### Lighting, effects, and world

- [x] Multiple warm room point lights replace reliance on one global light.
- [x] Each active stove has ember particles and a pulsing orange point light.
- [x] Outdoor grass surrounds the kitchen.
- [x] Left-side road runs from top to bottom and supports customer routes.
- [x] Tall northern building has visible depth, roof, and windows.
- [x] Southern garden contains low-poly trees, grass, and flowers.
- [x] Eastern marsh contains water, lotus, frogs, moving fish, and rare snake appearances.

### Menus and credits

- [x] Top-right pause control uses a graphical two-bar icon.
- [x] Pause screen uses a translucent full-screen overlay.
- [x] Pause screen displays current score, best score, controls, resume, and quit.
- [x] Resume and Keep Cooking buttons include a supported play-style marker.
- [x] Quit opens an attractive confirmation message instead of quitting immediately.
- [x] Supplied Glitchbong logo is included in the credits area.
- [x] `Developed by Glitchbong` opens <https://glitchbong.com/contact>.
- [x] `Check GitHub source code` opens <https://github.com/eziokittu/YesChef>.

## Architecture map

| Area | Main files |
|---|---|
| Match state and HUD | `Assets/Scripts/Core/GameManager.cs`, `GameTypes.cs` |
| Player movement and inventory | `Assets/Scripts/Gameplay/PlayerController.cs`, `PlayerInventory.cs` |
| Orders | `OrderTicket.cs`, `CustomerWindow.cs`, `CustomerAvatar.cs` |
| Ingredients | `IngredientFactory.cs`, `IngredientItem.cs` |
| Stations | `ChoppingTableStation.cs`, `StoveStation.cs`, `RefrigeratorStation.cs`, `TrashStation.cs` |
| Camera | `AdaptiveCinemachineCamera.cs` |
| Character presentation | `CharacterIdleMotion.cs` |
| Refrigerator presentation | `RefrigeratorAnimator.cs`, `FridgeMenuController.cs` |
| World labels | `WorldLabelFader.cs`, `Billboard.cs` |
| External links | `ExternalLinkButton.cs` |
| Environment motion | `WildlifeMover.cs` |
| Audio and ambience | `AudioDirector.cs`, `AmbientAudioZone.cs` |
| Exterior motion | `WildlifeMover.cs`, `FrogHopper.cs`, `ButterflyFlight.cs`, `WindSway.cs`, `WaterSurfaceAnimator.cs` |
| Kitchen micro-effects | `KitchenActivityEffects.cs`, `TrashLidAnimator.cs` |
| Editable scene construction | `Assets/Editor/YesChefSceneBuilder.cs` |
| Automated validation/build | `Assets/Editor/YesChefProjectValidator.cs`, `RuntimeVerifier.cs` |
| Blender source pipeline | `Tools/Blender/generate_low_poly_assets.py`, `ArtSource/YesChef_LowPoly.blend` |

See `docs/ARCHITECTURE.md` for the detailed code walkthrough.

## Current tuning values

| Setting | Current value | File/component |
|---|---:|---|
| Match length | 180 seconds | `GameManager.matchSeconds` |
| Player speed | 5.5 | `PlayerController.moveSpeed` |
| Interaction radius | 1.65 | `PlayerController.interactionRadius` |
| Vegetable preparation | 2 seconds | `ChoppingTableStation.preparationSeconds` |
| Meat cooking | 6 seconds | `StoveStation.cookingSeconds` |
| Customer return | 2.5-6 seconds | `CustomerWindow.respawnMinimum/Maximum` |
| Moving camera FOV | 52 | `AdaptiveCinemachineCamera.movingFieldOfView` |
| Idle camera FOV | 42 | `AdaptiveCinemachineCamera.idleFieldOfView` |
| Idle delay | 3 seconds | `AdaptiveCinemachineCamera.idleDelay` |
| Zoom smoothing | 1.8 seconds | `AdaptiveCinemachineCamera.zoomSmoothTime` |

## Latest verification evidence

- [x] Blender 5.2 generator completed and exported 21 low-poly FBX assets.
- [x] Unity editor scene validation completed successfully.
- [x] Upright chef bounds and floor collider validation passed.
- [x] Four customer routes, four idle components plus chef idle component, and customer UI references passed.
- [x] Two independent stove stations, particle materials, lights, and progress UI passed.
- [x] Refrigerator hinge, interior light, scroll view, and logo sprite references passed.
- [x] Cinemachine follow, perspective camera, three-second delay, and slow zoom validation passed.
- [x] Windows development build completed successfully.
- [x] Gameplay, refrigerator, pause, and quit runtime captures exited with code 0.
- [x] No runtime `NullReferenceException` or `MissingReferenceException` occurred in those captures.
- [x] Public GitHub revision and local commit were synchronized at `95f2aa5`.
- [x] Progress 2 Blender generator exported 23 FBX assets, including the new rat and bush.
- [x] Expanded isolated validator passed audio, ambience, exterior motion, water, camera reveal, UI toggles, micro-effects, light cones, and new-model checks.
- [x] Progress 2 Windows development build completed successfully (reported total size `150830570` bytes).
- [x] Runtime fridge probe logged `YES_CHEF_FRIDGE_E_CLOSE_WITH_HELD_ITEM: True`.
- [x] Runtime east-edge probe placed the chef at screen X `672.89` and widened the delayed idle FOV to `57.91`.
- [x] No runtime `NullReferenceException` or `MissingReferenceException` was reported by the Progress 2 behavior probes.

## Known issues and manual checks still required

- [x] The twelve concrete Progress 2 issues in `issues.txt` are implemented and individually status-mapped.
- [ ] Complete a full three-minute manual match with mixed two-item, three-item, and duplicate orders.
- [ ] Playtest customer pacing during both very fast and slow service and tune the arrival ranges if needed.
- [ ] Check customer walking/facing from both the top and bottom road spawn points over several cycles.
- [ ] Test refrigerator open/close repeatedly while walking out of range and while selecting each item.
- [ ] Test pause, resume, cancel quit, confirm quit, contact link, and repository link in a standalone build.
- [ ] Review UI at 16:9, 16:10, ultrawide, and smaller window sizes for overlap or clipping.
- [ ] Confirm the exterior building never occludes the camera at every reachable player position.
- [ ] Decide whether the submission should keep the one-customer/random-arrival override or restore the PDF's four-starting-orders/exact-five-second behavior.
- [ ] Optional: add original sound effects and music only if time permits; the assignment does not require audio.

## Repository state at this handoff

Before this file was created, `git status` showed:

- `Assets/Scenes/Kitchen.unity` modified locally after the last pushed commit, likely from the open Unity Editor reserializing the scene.
- `issues.txt` untracked.

Do not discard either file automatically. Inspect the scene diff and update `issues.txt` with concrete observations before the next commit.

## How to continue development

1. Add every new problem to `issues.txt` using the template below.
2. Reproduce it in Play Mode and record the exact scene state.
3. Update the smallest responsible runtime script.
4. If hierarchy or serialized references change, also update `YesChefSceneBuilder.cs`.
5. Rebuild the editable scene with **Tools > Yes Chef > Build Top-Down World**. This command replaces the generated scene hierarchy, so save intentional manual scene edits elsewhere first.
6. Run `YesChefProjectValidator.BuildAndValidate` in an isolated Unity project copy.
7. Produce and playtest a Windows build.
8. Update this document's checklist, verification section, and session log.
9. Check `git status` and the staged file list before committing. Do not commit builds, Unity cache folders, the assignment PDF, or local scratch files.

### Issue template

```text
Issue title:
Observed behavior:
Expected behavior:
Steps to reproduce:
Frequency:
Screenshot/video:
Likely script or object:
Status: Open
```

## Progress log

### Progress 1 - 2026-09-10

- Completed the assignment gameplay loop and all 36 order variations.
- Built the original low-poly Blender asset pipeline and expanded outdoor environment.
- Added the perspective Cinemachine follow camera and delayed adaptive zoom.
- Corrected persistent chef/customer orientation using separate facing and model-correction transforms.
- Added customer arrivals, randomized appearance/names, idle motion, dialogue clouds, and table order cards.
- Added proximity-fading station labels and activity-only progress displays.
- Rebuilt the refrigerator with an animated door, stocked interior, and fading light.
- Added improved pause and quit flows with Glitchbong and GitHub credits.
- Validated, built, runtime-tested, committed, pushed, and made the repository public.

### Progress 2 - 2026-09-10

- Issues addressed: all 12 implementation requests in `issues.txt`, including refrigerator close behavior, UI/credits, readable labels, exterior life, kitchen spacing/windows, audio mechanics, dialogue/order sizing, character motion, trash/mess systems, and instructions polish.
- Files changed: runtime gameplay/UI scripts, nine new audio/environment scripts, scene builder, validator, runtime verifier, Blender generator/source/FBX exports, generated scene/materials, `issues.txt`, architecture notes, and this handoff.
- Tests completed: Blender 5.2 generation; isolated Unity compile/build-and-validate; Windows development build; fridge and exterior runtime probes.
- Remaining risks: visible Play Mode visual review, full manual match/regression matrix, responsive-resolution checks, and final audio clip assignment/mix. Hidden automated captures were black and provide no visual evidence.
- Commit: not committed. Do not push without explicit permission.

## Starting the next chat

Use this prompt in the next development chat:

```text
Continue development of the YesChef Unity project. First read yeschefprogress1.md,
issues.txt, docs/ARCHITECTURE.md, and git status. Treat New_Developer_Test.pdf as
the original assignment requirements and the documented project-owner overrides
as the current design. Do not add third-party plugins or art packages. Preserve
unrelated local changes. Reproduce each actionable issue before changing code,
update the scene builder when serialized hierarchy changes, validate in an
isolated Unity copy, make a Windows build, and update yeschefprogress1.md with
the result. Do not push unless I explicitly ask you to push.
```

At the start of the next chat, confirm these points before editing:

- Read the latest entries and implementation-status section in `issues.txt`.
- Inspect the local modification to `Assets/Scenes/Kitchen.unity` before rebuilding or replacing the scene.
- Run `git status` and preserve any changes that do not belong to the current issue.
- Check whether the requested behavior follows the assignment or one of the later documented overrides.
- Keep generated builds, temporary captures, the assignment PDF, and local tooling out of source control.
