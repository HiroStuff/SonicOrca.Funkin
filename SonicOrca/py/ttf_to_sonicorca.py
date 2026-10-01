from PIL import Image, ImageDraw, ImageFont
import html

FONT_PATH = "vcr.ttf"
FONT_SIZE = 24

OUTPUT_PNG = "SHAPE.png"
OUTPUT_XML = "VCR.xml"

FONT_NAME = "vcr hud font"

SHAPE = "/SHAPE"
OVERLAYS = ["/OVERLAYSILVER", "/OVERLAYGOLD"]

CELL_W = 24
CELL_H = 40
TRACKING = 6

CHARS = "'\"-_0123456789:;ABCDEFGHIJKLMNOPQRSTUVWXYZ.,*?!"

COLUMNS = 16

font = ImageFont.truetype(FONT_PATH, FONT_SIZE)

rows = (len(CHARS) + COLUMNS - 1) // COLUMNS

img = Image.new("RGBA", (COLUMNS * CELL_W, rows * CELL_H), (0,0,0,0))
draw = ImageDraw.Draw(img)

xml = []

xml.append('<?xml version="1.0"?>')
xml.append("<font>")
xml.append(f"\t<name>{FONT_NAME}</name>")
xml.append(f"\t<shape>{SHAPE}</shape>")

for o in OVERLAYS:
    xml.append(f"\t<overlay>{o}</overlay>")

xml.append(f"\t<width>{CELL_W}</width>")
xml.append(f"\t<height>{CELL_H}</height>")
xml.append(f"\t<tracking>{TRACKING}</tracking>")
xml.append('\t<shadow x="4" y="4" />')

xml.append("\t<chardefs>")

for i, char in enumerate(CHARS):

    col = i % COLUMNS
    row = i // COLUMNS

    x = col * CELL_W
    y = row * CELL_H

    bbox = font.getbbox(char)

    w = bbox[2] - bbox[0]
    h = bbox[3] - bbox[1]

    draw.text(
        (x - bbox[0], y - bbox[1]),
        char,
        font=font,
        fill=(255,255,255,255)
    )

    esc = html.escape(char)

    xml.append(f'\t\t<chardef char="{esc}">')
    xml.append(f'\t\t  <rect x="{x}" y="{y}" w="{w}" h="{h}" />')

    if w < CELL_W:
        xml.append(f'\t\t  <width>{CELL_W}</width>')

    xml.append('\t\t</chardef>')

xml.append("\t</chardefs>")
xml.append("</font>")

img.save(OUTPUT_PNG)

with open(OUTPUT_XML, "w") as f:
    f.write("\n".join(xml))

print("Generated font.png and font.xml")
