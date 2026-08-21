from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / "UnityProject/Assets/Resources/Art/Weapons"

for path in ROOT.glob("weapon_*_v1.png"):
    image = Image.open(path).convert("RGBA")
    alpha = image.getchannel("A")
    box = alpha.getbbox()
    if box:
        image = image.crop(box)
    image.thumbnail((384, 384), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (384, 384), (0, 0, 0, 0))
    canvas.alpha_composite(image, ((384-image.width)//2, (384-image.height)//2))
    canvas.save(path, optimize=True)
    print(path.name, image.size)
