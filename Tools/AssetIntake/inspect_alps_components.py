import bpy,bmesh,json,os
from mathutils import Vector
R=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
rows=[]
for filename in ['Snow_District_Alps_Buildings.blend','Snow_District_Statue_and_Tower.blend']:
 bpy.ops.wm.open_mainfile(filepath=os.path.join(R,'SourceAssets/AlpsRebuild',filename));deps=bpy.context.evaluated_depsgraph_get()
 for c in sorted(bpy.data.collections,key=lambda c:c.name):
  if not c.name[:2].isdigit():continue
  for o in c.objects:
   if o.type!='MESH' or o.name.startswith('COLLISION_PROXY'):continue
   mesh=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);mesh.transform(o.matrix_world);bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update();seen=set()
   for face in bm.faces:
    if face in seen:continue
    todo=[face];faces=[];seen.add(face)
    while todo:
     f=todo.pop();faces.append(f)
     for e in f.edges:
      for nxt in e.link_faces:
       if nxt not in seen:seen.add(nxt);todo.append(nxt)
    verts=set(v for f in faces for v in f.verts);edges=set(e for f in faces for e in f.edges)
    closed=all(e.is_manifold for e in edges);centre=sum((v.co for v in verts),Vector())/len(verts)
    volume=sum(f.verts[0].co.dot(f.verts[i].co.cross(f.verts[i+1].co))/6 for f in faces for i in range(1,len(f.verts)-1))
    normal=sum((f.normal*f.calc_area() for f in faces),Vector());area=sum(f.calc_area() for f in faces)
    rows.append(dict(collection=c.name,object=o.name,faces=len(faces),closed=closed,volume=volume,centre=list(centre),size=[max(v.co[i] for v in verts)-min(v.co[i] for v in verts) for i in range(3)],meanNormal=list(normal/max(area,.00001))))
   bm.free();bpy.data.meshes.remove(mesh)
with open(os.path.join(R,'Logs/AlpsBuildingFaces/Components.json'),'w') as f:json.dump(rows,f,indent=2)
print('COMPONENTS',len(rows),'closed negative',sum(r['closed'] and r['volume']<-.0001 for r in rows),'open',sum(not r['closed'] for r in rows))
