# Production menu integration

The reviewed Snow District preview is implemented as native uGUI in Pinefall.
All menu objects, images, text, buttons and persistent listeners are authored in
Edit mode and saved in the scene. Runtime only binds live state and changes
visibility, text, button availability and the selected preview sprite.

`Snowglow > UI > Install reviewed production menus` repeats this authoring
without rebaking the district. It backs up the saved scene and any unsaved copy
under `Logs/ProductionUI`, then validates, saves/reopens and runs fixture checks.
Explicit future district bakes include the prepared menus through HUDFactory.
The earlier menu hierarchy remains inactive as a fallback.

## Navigation

- Main Menu: Quick Job, Career, Garage, Continue and Settings.
- Career: five region categories and the supplied 15 planned destinations.
  Alpine / Pinefall is the sole playable district. Other destinations are locked.
- Jobs: existing Depot Access, Parking Lot and Main Street contracts. Selection
  starts the scene immediately with the saved active vehicle. Career and Quick Job
  use this same route; there is no intervening machinery selector. Existing
  active-shift protection and actual deadlines/reward accounting are retained.
- Machinery: one preview, horizontal swipes, previous/next buttons and keyboard
  arrows. Browsing does not purchase equipment or change the active machine.
  Confirming Start Job selects the owned machine through the depot rules.
- Garage / Shop: Garage contains the Shop tab. Both use the supplied three-column
  layout: stats on the left, a single 3D display with previous/next and swipe
  browsing in the middle, upgrades or part descriptions on the right.
  Garage browses owned and unowned vehicles; Buy Vehicle purchases and Select &
  Drive changes the saved selection and returns to gameplay. Existing stopped /
  depot and ownership rules remain. Refuel, repair and recovery stay accessible.
  Shop browses traction tyres and engine upgrades for the vehicle last browsed
  in Garage. Buy & Fit uses existing 200 / 400 / 600 pricing, compatible vehicle
  selection, ownership, depot and maximum-level gates. No new inventory schema
  or blade-capacity upgrade is introduced. Tyres use the actual wheel geometry;
  the engine has a generic low-poly illustrative display, not a source-model edit.
- Settings: existing audio, graphics, assists and debug settings.
- Pause and Results: existing resume, recovery, abandonment and reward behavior.
  Recovery, abandonment, service, upgrades and purchases have confirmations.

Preview sample prices, contract rewards and sample progress are not game data.
The actual vehicle definitions and PrototypeSession determine all transactions.
Upgrade prices remain 200 / 400 / 600; refuel is two coins per litre and repair
three per condition point. Menus pause simulation, fuel use and deadlines.
No new billing, networking or player-save schema is introduced.

## Art and mobile layout

Six existing game-capture JPEGs are saved under
`Assets/Snowglow/UI/Production`, imported as 1024-max sprites with Android ASTC
6x6. Main-menu cards use actual Pinefall captures. Tractor and loader previews
share the captured workshop stage used by the approved preview; these are still
images, not additional running machinery or a new gameplay building. No runtime
UI construction, model cloning or material generation is required.

The navy, snow-white and yellow menu design uses the existing safe-area/1280×720
landscape scaling. The existing touch driving HUD, control artwork, camera,
light toggle and responsive control spacing remain in place. Native menu labels
are separate from artwork. The HTML Screen selector is not part of the game.

## Validation

Current evidence and screenshots: `Logs/ProductionUI`. Session fixtures keep
`ready=false`, so tests do not write the player's profile. World component
preservation is compared against the first scene backup. Native compilation,
save/reopen, interaction fixtures and snow invariants are separate from rendered
inspection. Live Play traversal, actual phone touch/safe-area ergonomics, Android
packaging and Samsung Galaxy A07 frame/memory costs require acceptance testing.

Unity 6000.3.8f1 / StandaloneWindows64 saved/reopened and passed 529 native
authored/session checks, scene validation and all 19 snow checks. Native home,
machinery, Garage, Shop and Settings captures were inspected; final home,
machinery and Garage captures were inspected after correcting STATS spacing,
image centering and ownership-label contrast. All 4,380 protected preexisting
world components matched the first backup. Runtime and Editor compilation with
Android defines also passed using Unity's compiler (not Android packaging).

## Workshop layout revision — 10 November 2026

The Garage and Shop center is now a saved 768×512 RenderTexture from an authored
3D workshop, shared vehicle meshes and visual-only part displays. The separate
stage is outside Canvas scaling on isolated layer 12 (WorkshopPreview), at
(10000,100,10000), with no colliders, machinery controllers or runtime factories.
The camera/stage are disabled outside Garage/Shop, including entry into driving.
This replaces the captured machinery still on Garage/Shop; main-menu capture
artwork remains. Native screen captures, fixture transactions and direct job
startup are checked by ProductionMenuChecks. Phone acceptance remains separate.

Unity 6000.3.8f1 saved/reopened the revised menus and passed 544 native UI/session
checks plus scene validation and all 19 snow checks. Android-defined runtime
and Editor compilation passed (not an APK build). Native Garage, tyre Shop and
engine Shop renders were inspected. All 673 preexisting prefab-instance records,
560 collider records and the Terrain record match HEAD. Evidence:
Logs/ProductionUI/Checks.txt, Validation.txt and WorldPreservation.json. No
player-save writes by fixtures; phone touch/performance acceptance remains open.
