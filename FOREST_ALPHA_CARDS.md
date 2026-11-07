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
