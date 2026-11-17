# Empty Alps terrain driving test

Open `Assets/Scenes/AlpsTerrainTest.unity`. This scene contains only the original
supplied terrain, one existing tractor and the necessary camera/light/control
helpers. The invisible TerrainData is used only for camera-height queries; the
supplied mesh provides physical collision. Original 1:1 source metres are used,
with no Alps game-scene height compression, edited hills, roads, buildings,
mountains, props, jobs, menus or deformable snow.

Editor Play is configured to start this scene directly. To restore ordinary
Editor Play, choose **Snowglow > Developer > Play current scene normally**.
Choose **Play terrain test directly** to enable the override again. The game's
build scenes are unchanged. These menu choices are Editor settings.

- W / Up: forward pedal
- S / Down: reverse pedal (existing stop/direction buffer)
- A / D or Left / Right: steering
- Space: brake; releasing pedals coasts
- R: return tractor to test spawn and reset motion
- C: cycle Orbit / Isometric / Follow cameras
- Orbit view: right-drag and scroll, or J/L, I/K and +/-

The sandbox does not create a PrototypeSession, load a profile or save progress.
The raised blade, stock parts, full fuel and full condition start fresh each Play.
It calls the same MachineController driving step as normal gameplay, with no
snow field. Normal gameplay retains its original session gates and snow logic.

The attachment checksum matches the retained source and reviewed FBX export.
Authoring is Editor-only: after native compilation succeeds, queue
`Logs/AlpsTerrainTest.request` with `build` once, or `play` for the isolated live
checks. Build refuses to overwrite an existing test scene. Evidence and backups
are under Logs/AlpsTerrainTest. This scene is for PC Editor terrain testing;
mobile controls, snow work and Android performance are outside this test.

Native scene save/reopen, existing direction/recovery checks and actual Play
startup/support/acceleration/coasting/reset checks passed. Protected game scenes,
build settings, source assets, tractor prefab and real profiles are unchanged.
Before running another Editor Play fixture, clear the test-scene startup override
and restore it afterward if you want to keep using this sandbox.
