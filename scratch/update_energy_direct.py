with open('Assets/Scripts/Systems/EnergySystem.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = """        public void ResetKi()
        {
            currentKi = 0f;
            isInRedZone = false;
            redZoneTimer = 0f;
            OnKiChanged?.Invoke(currentKi, maxKi);
        }"""

replacement = """        public void ResetKi()
        {
            currentKi = 0f;
            isInRedZone = false;
            redZoneTimer = 0f;
            OnKiChanged?.Invoke(currentKi, maxKi);
        }

        /// <summary>
        /// Directly synchronizes Ki from an authoritative LAN snapshot.
        /// </summary>
        public void SetKiDirect(float val)
        {
            currentKi = Mathf.Clamp(val, 0f, maxKi);
            OnKiChanged?.Invoke(currentKi, maxKi);
        }"""

text = text.replace('\r\n', '\n')
target = target.replace('\r\n', '\n')
replacement = replacement.replace('\r\n', '\n')

if target in text:
    text = text.replace(target, replacement)
    with open('Assets/Scripts/Systems/EnergySystem.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("EnergySystem updated successfully.")
else:
    print("Target not found in EnergySystem.")
