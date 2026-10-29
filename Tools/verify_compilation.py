import os
import xml.etree.ElementTree as ET
import subprocess

tree = ET.parse("Assembly-CSharp.csproj")
root = tree.getroot()

cs_files = []
dll_refs = []

for elem in root.iter():
    if elem.tag.endswith("Compile"):
        include = elem.attrib.get("Include")
        if include and include.endswith(".cs") and os.path.exists(include):
            cs_files.append(os.path.abspath(include).replace("\\", "/"))
    elif elem.tag.endswith("Reference"):
        hint = elem.find("{*}HintPath")
        if hint is not None and hint.text:
            dll_path = hint.text.strip()
            if os.path.exists(dll_path):
                dll_refs.append(os.path.abspath(dll_path).replace("\\", "/"))

# Also add NetStandard shims
ns_dir = "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/NetStandard/compat/2.1.0/shims/netstandard"
if os.path.exists(ns_dir):
    for f in os.listdir(ns_dir):
        if f.endswith(".dll"):
            dll_refs.append(os.path.abspath(os.path.join(ns_dir, f)).replace("\\", "/"))

# Include any newly created .cs files not yet in csproj
for r, d, files in os.walk("Assets/Scripts"):
    for f in files:
        if f.endswith(".cs"):
            full = os.path.abspath(os.path.join(r, f)).replace("\\", "/")
            if full not in cs_files:
                cs_files.append(full)

print(f"Total C# files: {len(cs_files)}")
print(f"Total Managed DLL references: {len(dll_refs)}")

rsp_path = "Tools/csc_args.rsp"
with open(rsp_path, "w", encoding="utf-8") as f:
    f.write("-target:library\n")
    f.write("-out:Temp/TestBuild.dll\n")
    f.write("-langversion:9.0\n")
    f.write("-define:UNITY_EDITOR;UNITY_6000;UNITY_6000_6\n")
    for r in dll_refs:
        f.write(f'\"-r:{r}\"\n')
    for cs in cs_files:
        f.write(f'\"{cs}\"\n')

dotnet = "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/DotNetSdk/dotnet.exe"
csc_dll = "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll"

env = os.environ.copy()
env["DOTNET_CLI_HOME"] = os.path.abspath("Tools/.dotnet")

cmd = [dotnet, "exec", csc_dll, f"@{rsp_path}"]
res = subprocess.run(cmd, capture_output=True, text=True, env=env)

print("Return code:", res.returncode)
if res.stdout:
    print("STDOUT:\n", res.stdout)
if res.stderr:
    print("STDERR:\n", res.stderr)

if res.returncode == 0:
    print("\n>>> ZERO COMPILATION ERRORS! PROJECT COMPILES 100% CLEANLY! <<<")
