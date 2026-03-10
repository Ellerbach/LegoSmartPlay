# LEGO Smart Brick — Fun reimplementation with .NET nanoFramework

A fun project aiming to recreate the functionality of the LEGO® Smart Play™ smart brick using .NET nanoFramework and off-the-shelf hardware.

> **🚧 This project is in early development and will evolve significantly over time.** APIs, hardware choices, and documentation may change without notice. Contributions and ideas are welcome!

## What is LEGO Smart Play?

The [LEGO® SMART Play™ system](https://www.lego.com/en-us/smart-play/article/innovation) is a screen-free interactive play platform launched in 2026. A **SMART Brick** is a 2×4 LEGO brick powered by two ICs: a **custom LEGO ASIC** (DA000001-01 — coil control, NFC tag reading, wireless charging, sensors, analog synthesizer) and an **EM9305** BLE 5.4 SoC (32-bit ARC CPU running the play engine, BLE stack, and application logic). It uses multiple NFC coils to create a **positioning system** — detecting which SMART Tags, minifigures and bricks are nearby and where they are relative to each other.

Key features of the official system:

- **Tag recognition** — Each brick, minifigure and accessory contains an NFC tag (SMART Tag) that the SMART Brick identifies on contact. The system knows *what* is placed and *where*.
- **Synthetic soundscape** — A 144-opcode software synthesizer generates all audio procedurally from synthesizer instruction sequences (not PCM recordings). A few core sound templates are manipulated in frequency and amplitude to produce a wide range of effects.
- **Motion & orientation** — The SMART Brick reacts to being moved, twisted, swung or thrown.
- **No screens required** — The entire experience is physical: no app, no phone, no tablet.
- **Multi-brick play** — Undocked bricks use BLE 5.4 PAwR (Periodic Advertising with Responses) for device-to-device communication. One brick coordinates, others respond. Tags with multi-brick content types trigger PAwR sessions automatically.
- **Generic play engine** — The firmware is content-agnostic. All specific behaviour lives in ROFS content files (`play.bin`, `audio.bin`, `animation.bin`). Tags are parameterized content selectors — the same script can drive both an X-Wing and an Imperial Turret with different audio/animation banks.
- **25 patents** filed to cover the positioning system, ASIC chip and interaction model.

This project aims to replicate parts of that experience using commodity hardware. As it's virtually impossible to reproduce everything. This new Brick from LEGO® is an amazing piece of hardware with tons of R&D in it.

## Goals

- **Read NFC tags** — Detect and identify LEGO Smart Play bricks (ISO 15693 / NFC-V) using a PN5180 NFC reader, extract the Brick ID, and map it to a known piece or minifigure.
- **Play sounds** — Play a unique sound effect when a tag is recognised, and trigger contextual sounds from colour detection.
- **Read colours** — Integrate a colour sensor to detect brick colours, enabling colour-driven interactions during play.
- **Understand the protocol** — Document the NFC card format and encryption used by LEGO Smart Play.

## How It Works

Place a **SMART Tag** (brick or minifigure) on the NFC reader and the system identifies it and plays a unique sound associated with that character or vehicle. Place a **blue brick** in front of the colour sensor and you hear water filling a tank. Place a **green brick** and you hear tools repairing the element.

This is a simplified demonstration of the kind of interactive play the real SMART Brick provides — combining NFC identification with colour-based contextual feedback to create a small physical-play experience.

### Differences from the real LEGO SMART Brick

The official SMART Brick is far more sophisticated:

- **Synthesized audio** — The real brick uses a 144-opcode software synthesizer that generates all sounds procedurally from instruction sequences baked into the encrypted tag payload. Effects, melodies and ambient sounds are all computed in real-time — no PCM recordings. Our project uses **pre-recorded WAV files** on an SD card for simplicity.
- **Tag-driven effects** — Each SMART Tag's encrypted payload contains synthesizer instructions, animation patterns and play scripts. The tag doesn't just identify *what* it is — it tells the brick *how* to react. We can only read the unencrypted Content ID (Brick ID) and map it to a fixed sound.
- **Multi-colour LED feedback** — The real brick has an RGB LED that provides visual feedback (animations, colour pulses, flashes) coordinated with audio. This is **not yet implemented** in our project.
- **Accelerometer / IMU** — The real brick detects motion, orientation, swings and throws, and reacts accordingly. This is also **not yet implemented**.
- **BLE multi-brick communication** — Multiple real bricks can communicate via BLE 5.4 PAwR for coordinated play. Not part of this project.
- **Positioning system** — The real brick uses multiple NFC coils to detect *where* a tag is placed, not just *what* it is. We use a single reader, so we only detect presence.

### Sample Output

```text
Board: ESP32-C3 Super Mini
PN5180 Product: 4.0
PN5180 Firmware: 4.0
PN5180 EEPROM: 153.0
RF field on: True, Status: 0, External: False
I2C bus speed: 0
TCS3472x chip ID: 0x4D  Gain: 1  Integration time: 0.0024
YX5300 MP3 player initialised on COM2.
Waiting for LEGO Smart Play tags (ISO 15693 / NFC-V)...
Colour sensor thread started.
ISO 15693 card detected:
  UID: A9-08-78-27-01-5C-16-E0
  DSFID: 0x00
  Slot: 9
  Block 0: 00-00-6B-01-0C
  Brick ID: 0x6B (107) => X-Wing wing (vehicle, set 75423)
  Payload size: 107 bytes (blocks 1-27)
  Playing track 107 for Brick ID 0x6B...
Tag removed.
ISO 15693 card detected:
  UID: 4A-E7-0C-22-01-5C-16-E0
  DSFID: 0x00
  Slot: 10
  Block 0: 00-00-3B-01-0C
  Brick ID: 0x3B (59) => TIE Fighter (vehicle, set 75421)
  Payload size: 59 bytes (blocks 1-15)
  Playing track 059 for Brick ID 0x3B...
Tag removed.
  Color => R: 143  G: 111  B: 37  Clear: 255  => Yellow
  Color => R: 255  G: 201  B: 63  Clear: 255  => Yellow
  Color => R: 228  G: 180  B: 58  Clear: 255  => Yellow
```

## Hardware

| Component | Role |
| --- | --- |
| ESP32 (nanoFramework) | Main controller |
| PN5180 | NFC reader (SPI), supports ISO 14443 and ISO 15693 |
| TCS3472x | Colour sensor (I2C), RGBC detection |
| YX5300 / YX5200 | Serial MP3 player (UART), plays from SD card |
| Accelerometer / IMU | Motion, orientation and positioning (TBD) |
| Speaker / DAC | Audio playback (TBD) |
| BLE | Bluetooth Low Energy |

## NFC Card Analysis

The NFC tags inside LEGO Smart Play bricks have been analysed in detail. Key findings:

- **Chip:** Custom LEGO die fabricated by EM Microelectronic (IC ref `0x17` — not an off-the-shelf EM4233 or EM4237), ISO 15693 compliant
- **Memory:** 66 blocks × 4 bytes (264 bytes), blocks 0–63 locked, blocks 64–65 writable
- **Header (block 0):** `00-[Content ID]-01-0C` — the Content ID byte (a.k.a. Brick ID) doubles as the total payload length
- **Payload:** Encrypted by the custom ASIC (DA000001-01), algorithm unknown (baked into ASIC silicon, inaccessible from firmware or JTAG)
- **Cloning test:** Fully cloning a card (UID + memory) onto a blank chip works — the official SMART Brick recognises the clone. Tags with the same Content ID have **byte-for-byte identical EEPROM data** regardless of UID. The SMART Brick does **not** perform `EM_AUTH` challenge-response authentication.
- **Card protection:** The payload is encrypted within the ASIC and factory-locked. The decryption key is embedded in the ASIC's internal silicon logic, not accessible from the EM9305 firmware, BLE interface, or external debug ports.

> **Note on Brick ID:** We currently use byte 2 of block 0 as the Brick ID (called **Content ID** in the [node-smartplay](https://github.com/nathankellenicki/node-smartplay) project). This is a compromise — the same byte also encodes the payload length, so different bricks with identical payload sizes would share the same "ID". A proper identification would require decrypting the payload, which is not currently feasible.

### Potential approaches to decrypt the payload

- **Reverse-engineer the ASIC silicon** — The decryption key and algorithm are inside the custom LEGO ASIC (DA000001-01). Decapping + die-level analysis is the most direct path.
- **Side-channel attack on the ASIC** — Power analysis or electromagnetic probing during tag reads to recover the decryption key.
- **Intercept the ASIC ↔ EM9305 SPI bus** — The ASIC sends decrypted tag content to the EM9305 via memory-mapped registers. Probing this bus during tag reads could reveal plain-text content, bypassing encryption.
- **Compare payloads across identical bricks** — Tags with the same Content ID have byte-for-byte identical EEPROM data (confirmed). Diffing similar Content IDs could reveal structural patterns.
- **Exploit the writable blocks (64–65)** — Investigate whether writing to the unlocked blocks triggers observable behaviour changes.

### Known Brick IDs

| Brick ID | Tag | Source |
| --- | --- | --- |
| `0x3B` (59) | TIE Fighter vehicle | Set 75421 |
| `0x4A` (74) | R2-D2 accessory | Set 75423 |
| `0x69` (105) | X-Wing body | Set 75423 |
| `0x6B` (107) | X-Wing wing | Set 75423 |
| `0x7E` (126) | Lightsaber tile | node-smartplay |
| `0x9D` (157) | Luke Skywalker minifig | Set 75423 |
| `0x9E` (158) | Princess Leia minifig | Set 75423 |
| `0xA9` (169) | Darth Vader minifig | Set 75421 |
| `0xAB` (171) | Emperor Palpatine minifig | node-smartplay |

Full analysis: [docs/lego-smart-brick-nfc.md](docs/lego-smart-brick-nfc.md) — Annotated card dumps: [docs/carte-details.md](docs/carte-details.md)

## Project Structure

```text
LegoSmartBrick/
├── Brick/              .NET nanoFramework firmware (ESP32 + PN5180)
│   └── Program.cs      Card detection, Brick ID lookup, debug output
├── docs/
│   ├── lego-smart-brick-nfc.md   Full NFC card format analysis
│   └── carte-details.md          Annotated raw card dumps (4 cards)
└── LegoSmartBrick.sln  Solution file
```

## Wiring — ESP32-C3 Super Mini + PN5180

Default pin assignments (can be changed in `Program.cs`):

| PN5180 Pin | ESP32-C3 GPIO | Function |
| --- | --- | --- |
| MOSI | GPIO 5 | SPI data out |
| MISO | GPIO 6 | SPI data in |
| SCK | GPIO 4 | SPI clock |
| NSS (CS) | GPIO 10 | SPI chip select |
| RST | GPIO 3 | Hardware reset (toggled at startup) |
| BUSY | GPIO 1 | PN5180 busy signal |

### TCS3472x Colour Sensor (I2C)

| TCS3472x Pin | ESP32-C3 GPIO | Function |
| --- | --- | --- |
| SDA | GPIO 8 | I2C data |
| SCL | GPIO 9 | I2C clock |
| VIN | 3.3 V | Power |
| GND | GND | Ground |

### YX5300 Serial MP3 Player (UART)

| YX5300 Pin | ESP32-C3 GPIO | Function |
| --- | --- | --- |
| TX | GPIO 21 (RX) | Serial data from MP3 module |
| RX | GPIO 20 (TX) | Serial data to MP3 module |
| VCC | 3.3–5 V | Power |
| GND | GND | Ground |

> **Note:** The MP3 module's TX connects to the ESP32 RX pin and vice versa (cross-wired). The module plays MP3/WAV files from a micro SD card.

### SD Card File Layout (YX5300)

The Brick ID byte value is used directly as the track number in folder `01`. Name each MP3 file with the **decimal** value of the Brick ID (3-digit, zero-padded):

| File | Brick ID | Decimal | Tag |
| --- | --- | --- | --- |
| `/01/059.mp3` | `0x3B` | 59 | TIE Fighter |
| `/01/074.mp3` | `0x4A` | 74 | R2-D2 |
| `/01/105.mp3` | `0x69` | 105 | X-Wing body |
| `/01/107.mp3` | `0x6B` | 107 | X-Wing wing |
| `/01/126.mp3` | `0x7E` | 126 | Lightsaber |
| `/01/157.mp3` | `0x9D` | 157 | Luke Skywalker |
| `/01/158.mp3` | `0x9E` | 158 | Princess Leia |
| `/01/169.mp3` | `0xA9` | 169 | Darth Vader |
| `/01/171.mp3` | `0xAB` | 171 | Emperor Palpatine |
| `/01/254.mp3` | — | 254 | Green colour → Tools / repair |
| `/01/255.mp3` | — | 255 | Blue colour → Water filling tank |

To add a sound for a new Brick ID, just place an MP3 file named with its decimal value in folder `01`. No code changes needed. Tracks 254 and 255 are reserved for colour-triggered sound effects.

> The SPI, I2C and UART pins are configured via ESP32 pin muxing (`Configuration.SetPinFunction`). The PN5180 reset pin is toggled manually before driver initialisation. BUSY and NSS are managed by the PN5180 driver.

## Building

1. Install [Visual Studio](https://visualstudio.microsoft.com/) with the [.NET nanoFramework extension](https://marketplace.visualstudio.com/items?itemName=nanoframework.nanoFramework-VS2022-Extension)
2. Open `LegoSmartBrick.sln`
3. Restore NuGet packages
4. Wire the PN5180 to the ESP32-C3 as shown above
5. Deploy to the device

## Acknowledgements

- [Coffee & Fun LLC](https://www.coffeeandfun.com/blog/reverse-engineering-lego-smart-play-nfc-part-1/) — Reverse engineering blog post on LEGO Smart Play NFC tags (set 75423) with Flipper Zero dump files that contributed additional Brick IDs to this project.
- [node-smartplay](https://github.com/nathankellenicki/node-smartplay) by Nathan Kellenicki — Extensive reverse engineering of the LEGO Smart Play system: firmware binary analysis (EM9305 ARC disassembly), hardware architecture (custom ASIC + EM9305 chipset), NFC tag format and security model, BLE protocol (WDX registers, ECDSA P-256 authentication), ROFS content system (play.bin PPL scripts, audio.bin synthesizer instructions, animation.bin LED patterns), PAwR multi-brick communication, and backend API (Bilbo). A major source for understanding the system's internal architecture.
- [nanoFramework](https://github.com/nanoframework) — The .NET nanoFramework runtime and IoT device bindings (including PN5180 / ISO 15693 support).

## Disclaimer

This project is an independent reverse-engineering effort for **educational and interoperability purposes only**. It is not intended to circumvent copy protection, break any terms of service, or infringe on any intellectual property rights.

No proprietary LEGO® software, firmware, or copyrighted assets are included in this repository. All analysis is based on publicly observable NFC communications and publicly available information.

## Trademarks

**LEGO®** is a trademark of the LEGO Group of companies. This project is **not** affiliated with, endorsed by, or sponsored by the LEGO Group. All other trademarks are the property of their respective owners.

## License

This project is licensed under the [MIT License](LICENSE).
