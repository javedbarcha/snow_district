"""Export only the revised test terrain. Run Blender with --factory-startup --disable-autoexec."""
import bpy, json, hashlib
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
source = root / 'SourceAssets/AlpsTerrainTestRevision/Terrain.blend'
dest = root / 'Assets/Snowglow/Levels/AlpsTerrainTest/Revision2'
dest.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
objects = [o for o in bpy.data.objects if o.type == 'MESH']
assert len(objects) == 1, 'Expected one terrain mesh'
src = objects[0]
deps = bpy.context.evaluated_depsgraph_get()
mesh = bpy.data.meshes.new_from_object(src.evaluated_get(deps), depsgraph=deps)
mesh.transform(src.matrix_world)
mesh.calc_loop_triangles()
points = [v.co for v in mesh.vertices]
bounds = [[min(v[i] for v in points), max(v[i] for v in points)] for i in range(3)]
assert all(abs(bounds[i][1]-bounds[i][0]-1000) < .01 for i in (0, 1))
image = next(i for i in bpy.data.images if i.size[0] > 0)
image.filepath_raw = str(dest / 'Palette.png')
image.file_format = 'PNG'
image.save()
mesh.materials.clear()
mesh.materials.append(bpy.data.materials.new('TestTerrainPalette'))
for polygon in mesh.polygons:
    polygon.material_index = 0
    polygon.use_smooth = False
obj = bpy.data.objects.new('RevisedTerrain', mesh)
bpy.context.scene.collection.objects.link(obj)
front = bpy.data.objects.new('Front', None)
bpy.context.scene.collection.objects.link(front)
front.location = (0, -1, 0)
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
front.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.fbx(filepath=str(dest/'Terrain.fbx'), use_selection=True,
    object_types={'MESH','EMPTY'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    bake_space_transform=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
manifest = dict(source=str(source.relative_to(root)), sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    source_object=src.name, blender=bpy.app.version_string, source_bounds_xyz=bounds,
    vertices=len(mesh.vertices), triangles=len(mesh.loop_triangles), scale='1:1 metres',
    instructions='Embedded text and drivers not executed; only evaluated terrain mesh and packed palette exported.')
(root/'SourceAssets/AlpsTerrainTestRevision/Provenance.json').write_text(json.dumps(manifest, indent=2))
print('TEST_TERRAIN_EXPORTED', json.dumps(manifest))
