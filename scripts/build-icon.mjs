import { chromium } from '@playwright/test';
import { readFile, writeFile } from 'node:fs/promises';

// Optional asset build: regenerate the Windows icon after editing its SVG source.
const root = new URL('../', import.meta.url);
const svg = await readFile(new URL('assets/app-icon.svg', root));
const browser = await chromium.launch();
try {
  const page = await browser.newPage({ deviceScaleFactor: 1 });
  await page.setContent('<style>body{margin:0}img{width:100vw;height:100vh}</style><img src="data:image/svg+xml;base64,' + svg.toString('base64') + '">');
  const sizes = [16, 24, 32, 48, 64, 128, 256], frames = [];
  for (const size of sizes) {
    await page.setViewportSize({ width: size, height: size });
    frames.push(await page.screenshot({ omitBackground: true }));
  }
  const header = Buffer.alloc(6 + sizes.length * 16);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(sizes.length, 4);
  let offset = header.length;
  sizes.forEach((size, index) => {
    const entry = 6 + index * 16;
    header[entry] = header[entry + 1] = size % 256;
    header.writeUInt16LE(1, entry + 4);
    header.writeUInt16LE(32, entry + 6);
    header.writeUInt32LE(frames[index].length, entry + 8);
    header.writeUInt32LE(offset, entry + 12);
    offset += frames[index].length;
  });
  await writeFile(new URL('assets/app-icon.ico', root), Buffer.concat([header, ...frames]));
} finally {
  await browser.close();
}
