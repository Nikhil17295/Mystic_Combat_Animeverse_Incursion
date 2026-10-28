from PIL import Image

sheet = Image.open(r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png")
print("Sheet dimensions:", sheet.size)

# Scan from y=1900 to bottom to see all strips at bottom of sheet
for y in range(1900, sheet.size[1], 30):
    row_alphas = sum(1 for x in range(sheet.size[0]) if sheet.getpixel((x, y))[3] > 20)
    print(f"y={y}: non-zero alpha={row_alphas}")
