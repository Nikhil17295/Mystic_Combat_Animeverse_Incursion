content = """

---

## 40. Local Area Network (LAN) Multiplayer & Sockets for Beginners

### 1. What is LAN Multiplayer?
In single-player or local multiplayer games, both players sit at the same physical computer and share one screen and keyboard. In **Local Area Network (LAN) Multiplayer**, two separate computers (Laptop A and Laptop B) are connected to the same Wi-Fi router or phone mobile hotspot. They exchange data packets across the local room without needing any internet servers (Steam, AWS, or cloud matchmakers).

### 2. The Host vs. Client Architecture
To play a multiplayer fighting game, one computer must act as the **Host**, and the other acts as the **Client**:
- **The Host (PC 1):** The "boss" machine. It opens a server listener on a chosen network port (e.g. `7777`) and runs the actual fighting game simulation—calculating hits, damage, physics velocities, and round timers.
- **The Client (PC 2):** Connects to the Host's IP address (e.g. `192.168.1.15`). The Client reads Player 2's local keyboard inputs and streams them to the Host. In return, the Client receives the Host's 60Hz match snapshot and smoothly mirrors the action on screen!

```
[ PC 1: THE HOST ]                                   [ PC 2: THE CLIENT ]
Runs Full Combat Simulation                         Reads Player 2 Local Keyboard
   • Fighter 1: Local Inputs                           • Move, Jump, Punch, Skill
   • Fighter 2: NetworkFighterInput ◄── [Inputs] ─────── Streams buttons to Host
   • Broadcasts 60Hz Snapshot ───────── [Snapshot] ────► Mirrors Sprites & HUD
```

### 3. IP Addresses & Ports Explained Simply
- **IP Address (Internet Protocol):** Like a house address. On a home Wi-Fi network, your router assigns your computer a local address like `192.168.1.24`. When you play alone on one machine, you use `127.0.0.1` (the universal computer alias for **Localhost** / "this computer").
- **Port:** Like an apartment door number. Your computer has thousands of ports. Mystic Combat uses port `7777`. When the Host listens on port `7777`, it tells Windows: *"Any network messages sent to door 7777 belong to Mystic Combat!"*

### 4. Nagle's Algorithm & Why `NoDelay = true` Matters in Fighting Games
By default, standard computer networking uses **Nagle's Algorithm**. To save bandwidth, Windows waits up to $40\text{ms}$ to gather small messages into one big envelope before sending them.
- In email or file downloading, a $40\text{ms}$ wait is invisible.
- **In a competitive fighting game, $40\text{ms}$ is catastrophic lag (2.5 full frames of delay)!**
- By setting `tcpClient.NoDelay = true` in C#, we order Windows to **bypass Nagle's algorithm completely**. The instant a player taps Light Attack, that packet is fired across Wi-Fi in less than $2\text{ms}$!

### 5. The 1-Byte Bitmask Super-Compression Trick
A fighting game controller has 7 action buttons: Jump, Crouch, Light Attack, Heavy Attack, Skill, Ultimate, and Meditate.
- If we sent each button as a separate `bool` (1 byte each) or `string`, an input packet would take 100+ bytes per frame.
- Instead, computer engineers use **Bitmasking**: a single byte has **8 binary bits** (`0` or `1`). We assign each button to a single bit:
  - Bit 0 (`1`): Jump
  - Bit 1 (`2`): Crouch
  - Bit 2 (`4`): Light Attack
  - Bit 3 (`8`): Heavy Attack
  - Bit 4 (`16`): Special Skill
  - Bit 5 (`32`): Ultimate Attack
  - Bit 6 (`64`): Meditate (Ki Charge)
- If a player holds Crouch (`2`) and presses Light Attack (`4`), their input is simply the number `6` ($2 + 4 = 6$ or binary `00000110`)! All 7 buttons fit into **1 single byte**, using virtually zero network bandwidth!

### 6. Thread-Safety: Why Sockets Must Not Call Unity Directly
In C#, network sockets receive messages on a background operating system thread (`ReceiveThread`).
- **Unity Rule:** Unity's core APIs (`GameObject`, `Transform`, `SceneManager`) can **ONLY** be modified on the main Unity thread! If a background network thread touches a Unity GameObject, Unity immediately crashes with an `InvalidOperationException`.
- **The Solution (Main Thread Queue):**
  When a packet arrives in `LanNetworkManager.cs`, the background thread places the packet into a thread-safe `ConcurrentQueue<Action>`. Then, inside Unity's normal `Update()` method on the main thread, we dequeue and execute the actions safely!

> **Godot Comparison:**
> - In **Godot Engine**, high-level networking is handled by `ENetMultiplayerPeer`. You attach a `MultiplayerSpawner` and `MultiplayerSynchronizer` node, and use the `@rpc("any_peer")` decorator above GDScript functions to send data across peers.
> - In Godot, ENet automatically dispatches incoming RPCs during `_process()` on Godot's main thread.
> - In Unity C#, rather than relying on bloated external packages, we built our own native `.NET Sockets` engine (`TcpListener` and `TcpClient`) with a thread-safe `ConcurrentQueue` that achieves identical sub-millisecond dispatch!
"""

with open('Docs/01_UNITY_FOR_BEGINNERS.md', 'a', encoding='utf-8') as f:
    f.write(content)

print("Docs/01_UNITY_FOR_BEGINNERS.md updated.")
