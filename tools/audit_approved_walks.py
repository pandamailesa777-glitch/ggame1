import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1] / "art" / "heroes" / "Approved" / "v2"
HEROES = ("amelia", "sam", "zike")
DIRECTIONS = ("east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east")
CELL = 96
SCALE = 2


def bounds(image: Image.Image):
    box = image.getchannel("A").getbbox()
    if box is None:
        return None
    return {
        "box": list(box),
        "width": box[2] - box[0],
        "height": box[3] - box[1],
        "center_x": (box[0] + box[2]) / 2,
        "feet_y": box[3],
    }


def main():
    available = {}
    reports = {}
    rows = []

    for hero in HEROES:
        packed_root = ROOT / f"{hero}_walk_v2_96"
        v2_root = ROOT / f"{hero}_walk_v2_audit" / "Idle" / "animations" / "walk_v2_audited"
        if packed_root.exists():
            animation_root = packed_root
        else:
            animation_root = v2_root if v2_root.exists() else ROOT / f"{hero}_walk_audit" / "Idle" / "animations" / "approved_walk"
        frame_count = 7 if animation_root in (packed_root, v2_root) else 6
        hero_report = {}
        for direction in DIRECTIONS:
            frame_paths = [animation_root / direction / f"frame_{frame:03d}.png" for frame in range(frame_count)]
            if not all(path.exists() for path in frame_paths):
                continue
            available.setdefault(hero, []).append(direction)
            frames = [Image.open(path).convert("RGBA") for path in frame_paths]
            measurements = [bounds(frame) for frame in frames]
            hero_report[direction] = measurements
            strip = Image.new("RGBA", (CELL * frame_count, CELL), (16, 19, 24, 255))
            for index, frame in enumerate(frames):
                strip.alpha_composite(frame, (index * CELL, 0))
            rows.append((hero, direction, strip))
        reports[hero] = hero_report

    label_width = 150
    row_height = CELL * SCALE
    max_frames = max((strip.width // CELL for _, _, strip in rows), default=1)
    sheet = Image.new("RGBA", (label_width + CELL * max_frames * SCALE, max(1, len(rows)) * row_height), (11, 14, 18, 255))
    draw = ImageDraw.Draw(sheet)
    for row_index, (hero, direction, strip) in enumerate(rows):
        y = row_index * row_height
        draw.text((8, y + 8), f"{hero} / {direction}", fill=(230, 236, 240, 255))
        sheet.alpha_composite(strip.resize((strip.width * SCALE, row_height), Image.Resampling.NEAREST), (label_width, y))

    sheet.save(ROOT / "approved_v2_walk_audit_2x.png")
    (ROOT / "approved_v2_walk_audit.json").write_text(
        json.dumps({"available": available, "measurements": reports}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
