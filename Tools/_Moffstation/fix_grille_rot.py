#!/usr/bin/env python3
"""
Remove 'rot:' lines from Grille entity Transform components in SS14 map YAML files.

Replicates the VS Code regex find-replace from space-wizards/space-station-14#44079,
which removed all rotation from standard (non-diagonal) grilles after grilles were
made norot=true.

Matches rot lines that:
  1. Are inside a '- proto: Grille' entities block
  2. Appear as a plain 'rot:' key under a Transform component
  3. Are immediately followed by a 'pos:' line

Usage:
  python fix_grille_rot.py [file_or_directory ...]

Defaults to Resources/Maps/ relative to the repo root.
Directories are searched recursively for *.yml files.
"""

import re
import sys
from pathlib import Path

_ANY_PROTO  = re.compile(r'^- proto: ')
_TARGET     = re.compile(r'^- proto: Grille\s*$')
_ENTITIES   = re.compile(r'^  entities:\s*$')
# In the actual map format, rot is a plain key under Transform: "      rot: X rad"
_ROT        = re.compile(r'^      rot: -?\d+\.\d+ rad\s*$')
# The pos continuation that follows: "      pos: X,Y"
_POS        = re.compile(r'^      pos: -?\d+\.\d+,-?\d+\.\d+')


def remove_grille_rotations(text: str) -> str:
    lines = text.splitlines(keepends=True)
    result = []
    in_target = False
    in_entities = False

    i = 0
    while i < len(lines):
        line = lines[i]
        bare = line.rstrip('\r\n')

        if _ANY_PROTO.match(bare):
            in_target = _TARGET.match(bare) is not None
            in_entities = False

        if in_target and _ENTITIES.match(bare):
            in_entities = True

        if in_entities and _ROT.match(bare):
            next_bare = lines[i + 1].rstrip('\r\n') if i + 1 < len(lines) else ''
            if _POS.match(next_bare):
                # Drop the rot line; pos/type/parent keys are plain, no promotion needed
                i += 1
                continue

        result.append(line)
        i += 1

    return ''.join(result)


def process_file(path: Path) -> bool:
    text = path.read_text(encoding='utf-8')
    new_text = remove_grille_rotations(text)
    if new_text != text:
        path.write_text(new_text, encoding='utf-8')
        return True
    return False


MAPS_DIR = Path(__file__).parent.parent.parent / 'Resources' / 'Maps'


def main():
    targets = sys.argv[1:] if len(sys.argv) > 1 else [str(MAPS_DIR)]

    changed = 0
    for arg in targets:
        p = Path(arg)
        if p.is_dir():
            for yml in sorted(p.rglob('*.yml')):
                if process_file(yml):
                    print(f"Fixed: {yml}")
                    changed += 1
        elif p.is_file():
            if process_file(p):
                print(f"Fixed: {p}")
                changed += 1
        else:
            print(f"Warning: not found: {p}", file=sys.stderr)

    print(f"\nModified {changed} file(s).")


if __name__ == '__main__':
    main()
