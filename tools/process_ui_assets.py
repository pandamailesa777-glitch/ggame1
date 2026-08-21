from pathlib import Path
from PIL import Image
import shutil

ROOT = Path(__file__).resolve().parents[1]
GEN = Path(r"C:\Users\PandaWork\.codex\generated_images\01a01f04-993d-7a63-9add-81bbc4e0547b")
SOURCE = ROOT / "art" / "ui" / "source"
UNITY = ROOT / "UnityProject" / "Assets" / "Resources" / "Art" / "UI"

FILES = {
    "exec-288c9967-8070-4fc7-b000-fef6e07f29dd.png": "ui_menu_background_v1.png",
    "exec-cb21e2be-1ecc-400d-9054-00d909794ce7.png": "ui_panel_frame_source_v1.png",
    "exec-39d9ed68-6f21-4907-952a-8c38b84b0a10.png": "ui_button_plate_source_v1.png",
    "exec-a76ca7c6-cfbc-4113-a0c0-0b2544719f1e.png": "ui_card_frame_source_v1.png",
}

def chroma_alpha(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pixels = []
    for r, g, b, _ in image.getdata():
        # The generator's key is close to pure green, with slight antialiasing.
        dominance = g - max(r, b)
        if g > 150 and dominance > 55:
            alpha = max(0, min(255, int(255 * (1 - (dominance - 55) / 155))))
            pixels.append((r, g, b, alpha))
        else:
            pixels.append((r, g, b, 255))
    image.putdata(pixels)
    return image

def alpha_bounds(image: Image.Image, threshold: int = 12):
    return image.getchannel("A").point(lambda a: 255 if a > threshold else 0).getbbox()

def trim_and_fit(image: Image.Image, size: tuple[int, int], padding: int = 8) -> Image.Image:
    box = alpha_bounds(image)
    if box:
        image = image.crop(box)
    max_w, max_h = size[0] - padding * 2, size[1] - padding * 2
    scale = min(max_w / image.width, max_h / image.height)
    fitted = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((size[0] - fitted.width) // 2, (size[1] - fitted.height) // 2))
    return canvas

SOURCE.mkdir(parents=True, exist_ok=True)
UNITY.mkdir(parents=True, exist_ok=True)
for src, dst in FILES.items():
    shutil.copy2(GEN / src, SOURCE / dst)

bg = Image.open(SOURCE / "ui_menu_background_v1.png").convert("RGB")
target_ratio = 16 / 9
crop_h = round(bg.width / target_ratio)
top = max(0, (bg.height - crop_h) // 2)
bg = bg.crop((0, top, bg.width, top + crop_h)).resize((1920, 1080), Image.Resampling.LANCZOS)
bg.save(UNITY / "ui_menu_background_v1.png", optimize=True)

panel = trim_and_fit(chroma_alpha(Image.open(SOURCE / "ui_panel_frame_source_v1.png")), (1024, 640), 8)
panel.save(UNITY / "ui_panel_frame_v1.png", optimize=True)
button = trim_and_fit(chroma_alpha(Image.open(SOURCE / "ui_button_plate_source_v1.png")), (1024, 256), 8)
button.save(UNITY / "ui_button_plate_v1.png", optimize=True)
card = trim_and_fit(chroma_alpha(Image.open(SOURCE / "ui_card_frame_source_v1.png")), (640, 960), 8)
card.save(UNITY / "ui_card_frame_v1.png", optimize=True)

print(UNITY)
