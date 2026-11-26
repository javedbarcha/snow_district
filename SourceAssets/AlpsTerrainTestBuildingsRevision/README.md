# Alps terrain-test building revision

The user-supplied `Snow_District_Alps_Buildings(1).blend` is preserved here as
`Snow_District_Alps_Buildings.blend` (SHA-256
`14B5805FC3ED8BE9685718BFB87F59EE3EF31ACC7B9A5A3111DA639910581916`).
`ReimportManifest.json` records the eleven exported variants and the source hash.

The test-only FBXs in
`Assets/Snowglow/Levels/AlpsTerrainTest/BuildingFaceRepair/Sources` were prepared
with Blender auto-execution disabled using
`Tools/AssetIntake/export_alps_face_repair.py`. The export retains the existing
part names/material bindings, corrects 214 inward-facing exterior panel faces and
raises 62 roof-snow vertices by at most 0.224 m. Source meshes, catalogue assets,
production scenes and gameplay code are not modified by the reimport.

`AlpsBuildingFaces`'s `apply-buildings` action refreshes the 75 map placements in
`AlpsTerrainTest.unity`. It preserves transforms, box colliders and materials,
then saves and reopens the test scene. Evidence and pre-import backups are under
`Logs/AlpsBuildingsReimport` and `Logs/AlpsBuildingFaces`.

### Attached-garage wall correction

The `Building_02` source already has correct outward winding on the garage's
left side and rear attachment walls. The initial test export flipped these two
faces because its radial test used the whole-building origin. The exporter now
preserves those exact faces and fails if it cannot identify both. Only the
`Building_02` test FBX changed; round-trip comparison confirmed only two face
normals changed, with vertices, topology, materials and transforms unchanged.
The 75 test-scene placements were refreshed and saved/reopened. The original
Blender source and production/catalogue assets remain untouched. Backups and
validation captures are under `Logs/AlpsBuildingFaces/GarageWallNormalFix`.

### Attached-garage roof/ceiling face correction

A further review found one horizontal panel facing away from the garage
interior. The exporter now targets the single source face centered at
`(5, -7, 3)` with area `23.52` and reverses its normal toward the interior. A
Blender FBX round-trip comparison against the wall-corrected version confirmed
that exactly this one face normal changed: the 1,580 vertices, 1,018 polygons,
materials, geometry and both corrected wall normals are unchanged. The corrected
test FBX is installed at the existing path, retaining its `.meta` GUID; SHA-256
is `7B68F717C35CEEFAD65902B8691DEC61EB1C25DC1BB91D18B704A63DC2008229`. The
previous wall-corrected FBX is backed up under
`Logs/AlpsBuildingFaces/BeforeGarageRoofFix`.

After a successful forced Unity compile/domain reload, the `apply-buildings`
action refreshed the 75 test-scene buildings and saved/reopened the scene.
Transforms, hierarchy, materials and box colliders passed the existing checks;
four-sided `Building_02` captures were regenerated and inspected. Production
scenes and catalogue assets remain untouched. No Play or device test was run.
Evidence is under `Logs/AlpsBuildingFaces/GarageRoofNormalFix` and
`Logs/AlpsBuildingFaces`.

### Attached-garage triangular gable correction

The user clarified that the remaining fault was the vertical triangular wall at
the end of the garage roof, not the horizontal ceiling panel described above.
The source has two gable ends; the rear end (source centre `(5, -4.5, 3.433)`)
was facing inward. The exporter now orients it outward while retaining the
already-correct front gable. A Blender FBX comparison against the installed
ceiling-corrected version confirms exactly one normal-only face change; the
1,580 vertices, 1,018 polygons, materials, both garage walls and ceiling
correction remain unchanged.

The updated test FBX retains its existing `.meta` GUID and has SHA-256
`C276A8E0159699A183E74974B50DB6E7F1C646155EC03EFBA4916D2C72353AD9`. The
previous version is backed up under `Logs/AlpsBuildingFaces/BeforeGarageGableFix`.
After a successful forced Unity compile/domain reload, `apply-buildings`
refreshed all 75 map placements; save/reopen checks and four-sided previews
passed and were reviewed. Blender backface-culling views confirm both gable
ends face outward. Production scenes and catalogue assets remain untouched.
No Play or device test was run. Evidence is under
`Logs/AlpsBuildingFaces/GarageGableNormalFix` and `Logs/AlpsBuildingFaces`.
