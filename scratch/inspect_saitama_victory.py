import os
from PIL import Image

saitama_frames = r"Assets\Sprites\Fighters\Saitama\Frames"
for i in range(9):
    p = os.path.join(saitama_frames, f"victory_{i}.png")
    if os.path.exists(p):
        im = Image.open(p)
        bbox = im.getbbox()
        # Find lowest foot pixel
        pixels = im.load()
        w, h = im.size
        lowest_y = -1
        feet_xs = []
        for y in range(h-1, -1, -1):
            xs = [x for x in range(w) if pixels[x, y][3] > 10]
            if xs:
                lowest_y = y
                feet_xs = xs
                break
        print(f"victory_{i}.png: size={im.size}, bbox={bbox}, lowest_y={lowest_y}, feet_xs={feet_xs[:5]}...")
