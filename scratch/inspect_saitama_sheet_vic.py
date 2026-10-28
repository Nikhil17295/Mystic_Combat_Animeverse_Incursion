from PIL import Image

sheet_p = r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png"
im = Image.open(sheet_p)

# Inspect the 9 boxes
boxes = [
    (140, 2110, 45, 59),
    (185, 2110, 45, 59),
    (230, 2110, 45, 59),
    (277, 2094, 35, 72),
    (327, 2093, 50, 63),
    (377, 2093, 50, 63),
    (427, 2093, 50, 63),
    (478, 2086, 48, 64),
    (526, 2086, 48, 64)
]

for idx, (x, y, w, h) in enumerate(boxes):
    crop = im.crop((x, y, x + w, y + h))
    bb = crop.getbbox()
    print(f"box {idx}: crop bbox within box={bb}, box bottom={y+h}")
