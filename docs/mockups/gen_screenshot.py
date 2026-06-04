"""Render the final launcher look as a documentation screenshot (docs/assets/screenshot.png).
Faithful approximation of MainWindow.axaml: shifted art, 25% dim, lowered gold title,
near-opaque bottom panel with 1px gold separator, gold progress + GRAJ/Ponów, caption —/×,
square gold frame."""
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 1520, 950
SRC = "/r/_mubg.jpg"
OUT = "/d/assets/screenshot.png"
FONT_DIR = "/usr/share/fonts/truetype/dejavu"
def font(n, s): return ImageFont.truetype(os.path.join(FONT_DIR, n), s)
F_TITLE = font("DejaVuSans-Bold.ttf", 68); F_SUB = font("DejaVuSans.ttf", 26)
F_STATUS = font("DejaVuSans.ttf", 30); F_BTN = font("DejaVuSans-Bold.ttf", 34)
F_GHOST = font("DejaVuSans.ttf", 30); F_CAP = font("DejaVuSans.ttf", 40)
GOLD = (212, 175, 55); GOLD_DK = (166, 138, 45); TEXT = (224, 224, 224); DIM = (180, 170, 140); BTNTXT = (26, 20, 8)

def cover(im, w, h):
    iw, ih = im.size; s = max(w/iw, h/ih); im = im.resize((int(iw*s), int(ih*s)))
    x = (im.size[0]-w)//2; y = (im.size[1]-h)//2 + 160; return im.crop((x, y, x+w, y+h))
def tw(d, t, f, tr): return sum(d.textlength(c, font=f)+tr for c in t)-tr
def spaced(d, x, y, t, f, fill, tr, glow=None):
    cx = x
    for c in t:
        if glow: d.text((cx+1, y+1), c, font=f, fill=glow)
        d.text((cx, y), c, font=f, fill=fill); cx += d.textlength(c, font=f)+tr
def hgrad(img, box, c0, c1, r):
    x0, y0, x1, y1 = box; w = x1-x0; h = y1-y0; g = Image.new("RGB", (w, 1))
    for i in range(w):
        t = i/max(1, w-1); g.putpixel((i, 0), tuple(int(c0[k]+(c1[k]-c0[k])*t) for k in range(3)))
    g = g.resize((w, h)); m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w-1, h-1], radius=r, fill=255); img.paste(g, (x0, y0), m)

img = cover(Image.open(SRC).convert("RGB"), W, H)
d = ImageDraw.Draw(img, "RGBA")
d.rectangle([0, 0, W, H], fill=(0, 0, 0, 64))                                    # 25% art dim
scrim_h = 300; sc = Image.new("RGBA", (W, scrim_h), (0, 0, 0, 0)); sd = ImageDraw.Draw(sc)
for y in range(scrim_h): sd.line([(0, y), (W, y)], fill=(0, 0, 0, int(0xCC*(1-y/scrim_h))))
img.paste(Image.alpha_composite(img.crop((0, 0, W, scrim_h)).convert("RGBA"), sc).convert("RGB"), (0, 0))
d = ImageDraw.Draw(img, "RGBA")
# caption buttons (top-right): minimise + close
d.text((W-128, 16), "—", font=F_CAP, fill=GOLD); d.text((W-64, 16), "✕", font=F_CAP, fill=GOLD)
# title + subtitle (lowered)
w1 = tw(d, "NAZWA SERWERA", F_TITLE, 12); spaced(d, (W-w1)/2, 116, "NAZWA SERWERA", F_TITLE, GOLD, 12, glow=(0, 0, 0))
w2 = tw(d, "LAUNCHER", F_SUB, 10); spaced(d, (W-w2)/2, 206, "LAUNCHER", F_SUB, DIM, 10)
# bottom panel
ptop = H-250
d.rectangle([0, ptop, W, H], fill=(20, 20, 20, 242)); d.rectangle([0, ptop, W, ptop+2], fill=GOLD+(255,))
d.text((56, ptop+30), "Pobieranie 12/40  (340 MB / 1.1 GB)", font=F_STATUS, fill=TEXT)
px, py, pw, ph = 56, ptop+78, W-112, 28
d.rounded_rectangle([px, py, px+pw, py+ph], 14, fill=(0, 0, 0, 150))
hgrad(img, [px, py, px+int(pw*0.62), py+ph], GOLD, GOLD_DK, 14); d = ImageDraw.Draw(img, "RGBA")
gx1 = W-56; gy0 = ptop+142; gy1 = ptop+204; pwb = 300; px0 = gx1-pwb
hgrad(img, [px0, gy0, gx1, gy1], GOLD, GOLD_DK, 8); d = ImageDraw.Draw(img, "RGBA")
bw = tw(d, "GRAJ", F_BTN, 2); spaced(d, px0+(pwb-bw)/2, gy0+12, "GRAJ", F_BTN, BTNTXT, 2)
rx1 = px0-24; rx0 = rx1-180; d.rounded_rectangle([rx0, gy0, rx1, gy1], 8, outline=GOLD, width=3)
rw = tw(d, "Ponów", F_GHOST, 0); d.text((rx0+(180-rw)/2, gy0+14), "Ponów", font=F_GHOST, fill=GOLD)
d.rectangle([0, 0, W-1, H-1], outline=GOLD, width=4)                             # square gold frame
img.save(OUT)
print("saved", OUT)
