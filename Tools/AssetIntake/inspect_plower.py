"""Read-only inspection of the supplied blend; run with --disable-autoexec."""
import bpy, json, os
from mathutils import Vector

out = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), 'Logs',
    'LoaderIntake' if os.path.basename(bpy.data.filepath)=='snow_loader_lp.blend' else 'PlowerIntake')
os.makedirs(out, exist_ok=True)
objects = []
for obj in bpy.data.objects:
    points = [obj.matrix_world @ Vector(c) for c in obj.bound_box] if obj.type == 'MESH' else []
    objects.append(dict(name=obj.name, type=obj.type, parent=obj.parent.name if obj.parent else None,
        location=list(obj.location), rotation=list(obj.rotation_euler), scale=list(obj.scale),
        bounds_min=[min(p[i] for p in points) for i in range(3)] if points else None,
        bounds_max=[max(p[i] for p in points) for i in range(3)] if points else None,
        vertices=len(obj.data.vertices) if obj.type == 'MESH' else 0,
        triangles=sum(len(p.vertices)-2 for p in obj.data.polygons) if obj.type == 'MESH' else 0,
        modifiers=[dict(name=m.name,type=m.type) for m in obj.modifiers],
        materials=[m.name if m else None for m in obj.data.materials] if obj.type == 'MESH' else []))
report=dict(blender=bpy.app.version_string,units=bpy.context.scene.unit_settings.system,
    unit_scale=bpy.context.scene.unit_settings.scale_length,objects=objects,
    materials=[dict(name=m.name,diffuse=list(m.diffuse_color)) for m in bpy.data.materials],
    embedded_text_names=[t.name for t in bpy.data.texts])
with open(os.path.join(out,'inspection.json'),'w') as f: json.dump(report,f,indent=2)
print('SNOWGLOW_INSPECTION: '+str(len(objects))+' objects; report: '+out)
