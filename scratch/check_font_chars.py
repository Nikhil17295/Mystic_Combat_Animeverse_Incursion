with open(r'Assets\Fonts\TMP_Assets\ArcadeClassic_SDF.asset', 'r', encoding='utf-8', errors='ignore') as f:
    t1 = f.read()
with open(r'Assets\Fonts\TMP_Assets\RetroGaming_SDF.asset', 'r', encoding='utf-8', errors='ignore') as f:
    t2 = f.read()

chars_to_test = [
    ('RIGHT_TRIANGLE', 0x25B6),
    ('LEFT_TRIANGLE', 0x25C0),
    ('EM_DASH', 0x2014),
    ('BULLET', 0x2022),
    ('STAR', 0x2605),
]
for name, code in chars_to_test:
    in_ac = f"m_Unicode: {code}" in t1
    in_rg = f"m_Unicode: {code}" in t2
    print(f"{name} (0x{code:X}): in ArcadeClassic: {in_ac}, in RetroGaming: {in_rg}")
