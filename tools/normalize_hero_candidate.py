from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1] / "art/heroes/Candidates/BaseSouth_v2"
for source in sorted(root.glob("*_south_alpha.png")):
    image = Image.open(source).convert("RGBA")
    box = image.getchannel("A").getbbox()
    if not box:
        continue
    image = image.crop(box)
    ratio = min(78 / image.width, 84 / image.height)
    image = image.resize((max(1, round(image.width*ratio)), max(1, round(image.height*ratio))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    canvas.alpha_composite(image, ((96-image.width)//2, 92-image.height))
    out = source.with_name(source.name.replace("_alpha", "_preview96"))
    canvas.save(out, optimize=True)
    print(out.name, image.size)
