"""Preserve the user ZIP; stage only atlas/metadata for Unity. Never execute bundled code."""
import hashlib,json,zipfile,shutil,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=Path(r'C:\Users\jrbar\Downloads\Alpine_Forest_Alpha_Cards.zip')
expected='cbda5aec744cc623295d5a47b2ee981cdb2bd98dd33b85de36a0c0af724b2997'
assert hashlib.sha256(source.read_bytes()).hexdigest()==expected,'source changed since inspection'
archive=root/'SourceAssets/ForestAlphaCards'
archive.mkdir(parents=True,exist_ok=True)
shutil.copy2(source,archive/source.name)
with zipfile.ZipFile(source) as z:
 for entry in z.infolist():
  target=(archive/entry.filename).resolve()
  assert target.is_relative_to(archive.resolve()),'unsafe archive path'
  if entry.is_dir():target.mkdir(parents=True,exist_ok=True)
  else:target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(z.read(entry))
prepared=root/'Assets/Snowglow/Catalog/Sources/ForestAlphaCards'
prepared.mkdir(parents=True,exist_ok=True)
bundle=archive/'Alpine_Forest_Alpha_Cards'
shutil.copy2(bundle/'Forest_Cards_Atlas_4096.png',prepared/'Forest_Cards_Atlas_4096.png')
cards=json.loads((bundle/'cards_manifest.json').read_text())
assert len(cards)==52
alpha_path=archive/'DerivedAlphaBounds.json'
subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'Tools/AssetIntake/inspect_forest_alpha_bounds.ps1'),'-ZipPath',str(source),'-OutputPath',str(alpha_path)],check=True)
alpha={row['file']:row['alpha_uv_bounds'] for row in json.loads(alpha_path.read_text())}
for card in cards:card['alpha_uv_bounds']=alpha[card['file']]
(prepared/'CardsManifest.json').write_text(json.dumps({'cards':cards,'sourceSha256':expected},indent=2))
print('Preserved ZIP and all source files outside Assets; staged one atlas and 52 manifest rows.')
