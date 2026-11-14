# Distinct low-poly Shop parts

The Shop now has twelve generated visual models, plus its factory plow
and bucket previews. The factory plow and bucket shell retain their source meshes;
the Shop bucket rear mounts are refined. Geometry is authored in Edit mode by
`Assets/Snowglow/Editor/MachineryPartModels.cs`, retained as reproducible source.
No Blender dependency, runtime mesh construction or extra gameplay colliders.

| Category | Tractor | Loader |
| --- | --- | --- |
| Standard tyres | Narrow chevron tread, yellow dished rim, six bolts per side | Wide industrial blocks, deeper dish, eight bolts per side |
| Winter tyres | Three staggered rows of small winter blocks, silver rim | Wider three-row winter tread, silver industrial rim |
| Chained tyres | Shoulder-to-shoulder linked cross chains and two side chains | Wider tyre, denser cross chains and two side chains |
| Standard engine | Green inline-four, fuel lines and single air cleaner | Yellow V6, split cylinder banks and central intake |
| Turbo engine | Longer blue inline-six, turbo and intercooler | Red V8, twin turbos, dual air cleaners and intercooler |
| New attachment | Curved reinforced plow, angled ends, wear strip and hazard markings | Open reinforced bucket, five cutting teeth and yellow cheek plates |

All tyres have rubber sidewalls, raised beads, recessed rims and projecting hubs
on both faces. Chain links are closed three-dimensional loops. Engines include
sumps, radiator fins, fans, manifolds, starters, alternators, mount feet and oil
filters. Both new attachments have rear braces and mounting details for orbit
inspection. Flat normals, chamfered forms and shared solid-colour URP materials
match the existing low-poly machinery theme.

Generated meshes, materials and reusable prefabs are stored under
`Assets/Snowglow/Catalog/Sources/MachineryParts`. Twelve stable `machinery-shop-*`
entries in the Asset Workshop catalogue retain generator provenance, bounds and
triangle counts with **Needs Review** status. They are Shop presentation assets;
pair-specific fitting/clearance review is required before replacing machinery
meshes in gameplay. Performance values, ownership, prices, upgrades and saves
are unchanged by this visual revision.

`WorkshopPreviewAuthoring.BuildParts` instantiates these saved models into the
saved showroom hierarchy in Edit mode. Only the selected model is active at
runtime. Factory blade/bucket copies still use the previous extraction code.
The old primitive engine generator has been removed. The upgraded tractor plow
also includes the original lift hydraulics as a separate saved mesh renderer. Full menu authoring reuses
the same model preparation, with stable asset paths/GUIDs on subsequent runs.

After native compile/domain reload succeeds, `Logs/MachineryParts.request`
backs up the scene and unsaved edits, checks factory model signatures, prepares
the assets, validates them, saves/reopens Pinefall and runs the existing parts/UI
and gameplay regressions. Geometry counts and front/rear renders are written to
`Logs/MachineryPartModels`; `AllVariants.png` has tractor parts across the top and
loader parts below, ordered standard tyres / winter tyres / chained tyres /
standard engine / turbo engine / new attachment. These captures are for visual
review; they do not constitute Android performance acceptance.

## Verified geometry and acceptance

| Model | Vertices | Triangles |
| --- | ---: | ---: |
| tractor-tyres-standard | 6,624 | 2,208 |
| tractor-engine-standard | 4,944 | 1,648 |
| tractor-tyres-winter | 7,992 | 2,664 |
| tractor-tyres-chained | 14,688 | 4,896 |
| tractor-engine-turbo | 6,528 | 2,176 |
| tractor-blade-heavy | 3,072 | 1,292 |
| loader-tyres-standard | 6,768 | 2,256 |
| loader-engine-standard | 6,216 | 2,072 |
| loader-tyres-winter | 7,848 | 2,616 |
| loader-tyres-chained | 17,136 | 5,712 |
| loader-engine-turbo | 9,624 | 3,208 |
| loader-blade-heavy | 2,388 | 796 |

These are actual mesh vertex counts (including vertices split for flat normals),
not unique position counts. Generated bodies use 16-bit indices and shared opaque URP materials.
The upgraded plow has a second renderer for the original hydraulic assembly. Only the selected Shop part renders; the two factory attachments
retain their original geometry. These budgets do not establish phone performance.

15 November 2026: Android-defined compilation and native forced compile/reload
passed. Unity saved/reopened Pinefall; 150 model checks, 183 parts checks, 661 UI,
187 flow, 34 Career snow, 19 snow-ledger, 1,108 isolated recovery and 1,134 loader
cycle assertions passed. Front/rear model captures and Shop views were inspected.
All 14 protected save/seed file hashes and 1,234 Rigidbody/collider/Terrain/prefab
blocks matched. Factory attachment geometry/transform signatures matched. No
Play entry, player-save writes, Android package or device acceptance performed.
The initial blank standalone sheet is retained; corrected captures now include
an empty-render guard. Final evidence: Logs/MachineryPartModels/Validation.txt.

## Mount and connection revision - 15 November 2026

Air-cleaner housings on all four engines now have supporting plates/brackets
and continuous intake pipes that enter the engine body. The standard loader
bucket Shop copy replaces its two cuboid rear mounts with chamfered mounting
plates and visible pins, retaining the original bucket shell and placement.
The upgraded tractor blade reuses the default tractor lift-frame/hydraulic
geometry, exported from the preserved Blender source by
`Tools/AssetIntake/export_shop_plow_hydraulics.py` with automatic script execution
disabled. The original file is never saved or overwritten. Source object names,
centres and SHA256 are retained in `TractorDefaultHydraulics.json` alongside the
new FBX. Existing tractor palette materials are reused. Its carrier connects to
the upgraded blade's rear crossbar; the generic mounting ears were removed.

These are Shop revisions. The playable loader's mounts, vehicle colliders,
attachment poses, hydraulics simulation, part stats and saves remain unchanged.
The factory-preservation check now excludes the deliberately replaced bucket
lug mesh and still checks the original bucket shell and tractor factory blade.
The upgraded plow has a separate renderer for the reused hydraulic assembly;
reported geometry totals include that assembly. Evidence: Logs/PartMountRevision.

This revision passed native forced compile/reload and save/reopen: 152 model,
183 parts, 664 UI, 187 flow, 34 Career snow, 19 snow-ledger, 1,108 recovery and
1,134 loader-cycle checks. Rear renders were visually inspected. All 15 protected
save/seed/source hashes and 1,234 protected world records matched. Reused hydraulic
geometry is 716 triangles; the combined heavy plow is 1,292 triangles across two
renderers with eleven slots. The revised factory bucket Shop lugs are 320 triangles.
The original Blender file remains byte-identical. No Play or device test performed.
