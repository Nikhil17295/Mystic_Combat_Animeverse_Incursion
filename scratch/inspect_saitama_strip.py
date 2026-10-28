from PIL import Image

sheet_p = r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png"
im = Image.open(sheet_p)

# Crop region from x=0 to 600, y=2070 to 2180
crop = im.crop((0, 2070, 600, 2180))
crop.save(r"scratch\saitama_row_preview.png")
print("Saved row preview to scratch/saitama_row_preview.png")
