"""Model-specific, non-destructive loader intake. Run with --factory-startup --disable-autoexec."""
import bpy, bmesh, os, json, math
from mathutils import Vector, Matrix
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
out=os.path.join(root,'Assets','Snowglow','Catalog','Sources','LowPolySnowLoader')
logs=os.path.join(root,'Logs','LoaderIntake')
os.makedirs(out,exist_ok=True);os.makedirs(logs,exist_ok=True)
source=bpy.data.objects['LOADER - move whole model']
objects=[o for o in source.children_recursive if o.type in {'MESH','CURVE'}]
groups={k:[] for k in ('body','boom','bucket','wheel_fl','wheel_fr','wheel_rl','wheel_rr')}
centres={'wheel_fl':Vector((1.03,.91,.73)),'wheel_fr':Vector((1.03,-.91,.73)),
         'wheel_rl':Vector((-1.35,.91,.73)),'wheel_rr':Vector((-1.35,-.91,.73)),
         'boom':Vector((.53,0,1.74)),'bucket':Vector((2.81,0,1.03))}
for o in objects:
    parents=[];parent=o.parent
    while parent:parents.append(parent);parent=parent.parent
    ancestry=[a.name for a in parents]
    if any(' wheel' in a for a in ancestry):
        wheel=next(a for a in parents if ' wheel' in a.name)
        # Source's left/right labels differ from Unity's chosen forward axes.
        tyre=next(c for c in wheel.children if c.name.startswith('Faceted tyre'))
        key=min((k for k in centres if k.startswith('wheel')),key=lambda k:(centres[k]-tyre.matrix_world.translation).length)
    elif '04 Wide snow bucket' in ancestry:key='bucket'
    elif '03 Low poly lift arms and hydraulics' in ancestry and not o.name.startswith('Lift arm tower'):key='boom'
    else:key='body'
    groups[key].append(o)
with open(os.path.join(out,'PartMap.json'),'w') as f:json.dump({k:[o.name for o in v] for k,v in groups.items()},f,indent=2)

# The supplied grip blocks have inward winding. Repair copies in memory; the
# original blend is never saved. Their 0.73 m tips already stand above the tyre.
tread_report=[]
for obj in objects:
    if not obj.name.startswith('Chunky staggered tread'):continue
    obj.data=obj.data.copy();bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
    unseen=set(bm.verts);blocks=0
    while unseen:
        pending=[unseen.pop()];verts=set(pending)
        while pending:
            for edge in pending.pop().link_edges:
                for v in edge.verts:
                    if v in unseen:unseen.remove(v);verts.add(v);pending.append(v)
        center=sum((v.co for v in verts),Vector())/len(verts)
        faces={f for v in verts for f in v.link_faces}
        assert all(f.normal.dot(f.calc_center_median()-center)>0 for f in faces)
        blocks+=1
    bm.to_mesh(obj.data);bm.free();tread_report.append(dict(name=obj.name,outward_blocks=blocks))
with open(os.path.join(out,'TreadValidation.json'),'w') as f:json.dump(tread_report,f,indent=2)

scene=bpy.context.scene
for o in scene.objects:
    if o.type=='MESH' and o not in objects:o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
cam=scene.camera
cam.location=(8,-9,6);cam.rotation_euler=(Vector((.3,0,1.5))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=8.2
scene.render.filepath=os.path.join(logs,'source-preview.png');bpy.ops.render.render(write_still=True)

# A point-sampled color atlas collapses all opaque colors to one shared material.
colors={m.name:list(m.diffuse_color) for m in bpy.data.materials if m.name[:2].isdigit()}
names=sorted(colors)
def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
atlas=bpy.data.images.new('Loader palette',16,1,alpha=False)
atlas.colorspace_settings.name='Non-Color'
pixels=[]
for i in range(16):
    color=colors[names[i]] if i<len(names) else [1,1,1,1]
    pixels+= [srgb(c) for c in color[:3]]+[1]
atlas.pixels=pixels;atlas.filepath_raw=os.path.join(out,'Palette.png');atlas.file_format='PNG';atlas.save()
opaque=bpy.data.materials.new('LoaderPalette');opaque.diffuse_color=(1,1,1,1)
glass=bpy.data.materials.new('LoaderGlass');glass.diffuse_color=(.53,.72,.8,.22)
deps=bpy.context.evaluated_depsgraph_get();rotation=Matrix.Rotation(-math.pi/2,4,'Z')
stats={}
def module(key):
    origin=centres.get(key,Vector());copies=[]
    for src in groups[key]:
        mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps)
        mesh.transform(src.matrix_world)
        # Lengthen the existing boom to move the bucket clear of front tyres.
        # Bucket-local shape and pin remain unchanged; no polygons are added.
        if key=='boom':
            for v in mesh.vertices:v.co.x=.53+(v.co.x-.53)*(2.28/1.48)
        elif key=='bucket':
            for v in mesh.vertices:v.co.x+=.8
        mesh.transform(rotation @ Matrix.Translation(-origin))
        slots=[m.name for m in mesh.materials]
        # Joining meshes matches UV layers by name. Use exactly one identically
        # named layer so curve hoses and Blender primitives don't export blanks
        # into UV0 while the palette coordinates end up in UV1.
        while mesh.uv_layers:mesh.uv_layers.remove(mesh.uv_layers[0])
        uv=mesh.uv_layers.new(name='PaletteUV')
        indices=[]
        for p in mesh.polygons:
            name=slots[p.material_index];assert name in names,name
            for n in p.loop_indices:uv.data[n].uv=((names.index(name)+.5)/16,.5)
            indices.append(1 if name.startswith('09 ') else 0)
        mesh.materials.clear();mesh.materials.append(opaque);mesh.materials.append(glass)
        for p,idx in zip(mesh.polygons,indices):p.material_index=idx
        obj=bpy.data.objects.new(src.name+'__prepared',mesh);scene.collection.objects.link(obj);copies.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies:o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
    obj=copies[0];obj.name=key;obj.location=rotation @ origin
    # Remove unused material slots, otherwise empty slots still inflate draw submissions.
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.material_slot_remove_unused()
    stats[key]=dict(vertices=len(obj.data.vertices),triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),materials=len(obj.data.materials))
    return obj
assert all(groups.values()),{k:len(v) for k,v in groups.items()}
made={k:module(k) for k in groups}
def export(name,keys):
    bpy.ops.object.select_all(action='DESELECT')
    for k in keys:made[k].select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(out,name+'.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
export('LoaderBody',['body','boom','wheel_fl','wheel_fr','wheel_rl','wheel_rr'])
export('LoaderBucket',['bucket'])
with open(os.path.join(out,'ExportMetrics.json'),'w') as f:json.dump(stats,f,indent=2)
print('LOADER_EXPORT_COMPLETE '+json.dumps(stats))
