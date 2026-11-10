import fs from 'node:fs';
import path from 'node:path';
const folder=path.resolve('work/ui-preview');
const targets=await (await fetch('http://127.0.0.1:9336/json')).json();
const target=targets.find(t=>t.type==='page');
if(!target)throw new Error('No preview browser tab');
const socket=new WebSocket(target.webSocketDebuggerUrl);
await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});
let id=0;const pending=new Map();const errors=[];
socket.addEventListener('message',event=>{const data=JSON.parse(event.data);if(data.id){const item=pending.get(data.id);if(item){pending.delete(data.id);data.error?item.reject(new Error(JSON.stringify(data.error))):item.resolve(data.result);}}else if(data.method==='Runtime.exceptionThrown')errors.push(data.params.exceptionDetails);});
function send(method,params={}){return new Promise((resolve,reject)=>{const key=++id;pending.set(key,{resolve,reject});socket.send(JSON.stringify({id:key,method,params}));});}
async function evaluate(expression){const result=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(result.exceptionDetails)throw new Error(JSON.stringify(result.exceptionDetails));return result.result.value;}
async function settle(){await evaluate('new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)))');}
async function click(selector){const hit=await evaluate(`(()=>{const e=document.querySelector(${JSON.stringify(selector)});if(!e)return false;e.click();return true;})()`);if(!hit)throw new Error('Missing clickable '+selector);await settle();}
await send('Page.enable');await send('Runtime.enable');
await send('Emulation.setDeviceMetricsOverride',{width:1100,height:880,deviceScaleFactor:1,mobile:false});
await send('Page.navigate',{url:'file:///'+path.join(folder,'snow-district-ui-preview.html').replaceAll('\\','/')});
for(let n=0;n<40;n++){if(await evaluate('Boolean(document.querySelector("#sd-page")?.children.length)'))break;await new Promise(r=>setTimeout(r,100));}
await evaluate('document.fonts.ready');await settle();
const cases=['home','regions','towns','jobs','machines','garage','shop','settings','gameplay','pause','results'];
const report=[];
await evaluate(`(()=>{const p=document.querySelector('#sd-screen-picker');p.value='machines';p.dispatchEvent(new Event('change'));})()`);
await click('[data-machine-step="1"]');
if(!await evaluate('document.querySelector("[data-action=start]").disabled && document.querySelector(".sd-machine-caption").textContent.includes("Golden Snow Loader")'))throw new Error('Locked loader carousel state');
await evaluate(`document.querySelector('[data-machine-step="-1"]').dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowLeft',bubbles:true}))`);
if(!await evaluate('!document.querySelector("[data-action=start]").disabled && document.querySelector(".sd-machine-caption").textContent.includes("Golden Snow Tractor")'))throw new Error('Carousel keyboard selection');
await send('Emulation.setTouchEmulationEnabled',{enabled:true});
async function swipe(dx,dy=0){
 const point=await evaluate(`(()=>{const b=document.querySelector('.sd-machine-picture').getBoundingClientRect();return {x:b.x+b.width/2,y:b.y+b.height/2};})()`);
 await send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...point,id:0}]});
 await send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:point.x+dx,y:point.y+dy,id:0}]});
 await send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await settle();
}
await swipe(-120);
if(!await evaluate('document.querySelector("[data-action=start]").disabled'))throw new Error('Touch swipe left did not browse loader');
await swipe(120);
if(!await evaluate('document.querySelector(".sd-machine-caption").textContent.includes("Golden Snow Tractor")'))throw new Error('Touch swipe right did not browse tractor');
await swipe(5,100);
if(!await evaluate('document.querySelector(".sd-machine-caption").textContent.includes("Golden Snow Tractor")'))throw new Error('Vertical scroll changed machinery');
await send('Emulation.setTouchEmulationEnabled',{enabled:false});
if(await evaluate('document.querySelectorAll(".sd-machine-picture").length!==1 || Boolean(document.querySelector(".sd-machine-choice"))'))throw new Error('Carousel contains separate cards');
for(const width of [1100,736,375,320]){
 await send('Emulation.setDeviceMetricsOverride',{width,height:950,deviceScaleFactor:1,mobile:false});
 for(const screen of cases){
  await evaluate(`(()=>{const p=document.querySelector('#sd-screen-picker');p.value=${JSON.stringify(screen==='shop'?'garage':screen)};p.dispatchEvent(new Event('change'));})()`);await settle();
  if(screen==='shop')await click('[data-workshop-tab="shop"]');
  const size=await evaluate(`(()=>{const r=document.querySelector('#snow-district-preview');const box=r.getBoundingClientRect();return {screen:${JSON.stringify(screen)},width:${width},documentWidth:document.documentElement.scrollWidth,viewport:innerWidth,boxWidth:box.width,boxHeight:box.height,imageFailures:[...r.querySelectorAll('img')].filter(i=>!i.complete||!i.naturalWidth).length};})()`);
  if(size.documentWidth>width+1)throw new Error('Horizontal overflow '+JSON.stringify(size));
  if(size.imageFailures)throw new Error('Image failed '+JSON.stringify(size));
  report.push(size);
  if(width===1100||width===375&&['home','garage'].includes(screen)){
   const screenshot=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true,clip:{x:0,y:0,width:Math.min(size.documentWidth,width),height:Math.ceil(size.boxHeight+16),scale:1}});
   fs.writeFileSync(path.join(folder,`${screen}-${width}.png`),Buffer.from(screenshot.data,'base64'));
  }
 }
}
await send('Emulation.setDeviceMetricsOverride',{width:1100,height:880,deviceScaleFactor:1,mobile:false});
await evaluate(`(()=>{const p=document.querySelector('#sd-screen-picker');p.value='home';p.dispatchEvent(new Event('change'));})()`);
await click('[data-go="regions"]');await click('[data-region="alpine"]');await click('[data-go="jobs"]');await click('[data-job="1"]');await click('[data-go="machines"]');await click('[data-action="start"]');await new Promise(r=>setTimeout(r,800));
if(!await evaluate('Boolean(document.querySelector(".sd-driving"))'))throw new Error('Career flow did not reach driving');
await click('[data-action="pause"]');await click('[data-dialog="recover"]');await click('[data-confirm="recover"]');
if(!await evaluate('Boolean(document.querySelector(".sd-driving"))'))throw new Error('Recovery did not return to gameplay');
await click('[data-action="garage"]');await click('[data-garage-tab="service"]');await click('[data-dialog="repair"]');await click('[data-confirm="repair"]');
if(!await evaluate('document.querySelector("#sd-page").textContent.includes("100%")'))throw new Error('Repair state did not update');
await click('[data-go="shop"]');await click('[data-dialog="purchase"]');await click('[data-confirm="purchase"]');await click('[data-action="owned-garage"]');await click('[data-action="select-drive"]');
if(!await evaluate('document.querySelector("#sd-page").textContent.includes("Golden Snow Loader")'))throw new Error('Purchase/selection did not use loader');
await click('[data-action="pause"]');await click('[data-dialog="abandon"]');await click('[data-confirm="abandon"]');
if(!await evaluate('Boolean(document.querySelector(".sd-mode-grid"))'))throw new Error('Abandon did not return home');
await click('[data-action="quick"]');
if(!await evaluate('document.querySelector("#sd-page").textContent.includes("QUICK JOB")'))throw new Error('Quick Job route incorrect');
for(const width of [1100,375,320]){
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
if(errors.length)throw new Error('Browser JS errors: '+JSON.stringify(errors));
fs.writeFileSync(path.join(folder,'browser-validation.json'),JSON.stringify({screenLayouts:report.length,flows:['Career region-town-job-machine-gameplay','Pause recovery resumes','Garage repair updates','Shop purchase and select loader','Abandon returns home','Quick Job route'],errors,layouts:report},null,2));
console.log(`PASS: ${report.length} layouts, 6 interaction flows, carousel arrows/keyboard/touch/ownership, ${errors.length} runtime errors`);
socket.close();
