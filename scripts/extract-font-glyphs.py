"""Optional artwork regeneration: Python + fontTools 4.63.0. CI uses committed outlines."""
import hashlib
import json
import sys
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen

root = Path(__file__).resolve().parent.parent
source = root / (sys.argv[1] if len(sys.argv) > 1 else 'assets/fonts/bangers/Bangers-Regular.ttf')
font = TTFont(source)
glyph_set = font.getGlyphSet()
cmap = font.getBestCmap()
result = {'sha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'unitsPerEm': font['head'].unitsPerEm, 'glyphs': {}}
for char in (chr(value) for value in range(32,127)):
    glyph = glyph_set[cmap[ord(char)]]
    pen = SVGPathPen(glyph_set)
    glyph.draw(pen)
    result['glyphs'][char] = {'advance': glyph.width, 'path': pen.getCommands()}
(source.parent / 'glyphs.json').write_text(json.dumps(result, separators=(',', ':'), sort_keys=True) + '\n', encoding='utf-8', newline='\n')
