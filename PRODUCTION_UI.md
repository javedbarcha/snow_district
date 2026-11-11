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
- Machinery selection is exclusively in Garage: one 3D preview with swipes,
  previous/next buttons and keyboard arrows. Browsing does not purchase equipment
  or change the selected machine. The obsolete machinery-page route redirects
  to Garage; job selection has no intervening vehicle screen.
- Garage / Shop: Garage contains the Shop tab. Both use the supplied three-column
  layout: stats on the left, a single 3D display with previous/next and swipe
  browsing in the middle, upgrades or part descriptions on the right.
  Garage browses owned and unowned vehicles; Buy Vehicle purchases and Select &
  Back changes the saved selection and returns to the originating screen. Existing stopped /
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
No new billing or networking is introduced. Profile schema 2 is retained with
optional, versioned Career progression and active Quick Play mode fields.

## Completed game-flow loop — 11 November 2026

Career follows Main Menu > Region > Town > Job > gameplay. A new save starts
with Depot Access; a successful Career completion unlocks the next Pinefall
job. Completing all three unlocks the next Alpine town. Regions require all
towns in the previous region completed. Content that has not been built remains
unplayable even when its prerequisite is satisfied: The Alps can show UNLOCKED /
COMING SOON, never a working button to a nonexistent scene. The unlock rules
live separately in CareerProgression with stable town/job identities.

Quick Play is a shuffled, duplicate-free pool of already-unlocked available
town jobs. It uses the selected Garage machine and the existing coin / XP
rewards, but it does not mark Career jobs complete or unlock locations. Shuffle
and page controls act on that pool; empty slots hide the entire card. Ordering
stays stable until the player explicitly shuffles. Only Pinefall currently has
implemented jobs.

Results offers Replay, Garage, Main Menu and Next Job; Quick Play labels the
last action Shuffle Again. Completed Career towns return to the town list.
Main Menu and Pause retain the unfinished shift; Continue resumes it. Starting
a replacement job still requires explicit abandonment, preserves already-earned
wallet/upgrades/vehicle condition, resets snow/carried loads and creates a fresh
reward transaction. Abandoning without a replacement resets the transient
worksite in place and returns to Main Menu without an unpaid reward.

Garage remembers whether it was opened from gameplay, Pause, Main Menu, a job
list or Results. Selecting machinery returns to that origin; no active job means
it cannot enter gameplay. Returning from Settings or Test Transactions preserves
the viewed vehicle, part and Garage/Shop tab. All existing depot, stopping,
ownership, affordability and compatibility checks still apply. Rejected actions
leave the Garage usable with Back/Resume available. Escape follows the same
return rules and dismisses a confirmation before leaving a screen.

Legacy schema-2 saves without careerVersion retain access to all three jobs
previously offered, without inventing completion records. Wallet, XP, fuel,
condition, carried snow, active contract and snow ledger are retained. New saves
receive no legacy unlock grant. Progression and active Quick Play mode survive
JSON roundtrips and normal autosaves; validation never writes the real profile.

Native acceptance: Unity 6000.3.8f1 saved/reopened Pinefall and passed 149 flow /
progression checks, 635 UI checks, scene validation and 19 snow checks. Android-
defined compilation passed. New-career, Quick Play and next-town screenshots
were inspected. 4,656 protected world records matched the pre-task snapshot.
Reports: Logs/GameFlow and Logs/ProductionUI. Full live Play traversal, Android
packaging and Galaxy A07 touch/performance remain separate acceptance tasks.

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

## Starting another job

If a contract or Free Work is pending, choosing another job opens a confirmation:
Continue Current Job resumes the existing job; Abandon & Start New Job discards
its progress and unpaid rewards and immediately starts the requested job. The
close icon / Escape dismisses the dialog and leaves the current job untouched.
Fuel, condition, ownership, upgrades and the selected vehicle are retained.
The new job receives a fresh snow worksite, empty carried loads and a new reward
transaction. Repeated acceptance cannot restart it twice. The same confirmation
also protects restarting the same contract. No automatic completion payout.

Job-conflict revision completed: Unity saved/reopened and passed 559 native UI/session checks, scene validation and all 19 snow checks. The job-conflict.png capture was visually inspected. Continue, close, same-contract consent, contract/Free Work replacement, retained funds/vehicle state and duplicate acceptance passed. Android-defined compilation passed; live phone acceptance remains separate.

## Shop test transactions

Garage > Shop > Test Transactions opens the existing simulated transaction page.
Back to Shop returns to the Shop tab. This is test currency only, with no real
money charged. New transaction followed by Success grants 2,000 coins once;
redelivery is duplicate-safe, and Cancel / Fail grant nothing.

## Inspected screen cleanup (11 November 2026)

Main Menu uses only footer Resume. Global Settings is below the header at upper
right and hidden on Settings and Pause. Pause keeps its main Resume / Settings
buttons, with shared footer Resume hidden. Town selection uses Back only.
Results keeps stats, Replay and Main Menu; Next Job directly starts the following
Career contract only if one exists. No results Garage or Shuffle action remains.
Quick Play shuffling stays on the Quick job-selection screen.

Depot Access's former work area was already snow-free at seed startup, causing
instant replay rewards. Its target is now the snowy adjacent road at x -59..-29,
z -69.5..-60.5. Only target/credit selection changes, not seed snow or geometry.
This also changes the progress calculation for an already active Depot Access.

Returning to gameplay stops momentum, queued forces, wheel spin/steer/torque,
drive-direction buffering and held input. It retains the current pose and load,
fuel and condition. New-job/recovery still perform their intended depot move.

Completed native validation: 600 UI checks, 175 flow/progression checks, 1,108
isolated recovery/resume physics assertions, scene validation and 19 snow checks
passed on Unity 6000.3.8f1 / Android. Completed-shift reports retain the actual
clearance, earned coins and XP independently of other menu notices. Native
Career / Quick / final Results and the cleaned screens were visually inspected.
Android-defined compilation passed; packaging and phone touch/driving remain
untested. World preservation: Logs/ScreenCleanup/WorldPreservation.json.
