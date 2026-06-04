"""Generate preview mockups of the launcher window for the three visual directions.
Run inside a python+Pillow container. Outputs PNGs into the same folder."""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1040, 600          # 2x of the real 520x300 window
OUT = os.path.dirname(os.path.abspath(__file__))

FONT_DIR = "/usr/share/fonts/truetype/dejavu"
def font(name, size):
    return ImageFont.truetype(os.path.join(FONT_DIR, name), size)

F_TITLE   = font("DejaVuSerif-Bold.ttf", 60)
F_TITLE2  = font("DejaVuSans-Bold.ttf", 54)
F_SUB     = font("DejaVuSans.ttf", 26)
F_STATUS  = font("DejaVuSans.ttf", 24)
F_BTN     = font("DejaVuSans-Bold.ttf", 30)
F_SMALL   = font("DejaVuSans.ttf", 20)

def vgrad(size, top, bottom):
    w, h = size
    base = Image.new("RGB", (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        base.putpixel((0, y), tuple(int(top[i] + (bottom[i]-top[i])*t) for i in range(3)))
    return base.resize((w, h))

def rrect(draw, box, r, **kw):
    draw.rounded_rectangle(box, radius=r, **kw)

def progress(draw, x, y, w, h, pct, track, fill, glow=None):
    rrect(draw, [x, y, x+w, y+h], h//2, fill=track)
    fw = int(w * pct)
    if fw > h:
        if glow:
            rrect(draw, [x, y, x+fw, y+h], h//2, fill=glow)
        rrect(draw, [x, y, x+fw, y+h], h//2, fill=fill)

def center_text(draw, cx, y, text, fnt, fill, shadow=None):
    bb = draw.textbbox((0,0), text, font=fnt)
    tw = bb[2]-bb[0]
    if shadow:
        draw.text((cx-tw/2+2, y+2), text, font=fnt, fill=shadow)
    draw.text((cx-tw/2, y), text, font=fnt, fill=fill)

# ---------------------------------------------------------------- 1. Dark fantasy
def dark_fantasy():
    img = vgrad((W, H), (28, 20, 12), (10, 7, 5)).convert("RGB")
    d = ImageDraw.Draw(img, "RGBA")
    # vignette-ish darker corners + faint "castle" silhouette band
    d.rectangle([0, 0, W, 8], fill=(180, 140, 70))           # top gold rule
    d.rectangle([0, H-8, W, H], fill=(120, 90, 40))          # bottom rule
    # faint emblem glow behind title
    glow = Image.new("RGBA", (W, H), (0,0,0,0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([W/2-260, 70, W/2+260, 250], fill=(150, 110, 40, 90))
    glow = glow.filter(ImageFilter.GaussianBlur(60))
    img.paste(Image.alpha_composite(img.convert("RGBA"), glow).convert("RGB"), (0,0))
    d = ImageDraw.Draw(img, "RGBA")
    gold = (224, 184, 96)
    center_text(d, W/2, 70, "⚔  NAZWA SERWERA  ⚔", F_TITLE, gold, shadow=(0,0,0))
    center_text(d, W/2, 150, "powered by MuMain", F_SMALL, (150, 130, 95))
    # status + progress
    d.text((60, 360), "Pobieranie 12/40  (340 MB / 1.1 GB)", font=F_STATUS, fill=(210, 198, 180))
    progress(d, 60, 410, W-120, 26, 0.62, (50, 40, 28, 255), (228, 178, 86, 255), glow=(255, 210, 120, 120))
    # buttons
    d.rounded_rectangle([W-470, 500, W-300, 556], 8, outline=(150,120,70), width=2)
    center_text(d, W-385, 510, "Ponów", F_SMALL, (200,180,140))
    rrect(d, [W-280, 492, W-60, 560], 10, fill=(214, 164, 70))
    center_text(d, W-170, 506, "GRAJ", F_BTN, (30, 20, 8))
    img.save(os.path.join(OUT, "1-dark-fantasy.png"))

# ---------------------------------------------------------------- 2. Modern dark minimal
def modern_dark():
    img = vgrad((W, H), (32, 34, 40), (20, 21, 26)).convert("RGB")
    d = ImageDraw.Draw(img, "RGBA")
    accent = (88, 166, 255)
    d.rectangle([0, 0, 6, H], fill=accent)                   # left accent strip
    d.text((60, 80), "NAZWA SERWERA", font=F_TITLE2, fill=(238, 240, 244))
    d.text((62, 150), "Launcher", font=F_SUB, fill=(140, 146, 158))
    d.text((60, 360), "Pobieranie 12/40  (340 MB / 1.1 GB)", font=F_STATUS, fill=(176, 182, 194))
    progress(d, 60, 410, W-120, 18, 0.62, (48, 51, 60, 255), accent+(255,))
    d.rounded_rectangle([W-470, 500, W-300, 556], 8, outline=(90, 96, 110), width=2)
    center_text(d, W-385, 510, "Ponów", F_SMALL, (170, 176, 190))
    rrect(d, [W-280, 492, W-60, 560], 8, fill=accent)
    center_text(d, W-170, 506, "GRAJ", F_BTN, (10, 24, 44))
    img.save(os.path.join(OUT, "2-modern-dark.png"))

# ---------------------------------------------------------------- 3. Light modern
def light_modern():
    img = vgrad((W, H), (248, 249, 251), (228, 232, 238)).convert("RGB")
    d = ImageDraw.Draw(img, "RGBA")
    accent = (37, 99, 235)
    d.rectangle([0, 0, W, 6], fill=accent)
    d.text((60, 80), "NAZWA SERWERA", font=F_TITLE2, fill=(28, 32, 40))
    d.text((62, 150), "Launcher", font=F_SUB, fill=(110, 118, 130))
    d.text((60, 360), "Pobieranie 12/40  (340 MB / 1.1 GB)", font=F_STATUS, fill=(70, 78, 92))
    progress(d, 60, 410, W-120, 18, 0.62, (210, 216, 224, 255), accent+(255,))
    d.rounded_rectangle([W-470, 500, W-300, 556], 8, outline=(180, 188, 200), width=2)
    center_text(d, W-385, 510, "Ponów", F_SMALL, (90, 98, 112))
    rrect(d, [W-280, 492, W-60, 560], 8, fill=accent)
    center_text(d, W-170, 506, "GRAJ", F_BTN, (255, 255, 255))
    img.save(os.path.join(OUT, "3-light-modern.png"))

dark_fantasy()
modern_dark()
light_modern()
print("done")
