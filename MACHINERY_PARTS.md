# Machinery parts and individual upgrades

Each vehicle has seven compatible parts: **three tyre types, two engines and two
blades** (buckets for the loader). Counts include the three factory parts, which
start owned and fitted. The other four are purchased separately for that vehicle.
These are gameplay variants using the current reviewed models and attachment rig;
the Shop reuses those models as previews. New replacement meshes are not supplied.

| Part | Tractor price | Loader price | Base effect |
| --- | ---: | ---: | --- |
| Standard tyres | Included | Included | Original grip |
| Winter tyres | 450 | 550 | Grip ×1.12 |
| Chained tyres | 750 | 850 | Grip ×1.25 |
| Standard diesel | Included | Included | Original torque |
| Turbo diesel | 900 | 1,100 | Torque ×1.22 |
| Standard plow / bucket | Included | Included | Original snow transfer rate |
| Heavy-duty plow / bucket | 650 | 800 | Snow transfer rate ×1.20 |

Prices and differences are starting values for the upcoming performance audit,
not a completed handling/economy balance. `MachineryParts.cs` contains the stable
catalogue keys, per-vehicle prices and effects. IDs combine the vehicle definition
ID with the part key; purchasing tractor tyres never grants loader tyres.

## Purchase, fit and upgrade

Garage arrows select machinery. Shop arrows browse its seven parts. **Buy & Fit**
purchases an alternative at level zero and fits it. **Fit owned part** swaps back
without charging again. Replaced parts remain owned and retain their levels.
The separate **Upgrade** action raises the displayed owned part by one level; it
does not implicitly fit it. Garage upgrades affect the fitted tyre/engine/blade.

Every individual part has three upgrade slots, starting at 0/3. Slot prices are
200, 400 and 600 coins. For the chosen part, grip/torque/snow-rate multipliers are:

`base variant multiplier × (1 + own upgrade level × bonus per level)`

Bonuses are 12% grip for tyres, 18% torque for engines and 10% snow transfer for
blades/buckets. Existing engine upgrades still improve snow processing as before;
the effective snow rate multiplies engine and blade factors. These change actual
wheel grip, motor torque and attachment collection/plowing throughput. Speed
limits, steering, fuel consumption, attachment width/mass/capacity, poses,
colliders and suspension are unchanged. The full bucket still auto-lifts at the
existing 92% threshold. Better grip/torque can change rollover/stall behavior;
audit the combinations before setting final values.

Transactions require an owned machine and a stopped active vehicle at the depot.
Confirmation pins the target machine and part. Cancellation has no effect;
duplicate acceptance cannot charge again. The backend also checks ownership,
funds and the three-level limit instead of relying only on disabled UI buttons.

## Saving and compatibility

Schema 2 adds `MachineSave.parts` and `fittedParts`. Each part stores its stable
ID, ownership and individual level. Fitted IDs are saved for all three categories.
Old saves without these fields retain their original tyre and engine levels on
the default parts. Missing/default selections recover to the owned factory part;
foreign IDs and unowned selections are rejected and levels are clamped 0–3.
Legacy tyre/engine fields remain for compatibility with existing diagnostics.

This gameplay inventory is independent of the Editor Asset Workshop's source
preparation catalogue. No source model is marked Ready or fitted automatically
through a category string. Shop display meshes are authored and saved in Edit
mode; no runtime mesh, material, model or UI factory was introduced.

## Authoring and checks

After a successful native compile/domain reload, create
`Logs/MachineryParts.request`. The targeted installer backs up the saved scene
and any unsaved edits, rebuilds only saved part previews/upgrade controls, saves
and reopens Pinefall, then runs catalogue/transactions/migration/UI/flow/Career
snow/scene/snow/recovery/loader checks. Full menu authoring reuses the same setup.
Evidence: `Logs/MachineryParts`, `Logs/ProductionUI`, `Logs/RecoveryPhysics` and
`Logs/LoaderCycle`. Fixtures disable player-save writes. Native UI renders are
separate from live driving, Android packaging and phone performance acceptance.

Validation on 15 November 2026: Android-defined C# compilation and native Unity
6000.3.8f1 forced compile/reload passed. Saved/reopened scene passed 183 parts
checks, 661 UI assertions, 187 flow checks, 34 Career snow checks, 19 snow-ledger
checks, 1,108 isolated recovery and 1,134 loader-cycle assertions. Native Garage,
tyre, engine and blade renders were inspected. All 14 save/seed file hashes and
1,234 protected physics/Terrain/prefab blocks matched. No Play entry or player
saves were written. These are data/flow/default-loader regressions; the upgrade
combination driving/performance matrix and phone acceptance have not run.
