from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "art" / "heroes" / "Approved" / "v2"
DESTINATION = ROOT / "UnityProject" / "Assets" / "Resources" / "Art" / "Generated"
HEROES = ("amelia", "sam", "zike")
# PNG rows from top to bottom, matching DirectionalSpriteVisual.PackedRowsForRuntime.
DIRECTIONS = ("east", "south-east", "south", "south-west", "west", "north-west", "north", "north-east")
CELL = 96
FRAMES = range(1, 7)  # frame_000 is the approved idle reference, not a walk phase.


def main():
    for hero in HEROES:
        animation = SOURCE / f"{hero}_walk_v2_96"
        sheet = Image.new("RGBA", (CELL * len(FRAMES), CELL * len(DIRECTIONS)), (0, 0, 0, 0))
        for row, direction in enumerate(DIRECTIONS):
            for column, frame in enumerate(FRAMES):
                path = animation / direction / f"frame_{frame:03d}.png"
                image = Image.open(path).convert("RGBA")
                if image.size != (CELL, CELL):
                    raise ValueError(f"{path}: expected {CELL}x{CELL}, got {image.size}")
                sheet.alpha_composite(image, (column * CELL, row * CELL))

        output = DESTINATION / f"hero_{hero}_canonical__move__8dir__7fps__loop.png"
        output.parent.mkdir(parents=True, exist_ok=True)
        sheet.save(output)
        print(f"{hero}: {sheet.size[0]}x{sheet.size[1]} -> {output}")


if __name__ == "__main__":
    main()
