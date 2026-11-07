import assert from 'node:assert/strict';
import { readFile, stat } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import postcss from 'postcss';

const root = new URL('../', import.meta.url);
const catalog = JSON.parse(await readFile(new URL('catalog.json', root), 'utf8'));
assert.equal(new Set(catalog.themes.map(profile=>profile.font.family)).size, catalog.themes.length, 'Each server must own its font alias');
const checksums = [];
for (const profile of catalog.themes) {
  const css = await readFile(new URL(profile.file, root), 'utf8');
  const ast = postcss.parse(css);
  const pkg = JSON.parse(await readFile(new URL('package.json', root), 'utf8'));
  assert.ok(css.includes('@version ' + pkg.version), 'Theme and package versions must agree');
  const resources = ast.nodes.filter(node => node.type === 'atrule' && node.name === 'font-face');
  assert.equal(resources.length, 1, 'Each theme needs one audited font registration');
  for (const resource of resources) {
    assert.equal(resource.nodes.find(n=>n.prop==='font-family')?.value, '"'+profile.font.family+'"');
    assert.equal(resource.nodes.find(n=>n.prop==='font-weight')?.value, String(profile.font.weight));
    assert.match(resource.nodes.find(n=>n.prop==='src')?.value ?? '', /^url\("data:font\/(?:ttf|woff2);base64,/);
    assert.ok(resource.nodes.every(n=>n.type==='decl'), 'Font registration cannot contain selectors');
  }
  ast.walkAtRules('font-face', resource => assert.ok(resources.includes(resource), 'Only the audited top-level font resource is allowed'));
  assert.ok(!/\/\s*0[123]\b/.test(profile.caption), 'Server identities must not have serial numbers');
  const top = ast.nodes.filter(node => node.type !== 'comment' && !resources.includes(node));
  assert.equal(top.length, 1, 'All styling must be inside one guarded scope');
  assert.equal(top[0].type, 'atrule');
  assert.equal(top[0].name, 'scope');
  assert.ok(top[0].params.startsWith('(:is(.base__5e434, .base_pog62O):has('));
  assert.ok(top[0].params.includes('> :is(.content__5e434, .content_pog62O) > :is(.sidebar__5e434, .sidebar_pog62O) > nav'));
  assert.ok(top[0].params.includes('[data-list-id="guildsnav"]'));
  assert.ok(top[0].params.includes('[data-list-item-id="guildsnav___'+profile.guild+'"][aria-selected="true"]'));
  for (const other of catalog.themes.filter(t => t.id !== profile.id)) assert.ok(!top[0].params.includes(other.guild));
  ast.walkAtRules(rule => assert.ok(['scope', 'media', 'font-face', 'container'].includes(rule.name), 'Unexpected @' + rule.name));
  ast.walkDecls(decl => {
    assert.ok(!/url\(\s*["']?(?:https?:|\/\/|file:)/i.test(decl.value), 'External dependency: ' + decl.prop);
    assert.ok(!/^(?:animation|animation-name|backdrop-filter)$/.test(decl.prop), 'Unnecessary ongoing effect: ' + decl.prop);
  });
  assert.ok(!/__[A-Z][A-Z_]+__/.test(css), 'Unresolved build placeholder');
  for (const area of ['chat','sidebar','members']) assert.ok(css.includes('--pb-'+area+'-art: url("data:image/svg+xml;base64,'), 'Missing panel artwork: '+area);
  assert.ok(profile.id === 'primebuild' ? css.includes('--pb-photo: url("data:image/webp;base64,') : css.includes('--pb-photo: none;'), 'Prime Build alone carries the board photo');
  assert.ok(Buffer.byteLength(css) <= 350 * 1024, 'Standalone theme exceeds its size budget');
  assert.ok((await stat(new URL('src/server-theme.css', root))).size <= 100 * 1024);
  const hash = createHash('sha256').update(css).digest('hex');
  assert.equal(hash,profile.sha256);
  assert.equal(Buffer.byteLength(css),profile.bytes);
  checksums.push(hash+'  '+profile.file);
}
assert.equal(await readFile(new URL('SHA256SUMS.txt', root),'utf8'), checksums.join('\n')+'\n');
assert.ok((await readFile(new URL('launcher/Directory.Build.props',root),'utf8')).includes('<Version>'+catalog.version+'</Version>'));
console.log('Scope, offline assets, effect budget, and checksum checks passed.');
