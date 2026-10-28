from PIL import Image

im = Image.open(r"scratch\islands_3_4_5.png")
w, h = im.size
for y in range(h):
    alphas = [im.getpixel((x, y))[3] for x in range(w)]
    non_zero = sum(1 for a in alphas if a > 20)
    if non_zero > 0:
        print(f"relative y={y} (absolute y={2070+y}): {non_zero} pixels")
