import { test, expect } from '@playwright/test';
import { readFile } from 'node:fs/promises';

const root = new URL('../',import.meta.url), origin='https://theme-preview.invalid';
async function studio(page){
  const seen=[];const requests=[];
  await page.route('**/*',async route=>{
    const url=new URL(route.request().url());requests.push(url.href);
    if(url.origin!==origin){await route.abort();return;}
    let path,contentType;
    if(url.pathname==='/'){path='launcher/Windows/ui/studio.html';contentType='text/html';}
    else if(['/studio.js','/studio.css'].includes(url.pathname)){path='launcher/Windows/ui'+url.pathname;contentType=url.pathname.endsWith('js')?'text/javascript':'text/css';}
    else if(url.pathname.startsWith('/font/')){path='assets/fonts/NotoSans-'+(url.pathname.includes('bold')?'Bold':'Regular')+'.ttf';contentType='font/ttf';}
    else if(url.pathname.startsWith('/art/')){const parts=url.pathname.split('/');path='assets/generated/'+parts[2]+'-'+parts[3]+'.svg';contentType='image/svg+xml';}
    else if(url.pathname==='/preview'){path='.local/builder-fixtures/'+(url.searchParams.get('preset')||'primebuild')+'.html';contentType='text/html';}
    else{await route.abort();return;}
    let body=await readFile(new URL(path,root));
    if(url.pathname==='/preview')body=Buffer.from(body.toString().replace('<body>','<body data-embedded="true">'));
    await route.fulfill({body,contentType});
  });
  await page.addInitScript(()=>{
    let listener,revision=0;
    const state={type:'state',values:{name:'Your community',guild:'987654321012345678',preset:'primebuild',palette:'primebuild',accent:'#ff6600',surface:'#101013',font:'primebuild',banner:'primebuild',background:'primebuild'},assets:{},dirty:false,saved:false,acknowledged:false,install:false,busy:false,previewMode:true,version:'0.3.0-beta.2',notice:'Preview mode · Discord is never changed.',validation:'',previewId:'1',previewUri:'https://theme-preview.invalid/preview?revision=1',canApply:false,canDisable:false,canRestore:false,canInstall:false,installation:'Discord + Vencord ready.'};
    window.testMessages=[];
    window.chrome={webview:{addEventListener:(_,callback)=>listener=callback,postMessage:message=>{
      window.testMessages.push(message);
      if(message.action==='edit')state.values=message.values;
      if(message.action==='preset'){for(const field of['preset','font','palette','banner','background'])state.values[field]=message.value;}
      if(['edit','preset','refresh'].includes(message.action)){state.previewId=String(++revision+1);state.previewUri='https://theme-preview.invalid/preview?revision='+state.previewId+'&preset='+state.values.preset;state.dirty=true;}
      if(message.action==='acknowledge')state.acknowledged=message.ready;
      if(message.action==='previewReady'&&message.id!==state.previewId)return;
      queueMicrotask(()=>listener?.({data:structuredClone(state)}));
    }}};
  });
  await page.goto(origin+'/');
  await expect(page.locator('#preview-loading')).toBeHidden();
  await expect(page.locator('#preview-error')).toBeHidden();
  return requests;
}

test('studio loads real artwork and readable controls without external dependencies',async({page})=>{
  const requests=await studio(page);
  await expect(page.getByRole('heading',{name:'Your server. Your style.'})).toBeVisible();
  const frame=page.frameLocator('#preview');
  await expect(frame.locator('.header_f37cb1 h2')).toHaveCSS('font-family',/PrimeBuild Personal Display/);
  await expect(frame.locator('.chatContent_f75fb0')).not.toHaveCSS('background-image','none');
  expect(await page.locator('.preset-card img').evaluateAll(images=>images.every(image=>image.complete&&image.naturalWidth>0))).toBe(true);
  expect(await page.evaluate(()=>[...document.fonts].some(font=>font.family==='Studio'&&font.status==='loaded'))).toBe(true);
  expect(requests.every(url=>url.startsWith(origin))).toBe(true);
  await page.screenshot({path:'.local/studio-design-preview.png'});
});

test('starting preset and isolation context have separate clear controls',async({page})=>{
  await studio(page);
  await page.getByRole('button',{name:'Prime Build Dev',exact:true}).click();
  await expect(page.locator('#preview-loading')).toBeHidden();
  await expect(page.locator('[data-preset="primebuild-dev"]')).toHaveAttribute('aria-pressed','true');
  await expect(page.frameLocator('#preview').locator('.chatContent_f75fb0')).toHaveCSS('background-color','rgb(16, 19, 27)');
  await page.getByRole('button',{name:'Other server',exact:true}).click();
  await expect(page.getByRole('status')).toContainText('Outside your target server');
  await expect(page.frameLocator('#preview').locator('.base__5e434')).toHaveCSS('background-color','rgb(49, 51, 56)');
  await page.getByRole('button',{name:'Your server',exact:true}).click();
  await expect(page.getByRole('status')).toContainText('Theme active');
  await page.getByRole('button',{name:'FantaFort',exact:true}).click();
  await expect(page.locator('#preview-loading')).toBeHidden();
  await expect(page.frameLocator('#preview').locator('.chatContent_f75fb0')).toHaveCSS('background-color','rgb(16, 23, 43)');
});

for(const viewport of[{width:1040,height:720},{width:1365,height:900},{width:1920,height:1080}])test(`studio keeps preview composer and primary action visible at ${viewport.width}×${viewport.height}`,async({page})=>{
  await page.setViewportSize(viewport);await studio(page);
  const frameBox=await page.locator('#preview').boundingBox();
  const composer=await page.frameLocator('#preview').locator('.scrollableContainer__74017').boundingBox();
  expect(composer.y+composer.height).toBeLessThanOrEqual(frameBox.y+frameBox.height);
  const apply=await page.locator('#apply').boundingBox();expect(apply.y+apply.height).toBeLessThanOrEqual(viewport.height);
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  await page.getByRole('button',{name:'Style',exact:true}).click();
  await expect(page.getByRole('combobox',{name:'Display font'})).toBeVisible();
  await page.getByRole('button',{name:'Artwork',exact:true}).click();
  await expect(page.getByRole('heading',{name:'Server banner',exact:true})).toBeVisible();
  expect(await page.locator('#banner-cards img').evaluateAll(images=>images.every(image=>image.complete&&image.naturalWidth>0))).toBe(true);
});

test('preview mode stays non-mutating and edit values flush before saving',async({page})=>{
  await studio(page);
  await page.getByLabel('Theme name',{exact:true}).fill('My custom name');
  await page.getByRole('button',{name:'Save',exact:true}).click();
  const actions=await page.evaluate(()=>window.testMessages.map(message=>message.action));
  expect(actions.lastIndexOf('edit')).toBeLessThan(actions.lastIndexOf('save'));
  await page.getByLabel('Calls finished, drafts saved. Discord may restart.').check();
  await expect(page.locator('#apply')).toBeDisabled();
  await page.getByRole('button',{name:'Recovery',exact:true}).click();
  await expect(page.locator('#disable')).toBeDisabled();await expect(page.locator('#restore')).toBeDisabled();
});
