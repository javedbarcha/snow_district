# Production vehicle handling

## Full production rerun — 12 November 2026

77 actual Play trials completed: original 73 plus four high-speed full-steering
turns. User accepts turning tip risk as gameplay for recovery/damage/repair.
No handling tuning changes made. Native support (37), scene, snow (19), recovery,
direction-buffer and loader regressions passed. Scene/profile/backup hashes matched.
All 48 moving snow cases had actual snow wheel contacts; max rise .635 m, tilt
16.64 deg and .06 s with no wheel contact. All 12 foundation cases reached target
and retained wheel contact; max rise .533 m and tilt 26.03 deg. A short impact pose
correction remains (max upward root step 7.48 cm); no straight-line snow/foundation
launch-like flight reproduced. Four faster ramp runs fell and overturned off the
raised deck; real landing jolts remain. One fast loader turn leaned 64.71 deg;
intended turn risk retained. Reports: Logs/HandlingFullReview/Review.md and Play.
Telemetry triage does not prove zero visible jerk. No A07 performance/packaging
or live manual driving acceptance. Details/trace plots preserve these limitations.

## Latest tuning acceptance — 12 November 2026

User authorized tests after the visual-first changes below. 36 actual Play cases
covered both vehicles, raised/down tools, target speeds 2/4/6.5 m/s, flat .3 m snow,
initial 2.4 m narrow piles and 1.5 m uneven snow. Raised-tool cases remained grounded
throughout (18 cases), maximum tilt 20.70 degrees and rise .668 m including climbing.
Down cases reached 16.61 degrees/.636 m rise/.06 s continuous airborne. Every case
recorded actual snow wheel contact. No raised-tool instability reproduced, so the
user's conditional extra raised-pile stabilization was not added. 37 native support/
vehicle checks plus scene/snow/recovery/direction/loader regressions passed.

The first run rewrote machine-state profile data; exact pre-run bytes were restored
from its matching unchanged backup. An Editor-only pre-fixture Save guard now blocks
diagnostic startup/focus writes. A separate Play check preserved all scene/profile/
backup hashes. Evidence: Logs/SuspensionReview. No reverse/full-load matrix, Android
packaging, phone performance or subjective suspension-motion acceptance claimed.

## Pending visual review — 12 November 2026

Suspension now uses freer historical settings outside actual lowered-tool snow
contact: angular damping 0.5, unlimited suspension expansion and original authored
wheel force application distances. Lowered Working attachment contact, sampled at
mouth and both blade edges with the existing height reach, enables damping 0.8,
limited expansion, lower force points and extra assisted tilt-spin damping.
Bounded anti-roll and collision safeguards remain in both modes. Springs and
travel were not changed. This restores freedom rather than unsafe old anti-roll;
visual motion and transition stability remain untested pending user review.

Additional deep-pile resistance samples at the leading edges of the front tyres
(rear tyres when actually reversing). Each tyre contributes half of
min(4, 3 * max(0, depth - 0.45)^2) m/s². Thus both tyres in uniform 0.5 / 1 / 1.5 m
snow add approximately 0.0075 / 0.9075 / 3.3075 m/s² opposing acceleration, capped
at 4 m/s². This is additional to existing centre-depth drag and attachment force.
It acts horizontally at the body only while grounded and moving, capped to avoid
reversing current motion by itself. It can strongly slow piles but is not a static
holding brake; sufficient torque/upgrades may still climb. Collider geometry and
support, grip and anti-roll are unchanged. No tests/Play run: visual review pending.

General snow drag now uses mass * (0.025 + depth * 0.25), increased from the
previous depth multiplier of 0.12 at the user's request. Sampling remains beneath
the vehicle centre; grip and attachment work forces are unchanged. This adds
resistance without guaranteeing a pile stall. Tests remain deferred until the
user's visual review; prior acceptance predates this adjustment too.

User requested approximately half as much tyre penetration and explicitly deferred
tests until their own visual review. Support now blends the previous compacted
height halfway toward raw depth before the existing weighted filter. On broad,
settled snow this halves the gap between support and filtered visual height.
For 1 m uniform snow, support changes from 0.3575 m to 0.67875 m above ground.
Filtering, 1 m mesh spacing, update rates and vehicle safeguards remain unchanged.
Edges and changing piles need not show exactly half the penetration. Higher support
can increase pile response and bumpiness. Previous acceptance below predates this
tuning; no new tests or Play runs were requested or performed. Diagram:
Reports/SnowSupport/Snow-collision-explained.png.

The collision audit is preserved in `SNOW_COLLISION_AUDIT.md`. The production fix
replaces the hard, stepped snow support and bounds the vehicle's corrective forces.

## Snow support

The visible 0.5 m snow columns and volume/credit ledger are unchanged. Physics uses
a separate compacted height surface, sampled every metre with a shared six-cell
filter across chunk boundaries. Only depth above 0.45 m contributes, at 65% of
that excess. Deep snow remains physical; shallow snow retains its existing
resistance and collection behavior without hard cube steps.

Initial support is prepared before driving. Later support grows at at most
0.35 m/s and descends at 1.2 m/s, with each update capped at 0.1 seconds of change.
This deliberately permits soft visual snow to sit above the collision surface.
Each active support chunk reuses one 289-vertex / 512-triangle mesh. Pending
updates use a fair, two-chunk-per-frame queue alongside existing visual updates.
No snow is removed, credited or disposed by the support calculation.

## Vehicle safeguards

- Anti-roll suspension travel is clamped to 0–1. Both wheels must have usable
  ground contact; correction is capped at 10% of current vehicle weight per axle.
- Wheel contacts steeper than 60 degrees receive no motor torque or tyre grip.
- Suspension expansion is limited; tyre forces act approximately 0.25 m below
  the current centre of mass.
- Per-body separation speed is 2 m/s, the angular limit is configured to 3.5 rad/s,
  and solver iterations are 10 position / 4 velocity.
- Angular damping is 0.8. Driving assistance additionally damps pitch/roll,
  especially while airborne, without damping steering yaw or braking translation.

Chassis colliders, mass/load/centre-of-mass calculation, coasting, pedals,
attachment controls, economy and recovery rules are retained. Rotation is not
locked. The changed snow support and correction forces will change pile climbing
and impact feel; Android feel and performance require device testing.

## Validation

`ProductionHandlingChecks` runs only on explicit
`Logs/ProductionHandling.request`. It checks physical raycasts, chunk seams,
graded steps, bounded growth, unchanged snow data and vehicle safeguards, then
runs existing snow, recovery, direction-buffer and loader-cycle regressions.

The explicit Play suite uses `Logs/CollisionAudit/Production.request` followed by
`Logs/CollisionAudit.request`, after native compilation succeeds. It runs 73
transient cases with saving/progression disabled. Preserve previous evidence
before rerunning. Ramp height gain is expected and must not be classified as a
launch based on rise alone. Target speeds are pedal-regulated, not imposed.

Current results are recorded in `Logs/ProductionHandling` and
`Logs/CollisionAudit`. No Android packaging or phone acceptance is implied.

### Completed native acceptance, 12 November 2026

Android-defined runtime/Editor compilation and the existing Android-target
Editor's forced compile/domain reload passed. Native checks passed: 29 support /
vehicle assertions, scene validation, 19 snow ledger checks, 1,108 recovery
assertions, 1,627 direction-buffer assertions and 1,134 loader-cycle assertions.

All 73 automated Play cases completed and returned to Edit mode. Scene, profile
and backup hashes were unchanged. All 48 moving snow cases stayed below 0.33 m
rise, 10.1 degrees tilt and 0.04 s continuous airborne time. Deep-snow cases had
actual snow wheel contacts. All 12 foundation approaches reached the intended
collider and stayed grounded; the largest lean was 20.6 degrees / 0.59 m rise.
Anti-roll travel stayed within 0–1 and peak correction was 4.66 kN across the
suite, compared with roughly 51–56 kN in the original audit.

| Same target-speed case | Before: rise / tilt | After: rise / tilt |
|---|---:|---:|
| Tractor tall pile, 6.5 m/s | 3.97 m / 166.7° | 0.17 m / 6.9° |
| Loader broad pile, 4 m/s | 4.95 m / 134.5° | 0.22 m / 5.8° |
| Loader uneven road, 6.5 m/s | 4.15 m / 163.9° | 0.31 m / 8.9° |

Both vehicles coasted with zero throttle and brake after pedal release, retaining
over 3 m/s at the end of the trial. Both steered through the gentle-turn trials.
All six ramp runs climbed at least 2.9 m; faster runs reached its raised end.

**Steep-drop limitation:** at the 4 m/s target, the loader tipped to 111.9 degrees
after driving off the approximately 5 m-high ramp end. This is a real ledge fall,
not the previous flat-road snow launch. Rotation remains free. Two ramp landings
briefly exceeded the configured angular limit (peak 4.26 rad/s), because Unity's
limit is applied before the solver; it is not a strict post-impact guarantee.
No COM smoothing was added because snow/edge acceptance passed with existing
load-dependent mass and COM behavior.

Consolidated evidence: `Logs/ProductionHandling/Validation.json`,
`Logs/ProductionHandling/Checks.txt` and `Logs/ProductionHandling/Play`.
The consolidated folder excludes stale screenshots from earlier shared-runner
tests. Its summary clarifies that the runner's historical “No handling edits”
header does not describe this revised production run.
