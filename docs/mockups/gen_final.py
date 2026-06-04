"""Faithful preview of the implemented MainWindow.axaml layout at 760x475 (rendered 2x)."""
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 1520, 950           # 2x of 760x475
OUT_DIR, ROOT = "/m", "/r"
FONT_DIR = "/usr/share/fonts/truetype/dejavu"
def font(n,s): return ImageFont.truetype(os.path.join(FONT_DIR,n),s)
F_TITLE  = font("DejaVuSans-Bold.ttf", 68)
F_SUB    = font("DejaVuSans.ttf", 26)
F_STATUS = font("DejaVuSans.ttf", 30)
F_BTN    = font("DejaVuSans-Bold.ttf", 34)
F_GHOST  = font("DejaVuSans.ttf", 30)

GOLD=(212,175,55); GOLD_DK=(166,138,45); GOLD_HI=(255,215,0)
TEXT=(224,224,224); DIM=(180,170,140); BTNTXT=(26,20,8)

def cover(im,w,h):
    iw,ih=im.size; s=max(w/iw,h/ih); im=im.resize((int(iw*s),int(ih*s)))
    x=(im.size[0]-w)//2; y=(im.size[1]-h)//2; return im.crop((x,y,x+w,y+h))
def text_w(d,t,f,tr): return sum(d.textlength(c,font=f)+tr for c in t)-tr
def spaced(d,x,y,t,f,fill,tr,glow=None):
    cx=x
    for c in t:
        if glow: d.text((cx+1,y+1),c,font=f,fill=glow)
        d.text((cx,y),c,font=f,fill=fill); cx+=d.textlength(c,font=f)+tr
def hgrad(img,box,c0,c1,r):
    x0,y0,x1,y1=box; w=x1-x0; h=y1-y0
    g=Image.new("RGB",(w,1))
    for i in range(w):
        t=i/max(1,w-1); g.putpixel((i,0),tuple(int(c0[k]+(c1[k]-c0[k])*t) for k in range(3)))
    g=g.resize((w,h)); m=Image.new("L",(w,h),0)
    ImageDraw.Draw(m).rounded_rectangle([0,0,w-1,h-1],radius=r,fill=255); img.paste(g,(x0,y0),m)

img=cover(Image.open(os.path.join(ROOT,"_mubg.jpg")).convert("RGB"),W,H)
d=ImageDraw.Draw(img,"RGBA")

# top scrim (gradient #CC000000 -> transparent over ~300px)
scrim_h=300
sc=Image.new("RGBA",(W,scrim_h),(0,0,0,0)); sd=ImageDraw.Draw(sc)
for y in range(scrim_h):
    a=int(0xCC*(1-y/scrim_h)); sd.line([(0,y),(W,y)],fill=(0,0,0,a))
img.paste(Image.alpha_composite(img.crop((0,0,W,scrim_h)).convert("RGBA"),sc).convert("RGB"),(0,0))
d=ImageDraw.Draw(img,"RGBA")

# title + subtitle
tw=text_w(d,"NAZWA SERWERA",F_TITLE,12); spaced(d,(W-tw)/2,60,"NAZWA SERWERA",F_TITLE,GOLD,12,glow=(0,0,0))
sw=text_w(d,"LAUNCHER",F_SUB,10); spaced(d,(W-sw)/2,150,"LAUNCHER",F_SUB,DIM,10)

# bottom panel: rgba(20,20,20,0.7) + 6px gold top border
ptop=H-250
d.rectangle([0,ptop,W,H],fill=(20,20,20,179)); d.rectangle([0,ptop,W,ptop+6],fill=GOLD+(255,))

# status
d.text((56,ptop+44),"Pobieranie 12/40  (340 MB / 1.1 GB)",font=F_STATUS,fill=TEXT)
# progress (gold gradient, dark track, h28 r14)
px,py,pw,ph=56,ptop+100,W-112,28
d.rounded_rectangle([px,py,px+pw,py+ph],14,fill=(0,0,0,150))
hgrad(img,[px,py,px+int(pw*0.62),py+ph],GOLD,GOLD_DK,14); d=ImageDraw.Draw(img,"RGBA")
# buttons (right)
gx1,gy0,gy1=W-56,ptop+170,ptop+232
# PLAY
pw_btn=300; px0=gx1-pw_btn
hgrad(img,[px0,gy0,gx1,gy1],GOLD,GOLD_DK,8); d=ImageDraw.Draw(img,"RGBA")
bw=text_w(d,"GRAJ",F_BTN,2); spaced(d,px0+(pw_btn-bw)/2,gy0+12,"GRAJ",F_BTN,BTNTXT,2)
# Retry (ghost) to the left
rx1=px0-24; rx0=rx1-180
d.rounded_rectangle([rx0,gy0,rx1,gy1],8,outline=GOLD,width=3)
rw=text_w(d,"Ponów",F_GHOST,0); d.text((rx0+(180-rw)/2,gy0+14),"Ponów",font=F_GHOST,fill=GOLD)

img.save(os.path.join(OUT_DIR,"final-launcher.png")); img.save(os.path.join(ROOT,"final-launcher.png"))
print("done")
