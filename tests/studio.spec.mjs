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
    else if(['/studio.js','/studio.css','/i18n.js'].includes(url.pathname)){path='launcher/Windows/ui'+url.pathname;contentType=url.pathname.endsWith('js')?'text/javascript':'text/css';}
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
    const state={type:'state',values:{name:'Your community',guild:'987654321012345678',preset:'primebuild',palette:'primebuild',accent:'#ff6600',surface:'#101013',font:'primebuild',banner:'primebuild',background:'primebuild'},assets:{},dirty:false,saved:false,busy:false,version:'0.3.0-beta.5',language:'en',notice:'Everything stays in preview until you confirm Apply.',validation:'',previewId:'1',previewUri:'https://theme-preview.invalid/preview?revision=1',canApply:true,applyReason:'',applyAction:'',applyActionLabel:'',canDisable:false,canRestore:false,confirmation:null,installation:'Discord + Vencord ready.'};
    window.testSetupRequired=false;window.testConfirmationProblem='';
    window.patchTestState=patch=>{Object.assign(state,patch);listener?.({data:structuredClone(state)})};
    window.testMessages=[];
    window.chrome={webview:{addEventListener:(_,callback)=>listener=callback,postMessage:message=>{
      window.testMessages.push(message);
      if(message.action==='edit')state.values=message.values;
      if(message.action==='preset'){for(const field of['preset','font','palette','banner','background'])state.values[field]=message.value;}
      if(['edit','preset','refresh'].includes(message.action)){state.previewId=String(++revision+1);state.previewUri='https://theme-preview.invalid/preview?revision='+state.previewId+'&preset='+state.values.preset;state.dirty=true;}
      if(['apply','disable','restore'].includes(message.action))state.confirmation={id:'confirmation-'+(++revision),operation:message.action,themeName:state.values.name,guild:state.values.guild,serverName:'Selected server',requiresSetup:window.testSetupRequired,repair:false,problem:window.testConfirmationProblem};
      if(message.action==='cancelConfirmation'&&message.id===state.confirmation?.id)state.confirmation=null;
      if(message.action==='confirmOperation'&&message.id===state.confirmation?.id&&!state.confirmation.problem&&(!state.confirmation.requiresSetup||message.allowSetup)){state.confirmation=null;state.notice='Theme applied in the test fixture.'}
      if(message.action==='language'&&['en','it','es','ru'].includes(message.value))state.language=message.value;
      if(message.action==='previewReady'&&message.id!==state.previewId)return;
      queueMicrotask(()=>listener?.({data:structuredClone(state)}));
    }}};
  });
  await page.goto(origin+'/');
  await expect(page.locator('#preview-loading')).toBeHidden();
  await expect(page.locator('#preview-error')).toBeHidden();
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
