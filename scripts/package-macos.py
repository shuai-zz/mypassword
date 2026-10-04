#!/usr/bin/env python3
"""Package a dotnet publish directory as a macOS application with a tray background mode."""
import argparse
import plistlib
import shutil
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('publish_dir', type=Path)
parser.add_argument('app_path', type=Path)
args = parser.parse_args()
if not (args.publish_dir / 'MyPassword').is_file():
    parser.error('publish_dir must contain the compiled MyPassword executable')
if args.app_path.exists() or args.app_path.suffix != '.app':
    parser.error('app_path must be a new .app directory')
macos = args.app_path / 'Contents' / 'MacOS'
macos.mkdir(parents=True)
for source in args.publish_dir.iterdir():
    if source.name.endswith('.dSYM'):
        continue
    if source.is_dir():
        shutil.copytree(source, macos / source.name)
    else:
        shutil.copy2(source, macos / source.name)
info = {
    'CFBundleExecutable': 'MyPassword',
    'CFBundleIdentifier': 'local.mypassword',
    'CFBundleName': 'MyPassword',
    'CFBundleDisplayName': 'MyPassword',
    'CFBundlePackageType': 'APPL',
    'CFBundleVersion': '1.0',
    'CFBundleShortVersionString': '0.0.0',
    'LSMinimumSystemVersion': '11.0',
    'LSUIElement': False,
    'NSHighResolutionCapable': True,
}
with (args.app_path / 'Contents' / 'Info.plist').open('wb') as stream:
    plistlib.dump(info, stream)
print(args.app_path)
