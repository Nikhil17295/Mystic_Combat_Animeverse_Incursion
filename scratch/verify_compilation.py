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
    sources = []
    
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
                    sources.append(src_path)
        elif tag == "DefineConstants":
            if elem.text:
                defines.extend(elem.text.strip().split(';'))

    # If compiling Editor assembly, ensure test runtime dll is referenced
    if "Editor" in csproj_path:
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
        f"/out:{out_path}",
        "/langversion:9.0",
        "/noconfig",
        "/nostdlib",
    ]
    
    if defines:
        cmd.append(f"/define:{';'.join(set(defines))}")
        
    for r in references:
        cmd.append(f"/r:{r}")
        
    for s in sources:
        cmd.append(s)

    # Use response file
    rsp_path = os.path.join(PROJECT_ROOT, "Temp", f"{out_dll_name}.rsp")
    with open(rsp_path, "w", encoding="utf-8") as f:
        for arg in cmd:
            if " " in arg and not (arg.startswith('"') and arg.endswith('"')):
                arg = f'"{arg}"'
            f.write(arg + "\n")
            
    print(f"Compiling {len(sources)} source files with {len(references)} references...")
    result = subprocess.run([DOTNET_PATH, CSC_DLL_PATH, f"@{rsp_path}"], capture_output=True, text=True)
    
    # Filter output: errors vs warnings
    lines = result.stdout.splitlines() + result.stderr.splitlines()
    errors = [l for l in lines if ": error " in l]
    warnings = [l for l in lines if ": warning " in l]
    
    if len(errors) == 0 and result.returncode == 0:
        print(f"-> SUCCESS! {out_dll_name} compiled with ZERO (0) ERRORS!")
        if warnings:
            print(f"   ({len(warnings)} compiler warnings present - informational only)")
        return True
    else:
        print(f"-> FAILED with {len(errors)} errors (exit code {result.returncode}):")
        for err in errors:
            print("  ", err)
        if not errors:
            print("\nFull output:\n", result.stdout, result.stderr)
        return False

if __name__ == "__main__":
    ok1 = check_project(os.path.join(PROJECT_ROOT, "Assembly-CSharp.csproj"), "test_Assembly-CSharp.dll")
    ok2 = check_project(os.path.join(PROJECT_ROOT, "Assembly-CSharp-Editor.csproj"), "test_Assembly-CSharp-Editor.dll")
    
    print("\n--------------------------------------------------------")
    if ok1 and ok2:
        print(">>> ALL C# ASSEMBLIES COMPILED CLEANLY WITH ZERO (0) ERRORS! <<<")
        print("--------------------------------------------------------")
        sys.exit(0)
    else:
        print(">>> COMPILATION ERRORS DETECTED! <<<")
        print("--------------------------------------------------------")
        sys.exit(1)
