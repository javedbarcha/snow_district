"""Read source data only; run Blender with --disable-autoexec."""
import bpy, json, os
from mathutils import Vector
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'SourceAssets/SevenWinterHomes/Seven_Winter_Homes.blend'))
deps=bpy.context.evaluated_depsgraph_get()
def record(o):
    r={'name':o.name,'type':o.type,'collections':[c.name for c in o.users_collection], 'parent':o.parent.name if o.parent else None}
    if o.type in {'MESH','CURVE','SURFACE','FONT'}:
        e=o.evaluated_get(deps);m=e.to_mesh();m.calc_loop_triangles()
        pts=[o.matrix_world@Vector(p) for p in o.bound_box]
        r.update(vertices=len(m.vertices),triangles=len(m.loop_triangles),bounds=[[min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]],materials=[s.material.name if s.material else None for s in o.material_slots],modifiers=[m.type for m in o.modifiers]);e.to_mesh_clear()
    return r
data={'units':bpy.context.scene.unit_settings.scale_length,'collections':[{'name':c.name,'children':[x.name for x in c.children],'objects':[o.name for o in c.objects]} for c in bpy.data.collections], 'objects':[record(o) for o in bpy.data.objects], 'texts':[t.name for t in bpy.data.texts], 'materials':[]}
for m in bpy.data.materials:
    nodes=[]
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type=='BSDF_PRINCIPLED':nodes.append({'base':list(n.inputs['Base Color'].default_value),'emission':list(n.inputs['Emission Color'].default_value),'strength':n.inputs['Emission Strength'].default_value,'roughness':n.inputs['Roughness'].default_value,'alpha':n.inputs['Alpha'].default_value})
    data['materials'].append({'name':m.name,'diffuse':list(m.diffuse_color),'nodes':nodes})
out=os.path.join(root,'SourceAssets/SevenWinterHomes/Inspection.json')
with open(out,'w') as f:json.dump(data,f,indent=2)
print('INSPECTED',len(data['objects']),'objects;',len(data['collections']),'collections;',out)
