content = """

---

## Question 50: Why did you choose a Host-Authoritative architecture over Peer-to-Peer Lockstep for LAN play, and how did your decoupled `IFighterInput` interface make networking effortless?

### Short Answer (30-second elevator pitch):
*"In competitive fighting games, Peer-to-Peer lockstep simulations frequently suffer from physics desyncs due to sub-pixel floating-point rounding differences across machines. Because Mystic Combat runs over local LAN with sub-3ms latency, we chose a Host-Authoritative State Mirroring architecture. The Host machine is the sole authority for physics, hitboxes, and timer, while the Client streams button inputs and mirrors the display. Crucially, because our fighters rely on the `IFighterInput` interface rather than reading the keyboard directly, attaching network multiplayer required zero modifications to combat code: the Host simply attaches a `NetworkFighterInput` component to Fighter 2!"*

### Detailed Technical Explanation:
1. **The Trap of Peer-to-Peer Lockstep in Unity 2D Physics:**
   - In 2D fighting games, physics collisions (`Box2D`) involve micro-calculations for knockback impulses and friction deceleration.
   - On separate CPU architectures or frame rates, floating-point rounding errors accumulate over a Best-of-3 round match. In P2P lockstep, this leads to fatal desyncs where a hit connects on Screen A but whiffs on Screen B.
   - Under **Host-Authoritative Mirroring**, the Host executes the ground truth. The Client receives 60Hz snapshots containing coordinates, velocities, health, and state, guaranteeing that both players experience identical gameplay without desynchronization.
2. **Architectural Decoupling via `IFighterInput`:**
   - In standard rookie Unity code, scripts call `Input.GetKey()` directly inside `FighterController.Update()`.
   - In Mystic Combat, `FighterController` delegates all input queries to `IFighterInput`:
     - Local Match: `PlayerInputHandler` (Hardware polling)
     - Single Player: `CpuFighterBrain` (Hierarchical FSM)
     - LAN Match: `NetworkFighterInput` (Decoded packet stream)
   - This adheres directly to the **Open/Closed Principle (SOLID)**: our combat controller was completely closed to modification yet open to network extension.
3. **Kinematic Client Optimization:**
   - To prevent the Client's local 2D physics engine from fighting the Host's position updates, `NetworkMatchSynchronizer` automatically switches both Client fighters to `RigidbodyType2D.Kinematic` and disables local trigger colliders, reducing Client CPU overhead and eliminating jitter.

---

## Question 51: How does your custom binary framing protocol prevent packet tearing, what is Nagle's algorithm (`NoDelay`), and how do you achieve 60Hz multiplayer synchronization with less than 3 KB/s bandwidth?

### Short Answer (30-second elevator pitch):
*"Standard TCP sockets can concatenate or fragment packets, causing packet tearing if not framed properly. We implemented a 3-byte binary header `[1B Type][2B Length][Payload]` using `BinaryWriter` so the receiver always reconstructs complete discrete messages. By setting `tcpClient.NoDelay = true`, we disable Nagle's buffering algorithm to eliminate 40ms transmission delays. Furthermore, by bitmasking 7 fighter actions into a single byte, our input packets are only 9 bytes and match snapshots are 38 bytes, requiring only 2.4 KB/s total bandwidth at 60Hz!"*

### Detailed Technical Explanation:
1. **Packet Framing & Preventing Stream Fragmentation:**
   - TCP is a streaming protocol, not a message protocol. If the Host sends three consecutive packets quickly, TCP may merge them into a single blob or split one packet across two socket reads.
   - To guarantee complete packet extraction, `LanNetworkManager.cs` prepends every payload with a 3-byte header:
     $$\\text{Header} = \\underbrace{\\text{Opcode}}_{1\\text{ byte}} + \\underbrace{\\text{Payload Length}}_{2\\text{ bytes (short)}}$$
   - The background reader thread reads exactly 1 byte for type, 2 bytes for length, and then calls `reader.ReadBytes(length)` to extract the exact unfragmented payload.
2. **The Critical Role of `NoDelay = true` (Disabling Nagle's Algorithm):**
   - The TCP protocol defaults to Nagle's algorithm, which delays small packets for up to $40\\text{ms}$ in hopes of coalescing them with future traffic.
   - In a 60 FPS fighting game, $40\\text{ms}$ represents 2.4 frames of artificial input lag.
   - By setting `activeClient.NoDelay = true`, packets are dispatched onto the network wire immediately upon flushing, achieving sub-2ms round-trip times on local Wi-Fi.
3. **Data Compression Mathematics:**
   - Client Input Packet: `Vector2 MoveInput` ($2 \\times 4\\text{B} = 8\\text{B}$) + 1 Button Bitmask ($1\\text{B}$) = **$9\\text{ Bytes}$**.
     $$\\text{Client Upload Bandwidth} = 9\\text{ B} \\times 60\\text{ packets/s} = 540\\text{ Bytes/s} \\approx 0.54\\text{ KB/s}$$
   - Host Snapshot Packet: Round ($1\\text{B}$) + Timer ($4\\text{B}$) + MatchState ($1\\text{B}$) + Wins ($2\\text{B}$) + 2 Fighters ($2 \\times 15\\text{B} = 30\\text{B}$) = **$38\\text{ Bytes}$**.
     $$\\text{Host Download Bandwidth} = 38\\text{ B} \\times 60\\text{ packets/s} = 2,280\\text{ Bytes/s} \\approx 2.28\\text{ KB/s}$$
   - Total match bandwidth is under **$3\\text{ KB/s}$**, ensuring zero packet congestion even on crowded university or conference Wi-Fi networks!
"""

with open('Docs/04_VIVA_AND_INTERVIEW_PREP.md', 'a', encoding='utf-8') as f:
    f.write(content)

print("Docs/04_VIVA_AND_INTERVIEW_PREP.md updated.")
