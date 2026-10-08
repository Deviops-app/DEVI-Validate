#!/usr/bin/env python3
"""Render the shared DEVI Windows app icon from source.

Writes, next to this script:
  devi-app-icon.svg      master artwork (256 px design grid)
  png/devi-app-<N>.png   one hand-tuned render per size
  devi-app.ico           multi-size icon (BMP frames up to 128 px, PNG frame at 256 px)

Requires Python 3 with cairosvg and Pillow:
  python3 -m venv .venv && .venv/bin/pip install cairosvg pillow
  .venv/bin/python shared/brand/make_app_icon.py

The tile is a continuous-corner rounded square (superellipse), near-black with the subtle
top-to-bottom DEVI header gradient, a faint light edge so it reads on dark desktops, and a soft
shadow at 40 px and up so it reads on light ones. The white DEVI mark is the same path as
the DEVI mark. Sizes 16 to 32 use a larger tile, a thicker mark, and no shadow so the
mark stays crisp.
"""
import io
import math
import struct
from pathlib import Path

import cairosvg
from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
SIZES = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256]

# The DEVI mark: a D drawn with one 43-unit stroke in a 298 x 215 box.
MARK_PATH = "M0 21.5L190.5 21.5A86 86 0 0 1 190.5 193.5L21.5 193.5L21.5 91"
MARK_W, MARK_H, MARK_STROKE = 298.0, 215.0, 43.0

# Palette (Devi.Theme Tokens.xaml): canvas #0A0A0B, card #111113, raised #1C1C20.
TOP, BOTTOM = "#26262B", "#0A0A0B"
EDGE = (255, 255, 255, 0.16)


def tune(size):
    """Per-size layout, checked by eye at 100 % and magnified 8x.

    Large sizes scale the master. 16 to 32 px place the mark on whole pixels: mark width, stroke,
    and offset are given in device pixels so the straight strokes land on the pixel grid.
    """
    if size == 16:
        return dict(inset=0.0, n=4.2, px=dict(w=11, stroke=2, x=3, y=4), shadow=False, edge=True)
    if size == 20:
        return dict(inset=0.0, n=4.4, px=dict(w=14, stroke=2, x=3, y=5), shadow=False, edge=True)
    if size == 24:
        return dict(inset=0.0, n=4.6, px=dict(w=18, stroke=3, x=3, y=5), shadow=False, edge=True)
    if size == 32:
        return dict(inset=0.0, n=4.8, px=dict(w=22, stroke=3, x=5, y=8), shadow=False, edge=True)
    if size <= 48:
        return dict(inset=0.06, n=5.0, mark=0.58, stroke=47.0, shadow=True, edge=True)
    return dict(inset=0.08, n=5.0, mark=0.54, stroke=43.0, shadow=True, edge=True)


def mark_path(grow):
    """The DEVI mark with the stroke thickened inwards by `grow` units on each side.

    The outer box stays 298 x 215, so a bolder mark at small sizes never eats the padding.
    """
    g = grow
    r = 86 - g
    return (f"M0 {21.5 + g:.3f}L190.5 {21.5 + g:.3f}A{r:.3f} {r:.3f} 0 0 1 190.5 {193.5 - g:.3f}"
            f"L{21.5 + g:.3f} {193.5 - g:.3f}L{21.5 + g:.3f} 91")


def squircle(cx, cy, r, n, steps=240):
    pts = []
    for i in range(steps):
        t = 2 * math.pi * i / steps
        c, s = math.cos(t), math.sin(t)
        x = cx + r * math.copysign(abs(c) ** (2 / n), c)
        y = cy + r * math.copysign(abs(s) ** (2 / n), s)
        pts.append(f"{x:.3f},{y:.3f}")
    return "M" + " L".join(pts) + " Z"


def svg(size, inset, n, edge, mark=None, stroke=MARK_STROKE, px=None, **_):
    grid = 256.0
    unit = grid / size  # one device pixel in grid units
    pad = inset * grid
    r = (grid - 2 * pad) / 2
    shape = squircle(grid / 2, grid / 2, r, n)
    if px:
        k_px = px["w"] / MARK_W  # device pixels per mark unit
        # Outer edge of the top and left strokes is at 0; inner edge must land on stroke px.
        stroke = px["stroke"] / k_px
        k = k_px * unit
        tx, ty = px["x"] * unit, px["y"] * unit
    else:
        k = mark * (grid - 2 * pad) / MARK_W
        tx = grid / 2 - MARK_W * k / 2
        ty = grid / 2 - MARK_H * k / 2
    grow = (stroke - MARK_STROKE) / 2
    edge_el = ""
    if edge:
        er, eg, eb, ea = EDGE
        edge_el = (f'<path d="{squircle(grid / 2, grid / 2, r - unit / 2, n)}" fill="none" '
                   f'stroke="rgb({er},{eg},{eb})" stroke-opacity="{ea}" stroke-width="{unit:.3f}"/>')
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 256 256">
  <defs>
    <linearGradient id="tile" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{TOP}"/>
      <stop offset="1" stop-color="{BOTTOM}"/>
    </linearGradient>
  </defs>
  <path d="{shape}" fill="url(#tile)"/>
  {edge_el}
  <path d="{mark_path(grow)}" transform="translate({tx:.3f} {ty:.3f}) scale({k:.5f})" fill="none" stroke="#FFFFFF" stroke-width="{stroke:.3f}"/>
</svg>
'''


def render(size):
    t = tune(size)
    scale = 1
    png = cairosvg.svg2png(bytestring=svg(size, **t).encode(), output_width=size, output_height=size)
    tile = Image.open(io.BytesIO(png)).convert("RGBA")
    if not t["shadow"]:
        return tile
    # Soft shadow under the tile: blurred alpha, nudged down, 35 % black.
    alpha = tile.split()[3]
    blur = max(1.0, size / 48)
    shadow = Image.new("RGBA", tile.size, (0, 0, 0, 0))
    mask = alpha.filter(ImageFilter.GaussianBlur(blur)).point(lambda a: int(a * 0.35))
    offset = Image.new("L", tile.size, 0)
    offset.paste(mask, (0, max(1, round(size / 64))))
    shadow.putalpha(offset)
    out = Image.alpha_composite(shadow, tile)
    return out


def bmp_frame(im):
    """32-bit BGRA DIB with an AND mask, the most widely read ICO frame format."""
    w, h = im.size
    px = im.tobytes("raw", "BGRA")
    rows = [px[y * w * 4:(y + 1) * w * 4] for y in range(h)][::-1]
    xor = b"".join(rows)
    mask_row = ((w + 31) // 32) * 4
    a = im.split()[3].tobytes()
    and_rows = []
    for y in range(h - 1, -1, -1):
        bits = bytearray(mask_row)
        for x in range(w):
            if a[y * w + x] == 0:
                bits[x // 8] |= 0x80 >> (x % 8)
        and_rows.append(bytes(bits))
    header = struct.pack("<IiiHHIIiiII", 40, w, h * 2, 1, 32, 0, len(xor) + len(and_rows) * mask_row, 0, 0, 0, 0)
    return header + xor + b"".join(and_rows)


def write_ico(path, images):
    frames = []
    for im in images:
        if im.size[0] >= 256:
            buf = io.BytesIO()
            im.save(buf, "PNG", optimize=True)
            frames.append((im.size[0], buf.getvalue()))
        else:
            frames.append((im.size[0], bmp_frame(im)))
    out = struct.pack("<HHH", 0, 1, len(frames))
    offset = 6 + 16 * len(frames)
    entries, blobs = b"", b""
    for size, data in frames:
        d = 0 if size >= 256 else size
        entries += struct.pack("<BBBBHHII", d, d, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
        blobs += data
    Path(path).write_bytes(out + entries + blobs)


def main():
    (HERE / "png").mkdir(exist_ok=True)
    (HERE / "devi-app-icon.svg").write_text(svg(256, **tune(256)), encoding="utf-8")
    images = []
    for size in SIZES:
        im = render(size)
        im.save(HERE / "png" / f"devi-app-{size}.png", optimize=True)
        images.append(im)
    write_ico(HERE / "devi-app.ico", images)
    print("wrote", HERE / "devi-app.ico", [s for s in SIZES])


if __name__ == "__main__":
    main()
