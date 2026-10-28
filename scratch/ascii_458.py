from PIL import Image

sheet = Image.open(r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png")

for i in [4, 5, 8]:
    bx, by, bw, bh = [
        (140, 2110, 45, 59),
        (185, 2110, 45, 59),
        (230, 2110, 45, 59),
        (277, 2094, 35, 72),
        (327, 2093, 50, 63),
        (377, 2093, 50, 63),
        (427, 2093, 50, 63),
        (478, 2086, 48, 64),
        (526, 2086, 48, 64)
    ][i]
    sub = sheet.crop((bx, by, bx+bw, by+bh))
    bb = sub.getbbox()
    c = sub.crop(bb)
    w, h = c.size
    print(f"\n--- FRAME {i} ({w}x{h}) ---")
    for y in range(0, h, 2):
        row = ""
        for x in range(0, w, 2):
            r, g, b, a = c.getpixel((x, y))
            if a < 20: row += " "
            elif r > 180 and g > 150: row += "Y"
            elif r > 150 and g < 100: row += "R"
            elif r > 180 and g > 180 and b > 180: row += "W"
            else: row += "#"
        if row.strip():
            print(f"{y:2d}: {row}")
