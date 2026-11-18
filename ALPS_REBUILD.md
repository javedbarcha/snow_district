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

## Rolling outer hills supersede the shelf

The `rollinghills` pass replaces the350-500m flat shelf with broad rolling relief from270m outward. Baseline gain eases in to .25m/m, then tapers beyond the mountains outer extent465m to .035m/m at500m. Smooth angular/2D bumps avoid a horizontal cut while slowing wave progression near the horizon. The protected town triangles, original5600-triangle terrain topology/UVs/footprint/vertical edges and all16 mountain XZ/mesh/rotation/scales remain. Mountain roots are reseated;35m margins remain. Physical terrain and hidden257 height query are saved together as new assets. This supersedes the shelf described in the previous section. Do not reapply `outerbelt` afterward. Native compile/save/reopen,441 inner samples,2195 static access and17 isolated Play assertions passed.32 mountain views and overview inspected;2376 existing files preserved. Backups/evidence: Logs/AlpsRollingHills. Actual outer-hill driving and Android acceptance remain pending.


## Alps mountains moved another60m with approved outer extension - 17 November 2026

User approved extending only the outer terrain border to support another60m outward mountain move. `MoveMountainsFurther` is the targeted `furthermountains` action in Logs/AlpsRebuild.request; queue only after native compilation exits0. Requires the RollingOuterHills input and refuses repetition. Saved/unsaved scene backups and evidence: Logs/AlpsFurtherMountains. Source assets and Pinefall remain protected; no district rebake.

All16 mountains move60m radially, retaining yaw/scale/identity and504-triangle natural meshes. Whole models are reseated with natural rims >=.8m below terrain, without stretching skirts. Only the outer465-500m terrain band expands horizontally to465-560m; existing vertex heights/UVs,5600 triangles and vertical boundary cuts remain. The terrain is now1120m square, superseding the former1km footprint with explicit approval. Town and inner hills remain in place. A new hidden257 camera query follows the new physical mesh; existing terrain assets remain unchanged. Native validation accepts1120m only for the specifically authored FurtherMountains query path.

Native forced compilation/reload, save/reopen,441 unchanged inner ground samples, scene/19 snow checks and2195 clear access samples passed.32 mountain views and overview reviewed;75 gameplay isometric views regenerated. All2381 protected source/prefab/previous terrain/Pinefall/save files unchanged. First pass restored the scene when a second legacy1000m validation guard rejected the approved extension; both validators now recognize only the approved Alps derivative. Failed-attempt assets with zero outside references archived under Logs. No Play rerun or Android/device acceptance for this visual placement revision. Alps open, Play stopped.

## Alps kilometre footprint restored with8.5m horizon - 17 November 2026

User supersedes the approved1120m extension: keep1000x1000m terrain and only7-10m beyond mountain outer edges. Targeted `FitMountainsToKilometre` (`kilometrehorizon` in Logs/AlpsRebuild.request, after native compile exit0) restores the preserved RollingOuterHills physical mesh and matching hidden257 query, without modifying those assets. Sixteen mountain roots retain bearings/yaw/scale/identity and504-triangle meshes, and move to full rotated world bounds at square boundary +/-491.5m. Each nearest outer margin is8.5m; natural rims are reseated >=.8m below terrain. No mountain vertices stretched. Central441 terrain samples, original5600 triangles and vertical boundary cuts remain. Sources/Pinefall/profile/snow protected. Saved/unsaved backups and evidence: Logs/AlpsMountainHorizon. Refuses repeated application. Do not rerun the older furthermountains/outerbelt/rollinghills passes on this saved layout; those are historical one-time migrations, not live layout settings.

Native forced compile/reload, save/reopen,441 unchanged inner ground samples, scene/19 snow and2195 clear access samples passed. All16 nearest boundary margins measured8.5m (within .00004m).32 mountain views and overview inspected;75 gameplay isometric captures regenerated. All2386 protected existing source/prefab/terrain/Pinefall/profile files unchanged. No Play rerun or Android/device acceptance. Alps open, Play stopped.

## Alps hills start just beyond town - 17 November2026

User marks the town boundary and requests hills to rise outward from there. `BringHillsToTownEdge` is the targeted `townedgehills` action in Logs/AlpsRebuild.request, only after native compile exits0. It requires the preserved RollingOuterHills mesh and refuses repeat application. Complete faces touching the +/-225m town envelope are pinned, protecting outer road widths and homes. In225-400m, sample the prior relief at r+45*(1-smoothstep(225,400,r)), raising only where the sampled ground is higher. The prior270m onset shifts inward with a gradual blend back into the original400m outer relief. No XZ, UV, topology, vertical-wall or1km footprint change. Independent physical mesh and hidden257 query save together. Mountain XYZ/yaw/scale/meshes remain, with8.5m border margins and buried natural rims checked. Backups/evidence: Logs/AlpsTownEdgeHills. No full district bake or source/Pinefall/profile/snow edits. Earlier one-time Alps terrain migration commands must not run on the resulting layout.

Native forced compile/reload, save/reopen,441 unchanged town ground samples, scene/19 snow and2195 clear access samples passed. All2386 protected existing files unchanged. Every saved scene Transform record is unchanged.32 mountain views and overview reviewed;75 gameplay isometric captures regenerated. Initial visual review caught a thin crack caused by separately duplicated flat-shaded vertex copies receiving different pin masks; pinning now propagates to all equal-position copies. Final geometry checks confirm zero new duplicate-position separations;3738 vertices actually raised, original9807 vertices/5600 triangles retained. Failed visual attempt and unused assets with zero outside references archived under Logs. No Play rerun or Android/device/outer-hill driving acceptance. Alps open, Play stopped.

## Twelve broader randomized Alps mountains - 17 November2026

User requests12 mountains, no matching opposite pairs, larger widths toward town and visibly seated bases. Targeted `ReviseTwelveMountains` action `twelvemountains` in Logs/AlpsRebuild.request, only after native compilation exits0, requires16 prior mountains and the saved TownEdgeHills terrain. Backup saved/unsaved scene; restore on failure. All8 source variants appear in a seeded12-slot shuffle with no equal adjacent or diametrically opposite entries; bearings jitter +/-3 degrees and yaw varies. Corrected natural504-triangle surfaces are reused as inputs, with radial widths1.6x and tangential1.12x, anchored8.5m inside1km boundary, expanding toward town. Existing terrain and town assets remain unchanged.

Prior whole-model minimum-rim seating hid low mountain shapes. New derivative meshes place each source-height vertex over local terrain (rim -.8m), rather than lowering the whole slope to the lowest rim terrain. Minimum relief45m prevents short low ranges disappearing. Natural rim edges sample every2m; local downward corrections keep them >=.4m buried, without restoring tall walls. Duplicated rim copies share each correction. Source Blender/FBX/catalogue and previous derivative assets remain unchanged. Twelve authored meshes total6048 triangles. Logs/AlpsTwelveMountains contains evidence/backups. Earlier one-time Alps migrations must not run on this layout.

Native forced compilation/reload, save/reopen, scene/19 snow and2195 clear access checks passed.24 mountain views and overview reviewed;75 gameplay isometric views regenerated. All2391 protected source/prefab/previous terrain/mesh/Pinefall/profile files unchanged. Saved mesh continuity checks passed for all12 derivatives. Only the mountain group changed among existing scene records. First pass caught a near-rim height tolerance and restored the scene before saving; the corrected rim is projected to a consistent-.8m baseline. Initial failure and unused derivatives with zero outside references retained under Logs. No Play rerun or Android/device acceptance. Alps open, Play stopped.


## Original imported terrain restored ? 17 November 2026

This supersedes the later shelf, rolling-hill and town-edge terrain shape passes.
Only the terrain mesh (render/collision) and hidden camera-height query return
to their preserved first-import references. The original 1000m footprint,
5600-triangle source geometry, import Y factor .33202338 and 5.13m snow datum
remain. All existing mountain/building/road/tree transforms are deliberately
left alone; their seating and access are deferred at the user's request.

After native compilation succeeds, queue Logs/AlpsRestoreImportedTerrain.request.
Backups and a terrain-only overview: Logs/AlpsRestoreImportedTerrain. The source
Blender/FBX/prefab, previous derived terrain assets, snow and profiles are kept.
Do not replay historical hill/mountain migrations to refine the restored shape.

Native save/reopen validation passed; all transforms and all 1341 protected
source/terrain/Pinefall/profile files are unchanged. The serialized scene diff
contains exactly the four expected terrain-related reference records. The
terrain-only preview was visually inspected. No Play or seating/access tests.

## Supplied V2 mountains in terrain test - 18 November2026

User supplies Snow_District_Alps_Mountains(2).blend and requests replacing test-scene mountains. Source preserved at SourceAssets/AlpsMountainsV2/Mountains.blend with SHA2563cca3f2fc86ba27e43dc6defb16cb45c598f5e4faf60d0d425843787f1ada48b. Evaluated export via Tools/AssetIntake/export_alps_mountains_v2.py with autoexecution disabled; embedded historical/trim text is not executed. Eight source variants,11819 triangles total, original positions/UVs/proportions retained in separateFBX files. Current icy palette derivative is reused for scene consistency; raw source palette retained separately.

TestMountainV2Intake prepares eight NeedsReview catalogue entries/prefabs, shared URP material and matching static MeshColliders. Marker Logs/AlpsMountainsV2.request submitted atomically only after native compile exit0. Work on current in-memory AlpsTerrainTest, backing up unsaved edits first; never reopen stale disk before backup. Existing eight XZ/rotations are kept, uniform scales match prior maximum footprint widths or reduce only to keep replacement geometry within actual terrain edges. All eight variants appear once (second old cliff becomes broad massif). Bottom ring samples <=2m seat via complete-instance Y translation only, .5m buried. No source deformation/skirt or terrain edits. Main Alps/Pinefall and profiles preserved. Evidence/backups: Logs/AlpsMountainsV2.

Native Android-target forced compile/domain reload and saved/reopened catalogue/scene checks passed. Eight individual renders/contact sheet and overview inspected. Source triangle counts match exports (11819 total); matching visible/static collision meshes saved. Three replacements use small uniform boundary-fit reductions (NW .9347, SE massif .8838, S .9170); XZ and rotations remain exact. Visible relief38.83-99.57m. All2934 protected existing asset/terrain/game-scene/build/profile hashes match. Among pre-existing test-scene records, only the mountain group child list changes; eight old prefab records replaced by eight new ones. Embedded Blend texts were not executed. No Play run or phone/performance acceptance; test scene remains open, Play stopped.

## Light gray rock and slope-based snow on test mountains - 18 November2026

User requests light gray rock shades and snow determined by face angle. AlpineSlopeSnow.shader supplies one shared opaque URP material at Levels/AlpsTerrainTest/SlopeSnow/MountainSlopeSnow.mat. Snow limit50 degrees from horizontal,4-degree transition (<=46 snow, >=54 rock); world-space imported flat normals retain faceted shape and respond to model rotation. Vertex-stage slope classification, direct main-light/fog/bounded pale shadow coloring; no textures, added lights, runtime geometry or scripts. Material exposes angle, blend and four colors in Inspector. Test-only overrides leave imported meshes/UVs/prefabs/catalogue, all poses/colliders/terrain and game scenes untouched.

TerrainTestMountainColors is a one-time Editor installer via atomic Logs/AlpsMountainColors.request after native compile exit0. Back up current in-memory scene before work; restore on failure and refuse repeated installation. Evidence/backups/renders: Logs/AlpsMountainColors. Native Android-target forced compile/domain reload, shader error checks and save/reopen passed. All eight variants contain valid rock/snow slope classes:4252 rock,5374 snow,2193 transition triangles (11819 unchanged). Overview/eight individual views/contact sheet inspected. All3287 protected files match before/after, including source/import/prefab/previous materials, terrain, game scenes, runtime scripts, build settings, local editor configuration and saves. Exactly eight test-scene prefab material-override records change; no records added/removed or transform/collider changes. No Play or Android device/performance acceptance. Test scene reopened, Play stopped.

## Altitude extends mountain snow onto steeper faces - 18 November2026

User requests snow extending onto angled faces as mountain height increases. AlpineSlopeSnow retains the base50-degree slope allowance, gradually increasing toward78 degrees over world altitude45-110m, with the existing4-degree slope transition. Material exposes high-altitude angle and start/end heights. Classification stays in the vertex shader, retaining shared opaque material, original light gray rock/snow colors, meshes, UVs, poses and collision. No extra geometry/lights/runtime scripts.

TerrainTestMountainColors height action through atomic Logs/AlpsMountainColors.request applies settings after native compile exit0, preserves in-memory scene and prior material, captures eight mountain views/overview and reopens the test scene. Evidence: Logs/AlpsMountainHeightSnow. Native forced compilation/domain reload, shader/material checks and visual review passed.4466/31410 vertices gain snow coverage; mean sampled vertex snow factor increases .5526 to .6363, with none losing snow. This is a vertex diagnostic, not screen area. All3290 protected files match, including every saved scene/terrain/source/prefab/profile; only the authorized shader/editor source/material change. No Play/device performance acceptance. Test scene open, Play stopped.
