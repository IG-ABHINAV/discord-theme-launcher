import assert from 'node:assert/strict';
import { readFile, stat } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import postcss from 'postcss';

const root = new URL('../', import.meta.url);
const css = await readFile(new URL('PrimeBuild.theme.css', root), 'utf8');
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
assert.ok(top[0].params.includes('[data-list-item-id="guildsnav___1206415718728601600"][aria-selected="true"]'));
ast.walkAtRules(rule => assert.ok(['scope', 'media'].includes(rule.name), 'Unexpected @' + rule.name));
ast.walkDecls(decl => {
  assert.ok(!/url\(\s*["']?(?:https?:|\/\/|file:)/i.test(decl.value), 'External dependency: ' + decl.prop);
  assert.ok(!/^(?:animation|animation-name|backdrop-filter)$/.test(decl.prop), 'Unnecessary ongoing effect: ' + decl.prop);
});
assert.ok(!css.includes('__OFFICINA_HEADER__'), 'Artwork has not been embedded');
assert.ok(Buffer.byteLength(css) <= 350 * 1024, 'Standalone theme exceeds its size budget');
assert.ok((await stat(new URL('src/PrimeBuild.css', root))).size <= 100 * 1024);
const expected = createHash('sha256').update(css).digest('hex') + '  PrimeBuild.theme.css\n';
assert.equal(await readFile(new URL('SHA256SUMS.txt', root), 'utf8'), expected, 'Stale checksum');
console.log('Scope, offline assets, effect budget, and checksum checks passed.');
