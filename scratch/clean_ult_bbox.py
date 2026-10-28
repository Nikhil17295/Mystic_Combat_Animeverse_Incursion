from PIL import Image
import os

raw_dir = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames_Raw"

for i in range(10):
    p = os.path.join(raw_dir, f"ultimate_{i}.png")
    im = Image.open(p)
    # find bbox considering only y < 60
    pixels = im.load()
    w, h = im.size
    min_x, max_x = w, 0
    min_y, max_y = 60, 0
    for y in range(min(h, 60)):
        for x in range(w):
            if pixels[x, y][3] > 10:
                if x < min_x: min_x = x
                if x > max_x: max_x = x
                if y < min_y: min_y = y
                if y > max_y: max_y = y
    
    # Also find bottom-most pixels (the feet)
    feet_xs = [x for x in range(w) if pixels[x, max_y][3] > 10]
    print(f"ult_{i}: clean bbox=({min_x}, {min_y}, {max_x}, {max_y}), width={max_x-min_x+1}, height={max_y-min_y+1}, feet_xs={feet_xs}")
