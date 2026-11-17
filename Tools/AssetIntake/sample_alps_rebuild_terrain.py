import bpy,os,json,struct
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'SourceAssets/AlpsRebuild/Snow_District_Alps_Town_Terrain.blend'))
o=next(o for o in bpy.data.objects if o.type=='MESH');m=o.data;v=[o.matrix_world@p.co for p in m.vertices];b=BVHTree.FromPolygons(v,[list(p.vertices) for p in m.polygons]);h=[]
for iz in range(257):
 row=[]
 for ix in range(257):
  x=max(-499.999,min(499.999,ix/256*1000-500));y=max(-499.999,min(499.999,iz/256*1000-500));hit=b.ray_cast(Vector((x,y,1000)),Vector((0,0,-1)),2000)[0]
  assert hit is not None,(ix,iz);row.append(hit.z)
 h.append(row)
p=os.path.join(root,'SourceAssets/AlpsRebuild/TerrainSurface.json');data=json.load(open(p));data['heights']=h
with open(p,'w')as f:json.dump(data,f)
with open(os.path.join(root,'SourceAssets/AlpsRebuild/TerrainHeight_257_f32le.raw'),'wb')as f:f.write(b''.join(struct.pack('<f',a)for row in reversed(h)for a in row))
print('HEIGHT_QUERIES_ALL_66049_HITS')
