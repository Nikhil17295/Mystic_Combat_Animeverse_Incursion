with open(r'Assets\Prefabs\UI\HUD_Canvas.prefab', 'r', encoding='utf-8') as f:
    text = f.read()

import re

# find all GameObject names in HUD_Canvas.prefab
names = re.findall(r'GameObject:[\s\S]*?m_Name:\s*([^\r\n]+)', text)
print("GameObject names in HUD_Canvas.prefab:")
for n in names:
    if any(k in n for k in ['Pause', 'Results', 'Btn', 'Modal', 'Score', 'Winner', 'Epithet']):
        print(f"  {n}")
