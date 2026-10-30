"""Prepare evaluated copies; never execute embedded text or overwrite the source."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'SourceAssets/AlpineTrees/Alpine_Trees_26_Variants.blend'))
out=root/'Assets/Snowglow/Catalog/Sources/AlpineTrees';out.mkdir(parents=True,exist_ok=True)
logs=root/'Logs/AlpineTrees';logs.mkdir(parents=True,exist_ok=True)
collections=sorted([c for c in bpy.data.collections if c.name[:2] in [f'{i:02}' for i in range(1,27)]],key=lambda c:c.name)
scene=bpy.context.scene;original=list(scene.objects);deps=bpy.context.evaluated_depsgraph_get()
materials=sorted({m for c in collections for o in c.all_objects if o.type=='MESH' for m in o.data.materials},key=lambda m:m.name);names=[m.name for m in materials];assert len(names)<=16
def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
palette=bpy.data.images.new('Alpine tree palette',16,1,alpha=False);palette.colorspace_settings.name='Non-Color'
palette.pixels=[v for i in range(16) for v in ([srgb(c) for c in materials[i].diffuse_color[:3]]+[1] if i<len(materials) else [1,1,1,1])]
palette.filepath_raw=str(out/'Palette.png');palette.file_format='PNG';palette.save()
body=bpy.data.materials.new('AlpineTreePalette');snow=bpy.data.materials.new('AlpineTreeSnow')
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=480;scene.render.resolution_y=600;scene.render.resolution_percentage=100
for coll in collections:
    for o in original:o.hide_render=o.name not in coll.all_objects
    scene.camera=bpy.data.objects[coll.name+' | Detail camera'];scene.camera.hide_render=False
    # The source log cameras crop their horizontal ends at portrait aspect.
    # Reframe the data-only preview without changing source geometry or saving it.
    points=[o.matrix_world@v.co for o in coll.all_objects if o.type=='MESH' for v in o.data.vertices]
    centre=Vector(tuple((min(p[i] for p in points)+max(p[i] for p in points))*.5 for i in range(3)))
    scene.camera.constraints.clear();scene.camera.data.type='ORTHO';scene.camera.data.shift_x=scene.camera.data.shift_y=0
    direction=Vector((1,-1,.45)).normalized()
    scene.camera.location=centre+direction*max((p-centre).length for p in points)*4
    scene.camera.rotation_euler=(-direction).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
    projected=[scene.camera.matrix_world.inverted()@p for p in points]
    scene.camera.data.ortho_scale=2.3*max(max(abs(p.y) for p in projected),max(abs(p.x) for p in projected)/(scene.render.resolution_x/scene.render.resolution_y))
    scene.render.filepath=str(logs/(coll.name[:2]+'-source.png'));bpy.ops.render.render(write_still=True)
for o in original:o.hide_render=True
rows=[];mapping={};audits=[]
for coll in collections:
    sources=[o for o in coll.all_objects if o.type=='MESH'];assert len(sources) in (1,2)
    points=[o.matrix_world@v.co for o in sources for v in o.data.vertices]
    floor=min(p.z for p in points);anchor=next(o for o in coll.all_objects if o.type=='EMPTY').matrix_world.translation
    origin=Vector((anchor.x,anchor.y,floor));made=[];groups={};base=[];woodpoints=[]
    for source in sources:
        mesh=bpy.data.meshes.new_from_object(source.evaluated_get(deps),depsgraph=deps);mesh.transform(Matrix.Translation(-origin)@source.matrix_world)
        bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update();unseen=set(bm.faces);closed=flipped=0
        while unseen:
            first=unseen.pop();component={first};pending=[first]
            while pending:
                face=pending.pop()
                for edge in face.edges:
                    for other in edge.link_faces:
                        if other in unseen:unseen.remove(other);component.add(other);pending.append(other)
            if all(len(e.link_faces)==2 for f in component for e in f.edges):
                before={f:f.normal.copy() for f in component};bmesh.ops.recalc_face_normals(bm,faces=list(component));bm.normal_update();flipped+=sum(n.dot(f.normal)<-.9 for f,n in before.items());closed+=1
        bm.to_mesh(mesh);bm.free();audits.append({'object':source.name,'closedSolids':closed,'flippedFaces':flipped})
        key='SnowCap' if source.name.endswith(' | Snow') else 'TreeWood';slots=[m.name for m in mesh.materials]
        while mesh.uv_layers:mesh.uv_layers.remove(mesh.uv_layers[0])
        uv=mesh.uv_layers.new(name='PaletteUV')
        for face in mesh.polygons:
            index=names.index(slots[face.material_index])
            for loop in face.loop_indices:uv.data[loop].uv=((index+.5)/16,.5)
            face.material_index=0
        mesh.materials.clear();mesh.materials.append(snow if key=='SnowCap' else body);mesh.calc_loop_triangles()
        degenerate=sum((mesh.vertices[t.vertices[1]].co-mesh.vertices[t.vertices[0]].co).cross(mesh.vertices[t.vertices[2]].co-mesh.vertices[t.vertices[0]].co).length<1e-10 for t in mesh.loop_triangles)
        groups[key]={'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles),'degenerateTriangles':degenerate}
        if key=='TreeWood':
            base=[list(v.co) for v in mesh.vertices if v.co.z<.08]
            woodpoints=[v.co.copy() for v in mesh.vertices]
        obj=bpy.data.objects.new(key,mesh);scene.collection.objects.link(obj);made.append(obj)
    for name,position in [('BasisX',(1,0,0)),('BasisY',(0,1,0)),('BasisZ',(0,0,1))]:
        obj=bpy.data.objects.new(name,None);scene.collection.objects.link(obj);obj.location=position;made.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in made:obj.select_set(True)
    slug=coll.name[3:].replace(' ','');bpy.ops.export_scene.fbx(filepath=str(out/(slug+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    pts=[v.co for o in made if o.type=='MESH' for v in o.data.vertices];lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)]
    lower=[p for p in woodpoints if p.z<min(.5,hi[2]*.25)]
    radius=max(.08,max((p.x*p.x+p.y*p.y)**.5 for p in lower))
    is_clutter=int(coll.name[:2])>=21
    rows.append({'index':coll.name[:2],'name':coll.name[3:],'model':slug,'size':[hi[0]-lo[0],hi[2]-lo[2],hi[1]-lo[1]],'groups':groups,'triangles':sum(g['triangles']-g['degenerateTriangles'] for g in groups.values()),'snow':len(sources)==2,'clutter':is_clutter,'basePoints':[{'xyz':p} for p in base],'trunkRadius':radius})
    mapping[coll.name]={'included':[o.name for o in sources],'excluded':[o.name for o in coll.all_objects if o not in sources],'origin':list(origin),'groups':list(groups)}
    for obj in made:bpy.data.objects.remove(obj,do_unlink=True)
(out/'ExportMetrics.json').write_text(json.dumps({'trees':rows,'normalChecks':audits,'palette':[{'name':m.name,'color':list(m.diffuse_color)} for m in materials]},indent=2))
(out/'PartMap.json').write_text(json.dumps({'collections':mapping,'excludedPresentation':[o.name for o in bpy.data.collections['00 Presentation | Hide for Export'].all_objects]},indent=2))
print('TREE_EXPORT_COMPLETE',[(r['name'],r['triangles']) for r in rows],flush=True)
