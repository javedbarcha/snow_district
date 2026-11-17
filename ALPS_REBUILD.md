# The Alps supplied-asset rebuild ? 17 November 2026

This replaces only `Assets/Scenes/TheAlps.unity`. Pinefall and the four original Blender files are preserved. The scene is authored and saved in Edit mode; gameplay does not build this town.

## Inputs and preparation

`SourceAssets/AlpsRebuild/Provenance.json` records the supplied terrain, mountains, buildings and statue/tower source checksums. The aerial reference informs the connected street topology and distribution; this is not a surveyed reconstruction. Blender exports run with autoexecution disabled. Twenty-two reusable catalogue assemblies retain source palette UVs, opaque shared URP materials and separate snow objects. Entries remain NeedsReview.

The terrain retains its 1 km square footprint, 5,600 triangles and vertical boundary walls. Uniform height compression matches Pinefall's measured maximum edge elevation (94.95 m), placing the existing flat centre at the snow datum, 5.13 m. A hidden 257-resolution TerrainData serves the existing camera queries; the supplied mesh provides physical collision. No downward edge rolloff is added.

Sixteen surrounding mountains use all eight supplied variants twice. Original LOD1 sources are696 triangles each; saved scene copies remove the bottom/wall skirt and contain504 triangles each. The initial420m ring is superseded by the outer-belt pass: all mountain bounds now leave35m to the nearest terrain edge, behind the inset hill belt. The full original sloping surface is lowered until the open rim is buried at least .8 m; bottom vertices are never stretched into a stem. The source models stay unchanged. Seventy homes use ten ordinary variants seven times each. Each has one enclosing box collider, southwest-facing fronts and at least 5.8 m projected-axis lot separation. The additional building-file clock tower is prepared but not placed; the separate arched landmark tower is used instead.

Twelve connected road routes combine straight streets and rounded corners. The statue stands in Tell square; the 1.6x tower straddles the northbound main street. Garage, fuel station, disposal yard, lamps and five captioned boards reuse Pinefall assets. Facilities retain existing gameplay service coordinates. Vehicles park side by side in the depot forecourt.

## Authoring and recovery

`AlpsRebuildAssets` prepares the catalogue. `AlpsRebuildScene` builds this scene, its independent snow seed, road meshes and fitted mountain derivatives. It preserves saved and unsaved scene copies under `Logs/AlpsRebuild`. A failed build restores the previous saved Alps scene and retains diagnostic assets. A successful saved scene is also copied to `Built-before-camera.unity` before captures. Rebuild refuses an already rebuilt scene; use review or a targeted refinement rather than a district rebake.

After native compilation succeeds, `Logs/AlpsRebuild.request` accepts `inspect`, `assets`, `build`, `audit`, `refine`, `polish`, `finish` or `review`. Review uses the actual gameplay CameraRig in isometric mode at every home and facility. Evidence is under `Logs/AlpsRebuild/Isometric`; Overview uses a wider audit lens.

Deformable snow remains central 256 x 256 m. Outer roads are physical scenery with no expanded snow simulation. Existing physics, contracts, economy and vehicle models are retained. Android URP / Samsung Galaxy A07 remains the target; asset preparation is not device performance acceptance.

## Validation status

Native source intake, the initial saved/reopened layout checks and 19 snow-ledger assertions passed. The first camera pass found a destroyed cached terrain-collider reference after scene reopening; it restored the previous scene. Capture now reacquires that reference. Final camera review, static vehicle-route clearance and isolated Play regression are pending until their reports explicitly pass. Packaging, phone frame costs, manual full jobs and exhaustive hill driving remain untested.

A first unused derived-asset attempt was moved from Assets into Logs/AlpsRebuild/UnusedDerivedDraft after checking its 81 GUIDs had no references outside that draft. Failure evidence is retained without duplicating unused snow meshes in the player project. The Editor bridge now publishes status atomically and retries transient Windows reader contention; its client permits atomic replacement while reading.

Mountain-only correction: after successful native compilation, the `mountains` request performs the guarded source-derived correction once. It refuses a second outward move on already-corrected instances. Backups,1320-file preservation report and32 inspected native mountain views: Logs/AlpsMountainRevision. No Play/device rerun for this visual-only revision.

## Inset hills and outer mountains

The `outerbelt` action is a targeted one-time refinement after source preparation and initial scene authoring. Complete terrain triangles touching the inner245m square are frozen; terrain vertices through270m are retained. Existing270-500m relief is compressed into270-350m, leaving an elevated outer shelf for the mountains. The physical mesh and hidden257 camera height query are saved as new assets. Source terrain/footprint/topology/vertical cut walls and the town remain. Sixteen natural mountain meshes retain their geometry and orientation, with35m nearest-edge margins and .8m minimum rim burial. This supersedes the circular420m placement. Native save/reopen,441 inner samples,2195 static access samples and17 isolated Play assertions passed.32 local views and overview were inspected;2368 existing assets/profile files were preserved. Evidence/backups: Logs/AlpsOuterMountainBelt. Outer-hill handling and device acceptance remain untested.
