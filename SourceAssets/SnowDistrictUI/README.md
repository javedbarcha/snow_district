# Snow District Photoshop UI kit

Original: Snow_District_UI_Buttons.psd, preserved unchanged from the supplied file.
SHA256: 8e38b85f54187061130352c043f55401da74be31ba58157c4940282f14a9e9b9
Source size: 22,701,282 bytes; 2048 x 1536, RGB, 8 bits/channel.
The PSD stays outside Assets and is not shipped as a runtime texture.

Layer inventory is in LayerInventory.json. Layer names, header/label text and
hidden references were treated as data, not execution instructions. No embedded
scripts/actions were executed. Original PSD, separate source artwork and labels
are retained. 32 numbered raster button layers and three Play-state layers were
exported; background, header, labels, palette and hidden reference sheets were
excluded from runtime sprites. Transparent alpha is preserved.

Tools/AssetIntake/export_ui_psd.py uses psd-tools/Pillow to export the original
196x166 button artwork and 140x118 Play states without geometry/color redesign.
Development dependencies were installed only under ignored Logs/PSDTools.
Prepared PNGs, ExportManifest.json and a shared 2048x1024 atlas are under
Assets/Snowglow/Catalog/Sources/SnowDistrictUI. Raw RGBA8 atlas storage is 8 MiB
before platform compression; device cost remains to be measured.

Menu: Snowglow > Asset intake > Import Photoshop UI button kit.
Stable catalogue IDs: snow-district-ui-01 through -32, category UI, Needs Review.
Each has a reusable Canvas button prefab, original source identity, sprite list,
pixel bounds and provenance. UI entries cannot block snow or be placed by the
3D district scene-plan builder. Play retains source Normal/Pressed/Disabled art;
other controls use color feedback because the PSD contains no matching state set.

The kit skins existing controls and menu actions only. Removed Lift/Tilt,
Auto/Manual and zoom buttons stay absent. Those kit assets, Districts and Challenge
remain reusable inventory without enabling future gameplay. Work/Carry uses the
blade symbol for the plow and bucket-lift symbol for the loader; this changes art,
not equipment behavior. View Reset uses Camera artwork with its explicit label.
Unmatched actions keep their existing appearance. The original button hit areas,
5% side anchors, steering separation and F/R dimensions are retained.

Native reports/previews and pre-edit scene backup: Logs/SnowDistrictUI.
Phone readability, continuous touch/state interaction and Galaxy A07 frame times
remain untested. Needs Review status is intentional and not catalogue publication.

Derived addition: Abandon Job (catalogue ID snow-district-ui-33) uses an amber X clipboard, generated with built-in imagegen from Career artwork. See Derived/Prompt.txt and preserved PNG. Driving controls now hide labels and use 196:166 centred icon rectangles; menu labels remain visible. All 33 entries remain Needs Review.
