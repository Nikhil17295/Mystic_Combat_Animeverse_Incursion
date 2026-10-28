with open(r'Assets\Prefabs\UI\HUD_Canvas.prefab', 'r', encoding='utf-8') as f:
    text = f.read()

import re

# Find CombatMenuController block
cmc_match = re.search(r'CombatMenuController[\s\S]*?(?=--- !u!|\Z)', text)
if cmc_match:
    cmc_block = cmc_match.group(0)
    print("CombatMenuController block:")
    print(cmc_block)
    # Check all fileIDs
    file_ids = re.findall(r'fileID:\s*(\d+)', cmc_block)
    print(f"\nChecking {len(file_ids)} fileIDs in HUD_Canvas.prefab:")
    for fid in file_ids:
        found = f"&{fid}" in text
        print(f"  fileID {fid}: found in prefab = {found}")
