import os
import math
from PIL import Image, ImageDraw

fx_dir = r"Assets\Sprites\Effects"
os.makedirs(fx_dir, exist_ok=True)

# 1. 4-Point Sharp Anime Hit Star (64x64)
# Bright white core, vibrant gold/cyan falloff
star_img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
cx, cy = 32, 32
for y in range(64):
    for x in range(64):
        dx = abs(x - cx)
        dy = abs(y - cy)
        # 4-point cross formula: 1 / (dx^p + dy^p)
        dist = (dx**0.6 + dy**0.6)
        if dist < 6.5:
            # White core to gold halo
            intensity = max(0.0, 1.0 - (dist / 6.5))
            r = int(255)
            g = int(220 * intensity + 180 * (1 - intensity))
            b = int(120 * intensity + 30 * (1 - intensity))
            a = int(255 * (intensity**0.5))
            star_img.putpixel((x, y), (r, g, b, a))
star_img.save(os.path.join(fx_dir, "hit_spark_star.png"), "PNG")
print("Saved hit_spark_star.png")

# 2. Glowing Particle Orb (32x32)
orb_img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
ocx, ocy = 16, 16
for y in range(32):
    for x in range(32):
        d = math.hypot(x - ocx, y - ocy)
        if d < 15:
            val = max(0.0, 1.0 - d / 15.0)
            a = int(255 * (val**1.5))
            orb_img.putpixel((x, y), (255, 255, 255, a))
orb_img.save(os.path.join(fx_dir, "hit_spark_orb.png"), "PNG")
print("Saved hit_spark_orb.png")

# 3. Expanding Shockwave Ring (128x128)
ring_img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
rcx, rcy = 64, 64
for y in range(128):
    for x in range(128):
        d = math.hypot(x - rcx, y - rcy)
        # ring peak at radius 52, thickness ~8
        dr = abs(d - 52)
        if dr < 8:
            val = max(0.0, 1.0 - dr / 8.0)
            a = int(255 * (val**1.2))
            ring_img.putpixel((x, y), (255, 255, 255, a))
ring_img.save(os.path.join(fx_dir, "shockwave_ring.png"), "PNG")
print("Saved shockwave_ring.png")

# 4. Stylized Anime Dust Puff (64x64)
dust_img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
draw = ImageDraw.Draw(dust_img)
# overlapping circles with soft opacity
circles = [
    (24, 38, 16, (230, 225, 210, 240)),
    (40, 36, 18, (240, 235, 220, 255)),
    (32, 24, 15, (255, 250, 240, 255)),
    (18, 42, 10, (210, 205, 195, 200)),
    (46, 42, 11, (210, 205, 195, 200)),
]
for cx, cy, r, col in circles:
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=col)
dust_img.save(os.path.join(fx_dir, "dust_puff.png"), "PNG")
print("Saved dust_puff.png")

# 5. Ki Flame Teardrop Particle (32x64)
flame_img = Image.new("RGBA", (32, 64), (0, 0, 0, 0))
fcx, fcy = 16, 48
for y in range(64):
    for x in range(32):
        # teardrop shape: radius narrows as y goes from 48 to 8
        if y > 56:
            continue
        progress = max(0.0, (56 - y) / 48.0) # 0 at bottom, 1 at top
        r_width = max(1.0, 14.0 * (1.0 - (progress**1.8)))
        dx = abs(x - fcx)
        if dx <= r_width:
            intensity = (1.0 - (dx / r_width)) * (1.0 - 0.3 * progress)
            a = int(255 * intensity)
            flame_img.putpixel((x, y), (255, 255, 255, a))
flame_img.save(os.path.join(fx_dir, "ki_flame_particle.png"), "PNG")
print("Saved ki_flame_particle.png")
