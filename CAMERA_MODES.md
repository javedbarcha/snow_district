# Camera controls

Press **C** or tap **CAM** to cycle **Isometric ? Follow ? Orbit ? Scenic**.
Isometric remains the starting view. Steering stays vehicle-relative in every mode;
W/Up drives forward and S/Down drives in reverse.

| Mode | View |
| --- | --- |
| Isometric | Original raised orthographic work view |
| Follow | Perspective chase view behind the machine |
| Orbit | Manual full 360-degree inspection around the active machine |
| Scenic | Wider diagonal panorama, slowly revolving around the active machine |

In Orbit and Scenic:

| Action | PC | Phone |
| --- | --- | --- |
| Rotate / tilt | Hold right mouse button and drag, or J/L and I/K | Drag an empty part of the view |
| Zoom | Mouse wheel or +/- (the + shares the = key) | Pinch with two world-view fingers, or ZOOM + / ZOOM - |
| Reset | Home | VIEW RESET |

Orbit starts 15 m from the vehicle at a 25-degree downward angle. Scenic starts
40 m away at an 18-degree angle and a diagonal heading, leaving more room for the
town and mountain backdrop. Scenic turns at 4 degrees per second; manual rotation
or zoom holds automatic movement for six seconds. Menus/pause freeze revolution.
Reset returns each mode to its starting vehicle-relative view. Switching machinery
recentres the view on the newly active machine. Mode/zoom are session-only settings;
this change does not extend player-save data.

Yaw wraps freely through 360 degrees. Tilt is limited to 5-75 degrees; zoom ranges
are 7-42 m in Orbit and 22-65 m in Scenic. Obstructions can shorten the actual camera
distance. Touches that begin on steering, driving or other HUD controls are excluded
from camera gestures, even if dragged off the control later. A separate finger can
orbit while another operates a driving control. No cursor locking is required.

The existing saved CameraRig moves the one authored gameplay camera. It samples
the saved Terrain and protects camera height plus all four near-plane corners.
Sphere sweeps against world/pile colliders pull the camera inward at obstructions;
vehicle colliders are excluded. The ground constraint also runs after smoothing,
so intermediate motion cannot dip below the sampled terrain. It is not a guarantee
of visibility through every tree, roof, mountain proxy or possible pile arrangement.

Camera buttons/help are authored into Pinefall in Edit mode. No camera, Canvas,
vehicle or scenery is assembled during gameplay. **Snowglow > Install orbit and
scenic cameras and validate** updates the saved bindings without a district rebake.
The same bindings/buttons are included in future full authoring. **Snowglow > Run
camera orbit input and terrain checks** exercises the saved configuration with
throwaway native fixtures. See `Logs/CameraModes/Validation.txt` and VALIDATION.md.
Mouse-wheel handling follows the installed Input System's normalized-scroll setting.

The wider views can expose more scene geometry and shadows. Galaxy A07 frame times,
physical touch ergonomics, live driving and all obstacle situations still need
separate testing. Keep the original work views available for close plowing.
