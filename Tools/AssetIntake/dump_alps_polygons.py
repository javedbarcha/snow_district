import bpy,os,json
R=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
rows=[]
for name in ['Snow_District_Alps_Buildings.blend','Snow_District_Statue_and_Tower.blend']:
 bpy.ops.wm.open_mainfile(filepath=os.path.join(R,'SourceAssets/AlpsRebuild',name));deps=bpy.context.evaluated_depsgraph_get()
 for c in bpy.data.collections:
  if not c.name[:2].isdigit():continue
  root=next(o for o in c.objects if o.type=='EMPTY' and o.name.endswith('ROOT')).matrix_world.translation.copy()
  for o in c.objects:
   if o.type!='MESH' or o.name.startswith('COLLISION_PROXY'):continue
   m=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);m.transform(o.matrix_world)
   for p in m.polygons:
    rows.append(dict(collection=c.name,object=o.name,index=p.index,vertices=[list(m.vertices[i].co-root) for i in p.vertices],normal=list(p.normal),area=p.area))
   bpy.data.meshes.remove(m)
with open(os.path.join(R,'Logs/AlpsBuildingFaces/Polygons.json'),'w') as f:json.dump(rows,f)
