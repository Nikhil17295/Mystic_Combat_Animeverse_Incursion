from PIL import Image

sheet_p = r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png"
im = Image.open(sheet_p)

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
    actual_crop = crop.crop(bb)
    # Print height and where feet are
    print(f"Frame {idx}: width={actual_crop.size[0]}, height={actual_crop.size[1]}, sheet_y_range=({y+bb[1]}..{y+bb[3]})")
