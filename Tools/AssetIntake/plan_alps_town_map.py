"""Trace the supplied plan's colored footprints into metre coordinates.

Only the image is data; embedded Blender text is never executed.
"""
from pathlib import Path
import json, math, hashlib
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
IMAGE = ROOT / 'SourceAssets/AlpsTownMap/TownPlan.png'
im = np.array(Image.open(IMAGE).convert('RGB'))
sy, sx = im.shape[0] / 1344, im.shape[1] / 1824
left, right, top, bottom = 202*sx, 1212*sx, 181*sy, 1190*sy

def coords(x, y):
    return (round((x-left)/(right-left)*400-200, 3),
            round(200-(y-top)/(bottom-top)*400, 3))

def components(color):
    mask = np.all(im == color, axis=2)
    mask[:round(top)] = False
    mask[round(bottom)+1:] = False
    mask[:, :round(left)] = False
    mask[:, round(right)+1:] = False
    parents, bounds, previous = [], [], []
    def find(i):
        while parents[i] != i:
            parents[i] = parents[parents[i]]
            i = parents[i]
        return i
    for y in range(round(top), round(bottom)+1):
        row = np.flatnonzero(np.diff(np.r_[False, mask[y], False]))
        current = []
        for a, end in zip(row[::2], row[1::2]):
            b = end-1
            touching = {find(i) for x, z, i in previous if x <= b and z >= a}
            if touching:
                ident = min(touching)
                for i in touching:
                    if i != ident:
                        parents[i] = ident
                        old = bounds[i]; now = bounds[ident]
                        bounds[ident] = [min(old[0], now[0]), min(old[1], now[1]),
                                         max(old[2], now[2]), max(old[3], now[3])]
                q = bounds[ident]
                bounds[ident] = [min(q[0], int(a)), min(q[1], y), max(q[2], int(b)), max(q[3], y)]
            else:
                ident = len(parents); parents.append(ident); bounds.append([int(a), y, int(b), y])
            current.append((a, b, ident))
        previous = current
    return [v for i, v in enumerate(bounds) if find(i) == i]

buildings = []
for kind, color in [('residential', (127,164,185)), ('shops', (197,161,117)), ('civic', (136,167,153))]:
    for x0,y0,x1,y1 in components(color):
        if (x1-x0)/sx < 20 or (y1-y0)/sy < 20:
            continue
        cx, cy = (x0+x1)/2, (y0+y1)/2
        x,z = coords(cx,cy)
        # Front indicators use a darker ink than the symmetrical outline.
        pad=round(9*sx)
        patch=im[y0-pad:y1+pad+1, x0-pad:x1+pad+1]
        yy,xx=np.indices(patch.shape[:2]); xx=xx+x0-pad; yy=yy+y0-pad
        ink=(patch[:,:,0] < 48)&(patch[:,:,1] < 86)&(patch[:,:,2] < 106)
        border=(abs(xx-cx)>(x1-x0)*.40)|(abs(yy-cy)>(y1-y0)*.40)
        yy,xx=yy[ink&border],xx[ink&border]
        dx,dz=(float(xx.mean()-cx), float(cy-yy.mean())) if len(xx) else (0,-1)
        yaw=round(math.degrees(math.atan2(dx,dz))/45)*45%360
        buildings.append(dict(id=0,kind=kind,x=x,z=z,width=round((x1-x0)/ (right-left)*400,3),
                              depth=round((y1-y0)/(bottom-top)*400,3),yaw=yaw))
assert len(buildings)==75, len(buildings)

# Numbering is explicitly printed in the plan (bottom row first within each block).
def assign(items, ids):
    assert len(items)==len(ids),(len(items),ids)
    for b,number in zip(items,ids): b['id']=number
for lo,hi,first in [(-180,-90,1),(-85,-5,7),(5,85,13),(90,180,19)]:
    bs=[b for b in buildings if lo<b['x']<hi and b['z']>115]
    assign(sorted(bs,key=lambda b:(round(b['z']/10),b['x'])),range(first,first+6))
for lo,hi,first in [(-180,-90,25),(-85,-5,29),(90,180,36)]:
    bs=[b for b in buildings if lo<b['x']<hi and 25<b['z']<100]
    assign(sorted(bs,key=lambda b:(round(b['z']/10),b['x'])),range(first,first+4))
assign(sorted([b for b in buildings if b['kind']=='civic'], key=lambda b:(-b['z'],b['x'])),[33,34,35])
for lo,hi,first in [(-180,-90,40),(-85,-5,47),(5,85,54),(90,180,61)]:
    bs=[b for b in buildings if lo<b['x']<hi and -80<b['z']<0]
    assign(sorted(bs,key=lambda b:(round(b['z']/10),b['x'])),range(first,first+7))
for lo,hi,first in [(-85,-5,68),(5,85,72)]:
    bs=[b for b in buildings if lo<b['x']<hi and b['z']<-100]
    assign(sorted(bs,key=lambda b:(round(b['z']/10),b['x'])),range(first,first+4))
assert sorted(b['id'] for b in buildings)==list(range(1,76))

def dots(color, minimum=2, maximum=10):
    result=[]
    for x0,y0,x1,y1 in components(color):
        w,h=(x1-x0)/sx,(y1-y0)/sy
        if minimum<=w<=maximum and minimum<=h<=maximum:
            x,z=coords((x0+x1)/2,(y0+y1)/2);result.append(dict(x=x,z=z))
    return result

# Use exact dot colors sampled from the layout key.
lights=dots((243,197,81),1,8)
if len(lights)!=74:
    # The native image's circle centers are a uniform gold.
    colors,counts=np.unique(im.reshape(-1,3),axis=0,return_counts=True)
    candidates=[tuple(map(int,c)) for c,n in zip(colors,counts) if c[0]>150 and 90<c[1]<190 and c[2]<80 and n>500]
    for color in candidates:
        q=dots(color,1,8)
        if len(q)==74: lights=q;break
assert len(lights)==74,len(lights)
signs=dots((49,109,101),1,7)
if len(signs)!=10:
    colors,counts=np.unique(im.reshape(-1,3),axis=0,return_counts=True)
    for color,n in zip(colors,counts):
        if 25<color[0]<80 and 80<color[1]<140 and 70<color[2]<130 and n>300:
            q=dots(tuple(map(int,color)),1,7)
            if len(q)==10:signs=q;break
assert len(signs)==10,len(signs)

out=dict(image=str(IMAGE.relative_to(ROOT)),imageSha256=hashlib.sha256(IMAGE.read_bytes()).hexdigest(),
         areaSize=400,buildings=sorted(buildings,key=lambda b:b['id']),lights=lights,signs=signs,
         statue=dict(x=0,z=20),tower=dict(x=43,z=59),depot=dict(x=-130,z=-137),yard=dict(x=131,z=-129))
(ROOT/'SourceAssets/AlpsTownMap/Layout.json').write_text(json.dumps(out,indent=2))
print('TRACED',len(buildings),'buildings',len(lights),'lights',len(signs),'signboards')

