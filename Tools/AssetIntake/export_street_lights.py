"""Export reviewed copies only. Original .blend and embedded text remain untouched."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'SourceAssets/AlpineStreetLights/Alpine_Street_Lights_12_Variants.blend'))
out=root/'Assets/Snowglow/Catalog/Sources/AlpineStreetLights';out.mkdir(parents=True,exist_ok=True)
logs=root/'Logs/AlpineStreetLights';logs.mkdir(parents=True,exist_ok=True)
collections=sorted([c for c in bpy.data.collections if c.name[:2] in [f'{i:02}' for i in range(1,13)]],key=lambda c:c.name)
original=list(bpy.context.scene.objects);deps=bpy.context.evaluated_depsgraph_get();scene=bpy.context.scene
used={m for c in collections for o in c.all_objects if o.type=='MESH' for m in o.data.materials}
materials=sorted(used,key=lambda m:m.name);names=[m.name for m in materials]
def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
palette=bpy.data.images.new('Street light palette',16,1,alpha=False);palette.colorspace_settings.name='Non-Color'
palette.pixels=[v for i in range(16) for v in ([srgb(c) for c in materials[i].diffuse_color[:3]]+[1] if i<len(materials) else [1,1,1,1])]
palette.filepath_raw=str(out/'Palette.png');palette.file_format='PNG';palette.save()
target={k:bpy.data.materials.new(n) for k,n in [('Frame','StreetLightPalette'),('Luminous','StreetLightGlow'),('SnowCap','StreetLightSnow')]}
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=480;scene.render.resolution_y=600;scene.render.resolution_percentage=100
for coll in collections:
    for o in original:o.hide_render=o.name not in coll.all_objects
    scene.camera=bpy.data.objects[coll.name+' | Detail camera'];scene.camera.hide_render=False
    scene.render.filepath=str(logs/(coll.name[:2]+'-source.png'));bpy.ops.render.render(write_still=True)
for o in original:o.hide_render=True
rows=[];mapping={};audits=[]
for coll in collections:
    origin=next(o for o in coll.all_objects if o.type=='EMPTY').matrix_world.translation.copy()
    sources=[o for o in coll.all_objects if o.type=='MESH'];made=[];groups={}
    assert len(sources)==2,coll.name
    for source in sources:
        evaluated=bpy.data.meshes.new_from_object(source.evaluated_get(deps),depsgraph=deps);evaluated.transform(Matrix.Translation(-origin)@source.matrix_world)
        bm=bmesh.new();bm.from_mesh(evaluated);bm.normal_update();unseen=set(bm.faces);closed_count=flipped=0
        while unseen:
            first=unseen.pop();component={first};pending=[first]
            while pending:
                face=pending.pop()
                for edge in face.edges:
                    for other in edge.link_faces:
                        if other in unseen:unseen.remove(other);component.add(other);pending.append(other)
            if all(len(e.link_faces)==2 for f in component for e in f.edges):
                before={f:f.normal.copy() for f in component};bmesh.ops.recalc_face_normals(bm,faces=list(component));bm.normal_update()
                flipped+=sum(n.dot(f.normal)<-.9 for f,n in before.items());closed_count+=1
        bm.to_mesh(evaluated);bm.free();audits.append({'object':source.name,'closedSolids':closed_count,'flippedFaces':flipped})
        keys=['SnowCap'] if source.name.endswith(' | Snow') else ['Frame','Luminous']
        for key in keys:
            mesh=evaluated.copy();slots=[m.name for m in mesh.materials];bm=bmesh.new();bm.from_mesh(mesh)
            if key!='SnowCap':
                remove=[f for f in bm.faces if (slots[f.material_index] in ['Warm lantern glazing','Road LED diffuser'])!=(key=='Luminous')]
                bmesh.ops.delete(bm,geom=remove,context='FACES')
                bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
            bm.to_mesh(mesh);bm.free()
            while mesh.uv_layers:mesh.uv_layers.remove(mesh.uv_layers[0])
            uv=mesh.uv_layers.new(name='PaletteUV')
            for face in mesh.polygons:
                index=names.index(slots[face.material_index])
                for loop in face.loop_indices:uv.data[loop].uv=((index+.5)/16,.5)
                face.material_index=0
            mesh.materials.clear();mesh.materials.append(target[key]);mesh.calc_loop_triangles()
            degenerate=sum((mesh.vertices[t.vertices[1]].co-mesh.vertices[t.vertices[0]].co).cross(mesh.vertices[t.vertices[2]].co-mesh.vertices[t.vertices[0]].co).length<1e-10 for t in mesh.loop_triangles)
            groups[key]={'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles),'degenerateTriangles':degenerate}
            obj=bpy.data.objects.new(key,mesh);scene.collection.objects.link(obj);made.append(obj)
        bpy.data.meshes.remove(evaluated)
    for name,position in [('BasisX',(1,0,0)),('BasisY',(0,1,0)),('BasisZ',(0,0,1))]:
        obj=bpy.data.objects.new(name,None);scene.collection.objects.link(obj);obj.location=position;made.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in made:obj.select_set(True)
    slug=coll.name[3:].replace(' ','');bpy.ops.export_scene.fbx(filepath=str(out/(slug+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    pts=[v.co for obj in made if obj.type=='MESH' for v in obj.data.vertices];low=[min(p[i] for p in pts) for i in range(3)];high=[max(p[i] for p in pts) for i in range(3)]
    lights=[{'name':o.name,'position':list(o.matrix_world.translation-origin),'color':list(o.data.color),'sourceWatts':o.data.energy} for o in coll.all_objects if o.type=='LIGHT']
    # Lower pole/base envelope, not the outreach arm: avoids an invisible full-width wall.
    fixture=next(o for o in sources if o.name.endswith(' | Fixture'))
    mesh=bpy.data.meshes.new_from_object(fixture.evaluated_get(deps),depsgraph=deps);mesh.transform(Matrix.Translation(-origin)@fixture.matrix_world)
    base=[v.co for v in mesh.vertices if v.co.z<=min(high[2]*.25,1.2)+.001]
    if coll.name[:2]=='06':base=[v.co for v in mesh.vertices] # wall-only compact fixture proxy
    assert base
    bmin=[min(p[i] for p in base) for i in range(3)];bmax=[max(p[i] for p in base) for i in range(3)]
    center=[(bmin[i]+bmax[i])/2 for i in range(3)];size=[max(.08,bmax[i]-bmin[i]) for i in range(3)]
    if coll.name[:2]!='06':center[2]=high[2]*.5;size[2]=high[2]
    bpy.data.meshes.remove(mesh)
    rows.append({'index':coll.name[:2],'name':coll.name[3:],'model':slug,'size':[high[0]-low[0],high[2]-low[2],high[1]-low[1]],'groups':groups,'triangles':sum(g['triangles']-g['degenerateTriangles'] for g in groups.values()),'lights':lights,'proxyCenter':center,'proxySize':size})
    mapping[coll.name]={'geometry':[o.name for o in sources],'optionalLights':[o.name for o in coll.all_objects if o.type=='LIGHT'],'excluded':[o.name for o in coll.all_objects if o.type=='EMPTY'],'rootOrigin':list(origin),'groups':list(groups)}
    for obj in made:bpy.data.objects.remove(obj,do_unlink=True)
(out/'ExportMetrics.json').write_text(json.dumps({'fixtures':rows,'normalChecks':audits,'palette':[{'name':m.name,'color':list(m.diffuse_color)} for m in materials]},indent=2))
(out/'PartMap.json').write_text(json.dumps({'collections':mapping,'excludedPresentation':[o.name for o in bpy.data.collections['00 Presentation | hide for export'].all_objects]},indent=2))
print('STREET_LIGHT_EXPORT_COMPLETE',[(r['name'],r['triangles']) for r in rows],flush=True)
