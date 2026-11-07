# Pinefall asset geometry audit

Measured from the **saved scene**, 6 November 2026, using Unity 6000.3.8f1 / Android. Read-only preview-scene inventory; no gameplay, geometry, placement, snow, save or material changes. Active Editor scene and dirty state retained.

## Counting method and limits

Vertices are Unity `Mesh.vertexCount`, including normal/UV/color splits, not welded Blender control points. Triangles are triangle-topology index counts divided by three. Scene totals count every placed mesh renderer instance once, including inactive/disabled authored meshes; shared assets repeat for each placement. Unique-mesh totals count each GUID/local ID once and describe mesh reuse, not total RAM or GPU memory. Material slots are submission candidates, **not measured draw calls**. Catalog prefab totals include inactive authored mesh children; scene-only caption/rear-panel additions can make placed geometry differ from the catalog.

Terrain, particles, dynamic world lettering and Canvas UI are separated below. No camera-frustum/occlusion/shadow-pass multiplication is applied; these are **whole-scene content totals, not rendered-frame counts, FPS or GPU/CPU milliseconds**. Actual Play snow shape, particle counts, vehicle visibility and camera location change the cost.

## Whole-scene mesh inventory

| Asset type | Vertices across placements | Triangles across placements | Renderers | Material slots | Shadow-casting renderers |
| --- | --- | --- | --- | --- | --- |
| Interactive snow | 1,265,280 | 632,640 | 256 | 256 | 256 |
| Trees and wood (LOD3) | 479,481 | 187,488 | 657 | 657 | 657 |
| Homes | 149,096 | 72,552 | 160 | 160 | 120 |
| Sign boards | 26,022 | 18,050 | 102 | 102 | 97 |
| Street lights | 30,316 | 16,744 | 75 | 75 | 50 |
| Playable machinery | 29,247 | 16,018 | 43 | 67 | 15 |
| Parked vehicles | 26,756 | 13,344 | 15 | 15 | 15 |
| Clouds | 37,350 | 13,280 | 40 | 40 | 0 |
| Mountains | 22,711 | 10,088 | 24 | 24 | 24 |
| Facilities | 15,420 | 7,844 | 16 | 16 | 16 |
| Lot foundations and fences | 7,680 | 3,840 | 320 | 320 | 320 |
| Snowmen | 7,264 | 3,000 | 6 | 6 | 6 |
| Traction ramp | 5,580 | 2,668 | 2 | 2 | 2 |
| Roads, curbs and markings | 5,064 | 2,532 | 25 | 25 | 24 |
| Other authored scenery | 192 | 96 | 8 | 8 | 8 |

Total: **2,107,459 vertices / 1,000,184 triangles / 1,749 mesh renderers**. Distinct mesh assets: 471; their combined geometry is 1,452,323 vertices / 723,404 triangles. Static saved active+enabled mesh inventory: 998,648 triangles, still before camera culling.

## Forest: every tree and wood variant

All 26 variants use LOD3. **438 placements; 479,481 vertices; 187,488 triangles; 657 renderers.**

| Part | Placements | Vertices in scene | Triangles in scene |
| --- | --- | --- | --- |
| Trees/snags (01–20) | 420 | 475,500 | 185,841 |
| Stumps/logs (21–26) | 18 | 3,981 | 1,647 |

| Variant | Name | Vertices each | Triangles each | Placed | Scene triangles |
| --- | --- | --- | --- | --- | --- |
| 01 | Tower Spruce | 712 | 270 | 18 | 4,860 |
| 02 | Broad Fir | 544 | 208 | 18 | 3,744 |
| 03 | Slender Spruce | 752 | 282 | 9 | 2,538 |
| 04 | Irregular Drooping Fir | 650 | 234 | 18 | 4,212 |
| 05 | Wind Bent Pine | 515 | 192 | 27 | 5,184 |
| 06 | Open Alpine Larch | 1,635 | 588 | 9 | 5,292 |
| 07 | Young Fir | 329 | 126 | 24 | 3,024 |
| 08 | Spruce Sapling | 315 | 116 | 24 | 2,784 |
| 09 | Snowy Tower Spruce | 1,547 | 644 | 15 | 9,660 |
| 10 | Snowy Broad Fir | 1,171 | 489 | 33 | 16,137 |
| 11 | Snowy Slender Spruce | 1,673 | 679 | 36 | 24,444 |
| 12 | Snowy Irregular Drooping Fir | 1,410 | 550 | 33 | 18,150 |
| 13 | Snowy Wind Bent Pine | 1,051 | 445 | 33 | 14,685 |
| 14 | Snowy Open Alpine Larch | 3,390 | 1,315 | 36 | 47,340 |
| 15 | Snowy Young Fir | 806 | 321 | 9 | 2,889 |
| 16 | Snowy Spruce Sapling | 721 | 288 | 18 | 5,184 |
| 17 | Tall Dead Snag | 890 | 316 | 24 | 7,584 |
| 18 | Thin Bare Snag | 757 | 262 | 15 | 3,930 |
| 19 | Broken Alpine Trunk | 634 | 224 | 12 | 2,688 |
| 20 | Bare Young Sapling | 496 | 168 | 9 | 1,512 |
| 21 | Low Cut Stump | 172 | 66 | 3 | 198 |
| 22 | Tall Cut Stump | 180 | 66 | 3 | 198 |
| 23 | Split Cut Stump | 200 | 74 | 3 | 222 |
| 24 | Snow Capped Stump | 285 | 113 | 3 | 339 |
| 25 | Fallen Cut Log | 158 | 68 | 3 | 204 |
| 26 | Snowy Broken Log | 332 | 162 | 3 | 486 |

Forest is 18.7% of inventoried mesh triangles (Terrain/particles/UI excluded). Catalog sum for one of each of the 26 variants: 21,325 vertices / 8,266 triangles. Forest geometry reconciles exactly between per-variant multiplication and measured placed renderers.

## Per-asset counts for other placed catalog variants

Counts below describe one reusable prefab, not multiplied instances. Scene-only edits remain in the category totals above. Complete unplaced/prototype catalog entries are in CatalogAssets.csv.

| Asset ID / name | Vertices each | Triangles each | Renderers each | Material slots each | Placements |
| --- | --- | --- | --- | --- | --- |
| imported-tractor-beacon / Golden tractor  /  roof beacon | 144 | 72 | 1 | 2 | 1 |
| imported-loader-bucket / Golden loader  /  wide snow bucket | 1,063 | 1,060 | 2 | 2 | 1 |
| imported-tractor-plow / Golden tractor  /  curved plow and lift frame | 3,024 | 1,654 | 1 | 4 | 1 |
| four-bay-snow-garage / Four-bay snow garage | 7,260 | 3,832 | 10 | 10 | 1 |
| snow-disposal-yard / Snow disposal yard | 2,073 | 972 | 3 | 3 | 1 |
| snow-machinery-fuel-station / Snow machinery fuel station | 6,087 | 3,040 | 3 | 3 | 1 |
| winter-home-01 / Red Cottage | 3,566 | 1,724 | 4 | 4 | 6 |
| winter-home-02 / Green Porch | 3,960 | 1,944 | 4 | 4 | 7 |
| winter-home-03 / Garage Home | 3,762 | 1,828 | 4 | 4 | 6 |
| winter-home-04 / White Colonial | 3,568 | 1,728 | 4 | 4 | 5 |
| winter-home-05 / Storybook Cottage | 3,600 | 1,748 | 4 | 4 | 5 |
| winter-home-06 / Twin A Frame | 2,078 | 1,002 | 4 | 4 | 6 |
| winter-home-07 / Brick Barn | 5,820 | 2,848 | 4 | 4 | 5 |
| alpine-mountain-01 / Needle Peak | 1,524 | 690 | 2 | 2 | 3 |
| alpine-mountain-02 / Twin Summits | 1,601 | 726 | 2 | 2 | 3 |
| alpine-mountain-03 / Sawtooth Ridge | 2,380 | 1,016 | 2 | 2 | 2 |
| alpine-mountain-04 / Glacier Massif | 2,041 | 930 | 2 | 2 | 2 |
| alpine-mountain-05 / Alpine Cirque | 2,247 | 974 | 2 | 2 | 2 |
| alpine-sign-01 / Trail directory four routes | 1,314 | 854 | 7 | 7 | 1 |
| alpine-sign-02 / Single green trail board | 444 | 290 | 3 | 3 | 1 |
| alpine-sign-03 / Blue direction arrow | 375 | 231 | 3 | 3 | 1 |
| alpine-sign-04 / Alpine hazard warning | 734 | 524 | 3 | 3 | 12 |
| alpine-sign-05 / Road closed warning | 627 | 421 | 3 | 3 | 12 |
| alpine-sign-08 / Wooden arrow right | 388 | 240 | 3 | 3 | 1 |
| alpine-sign-10 / Blank hanging board | 722 | 392 | 3 | 3 | 1 |
| alpine-sign-11 / Festive hanging board | 1,989 | 1,037 | 3 | 3 | 1 |
| alpine-street-light-02 / Park Globe | 1,092 | 600 | 3 | 3 | 2 |
| alpine-street-light-03 / Compact Village Lantern | 1,128 | 630 | 3 | 3 | 3 |
| alpine-street-light-04 / Heritage Single | 1,128 | 630 | 3 | 3 | 3 |
| alpine-street-light-05 / Heritage Twin | 1,980 | 1,084 | 3 | 3 | 2 |
| alpine-street-light-07 / Alpine Hanging Lantern | 1,244 | 684 | 3 | 3 | 2 |
| alpine-street-light-08 / Heritage Triple | 2,384 | 1,290 | 3 | 3 | 2 |
| alpine-street-light-09 / Residential Curved LED | 780 | 438 | 3 | 3 | 4 |
| alpine-street-light-10 / Main Road Outreach | 780 | 438 | 3 | 3 | 4 |
| alpine-street-light-11 / Dual Median LED | 1,092 | 604 | 3 | 3 | 2 |
| alpine-street-light-12 / Four Way Area Light | 1,724 | 936 | 3 | 3 | 1 |
| angular-cloud-01 / Alpine Puff | 923 | 332 | 1 | 1 | 5 |
| angular-cloud-02 / Wide Cumulus | 935 | 332 | 1 | 1 | 5 |
| angular-cloud-03 / Tall Cluster | 926 | 332 | 1 | 1 | 5 |
| angular-cloud-04 / Long Cloud Bank | 967 | 332 | 1 | 1 | 5 |
| angular-cloud-05 / Asymmetric Puff | 928 | 332 | 1 | 1 | 5 |
| angular-cloud-06 / Stepped Cluster | 919 | 332 | 1 | 1 | 5 |
| angular-cloud-07 / Compact Crown | 930 | 332 | 1 | 1 | 5 |
| angular-cloud-08 / Broad Alpine Bank | 942 | 332 | 1 | 1 | 5 |
| parked-town-vehicle-01 / Snow Hatchback  /  parked | 5,196 | 2,602 | 3 | 3 | 1 |
| parked-town-vehicle-02 / Family Sedan  /  parked | 5,196 | 2,602 | 3 | 3 | 1 |
| parked-town-vehicle-03 / Utility Pickup  /  parked | 5,804 | 2,852 | 3 | 3 | 1 |
| parked-town-vehicle-05 / Alpine SUV  /  parked | 5,268 | 2,642 | 3 | 3 | 1 |
| parked-town-vehicle-08 / Town Taxi  /  parked | 5,292 | 2,646 | 3 | 3 | 1 |
| snowman-01 / Classic snowman | 2,322 | 1,000 | 2 | 2 | 1 |
| snowman-02 / Knit-cap snowman | 2,440 | 1,000 | 2 | 2 | 1 |
| snowman-03 / Earmuff snowman | 2,502 | 1,000 | 2 | 2 | 1 |
| vehicle-traction-test-ramp / Vehicle traction test ramp | 5,580 | 2,668 | 2 | 2 | 1 |

## Machinery as actually saved in the scene

| Machine | Vertices | Triangles | Renderers | Slots | Distinct materials |
| --- | --- | --- | --- | --- | --- |
| GoldenTractor-Starter | 13,788 | 7,408 | 19 | 42 | 16 |
| ImportedSnowLoader | 15,459 | 8,610 | 24 | 25 | 11 |

Includes saved inactive carried-snow/lamp mesh children; excludes emitted particle instances.

## Separate costs and exclusions

- Terrain `Terrain | 1000 x 1000 metres`: 257 × 257 height samples; un-LOD full-grid reference 66,049 vertices / 131,072 triangles. **Not an actual terrain draw count**: adaptive tessellation/culling depends on camera and pixel error (12.0). Instanced terrain: True.
- 10 renderers without a saved mesh were excluded (including engine-generated world text/sprites); UI Canvas geometry is also outside this mesh audit.
- 4 authored particle systems are listed in NativeInventory.json; actual particle geometry is not multiplied into scene totals.
- Mesh particles `Exhaust smoke | low-poly authored`: 42 vertices / 80 triangles **per puff**, capacity 24; capacity ceiling 1,920 triangles per system, not an observed simultaneous count.
- Mesh particles `Exhaust smoke | low-poly authored`: 42 vertices / 80 triangles **per puff**, capacity 24; capacity ceiling 1,920 triangles per system, not an observed simultaneous count.
- 1093 saved colliders; 840 active+enabled. 41 saved Light components; 1 active+enabled in Editor state, before runtime gates. Collision, light/shadow, texture, shader, transparent fill-rate, snow-update, UI and save costs cannot be inferred from triangle counts.
- Existing distance-culling components: 438 at 230 m. DistanceVisibility checks selected-machine distance every 0.7 s; this does not guarantee every tree inside the radius is visible to the camera.
## Assessment by asset type

| Type | Suggested action |
| --- | --- |
| Interactive snow | Largest single mesh category; profile visible chunks, mesh/collider rebuilds and shadows before reducing simulation/visual resolution. |
| Trees and wood (LOD3) | Already lowest LOD3. Keep existing 230 m distance culling; investigate snowy canopy geometry only if the forest is a measured bottleneck. |
| Homes | 40 homes, 160 renderers. Per-model geometry is reasonable; investigate rigid-part/material consolidation while keeping removable snow and windows. |
| Sign boards | 102 renderers for 18,050 triangles, including inactive original artwork. Consider merging rigid same-material pieces per board, preserving edited captions and rear panels. |
| Street lights | 75 renderers and three shared materials. Consider per-fixture combining; preserve lens/snow control and avoid enabling optional real-time lights across the town. |
| Playable machinery | 43 renderers and 67 material slots. Investigate slot consolidation before triangle decimation; preserve wheels, blade/bucket articulation, snow load and light switching. |
| Parked vehicles | Five vehicles, 15 renderers and 13,344 triangles. Low priority for geometry reduction. |
| Clouds | 40 opaque renderers, 13,280 triangles, no shadow casters. Keep geometry; investigate compatible repetition batching only if submissions are a bottleneck. |
| Mountains | 12 mountains total only 10,088 triangles. Keep nearby geometry; panorama can supply beyond-geometry scenery. Low priority for decimation. |
| Facilities | Garage/fuel station/disposal yard total 7,844 triangles. Keep geometry; combine only rigid same-material pieces if measured submission cost warrants it. |
| Lot foundations and fences | Many renderers for very few triangles. Candidate for small per-lot batching experiments; preserve separate collision and useful culling boundaries. |
| Snowmen | Three models, six renderers and 3,000 triangles. No geometry reduction justified by these counts. |
| Traction ramp | One ramp, two renderers and 2,668 triangles. Keep its low-cost smooth collision deck; low priority for decimation. |
| Roads, curbs and markings | 25 renderers and 2,532 triangles. Modest geometry; retain combined paint and current accesses. |
| Other authored scenery | Only eight renderers and 96 triangles; negligible geometry. |


## Performance suggestions, in priority order

1. **Measure the Android build on Galaxy A07 before reducing assets.** Target is 30 FPS / 33.3 ms per frame. Compare CPU, render-thread and GPU times, Rendering Profiler batches/SetPass, snow updates and GC in town, dense forest, depot and maximum snow pushing. This audit measures no frame-time savings; triangle percentages are not performance percentages.
2. **Interactive snow and Terrain first if geometry/updates dominate.** The category totals reveal how much mesh snow contributes; profile dirty-chunk rebuilds/colliders, Terrain pixel error and visible area. Consider smaller visual detail only after preserving the snow-volume ledger and blade/tyre interaction. Terrain’s full-grid reference must not be added as if it were rendered every frame.
3. **Batch rigid foundations/fences in small per-lot groups if CPU submission dominates.** They contribute only 3,840 triangles but 320 renderers. Preserve individual collision boxes and useful culling boundaries. Material sharing/SRP batching may already make some submissions cheap, so verify the device/Frame Debugger before choosing a rewrite.
4. **Forest: keep LOD3 and existing distance culling.** No broad decimation is justified solely by the current totals. Snowy Open Alpine Larch (14) is the heaviest variant at 1,315 triangles; compare its screen footprint and snow-cap topology before simplifying it. Test a lower density of the heaviest snowy variants or culling changes only if forest views are a measured bottleneck. Do not reintroduce the unused higher LODs.
5. **Repeated trees/homes/street lights: investigate submission and shadows.** Shared mesh/material repetitions are candidates for compatible instancing or modest spatial batching. Verify shader/URP/SRP Batcher compatibility and actual Frame Debugger results; a checkbox is not proof of batching. Avoid combining the whole forest into one giant mesh, which weakens culling. Distant tree/street-light shadow distance or casting can be a better experiment than removing triangles, subject to visual approval.
6. **Homes and machinery: inspect renderer/material fragmentation.** Current one-box home collision policy already reduces physics complexity; it does not reduce render work. Combine only rigid parts with identical materials in Editor, keeping snow caps, wheels, blades, doors and glow independently controllable. Check street-light meshes and home fences as repeated submissions, rather than removing their visible details first.
7. **Mountains and facilities: retain current low-poly geometry unless profiling says otherwise.** A skybox can provide scenery beyond the modeled ring, but triangle counts alone do not justify deleting the nearby mountains. The one-off ramp and service buildings are low-priority decimation targets.
8. **Transparency and lighting require a different budget.** Exhaust puffs, glows and bloom can cost fill-rate despite tiny triangle counts. Keep optional street lights gated, keep vehicle beam/shadow counts controlled, and measure overdraw with the camera close to smoke. No geometry-only estimate can price these effects.

No optimization has been applied. Suggested experiments require before/after device capture; there is no defensible FPS improvement estimate from this inventory alone.

## Evidence and repeat workflow

- `NativeInventory.json`: native saved-mesh measurements and render/culling/terrain context.
- `SceneRenderers.csv`: every measured renderer, owner, mesh, vertices, triangles, materials, active/enabled and shadow flags.
- `SceneCategories.csv`: category totals.
- `TreesByVariant.csv`: all 26 tree/wood variants, placements and totals.
- `TreesByPlacement.csv`: one row for each placed tree/log/stump.
- `CatalogAssets.csv`: all catalog prefab variants, including unused entries.
- `Validation.txt`: read-only state preservation result.

Repeat: **Snowglow > Audit > Saved scene geometry inventory (read only)**, then `python Tools/AssetIntake/report_geometry_audit.py`. Audit always measures the saved Pinefall scene, not unsaved Editor edits.

Unity references: [optimizing many objects](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/reduce-draw-calls), [GPU instancing](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/reduce-draw-calls/gpuinstancing/gpuinstancing), [profiling on target devices](https://activation.unity3d.com/how-to/best-practices-for-profiling-game-performance).
