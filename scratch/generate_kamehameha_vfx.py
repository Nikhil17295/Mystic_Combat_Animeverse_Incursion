import os
import math
from PIL import Image, ImageDraw

fx_dir = r"Assets\Sprites\Effects"
os.makedirs(fx_dir, exist_ok=True)

# -------------------------------------------------------------
# 1. Kamehameha Charge Orb (128x128)
# Concentrated, brilliant white core with electric cyan corona
# -------------------------------------------------------------
orb_size = 128
orb_img = Image.new("RGBA", (orb_size, orb_size), (0, 0, 0, 0))
cx, cy = orb_size // 2, orb_size // 2
radius_core = 22.0
radius_halo = 46.0

for y in range(orb_size):
    for x in range(orb_size):
        dx = x - cx
        dy = y - cy
        dist = math.hypot(dx, dy)
        if dist > radius_halo:
            continue
        
        # Angle for subtle energy tendril variation
        angle = math.atan2(dy, dx)
        tendril = 1.0 + 0.12 * math.sin(angle * 7.0) + 0.08 * math.cos(angle * 13.0)
        eff_dist = dist / tendril

        if eff_dist <= radius_core:
            # Pure white to intense cyan core
            t = eff_dist / radius_core
            r = int(255)
            g = int(255 - 15 * t)
            b = int(255)
            a = 255
        elif eff_dist <= radius_halo:
            # Electric cyan to outer blue halo falloff
            t = (eff_dist - radius_core) / (radius_halo - radius_core)
            # Quadratic smoothstep
            falloff = 1.0 - t * t
            r = int(255 * (1.0 - t * 0.9))
            g = int(240 * (1.0 - t * 0.45) + 180 * (1.0 - t))
            b = int(255)
            a = int(255 * falloff)
        else:
            continue

        orb_img.putpixel((x, y), (min(255, r), min(255, g), min(255, b), min(255, a)))

orb_path = os.path.join(fx_dir, "kamehameha_charge_orb.png")
orb_img.save(orb_path, "PNG")
print(f"Generated {orb_path}")

# -------------------------------------------------------------
# 2. Kamehameha Beam Body (256x128)
# Horizontal solid light blue beam with brilliant white core
# -------------------------------------------------------------
bw, bh = 256, 128
beam_body = Image.new("RGBA", (bw, bh), (0, 0, 0, 0))
bcy = bh // 2
core_h = 16.0  # half height of white core
outer_h = 44.0 # half height of cyan casing

for y in range(bh):
    dy = abs(y - bcy)
    if dy > outer_h:
        continue
    
    for x in range(bw):
        # Subtle horizontal laser stream pulsation
        streak = 1.0 + 0.05 * math.sin(x * 0.15) + 0.03 * math.cos(x * 0.31 + y * 0.2)
        eff_dy = dy / streak

        if eff_dy <= core_h:
            # White core
            t = eff_dy / core_h
            r = 255
            g = int(255 - 20 * t)
            b = 255
            a = 255
        else:
            # Electric cyan / light blue casing
            t = (eff_dy - core_h) / (outer_h - core_h)
            falloff = max(0.0, 1.0 - t * t)
            r = int(140 * (1.0 - t))
            g = int(230 * (1.0 - t * 0.35))
            b = 255
            a = int(245 * falloff)

        beam_body.putpixel((x, y), (min(255, r), min(255, g), min(255, b), min(255, a)))

body_path = os.path.join(fx_dir, "kamehameha_beam_body.png")
beam_body.save(body_path, "PNG")
print(f"Generated {body_path}")

# -------------------------------------------------------------
# 3. Kamehameha Beam Head (128x128)
# Surging rounded energy arrowhead dome leading the beam
# -------------------------------------------------------------
hw, hh = 128, 128
beam_head = Image.new("RGBA", (hw, hh), (0, 0, 0, 0))
hcy = hh // 2

for y in range(hh):
    dy = abs(y - hcy)
    if dy > outer_h:
        continue
    for x in range(hw):
        # Dome curves forward towards right edge (x ~ 90)
        # Parabolic front: (x - 20) + (dy / 38)^2 * 60 <= 70
        front_x = 20 + 70 * (1.0 - (dy / outer_h)**2)
        if x > front_x:
            continue
        
        dist_to_front = front_x - x
        if dist_to_front < 0:
            continue
        
        eff_dy = dy
        if eff_dy <= core_h and dist_to_front > 8:
            t = eff_dy / core_h
            r = 255
            g = int(255 - 20 * t)
            b = 255
            a = 255
        else:
            t = max(eff_dy / outer_h, 1.0 - dist_to_front / 24.0)
            t = min(1.0, max(0.0, t))
            falloff = 1.0 - t * t
            r = int(160 * (1.0 - t))
            g = int(240 * (1.0 - t * 0.3))
            b = 255
            a = int(255 * falloff)

        beam_head.putpixel((x, y), (min(255, r), min(255, g), min(255, b), min(255, a)))

head_path = os.path.join(fx_dir, "kamehameha_beam_head.png")
beam_head.save(head_path, "PNG")
print(f"Generated {head_path}")

# -------------------------------------------------------------
# 4. Kamehameha Muzzle Flare (128x128)
# Blast flare at hands where the beam erupts
# -------------------------------------------------------------
fw, fh = 128, 128
flare_img = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
fcx, fcy = fw // 2, fh // 2

for y in range(fh):
    for x in range(fw):
        dx = x - fcx
        dy = y - fcy
        d = math.hypot(dx, dy)
        if d > 54:
            continue
        
        # Starburst / cross rays
        ang = math.atan2(dy, dx)
        ray = 1.0 + 0.35 * (math.cos(ang * 4.0)**2) + 0.20 * (math.sin(ang * 8.0)**2)
        eff_d = d / ray

        if eff_d < 16:
            r, g, b, a = 255, 255, 255, 255
        elif eff_d < 50:
            t = (eff_d - 16) / 34.0
            r = int(255 * (1.0 - t * 0.8))
            g = int(245 * (1.0 - t * 0.25))
            b = 255
            a = int(255 * (1.0 - t * t))
        else:
            continue

        flare_img.putpixel((x, y), (min(255, r), min(255, g), min(255, b), min(255, a)))

flare_path = os.path.join(fx_dir, "kamehameha_muzzle_flare.png")
flare_img.save(flare_path, "PNG")
print(f"Generated {flare_path}")
