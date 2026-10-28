from PIL import Image

im = Image.open(r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Sprites\Fighters\Goku\Frames_Raw\ultimate_1.png")
print("ultimate_1 dimensions:", im.size)
for y in range(58, 75):
    row_alphas = [im.getpixel((x, y))[3] if im.mode == 'RGBA' else 0 for x in range(im.size[0])]
    non_zero = sum(1 for a in row_alphas if a > 0)
    print(f"y={y}: non-zero alpha count = {non_zero}")
