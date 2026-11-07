# Snowglow mobile art budget

Current cloud policy (7 November): 10–15 active clouds, 3,320–4,980 triangles and
10–15 renderers before culling; one shared opaque material. Forty authored slots
retain inactive roots and scale baselines, so inactive-inclusive inventory still
differs from active geometry. Distribution spans a 1,500 m radius with staggered
upwind arrivals and radial cleanup. Earlier fixed-40 numbers below are historical.
Phone profiling remains required; see CLOUD_WIND.md.

## Wind-driven cloud lifecycle - 7 November 2026

The user's explicit cloud request now supersedes static-cloud behavior and the
general no-runtime-spawning restriction for cloud roots only. Existing 40 cloud
slots (five per imported variant) retain prepared prefab references and original
baseline scales. Editor randomization applies uniform 0.5-3x baseline size,
random yaw and full-mesh altitude within 250-450 m above town. Initial X/Z sky
coverage remains prefilled; later clouds spawn at the upwind plane. Repeated
authoring preserves baselines and does not compound scales.

CloudWindController uses a private RNG, 3-8 m/s horizontal wind, new random
heading/speed targets every 300-900 gameplay seconds, and a smooth 20-second turn.
Entry/exit planes sit 1,500 m upwind/downwind of centre (0,5,0), perpendicular to
current wind. Entries sample +/-900 m across the vertical plane and fit the full
scaled mesh inside the altitude band. Roots crossing the exit are disabled and
destroyed, then replaced from the saved same-variant prefab at a fresh entry.
Forty active clouds remain; Unity may defer retired inactive destruction to the
end of the frame. Off-course roots beyond ~2.1 km radius are also cleaned up when
wind turns would otherwise leave them drifting outside the flow volume. Runtime
creates no meshes/materials, colliders, lights, shadows or snow. This is visual
cloud wind only; vehicles, trees, smoke and the snow ledger are unaffected.
Gameplay pause/menu stops both movement and wind timers. State is session-only.

CloudWindAuthoring saves all settings/slots before Play and future explicit
AngularCloudsIntake reuses it. Native scene clouds remain connected prefab
instances; imported source meshes/materials/prefabs and the panorama are retained.
40 active clouds still total 13,280 triangles / 40 renderers / one shared opaque
material; larger screen coverage, movement and lifecycle costs are unmeasured.
Guide: CLOUD_WIND.md. Menu: Snowglow > Environment > Install wind-driven cloud
lifecycle. Evidence/backup/four horizon previews: Logs/CloudWind.

Unity 6000.3.8f1 / Android compiled, saved/reopened and passed 23,376 checks:
1,000 wind changes, thousands of entry-plane/height/size samples, pause gates,
movement, actual disposable-Editor-fixture destroy/replacement, fixed active count,
shared materials and repeat authoring. Two of four native sky previews visually
inspected. Scene validation and 19 snow checks passed. Protected mesh/Terrain/snow
hashes and all non-cloud/forest/fleet poses preserved; no player-save writes or
district rebake. Live Play/deferred lifecycle timing, Android packaging and phone
performance remain untested. Other deferred weather/night/snowfall work is not
implemented by this cloud-only change.


## Tripled hill-weighted forest - 6 November 2026

User requested 3x the current population before testing Android phones, with
hills > hilly slopes > outskirts. Pinefall now has 438 LOD3 tree/wood placements
(420 trees/snags, 18 stumps/logs), adding 292 to the audited 146. Every variant's
population is exactly tripled. All 146 prior poses/IDs/variants remain unchanged.
Distribution: 241 hills, 132 hilly slopes, 65 outskirts (about 55/30/15%). Hills
are Terrain >=15 m above town with slope <12 degrees; otherwise >=5 degrees is
hillside, with remaining outer terrain classified as outskirts. These are Terrain
hills, not placements inside the imported mountain meshes. Existing 365 m
root/full-bounds radius, 3 m mountain exclusion and near-side visibility rules,
terrain support/visual burial limits, slope-aware rotation, one solid box per
instance and 230 m render-distance culling remain. No higher LODs or new materials.

Forest geometry: 479,481 imported vertices / 187,488 triangles / 657 renderers.
Scene mesh inventory: 2,107,459 vertices / 1,000,184 triangles / 1,749 renderers,
before camera culling and excluding Terrain/particles/UI. These are not draw
calls, visible-frame counts or performance timings. The prior 146-instance audit
is preserved at Reports/GeometryAuditBaseline_146; Reports/GeometryAudit is current.

Unity 6000.3.8f1 / Android compiled, saved/reopened Pinefall and passed 2,851 checks
plus full terrain seating validation, scene validation and all 19 snow checks.
Native fog-free overview inspected. Snow mask changes: zero; no new seed needed.
Original sources/meshes/Terrain/snow quantities and all prior catalog/fleet poses
preserved. No district rebake, player-save writes or runtime spawning. Actual
driving, Android packaging and phone performance remain untested. Menu:
Snowglow > Environment > Triple forest with hills first. The former half-expansion
menu forwards to this density policy when triple placements exist, avoiding
further multiplication. Evidence/backup/placement CSV/overview: Logs/ForestTriple.


Home collision simplification (6 November): 40 placed home/foundation assemblies
now use 40 solid full-height boxes instead of 230 colliders. Separate fence shapes
remain. Seven catalog prefabs each use one box, removed as a scene override when
the foundation supplies collision. Geometry/render costs unchanged; physics savings
are unmeasured on Galaxy A07. Roofs/porches inside each box are intentionally solid.

Traction-test ramp replacement (6 November): 2,668 visible triangles, two renderers
and two shared materials (one small opaque palette plus existing toon snow).
Two supplied static collision meshes total 24 triangles. No lights, transparency
or runtime assembly added. This is a modest one-off fixture, but actual
WheelCollider driving and Galaxy A07 frame time remain unmeasured.

Isometric presentation revision (5 November): twelve rear warning faces reuse
two source-derived meshes and the existing opaque palette. Added cost is 3,654
triangles/twelve renderers, no new textures/materials/lights/colliders. Rear
additions do not cast shadows. Native scene/render checks passed on the Windows
Editor target; Galaxy A07 costs remain unmeasured.

Permanent colored lamp housings add four boxes / 48 triangles to tractor and
six boxes / 72 triangles to loader. Orange front and white rear materials are
shared across the fleet. Housings stay visible when lamps are off; no colliders
or additional lights. These counts exclude the existing glow/lens geometry and
do not establish device frame time.

Lights toggle/glow revision (4 November): four tractor and six loader lamp halo
quads join the existing one-per-machine beacon halo. These are small additive
billboards, enabled with that machine's light switch. Beacon glow remains enabled
independently during operation. Front spot intensity is 10 rather than 7; there
are still only two enabled shadowless spots on the selected machine. Phone
profiling must include these small transparent surfaces; geometry counts do not
establish their overdraw cost.

Operating vehicle lamps (4 November): two shadowless 22 m spotlights on the
selected operating machine; parked machinery has no enabled beams. URP now
allows two additional lights per object, previously zero. Three extra opaque
lamp-surface renderers are enabled on that machine; rear/beacon surfaces add
no point lights. Native ground-illumination previews passed, but A07 cost and
daytime readability across all gameplay cameras still need device validation.
The subsequent beacon revision adds one small additive halo quad per vehicle,
enabled only with that machine's operating lights, plus a rotating opaque red
lens shader. No additional point lights. Paired lamp groups use one shared
material slot each; phone profiling remains required.

Reference: Samsung Galaxy A07, Android URP, 30 FPS (33.3 ms/frame). These are
conservative starting budgets for this mid low-poly game, not device limits or
evidence that most phones run it smoothly. Profile representative older/lower-end
phones and the A07 with snow simulation, UI, shadows and saving enabled.

| Content | Suggested working target | Review trigger |
| --- | --- | --- |
| Complete nearby vehicle, including wheels and equipment | 5,000–10,000 triangles | Above 15,000 triangles or 30,000 imported vertices |
| Small scenery prop | 50–1,000 triangles | Repeated instances or tiny screen footprint justify less detail |
| House/building assembly | 500–3,000 triangles | Above 5,000; consider repeated windows/trim and LOD |
| Vehicle materials | 1–4 shared materials, preferably one opaque palette plus opaque toon windows | Above 12 material slots; count slots across every renderer |
| Vehicle renderers | About 6–10 for separate moving modules | Many tiny renderers; combine rigid parts in the Editor |
| Visible scene geometry | Initially aim around 100,000–200,000 triangles | Includes snow/terrain; count extra shadow passes separately |

Numerical targets above are Snowglow recommendations. They are advisory: screen
coverage, transparency/overdraw, shadow passes, shader cost, CPU draw submission,
mesh updates and physics can dominate at a low triangle count. Use Unity's device
Profiler and Frame Debugger for actual timings and submissions. The SRP Batcher
reduces submission/setup work; it does not make every material slot a single draw.

The seven saved winter-home prefabs include detachable roof/ground snow:

| House | Unity triangles | Imported vertices |
| --- | ---: | ---: |
| Red Cottage | 1,724 | 3,566 |
| Green Porch | 1,944 | 3,960 |
| Garage Home | 1,828 | 3,762 |
| White Colonial | 1,728 | 3,568 |
| Storybook Cottage | 1,748 | 3,600 |
| Twin A Frame | 1,002 | 2,078 |
| Brick Barn | 2,848 | 5,820 |

Each is within the project's 500-3,000 building working band. No decimation was
needed. Four renderers/material slots per house use three shared materials across
the set (opaque palette, unlit warm windows and snow). Six exports contain eight
zero-area architecture triangles each, verified in Blender; Unity removes them.
Export metadata records these separately so import checks compare visible geometry.
Unity prepared and saved all seven and reopened their Pinefall placements on
29 October 2026 (`Logs/WinterHomes/UnityIntake.txt`). These counts do not establish
Android frame times, overdraw, draw submissions or sustained phone performance.

Five alpine mountains use 690 / 726 / 1,016 / 930 / 974 triangles and
1,524 / 1,601 / 2,380 / 2,041 / 2,247 Unity-imported vertices respectively
(Needle Peak, Twin Summits, Sawtooth Ridge, Glacier Massif, Alpine Cirque).
Each has two renderers/material slots, sharing rock and snow materials across the
set. Twelve saved placements total 10,088 mesh triangles, before shadow passes.
Geometry is modest for background assets and did not require decimation. Terrain,
view coverage, fog and shadow costs still need device profiling; these counts are
not frame-time or draw-submission measurements.

The new loader is within the vehicle geometry target: 7,656 vehicle triangles,
8,424 including its preauthored carried-snow visual. The complete prefab has
15,095 imported vertices, eight renderers and nine material slots / three distinct
materials. The tractor is also within the geometry target (7,274 triangles), but
its 30 material slots should be consolidated into a shared palette in a future
optimization pass. No triangle decimation is justified for these vehicles yet.

At the saved depot camera, an Editor frustum-bounds inventory found 69,770 mesh
triangles, including 54,432 snow triangles, and 69 material slots. This is only a
candidate inventory: it excludes Terrain, UI, particles and shadow passes, and
does not model batching or occlusion. Other camera positions and tall snow piles
can cost more. It is not a rendered-frame or FPS measurement.

Future catalog vehicle checks now warn above 15,000 triangles / 30,000 imported
vertices. Existing material-slot warnings remain active. Recheck whole equipped
vehicles: reviewing body, wheels and equipment separately can hide their total.
Both vehicles now use opaque toon windows to avoid transparent window blending
and sorting. Use LOD/culling for distant/repeated assets, limit transparency, and
avoid adding tiny unseen bevels/bolts. Preserve each original source before any
optimization; author prepared meshes and proxies in the Editor.

Primary guidance: [Android geometry optimization](https://developer.android.com/games/optimize/geometry),
[Unity mobile graphics/art optimization](https://unity.com/how-to/mobile-game-optimization-tips-part-2).

The 30 October residential expansion has 40 homes: 72,552 house mesh triangles
and 160 house renderers, before foundations/fences/shadow passes. Original
16 homes remain; ten blocks contain 2, 4 or 6 homes each. Native Editor count and
clearance checks passed; profile the denser layout again on A07 before judging
mobile frame times. No new per-house materials, textures or real-time lights.

The twelve alpine street-light prefabs use three shared opaque materials and
three renderer groups each. Native imported geometry (including removable snow):

| Fixture | Triangles | Unity vertices |
| --- | ---: | ---: |
| Path Bollard | 228 | 396 |
| Park Globe | 600 | 1,092 |
| Compact Village Lantern | 630 | 1,128 |
| Heritage Single | 630 | 1,128 |
| Heritage Twin | 1,084 | 1,980 |
| Wall Lantern | 414 | 796 |
| Alpine Hanging Lantern | 684 | 1,244 |
| Heritage Triple | 1,290 | 2,384 |
| Residential Curved LED | 438 | 780 |
| Main Road Outreach | 438 | 780 |
| Dual Median LED | 604 | 1,092 |
| Four Way Area Light | 936 | 1,724 |

Fourteen scene fixtures total 8,804 triangles / 42 renderer-material slots, before
shadow passes. Twin/Triple heritage lamps slightly exceed the small-prop 1,000
triangle working target because they have multiple heads; no decimation was needed.
All 21 placed optional point lights are inactive by default and shadowless when
enabled; use their small proposed ranges deliberately and profile night scenes.
Opaque unlit luminous surfaces avoid transparent glazing. Native save/reopen and
structural checks do not establish A07 frame times or a safe active-light count.

The 26 alpine tree/wood prefabs use two shared opaque materials across the set,
one body renderer and one additional renderer on supplied snowy variants:

| Variant | Triangles | Unity vertices |
| --- | ---: | ---: |
| Tower Spruce | 904 | 1,590 |
| Broad Fir | 694 | 1,224 |
| Slender Spruce | 942 | 1,600 |
| Irregular Drooping Fir | 780 | 1,338 |
| Wind Bent Pine | 646 | 1,130 |
| Open Alpine Larch | 1,966 | 3,460 |
| Young Fir | 426 | 724 |
| Spruce Sapling | 388 | 650 |
| Snowy Tower Spruce | 1,290 | 2,280 |
| Snowy Broad Fir | 981 | 1,745 |
| Snowy Slender Spruce | 1,361 | 2,303 |
| Snowy Irregular Drooping Fir | 1,100 | 1,882 |
| Snowy Wind Bent Pine | 893 | 1,567 |
| Snowy Open Alpine Larch | 2,633 | 4,523 |
| Snowy Young Fir | 647 | 1,121 |
| Snowy Spruce Sapling | 576 | 986 |
| Tall Dead Snag | 1,054 | 1,988 |
| Thin Bare Snag | 874 | 1,628 |
| Broken Alpine Trunk | 750 | 1,394 |
| Bare Young Sapling | 562 | 1,028 |
| Low Cut Stump | 220 | 357 |
| Tall Cut Stump | 220 | 357 |
| Split Cut Stump | 252 | 417 |
| Snow Capped Stump | 223 | 362 |
| Fallen Cut Log | 232 | 300 |
| Snowy Broken Log | 318 | 464 |

Ninety-seven placed forest assemblies total 96,243 triangles / 145 renderer-material
slots, versus 454,736 / 679 in the original primitive forest. Existing 230 m distance
visibility is retained. These are whole-placement inventories, excluding extra
shadow passes, and do not establish active-frame costs or A07 performance. The
Larch / Snowy Larch are heaviest (1,966 / 2,633 triangles, placed 2 / 8 times);
review these in device profiling before increasing forest density. No transparent
foliage cards, per-tree material copies, wind animation or per-instance mesh copies.

Orbit/Scenic camera modes (30 October) use the same single gameplay Camera and add
no environment geometry or enabled lights. Camera safety uses bounded, nonallocating
sphere sweeps and Terrain samples. Wider/rotating viewpoints can reveal more assets
and shadows than the original camera, so the existing geometry counts do not predict
A07 timings. Editor input/terrain checks and three native renders passed; profile
all four views during actual driving and snow work on the phone.

The roadside fixture expansion adds 14 prepared lamps (28 total): +8,810 triangles
and +42 renderers/material slots. Total fixture geometry is now 17,614 triangles /
84 renderers, sharing the existing materials. New optional point lights remain off;
there is no enabled real-time light-count increase. Native save/reopen, clearance
and snow-accounting checks passed. Counts do not establish device frame-time cost.

Before density expansion, supplied LOD3 reduced the 97-instance forest from
96,243 to 40,939 triangles / 145 renderers (57.46% fewer triangles). Native
mesh references and saved/reopened scene verified. No LOD0/1/2 from the new pack
are imported and no LODGroup switching is added. Material slots/culling are
unchanged; render-submission and FPS improvements are not measured. See
SourceAssets/AlpineTreesLowestLOD/README.md for per-variant LOD3 counts.

4 November 2026 density/seating update: current forest is 146 instances,
62,496 triangles / 219 renderers, still LOD3 only. The 49 additions contribute
21,557 triangles. Terrain seating and conservative mountain exclusion passed native
save/reopen checks. Geometry counts are not measured A07 performance.

Alpine sign boards (4 November 2026): 12 variants, 200-1,037 triangles each
(4,981 for one of each), including source lettering and decorative snow. Sixteen
Pinefall instances total 6,117 mesh triangles / 39 mesh renderers, plus five
destination TextMesh labels. Two shared opaque materials (palette and existing
snow); no new lights. Envelope colliders need handling review. Android native
Editor checks do not establish A07 frame-time or touch-readability acceptance.

Latest curated signs: 31 scene instances across nine of the 12 catalog variants,
14,584 prepared mesh triangles including source lettering hidden by scene caption
overrides. Twelve road closures and twelve boundary warnings account for 11,340
triangles. Custom destination TextMesh captions are additional. Native scene and
snow checks are not A07 frame-time or live touch-readability measurements.

Four-bay garage (5 November): 3,832 triangles / 10 renderers, shared opaque
palette and existing toon snow material. Preview ground excluded; four doors
remain separate. Static mesh collision requires mobile handling/performance
review. Native preview counts are not Galaxy A07 frame-time measurements.

Snow disposal yard (5 November): 972 triangles / three renderers / two shared
materials, excluding studio ground. Static shape colliders; no new realtime
lights. Replaces primitive pad/walls/markers, with removable decorative snow.
Native render/structural checks passed; A07 frame-time cost remains unmeasured.

Fuel station (5 November): 3,040 prepared triangles / three renderers / two
shared materials; source 3,088 minus 48 zero-area faces. Body, lenses and
decorative canopy snow separate. Static mesh collision, no added realtime lights.
Native preview/structural checks passed; physical A07 performance unmeasured.

Three snowmen (5 November): 1,000 triangles/two renderers each; 3,000 triangles
and six renderers total. Two shared materials, static body capsules, no lights
or runtime effects. Native scene checks/previews passed; A07 cost unmeasured.
