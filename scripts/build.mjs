import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { artwork } from './artwork.mjs';

const root = new URL('../', import.meta.url);
const copy = JSON.parse(await readFile(new URL('launcher/Windows/ui/studio-copy.json', root), 'utf8'));
const german = JSON.parse(await readFile(new URL('launcher/Windows/ui/studio-de.json', root), 'utf8'));
const i18nFile = new URL('launcher/Windows/ui/i18n.js', root);
const i18n = (await readFile(i18nFile, 'utf8')).split('// Generated studio copy')[0].trimEnd();
await writeFile(i18nFile, i18n + '\n// Generated studio copy; edit studio-copy.json and studio-de.json.\n' +
  'for(const [code,copy] of Object.entries(' + JSON.stringify(copy) + '))Object.assign(I18N[code],copy);\nI18N.de=' + JSON.stringify(german) + ';\n');
const source = await readFile(new URL('src/server-theme.css', root), 'utf8');
// Exact aliases observed in stable Discord on 2026-11-07. Unknown layouts still fail closed.
const discordClasses = JSON.parse(await readFile(new URL('src/discord-classes.json', root), 'utf8'));
const profiles = JSON.parse(await readFile(new URL('src/profiles.json', root), 'utf8'));
const {version} = JSON.parse(await readFile(new URL('package.json', root), 'utf8'));
const logo = await readFile(new URL('assets/prime-build-logo.png', root));
const board = await readFile(new URL('assets/backgrounds/primebuild-board.webp', root));
const catalog = {version, repository:'PrimeBuild-pc/discord-theme-launcher', themes:[]};
const checksums = [];
await mkdir(new URL('assets/generated/', root), {recursive:true});
for (const p of profiles) {
  const fontUrl = new URL('assets/fonts/'+p.font.file, root);
  const displayBytes = await readFile(fontUrl);
  const displayLicense = await readFile(new URL('OFL.txt',fontUrl),'utf8');
  const displayFont = JSON.parse(await readFile(new URL('glyphs.json',fontUrl),'utf8'));
  if (createHash('sha256').update(displayBytes).digest('hex') !== displayFont.sha256) throw Error('Display font outlines need regeneration: '+p.id);
  const arena = p.id === 'fantafort', hardware = p.id === 'primebuild';
  const headingSize = arena ? 22 : hardware ? 22 : 17;
  const channelSize = arena ? 18 : hardware ? 18 : 14;
  const groupSize = arena ? 16 : hardware ? 16 : 13;
  const spacing = arena ? '.045em' : hardware ? '.025em' : '0';
  const fallback = arena ? 'Impact, "Arial Narrow", sans-serif' : hardware ? '"Arial Narrow", sans-serif' : 'Consolas, monospace';
  const fontFamily = '"'+p.font.family+'", '+fallback;
  const mime = p.font.format === 'woff2' ? 'woff2' : 'ttf';
  const art = artwork(p, 'data:image/png;base64,' + logo.toString('base64'), displayFont);
  const values = {THEME_NAME:p.name, VERSION:version, GUILD_ID:p.guild, DESCRIPTION:p.tagline + ' Server-scoped desktop theme. Disable to restore.',
    ACCENT:p.accent, ACCENT_TEXT:p.accentText, ACCENT_RGB:p.rgb, CHAT:p.chat, SIDEBAR:p.sidebar, PANEL:p.panel, PANEL_RAISED:p.raised, BORDER:p.border, TEXT_MUTED:p.muted, PROFILE_ID:p.id,
    FONT_FACE:'/* Embedded '+p.font.name+' font license:\n'+displayLicense+'\n*/\n@font-face { font-family: "'+p.font.family+'"; src: url("data:font/'+mime+';base64,'+displayBytes.toString('base64')+'") format("'+p.font.format+'"); font-style: normal; font-weight: '+p.font.weight+'; font-display: swap; }',
    PHOTO: p.id === 'primebuild' ? 'url("data:image/webp;base64,'+board.toString('base64')+'")' : 'none',
    PROFILE_RULES:`  /* Display typography stays in this selected server scope. */
  .header_f37cb1 h2, .container__9293f :is(h1,h2,.title__9293f), .name__2ea32, .name__29444,
  .membersGroup_c8ffbb, [class^="membersGroup__"], [class*=" membersGroup__"] {
    font-family: ${fontFamily};
    font-weight: ${p.font.weight}; letter-spacing: ${spacing};
  }
  .header_f37cb1 h2, .container__9293f :is(h1,h2,.title__9293f) { font-size: ${headingSize}px; line-height: 1.15; ${arena ? 'text-transform: uppercase;' : ''} }
  #channels .name__2ea32, #channels .name__2ea32 :is([class^="body-"],[class*=" body-"]) {
    font-family: ${fontFamily};
    font-weight: ${p.font.weight}; font-size: ${channelSize}px; line-height: 22px; letter-spacing: ${spacing};
  }
  .name__29444 { font-size: ${arena ? 17 : groupSize}px; }
  .membersGroup_c8ffbb, [class^="membersGroup__"], [class*=" membersGroup__"] { font-size: ${groupSize}px; }`};
  for (const [key, svg] of Object.entries(art)) {
    values[key.toUpperCase()+'_ART'] = 'data:image/svg+xml;base64,' + Buffer.from(svg).toString('base64');
    await writeFile(new URL('assets/generated/'+p.id+'-'+key+'.svg', root), svg);
  }
  let css = source;
  for (const [key, value] of Object.entries(values)) css = css.replaceAll('__'+key+'__', value);
  css = css.replace(/\.([A-Za-z][\w-]*)/g, (selector, name) => discordClasses[name] ? `:is(${selector}, .${discordClasses[name]})` : selector);
  if (/__[A-Z][A-Z_]+__/.test(css)) throw Error('Unresolved template placeholder');
  await writeFile(new URL(p.file, root), css);
  const sha256 = createHash('sha256').update(css).digest('hex');
  catalog.themes.push({...p, author:'Prime Build', source:'https://github.com/PrimeBuild-pc/discord-theme-launcher', license:'MIT', capabilities:['palette','font','banner','background'], sha256, bytes:Buffer.byteLength(css)});
  checksums.push(sha256+'  '+p.file);
  console.log('Built '+p.file+' ('+Buffer.byteLength(css)+' bytes)');
}
await writeFile(new URL('catalog.json', root), JSON.stringify(catalog,null,2)+'\n');
await writeFile(new URL('SHA256SUMS.txt', root), checksums.join('\n')+'\n');
