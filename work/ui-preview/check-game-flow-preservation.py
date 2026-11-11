from pathlib import Path
import re,json
folder=Path('Logs/GameFlow')
def docs(path):
    s=path.read_text(encoding='utf-8-sig')
    return {int(m.group(2)):(int(m.group(1)),m.group()) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(?:(?!^--- !u!).*\n?)*',s,re.M)}
def guid(name):return re.search(r'guid: (\w+)',Path('Assets/Snowglow/Scripts/'+name+'.cs.meta').read_text()).group(1)
a,b=docs(folder/'Pinefall-before.unity'),docs(Path('Assets/Scenes/Pinefall.unity'))
ui_guids=[guid('PrototypeHUD'),guid('WorkshopPreview')]
ui_roots=set()
for kind,doc in a.values():
    if kind==114 and any(g in doc for g in ui_guids):ui_roots.add(int(re.search(r'm_GameObject: \{fileID: (-?\d+)',doc).group(1)))
parents,owners={},{}
for identity,(kind,doc) in a.items():
    if kind not in (4,224):continue
    go=re.search(r'm_GameObject: \{fileID: (-?\d+)',doc)
    parent=re.search(r'm_Father: \{fileID: (-?\d+)',doc)
    if go:owners[identity]=int(go.group(1))
    if parent:parents[identity]=int(parent.group(1))
excluded=set(ui_roots)
for identity,go in owners.items():
    node=identity;seen=set()
    while node and node not in seen:
        seen.add(node)
        if owners.get(node) in ui_roots:excluded.add(go);break
        node=parents.get(node,0)
protected={1,4,23,33,64,65,54,56,114,136,146,154,218,1001}
session_guid=guid('PrototypeSession')
ids=[]
for identity,(kind,doc) in a.items():
    go=re.search(r'm_GameObject: \{fileID: (-?\d+)',doc)
    if kind not in protected or (go and int(go.group(1)) in excluded) or (kind==1 and identity in excluded):continue
    if kind==114 and session_guid in doc:continue # additive serialized progression defaults
    ids.append(identity)
changed=[identity for identity in ids if a[identity]!=b.get(identity)]
result={'protectedWorldObjectsAndComponents':len(ids),'changedIds':changed,'excluded':'Canvas/workshop preview hierarchy and additive session profile defaults'}
(folder/'WorldPreservation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
if changed:raise SystemExit(1)
