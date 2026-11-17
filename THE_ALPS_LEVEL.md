# The Alps district

The 17 November supplied-source rebuild supersedes the original generated layout below. See [ALPS_REBUILD.md](ALPS_REBUILD.md): 70 imported homes, a connected twelve-route network, supplied 1 km terrain, sixteen surrounding mountains, imported Tell statue and gate tower, and Pinefall facilities. The current scene is saved/reopened; 2,195 static access samples and 17 isolated Play assertions passed. Evidence is in Logs/AlpsRebuild. Original source assets and Pinefall remain preserved.


The next Alpine Career town after Pinefall. Reference: the existing `alps.jpg`
card, with a Swiss-style old town, gate/clock tower, Tell and son monument,
plaster/shuttered buildings and snow-covered ridges. This is an original stylized
interpretation, not a survey or a replica of Altdorf.

Open `Assets/Scenes/TheAlps.unity` for direct scene editing. The terrain is
1000 x 1000 metres, centred on world origin. The Alps owns its TerrainData,
snow seed and snow meshes under `Assets/Snowglow/Levels/TheAlps`.
Pinefall remains the first build scene. Career unlocks The Alps after all three
Pinefall contracts; Quick Job includes unlocked Alps work. Vehicles, ownership,
parts, fuel, condition and wallet travel between scenes. Completed Career snow
uses the existing town-keyed checkpoints; an explicit replay reseeds its venue.

The flat valley floor holds the three jobs (Depot Access, Parking Lot, Main
Street), using the current common contract coordinate layout. The surrounding
mountain roads climb beyond the plowable simulation grid. Their snow blanket is
decorative: extending deformable snow to slopes needs a separate physics change.
The municipal depot, fuel stop and disposal yard retain the existing service and
recovery coordinates. The factory machinery, workshop, HUD, cloud flow and
job guidance are reused; architecture, town scenery and terrain are authored
separately.

`AlpsTownAuthoring.cs` is the deterministic Editor source, with no runtime scene
factories. It refuses to overwrite an existing Alps scene. The first build marker
is `Logs/TheAlps.request`, queued only after an ID-matched native Compile success.
Backups, native captures and validation evidence are under `Logs/TheAlps`.

Device performance, manual mobile handling and visual polish remain separate
acceptance; native automated checks cannot establish phone quality.

The authored town includes 28 old-town buildings, 9 hillside chalets, a Tell and
son monument, an arched Swiss clock tower, 10 fractured mountain ridges and
375 existing spruce/wood variants after clearing chalet footprints. Thirty-nine
new building/landmark assemblies are retained as reusable prefabs and NeedsReview
catalogue entries with provenance; nothing is automatically marked Ready.
Decorative meshes are merged by material and 6,944 initially empty transforms
were removed. This does not establish Android performance acceptance.

Cross-town job startup uses the additive saved `pendingTownStart` flag, consumed
only after initializing the destination's own snow. This makes new/replayed venue
seeding explicit across JSON and application restart, without relying on a null
custom-class field. Departing sessions stop saving before scene replacement.
Active ordinary resumes still restore their complete saved snow grid.

`Logs/TheAlpsChecks.request` runs an isolated native Play test through both towns.
Its profile is serialized only into Editor SessionState. No real profile is
replaced for tests. The explicit `regress` authoring action runs the established
Pinefall UI/flow/Career snow/ledger/recovery fixtures without scene writes.

Final verification: Android-defined C# compilation and ID-matched native forced
compile/reload passed. Native save/reopen, UI bindings and road vertex/face-centre
clearance checks passed; the road meshes contain 52,444 collision triangles.
Two-way Play travel, coasting/support, Career/Quick separation and resumed physics
passed 17 assertions. The shared Pinefall regression suites passed again after
the final changes. These use synthetic venue clearance for progression checks;
they do not establish manual plowing of all jobs or exhaustive hill handling.
Pinefall and protected original seeds/saves match their initial hashes. New scene,
terrain and snow seed were unchanged by Play. A subsequent scene-only visual LOD
override (heightmapPixelError 1) is the exact sole post-Play scene edit, reducing
far terrain showing through roads; the first-build generator defaults to 10, and
finish/review preserve saved scene overrides. Phone rendering cost remains untested.


## Shared Pinefall scenery

`AlpsBorrowedScenery` is a targeted Editor installer; it never rebakes the town
or edits Pinefall. After native compilation succeeds, queue
`Logs/AlpsBorrowedScenery.request` containing `apply` for the first install,
`inspect` for the source/old-tree audit, `access` to finalize the paved north
yard connection after the first install, or `review` for saved-scene checks and
captures. Apply refuses a second install rather than duplicating scenery.
Backups and evidence live in `Logs/AlpsBorrowedScenery`.

The source scenery is reused by reference: detailed four-bay garage, fuel stop,
disposal yard, heritage-style street lamps and destination signs. Garage gates
use an Alps-only red swatch derivative of the source palette; other door colours
and Pinefall remain unchanged. The borrowed garage now uses its original roof and snow cap, without the added
Alps pitched roof. The user superseded the hybrid roof after noticing fit/render
problems. Added signs use Alps captions.

Trees retain their existing models, scale, colliders and 200m / 2km geometry/card
settings. Placement uses transformed mesh-base support points against actual
saved terrain, with upright trunks and clearance from roads, buildings and
mountain silhouettes. The previous generator incorrectly used root-local bounds
as world bounds, adding terrain height again to already raised source poses.
The retained generator and polish seating now use the corrected helper too.

Obstacle changes use a separate Alps snow-seed revision: only blocked flags are
reconciled; depth, credit and snow volume remain intact. The original Alps seed
and all Pinefall source assets are retained. Android and manual driving acceptance
remain distinct from native saved-scene checks.

Native checks passed after saving/reopening: 374 supported upright trees,
18 lamps, five boards and the three detailed facilities. Sampled 3.6m-wide
service/yard corridors are clear. The road mesh audit and all 17 isolated Alps
Play assertions passed again. Pinefall/shared assets, original Alps assets and
real profiles retain their protected hashes; the Alps scene did not change in
Play. Depot/Yard/Hills/Overview captures are under Logs/AlpsBorrowedScenery.
These checks do not establish Android performance or exhaustive collision feel.


Roof correction: `Logs/AlpsBorrowedScenery.request` containing `roof` removes the
added pitched roof pair and enables the borrowed garage's original snow cap.
It preserves unsaved/disk scene backups under Logs/AlpsGarageRoof and compares
all shared mesh references and root-relative mesh transforms with Pinefall.
The initial installer now uses the original borrowed roof too. Red gates and
all other facility/tree placements remain. No runtime or collision change.
