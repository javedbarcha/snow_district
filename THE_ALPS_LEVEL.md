# The Alps district

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
