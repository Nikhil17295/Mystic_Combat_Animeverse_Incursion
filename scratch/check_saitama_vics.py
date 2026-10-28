from PIL import Image
import os

saitama_frames = r"Assets\Sprites\Fighters\Saitama\Frames"
idle_0 = Image.open(os.path.join(saitama_frames, "idle_0.png"))
print("idle_0 bbox:", idle_0.getbbox())

for i in range(9):
    p = os.path.join(saitama_frames, f"victory_{i}.png")
    im = Image.open(p)
    bb = im.getbbox()
    print(f"victory_{i}: top={bb[1]}, bottom={bb[3]}, height={bb[3]-bb[1]}")
