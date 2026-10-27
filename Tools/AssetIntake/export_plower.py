"""Deterministic intake of this model, never executes its embedded text.
Usage: blender --background --factory-startup --disable-autoexec SOURCE --python THIS
Source stays untouched. FBX uses metres, +Y up/-Z forward; Unity's importer converts
to +Z forward. The Blender mapping first makes original +X forward into -Y.
"""
import bpy, json, os, math
from mathutils import Matrix, Vector

root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
out = os.path.join(root,'Assets','Snowglow','Catalog','Sources','LowPolySnowPlower')
logs = os.path.join(root,'Logs','PlowerIntake')
os.makedirs(out,exist_ok=True)
os.makedirs(logs,exist_ok=True)
source = bpy.data.objects.get('TRACTOR - move whole model')
assert source, 'Expected named model root'
original = [o for o in source.children_recursive if o.type == 'MESH']
assert len(original)>100
wheel_prefixes=('Front tire','Rear tire','Chevron tyre grips','Yellow wheel rim','Recessed hub','Hub cap','Hub bolt')
blade_prefixes=('Blade ','Bottom cutting edge','Curved orange snowplow','Cutting edge bolt','Graphite rolled blade lip','Rear blade reinforcing rib','Box section lift arm','Carrier diagonal','Lift ram','Lower chassis pivot','Upper cylinder mount')
groups={k:[] for k in ('body','blade','wheel_fl','wheel_fr','wheel_rl','wheel_rr','beacon')}
centres={'wheel_fl':Vector((.91,.91,.56)), 'wheel_fr':Vector((.91,-.91,.56)),
         'wheel_rl':Vector((-1.02,.91,.87)), 'wheel_rr':Vector((-1.02,-.91,.87))}
for o in original:
    if o.name.startswith(wheel_prefixes):
        pts=[o.matrix_world @ Vector(c) for c in o.bound_box]
        c=sum(pts,Vector())/8
        key=min(centres,key=lambda k:(c-centres[k]).length)
    elif o.name in ('Amber roof beacon','Roof beacon base'): key='beacon'
    elif o.name.startswith(blade_prefixes): key='blade'
    else: key='body'
    groups[key].append(o)
assert sum(map(len,groups.values()))==len(original)
mapping={key:[o.name for o in objects] for key,objects in groups.items()}
with open(os.path.join(out,'PartMap.json'),'w') as f: json.dump(mapping,f,indent=2)

# Save a source-view image before modifying the in-memory model.
for o in bpy.context.scene.objects:
    if o.type=='MESH' and o not in original: o.hide_render=True
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
if not scene.camera:
    cam=bpy.data.objects.new('Intake camera',bpy.data.cameras.new('Intake camera'));scene.collection.objects.link(cam);scene.camera=cam
cam=scene.camera;cam.location=(7,-8,5.5);cam.rotation_euler=(Vector((.2,0,1.25))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=7.3
scene.render.filepath=os.path.join(logs,'source-preview.png')
bpy.ops.render.render(write_still=True)

# Evaluate chamfers; bake all transforms into meshes then join each rigid module.
rotation=Matrix.Rotation(-math.pi/2,4,'Z')
deps=bpy.context.evaluated_depsgraph_get()
def module(key,origin):
    copies=[]
    for src in groups[key]:
        mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps)
        mesh.transform(Matrix.Translation(-origin) @ src.matrix_world)
        obj=bpy.data.objects.new(src.name+'__prepared',mesh);scene.collection.objects.link(obj);copies.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies: o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
    obj=copies[0];obj.name=key
    obj.data.transform(rotation);obj.location=rotation @ origin
    return obj
made={key:module(key,centres.get(key,Vector())) for key in groups if groups[key]}
def export(name,keys):
    bpy.ops.object.select_all(action='DESELECT')
    for key in keys: made[key].select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(out,name+'.fbx'),use_selection=True,
        object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,
        use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
export('TractorBody',['body','wheel_fl','wheel_fr','wheel_rl','wheel_rr'])
export('PlowAttachment',['blade'])
export('RoofBeacon',['beacon'])
print('SNOWGLOW_EXPORT_COMPLETE '+json.dumps({k:len(v) for k,v in groups.items()}))
