path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Tools\SpriteSlicer\index.html"
with open(path, "r", encoding="utf-8") as f:
    code = f.read()

target = """            <!-- Floating Canvas Toolbar -->
            <div class="canvas-toolbar">
                <span>Selection:</span>
                <span class="coord-badge" id="coordBadge">None</span>
                <span>Zoom: <strong id="zoomLevel">1.5x</strong></span>
                <span style="color:#4a5073;">|</span>
                <span id="imageInfoBadge" style="font-size: 0.75rem; color: #88c0d0; font-family: monospace;">Loading...</span>
                <span style="color:#4a5073;">|</span>
                <span style="font-size: 0.75rem; color:#888;">Pan: <kbd>Right-Click</kbd> or <kbd>Space + Drag</kbd></span>
            </div>"""

replacement = """            <!-- Floating Canvas Toolbar -->
            <div class="canvas-toolbar">
                <span id="activeCharacterBadge" style="background: rgba(251, 191, 36, 0.15); border: 1px solid #fbbf24; color: #fbbf24; padding: 2px 8px; border-radius: 4px; font-weight: 700; font-size: 0.75rem;">⚡ Goku (Super Saiyan)</span>
                <span style="color:#4a5073;">|</span>
                <span>Selection:</span>
                <span class="coord-badge" id="coordBadge">None</span>
                <span>Zoom: <strong id="zoomLevel">1.5x</strong></span>
                <span style="color:#4a5073;">|</span>
                <span id="imageInfoBadge" style="font-size: 0.75rem; color: #88c0d0; font-family: monospace;">Loading...</span>
                <span style="color:#4a5073;">|</span>
                <span style="font-size: 0.75rem; color:#888;">Pan: <kbd>Right-Click</kbd> or <kbd>Space + Drag</kbd></span>
            </div>"""

target_sel = """        function selectCharacter(char) {
            currentCharacter = char;

            document.getElementById("btnCharGoku").classList.toggle("active", char === "goku");
            const btnGokuSSJ = document.getElementById("btnCharGokuSSJ");
            if (btnGokuSSJ) btnGokuSSJ.classList.toggle("active", char === "goku_ssj");
            document.getElementById("btnCharSaitama").classList.toggle("active", char === "saitama");"""

replacement_sel = """        function selectCharacter(char) {
            currentCharacter = char;

            const btnGokuSSJ = document.getElementById("btnCharGokuSSJ");
            if (btnGokuSSJ) btnGokuSSJ.classList.toggle("active", char === "goku_ssj");
            document.getElementById("btnCharGoku").classList.toggle("active", char === "goku");
            document.getElementById("btnCharSaitama").classList.toggle("active", char === "saitama");

            const charBadge = document.getElementById("activeCharacterBadge");
            if (charBadge) {
                if (char === "goku_ssj") {
                    charBadge.textContent = "⚡ Goku (Super Saiyan)";
                    charBadge.style.color = "#fbbf24";
                    charBadge.style.borderColor = "#fbbf24";
                    charBadge.style.background = "rgba(251, 191, 36, 0.15)";
                } else if (char === "goku") {
                    charBadge.textContent = "🥋 Goku (Base)";
                    charBadge.style.color = "#ff7700";
                    charBadge.style.borderColor = "#ff7700";
                    charBadge.style.background = "rgba(255, 119, 0, 0.15)";
                } else if (char === "saitama") {
                    charBadge.textContent = "🥊 Saitama";
                    charBadge.style.color = "#ef4444";
                    charBadge.style.borderColor = "#ef4444";
                    charBadge.style.background = "rgba(239, 68, 68, 0.15)";
                } else {
                    charBadge.textContent = "🧪 " + (characterData.custom.name || "Custom");
                    charBadge.style.color = "#2563eb";
                    charBadge.style.borderColor = "#2563eb";
                    charBadge.style.background = "rgba(37, 99, 235, 0.15)";
                }
            }"""

code_norm = code.replace("\r\n", "\n")
target_norm = target.replace("\r\n", "\n")
rep_norm = replacement.replace("\r\n", "\n")
target_sel_norm = target_sel.replace("\r\n", "\n")
rep_sel_norm = replacement_sel.replace("\r\n", "\n")

if target_norm in code_norm:
    code_norm = code_norm.replace(target_norm, rep_norm, 1)
    print("Replaced toolbar!")
else:
    print("Could not find toolbar target!")

if target_sel_norm in code_norm:
    code_norm = code_norm.replace(target_sel_norm, rep_sel_norm, 1)
    print("Replaced selectCharacter!")
else:
    print("Could not find selectCharacter target!")

with open(path, "w", encoding="utf-8") as f:
    f.write(code_norm)
print("Updated index.html badge!")
