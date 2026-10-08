# DEVI Windows app icon

One icon for every DEVI Windows app: a near-black continuous-corner rounded square (superellipse) with the subtle top-to-bottom DEVI header gradient, the white DEVI mark centered, transparent corners, a faint light edge for dark desktops, and a soft shadow at 40 px and up for light ones.

| File | Use |
| --- | --- |
| `make_app_icon.py` | The source. Draws every size from the DEVI mark path and writes the files below. |
| `devi-app-icon.svg` | The 256 px master, for reference and design review. |
| `png/devi-app-<N>.png` | One render per size: 16, 20, 24, 32, 40, 48, 64, 96, 128, 256. |
| `devi-app.ico` | All ten sizes in one file (32-bit BMP frames up to 128 px, PNG at 256 px). |

16, 20, 24, and 32 px are tuned by hand in `tune()`: a fuller tile, a bolder mark, and the straight strokes placed on whole pixels so they stay sharp. Check them magnified after any change.

Regenerate:

```
python3 -m venv /tmp/icon-venv && /tmp/icon-venv/bin/pip install cairosvg pillow
/tmp/icon-venv/bin/python shared/brand/make_app_icon.py
```

Then run `assets/brand/render.py`, which copies `devi-app.ico` to `assets/brand/devi.ico` and `src/DeviValidate.Desktop/Assets/devi.ico`.

That one file is the exe icon (`ApplicationIcon`), the window and taskbar icon (`Icon="Assets/devi.ico"`), the Start menu and desktop shortcut icon, and the Setup, uninstaller, and Add/Remove Programs icon. Windows caches shortcut icons, so the installer sets `ChangesAssociations=yes`, which makes Setup call `SHChangeNotify` when it finishes.

The website favicon is separate and does not use this icon.
