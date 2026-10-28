content = """

---

### Bug 43: Asynchronous Socket Reception Thread Safety Collision & C# Lexical Region Directives (CS1038)

- **Date:** October 28, 2026
- **Severity:** High (LAN Architecture Integrity & Build Pipeline)
- **Component:** `Assets/Scripts/Network/LanNetworkManager.cs`, `Assets/Scripts/UI/CharacterSelectController.cs`, `scratch/verify_compilation.py`

#### Symptom:
1. When developing the LAN multiplayer socket listener, incoming network packets received on the OS background thread (`ReceiveThread`) risked throwing `InvalidOperationException` when attempting to interact with Unity components or load scenes directly.
2. During assembly verification, the Roslyn compiler rejected `CharacterSelectController.cs` with `error CS1038: #endregion directive expected`.
3. In the verification script `scratch/verify_compilation.py`, newly added `.cs` source files were initially omitted from the static `.csproj` XML until automated disk scanning and argument path quoting were introduced.

#### Root Causes:
1. **Unity Engine Thread Affinity:**
   In Unity, the GameObject lifecycle, Transform hierarchy, physics engine, and UI canvases are strictly bound to the main thread. Asynchronous TCP sockets operate on background worker threads allocated by the OS thread pool. Invoking Unity methods directly from a background socket callback violates Unity's thread affinity model.
2. **Missing Region Closure in C# Directives:**
   During the insertion of lifecycle network listener hooks (`OnEnable()` and `OnDisable()`), a `#region Unity Lifecycle Events` block was introduced without an immediate closing `#endregion` before `#region Unity Callbacks`. Furthermore, an inner `#region LAN Network Callbacks` was nested inside `#region Input Handling` without closing the outer scope, violating C# preprocessor lexical rules.
3. **Response File Space Delimiting in Roslyn CSC:**
   When invoking the C# compiler (`csc.dll`) via an `@args.rsp` response file, command-line arguments separated by whitespace are parsed as distinct positional tokens. Paths containing spaces (e.g. `C:\\Program Files\\Unity...`) were split into `/r:C:\\Program` and `Files\\Unity...`, causing the compiler to misinterpret reference flags as missing source files.

#### Resolution:
1. **Thread-Safe Main Thread Action Queue (`ConcurrentQueue<Action>`):**
   - In `LanNetworkManager.cs`, added a thread-safe `ConcurrentQueue<Action> mainThreadActions`.
   - When the background reading thread decodes a packet (`FighterInput`, `MatchSnapshot`, `CharSelectSync`), it enqueues an action lambda:
     ```csharp
     mainThreadActions.Enqueue(() => OnRemoteInputReceived?.Invoke(inputPacket));
     ```
   - In `Update()`, the main thread drains the queue safely every frame:
     ```csharp
     while (mainThreadActions.TryDequeue(out Action action))
     {
         action?.Invoke();
     }
     ```
2. **Normalized C# Region Scope in `CharacterSelectController.cs`:**
   - Corrected all `#region` blocks, ensuring every `#region` is paired with an exact `#endregion` before declaring sibling sections.
3. **Quoted Response File Arguments in `verify_compilation.py`:**
   - Wrapped output paths, reference DLLs, and source file paths in explicit double quotes (`f'/r:\"{r}\"'` and `f'\"{s}\"'`).
   - Added automatic disk walk discovery across `Assets/` to incorporate newly added scripts into the compilation test.

#### Verification:
- Ran `python scratch/verify_compilation.py`: Both `Assembly-CSharp.csproj` (41 source files) and `Assembly-CSharp-Editor.csproj` (4 source files) compiled cleanly with **ZERO (0) ERRORS**.
"""

with open('Docs/05_DEV_LOG_AND_BUGS.md', 'a', encoding='utf-8') as f:
    f.write(content)

print("Docs/05_DEV_LOG_AND_BUGS.md updated.")
