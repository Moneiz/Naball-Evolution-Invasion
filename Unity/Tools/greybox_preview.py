# Aperçu d'une salle greybox sans Unity : la salle rendue trois fois côte à côte (bleu, neutre, rouge).
# Usage : blender -b --python Unity/Tools/greybox_preview.py -- Unity/Assets/Naball/Greybox/<salle>.json <sortie.png>
# Pièces violettes : elles bougent avec la dimension. Bleues / rouges : présentes seulement de ce côté. Translucides : absentes.
import bpy, json, math, sys
from mathutils import Vector, Euler
room=json.load(open(sys.argv[sys.argv.index('--')+1]))
out=sys.argv[sys.argv.index('--')+2]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene
def mat(name,col,alpha=1):
    m=bpy.data.materials.get(name)
    if m: return m
    m=bpy.data.materials.new(name); m.use_nodes=True
    b=m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value=(*col,1); b.inputs["Alpha"].default_value=alpha
    if col==(1,.35,.05) or name.startswith(('fruit','clim')):
        b.inputs["Emission Color"].default_value=(*col,1); b.inputs["Emission Strength"].default_value=1.5
    return m
def u2b(v): return Vector((v[0], v[2], v[1]))  # Unity (x,y,z) -> Blender (x, z->y, y->z)
def member(s):
    if not s: return {'bleu','neutre','rouge'}
    return {p.strip() for p in s.split(',')}
def blend(d, b, n, r):
    return [n[i]+(b[i]-n[i])*(-d) if d<0 else n[i]+(r[i]-n[i])*d for i in range(3)]
Z=[0,0,0]
for k,(d,dname) in enumerate([(-1,'bleu'),(0,'neutre'),(1,'rouge')]):
    ox=k*22-22
    for p in room['pieces']:
        m=member(p.get('dimensions',''))
        size=p.get('taille',[2,1,2]); pos=p['position']
        off=blend(d,p.get('bleu',Z),p.get('neutre',Z),p.get('rouge',Z))
        rot=blend(d,p.get('rotationBleu',Z),p.get('rotationNeutre',Z),p.get('rotationRouge',Z))
        base=p.get('rotation',Z)
        present = dname in m
        bpy.ops.mesh.primitive_cube_add(size=1)
        o=bpy.context.object
        o.scale=(size[0],size[2],size[1])
        c=[pos[0]+off[0]+ox, pos[1]+off[1]+size[1]/2, pos[2]+off[2]]
        o.location=u2b(c)
        o.rotation_euler=Euler((0,0,-math.radians(base[1]+rot[1])))
        surf=p.get('surface','sol')
        col=(1,.35,.05) if surf=='lave' else (.5,.5,.53) if surf=='mur' else (.7,.7,.72)
        moving=any(k in p for k in ('bleu','neutre','rouge','rotationBleu','rotationNeutre','rotationRouge'))
        if surf!='lave':
            if 'rouge' not in m: col=(.25,.45,1)
            elif 'bleu' not in m: col=(.95,.35,.3)
            elif moving: col=(.65,.4,.95)
        o.data.materials.append(mat(f"{surf}{col}{present}",col, 1 if present else .12))
        if not present: o.display_type='WIRE'
    for f in room['fruits']:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=.4,location=u2b([f['position'][0]+ox,f['position'][1],f['position'][2]]))
        c={'bleu':(.3,.5,1),'rouge':(1,.3,.2)}.get(f['vers'],(1,1,1)); bpy.context.object.data.materials.append(mat('fruit'+f['vers'],c))
    for cl in room['clims']:
        if dname not in member(cl.get('dimensions','')): continue
        bpy.ops.mesh.primitive_uv_sphere_add(radius=.3,location=u2b([cl['position'][0]+ox,cl['position'][1],cl['position'][2]]))
        bpy.context.object.data.materials.append(mat('clim',(.2,.9,1)))
    bpy.ops.object.text_add(location=u2b([ox-3,0.2,-12])); t=bpy.context.object; t.data.body=dname.upper(); t.data.size=4; t.rotation_euler=Euler((0,0,0))
    t.data.materials.append(mat('txt'+dname,{'bleu':(.3,.5,1),'rouge':(1,.35,.3)}.get(dname,(1,1,1))))
    a=room['apparition']; bpy.ops.mesh.primitive_cylinder_add(radius=.35,depth=2,location=u2b([a[0]+ox,a[1]+1,a[2]]))
    bpy.context.object.data.materials.append(mat('lumka',(1,.8,.1)))
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
cam.location=(75,-5,48); 
direction=Vector((0,42,2))-cam.location; cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
cam.data.lens=22
sc.render.engine='CYCLES'; sc.cycles.samples=24; sc.cycles.use_denoising=False; sc.cycles.device='CPU'
sun=bpy.data.objects.new('sun',bpy.data.lights.new('sun','SUN')); sun.data.energy=2.2; sun.rotation_euler=(math.radians(40),math.radians(20),math.radians(30)); sc.collection.objects.link(sun)
w=bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(.12,.13,.16,1); w.node_tree.nodes['Background'].inputs[1].default_value=.5
sc.render.film_transparent=False
sc.render.resolution_x=1600; sc.render.resolution_y=1000
sc.render.filepath=out
bpy.ops.render.render(write_still=True)
