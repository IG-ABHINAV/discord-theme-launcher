import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { artwork } from './artwork.mjs';

const root = new URL('../', import.meta.url);
const source = await readFile(new URL('src/server-theme.css', root), 'utf8');
const profiles = JSON.parse(await readFile(new URL('src/profiles.json', root), 'utf8'));
const {version} = JSON.parse(await readFile(new URL('package.json', root), 'utf8'));
const logo = await readFile(new URL('assets/prime-build-logo.png', root));
const displayBytes = await readFile(new URL('assets/fonts/bangers/Bangers-Regular.ttf', root));
const displayLicense = await readFile(new URL('assets/fonts/bangers/OFL.txt', root), 'utf8');
const displayFont = JSON.parse(await readFile(new URL('assets/fonts/bangers/glyphs.json', root), 'utf8'));
if (createHash('sha256').update(displayBytes).digest('hex') !== displayFont.sha256) throw Error('Display font outlines need regeneration');
const catalog = {version, repository:'PrimeBuild-pc/discord-theme-launcher', themes:[]};
const checksums = [];
await mkdir(new URL('assets/generated/', root), {recursive:true});
for (const p of profiles) {
  const art = artwork(p, 'data:image/png;base64,' + logo.toString('base64'), displayFont);
  const values = {THEME_NAME:p.name, VERSION:version, GUILD_ID:p.guild, DESCRIPTION:p.tagline + ' Server-scoped desktop theme. Disable to restore.',
    ACCENT:p.accent, ACCENT_TEXT:p.accentText, ACCENT_RGB:p.rgb, CHAT:p.chat, SIDEBAR:p.sidebar, PANEL:p.panel, PANEL_RAISED:p.raised, BORDER:p.border, TEXT_MUTED:p.muted, PROFILE_ID:p.id,
    FONT_FACE:p.id === 'fantafort' ? '/* Embedded Bangers font license:\n'+displayLicense+'\n*/\n@font-face { font-family: "PrimeBuild FantaFort Display"; src: url("data:font/ttf;base64,'+displayBytes.toString('base64')+'") format("truetype"); font-style: normal; font-weight: 400; font-display: swap; }' : '',
    PROFILE_RULES:p.id === 'fantafort' ? `  /* Display typography stays in the selected FantaFort server scope. */
  .header_f37cb1 h2, .container__9293f :is(h1,h2,.title__9293f), .name__2ea32, .name__29444,
  .membersGroup_c8ffbb, [class^="membersGroup__"], [class*=" membersGroup__"] {
    font-family: "PrimeBuild FantaFort Display", Impact, "Arial Narrow", sans-serif;
    font-weight: 400; letter-spacing: .045em;
  }
  .header_f37cb1 h2, .container__9293f :is(h1,h2,.title__9293f) { font-size: 22px; line-height: 1.15; text-transform: uppercase; }
  #channels .name__2ea32, #channels .name__2ea32 :is([class^="body-"],[class*=" body-"]) {
    font-family: "PrimeBuild FantaFort Display", Impact, "Arial Narrow", sans-serif;
    font-weight: 400; font-size: 18px; line-height: 22px; letter-spacing: .045em;
  }
  .name__29444 { font-size: 17px; }
  .membersGroup_c8ffbb, [class^="membersGroup__"], [class*=" membersGroup__"] { font-size: 16px; }` : ''};
  for (const [key, svg] of Object.entries(art)) {
    values[key.toUpperCase()+'_ART'] = 'data:image/svg+xml;base64,' + Buffer.from(svg).toString('base64');
    await writeFile(new URL('assets/generated/'+p.id+'-'+key+'.svg', root), svg);
  }
  let css = source;
  for (const [key, value] of Object.entries(values)) css = css.replaceAll('__'+key+'__', value);
  if (/__[A-Z][A-Z_]+__/.test(css)) throw Error('Unresolved template placeholder');
  await writeFile(new URL(p.file, root), css);
  const sha256 = createHash('sha256').update(css).digest('hex');
  catalog.themes.push({...p, sha256, bytes:Buffer.byteLength(css)});
  checksums.push(sha256+'  '+p.file);
  console.log('Built '+p.file+' ('+Buffer.byteLength(css)+' bytes)');
}
await writeFile(new URL('catalog.json', root), JSON.stringify(catalog,null,2)+'\n');
await writeFile(new URL('SHA256SUMS.txt', root), checksums.join('\n')+'\n');
