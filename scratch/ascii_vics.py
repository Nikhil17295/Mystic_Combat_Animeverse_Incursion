from PIL import Image

for i in range(4):
    im = Image.open(f"scratch/vic_frame_raw_{i}.png")
    w, h = im.size
    print(f"\n--- FRAME {i} ({w}x{h}) ---")
    for y in range(0, h, 2):
        row = ""
        for x in range(0, w, 2):
            r, g, b, a = im.getpixel((x, y))
            if a < 20: row += " "
            elif r > 180 and g > 150: row += "Y" # yellow suit
            elif r > 150 and g < 100: row += "R" # red glove/boot
            elif r > 180 and g > 180 and b > 180: row += "W" # white cape/bald head
            else: row += "#"
        if row.strip():
            print(f"{y:2d}: {row}")
