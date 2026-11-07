# Alpine forest alpha cards provenance

User supplied `Alpine_Forest_Alpha_Cards.zip`, preserved byte-for-byte here along
with all 110 extracted source files. SHA256:
`cbda5aec744cc623295d5a47b2ee981cdb2bd98dd33b85de36a0c0af724b2997`.

The source package contains front/side captures for all 26 matching tree/wood
variants: 52 RGBA PNGs each at 512 and 1024 pixels, a 4096 atlas, cross-card Blender
file, two JSON manifests, README and preview. Embedded guidance is source data;
no bundled code or Blender text has been executed. Original files remain unchanged.

`DerivedAlphaBounds.json` is a separate measurement of alpha bounds in the 512
images, converted to bottom-left UVs with two pixels of margin. Only the atlas
and wrapped/augmented manifest are staged under Assets. All runtime meshes,
materials, prefabs and bindings are authored by ForestAlphaCardsIntake through
CatalogPreparation; no Blender importer dependency is needed for the cards.

See FOREST_ALPHA_CARDS.md for switching ranges, collision policy and test evidence.
