# Yes Chef architecture walkthrough

This project is deliberately split into small scripts with one clear job each. The scene stores references in the Inspector; gameplay scripts do not search for objects by name or rebuild the kitchen at runtime. The editor scene-builder exists only to generate the editable demonstration scene.

## The game in one sentence

`GameManager` controls the three-minute match, stations transform ingredients, `OrderTicket` tracks recipe progress, `CustomerWindow` runs each physical customer visit, and Cinemachine presents the world from a following top-down perspective.

## Main runtime flow

1. `GameManager` starts on the instruction screen with time paused.
2. **Start Cooking** resets the chef, stations, score, timer, and customer schedule. One customer starts immediately; the other tables fill at staggered random intervals.
3. `PlayerController` reads movement and interaction input.
4. `PlayerInventory` enforces the one-held-item rule.
5. The refrigerator creates raw ingredients through `IngredientFactory`.
6. The chopping table prepares vegetables in 2 seconds; either of two separate stove tables cooks one meat in 6 seconds; cheese is already delivery-ready.
7. A customer window accepts only a prepared ingredient that occurs in its remaining list.
8. Completing an order awards ingredient points minus `floor(seconds open)`, then that window waits a short random interval before its next customer walks in.
9. When the match timer reaches zero, `GameManager` saves a new high score with `PlayerPrefs` and displays the result screen.

## Script responsibilities

| Script | Responsibility |
|---|---|
| `GameManager` | Match state, timer, score, high score, and screen panels |
| `GameTypes` | Shared enums, ingredient rules, colors, and scoring formula |
| `OrderTicket` | Pure runtime order data: required items, remaining items, age, and score |
| `OrderGenerator` | Exact 50/50 choice of 2 or 3 slots and independent random ingredients |
| `PlayerController` | Movement, upright visual rotation, nearest interaction, and keyboard commands |
| `CharacterIdleMotion` | Small breathing/bobbing motion on a mesh child without changing movement-facing rotation |
| `PlayerInventory` | The one-item hand and transfer methods |
| `IngredientFactory` | Creates an ingredient and chooses the correct low-poly model |
| `IngredientItem` | Ingredient type/state and visual switch from raw to prepared |
| Station scripts | Accept, process, return, or discard ingredients; each stove also drives its own flame/light effect |
| `CustomerWindow` | Customer arrival/departure, order delivery, serving card, timed dialogue and score feedback |
| `CustomerAvatar` | Chooses from 50 names and randomizes skin, clothes and hairstyle |
| `CharacterIdleMotion` | Animates chef/customer motion roots with idle breathing/sway, route-aware walking, pivoted limbs, and the chef's fridge reach pose |
| `FridgeMenuController` | Opens the scrollable screen catalogue and routes button choices to the refrigerator |
| `RefrigeratorAnimator` | Smooth Blender-hinge door animation and synchronized interior-light fade |
| `WorldLabelFader` | Softens station labels near the chef and hides them while a station surface is occupied |
| `DialogueBubbleAnimator` | Plays the short unscaled-time overshoot when a clean customer speech bubble appears |
| `ExternalLinkButton` | Opens the Glitchbong contact page and public source URL from the credits UI |
| `AdaptiveCinemachineCamera` | Smooth Cinemachine follow lens with idle marsh, garden, and customer-road edge reveals |
| `DayNightCycle` | Three-minute morning/sunset/night/sunrise timeline, four independent daylight window cones, and four outward night spill cones |
| `AudioDirector` | Persistent generated two-track felt-piano playlist, customer arrival/success gestures, focus/restart recovery, automatic Main Camera listener repair, music/SFX preferences, and a music-forward mix |
| `AmbientAudioZone` | Distance-faded wind/water beds plus independently randomized bird, cricket, splash, frog, and hiss one-shots with small pitch/stereo variation |
| `WindSway`, `WaterSurfaceAnimator` | Lightweight procedural plant and faceted-water motion |
| `KitchenActivityEffects` | Event-only bounded spill particles and persistent trash insects with quiet spatial buzzing |
| `TrashLidAnimator` | Reusable timed lid hinge animation |
| `Billboard` | Rotates world-space TMP cards toward the active Cinemachine-driven camera |

## Why duplicate orders work

An order keeps two lists:

- `RequiredIngredients` never changes and is used for display and final scoring.
- `RemainingIngredients` removes only one matching occurrence for each delivery.

For `Meat + Meat + Cheese`, the first meat delivery removes one meat, not both. The editor validator exercises all 36 ordered recipes: 9 two-item sequences plus 27 three-item sequences.

The 50/50 rule applies to order length first. After that, each slot independently picks vegetable, cheese, or meat, so duplicate ingredients are naturally possible.

## Useful explanation for an interview

The most important design decision is separating definitions, runtime state, and presentation. Ingredient rules are shared static data. An `OrderTicket` is plain C# state. `CustomerWindow` is the Unity presentation/interaction layer. This lets the order rules be validated independently and keeps every MonoBehaviour focused on a scene-related responsibility.

The stations use the same pattern: validate the held item, transfer ownership from the inventory to a station anchor, advance a timer, change preparation state, and allow collection. Two separate `StoveStation` instances make the cooking positions visually and logically independent.

The real Camera contains the scene's single enabled `AudioListener` and a `CinemachineBrain`; a `CinemachineVirtualCamera` follows the player through a framing transposer. `AdaptiveCinemachineCamera` waits for three idle seconds before slowly changing the perspective field of view, keeping camera responsibilities separate from movement.

The chef and customer hierarchy separates facing/route movement, procedural posing, and Blender model-axis correction. A dedicated motion root sits above the imported visual, while Blender-authored `ArmPivot_*` and `LegPivot_*` transforms provide correct shoulder/hip rotation. This prevents navigation rotation and animation from fighting each other. Refrigerator selection is asynchronous: shortcuts are accepted only from the open catalogue, wait for visible door openness, lock the chef for a short reach pose, create the ingredient midway, then close and unlock.

## Editing common rules

- Match length: `GameManager.matchSeconds`
- Vegetable time: `ChoppingTableStation.preparationSeconds`
- Meat time: each `StoveStation.cookingSeconds`
- Customer return interval: `CustomerWindow.respawnMinimum` and `respawnMaximum`
- Ingredient points: `IngredientRules.Score` in `GameTypes.cs`
- Player speed/range: `PlayerController.moveSpeed` and `interactionRadius`

These values are visible in the Inspector because the scene references and tuning values are serialized.
