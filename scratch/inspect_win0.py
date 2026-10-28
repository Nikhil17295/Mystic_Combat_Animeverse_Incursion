from PIL import Image

sheet = Image.open(r"Assets\Sprites\Fighters\Saitama\saitama_jus__by_arzeer_de00xcg.png")

# Let's crop x=0..150, y=2070..2165
c = sheet.crop((0, 2070, 150, 2165))
c.save(r"scratch\saitama_win_island0.png")
print("Saved scratch/saitama_win_island0.png")
