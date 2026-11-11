# Production vehicle handling

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
