# Extrait du .blend 2.78 tout ce que Blender 2.8+ a oublié :
#   logic/<scène>.json  objets, propriétés de jeu, sensors / controllers / actuators et leurs liens
#   scenes.json         calques visibles de chaque scène
#   materials.json      textures des matériaux Blender Internal
#   texts/              scripts Python intégrés au .blend
# Usage : python3 Unity/Tools/extract_logic.py Assets/MAP_DATA.scenes <sortie>
import json
import os
import struct
import sys

from blendfile import Blend

SENSORS = {0: ("always", None), 1: ("touch", "bTouchSensor"), 2: ("near", "bNearSensor"),
           3: ("keyboard", "bKeyboardSensor"), 4: ("property", "bPropertySensor"),
           5: ("mouse", "bMouseSensor"), 6: ("collision", "bCollisionSensor"),
           7: ("radar", "bRadarSensor"), 8: ("random", "bRandomSensor"), 9: ("ray", "bRaySensor"),
           10: ("message", "bMessageSensor"), 11: ("joystick", "bJoystickSensor"),
           12: ("actuator", "bActuatorSensor"), 13: ("delay", "bDelaySensor"),
           14: ("armature", "bArmatureSensor")}
CONTROLLERS = {0: ("and", None), 1: ("or", None), 2: ("expression", "bExpressionCont"),
               3: ("python", "bPythonCont"), 4: ("nand", None), 5: ("nor", None),
               6: ("xor", None), 7: ("xnor", None)}
ACTUATORS = {0: ("motion", "bObjectActuator"), 3: ("camera", "bCameraActuator"),
             5: ("sound", "bSoundActuator"), 6: ("property", "bPropertyActuator"),
             9: ("constraint", "bConstraintActuator"), 10: ("edit_object", "bEditObjectActuator"),
             11: ("scene", "bSceneActuator"), 13: ("random", "bRandomActuator"),
             14: ("message", "bMessageActuator"), 15: ("action", "bActionActuator"),
             17: ("game", "bGameActuator"), 18: ("visibility", "bVisibilityActuator"),
             19: ("2dfilter", "bTwoDFilterActuator"), 20: ("parent", "bParentActuator"),
             22: ("state", "bStateActuator"), 23: ("armature", "bArmatureActuator"),
             24: ("steering", "bSteeringActuator"), 25: ("mouse", "bMouseActuator")}
ID_CODES = ("OB", "SC", "SO", "AC", "ME", "CA", "TX", "MA", "IM")


def clean(x):
    if isinstance(x, float):
        return round(x, 4)
    if isinstance(x, list):
        return [clean(i) for i in x]
    if isinstance(x, dict):
        return {k: clean(v) for k, v in x.items()}
    return x


class Extractor:
    def __init__(self, path):
        self.B = B = Blend(path)
        self.objects = {b[2]: b for b in B.blocks if b[0] == "OB"}
        # Un controller peut piloter l'actuator d'un autre objet : index global pour les nommer.
        self.actuators = {}
        for ob in self.objects.values():
            for ab in B.listbase("Object", ob[5], "actuators"):
                self.actuators[ab[2]] = B.id_name(ob[5]) + "/" + B.get("bActuator", ab[5], "name")

    def ref(self, ptr):
        b = self.B.block(ptr)
        if b and b[0] in ID_CODES:
            return (b[0] + ":" if b[0] != "OB" else "") + self.B.id_name(b[5])
        return None

    def struct_data(self, sname, ptr):
        B, b = self.B, self.B.block(ptr)
        if not b or not sname:
            return {}
        out = {}
        for field, (_, _, raw, _, _) in B.fields(sname).items():
            if field.startswith("pad") or field.startswith("_pad"):
                continue
            v = B.get(sname, b[5], field)
            if raw.startswith("*"):
                v = self.ref(v)
                if v is None:
                    continue
            if v in (0, 0.0, "", [0.0, 0.0, 0.0], [0, 0, 0]):
                continue
            out[field] = v
        return out

    def props(self, off):
        B, out = self.B, {}
        data_off = B.fields("bProperty")["data"][0]
        for b in B.listbase("Object", off, "prop"):
            o = b[5]
            kind, name = B.get("bProperty", o, "type"), B.get("bProperty", o, "name")
            raw = B.f[o + data_off:o + data_off + 4]
            if kind == 3:
                v = B.cstring(B.get("bProperty", o, "poin"))
            elif kind in (2, 5):  # float, timer
                v = struct.unpack("<f", raw)[0]
            elif kind == 0:
                v = bool(struct.unpack("<i", raw)[0])
            else:
                v = struct.unpack("<i", raw)[0]
            out[name] = v
        return out

    def links(self, block_ptr, names):
        B = self.B
        b = B.block(block_ptr)
        if not b:
            return []
        n = b[1] // 8
        ptrs = struct.unpack("<%dQ" % n, B.f[b[5]:b[5] + 8 * n])
        return [names.get(p) or self.actuators.get(p, "?") for p in ptrs if p]

    def object(self, ptr):
        B = self.B
        o = self.objects[ptr][5]
        g = lambda f: B.get("Object", o, f)
        ob = {"name": B.id_name(o), "type": g("type"), "data": self.ref(g("data")),
              "parent": self.ref(g("parent")), "loc": g("loc"), "rot": g("rot"), "size": g("size"),
              "layer": g("lay"), "state": g("state"), "init_state": g("init_state"),
              "gameflag": g("gameflag"), "body_type": g("body_type"),
              "bound": g("collision_boundtype"), "radius": g("inertia"), "mass": g("mass"),
              "damping": g("damping"), "rdamping": g("rdamping"),
              "props": self.props(o), "sensors": [], "controllers": [], "actuators": []}
        conts = {cb[2]: B.get("bController", cb[5], "name") for cb in B.listbase("Object", o, "controllers")}
        acts = {ab[2]: B.get("bActuator", ab[5], "name") for ab in B.listbase("Object", o, "actuators")}
        for sb in B.listbase("Object", o, "sensors"):
            s = lambda f: B.get("bSensor", sb[5], f)
            kind, sname = SENSORS.get(s("type"), (s("type"), None))
            ob["sensors"].append({"name": s("name"), "type": kind, "invert": s("invert"),
                                  "pulse": s("pulse"), "freq": s("freq"),
                                  "data": self.struct_data(sname, s("data")),
                                  "to": self.links(s("links"), conts)})
        for cb in B.listbase("Object", o, "controllers"):
            c = lambda f: B.get("bController", cb[5], f)
            kind, sname = CONTROLLERS.get(c("type"), (c("type"), None))
            ob["controllers"].append({"name": c("name"), "type": kind, "states": c("state_mask"),
                                      "data": self.struct_data(sname, c("data")),
                                      "to": self.links(c("links"), acts)})
        for ab in B.listbase("Object", o, "actuators"):
            a = lambda f: B.get("bActuator", ab[5], f)
            kind, sname = ACTUATORS.get(a("type"), (a("type"), None))
            ob["actuators"].append({"name": a("name"), "type": kind,
                                    "data": self.struct_data(sname, a("data"))})
        return clean(ob)

    def scenes(self):
        B, out = self.B, {}
        for b in B.blocks:
            if b[0] == "SC":
                obs = [B.get("Base", bb[5], "object") for bb in B.listbase("Scene", b[5], "base")]
                out[B.id_name(b[5])] = {"lay": B.get("Scene", b[5], "lay"),
                                        "objects": [p for p in obs if p in self.objects]}
        return out

    def materials(self):
        B, out = self.B, {}
        for b in B.blocks:
            if b[0] != "MA":
                continue
            o, slots = b[5], []
            for i, p in enumerate(B.ptr_array("Material", o, "mtex")):
                mb = B.block(p)
                tb = mb and B.block(B.get("MTex", mb[5], "tex"))
                ib = tb and B.block(B.get("Tex", tb[5], "ima"))
                if not ib:
                    continue
                m = lambda f: B.get("MTex", mb[5], f)
                slots.append({"slot": i, "image": B.get("Image", ib[5], "name"), "mapto": m("mapto"),
                              "blend": m("blendtype"), "texco": m("texco"), "uv": m("uvname"),
                              "size": m("size"), "ofs": m("ofs"), "colfac": m("colfac"),
                              "norfac": m("norfac"), "alphafac": m("alphafac")})
            g = lambda f: B.get("Material", o, f)
            out[B.id_name(o)] = clean({"color": [g("r"), g("g"), g("b")], "alpha": g("alpha"),
                                       "emit": g("emit"), "mode": g("mode"), "slots": slots})
        return out

    def sounds(self):
        B = self.B
        return {B.id_name(b[5]): B.get("bSound", b[5], "name") for b in B.blocks if b[0] == "SO"}

    def texts(self):
        B = self.B
        for b in B.blocks:
            if b[0] == "TX":
                lines = [B.cstring(B.get("TextLine", lb[5], "line")) for lb in B.listbase("Text", b[5], "lines")]
                yield B.id_name(b[5]), "\n".join(lines)


def main(path, out):
    ex = Extractor(path)
    os.makedirs(os.path.join(out, "logic"), exist_ok=True)
    os.makedirs(os.path.join(out, "texts"), exist_ok=True)
    scenes = ex.scenes()
    for name, sc in scenes.items():
        data = [ex.object(p) for p in sc["objects"]]
        with open(os.path.join(out, "logic", name.replace(" ", "_") + ".json"), "w", encoding="utf-8") as f:
            json.dump(data, f, indent=1, ensure_ascii=False)
        print("%-28s %4d objets %5d briques" % (name, len(data), sum(
            len(o["sensors"]) + len(o["controllers"]) + len(o["actuators"]) for o in data)))
    with open(os.path.join(out, "scenes.json"), "w", encoding="utf-8") as f:
        json.dump({k: {"lay": v["lay"]} for k, v in scenes.items()}, f, indent=1, ensure_ascii=False)
    with open(os.path.join(out, "materials.json"), "w", encoding="utf-8") as f:
        json.dump(ex.materials(), f, indent=1, ensure_ascii=False)
    with open(os.path.join(out, "sounds.json"), "w", encoding="utf-8") as f:
        json.dump(ex.sounds(), f, indent=1, ensure_ascii=False)
    for name, text in ex.texts():
        with open(os.path.join(out, "texts", name.replace("/", "_")), "w", encoding="utf-8") as f:
            f.write(text)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
