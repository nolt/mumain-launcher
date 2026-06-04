"""Compare window-chrome options: A=system title bar, B=borderless custom chrome."""
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 1520, 950
OUT_DIR, ROOT = "/m", "/r"
FONT_DIR = "/usr/share/fonts/truetype/dejavu"
def font(n,s): return ImageFont.truetype(os.path.join(FONT_DIR,n),s)
F_TITLE=font("DejaVuSans-Bold.ttf",68); F_SUB=font("DejaVuSans.ttf",26)
F_STATUS=font("DejaVuSans.ttf",30); F_BTN=font("DejaVuSans-Bold.ttf",34)
F_GHOST=font("DejaVuSans.ttf",30); F_BAR=font("DejaVuSans.ttf",26); F_WIN=font("DejaVuSans.ttf",36)

GOLD=(212,175,55); GOLD_DK=(166,138,45); TEXT=(224,224,224); DIM=(180,170,140); BTNTXT=(26,20,8)

def cover(im,w,h):
    iw,ih=im.size; s=max(w/iw,h/ih); im=im.resize((int(iw*s),int(ih*s)))
    x=(im.size[0]-w)//2; y=(im.size[1]-h)//2; return im.crop((x,y,x+w,y+h))
def tw(d,t,f,tr): return sum(d.textlength(c,font=f)+tr for c in t)-tr
def spaced(d,x,y,t,f,fill,tr,glow=None):
    cx=x
    for c in t:
        if glow: d.text((cx+1,y+1),c,font=f,fill=glow)
        d.text((cx,y),c,font=f,fill=fill); cx+=d.textlength(c,font=f)+tr
def hgrad(img,box,c0,c1,r):
    x0,y0,x1,y1=box; w=x1-x0; h=y1-y0; g=Image.new("RGB",(w,1))
    for i in range(w):
        t=i/max(1,w-1); g.putpixel((i,0),tuple(int(c0[k]+(c1[k]-c0[k])*t) for k in range(3)))
    g=g.resize((w,h)); m=Image.new("L",(w,h),0)
    ImageDraw.Draw(m).rounded_rectangle([0,0,w-1,h-1],radius=r,fill=255); img.paste(g,(x0,y0),m)

def content(img, top=0):
    """Draw the launcher content (scrim+title+panel) into img, content area starting at y=top."""
    d=ImageDraw.Draw(img,"RGBA"); ch=img.height-top
    # scrim
    scrim_h=300; sc=Image.new("RGBA",(W,scrim_h),(0,0,0,0)); sd=ImageDraw.Draw(sc)
    for y in range(scrim_h): sd.line([(0,y),(W,y)],fill=(0,0,0,int(0xCC*(1-y/scrim_h))))
    reg=img.crop((0,top,W,top+scrim_h)).convert("RGBA")
    img.paste(Image.alpha_composite(reg,sc).convert("RGB"),(0,top)); d=ImageDraw.Draw(img,"RGBA")
    w1=tw(d,"NAZWA SERWERA",F_TITLE,12); spaced(d,(W-w1)/2,top+60,"NAZWA SERWERA",F_TITLE,GOLD,12,glow=(0,0,0))
    w2=tw(d,"LAUNCHER",F_SUB,10); spaced(d,(W-w2)/2,top+150,"LAUNCHER",F_SUB,DIM,10)
    ptop=img.height-250
    d.rectangle([0,ptop,W,img.height],fill=(20,20,20,179)); d.rectangle([0,ptop,W,ptop+6],fill=GOLD+(255,))
    d.text((56,ptop+44),"Pobieranie 12/40  (340 MB / 1.1 GB)",font=F_STATUS,fill=TEXT)
    px,py,pw,ph=56,ptop+100,W-112,28
    d.rounded_rectangle([px,py,px+pw,py+ph],14,fill=(0,0,0,150))
    hgrad(img,[px,py,px+int(pw*0.62),py+ph],GOLD,GOLD_DK,14); d=ImageDraw.Draw(img,"RGBA")
    gx1=W-56; gy0=ptop+170; gy1=ptop+232; pwb=300; px0=gx1-pwb
    hgrad(img,[px0,gy0,gx1,gy1],GOLD,GOLD_DK,8); d=ImageDraw.Draw(img,"RGBA")
    bw=tw(d,"GRAJ",F_BTN,2); spaced(d,px0+(pwb-bw)/2,gy0+12,"GRAJ",F_BTN,BTNTXT,2)
    rx1=px0-24; rx0=rx1-180; d.rounded_rectangle([rx0,gy0,rx1,gy1],8,outline=GOLD,width=3)
    rw=tw(d,"Ponów",F_GHOST,0); d.text((rx0+(180-rw)/2,gy0+14),"Ponów",font=F_GHOST,fill=GOLD)

SRC=os.path.join(ROOT,"_mubg.jpg")

# ── A: system title bar (gray OS strip on top, content below) ──
BAR=64
imgA=Image.new("RGB",(W,H+BAR),(0,0,0))
imgA.paste(cover(Image.open(SRC).convert("RGB"),W,H),(0,BAR))
content(imgA, top=BAR)
dA=ImageDraw.Draw(imgA)
dA.rectangle([0,0,W,BAR],fill=(45,45,48))               # OS bar
dA.text((20,BAR/2-16),"NAZWA SERWERA",font=F_BAR,fill=(220,220,220))
for i,(g,col) in enumerate([("—",(210,210,210)),("✕",(230,120,110))][::-1]):
    cx=W-50-i*64; dA.text((cx,BAR/2-18),g,font=F_WIN,fill=col)  # min + close, OS-styled
imgA.save(os.path.join(OUT_DIR,"win-A-systembar.png")); imgA.save(os.path.join(ROOT,"win-A-systembar.png"))

# ── B: borderless custom chrome (full-bleed art, gold frame, custom buttons) ──
imgB=cover(Image.open(SRC).convert("RGB"),W,H)
content(imgB, top=0)
dB=ImageDraw.Draw(imgB,"RGBA")
# custom window buttons top-right, gold, over the art
dB.text((W-58,22),"✕",font=F_WIN,fill=(212,175,55))     # close
dB.text((W-122,22),"—",font=F_WIN,fill=(212,175,55))    # minimize
# thin gold frame around the whole window
dB.rectangle([0,0,W-1,H-1],outline=GOLD,width=3)
imgB.save(os.path.join(OUT_DIR,"win-B-borderless.png")); imgB.save(os.path.join(ROOT,"win-B-borderless.png"))
print("done")
