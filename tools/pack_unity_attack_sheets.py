from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "art" / "heroes" / "Approved" / "v2"
ATTACK_V3 = ROOT / "art" / "heroes" / "AttackV3Audit" / "amelia_rootlocked_8dir"
DESTINATION = ROOT / "UnityProject" / "Assets" / "Resources" / "Art" / "Generated"
DIRECTIONS = ("east", "south-east", "south", "south-west", "west", "north-west", "north", "north-east")
TARGET = 128
# Every direction and every frame must share the same gameplay root.  The old
# packer stabilized a direction against its own idle frame, which left eight
# different roots and made direction changes look like an orbit around the
# character transform.
SHARED_ROOT = (TARGET // 2, TARGET - 11)


def source_for(hero: str, direction: str) -> Path:
    if hero == "amelia" and direction == "north-east":
        return ART / "amelia_attack_body_v2_clean_ne_fix_audit" / "Idle" / "animations" / "attack_body_v2_clean_ne_fix"
    if hero == "zike" and direction in ("north", "north-west"):
        return ART / "zike_attack_body_v2_clean_north_fix_audit" / "Idle" / "animations" / "attack_body_v2_clean_north_fix"
    return ART / f"{hero}_attack_body_v2_clean_audit" / "Idle" / "animations" / "attack_body_v2_clean"


def remove_detached_yellow_specks(image: Image.Image) -> Image.Image:
    pixels = image.load()
    alpha = image.getchannel("A")
    seen = set()
    for y in range(image.height):
        for x in range(image.width):
            if (x, y) in seen or alpha.getpixel((x, y)) < 16:
                continue
            queue = deque([(x, y)])
            seen.add((x, y))
            component = []
            while queue:
                px, py = queue.popleft()
                component.append((px, py))
                for nx in range(max(0, px - 1), min(image.width, px + 2)):
                    for ny in range(max(0, py - 1), min(image.height, py + 2)):
                        if (nx, ny) not in seen and alpha.getpixel((nx, ny)) >= 16:
                            seen.add((nx, ny))
                            queue.append((nx, ny))
            if len(component) >= 20:
                continue
            colors = [pixels[px, py] for px, py in component]
            if all(r > 135 and g > 85 and b < 95 for r, g, b, _ in colors):
                for px, py in component:
                    pixels[px, py] = (0, 0, 0, 0)
    return image


def padded_frame(path: Path) -> Image.Image:
    image = remove_detached_yellow_specks(Image.open(path).convert("RGBA"))
    if image.width > TARGET or image.height > TARGET:
        content = image.getchannel("A").getbbox()
        if content is not None and content[2] - content[0] <= TARGET and content[3] - content[1] <= TARGET:
            image = image.crop(content)
        else:
            raise ValueError(
                f"source frame exceeds {TARGET}x{TARGET} and would be clipped: "
                f"{path} is {image.width}x{image.height}"
            )
    packed = Image.new("RGBA", (TARGET, TARGET), (0, 0, 0, 0))
    packed.alpha_composite(image, ((TARGET - image.width) // 2, (TARGET - image.height) // 2))
    return packed


def root_anchor(image: Image.Image) -> tuple[float, int]:
    alpha = image.getchannel("A")
    box = alpha.getbbox()
    if box is None:
        raise ValueError("empty attack frame")
    feet_y = box[3]
    lower_top = max(box[1], feet_y - 18)
    points = [(x, y) for y in range(lower_top, feet_y) for x in range(box[0], box[2]) if alpha.getpixel((x, y)) >= 32]
    xs = sorted(x for x, _ in points)
    return (xs[len(xs) // 2], feet_y)


def stabilize(image: Image.Image, reference: tuple[float, int]) -> Image.Image:
    anchor = root_anchor(image)
    dx = round(reference[0] - anchor[0])
    dy = reference[1] - anchor[1]
    result = Image.new("RGBA", (TARGET, TARGET), (0, 0, 0, 0))
    result.alpha_composite(image, (dx, dy))
    if sum(image.getchannel("A").histogram()[1:]) != sum(result.getchannel("A").histogram()[1:]):
        raise ValueError(f"stabilization clips pixels: shift=({dx},{dy})")
    return result


def main():
    for hero in ("amelia", "sam", "zike"):
        frame_indices = range(5) if hero == "amelia" else range(1, 7)
        sheet = Image.new("RGBA", (TARGET * len(frame_indices), TARGET * 8), (0, 0, 0, 0))
        for row, direction in enumerate(DIRECTIONS):
            source = ATTACK_V3 if hero == "amelia" else source_for(hero, direction)
            for column, frame_index in enumerate(frame_indices):
                path = source / direction / f"{frame_index}.png" if hero == "amelia" else source / direction / f"frame_{frame_index:03d}.png"
                packed = stabilize(padded_frame(path), SHARED_ROOT)
                sheet.alpha_composite(packed, (column * TARGET, row * TARGET))
        output = DESTINATION / f"hero_{hero}_attack_8dir.png"
        sheet.save(output)
        print(f"{hero}: {sheet.size} -> {output}")


if __name__ == "__main__":
    main()
