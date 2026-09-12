# Yes Chef!

A small 3D, top-down kitchen game built for the Tentworks Interactive developer test.

## Development environment

- Unity 6000.3.11f1 (meets the assignment requirement of Unity 6000+)
- C#
- Target platform: Windows desktop
- Unity packages: Cinemachine 2.10.5 and Unity UI/TextMesh Pro
- Third-party plugins: none

## Core game loop

The player has three minutes to complete as many orders as possible across four serving tables. One customer begins at the counter and additional customers arrive gradually from the road. Ingredients come from the refrigerator and must be carried one at a time. Vegetables require two seconds at the chopping table, meat requires six seconds on either stove, and cheese can be delivered immediately.

Order score is the sum of ingredient values minus `floor(seconds open)`:

- Vegetable: 20 points
- Cheese: 10 points
- Meat: 30 points

## Controls

- **WASD / Arrow keys:** Move
- **E:** Interact, place, pick up, deliver, or discard
- **1 / 2 / 3 near the refrigerator:** Take vegetable, cheese, or meat
- **Escape:** Pause or resume
- Screen buttons: Start, Pause, Resume, Play Again, and Quit

## Implemented features

- Perspective top-down 3D kitchen driven by Cinemachine with smooth player follow
- 36-degree vertical FOV while playing and a smooth 28-degree idle zoom for 16:9 landscape presentation
- One starting customer plus staggered, randomized arrivals across four serving tables
- 50/50 selection between two- and three-ingredient orders
- All 36 ordered two- and three-ingredient recipe variations supported and validator-checked
- One-item player inventory
- Two-second vegetable chopping with countdown and prepared visual
- Two physically separate stove tables with independent six-second cooking, flame particles and pulsing point lights
- Direct cheese delivery and a trash station for all ingredient states
- Order scoring using ingredient value minus floored seconds open, including negative scores
- Short randomized customer return delay and fading score feedback
- Animated openable refrigerator with stocked shelves and a fading cool interior light
- Three-minute timer, instructions, translucent pause screen, quit confirmation, restart, current score, persistent high score, and linked credits
- Persistent bottom control/guidance strip plus a scrollable, clickable refrigerator catalogue
- Physical customers with randomized names, clothing, skin tones, hairstyles, and visible idle/walking animation
- Timed rounded customer speech clouds above their heads, serving-table order cards and fading score popups
- Outdoor road and walking customer routes, garden/flowers, tall windowed building, and animated faceted marsh with lotus plants
- Four independent daylight window cones and four outward-facing night spill lights
- Event-only kitchen mess effects plus delayed, persistent trash ants/flies with quiet spatial buzzing
- Symmetric neutral checker tiles plus patterned wall trim and motifs
- Original two-track calm felt-piano soundtrack, complete kitchen/customer SFX, dynamically timed exterior ambience, and proximity fly buzz
- Animated refrigerator pickup: the door opens with sound, the chef bends and reaches inside, the item appears in hand, and the door closes with sound
- Clean single-silhouette customer speech bubbles with a short entrance bounce
- Polished TextMesh Pro HUD, held-item display, proximity-fading station labels and activity-only progress bars

## Understanding the code

Start with [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). It explains the complete gameplay flow, each script's single responsibility, duplicate-order handling, scoring, and concise interview talking points.

## Low-poly art pipeline

- Editable Blender source: `ArtSource/YesChef_LowPoly.blend`
- Blender generation script: `Tools/Blender/generate_low_poly_assets.py`
- Unity-ready FBXs: `Assets/Art/Models/`
- Included assets: chef, refrigerator, chopping table, stove, trash bin, customer window, floor/wall modules, raw/chopped vegetables, cheese, and raw/cooked meat
- Additional Blender assets used in the scene: single stove, customizable customer, trees, flowers, lotus, and bush
- Frog, fish, snake, and butterfly audio slots remain available, but their visual models are intentionally not instantiated

Regenerate the models with Blender 5.2+ from the repository root:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python ".\Tools\Blender\generate_low_poly_assets.py"
```

## Original generated audio

- Reproducible generator: `Tools/Audio/generate_audio_assets.py`
- Output: `Assets/Audio/Generated/`
- Mix: background music at 72%, kitchen SFX at 22%, and exterior/spatial ambience at 16% or below
- Format: 44.1 kHz, 16-bit WAV; stereo music and mono positional effects
- Includes two 44-46 second music tracks, eight kitchen cues, two customer voice gestures, two soft environmental beds, five randomized wildlife details, and one trash-fly loop

Regenerate the complete audio pack with:

```powershell
python .\Tools\Audio\generate_audio_assets.py
```

The music and sound design are original deterministic synthesis and require no downloaded samples.

## Planned project structure

```text
Assets/
  Art/
  Audio/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Gameplay/
    Interactions/
    UI/
  Settings/
  Tests/
```

## Opening and testing

1. Open the repository folder in Unity `6000.3.11f1` or another Unity 6000+ editor.
2. Open `Assets/Scenes/Kitchen.unity`.
3. Press Play and select **Start Cooking**.
4. To regenerate the scene, use **Tools > Yes Chef > Build Playable Kitchen**.

The editor validator is `Assets/Editor/YesChefProjectValidator.cs`. It checks scene references, all required model imports, four customer windows, character orientation and idle motion, camera timing, refrigerator animation, TextMesh Pro assignment, station UI, two stove slots, and the scoring examples.

## Submission notes

- The supplied assignment PDF is intentionally excluded from source control.
- Generated Unity folders, IDE files, local settings, and builds are ignored.
- Only Unity source assets, packages, project settings, and project documentation should be committed.
