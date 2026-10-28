with open('Assets/Scripts/Fighter/FighterController.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = """        public void SetBaseTintColor(Color tint)
        {
            baseTintColor = tint;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = tint;
            }
        }"""

replacement = """        public void SetBaseTintColor(Color tint)
        {
            baseTintColor = tint;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = tint;
            }
        }

        /// <summary>
        /// Forces fighter state from an authoritative network snapshot (used by Client).
        /// </summary>
        public void ForceNetworkState(FighterState state)
        {
            CurrentState = state;
        }

        /// <summary>
        /// Sets horizontal facing flip directly from network state.
        /// </summary>
        public void SetFacing(bool flipX)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = flipX;
            }
        }"""

text = text.replace('\r\n', '\n')
target = target.replace('\r\n', '\n')
replacement = replacement.replace('\r\n', '\n')

if target in text:
    text = text.replace(target, replacement)
    with open('Assets/Scripts/Fighter/FighterController.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("FighterController updated successfully.")
else:
    print("Target not found in FighterController.")
