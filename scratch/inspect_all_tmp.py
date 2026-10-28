with open(r'Assets\Prefabs\UI\HUD_Canvas.prefab', 'r', encoding='utf-8') as f:
    text = f.read()

import re

fids = [
    ('pauseBtnText0', '4559421345588250078'),
    ('pauseBtnText1', '4534814605807488412'),
    ('pauseBtnText2', '287973825205142015'),
    ('pauseBtnText3', '8294445766462903913'),
    ('resultsTitle', '4048825498468611435'),
    ('resultsWinner', '7436857057322700676'),
    ('resultsEpithet', '8659782911892112190'),
    ('resultsScore', '7963487613820214320'),
    ('resultsBtn0', '5202128560018282419'),
    ('resultsBtn1', '7164026911889093709'),
    ('resultsBtn2', '5618127270247238622')
]

for name, fid in fids:
    m = re.search(r'&' + fid + r'\s+MonoBehaviour:[\s\S]*?(?=--- !u!|\Z)', text)
    if m:
        block = m.group(0)
        font_match = re.search(r'm_fontAsset:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]+)', block)
        guid = font_match.group(1) if font_match else 'NONE'
        text_match = re.search(r'm_text:\s*(.*)', block)
        txt = text_match.group(1) if text_match else 'NONE'
        print(f"{name:15}: font_guid={guid}, text={txt}")
    else:
        print(f"{name:15}: NOT FOUND")
