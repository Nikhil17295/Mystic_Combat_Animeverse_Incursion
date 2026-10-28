from PIL import Image

sheet = Image.open(r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png")

# Let's see: on the sheet, how are the heads and feet aligned?
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

for idx, (bx, by, bw, bh) in enumerate(boxes):
    sub = sheet.crop((bx, by, bx+bw, by+bh))
    bb = sub.getbbox()
    abs_top = by + bb[1]
    abs_bot = by + bb[3]
    print(f"Frame {idx}: abs_top={abs_top}, abs_bot={abs_bot}, height={abs_bot-abs_top}")
