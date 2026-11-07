# Distant forest cards

The original ZIP and all supplied files are preserved in `SourceAssets/ForestAlphaCards`.
The archive SHA256 is `cbda5aec744cc623295d5a47b2ee981cdb2bd98dd33b85de36a0c0af724b2997`.
Bundled instructions are treated as source documentation; no Blender scripts are executed.
Only the 4096 atlas and derived metadata enter runtime Assets. The standalone PNG sets,
Blender file and preview remain outside Assets.

All 26 existing tree/wood variants retain their LOD3 meshes, prefab identities and solid
collision. They gain one saved crossed-card child: eight vertices, four triangles, one
shared double-sided alpha-cutout material. There are also 26 reusable `forest-card-01..26`
catalog entries, all Needs Review. Cards have no colliders and cast no shadows.

`DistanceVisibility` uses the gameplay camera for card-equipped trees. The nominal
switch is 230 metres, with a 12 m hysteresis buffer: geometry switches to cards beyond
242 m; geometry returns below 218 m. Cards hide at 650 m, where existing world fog
already reaches full coverage. Objects without cards keep their original vehicle-based
distance behavior. Checks run after camera tracking, about five times per second.

Both representations are authored in Editor. Runtime only toggles saved renderers;
it does not instantiate trees, meshes, materials or collision. Solid collision stays
enabled even when all visuals are hidden. There is no simultaneous near/card rendering.

Front and side alpha bounds are measured from the actual 512 PNGs with a small safety
margin. The atlas UVs are cropped to those bounds; each plane is fitted to the matching
LOD3 dimensions and mounting frame. This reduces empty-card overdraw and prevents
portrait log images from becoming tall upright scenery. Two views still cannot reproduce
every camera elevation or the precise 3D silhouette; inspect Orbit transitions on device.

The texture uses sRGB, alpha transparency processing, trilinear mipmaps with alpha
coverage, clamp wrapping and Android ASTC 6x6. Runtime readability is off. The atlas is
64 MiB uncompressed before mipmaps; actual device compression/support determines memory.
Alpha testing and double-sided cards still have fill cost. Geometry savings alone do
not establish a frame-rate improvement.

All-far geometry for 438 placements is 1,752 triangles / 438 renderers before culling.
Near geometry remains 187,488 triangles; stored inventory includes both representations.
The existing 438 placements, slopes, collider shapes, Terrain, original meshes and snow
remain unchanged. No additional forest placements or higher-detail tree LODs are added.

Repeat staging: `python Tools/AssetIntake/intake_forest_alpha_cards.py`.
Editor menu: **Snowglow > Asset intake > Import distant forest alpha cards**.
Existing forest seating, collider and lowest-LOD tools exclude alternative cards when
measuring physical bounds or selecting source meshes. Catalog preparation's optional
visual-only mode is restricted to Landscape; existing callers retain prior behavior.

Evidence: `Logs/ForestAlphaCards/Validation.txt`, per-variant near/card previews and
original prefab/scene backups. Live driving, Android packaging and phone measurements
remain separate acceptance.

Native validation in Unity 6000.3.8f1 / Android passed 9,801 checks, saved/reopened
Pinefall, scene validation and 19 snow checks. Six near/card variant pairs were
rendered; evergreen, snowy-tree and log pairs inspected. A four-heading full-scene
review with fog/post-processing confirmed visible distant cards (up to 6,255
contrast pixels) while preserving scene bytes, renderer states and dirty state.

## Bright colours matching the nearby trees

The card atlas contains baked lighting and originally appeared much darker than
sunlit 3D foliage. ForestCardPalette.shader now applies separate RGB gains to
foliage, snow and wood before normal world fog. Current gains are approximately
(2.084, 1.835, 1.932), (1.135, 1.146, 1.127) and (1.952, 1.931, 1.887), calibrated
in this Gamma-space project. No texture, alpha, near geometry or material is
modified. The shader still uses one atlas sample and one shared material.

Calibration compares sun-facing 3D trees with cards under identical scene lights,
camera and post-processing. It targets the upper-middle sunlit reference colours;
fixed baseline masks keep pixel families stable during the after comparison.
Run **Snowglow > Environment > Match forest card colours to 3D trees** after
changing sun/exposure or project colour space. This is a saved lighting match,
not dynamic day/night lighting. Flat card silhouettes and missing geometry
shadows remain different from the 3D trees. Reports/previews: Logs/ForestCardColors.
Latest forest tuning: 330 m full-bound placement radius, target 638 placements,
including 200 added trees on hills/slopes only. Geometry/card distance
is now 200 m with 12 m hysteresis (212 m out, 188 m in); far card cutoff is 2 km.
Dedicated ForestCards layer 11 extends only card rendering past the previous
world-layer limit. Existing world fog remains, so this is a culling limit, not
a fog-free visibility guarantee. See Logs/Forest638/Validation.txt for native
save/reopen and seating validation status; phone performance remains untested.


## Moderate forest card brightness

User chose a fixed middle colour as a compromise between sunlight and shade.
The shared card material now uses a 50% blend between the original atlas gains
(1,1,1) and the earlier bright calibrated foliage/snow/wood gains. This supersedes
the full-bright palette above. The existing simple fogged cutout shader remains;
no shadow-aware system is installed. Atlas, near-tree materials, geometry, poses,
collision and 200 m / 2 km visibility settings are unchanged. Only three gain
vectors were edited and checked; native sun/shade appearance and phone acceptance
remain unverified for this adjustment. Values/backup: Logs/ForestCardMidtone.

Latest population is 500 total forest objects: 405 leafy/sapling trees plus
95 dead trunks/snags/stumps/logs. Only the latter group (variants 17-19, 21-26)
is limited to 200 m root/full-bound radius. Other trees retain 330 m. Geometry/
card switching remains 200 m with 12 m hysteresis and a 2 km cutoff. The moderate
fixed card palette is retained. Logs/Forest500CloudFade records validation.
