# Exporte un niveau du jeu BGE vers des FBX lisibles par Unity.
# Usage : blender -b Assets/MAP_DATA.scenes --python Unity/Tools/export_level.py -- <scene> <logic.json> <sortie>
#
# Blender 2.8+ ouvre le .blend mais perd les logic bricks et les propriétés de jeu :
# ces informations viennent du JSON produit par extract_logic.py (lecture directe du .blend 2.78).
import bpy, json, os, sys

args = sys.argv[sys.argv.index("--") + 1:]
SCENE, LOGIC_JSON, OUT = args[0], args[1], args[2]
os.makedirs(OUT, exist_ok=True)

logic = {o["name"]: o for o in json.load(open(LOGIC_JSON, encoding="utf-8"))}
scene_layers = json.load(open(os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(LOGIC_JSON))), "scenes.json"), encoding="utf-8"))[SCENE]["lay"]

# Objets servant de modèles (ajoutés en jeu par un actuator "Add Object") : exportés à part.
TEMPLATES = {
    "Clim_white_Prison_of_the_Terioriams.001": "Clim_white",
}
PLAYER_ROOT = "Cube"
SKIP_TYPES = {"FONT", "LIGHT", "CAMERA", "SPEAKER"}


def descendants(ob):
    out = [ob]
    for c in ob.children:
        out += descendants(c)
    return out


def export(objects, path, anim=False):
    tmp = bpy.data.scenes.new("__export__")
    for ob in objects:
        tmp.collection.objects.link(ob)
    with bpy.context.temp_override(scene=tmp, view_layer=tmp.view_layers[0]):
        bpy.ops.export_scene.fbx(
            filepath=path,
            use_active_collection=True,
            object_types={"MESH", "EMPTY", "ARMATURE"},
            apply_scale_options="FBX_SCALE_ALL",
            axis_forward="-Z",
            axis_up="Y",
            bake_space_transform=True,
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            path_mode="STRIP",
            bake_anim=anim,
            bake_anim_use_all_actions=False,
            bake_anim_use_nla_strips=False,
            add_leaf_bones=False,
        )
    bpy.data.scenes.remove(tmp)
    print("EXPORT", path, len(objects))


sc = bpy.data.scenes[SCENE]
player = set(descendants(bpy.data.objects[PLAYER_ROOT]))
level = []
for ob in sc.objects:
    info = logic.get(ob.name)
    if info is None or ob.type in SKIP_TYPES or ob in player or ob.name in TEMPLATES:
        continue
    if not (info["layer"] & scene_layers):
        continue  # calque masqué : objet inactif ou modèle
    level.append(ob)
export(level, os.path.join(OUT, SCENE + ".fbx"))

for name, out_name in TEMPLATES.items():
    ob = bpy.data.objects[name]
    saved = ob.matrix_world.copy()
    ob.matrix_world.translation = (0, 0, 0)
    export([ob], os.path.join(OUT, out_name + ".fbx"))
    ob.matrix_world = saved

# La boule : collision invisible (Cube) + armature + peau.
arm = bpy.data.objects["Armature.003"]
arm.animation_data_create().action = bpy.data.actions["ArmatureAction.004"]
root = bpy.data.objects[PLAYER_ROOT]
saved = root.matrix_world.copy()
root.matrix_world.translation = (0, 0, 0)
sc.frame_start, sc.frame_end = 0, 70
export([o for o in player if o.type != "FONT"], os.path.join(OUT, "Naball.fbx"), anim=True)
root.matrix_world = saved

# Ce que le FBX ne dit pas au builder Unity : objets invisibles en jeu et matériaux utilisés.
exported = level + [bpy.data.objects[n] for n in TEMPLATES] + list(player)
json.dump({
    "hidden": sorted(ob.name for ob in exported if ob.hide_render),
    "materials": sorted({s.material.name for ob in exported for s in ob.material_slots if s.material}),
}, open(os.path.join(OUT, SCENE + ".export.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
