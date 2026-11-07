ALPINE FOREST ALPHA CARDS
26 source variants, each with Front and Side captures: 52 RGBA PNGs.
Rendered directly from the original highest-detail meshes. No background or ground shadows.
1024px cards for closer distant views; matching 512px versions for mobile.
Forest_Cards_Atlas_4096.png packs all 52 cards into an 8x8 grid of 512px cells.
Unused atlas cells remain transparent. UVs and source scale/pivots are in cards_manifest.json.

UNITY URP
1. Import either PNG_512 (recommended starting point for mobile) or PNG_1024.
2. Enable Alpha Is Transparency, sRGB, mipmaps; use Clamp wrap, Trilinear filtering.
3. Use a URP/Unlit material with Alpha Clipping enabled, cutoff around 0.4-0.5.
   Use opaque cutout rather than transparent blending. Select Render Face Both for crossed cards.
   URP/Lit Simple Lit also works but adds lighting on top of these gently lit captures.
4. Apply each PNG to a square quad. Set world size from card_width_metres in the manifest.
   pivot_uv_bottom_left gives the ground/root location in the square texture. Offset vertices
   by this pivot so the root sits at your terrain placement point. Fallen logs use their center base.
5. For far hills, one upright camera-facing quad per tree costs 2 triangles. Rotate only on Y
   in Unity so trees remain upright. For fixed wide camera views, cross two quads 90 degrees
   (4 triangles) using Front and Side images. Small stumps/logs use a slightly elevated capture.
6. Fade mesh LODs into these cards at distance; vary rotation, scale and spacing.
   Disable card shadow casting at long range; use GPU instancing and a shared atlas material
   for large forests. Atlas UV rectangles are bottom-left Unity convention.
7. Start texture compression with ASTC 6x6 on compatible Android devices; inspect thin branches
   and switch to ASTC 4x4 if necessary. Preserve alpha coverage for mipmaps where available.

These are flat impostors; best for distant scenery. They do not reconstruct 3D geometry.
BONUS BLENDER FILE
Alpine_Forest_Cross_Cards.blend contains 26 independently movable crossed-card objects,
4 triangles each, with the shared atlas packed into the file. Origins are at source bases.
The cards use an unlit alpha shader for inspection in Blender Material Preview.
Recreate this as URP/Unlit with Alpha Clipping and Render Face Both after Unity import.

The source .blend is unchanged. Front and Side refer to capture azimuths 0 and 90 degrees.
