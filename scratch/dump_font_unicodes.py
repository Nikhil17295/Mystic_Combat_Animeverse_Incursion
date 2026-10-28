import re

with open(r'Assets\Fonts\TMP_Assets\ArcadeClassic_SDF.asset', 'r', encoding='utf-8', errors='ignore') as f:
    t1 = f.read()

unicodes1 = sorted([int(x) for x in re.findall(r'm_Unicode:\s*(\d+)', t1)])
print(f"ArcadeClassic has {len(unicodes1)} characters:")
chars1 = "".join([chr(u) for u in unicodes1 if 32 <= u < 127])
print(f"  Printable ASCII: '{chars1}'")
other1 = [hex(u) for u in unicodes1 if u < 32 or u >= 127]
print(f"  Other unicodes: {other1}")

with open(r'Assets\Fonts\TMP_Assets\RetroGaming_SDF.asset', 'r', encoding='utf-8', errors='ignore') as f:
    t2 = f.read()

unicodes2 = sorted([int(x) for x in re.findall(r'm_Unicode:\s*(\d+)', t2)])
print(f"\nRetroGaming has {len(unicodes2)} characters:")
chars2 = "".join([chr(u) for u in unicodes2 if 32 <= u < 127])
print(f"  Printable ASCII: '{chars2}'")
other2 = [hex(u) for u in unicodes2 if u < 32 or u >= 127]
print(f"  Other unicodes: {other2}")
