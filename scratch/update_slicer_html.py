path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Tools\SpriteSlicer\index.html"
with open(path, "r", encoding="utf-8") as f:
    code = f.read()

# 1. Update character selector buttons
old_buttons = """            <div class="char-selector">
                <button class="char-btn" id="btnCharGoku" onclick="selectCharacter('goku')">Goku (Base)</button>
                <button class="char-btn active" id="btnCharGokuSSJ" onclick="selectCharacter('goku_ssj')" style="border-color:#fbbf24; color:#fbbf24;">⚡ Goku (SSJ)</button>
                <button class="char-btn" id="btnCharSaitama" onclick="selectCharacter('saitama')">Saitama</button>
                <button class="char-btn" id="btnCharCustom" onclick="selectCharacter('custom')">🧪 Custom / Test</button>
            </div>"""

new_buttons = """            <div class="char-selector">
                <button class="char-btn active" id="btnCharGokuSSJ" onclick="selectCharacter('goku_ssj')" style="border-color:#fbbf24; color:#fbbf24; font-weight:700;">⚡ Goku (Super Saiyan)</button>
                <button class="char-btn" id="btnCharGoku" onclick="selectCharacter('goku')">Goku (Base)</button>
                <button class="char-btn" id="btnCharSaitama" onclick="selectCharacter('saitama')">Saitama</button>
                <button class="char-btn" id="btnCharCustom" onclick="selectCharacter('custom')">🧪 Custom / Test</button>
            </div>"""

# 2. Update currentCharacter default
old_app_state = """        // App State
        let currentCharacter = "goku";"""
new_app_state = """        // App State
        let currentCharacter = "goku_ssj";"""

# 3. Update characterData with clean Goku SSJ and Base Goku
old_char_data = """        // Stored Character Data
        const characterData = {
            goku: {
                name: "Goku",
                isLocal: false,
                sheets: [
                    { name: "Super Saiyan JUS (Clean Transparent)", path: "Fighters/Goku/goku_ssj_transparent.png", bgColor: null },
                    { name: "Base Goku JUS (Clean Transparent)", path: "Fighters/Goku/goku_transparent.png", bgColor: null },
                    { name: "Original JUS (Blue BG)", path: "Fighters/Goku/DS _ DSi - Jump Ultimate Stars - Fighters - Goku.png", bgColor: [128, 128, 255] }
                ],
                activeSheetIndex: 0,
                animations: {
                    idle: [
                        {x: 22, y: 25, w: 47, h: 47},
                        {x: 115, y: 25, w: 46, h: 47},
                        {x: 208, y: 25, w: 47, h: 47},
                        {x: 301, y: 25, w: 46, h: 47}
                    ],
                    walk: [],
                    jump: [],
                    crouch: [],
                    punch: [],
                    heavy: [],
                    skill: [],
                    ultimate: [],
                    meditate: [],
                    hurt: [],
                    knockout: [],
                    victory: []
                }
            },
            goku_ssj: {
                name: "Goku (Super Saiyan)",
                isLocal: false,
                sheets: [
                    { name: "Super Saiyan JUS (Clean Transparent)", path: "Fighters/Goku/goku_ssj_transparent.png", bgColor: null }
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
                    knockout: [],
                    victory: []
                }
            },"""

new_char_data = """        // Stored Character Data
        const characterData = {
            goku_ssj: {
                name: "Goku (Super Saiyan)",
                isLocal: false,
                sheets: [
                    { name: "Super Saiyan JUS (Clean Transparent)", path: "Fighters/Goku/goku_ssj_transparent.png", bgColor: null },
                    { name: "Super Saiyan JUS (Original Blue BG)", path: "Fighters/Goku/DS _ DSi - Jump Ultimate Stars - Fighters - Goku (Super Saiyan).png", bgColor: [128, 128, 255] }
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
                    knockout: [],
                    victory: []
                }
            },
            goku: {
                name: "Goku (Base)",
                isLocal: false,
                sheets: [
                    { name: "Base Goku JUS (Clean Transparent)", path: "Fighters/Goku/goku_transparent.png", bgColor: null },
                    { name: "Original JUS (Blue BG)", path: "Fighters/Goku/DS _ DSi - Jump Ultimate Stars - Fighters - Goku.png", bgColor: [128, 128, 255] }
                ],
                activeSheetIndex: 0,
                animations: {
                    idle: [
                        {x: 22, y: 25, w: 47, h: 47},
                        {x: 115, y: 25, w: 46, h: 47},
                        {x: 208, y: 25, w: 47, h: 47},
                        {x: 301, y: 25, w: 46, h: 47}
                    ],
                    walk: [],
                    jump: [],
                    crouch: [],
                    punch: [],
                    heavy: [],
                    skill: [],
                    ultimate: [],
                    meditate: [],
                    hurt: [],
                    knockout: [],
                    victory: []
                }
            },"""

# 4. Update loadActiveSheet to support file:/// protocol AND cache-busting
old_load = """            if (sheet.isLocalBlob) {
                sheetImage.src = sheet.blobUrl;
            } else {
                const encodedPath = encodeURI(sheet.path);
                sheetImage.src = "/sprites/" + encodedPath;
            }"""

new_load = """            if (sheet.isLocalBlob) {
                sheetImage.src = sheet.blobUrl;
            } else if (window.location.protocol === "file:") {
                // Direct double-click of HTML file from Windows Explorer
                sheetImage.src = "../../Assets/Sprites/" + encodeURI(sheet.path);
            } else {
                // Served via local HTTP server with cache-busting timestamp
                const encodedPath = encodeURI(sheet.path);
                sheetImage.src = "/sprites/" + encodedPath + "?v=" + Date.now();
            }"""

# Normalize and replace
code_norm = code.replace("\r\n", "\n")
old_buttons_norm = old_buttons.replace("\r\n", "\n")
new_buttons_norm = new_buttons.replace("\r\n", "\n")
old_app_state_norm = old_app_state.replace("\r\n", "\n")
new_app_state_norm = new_app_state.replace("\r\n", "\n")
old_char_data_norm = old_char_data.replace("\r\n", "\n")
new_char_data_norm = new_char_data.replace("\r\n", "\n")
old_load_norm = old_load.replace("\r\n", "\n")
new_load_norm = new_load.replace("\r\n", "\n")

if old_buttons_norm in code_norm:
    code_norm = code_norm.replace(old_buttons_norm, new_buttons_norm, 1)
    print("Replaced buttons!")
else:
    print("Could not find old_buttons!")

if old_app_state_norm in code_norm:
    code_norm = code_norm.replace(old_app_state_norm, new_app_state_norm, 1)
    print("Replaced app state!")
else:
    print("Could not find old_app_state!")

if old_char_data_norm in code_norm:
    code_norm = code_norm.replace(old_char_data_norm, new_char_data_norm, 1)
    print("Replaced characterData!")
else:
    print("Could not find old_char_data!")

if old_load_norm in code_norm:
    code_norm = code_norm.replace(old_load_norm, new_load_norm, 1)
    print("Replaced loadActiveSheet!")
else:
    print("Could not find old_load!")

with open(path, "w", encoding="utf-8") as f:
    f.write(code_norm)
print("Updated index.html successfully!")
