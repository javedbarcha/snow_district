"""Extract only the existing tractor lift assembly; never run embedded texts or save source."""
import bpy, json, math, os, hashlib
from mathutils import Matrix, Vector
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
out=os.path.join(root,'Assets','Snowglow','Catalog','Sources','MachineryParts')
os.makedirs(out,exist_ok=True)
prefixes=('Blade carrier pivot','Blade rear carrier','Box section lift arm','Carrier diagonal','Lift ram','Lower chassis pivot','Upper cylinder mount')
objects=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith(prefixes)]
assert len(objects)>15
deps=bpy.context.evaluated_depsgraph_get();copies=[];report=[]
for src in objects:
    mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps)
    mesh.transform(src.matrix_world)
    obj=bpy.data.objects.new(src.name+'__shop',mesh);bpy.context.scene.collection.objects.link(obj);copies.append(obj)
    points=[src.matrix_world @ Vector(p) for p in src.bound_box];center=sum(points,Vector())/8
    report.append({'name':src.name,'center':{'x':-center.y,'y':center.z,'z':center.x}})
bpy.ops.object.select_all(action='DESELECT')
for obj in copies:obj.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();obj=copies[0];obj.name='Original tractor lift hydraulics'
obj.data.transform(Matrix.Rotation(-math.pi/2,4,'Z'))
bpy.ops.export_scene.fbx(filepath=os.path.join(out,'TractorDefaultHydraulics.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
source=os.path.join(root,'SourceAssets','LowPolySnowPlower','low_poly_snow_plower.blend')
with open(os.path.join(out,'TractorDefaultHydraulics.json'),'w') as f:json.dump({'source':'SourceAssets/LowPolySnowPlower/low_poly_snow_plower.blend','sha256':hashlib.sha256(open(source,'rb').read()).hexdigest(),'objects':report},f,indent=2)
print('SHOP_HYDRAULICS_EXPORTED',json.dumps(report))
