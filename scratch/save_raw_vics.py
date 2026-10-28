from PIL import Image

sheet = Image.open(r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png")

for i in range(4):
    bx, by, bw, bh = [
        (140, 2110, 45, 59),
        (185, 2110, 45, 59),
        (230, 2110, 45, 59),
        (277, 2094, 35, 72)
    ][i]
    sub = sheet.crop((bx, by, bx+bw, by+bh))
    bb = sub.getbbox()
    c = sub.crop(bb)
    c.save(f"scratch/vic_frame_raw_{i}.png")
    print(f"Saved scratch/vic_frame_raw_{i}.png")
