import math
from PIL import Image, ImageDraw, ImageFilter

# Create fluid, organic anime fire aura (not rigid spikes, but licking flame tongues)
# Width: 180, Height: 240
# Base: Rounded smooth base at feet level (y=230), zero pixels below y=230!
w, h = 180, 240
img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

cx = w // 2
base_y = 230 # Feet contact plane

# Generate smooth, organic licking flame silhouette:
# 1. Rounded bottom base:
#    Arc from (cx - 40, base_y) through (cx, base_y + 2) to (cx + 40, base_y)
# 2. Rising fluid flame contours along right side to top, then down left side

points = []

# Bottom base: rounded half-oval along ground
for i in range(16):
    theta = math.pi * (i / 15.0)
    bx = cx + 42 * math.cos(theta)
    by = base_y - 8 + 8 * math.sin(theta)
    points.append((bx, by))

# Right side: curved, wavy licking flame tongues
points.append((cx + 46, base_y - 30))
points.append((cx + 52, base_y - 65))
points.append((cx + 44, base_y - 95))
points.append((cx + 50, base_y - 130))
points.append((cx + 38, base_y - 155))
points.append((cx + 36, base_y - 185))

# Top flame tips (curved tongues of fire)
points.append((cx + 18, base_y - 212))
points.append((cx + 8, base_y - 198))
points.append((cx, base_y - 225))      # Top central flame tip
points.append((cx - 8, base_y - 198))
points.append((cx - 18, base_y - 212))

# Left side: curved, wavy licking flame tongues
points.append((cx - 36, base_y - 185))
points.append((cx - 38, base_y - 155))
points.append((cx - 50, base_y - 130))
points.append((cx - 44, base_y - 95))
points.append((cx - 52, base_y - 65))
points.append((cx - 46, base_y - 30))

# 1. Outer translucent fire mantle (soft alpha)
draw.polygon(points, fill=(255, 255, 255, 130))

# 2. Mid flame layer: slightly contracted, brighter
mid_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - (base_y - 80)
    mid_points.append((cx + dx * 0.76, (base_y - 80) + dy * 0.78))
draw.polygon(mid_points, fill=(255, 255, 255, 190))

# 3. Inner flame core: hot, dense
core_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - (base_y - 70)
    core_points.append((cx + dx * 0.48, (base_y - 70) + dy * 0.52))
draw.polygon(core_points, fill=(255, 255, 255, 240))

# 4. Innermost white-hot plasma column (torso / core)
innermost = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - (base_y - 65)
    innermost.append((cx + dx * 0.28, (base_y - 65) + dy * 0.32))
draw.polygon(innermost, fill=(255, 255, 255, 255))

# Soft Gaussian Blur to give it authentic fluid anime fire look (not rigid / jagged)
fluid_fire = img.filter(ImageFilter.GaussianBlur(radius=3.5))

out_path = r"Assets\Sprites\Effects\dbz_spiky_aura.png"
fluid_fire.save(out_path, "PNG")
print("Saved fluid anime fire aura to:", out_path)
