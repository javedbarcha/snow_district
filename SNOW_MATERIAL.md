# Soft matte toon snow

The user initially selected off-white/icy-blue matte snow, then supplied a snowy
mountain reference on 28 October 2026 and requested **pure-white tops with pale
cyan side shading**. The reference revision supersedes the off-white palette.
This change is material/shading only. The proposed second task
(replace one distinct scenery primitive with finished art) remains deferred.

- Snow highlight color: `#FFFFFF`.
- Icy shade color: `#BFE7EE`, approximated from the reference.
- Upward mesh faces reach pure white independently of sun direction; slopes blend
  toward cyan, with softly lit side faces and deeper cyan on shadowed sides.
- No specular sparkle, roughness/normal textures, reflection probes, displacement,
  added mesh detail or extra lights. Existing main-light shadows remain supported.
- Shared mesh material covers plowable snow, piles, roof/tree snow caps, bucket
  contents and existing snow effects where they use the prototype snow palette.
- Terrain retains the supported URP Terrain/Lit shader, terrain patch instancing,
  depth/shadow and distant base-map passes. Its existing snow layer is tinted
  white with zero metallic and smoothness. Terrain shading follows scene lighting.

Edit the existing palette's **snow** material to tune its snow and shade colors.
Terrain color is controlled by the existing TerrainLayer's diffuse remap.
Mesh shading uses `_BaseColor`. Its shading ramp is in
`Assets/Snowglow/Shaders/SoftSnowLighting.hlsl`.

**Snowglow > Art > Apply soft matte toon snow** applies the approved defaults in
Edit mode. It updates existing material assets in place, preserving their GUIDs
and saved scene/prefab bindings. A full district bake also uses these defaults;
reapplying the command resets customized colors to the approved starting palette.

The authoring command expects Pinefall's current **single snow layer**. Review
that boundary before applying the command to mixed terrain layers. An experimental
custom terrain pass failed to render during visual review and was removed; no
unsupported terrain shader remains in the project.

Authoring checks verify shader imports, opaque materials, shared chunk/terrain
bindings, unchanged snow-seed dependency hash and retained terrain instancing,
then run saved scene structural validation. Results are in
`Logs/SoftSnowMaterial.txt`. Mesh geometry, volume/credit/disposal mechanics,
colliders, vehicle physics and scene layout are not changed by material authoring.
Android player shader compilation and Galaxy A07 appearance/frame timings still
need device validation.
