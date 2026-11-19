"""Compare evaluated source face orientation against the historical export bake."""
import bpy, bmesh, json, os
from mathutils import Matrix
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
rows = []
for filename in ['Snow_District_Alps_Buildings.blend', 'Snow_District_Statue_and_Tower.blend']:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceAssets/AlpsRebuild', filename))
    deps = bpy.context.evaluated_depsgraph_get()
    for collection in sorted(bpy.data.collections, key=lambda c: c.name):
        if not collection.name[:2].isdigit():
            continue
        for src in collection.objects:
            if src.type != 'MESH' or src.name.startswith('COLLISION_PROXY'):
                continue
            mesh = bpy.data.meshes.new_from_object(src.evaluated_get(deps), depsgraph=deps)
            normal_matrix = src.matrix_world.to_3x3().inverted().transposed()
            expected = [(normal_matrix @ p.normal).normalized() for p in mesh.polygons]
            mesh.transform(src.matrix_world)
            bm = bmesh.new(); bm.from_mesh(mesh)
            manifold = bool(bm.faces) and all(e.is_manifold for e in bm.edges)
            if manifold:
                bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.to_mesh(mesh)
            bm.free(); mesh.update()
            inverted = [i for i, p in enumerate(mesh.polygons) if expected[i].dot(p.normal) < -.5]
            rows.append(dict(file=filename, collection=collection.name, name=src.name,
                             determinant=src.matrix_world.determinant(), manifold=manifold,
                             faces=len(mesh.polygons), reversedFaces=len(inverted), indices=inverted))
            bpy.data.meshes.remove(mesh)
os.makedirs(os.path.join(ROOT, 'Logs/AlpsBuildingFaces'), exist_ok=True)
with open(os.path.join(ROOT, 'Logs/AlpsBuildingFaces/SourceAudit.json'), 'w') as f:
    json.dump(rows, f, indent=2)
print('FACE_AUDIT', len(rows), 'objects;', sum(r['reversedFaces'] for r in rows), 'reversed faces')
for r in rows:
    if r['reversedFaces']:
        print(r['collection'], r['name'], r['determinant'], r['reversedFaces'], '/', r['faces'])
