// Bakes the Korean pixel font (Galmuri7, OFL) into unity/Assets/Resources/kfont.txt.
// Galmuri7 draws Hangul on an 8px grid (7x7 glyphs), so one font pixel = one game pixel.
// Included: printable ASCII, the 2,350 common Hangul syllables (KS X 1001), compatibility jamo,
// a few symbols, and every non-ASCII character found in the game's C# sources.
//   node pipeline/kfont.mjs
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const bdf = zlib.gunzipSync(fs.readFileSync(path.join(root, 'pipeline/fonts/Galmuri7.bdf.gz'))).toString('latin1');

const want = new Set();
for (let c = 0x20; c <= 0x7e; c++) want.add(c);
const dec = new TextDecoder('euc-kr');
for (let hi = 0xb0; hi <= 0xc8; hi++) for (let lo = 0xa1; lo <= 0xfe; lo++) {
  const s = dec.decode(new Uint8Array([hi, lo]));
  const cp = s.codePointAt(0);
  if (cp >= 0xac00 && cp <= 0xd7a3) want.add(cp);
}
for (let c = 0x3131; c <= 0x318e; c++) want.add(c);
for (const s of '·…↑↓←→★☆♥♡○●◆◇■□▶◀▲▼※~「」『』“”‘’%') want.add(s.codePointAt(0));
const srcDir = path.join(root, 'unity/Assets/Scripts');
for (const f of fs.readdirSync(srcDir).filter(f => f.endsWith('.cs')))
  for (const ch of fs.readFileSync(path.join(srcDir, f), 'utf8')) { const cp = ch.codePointAt(0); if (cp > 0x7e && cp !== 0xfeff) want.add(cp); }

const glyphs = new Map();
const re = /ENCODING (\d+)\s+SWIDTH[^\n]*\nDWIDTH (\d+) \d+\nBBX (\d+) (\d+) (-?\d+) (-?\d+)\nBITMAP\n([\s\S]*?)ENDCHAR/g;
let m;
while ((m = re.exec(bdf))) {
  const cp = +m[1];
  if (!want.has(cp)) continue;
  const rows = m[7].trim().split(/\s+/).filter(Boolean);
  glyphs.set(cp, [cp, +m[2], +m[3], +m[4], +m[5], +m[6], rows.join('') || '-'].join(' '));
}
const missing = [...want].filter(c => !glyphs.has(c) && c > 0x7e && !(c >= 0x3131 && c <= 0x318e));
const out = ['# Galmuri7 (c) 2019-2025 Lee Minseo (quiple), SIL Open Font License 1.1 - cp adv w h xoff yoff hexrows', ...[...glyphs.keys()].sort((a, b) => a - b).map(k => glyphs.get(k))];
fs.writeFileSync(path.join(root, 'unity/Assets/Resources/kfont.txt'), out.join('\n') + '\n');
console.log(`kfont.txt: ${glyphs.size} glyphs` + (missing.length ? `, missing: ${missing.map(c => String.fromCodePoint(c)).join('')}` : ''));
