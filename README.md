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

Controls will be documented here once the first playable slice is complete.

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

## Implementation order

1. Create the Unity 6000.3.11f1 Core 3D project in this repository folder. (Complete.)
2. Set asset serialization to **Force Text** and version-control mode to **Visible Meta Files**. (Verified.)
3. Block out one kitchen scene with walls, four windows, refrigerator, table, two-slot stove, trash, player, and a fixed top-down camera.
4. Build the player movement and a single reusable interaction interface.
5. Implement the ingredient state flow: raw, preparing, prepared, held, and delivered/discarded.
6. Implement orders and scoring independently of scene UI so the rules can be unit tested.
7. Add game states: instructions, playing, paused, and results/restart.
8. Add station/order UI, persistent high score, then polish and test.

## Submission notes

- The supplied assignment PDF is intentionally excluded from source control.
- Generated Unity folders, IDE files, local settings, and builds are ignored.
- Only Unity source assets, packages, project settings, and project documentation should be committed.
