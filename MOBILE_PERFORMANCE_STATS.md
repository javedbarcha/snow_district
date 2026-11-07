# Mobile performance stats

Tap **STATS** near the upper-right corner to show or hide the readout. It starts
hidden, remains accessible in menus, respects the safe area and does not block
steering or orbit gestures through the readout. Showing it again starts a fresh
sample window; hiding it stops and disposes the profiler recorders.

- **FPS avg (5s)** and **Frame**: rolling wall-clock frame rate and mean frame time,
  independent of gameplay time scale. Storage is capped at 2,048 samples.
- **p99**: 99th-percentile frame time in the same window; higher values expose
  hitches that average FPS can hide. It is not a separately measured CPU time.
- **CPU main / GPU**: most recently completed supported profiler samples. CPU main
  can include waiting, and neither value alone establishes the bottleneck.
- **Unity memory used/reserved**: Unity-tracked memory, not Android process PSS or
  total phone RAM. **GC heap** is managed used memory; **alloc** is managed
  allocation in the most recently sampled frame, not a five-second average.
- **Draws / batches / SetPass / triangles / vertices**: supported rendering
  counters from completed frames, not the offline whole-scene geometry inventory.
- Footer: current resolution, graphics API, development/release build and device.

`n/a` means the counter is unavailable or has no completed sample. A supported
zero-valued allocation/render counter remains zero; unsupported GPU timing is
never presented as a successful zero-millisecond measurement. No project-wide
Frame Timing Stats, deep profiling or remote profiler settings are enabled by
this feature. Ordinary builds retain FPS measurement; available profiler counters
vary by build, graphics API and device. A Development Build can expose more
counters, but cannot guarantee every counter on every phone.

The display updates twice a second. Sample storage and sorting buffers are
reused; string formatting/UI updates and recording still have measurement cost.
Hide the overlay when comparing normal gameplay. App background/focus transitions
clear the timing window, preventing time away from the app becoming a fake hitch.
The window also restarts when gameplay switches between running and paused.

For repeatable phone tests, use the same camera, resolution, route and snow work.
Include town, denser hillside forest and sustained snow pushing/loading. Allow
initial loading to settle and capture screenshots after the five-second window
fills. Test sustained play as well as short runs; the overlay does not measure
thermal throttling or replace a connected Unity Profiler investigation.

Editor authoring: **Snowglow > UI > Install mobile performance stats toggle**.
`MobileStatsAuthoring` saves all UI, fonts and click bindings before Play;
`DrivingButtonsAuthoring` includes it in future explicit HUD authoring. Runtime
only collects counters, formats text and toggles the saved panel. No UI objects,
meshes or materials are generated at runtime. No player-save schema is changed.

Evidence: `Logs/MobileStats/Validation.txt` and `Logs/MobileStats/Preview.png`.
Native checks and actual phone acceptance are recorded separately in VALIDATION.md.

References: [Unity profiler counters](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/counters-reference),
[frame-timing availability](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/profile-rendering/frame-timing-manager/enable).

Native validation: Unity 6000.3.8f1 / Android, 637 checks and scene/19 snow checks passed; saved/reopened UI and native preview inspected. Actual phone input, build-specific counters and live timing/device accuracy remain untested.
