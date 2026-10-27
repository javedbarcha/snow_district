import bpy, bmesh, json, os
from mathutils import Vector
out=[]
for obj in bpy.data.objects:
 if not obj.name.startswith(('Chunky staggered tread','Faceted tyre','Chevron tyre grips')):continue
 mesh=obj.to_mesh();bm=bmesh.new();bm.from_mesh(mesh)
 unseen=set(bm.verts);components=[]
 while unseen:
  pending=[unseen.pop()];verts=set(pending)
  while pending:
   for e in pending.pop().link_edges:
    for v in e.verts:
     if v in unseen:unseen.remove(v);verts.add(v);pending.append(v)
  faces={f for v in verts for f in v.link_faces};centre=sum((v.co for v in verts),Vector())/len(verts)
  signs=[f.normal.dot(f.calc_center_median()-centre) for f in faces]
  components.append(dict(vertices=len(verts),negative=sum(s<-.00001 for s in signs),positive=sum(s>.00001 for s in signs)))
 wheel=obj.parent
 pts=[obj.matrix_world@v.co for v in bm.verts];center=wheel.matrix_world.translation
 radii=[((p.x-center.x)**2+(p.z-center.z)**2)**.5 for p in pts]
 out.append(dict(name=obj.name,determinant=obj.matrix_world.determinant(),components=len(components),negative_faces=sum(c['negative'] for c in components),positive_faces=sum(c['positive'] for c in components),radial_min=min(radii),radial_max=max(radii)))
 bm.free();obj.to_mesh_clear()
print('TREAD_CHECK '+json.dumps(out))
