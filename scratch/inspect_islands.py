from PIL import Image

crop = Image.open(r"scratch\saitama_row_preview.png")
w, h = crop.size
# Find columns with pixels
cols = [x for x in range(w) if any(crop.getpixel((x, y))[3] > 20 for y in range(h))]

# Group columns into distinct sprite islands
islands = []
start = None
prev = None
for c in cols:
    if start is None:
        start = c
        prev = c
    elif c - prev <= 4:
        prev = c
    else:
        islands.append((start, prev))
        start = c
        prev = c
if start:
    islands.append((start, prev))

print(f"Found {len(islands)} sprite islands in this strip:")
for idx, (x1, x2) in enumerate(islands):
    sub = crop.crop((x1, 0, x2+1, h))
    bb = sub.getbbox()
    print(f"  Island {idx}: x=({x1}..{x2}), bbox within sub={bb}, absolute y=({2070+bb[1]}..{2070+bb[3]}), height={bb[3]-bb[1]}")
