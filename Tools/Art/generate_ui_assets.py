"""Generate deterministic, original UI raster assets used by the scene builder."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets" / "UI" / "CustomerSpeechBubble.png"
SCALE = 4
WIDTH, HEIGHT = 600, 250
CONTENT_SCALE_X = 0.90
CONTENT_SCALE_Y = 0.78
CONTENT_OFFSET_X = 20
CONTENT_OFFSET_Y = 25


def scaled_box(box):
    left, top, right, bottom = box
    return (
        int((left * CONTENT_SCALE_X + CONTENT_OFFSET_X) * SCALE),
        int((top * CONTENT_SCALE_Y + CONTENT_OFFSET_Y) * SCALE),
        int((right * CONTENT_SCALE_X + CONTENT_OFFSET_X) * SCALE),
        int((bottom * CONTENT_SCALE_Y + CONTENT_OFFSET_Y) * SCALE),
    )


def create_speech_bubble():
    size = (WIDTH * SCALE, HEIGHT * SCALE)
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)

    # One unioned silhouette: scallops affect only the outside boundary, so
    # there can never be dark overlap lines behind the dialogue text.
    draw.rounded_rectangle(scaled_box((35, 58, 510, 190)), radius=39 * SCALE, fill=255)
    for cx, cy, radius in (
        (76, 77, 45), (145, 60, 48), (222, 59, 52), (304, 58, 55),
        (389, 62, 50), (462, 79, 46), (92, 167, 40), (176, 184, 45),
        (274, 186, 48), (372, 182, 44), (454, 167, 42),
    ):
        draw.ellipse(scaled_box((cx - radius, cy - radius, cx + radius, cy + radius)), fill=255)

    # Two detached thought-cloud dots point toward the speaking customer.
    draw.ellipse(scaled_box((512, 180, 550, 218)), fill=255)
    draw.ellipse(scaled_box((558, 216, 580, 238)), fill=255)

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
