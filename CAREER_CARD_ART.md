# Career region and destination artwork

Five region images were imported from the supplied
`Snow_District_Region_Card_Images.zip`. Its archive, originals, fitted images,
README and placement preview are preserved under `SourceAssets/RegionCards`.
ZIP content was treated as data; no bundled code was run.

Fifteen town illustrations were generated individually with built-in image_gen.
Original outputs are preserved under `SourceAssets/TownCards/Originals` and the
complete prompt set is `SourceAssets/TownCards/Prompts.json`. These are stylized
landmark concepts, rather than surveys or exact architectural reconstructions.
Pinefall was excluded from generation and retains the existing game capture
until the user supplies its replacement. Existing location labels and stable
progression IDs remain unchanged; `alps`, `svalbard`, `andes` and `coquihalla`
art depict Altdorf, Longyearbyen, Bariloche and the Hope-area Othello Tunnels.

## Imported assets and layout

- `Assets/Snowglow/UI/Production/Regions`: five supplied PNGs, 1220 x 384 sources.
- `Assets/Snowglow/UI/Production/Towns`: fifteen fitted 1024 x 498 JPEGs.
- Both use single sprites, no mipmaps, clamp wrapping, a 1024 maximum and Android
  ASTC 6x6. Source originals remain outside player builds.
- Region buttons retain their 382 x 120 layout, with a separate navy label scrim
  and a yellow border for unlocked regions.
- Town cards use the same approximately 2.056:1 image ratio. Four Alpine cards
  fit in one row; three larger cards are used for the other regions. Labels and
  availability appear below the images. No landmark is stretched into a tall box.

CareerCardGallery switches saved sprite references and layout when the selected
region changes. It creates no UI, sprite, mesh or material at runtime. All actions,
progression, rewards and playable-content gates retain existing behavior. Locked
regions can still be inspected, as before; only Pinefall has playable content.

## Authoring and verification

Menu: Snowglow > UI > Install region and destination card artwork.
For automation, await a successful native compile/reload, then write
`Logs/CareerCards.request`. Authoring preserves disk/unsaved scene backups, saves
and reopens Pinefall, and runs existing menu/progression fixtures without Play.
Full production-menu authoring also reuses prepared CareerCardGallery artwork.

Android-defined runtime/Editor compilation and native import/reload passed.
Native binding/image/layout validation, 607 menu assertions and 187 progression
assertions passed. Region and town-page renders are in `Logs/CareerCards`;
the region page and Alpine, City and Industrial pages were visually inspected.
Protected world-record and player-profile/backup comparisons are recorded in
`Logs/CareerCards/Preservation.json`. No phone, APK or manual-touch acceptance
is implied.
