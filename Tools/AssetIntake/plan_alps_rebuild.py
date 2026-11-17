import json,math,random
from pathlib import Path
random.seed(171126)
roads=[]
def road(name,points,width=10):
 # Rounded quadratic corners with bounded cut; all original junctions stay under the road width.
 p=[tuple(v) for v in points];out=[p[0]]
 def line(b):
  a=out[-1];n=max(1,math.ceil(math.dist(a,b)/2))
  for i in range(1,n+1):out.append(tuple(a[j]+(b[j]-a[j])*i/n for j in range(2)))
 for k in range(1,len(p)-1):
  a,b,c=p[k-1:k+2];li=math.dist(a,b);lo=math.dist(b,c);cut=min(8,li*.25,lo*.25)
  u=tuple(b[j]-(b[j]-a[j])/li*cut for j in range(2));v=tuple(b[j]+(c[j]-b[j])/lo*cut for j in range(2));line(u)
  for q in range(1,17):
   t=q/16;out.append(tuple((1-t)**2*u[j]+2*(1-t)*t*b[j]+t*t*v[j] for j in range(2)))
 line(p[-1]);roads.append(dict(name=name,width=width,points=[dict(x=a,z=b) for a,b in out],controlPoints=[dict(x=a,z=b) for a,b in p]))
road('Main street | southwest to north gate',[(-205,-205),(-80,-135),(-25,-65),(0,0),(0,30),(0,110),(45,140),(125,175),(180,210)])
road('Eastern curved avenue',[(0,-65),(60,-35),(135,0),(155,95),(125,175)],11)
road('Western old town crescent',[(-80,-135),(-145,-60),(-145,30),(-105,125),(0,110)])
road('Market cross street',[(-205,60),(-145,30),(-85,5),(-35,0),(35,0),(75,20),(135,0),(205,-5)])
road('Southern neighbourhood loop',[(-145,-65),(-95,-65),(0,-65),(94,-60),(145,-60),(145,-80),(155,-145),(80,-170),(-80,-135)])
road('Northern hillside-facing crescent',[(-105,125),(-65,165),(25,175),(125,175)])
road('Northwest village lane',[(-145,30),(-195,105),(-170,170),(-65,165)],9)
road('South village lane',[(-80,-135),(-70,-205),(15,-220),(80,-170)],9)
road('Southeast village lane',[(155,-145),(210,-110),(205,-5)],9)
road('Depot access',[(-95,-60),(-95,-90)],12)
road('Disposal access',[(94,-60),(120,-60),(120,-109),(94,-109),(94,-96)],12)
road('Visitor parking access',[(75,20),(92,27)],10)
metrics=json.load(open('Assets/Snowglow/Catalog/Sources/AlpsRebuild/ExportMetrics.json'))['assets'];models={m['slug']:m for m in metrics}
h=json.load(open('SourceAssets/AlpsRebuild/TerrainSurface.json'))['heights']
def height(x,z):
 u=(x+500)/1000*256;v=(-z+500)/1000*256;i=min(255,max(0,int(u)));j=min(255,max(0,int(v)));a=u-i;b=v-j
 return h[j][i]*(1-a)*(1-b)+h[j][i+1]*a*(1-b)+h[j+1][i]*(1-a)*b+h[j+1][i+1]*a*b
def nearest(x,z):
 best=(1e10,None,None)
 for r in roads:
  for a,b in zip(r['points'],r['points'][1:]):
   dx=b['x']-a['x'];dz=b['z']-a['z'];ll=dx*dx+dz*dz
   if ll<1e-10:continue
   t=max(0,min(1,((x-a['x'])*dx+(z-a['z'])*dz)/ll));p=(a['x']+dx*t,a['z']+dz*t);d=math.dist((x,z),p)-r['width']/2
   if d<best[0]:best=(d,p,r['name'])
 return best
# Bounds include generous turning yards and full feature fronts. Landmarks are additional to 70 homes.
reserved=[(-120,-62,-124,-72),(-139,-112,-104,-64),(74,128,-116,-62),(71,113,3,50),(-3,39,7,45),(-10,10,63,87)]
def overlap(a,b,gap=0):return a[0]<b[1]+gap and a[1]>b[0]-gap and a[2]<b[3]+gap and a[3]>b[2]-gap
candidates=[]
for iz in range(-17,18):
 for ix in range(-17,18):
  x=ix*13+random.uniform(-3,3);z=iz*13+random.uniform(-3,3)
  if math.hypot(x,z)>265 or height(x,z)>.65:continue
  candidates.append((x,z))
random.shuffle(candidates)
homes=[];variants=[i for i in range(1,12) if i!=5]
for x,z in candidates:
 variant=variants[len(homes)%len(variants)];slug='Building_%02d'%variant;m=models[slug];w,ht,d=m['size'];extent=(w+d)*math.sqrt(.5)/2;rect=(x-extent,x+extent,z-extent,z+extent)
 if any(overlap(rect,r,5) for r in reserved) or any(overlap(rect,b['_rect'],6) for b in homes):continue
 distance,point,name=nearest(x,z)
 if distance<extent+4 or distance>58:continue
 # Full footprint remains within the source's flat town patch; no terrain flattening is performed.
 if max(height(px,pz) for px in [rect[0],x,rect[1]] for pz in [rect[2],z,rect[3]])>1:continue
 homes.append(dict(id='Home %02d'%(len(homes)+1),asset=slug,x=x,z=z,yaw=225,nearRoad=name,roadDistance=distance,frontRoadPoint=dict(x=point[0],z=point[1]),_rect=rect))
 if len(homes)==70:break
print('BUILDINGS',len(homes),'candidates',len(candidates))
# Preserve candidate/road plan even if packing needs adjustment; do not silently lower target.
for h in homes:h.pop('_rect')
plan={'targetBuildings':70,'buildings':homes,'roads':roads,'statue':dict(x=18,z=25,yaw=225),'tower':dict(x=0,z=75,yaw=180),'notes':'Aerial-inspired diagonal main street, curved neighbourhood loops and side streets, spread across the provided flat town patch. Not a geospatial reconstruction. Facility coordinates retain existing gameplay service/disposal rules. All fronts oriented toward the fixed southwest isometric viewpoint.'}
Path('SourceAssets/AlpsRebuild/LayoutPlan.json').write_text(json.dumps(plan,indent=2))
Path('Reports/AlpsRebuild/LayoutPlanning.txt').write_text('Generated %d / 70 building placements; %d connected road routes. Candidate footprint/road/yard gaps checked; native scene validation and gameplay-camera audit still required.\n'%(len(homes),len(roads)))
assert len(homes)==70,'Packing incomplete: keep the requested target and revise layout'
