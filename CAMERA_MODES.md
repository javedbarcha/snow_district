# Camera controls

Press **C** or tap **CAM** to cycle **Isometric -> Follow -> Orbit**.
Scenic is removed from gameplay. Its composition is reserved for a future static
main-menu background preview; that menu feature is not implemented here.
Existing serialized Scenic values migrate to Isometric.

| Mode | View |
| --- | --- |
| Isometric | Closer orthographic view at rest; smoothly widens with vehicle speed |
| Follow | Rear chase view forward; smoothly arcs to the front for reversing |
| Orbit | Manual full 360-degree inspection around the active machine |

Isometric uses a 12 m orthographic half-height at rest and 19.2 m at 8 m/s,
with a 1.1 s exponential response. Reverse speed widens it too. Narrower aspect
ratios widen the vertical framing to retain vehicle/work-area coverage.
These are initial tuning values, editable on the saved CameraRig.

Follow uses actual Rigidbody velocity along the vehicle's forward axis. It changes
direction beyond +/-0.35 m/s and retains its last direction while stopped, avoiding
jitter near zero or an early swing while braking. The camera moves around an arc
rather than passing through the cab; direction changes use a 0.9 s smoothing
parameter, followed by the existing position/rotation smoothing. Follow defaults
to 13 m horizontal distance and 6 m height. Camera direction never remaps steering.

In Orbit:

| Action | PC | Phone |
| --- | --- | --- |
| Rotate / tilt | Right-drag, or J/L and I/K | Drag an empty part of the view |
| Zoom | Wheel or +/- | Pinch with two world-view fingers |
| Reset | Home | VIEW RESET |

Orbit starts at 15 m / 25 degrees, with 7-42 m distance, 5-75 degree pitch and full
yaw. HUD-origin touches are excluded from camera gestures. Switching machinery
recentres the camera. Pause freezes speed zoom and direction changes. Camera
preferences remain session-only, with no player-save schema change.

The single authored CameraRig retains world/pile obstruction sweeps, vehicle
exclusion and Terrain/near-plane safety after smoothing. Obstructions can reduce
the requested distance. Menu: **Snowglow > Install gameplay cameras and validate**.
Actual verification and remaining limitations belong in VALIDATION.md and
Logs/CameraModes/Validation.txt.

Orbit has only a left-anchored VIEW RESET button (at the 5% safe-area inset). On-screen zoom buttons are removed; pinch, wheel and keyboard +/- zoom remain.
