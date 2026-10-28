from PIL import Image
import os

raw_dir = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames_Raw"
out_dir = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames"

for i in range(10):
    p = os.path.join(raw_dir, f"ultimate_{i}.png")
    im = Image.open(p)
    w, h = im.size
    
    # Crop at y < 60 to strip artifact
    crop_h = min(h, 60)
    cropped = im.crop((0, 0, w, crop_h))
    
    # In each frame, the back foot starts at (13 - i)
    back_foot_x = 13 - i
    target_canvas_foot_x = 41
    offset_x = target_canvas_foot_x - back_foot_x
    
    # Foot bottom in raw is at y=58 (so max_y is 59)
    # Target canvas bottom is y=104 (max_y is 105)
    offset_y = 105 - 59 # 46
    
    canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    canvas.paste(cropped, (offset_x, offset_y), cropped)
    
    out_p = os.path.join(out_dir, f"ultimate_{i}.png")
    canvas.save(out_p)
    print(f"ultimate_{i}: saved, bbox={canvas.getbbox()}")
