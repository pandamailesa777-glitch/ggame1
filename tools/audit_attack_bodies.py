import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1] / "art" / "heroes" / "Approved" / "v2"
HEROES = ("amelia", "sam", "zike")
DIRECTIONS = ("east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east")
TARGET = 128
SCALE = 2


def main():
    report = {}
    rows = []
    for hero in HEROES:
        clean_source = ROOT / f"{hero}_attack_body_v2_clean_audit" / "Idle" / "animations" / "attack_body_v2_clean"
        source = clean_source if clean_source.exists() else ROOT / f"{hero}_attack_body_v1_audit" / "Idle" / "animations" / "attack_body_v1_audit"
        destination = ROOT / f"{hero}_attack_body_v2_128" if source == clean_source else ROOT / f"{hero}_attack_body_v1_128"
        if not source.exists():
            continue
        hero_report = {}
        for direction in DIRECTIONS:
            frames = []
            measurements = []
            for frame_index in range(7):
                path = source / direction / f"frame_{frame_index:03d}.png"
                image = Image.open(path).convert("RGBA")
                width, height = image.size
                left, top = (TARGET - width) // 2, (TARGET - height) // 2
                packed = Image.new("RGBA", (TARGET, TARGET), (0, 0, 0, 0))
                packed.alpha_composite(image, (left, top))
                if sum(image.getchannel("A").histogram()[1:]) != sum(packed.getchannel("A").histogram()[1:]):
                    raise ValueError(f"{path}: centered crop clips character pixels")
                output = destination / direction / path.name
                output.parent.mkdir(parents=True, exist_ok=True)
                packed.save(output)
                box = packed.getchannel("A").getbbox()
                measurements.append({"box": list(box), "center_x": (box[0] + box[2]) / 2, "feet_y": box[3]})
                frames.append(packed)
            hero_report[direction] = measurements
            rows.append((hero, direction, frames))
        report[hero] = hero_report

    label_width = 150
    sheet = Image.new("RGBA", (label_width + TARGET * 7 * SCALE, max(1, len(rows)) * TARGET * SCALE), (11, 14, 18, 255))
    draw = ImageDraw.Draw(sheet)
    for row_index, (hero, direction, frames) in enumerate(rows):
        y = row_index * TARGET * SCALE
        draw.text((8, y + 8), f"{hero} / {direction}", fill=(230, 236, 240, 255))
        for column, frame in enumerate(frames):
            sheet.alpha_composite(frame.resize((TARGET * SCALE, TARGET * SCALE), Image.Resampling.NEAREST), (label_width + column * TARGET * SCALE, y))
    sheet.save(ROOT / "approved_v2_attack_body_audit_2x.png")
    (ROOT / "approved_v2_attack_body_audit.json").write_text(json.dumps(report, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
