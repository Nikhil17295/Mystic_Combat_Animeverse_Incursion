from PIL import Image
import os

for i in range(9):
    im = Image.open(f"Assets/Sprites/Fighters/Saitama/Frames/victory_{i}.png")
    bb = im.getbbox()
    print(f"victory_{i}: size={im.size}, bbox={bb}")
