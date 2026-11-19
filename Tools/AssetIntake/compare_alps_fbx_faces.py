import bpy, os, json
from mathutils import Vector
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
result=[]
for filename in ['Snow_District_Alps_Buildings.blend','Snow_District_Statue_and_Tower.blend']:
 bpy.ops.wm.open_mainfile(filepath=os.path.join(R,'SourceAssets/AlpsRebuild',filename))
 deps=bpy.context.evaluated_depsgraph_get(); expected={}
 for c in sorted(bpy.data.collections,key=lambda c:c.name):
  if not c.name[:2].isdigit():continue
  origin=next(o for o in c.objects if o.type=='EMPTY' and o.name.endswith('ROOT')).matrix_world.translation.copy()
  slug='Building_'+c.name[:2] if 'Buildings' in filename else 'Monument' if c.name.startswith('01') else 'GateTower'
  triangles={};parts=[]
  for o in c.objects:
   if o.type!='MESH' or o.name.startswith('COLLISION_PROXY'):continue
   m=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);m.calc_loop_triangles()
   nm=o.matrix_world.to_3x3().inverted().transposed()
   for t in m.loop_triangles:
    pts=[o.matrix_world@m.vertices[i].co-origin for i in t.vertices]
    centre=sum(pts,Vector())/3
    key=tuple(round(v,3) for v in centre)
    triangles.setdefault(key,[]).append(((nm@t.normal).normalized().copy(),o.name))
   # signed volume per source object identifies closed faces directed inward
   volume=sum(m.vertices[t.vertices[0]].co.dot(m.vertices[t.vertices[1]].co.cross(m.vertices[t.vertices[2]].co))/6 for t in m.loop_triangles)
   parts.append({'name':o.name,'volume':volume,'faces':len(m.polygons)})
   bpy.data.meshes.remove(m)
  expected[slug]=(triangles,parts)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for slug,(triangles,parts) in expected.items():
  bpy.ops.import_scene.fbx(filepath=os.path.join(R,'Assets/Snowglow/Catalog/Sources/AlpsRebuild',slug+'.fbx'))
  matched=wrong=missing=0;bad={}
  for o in bpy.context.selected_objects:
   if o.type!='MESH':continue
   m=o.data;m.calc_loop_triangles();nm=o.matrix_world.to_3x3().inverted().transposed()
   for t in m.loop_triangles:
    centre=sum((o.matrix_world@m.vertices[i].co for i in t.vertices),Vector())/3
    key=tuple(round(v,3) for v in centre);options=triangles.get(key,[])
    if not options:missing+=1;continue
    matched+=1;n=(nm@t.normal).normalized()
    if max(n.dot(v[0]) for v in options)<-.5:
     wrong+=1;name=options[0][1];bad[name]=bad.get(name,0)+1
  result.append(dict(slug=slug,matched=matched,reversed=wrong,unmatched=missing,bad=bad,parts=parts))
  bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
with open(os.path.join(R,'Logs/AlpsBuildingFaces/FbxComparison.json'),'w') as f:json.dump(result,f,indent=2)
for row in result:print(row['slug'],row['matched'],row['reversed'],row['unmatched'],row['bad'])
