import bpy,bmesh,os,json,math
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
base=os.path.join(root,'Assets/Snowglow/Catalog/Sources/AlpsRebuild');os.makedirs(base,exist_ok=True)
rows=[]
def export_asset(slug,kind,objects,origin,palette):
 deps=bpy.context.evaluated_depsgraph_get();made=[];metrics={};partmap={}
 groups={}
 for src in objects:
  key='SnowCover' if 'snow' in src.name.lower() else 'Windows' if 'windows' in src.name.lower() else 'Architecture'
  if kind in ['Terrain','Mountain']:key='Terrain' if kind=='Terrain' else 'Mountain'
  groups.setdefault(key,[]).append(src)
 for key,sources in groups.items():
  copies=[]
  for src in sources:
   mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps);mesh.transform(Matrix.Translation(-origin)@src.matrix_world)
   # Correct normals on evaluated source solids, keeping open panel winding intact.
   bm=bmesh.new();bm.from_mesh(mesh)
   if bm.faces and all(e.is_manifold for e in bm.edges):bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh)
   bm.free()
   for p in mesh.polygons:p.use_smooth=False;p.material_index=0
   mesh.materials.clear();m=bpy.data.materials.get('Rebuild_'+palette+'_'+key) or bpy.data.materials.new('Rebuild_'+palette+'_'+key);mesh.materials.append(m)
   obj=bpy.data.objects.new(src.name+'__prepared',mesh);bpy.context.scene.collection.objects.link(obj);copies.append(obj)
  bpy.ops.object.select_all(action='DESELECT')
  for o in copies:o.select_set(True)
  bpy.context.view_layer.objects.active=copies[0]
  if len(copies)>1:bpy.ops.object.join()
  obj=copies[0];obj.name=key;obj.data.calc_loop_triangles();made.append(obj)
  metrics[key]={'triangles':len(obj.data.loop_triangles),'vertices':len(obj.data.vertices)};partmap[key]=[o.name for o in sources]
 for name,co in [('BasisX',(1,0,0)),('BasisY',(0,1,0)),('BasisZ',(0,0,1)),('Front',(0,-1,0))]:
  o=bpy.data.objects.new(name,None);bpy.context.scene.collection.objects.link(o);o.location=co;made.append(o)
 bpy.ops.object.select_all(action='DESELECT')
 for o in made:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=os.path.join(base,slug+'.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
 pts=[o.matrix_world@v.co for o in made if o.type=='MESH' for v in o.data.vertices]
 row={'slug':slug,'kind':kind,'palette':palette,'parts':list(metrics),'metrics':metrics,'size':[max(p.x for p in pts)-min(p.x for p in pts),max(p.z for p in pts)-min(p.z for p in pts),max(p.y for p in pts)-min(p.y for p in pts)],'partSources':partmap}
 rows.append(row)
 for o in made:bpy.data.objects.remove(o,do_unlink=True)
def open_source(name,palette):
 bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'SourceAssets/AlpsRebuild',name))
 image=next(i for i in bpy.data.images if i.size[0]>0);image.filepath_raw=os.path.join(base,palette+'.png');image.file_format='PNG';image.save()
open_source('Snow_District_Alps_Town_Terrain.blend','TerrainPalette')
o=next(o for o in bpy.data.objects if o.type=='MESH');mesh=o.data;verts=[o.matrix_world@v.co for v in mesh.vertices];polys=[list(p.vertices) for p in mesh.polygons];bvh=BVHTree.FromPolygons(verts,polys)
heights=[]
for z in range(257):
 row=[]
 for x in range(257):
  hit=bvh.ray_cast(Vector((x/256*1000-500,z/256*1000-500,1000)),Vector((0,0,-1)),2000)[0]
  row.append(hit.z if hit else 0)
 heights.append(row)
probe={'centre':heights[128][128],'samples':[{'x':x,'y':y,'height':bvh.ray_cast(Vector((x,y,1000)),Vector((0,0,-1)),2000)[0].z} for x,y in [(0,0),(150,0),(220,0),(0,220),(-220,-220),(400,0),(499,0),(0,499)]], 'heights':heights,'vertices':[[v.x,v.z,-v.y] for v in verts],'triangles':[list(t.vertices) for t in []]}
mesh.calc_loop_triangles();probe['triangles']=[list(t.vertices) for t in mesh.loop_triangles]
with open(os.path.join(root,'SourceAssets/AlpsRebuild/TerrainSurface.json'),'w') as f:json.dump(probe,f)
export_asset('TerrainBase','Terrain',[o],Vector((0,0,0)),'TerrainPalette')
open_source('Snow_District_Alps_Mountains.blend','MountainPalette')
for c in sorted(bpy.data.collections,key=lambda c:c.name):
 if c.name[:2].isdigit():
  obj=next(o for o in c.objects if o.type=='MESH' and o.name.endswith('LOD1'))
  pts=[obj.matrix_world@v.co for v in obj.data.vertices];origin=Vector(((min(v.x for v in pts)+max(v.x for v in pts))*.5,(min(v.y for v in pts)+max(v.y for v in pts))*.5,min(v.z for v in pts)))
  export_asset('Mountain_'+c.name[:2],'Mountain',[obj],origin,'MountainPalette')
open_source('Snow_District_Alps_Buildings.blend','BuildingPalette')
for c in sorted(bpy.data.collections,key=lambda c:c.name):
 if c.name[:2].isdigit():
  objects=[o for o in c.objects if o.type=='MESH' and not o.name.startswith('COLLISION_PROXY')]
  rootobj=next(o for o in c.objects if o.type=='EMPTY' and o.name.endswith('ROOT'))
  export_asset('Building_'+c.name[:2],'Building',objects,rootobj.matrix_world.translation.copy(),'BuildingPalette')
open_source('Snow_District_Statue_and_Tower.blend','LandmarkPalette')
for c in sorted(bpy.data.collections,key=lambda c:c.name):
 if c.name[:2].isdigit():
  rootobj=next(o for o in c.objects if o.type=='EMPTY' and o.name.endswith('ROOT'))
  export_asset('Monument' if c.name.startswith('01') else 'GateTower','Landmark',[o for o in c.objects if o.type=='MESH'],rootobj.matrix_world.translation.copy(),'LandmarkPalette')
with open(os.path.join(base,'ExportMetrics.json'),'w') as f:json.dump({'assets':rows},f,indent=2)
print('ALPS_REBUILD_EXPORT_COMPLETE',len(rows))
