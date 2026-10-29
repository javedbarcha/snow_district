"""Inspect supplied data with Blender autoexecution disabled; never run embedded text."""
import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'SourceAssets/FiveAlpineMountains/Five_Alpine_Mountains.blend'))
deps=bpy.context.evaluated_depsgraph_get()
rows=[]
for o in list(bpy.data.objects):
    row={'name':o.name,'type':o.type,'collections':[c.name for c in o.users_collection]}
    if o.type in {'MESH','CURVE','FONT'}:
        evaluated=o.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        points=[o.matrix_world@v.co for v in mesh.vertices]
        row.update(vertices=len(mesh.vertices),triangles=len(mesh.loop_triangles),materials=[m.name if m else None for m in mesh.materials],bounds=[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]])
        evaluated.to_mesh_clear()
    rows.append(row)
report={'units':bpy.context.scene.unit_settings.scale_length,'collections':[{'name':c.name,'objects':[o.name for o in c.all_objects]} for c in bpy.data.collections],'objects':rows,'materials':[{'name':m.name,'color':list(m.diffuse_color),'nodes':[n.type for n in m.node_tree.nodes] if m.use_nodes else []} for m in bpy.data.materials],'embeddedTexts':[t.name for t in bpy.data.texts]}
(root/'SourceAssets/FiveAlpineMountains/Inspection.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report),flush=True)
