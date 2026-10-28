import os
import sys
import xml.etree.ElementTree as ET
import subprocess

PROJECT_ROOT = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion"
DOTNET_PATH = r"C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe"
CSC_DLL_PATH = r"C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll"

def check_project(csproj_path, out_dll_name):
    print(f"\n========================================================")
    print(f"Checking compilation for: {os.path.basename(csproj_path)}")
    print(f"========================================================")
    
    tree = ET.parse(csproj_path)
    root = tree.getroot()
    
    references = []
    defines = []
    sources = set()
    is_editor = "Editor" in csproj_path
    
    for elem in root.iter():
        tag = elem.tag.split('}')[-1] if '}' in elem.tag else elem.tag
        if tag == "Reference":
            hint_elem = elem.find("HintPath")
            if hint_elem is not None and hint_elem.text:
                ref_path = hint_elem.text.strip()
                if os.path.exists(ref_path):
                    references.append(ref_path)
                else:
                    rel = os.path.join(PROJECT_ROOT, ref_path)
                    if os.path.exists(rel):
                        references.append(rel)
        elif tag == "Compile":
            inc = elem.attrib.get("Include")
            if inc:
                src_path = os.path.join(PROJECT_ROOT, inc)
                if os.path.exists(src_path):
                    sources.add(os.path.abspath(src_path))
        elif tag == "DefineConstants":
            if elem.text:
                defines.extend(elem.text.strip().split(';'))

    # Automatically discover all .cs files in Assets to catch newly added files
    assets_dir = os.path.join(PROJECT_ROOT, "Assets")
    for r, d, files in os.walk(assets_dir):
        for f in files:
            if f.endswith(".cs"):
                full_p = os.path.abspath(os.path.join(r, f))
                in_editor = "\\Editor\\" in full_p or "/Editor/" in full_p
                if is_editor and in_editor:
                    sources.add(full_p)
                elif not is_editor and not in_editor:
                    sources.add(full_p)

    sources = sorted(list(sources))

    # If compiling Editor assembly, ensure test runtime dll is referenced
    if is_editor:
        runtime_dll = os.path.join(PROJECT_ROOT, "Temp", "test_Assembly-CSharp.dll")
        if os.path.exists(runtime_dll):
            references.append(runtime_dll)
        else:
            lib_runtime = os.path.join(PROJECT_ROOT, "Library", "ScriptAssemblies", "Assembly-CSharp.dll")
            if os.path.exists(lib_runtime):
                references.append(lib_runtime)

    os.makedirs(os.path.join(PROJECT_ROOT, "Temp"), exist_ok=True)
    out_path = os.path.join(PROJECT_ROOT, "Temp", out_dll_name)
    
    cmd = [
        "/target:library",
        f'/out:"{out_path}"',
        "/langversion:9.0",
        "/noconfig",
        "/nostdlib",
    ]
    
    if defines:
        cmd.append(f"/define:{';'.join(set(defines))}")
        
    for r in references:
        cmd.append(f'/r:"{r}"')
        
    for s in sources:
        cmd.append(f'"{s}"')

    # Write response file
    rsp_path = os.path.join(PROJECT_ROOT, "Temp", f"{out_dll_name}.rsp")
    with open(rsp_path, "w", encoding="utf-8") as f:
        for arg in cmd:
            f.write(f"{arg}\n")
            
    print(f"Compiling {len(sources)} source files with {len(references)} references...")
    
    proc = subprocess.run(
        [DOTNET_PATH, CSC_DLL_PATH, f"@{rsp_path}"],
        capture_output=True,
        text=True,
        cwd=PROJECT_ROOT
    )
    
    if proc.returncode == 0:
        print(f"-> SUCCESS! Compiled {out_dll_name} without errors.")
        return True
    else:
        print(f"-> FAILED with {proc.stdout.count('error CS')} errors (exit code {proc.returncode}):")
        for line in proc.stdout.splitlines():
            if "error CS" in line:
                print("   " + line)
        return False

if __name__ == "__main__":
    runtime_ok = check_project(os.path.join(PROJECT_ROOT, "Assembly-CSharp.csproj"), "test_Assembly-CSharp.dll")
    editor_ok = check_project(os.path.join(PROJECT_ROOT, "Assembly-CSharp-Editor.csproj"), "test_Assembly-CSharp-Editor.dll")
    
    if runtime_ok and editor_ok:
        print("\n--------------------------------------------------------")
        print(">>> ALL C# PROJECTS COMPILED CLEANLY WITH ZERO ERRORS <<<")
        print("--------------------------------------------------------\n")
        sys.exit(0)
    else:
        print("\n--------------------------------------------------------")
        print(">>> COMPILATION ERRORS DETECTED! <<<")
        print("--------------------------------------------------------\n")
        sys.exit(1)
