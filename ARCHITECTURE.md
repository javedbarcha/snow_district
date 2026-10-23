# Prototype structure and extension boundaries

## Authored content lifetime

`Editor/PrototypeAuthoring` orchestrates `DistrictFactory`, `MachineFactory` and `HUDFactory` in Edit mode. It writes native assets and the populated Pinefall scene. Generated objects are not recreated by runtime callbacks. Re-running the command intentionally replaces the scene and creates a fresh timestamped asset folder; earlier folders are retained.

`VehicleDefinition`, `AttachmentDefinition`, `PrototypePalette` and `SnowSeed` are immutable authored inputs. Scene references point directly to saved machinery, controls, camera, audio and snow chunks. Wheel/mount/body visual anchors are separate from chassis and wheel physics.

`Editor/SceneValidation` checks persisted machine rigs, attachment compatibility, snow seed/chunk assets, terrain, shared materials, URP and build-scene inclusion. Its menu command reopens the saved scene and runs the nine snow ledger invariants. Baking checks the scene before writing the ready marker and treats scene-save failure as an error. First-import baking is disabled in batch mode so validation never silently regenerates content.

## Runtime mechanics

- `MachineController`: Rigidbody/WheelCollider movement, surface-dependent tyre friction, assistance, fuel, condition and carried-load mass/center of mass.
- `AttachmentController`: blade/bucket state, Auto/Manual input, working-edge queries, processing budget, carried volume and dumping.
- `SnowVolumeGrid`: engine-independent, authoritative volume and original-work-credit accounting. It refuses full/out-of-bounds/static-obstacle deposition.
- `SnowField`: field queries, chunk visual/collision refresh, target accessibility and disposal processing. Baked assets are cloned into transient mutable meshes so Play does not overwrite source assets.
- `PrototypeInput` / `TouchControl`: keyboard and multi-pointer touch input. Camera modes do not change steering interpretation.
- `CameraRig`, `SynthAudio`, `PrototypeHUD`, `UICommand`: presentation and player commands.
- `PrototypeSession`: small prototype coordinator for contracts, fleet selection, economy, XP, settings and pause states.
- `SaveStore`: versioned snapshots, safe file replacement and backup load.

The prototype coordinator intentionally keeps the first district compact. Before adding multiple regions or production billing, extract the economy, contract and progression sections into dedicated services while retaining stable definition IDs and the existing volume model. Do not add content by extending conditional chains indefinitely.

## Compatibility contract

Vehicles declare mount identity, hydraulics, permitted equipment mass and working width. Equipment declares requirements. Bucket compatibility includes rated full snow mass at 250 kg/m³. The two rigs are explicitly authored; matching strings alone does not create a new mechanical rig. Future blower/tracked implementations must supply the locomotion/attachment behavior and a fitted visual rig before compatibility is enabled.

## Expansion order

1. Prove/tune the two supplied machines on the phone.
2. Add authored fitting profiles and a genuine equipment inventory when introducing alternative attachments.
3. Extract contract/economy/progression services; add district and region content definitions.
4. Add a second district to validate content boundaries and stable save IDs.
5. Implement tracked locomotion or blower processing as distinct behaviors.
6. Introduce platform services/real purchasing behind tested transaction boundaries.

The primitive presentation can be replaced throughout this sequence. Preserve metre scale, Y-up/Z-forward axes, wheel pivots, attachment mounts, working-edge anchors and collision clearances.
