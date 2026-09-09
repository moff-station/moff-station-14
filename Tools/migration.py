import re
import sys
from pathlib import Path

ROOT = Path(r"B:/dev/moff-station-14/")

FLOW_RE = re.compile(r'^\{\s*sprite\s*:\s*(?P<sprite>\S+),\s*state\s*:\s*(?P<state>"[^"]*"|\S+)\s*\}$')


def parse_icon_value(lines, i, field_indent):
    line = lines[i]
    m = re.match(r"^(\s*)(icon|iconOn|backgroundOn):\s*(.*)$", line)
    rest = m.group(3).rstrip("\n").strip()
    if rest == "":
        j = i + 1
        sprite = None
        state = None
        child_indent = None
        while j < len(lines):
            cl = lines[j]
            if cl.strip() == "":
                j += 1
                continue
            cm = re.match(r'^(\s*)(sprite|state)\s*:\s*("[^"]*"|\S+)\s*$', cl)
            if not cm:
                break
            if child_indent is None:
                child_indent = len(cm.group(1))
            elif len(cm.group(1)) != child_indent:
                break
            if cm.group(2) == "sprite":
                sprite = cm.group(3)
            else:
                state = cm.group(3)
            j += 1
        return "block", sprite, state, j
    fm = FLOW_RE.match(rest)
    if fm:
        return "flow", fm.group("sprite"), fm.group("state"), i + 1
    return "texture", None, rest, i + 1


def process_action_block(lines, start, action_indent, has_charges):
    field_indent = action_indent + "  "
    block = [lines[start]]
    i = start + 1
    icon = None
    icon_on = None
    other_field_lines = []
    while i < len(lines):
        cl = lines[i]
        if cl.strip() == "":
            # look ahead past blank lines: only consume them if the block continues afterward
            j = i
            while j < len(lines) and lines[j].strip() == "":
                j += 1
            if j < len(lines):
                nm = re.match(r"^(\s*)(\S.*)$", lines[j])
                if len(nm.group(1)) >= len(field_indent):
                    other_field_lines.extend(lines[i:j])
                    i = j
                    continue
            break
        fm = re.match(r"^(\s*)(\S.*)$", cl)
        cur_indent = len(fm.group(1))
        if cur_indent < len(field_indent):
            break
        if cur_indent > len(field_indent):
            other_field_lines.append(cl)
            i += 1
            continue
        keym = re.match(r"^\s*(icon|iconOn|backgroundOn):", cl)
        if keym:
            key = keym.group(1)
            kind, sprite, state, nexti = parse_icon_value(lines, i, field_indent)
            if key == "icon":
                icon = (kind, sprite, state)
            elif key == "iconOn":
                icon_on = (kind, sprite, state)
            i = nexti
            continue
        other_field_lines.append(cl)
        i += 1
    block.extend(other_field_lines)

    sprite_block = []
    if icon is not None or icon_on is not None:
        sprites_present = [e[1] for e in (icon, icon_on) if e and e[0] != "texture"]
        distinct_sprites = set(sprites_present)
        hoistable = len(distinct_sprites) == 1
        hoist_sprite = sprites_present[0] if hoistable else None

        icon_iconon_identical = (icon is not None and icon_on is not None and icon == icon_on)

        def layer_lines(entry, layer_key, add_map, extra=None):
            kind, sprite, state = entry
            ll = []
            if kind == "texture":
                ll.append(f"{field_indent}- texture: {state}")
            else:
                if hoistable:
                    ll.append(f"{field_indent}- state: {state}")
                else:
                    ll.append(f"{field_indent}- sprite: {sprite}")
                    ll.append(f"{field_indent}  state: {state}")
            if add_map:
                ll.append(f"{field_indent}  map: [\"enum.ActionVisuals.{layer_key}\"]")
            if extra:
                for e in extra:
                    ll.append(f"{field_indent}  {e}")
            return ll

        sprite_block = [f"{action_indent}- type: Sprite"]
        if hoistable:
            sprite_block.append(f"{field_indent}sprite: {hoist_sprite}")
        sprite_block.append(f"{field_indent}layers:")

        if icon_iconon_identical:
            sprite_block.extend(layer_lines(icon, "Icon", has_charges))
        elif icon and icon_on:
            sprite_block.extend(layer_lines(icon, "Icon", True))
            sprite_block.extend(layer_lines(icon_on, "IconToggled", True, extra=["visible: false"]))
        elif icon:
            sprite_block.extend(layer_lines(icon, "Icon", has_charges))
        elif icon_on:
            sprite_block.extend(layer_lines(icon_on, "Icon", has_charges))

    return block, sprite_block, i


def process_file(path: Path):
    text = path.read_text(encoding="utf-8")
    lines = text.split("\n")

    # find entity block boundaries: lines matching '- type: entity' at col 0
    entity_starts = [idx for idx, l in enumerate(lines) if re.match(r"^- type: entity\s*$", l)]
    entity_bounds = []
    for k, s in enumerate(entity_starts):
        e = entity_starts[k + 1] if k + 1 < len(entity_starts) else len(lines)
        entity_bounds.append((s, e))

    def has_limited_charges(s, e):
        for idx in range(s, e):
            if re.match(r"^\s*- type: LimitedCharges\s*$", lines[idx]):
                return True
        return False

    out = []
    i = 0
    changed = False
    bound_idx = 0
    cur_bounds = entity_bounds[0] if entity_bounds else (0, len(lines))
    while i < len(lines):
        while bound_idx + 1 < len(entity_bounds) and i >= entity_bounds[bound_idx][1]:
            bound_idx += 1
        cur_bounds = entity_bounds[bound_idx] if entity_bounds else (0, len(lines))

        line = lines[i]
        m = re.match(r"^(\s*)- type: Action\s*$", line)
        if not m:
            out.append(line)
            i += 1
            continue
        action_indent = m.group(1)
        has_charges = has_limited_charges(*cur_bounds)
        block, sprite_block, nexti = process_action_block(lines, i, action_indent, has_charges)
        out.extend(block)
        if sprite_block:
            out.extend(sprite_block)
            changed = True
        i = nexti

    new_text = "\n".join(out)
    if changed:
        path.write_text(new_text, encoding="utf-8")
    return changed


def main():
    files = sys.argv[1:]
    for f in files:
        p = ROOT / f
        if not p.exists():
            print(f"MISSING: {f}")
            continue
        changed = process_file(p)
        print(f"{'CHANGED' if changed else 'unchanged'}: {f}")


if __name__ == "__main__":
    main()
