"""Generate deterministic, original UI raster assets used by the scene builder."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets" / "UI" / "CustomerSpeechBubble.png"
SCALE = 4
WIDTH, HEIGHT = 600, 220


def scaled_box(box):
    return tuple(int(value * SCALE) for value in box)


def create_speech_bubble():
    size = (WIDTH * SCALE, HEIGHT * SCALE)
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)

    # One unioned silhouette: scallops affect only the outside boundary, so
    # there can never be dark overlap lines behind the dialogue text.
    draw.rounded_rectangle(scaled_box((35, 36, 510, 168)), radius=48 * SCALE, fill=255)
    for cx, cy, radius in (
        (76, 55, 45), (145, 38, 48), (222, 37, 52), (304, 36, 55),
        (389, 40, 50), (462, 57, 46), (92, 145, 40), (176, 162, 45),
        (274, 164, 48), (372, 160, 44), (454, 145, 42),
    ):
        draw.ellipse(scaled_box((cx - radius, cy - radius, cx + radius, cy + radius)), fill=255)

    # Two detached thought-cloud dots point toward the speaking customer.
    draw.ellipse(scaled_box((512, 158, 550, 196)), fill=255)
    draw.ellipse(scaled_box((558, 194, 580, 216)), fill=255)

    dilation = mask.filter(ImageFilter.MaxFilter(25))
    shadow = dilation.filter(ImageFilter.GaussianBlur(8 * SCALE))
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.paste((0, 0, 0, 70), (4 * SCALE, 7 * SCALE), shadow)
    canvas.paste((30, 34, 36, 255), (0, 0), dilation)
    canvas.paste((255, 250, 232, 255), (0, 0), mask)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS).save(OUTPUT, optimize=True)
    print(f"Generated {OUTPUT}")


if __name__ == "__main__":
    create_speech_bubble()
