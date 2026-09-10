# YesChef Progress 4

Last updated: 2026-09-10  
Progress 4 state: implemented and verified locally, but not committed or pushed

## Status verdict

Issues 17-22 are implemented in runtime code, the editable scene builder, and the generated Kitchen scene.

- Animal visuals were completely removed from scene generation while the future wildlife audio slots were preserved.
- All four physical windows now light independently in both directions across the day/night cycle.
- Cheese spills can summon a rat from any of four kitchen corners.
- Kitchen mess, ant, and fly particles stay hidden until their matching gameplay events.
- The kitchen floor uses a centered symmetric tile pattern, while the walls retain coordinated geometric trim.
- The marsh uses more visible phase-offset vertical waves with subtle tilt and scale movement.

## Main implementation

### Exterior visuals and lighting

`YesChefSceneBuilder` no longer loads or instantiates frog, fish, snake, or butterfly models. Lotus plants and moving water remain. Four daylight spotlights originate outside the two south and two east panes; four warm night spotlights originate inside and point back through the same panes. `DayNightCycle` cross-fades these independent groups with the established 50/70/110/130-second timeline.

### Kitchen activity

`KitchenActivityEffects` clears all pools on scene start and match restart. Cheese and meat retain a 50% spill chance, but only cheese crumbs schedule a rat. The rat chooses one of four corner entrances, visits the food point after 2-3 seconds, and returns to its chosen entrance. Chopping and cooking particles only emit while those stations are active. The first trash discard schedules ground-level red ants and small noise-driven orbiting flies after 5-6 seconds.

### Surfaces and water

The floor uses a charcoal grout base, brass perimeter, and a neutral 9-by-7 checker layout mirrored across both axes. Walls use warm plaster, green wainscot, brass rails, and terracotta motifs. These are collider-free scene-authored pieces, so they do not affect movement. Each water facet now combines phase-offset up/down movement, slight two-axis tilt, and subtle length variation.

## Verification evidence

- Unity 6000.3.11f1 isolated `BuildAndValidate`: passed after final scene regeneration.
- Windows development build: passed, 151022977 bytes.
- Runtime initial state: chop `False`, stove `False`, ants `False`, flies `False`, rat `False`.
- Runtime triggered state: chop `True`, stove `True`.
- Rat probe: active after the delay, selected entrance index `0`, and moving from that corner.
- Trash probe: 11 ant particles and 6 fly particles after the delay; no particle curve warnings.
- Night probe: daylight `0.00`, sun `0.00`, day windows `0.00`, night windows `1.65`, kitchen minimum `2.25`.
- Visible 1280x720 Windows capture inspected: patterned surfaces render correctly and wildlife visuals are absent.
- Runtime logs contained no `NullReferenceException` or `MissingReferenceException`.

## Verification boundary

The generated Windows build and visible capture were produced from an isolated project copy because the main Unity Editor was already open. Final audio clips are still intentionally absent. A user-driven Play Mode pass remains useful for subjective tuning of wave amplitude, surface colors, and particle density.

## Preservation notes

- Existing uncommitted Progress 2 and Progress 3 work was preserved.
- The scene builder and generated Kitchen scene were updated together.
- No commit or push was performed.
