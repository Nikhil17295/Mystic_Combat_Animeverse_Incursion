import os
import glob

def analyze_character(char_dir, name):
    print(f"\n=== CHARACTER: {name} ===")
    frames_dir = os.path.join(char_dir, "Frames")
    files = glob.glob(os.path.join(frames_dir, "*.png"))
    
    anims = {}
    for f in files:
        base = os.path.basename(f)
        if "_" in base:
            prefix = base.rsplit("_", 1)[0]
        else:
            prefix = base.split(".")[0]
        anims[prefix] = anims.get(prefix, 0) + 1
        
    for anim, count in sorted(anims.items()):
        print(f"  {anim}: {count} frames")

analyze_character(r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku", "Goku (Base)")
analyze_character(r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Saitama", "Saitama")
