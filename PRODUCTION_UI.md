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
  opens machinery preparation; starting requires an owned machine. Existing
  active-shift protection and actual deadlines/reward accounting are retained.
- Machinery: one preview, horizontal swipes, previous/next buttons and keyboard
  arrows. Browsing does not purchase equipment or change the active machine.
  Confirming Start Job selects the owned machine through the depot rules.
- Garage / Shop: one screen with Garage and Shop tabs. Garage has Overview,
  Repair / Fuel and Upgrades sub-tabs. Purchases include the supplied bucket;
  selecting, servicing and upgrading use existing depot and ownership checks.
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
