"""Preserve the .blend; export evaluated copies with palette UVs and detachable snow."""
import bpy, bmesh, os, json, math
from mathutils import Vector, Matrix
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'SourceAssets/SevenWinterHomes/Seven_Winter_Homes.blend'))
out=os.path.join(root,'Assets/Snowglow/Catalog/Sources/SevenWinterHomes');os.makedirs(out,exist_ok=True)
preview=os.path.join(root,'Logs/WinterHomes');os.makedirs(preview,exist_ok=True)
collections=sorted([c for c in bpy.data.collections if c.name[:2] in ['01','02','03','04','05','06','07']],key=lambda c:c.name)
materials=sorted([m for m in bpy.data.materials if m.name not in ['Dots Stroke'] and not m.name.startswith('Backdrop')],key=lambda m:m.name)
names=[m.name for m in materials]
def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
atlas=bpy.data.images.new('Winter homes palette',32,1,alpha=False);atlas.colorspace_settings.name='Non-Color'
atlas.pixels=[v for i in range(32) for v in ([srgb(c) for c in materials[i].diffuse_color[:3]]+[1] if i<len(materials) else [1,1,1,1])]
atlas.filepath_raw=os.path.join(out,'Palette.png');atlas.file_format='PNG';atlas.save()
bodymat=bpy.data.materials.new('WinterHomesPalette');bodymat.diffuse_color=(1,1,1,1)
windowmat=bpy.data.materials.new('WinterHomesWarmWindows');windowmat.diffuse_color=(1,.65,.22,1)
snowmat=bpy.data.materials.new('WinterHomesSnow');snowmat.diffuse_color=(1,1,1,1)
deps=bpy.context.evaluated_depsgraph_get();scene=bpy.context.scene;stats=[];partmap={};normal_checks=[]
# Render the original named collections independently before making export copies.
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.background_type='WORLD'
scene.world.color=(.16,.24,.30);scene.render.resolution_x=720;scene.render.resolution_y=600;scene.render.resolution_percentage=100
original=list(scene.objects)
for coll in collections:
    idx=coll.name[:2]
    for o in original:o.hide_render=o.name not in coll.all_objects
    scene.camera=bpy.data.objects[coll.name+' • camera'];scene.camera.hide_render=False
    scene.render.filepath=os.path.join(preview,idx+'-source.png');bpy.ops.render.render(write_still=True)
for o in original:o.hide_render=True
for coll in collections:
    idx=coll.name[:2];slug=coll.name[3:].replace(' ','')
    # Fonts, camera rigs, studio and square diorama ground are presentation only.
    objects=[o for o in coll.all_objects if o.type=='MESH' and not o.name.startswith(('Raised diorama foundation','Snow ground'))]
    pts=[o.matrix_world@Vector(p) for o in objects for p in o.bound_box]
    floor=min((o.matrix_world@Vector(p)).z for o in objects if not o.name.startswith('Low-poly snow drift') for p in o.bound_box)
    origin=Vector(((min(p.x for p in pts)+max(p.x for p in pts))/2,(min(p.y for p in pts)+max(p.y for p in pts))/2,floor))
    groups={'Architecture':[],'WarmWindows':[],'RoofSnow':[],'GroundSnow':[]}
    for o in objects:
        mats=[m.name for m in o.data.materials]
        if any(m.startswith('Windows') for m in mats):key='WarmWindows'
        elif any(m.startswith('Snow') for m in mats):key='GroundSnow' if o.name.startswith('Low-poly snow drift') else 'RoofSnow'
        else:key='Architecture'
        groups[key].append(o)
    partmap[idx]={'collection':coll.name,'excluded':[o.name for o in coll.all_objects if o not in objects], 'groups':{k:[o.name for o in v] for k,v in groups.items()}}
    made=[];metrics={}
    for key,sources in groups.items():
        copies=[]
        for src in sources:
            mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps);mesh.transform(Matrix.Translation(-origin)@src.matrix_world)
            if key=='GroundSnow':
                bm=bmesh.new();bm.from_mesh(mesh)
                cut=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,0),plane_no=(0,0,1),clear_inner=True)
                boundary=[e for e in cut['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary]
                if boundary:bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
                bm.to_mesh(mesh);bm.free()
            # Repair each closed source solid before joining: recalculating the
            # combined building can confuse overlapping shells. Open facade
            # panels retain their intentional front direction.
            bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update()
            closed=bool(bm.faces) and all(e.is_manifold for e in bm.edges)
            if closed:
                before=[f.normal.copy() for f in bm.faces]
                before_volume=bm.calc_volume(signed=True)
                bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
                changed=sum(n.dot(f.normal)<-.9 for n,f in zip(before,bm.faces))
                volume=bm.calc_volume(signed=True)
                assert volume>0,(src.name,volume)
                normal_checks.append({'collection':coll.name,'object':src.name,'faces':len(bm.faces),'flippedFaces':changed,'signedVolumeBefore':before_volume,'signedVolumeAfter':volume})
                bm.to_mesh(mesh)
            bm.free()
            slots=[m.name for m in mesh.materials]
            while mesh.uv_layers:mesh.uv_layers.remove(mesh.uv_layers[0])
            uv=mesh.uv_layers.new(name='PaletteUV')
            for p in mesh.polygons:
                name=slots[p.material_index];assert name in names,name
                for n in p.loop_indices:uv.data[n].uv=((names.index(name)+.5)/32,.5)
                p.material_index=0
            mesh.materials.clear();mesh.materials.append(windowmat if key=='WarmWindows' else snowmat if 'Snow' in key else bodymat)
            obj=bpy.data.objects.new(src.name+'__prepared',mesh);scene.collection.objects.link(obj);copies.append(obj)
        if not copies:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in copies:o.select_set(True)
        bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=copies[0];o.name=key
        # Flat faces suit the source's mid low-poly presentation.
        for p in o.data.polygons:p.use_smooth=False
        o.data.calc_loop_triangles()
        degenerate=sum((o.data.vertices[t.vertices[1]].co-o.data.vertices[t.vertices[0]].co).cross(o.data.vertices[t.vertices[2]].co-o.data.vertices[t.vertices[0]].co).length*.5<1e-10 for t in o.data.loop_triangles)
        metrics[key]={'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'degenerateTriangles':degenerate};made.append(o)
    # Export reference frames so Unity derives axes from its importer, not a
    # guessed handedness conversion. They are removed from prepared prefabs.
    for name,position in [('BasisX',(1,0,0)),('BasisY',(0,1,0)),('BasisZ',(0,0,1))]:
        marker=bpy.data.objects.new(name,None);scene.collection.objects.link(marker);marker.location=position;made.append(marker)
    bpy.ops.object.select_all(action='DESELECT')
    for o in made:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(out,slug+'.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    # Unity coordinates under the exporter: (x,z,-y).
    door=next((o for o in objects if o.name.startswith('Door leaf')),None)
    entry=door.matrix_world.translation-origin if door else Vector((0,min(p.y for p in pts)-origin.y,0))
    boxes=[]
    for o in objects:
        if not any(w in o.name.lower() for w in ['walls','entry step','porch deck','raised deck']):continue
        if 'dormer' in o.name.lower():continue
        p=[o.matrix_world@Vector(v)-origin for v in o.bound_box]
        lo=Vector((min(v.x for v in p),min(v.z for v in p),-max(v.y for v in p)))
        hi=Vector((max(v.x for v in p),max(v.z for v in p),-min(v.y for v in p)))
        boxes.append({'name':o.name,'center':list((lo+hi)*.5),'size':list(hi-lo)})
    stats.append({'index':idx,'name':coll.name[3:],'model':slug,'groups':metrics,'triangles':sum(g['triangles'] for g in metrics.values()),'degenerateTriangles':sum(g['degenerateTriangles'] for g in metrics.values()),'size':[max(p.x for p in pts)-min(p.x for p in pts),max(p.z for p in pts)-floor,max(p.y for p in pts)-min(p.y for p in pts)],'entrance':[entry.x,0,-entry.y],'sourceOrigin':list(origin),'colliders':boxes})
    for o in made:bpy.data.objects.remove(o,do_unlink=True)
with open(os.path.join(out,'ExportMetrics.json'),'w') as f:json.dump({'homes':stats,'palette':{m.name:list(m.diffuse_color) for m in materials}},f,indent=2)
with open(os.path.join(out,'PartMap.json'),'w') as f:json.dump(partmap,f,indent=2)
with open(os.path.join(out,'NormalValidation.json'),'w') as f:json.dump({'closedSolidsChecked':len(normal_checks),'repaired':[r for r in normal_checks if r['flippedFaces']],'checks':normal_checks,'openPanels':'Preserved source front direction; no automatic normal reversal.'},f,indent=2)
print('WINTER_HOMES_EXPORT_COMPLETE',json.dumps(stats))
