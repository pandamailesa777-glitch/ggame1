import json
import math
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "UnityProject/Assets/Resources/Art/Maps/PixelLab"
OUTPUT = ROOT / "UnityProject/Assets/Resources/Art/Maps"
COLS, ROWS = 40, 24


def terrain(variant: str, x: int, y: int) -> str:
    cx, cy = COLS / 2, ROWS / 2
    if variant == "forest":
        winding_path = abs(y - cy - math.sin(x * .34) * 2.1) < 1.35
        western_ruin = ((x - 9) / 5.0) ** 2 + ((y - 7) / 3.5) ** 2 < 1
        eastern_ruin = ((x - 31) / 5.5) ** 2 + ((y - 17) / 3.8) ** 2 < 1
        return "upper" if winding_path or western_ruin or eastern_ruin else "lower"
    ritual_cross = abs(x - cx) < 2.0 or abs(y - cy) < 1.5
    ring = 5.4 < math.hypot((x - cx) * .72, y - cy) < 7.4
    side_altar = ((x - 8) / 4.2) ** 2 + ((y - 17) / 3.0) ** 2 < 1
    return "upper" if ritual_cross or ring or side_altar else "lower"


def build(variant: str) -> None:
    with (SOURCE / f"{variant}_tileset.json").open(encoding="utf-8") as handle:
        metadata = json.load(handle)
    sheet = Image.open(SOURCE / f"{variant}_tileset.png").convert("RGBA")
    lookup = {}
    for tile in metadata["tileset_data"]["tiles"]:
        corners = tile["corners"]
        key = (corners["NW"], corners["NE"], corners["SW"], corners["SE"])
        if "transition" not in key and key not in lookup:
            box = tile["bounding_box"]
            lookup[key] = sheet.crop((box["x"], box["y"], box["x"] + box["width"], box["y"] + box["height"]))

    vertices = [[terrain(variant, x, y) for x in range(COLS + 1)] for y in range(ROWS + 1)]
    canvas = Image.new("RGBA", (COLS * 64, ROWS * 64))
    for y in range(ROWS):
        for x in range(COLS):
            key = (vertices[y][x], vertices[y][x + 1], vertices[y + 1][x], vertices[y + 1][x + 1])
            tile = lookup.get(key) or lookup[("lower",) * 4]
            canvas.paste(tile, (x * 64, y * 64))
    canvas.save(OUTPUT / f"{variant}_arena_v1.png", optimize=True)


for map_variant in ("forest", "moon"):
    build(map_variant)
