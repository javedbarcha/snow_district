# Driving HUD control sheet

Source: user-supplied Snow District control icon sheet.png, preserved unchanged
in SourceAssets/ControlIconSheet/Original.png. SHA256, extraction rules and crop
rectangles are retained beside it. Fourteen individual transparent PNG crops
retain source resolution and colour, trimmed with four pixels padding. Only
near-transparent background residue (alpha <=16) is removed.

Import: Snowglow > Asset intake > Import driving control icon sheet.
Catalogue IDs: control-sheet-{camera,garage,pause,lights-off,lights-on,steer-left,
steer-right,blade-left,blade-straight,blade-right,dump,lift-toggle,forward,reverse}.
Each UI entry has a reusable prefab and remains Needs Review. Android sprites
use ASTC 6x6; Image.preserveAspect is enabled. No runtime image/object factories.

Layout follows the supplied sketch within the existing safe area. Side groups
retain 5% horizontal margins and uniform fit to a 1280x720 reference area:
- Camera and Garage at upper left; Pause and Lights at upper right.
- Three blade-angle selectors above left/right steering for the tractor.
- Loader shows only Dump at the leftmost selector position.
- Forward pedal above Reverse pedal at lower right; Lift toggle beside Reverse.
- Recover is available only in Garage > Fuel / Repairs; Orbit reset stays beside Camera.
- Existing stats toggle and text-only status readouts stay available.

The sketch's Gas/Brake positions retain the previously selected two-directional
pedal mechanics: Forward drives forward; Reverse brakes opposing travel then
drives backward. Switching direction brakes to a standstill and holds a 0.2 s
stopped buffer before opposite drive torque. Releasing both pedals coasts with
normal rolling/snow resistance; holding both brakes.
Tractor selectors set -1/0/+1 directly, replacing angle cycling on the HUD.
Desktop Q still cycles angles. Lift uses existing Work/Carry behavior, including
loader automatic full-bucket carry. No other attachment automation is introduced.

Both DrivingButtonsAuthoring and PSDUIIntake reapply the prepared control-sheet
layout/icons so later HUD rebuilds keep the new policy. Runtime HUD switches the
three angle buttons and Dump when the active machine changes.

Evidence: Logs/ControlSheet/Validation.txt, Tractor.png and Loader.png.
Native Editor validation and phone touch/ergonomic acceptance are separate.

## Percentage spacing revision

ResponsiveDrivingHUD reflows the saved controls every LateUpdate, using the
usable safe-area width/height. Left/right edges remain 5% in; the top 10% is
reserved for text and the Stats toggle, and bottom controls sit 5% above bottom.
Forward uses 1.25 times Reverse width and height. The pedal vertical gap is 5% height; steering-to-angle/Dump is 8% height.
Brake-to-Lift and steering left-to-right gaps are 5% width, edge to edge. Icon aspect remains preserved.
Uniformly fitted button sizes retain the mobile reference proportions while
spacing follows the current screen dimensions. No UI objects are created at runtime.
Repeat: Snowglow > UI > Apply percentage driving HUD spacing.
Checks/previews: Logs/HUDSpacing.

Adjacent tilt/angle buttons have a 1.5% safe-area width edge-to-edge gap.
