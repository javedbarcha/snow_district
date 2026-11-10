from pathlib import Path
import re,json
folder=Path('Logs/ProductionUI')
before=sorted(folder.glob('Pinefall-before-*.unity'))[0]
def objects(p):
    s=p.read_text(encoding='utf-8-sig')
    return {int(m.group(2)):(int(m.group(1)),m.group()) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(?:(?!^--- !u!).*\n?)*',s,re.M)}
a,b=objects(before),objects(Path('Assets/Scenes/Pinefall.unity'))
protected={4,23,33,64,65,54,56,114,136,146,154,218,1001}
hud_guid=re.search(r'guid: (\w+)',Path('Assets/Snowglow/Scripts/PrototypeHUD.cs.meta').read_text()).group(1)
ids=[k for k,v in a.items() if v[0] in protected and hud_guid not in v[1]]
changed=[k for k in ids if k not in b or a[k]!=b[k]]
result={'before':str(before),'protectedWorldComponents':len(ids),'changedIds':changed}
(folder/'WorldPreservation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
if changed:raise SystemExit(1)
