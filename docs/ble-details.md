# BLE Protocol — LEGO Smart Brick

Technical documentation of the Bluetooth Low Energy protocol used by the LEGO® SMART Brick, as reverse-engineered through BLE traffic analysis, firmware disassembly (via [node-smartplay](https://github.com/nathankellenicki/node-smartplay)), and live testing with the companion app.

## Overview

The SMART Brick uses an **EM9305** BLE 5.4 SoC running the **Packetcraft Cordio** stack. When docked on a charging base (or powered via USB), it advertises over BLE using the **WDX (Wireless Data Exchange)** profile — a Cordio-specific GATT-based protocol for device configuration and firmware updates. A separate BLE mode — **PAwR (Periodic Advertising with Responses)** — is used for multi-brick play when undocked, but is outside the scope of this document.

## BLE Advertisement

The brick advertises with the following data:

| Field | Value |
| --- | --- |
| Local Name | `Smart Brick` |
| Service UUID | `0xFEF6` (16-bit, registered to Wicentric / Packetcraft) |
| Flags | General Discoverable, BR/EDR Not Supported |
| Company ID | `0x0397` (LEGO Systems A/S, Bluetooth SIG registered) |
| Manufacturer Data | 6 bytes: `[ButtonState, SystemType, Capabilities, LastNetwork, Status, Option]` |

The `SystemType` byte is `0x60`, which identifies the device as a SMART Brick. The manufacturer data is populated from the Cordio stack and includes button state and device capability flags.

## GATT Services

### Device Information Service (0x180A)

The brick exposes the standard BLE SIG Device Information Service with read-only characteristics:

| Characteristic | UUID | Value (from real brick) |
| --- | --- | --- |
| Device Name | `0x2A00` | `Smart Brick` |
| Manufacturer Name | `0x2A29` | `LEGO` |
| Model Number | `0x2A24` | `Smart Brick` |
| Firmware Revision | `0x2A26` | `2.29.2` (varies by firmware) |
| Software Revision | `0x2A28` | `2.29.2` (same as firmware) |

These are standard GATT reads — plain UTF-8 strings, no null terminator, no padding. The Device Name (`0x2A00`) comes from the Generic Access service (`0x1800`), which is managed automatically by the BLE stack.

### Primary Service — WDX (FEF6)

The main GATT service is advertised with the 16-bit UUID `0xFEF6` but internally uses the 128-bit Cordio WDX base: `005fXXXX-2ff2-4ed5-b045-4c7463617865`.

| Characteristic | UUID | Properties | Purpose |
| --- | --- | --- | --- |
| Device Configuration (DC) | `005f0002-2ff2-…` | Write, Notify | Register GET/SET for all device parameters |
| File Transfer Control (FTC) | `005f0003-2ff2-…` | WriteNoResponse, Notify | OTA update control commands |
| File Transfer Data (FTD) | `005f0004-2ff2-…` | WriteNoResponse, Notify | OTA update bulk data |
| Authentication (AU) | `005f0005-2ff2-…` | Write, Notify | ECDSA P-256 challenge/response |

### Secondary Service — LEGO Custom (3ff2 base)

A second service with a different UUID base is used for bidirectional command exchange:

| Characteristic | UUID | Properties | Purpose |
| --- | --- | --- | --- |
| Bidirectional | `005f000a-3ff2-4ed5-b045-4c7463617865` | WriteNoResponse, Notify | Command/data channel |

The secondary service UUID is `005f0001-3ff2-4ed5-b045-4c7463617865`. This service is discovered by the app after connecting (it is not in the advertisement). Its exact protocol is not yet documented — the [node-smartplay](https://github.com/nathankellenicki/node-smartplay) project subscribes to its notifications during the handshake. And we haven't seen any communication with it during the manual tests.

---

## DC Protocol — Device Configuration

The DC characteristic is the primary communication channel. All reads and writes use a simple PDU format:

```text
[op: 1 byte] [register_id: 1 byte] [value: 0..N bytes]
```

### Opcodes

| Op | Code | Direction | Description |
| --- | --- | --- | --- |
| GET | `0x01` | Client → Brick | Request a register value |
| SET | `0x02` | Client → Brick | Write a register value |
| UPDATE | `0x03` | Brick → Client | Notification response with register value |

The flow is always: client sends GET or SET → brick responds with UPDATE (notification). There are no read operations on the characteristic itself — all data flows through write + notify.

### Unsolicited Notifications

The real LEGO Smart Brick sends **unsolicited UPDATE notifications** — register updates pushed to the client without a preceding GET request. Observed:

| Register | Data (hex) | Trigger | Decoded |
| --- | --- | --- | --- |
| ConnParam (`0x02`) | `00 30 00 00 00 C0 03` | Client subscribes to DC notifications | interval=48 (60ms), latency=0, timeout=192 (1.92s), format=3 |

This is a standard Cordio WDX behaviour: the stack notifies the client of the negotiated connection parameters immediately after the CCCD subscription is established.

---

## DC Register Map

### Standard Cordio WDX Registers (0x01–0x0A)

These registers are part of the Packetcraft Cordio WDX specification:

| Register | ID | Access | Size | Format | Description |
| --- | --- | --- | --- | --- | --- |
| ConnUpdateReq | `0x01` | SET | 7 B | `[interval:2, latency:2, timeout:2, format:1]` | Request connection parameter update |
| ConnParam | `0x02` | GET | 7 B | `[interval:2, latency:2, timeout:2, format:1]` | Current connection parameters (LE uint16 each) |
| DisconnectReq | `0x03` | SET | 0 B | — | Request BLE disconnection |
| ConnSecLevel | `0x04` | GET | 1 B | `uint8` | Current connection security level (0 = none) |
| SecurityReq | `0x05` | SET | 0 B | — | Request security level change |
| ServiceChanged | `0x06` | SET | 0 B | — | Trigger GATT service changed indication |
| DeleteBonds | `0x07` | SET | 0 B | — | Delete all BLE bond information |
| AttMtu | `0x08` | GET | 2 B | `uint16 LE` | Current ATT MTU (default 23) |
| PhyUpdateReq | `0x09` | SET | 3 B | `[txPhy:1, rxPhy:1, options:1]` | Request PHY update |
| Phy | `0x0A` | GET | 3 B | `[txPhy:1, rxPhy:1, options:1]` | Current PHY (1 = 1M, 2 = 2M, 4 = Coded) |

### Standard Cordio WDX Registers (0x20–0x26)

| Register | ID | Access | Size | Format | Description |
| --- | --- | --- | --- | --- | --- |
| BatteryLevel | `0x20` | GET | 1 B | `uint8` (0–100) | Battery percentage |
| ModelNumber | `0x21` | GET | 18 B | UTF-8, zero-padded | Device model string (`"Smart Brick"`) |
| FirmwareRev | `0x22` | GET | variable | UTF-8, null-terminated | Firmware version (e.g. `"0.72.11\0"`) |
| EnterDiagnostics | `0x23` | SET | 0 B | — | Enter diagnostic mode |
| DiagnosticsComplete | `0x24` | GET | 1 B | `uint8` | Diagnostic result |
| DisconnectAndReset | `0x25` | SET | 0 B | — | Disconnect and reboot |
| ClearConfig | `0x26` | SET | 0 B | — | Factory reset configuration |

### LEGO Custom Registers (0x30–0x32)

| Register | ID | Access | Size | Format | Description |
| --- | --- | --- | --- | --- | --- |
| Volume | `0x30` | GET/SET | 1 B | `uint8` (0–15) | Hardware volume level |
| DeviceName | `0x31` | GET | 18 B | UTF-8, zero-padded | BLE device name string |
| MacAddress | `0x32` | GET | 6 B | Big-endian bytes | BLE MAC address |

### LEGO Vendor Registers (0x80+)

These registers are LEGO-specific extensions, identified through firmware disassembly and BLE traffic captures:

| Register | ID | Access | Size | Format | Description |
| --- | --- | --- | --- | --- | --- |
| HubLocalName | `0x80` | GET/SET | ≤18 B | UTF-8, null-terminated | User-configurable device name (max 12 chars + null) |
| UserVolume | `0x81` | GET/SET | 1 B | `uint8` (0–100) | Volume as percentage |
| CurrentWriteOffset | `0x82` | GET | 4 B | `uint32 LE` | Current OTA write offset |
| PrimaryMacAddress | `0x84` | GET | 6 B | Big-endian bytes | Primary BLE MAC address |
| UpgradeState | `0x85` | GET | 1 B | `uint8` | OTA upgrade state (0=Ready, 1=InProgress, 2=LowBattery) |
| SignedCommandNonce | `0x86` | GET | 16 B | Random bytes | Fresh 16-byte nonce for ownership/signing |
| SignedCommand | `0x87` | SET | variable | — | Signed command payload |
| UpdateState | `0x88` | GET | 1 B | `uint8` | Firmware update state |
| PipelineStage | `0x89` | GET | 1 B | `uint8` | OTA pipeline stage |
| UXSignal | `0x90` | SET | 2 B | `[0xEA, 0x00]` | Keepalive / heartbeat (app writes periodically) |
| OwnershipProof | `0x91` | SET | 23 B | `[type:1, nonce:16, mac:6]` | Ownership claim payload |
| ChargingState | `0x93` | GET | 1 B | `uint8` | Charging state (0=not charging) |
| FactoryReset | `0x95` | SET | variable | — | Trigger factory reset (authenticated) |
| TravelMode | `0x96` | GET/SET | 1 B | `uint8` | Travel mode toggle |

---

## Authentication — AU Characteristic

The AU characteristic implements a challenge/response authentication protocol based on **ECDSA P-256** (secp256r1).

### Protocol Flow

```text
Client                              Brick
  |                                   |
  |-- AU_OP_START [level] ----------->|   1. Request auth level
  |                                   |
  |<-- AU_OP_CHALLENGE [nonce:16B] ---|   2. Brick sends random challenge
  |                                   |
  |   (client signs nonce via         |
  |    LEGO cloud backend)            |
  |                                   |
  |-- AU_OP_REPLY [signature:8B] ---->|   3. Client sends signed hash
  |                                   |
  |<-- AU_OP_REPLY [level] -----------|   4. Brick grants or rejects
```

### AU Opcodes

| Op | Code | Direction | Payload |
| --- | --- | --- | --- |
| AU_OP_START | `0x01` | Client → Brick | `[level_lo:1, level_hi:1]` (uint16 LE) |
| AU_OP_CHALLENGE | `0x02` | Brick → Client | `[nonce:16]` (16 random bytes) |
| AU_OP_REPLY | `0x03` | Client → Brick | `[hash:8]` (8-byte truncated signature) |
| AU_OP_REPLY | `0x03` | Brick → Client | `[granted_level:1]` (result) |

### Authentication Levels

| Level | Code | Description |
| --- | --- | --- |
| None | `0x00` | No authentication (default) |
| User | `0x01` | Basic user operations |
| Maintenance | `0x02` | Firmware update / diagnostics |
| Debug | `0x03` | Debug / factory access |

### Error Codes (ATT)

| Error | Code | Description |
| --- | --- | --- |
| Auth Required | `0x80` | Operation requires authentication |
| Invalid Message | `0x81` | Malformed AU message |
| Invalid State | `0x82` | No challenge pending for reply |
| Auth Failed | `0x83` | Signature verification failed |

---

## Ownership Proof — The LEGO Security Model

The ownership proof mechanism is one of the most interesting aspects of the LEGO Smart Brick BLE protocol — and it showcases genuinely thoughtful security engineering from LEGO.

### How It Works

When the LEGO companion app connects to a SMART Brick, it must prove it is an authorized LEGO application — not a third-party tool — before the brick grants full access. The flow is:

1. **App reads `SignedCommandNonce` (0x86)** — the brick generates a fresh 16-byte random nonce.
2. **App sends `OwnershipProof` (0x91)** — a 23-byte payload: `[claim_type:1, nonce:16, mac:6]`.
3. **Brick validates** the proof and either accepts or rejects.

The critical detail: the nonce in step 2 is **not** sent back raw. The official app forwards the nonce to **LEGO's cloud backend** (`p11.bilbo.lego.com`) where it is signed with LEGO's ECDSA P-256 private key. The brick has the corresponding **public key** embedded in firmware and verifies the signature. This means:

- Only LEGO's servers can generate valid ownership proofs.
- The private key never leaves LEGO's infrastructure.
- Each proof is tied to a specific nonce, preventing replay attacks.
- Even if the BLE traffic is fully captured, the proof cannot be forged without LEGO's private key.

### Why This Is Well Designed

LEGO's approach is a textbook implementation of asymmetric cryptography for device binding:

- **Hardware-rooted trust** — The public key is burned into the EM9305 firmware. No software update can change it without LEGO's signing.
- **Nonce freshness** — Each connection gets a new random nonce, preventing replay of captured proofs.
- **Server-side signing** — The private key lives exclusively on LEGO's backend, behind their API authentication. Even a compromised app binary cannot extract it.
- **Graceful degradation** — A brick that cannot reach the backend (e.g. offline play) still functions for basic interactions. Only administrative operations (firmware update, factory reset, telemetry consent) require the full authentication chain.
- **No over-engineering** — The protocol is simple (challenge → sign → verify), uses standard cryptography (ECDSA P-256 / secp256r1), and fits within the constraints of a BLE SoC with limited RAM and compute.

It's a clean, honest security boundary: *you can play with the brick without authentication, but you cannot administer it without proving you are LEGO's software.* For a consumer toy, this is an impressive level of security consciousness.

### Implications for This Project

Our ESP32 implementation can **mimic the protocol** — generate nonces, receive ownership proofs, send UPDATE responses — but it **cannot validate** the cryptographic signature because we don't have LEGO's private key (and rightly so). The app will always reject our brick at the ownership verification stage because:

1. Our brick generates a nonce that the app sends to LEGO's backend for signing.
2. LEGO's backend signs it.
3. The app sends the signed proof to our brick.
4. Our brick would need LEGO's **public key** and the ECDSA verification logic to validate — which we don't implement (and even if we did, the app also validates the *brick's* identity server-side).

This is by design: it means nobody can build a rogue brick that fully impersonates a LEGO Smart Brick. The security model works. Our implementation serves as a protocol study and educational exercise — demonstrating *how* the flow works, even though it will never complete successfully against the real app.

---

## App Handshake Sequence

The LEGO companion app follows a specific connection sequence (observed from BLE traffic captures and confirmed by [node-smartplay](https://github.com/nathankellenicki/node-smartplay)):

```text
1. Connect to "Smart Brick" via FEF6 advertisement
2. Service discovery → find DC, FTC, FTD, AU characteristics
3. Subscribe to DC notifications (CCCD)
4. DC GET ModelNumber     (0x21)  → "Smart Brick"
5. DC GET FirmwareRev     (0x22)  → "0.72.33"
6. DC GET UserVolume      (0x81)  → percentage (0-100)
7. DC GET PrimaryMacAddress (0x84) → 6-byte MAC
8. DC GET HubLocalName    (0x80)  → user-set name
9. DC GET BatteryLevel    (0x20)  → percentage (0-100)
10. DC SET UXSignal       (0x90)  → [0xEA, 0x00] keepalive
11. DC GET AttMtu          (0x08)  → current MTU
12. Begin polling loop (battery, charging state, keepalive)
```

The [node-smartplay](https://github.com/nathankellenicki/node-smartplay) project **skips authentication and ownership entirely** — its source comments state: *"Auth skipped — ECDSA P-256 signing requires LEGO's backend server. Reading register 0x86 (auth nonce) triggers BLE pairing, so we avoid it."*

Basic operations (volume control, renaming, battery reads) work without authentication. This confirms that LEGO's security model is specifically designed to protect administrative functions while leaving everyday play unrestricted.

---

## Persistence

This implementation stores the following values to ESP32 internal flash (`I:\brick\`):

| Setting | File | Format | Loaded at boot |
| --- | --- | --- | --- |
| Hub name | `I:\brick\name.txt` | UTF-8 text | Yes → `WdxRegisters.HubLocalName` |
| Volume | `I:\brick\vol.txt` | Decimal string (0–100) | Yes → `WdxRegisters.VolumePct` + `Volume` |
| Battery level | `I:\brick\bat.txt` | Decimal string (0–100) | Yes → `WdxRegisters.BatteryLevel` |

Battery level is simulated: it starts at 20% on first boot and increases by 1% every 30 seconds, simulating a charge cycle. The level is persisted so it survives reboots.

---

## References

- **Packetcraft Cordio WDX** — The Wireless Data Exchange profile source code (`wdx_defs.h`, `wdxs_dc.c`, `svc_wdxs.c`) from the [Packetcraft SDK](https://github.com/packetcraft-inc/stacks).
- **node-smartplay** — [github.com/nathankellenicki/node-smartplay](https://github.com/nathankellenicki/node-smartplay) — TypeScript library for LEGO Smart Brick interaction. Primary source for register IDs, handshake sequence, and authentication flow.
- **nanoFramework BLE** — [nanoFramework.Device.Bluetooth](https://github.com/nanoframework/nanoFramework.Device.Bluetooth) — The BLE API used by this project, modelled after the Windows.Devices.Bluetooth UWP API.
- **Bluetooth SIG** — LEGO company ID `0x0397` and FEF6 service UUID registration.
