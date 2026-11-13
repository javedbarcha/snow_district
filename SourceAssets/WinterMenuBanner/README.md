# Shared winter menu banner

`Original.png` preserves the user's 2172 x 724 source supplied on 13 November
2026. The usable strip is extracted from x=5, y=293, width=2162, height=129,
excluding the surrounding white canvas. The derived PNG is
`Assets/Snowglow/UI/Production/winter-banner.png`.

PrepareHeaderArt imports a single sprite with a 2048 maximum, no mipmaps,
clamped sampling and Android ASTC 6x6. ApplyHeader assigns it to the existing
shared 1280 x 78 header with white tint. It fills the header with slight aspect
adjustment, retaining all tread artwork. The title, wallet and menu controls
retain their authored positions. The header image does not intercept input.

Full production-menu authoring and targeted saved-scene authoring reuse these
methods. No runtime UI objects or materials are created. Original source is
outside Assets and is excluded from player builds.
