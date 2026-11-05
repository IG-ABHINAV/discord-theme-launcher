import assert from 'node:assert/strict';
import { readFile, stat } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import postcss from 'postcss';

const root = new URL('../', import.meta.url);
const catalog = JSON.parse(await readFile(new URL('catalog.json', root), 'utf8'));
const checksums = [];
for (const profile of catalog.themes) {
  const css = await readFile(new URL(profile.file, root), 'utf8');
  const ast = postcss.parse(css);
  const pkg = JSON.parse(await readFile(new URL('package.json', root), 'utf8'));
  assert.ok(css.includes('@version ' + pkg.version), 'Theme and package versions must agree');
  const top = ast.nodes.filter(node => node.type !== 'comment');
  assert.equal(top.length, 1, 'All styling must be inside one guarded scope');
  assert.equal(top[0].type, 'atrule');
  assert.equal(top[0].name, 'scope');
  assert.match(top[0].params, /^\(\.base__5e434:has\(/);
  assert.ok(top[0].params.includes('> .content__5e434 > .sidebar__5e434 > nav'));
  assert.ok(top[0].params.includes('[data-list-id="guildsnav"]'));
  assert.ok(top[0].params.includes('[data-list-item-id="guildsnav___'+profile.guild+'"][aria-selected="true"]'));
  for (const other of catalog.themes.filter(t => t.id !== profile.id)) assert.ok(!top[0].params.includes(other.guild));
  ast.walkAtRules(rule => assert.ok(['scope', 'media'].includes(rule.name), 'Unexpected @' + rule.name));
  ast.walkDecls(decl => {
    assert.ok(!/url\(\s*["']?(?:https?:|\/\/|file:)/i.test(decl.value), 'External dependency: ' + decl.prop);
    assert.ok(!/^(?:animation|animation-name|backdrop-filter)$/.test(decl.prop), 'Unnecessary ongoing effect: ' + decl.prop);
  });
  assert.ok(!/__[A-Z][A-Z_]+__/.test(css), 'Unresolved build placeholder');
  for (const area of ['chat','sidebar','members']) assert.ok(css.includes('--pb-'+area+'-art: url("data:image/svg+xml;base64,'), 'Missing panel artwork: '+area);
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
