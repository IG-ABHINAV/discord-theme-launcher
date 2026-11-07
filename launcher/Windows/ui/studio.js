'use strict';
const $=selector=>document.querySelector(selector);
const bridge=window.chrome?.webview;
const send=(action,data={})=>bridge?.postMessage({action,...data});
let state=null,editTimer=0,activePreview='',currentContext='target',contextActive=false,contextKnown=false,currentPage='server',lang='';
let presets=[],catalogSignature='',variableSignature='';
const palettes=[['primebuild','palette.primebuild',['orange','coal','raised']],['primebuild-dev','palette.dev',['orange','blue','cyan']],['fantafort','palette.fantafort',['yellow','violet','cyan']],['blurple','palette.blurple',['brand','darkblue','light']],['custom','palette.custom',['custom']]];
const pages={community:['page.community.title','page.community.sub'],library:['page.library.title','page.library.sub'],server:['page.server.title','page.server.sub'],style:['page.style.title','page.style.sub'],art:['page.art.title','page.art.sub'],recovery:['page.recovery.title','page.recovery.sub']};
function t(key,vars){let value=(I18N[lang]||I18N.en)[key]??I18N.en[key]??key;if(vars)for(const name in vars)value=value.replaceAll('{'+name+'}',vars[name]);return value}
function icon(name){const svg=document.createElementNS('http://www.w3.org/2000/svg','svg');const use=document.createElementNS(svg.namespaceURI,'use');use.setAttribute('href','#i-'+name);svg.append(use);svg.setAttribute('aria-hidden','true');return svg}
function button(label,className){const el=document.createElement('button');el.type='button';el.className=className;el.setAttribute('aria-label',label);return el}
function showPage(id){currentPage=id;$('#section-title').textContent=t(pages[id][0]);$('#section-subtitle').textContent=t(pages[id][1])}
function applyLanguage(code){if(!I18N[code])code='en';if(lang===code)return;lang=code;document.documentElement.lang=code;document.title=t('docTitle');const select=$('#language');if(select.value!==code)select.value=code;document.querySelectorAll('[data-i18n]').forEach(el=>{el.textContent=t(el.dataset.i18n)});document.querySelectorAll('[data-i18n-title]').forEach(el=>{el.title=t(el.dataset.i18nTitle)});document.querySelectorAll('[data-i18n-aria]').forEach(el=>{el.setAttribute('aria-label',t(el.dataset.i18nAria))});document.querySelectorAll('[data-i18n-placeholder]').forEach(el=>{el.placeholder=t(el.dataset.i18nPlaceholder)});showPage(currentPage);if(contextKnown)updateContext(currentContext,contextActive)}
function renderCatalog(designs){
 const signature=JSON.stringify(designs);if(signature===catalogSignature)return;catalogSignature=signature;
 presets=designs.filter(d=>d.adapter==='preset').map(d=>[d.id,d.name]);$('#preset-cards').replaceChildren();
 for(const design of designs){const card=document.createElement('article');card.className='community-card';
 const el=button(design.name,'preset-card');el.dataset.preset=design.id;const img=new Image();img.src='/art/'+design.id+'/header';img.alt='';
 const text=document.createElement('div'),name=document.createElement('strong'),caption=document.createElement('span');name.textContent=design.name;caption.textContent=design.author+' · '+design.license+' · '+design.version;text.append(name,caption);el.append(img,text);
 el.onclick=()=>{flushEdits();send('community',{value:design.id});document.querySelector('[data-page=server]').click()};
 const links=document.createElement('div');links.className='credit-links';for(const [action,key] of [['communityAuthor','author'],['communitySource','source']]){const link=button(t(key),'text-button');link.dataset.i18n=key;link.textContent=t(key);link.onclick=()=>send(action,{value:design.id});links.append(link)}
 if(design.adaptation){const note=document.createElement('p');note.className='hint';note.dataset.i18n='serverAdaptation';note.textContent=t('serverAdaptation');card.append(note)}card.prepend(el);card.append(links);$('#preset-cards').append(card)}
 artworkCards('banner',state?.assets?.banner);artworkCards('background',state?.assets?.background);
}
for(const[id,key,colors]of palettes){const el=button(t(key),'palette-card');el.dataset.palette=id;el.dataset.i18nAria=key;const label=document.createElement('span');label.dataset.i18n=key;label.textContent=t(key);const swatches=document.createElement('span');swatches.className='swatches';for(const color of colors){const dot=document.createElement('i');dot.className='swatch-'+color;swatches.append(dot)}el.append(label,swatches);el.onclick=()=>changeChoice('palette',id);$('#palette-cards').append(el)}
function artworkCards(kind,customName){const parent=$('#'+kind+'-cards');parent.replaceChildren();for(const[id,title,key]of[...presets.map(([preset,name])=>[preset,name,'']),['none',t('art.none'),'art.none'],...(customName?[['custom',t('art.custom'),'art.custom']]:[])]){const el=button(title,'art-card');el.dataset[kind]=id;if(key)el.dataset.i18nAria=key;if(id!=='none'&&id!=='custom'){const img=new Image();img.src='/art/'+id+'/'+(kind==='banner'?'header':'chat');img.alt='';el.append(img)}else{const empty=document.createElement('div');empty.className='empty-art';empty.append(icon(id==='none'?'image':'folder'));el.append(empty)}const label=document.createElement('span');if(key)label.dataset.i18n=key;label.textContent=title;el.append(label);el.onclick=()=>changeChoice(kind,id);parent.append(el)}}
artworkCards('banner');artworkCards('background');
function readEdits(){const values={...state?.values};document.querySelectorAll('[data-field]').forEach(input=>values[input.dataset.field]=input.value);return values}
function flushEdits(){if(editTimer){clearTimeout(editTimer);editTimer=0;send('edit',{values:readEdits()})}}
function changeChoice(field,value){flushEdits();const values=readEdits();values[field]=value;send('edit',{values});loading()}
function loading(){if(state?.validation&&!state?.previewUri)return;$('#preview-loading').hidden=false;$('#preview-error').hidden=true;$('#apply').disabled=true}
document.querySelectorAll('[data-field]').forEach(input=>input.addEventListener(input.tagName==='SELECT'?'change':'input',()=>{clearTimeout(editTimer);editTimer=setTimeout(()=>{editTimer=0;send('edit',{values:readEdits()})},180);loading()}));
for(const color of ['accent','surface'])$('#'+color+'-picker').addEventListener('input',event=>{const input=$('#'+color);input.value=event.target.value;input.dispatchEvent(new Event('input'))});
document.querySelectorAll('[data-page]').forEach(item=>item.addEventListener('click',()=>{document.querySelectorAll('[data-page]').forEach(tab=>{const selected=item===tab;tab.classList.toggle('selected',selected);if(selected)tab.setAttribute('aria-current','page');else tab.removeAttribute('aria-current')});document.querySelectorAll('[data-panel]').forEach(panel=>panel.hidden=panel.dataset.panel!==item.dataset.page);showPage(item.dataset.page);$('.editor-scroll').scrollTop=0}));
document.querySelectorAll('[data-action]').forEach(item=>item.addEventListener('click',()=>{flushEdits();send(item.dataset.action)}));
$('#language').addEventListener('change',()=>{applyLanguage($('#language').value);if(state)renderCopy();send('language',{value:lang})});
function updateContext(context,active){currentContext=context;contextActive=active;contextKnown=true;document.querySelectorAll('[data-context]').forEach(item=>{const selected=item.dataset.context===context;item.classList.toggle('active',selected);item.setAttribute('aria-pressed',String(selected))});const status=$('#preview-status');status.replaceChildren();const dot=document.createElement('span');dot.className='tiny-dot';status.append(dot,document.createTextNode(active?(state?.values.guild?t('statusActive'):t('statusSample')):context==='target'?t('statusDisabled'):t('statusOutside')));status.dataset.outside=String(!active)}
document.querySelectorAll('[data-context]').forEach(item=>item.addEventListener('click',()=>{if(!$('#preview').contentWindow?.setPreviewContext)return;$('#preview').contentWindow.setPreviewContext(item.dataset.context)}));
window.addEventListener('message',event=>{if(event.source!==$('#preview').contentWindow||event.data?.type!=='preview-context')return;if(!['target','other','dm'].includes(event.data.context)||typeof event.data.active!=='boolean')return;updateContext(event.data.context,event.data.active)});
async function previewLoaded(event){
 const frame=event.currentTarget;if(frame!==document.getElementById('preview'))return;const doc=frame.contentDocument,id=doc?new URL(doc.URL).searchParams.get('revision'):null;
 if(!doc){if(state&&activePreview===frame.src){send('previewReady',{id:state.previewId,ready:false})}return}if(id!==state?.previewId)return;
 try{await doc.fonts.ready;await previewImages(doc,frame.contentWindow);if(id!==state?.previewId||frame.contentDocument!==doc)return;const target=doc.querySelector('.base__5e434');const themed=target&&frame.contentWindow.getComputedStyle(target).getPropertyValue('--pb-profile').trim()==='"personal"';const fontReady=Boolean(state.imported)||state.values.font==='native'||[...doc.fonts].some(font=>font.family.includes('PrimeBuild Personal Display')&&font.status==='loaded');const ready=Boolean(themed&&fontReady);$('#preview-loading').hidden=true;$('#preview-error').hidden=ready;if(!ready)$('#error-message').textContent=t('loadError');send('previewReady',{id,ready});if(ready)updateContext('target',true)}catch{send('previewReady',{id,ready:false})}
}
async function previewImages(doc,view){
 const urls=new Set();function scan(rules){for(const rule of rules){if(rule.style)for(const key of rule.style)for(const match of rule.style.getPropertyValue(key).matchAll(/url\(\s*["']?(data:image\/[a-z+]+;base64,[a-z0-9+/=]+)["']?\s*\)/gi))urls.add(match[1]);if(rule.cssRules)scan(rule.cssRules)}}
 for(const sheet of doc.styleSheets)scan(sheet.cssRules);
 await Promise.all([...urls].map(async uri=>{const img=new view.Image();img.src=uri;await img.decode()}));
}
function navigatePreview(uri){const previous=document.getElementById('preview'),frame=previous.cloneNode(false);frame.addEventListener('load',previewLoaded);frame.src=uri;previous.replaceWith(frame)}
function renderCopy(){const next=state;if(!next)return;$('#mode-badge').textContent=next.busy?t('modeApplying'):t('modePreview');$('#notice').textContent=next.notice;$('#document-status').textContent=next.dirty?t('statusUnsaved'):next.saved?t('statusSaved'):t('statusNew');$('#preview-description').textContent=next.values.name||t('previewFallback');$('#font option[value=custom]').textContent=next.assets.font?t('font.named',{name:next.assets.font}):t('font.custom');$('#apply').title=next.applyReason||t('applyTitle');$('#apply-help').textContent=next.applyActionLabel||t('reviewSetup');for(const key of['banner','background']){const label=next.assets[key]||'';if($('#'+key+'-cards').dataset.custom!==label){artworkCards(key,label);$('#'+key+'-cards').dataset.custom=label}document.querySelectorAll('[data-'+key+']').forEach(el=>{const selected=el.dataset[key]===next.values[key];el.classList.toggle('selected',selected);el.setAttribute('aria-pressed',String(selected))});$('#'+key+'-file').textContent=label?t('importedFile',{name:label}):''}renderConfirmation(next.confirmation)}
function update(next){state=next;renderCatalog(next.designs||[]);renderImported(next.imported);renderImportReview(next.importReview);if(next.language)applyLanguage(next.language);document.body.dataset.busy=String(next.busy);document.querySelector('.editor-scroll').inert=next.busy;renderCopy();for(const input of document.querySelectorAll('[data-field]'))if(document.activeElement!==input)input.value=next.values[input.dataset.field]??'';for(const key of['accent','surface'])if(/^#[a-fA-F0-9]{6}$/.test(next.values[key]))$('#'+key+'-picker').value=next.values[key];document.querySelectorAll('[data-preset]').forEach(el=>{const selected=el.dataset.preset===(next.imported?next.designs?.find(d=>d.sha256===next.imported.sha256)?.id:next.values.preset);el.classList.toggle('selected',selected);el.setAttribute('aria-pressed',String(selected))});document.querySelectorAll('[data-palette]').forEach(el=>{const selected=el.dataset.palette===next.values.palette;el.classList.toggle('selected',selected);el.setAttribute('aria-pressed',String(selected))});$('#custom-colors').hidden=next.values.palette!=='custom';$('#apply').disabled=!next.canApply;$('#apply-reason').textContent=next.applyReason||'';$('#apply-help').hidden=!next.applyAction;$('#apply-help').disabled=next.busy;$('#disable').disabled=!next.canDisable;$('#restore').disabled=!next.canRestore;$('#installation-state').textContent=next.installation;document.querySelectorAll('[data-action=new],[data-action=open],[data-action=save],[data-action=share]').forEach(el=>el.disabled=next.busy);$('#preview-error').hidden=!next.validation||Boolean(next.previewUri);if(next.validation&&!next.previewUri){$('#preview-loading').hidden=true;$('#error-message').textContent=next.validation}else if(next.previewUri&&activePreview!==next.previewUri){activePreview=next.previewUri;loading();navigatePreview(next.previewUri)}}
bridge?.addEventListener('message',event=>{if(event.data?.type==='state')update(event.data)});
applyLanguage('en');
send('loaded');

document.getElementById('apply-help').addEventListener('click',()=>{if(state?.applyAction==='recovery')document.querySelector('[data-page=recovery]').click();else if(state?.applyAction==='refresh'){flushEdits();send('refresh')}});

let reviewedConfirmation='';
function renderConfirmation(value){
 const dialog=document.getElementById('operation-confirmation');
 if(!value){reviewedConfirmation='';if(dialog.open)dialog.close();return}
 const fresh=reviewedConfirmation!==value.id;reviewedConfirmation=value.id;
 const applying=value.operation==='apply',disabling=value.operation==='disable';
 document.getElementById('confirmation-title').textContent=applying?t('confirmApplyTitle'):disabling?t('confirmDisableTitle'):t('confirmRestoreTitle');
 document.getElementById('confirmation-eyebrow').textContent=applying?t('eyebrowApply'):t('eyebrowReview');
 document.getElementById('confirmation-description').textContent=applying?t('confirmApplyBody'):disabling?t('confirmDisableBody'):t('confirmRestoreBody');
 document.getElementById('confirmation-target').hidden=!applying;
 document.getElementById('confirmation-theme').textContent=value.themeName||'';
 document.getElementById('confirmation-server').textContent=value.serverName||t('selectedServer');
 document.getElementById('confirmation-guild').textContent=value.guild||'';
 document.getElementById('confirmation-setup').hidden=!value.requiresSetup||Boolean(value.problem);
 document.getElementById('confirmation-install-label').textContent=value.repair?t('repairVencord'):t('installVencord');
 document.getElementById('confirmation-problem').hidden=!value.problem;
 document.getElementById('confirmation-problem').textContent=value.problem||'';
 if(fresh)document.getElementById('confirmation-install').checked=false;
 document.getElementById('confirmation-continue').textContent=applying?t('continueApply'):disabling?t('continueDisable'):t('continueRestore');
 document.getElementById('confirmation-continue').disabled=Boolean(value.problem)||Boolean(value.requiresSetup&&!document.getElementById('confirmation-install').checked)||Boolean(state?.busy);
 if(!dialog.open){dialog.showModal();document.getElementById('confirmation-cancel').focus()}
}
function cancelConfirmation(){if(reviewedConfirmation)send('cancelConfirmation',{id:reviewedConfirmation})}
document.getElementById('operation-confirmation').addEventListener('cancel',event=>{event.preventDefault();cancelConfirmation()});
document.getElementById('confirmation-cancel').addEventListener('click',cancelConfirmation);
document.getElementById('confirmation-install').addEventListener('change',()=>renderConfirmation(state?.confirmation));
document.getElementById('confirmation-continue').addEventListener('click',()=>{const value=state?.confirmation;if(!value||document.getElementById('confirmation-continue').disabled)return;document.getElementById('confirmation-continue').disabled=true;send('confirmOperation',{id:value.id,allowSetup:Boolean(value.requiresSetup&&document.getElementById('confirmation-install').checked)})});

function renderImported(imported){
 $('#builtin-style').hidden=Boolean(imported);$('#builtin-art').hidden=Boolean(imported);$('#import-art-note').hidden=!imported;$('#import-variables').hidden=!imported;$('#imported-details').hidden=!imported;
 if(!imported){variableSignature='';return}
 $('#imported-credit').textContent=imported.name+' · '+imported.author+' · '+imported.source;$('#imported-hash').textContent='SHA-256 '+imported.sha256;$('#imported-notes').textContent=imported.notes.join(' ');$('#imported-license').textContent=imported.license||t('licenseUnknown');
 const signature=imported.sha256+imported.variables.map(v=>v.name).join();
 if(variableSignature!==signature){variableSignature=signature;$('#variable-fields').replaceChildren();for(const variable of imported.variables){const label=document.createElement('label');label.className='field-label';label.textContent=variable.name;const input=document.createElement('input');input.dataset.variable=variable.name;input.type=variable.kind==='color'?'color':'number';if(variable.kind==='number'){input.min='0';input.max='999.999';input.step='0.001'}input.addEventListener('change',()=>{flushEdits();send('importVariable',{name:variable.name,value:input.value});loading()});label.append(input);$('#variable-fields').append(label)}}
 for(const variable of imported.variables){const input=[...document.querySelectorAll('[data-variable]')].find(el=>el.dataset.variable===variable.name);if(input&&document.activeElement!==input)input.value=variable.value}
}
function renderImportReview(review){const dialog=$('#import-review');if(!review){if(dialog.open)dialog.close();return}$('#import-review-title').textContent=review.name||t('unsupportedImport');$('#import-review-credit').textContent=review.author||'';$('#import-review-status').textContent=review.supported?t('supportedImport'):t('unsupportedImport');$('#import-review-notes').textContent=review.notes.join(' ');$('#import-review-resources').replaceChildren();for(const resource of review.resources.length?review.resources:[t('noExternalResources')]){const item=document.createElement('li');item.textContent=resource;$('#import-review-resources').append(item)}$('#import-accept').disabled=!review.supported;if(!dialog.open){dialog.showModal();$('#import-cancel').focus()}}
$('#import-cancel').onclick=()=>send('cancelImport');$('#import-review').addEventListener('cancel',event=>{event.preventDefault();send('cancelImport')});$('#import-accept').onclick=()=>{if(!$('#import-accept').disabled){send('acceptImport');document.querySelector('[data-page=server]').click()}};
