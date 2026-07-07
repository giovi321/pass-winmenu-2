"""Generates the pass-winmenu-2 icons (keyhole glyph + git-sync badges).

Draws with Pillow primitives, mirroring the geometry in resources/icon.svg,
supersampled 8x, and packs multi-size .ico files (PNG-compressed entries)
into pass-winmenu/embedded/. A faint dark outline is baked in so the white
glyph stays visible on light taskbars. 16/20 px entries use pixel-hinted
geometry (resources/icon-16.svg).

Usage: python generate_icons.py    (requires: pip install pillow)
"""
import io
import struct
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

SS = 8  # supersampling factor
SIZES = [16, 20, 24, 32, 48, 256]
OUT_DIR = Path(__file__).resolve().parent.parent / 'pass-winmenu' / 'embedded'
PREVIEW_DIR = Path(__file__).resolve().parent / 'preview'

WHITE = (255, 255, 255, 255)
OUTLINE = (0, 0, 0, 140)

# Base geometry, 32-unit box (resources/icon.svg)
BASE = {
    'units': 32.0,
    'disc_c': (16.0, 16.0),
    'disc_r': 13.0,
    'hole_c': (16.0, 11.5),
    'hole_r': 4.2,
    'trap': [(13.4, 14.8), (18.6, 14.8), (20.8, 23.5), (11.2, 23.5)],
    'outline': 0.8,
}
# Pixel-hinted geometry for 16/20 px, 16-unit box (resources/icon-16.svg)
HINTED = {
    'units': 16.0,
    'disc_c': (8.0, 8.0),
    'disc_r': 7.0,
    'hole_c': (8.0, 5.6),
    'hole_r': 2.3,
    'trap': [(6.7, 7.4), (9.3, 7.4), (10.4, 12.0), (5.6, 12.0)],
    'outline': 0.5,
}

# Badges, 32-unit box
BADGE_UNITS = 32.0
BADGE_C = (24.6, 24.6)
BADGE_R = 6.4
BADGE_GAP = 1.6
BADGES = {
    'ahead': ((63, 185, 80, 255),
              [(24.6, 21.0), (28.0, 24.8), (25.9, 24.8), (25.9, 28.4),
               (23.3, 28.4), (23.3, 24.8), (21.2, 24.8)]),
    'behind': ((56, 139, 253, 255),
               [(24.6, 28.2), (28.0, 24.4), (25.9, 24.4), (25.9, 20.8),
                (23.3, 20.8), (23.3, 24.4), (21.2, 24.4)]),
    'diverged': ((219, 109, 40, 255),
                 [(24.6, 21.2), (27.8, 24.6), (24.6, 28.0), (21.4, 24.6)]),
}


def circle_box(cx, cy, r, scale):
    return [(cx - r) * scale, (cy - r) * scale, (cx + r) * scale, (cy + r) * scale]


def inset_polygon(points, amount):
    """Moves each vertex towards the centroid so edges shift inwards by roughly
    `amount` units. Exact edge offsetting is unnecessary at these sizes."""
    n = len(points)
    cx = sum(p[0] for p in points) / n
    cy = sum(p[1] for p in points) / n
    result = []
    for x, y in points:
        dx, dy = x - cx, y - cy
        dist = (dx * dx + dy * dy) ** 0.5
        factor = max(dist - amount, 0.0) / dist
        result.append((cx + dx * factor, cy + dy * factor))
    return result


def keyhole_mask(canvas, g, scale, grow):
    """Alpha mask of the disc with the keyhole punched out. A positive `grow`
    expands the disc and shrinks the cutout, producing the outline silhouette."""
    mask = Image.new('L', (canvas, canvas), 0)
    draw = ImageDraw.Draw(mask)
    draw.ellipse(circle_box(*g['disc_c'], g['disc_r'] + grow, scale), fill=255)
    cut = Image.new('L', (canvas, canvas), 0)
    cut_draw = ImageDraw.Draw(cut)
    cut_draw.ellipse(circle_box(*g['hole_c'], g['hole_r'] - grow, scale), fill=255)
    cut_draw.polygon([(x * scale, y * scale) for x, y in inset_polygon(g['trap'], grow)], fill=255)
    return ImageChops.subtract(mask, cut)


def draw_glyph(size, badge=None):
    g = HINTED if size <= 20 else BASE
    scale = size * SS / g['units']
    canvas = size * SS
    img = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))

    img.paste(Image.new('RGBA', (canvas, canvas), OUTLINE),
              mask=keyhole_mask(canvas, g, scale, g['outline']))
    img.paste(Image.new('RGBA', (canvas, canvas), WHITE),
              mask=keyhole_mask(canvas, g, scale, 0.0))

    if badge is not None:
        bscale = size * SS / BADGE_UNITS
        colour, symbol = BADGES[badge]
        alpha = img.getchannel('A')
        punch = Image.new('L', (canvas, canvas), 0)
        ImageDraw.Draw(punch).ellipse(circle_box(*BADGE_C, BADGE_R + BADGE_GAP, bscale), fill=255)
        img.putalpha(ImageChops.subtract(alpha, punch))
        draw = ImageDraw.Draw(img)
        draw.ellipse(circle_box(*BADGE_C, BADGE_R, bscale), fill=colour)
        draw.polygon([(x * bscale, y * bscale) for x, y in symbol], fill=WHITE)

    return img.resize((size, size), Image.LANCZOS)


def write_ico(path, images):
    header = struct.pack('<HHH', 0, 1, len(images))
    entries = []
    blobs = []
    offset = 6 + 16 * len(images)
    for img in images:
        buf = io.BytesIO()
        img.save(buf, 'PNG')
        blob = buf.getvalue()
        dim = img.width % 256  # 256 px is stored as 0
        entries.append(struct.pack('<BBBBHHII', dim, dim, 0, 0, 1, 32, len(blob), offset))
        blobs.append(blob)
        offset += len(blob)
    path.write_bytes(header + b''.join(entries) + b''.join(blobs))


def main():
    PREVIEW_DIR.mkdir(exist_ok=True)
    for variant, badge in [('plain', None), ('ahead', 'ahead'),
                           ('behind', 'behind'), ('diverged', 'diverged')]:
        images = [draw_glyph(size, badge) for size in SIZES]
        write_ico(OUT_DIR / f'pass-winmenu-{variant}.ico', images)
        for img in images:
            if img.width in (16, 32, 48):
                img.save(PREVIEW_DIR / f'{variant}-{img.width}.png')
        print(f'wrote pass-winmenu-{variant}.ico')


if __name__ == '__main__':
    main()
