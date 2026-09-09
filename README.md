# Yes Chef!

A small 3D, top-down kitchen game built for the Tentworks Interactive developer test.

## Development environment

- Unity 6000.3.11f1 (meets the assignment requirement of Unity 6000+)
- C#
- Target platform: Windows desktop
- Third-party plugins: none

## Core game loop

The player has three minutes to complete as many of four active orders as possible. Ingredients come from the refrigerator and must be carried one at a time. Vegetables require two seconds at the chopping table, meat requires six seconds in one of two stove slots, and cheese can be delivered immediately. Completed orders respawn after five seconds.

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

- Fixed top-down 3D kitchen with the entire play area visible
- Four simultaneous randomized orders with duplicate ingredients supported
- 50/50 selection between two- and three-ingredient orders
- One-item player inventory
- Two-second vegetable chopping with countdown and prepared visual
- Two independent six-second meat cooking slots with countdowns and cooked visual
- Direct cheese delivery and a trash station for all ingredient states
- Order scoring using ingredient value minus floored seconds open, including negative scores
- Five-second order respawn delay and fading score feedback
- Three-minute game timer, instructions, pause, results, restart, quit, current score, and persistent high score
- TextMesh Pro HUD and world-space station/order displays

## Low-poly art pipeline

- Editable Blender source: `ArtSource/YesChef_LowPoly.blend`
- Blender generation script: `Tools/Blender/generate_low_poly_assets.py`
- Unity-ready FBXs: `Assets/Art/Models/`
- Included assets: chef, refrigerator, chopping table, stove, trash bin, customer window, floor/wall modules, raw/chopped vegetables, cheese, and raw/cooked meat

Regenerate the models with Blender 5.2+ from the repository root:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python ".\Tools\Blender\generate_low_poly_assets.py"
```

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

The editor validator is `Assets/Editor/YesChefProjectValidator.cs`. It checks scene references, all 13 model imports, four customer windows, TextMesh Pro font assignment, two stove slots, required UI, and the scoring examples.

## Submission notes

- The supplied assignment PDF is intentionally excluded from source control.
- Generated Unity folders, IDE files, local settings, and builds are ignored.
- Only Unity source assets, packages, project settings, and project documentation should be committed.
