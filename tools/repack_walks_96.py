from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1] / "art" / "heroes" / "Approved" / "v2"
HEROES = ("amelia", "sam", "zike")
TARGET = 96


def main():
    for hero in HEROES:
        source = ROOT / f"{hero}_walk_v2_audit" / "Idle" / "animations" / "walk_v2_audited"
        destination = ROOT / f"{hero}_walk_v2_96"
        if not source.exists():
            continue

        written = 0
        for frame_path in sorted(source.glob("*/*.png")):
            image = Image.open(frame_path).convert("RGBA")
            width, height = image.size
            if width < TARGET or height < TARGET:
                raise ValueError(f"{frame_path}: canvas {image.size} is smaller than {TARGET}x{TARGET}")

            left = (width - TARGET) // 2
            top = (height - TARGET) // 2
            packed = image.crop((left, top, left + TARGET, top + TARGET))

            # Cropping transparent padding must never remove character pixels.
            original_alpha = sum(image.getchannel("A").histogram()[1:])
            packed_alpha = sum(packed.getchannel("A").histogram()[1:])
            if original_alpha != packed_alpha:
                raise ValueError(
                    f"{frame_path}: centered crop would clip {original_alpha - packed_alpha} opaque pixels"
                )

            output = destination / frame_path.parent.name / frame_path.name
            output.parent.mkdir(parents=True, exist_ok=True)
            packed.save(output)
            written += 1

        print(f"{hero}: wrote {written} frames to {destination}")


if __name__ == "__main__":
    main()
