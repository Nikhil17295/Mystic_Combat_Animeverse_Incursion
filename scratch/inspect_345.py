from PIL import Image

sheet_p = r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png"
im = Image.open(sheet_p)

# Let's inspect x=140..280 from y=2070 to 2160
c = im.crop((140, 2070, 280, 2165))
c.save(r"scratch\islands_3_4_5.png")
print("Saved islands_3_4_5.png")
