# Career main-menu capture

`Career_mode.PNG` is the user's original 1359 x 587 screenshot supplied on
13 November 2026. The production UI intake imports a quality-95 JPEG derivative
at `Assets/Snowglow/UI/Production/career.jpg`, using the existing 1024 maximum,
no mipmaps and Android ASTC 6x6 settings.

The dedicated sprite is used only by the main-menu Career card. Existing
`pinefall.jpg` references elsewhere remain unchanged. At the user's request, the
derivative is cropped to 1207 x 587 by removing 152 pixels from the left only.
This fills the standard 382 x 185.83784 image box while retaining the right edge
and full original height. Integer rounding differs from the box ratio by <0.04%.
Original PNG is retained outside Assets and is not included in player builds.
