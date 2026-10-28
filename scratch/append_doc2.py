content = """

---

## 49. LAN Multiplayer Architecture: Host-Authoritative Simulation, Decoupled Input Bridge & Zero-Allocation Packet Framing

### Architectural Overview
Multiplayer fighting games require ultra-low latency ($< 5\\text{ms}$) and absolute state consistency to prevent hit detection discrepancies. Mystic Combat implements a **Host-Authoritative State Mirroring Architecture** using native C# `.NET Sockets` (`TcpListener` on Host, `TcpClient` on Client) with `NoDelay = true` for zero-buffer transmission.

```mermaid
flowchart TD
    subgraph HostPC ["Host Machine (PC 1 - Master Authority)"]
        H_Input["P1: PlayerInputHandler (Local Keyboard)"]
        H_NetInput["P2: NetworkFighterInput (Buffered Stream)"]
        H_Sim["Simulation Engine (Physics, Hitboxes, Health, MatchManager)"]
        H_Sync["NetworkMatchSynchronizer (Host Broadcast 60Hz)"]
        H_NetMgr["LanNetworkManager (TcpListener Port 7777)"]

        H_Input --> H_Sim
        H_NetInput --> H_Sim
        H_Sim --> H_Sync
        H_Sync --> H_NetMgr
    end

    subgraph ClientPC ["Client Machine (PC 2 - Input Streamer & Screen Mirror)"]
        C_Input["P2: Local Keyboard Input Collector (WASD / Arrows)"]
        C_Sync["NetworkMatchSynchronizer (Client Mirror)"]
        C_NetMgr["LanNetworkManager (TcpClient)"]
        C_Mirror["Kinematic Fighter Presentation (Sprites, State, HUD)"]

        C_Input --> C_NetMgr
        C_NetMgr --> C_Sync
        C_Sync --> C_Mirror
    end

    H_NetMgr -- "MatchSnapshotPacket (38 Bytes @ 60Hz)" --> C_NetMgr
    C_NetMgr -- "FighterInputPacket (9 Bytes @ 60Hz)" --> H_NetMgr
```

### Key Architectural Pillars

#### 1. Decoupled Input Architecture (`IFighterInput` Bridge)
Because `FighterController.cs` depends solely on the `IFighterInput` interface rather than reading hardware directly, the host machine swaps out the local input provider for `NetworkFighterInput`:
```csharp
public interface IFighterInput
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool CrouchHeld { get; }
    bool LightAttackPressed { get; }
    bool HeavyAttackPressed { get; }
    bool SkillPressed { get; }
    bool UltimatePressed { get; }
    bool MeditateHeld { get; }
}
```
When `LanNetworkManager.OnRemoteInputReceived` fires on the Host, `NetworkFighterInput.FeedInput()` buffers the remote commands. On the next frame, `FighterController` executes the moves identically to a local player with 0 code branch penalties.

#### 2. Compact Binary Packet Protocol
To prevent garbage collection allocation and ensure minimal MTU utilization, all network traffic is framed into discrete binary packets with a 3-byte header:
$$\\text{Packet Format} = [\\text{Type}: 1\\text{ byte}] [\\text{Payload Length}: 2\\text{ bytes}] [\\text{Payload}: N\\text{ bytes}]$$

| Packet Type | Opcode | Size | Payload Description | Frequency |
| :--- | :---: | :---: | :--- | :--- |
| `FighterInput` | `0x04` | $9\\text{ B}$ | `Vector2 MoveInput` ($8\\text{B}$) + $1\\text{B}$ button bitmask (Jump, Crouch, Light, Heavy, Skill, Ult, Meditate). | $60\\text{Hz}$ (Client $\\rightarrow$ Host) |
| `MatchSnapshot` | `0x05` | $38\\text{ B}$ | Round ($1\\text{B}$), Timer ($4\\text{B}$), MatchState ($1\\text{B}$), Wins ($2\\text{B}$), P1 State ($15\\text{B}$), P2 State ($15\\text{B}$). | $60\\text{Hz}$ (Host $\\rightarrow$ Client) |
| `CharSelectSync`| `0x02` | $\\sim 12\\text{ B}$| PlayerIndex ($1\\text{B}$), CharacterId string, LockedIn bool. | On Selection Change |
| `MatchStart` | `0x03` | $\\sim 16\\text{ B}$| P1 & P2 finalized CharacterId strings. | On Match Launch |

#### 3. Kinematic Client Mirroring (Zero Physics Drift)
In fighting games, dual physics simulations will inevitably drift due to sub-pixel floating-point rounding. On the Client machine, `NetworkMatchSynchronizer` automatically sets both fighters' `Rigidbody2D` components to `isKinematic = true` and deactivates local `HitboxController` scripts:
- **Authority:** All hit registration, knockback, damage scaling, and Red Zone exhaustion occur exclusively on the Host.
- **Client Presentation:** The Client receives authoritative positions $(X, Y)$, linear velocities $(V_x, V_y)$, health, Ki, facing flips, and active `FighterState` values, ensuring flawless visual mirroring with zero desyncs.

#### 4. Thread-Safe Unity Dispatch (`ConcurrentQueue<Action>`)
Network streaming threads cannot call Unity API methods directly without triggering `InvalidOperationException`. `LanNetworkManager` utilizes a thread-safe `ConcurrentQueue<Action>`:
1. Incoming socket thread decodes binary packets and enqueues lambda dispatchers.
2. In `LanNetworkManager.Update()`, the main thread drains the queue and fires C# events safely within Unity's execution cycle.
"""

with open('Docs/02_ARCHITECTURE.md', 'a', encoding='utf-8') as f:
    f.write(content)

print("Docs/02_ARCHITECTURE.md updated.")
