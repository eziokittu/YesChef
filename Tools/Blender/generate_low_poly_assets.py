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


def cone(collection, name, location, radius1, radius2, depth, mat, vertices=8, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth,
                                   location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    move_to_collection(obj, collection)
    return finish(obj, mat, 0.018)


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
        if obj != root and obj.parent is None:
            obj.parent = root


def empty(collection, name, location):
    obj = bpy.data.objects.new(name, None)
    obj.location = location
    collection.objects.link(obj)
    return obj


def pivoted_cylinder(collection, pivot_name, part_name, pivot_location, part_location,
                     radius, depth, mat, vertices=8):
    """Create an animation-ready limb whose origin is at its shoulder or hip."""
    pivot = empty(collection, pivot_name, pivot_location)
    part = cylinder(collection, part_name, part_location, radius, depth, mat, vertices=vertices)
    part.parent = pivot
    part.matrix_parent_inverse = pivot.matrix_world.inverted()
    return pivot, part


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
PINK = material("M_Pink", (0.86, 0.26, 0.48), roughness=0.62)
PURPLE = material("M_Purple", (0.42, 0.18, 0.66), roughness=0.62)
BLACK = material("M_Black", (0.025, 0.02, 0.018), roughness=0.75)
GRASS = material("M_Grass", (0.13, 0.43, 0.17), roughness=0.92)
GLASS = material("M_Window", (0.15, 0.55, 0.72), metallic=0.05, roughness=0.25)
WALL = material("M_Wall", (0.70, 0.74, 0.68), roughness=0.9)
FLOOR = material("M_Floor", (0.24, 0.30, 0.31), roughness=0.86)
FROG_DARK = material("M_FrogDark", (0.045, 0.28, 0.08), roughness=0.78)
FISH_BLUE = material("M_FishBlue", (0.04, 0.34, 0.56), metallic=0.08, roughness=0.48)
FISH_CREAM = material("M_FishCream", (0.96, 0.72, 0.30), roughness=0.62)
SNAKE_BELLY = material("M_SnakeBelly", (0.82, 0.68, 0.30), roughness=0.76)
WING_ORANGE = material("M_WingOrange", (0.96, 0.35, 0.045), roughness=0.52)
WING_YELLOW = material("M_WingYellow", (1.0, 0.72, 0.08), roughness=0.52)

assets = {}

c, root = begin_asset("KitchenFloor")
box(c, "FloorTile", (0, 0, -0.075), (2, 2, 0.15), FLOOR, 0.02)
parent_parts(c, root); assets["KitchenFloor"] = c

c, root = begin_asset("KitchenWall")
box(c, "Wall", (0, 0, 1.25), (4, 0.25, 2.5), WALL, 0.04)
box(c, "WallTrim", (0, -0.14, 0.12), (4, 0.08, 0.24), WOOD_LIGHT, 0.02)
parent_parts(c, root); assets["KitchenWall"] = c

c, root = begin_asset("Refrigerator")
# The main compartment is built as a shallow shell so its stocked interior is
# visible whenever Unity rotates the door around the authored hinge.
box(c, "FridgeBack", (0, 0.34, 1.35), (1.45, 0.16, 2.7), WHITE, 0.08)
box(c, "FridgeLeft", (-0.66, 0, 1.35), (0.13, 0.72, 2.7), WHITE, 0.05)
box(c, "FridgeRight", (0.66, 0, 1.35), (0.13, 0.72, 2.7), WHITE, 0.05)
box(c, "FridgeTop", (0, 0, 2.64), (1.32, 0.72, 0.13), WHITE, 0.05)
box(c, "FridgeBottom", (0, 0, 0.08), (1.32, 0.72, 0.13), WHITE, 0.05)
box(c, "FreezerDoor", (0, -0.45, 2.10), (1.32, 0.10, 0.95), WHITE, 0.035)
box(c, "HandleTop", (0.48, -0.54, 1.95), (0.10, 0.10, 0.48), DARK, 0.025)
for shelf_z in (0.48, 0.91, 1.34):
    box(c, f"InteriorShelf_{shelf_z}", (0, -0.02, shelf_z), (1.10, 0.58, 0.055), STEEL, 0.015)
ico(c, "StockVegetable", (-0.30, -0.14, 0.64), 0.18, GREEN, subdivisions=1)
box(c, "StockCheese", (0.28, -0.12, 1.05), (0.34, 0.24, 0.20), YELLOW, 0.025)
ico(c, "StockMeat", (-0.20, -0.13, 1.49), 0.19, MEAT, scale=(1.35, 0.75, 0.55), subdivisions=1)
door_hinge = empty(c, "MainDoorHinge", (-0.66, -0.45, 0.92))
main_door = box(c, "MainDoor", (0, -0.45, 0.92), (1.32, 0.10, 1.30), WHITE, 0.035)
main_handle = box(c, "HandleBottom", (0.48, -0.54, 1.10), (0.10, 0.10, 0.58), DARK, 0.025)
for door_part in (main_door, main_handle):
    door_part.parent = door_hinge
    door_part.matrix_parent_inverse = door_hinge.matrix_world.inverted()
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

c, root = begin_asset("SingleStove")
box(c, "SingleStoveBody", (0, 0, 0.22), (1.05, 0.92, 0.44), STEEL, 0.07)
box(c, "SingleStoveTop", (0, 0, 0.48), (1.08, 0.95, 0.10), DARK, 0.04)
torus(c, "SingleBurner", (0, 0, 0.56), 0.30, 0.055, RED)
cylinder(c, "SingleKnob", (0, -0.49, 0.20), 0.09, 0.10, DARK, vertices=12, rotation=(math.pi / 2, 0, 0))
parent_parts(c, root); assets["SingleStove"] = c

c, root = begin_asset("TrashBin")
cylinder(c, "Bin", (0, 0, 0.55), 0.48, 1.1, STEEL, vertices=12)
lid_hinge = empty(c, "TrashLidHinge", (0, 0.42, 1.12))
lid = cylinder(c, "Lid", (0, 0, 1.12), 0.53, 0.12, DARK, vertices=12)
lid.parent = lid_hinge
lid.matrix_parent_inverse = lid_hinge.matrix_world.inverted()
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
for side, x in (("Left", -0.43), ("Right", 0.43)):
    pivoted_cylinder(c, f"ArmPivot_{side}", f"Arm_{side}", (x, 0, 1.22),
                     (x, 0, 0.92), 0.11, 0.60, SKIN)
for side, x in (("Left", -0.20), ("Right", 0.20)):
    pivoted_cylinder(c, f"LegPivot_{side}", f"Leg_{side}", (x, 0, 0.50),
                     (x, 0, 0.24), 0.14, 0.52, BLUE)
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

# A reusable customer with three hairstyle meshes. Unity enables one hairstyle
# and recolours named body parts to produce many distinct visitors.
c, root = begin_asset("Customer")
cylinder(c, "ShirtBody", (0, 0, 0.88), 0.40, 1.05, BLUE, vertices=10)
ico(c, "SkinHead", (0, 0, 1.66), 0.37, SKIN, subdivisions=2)
for side, x in (("Left", -0.43), ("Right", 0.43)):
    pivoted_cylinder(c, f"ArmPivot_{side}", f"SkinArm_{side}", (x, 0, 1.20),
                     (x, 0, 0.92), 0.11, 0.56, SKIN)
for side, x in (("Left", -0.19), ("Right", 0.19)):
    pivoted_cylinder(c, f"LegPivot_{side}", f"PantsLeg_{side}", (x, 0, 0.51),
                     (x, 0, 0.25), 0.14, 0.52, PURPLE)
cylinder(c, "HairStyle_Cap", (0, 0, 1.91), 0.34, 0.18, BLACK, vertices=10)
ico(c, "HairStyle_Bun", (0, 0.08, 2.03), 0.22, BLACK, subdivisions=1)
for x in (-0.17, 0, 0.17):
    ico(c, f"HairStyle_Curls_{x}", (x, -0.02, 1.94), 0.16, BLACK, subdivisions=1)
parent_parts(c, root); assets["Customer"] = c

c, root = begin_asset("Tree")
cylinder(c, "TreeTrunk", (0, 0, 1.05), 0.24, 2.1, WOOD, vertices=8)
ico(c, "TreeLeavesLow", (0, 0, 2.35), 1.05, GREEN, scale=(1, 1, 0.8), subdivisions=1)
ico(c, "TreeLeavesHigh", (0.25, 0, 3.0), 0.78, GREEN_LIGHT, scale=(1, 1, 0.85), subdivisions=1)
parent_parts(c, root); assets["Tree"] = c

c, root = begin_asset("Flower")
cylinder(c, "FlowerStem", (0, 0, 0.28), 0.035, 0.56, GREEN, vertices=6)
for angle in range(0, 360, 72):
    rad = math.radians(angle)
    ico(c, f"FlowerPetal_{angle}", (math.cos(rad) * 0.14, math.sin(rad) * 0.14, 0.58), 0.11, PINK, scale=(1, 1, 0.45), subdivisions=1)
ico(c, "FlowerCenter", (0, 0, 0.59), 0.09, YELLOW, scale=(1, 1, 0.55), subdivisions=1)
parent_parts(c, root); assets["Flower"] = c

c, root = begin_asset("Lotus")
for angle in range(0, 360, 45):
    rad = math.radians(angle)
    petal = ico(c, f"LotusPetal_{angle}", (math.cos(rad) * 0.22, math.sin(rad) * 0.22, 0.08), 0.18, PINK, scale=(1.35, 0.65, 0.30), subdivisions=1)
    petal.rotation_euler.z = rad
ico(c, "LotusCenter", (0, 0, 0.12), 0.12, YELLOW, scale=(1, 1, 0.5), subdivisions=1)
parent_parts(c, root); assets["Lotus"] = c

c, root = begin_asset("Frog")
ico(c, "FrogBody", (0, 0.08, 0.20), 0.29, GREEN_LIGHT, scale=(1.18, 0.92, 0.62), subdivisions=2)
ico(c, "FrogBackStripe", (0, 0.08, 0.31), 0.22, FROG_DARK, scale=(.72, 1.0, .18), subdivisions=1)
ico(c, "FrogHead", (0, -0.23, 0.27), 0.24, GREEN_LIGHT, scale=(1.08, 0.82, 0.68), subdivisions=2)
for x in (-0.15, 0.15):
    ico(c, f"FrogEye_{x}", (x, -0.29, 0.39), 0.078, YELLOW, subdivisions=2)
    ico(c, f"FrogPupil_{x}", (x, -0.357, 0.40), 0.029, BLACK, scale=(.55, .38, 1.25), subdivisions=1)
    cylinder(c, f"FrogThigh_{x}", (x * 1.65, 0.15, 0.14), 0.105, 0.30, GREEN, vertices=7, rotation=(0, math.pi / 2, 0))
    cylinder(c, f"FrogShin_{x}", (x * 2.05, -0.02, 0.085), 0.065, 0.32, GREEN_LIGHT, vertices=7, rotation=(math.pi / 2, 0, 0))
    foot = box(c, f"FrogWebbedFoot_{x}", (x * 2.05, -0.23, 0.045), (0.22, 0.25, 0.045), GREEN, 0.018)
    foot.rotation_euler.z = -x * 1.5
for x in (-0.18, 0.18):
    cylinder(c, f"FrogFrontLeg_{x}", (x, -0.35, 0.12), 0.045, 0.24, GREEN_LIGHT, vertices=6, rotation=(math.pi / 2, 0, 0))
box(c, "FrogMouth", (0, -0.443, 0.255), (.22, .018, .018), FROG_DARK, .004)
for x in (-0.065, 0.065):
    ico(c, f"FrogNostril_{x}", (x, -0.444, .315), .012, FROG_DARK, subdivisions=1)
parent_parts(c, root); assets["Frog"] = c

c, root = begin_asset("Fish")
ico(c, "FishBody", (0, 0, 0.02), 0.32, FISH_BLUE, scale=(1.58, 0.68, 0.72), subdivisions=2)
ico(c, "FishBelly", (-.05, 0, -.13), .25, FISH_CREAM, scale=(1.45, .66, .30), subdivisions=1)
cone(c, "FishTail", (0.57, 0, 0.02), .29, 0, .42, YELLOW, vertices=3, rotation=(0, math.pi / 2, 0))
for side in (-1, 1):
    ico(c, f"FishEye_{side}", (-0.36, side * 0.20, 0.10), 0.057, WHITE, subdivisions=2)
    ico(c, f"FishPupil_{side}", (-0.39, side * 0.242, 0.10), 0.023, BLACK, subdivisions=1)
    fin = cone(c, f"FishSideFin_{side}", (.02, side * .23, -.01), .13, 0, .30, YELLOW, vertices=3,
               rotation=(math.pi / 2, 0, 0))
    fin.rotation_euler.z = side * .24
cone(c, "FishTopFin", (0.02, 0, 0.31), .19, 0, .34, YELLOW, vertices=3, rotation=(math.pi / 2, 0, 0))
torus(c, "FishMouth", (-.505, 0, .015), .055, .014, FISH_CREAM, rotation=(0, math.pi / 2, 0))
for side in (-1, 1):
    torus(c, f"FishGill_{side}", (-.25, side * .205, .0), .10, .012, FISH_CREAM, rotation=(math.pi / 2, 0, 0))
parent_parts(c, root); assets["Fish"] = c

c, root = begin_asset("Snake")
for index in range(12):
    taper = 1.0 - index * .045
    x = (index - 5) * .145
    y = math.sin(index * 1.05) * .13
    ico(c, f"SnakeSegment_{index:02d}", (x, y, .09), .135 * taper, GREEN,
        scale=(1.18, .9, .76), subdivisions=1)
    if index % 2 == 0:
        ico(c, f"SnakeBackMark_{index:02d}", (x, y, .19), .055 * taper, FROG_DARK,
            scale=(1.25, .7, .25), subdivisions=1)
ico(c, "SnakeHead", (-0.88, -0.01, 0.13), 0.20, GREEN_LIGHT, scale=(1.25, 0.95, 0.76), subdivisions=2)
ico(c, "SnakeBelly", (-.80, -.01, .045), .15, SNAKE_BELLY, scale=(1.15,.82,.28), subdivisions=1)
for y in (-0.10, 0.01):
    eye_y = -.105 if y < 0 else .105
    ico(c, f"SnakeEye_{eye_y}", (-1.0, eye_y, 0.22), 0.043, YELLOW, subdivisions=2)
    ico(c, f"SnakePupil_{eye_y}", (-1.035, eye_y * 1.04, .22), .017, BLACK, scale=(.45,.45,1.35), subdivisions=1)
box(c, "SnakeTongue", (-1.16, 0, 0.10), (.28, .025, .025), RED, 0.004)
for fork_y in (-.025, .025):
    fork = box(c, f"SnakeTongueFork_{fork_y}", (-1.31, fork_y, .10), (.13,.018,.018), RED, .003)
    fork.rotation_euler.z = fork_y * 9
parent_parts(c, root); assets["Snake"] = c

c, root = begin_asset("Butterfly")
cylinder(c, "ButterflyBody", (0, 0, .08), .035, .34, BLACK, vertices=8, rotation=(0, math.pi / 2, 0))
ico(c, "ButterflyHead", (-.19, 0, .08), .065, BLACK, subdivisions=1)
for side in (-1, 1):
    wing_name = "ButterflyLeftWing" if side < 0 else "ButterflyRightWing"
    wing = empty(c, wing_name, (0, 0, .08))
    fore_border = ico(c, f"ButterflyForewingBorder_{side}", (-.03, side * .20, .09), .22, BLACK,
                      scale=(1.22, .92, .09), subdivisions=2)
    fore_border.parent = wing; fore_border.location = (-.03, side * .20, .01)
    fore = ico(c, f"ButterflyForewing_{side}", (-.04, side * .20, .105), .19, WING_ORANGE,
               scale=(1.18, .86, .09), subdivisions=2)
    fore.parent = wing; fore.location = (-.04, side * .20, .025)
    hind_border = ico(c, f"ButterflyHindwingBorder_{side}", (.15, side * .16, .085), .17, BLACK,
                      scale=(.9, .92, .09), subdivisions=2)
    hind_border.parent = wing; hind_border.location = (.15, side * .16, .005)
    hind = ico(c, f"ButterflyHindwing_{side}", (.15, side * .16, .10), .145, WING_YELLOW,
               scale=(.86, .84, .09), subdivisions=2)
    hind.parent = wing; hind.location = (.15, side * .16, .02)
    spot = ico(c, f"ButterflyWingSpot_{side}", (-.08, side * .23, .125), .055, WHITE,
               scale=(1.0,.72,.08), subdivisions=1)
    spot.parent = wing; spot.location = (-.08, side * .23, .045)
    antenna = cylinder(c, f"ButterflyAntenna_{side}", (-.27, side * .045, .13), .009, .20, BLACK, vertices=6,
                       rotation=(0, math.pi / 2 - side * .26, 0))
parent_parts(c, root); assets["Butterfly"] = c

c, root = begin_asset("Bush")
for x, y, z, scale in ((0, 0, .34, 1), (-.28, .02, .28, .75), (.28, -.03, .29, .8)):
    ico(c, f"BushLeaf_{x}", (x, y, z), .40, GREEN, scale=(scale, scale, scale * .75), subdivisions=1)
parent_parts(c, root); assets["Bush"] = c

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
    ("Frog", (-4.8, -3.6, 0)),
    ("Fish", (-3.0, -3.6, .35)),
    ("Snake", (-.8, -3.6, .2)),
    ("Butterfly", (1.3, -3.6, .4)),
    ("Bush", (4.7, -3.6, 0)),
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

floor = box(preview, "PreviewFloor", (0, -.75, -0.12), (12.5, 8.5, 0.18), FLOOR, 0.04)
bpy.ops.object.light_add(type="AREA", location=(0, -1, 8))
key = bpy.context.object; key.name = "KeyLight"; key.data.energy = 1700; key.data.shape = "DISK"; key.data.size = 7
bpy.ops.object.light_add(type="AREA", location=(-5, -4, 4))
fill = bpy.context.object; fill.name = "FillLight"; fill.data.energy = 900; fill.data.size = 5
fill.rotation_euler = (math.radians(50), 0, math.radians(-35))
bpy.ops.object.camera_add(location=(10.5, -17.5, 12.5))
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
