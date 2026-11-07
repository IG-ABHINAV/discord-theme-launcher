import { test, expect } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const catalog = JSON.parse(await readFile(new URL('../catalog.json', import.meta.url), 'utf8'));
const fixture = new URL('./fixture.html', import.meta.url).href;
const target = '[data-list-item-id="guildsnav___1206415718728601600"]';
const theme = fileURLToPath(new URL('../PrimeBuild.theme.css', import.meta.url));
const discordClasses = JSON.parse(await readFile(new URL('../src/discord-classes.json', import.meta.url), 'utf8'));
const token = page => page.locator('.base__5e434').evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent').trim());

test.beforeEach(async ({ page }) => { await page.goto(fixture); });

for (const profile of catalog.themes) test(`${profile.server}: November Discord classes apply and remain isolated`, async ({ page }) => {
  await page.locator('#theme').evaluate((el, file) => el.href = '../' + file, profile.file);
  await page.locator(`[data-list-item-id="guildsnav___${profile.guild}"]`).click();
  await page.evaluate(classes => {
    for (const el of document.querySelectorAll('[class]')) for (const name of [...el.classList]) {
      if (classes[name]) el.classList.replace(name, classes[name]);
    }
  }, discordClasses);
  const base = page.locator('.base_pog62O');
  const accent = () => base.evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent').trim());
  await expect.poll(accent).toBe(profile.accent);
  const rgb = hex => `rgb(${[1,3,5].map(i => parseInt(hex.slice(i,i+2),16)).join(', ')})`;
  await expect(page.locator('.chatContent_z_gUBg')).toHaveCSS('background-color', rgb(profile.chat));
  expect(await page.locator('.sidebarList_pog62O').evaluate(el => getComputedStyle(el).backgroundImage)).toContain('data:image/svg+xml');
  await expect(page.locator('.scrollableContainer_2x8-Qn')).toHaveCSS('background-color', rgb(profile.panel));
  await page.locator('[data-list-item-id="guildsnav___other"]').click();
  await expect.poll(accent).toBe('');
  await page.locator('[data-list-item-id="guildsnav___home"]').click();
  await expect.poll(accent).toBe('');
  await page.locator(`[data-list-item-id="guildsnav___${profile.guild}"]`).click();
  await expect.poll(accent).toBe(profile.accent);
  await page.locator('nav.guilds_pog62O').evaluate(el => el.hidden = true);
  await expect.poll(accent).toBe('');
});

test('activates in Prime Build, switches off in another server and DMs, and returns', async ({ page }) => {
  await expect.poll(() => token(page)).toBe('#ff6600');
  await page.locator('[data-list-item-id="guildsnav___other"]').click();
  await expect.poll(() => token(page)).toBe('');
  await expect(page.locator('.base__5e434')).toHaveCSS('background-color', 'rgb(49, 51, 56)');
  await page.locator('[data-list-item-id="guildsnav___home"]').click();
  await expect.poll(() => token(page)).toBe('');
  await page.locator(target).click();
  await expect.poll(() => token(page)).toBe('#ff6600');
});

test('an unselected icon and a target channel link inside a message do not activate it', async ({ page }) => {
  await page.locator('[data-list-item-id="guildsnav___home"]').click();
  await page.locator('.scroller__36d07').evaluate(el => {
    const link = document.createElement('a');
    link.href = '/channels/1206415718728601600/123';
    link.dataset.listItemId = 'guildsnav___1206415718728601600';
    link.setAttribute('aria-selected', 'true');
    link.textContent = 'An ordinary message link';
    el.append(link);
  });
  await expect.poll(() => token(page)).toBe('');
});

test('missing selection, changed layout, and missing server all fail closed', async ({ page }) => {
  await page.locator(target).evaluate(el => el.removeAttribute('aria-selected'));
  await expect.poll(() => token(page)).toBe('');
  await page.reload();
  await page.locator('.base__5e434').evaluate(el => el.className = 'base_afterDiscordUpdate');
  await expect(page.locator('.base_afterDiscordUpdate')).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)');
  await page.reload();
  await page.locator(target).evaluate(el => el.remove());
  await expect.poll(() => token(page)).toBe('');
});

test('portaled global settings and semantic role/status colors are preserved', async ({ page }) => {
  await page.getByRole('button', { name: 'Global settings', exact: true }).click();
  await expect(page.locator('#global-settings')).toHaveCSS('background-color', 'rgb(49, 51, 56)');
  await expect(page.locator('#global-settings')).toHaveCSS('color', 'rgb(238, 238, 242)');
  expect(await page.locator('#global-settings').evaluate(el => getComputedStyle(el).getPropertyValue('--pb-accent'))).toBe('');
  await expect(page.locator('.author.role-color')).toHaveCSS('color', 'rgb(119, 185, 250)');
  await expect(page.locator('.presence')).toHaveCSS('color', 'rgb(118, 211, 159)');
  await expect(page.locator('.danger')).toHaveCSS('color', 'rgb(243, 123, 131)');
});

test('a hidden server rail or ambiguous selection fails closed', async ({ page }) => {
  await page.locator('nav.guilds__5e434').evaluate(el => el.hidden = true);
  await expect.poll(() => token(page)).toBe('');
  await page.reload();
  await page.locator('nav.guilds__5e434').evaluate(el => el.setAttribute('aria-hidden', 'true'));
  await expect.poll(() => token(page)).toBe('');
  await page.reload();
  await page.locator('[data-list-item-id="guildsnav___other"]').evaluate(el => el.setAttribute('aria-selected', 'true'));
  await expect.poll(() => token(page)).toBe('');
});

test('normal text and input boundaries meet the contrast targets', async ({ page }) => {
  const colors = await page.locator('.scrollableContainer__74017').evaluate(el => {
    const style = getComputedStyle(el);
    const content = getComputedStyle(document.querySelector('.markup__75297'));
    const chat = getComputedStyle(document.querySelector('.chatContent_f75fb0'));
    return { border: style.borderTopColor, surface: style.backgroundColor, text: content.color, chat: chat.backgroundColor };
  });
  function luminance(rgb) {
    return rgb.match(/[\d.]+/g).slice(0, 3).map(Number).map(c => c / 255).map(c => c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
  }
  function contrast(a, b) { const values = [luminance(a), luminance(b)].sort((a, b) => b - a); return (values[0] + .05) / (values[1] + .05); }
  expect(contrast(colors.text, colors.chat)).toBeGreaterThanOrEqual(4.5);
  expect(contrast(colors.border, colors.surface)).toBeGreaterThanOrEqual(3);
});

test('disable and file removal restore surfaces, fonts, and decoration', async ({ page }) => {
  const before = await page.locator('.base__5e434').evaluate(el => ({ font: getComputedStyle(el).fontFamily }));
  await page.getByRole('button', { name: 'Disable theme', exact: true }).click();
  await expect.poll(() => token(page)).toBe('');
  await expect(page.locator('.base__5e434')).toHaveCSS('background-color', 'rgb(49, 51, 56)');
  expect(await page.locator('#channels').evaluate(el => getComputedStyle(el, '::before').content)).toBe('none');
  await expect(page.locator('.base__5e434')).toHaveCSS('font-family', before.font);
  await page.getByRole('button', { name: 'Enable theme', exact: true }).click();
  await expect.poll(() => token(page)).toBe('#ff6600');
  await page.locator('#theme').evaluate(el => el.remove());
  await expect.poll(() => token(page)).toBe('');
});

test('reloading CSS and repeated navigation do not accumulate styles', async ({ page }) => {
  for (let i = 0; i < 12; i++) {
    await page.locator('[data-list-item-id="guildsnav___other"]').click();
    expect(await token(page)).toBe('');
    await page.locator(target).click();
    expect(await token(page)).toBe('#ff6600');
  }
  await page.reload();
  await expect.poll(() => token(page)).toBe('#ff6600');
  expect(await page.locator('link[rel="stylesheet"]').count()).toBe(3);
});

test('styles parse in Chromium and contain no network-dependent assets', async ({ page }) => {
  const css = await readFile(theme, 'utf8');
  const info = await page.evaluate(text => {
    const sheet = new CSSStyleSheet(); sheet.replaceSync(text);
    const scope = Array.from(sheet.cssRules).find(rule => rule.constructor.name === 'CSSScopeRule');
    return { count: sheet.cssRules.length, type: scope?.constructor.name, rules: scope?.cssRules.length };
  }, css);
  expect(info.type).toBe('CSSScopeRule');
  expect(info.count).toBe(2);
  expect(info.rules).toBeGreaterThan(50);
  expect(css).not.toMatch(/url\(["']?https?:/);
  expect(await page.locator('#channels').evaluate(el => getComputedStyle(el, '::before').backgroundImage)).toContain('data:image/svg+xml;base64,');
});

for (const profile of catalog.themes) test(profile.server+': display font loads offline, stays server-scoped and keeps message typography', async ({page})=>{
  await page.locator('[data-list-item-id="guildsnav___home"]').click();
  const normal = await page.locator('.header_f37cb1 h2').evaluate(el=>getComputedStyle(el).fontFamily);
  const body = await page.locator('.markup__75297').first().evaluate(el=>getComputedStyle(el).fontFamily);
  const composer = await page.getByRole('textbox',{name:'Unsent fixture message'}).evaluate(el=>getComputedStyle(el).fontFamily);
  const author = await page.locator('.author').first().evaluate(el=>getComputedStyle(el).fontFamily);
  await page.locator('[data-list-item-id="guildsnav___'+profile.guild+'"]').click();
  expect(await page.evaluate(async({family,weight})=>{
    await document.fonts.load(weight+' 22px "'+family+'"','PRIME BUILD');
    return Array.from(document.fonts).some(font=>font.family.includes(family) && font.status==='loaded');
  },profile.font)).toBe(true);
  for (const selector of ['.header_f37cb1 h2','.container__9293f .title__9293f','.name__2ea32','.name__2ea32 > span','.name__29444','.membersGroup__test']) {
    expect(await page.locator(selector).first().evaluate(el=>getComputedStyle(el).fontFamily)).toContain(profile.font.family);
  }
  expect(await page.locator('.markup__75297').first().evaluate(el=>getComputedStyle(el).fontFamily)).toBe(body);
  expect(await page.getByRole('textbox',{name:'Unsent fixture message'}).evaluate(el=>getComputedStyle(el).fontFamily)).toBe(composer);
  expect(await page.locator('.author').first().evaluate(el=>getComputedStyle(el).fontFamily)).toBe(author);
  const other = catalog.themes.find(item=>item.id!==profile.id);
  await page.locator('[data-list-item-id="guildsnav___'+other.guild+'"]').click();
  expect(await page.locator('.header_f37cb1 h2').evaluate(el=>getComputedStyle(el).fontFamily)).toContain(other.font.family);
  await page.locator('[data-list-item-id="guildsnav___home"]').click();
  expect(await page.locator('.header_f37cb1 h2').evaluate(el=>getComputedStyle(el).fontFamily)).toBe(normal);
});

for (const width of [1024, 1280, 1920]) {
  test('desktop layout stays usable at width ' + width, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    const input = page.getByRole('textbox', { name: 'Unsent fixture message' });
    await input.fill('Local fixture only; no messages are sent.');
    await expect(input).toBeVisible();
    await expect(input).toBeFocused();
    await input.fill('');
  });
}

test('sanitized preview uses the distributed stylesheet', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 920 });
  await page.screenshot({ path: 'test-results/officina-preview.png', fullPage: true });
});

for (const profile of catalog.themes) {
  test(profile.server + ': distinct background on every panel, isolated selection and preview', async ({page}) => {
    await page.locator('[data-list-item-id="guildsnav___'+profile.guild+'"]').click();
    const root = page.locator('.base__5e434');
    expect(await root.evaluate(el=>getComputedStyle(el).getPropertyValue('--pb-profile').trim())).toBe('"'+profile.id+'"');
    for (const selector of ['.chatContent_f75fb0','.sidebarList__5e434','.membersWrap_c8ffbb']) {
      expect(await page.locator(selector).evaluate(el=>getComputedStyle(el).backgroundImage)).toContain('data:image/svg+xml;base64,');
    }
    // Observed current Discord forum container gets the same server-specific main-panel artwork.
    await page.locator('.chatContent_f75fb0').evaluate(el => el.classList.replace('chatContent_f75fb0','container_f369db'));
    expect(await page.locator('.container_f369db').evaluate(el=>getComputedStyle(el).backgroundImage)).toContain('data:image/svg+xml;base64,');
    await page.locator('.container_f369db').evaluate(el => el.classList.replace('container_f369db','chatContent_f75fb0'));
    const pairs = await page.locator('.scrollableContainer__74017').evaluate(el=>({border:getComputedStyle(el).borderTopColor,surface:getComputedStyle(el).backgroundColor,text:getComputedStyle(document.querySelector('.markup__75297')).color,chat:getComputedStyle(document.querySelector('.chatContent_f75fb0')).backgroundColor}));
    const lum = c => c.match(/[\d.]+/g).slice(0,3).map(Number).map(v=>v/255).map(v=>v<=.04045?v/12.92:((v+.055)/1.055)**2.4).reduce((s,v,i)=>s+v*[.2126,.7152,.0722][i],0);
    const ratio = (a,b) => { const v=[lum(a),lum(b)].sort((a,b)=>b-a);return (v[0]+.05)/(v[1]+.05); };
    expect(ratio(pairs.text,pairs.chat)).toBeGreaterThanOrEqual(4.5);
    expect(ratio(pairs.border,pairs.surface)).toBeGreaterThanOrEqual(3);
    await expect(page.locator('.guilds__5e434')).toHaveCSS('background-color','rgb(30, 31, 34)');
    await page.setViewportSize({width:1440,height:920});
    const name = profile.id === 'primebuild' ? 'officina' : profile.id;
    await page.screenshot({path:'test-results/'+name+'-preview.png',fullPage:true});
    await page.locator('[data-list-item-id="guildsnav___other"]').click();
    expect(await root.evaluate(el=>getComputedStyle(el).getPropertyValue('--pb-profile').trim())).toBe('');
    await page.locator('[data-list-item-id="guildsnav___home"]').click();
    expect(await token(page)).toBe('');
  });
}
