from PIL import Image

for i in range(9):
    im = Image.open(f"Assets/Sprites/Fighters/Saitama/Frames/victory_{i}.png")
    # Let's count yellow (suit), white (cape/gloves), red (gloves/boots)
    pixels = im.load()
    yellow_count = 0
    white_count = 0
    red_count = 0
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a > 50:
                if r > 180 and g > 150 and b < 100: yellow_count += 1
                elif r > 200 and g > 200 and b > 200: white_count += 1
                elif r > 150 and g < 80 and b < 80: red_count += 1
    print(f"victory_{i}: solid_px={sum(1 for y in range(h) for x in range(w) if pixels[x,y][3]>50)}, yellow={yellow_count}, white={white_count}, red={red_count}")
