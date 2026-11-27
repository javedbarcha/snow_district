import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy


def parse_args():
    argv = sys.argv
    script_args = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser(
        description="Create editable Blender sidecars for the project's Unity FBX models."
    )
    parser.add_argument("--input-root", type=Path)
    parser.add_argument("--output-root", type=Path)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args(script_args)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def scene_metrics(scene):
    objects = list(scene.objects)
    meshes = [obj for obj in objects if obj.type == "MESH" and obj.data]
    return {
        "objects": len(objects),
        "meshes": len(meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "materials": sorted({
            material.name
            for obj in meshes
            for material in obj.data.materials
            if material
        }),
    }


def manifest_path(path, project_root):
    try:
        return path.relative_to(project_root).as_posix()
    except ValueError:
        return str(path)


def main():
    args = parse_args()
    project_root = Path(__file__).resolve().parents[2]
    input_root = (args.input_root or project_root / "Assets" / "Snowglow").resolve()
    output_root = (args.output_root or project_root / "SourceAssets" / "EditableModels").resolve()
    if not input_root.is_dir():
        raise RuntimeError("FBX input root does not exist: " + str(input_root))
    if args.limit is not None and args.limit < 1:
        raise RuntimeError("--limit must be a positive integer")

    sources = sorted(input_root.rglob("*.fbx"), key=lambda path: str(path).lower())
    if args.limit:
        sources = sources[:args.limit]
    if not sources:
        raise RuntimeError("No FBX files found under " + str(input_root))

    output_root.mkdir(parents=True, exist_ok=True)
    manifest_path = output_root / "conversion_manifest.json"
    previous = {}
    if manifest_path.exists():
        try:
            previous = {
                entry["source"]: entry
                for entry in json.loads(manifest_path.read_text(encoding="utf-8")).get("files", [])
            }
        except (ValueError, KeyError, TypeError):
            previous = {}

    results = []
    failures = []
    for index, source in enumerate(sources, 1):
        relative = source.relative_to(input_root)
        destination = (output_root / relative).with_suffix(".blend")
        key = relative.as_posix()
        if destination.exists() and not args.overwrite:
            prior = previous.get(key)
            results.append({
                "source": key,
                "source_sha256": sha256(source),
                "blend": destination.relative_to(output_root).as_posix(),
                "status": "preserved-existing",
                "previous_record": prior,
            })
            print("[{}/{}] Preserved existing Blender file: {}".format(index, len(sources), destination))
            continue

        try:
            destination.parent.mkdir(parents=True, exist_ok=True)
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.fbx(filepath=str(source), use_image_search=True)
            scene = bpy.context.scene
            imported = scene_metrics(scene)
            if imported["meshes"] == 0 or imported["polygons"] == 0:
                raise RuntimeError("FBX import produced no polygon mesh")

            bpy.ops.file.pack_all()
            bpy.ops.wm.save_as_mainfile(filepath=str(destination), check_existing=False)
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.wm.open_mainfile(filepath=str(destination))
            reopened = scene_metrics(bpy.context.scene)
            if imported != reopened:
                raise RuntimeError(
                    "saved scene metrics differ: imported={} saved={}".format(imported, reopened)
                )

            result = {
                "source": key,
                "source_sha256": sha256(source),
                "blend": destination.relative_to(output_root).as_posix(),
                "blend_sha256": sha256(destination),
                "status": "converted-and-verified",
                "metrics": imported,
                "packed_images": sum(1 for image in bpy.data.images if image.packed_file),
            }
            results.append(result)
            print("[{}/{}] Converted and verified: {}".format(index, len(sources), destination))
        except Exception as exception:
            failures.append({"source": key, "error": repr(exception)})
            print("[{}/{}] FAILED {}: {}".format(index, len(sources), source, exception))

    manifest = {
        "format": 1,
        "blender_version": bpy.app.version_string,
        "input_root": manifest_path(input_root, project_root),
        "output_root": manifest_path(output_root, project_root),
        "source_count_in_run": len(sources),
        "converted_count": sum(1 for item in results if item["status"] == "converted-and-verified"),
        "preserved_existing_count": sum(1 for item in results if item["status"] == "preserved-existing"),
        "failed_count": len(failures),
        "failures": failures,
        "files": results,
    }
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print("Manifest: {}".format(manifest_path))
    print("Result: {} converted, {} preserved, {} failed.".format(
        manifest["converted_count"],
        manifest["preserved_existing_count"],
        manifest["failed_count"],
    ))
    if failures:
        raise RuntimeError("FBX conversion finished with failures; see the manifest.")


if __name__ == "__main__":
    main()
