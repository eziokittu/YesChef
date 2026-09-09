import bpy
import math
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "Assets" / "Art" / "Models"
SOURCE_DIR = ROOT / "ArtSource"
MODEL_DIR.mkdir(parents=True, exist_ok=True)
SOURCE_DIR.mkdir(parents=True, exist_ok=True)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)


def material(name, color, metallic=0.0, roughness=0.7):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return mat


def move_to_collection(obj, collection):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    return obj


def finish(obj, mat, bevel=0.0):
    if mat:
        obj.data.materials.append(mat)
    if bevel > 0 and obj.type == "MESH":
        modifier = obj.modifiers.new("SoftEdges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    for polygon in getattr(obj.data, "polygons", []):
        polygon.use_smooth = False
    return obj


def box(collection, name, location, dimensions, mat, bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    return finish(obj, mat, bevel)


def cylinder(collection, name, location, radius, depth, mat, vertices=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    move_to_collection(obj, collection)
    return finish(obj, mat, 0.025)


def ico(collection, name, location, radius, mat, scale=(1, 1, 1), subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    return finish(obj, mat)


def torus(collection, name, location, major_radius, minor_radius, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=12,
        minor_segments=6,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    move_to_collection(obj, collection)
    return finish(obj, mat)


def begin_asset(name):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new(f"{name}_ROOT", None)
    collection.objects.link(root)
    return collection, root


def parent_parts(collection, root):
    for obj in collection.objects:
        if obj != root:
            obj.parent = root


def export_asset(name, collection):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.all_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next((o for o in collection.objects if o.name.endswith("_ROOT")), None)
    bpy.ops.export_scene.fbx(
        filepath=str(MODEL_DIR / f"{name}.fbx"),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )


reset_scene()

WHITE = material("M_White", (0.86, 0.90, 0.92), metallic=0.05, roughness=0.38)
DARK = material("M_Dark", (0.055, 0.065, 0.08), metallic=0.1, roughness=0.55)
STEEL = material("M_Steel", (0.31, 0.36, 0.40), metallic=0.75, roughness=0.25)
WOOD = material("M_Wood", (0.45, 0.22, 0.09), roughness=0.72)
WOOD_LIGHT = material("M_WoodLight", (0.72, 0.47, 0.22), roughness=0.68)
RED = material("M_Red", (0.78, 0.08, 0.07), roughness=0.62)
GREEN = material("M_Green", (0.10, 0.55, 0.17), roughness=0.7)
GREEN_LIGHT = material("M_GreenLight", (0.34, 0.78, 0.20), roughness=0.72)
ORANGE = material("M_Orange", (0.95, 0.35, 0.055), roughness=0.66)
YELLOW = material("M_Yellow", (0.95, 0.68, 0.08), roughness=0.58)
MEAT = material("M_RawMeat", (0.70, 0.12, 0.15), roughness=0.58)
COOKED = material("M_CookedMeat", (0.28, 0.095, 0.04), roughness=0.78)
SKIN = material("M_Skin", (0.88, 0.59, 0.40), roughness=0.72)
BLUE = material("M_Blue", (0.06, 0.35, 0.70), roughness=0.55)
GLASS = material("M_Window", (0.15, 0.55, 0.72), metallic=0.05, roughness=0.25)
WALL = material("M_Wall", (0.70, 0.74, 0.68), roughness=0.9)
FLOOR = material("M_Floor", (0.24, 0.30, 0.31), roughness=0.86)

assets = {}

c, root = begin_asset("KitchenFloor")
box(c, "FloorTile", (0, 0, -0.075), (2, 2, 0.15), FLOOR, 0.02)
parent_parts(c, root); assets["KitchenFloor"] = c

c, root = begin_asset("KitchenWall")
box(c, "Wall", (0, 0, 1.25), (4, 0.25, 2.5), WALL, 0.04)
box(c, "WallTrim", (0, -0.14, 0.12), (4, 0.08, 0.24), WOOD_LIGHT, 0.02)
parent_parts(c, root); assets["KitchenWall"] = c

c, root = begin_asset("Refrigerator")
box(c, "Body", (0, 0, 1.35), (1.45, 0.85, 2.7), WHITE, 0.10)
box(c, "FreezerDoor", (0, -0.45, 2.10), (1.32, 0.10, 0.95), WHITE, 0.035)
box(c, "MainDoor", (0, -0.45, 0.92), (1.32, 0.10, 1.30), WHITE, 0.035)
box(c, "HandleTop", (0.48, -0.54, 1.95), (0.10, 0.10, 0.48), DARK, 0.025)
box(c, "HandleBottom", (0.48, -0.54, 1.10), (0.10, 0.10, 0.58), DARK, 0.025)
parent_parts(c, root); assets["Refrigerator"] = c

c, root = begin_asset("ChoppingTable")
box(c, "Top", (0, 0, 1.03), (2.25, 1.05, 0.18), WOOD_LIGHT, 0.07)
for x in (-0.92, 0.92):
    for y in (-0.38, 0.38):
        box(c, f"Leg_{x}_{y}", (x, y, 0.50), (0.15, 0.15, 1.0), WOOD, 0.03)
box(c, "CuttingBoard", (0, -0.02, 1.16), (1.25, 0.62, 0.08), WHITE, 0.05)
box(c, "KnifeBlade", (0.18, 0.08, 1.23), (0.72, 0.04, 0.10), STEEL, 0.015)
box(c, "KnifeHandle", (-0.30, 0.08, 1.23), (0.26, 0.08, 0.10), DARK, 0.02)
parent_parts(c, root); assets["ChoppingTable"] = c

c, root = begin_asset("Stove")
box(c, "Body", (0, 0, 0.58), (2.15, 1.12, 1.16), STEEL, 0.08)
box(c, "Top", (0, 0, 1.20), (2.18, 1.15, 0.12), DARK, 0.04)
for x in (-0.58, 0.58):
    torus(c, f"Burner_{x}", (x, -0.03, 1.29), 0.30, 0.055, RED)
    cylinder(c, f"Knob_{x}", (x, -0.60, 0.75), 0.095, 0.10, DARK, vertices=12, rotation=(math.pi / 2, 0, 0))
box(c, "OvenDoor", (0, -0.59, 0.40), (1.52, 0.08, 0.58), DARK, 0.04)
parent_parts(c, root); assets["Stove"] = c

c, root = begin_asset("TrashBin")
cylinder(c, "Bin", (0, 0, 0.55), 0.48, 1.1, STEEL, vertices=12)
cylinder(c, "Lid", (0, 0, 1.12), 0.53, 0.12, DARK, vertices=12)
box(c, "Pedal", (0, -0.48, 0.10), (0.34, 0.24, 0.10), DARK, 0.03)
parent_parts(c, root); assets["TrashBin"] = c

c, root = begin_asset("CustomerWindow")
box(c, "Counter", (0, -0.12, 0.77), (2.30, 0.68, 0.18), WOOD_LIGHT, 0.05)
box(c, "LeftFrame", (-1.03, 0, 1.70), (0.18, 0.28, 1.75), RED, 0.03)
box(c, "RightFrame", (1.03, 0, 1.70), (0.18, 0.28, 1.75), RED, 0.03)
box(c, "TopFrame", (0, 0, 2.52), (2.22, 0.28, 0.18), RED, 0.03)
box(c, "Glass", (0, 0.08, 1.75), (1.85, 0.08, 1.38), GLASS, 0.02)
parent_parts(c, root); assets["CustomerWindow"] = c

c, root = begin_asset("Chef")
cylinder(c, "Body", (0, 0, 0.85), 0.40, 1.15, WHITE, vertices=10)
ico(c, "Head", (0, 0, 1.70), 0.38, SKIN, subdivisions=2)
cylinder(c, "HatBand", (0, 0, 2.03), 0.36, 0.25, WHITE, vertices=12)
for x, y in ((-0.22, 0), (0, 0.08), (0.22, 0)):
    ico(c, f"HatPuff_{x}", (x, y, 2.22), 0.25, WHITE, scale=(1, 1, 0.85), subdivisions=1)
for x in (-0.50, 0.50):
    cylinder(c, f"Arm_{x}", (x, 0, 0.92), 0.11, 0.72, SKIN, vertices=8, rotation=(0, math.pi / 2, 0))
for x in (-0.20, 0.20):
    cylinder(c, f"Leg_{x}", (x, 0, 0.24), 0.14, 0.48, BLUE, vertices=8)
box(c, "Apron", (0, -0.38, 0.86), (0.50, 0.06, 0.72), WHITE, 0.025)
parent_parts(c, root); assets["Chef"] = c

c, root = begin_asset("VegetableRaw")
ico(c, "BroccoliHead", (0, 0, 0.42), 0.34, GREEN, scale=(1.0, 1.0, 0.78), subdivisions=1)
for x, y in ((-0.18, 0.04), (0.16, 0.07), (0, -0.15)):
    ico(c, f"Floret_{x}_{y}", (x, y, 0.52), 0.22, GREEN_LIGHT, subdivisions=1)
cylinder(c, "Stem", (0, 0, 0.15), 0.10, 0.30, GREEN_LIGHT, vertices=8)
parent_parts(c, root); assets["VegetableRaw"] = c

c, root = begin_asset("VegetableChopped")
for i, (x, y) in enumerate(((-0.22, -0.08), (0.06, 0.12), (0.26, -0.10), (-0.02, -0.22))):
    cylinder(c, f"Slice_{i}", (x, y, 0.08), 0.15, 0.16, GREEN_LIGHT, vertices=8)
parent_parts(c, root); assets["VegetableChopped"] = c

c, root = begin_asset("Cheese")
bpy.ops.mesh.primitive_cone_add(vertices=3, radius1=0.48, radius2=0.48, depth=0.34, location=(0, 0, 0.17), rotation=(0, 0, math.pi / 2))
cheese = move_to_collection(bpy.context.object, c); cheese.name = "CheeseWedge"; finish(cheese, YELLOW, 0.035)
for i, loc in enumerate(((-0.12, -0.30, 0.20), (0.18, -0.30, 0.12))):
    ico(c, f"CheeseHole_{i}", loc, 0.055, DARK, scale=(1, 0.35, 1), subdivisions=1)
parent_parts(c, root); assets["Cheese"] = c

c, root = begin_asset("MeatRaw")
ico(c, "Steak", (0, 0, 0.15), 0.46, MEAT, scale=(1.2, 0.78, 0.32), subdivisions=2)
ico(c, "Fat", (-0.18, -0.31, 0.17), 0.16, WHITE, scale=(1.2, 0.25, 0.25), subdivisions=1)
parent_parts(c, root); assets["MeatRaw"] = c

c, root = begin_asset("MeatCooked")
ico(c, "CookedSteak", (0, 0, 0.15), 0.46, COOKED, scale=(1.2, 0.78, 0.32), subdivisions=2)
for i, x in enumerate((-0.20, 0.0, 0.20)):
    box(c, f"GrillMark_{i}", (x, -0.35, 0.19), (0.055, 0.42, 0.035), DARK, 0.01).rotation_euler.z = -0.35
parent_parts(c, root); assets["MeatCooked"] = c

for asset_name, collection in assets.items():
    export_asset(asset_name, collection)

# Preserve the editable, origin-aligned source before composing the preview.
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_DIR / "YesChef_LowPoly.blend"))

# Build a disposable preview lineup without altering the saved source file.
for collection in assets.values():
    collection.hide_render = True

preview = bpy.data.collections.new("PreviewLineup")
bpy.context.scene.collection.children.link(preview)
lineup = [
    ("Chef", (-5.5, 1.8, 0)),
    ("Refrigerator", (-3.5, 1.8, 0)),
    ("ChoppingTable", (-0.8, 1.8, 0)),
    ("Stove", (2.0, 1.8, 0)),
    ("TrashBin", (4.5, 1.8, 0)),
    ("CustomerWindow", (-4.2, -1.6, 0)),
    ("VegetableRaw", (-1.6, -1.6, 0)),
    ("VegetableChopped", (-0.2, -1.6, 0)),
    ("Cheese", (1.3, -1.6, 0)),
    ("MeatRaw", (2.8, -1.6, 0)),
    ("MeatCooked", (4.3, -1.6, 0)),
]
for asset_name, offset in lineup:
    source_collection = assets[asset_name]
    for source in source_collection.objects:
        if source.type != "MESH":
            continue
        duplicate = source.copy()
        duplicate.data = source.data.copy()
        duplicate.parent = None
        duplicate.matrix_world = source.matrix_world.copy()
        duplicate.location += Vector(offset)
        preview.objects.link(duplicate)

floor = box(preview, "PreviewFloor", (0, 0, -0.12), (12.5, 7.0, 0.18), FLOOR, 0.04)
bpy.ops.object.light_add(type="AREA", location=(0, -1, 8))
key = bpy.context.object; key.name = "KeyLight"; key.data.energy = 1700; key.data.shape = "DISK"; key.data.size = 7
bpy.ops.object.light_add(type="AREA", location=(-5, -4, 4))
fill = bpy.context.object; fill.name = "FillLight"; fill.data.energy = 900; fill.data.size = 5
fill.rotation_euler = (math.radians(50), 0, math.radians(-35))
bpy.ops.object.camera_add(location=(10.5, -15.5, 11.5))
camera = bpy.context.object
direction = Vector((0, 0, 1.0)) - camera.location
camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
camera.data.lens = 52
bpy.context.scene.camera = camera
bpy.context.scene.render.engine = "BLENDER_EEVEE"
bpy.context.scene.render.resolution_x = 1400
bpy.context.scene.render.resolution_y = 800
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.image_settings.file_format = "PNG"
bpy.context.scene.render.filepath = str(SOURCE_DIR / "YesChef_LowPoly_Preview.png")
bpy.context.scene.world.color = (0.025, 0.03, 0.045)
bpy.ops.render.render(write_still=True)

print(f"Generated {len(assets)} low-poly FBX assets in {MODEL_DIR}")
