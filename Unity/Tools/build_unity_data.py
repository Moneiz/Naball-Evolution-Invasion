# Transforme ce qu'ont produit extract_logic.py et export_level.py en données pour le projet Unity :
# modèles, textures, sons, description du niveau, matériaux et textes traduits.
# Usage : python3 Unity/Tools/build_unity_data.py <dossier extrait> <dossier export> <scène>
import ast
import json
import os
import shutil
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GAME = os.path.join(REPO, "Assets")
UNITY = os.path.join(REPO, "Unity", "Assets", "Naball")
MAP_TO_COLOR, MAP_TO_NORMAL, MAP_TO_ALPHA = 1, 2, 128
MA_SHADELESS, MA_ZTRANSP = 4, 64
PROP_GREATER_THAN = 6
OB_MESH, OB_GHOST = 1, 128
FPS = 60.0  # le BGE tournait à 60 logic ticks par seconde


def write_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1, ensure_ascii=False)


def game_file(blender_path):
    """Chemin Blender ("//textures\\x.png", "G:\\...\\x.png") vers un fichier du jeu, ou None."""
    p = blender_path.replace("\\", "/")
    if p.startswith("//"):
        full = os.path.normpath(os.path.join(GAME, p[2:]))
        if os.path.isfile(full):
            return full
    name = os.path.basename(p)
    for folder in ("textures", "Skin_naball", "sounds", "textures/LoadLVL"):
        full = os.path.join(GAME, folder, name)
        if os.path.isfile(full):
            return full
    return None


def copy_asset(src, folder):
    dst = os.path.join(UNITY, folder, os.path.basename(src))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst)
    return os.path.basename(src)


def sensors_to(ob, name):
    return [s for s in ob["sensors"] if name in s["to"]]


def actuators_of(ob, controller):
    names = set(controller["to"])
    return [a for a in ob["actuators"] if a["name"] in names]


def build_level(extracted, export, scene):
    objs = {o["name"]: o for o in json.load(open(os.path.join(extracted, "logic", scene.replace(" ", "_") + ".json"), encoding="utf-8"))}
    sounds = json.load(open(os.path.join(extracted, "sounds.json"), encoding="utf-8"))
    exported = json.load(open(os.path.join(export, scene + ".export.json"), encoding="utf-8"))
    level = {"scene": scene, "hidden": exported["hidden"], "clims": [], "portals": [], "soundFields": [],
             "ambientSounds": [], "dialogTriggers": [], "rotators": [], "progress": [], "physics": [], "references": []}

    def sound_clip(ref):
        path = game_file(sounds.get(ref[3:], "")) if ref else None
        return copy_asset(path, "Audio") if path else None

    scene_layers = json.load(open(os.path.join(extracted, "scenes.json"), encoding="utf-8"))[scene]["lay"]
    template = next((o for o in objs.values() if o["name"].startswith("Clim_white")), None)
    template_steer = [a["data"] for a in template["actuators"] if a["type"] == "steering"] if template else []
    template_spin = [a["data"]["drot"] for a in template["actuators"] if a["type"] == "motion" and "drot" in a["data"]] if template else []
    level["climSpin"] = [v * FPS for v in template_spin[0]] if template_spin else [0.0, 0.0, 3.0]

    for name, ob in objs.items():
        if not ob["layer"] & scene_layers:
            continue  # calque masqué : modèle ou objet inactif
        props = ob["props"]
        if ob["type"] == OB_MESH:
            level["physics"].append({"objectName": name, "bodyType": ob["body_type"], "bound": ob["bound"],
                                     "ghost": bool(ob["gameflag"] & OB_GHOST)})
        if not ob["parent"]:
            # Positions Blender connues : le builder Unity s'en sert pour vérifier la conversion d'axes.
            level["references"].append({"objectName": name, "blenderPosition": ob["loc"]})
        modules = [c["data"].get("module", "") for c in ob["controllers"] if c["type"] == "python"]

        # Clim : un Empty qui fait apparaître "Clim_white" et se retire quand la boule passe à côté.
        adds = [a for a in ob["actuators"] if a["type"] == "edit_object" and "Clim_white" in str(a["data"].get("ob"))]
        if adds and "spw" in props:
            near = [s["data"]["dist"] for s in ob["sensors"] if s["type"] == "near"]
            steer = [a["data"] for a in ob["actuators"] if a["type"] == "steering"] or template_steer
            level["clims"].append({"objectName": name, "id": name.split(".", 1)[1],
                                   "collectRadius": max(near) if near else 9.0,
                                   "magnetSpeed": steer[0]["velocity"] if steer else 0.0})

        # Portail : changement de scène au contact de la boule.
        scene_acts = [a for a in ob["actuators"] if a["type"] == "scene" and a["data"].get("type") == 1]
        if scene_acts and any(s["type"] == "collision" and s["data"].get("name") == "Naball" for s in ob["sensors"]):
            gate = [s["data"] for s in ob["sensors"] if s["type"] == "property"
                    and s["data"].get("name") == "alr" and s["data"].get("type") == PROP_GREATER_THAN]
            indicator = "Cos_back." + name.split(".", 2)[-1].lower()
            timer = [s["data"] for s in ob["sensors"] if s["type"] == "property" and s["data"].get("name") == "time"]
            level["portals"].append({
                "objectName": name, "targetScene": scene_acts[0]["data"]["scene"][3:],
                "requiredProgress": int(gate[0]["value"]) + 1 if gate else 0,
                "locked": "act" in props,  # activés par un message que rien n'envoie dans ce niveau
                "delay": float(timer[0]["value"]) if timer else 4.0,
                "loadingImage": props.get("lvl", ""),
                "indicatorName": indicator if indicator in objs else "",
            })

        # Zones sonores : volume = 1 - distance / rayon (AudiViews.GerMod).
        if any("AudiViews.GerMod" in m for m in modules):
            snd = next(a for a in ob["actuators"] if a["type"] == "sound")
            clip = sound_clip(snd["data"].get("sound"))
            if clip:
                level["soundFields"].append({"objectName": name, "clip": clip, "radius": float(props.get("prop", 50))})

        # Son d'ambiance joué en boucle dès le départ.
        for c in ob["controllers"]:
            for a in actuators_of(ob, c):
                if a["type"] == "sound" and not any("AudiViews" in m for m in modules) \
                        and all(s["type"] in ("always", "random") for s in sensors_to(ob, c["name"])):
                    clip = sound_clip(a["data"].get("sound"))
                    if clip:
                        level["ambientSounds"].append({"objectName": name, "clip": clip,
                                                       "volume": min(1.0, a["data"].get("volume", 1.0))})

        # Dialogue déclenché par contact (DlgInvocation.RegDlg).
        if any(m.endswith("DlgInvocation.RegDlg") for m in modules) and "dlg.name" in props \
                and any(s["type"] == "collision" for s in ob["sensors"]):
            level["dialogTriggers"].append({"objectName": name, "dialog": props["dlg.name"].split(".")[0]})

        # Rotation permanente (sensor Always → Motion avec seulement une rotation).
        for c in ob["controllers"]:
            if c["type"] != "and" or not all(s["type"] == "always" for s in sensors_to(ob, c["name"])):
                continue
            for a in actuators_of(ob, c):
                d = a["data"]
                if a["type"] == "motion" and "drot" in d and "dloc" not in d and sensors_to(ob, c["name"]):
                    level["rotators"].append({"objectName": name, "radiansPerSecond": [v * FPS for v in d["drot"]],
                                              "local": bool(d.get("flag", 0) & 8)})

    # Progression du hub (gameInstance/Cont/ger.sii) : point d'apparition, caméra, dialogue d'accueil.
    ger = ast.literal_eval(open(os.path.join(GAME, "gameInstance", "Cont", "ger.sii"), encoding="utf-8").read())
    for key in sorted(k for k in ger if k.startswith("cond")):
        c = ger[key]
        level["progress"].append({"progress": int(key[4:]), "playerSpawn": c["Cube"],
                                  "cameraPosition": c["Ger.cam"], "welcomeDialog": c["Ger.normal"][1].split(".")[0]})
    return level


def build_materials(extracted, export):
    mats = json.load(open(os.path.join(extracted, "materials.json"), encoding="utf-8"))
    out = []
    for name in used_materials(export):
        m = mats.get(name)
        if not m:
            continue
        entry = {"name": name, "color": m["color"], "alpha": m["alpha"], "emission": m["emit"],
                 "shadeless": bool(m["mode"] & MA_SHADELESS), "transparent": bool(m["mode"] & MA_ZTRANSP),
                 "albedo": "", "tiling": [1.0, 1.0], "offset": [0.0, 0.0], "normalMap": "", "alphaFromTexture": False}
        for slot in m["slots"]:
            src = game_file(slot["image"])
            if not src:
                print("  texture introuvable :", slot["image"], "(%s)" % name)
                continue
            if slot["mapto"] & MAP_TO_COLOR and not entry["albedo"]:
                entry["albedo"] = copy_asset(src, "Textures")
                entry["tiling"], entry["offset"] = slot["size"][:2], slot["ofs"][:2]
                entry["alphaFromTexture"] = bool(slot["mapto"] & MAP_TO_ALPHA)
            elif slot["mapto"] & MAP_TO_NORMAL and not entry["normalMap"]:
                entry["normalMap"] = copy_asset(src, "Textures")
            elif slot["mapto"] & MAP_TO_ALPHA:
                entry["alphaFromTexture"] = True
        out.append(entry)
    return {"materials": out}


def used_materials(export):
    names = set()
    for f in os.listdir(export):
        if f.endswith(".export.json"):
            names |= set(json.load(open(os.path.join(export, f), encoding="utf-8"))["materials"])
    return sorted(names)


def parse_python_literal(raw):
    """Quelques fichiers du jeu ont une accolade en trop : on retente en retirant une ligne "}" isolée."""
    try:
        return ast.literal_eval(raw)
    except (SyntaxError, ValueError):
        lines = raw.split("\n")
        for i, line in enumerate(lines):
            if line.strip() in ("}", "},"):
                try:
                    return ast.literal_eval("\n".join(lines[:i] + lines[i + 1:]))
                except (SyntaxError, ValueError):
                    pass
    return None


def build_lang():
    """Les fichiers .lg du jeu sont des dictionnaires Python : on les réécrit en JSON pour JsonUtility."""
    for lang in os.listdir(os.path.join(GAME, "lang")):
        folder = os.path.join(GAME, "lang", lang)
        for f in sorted(os.listdir(folder)):
            if not f.endswith(".lg"):
                continue
            raw = open(os.path.join(folder, f), encoding="utf-8-sig", errors="replace").read()
            data = parse_python_literal(raw)
            if data is None:
                print("  illisible :", lang, f)
                continue
            base = f[:-3]
            if any(k.startswith("dlg") for k in data):
                lines, i = [], 1
                while "dlg%d" % i in data and data["dlg%d" % i] != "Stop":
                    d = data["dlg%d" % i]
                    lines.append({"color": d.get("color", "Default"), "text": " ".join(d.get("text", "").split())})
                    i += 1
                write_json(os.path.join(UNITY, "Resources", "Lang", lang, base + ".json"), {"lines": lines})
            else:
                write_json(os.path.join(UNITY, "Resources", "Lang", lang, base + ".json"),
                           {"entries": [{"key": k, "value": " ".join(str(v).split())} for k, v in data.items()]})


def main(extracted, export, scene):
    for f in os.listdir(export):
        if f.endswith(".fbx"):
            copy_asset(os.path.join(export, f), "Models")
    level = build_level(extracted, export, scene)
    write_json(os.path.join(UNITY, "Data", scene + ".level.json"), level)
    write_json(os.path.join(UNITY, "Data", "Materials.json"), build_materials(extracted, export))
    for p in level["portals"]:
        img = os.path.join(GAME, "textures", "LoadLVL", p["loadingImage"] + ".png")
        if os.path.isfile(img):
            dst = os.path.join(UNITY, "Resources", "LoadingScreens", os.path.basename(img))
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copyfile(img, dst)
    copy_asset(os.path.join(GAME, "sounds", "Magic61.wav"), "Audio")  # effet de ramassage d'un Clim
    build_lang()
    print("clims %d, portails %d, zones sonores %d, ambiances %d, dialogues %d, rotations %d" % tuple(
        len(level[k]) for k in ("clims", "portals", "soundFields", "ambientSounds", "dialogTriggers", "rotators")))


if __name__ == "__main__":
    main(*sys.argv[1:4])
