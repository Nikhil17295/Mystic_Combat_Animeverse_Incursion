with open(r'Assets\Fonts\TMP_Assets\ArcadeClassic_SDF.asset', 'r', encoding='utf-8', errors='ignore') as f:
    t1 = f.read()
with open(r'Assets\Fonts\TMP_Assets\RetroGaming_SDF.asset', 'r', encoding='utf-8', errors='ignore') as f:
    t2 = f.read()

chars_to_test = ['>', '<', '-', '*', '[', ']', '|']
for c in chars_to_test:
    code = ord(c)
    in_ac = f"m_Unicode: {code}" in t1
    in_rg = f"m_Unicode: {code}" in t2
    print(f"Char '{c}' (0x{code:X}): in ArcadeClassic: {in_ac}, in RetroGaming: {in_rg}")
