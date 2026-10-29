# -*- coding: utf-8 -*-
with open('Tools/SpriteSlicer/index.html', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Header button
old_btn = '<button class="char-btn" id="btnCharSaitama" onclick="selectCharacter(\'saitama\')">Saitama</button>'
new_btn = '<button class="char-btn" id="btnCharSaitama" onclick="selectCharacter(\'saitama\')">Saitama</button>\r\n                <button class="char-btn" id="btnCharTsunade" onclick="selectCharacter(\'tsunade\')">🌸 Tsunade</button>'
assert old_btn in content, 'old_btn not found'
content = content.replace(old_btn, new_btn, 1)

# 2. Export folder select
old_opt = '<option value="Fighters/Saitama/Frames">Assets/.../Saitama/Frames</option>'
new_opt = '<option value="Fighters/Saitama/Frames">Assets/.../Saitama/Frames</option>\r\n                            <option value="Fighters/Tsunade/Frames">Assets/.../Tsunade/Frames</option>'
assert old_opt in content, 'old_opt not found'
content = content.replace(old_opt, new_opt, 1)

# 3. characterData tsunade
tsunade_block = '''            tsunade: {
                name: "Tsunade",
                isLocal: false,
                sheets: [
                    { name: "Tsunade (Clean Transparent)", path: "Fighters/Tsunade/tsunade_transparent.png", bgColor: null },
                    { name: "Tsunade (Ninja Council 4 / Original)", path: "Fighters/Tsunade/DS _ DSi - Naruto Shippuden_ Ninja Council 4 - Playable Characters - Tsunade.png", bgColor: null },
                    { name: "Tsunade (Sheet Alias)", path: "Fighters/Tsunade/tsunade_sheet.png", bgColor: null }
                ],
                activeSheetIndex: 0,
                animations: {
                    idle: [],
                    walk: [],
                    jump: [],
                    crouch: [],
                    punch: [],
                    heavy: [],
                    skill: [],
                    ultimate: [],
                    meditate: [],
                    hurt: [],
                    death: [],
                    victory: []
                }
            },\r\n'''
old_custom = '            custom: {'
assert old_custom in content, 'old_custom not found'
content = content.replace(old_custom, tsunade_block + old_custom, 1)

# 4. selectCharacter button toggle & badge
old_toggle = 'document.getElementById("btnCharSaitama").classList.toggle("active", char === "saitama");'
new_toggle = 'document.getElementById("btnCharSaitama").classList.toggle("active", char === "saitama");\r\n            const btnTsunade = document.getElementById("btnCharTsunade");\r\n            if (btnTsunade) btnTsunade.classList.toggle("active", char === "tsunade");'
assert old_toggle in content, 'old_toggle not found'
content = content.replace(old_toggle, new_toggle, 1)

old_badge_saitama = 'charBadge.style.background = "rgba(239, 68, 68, 0.15)";'
new_badge_tsunade = '''charBadge.style.background = "rgba(239, 68, 68, 0.15)";
                } else if (char === "tsunade") {
                    charBadge.textContent = "🌸 Tsunade (5th Hokage)";
                    charBadge.style.color = "#10b981";
                    charBadge.style.borderColor = "#10b981";
                    charBadge.style.background = "rgba(16, 185, 129, 0.15)";'''
assert old_badge_saitama in content, 'old_badge_saitama not found'
content = content.replace(old_badge_saitama, new_badge_tsunade, 1)

# 5. Default init to tsunade
old_init = 'selectCharacter("goku_ssj");'
new_init = 'selectCharacter("tsunade");'
assert old_init in content, 'old_init not found'
content = content.replace(old_init, new_init, 1)

with open('Tools/SpriteSlicer/index.html', 'w', encoding='utf-8', newline='') as f:
    f.write(content)

print('Successfully applied clean Tsunade updates to Tools/SpriteSlicer/index.html!')
