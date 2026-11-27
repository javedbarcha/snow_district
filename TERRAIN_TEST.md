# Empty Alps terrain driving test

## Building surface repair - 19 November 2026

All 75 map buildings and the landmarks use shared test-only FBX derivatives. Inward facade/window/tower-art panels face outward, and roof snow clears the underlying slopes. All 11 source building variants and both landmarks were reviewed from four sides, plus actual gameplay isometric views. Town placement, terrain/mountains, original catalogue assets, materials and triangle counts remain unchanged. Evidence and before/after views: Logs/AlpsBuildingFaces. Play remains stopped; no handling changes or Play rerun.

## Current scene: supplied-map town

AlpsTerrainTest now includes the supplied 400 x 400 m town plan around the existing tractor. Terrain and eight fitted mountains remain unchanged. The tractor starts on the depot apron. Play directly; W/S or arrows drive, A/D steer, Space brakes, R recovers to the apron, and C cycles cameras. The standalone test still has no production menus, jobs, deformable snow or save access.

The town has 75 numbered buildings plus depot, disposal/service facilities, statue/tower, 74 lamps and 30 signs. Road widths and building/front placements follow the supplied map. Source input/layout: SourceAssets/AlpsTownMap. Assets: Levels/AlpsTerrainTest/TownMap. The building-file extra clock tower is imported but unplaced. Existing terrain, mountain placement, shading, machinery handling and game scenes stay preserved.

Top/iso overview and per-building gameplay-camera captures: Logs/AlpsTownMap. Native layout, save/reopen and a short actual driving/support/coasting/reset check passed. Full-town driving and phone performance remain for review. Historical sections below describe earlier isolated test stages; the town is now deliberately present.

## Town sign layout

The test town uses 30 prepared Alpine sign prefabs without editing their source
blend or catalogue assets. Ten supplied-map sign identities and X/Z placements
are retained, with a mix of directory, timber, noticeboard and hanging-board
variants. The central four-board directory reads UPPER VILLAGE, TOWN SQUARE,
SNOW DEPOT and SOUTH ENTRY. Sixteen Alpine Danger boards line the town boundary
and face inward; four Road Closed boards mark the north, south, east and west
road termini and face approaching traffic.

The Editor-authored layout was saved and reopened in Unity 6000.3.8f1. Validation
checks the board count/variants, captions, terrain seating, asphalt/static-collider
clearance, retained map-sign direction and unchanged non-sign catalogue placements.
Rendered evidence and the correction backup are under Logs/AlpsTerrainTestSigns.
This is Editor render/scene evidence only: no Play, Android build or device test.
The one-time authoring request is `Logs/AlpsTerrainTestSigns.request` with `apply`;
it refuses to install twice. The already-installed scene also has a guarded
`correct-facing` action; do not rerun either action on the corrected scene.

Open `Assets/Scenes/AlpsTerrainTest.unity`. The scene contains the supplied
terrain, one tractor, camera/light/control helpers and the authored town
(buildings, roads, mountains, facilities and signs). The invisible TerrainData is
used only for camera-height queries; the supplied mesh provides physical
collision. Original 1:1 source metres are used, with no Alps game-scene height
compression. Jobs, production menus, deformable snow and save access are absent.

Editor Play is configured to start this scene directly. To restore ordinary
Editor Play, choose **Snowglow > Developer > Play current scene normally**.
Choose **Play terrain test directly** to enable the override again. The game's
build scenes are unchanged. These menu choices are Editor settings.

- W / Up: forward pedal
- S / Down: reverse pedal (existing stop/direction buffer)
- A / D or Left / Right: steering
- Space: brake; releasing pedals coasts
- R: return tractor to test spawn and reset motion
- C: cycle Orbit / Isometric / Follow cameras
- Orbit view: right-drag and scroll, or J/L, I/K and +/-

The sandbox does not create a PrototypeSession, load a profile or save progress.
The raised blade, stock parts, full fuel and full condition start fresh each Play.
It calls the same MachineController driving step as normal gameplay, with no
snow field. Normal gameplay retains its original session gates and snow logic.

The revised attachment `Snow_District_Alps_Town_Terrain (1).blend` is preserved
as `SourceAssets/AlpsTerrainTestRevision/Terrain.blend`, with its checksum and
export metrics in `Provenance.json`. Its separate FBX and palette are under
`Assets/Snowglow/Levels/AlpsTerrainTest/Revision2`. The 1000 x 1000 m terrain
retains its original -35 to 70 m source heights, with no height compression.
The original terrain assets remain available; only the test scene is replaced.
Camera heights are sampled from the revised physical mesh and the test tractor
is reseated at the centre. Export uses Blender with autoexecution disabled.

Authoring is Editor-only: after native compilation succeeds, queue
`Logs/AlpsTerrainTest.request` with `build` once, or `play` for the isolated live
checks. `replace` installs the prepared Revision2 terrain once and refuses a
duplicate installation. Build refuses to overwrite an existing test scene. Evidence and backups
are under Logs/AlpsTerrainTest. This scene is for PC Editor terrain testing;
mobile controls, snow work and Android performance are outside this test.
Replacement backups and captures are under Logs/AlpsTerrainTestRevision.

Native scene save/reopen, existing direction/recovery checks and actual Play
startup/support/acceleration/coasting/reset checks passed. Protected game scenes,
build settings, source assets, tractor prefab and real profiles are unchanged.
Before running another Editor Play fixture, clear the test-scene startup override
and restore it afterward if you want to keep using this sandbox.

## Provisional mountain review

Seven mountain instances occupy the marked NW, N, NE, W, SE, SW and S perimeter
areas. They reuse the prepared models exported from the exact attached
`Snow_District_Alps_Mountains.blend` (matching SHA256 in the original intake
manifest). Each has a uniform scale and yaw to suit its marked footprint, with
its base initially placed at the terrain height of its centre. Seating is
deliberately pending: no terrain sculpting, skirts, mesh refits or seam blending.
The added mountains have static collision using their visible shared meshes.

Test-only derivative palettes replace dark rock swatches with icy blues on the
terrain and placed mountains; snow swatches and all source textures/materials
remain intact. Assets live under `MountainPlacement`. Editor-only installer:
queue `Logs/AlpsTerrainTest.request` containing `mountains` after successful
native compilation. It refuses duplicate placement. Original scene backups,
before/after rock views and placement renders are under Logs/AlpsTestMountains.

The subsequent southeast gap addition uses the previously unused fractured rock
buttress (Mountain_07), making eight mountain instances. `mountaingap` is a
separate one-time action: it uses the current Editor scene, backs up unsaved user
edits and checks all seven existing mountain poses before/after saving. It does
not rerun the original placements. Gap evidence: Logs/AlpsMountainGap.
The screenshot views from the east, so its near-left gap is southeast in world
coordinates. The added instance is at x=380, z=-330. Existing placements are
checked from the current in-memory Editor scene, including unsaved user changes.

## Mountain seating and final dark swatch

All eight mountains now have Y-only seating adjustments. Their source slope/base
transition rings are sampled every 2 m or less and buried 0.5 m beneath the
actual terrain, hiding the original short lower walls. Horizontal positions,
rotations, scales and shared meshes stay as authored by the user. This lowers
some visible mountain heights; terrain and mountain geometry are unchanged.
The remaining dark green rock swatch on the rounded mountain is replaced by
icy blue in a new test-only palette/material under `MountainSeating`.

The one-time `mountainseat` action preserves the current in-memory scene and
refuses repeat application. Queue only after native compile exits zero.
Saved/reopened constraints, individual renders, overview, exact Y changes and
protected file hashes are in Logs/AlpsMountainSeating. All rim samples lie within
the terrain. No mesh refitting, terrain sculpting or gameplay handling changes.


## Test mountain bases fitted to unchanged terrain - 18 November 2026

User permits up to10% burial and explicitly approves fitting base geometry after an audit showed0/8 mountains could meet that limit with Y translation alone. TestMountainClearance `conform` works on the current in-memory AlpsTerrainTest, backing it up before changes and refusing repeat fitting. It creates eight derivative meshes under Levels/AlpsTerrainTest/FittedMountainBases; original source/import/prefab meshes stay unchanged. Welded buried vertices are raised to physical terrain minus .5m; true open-bottom rim edges sample every <=1m and are lowered locally where required to prevent gaps. Already exposed vertex positions, all root XYZ/rotation/scale, UVs, materials and terrain remain. Matching static MeshColliders use the same saved fitted meshes. No added/removed triangles or vertices:11819 triangles total.

Native forced compile/domain reload request cd031da0e1684eda813a2dc6c5996e96 exited0. Native save/reopen, perimeter and vertex/triangle-centre/edge-midpoint checks passed for all8: maximum sampled burial .597-1.436m (0.52-2.80% original height), minimum perimeter burial .25-.434m. Overview and16 base-level views reviewed; some views have foreground hill/mountain occlusion, so geometric sampling is the clearance evidence. All3290 protected file hashes match, including source/catalogue/prefabs, terrain/materials, production scenes and real saves. Exactly eight prefab-instance records gain render/collider mesh overrides; no scene objects or transforms change. Evidence/backups: Logs/AlpsMountainBaseFit; original audit: Logs/AlpsMountainClearance. Submit atomic Logs/AlpsMountainClearance.request `conform` only after successful native compile; do not rerun old source replacement/Y-seating migrations on this fitted scene. Test scene reopened, Play stopped. No Play/hill-driving or device performance acceptance.
