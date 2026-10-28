import math
from PIL import Image, ImageDraw, ImageFilter

# Create authentic DBZ Spiky Flame Aura sprite texture
# 256x384 canvas, sharp jagged flame spires pointing upward and outward
w, h = 256, 384
img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

cx = w // 2
cy = h // 2 + 30

# Outer flame envelope: 26 jagged flame spires
points = []
num_teeth = 26
for i in range(num_teeth):
    angle = (i / num_teeth) * 2 * math.pi
    # Strong upward elongation matching DBZ reference image
    # In DBZ, flames flare high above the head and taper near the feet
    sin_a = math.sin(angle)
    cos_a = math.cos(angle)
    
    # Upward bias: maximum when sin_a < 0 (pointing upwards)
    upward = 1.0 + 0.85 * max(0.0, -sin_a)
    # Lateral flare: wider around torso and shoulders
    lateral = 1.0 + 0.3 * (cos_a * cos_a)
    
    # Base radius
    r_x = 75 * lateral
    r_y = 120 * upward
    
    # Tip of spike
    # Alternate between taller primary spikes and secondary teeth
    spike_factor = 1.45 if (i % 2 == 0) else 1.25
    tip_x = cx + r_x * spike_factor * cos_a
    tip_y = cy + r_y * spike_factor * sin_a
    
    # Valley between spikes
    val_angle = angle - (math.pi / num_teeth)
    val_sin = math.sin(val_angle)
    val_cos = math.cos(val_angle)
    val_upward = 1.0 + 0.85 * max(0.0, -val_sin)
    val_lateral = 1.0 + 0.3 * (val_cos * val_cos)
    val_rx = 75 * val_lateral * 0.82
    val_ry = 120 * val_upward * 0.82
    val_x = cx + val_rx * val_cos
    val_y = cy + val_ry * val_sin
    
    points.append((val_x, val_y))
    points.append((tip_x, tip_y))

# 1. Outer flame silhouette (semi-opaque white for color tinting)
draw.polygon(points, fill=(255, 255, 255, 175))

# 2. Mid flame layer (slightly contracted, brighter)
mid_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - cy
    mid_points.append((cx + dx * 0.75, cy + dy * 0.75))
draw.polygon(mid_points, fill=(255, 255, 255, 220))

# 3. Inner flame core (bright solid white)
core_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - cy
    core_points.append((cx + dx * 0.48, cy + dy * 0.48))
draw.polygon(core_points, fill=(255, 255, 255, 255))

# Subtle 1.5px blur for smooth antialiased edges while preserving sharp jagged tips
blurred = img.filter(ImageFilter.GaussianBlur(radius=1.5))

out_path = r"Assets\Sprites\Effects\dbz_spiky_aura.png"
blurred.save(out_path, "PNG")
print("Saved authentic DBZ spiky flame aura to:", out_path)
