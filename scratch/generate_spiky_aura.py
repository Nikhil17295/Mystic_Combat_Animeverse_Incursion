import math
from PIL import Image, ImageDraw, ImageFilter

# Create authentic DBZ Spiky Super Saiyan Aura matching user's reference image
w, h = 256, 384
img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

cx = w // 2
cy = h // 2 + 20

# Number of jagged flame spires around the perimeter
# In the DBZ reference, there are ~16-20 large spiky flame teeth pointing upward and outward
points = []
num_teeth = 24
for i in range(num_teeth):
    angle = (i / num_teeth) * 2 * math.pi
    # Base ellipse radius: rx ~ 85, ry ~ 140
    # Spikes are much longer at the top and upper sides, just like the reference photo!
    upward_bias = 1.0 + 0.6 * max(0.0, -math.sin(angle)) # longer at top
    
    # Alternating spike tip and spike valley
    r_base_x = 80 * upward_bias
    r_base_y = 130 * upward_bias
    
    # Outer spike tip
    r_tip_x = r_base_x * 1.35
    r_tip_y = r_base_y * 1.35
    
    # Valley
    r_val_x = r_base_x * 0.85
    r_val_y = r_base_y * 0.85
    
    # Add valley then tip
    val_angle = angle - (math.pi / num_teeth)
    points.append((cx + r_val_x * math.cos(val_angle), cy + r_val_y * math.sin(val_angle)))
    points.append((cx + r_tip_x * math.cos(angle), cy + r_tip_y * math.sin(angle)))

# Draw outer golden flame silhouette
draw.polygon(points, fill=(255, 235, 75, 210))

# Inner bright yellow/white energy core
core_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - cy
    core_points.append((cx + dx * 0.68, cy + dy * 0.68))
draw.polygon(core_points, fill=(255, 255, 170, 240))

# Innermost white-hot core
center_points = []
for p in points:
    dx = p[0] - cx
    dy = p[1] - cy
    center_points.append((cx + dx * 0.40, cy + dy * 0.40))
draw.polygon(center_points, fill=(255, 255, 240, 255))

# Soften slightly for energy glow feel
blurred = img.filter(ImageFilter.GaussianBlur(radius=2))

out_path = r"Assets\Sprites\Effects\dbz_spiky_aura.png"
blurred.save(out_path, "PNG")
print("Saved dbz_spiky_aura.png successfully!")
