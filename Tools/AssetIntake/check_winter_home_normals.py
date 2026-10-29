"""Check exported closed shell winding; open facade panels are intentionally excluded."""
import bpy,bmesh,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
folder=root/'Assets/Snowglow/Catalog/Sources/SevenWinterHomes';results=[]
for path in sorted(folder.glob('*.fbx')):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path))
 closed=0;negative=[]
 for obj in list(bpy.context.scene.objects):
  if obj.type!='MESH':continue
  bm=bmesh.new();bm.from_mesh(obj.data);bm.transform(obj.matrix_world);bm.normal_update();remaining=set(bm.faces)
  while remaining:
   todo=[remaining.pop()];component=[]
   while todo:
    f=todo.pop();component.append(f)
    for edge in f.edges:
     for neighbor in edge.link_faces:
      if neighbor in remaining:remaining.remove(neighbor);todo.append(neighbor)
   if all(e.is_manifold for f in component for e in f.edges):
    closed+=1
    volume=sum(f.calc_area()*f.normal.dot(f.calc_center_median())/3 for f in component)
    if volume< -1e-5:negative.append(volume)
  bm.free()
 assert not negative,(path.name,negative)
 results.append({'model':path.stem,'closedShellsChecked':closed,'inwardClosedShells':len(negative)})
 print('PASS_NORMALS',path.stem,closed,flush=True)
(root/'Logs/WinterHomes').mkdir(parents=True,exist_ok=True)
(root/'Logs/WinterHomes/NormalsRoundTrip.json').write_text(json.dumps(results,indent=2))
