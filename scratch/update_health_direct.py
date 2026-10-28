with open('Assets/Scripts/Systems/HealthSystem.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = """        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }"""

replacement = """        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Directly synchronizes health from an authoritative LAN snapshot.
        /// </summary>
        public void SetHealthDirect(int val)
        {
            currentHealth = Mathf.Clamp(val, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }"""

text = text.replace('\r\n', '\n')
target = target.replace('\r\n', '\n')
replacement = replacement.replace('\r\n', '\n')

if target in text:
    text = text.replace(target, replacement)
    with open('Assets/Scripts/Systems/HealthSystem.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("HealthSystem updated successfully.")
else:
    print("Target not found in HealthSystem.")
