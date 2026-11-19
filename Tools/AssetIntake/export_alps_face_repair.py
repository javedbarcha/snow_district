"""Test-only derivatives: exterior panel winding and intersecting roof snow.

Run with --factory-startup --disable-autoexec. Never executes source Text blocks.
Keeps source vertex/face counts, UVs, materials and all architecture positions.
"""
import bpy, bmesh, os, json, math, hashlib
from mathutils import Vector, Matrix
R=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT=os.path.join(R,'Assets/Snowglow/Levels/AlpsTerrainTest/BuildingFaceRepair/Sources')
os.makedirs(OUT,exist_ok=True)
report=[]

def components(bm):
 seen=set()
 for first in bm.faces:
  if first in seen:continue
  stack=[first];seen.add(first);group=[]
  while stack:
   f=stack.pop();group.append(f)
   for e in f.edges:
    for other in e.link_faces:
     if other not in seen:seen.add(other);stack.append(other)
  yield group

for filename in ['Snow_District_Alps_Buildings.blend','Snow_District_Statue_and_Tower.blend']:
 path=os.path.join(R,'SourceAssets/AlpsRebuild',filename)
 bpy.ops.wm.open_mainfile(filepath=path)
 deps=bpy.context.evaluated_depsgraph_get()
 for collection in sorted(bpy.data.collections,key=lambda c:c.name):
  if not collection.name[:2].isdigit():continue
  slug='Building_'+collection.name[:2] if 'Buildings' in filename else 'Monument' if collection.name.startswith('01') else 'GateTower'
  origin=next(o for o in collection.objects if o.type=='EMPTY' and o.name.endswith('ROOT')).matrix_world.translation.copy()
  objects=[o for o in collection.objects if o.type=='MESH' and not o.name.startswith('COLLISION_PROXY')]
  meshes=[];roofs=[];details=[]
  for src in objects:
   mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps)
   matrix=Matrix.Translation(-origin)@src.matrix_world;mesh.transform(matrix)
   if matrix.determinant()<0:mesh.flip_normals()
   bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update();flipped=0
   # Closed components already have positive signed volume. Only disconnected,
   # vertical single-sided facade panels need exterior-facing winding.
   if slug.startswith('Building_') or src.name=='Tower | heraldic shield lions and sundial':
    for faces in components(bm):
     edges=set(e for f in faces for e in f.edges)
     if all(e.is_manifold for e in edges):continue
     for f in faces:
      if abs(f.normal.z)>.05:continue
      # A vertical panel's radial dot is unambiguous on these axis-aligned facades.
      centre=f.calc_center_median();outward=Vector((centre.x,centre.y,0))
      if f.normal.dot(outward)<-.1:
       f.normal_flip();flipped+=1
   bm.to_mesh(mesh);bm.free();mesh.update()
   key='SnowCover' if 'snow' in src.name.lower() else 'Windows' if 'windows' in src.name.lower() else 'Architecture'
   if key=='Architecture' and slug!='Monument':
    # Actual upward roof planes, excluding horizontal floor/balcony/chimney tops.
    for p in mesh.polygons:
     if .15<p.normal.z<.99 and p.area>1:
      roofs.append((p.normal.copy(),p.center.copy(),[mesh.vertices[i].co.copy() for i in p.vertices]))
   meshes.append((src.name,key,mesh));details.append(dict(name=src.name,flipped=flipped,movedSnowVertices=0,maxSnowLift=0))
  for (name,key,mesh),detail in zip(meshes,details):
   if key!='SnowCover' or slug=='Monument':continue
   targets={}
   for p in mesh.polygons:
    if not (.15<p.normal.z<.99 and p.area>1):continue
    candidates=[r for r in roofs if r[0].dot(p.normal)>.95]
    if not candidates:continue
    normal,centre,points=min(candidates,key=lambda r:(r[1]-p.center).length_squared)
    # Pair homologous roof surfaces; never project the main roof onto a dormer.
    if (centre-p.center).length>2:continue
    for i in p.vertices:
     v=mesh.vertices[i].co
     z=centre.z-(normal.x*(v.x-centre.x)+normal.y*(v.y-centre.y))/normal.z+.08
     targets[i]=max(targets.get(i,v.z),v.z,z)
   for i,z in targets.items():
    lift=z-mesh.vertices[i].co.z
    if lift>1e-5:detail['movedSnowVertices']+=1;detail['maxSnowLift']=max(detail['maxSnowLift'],lift);mesh.vertices[i].co.z=z
   mesh.update()
  made=[];palette='BuildingPalette' if slug.startswith('Building') else 'LandmarkPalette';counts={}
  for key in sorted(set(k for _,k,_ in meshes)):
   copies=[]
   for name,k,mesh in meshes:
    if k!=key:continue
    for p in mesh.polygons:p.use_smooth=False;p.material_index=0
    mesh.materials.clear();mat=bpy.data.materials.get('Rebuild_'+palette+'_'+key) or bpy.data.materials.new('Rebuild_'+palette+'_'+key);mesh.materials.append(mat)
    obj=bpy.data.objects.new(name+'__faceRepair',mesh);bpy.context.scene.collection.objects.link(obj);copies.append(obj)
   bpy.ops.object.select_all(action='DESELECT')
   for o in copies:o.select_set(True)
   bpy.context.view_layer.objects.active=copies[0]
   if len(copies)>1:bpy.ops.object.join()
   obj=copies[0];obj.name=key;obj.data.calc_loop_triangles();counts[key]=len(obj.data.loop_triangles);made.append(obj)
  for name,co in [('BasisX',(1,0,0)),('BasisY',(0,1,0)),('BasisZ',(0,0,1)),('Front',(0,-1,0))]:
   obj=bpy.data.objects.new(name,None);bpy.context.scene.collection.objects.link(obj);obj.location=co;made.append(obj)
  bpy.ops.object.select_all(action='DESELECT')
  for o in made:o.select_set(True)
  bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,slug+'.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
  report.append(dict(slug=slug,source=filename,sha256=hashlib.sha256(open(path,'rb').read()).hexdigest(),parts=details,triangles=counts))
  for o in made:bpy.data.objects.remove(o,do_unlink=True)
with open(os.path.join(OUT,'RepairManifest.json'),'w') as f:json.dump(report,f,indent=2)
print('REPAIR_EXPORTED',len(report),'assemblies;',sum(p['flipped'] for r in report for p in r['parts']),'panel faces flipped;',sum(p['movedSnowVertices'] for r in report for p in r['parts']),'snow vertices lifted')
