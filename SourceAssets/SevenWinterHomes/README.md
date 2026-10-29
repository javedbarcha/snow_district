# Seven winter homes intake

Supplied 28 October 2026: `Seven_Winter_Homes.blend`.
Original and preserved copy SHA256:
`2AD3BEC380E5EE05A400561C0E9CA5445607C4D389EBFBF46EF4E4DECB9F8CDE`.
The source stays outside Assets to avoid implicit Blender import. Embedded
`START HERE` text was treated as source data and was not executed.

All seven named house collections were inspected. The presentation collection,
per-house cameras, collection movement empties, labels, square display foundations
and ground tiles are excluded from the prepared models. Building geometry and
source colors are retained. Evaluated rigid parts are combined into four groups:

- Architecture: shared opaque URP Simple Lit material, point-sampled color palette.
- WarmWindows: opaque URP Unlit material using the same palette; no real-time lights.
- RoofSnow: the project's white-top/pale-cyan snow material.
- GroundSnow: independently removable drifts, clipped/capped at ground level on
  prepared copies so their buried bases do not lift the entire house off the ground.

RoofSnow and GroundSnow are under `SnowCover | removable decorative snow` in the
prepared prefab. Disable that parent or either child in the Editor to remove snow.
This decorative mesh snow is not part of the authoritative plowing volume grid.
No roof clearing, falling roof snow, live house assembly or runtime spawning is added.

| Catalog ID | House | Prepared triangles (including both snow groups) |
| --- | --- | ---: |
| winter-home-01 | Red Cottage | 1,732 |
| winter-home-02 | Green Porch | 1,952 |
| winter-home-03 | Garage Home | 1,836 |
| winter-home-04 | White Colonial | 1,736 |
| winter-home-05 | Storybook Cottage | 1,756 |
| winter-home-06 | Twin A Frame | 1,002 |
| winter-home-07 | Brick Barn | 2,856 |

Each has four renderers/material slots, three shared materials across the set,
an Entrance anchor and compound wall/deck/step BoxCollider proxies. No transparent
glass, interior, physics-driven door or real-time window light is inferred.
The original .blend retains all separate source parts for further editing.

`Tools/AssetIntake/inspect_winter_homes.py` writes `Inspection.json`.
`Tools/AssetIntake/export_winter_homes.py` writes seven FBXs, palette, metrics,
coordinate reference frames and the complete included/excluded part map under
`Assets/Snowglow/Catalog/Sources/SevenWinterHomes`. Always run with Blender
`--background --factory-startup --disable-autoexec --python <script>`.
Never save over the original while preparing exports.

Editor command: **Snowglow > Asset intake > Import seven winter homes**.
Open saved Pinefall with Play stopped. The command prepares Needs Review catalog
entries and fills all 16 house lots with a balanced shuffled mix of the seven
variants. Houses are 1.5x the former lot-fitted size; foundation footprints expand
horizontally to match while height/ground level and fences remain unchanged. It saves/reopens the scene, checks bindings
and the snow seed, runs the 19 snow ledger checks and renders seven Unity previews.
Repeated installation preserves the saved shuffled layout without cumulative scaling.
Every variant is used at least twice; no primitive cottages remain in Pinefall. A future full district bake uses the prepared catalogue set.

Check a fresh `Logs/WinterHomes/UnityIntake.txt` to establish actual installation and
verification status. Source renders are `Logs/WinterHomes/NN-source.png`; prepared
Unity previews are `NN-unity.png`. A pre-intake scene backup is kept in that folder.
The entries remain Needs Review until visual/clearance and performance approval;
source appearance and low triangle counts do not establish Galaxy A07 frame times.

Completed in Unity 6000.3.8f1 / Android on 29 October 2026: all seven catalog
prefabs and Pinefall placements saved/reopened; scene checks and 19 snow-ledger
checks passed. Imported triangle counts are eight lower than the export table for
each house except Twin A Frame: Unity removes eight zero-area architecture
triangles, verified by Blender round-trip inspection and recorded separately in
ExportMetrics.json. Actual counts and imported vertices are listed in
../../MOBILE_ASSET_BUDGET.md. No original geometry was edited for this correction.

Wall normal repair (29 October): Red Cottage right wing, Garage Home living wing
and White Colonial cross wing had reversed closed shells. The exporter now
recalculates outward winding per closed source solid before merging, while leaving
open facade panels unchanged. All 958 closed solids and exported FBX shells pass;
see Catalog/Sources/SevenWinterHomes/NormalValidation.json under Assets/Snowglow.
The original Blender file and all geometry counts remain unchanged. Unity reimport,
scene save/reopen, structural validation and 19 snow-ledger checks passed again.
