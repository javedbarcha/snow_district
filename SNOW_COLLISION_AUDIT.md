# Snow and vehicle stability audit — 11 November 2026

The strongest evidence points to physical snow support/contact geometry interacting
with the wheels and suspension. Unbounded anti-roll correction also contributes
to foundation-edge instability. A moving centre of mass or duplicate chassis
proxy does not explain the problem on its own. No production handling fix was
implemented during this investigation.

## Scope and safeguards

120 actual Play-mode approaches/checks in Pinefall, Unity 6000.3.8f1, Android
target: a 68-case matrix, 40 focused cases and 12 corrected proxy comparisons.
These used the saved scene's real machines, controllers, wheels and SnowField
mesh/collider rebuilds. They were automated approaches, not manual desktop driving
or isolated Edit-mode physics simulations. No launch force/velocity was injected.

Both machines used ordinary throttle/brake regulation targeting 2, 4 and 6.5 m/s
(7.2, 14.4 and 23.4 km/h). Those are targets, not guaranteed impact speeds;
collisions can accelerate, stop or redirect the body. Actual speed/contact data
are in the fixed-step traces. Empty/down attachments collected snow normally and
the loader could fill and auto-lift. Fuel/condition/upgrades reset between cases.

The initial snow fields covered clear/shallow road, 0.55/0.65/1.2/2.4 m peak
mounds, widths 2/4/8 m, and a 12 m continuous uneven strip varying up to roughly
1.5 m. Starting dimensions are prescribed values; settling and plowing remained
active, so they are not measurements of the pile at impact. Most drives ran up
to 12 simulated seconds, uneven-road drives up to 14, with 1.5 seconds to settle
first. Early exit after traversing the lane bounded the test region.

All three suites exited Play and matched saved scene, profile and profile-backup
hashes. Saving and session progression were disabled; synthetic snow volumes were
temporary test inputs, not a ledger-conservation acceptance test. No scene,
prefab, source seed, material or player-save changes were saved. Diagnostic
switches/probes are UNITY_EDITOR-only, nonserialized and default inactive.
Removing Editor-only blocks and whitespace yields identical pre/post player
logic for MachineController and SnowField. Native compilation/reload passed.
This is not Android packaging, phone handling or performance acceptance.

## Reproduced behavior with normal handling

Examples from the 68-case matrix; speeds below are approach targets.

| Vehicle / snow | Target | Maximum chassis-root rise | Maximum tilt |
| --- | --- | --- | --- |
| Tractor, shallow road | 7.2 km/h | 0.83 m | 24° |
| Tractor, continuous uneven road | 7.2 km/h | 1.08 m | 57° |
| Tractor, tall narrow pile | 23.4 km/h | 3.97 m | 167° |
| Loader, broad pile | 14.4 km/h | 4.95 m | 135° |
| Loader, broad pile | 23.4 km/h | 2.61 m | 180° |
| Loader, continuous uneven road | 23.4 km/h | 4.15 m | 164° |

Tilt is angle from world up, not accumulated rotations. The 180° case confirms
an inverted orientation, not a measured count of complete barrel rolls. Rise is
root height above the initial approach position, not tyre clearance. Some small
piles were removed or settled before the wheels contacted them; absence of a hop
in those cases does not prove the untouched pile is safe. Results vary with the
evolving snow and contact sequence; these are not statistical repeatability bounds.

## What the comparisons establish

### Physical snow contact is the leading cause during clearing

The code builds snow collision from separate 0.5 m cube columns, adding a column
only when its depth exceeds 0.55 m. Meshes are rebuilt as snow settles or moves.
This gives wheels discrete steps and changing support, including new colliders
where previously shallow snow has been pushed into a deeper pile.

In the continuous-road isolation passes:

| Vehicle | Normal rise / tilt | Anti-roll off | COM fixed | Snow collisions excluded |
| --- | --- | --- | --- | --- |
| Tractor | 0.58 m / 39° | 0.78 m / 29° | 0.82 m / 30° | 0.02 m / 5° |
| Loader | 4.14 m / 144° | 5.80 m / 146° | 3.00 m / 146° | 0.05 m / 3° |

Snow-excluded runs recorded zero snow wheel contacts. Collection, resistance,
load and auto-lift still ran. Disabling anti-roll did not remove the problem and
could worsen it. Holding COM fixed did not remove it either.

Frozen-collision tests kept the initial collision mesh while visual/data changes
continued. In initially shallow snow, these prevented newly built piles from
becoming rigid obstacles and suppressed the tractor hop (about 0.02 m versus
0.45–0.50 m in its normal/fixed-COM focused passes). This implicates collision
creation/shape changes during clearing; it does not prove every mesh recook is
faulty or justify leaving invisible frozen collision in the game.

Unity documents that WheelCollider uses a downward ray and can pop over steps
rather than roll smoothly over them. That limitation is consistent with this
code and contact evidence. [Unity wheel-collider introduction](https://docs.unity3d.com/cn/2023.2/Manual/wheel-colliders-introduction.html),
[Unity 6 WheelCollider API](https://docs.unity3d.com/kr/6000.0/ScriptReference/WheelCollider.html).

### Anti-roll and foundation edges are a separate contributor

On a cleared approach, the loader recorded wheel contacts on the foundation,
about 51 kN peak anti-roll correction, 1.33 m rise and 50° tilt. Its fixed-COM
pass still overturned. Disabling anti-roll produced a much calmer wall pass,
although single-pass trajectory differences prevent assigning an exact reduction.

The normalized travel values used by the current anti-roll code frequently went
outside 0–1; the code directly multiplies their difference by 9,000 without a
clamp or force cap. Snow passes reached about 56 kN correction as well. Thus
anti-roll can magnify an unfavorable contact, but it is not the sole snow cause.

Some early matrix wall approaches hit intervening snow and did not reach their
intended foundation. They must not be treated as successful wall comparisons.
The focused/proxy suites used the earlier reproducing face and a cleared snow
approach; target-contact and wheel-contact counts identify what was really hit.

### Chassis proxies are not a standalone explanation

Each live machine has one chassis BoxCollider and four WheelColliders, with no
separate physical blade/bucket collider. The initial inventory found 902 enabled
colliders: 877 boxes, three capsules, 13 meshes, one Terrain and eight wheels.
It found no authored nonzero-bounce material and no exact duplicate static bounds.
This does not rule out every overlap or certify all proxy dimensions.

Removing the only chassis box changed automatic inertia to (1,1,1), producing
artificial flips. Focused trials 14–16 and 34–36 are excluded from proxy causality.
They were followed by 12 corrected comparisons that kept the collider and its
normal inertia, ignoring only chassis/world contact pairs before physics steps.
All six corrected contact-off passes recorded zero chassis collision events.

Even then, the tractor rose 1.09 m over the tall pile and 0.72 m on shallow snow,
with recorded snow wheel contacts. The loader also rose 1.09 m on the tall pile.
The proxy therefore is not necessary for a snow hop. Removing it would also let
the machine pass through walls; it is not a production fix.

### COM changes and collision separation

The loader had roughly 22 cm per-step COM changes during collection/auto-lift.
However, fixed-COM drives still launched. A stationary full-bucket dump shifted
COM about 33 cm overall, without becoming airborne; empty/full/normal/fixed-COM
stationary tests remained grounded. COM smoothing may improve feel but is not
the first remedy for these launches.

A 2 m/s separation limit helped one loader foundation pass (about 0.07 m rise),
but a tall-pile pass still rose 4.05 m and tilted 160°. This is a possible
vehicle-specific safeguard, not a complete fix. Unity warns that excessive
separation speed can destabilize collisions, while setting it too low can leave
penetration unresolved. [Unity documentation](https://docs.unity3d.com/cn/6000.0/ScriptReference/Physics-defaultMaxDepenetrationVelocity.html).

## Recommended implementation order

1. Replace hard per-cell snow support with smoother, more stable collision/support
   transitions. Avoid introducing solid support inside a moving vehicle. Keep the
   visual style, volume ledger, bucket capacity and working resistance. This will
   change how tyres climb/deform piles and requires new drive acceptance tests.
2. Correct and bound the suspension-travel calculation and cap anti-roll force.
   Preserve useful roll resistance rather than disabling the whole assistance
   system. This changes curb/foundation reaction and must be checked on slopes.
3. Tune per-vehicle separation limits and pitch/roll damping as safeguards, then
   recheck chassis clearance versus wheel footprint. Keep physical blocking.
   These settings trade gentler impacts against penetration/sticking behavior.
4. Smooth loader COM/load transitions if still needed after contact fixes.

Do not start by increasing mass, deleting chassis proxies, freezing all rotation,
or simply disabling all snow physics in production. Diagnostic exclusions isolate
causes; they are not the requested finished gameplay behavior.

## Evidence and reruns

- Logs/CollisionAuditMatrix: 68-case Results.csv, traces, contacts, snapshots.
- Logs/CollisionAuditFocused: 40-case comparison data; six invalid inertia cases
  flagged above. Foundation target-contact counts must be respected.
- Logs/CollisionAudit: final 12 corrected proxy runs and Validation.json.
- Each folder's Summary.txt records saved-file preservation after exiting Play.

Only trace/contact IDs listed in that folder's Results.csv belong to its run;
the working folder can retain older numbered files. Screenshots are early-event
frames, not necessarily peak motion. Matrix Trial-46-abnormal.png was inspected.

After an explicitly authorized native Compile and with clean saved Pinefall open,
create Logs/CollisionAudit.request for the matrix. For focused comparisons also
create Logs/CollisionAudit/Targeted.request; for corrected proxy-only comparisons
use Logs/CollisionAudit/ProxyOnly.request instead. Copy prior evidence first.
The opt-in runner enters Play, disables saving, runs and exits automatically.
No performance conclusions should be drawn from this instrumented Editor run.
