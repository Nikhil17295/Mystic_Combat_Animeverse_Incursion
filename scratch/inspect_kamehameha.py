from PIL import Image
import os

raw_dir = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames_Raw"
cur_dir = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames"

print("--- RAW FRAMES ---")
for i in range(10):
    p = os.path.join(raw_dir, f"ultimate_{i}.png")
    if os.path.exists(p):
        im = Image.open(p)
        bbox = im.getbbox()
        print(f"ultimate_{i}: size={im.size}, bbox={bbox}")

print("\n--- CURRENT PROCESSED FRAMES ---")
for i in range(10):
    p = os.path.join(cur_dir, f"ultimate_{i}.png")
    if os.path.exists(p):
        im = Image.open(p)
        bbox = im.getbbox()
        print(f"ultimate_{i}: size={im.size}, bbox={bbox}")
