from pathlib import Path
from PIL import Image

from pack_unity_attack_sheets import TARGET, SHARED_ROOT, padded_frame, stabilize


ROOT = Path(__file__).resolve().parents[1]
DESTINATION = ROOT / "UnityProject" / "Assets" / "Resources" / "Art" / "Generated"
DIRECTIONS = ("east", "south-east", "south", "south-west", "west", "north-west", "north", "north-east")

SOURCES = {
    "enemy_vampire": ROOT / "art" / "enemies" / "WalkAudit" / "vampire" / "extract" / "Idle" / "animations" / "walk_v3_rootlocked",
    "enemy_bandit": ROOT / "art" / "enemies" / "WalkAudit" / "bandit" / "extract" / "Idle" / "animations" / "walk_v3_rootlocked",
}


def pack(enemy_id: str, source: Path) -> Path:
    frames = 7
    sheet = Image.new("RGBA", (TARGET * frames, TARGET * 8), (0, 0, 0, 0))
    for row, direction in enumerate(DIRECTIONS):
        for column in range(frames):
            path = source / direction / f"frame_{column:03d}.png"
            packed = stabilize(padded_frame(path), SHARED_ROOT)
            sheet.alpha_composite(packed, (column * TARGET, row * TARGET))
    output = DESTINATION / f"{enemy_id}_move_8dir.png"
    sheet.save(output)
    return output


def main():
    for enemy_id, source in SOURCES.items():
        if not source.exists():
            print(f"skip {enemy_id}: {source}")
            continue
        print(f"{enemy_id}: {pack(enemy_id, source)}")


if __name__ == "__main__":
    main()
