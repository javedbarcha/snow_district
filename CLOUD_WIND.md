# Wind-driven clouds

CloudPalette uses `Snowglow/Cloud Sky Unlit` (CloudSky.shader) to keep its palette
visible beyond the world's 650 m fog end. This bypasses fog for clouds only;
terrain, town and global fog remain unchanged. Normal depth/occlusion by geometry
and the camera frustum remain. Evidence: Logs/CloudFog/Validation.txt, contrast
comparisons against the fog-coloured sky and full-scene Gameplay-clouds.png.

Cloud visibility: layer 10 (Clouds) bypasses per-layer distance culling, with a
4 km gameplay-camera far envelope. Other layers retain their previous effective
limits (normally 1 km) and trees keep 230 m DistanceVisibility. Saved
CloudCameraVisibility reapplies layer distances on enable; cloud prefab layers
also cover future spawns. Normal view-frustum clipping remains. No population or
placement change. Evidence: Logs/CloudVisibility/Validation.txt.

## Current circular distribution (7 November 2026)

This supersedes the fixed-40/plane policy retained below as history. Startup now
randomizes **10–15 active clouds** through a **1,500 m horizontal disk**, using
radial strata and jittered angles, with one root within 150 m of the centre.
The radius measures cloud roots; mesh edges can extend across the distant edge.
Clouds are destroyed when roots cross the radius in any direction. New arrivals
sample the current upwind circular arc, 2 m inside the edge, with random crosswind
positions. Existing wind turns smoothly bend their paths.

Arrival opportunities occur every **5–20 gameplay seconds**. The target population
is independently reselected within 10–15 every **30–90 seconds**. Targets below the
current count allow natural departures rather than deleting interior clouds.
Departures above the minimum do not force immediate replacements; only falling
below 10 triggers immediate replenishment. Movement and both timers pause with
gameplay. These timers are distinct from the unchanged 300–900 s wind changes.

The 40 authored slots retain original scale baselines and prefab references;
only 10–15 roots render. Unselected saved roots are inactive as a bounded reservoir
until first needed at an entry point. Expired roots are destroyed and subsequent
replacements instantiate the saved prefab. Active geometry is **3,320–4,980
triangles / 10–15 renderers**, before culling, sharing one opaque material.
Uniform 0.5–3x size, full-mesh 250–450 m altitude above town, 3–8 m/s wind and
20 s transition remain. No snow, physics, source meshes or player-save changes.

The existing Editor menu applies this policy idempotently; runtime startup
scatters anew. Evidence: `Logs/CloudWind/PopulationRadius/Validation.txt`, native
overhead/horizon previews and scene backup. Play/device acceptance is separate.

## Superseded initial implementation

Pinefall keeps 40 active clouds (five of each imported variant). Their meshes and
single opaque palette material are shared; no colliders, shadows or real-time
lights are added. Cloud wind affects sky visuals only, not machinery, exhaust,
trees or the snow ledger.

| Setting | Default |
| --- | --- |
| Size | Uniform random 0.5-3x each slot's original placement scale |
| Wind speed | 3-8 metres/second (about 11-29 km/h) |
| New direction/speed target | Random interval of 300-900 gameplay seconds |
| Wind transition | Smooth 20-second turn/speed blend |
| Altitude | Entire mesh bounds 250-450 m above town level |
| Entry | Plane 1,500 m upwind of town centre |
| Entry width | Random crosswind position, +/-900 m |
| Exit | Plane 1,500 m downwind of town centre |
| Off-course cleanup | Outside approximately 2.1 km horizontal radius |

These height/speed defaults are visual choices for the 1 km stylized district,
not a meteorological simulation. The complete scaled mesh fits inside the height
band, rather than allowing a large cloud to hang beneath the minimum. Initial
cloud X/Z coverage is prefilled from the saved scatter, so the sky does not start
empty; its scale, yaw and altitude are randomized once in Editor. After that,
replacement clouds enter only through the current upwind plane.

The planes are vertical and perpendicular to current horizontal wind. As wind
turns, the entry/exit orientation turns with it. Existing clouds follow the same
changing wind smoothly. They keep their altitude and shape orientation. The outer
radial guard removes clouds carried far off-course during wind changes, outside
the normal camera range, rather than leaving invisible objects drifting forever.

At an exit, the expired root is disabled and destroyed; a replacement of the same
variant is instantiated at a random entry position with fresh size, yaw and
altitude. Player destruction is deferred until end-of-frame by Unity, so briefly
retired inactive objects can coexist with replacements. Active cloud roots remain
capped at 40. No new meshes or materials are allocated. This **cloud-only runtime
lifecycle is explicitly requested by the user** and supersedes the older general
no-runtime-spawning rule for this system. Other scenery stays Editor-authored.

The saved controller owns 40 slots with persistent initial scene references,
prepared prefab references, original baseline scales and source bounds. Baselines
are captured before the first randomization; repeated authoring never compounds
scales. Runtime randomization uses a private RNG and does not reseed Unity's
shared random stream. Wind is session-only and pauses with gameplay; menus,
background/resume and contract saves do not create persistent cloud state.

Editor menu: **Snowglow > Environment > Install wind-driven cloud lifecycle**.
Explicit cloud reimport also binds/preserves the controller. Original Blender,
FBXs, reusable prefabs, palette, skybox, camera tuning, Terrain, forest, snow and
player saves are preserved. Scene-specific cloud transforms and the new saved
controller are the intended changes.

The active mesh count stays at 13,280 triangles / 40 renderers. Larger screen
coverage, moving transforms and occasional instantiate/destroy work still have a
cost; the count is not an FPS guarantee. Test sustained play on the target phones.

Evidence: `Logs/CloudWind/Validation.txt`, four native `Sky-0/90/180/270.png`
previews and the saved scene backup. Validation status belongs in VALIDATION.md.

Unity notes that repeated [Instantiate/Destroy can create allocation and CPU
cost](https://unity.com/how-to/advanced-programming-and-code-architecture).
The requested lifecycle is bounded and occurs at distant boundaries; pooling
would be a separate optimization if phone measurements justify changing it.

Native validation: Unity 6000.3.8f1 / Android, 23,376 checks and scene/19 snow checks passed. Saved/reopened bindings and two sky previews inspected. Actual Play/deferred destruction, Android packaging and phone performance remain untested.

Native circular-cloud validation: Unity 6000.3.8f1 / Android passed 115,991 assertions, including 100 startup seeds and two simulated hours. Saved/reopened Pinefall, scene validation and 19 snow checks passed; protected assets and non-cloud/fleet poses matched. Native overhead and horizon previews inspected. Actual Play, Android packaging and phone performance remain untested. Evidence: Logs/CloudWind/PopulationRadius/Validation.txt.
