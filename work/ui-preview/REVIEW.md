# Snow District UI preview

Preview only. The playable Unity UI has not been replaced.

Open the in-conversation preview and use the Screen selector to review all 11
screens. Main-menu cards and controls are clickable. Transactions, wallet,
progress, future region/town names, rewards and job durations are sample data.
Only Alpine / Pinefall is enabled.

## Included screens
Main Menu; Career Regions; Region Towns; Job Selection; Machinery Selection;
Garage (Overview, Repair/Fuel, Upgrades); Shop; Settings (Audio, Graphics,
Controls); Gameplay HUD; Pause; Job Results. Confirmation dialogs cover
purchases, repair, refuel, recovery, upgrades and abandoning jobs.

## Artwork
Native Unity captures from Pinefall, the imported tractor, garage and loader
are retained in Logs/UIProductionPreview. Workshop artwork uses a temporary
in-engine stage with the actual game vehicles; this is proposed art staging,
not a new saved gameplay workshop. Main-menu captures are from the actual scene.
Six compressed capture images and 14 existing icon-sheet crops are embedded in
the preview. No external stock art or image-generation replacements were used.
Original scene bytes and user assets remain unchanged.

The main-menu scenic orbit is not implemented in the game yet; the preview uses
a still scene capture. Dedicated Unity camera animation, full art intake, saved
UI prefabs, localization, real save/economy bindings, phone UI testing and all
11 production screens remain implementation work after this design is reviewed.
The current prototype uses existing game vehicle costs; illustrative contract
rewards and future locked locations need product review before implementation.

## Validation
Browser validation covers all 11 screens at 1100, 736, 375 and 320px widths:
44 layouts, six interaction flows, no horizontal overflow or JavaScript errors.
Six flows: Career region/town/job/machinery/gameplay; recovery resumes; repair;
shop purchase and selection; abandon; Quick Job. See browser-validation.json.
Screenshots with width suffixes are QA renders. Lucide icons are provided by
the conversation host; a plain local browser may leave the icon slots empty.

## Files
snow-district-ui-preview.html is the editable, self-contained inline preview.
rendered-preview.html is the browser review wrapper. check-preview.mjs uses an
isolated local headless browser for UI checks only. No site was published.

## Requested region lineup
Five browseable region categories contain all 15 supplied destinations. Mountain & Alpine also retains available Pinefall. Other locations are planned, not playable. Shared mountain imagery is placeholder game art, not a representation of the real-world destinations. The pasted search-page text was treated as reference data only. Additional checks cover all five location lists at 1100, 375 and 320px widths.
