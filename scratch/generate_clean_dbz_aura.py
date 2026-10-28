import math
from PIL import Image, ImageDraw, ImageFilter

# Clean DBZ Aura:
# - Rounded bottom near the ground (semicircle/flat at the base, never below feet)
# - Snug proportions (width 160, height 220)
# - Only 7-8 clean, bold upward flame spires (no excessive noisy spikes!)
w, h = 180, 240
img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

cx = w // 2
base_y = h - 10 # Bottom of aura at the ground/feet line

# Construct flame envelope:
# 1. Bottom: smooth rounded arc from left foot (cx - 40, base_y) to right foot (cx + 40, base_y)
# 2. Sides & Top: 7 bold, clean flame spires pointing upward

points = []

# Bottom rounded arc (from angle 0 to pi, passing through bottom)
# Bottom base width: radius_x = 42, radius_y = 12
for i in range(16):
    theta = math.pi * (i / 15.0) # 0 to pi
    px = cx + 42 * math.cos(theta)
    py = base_y - 10 + 10 * math.sin(theta)
    points.append((px, py))

# Right side rising to top spires
# Spires:
# Right lower spire
points.append((cx + 48, base_y - 45))
points.append((cx + 56, base_y - 75))
points.append((cx + 44, base_y - 95))

# Right mid spire
points.append((cx + 52, base_y - 130))
points.append((cx + 38, base_y - 145))

# Right upper spire
points.append((cx + 40, base_y - 180))
points.append((cx + 25, base_y - 190))

# Central crown spires (top of head and above)
points.append((cx + 15, base_y - 215)) # Top right spire
points.append((cx + 5, base_y - 200))
points.append((cx, base_y - 228))      # Tallest center spire
points.append((cx - 5, base_y - 200))
points.append((cx - 15, base_y - 215)) # Top left spire

# Left upper spire
points.append((cx - 25, base_y - 190))
points.append((cx - 40, base_y - 180))

# Left mid spire
points.append((cx - 38, base_y - 145))
points.append((cx - 52, base_y - 130))

# Left lower spire
points.append((cx - 44, base_y - 95))
points.append((cx - 56, base_y - 75))
points.append((cx - 48, base_y - 45))

# 1. Outer flame body (antialiased white)
draw.polygon(points, fill=(255, 255, 255, 175))

# 2. Mid flame layer (contracted toward center, brighter)
mid_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - (base_y - 80)
    mid_points.append((cx + dx * 0.70, (base_y - 80) + dy * 0.72))
draw.polygon(mid_points, fill=(255, 255, 255, 220))

# 3. Inner core (solid white)
core_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - (base_y - 70)
    core_points.append((cx + dx * 0.42, (base_y - 70) + dy * 0.45))
draw.polygon(core_points, fill=(255, 255, 255, 255))

# Smooth 1.5px antialiasing blur
blurred = img.filter(ImageFilter.GaussianBlur(radius=1.2))

out_path = r"Assets\Sprites\Effects\dbz_spiky_aura.png"
blurred.save(out_path, "PNG")
print("Saved clean, snug DBZ aura to", out_path)
