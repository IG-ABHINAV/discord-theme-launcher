import { test, expect } from '@playwright/test';
import { readFile } from 'node:fs/promises';

const root = new URL('../',import.meta.url), origin='https://theme-preview.invalid';
async function studio(page,brokenResources=false){
  const seen=[];const requests=[];
  await page.route('**/*',async route=>{
    const url=new URL(route.request().url());requests.push(url.href);
    if(url.origin!==origin){await route.abort();return;}
    let path,contentType;
    if(url.pathname==='/'){path='launcher/Windows/ui/studio.html';contentType='text/html';}
    else if(['/studio.js','/studio.css','/i18n.js'].includes(url.pathname)){path='launcher/Windows/ui'+url.pathname;contentType=url.pathname.endsWith('js')?'text/javascript':'text/css';}
    else if(url.pathname.startsWith('/font/')){path='assets/fonts/NotoSans-'+(url.pathname.includes('bold')?'Bold':'Regular')+'.ttf';contentType='font/ttf';}
    else if(url.pathname.startsWith('/art/')){const parts=url.pathname.split('/');if(parts[2]==='zenit-gold'){const css=await readFile(new URL('assets/community/zenit-gold/ZenitGold.theme.css',root),'utf8');const uri=css.match(/--zg-banner:\s*url\("([^"]+)"\)/)[1];await route.fulfill({body:Buffer.from(uri.split(',')[1],'base64'),contentType:'image/webp'});return;}path='assets/generated/'+parts[2]+'-'+parts[3]+'.svg';contentType='image/svg+xml';}
    else if(url.pathname==='/preview'){path='.local/builder-fixtures/'+(brokenResources?'zenit-gold':url.searchParams.get('preset')||'primebuild')+'.html';contentType='text/html';}
    else{await route.abort();return;}
    let body=await readFile(new URL(path,root));
    if(url.pathname==='/preview')body=Buffer.from(body.toString().replace('<body>','<body data-embedded="true">'));
    if(url.pathname==='/preview'&&brokenResources)body=Buffer.from(body.toString().replace(/data:image\/webp;base64,[A-Za-z0-9+/=]+/g,'data:image/png;base64,AA=='));
    await route.fulfill({body,contentType});
  });
  await page.addInitScript(({designs,review})=>{
    let listener,revision=0;
    const state={type:'state',designs,values:{name:'Your community',guild:'987654321012345678',preset:'fluent',palette:'custom',accent:'#8791ff',surface:'#202020',font:'native',banner:'none',background:'none'},assets:{},dirty:false,saved:false,busy:false,version:'0.3.0-beta.5',language:'en',notice:'Everything stays in preview until you confirm Apply.',validation:'',previewId:'1',previewUri:'https://theme-preview.invalid/preview?revision=1&preset=fluent',canApply:true,applyReason:'',applyAction:'',applyActionLabel:'',canDisable:false,canRestore:false,confirmation:null,installation:'Discord + Vencord ready.'};
    window.testSetupRequired=false;window.testConfirmationProblem='';
    window.patchTestState=patch=>{Object.assign(state,patch);listener?.({data:structuredClone(state)})};
    window.testMessages=[];
    window.chrome={webview:{addEventListener:(_,callback)=>listener=callback,postMessage:message=>{
      window.testMessages.push(message);
      if(message.action==='edit')state.values=message.values;
      if(['preset','community'].includes(message.action)){if(message.value==='zenit-gold'){state.imported=review;state.values.preset='zenit-gold';state.values.font='native'}else{state.imported=null;for(const field of['preset','font','palette','banner','background'])state.values[field]=message.value;}}
      if(message.action==='importCss')state.importReview=review;
      if(message.action==='cancelImport')state.importReview=null;
      if(message.action==='acceptImport'){state.imported=state.importReview;state.importReview=null;state.values.preset='zenit-gold';state.values.font='native'}
      if(message.action==='importVariable'){state.imported.variables.find(v=>v.name===message.name).value=message.value;state.values.preset='zenit-edited'}
      if(['edit','preset','community','acceptImport','importVariable','refresh'].includes(message.action)){state.previewId=String(++revision+1);state.previewUri='https://theme-preview.invalid/preview?revision='+state.previewId+'&preset='+state.values.preset;state.dirty=true;}
      if(['apply','disable','restore'].includes(message.action))state.confirmation={id:'confirmation-'+(++revision),operation:message.action,themeName:state.values.name,guild:state.values.guild,serverName:'Selected server',requiresSetup:window.testSetupRequired,repair:false,problem:window.testConfirmationProblem};
      if(message.action==='cancelConfirmation'&&message.id===state.confirmation?.id)state.confirmation=null;
      if(message.action==='confirmOperation'&&message.id===state.confirmation?.id&&!state.confirmation.problem&&(!state.confirmation.requiresSetup||message.allowSetup)){state.confirmation=null;state.notice='Theme applied in the test fixture.'}
      if(message.action==='language'&&['en','it','es','ru','de'].includes(message.value))state.language=message.value;
      if(message.action==='previewReady'&&message.id!==state.previewId)return;
      if(message.action==='previewReady'){state.canApply=message.ready;state.validation=message.ready?'':'An embedded preview resource did not load.';if(!message.ready)state.previewUri='';}
      queueMicrotask(()=>listener?.({data:structuredClone(state)}));
    }}};
  },{designs:JSON.parse(await readFile(new URL('src/community.json',root),'utf8')),review:JSON.parse(await readFile(new URL('.local/builder-fixtures/zenit-review.json',root),'utf8'))});
  await page.goto(origin+'/');
  await expect(page.locator('#preview-loading')).toBeHidden();
  if(brokenResources)await expect(page.locator('#preview-error')).toBeVisible();else await expect(page.locator('#preview-error')).toBeHidden();
  return requests;
}

test('editing starts in preview and Apply opens a confirmation without applying',async({page})=>{
  await studio(page);
  await expect(page.locator('#mode-badge')).toHaveText('Preview');
  await expect(page.locator('#restart-ready')).toHaveCount(0);
  await expect(page.locator('#apply')).toBeEnabled();
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.locator('#confirmation-theme')).toHaveText('Your community');
  await expect(page.locator('#confirmation-guild')).toHaveText('987654321012345678');
  await expect(page.locator('#confirmation-cancel')).toBeFocused();
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
  await page.screenshot({path:'.local/studio-confirmation-preview.png'});
});

test('an undecodable embedded image blocks application and produces a resource failure',async({page})=>{
  await studio(page,true);await expect(page.locator('#apply')).toBeDisabled();
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='previewReady'&&!m.ready))).toBe(true);
});

test('Back to preview and Escape cancel without sending application confirmation',async({page})=>{
  await studio(page);
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
  await expect(page.getByRole('dialog')).toBeHidden();
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toBeHidden();
  await expect(page.getByLabel('Theme name',{exact:true})).toHaveValue('Your community');
  expect(await page.evaluate(()=>window.testMessages.filter(m=>m.action==='confirmOperation').length)).toBe(0);
});

test('Continue and apply confirms the reviewed operation once',async({page})=>{
  await studio(page);
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await page.getByRole('button',{name:'Continue & apply',exact:true}).click();
  await expect(page.getByRole('dialog')).toBeHidden();
  const confirmations=await page.evaluate(()=>window.testMessages.filter(m=>m.action==='confirmOperation'));
  expect(confirmations).toHaveLength(1);expect(confirmations[0].id).toMatch(/^confirmation-/);expect(confirmations[0].allowSetup).toBe(false);
});

test('first-time Vencord setup is offered inside the confirmation and requires consent',async({page})=>{
  await studio(page);await page.evaluate(()=>window.testSetupRequired=true);
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await expect(page.locator('#confirmation-install')).not.toBeChecked();
  await expect(page.getByRole('button',{name:'Continue & apply',exact:true})).toBeDisabled();
  await page.getByLabel('Install official Vencord for stable Discord',{exact:true}).check();
  await page.getByRole('button',{name:'Continue & apply',exact:true}).click();
  expect(await page.evaluate(()=>window.testMessages.find(m=>m.action==='confirmOperation').allowSetup)).toBe(true);
});

test('unsupported installation is explained inside confirmation and cannot continue',async({page})=>{
  await studio(page);await page.evaluate(()=>window.testConfirmationProblem='Install the standard Discord desktop client first.');
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await expect(page.locator('#confirmation-problem')).toContainText('standard Discord');
  await expect(page.getByRole('button',{name:'Continue & apply',exact:true})).toBeDisabled();
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
});

test('Recovery disable and restore also require a reviewed confirmation',async({page})=>{
  await studio(page);await page.evaluate(()=>window.patchTestState({canDisable:true,canRestore:true}));
  await page.getByRole('button',{name:'Recovery',exact:true}).click();
  await page.getByRole('button',{name:'Disable personal theme',exact:true}).click();
  await expect(page.getByRole('heading',{name:'Disable your personal theme?',exact:true})).toBeVisible();
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
  await page.getByRole('button',{name:'Restore previous setup',exact:true}).click();
  await expect(page.getByRole('heading',{name:'Restore the previous setup?',exact:true})).toBeVisible();
  await page.getByRole('button',{name:'Continue & restore',exact:true}).click();
  expect(await page.evaluate(()=>window.testMessages.filter(m=>m.action==='confirmOperation').length)).toBe(1);
});

test('studio loads real artwork and readable controls without external dependencies',async({page})=>{
  const requests=await studio(page);
  await expect(page.getByRole('heading',{name:'Your server. Your style.'})).toBeVisible();
  const frame=page.frameLocator('#preview');
  await expect(frame.locator('.header_f37cb1 h2')).not.toHaveCSS('font-family',/PrimeBuild Personal Display/);
  await expect(frame.locator('.chatContent_f75fb0')).toHaveCSS('background-image',/^none(?:, none)*$/);
  expect(await page.locator('.preset-card img').evaluateAll(images=>images.every(image=>image.complete&&image.naturalWidth>0))).toBe(true);
  expect(await page.evaluate(()=>[...document.fonts].some(font=>font.family==='Studio'&&font.status==='loaded'))).toBe(true);
  expect(requests.every(url=>url.startsWith(origin))).toBe(true);
  await page.screenshot({path:'.local/studio-design-preview.png'});
});

test('starting preset and isolation context have separate clear controls',async({page})=>{
  await studio(page);
  await page.getByRole('button',{name:'Community',exact:true}).click();
  await page.getByRole('button',{name:'Prime Build Dev',exact:true}).click();
  await expect(page.locator('#preview-loading')).toBeHidden();
  await expect(page.locator('[data-preset="primebuild-dev"]')).toHaveAttribute('aria-pressed','true');
  await expect(page.frameLocator('#preview').locator('.chatContent_f75fb0')).toHaveCSS('background-color','rgb(16, 19, 27)');
  await page.getByRole('button',{name:'Other server',exact:true}).click();
  await expect(page.getByRole('status')).toContainText('Outside your target server');
  await expect(page.frameLocator('#preview').locator('.base__5e434')).toHaveCSS('background-color','rgb(49, 51, 56)');
  await page.getByRole('button',{name:'Your server',exact:true}).click();
  await expect(page.getByRole('status')).toContainText('Theme active');
  await page.getByRole('button',{name:'Community',exact:true}).click();
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

test('editing and save actions remain previews until the explicit application confirmation',async({page})=>{
  await studio(page);
  await page.getByLabel('Theme name',{exact:true}).fill('My custom name');
  await page.getByRole('button',{name:'Save',exact:true}).click();
  const actions=await page.evaluate(()=>window.testMessages.map(message=>message.action));
  expect(actions.lastIndexOf('edit')).toBeLessThan(actions.lastIndexOf('save'));
  expect(actions).not.toContain('confirmOperation');
  await page.getByRole('button',{name:'Apply to Discord',exact:true}).click();
  await expect(page.locator('#confirmation-theme')).toHaveText('My custom name');
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
});

test('interface defaults to English and can switch to Italian, Spanish and Russian',async({page})=>{
  await studio(page);
  await expect(page.locator('#language')).toHaveValue('en');
  await expect(page.locator('html')).toHaveAttribute('lang','en');
  const catalog=await page.evaluate(()=>{
    const keys=Object.keys(I18N.en);
    return ['it','es','ru'].every(code=>keys.every(key=>typeof I18N[code][key]==='string'&&I18N[code][key].length>0)&&keys.length===Object.keys(I18N[code]).length);
  });
  expect(catalog).toBe(true);
  await page.locator('#language').selectOption('it');
  await expect(page.locator('html')).toHaveAttribute('lang','it');
  await expect(page.getByRole('button',{name:'Applica su Discord',exact:true})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Il tuo server. Il tuo stile.'})).toBeVisible();
  await page.locator('#language').selectOption('es');
  await expect(page.getByRole('button',{name:'Aplicar en Discord',exact:true})).toBeVisible();
  await page.locator('#language').selectOption('ru');
  await expect(page.locator('html')).toHaveAttribute('lang','ru');
  await expect(page.getByRole('button',{name:'Применить в Discord',exact:true})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Ваш сервер. Ваш стиль.'})).toBeVisible();
  await page.locator('#language').selectOption('en');
  await expect(page.getByRole('button',{name:'Apply to Discord',exact:true})).toBeVisible();
  await expect(page.getByLabel('Server ID or channel link')).toBeVisible();
});

test('community exposes four credited designs, catalog browsing and a working submission action',async({page})=>{
  await studio(page);await page.getByRole('button',{name:'Community',exact:true}).click();
  await expect(page.locator('.community-card')).toHaveCount(4);
  await expect(page.getByRole('button',{name:'Zenit Gold — by JACKPOT71',exact:true})).toBeVisible();
  await page.getByRole('button',{name:'Browse BetterDiscord themes',exact:true}).click();
  await page.getByRole('button',{name:'Propose a theme',exact:true}).click();
  const actions=await page.evaluate(()=>window.testMessages.map(m=>m.action));
  expect(actions).toContain('browseThemes');expect(actions).toContain('submitTheme');expect(actions).not.toContain('confirmOperation');
  await page.screenshot({path:'.local/studio-community-preview.png'});
});

test('downloaded CSS is reviewed before accepting, cancellation leaves the existing design untouched',async({page})=>{
  await studio(page);await page.getByRole('button',{name:'Community',exact:true}).click();
  await page.getByRole('button',{name:'Import downloaded .theme.css',exact:true}).click();
  await expect(page.locator('#import-review')).toBeVisible();
  await expect(page.locator('#import-review-resources')).toContainText('Montserrat');
  await expect(page.locator('#import-review-status')).toContainText('Compatible');
  await page.keyboard.press('Escape');await expect(page.locator('#import-review')).toBeHidden();
  expect(await page.evaluate(()=>state.values.preset)).toBe('fluent');
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='acceptImport'||m.action==='confirmOperation'))).toBe(false);
});

test('accepted CSS previews the author design, edits supported variables, and shares without applying',async({page})=>{
  await studio(page);await page.getByRole('button',{name:'Community',exact:true}).click();
  await page.getByRole('button',{name:'Import downloaded .theme.css',exact:true}).click();
  await page.getByRole('button',{name:'Use this design',exact:true}).click();
  await expect(page.locator('#preview-loading')).toBeHidden();
  expect(await page.frameLocator('#preview').locator('.base__5e434').evaluate(el=>getComputedStyle(el).getPropertyValue('--zg-gold').trim())).toBe('#e6c15e');
  await page.getByRole('button',{name:'Style',exact:true}).click();
  await expect(page.locator('#builtin-style')).toBeHidden();
  await page.locator('[data-variable="--zg-gold"]').fill('#31c6b4');await page.locator('[data-variable="--zg-gold"]').dispatchEvent('change');
  await expect(page.locator('#preview-loading')).toBeHidden();
  await page.getByRole('button',{name:'My themes',exact:true}).click();
  await expect(page.locator('#imported-license')).toContainText('Copyright (c) 2026 ZENIT / Jackpot');
  await page.locator('[data-panel=library] [data-action=share]').click();
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
  await page.screenshot({path:'.local/studio-zenit-preview.png'});
});

test('unsupported CSS report blocks acceptance before any configuration change',async({page})=>{
  await studio(page);
  await page.evaluate(()=>window.patchTestState({importReview:{supported:false,name:'Unsupported CSS',notes:['A stylesheet-level @import needs a reviewed offline adapter.'],resources:[],variables:[]}}));
  await expect(page.locator('#import-accept')).toBeDisabled();
  await expect(page.locator('#import-review-notes')).toContainText('@import');
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
  expect(await page.evaluate(()=>state.values.preset)).toBe('fluent');
});

for(const viewport of [{width:1040,height:720},{width:1365,height:900}])test(`German copy is complete and long labels stay usable at ${viewport.width}`,async({page})=>{
  await page.setViewportSize(viewport);await studio(page);await page.locator('#language').selectOption('de');
  expect(await page.evaluate(()=>Object.keys(I18N.en).every(key=>typeof I18N.de[key]==='string'&&I18N.de[key].length>0)&&Object.keys(I18N.en).length===Object.keys(I18N.de).length)).toBe(true);
  await expect(page.getByRole('button',{name:'Auf Discord anwenden',exact:true})).toBeVisible();
  await page.getByRole('button',{name:'Wiederherstellung',exact:true}).click();
  await expect(page.getByRole('button',{name:'Vorherige Einrichtung wiederherstellen',exact:true})).toBeVisible();
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  const apply=await page.locator('#apply').boundingBox();expect(apply.y+apply.height).toBeLessThanOrEqual(viewport.height);
  await page.screenshot({path:'.local/studio-german-'+viewport.width+'.png'});
});
