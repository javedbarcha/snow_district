"""Export prepared copies, preserving the original .blend and separate snow caps."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'SourceAssets/FiveAlpineMountains/Five_Alpine_Mountains.blend'))
out=root/'Assets/Snowglow/Catalog/Sources/FiveAlpineMountains';out.mkdir(parents=True,exist_ok=True)
logs=root/'Logs/AlpineMountains';logs.mkdir(parents=True,exist_ok=True)
collections=sorted([c for c in bpy.data.collections if c.name[:2] in ['01','02','03','04','05']],key=lambda c:c.name)
materials=sorted([m for m in bpy.data.materials if m.name.startswith('Rock /')],key=lambda m:m.name)
def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
palette=bpy.data.images.new('Alpine rock palette',16,1,alpha=False);palette.colorspace_settings.name='Non-Color'
palette.pixels=[v for i in range(16) for v in ([srgb(c) for c in materials[i].diffuse_color[:3]]+[1] if i<len(materials) else [1,1,1,1])]
palette.filepath_raw=str(out/'Palette.png');palette.file_format='PNG';palette.save()
rock=bpy.data.materials.new('AlpineRockPalette');snow=bpy.data.materials.new('AlpineSnow')
scene=bpy.context.scene;deps=bpy.context.evaluated_depsgraph_get();original=list(scene.objects)
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=720;scene.render.resolution_y=600;scene.render.resolution_percentage=100
for coll in collections:
    for o in original:o.hide_render=o.name not in coll.all_objects
    scene.camera=bpy.data.objects[coll.name+' | detail camera'];scene.camera.hide_render=False
    scene.render.filepath=str(logs/(coll.name[:2]+'-source.png'));bpy.ops.render.render(write_still=True)
for o in original:o.hide_render=True
rows=[];mapping={};normal_checks=[]
for coll in collections:
    sources=[o for o in coll.all_objects if o.type=='MESH']
    assert len(sources)==2,coll.name
    points=[o.matrix_world@Vector(v) for o in sources for v in o.bound_box]
    low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    origin=Vector(((low.x+high.x)*.5,(low.y+high.y)*.5,low.z));made=[];groups={}
    for source in sources:
        mesh=bpy.data.meshes.new_from_object(source.evaluated_get(deps),depsgraph=deps);mesh.transform(Matrix.Translation(-origin)@source.matrix_world)
        bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update();closed=all(e.is_manifold for e in bm.edges)
        before=[f.normal.copy() for f in bm.faces]
        if closed:
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update();assert bm.calc_volume(signed=True)>0,source.name
        normal_checks.append({'object':source.name,'closed':closed,'flippedFaces':sum(n.dot(f.normal)<-.9 for n,f in zip(before,bm.faces))})
        bm.to_mesh(mesh);bm.free()
        is_snow=source.name.endswith('Snow cap');name='SnowCap' if is_snow else 'Rock'
        slots=[m.name for m in mesh.materials]
        while mesh.uv_layers:mesh.uv_layers.remove(mesh.uv_layers[0])
        uv=mesh.uv_layers.new(name='PaletteUV')
        for face in mesh.polygons:
            index=0 if is_snow else next(i for i,m in enumerate(materials) if m.name==slots[face.material_index])
            for loop in face.loop_indices:uv.data[loop].uv=((index+.5)/16,.5)
            face.material_index=0;face.use_smooth=False
        mesh.materials.clear();mesh.materials.append(snow if is_snow else rock);mesh.calc_loop_triangles()
        assert all((mesh.vertices[t.vertices[1]].co-mesh.vertices[t.vertices[0]].co).cross(mesh.vertices[t.vertices[2]].co-mesh.vertices[t.vertices[0]].co).length>1e-10 for t in mesh.loop_triangles),source.name
        groups[name]={'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles)}
        obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj);made.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in made:obj.select_set(True)
    slug=coll.name[3:].replace(' ','')
    bpy.ops.export_scene.fbx(filepath=str(out/(slug+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    rows.append({'index':coll.name[:2],'name':coll.name[3:],'model':slug,'size':[high.x-low.x,high.z-low.z,high.y-low.y],'groups':groups,'triangles':sum(g['triangles'] for g in groups.values())})
    mapping[coll.name]={'included':[o.name for o in sources],'excluded':[o.name for o in coll.all_objects if o not in sources]}
    for obj in made:bpy.data.objects.remove(obj,do_unlink=True)
(out/'ExportMetrics.json').write_text(json.dumps({'mountains':rows,'normalChecks':normal_checks,'rockPalette':[{'name':m.name,'color':list(m.diffuse_color)} for m in materials]},indent=2))
(out/'PartMap.json').write_text(json.dumps({'collections':mapping,'excludedPresentation':[o.name for o in bpy.data.collections['00 Presentation | hide for export'].all_objects]},indent=2))
print('ALPINE_EXPORT_COMPLETE',json.dumps(rows),flush=True)
