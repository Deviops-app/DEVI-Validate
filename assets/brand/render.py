#!/usr/bin/env python3
"""Rasterize the official DEVI SVGs with rsvg-convert. Path data is not edited."""

import subprocess
from io import BytesIO
from pathlib import Path

from PIL import Image

BRAND = Path(__file__).resolve().parent
ROOT = BRAND.parents[1]
WHITE = "#FFFFFF"
DARK = "#141416"
SIZES = (16, 24, 32, 48, 64, 128, 256)
INK_WIDTH = 0.62
ALPHA = 32


def render_svg(svg: Path, width: int) -> Image.Image:
    proc = subprocess.run(
        ["rsvg-convert", "-w", str(width), "-b", "rgba(0,0,0,0)", svg.as_posix()],
        check=True,
        capture_output=True,
    )
    return Image.open(BytesIO(proc.stdout)).convert("RGBA")


def ink_box(image: Image.Image):
    mask = image.getchannel("A").point(lambda value: 255 if value > ALPHA else 0)
    return mask.getbbox()


def crop_ink(image: Image.Image) -> Image.Image:
    box = ink_box(image)
    if box is None:
        raise SystemExit(f"no ink in {image.size}")
    return image.crop(box)


def write_dark_svg(name: str) -> None:
    text = (BRAND / name).read_text(encoding="utf-8")
    dark_name = name.replace("-white.svg", "-dark.svg")
    dark = text.replace(WHITE, DARK)
    if WHITE in dark or dark == text:
        raise SystemExit(f"recolor failed for {name}")
    (BRAND / dark_name).write_text(dark, encoding="utf-8")


def icon_from_d() -> None:
    probe = render_svg(BRAND / "devi-d-white.svg", 2980)
    box = ink_box(probe)
    if box is None:
        raise SystemExit("D has no ink")
    fraction = (box[2] - box[0]) / probe.width
    frames = []
    for size in SIZES:
        target = max(1, round(size * INK_WIDTH))
        rendered = render_svg(BRAND / "devi-d-white.svg", max(1, round(target / fraction)))
        ink = crop_ink(rendered)
        scaled_h = max(1, round(ink.height * (target / ink.width)))
        if scaled_h >= size:
            raise SystemExit(f"D is taller than the {size}px square")
        ink = ink.resize((target, scaled_h), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (size, size), (0, 0, 0, 255))
        x = (size - ink.width) // 2
        y = (size - ink.height) // 2
        canvas.paste(ink, (x, y), ink)
        ratio = ink.width / size
        if abs(ratio - INK_WIDTH) > 0.02:
            raise SystemExit(f"{size}px ink width ratio is {ratio:.3f}")
        frames.append(canvas)
    # The Windows app icon is shared by every DEVI app and rendered by
    # shared/brand/make_app_icon.py (rounded tile, hand-tuned small sizes).
    # Copy it here so the project, the installer, and the shortcuts all use the same file.
    shared = Path(__file__).resolve().parents[2] / "shared" / "brand"
    (BRAND / "devi-app-icon-256.png").write_bytes((shared / "png" / "devi-app-256.png").read_bytes())
    desktop = ROOT / "src" / "DeviValidate.Desktop" / "Assets"
    desktop.mkdir(parents=True, exist_ok=True)
    (desktop / "devi.png").write_bytes((shared / "png" / "devi-app-256.png").read_bytes())
    ico_bytes = (shared / "devi-app.ico").read_bytes()
    (BRAND / "devi.ico").write_bytes(ico_bytes)
    (desktop / "devi.ico").write_bytes(ico_bytes)


def save_wordmark(svg_name: str, png_name: str) -> None:
    image = crop_ink(render_svg(BRAND / svg_name, 2000))
    image.save(BRAND / png_name)


def main() -> None:
    for name in (
        "devi-d-white.svg",
        "devi-wordmark-white.svg",
        "devi-wordmark-compact-white.svg",
    ):
        write_dark_svg(name)
    icon_from_d()
    save_wordmark("devi-wordmark-white.svg", "devi-wordmark-white.png")
    save_wordmark("devi-wordmark-dark.svg", "devi-wordmark-dark.png")
    mark = crop_ink(render_svg(BRAND / "devi-d-dark.svg", 1200))
    mark.save(BRAND / "devi-d-dark.png")
    desktop = ROOT / "src" / "DeviValidate.Desktop" / "Assets"
    (desktop / "devi-wordmark-white.png").write_bytes((BRAND / "devi-wordmark-white.png").read_bytes())
    brand_dir = ROOT / "src" / "DeviValidate.Core" / "Reporting" / "Brand"
    brand_dir.mkdir(parents=True, exist_ok=True)
    (brand_dir / "devi-wordmark-dark.png").write_bytes((BRAND / "devi-wordmark-dark.png").read_bytes())
    (brand_dir / "devi-d-dark.png").write_bytes((BRAND / "devi-d-dark.png").read_bytes())


if __name__ == "__main__":
    main()
