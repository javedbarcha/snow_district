# Supplied low-poly snow loader

Original `snow_loader_lp.blend` is preserved here outside Unity's Assets folder.
SHA256: `365804EDC406BD3D021C8156956C984815D4567F30ABAA58D2F29ED993467327`.
The original and this copy are identical. Embedded scripts/text were not executed.

`Tools/AssetIntake/export_loader.py` runs in Blender with `--background
--factory-startup --disable-autoexec`. It evaluates meshes and three curve hoses,
excludes studio ground/default cube/cameras/lights, combines rigid modules, and
exports `LoaderBody.fbx` and `LoaderBucket.fbx` under
`Assets/Snowglow/Catalog/Sources/LowPolySnowLoader`. `PartMap.json` retains the source
part names; the original Blender hierarchy remains editable. `ExportMetrics.json`
records evaluated Blender counts before Unity's normal/UV vertex splits.

The opaque source colors use one shared point-sampled 16x1 palette texture and
URP Simple Lit material. Windows use a separate opaque unlit cyan toon material
with pale diagonal reflection streaks (updated 28 October 2026). Each module
has exactly one UV layer, preserving palette coordinates through FBX joining.
The source's toon shading is approximated; no textures or colors were discarded.

Catalog entries `imported-loader-body` and `imported-loader-bucket` remain Needs
Review. Recipe: `Assets/Snowglow/Catalog/Recipes/ImportedLoader.asset`.
Gameplay prefab: `Assets/Snowglow/Catalog/Prefabs/ImportedSnowLoader.prefab`.
**Snowglow > Asset intake > Use imported snow loader** installs it into Pinefall's
Fleet[1]. Regular district baking also uses the saved imported prefab.

The loader retains `wheeled-loader-01`, purchase price 1,400, and initially unowned
status. Saved ownership/upgrades/fuel carry over. The free tractor is unchanged.
No player save was loaded or rewritten by the authoring checks.

Wheel centres, 0.73 m radius, 2.38 m wheelbase, 1.82 m track and 2.84 m bucket width
come from the model. Existing 4,200 kg body, 550 kg bucket, 1.6 cubic metre capacity
and power ratings remain provisional. The loader retains front-wheel steering;
its visual chassis articulation is not a joint simulation. Boom and bucket have
separate authored pivots. The prepared boom extends 0.8 m forward for tyre clearance;
boom pitch +9 degrees establishes the low work pose;
the socket counter-rotates the bucket. A separate tilt pivot aligns the connection
anchor rather than rotating around the centered catalog prefab origin. Full lift
is required for the automatic dump pose to clear the ground. The default 0.55 dump
lift remains unchanged for other machines. Rams/hoses remain simplified rigid
visuals, not dynamically constrained hydraulics. The prepared bucket pin is now at
source X2.81 / Z1.03 and cutting edge at X4.01 / Z0.567. Bucket-local dimensions stay
unchanged. A 441-pose lift/tilt check found at least 0.239 m clearance beyond a
conservative steered tyre envelope. Grip blocks' inward normals were corrected in
the prepared export (66 blocks per wheel); the original .blend remains intact.

Measured geometry:

| Content | Vertices | Triangles | Renderers | Material slots |
| --- | ---: | ---: | ---: | ---: |
| Evaluated Blender vehicle | 4,740 | 7,656 | 7 combined modules | 8 |
| Complete Unity loader including inactive carried snow | 15,095 | 8,424 | 8 | 9 |
| Existing imported tractor | 13,528 | 7,274 | 7 | 30 |

The complete loader uses three distinct materials including carried snow. Counts
exclude particle geometry. Unity vertices include splits for flat normals/UVs;
Blender edit-mode vertex counts are not the final GPU vertex inventory. Material
slots are potential submissions per pass, not actual measured draw calls.

Unity 6000.3.8f1 / Android compiled, saved and reopened the scene, and passed loader
identity/price, rig compatibility, controller initialization, carry/dump shell and
edge clearance, load visual, drive/park and persistent mesh checks. Source render
and Unity colors/alignment at rest were visually inspected. All 19 snow checks and
39 catalog checks passed. Logs are in `Logs/LoaderIntake` and `Logs/AssetWorkshopChecks.txt`.
Sustained driving/snow interaction, all manual motion combinations, faithful
hydraulics, Android packaging and Galaxy A07 profiling remain untested.

See [MOBILE_ASSET_BUDGET.md](../../MOBILE_ASSET_BUDGET.md) for provisional budgets.
