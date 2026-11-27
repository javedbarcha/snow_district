# Editable Blender copies of project FBX models

This folder mirrors the model directories under `Assets/Snowglow/`. Each `.blend`
is an editable Blender sidecar imported from its matching Unity `.fbx`. The Unity
FBX files remain the runtime/import sources and are not replaced or modified.
Keeping the Blender files here, outside `Assets`, prevents Unity from importing
each editable copy as an additional model.

The initial conversion contains 169 models (about 17.7 MiB) and was made with
Blender 5.2.0 LTS.

Open the `.blend` files directly in Blender to edit object geometry, transforms,
mesh names, UVs and imported material assignments. FBX-to-Blender conversion
preserves the imported object hierarchy and mesh data, but cannot recreate the
original Blender collections, modifiers, procedural setup, rigs, or authoring
history that were not present in the FBX. Review materials and texture paths
before relying on exact visual parity; packed image resources travel with the
converted file when the FBX contains/imports them.

The matching FBX remains the file Unity imports. After editing a Blender copy,
export it to the corresponding FBX location using that asset's established
pipeline and verify its dimensions, material slots, transforms and scene
references before replacing any FBX. Do not copy a `.blend` over an `.fbx`.

The conversion inventory and source/output checksums are recorded in
`conversion_manifest.json`. Run the repeatable converter from the project root:

```powershell
& 'C:\path\to\blender.exe' --background --factory-startup --disable-autoexec `
  --python Tools\AssetIntake\convert_project_fbx_to_blend.py
```

By default, existing `.blend` files are preserved so user edits are never
overwritten. Use `--overwrite` only when intentionally regenerating the Blender
copies from the current FBX files. `--limit N` converts only the first N FBXs for
a small test run. Blender's FBX importer is used; no source FBX text or embedded
scripts are executed.
