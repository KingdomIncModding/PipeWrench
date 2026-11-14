import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
const [root, out] = process.argv.slice(2);
const require = createRequire(path.join(root, '_Tools/UIDesigner/scripts/package.json'));
const { createCanvas, loadImage, GlobalFonts } = require('@napi-rs/canvas');
for(const [file,name] of [['segoeui.ttf','UI'],['segoeuib.ttf','Bold'],['consolab.ttf','Mono']])
  GlobalFonts.registerFromPath('C:/Windows/Fonts/'+file,name);
GlobalFonts.registerFromPath(path.join(root,'_Tools/UIDesigner/assets/fonts/LiberationSans.ttf'),'Sans');
const mod = path.join(root,'_Mods/PipeWrench');
const version = JSON.parse(fs.readFileSync(path.join(mod,'mod.json'))).version;
const sprites = path.join(root,'_SrcExtraction/Extractions/1.0.14/Exports/Sprite');
const forge = await loadImage(path.join(sprites,'sharedassets2.assets/179__Forge.png'));
const filter = await loadImage(path.join(sprites,'sharedassets2.assets/272__Filter.png'));
const dark = await loadImage(path.join(sprites,'sharedassets1.assets/142__DecoRectDark.png'));
const light = await loadImage(path.join(sprites,'sharedassets1.assets/184__DecoRectLight.png'));
// The user's actual toolbar frame/title comes from the installed mod's Unity render.
const hud = await loadImage(path.join(mod,`_AI/Notes/Runtime-${version}/tools-started.png`));
const layout = JSON.parse(fs.readFileSync(path.join(mod,'Assets/UI/PipeWrenchToolbar.kingdom-ui.json'))).nodes[0];
const rules = layout.children.filter(n=>n.name.startsWith('Rule - '));
const routes = JSON.parse(fs.readFileSync(path.join(out,'routes.json')));
const canvas = createCanvas(960,600), ctx = canvas.getContext('2d');
const C={bg:'#101916',panel:'#17241f',grid:'#22352b',ink:'#f3e8cb',muted:'#abb6a5',gold:'#d5aa56',cyan:'#75e4e4',dark:'#060d0a',edge:'#4e6249'};
const fps=12, seconds=53;
const ease=n=>{n=Math.max(0,Math.min(1,n));return n*n*(3-2*n);};
const mix=(a,b,n)=>a+(b-a)*n;
const screen=points=>points.map(([x,y])=>[x,y+28]);
const snapX=x=>Math.round(x/16)*16;
const center=screen(routes.bends2);
function text(value,x,y,size=20,color=C.ink,font='UI',align='left') {
  ctx.fillStyle=color;ctx.font=`${size}px "${font}"`;ctx.textAlign=align;ctx.textBaseline='alphabetic';ctx.fillText(value,x,y);
}
function line(points,color=C.cyan,width=5,dash=[]) {
  ctx.save();ctx.strokeStyle=C.dark;ctx.lineWidth=width+4;ctx.lineJoin='miter';ctx.beginPath();
  points.forEach((p,i)=>i?ctx.lineTo(...p):ctx.moveTo(...p));ctx.stroke();
  ctx.strokeStyle=color;ctx.lineWidth=width;ctx.setLineDash(dash);ctx.stroke();ctx.restore();
}
function cursor(x,y,held=false) {
  ctx.save();ctx.translate(Math.round(x),Math.round(y));
  if(held){ctx.strokeStyle=C.gold;ctx.lineWidth=2;ctx.beginPath();ctx.arc(0,0,20,0,Math.PI*2);ctx.stroke();}
  ctx.beginPath();ctx.moveTo(0,0);ctx.lineTo(0,23);ctx.lineTo(6,17);ctx.lineTo(12,28);ctx.lineTo(17,25);ctx.lineTo(11,15);ctx.lineTo(20,14);ctx.closePath();
  ctx.fillStyle=C.ink;ctx.strokeStyle=C.dark;ctx.lineWidth=3;ctx.fill();ctx.stroke();ctx.restore();
}
function marker(x,y,held=false) {
  ctx.fillStyle=held?C.gold:C.dark;ctx.strokeStyle=held?C.gold:C.cyan;ctx.lineWidth=2;
  ctx.fillRect(x-9,y-9,18,18);ctx.strokeRect(x-9,y-9,18,18);ctx.fillStyle=held?C.dark:C.cyan;ctx.fillRect(x-4,y-2,8,4);
}
function nineSlice(image,x,y,w,h) {
  const s=[0,8,24,32],dx=[x,x+16,x+w-16,x+w],dy=[y,y+16,y+h-16,y+h];
  for(let j=0;j<3;j++)for(let i=0;i<3;i++)ctx.drawImage(image,s[i],s[j],s[i+1]-s[i],s[j+1]-s[j],dx[i],dy[j],dx[i+1]-dx[i],dy[j+1]-dy[j]);
}
function toolbar(rule=0) {
  ctx.imageSmoothingEnabled=false;ctx.drawImage(hud,24,24,568,53,50,150,568,53);
  rules.forEach((n,i)=>{const x=50+n.x,y=150+Math.round(n.y);nineSlice(i===rule?light:dark,x,y,n.w,n.h);
    text(n.name.replace('Rule - ',''),x+n.w/2,y+28,17,i===rule?'#241e1b':'#e8e0cf','Sans','center');});
}
function base(t,stage) {
  ctx.fillStyle=C.bg;ctx.fillRect(0,0,960,600);ctx.fillStyle=C.gold;ctx.fillRect(0,0,960,4);
  text('KINGDOM .INC  /  LOCAL MOD',40,33,13,C.gold,'Mono');text('PIPE WRENCH',40,82,42,C.ink,'Bold');
  text('Square corners. Movable sections.',42,112,19,C.muted);text(`0${stage+1} / 07`,920,40,14,C.muted,'Mono','right');
  text('SNAPPY UNLOCKED / 16-UNIT SNAP',920,112,12,C.gold,'Mono','right');
  for(let i=0;i<7;i++){ctx.fillStyle=i===stage?C.gold:'#38503d';ctx.fillRect(767+i*22,76,13,4);}
  ctx.fillStyle=C.panel;ctx.fillRect(32,132,896,330);ctx.strokeStyle=C.grid;ctx.lineWidth=1;ctx.beginPath();
  for(let x=32;x<928;x+=16){ctx.moveTo(x+.5,132);ctx.lineTo(x+.5,462);}
  for(let y=132;y<462;y+=16){ctx.moveTo(32,y+.5);ctx.lineTo(928,y+.5);}ctx.stroke();
  ctx.strokeStyle=C.edge;ctx.strokeRect(32.5,132.5,895,329);ctx.imageSmoothingEnabled=false;
  ctx.drawImage(forge,72,216,136,88);ctx.drawImage(forge,752,344,136,88);
  text('SOURCE',72,329,12,C.muted,'Mono');text('DESTINATION',888,450,12,C.muted,'Mono','right');
  ctx.fillStyle=C.gold;ctx.fillRect(205,253,19,14);ctx.fillRect(736,381,19,14);
  ctx.fillStyle=C.cyan;ctx.fillRect(220,257,5,6);ctx.fillRect(735,385,5,6);
  text('Illustrated showcase • current controls',40,582,12,C.muted);text(`PIPE WRENCH ${version}`,920,582,12,C.muted,'Mono','right');
  ctx.fillStyle='#293c30';ctx.fillRect(40,559,880,2);ctx.fillStyle=C.gold;ctx.fillRect(40,559,880*t/seconds,2);
}
function caption(key,title,detail,width=100) {
  ctx.fillStyle=C.gold;ctx.fillRect(40,487,width,49);ctx.fillStyle='#805e2d';ctx.fillRect(40,532,width,4);
  text(key,40+width/2,519,key.length>5?16:23,C.bg,'Mono','center');
  text(title,60+width,507,24,C.ink,'Bold');text(detail,60+width,535,17,C.muted);
}
function tag(label){text(label,901,229,14,C.gold,'Mono','right');}
function ring(x,y,color=C.ink) {
  ctx.strokeStyle=color;ctx.lineWidth=2;ctx.beginPath();ctx.arc(x,y,14,0,Math.PI*2);ctx.stroke();
}
function ghost(points) {
  ctx.save();ctx.strokeStyle='#70645b';ctx.lineWidth=2;ctx.setLineDash([5,6]);ctx.beginPath();
  points.forEach((p,i)=>i?ctx.lineTo(...p):ctx.moveTo(...p));ctx.stroke();ctx.restore();
}
function movingDot(points,t) {
  const lengths=points.slice(1).map((p,i)=>Math.hypot(p[0]-points[i][0],p[1]-points[i][1]));
  let distance=lengths.reduce((a,b)=>a+b,0)*((t*.35)%1);
  for(let i=0;i<lengths.length;i++){
    if(distance<=lengths[i]){const p=points[i],q=points[i+1],k=distance/lengths[i];ctx.fillStyle=C.ink;
      ctx.beginPath();ctx.arc(mix(p[0],q[0],k),mix(p[1],q[1],k),4,0,Math.PI*2);ctx.fill();return;}
    distance-=lengths[i];
  }
}
function policyDemo(t) {
  const local=t-16,rule=local<4?0:local<10?1:local<14?2:3;
  const age=local-[0,4,10,14][rule];toolbar(rule);
  ctx.drawImage(filter,440,269,96,104);text('BUILDING',488,365,11,C.muted,'Mono','center');
  const old='#d8a472';
  for(let i=0;i<3;i++)line(screen(routes['policyPipe'+i]),old,7);
  line([[665,157],[690,157]],old,5);text('EXISTING PIPES',700,162,12,C.muted,'Mono');
  line([[665,181],[690,181]],C.cyan,4);text('NEW ROUTE',700,186,12,C.muted,'Mono');
  const keys=['policyBuildings','policyOverlap','policyBoth','policyStrict'];
  const route=screen(routes[keys[rule]]);
  const before=rule===1?center:screen(routes[rule===2?'policyBuildings':'policyBoth']);
  if(rule>0)ghost(before);
  if(rule>0&&age<.85){
    line(before,'#d98175',4,[5,5]);
    tag(rule===3?'CROSSING BLOCKED':'SHARED RUN BLOCKED');
  }else{
    line(route,C.cyan,4);movingDot(route,age);
    tag(['SHARED RUN ALLOWED','SHARED RUN AVOIDED','BUILDINGS + SHARED RUNS','NO OVERLAP / NO CROSSING'][rule]);
  }
  if(rule===0){
    ring(450,244);ring(640,244);text('SHARED LENGTH',345,226,13,old,'Mono');
    text('CROSSING',601,446,13,old,'Mono');
  }else if(rule===1||rule===2){
    const crossY=rule===1?276:404;ring(640,crossY);
    // Enlarged detail makes the router's separate parallel runs easy to see.
    ctx.fillStyle=C.bg;ctx.fillRect(755,251,151,80);ctx.strokeStyle=C.edge;ctx.strokeRect(755.5,251.5,150,79);
    text('SEPARATE RUNS',830,267,12,C.ink,'Mono','center');
    line([[770,283],[890,283]],old,6);line([[770,310],[890,310]],C.cyan,4);
    text('ONE SNAP INTERVAL',830,325,10,C.muted,'Mono','center');
    text('CROSSING ALLOWED',543,446,13,C.cyan,'Mono');
  }else{
    ring(640,420);text('NO CROSSING',745,287,13,C.cyan,'Mono');
    text('AROUND THE END',395,455,13,C.cyan,'Mono');
    ctx.strokeStyle='#d98175';ctx.lineWidth=3;ctx.beginPath();
    ctx.moveTo(632,396);ctx.lineTo(648,412);ctx.moveTo(648,396);ctx.lineTo(632,412);ctx.stroke();
  }
  const details=['Shared pipe runs and crossings remain allowed. The building is avoided.',
    'The new pipe takes a separate lane, then crosses the upright pipe.',
    'The route clears the building and shared runs. The crossing stays allowed.',
    'The new route goes around the pipe end instead of crossing it.'];
  const titles=['Buildings: shared runs are allowed','Overlap: separate runs, crossing allowed',
    'Both: clear buildings and shared runs','Strict: go around the crossing'];
  caption('SHIFT',titles[rule],details[rule]);
}
function draw(t) {
  const stage=t<5?0:t<11?1:t<16?2:t<36?3:t<42?4:t<49?5:6;base(t,stage);
  if(stage===0){
    const started=t>=.6;if(started){toolbar();line(center);tag('CENTER / DEFAULT');}
    cursor(...(started?[mix(224,736,ease((t-.6)/2.5)),mix(260,388,ease((t-.6)/2.5))]:[224,260]),t<.8);
    caption('CONNECT','Start at a port. Tools appear immediately.','Right-angle routing is automatic from the first connection.',112);
  }else if(stage===1){
    toolbar();const i=Math.min(3,Math.floor((t-5)/1.5));
    const names=['NEAR START','NEAR END','FREEFORM','CENTER'];
    line([screen(routes.nearStart),screen(routes.nearEnd),[[224,260],[736,388]],center][i]);tag(names[i]);cursor(736,388);
    caption('P','Cycle route shapes','Center → Near Start → Near End → Freeform. Works after connecting.');
  }else if(stage===2){
    toolbar();const local=t-11,count=local<1?2:local<2.4?4:local<3.8?6:4;
    line(screen(routes['bends'+count]));tag(`${count} BENDS`);cursor(736,388);
    caption(local<3.8?'=':'−',local<3.8?'Add bends in pairs':'Reduce the bend count','Request 2–12 bends. The available space determines what fits.');
  }else if(stage===3){
    policyDemo(t);
  }else if(stage===4){
    toolbar();const local=t-36;let x=480;
    if(local>=1&&local<3)x=mix(480,584,ease((local-1)/2));else if(local>=3)x=mix(584,426,ease((local-3)/2));
    x=snapX(x);line([[224,260],[x,260],[x,388],[736,388]]);marker(x,324,local>=1);cursor(x,324,local>=1);tag('CONNECTED / STILL EDITING');
    caption('DRAG','Slide on the active snap grid','Snappy unlocked: 16-unit steps. Without it: the normal game grid.');
  }else if(stage===5){
    toolbar();const local=t-42;let x=480;if(local>=2)x=snapX(mix(480,592,ease((local-2)/2)));
    line([[224,260],[x,260],[x,388],[736,388]]);
    if(local>=1.1&&local<5.2)marker(x,324,local>=2&&local<4);
    cursor(x,324,local>=2&&local<4);tag('EXISTING BENT PIPE');
    if(local<5.2)caption('ALT+P','Insert a slider on an existing section','Click the section you want—even when the whole pipe has bends.');
    else caption('R-CLICK','Remove the marker. Keep the pipe.','Sliders are session-only. Pipe shapes save normally.',112);
  }else{
    toolbar();line(center);marker(480,324);text('START A CONNECTION',672,274,15,C.gold,'Mono');
    text('Shape it. Connect it.',672,305,20,C.ink,'Bold');text('Slide it into place.',672,331,20,C.ink,'Bold');
    caption('READY','P  modes    = / −  bends    Shift  avoidance','Alt+P  insert slider     Right-click  remove     Click away  finish');
  }
}
fs.mkdirSync(path.join(out,'Frames'),{recursive:true});
for(let frame=0;frame<seconds*fps;frame++){
  draw(frame/fps);fs.writeFileSync(path.join(out,'Frames',`${String(frame).padStart(4,'0')}.png`),canvas.toBuffer('image/png'));
}
fs.writeFileSync(path.join(out,'render.json'),JSON.stringify({width:960,height:600,fps,seconds,frames:seconds*fps,version},null,2));
console.log(`Rendered ${seconds*fps} frames at 960×600.`);
