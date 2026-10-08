#!/usr/bin/env python3
"""Release guard. Fails if the staged release could break on Windows.

1. No folder may contain two entries whose names differ only by case
   (0.1.1/0.1.2 shipped devi-validate.exe beside DEVI-Validate.exe).
2. app/DEVI-Validate.exe must be a Windows GUI PE (subsystem 2).
3. cli/devi-validate.exe must be a console PE (subsystem 3).
4. app/ must hold DEVI-Validate.dll and no devi-validate.* CLI files.
5. app/DEVI-Validate.dll must not declare loose WPF content files, and devi-validate.ico must ship.
"""
import os, struct, sys

def subsystem(path):
    with open(path, 'rb') as f:
        data = f.read(4096)
    pe = struct.unpack_from('<I', data, 0x3C)[0]
    assert data[pe:pe+4] == b'PE\0\0', path + ' is not a PE file'
    opt = pe + 24
    return struct.unpack_from('<H', data, opt + 68)[0]

root = sys.argv[1]
errors = []
for d, dirs, files in os.walk(root):
    seen = {}
    for n in dirs + files:
        k = n.lower()
        if k in seen:
            errors.append(f'case collision in {d}: {seen[k]} vs {n}')
        seen[k] = n
app = os.path.join(root, 'app'); cli = os.path.join(root, 'cli')
gui = os.path.join(app, 'DEVI-Validate.exe'); con = os.path.join(cli, 'devi-validate.exe')
if not os.path.isfile(gui): errors.append('missing app/DEVI-Validate.exe')
elif subsystem(gui) != 2: errors.append(f'app/DEVI-Validate.exe subsystem {subsystem(gui)} (want 2 = GUI)')
if not os.path.isfile(con): errors.append('missing cli/devi-validate.exe')
elif subsystem(con) != 3: errors.append(f'cli/devi-validate.exe subsystem {subsystem(con)} (want 3 = console)')
if not os.path.isfile(os.path.join(app, 'DEVI-Validate.dll')): errors.append('missing app/DEVI-Validate.dll')
for n in os.listdir(app):
    if n.startswith('devi-validate.') and n != 'devi-validate.ico':
        errors.append(f'CLI file in app/: {n}')
# 5. The window app ships no loose WPF content files. A Content item (0.1.3: Assets\devi.ico) adds
#    AssemblyAssociatedContentFileAttribute, WPF then looks for the file on disk and MainWindow fails to load.
app_dll = os.path.join(app, 'DEVI-Validate.dll')
if os.path.isfile(app_dll):
    with open(app_dll, 'rb') as f:
        if b'AssemblyAssociatedContentFileAttribute' in f.read():
            errors.append('app/DEVI-Validate.dll declares loose content files (AssemblyAssociatedContentFile); use Resource or None items')
if not os.path.isfile(os.path.join(app, 'devi-validate.ico')): errors.append('missing app/devi-validate.ico (installer shortcut icon)')
if errors:
    print('LAYOUT CHECK FAILED'); [print(' -', e) for e in errors]; sys.exit(1)
print('layout ok:', root)
