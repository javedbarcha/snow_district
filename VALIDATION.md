# Verification record

## All houses replaced and enlarged - 29 October 2026

- Replaced every prototype cottage: all 16 saved lots now use imported homes. Editor-only balanced shuffling (seed 291026) places Red Cottage and Green Porch three times each; Garage Home, White Colonial, Storybook Cottage, Twin A Frame and Brick Barn twice each. Every variant is used; no runtime randomization/construction was added.
- Houses are uniformly enlarged 1.5x relative to the previous per-lot fitting calculation. Foundation X/Z footprints increase exactly 1.5x; height/ground level and fences remain unchanged. Catalog maximum placement scale is 1.5. Existing home IDs are preserved, new IDs are unique, and repeat intake preserves the saved layout without cumulative scaling. Future full bakes use this all-lot path.
- Unity 6000.3.8f1 / Android compiled, saved/reopened Pinefall, confirmed all 16 placements and seven variants, passed house/road and house/house footprint overlap preflight, scene structural validation and all 19 snow-ledger checks. Independent saved-YAML checks verified 16 unique home IDs, no primitive cottage groups, and all 16 foundations' exact horizontal size change with unchanged height. District overview inspected in Scene view. Report: `Logs/WinterHomes/UnityIntake.txt`.
- No snow seed/geometry, terrain, vehicle prefab or player save was regenerated. Pre-change scene backup: `Logs/WinterHomes/Pinefall-before-all-homes.unity`. Live vehicle clearance, snow interaction around larger foundations, current Android build/appearance and sustained Galaxy A07 profiling remain untested.


## Wall winding correction - 29 October 2026

The right wing of Red Cottage, living wing of Garage Home and cross wing of White
Colonial had inward-facing closed wall shells (seven polygons each). Prepared
exports now recalculate consistent outward face winding per closed source solid
before joining; intentionally open facade panels retain their authored direction.
All 958 closed solids passed outward-volume checks, and a Blender FBX round trip
found zero inward closed shells across all seven exports. NormalValidation.json
records the three repairs. Tools/AssetIntake/check_winter_home_normals.py repeats
the exported-shell check with Blender --background --factory-startup --disable-autoexec.

Unity reimported the FBXs, saved/reopened Pinefall, passed structural validation
and all 19 snow-ledger checks, and regenerated the seven prefab previews. The three
corrected facades were visually inspected. Triangle/vertex counts, mesh/material
GUIDs, original .blend, existing placements, removable snow and opaque windows
are preserved. No district or snow rebuild was performed. Android/device rendering
and continuous vehicle clearance remain untested.

## Seven winter homes intake - completed 29 October 2026

- Preserved the supplied Blender file byte-for-byte (SHA256 `2AD3BEC380E5EE05A400561C0E9CA5445607C4D389EBFBF46EF4E4DECB9F8CDE`). Inspected all seven collections and rendered their source views with embedded-script autoexecution disabled. Complete part maps account for every house-collection object without duplicates; display tiles, fonts, cameras and lights are excluded.
- Prepared seven catalog entries/prefabs, IDs `winter-home-01` through `winter-home-07`. Four merged renderers per house retain architecture, opaque warm windows, detachable roof snow and ground drifts. Ground drift bases are clipped/capped on prepared copies; source geometry stays intact. Compound wall/deck/step colliders and entrance anchors are saved for review.
- Fixed the initial triangle-count assertion: six FBX architecture meshes contain eight zero-area triangles each, verified by Blender round-trip inspection. Export metrics now record these explicitly; native Unity counts must equal export triangles minus zero-area triangles. Actual prepared geometry is 1,002-2,848 triangles / 2,078-5,820 imported vertices per home. See `MOBILE_ASSET_BUDGET.md` for individual counts.
- Unity 6000.3.8f1 with Android selected compiled the Editor intake, prepared/validated all seven prefabs, replaced seven cottage buildings near the depot, saved and reopened Pinefall, and passed scene structural validation plus all 19 snow-ledger checks. Foundations/fences, fleet bindings and snow seed dependency hash were preserved. Existing generated snow assets, terrain and vehicle prefabs were not regenerated; player saves were not loaded or written.
- Rendered seven native Unity prefab previews and inspected the saved cottage in Scene view. Preview key-light direction was corrected to illuminate the visible facades; all seven corrected previews were inspected. Repeated intake preserved exactly seven placements and passed the same saved-scene checks. Entries remain Needs Review; live vehicle/house clearance, full placement/collider walkthrough, Android packaging, shader appearance on phone and sustained Galaxy A07 timings remain untested. Reports/previews: `Logs/WinterHomes/UnityIntake.txt`, `NN-unity.png`; scene backup: `Pinefall-before.unity` in the same ignored folder.

## Keyboard / loaded-bucket control fixes - 28 October 2026

- Prevent arrow/Space gameplay input from also navigating/submitting selected HUD buttons; restore UI keyboard navigation in menus. This addresses a confirmed input-routing conflict, but the user's complete keyboard outage has not yet been reproduced and its Editor/build context is awaiting clarification.
- Move the loader's 92%-full Auto carry check outside the moving/collecting branch, covering stationary/restored loads. Preserve a higher lift and the dump pose. Explicit lift/tilt axes immediately enter Manual; lowering into the work pose re-enables collection. Snow transfer arithmetic and saved equipment geometry are unchanged.
- Runtime and Editor sources (including the new regression helper) compiled with Unity 6000.3.8f1's Roslyn compiler and the project's existing Unity assembly/reference response files. Output is isolated in ignored `Logs/ControlsCompile`, not installed into the running Editor. Editor compile has one pre-existing `AssetWorkshopWindow.title` hiding warning. This is a C# compile check, not a completed Unity scene/play test.
- Added **Snowglow > Run driving input and loaded bucket checks**, queued with `Logs/DrivingControls.request`. It exercises injected keyboard events, HUD navigation routing, actual imported loader mouth elevation/full/partial/manual/dump poses and the 19 snow ledger checks without loading/writing player saves. Execution is pending refresh of the existing Editor, whose main window was unavailable to automation. Read a fresh `Logs/DrivingControls.txt` before claiming these checks passed. Physical keyboard/Game view focus, continuous driving/loading and Android device testing remain pending.

## Soft matte toon snow - 28 October 2026

- Reference revision: user requested pure-white tops and pale cyan shading from a snowy mountain image. Updated mesh palette to `#FFFFFF` / `#BFE7EE`, added upward-normal whitening and reset terrain layer tint to white. Unity imported the shader and passed the material/binding/seed/scene checks again (`Logs/SoftSnowMaterial.txt`). Final reference revision was not visually inspected because the Unity window closed before capture; Android appearance remains untested. The off-white palette below records the earlier iteration.

- Applied off-white `#F1F3EE` and icy-blue shade `#AFCBE2` to the shared opaque snow mesh material using a lightweight URP shader. This covers plowable snow, piles, snow caps and carried snow through existing palette bindings. Terrain retains supported URP Terrain/Lit with its snow layer tinted off-white and metallic/smoothness zero. An experimental custom terrain pass failed visual review and was removed.
- Unity 6000.3.8f1 / Android compiled Editor code and imported the mesh shader without reported shader errors. Material GUIDs, seed dependency hash, chunk bindings and terrain instancing were preserved; Pinefall scene structural validation passed. Visually inspected snow surfaces, roof caps and restored terrain rendering in Unity Scene view. See `Logs/SoftSnowMaterial.txt`. Future Editor district bakes apply the same defaults; a full district rebake was not run for this material change.
- Snow geometry, volume mechanics, vehicle physics and scenery layout are unchanged. Android player shader compilation, current gameplay with the new material and Galaxy A07 appearance/frame timing remain untested. Task 2 (finished scenery asset) is deferred.

## Bright opaque vehicle windows - 28 October 2026

- Replaced transparent glazing on both imported vehicles with the shared `Snowglow/Toon Vehicle Windows` URP shader: bright cyan, pale diagonal painted reflection streaks, opaque geometry queue, solid alpha and depth writes. Object-space projection works with both existing combined meshes without new UVs, textures, triangles or runtime construction. Imported single-plane windows remain visible from both sides.
- Updated the two existing shared window materials in place, preserving prefab/scene references, plus future model-intake material creation. The shader is unlit and does not sample lighting, reflection probes or textures. This removes transparent window blending/sorting; it does not establish a measured FPS improvement.
- Unity 6000.3.8f1 / Android compiled runtime/Editor code and the Editor shader, verified opaque material state, both saved equipped prefab bindings and the current Pinefall scene. Inspected the tractor's opaque cyan windows and diagonal highlights in Unity. See `Logs/ToonWindows.txt`. Android player shader compilation and Galaxy A07 timings remain untested.

## Loader tyre clearance and raised tread repair - 27 October 2026

- Corrected the bucket/tyre overlap by extending the prepared boom reach 0.8 m, moving its bucket pin accordingly, and reducing the working boom pitch from 18 to 9 degrees. The boom mesh stretches with the new pin; bucket-local shape, intake and snow-load anchors are unchanged. Updated both catalog body and equipped prefab, its equipment COM and Pinefall in Edit mode.
- Found all loader tread block faces pointing inward, plus inconsistent winding on tractor chevron grips. Recalculated outward faces on prepared mesh copies: 66 blocks per loader wheel and 40/32 per rear/front tractor wheel. Loader tread tips already protrude to 0.73 m over the 0.68 m tyre carcass, so no geometry was added. Source .blend files remain untouched. Export scripts assert outward orientation for every block; saved `TreadValidation.json` files record the results.
- Unity 6000.3.8f1 / Android saved/reopened the scene and passed 441 lift/tilt samples against a conservative tyre envelope allowing for steering. Minimum longitudinal gap was 0.239 m (required >0.08 m). Work/carry/dump ground clearance, load display, machine identity/controller bindings and all 19 snow checks passed. See `Logs/LoaderIntake/ClearanceChecks.txt` and `UnityValidation.txt`.
- Inspected the corrected loader in Unity: bucket stands clear of the tyres and grips have solid outward-facing surfaces. Triangle counts remain 8,424 for the loader including snow load and 7,274 for the tractor. Continuous motion, suspension/body contacts, live driving and phone profiling remain untested; the sampled geometry check does not replace a full playtest.

## Imported snow loader replacement - 27 October 2026

- Preserved `snow_loader_lp.blend` byte-for-byte in `SourceAssets/LowPolySnowLoader` (SHA256 `365804EDC406BD3D021C8156956C984815D4567F30ABAA58D2F29ED993467327`). Inspected named parts without executing embedded text. Exported evaluated meshes/curve hoses into body, four wheels, boom and bucket modules; excluded presentation objects.
- Saved two Needs Review catalog entries, a fitting/recipe and `ImportedSnowLoader.prefab`. Replaced Pinefall Fleet[1], retaining the `wheeled-loader-01` save ID, 1,400 price, initial unowned state and existing upgrade/ownership lookup. The imported free tractor, scenery and saved snow assets are unchanged. Full district Editor baking now uses both imported machinery prefabs.
- Authored separate boom and bucket tilt pivots, connection/intake/load anchors, wheel rig, collision proxy and snow spray. The low working pose and full-lift automatic dump passed geometric shell/edge clearance checks. Added per-rig minimum dump lift; existing machines retain 0.55, imported loader uses 1. Hydraulics/chassis articulation remain prototype approximations.
- Unity 6000.3.8f1 / Android compiled, saved and reopened Pinefall; passed persistent meshes, rig/controller initialization, carry/dump/load display, drive/park, purchase/save identity and all 19 snow checks. The 39 workshop checks also passed. Fixed catalog prototype reimport overwriting an existing wheel entry's physics from the newly replaced fleet. Palette colors and rest alignment were inspected in Unity; the source render was also inspected.
- Loader geometry: 7,656 triangles before carried snow; 8,424 triangles / 15,095 imported vertices / 8 renderers / 9 material slots including its inactive load visual, excluding particles. Shared opaque palette and separate glass reduce color-related submissions. Vertex splits explain the difference from Blender's 4,740 evaluated vertices. See `MOBILE_ASSET_BUDGET.md` and `Logs/LoaderIntake/UnityValidation.txt`.
- Catalog now warns at provisional vehicle review thresholds of 15,000 triangles / 30,000 imported vertices, alongside material-slot warnings. These do not establish device performance. Live driving/snow interaction, all manual poses, hydraulic linkage fidelity, APK and sustained Galaxy A07 measurements remain untested. No player save was modified.

## Imported tractor as free starter - 27 October 2026

- Replaced only Pinefall's first fleet machine with the saved `GoldenTractor-Starter.prefab`. Its physics definition costs zero, uses the existing `municipal-plow-01` save ID, and is owned initially. Session initialization now guarantees starter ownership even if an old profile recorded it as unowned. Existing coins, upgrades, active selection and loader purchase behavior are preserved.
- Bound the imported model's four wheel pivots to WheelColliders with separate front/rear radii, and connected its fitted blade working edge, existing work/lift/angle controls and preauthored snow spray. Mass includes the beacon and plow. Full district Editor baking also uses this starter prefab. Catalog source entries retain their review status; this is an explicit prototype integration, not a claim of production approval.
- Unity 6000.3.8f1 / Android compiled and saved/reopened Pinefall. Checked connected starter prefab in Fleet[0], zero price/owned state, legacy ID, unchanged loader prefab, unequal wheel radii, equipment compatibility, controller initialization, work/raise/angle commands and drive/park activation. Scene validation and all 19 snow checks passed. No player save was loaded or modified by these checks. Report: `Logs/StarterPlowValidation.txt`.
- The blade/lift hardware moves as one rigid module under the prototype controls. Faithful hydraulic linkage, motion clearance, live driving/snow interaction, Android packaging and Galaxy A07 performance remain untested. The scene replacement did not regenerate terrain, snow, scenery or the loader.

## Supplied Blender tractor intake - 27 October 2026

- Inspected `low_poly_snow_plower.blend` using Blender 5.2 with embedded script autoexecution disabled. Preserved a byte-identical original under `SourceAssets/LowPolySnowPlower`; source/copy SHA256 is `8F48568B0051F2379707F15809558090DE7128C23637BAD6E6A8774BA815B85C`. The embedded generator was not executed.
- Accounted for all 209 named vehicle meshes in a saved part map. Excluded the unrelated cube, studio floor, cameras and lights. Exported body/four wheels, plow/lift hardware and beacon as separate FBX modules. Rigid modules are combined while the original Blender parts remain editable.
- Unity 6000.3.8f1 with Android selected compiled the intake and imported metre-scale, Y-up/Z-forward meshes. Verified all four wheel pivots and fitted front/roof sockets. Prepared three Needs Review catalog entries, two unapproved fittings, a recipe and `GoldenTractor-REVIEW.prefab`. All geometry and materials are persisted through Editor authoring; no runtime construction or Pinefall/fleet replacement was added.
- Model-specific structural checks passed: prefab save/reopen, persistent meshes, wheel bindings, front/roof compatibility and refusal to bake unapproved assets. Source render and the combined Unity prefab were visually inspected at rest. Original toon materials are approximated with a URP palette; this does not establish shading fidelity in gameplay.
- Reran all 39 Asset Workshop acceptance checks, saved Pinefall validation and all 19 snow checks successfully. Reports: `Logs/PlowerIntake/UnityIntake.txt`, `Logs/AssetWorkshopChecks.txt`, `Logs/SceneValidation.txt`. Saved Pinefall and Generated assets have no changes.
- Remaining: author/review lift linkage and yaw behavior, full movement/clipping clearance, physical mass/COM/suspension tuning, driving and snow interaction, Android build and Galaxy A07 profiling. The source has unequal front/rear wheels; the uniform wheel-set socket is omitted. The review preview has no MachineController and is not a drivable machine. Total 7,274 triangles / 7 renderers / 30 material slots; phone performance is unmeasured. See `SourceAssets/LowPolySnowPlower/README.md`.

## Asset Workshop phase - 26 October 2026

- Added Editor-only model preparation, source-only prompt/image/script drafts, a categorized prefab catalog, explicit review/Ready publication, fitting profiles, modular vehicle assembly and a Ready-only JSON manifest/scene-plan workflow. Nine prototype catalog entries are saved as separate prefab copies and remain Needs Review.
- Preparation preserves imported sources, proposes collision and four-wheel rigs, records provenance and metrics, normalizes roots, and copies Standard/legacy materials to URP Simple Lit. Fused wheels, absent loader pivots and missing equipment anchors block publication. Stable IDs are read-only in the custom entry inspector.
- Approval fingerprints cover prefab dependencies, physics and constraints; fitting fingerprints additionally cover socket and mount transforms. Assembly baking supports blades, buckets, wheel sets and accessory mass/modifiers, retains authored rig rest transforms, and returns registered outputs to review after rebaking.
- Scene plans resolve approved IDs and road connections before authoring. Validation rejects invalid transforms/bounds/scales, overlaps, unknown/unapproved/stale entries and missing/cyclic road targets. New scenes are persisted in Edit mode; Pinefall and existing scene files cannot be overwritten by this workflow. Generated layouts do not automatically add contracts, snow coverage or machines to the current session fleet.
- Unity 6000.3.8f1 / Android compiled all runtime/Editor scripts and passed **39 actual Editor acceptance checks**, including source preservation, draft completion, material conversion, incompatible/overweight parts, changed approvals, plow/loader baking, wheel physics, catalog identity updates, connected roads, scene save/reopen and fixture cleanup. Pinefall scene validation and all 19 snow checks also passed. See `Logs/AssetWorkshopChecks.txt`.
- Verified `Assets/Scenes/Pinefall.unity` and `Assets/Snowglow/Generated` have no changes from this phase. Existing vehicle defaults retain grip 1 and the original dry centre of mass; new rig behavior is opt-in.
- The workshop window's visual layout/drag interaction, imported production models, manual clipping/movement review, live driving of newly assembled machines and sustained Galaxy A07 performance have not been tested. No new APK was built. Current player shop/ownership/save UI is unchanged; broader catalog-driven garage integration is a later step.
- Full instructions and scene-plan schema example are in `ASSET_WORKSHOP.md` and `Assets/Snowglow/Catalog/Examples/DistrictLayout.json`.

## Git-friendly snow asset split - 26 October 2026

- Split `Snow50cm_20261026_085523_745.asset` (170,024,383 bytes) into its existing seed asset plus 32 native mesh-part assets. Kept the seed GUID, scenery, vehicles and authoritative snow data; rewired saved visual/collision mesh references in Pinefall.
- Both snow rebuild and full district bake now group eight chunks per mesh file, enforcing a 90,000,000 byte per-file safety budget. Earlier unrelated generated assets are unchanged.
- Unity 6000.3.8f1 / Android saved and reopened the scene, passed reference/rig/asset validation and all 19 ledger checks, and confirmed the snow volume was unchanged. See `Logs/SnowAssetSplit.txt` and `Logs/SceneValidation.txt`. A local copy of the original asset and scene is kept in ignored `Logs/SnowAssetSplitBackup/`.

## Cohesive pile spreading - 26 October 2026

- Replaced centre-first stacking with lower-cell deposition weighted by distance, so piles grow outward as well as upward. Added bounded eight-neighbor slumping with a tuned slope of about 32 degrees, 3 cm cohesion allowance and gradual transfer rate. The 50 cm grid and current visual representation are unchanged.
- Existing and newly modified piles settle during gameplay. Pause freezes settling; blocked cells and field edges prevent invalid transfers; passive movement cannot generate work credit. Per-frame settling and mesh work remain budgeted.
- All 19 actual C# ledger checks passed under Mono (Unity logging/editor wrappers stubbed), then passed in Unity 6000.3.8f1 with Android selected after successful compilation. Seven new cases cover mound footprint, pause, downhill movement, volume conservation, resting slope, credit, obstacles and field edges. See `Logs/PileChecks.txt`.
- No rendered gameplay, handling or sustained Galaxy A07 performance measurement was performed for this change.

## Half-size snow cells - 26 October 2026

- The current checked-out source and scene used the original 1 m cells. Reduced width/length to 50 cm: four times the horizontal resolution over the same 256 m working field. Kept the existing combined-column representation.
- Updated cell-area volume accounting, coordinate conversion, working footprint, disposal bounds, scene validation and legacy save/seed migration. The snow-only editor rebuild retains vehicles, scenery and previous snow assets.
- Unity 6000.3.8f1 / Android compiled, rebuilt the saved snow, reopened Pinefall and passed scene validation plus all 12 ledger checks. The same checks also passed independently under Mono. `Logs/SceneValidation.txt` records the latest result.
- Visual gameplay and sustained Galaxy A07 performance at this resolution remain untested.

## Unity verification - 22 October 2026

- Read README.md, ARCHITECTURE.md and this checklist before changes.
- Used installed Unity 6000.3.8f1 with the project targeting Android; URP 17.3.0, Input System 1.18.0 and uGUI 2.0.0 imported.
- Fixed package resolution: removed the nonexistent `com.unity.modules.textrendering` dependency and enabled `com.unity.modules.vehicles`, which supplies WheelCollider. Both runtime and editor assemblies compiled in Unity after the fix.
- Executed the Edit-mode authoring workflow. Unity logged authoring completion and saved the populated `Assets/Scenes/Pinefall.unity`, generated assets and connected machinery prefabs under `Assets/Snowglow/Generated/Bake_20261022_111754_769/`.
- Added `Snowglow > Validate saved district and snow ledger`. It reopens the scene and checks missing scripts, persisted rigs/definitions, compatibility, loader anchors, 256 snow chunks and their saved meshes/colliders, terrain, materials, URP and build-scene inclusion. Results go to `Logs/SceneValidation.txt`.
- Scene save failures now stop the bake; the ready marker is written only after asset/rig validation. Automatic first-import baking is disabled in batch mode.
- Headless Unity compilation and saved-scene reopening passed in an isolated project copy with the Android target. All scene validation checks and all nine snow ledger invariants passed. See `Logs/UnityValidation.log` and `Logs/SceneValidation.txt`. The copy preserved the open editor session and original authored content.
- A subsequent fresh-bake recheck was interrupted during compilation; `Logs/UnityBakeValidation.log` does not establish a completed second bake.
- The temporary `.validation-project` copy was removed at the user's request. Keep one Unity project at this root; use the Snowglow validation menu here for future checks. Validation logs remain under `Logs/`.

## Historical source checks (before Unity verification)

- Parsed all 24 C# source files with the tree-sitter C# grammar: no syntax-error nodes.
- Checked player-side code after excluding UNITY_EDITOR blocks: no `new GameObject`, primitive scene creation or Terrain factory calls. All authored scene construction is confined to Editor code.
- Checked the authoring source declares a 1000 × 1000 m Terrain, persistent terrain/material/definition/mesh assets, connected machinery prefabs and an explicit scene save.
- Checked URP/Input System/uGUI and Terrain/physics/text/audio module declarations.
- Verified all 10 synthesized WAV files are readable mono 16-bit PCM.
- Geometric checks: depot-access, parking and main-street contract footprints contain 248, 744 and 560 cells respectively. Road-based objectives lie within the intended road geometry.
- Checked ZIP integrity after packaging.

These checks do not establish Unity compilation or runtime correctness.

## Repeatable mechanics checks

`Snowglow → Run snow ledger checks` exercises the actual engine-independent C# volume model in Edit mode. Nine invariants cover pushing, source reward credit, recycled snow, carrying, dumping, rejected out-of-bounds/full destinations, randomized transfers and blocked cells. This command requires the scripts to compile in Unity first.

## Required first-import/editor checks

1. Open in Unity 6.3 LTS and resolve package import. No script/compiler errors.
2. Execute the Edit-mode bake if the first-import attempt does not run. No authoring exceptions, missing material resources or invalid mesh/collider errors.
3. With Play stopped, inspect the concrete Terrain, colored scenery, truck and loader, UI, snow and generated asset folder. Save, close and reopen: all remain present without rebuilding.
4. Run the ledger checks. Inspect attachment mount IDs, dry/loaded mass and width compatibility ratings.
5. Enter Play. Inspect camera framing, world labels, HUD scaling and touch controls.
6. Drive and brake on asphalt, snow and the ice strip; test slope/curb behavior and assistance on/off. Tune suspension, grip and torque using the Galaxy A07.
7. Push snow with the blade down, angle it both directions, then raise it. Observe pile obstruction, traction loss and load resistance. Check no snow enters solid structures.
8. Use the loader intake, lift, carry, curl/tilt and dump. Verify loaded mass/center of mass, finite capacity and conservation through partial/rejected dumping.
9. Switch vehicles only when stopped at the depot. Worksite, piles, loads, fuel and condition must remain.
10. Complete/abandon/restart jobs; confirm late pay reduction, original-work-only credit and no repeated completion grant. Test simulated success/cancel/failure/duplicate purchase delivery.
11. Save during a job with a loaded bucket and displaced snow. Restart: state restores consistently. Check backup recovery after an interrupted write.
12. Build Android, test all multitouch combinations and safe-area layouts, then run at least 15 minutes on the Galaxy A07. Measure CPU/GPU/physics, memory, heat, loading and save hitches. 30 FPS is a target, not a measured result.

## Known prototype limits

- Unity and Android Build Support are installed, and the district has been baked. Android player compilation, APK packaging and connected-phone validation have not been performed.
- No rendered screenshot, Unity playtest, sound playback test or APK has been produced. Computer-use capture could not inspect the editor because it reported `no monitor found for window`; headless checks do not verify appearance.
- Handling, snow forces, collider cooking and mobile draw cost require tuning in Unity. The source is a mechanics prototype, not a production-ready release.
- Approximately 250 m square has interactive snow; the full 1 km Terrain supplies the surrounding landscape. Terrain itself is not deformed by plowing.
- Snow is a depth/volume grid with blocky combined-cube visuals and substantial-pile collision proxies. No granular flow, compaction simulation, melt, active accumulation or snowblower behavior.
- Loader boom/bucket are kinematic articulated visuals coupled to load/volume mechanics, not simulated hydraulic joints. Only the two supplied equipment fits are authored.
- Low/standard quality controls affect rendering. Galaxy A07 performance has not been established.
- Three contract footprints, one district, two cameras, basic XP/levels, tyre/engine upgrades and local saving are implemented in source. Regional unlock maps, alternative attachment purchasing, tracked locomotion, advanced challenge rules, monetization SDKs and cloud services remain future work.
