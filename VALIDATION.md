# Verification record

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
