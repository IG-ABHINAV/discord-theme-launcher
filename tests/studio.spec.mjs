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
    else if(url.pathname==='/app-icon.svg'){path='assets/app-icon.svg';contentType='image/svg+xml';}
    else if(['/studio.js','/studio.css','/i18n.js'].includes(url.pathname)){path='launcher/Windows/ui'+url.pathname;contentType=url.pathname.endsWith('js')?'text/javascript':'text/css';}
    else if(url.pathname.startsWith('/font/')){path='assets/fonts/NotoSans-'+(url.pathname.includes('bold')?'Bold':'Regular')+'.ttf';contentType='font/ttf';}
    else if(url.pathname.startsWith('/selected-art/')||url.pathname.startsWith('/local-image/')){path='assets/prime-build-logo.png';contentType='image/png';}
    else if(url.pathname.startsWith('/art/')){const parts=url.pathname.split('/');if(parts[2]==='zenit-gold'){const css=await readFile(new URL('assets/community/zenit-gold/ZenitGold.theme.css',root),'utf8');const uri=css.match(/--zg-banner:\s*url\("([^"]+)"\)/)[1];await route.fulfill({body:Buffer.from(uri.split(',')[1],'base64'),contentType:'image/webp'});return;}path='assets/generated/'+parts[2]+'-'+parts[3]+'.svg';contentType='image/svg+xml';}
    else if(url.pathname==='/preview'){path='.local/builder-fixtures/'+(brokenResources?'zenit-gold':url.searchParams.get('preset')||'primebuild')+'.html';contentType='text/html';}
    else{await route.abort();return;}
    let body=await readFile(new URL(path,root));
    if(url.pathname==='/preview')body=Buffer.from(body.toString().replace('<body>','<body data-embedded="true">'));
    if(url.pathname==='/preview'&&brokenResources)body=Buffer.from(body.toString().replace(/data:image\/webp;base64,[A-Za-z0-9+/=]+/g,'data:image/png;base64,AA=='));
    await route.fulfill({body,contentType});
  });
  await page.addInitScript(({designs,review,fonts})=>{
    let listener,revision=0;
    const state={type:'state',designs,fonts,values:{name:'Your community',guild:'987654321012345678',preset:'fluent',palette:'custom',accent:'#8791ff',surface:'#202020',font:'native',banner:'none',background:'none',panel:'#202020',dms:''},assets:{},localThemes:[],localPalettes:[],localImages:[],paletteId:'',dirty:false,saved:false,busy:false,version:'0.3.0-beta.5',language:'en',notice:'Everything stays in preview until you confirm Apply.',validation:'',previewId:'1',previewUri:'https://theme-preview.invalid/preview?revision=1&preset=fluent',canApply:true,applyReason:'',applyAction:'',applyActionLabel:'',canDisable:false,canRestore:false,confirmation:null,installation:'Discord + Vencord ready.'};
    window.testSetupRequired=false;window.testConfirmationProblem='';
    window.patchTestState=patch=>{Object.assign(state,patch);listener?.({data:structuredClone(state)})};
    window.testMessages=[];
    window.chrome={webview:{addEventListener:(_,callback)=>listener=callback,postMessage:message=>{
      window.testMessages.push(message);
      if(message.action==='edit')state.values=message.values;
      if(message.action==='loadLocalTheme'){const theme=state.localThemes.find(t=>t.id===message.value);if(theme){state.values={...state.values,...theme.values,name:theme.name};state.localThemeId=theme.id;state.saved=true;state.dirty=false;}}
      if(message.action==='duplicateLocalTheme')state.localThemes.push({id:'duplicate'+state.localThemes.length,name:message.name});
      if(message.action==='save'){state.localThemes.push({id:'local'+state.localThemes.length,name:state.values.name});state.localThemeId='local'+(state.localThemes.length-1);state.saved=true;state.dirty=false;}
      if(message.action==='savePalette'){state.paletteId=state.paletteId||'palette'+state.localPalettes.length;const value={id:state.paletteId,name:message.name,accent:state.values.accent,surface:state.values.surface,panel:state.values.panel};state.localPalettes=state.localPalettes.filter(p=>p.id!==value.id).concat(value)}
      if(message.action==='loadPalette'){const palette=state.localPalettes.find(p=>p.id===message.value);Object.assign(state.values,{palette:'custom',accent:palette.accent,surface:palette.surface,panel:palette.panel});state.paletteId=palette.id}
      if(message.action==='newPalette'){state.paletteId='';state.values.palette='custom'}
      if(message.action==='saveImage')state.localImages.push({id:'image'+state.localImages.length,name:message.name,sha256:state.assets[message.kind+'Hash']});
      if(message.action==='loadImage'){state.values[message.kind]='custom';state.assets[message.kind]=state.localImages.find(i=>i.id===message.value).name;state.assets[message.kind+'Hash']=state.localImages.find(i=>i.id===message.value).sha256}
      if(message.action==='renameLocalTheme')state.localThemes.find(t=>t.id===message.value).name=message.name;

      if(message.action==='downloadFont'){state.values.font='custom';state.assets.font=fonts.find(f=>f.id===message.value).fileName;}
      if(['preset','community'].includes(message.action)){if(message.value==='zenit-gold'){state.imported=review;state.values.preset='zenit-gold';state.values.font='native'}else{state.imported=null;for(const field of['preset','font','palette','banner','background'])state.values[field]=message.value;}}
      if(['importCss','open'].includes(message.action))state.importReview=review;
      if(message.action==='cancelImport')state.importReview=null;
      if(message.action==='acceptImport'){state.imported=state.importReview;state.importReview=null;state.values.preset='zenit-gold';state.values.font='native'}
      if(message.action==='importVariable'){state.imported.variables.find(v=>v.name===message.name).value=message.value;state.values.preset='zenit-edited'}
      if(['edit','preset','community','acceptImport','importVariable','refresh'].includes(message.action)){state.previewId=String(++revision+1);state.previewUri='https://theme-preview.invalid/preview?revision='+state.previewId+'&preset='+state.values.preset;state.dirty=true;}
      if(['apply','disable','restore','disableManaged','disableAll'].includes(message.action))state.confirmation={id:'confirmation-'+(++revision),operation:message.action,themeName:state.values.name,guild:state.values.guild,serverName:'Selected server',requiresSetup:window.testSetupRequired,repair:false,problem:window.testConfirmationProblem};
      if(message.action==='cancelConfirmation'&&message.id===state.confirmation?.id)state.confirmation=null;
      if(message.action==='confirmOperation'&&message.id===state.confirmation?.id&&!state.confirmation.problem&&(!state.confirmation.requiresSetup||message.allowSetup)){state.confirmation=null;state.notice='Theme applied in the test fixture.'}
      if(message.action==='language'&&['en','it','es','ru','de'].includes(message.value))state.language=message.value;
      if(message.action==='previewReady'&&message.id!==state.previewId)return;
      if(message.action==='previewReady'){state.canApply=message.ready;state.validation=message.ready?'':'An embedded preview resource did not load.';if(!message.ready)state.previewUri='';}
      queueMicrotask(()=>listener?.({data:structuredClone(state)}));
    }}};
  },{fonts:JSON.parse(await readFile(new URL('src/font-catalog.json',root),'utf8')).fonts.map(f=>({...f,fileName:f.path.split('/').at(-1)})),designs:JSON.parse(await readFile(new URL('src/community.json',root),'utf8')),review:JSON.parse(await readFile(new URL('.local/builder-fixtures/zenit-review.json',root),'utf8'))});
  await page.goto(origin+'/');
  await expect(page.locator('#preview-loading')).toBeHidden();
  if(brokenResources)await expect(page.locator('#preview-error')).toBeVisible();else await expect(page.locator('#preview-error')).toBeHidden();
  await page.locator('[data-page=editor]').click();
  return requests;
}

test('theme card descriptions are labeled, flushed before save and restored from host state',async({page})=>{
  await studio(page);
  const description=page.getByLabel('Description (optional)',{exact:true});
  await expect(description).toHaveAttribute('maxlength','280');
  await expect(description).toHaveAttribute('aria-describedby','theme-description-hint');
  await page.getByLabel('Theme name',{exact:true}).fill('Pink Harddisk');
  await description.fill('Pink artwork for Giorgio.\nMade by me.');
  await page.locator('.file-actions [data-action=save]').click();
  const edits=await page.evaluate(()=>window.testMessages.filter(m=>m.action==='edit').at(-1).values);
  expect(edits.name).toBe('Pink Harddisk');
  expect(edits.description).toBe('Pink artwork for Giorgio.\nMade by me.');
  await page.evaluate(()=>window.patchTestState({values:{...readEdits(),description:'Reopened description'}}));
  await expect(description).toHaveValue('Reopened description');
});

test('Home exposes saved themes and opens their existing server list without creating a new design',async({page})=>{
  await studio(page);
  await page.evaluate(()=>window.patchTestState({localThemes:[{id:'existing',name:'Work theme',values:{guild:'987654321012345678\n876543210123456789'}}]}));
  await page.locator('[data-page=home]').click();
  await page.getByRole('button',{name:'Use a saved theme',exact:true}).click();
  await expect(page.locator('#template-tab-local')).toHaveAttribute('aria-selected','true');
  await page.getByRole('button',{name:'Edit Work theme',exact:true}).click();
  await expect(page.getByLabel('Theme name',{exact:true})).toHaveValue('Work theme');
  await expect(page.locator('#guild')).toHaveValue('987654321012345678\n876543210123456789');
  await expect(page.locator('#targets-help')).toContainText('Keep existing IDs');
  await expect(page.locator('#save-apply-hint')).toContainText('Save keeps your theme');
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='new'||m.action==='confirmOperation'))).toBe(false);
});

test('My themes shows which themes are active with quick enable, disable and two-step delete',async({page})=>{
  await studio(page);
  await page.evaluate(()=>window.patchTestState({localThemes:[{id:'pink',name:'Pink'},{id:'arena',name:'Arena'}],activeThemes:['pink']}));
  await page.locator('[data-page=home]').click();
  await page.locator('#home-saved').click();
  await expect(page.locator('[data-theme-status=pink]')).toHaveText('Active');
  await expect(page.locator('[data-theme-status=arena]')).toHaveText('Inactive');
  await page.locator('[data-toggle-theme=pink]').click();
  await page.locator('[data-toggle-theme=arena]').click();
  await page.locator('[data-delete-theme=arena]').click();
  await expect(page.locator('[data-delete-theme=arena]')).toHaveText('Confirm delete');
  expect(await page.evaluate(()=>window.testMessages.filter(m=>/LocalTheme$/.test(m.action)&&m.action!=='loadLocalTheme'))).toEqual([{action:'deactivateLocalTheme',value:'pink'},{action:'activateLocalTheme',value:'arena'}]);
  await page.locator('[data-delete-theme=arena]').click();
  expect(await page.evaluate(()=>window.testMessages.at(-1))).toEqual({action:'deleteLocalTheme',value:'arena'});
  await page.evaluate(()=>window.patchTestState({busy:true}));
  await expect(page.locator('[data-toggle-theme=pink]')).toBeDisabled();
});

test('update and theme optimization buttons appear only when the host offers them',async({page})=>{
  await studio(page);
  await expect(page.locator('#update-app')).toBeHidden();
  await expect(page.locator('#migrate-themes')).toBeHidden();
  await page.evaluate(()=>window.patchTestState({update:'0.3.0-beta.99',migrationPending:true}));
  await expect(page.locator('#update-app')).toHaveText('Update to 0.3.0-beta.99');
  await page.locator('#migrate-themes').click();
  await page.locator('#update-app').click();
  expect(await page.evaluate(()=>window.testMessages.filter(m=>['installUpdate','migrateThemes'].includes(m.action)).map(m=>m.action))).toEqual(['migrateThemes','installUpdate']);
  await page.evaluate(()=>window.patchTestState({busy:true}));
  await expect(page.locator('#update-app')).toBeDisabled();
});

test('a saved theme can be duplicated without replacing the open draft or changing Discord',async({page})=>{
  await studio(page);
  await page.evaluate(()=>window.patchTestState({dirty:true,localThemes:[{id:'original',name:'Original theme'}]}));
  await page.locator('[data-page=home]').click();
  await page.locator('#home-saved').click();
  await page.getByRole('button',{name:'Duplicate theme… · Original theme',exact:true}).click();
  await page.locator('#name-dialog-input').fill('Separate copy');
  await page.locator('#name-dialog-save').click();
  await expect(page.locator('#local-themes')).toContainText('Separate copy');
  expect(await page.evaluate(()=>readEdits().name)).toBe('Your community');
  expect(await page.evaluate(()=>window.testMessages.find(m=>m.action==='duplicateLocalTheme'))).toEqual({action:'duplicateLocalTheme',value:'original',name:'Separate copy'});
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
});

test('saved theme names remain readable at the minimum desktop size and actions work by keyboard',async({page})=>{
  await page.setViewportSize({width:1020,height:700});
  await studio(page);
  const name='Long'.repeat(20);
  await page.evaluate(name=>window.patchTestState({localThemes:[{id:'long',name}]}),name);
  await page.locator('[data-page=home]').click();
  await page.locator('#home-saved').focus();await page.keyboard.press('Enter');
  const edit=page.getByRole('button',{name:'Edit '+name,exact:true});
  await edit.focus();
  expect(await page.locator('.editor-scroll').evaluate(el=>el.scrollWidth<=el.clientWidth+1)).toBe(true);
  await page.keyboard.press('Enter');
  await expect(page.getByLabel('Theme name',{exact:true})).toHaveValue(name);
  await page.locator('[data-page=home]').click();await page.locator('#home-saved').click();
  await page.locator('[data-duplicate-theme]').click();
  await expect(page.locator('#name-dialog-input')).toHaveValue(name.slice(0,75)+' copy');
});

test('busy operations disable name and language edits instead of silently dropping them',async({page})=>{
  await studio(page);
  await page.evaluate(()=>window.patchTestState({busy:true}));
  await expect(page.getByLabel('Theme name',{exact:true})).toBeDisabled();
  await expect(page.locator('#language')).toBeDisabled();
  await expect(page.locator('#guild')).toBeDisabled();
  await page.evaluate(()=>window.patchTestState({busy:false}));
  await expect(page.getByLabel('Theme name',{exact:true})).toBeEnabled();
});

test('editing starts in preview and Apply opens a confirmation without applying',async({page})=>{
  await studio(page);
  await expect(page.locator('#mode-badge')).toHaveText('Preview');
  await expect(page.locator('#restart-ready')).toHaveCount(0);
  await expect(page.locator('#apply')).toBeEnabled();
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.locator('#confirmation-theme')).toHaveText('Your community');
  await expect(page.locator('#confirmation-guild')).toHaveText('987654321012345678');
  await expect(page.locator('#confirmation-description')).toContainText('all listed servers and DM chats');
  await expect(page.locator('#confirmation-description')).toContainText('your other enabled themes stay active');
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
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
  await expect(page.getByRole('dialog')).toBeHidden();
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toBeHidden();
  await expect(page.getByLabel('Theme name',{exact:true})).toHaveValue('Your community');
  expect(await page.evaluate(()=>window.testMessages.filter(m=>m.action==='confirmOperation').length)).toBe(0);
});

test('Continue and apply confirms the reviewed operation once',async({page})=>{
  await studio(page);
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
  await page.getByRole('button',{name:'Continue & apply',exact:true}).click();
  await expect(page.getByRole('dialog')).toBeHidden();
  const confirmations=await page.evaluate(()=>window.testMessages.filter(m=>m.action==='confirmOperation'));
  expect(confirmations).toHaveLength(1);expect(confirmations[0].id).toMatch(/^confirmation-/);expect(confirmations[0].allowSetup).toBe(false);
});

test('first-time Vencord setup is offered inside the confirmation and requires consent',async({page})=>{
  await studio(page);await page.evaluate(()=>window.testSetupRequired=true);
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
  await expect(page.locator('#confirmation-install')).not.toBeChecked();
  await expect(page.getByRole('button',{name:'Continue & apply',exact:true})).toBeDisabled();
  await page.getByLabel('Install official Vencord for stable Discord',{exact:true}).check();
  await page.getByRole('button',{name:'Continue & apply',exact:true}).click();
  expect(await page.evaluate(()=>window.testMessages.find(m=>m.action==='confirmOperation').allowSetup)).toBe(true);
});

test('unsupported installation is explained inside confirmation and cannot continue',async({page})=>{
  await studio(page);await page.evaluate(()=>window.testConfirmationProblem='Install the standard Discord desktop client first.');
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
  await expect(page.locator('#confirmation-problem')).toContainText('standard Discord');
  await expect(page.getByRole('button',{name:'Continue & apply',exact:true})).toBeDisabled();
  await page.getByRole('button',{name:'Back to preview',exact:true}).click();
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
});

test('Recovery scopes and historical selection require reviewed confirmation',async({page})=>{
  await studio(page);await page.evaluate(()=>window.patchTestState({canDisable:true,canRestore:true,recoveryHistory:[{id:'abc',created:'2026-11-07T10:00:00Z',themes:['Personal theme']}]}));
  await page.getByRole('button',{name:'Recovery',exact:true}).click();await page.locator('#restore').click();
  await expect(page.locator('#reset-dialog')).toBeVisible();await page.locator('#reset-scope').selectOption('disableAll');
  await expect(page.locator('#reset-apply')).toBeEnabled();await page.locator('#reset-apply').click();
  await expect(page.locator('#reset-dialog')).toBeHidden();
  await page.locator('#restore-history').click();await expect(page.locator('#operation-confirmation')).toBeVisible();
  await page.getByRole('button',{name:'Continue & restore',exact:true}).click();
  const actions=await page.evaluate(()=>window.testMessages);expect(actions.filter(m=>m.action==='confirmOperation').length).toBe(2);expect(actions.find(m=>m.action==='restore').value).toBe('abc');
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
  await page.getByRole('button',{name:'Templates',exact:true}).click();
  await page.getByRole('button',{name:'Prime Build Dev',exact:true}).click();
  await expect(page.locator('#preview-loading')).toBeHidden();
  await expect(page.locator('[data-preset="primebuild-dev"]')).toHaveAttribute('aria-pressed','true');
  await expect(page.frameLocator('#preview').locator('.chatContent_f75fb0')).toHaveCSS('background-color','rgb(16, 19, 27)');
  await page.frameLocator('#preview').locator('body').evaluate(()=>window.setPreviewContext('other'));
  await expect(page.getByRole('status')).toContainText('Outside your target server');
  await expect(page.frameLocator('#preview').locator('.base__5e434')).toHaveCSS('background-color','rgb(49, 51, 56)');
  await page.frameLocator('#preview').locator('body').evaluate(()=>window.setPreviewContext('target'));
  await expect(page.getByRole('status')).toContainText('Theme active');
  await page.getByRole('button',{name:'Templates',exact:true}).click();
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
  await page.getByRole('tab',{name:'Style',exact:true}).click();
  await expect(page.getByRole('combobox',{name:'Display font'})).toBeVisible();
  await page.getByRole('tab',{name:'Artwork',exact:true}).click();
  await expect(page.getByRole('heading',{name:'Server banner',exact:true})).toBeVisible();
  expect(await page.locator('#banner-cards img').evaluateAll(images=>images.every(image=>image.complete&&image.naturalWidth>0))).toBe(true);
});

test('editing and save actions remain previews until the explicit application confirmation',async({page})=>{
  await studio(page);
  await page.getByLabel('Theme name',{exact:true}).fill('My custom name');
  await page.getByRole('button',{name:'Save theme',exact:true}).click();
  const actions=await page.evaluate(()=>window.testMessages.map(message=>message.action));
  expect(actions.lastIndexOf('edit')).toBeLessThan(actions.lastIndexOf('save'));
  expect(actions).not.toContain('confirmOperation');
  await page.getByRole('button',{name:'Review & apply',exact:true}).click();
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
  await expect(page.getByRole('button',{name:'Rivedi e applica',exact:true})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Il tuo server. Il tuo stile.'})).toBeVisible();
  await page.locator('#language').selectOption('es');
  await expect(page.getByRole('button',{name:'Revisar y aplicar',exact:true})).toBeVisible();
  await page.locator('#language').selectOption('ru');
  await expect(page.locator('html')).toHaveAttribute('lang','ru');
  await expect(page.getByRole('button',{name:'Проверить и применить',exact:true})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Ваш сервер. Ваш стиль.'})).toBeVisible();
  await page.locator('#language').selectOption('en');
  await expect(page.getByRole('button',{name:'Review & apply',exact:true})).toBeVisible();
  await expect(page.getByLabel('Server IDs or channel links')).toBeVisible();
});

test('community exposes four credited designs, catalog browsing and a working submission action',async({page})=>{
  await studio(page);await page.getByRole('button',{name:'Templates',exact:true}).click();
  await expect(page.locator('.community-card')).toHaveCount(4);
  await expect(page.getByRole('button',{name:'Zenit Gold — by JACKPOT71',exact:true})).toBeVisible();
  await page.getByRole('button',{name:'Browse BetterDiscord themes',exact:true}).click();
  await page.locator('[data-panel=community] [data-action=submitTheme]').click();
  await expect(page.locator('#info-dialog')).toBeVisible();await page.locator('#info-continue').click();
  const actions=await page.evaluate(()=>window.testMessages.map(m=>m.action));
  expect(actions).toContain('browseThemes');expect(actions).toContain('submitTheme');expect(actions).not.toContain('confirmOperation');
  await page.locator('.editor-scroll').evaluate(el=>el.scrollTop=0);await page.screenshot({path:'.local/studio-community-preview.png'});
});

test('downloaded CSS is reviewed before accepting, cancellation leaves the existing design untouched',async({page})=>{
  await studio(page);await page.getByRole('button',{name:'Templates',exact:true}).click();
  await page.locator('[data-page=editor]').click();await page.locator('[data-tab=library]').click();await page.locator('[data-panel=library] [data-action=open]').click();
  await expect(page.locator('#import-review')).toBeVisible();
  await expect(page.locator('#import-review-resources')).toContainText('Montserrat');
  await expect(page.locator('#import-review-status')).toContainText('Compatible');
  await page.keyboard.press('Escape');await expect(page.locator('#import-review')).toBeHidden();
  expect(await page.evaluate(()=>state.values.preset)).toBe('fluent');
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='acceptImport'||m.action==='confirmOperation'))).toBe(false);
});

test('accepted CSS previews the author design, edits supported variables, and shares without applying',async({page})=>{
  await studio(page);await page.getByRole('button',{name:'Templates',exact:true}).click();
  await page.locator('[data-page=editor]').click();await page.locator('[data-tab=library]').click();await page.locator('[data-panel=library] [data-action=open]').click();
  await page.getByRole('button',{name:'Use this design',exact:true}).click();
  await expect(page.locator('#preview-loading')).toBeHidden();
  expect(await page.frameLocator('#preview').locator('.base__5e434').evaluate(el=>getComputedStyle(el).getPropertyValue('--zg-gold').trim())).toBe('#e6c15e');
  await page.getByRole('tab',{name:'Style',exact:true}).click();
  await expect(page.locator('#builtin-style')).toBeHidden();
  await page.locator('[data-variable="--zg-gold"]').fill('#31c6b4');await page.locator('[data-variable="--zg-gold"]').dispatchEvent('change');
  await expect(page.locator('#preview-loading')).toBeHidden();
  await page.getByRole('tab',{name:'Files',exact:true}).click();
  await page.locator('#imported-details summary').click();
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
  await expect(page.getByRole('button',{name:'Prüfen und anwenden',exact:true})).toBeVisible();
  await page.locator('[data-page=recovery]').click();
  await expect(page.locator('#restore')).toHaveText('Discord zurücksetzen');
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  const apply=await page.locator('#apply').boundingBox();expect(apply.y+apply.height).toBeLessThanOrEqual(viewport.height);
  await page.screenshot({path:'.local/studio-german-'+viewport.width+'.png'});
});


test('refinement: one editor contains all editing tools and help stays embedded',async({page})=>{
  await studio(page);
  await expect(page.locator('.rail [data-page="editor"]')).toHaveCount(1);
  await expect(page.locator('.rail [data-page="style"],.rail [data-page="art"],.rail [data-page="library"]')).toHaveCount(0);
  await page.locator('[data-action="help"]').click();
  await expect(page.locator('#info-dialog')).toBeVisible();
});
test('refinement: restore offers both scopes before any mutation',async({page})=>{
  await studio(page);await page.locator('[data-page="recovery"]').click();
  await page.locator('#restore').click();
  await expect(page.locator('#reset-dialog')).toBeVisible();
  await expect(page.locator('#reset-scope option')).toHaveCount(2);
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
});
test('refinement: German preview translates illustrative content',async({page})=>{
  await studio(page);await page.locator('#language').selectOption('de');
  await expect(page.frameLocator('#preview').locator('.welcome-title')).toHaveText('Ein Ort, der sich nach dir anfühlt.');
  await expect(page.locator('[data-page="community"]')).toHaveText('Vorlagen');
});

test('font library offers fifty-plus searchable families without automatic downloads',async({page})=>{
  await studio(page);await page.locator('[data-tab=style]').click();
  expect(await page.locator('#font [data-catalog]').count()).toBeGreaterThanOrEqual(50);
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='downloadFont'))).toBe(false);
  await page.locator('#font-search').fill('Alata');
  expect(await page.locator('#font [data-catalog]').evaluateAll(options=>options.filter(o=>!o.hidden).map(o=>o.textContent))).toEqual(['Alata']);
  await page.locator('#font').selectOption('catalog:alata');
  expect(await page.evaluate(()=>window.testMessages.filter(m=>m.action==='downloadFont').map(m=>m.value))).toEqual(['alata']);
  await expect(page.locator('[data-action=importFont]')).toBeVisible();
  expect(await page.evaluate(()=>window.testMessages.some(m=>m.action==='confirmOperation'))).toBe(false);
});

test('local profiles and three-color palettes are named and available in Templates',async({page})=>{
 await studio(page);expect(await page.locator('.rail [data-page]').evaluateAll(items=>items.map(i=>i.dataset.page))).toEqual(['home','community','editor','recovery']);
 await page.locator('[data-page=community]').click();await expect(page.locator('[data-preset=fluent]')).toBeVisible();
 await page.locator('[data-page=editor]').click();await page.locator('#theme-name').fill('Violet work');await page.locator('.file-actions [data-action=save]').click();await page.locator('[data-page=community]').click();await page.locator('#template-tab-local').click();await expect(page.locator('#local-themes')).toContainText('Violet work');
 await page.locator('[data-page=editor]').click();await page.locator('[data-tab=style]').click();await expect(page.locator('#panel-picker')).toBeVisible();await page.locator('#palette-name').fill('My violet');await page.locator('#save-palette').click();await expect(page.locator('#saved-palettes')).toContainText('My violet');
 await page.locator('#surface').fill('#ffffff');await expect(page.locator('#surface-warning')).toBeVisible();await expect(page.locator('#surface')).toHaveAttribute('aria-invalid','true');await expect(page.locator('#surface-warning')).toHaveCSS('color','rgb(255, 156, 170)');
});
test('custom artwork gets a real thumbnail and can be saved and reused',async({page})=>{
 await studio(page);await page.evaluate(()=>window.patchTestState({assets:{banner:'banner.png',bannerHash:'first'},values:{...state.values,banner:'custom'}}));await page.locator('[data-tab=art]').click();
 const thumbnail=page.locator('[data-banner=custom] img');await expect(thumbnail).toBeVisible();expect(await thumbnail.evaluate(i=>i.complete&&i.naturalWidth>0)).toBe(true);
 await page.locator('#save-banner').click();await page.locator('#name-dialog-input').fill('Reusable art');await page.locator('#name-dialog-save').click();await expect(page.locator('#save-banner')).toBeDisabled();await expect(page.locator('#save-banner')).toContainText('✓');await page.locator('[data-images-kind=background]').click();await expect(page.locator('#saved-images')).toContainText('Reusable art');await page.locator('[data-saved-image]').click();
 expect(await page.evaluate(()=>window.testMessages.find(m=>m.action==='loadImage').kind)).toBe('background');await expect(page.locator('[data-background=custom] img')).toBeVisible();
});

test('Files has one save-as-new action and theme saves show a persistent check',async({page})=>{
 await studio(page);await page.locator('[data-tab=library]').click();
 await expect(page.locator('#panel-library [data-action=saveAsNew]')).toHaveCount(1);
 await page.locator('#panel-library [data-action=save]').click();
 await expect(page.locator('#panel-library [data-action=save]')).toBeDisabled();
 await expect(page.locator('#panel-library [data-action=save]')).toContainText('✓');
 await page.locator('#theme-name').fill('Changed name');
 await expect(page.locator('#panel-library [data-action=save]')).toBeEnabled();
});

test('palette saves acknowledge success and only enable when colors or name differ',async({page})=>{
 await studio(page);await page.locator('[data-tab=style]').click();
 await page.locator('#palette-name').fill('Saved violet');await page.locator('#save-palette').click();
 await expect(page.locator('#save-palette')).toBeDisabled();await expect(page.locator('#save-palette')).toContainText('✓');
 await page.locator('#accent').fill('#aa88ff');await expect(page.locator('#save-palette')).toBeEnabled();
 await page.locator('#accent').fill('#8791ff');await expect(page.locator('#save-palette')).toBeDisabled();
 await page.locator('#palette-name').fill('Renamed violet');await expect(page.locator('#save-palette')).toBeEnabled();
 await page.locator('#panel').fill('#15151b');await expect(page.locator('#palette-name')).toHaveValue('Renamed violet');
 await page.locator('#save-palette').click();await expect(page.locator('#save-palette')).toBeDisabled();
});
