with open("Assembly-CSharp.csproj", "r", encoding="utf-8") as f:
    text = f.read()

t = '<Compile Include="Assets\\Scripts\\Combat\\HitboxController.cs" />'
r = """<Compile Include="Assets\\Scripts\\Combat\\HitboxController.cs" />
    <Compile Include="Assets\\Scripts\\Combat\\VFXManager.cs" />
    <Compile Include="Assets\\Scripts\\Combat\\FighterKiAura.cs" />"""

if t in text:
    text = text.replace(t, r, 1)
    with open("Assembly-CSharp.csproj", "w", encoding="utf-8") as f:
        f.write(text)
    print("Assembly-CSharp.csproj updated with new combat scripts!")
else:
    print("Target not found in Assembly-CSharp.csproj!")
