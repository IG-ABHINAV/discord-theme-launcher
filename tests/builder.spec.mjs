import { test, expect } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import postcss from 'postcss';

const directory = new URL('../.local/builder-fixtures/', import.meta.url);
const target = '[data-list-item-id="guildsnav___987654321012345678"]';
const token = page => page.locator('.base__5e434').evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent').trim());
const discordClasses = JSON.parse(await readFile(new URL('../src/discord-classes.json', import.meta.url), 'utf8'));

test('personal custom background and typography support current Discord classes', async ({ page }) => {
  await page.goto(new URL('custom.html', directory).href);
  await page.evaluate(classes => {
    for (const el of document.querySelectorAll('[class]')) for (const name of [...el.classList]) {
      if (classes[name]) el.classList.replace(name, classes[name]);
    }
  }, discordClasses);
  expect(await page.locator('.base_pog62O').evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent').trim())).toBe('#48cfae');
  expect(await page.locator('.chatContent_z_gUBg').evaluate(el => getComputedStyle(el).backgroundImage)).toContain('linear-gradient');
  expect(await page.locator('.header_XH4OpX h2').evaluate(el => getComputedStyle(el).fontFamily)).toContain('PrimeBuild Personal Display');
  await page.getByRole('treeitem', { name:'DM', exact:true }).click();
  expect(await page.locator('.base_pog62O').evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent').trim())).toBe('');
});

test('preview explains outside-server styling and keeps the composer in view', async ({ page }) => {
  await page.setViewportSize({width: 880, height: 540});
  await page.goto(new URL('primebuild.html', directory).href);
  await expect(page.getByRole('status')).toContainText('Theme active', {timeout: 2000});
  await page.getByRole('treeitem', {name:'ALT',exact:true}).click();
  await expect(page.getByRole('status')).toContainText('Outside your target server', {timeout: 2000});
  expect(await token(page)).toBe('');
  const composer = await page.locator('.scrollableContainer__74017').boundingBox();
  expect(composer.y + composer.height).toBeLessThanOrEqual(540);
});

for (const preset of ['primebuild', 'primebuild-dev', 'fantafort', 'custom', 'native']) {
  test(`personal ${preset}: scoped generated CSS, offline assets and native chat typography`, async ({ page }) => {
    const requests = [];
    page.on('request', request => { if (/^https?:/.test(request.url())) requests.push(request.url()); });
    await page.goto(new URL(`${preset}.html`, directory).href);
    await page.evaluate(() => document.fonts.ready);
    expect(await token(page)).not.toBe('');
    const families = await page.evaluate(() => {
      const family = selector => getComputedStyle(document.querySelector(selector)).fontFamily;
      return { heading: family('.header_f37cb1 h2'), message: family('.markup__75297'), author: family('.author'), composer: family('input'), fonts: [...document.fonts].map(f => ({family:f.family,status:f.status})) };
    });
    if (preset !== 'native') {
      expect(families.heading).toContain('PrimeBuild Personal Display');
      expect(families.fonts.some(f => f.family.includes('PrimeBuild Personal Display') && f.status === 'loaded')).toBe(true);
    }
    for (const family of [families.message, families.author, families.composer]) expect(family).not.toContain('Personal Display');
    await page.getByRole('treeitem', { name: 'ALT', exact: true }).click();
    expect(await token(page)).toBe('');
    await expect(page.locator('.header_f37cb1 h2')).not.toHaveCSS('font-family', /Personal Display/);
    await page.getByRole('treeitem', { name: 'DM', exact: true }).click();
    expect(await token(page)).toBe('');
    await page.locator(target).click();
    expect(await token(page)).not.toBe('');
    await page.getByRole('button', { name: 'Global settings', exact: true }).click();
    expect(await page.locator('#global-settings').evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent'))).toBe('');
    await page.getByRole('button', { name: 'Disable theme', exact: true }).click();
    expect(await token(page)).toBe('');
    expect(requests).toEqual([]);
    const html = await readFile(new URL(`${preset}.html`, directory), 'utf8');
    const css = html.match(/<style id="personal-theme">([\s\S]*?)<\/style>/)[1];
    const root = postcss.parse(css);
    // Root rules carry the exact :has() target guard; everything else sits behind its inherited style gate.
    expect(root.nodes.filter(n => n.type === 'atrule' && n.name === 'container' && /^style\(--pb-gate-[0-9a-f]{12}: 1\)$/.test(n.params))).toHaveLength(1);
    for (const node of root.nodes.filter(n => n.type === 'rule')) expect(node.selector).toContain('guildsnav___');
    for (const node of root.nodes.filter(n => n.type === 'atrule')) expect(['container','font-face']).toContain(node.name);
    expect(css).not.toMatch(/__[A-Z_]+__|@import|url\(["']?https?:/);
  });
}

test('personal theme fails closed for hidden rails, ambiguous selection and changed Discord structure', async ({ page }) => {
  const url = new URL('custom.html', directory).href;
  await page.goto(url);
  await page.locator('.guilds__5e434').evaluate(el => el.setAttribute('aria-hidden','true'));
  expect(await token(page)).toBe('');
  await page.goto(url);
  await page.getByRole('treeitem', { name:'ALT', exact:true }).evaluate(el => el.setAttribute('aria-selected','true'));
  expect(await token(page)).toBe('');
  await page.goto(url);
  await page.locator('.content__5e434').evaluate(el => el.className = 'unsupported-layout');
  expect(await token(page)).toBe('');
});

test('custom image uses a strong scrim, readable messages and a solid composer', async ({ page }) => {
  await page.goto(new URL('custom.html', directory).href);
  const styles = await page.evaluate(() => {
    const style = selector => getComputedStyle(document.querySelector(selector));
    return { image:style('.chatContent_f75fb0').backgroundImage, text:style('.markup__75297').color, surface:style('.scrollableContainer__74017').backgroundColor, border:style('.scrollableContainer__74017').borderTopColor, font:style('.markup__75297').fontFamily };
  });
  expect(styles.image).toContain('linear-gradient');
  expect(Number(styles.image.match(/rgba\([^)]*, ([\d.]+)\)/)[1])).toBeGreaterThanOrEqual(.92);
  const lum = rgb => rgb.match(/[\d.]+/g).slice(0,3).map(Number).map(c=>c/255).map(c=>c<=.04045?c/12.92:((c+.055)/1.055)**2.4).reduce((s,c,i)=>s+c*[.2126,.7152,.0722][i],0);
  const contrast = (a,b) => { const [high,low]=[lum(a),lum(b)].sort((a,b)=>b-a); return (high+.05)/(low+.05); };
  expect(contrast(styles.text, styles.surface)).toBeGreaterThanOrEqual(4.5);
  expect(contrast(styles.border, styles.surface)).toBeGreaterThanOrEqual(3);
  await page.screenshot({path:'.local/custom-builder-preview.png',fullPage:true});
});

test('configuration labels and license text cannot inject preview markup', async ({ page }) => {
  const dialogs=[]; page.on('dialog',async dialog=>{dialogs.push(dialog.message());await dialog.dismiss();});
  await page.goto(new URL('escaped.html',directory).href);
  expect(await page.locator('#server-name').textContent()).toContain('</style><script>');
  expect(await token(page)).toBe('#48cfae');
  expect(dialogs).toEqual([]);
});


test('multi-target theme styles selected servers and only the chosen DM conversation',async({page})=>{
  await page.goto(new URL('../.local/builder-fixtures/multi-target.html',import.meta.url).href);
  const root=page.locator('.base__5e434');const token=()=>root.evaluate(el=>getComputedStyle(el).getPropertyValue('--pb-profile').trim());
  await expect.poll(token).toBe('"personal"');
  await page.evaluate(()=>{const item=document.querySelector('[data-list-item-id=\"guildsnav___other\"]');item.dataset.listItemId='guildsnav___1206415718728601600';document.querySelectorAll('[aria-selected]').forEach(el=>el.setAttribute('aria-selected','false'));item.setAttribute('aria-selected','true')});await expect.poll(token).toBe('\"personal\"');
  await page.evaluate(()=>window.setPreviewContext('dm'));
  await expect(page.locator('.dm-navigation')).toBeVisible();await expect(page.locator('.server-navigation')).toBeHidden();
  await expect(page.locator('#conversation-title')).toHaveText('@ Alex');await expect.poll(token).toBe('"personal"');
  await page.evaluate(()=>window.setPreviewContext('dm-other'));await expect.poll(token).toBe('');
  await page.evaluate(()=>window.setPreviewContext('dm'));await expect.poll(token).toBe('"personal"');
  await page.locator('[data-fixture-message]').evaluateAll(items=>items.forEach(item=>item.remove()));await expect.poll(token).toBe('');
  await page.evaluate(()=>{const decoy=document.createElement('div');decoy.id='chat-messages-876543210123456789-decoy';document.body.append(decoy)});await expect.poll(token).toBe('');
});

test('DM-only preview starts with an actual private conversation',async({page})=>{
 await page.goto(new URL('../.local/builder-fixtures/dm-only.html',import.meta.url).href);await expect(page.locator('body')).toHaveAttribute('data-context','dm');await expect(page.locator('.dm-navigation')).toBeVisible();await expect(page.locator('.server-navigation')).toBeHidden();expect(await page.locator('.base__5e434').evaluate(el=>getComputedStyle(el).getPropertyValue('--pb-profile').trim())).toBe('"personal"');
});
test('imported themes can use reusable local artwork inside the selected scope',async({page})=>{
 await page.goto(new URL('../.local/builder-fixtures/zenit-artwork.html',import.meta.url).href);await expect(page.locator('.chatContent_f75fb0')).toHaveCSS('background-image',/data:image\/png/);await page.evaluate(()=>setPreviewContext('other'));await expect(page.locator('.chatContent_f75fb0')).not.toHaveCSS('background-image',/data:image\/png/);
});
