import bpy,bmesh,os,json,hashlib
from mathutils import Matrix,Vector
root=os.getcwd();out=os.path.join(root,'Assets/Snowglow/Catalog/Sources/AlpsMountainsV2');os.makedirs(out,exist_ok=True)
image=next(i for i in bpy.data.images if i.size[0]>0);image.filepath_raw=os.path.join(out,'SourcePalette.png');image.file_format='PNG';image.save()
rows=[];deps=bpy.context.evaluated_depsgraph_get()
for c in sorted(bpy.data.collections,key=lambda c:c.name):
 if not c.name[:2].isdigit():continue
 objects=[o for o in c.objects if o.type=='MESH'];assert len(objects)==1
 src=objects[0];mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps);pts=[src.matrix_world@v.co for v in mesh.vertices];origin=Vector(((min(v.x for v in pts)+max(v.x for v in pts))*.5,(min(v.y for v in pts)+max(v.y for v in pts))*.5,min(v.z for v in pts)));mesh.transform(Matrix.Translation(-origin)@src.matrix_world)
 bm=bmesh.new();bm.from_mesh(mesh);closed=all(e.is_manifold for e in bm.edges)
 if closed:bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh)
 bm.free()
 for face in mesh.polygons:face.use_smooth=False;face.material_index=0
 mesh.materials.clear();mat=bpy.data.materials.get('AlpsV2Palette') or bpy.data.materials.new('AlpsV2Palette');mesh.materials.append(mat);obj=bpy.data.objects.new('Mountain',mesh);bpy.context.scene.collection.objects.link(obj)
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 slug='Mountain_'+c.name[:2];bpy.ops.export_scene.fbx(filepath=os.path.join(out,slug+'.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
 mesh.calc_loop_triangles();v=[p.co for p in mesh.vertices];rows.append({'slug':slug,'label':c.name,'vertices':len(v),'triangles':len(mesh.loop_triangles),'closed':closed,'size':[max(p.x for p in v)-min(p.x for p in v),max(p.z for p in v)-min(p.z for p in v),max(p.y for p in v)-min(p.y for p in v)]});bpy.data.objects.remove(obj,do_unlink=True)
source=os.path.join(root,'SourceAssets/AlpsMountainsV2/Mountains.blend');data={'source':source,'sha256':hashlib.sha256(open(source,'rb').read()).hexdigest(),'assets':rows,'embeddedTextExecuted':False};open(os.path.join(out,'ExportMetrics.json'),'w').write(json.dumps(data,indent=2));open(os.path.join(root,'SourceAssets/AlpsMountainsV2/Provenance.json'),'w').write(json.dumps(data,indent=2));print('EXPORT_COMPLETE',len(rows),sum(r['triangles'] for r in rows))
