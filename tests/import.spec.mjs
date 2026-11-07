import { test, expect } from '@playwright/test';

const directory=new URL('../.local/builder-fixtures/',import.meta.url);
for(const [design,variable,value] of [['zenit-gold','--zg-gold','#e6c15e'],['dracula','--dracula-purple','#bd93f9']]){
  test(`${design}: original imported appearance, offline assets, and strict server isolation`,async({page})=>{
    const network=[];page.on('request',r=>{if(/^https?:/.test(r.url()))network.push(r.url())});
    await page.goto(new URL(design+'.html',directory).href);await page.evaluate(()=>document.fonts.ready);
    const root=page.locator('.base__5e434');
    expect(await root.evaluate((el,key)=>getComputedStyle(el).getPropertyValue(key).trim(),variable)).toBe(value);
    if(design==='zenit-gold'){
      expect(await root.evaluate(el=>getComputedStyle(el,'::before').backgroundImage)).toContain('data:image/webp;base64,');
      expect(await root.evaluate(el=>Number(getComputedStyle(el,'::before').zIndex))).toBeLessThan(0);
      expect(await root.evaluate(el=>getComputedStyle(el,'::after').animationName)).toMatch(/^ts-[a-f0-9]+-zg-sheen$/);
      expect(await page.evaluate(()=>[...document.fonts].some(f=>f.family.includes('Montserrat')&&f.status==='loaded'))).toBe(true);
      await expect(page.locator('.modeSelected__2ea32')).toHaveCSS('box-shadow',/230, 193, 94/);
    }else{
      await expect(root).toHaveCSS('background-color','rgb(40, 42, 54)');
    }
    for(const context of ['other','dm']){
      await page.evaluate(context=>window.setPreviewContext(context),context);
      expect(await root.evaluate((el,key)=>getComputedStyle(el).getPropertyValue(key).trim(),variable)).toBe('');
      await expect(root).toHaveCSS('background-color','rgb(49, 51, 56)');
    }
    await page.evaluate(()=>window.setPreviewContext('target'));
    await page.getByRole('button',{name:'Global settings',exact:true}).click();
    expect(await page.locator('#global-settings').evaluate((el,key)=>getComputedStyle(el).getPropertyValue(key).trim(),variable)).toBe('');
    expect(await page.locator('#global-settings').evaluate(el=>{const r=el.getBoundingClientRect();return Boolean(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2)?.closest('#global-settings'))})).toBe(true);
    await page.getByRole('button',{name:'Global settings',exact:true}).click();
    for(const failure of ['hidden','ambiguous','missing']){
      await page.reload();
      await page.evaluate(failure=>{
        const rail=document.querySelector('.guilds__5e434');
        if(failure==='hidden')rail.hidden=true;
        if(failure==='ambiguous')document.querySelector('[data-list-item-id="guildsnav___other"]').setAttribute('aria-selected','true');
        if(failure==='missing')rail.remove();
      },failure);
      expect(await root.evaluate((el,key)=>getComputedStyle(el).getPropertyValue(key).trim(),variable)).toBe('');
    }
    expect(network).toEqual([]);
    await page.goto(new URL(design+'.html',directory).href);await page.evaluate(()=>document.fonts.ready);
    await page.screenshot({path:'.local/'+design+'-import-preview.png'});
  });
}
test('supported imported variable edits change the author design without replacing artwork',async({page})=>{
  await page.goto(new URL('zenit-edited.html',directory).href);
  expect(await page.locator('.base__5e434').evaluate(el=>getComputedStyle(el).getPropertyValue('--zg-gold').trim())).toBe('#31c6b4');
  await expect(page.locator('.modeSelected__2ea32')).toHaveCSS('box-shadow',/49, 198, 180/);
  expect(await page.locator('.base__5e434').evaluate(el=>getComputedStyle(el,'::before').backgroundImage)).toContain('data:image/webp;base64,');
});
