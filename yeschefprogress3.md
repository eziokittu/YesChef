# YesChef Progress 3

Last updated: 2026-09-10  
Progress 3 state: implemented and verified locally, but not committed or pushed

## Status verdict

New issues 13-16 are implemented in runtime code, the editable scene builder, the generated Kitchen scene, and the Blender source pipeline.

- The timed day/night/day cycle follows the requested 0/50/70/110/130/180-second boundaries.
- Frog, fish, snake, and butterfly assets were rebuilt in Blender and imported as FBX models.
- Every major menu uses the official GitHub Invertocat and the reviewed 1280x720 layouts no longer overlap.
- Idle camera framing now reveals the marsh/garden at the east and south windows and the customer road on the west.
- Unity validation, the Windows build, runtime probes, and visible standalone captures pass.
- Final audio clips remain pending. Nothing was committed or pushed.

## Main implementation

### Lighting

`DayNightCycle` owns a global morning point light, two independent exterior window spot lights, and six sequential kitchen lights. Daylight is full through 50 seconds, fades to zero by 70, stays off until 110, returns by 130, and stays full through the end. The kitchen fixtures cross-fade in the opposite direction with staggered bounded flicker during switching.

### Blender wildlife

`Tools/Blender/generate_low_poly_assets.py` now generates 24 FBX files. Frog, fish, and snake have clearer anatomy and face markings. `Butterfly.fbx` contains authored body, antennae, bordered fore/hind wings, spots, and named left/right wing pivots used by `ButterflyFlight`.

### UI

`Assets/UI/Brand/GitHub_Invertocat_White.png` is the official high-contrast GitHub mark. The GH letters were removed. Instruction icons have a separate column, modal spacing was rebalanced, results and Start buttons do not collide with credits, pause toggles fit on one line, and panels/buttons use restrained drop shadows.

### Camera

`AdaptiveCinemachineCamera` now detects idle east, south, and west edge zones. It widens smoothly and moves the framing target so the chef remains visible while the marsh, garden, or customer road/dialogues receive more screen space.

## Verification evidence

- Blender: 5.2.1 LTS, 24 FBX assets, `.blend` and preview regenerated successfully.
- Unity: 6000.3.11f1 isolated `BuildAndValidate` passed after the final rebuild.
- Windows development build: success, 151658942 bytes.
- Night probe: daylight `0.00`, sun intensity `0.00`, minimum kitchen-light intensity `2.25`.
- East idle probe: chef screen X `447.63`, final FOV `57.93`.
- West idle probe: chef screen X `793.95`, final FOV `57.93`.
- Visible 1280x720 captures inspected: instructions, pause, morning, night, east marsh, and west customer road.
- No `NullReferenceException` or `MissingReferenceException` in the runtime logs.

## Verification boundary

The Computer Use native bridge was unavailable after its required retry, so the already-open Unity Editor window could not be clicked. Unity itself was exercised through isolated editor batch validation, scene generation, and a real Windows player. The visible player captures were non-black and inspected directly. A user-driven Editor Play Mode pass can still be useful for subjective animation tuning and a full three-minute manual service.

## Preservation notes

- Existing uncommitted Progress 2 work was preserved.
- The scene builder was updated alongside serialized hierarchy/reference changes.
- Temporary builds, logs, downloaded logo archives, and captures remain under ignored temporary paths.
- Do not push without explicit permission.
