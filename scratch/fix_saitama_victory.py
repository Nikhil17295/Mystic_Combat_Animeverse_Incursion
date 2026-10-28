from PIL import Image
import os

sheet = Image.open(r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png")

# Standing victory frames on sheet:
# In inspect_sheet_ground.py:
# Frame 4: (327, 2093, 50, 63) -> abs_top=2099, abs_bot=2146
# Frame 5: (377, 2093, 50, 63) -> abs_top=2095, abs_bot=2144
# Frame 6: (427, 2093, 50, 63) -> abs_top=2095, abs_bot=2144
# Frame 7: (478, 2086, 48, 64) -> abs_top=2095, abs_bot=2144
# Frame 8: (526, 2086, 48, 64) -> abs_top=2092, abs_bot=2147

# Notice: All of frames 4, 5, 6, 7, 8 have feet at 2144..2147 and head at 2092..2095!
# They form a consistent, heroic standing victory pose with cape fluttering!

# Let's crop them directly using their shared feet baseline (abs_bot = 2146)
standing_boxes = [
    (327, 2093, 50, 63), # Cape flap 1
    (377, 2093, 50, 63), # Cape flap 2
    (427, 2093, 50, 63), # Cape flap 3
    (478, 2086, 48, 64), # Cape flap 4
    (526, 2086, 48, 64), # Fist raise / proud
]

dest_dir = r"Assets\Sprites\Fighters\Saitama\Frames"

# Create a smooth 5-frame loop (or ping-pong 6-frame loop): 0, 1, 2, 3, 2, 1
# Across all frames, feet must be LOCKED to y = 135 on 160x160 canvas!
# Center X locked to 80!

for idx, (bx, by, bw, bh) in enumerate(standing_boxes):
    sub = sheet.crop((bx, by, bx+bw, by+bh))
    bb = sub.getbbox()
    solid = sub.crop(bb)
    sw, sh = solid.size
    
    # In every frame, feet are at the bottom of solid
    canvas = Image.new("RGBA", (160, 160), (0, 0, 0, 0))
    paste_x = 80 - sw // 2
    paste_y = 135 - sh # Lock feet bottom to y = 135!
    canvas.paste(solid, (paste_x, paste_y))
    
    out_p = os.path.join(dest_dir, f"victory_{idx}.png")
    canvas.save(out_p, "PNG")
    print(f"Saved victory_{idx}.png: solid {sw}x{sh}, paste at ({paste_x}, {paste_y}), canvas bbox={canvas.getbbox()}")

# Clean up any leftover victory_5..8 if we now have 5 clean frames
for old_idx in range(5, 12):
    old_p = os.path.join(dest_dir, f"victory_{old_idx}.png")
    if os.path.exists(old_p):
        os.remove(old_p)
        meta_p = old_p + ".meta"
        if os.path.exists(meta_p):
            os.remove(meta_p)
        print(f"Removed old inconsistent victory_{old_idx}.png")
