"""Faithful dark-fantasy mockup using the website's gold palette + the user's launcher_bg.png."""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1040, 600
OUT_DIR = "/m"
ROOT = "/r"
FONT_DIR = "/usr/share/fonts/truetype/dejavu"

def font(name, size): return ImageFont.truetype(os.path.join(FONT_DIR, name), size)
F_TITLE  = font("DejaVuSans-Bold.ttf", 52)
F_SUB    = font("DejaVuSans.ttf", 22)
F_STATUS = font("DejaVuSans.ttf", 24)
F_BTN    = font("DejaVuSans-Bold.ttf", 30)
F_SMALL  = font("DejaVuSans.ttf", 22)

GOLD      = (212, 175, 55)     # #d4af37
GOLD_DK   = (166, 138, 45)     # #a68a2d
GOLD_HI   = (255, 215, 0)      # #ffd700
TEXT      = (224, 224, 224)    # #e0e0e0

def cover(im, w, h):
    iw, ih = im.size
    s = max(w/iw, h/ih)
    im = im.resize((int(iw*s), int(ih*s)))
    x = (im.size[0]-w)//2; y = (im.size[1]-h)//2
    return im.crop((x, y, x+w, y+h))

def spaced(draw, x, y, text, fnt, fill, tracking=6, glow=None):
    cx = x
    for ch in text:
        if glow:
            draw.text((cx+1, y+1), ch, font=fnt, fill=glow)
        draw.text((cx, y), ch, font=fnt, fill=fill)
        cx += draw.textlength(ch, font=fnt) + tracking
    return cx

def text_w(draw, text, fnt, tracking=6):
    return sum(draw.textlength(c, font=fnt)+tracking for c in text) - tracking

def hgrad_rect(img, box, c0, c1, radius=10):
    x0,y0,x1,y1 = box; w=x1-x0; h=y1-y0
    grad = Image.new("RGB",(w,1))
    for i in range(w):
        t=i/max(1,w-1)
        grad.putpixel((i,0),tuple(int(c0[k]+(c1[k]-c0[k])*t) for k in range(3)))
    grad=grad.resize((w,h))
    mask=Image.new("L",(w,h),0)
    ImageDraw.Draw(mask).rounded_rectangle([0,0,w-1,h-1],radius=radius,fill=255)
    img.paste(grad,(x0,y0),mask)

# --- background: user image, darkened like the site (rgba(0,0,0,0.5) overlay) ---
bg = cover(Image.open(os.path.join(ROOT,"launcher_bg.png")).convert("RGB"), W, H)
overlay = Image.new("RGB",(W,H),(0,0,0))
img = Image.blend(bg, overlay, 0.5)
d = ImageDraw.Draw(img, "RGBA")

# title glow behind text
glow = Image.new("RGBA",(W,H),(0,0,0,0))
gd = ImageDraw.Draw(glow)
tw = text_w(d, "NAZWA SERWERA", F_TITLE, 8)
gd.text(((W-tw)/2, 78), "NAZWA SERWERA", font=F_TITLE, fill=(255,215,0,110))
glow = glow.filter(ImageFilter.GaussianBlur(14))
img = Image.alpha_composite(img.convert("RGBA"), glow).convert("RGB")
d = ImageDraw.Draw(img, "RGBA")

tw = text_w(d, "NAZWA SERWERA", F_TITLE, 8)
spaced(d, (W-tw)/2, 78, "NAZWA SERWERA", F_TITLE, GOLD, tracking=8, glow=(0,0,0))
sw = text_w(d, "LAUNCHER", F_SUB, 4)
spaced(d, (W-sw)/2, 150, "LAUNCHER", F_SUB, (180,170,140), tracking=4)

# bottom panel: rgba(20,20,20,0.7) with 4px gold top border (like .container)
panel_top = 330
d.rectangle([0, panel_top, W, H], fill=(20,20,20,180))
d.rectangle([0, panel_top, W, panel_top+4], fill=GOLD+(255,))

# status + progress
d.text((60, 372), "Pobieranie 12/40  (340 MB / 1.1 GB)", font=F_STATUS, fill=TEXT)
px,py,pw,ph = 60, 418, W-120, 22
d.rounded_rectangle([px,py,px+pw,py+ph], ph//2, fill=(45,40,28,255))
fill_w = int(pw*0.62)
hgrad_rect(img, [px,py,px+fill_w,py+ph], GOLD, GOLD_DK, radius=ph//2)
d = ImageDraw.Draw(img, "RGBA")

# buttons
d.rounded_rectangle([W-470, 500, W-300, 556], 8, outline=GOLD, width=2)
rw = text_w(d, "Ponów", F_SMALL, 0)
d.text((W-385-rw/2, 514), "Ponów", font=F_SMALL, fill=GOLD)
hgrad_rect(img, [W-280, 492, W-60, 560], GOLD, GOLD_DK, radius=8)
d = ImageDraw.Draw(img, "RGBA")
bw = text_w(d, "GRAJ", F_BTN, 2)
spaced(d, W-170-bw/2, 506, "GRAJ", F_BTN, (20,15,5), tracking=2)

img.save(os.path.join(OUT_DIR,"1-dark-fantasy-gold.png"))
img.save(os.path.join(ROOT,"1-dark-fantasy-gold.png"))
print("done")
