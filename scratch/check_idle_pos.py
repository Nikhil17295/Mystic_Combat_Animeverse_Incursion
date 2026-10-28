from PIL import Image

idle = Image.open(r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames\idle_0.png")
print("idle_0 bbox:", idle.getbbox())
pixels = idle.load()
w, h = idle.size
bottom_y = idle.getbbox()[3] - 1
feet_xs = [x for x in range(w) if pixels[x, bottom_y][3] > 10]
print(f"idle_0 bottom_y={bottom_y}, feet_xs={feet_xs}")
