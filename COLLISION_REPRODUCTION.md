# Play-mode collision reproduction — 11 November 2026

The subsequent [snow collision audit](SNOW_COLLISION_AUDIT.md) expands this
investigation with 120 Play-mode checks, including pile sizes, speeds, continuous
snow roads and controlled comparisons. Use that newer report for recommendations.

Repeated launches were reproduced in Unity 6000.3.8f1, Android target, in the
actual Pinefall Play-mode scene. This was automated driving through the existing
MachineController and WheelColliders, not an Edit-mode physics fixture or manual
desktop driving. Desktop control was unavailable. Handling was not tuned.

Two completed sequences ran 12 approaches each (24 total), with both machines,
fence/home obstacles, small lateral offsets and down/raised attachment poses.
Each approach settled for 1.5 seconds then applied forward throttle; the complete
trial lasted nine simulated seconds. No collision impulse or launch velocity
was injected. Diagnostics use a temporary session with assists on, no upgrades,
full fuel/condition and initially empty attachments. Snow collection remains live.

## Results

The second sequence returned inactive machines to the depot between trials,
preventing the tractor from obstructing the loader. Every second-sequence trial
recorded contact with its intended obstacle. Foundation centre: (-22, 9.39, 16).
Fence post centre: (-16, 5.90, 11).

| Second sequence | Impact speed | Maximum root rise | Maximum tilt | Longest continuous airborne time |
| --- | --- | --- | --- | --- |
| Tractor, foundation, down, trial 3 | 6.03 m/s | 1.81 m | 67.9° | 0.72 s |
| Tractor, foundation, down, trial 4 | 5.89 m/s | 1.08 m | 47.6° | 0.16 s |
| Tractor, foundation, raised, trial 5 | 6.27 m/s | 0.07 m | 4.3° | 0 s |
| Loader, foundation, down, trial 9 | 6.43 m/s | 0.09 m | 3.4° | 0 s |
| Loader, foundation, down, trial 10 | 6.41 m/s | 1.87 m | 99.4° | 0.88 s |
| Loader, foundation, raised, trial 11 | 6.33 m/s | 0.10 m | 3.0° | 0 s |

All six second-sequence fence passes stayed grounded. Loader trial 10 accumulated
1.50 seconds airborne across multiple episodes. Airborne means all four wheel
contacts absent; rise is chassis-root Y above its approach starting Y. Tilt is
the angle between vehicle up and world up, not cumulative roll revolutions.
No complete 360-degree roll, or two full rotations, was confirmed by these tests.

The first sequence also reproduced a 0.98 m tractor hop / 46.3° tilt / 0.26 s
airborne at the foundation at (-22, 9.39, -16). Some other first-sequence paths
hit intervening colliders; its three loader foundation approaches were obstructed
by the parked test tractor. Those are not successful intended-foundation tests.

The down/raised comparison changes lateral alignment slightly, and lowered
attachments collect snow, so it does not isolate pose as the cause. The saved
machinery prefabs have one chassis BoxCollider each, plus wheels; this evidence
does not establish that a blade collider catches a wall. Investigate wheel/edge
contacts, unclamped anti-roll forces, load-dependent centre of mass and collision
separation before selecting a handling fix.

## Evidence and rerun

Logs/CollisionPlay contains First-pass-trials.txt, First-pass-summary.txt,
Trials.txt, Summary.txt and abnormal-motion game screenshots. Two tractor frames
and one loader frame were captured; the tractor frame was inspected and the
house partly obscures the vehicle, so it is not a clean visual proof of peak
height. Numerical peaks are recorded by the fixed-step probe.

Both completed sequences exited Play and matched the saved scene, profile and
profile-backup byte hashes. Tests disabled PrototypeSession saving and automatic
progression before driving; no rewards or condition changes were persisted.
Play mode is stopped. No vehicle physics, prefab, scene or collider edits.

For an explicitly authorized rerun, open clean saved Pinefall, stop Play and
complete the background Compile request first. Then create
Logs/CollisionPlay.request. The opt-in Editor runner enters Play, disables saves,
runs the sequence and exits automatically. It overwrites the latest report, so
copy prior evidence first. CollisionPlayProbe is wrapped in UNITY_EDITOR and
does not ship in a player. An early diagnostic setup error was corrected before
the completed sequences; that aborted attempt also preserved scene/save hashes.

During setup a separate Editor request race was corrected: a request accepted
while automatic compilation is already running now persists its ID immediately,
so it survives that assembly reload. Native ID-matched compilation completed
after the fix. This is Editor workflow code, not a vehicle handling change.
