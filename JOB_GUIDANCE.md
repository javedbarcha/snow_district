# Contract direction and venue holograms

During a Career or Quick Job, an amber 3D arrow floats above the active vehicle and points horizontally toward the closest point on the job area. It hides within 10 metres of the area's edge, remains hidden inside the venue, and returns beyond 14 metres. This arrival/departure buffer avoids flickering at the threshold. The arrow follows vehicle selection and gently bobs; its dark outline keeps it readable over snow.

Only the active venue has a cyan hologram: a translucent, animated 2.2 m perimeter wall plus a bright rim. The rim samples the existing snow height every 0.25 seconds, following clearance and deposited piles. Depth testing lets scenery occlude the hologram naturally; it is a visual guide, not a physical fence. The boundary remains visible after arrival. Guidance hides in menus, Free Work and after the job ends.

SnowField.ContractBounds provides the existing contract rectangles to both progress evaluation and the saved visuals:

| Contract | World X | World Z |
| --- | --- | --- |
| Depot Access | -59 to -29 | -69.5 to -60.5 |
| Parking Lot | 77 to 108 | 9 to 33 |
| Main Street | -35 to 35 | -4.5 to 4.5 |

Target comparisons retain strict edges. The extraction does not enlarge the work area, alter snow, change rewards or move any gameplay vehicle.

All meshes, materials, line points and objects are authored and saved in Edit mode. Runtime only changes visibility/poses and existing LineRenderer point heights. There are no hint colliders, lights, runtime asset/model factories or changes to the save schema. The hologram shader animates scan bands and opacity without per-frame material instances. Existing static reference boundaries are retained but disabled.

Install/repeat: Snowglow > UI > Install job direction and venue holograms. For background automation, await the forced native compilation helper before creating Logs/JobGuidance.request. The installer backs up the disk scene and any unsaved edits, saves/reopens Pinefall and runs native checks. The district authoring path also installs these objects on future explicit bakes; installing hints does not rebake the district.

Evidence and captures: Logs/JobGuidance. Validation includes every snow-grid cell against the prior three target rules, all venue edges, far/arrival/departure/hysteresis states, selected-vehicle switching, snow surface seating and menu/free-work/completion hiding. Editor fixtures keep seed data read-only and avoid player-save writes. Actual Android touch/rendering cost and Play driving acceptance remain separate.
