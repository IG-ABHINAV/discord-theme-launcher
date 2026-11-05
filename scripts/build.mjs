import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';

const root = new URL('../', import.meta.url);
const source = await readFile(new URL('src/PrimeBuild.css', root), 'utf8');
const logo = await readFile(new URL('assets/prime-build-logo.png', root));
const artwork = (await readFile(new URL('assets/officina-header.svg', root), 'utf8'))
  .replace('__LOGO_DATA_URI__', 'data:image/png;base64,' + logo.toString('base64'));
const data = 'data:image/svg+xml;base64,' + Buffer.from(artwork).toString('base64');
if (!source.includes('__OFFICINA_HEADER__')) throw new Error('Missing artwork placeholder');
const css = source.replace('__OFFICINA_HEADER__', data);
await writeFile(new URL('PrimeBuild.theme.css', root), css);
const hash = createHash('sha256').update(css).digest('hex');
await writeFile(new URL('SHA256SUMS.txt', root), hash + '  PrimeBuild.theme.css\n');
console.log('Built PrimeBuild.theme.css (' + Buffer.byteLength(css) + ' bytes)');
