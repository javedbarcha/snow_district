import bpy,json,os,math
from mathutils import Vector
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
reports=[]
for name in ['Snow_District_Alps_Town_Terrain.blend','Snow_District_Alps_Mountains.blend','Snow_District_Alps_Buildings.blend','Snow_District_Statue_and_Tower.blend']:
 bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'SourceAssets/AlpsRebuild',name))
 deps=bpy.context.evaluated_depsgraph_get();objects=[]
 for o in bpy.data.objects:
  row={'name':o.name,'type':o.type,'collections':[c.name for c in o.users_collection],'parent':o.parent.name if o.parent else None,'location':list(o.location),'scale':list(o.scale)}
  if o.type=='MESH':
   m=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);m.calc_loop_triangles();pts=[o.matrix_world@v.co for v in m.vertices]
   row.update(vertices=len(m.vertices),triangles=len(m.loop_triangles),boundsMin=[min(p[i] for p in pts) for i in range(3)],boundsMax=[max(p[i] for p in pts) for i in range(3)],materials=[s.material.name if s.material else None for s in o.material_slots],modifiers=[x.type for x in o.modifiers]);bpy.data.meshes.remove(m)
  objects.append(row)
 reports.append({'file':name,'objects':objects,'collections':[{'name':c.name,'objects':len(c.objects),'allObjects':len(c.all_objects),'children':[v.name for v in c.children]} for c in bpy.data.collections],'materials':[{'name':m.name,'color':list(m.diffuse_color)} for m in bpy.data.materials],'texts':[t.name for t in bpy.data.texts],'images':[{'name':i.name,'size':list(i.size)} for i in bpy.data.images]})
with open(os.path.join(root,'Reports/AlpsRebuild/SourceInspection.json'),'w') as f:json.dump(reports,f,indent=2)
print('ALPS_REBUILD_INSPECTION_COMPLETE')
