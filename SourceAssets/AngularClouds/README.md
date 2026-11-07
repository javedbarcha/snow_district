# Eight angular low-poly clouds

Current scene policy (7 November): 10–15 active clouds randomly scattered through
a 1,500 m disk including centre, then staggered upwind arrivals and radial exits.
The 40 authored slots retain original scale baselines and inactive source roots.
Size 0.5–3x, full-mesh altitude 250–450 m above town, wind 3–8 m/s and 5–15 minute
wind changes remain. See CLOUD_WIND.md; fixed-40/plane descriptions below are
historical. Source meshes, prepared prefabs and palette remain unchanged.

Supplied 3 November 2026: `Eight_Angular_Low_Poly_Clouds.blend`, preserved
byte-for-byte from the user's Downloads file (1,373,168 bytes).
SHA256: `8AE32B6DE44F38348DB0D38BD745FA0C274D657D6E0E7B7D0A6D3318BEE681E9`.

Blender 5.2.2 inspected/exported with `--factory-startup --disable-autoexec`.
The file contains eight mesh objects and no embedded texts, cameras or lights.
Each variant has 168 source vertices and 332 triangles. Original layout positions
are presentation only; exported copies are horizontally centered with base Y=0
in Unity. Source shape/proportions remain unchanged, flat face normals are
recalculated, and the three source face colors are packed into one opaque palette.
No original Blender data is saved over or executed.

| Catalog ID | Variant | Triangles |
| --- | --- | ---: |
| angular-cloud-01 | Alpine Puff | 332 |
| angular-cloud-02 | Wide Cumulus | 332 |
| angular-cloud-03 | Tall Cluster | 332 |
| angular-cloud-04 | Long Cloud Bank | 332 |
| angular-cloud-05 | Asymmetric Puff | 332 |
| angular-cloud-06 | Stepped Cluster | 332 |
| angular-cloud-07 | Compact Crown | 332 |
| angular-cloud-08 | Broad Alpine Bank | 332 |

Prepared FBXs, palette and object inclusion/metrics manifest:
`Assets/Snowglow/Catalog/Sources/AngularClouds`.
Editor intake: `AngularCloudsIntake.cs`; menu **Snowglow > Asset intake > Import
and scatter eight angular clouds**. All catalog entries remain Needs Review.

The scene scatter uses 40 saved prefab instances, five of every variant, under
**Sky clouds | 40 angular low-poly clouds**. Random seed 311103 produces jittered
angular sectors, radii 260-620 m, base altitudes 195-260 m, random yaw and uniform
scales fitting source width to 45-85 m. Cloud/mountain bounds receive clearance
checks. Repeat intake preserves existing cloud poses instead of reshuffling or
duplicating them. Existing skybox and camera background settings are left unchanged.

Total added geometry: 13,280 triangles, 40 renderers/material slots, one shared
opaque URP Unlit palette material. Clouds have no colliders, Rigidbody, lights,
shadow casting, wind animation or runtime construction. They do not modify snow.
Unlit face colors match the current daytime setting; coordinated day/night cloud
response would be separate future work. These counts are not device timings.

Inspection/export scripts: `Tools/AssetIntake/inspect_angular_clouds.py` and
`export_angular_clouds.py`. Native report/previews and original scene backup:
`Logs/AngularClouds` (ignored). Android/device performance is not established.

Unity 6000.3.8f1 / Android compiled, imported and validated all eight catalog
prefabs, then saved/reopened the 40 placements. Native metrics match 332 triangles
and one renderer/material slot per variant. Five of each variant, unique placement
IDs, prefab bindings, absent physics and disabled shadow casting were verified.
Existing non-cloud catalog identities/transforms and all existing mesh/Terrain/
snow dependency hashes matched before/after. No district rebake or player-save
write. Four native 1280x720 sky/horizon views rendered; north/east views were
visually inspected. Scene validation passed. Continuous live-camera inspection
and Android/device profiling remain untested. Entries remain Needs Review.

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

