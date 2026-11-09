from pathlib import Path
import re
p=Path('work/ui-preview/snow-district-ui-preview.html')
s=p.read_text(encoding='utf-8')
s=s.replace("screen:'home',mode:'Career'", "screen:'home',region:'alpine',mode:'Career'")
data="""    const regions=[
      {id:'alpine',name:'Mountain & Alpine',places:[['The Alps','Switzerland / Austria'],['Svalbard','Norway'],['The Andes','Chile / Argentina']]},
      {id:'urban',name:'City & Urban',places:[['Aomori City','Japan'],['Sapporo (Hokkaido)','Japan'],['Harbin','China']]},
      {id:'highway',name:'Highway & Open Country',places:[['Rogers Pass (British Columbia)','Canada'],['Coquihalla Highway','Canada'],['Tromsø','Norway']]},
      {id:'suburban',name:'Suburban & Commercial',places:[['Buffalo (New York)','USA'],['Anchorage (Alaska)','USA'],['Minneapolis–St. Paul (Minnesota)','USA']]},
      {id:'industrial',name:'Industrial & Extreme Weather',places:[['Norilsk (Siberia)','Russia'],['Yakutsk','Russia'],['McMurdo Station','Antarctica']]}
    ];
    function regionChoice(r){return '<button class="sd-region" data-region="'+r.id+'"><div class="sd-region-picture sd-art-'+(r.id==='alpine'?'town':'mountains')+'">'+badge(r.id==='alpine'?'PINEFALL AVAILABLE':'PLANNED','gold')+'</div><div class="sd-region-label"><span><strong>'+r.name+'</strong><br><small>'+r.places.length+' planned locations'+(r.id==='alpine'?' · Pinefall preview':'')+'</small></span>'+icon('arrow-right')+'</div></button>';}
"""
s=s.replace('    function regionCard(',data+'    function regionCard(')
lines=s.splitlines()
for i,line in enumerate(lines):
    if "if(state.screen==='regions')html=" in line:
        lines[i]="      if(state.screen==='regions')html=wrap(back('Main Menu','home')+heading('CAREER','Choose your region','Explore five region types. Pinefall is available; other locations are planned.')+'<div class=\"sd-regions\">'+regions.map(regionChoice).join('')+'</div>');"
    if "if(state.screen==='towns')html=" in line:
        lines[i]="      if(state.screen==='towns'){const r=regions.find(r=>r.id===state.region)||regions[0];html=wrap(back('Career / Regions','regions')+heading('CAREER / '+r.name.toUpperCase(),'Choose your location',r.id==='alpine'?'Start in Pinefall. Real-world destinations below are planned.':'Planned destinations · Browse the lineup for future updates.')+'<div class=\"sd-town-grid\">'+(r.id==='alpine'?regionCard('Pinefall','town',true,'0 / 3 contracts completed','jobs'):'')+r.places.map(([name,country])=>regionCard(name,'mountains',false,country+' · Planned','')).join('')+'</div>');}"
s='\n'.join(lines)+'\n'
s=s.replace("      if(b.dataset.go)","      if(b.dataset.region){state.region=b.dataset.region;go('towns');return;}\n      if(b.dataset.go)")
s=s.replace('</style>','#snow-district-preview .sd-town-grid {grid-template-columns:repeat(auto-fit,minmax(min(100%,230px),1fr));}\n#snow-district-preview .sd-region-label>span {min-width:0;overflow-wrap:anywhere;}\n</style>')
p.write_text(s,encoding='utf-8')
Path('work/ui-preview/preview-check.js').write_text(re.search(r'<script>([\s\S]*?)</script>',s).group(1),encoding='utf-8')
q=Path('work/ui-preview/check-preview.mjs')
t=q.read_text().replace("await click('[data-go=\"towns\"]')","await click('[data-region=\"alpine\"]')")
marker="if(errors.length)throw new Error"
extra="""for(const width of [1100,375,320]){
 await send('Emulation.setDeviceMetricsOverride',{width,height:950,deviceScaleFactor:1,mobile:false});
 for(const region of ['alpine','urban','highway','suburban','industrial']){
  await evaluate(`(()=>{const p=document.querySelector('#sd-screen-picker');p.value='regions';p.dispatchEvent(new Event('change'));})()`);
  await click('[data-region="'+region+'"]');
  const count=await evaluate('document.querySelectorAll(".sd-town-grid .sd-region").length');
  if(count!==(region==='alpine'?4:3))throw new Error('Region location count '+region);
  if(await evaluate('document.documentElement.scrollWidth>innerWidth+1'))throw new Error('Region overflow '+region+' '+width);
  if(width===1100){const shot=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true});fs.writeFileSync(path.join(folder,'region-'+region+'.png'),Buffer.from(shot.data,'base64'));}
 }
}
"""
t=t.replace(marker,extra+marker)
q.write_text(t)
