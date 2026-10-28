import os
from PIL import Image

ssj_sheet = Image.open(r"Assets\Sprites\Fighters\Goku\goku_ssj_transparent.png")
dest_dir = r"Assets\Sprites\Fighters\Goku\Frames_SSJ"
os.makedirs(dest_dir, exist_ok=True)

def slice_sprites_in_strip(y1, y2, x_min=0, x_max=980):
    crop = ssj_sheet.crop((x_min, y1, x_max, y2))
    cols = [x for x in range(crop.size[0]) if any(crop.getpixel((x, y))[3] > 20 for y in range(crop.size[1]))]
    ranges = []
    cur_start, prev = None, None
    for c in cols:
        if cur_start is None:
            cur_start, prev = c, c
        elif c - prev <= 5:
            prev = c
        else:
            ranges.append((cur_start, prev, prev - cur_start))
            cur_start, prev = c, c
    if cur_start:
        ranges.append((cur_start, prev, prev - cur_start))
    
    sprites = []
    for r in ranges:
        sub = crop.crop((r[0], 0, r[1], crop.size[1]))
        bb = sub.getbbox()
        if bb and bb[2] - bb[0] > 10 and bb[3] - bb[1] > 10:
            solid = sub.crop(bb)
            sprites.append(solid)
    return sprites

def save_standardized(sprites, prefix, max_count=None):
    if max_count:
        sprites = sprites[:max_count]
    for idx, sp in enumerate(sprites):
        cw, ch = sp.size
        canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        paste_x = max(0, 64 - cw // 2)
        paste_y = max(0, 105 - ch)
        canvas.paste(sp, (paste_x, paste_y))
        fname = f"{prefix}_{idx}.png" if len(sprites) > 1 or max_count else f"{prefix}.png"
        out_p = os.path.join(dest_dir, fname)
        canvas.save(out_p, "PNG")
        print(f"Saved {fname} (original: {cw}x{ch})")

# 1. Idle & Walk (Row 0: Y=44..108)
row0_sprites = slice_sprites_in_strip(44, 108)
print(f"Row 0 sprites: {len(row0_sprites)}")
if len(row0_sprites) >= 10:
    save_standardized(row0_sprites[0:4], "idle")
    save_standardized(row0_sprites[4:10], "walk")

# 2. Jump & Crouch (Row 1: Y=168..232)
row1_sprites = slice_sprites_in_strip(168, 232)
print(f"Row 1 sprites: {len(row1_sprites)}")
if len(row1_sprites) >= 7:
    save_standardized(row1_sprites[0:5], "jump")
    # Crouch
    c_sp = row1_sprites[5]
    cw, ch = c_sp.size
    canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    canvas.paste(c_sp, (max(0, 64 - cw // 2), max(0, 105 - ch)))
    canvas.save(os.path.join(dest_dir, "crouch.png"), "PNG")
    print("Saved crouch.png")

# 3. Hurt & Knockout (Row 2: Y=304..356 and Row 3: Y=412..480)
row2_sprites = slice_sprites_in_strip(304, 356)
if len(row2_sprites) >= 3:
    save_standardized(row2_sprites[0:3], "hurt")

row3_sprites = slice_sprites_in_strip(412, 480)
if len(row3_sprites) >= 4:
    save_standardized(row3_sprites[0:4], "death")

# 4. Ki Charge & Transformation Roar (Row 5: Y=664..728)
row5_sprites = slice_sprites_in_strip(664, 728)
print(f"Row 5 sprites: {len(row5_sprites)}")
if len(row5_sprites) >= 9:
    # Skill / Transformation Roar
    save_standardized(row5_sprites[4:8], "skill")
    save_standardized(row5_sprites[4:7], "meditate")
    save_standardized(row5_sprites[0:4], "victory")

# 5. Combat Punches & Heavy Attacks (Y=2212..2272 & Y=2284..2348)
combat_punches = slice_sprites_in_strip(2212, 2272)
print(f"Combat punches: {len(combat_punches)}")
if len(combat_punches) >= 4:
    save_standardized(combat_punches[0:4], "punch")
if len(combat_punches) >= 8:
    save_standardized(combat_punches[4:8], "heavy")
else:
    combat_heavy = slice_sprites_in_strip(2284, 2348)
    if len(combat_heavy) >= 4:
        save_standardized(combat_heavy[0:4], "heavy")

print("Super Saiyan automated extraction finished successfully!")
