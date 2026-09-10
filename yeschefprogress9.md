# YesChef Progress 9

Last updated: 2026-09-10
Base revision: `2186d12c18cbbd3b340792df8ab680cdcf3207ea`
Progress 9 state: implemented, verified, committed, and pushed to `origin/main`

## Status verdict

Chef/customer animation, refrigerator pickup sequencing, refrigerator door audio routing, the clipped customer speech cloud, and exclusive quit-confirmation menu transitions are fixed in the runtime code, Blender source/models, reproducible scene builder, validator, generated UI asset, and `Kitchen.unity`.

## Implemented changes

- Rebuilt the chef and customer models with two named shoulder pivots and two named hip pivots each. Their arms now hang naturally from the shoulders instead of rotating around the middle of a horizontal mesh.
- Moved procedural posing onto dedicated motion roots below each facing transform. This keeps navigation rotation, Blender axis correction, and animation independent.
- Idle characters now show visible breathing, vertical bob, body sway, and gentle arm/leg movement.
- Walking characters now show alternating 40-degree limb swing, stronger step bounce, and a small forward lean.
- Customer animation detects movement of its route parent, fixing the former player-only condition that left walking customers in their idle pose.
- Refrigerator number shortcuts now require the fridge catalogue to be open; standing nearby can no longer place an item directly into the chef's hand.
- A selection made immediately after pressing E is queued while the door opens. The chef faces the refrigerator, movement is briefly locked, the body bends forward and both arms reach inside, the item appears midway through the reach, and the door closes before control returns.
- Existing generated refrigerator-open and refrigerator-close clips remain assigned and now correspond to the visible open/close sequence instead of being bypassed by direct quick-pick behavior.
- Regenerated the original speech-cloud PNG at 600 by 250 with safe padding above every scallop. Unity preserves its aspect ratio, and the world-space dialogue layout is taller with corrected text padding.
- Opening quit confirmation from Pause or Game Over now disables the originating panel. Cancelling quit restores the correct panel and phase instead of leaving layered menus visible.

## Main files

- `Tools/Blender/generate_low_poly_assets.py`
- `ArtSource/YesChef_LowPoly.blend`
- `Assets/Art/Models/Chef.fbx`
- `Assets/Art/Models/Customer.fbx`
- `Assets/Scripts/Gameplay/CharacterIdleMotion.cs`
- `Assets/Scripts/Gameplay/PlayerController.cs`
- `Assets/Scripts/Gameplay/RefrigeratorAnimator.cs`
- `Assets/Scripts/Interactions/RefrigeratorStation.cs`
- `Assets/Scripts/UI/FridgeMenuController.cs`
- `Tools/Art/generate_ui_assets.py`
- `Assets/UI/CustomerSpeechBubble.png`
- `Assets/Editor/YesChefSceneBuilder.cs`
- `Assets/Editor/YesChefProjectValidator.cs`
- `Assets/Scripts/Core/RuntimeVerifier.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scenes/Kitchen.unity`

## Verification evidence

- Blender 5.2.1 regenerated 23 FBX assets, the editable `.blend`, and preview successfully. Only the changed chef/customer FBX outputs remain in the working diff.
- Unity 6000.3.11f1 isolated `BuildAndValidate` passed with five motion roots, two shoulder pivots and two hip pivots per character, the new fridge wiring, and four corrected dialogue layouts.
- A fresh Windows development build passed at `154,438,213` bytes after the quit-modal fix.
- Fridge runtime probe exited `0`: `queued=True`, door openness `1.00`, reach active with no held item, then pickup completed with an item held, action lock released, and menu closed.
- Character runtime probe exited `0`: five motion controllers found, idle displacement `0.006`, customer walking observed, and the chef reported two arm plus two leg pivots.
- The regenerated speech bubble was visually inspected at its original resolution; all scallops and both thought dots have complete outlines. Alpha bounds are inset from the 600-by-250 canvas at `(30, 13, 567, 239)`, leaving transparent exterior margins on every side.
- The quit-modal runtime probe exited `0`: `pauseHidden=True`, `pauseRestored=True`, `reachedResults=True`, `resultsHidden=True`, and `resultsRestored=True`.

## Publication and review boundary

Progress 9 was committed and pushed to `origin/main` after validation. The main Unity Editor was open, so validation, scene regeneration, build, and runtime probes used an isolated copy. The prior local `Kitchen.unity` difference contained only Unity/TextMesh Pro serialization normalization; the requested implementation regenerated the scene from the authoritative builder.

A normal visible Play Mode pass remains recommended to judge animation strength, fridge reach direction, door-sound loudness, and dialogue positioning from the final camera. If the main Editor still holds the older scene in memory, reload `Assets/Scenes/Kitchen.unity` from disk before saving.
