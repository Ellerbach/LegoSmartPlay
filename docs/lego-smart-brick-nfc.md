# LEGO Smart Brick NFC Card Format

LEGO Smart Play smart bricks contain embedded **ISO 15693** (NFC-V / Vicinity) tags. These tags operate at 13.56 MHz and are designed for short-range identification. Each brick carries a unique identifier and a variable-length cryptographic payload that the LEGO play system uses to recognise and authenticate which brick is being placed on the reader.

## Card Hardware

| Property | Value |
| --- | --- |
| **Standard** | ISO/IEC 15693 |
| **Manufacturer** | EM Microelectronic-Marin SA (code `0x16`) |
| **IC Reference** | `0x17` |
| **IC Chip** | **Custom LEGO die** fabricated by EM Microelectronic — IC ref `0x17` does not match any off-the-shelf product (EM4233SLIC = `0x02`, EM4233 = `0x09`). Extended memory (66 blocks vs 32 for EM4233SLIC). See [node-smartplay findings](#node-smartplay-reverse-engineering-findings). |
| **Memory** | 66 blocks × 4 bytes = 264 bytes (2112 bits) |
| **Block size** | 4 bytes (indicated by `0x03` in system info, meaning size = 3 + 1) |
| **Block security** | Blocks 0–63 locked (factory), blocks 64–65 unlocked |

---

## UID (Unique Identifier) — 8 bytes

The UID is a factory-programmed, globally unique 64-bit identifier. It is read LSB-first over the air, but is typically displayed MSB-first.

### Structure (MSB → LSB as displayed)

| Byte(s) | Meaning |
| --- | --- |
| Byte 8 | Always `0xE0` — ISO 15693 tag family identifier |
| Byte 7 | `0x16` — IC manufacturer code (EM Microelectronic) |
| Byte 6 | `0x5C` — Constant across all LEGO smart bricks (product/model code) |
| Byte 5 | `0x01` — Constant across all LEGO smart bricks (product/model code) |
| Bytes 1–4 | Unique serial number (differs per brick) |

### The 4 examples

| # | UID | Serial (unique) | Fixed suffix |
| --- | --- | --- | --- |
| 1 | **`A9-08-78-27`**`-01-5C-16-E0` | `A9-08-78-27` | `01-5C-16-E0` |
| 2 | **`4A-E7-0C-22`**`-01-5C-16-E0` | `4A-E7-0C-22` | `01-5C-16-E0` |
| 3 | **`6D-EA-D4-26`**`-01-5C-16-E0` | `6D-EA-D4-26` | `01-5C-16-E0` |
| 4 | **`FC-F1-6F-2A`**`-01-5C-16-E0` | `FC-F1-6F-2A` | `01-5C-16-E0` |

> **Reading tip:** The last 4 bytes `01-5C-16-E0` are always the same — they identify the chip family and manufacturer. Only the **first 4 bytes** (bold) change between bricks.

---

## System Information Response — 15 bytes

The system information is returned by the **Get System Information** command (ISO 15693 command code `0x2B`).

### Byte layout

```text
 00  0F  A9  08  78  27  01  5C  16  E0  00  00  41  03  17
 ──  ──  ──────────────────────────────  ──  ──  ──  ──  ──
 │   │   │                               │   │   │   │   └─ IC reference (0x17)
 │   │   │                               │   │   │   └─ Block size (0x03 → 4 bytes)
 │   │   │                               │   │   └─ Number of blocks (0x41 → 66 blocks, 0-indexed = 65+1)
 │   │   │                               │   └─ AFI (Application Family Identifier: 0x00)
 │   │   │                               └─ DSFID (Data Storage Format ID: 0x00)
 │   │   └─ UID (8 bytes, same as detected UID)
 │   └─ Info flags (0x0F = DSFID, AFI, memory size, IC ref all present)
 └─ Response flags (0x00 = no error)
```

### The 4 examples

| # | Resp flags | Info flags | UID | DSFID | AFI | Blocks | Block size | IC ref |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `00` | `0F` | `A9-08-78-27-01-5C-16-E0` | `00` | `00` | `41` (66) | `03` (4B) | `17` |
| 2 | `00` | `0F` | `4A-E7-0C-22-01-5C-16-E0` | `00` | `00` | `41` (66) | `03` (4B) | `17` |
| 3 | `00` | `0F` | `6D-EA-D4-26-01-5C-16-E0` | `00` | `00` | `41` (66) | `03` (4B) | `17` |
| 4 | `00` | `0F` | `FC-F1-6F-2A-01-5C-16-E0` | `00` | `00` | `41` (66) | `03` (4B) | `17` |

> **Reading tip:** Everything is identical across all bricks except the embedded UID. The info flags `0x0F` means all four optional fields (DSFID, AFI, memory size, IC reference) are included in the response.

---

## Memory Layout — Full 66-Block Analysis

The full memory dump reveals a clear structure with three regions: a **4-byte header**, a **variable-length payload**, and **zero-padded empty space**.

### Overview

```text
 Block 0         Block 1         Block 2  ...  Block N      Block N+1 ... Block 65
┌────────────┐ ┌──────────────┐ ┌─────────────────────────┐ ┌──────────────────────┐
│  Header    │ │  Payload     │ │  Payload (continued)    │ │  00-00-00-00 (empty) │
│ 00-LL-01-0C│ │ 01-XX-XX-XX  │ │  XX-XX-XX-XX ...        │ │  00-00-00-00 ...     │
└────────────┘ └──────────────┘ └─────────────────────────┘ └──────────────────────┘
                              ◄── LL bytes total ──►
                              (from byte 0 of block 0)
```

### Memory regions per card

| # | Brick ID | Data blocks | Data bytes | Empty blocks |
| --- | --- | --- | --- | --- |
| 1 | `0x6B` (107) | Blocks 0–26 | **107 bytes** | Blocks 27–65 (all zeros) |
| 2 | `0x3B` (59) | Blocks 0–14 | **59 bytes** | Blocks 15–65 (all zeros) |
| 3 | `0x9E` (158) | Blocks 0–39 | **158 bytes** | Blocks 40–65 (all zeros) |
| 4 | `0xA9` (169) | Blocks 0–42 | **169 bytes** | Blocks 43–65 (all zeros) |

> **Key discovery:** The total number of meaningful data bytes on each card **exactly equals** the value of byte 2 in block 0 (the "Brick ID" byte). This holds for all 4 cards — see [Block 0 analysis](#block-0--header-4-bytes) below.

---

## Block 0 — Header (4 bytes)

Block 0 contains a fixed-format header. The actual data is 4 bytes (the dump format does not include a security status byte).

```text
 00  LL  01  0C
 ──  ──  ──  ──
 │   │   │   └─ 0x0C (constant) — card type or format marker
 │   │   └─ 0x01 (constant) — version or sub-type
 │   └─ ** Brick ID / Data Length ** — varies per brick
 └─ 0x00 (constant) — flags or padding
```

### The 4 examples — Block 0

| # | Block 0 (hex) | Byte 2 value | Decimal | Total data bytes on card |
| --- | --- | --- | --- | --- |
| 1 | `00-`**`6B`**`-01-0C` | `0x6B` | **107** | 107 ✓ |
| 2 | `00-`**`3B`**`-01-0C` | `0x3B` | **59** | 59 ✓ |
| 3 | `00-`**`9E`**`-01-0C` | `0x9E` | **158** | 158 ✓ |
| 4 | `00-`**`A9`**`-01-0C` | `0xA9` | **169** | 169 ✓ |

> **Reading tip:** **Byte 2** (the second byte) is the only byte that changes in block 0. It serves as the **Brick ID** — identifying which LEGO character or object this brick represents. It also precisely equals the total number of data bytes stored on the card (from byte 0 of block 0 through the last non-zero byte). Bytes 1, 3, and 4 are always `00-xx-01-0C`.

### Constant bytes

| Byte | Value | Possible meaning |
| --- | --- | --- |
| Byte 1 | `0x00` | Flags or reserved |
| Byte 3 | `0x01` | Format version |
| Byte 4 | `0x0C` | Card type identifier (12 decimal) |

---

## Blocks 1–N — Cryptographic Payload (variable length)

Starting at block 1, the card contains a **variable-length encrypted payload**. This data appears pseudo-random and differs between bricks with different content IDs — consistent with block cipher or stream cipher encryption. However, tags with the **same content ID** contain **byte-for-byte identical** encrypted payloads regardless of UID (confirmed by node-smartplay's analysis of two Lightsaber tiles).

### Payload structure

```text
 Block 1 byte 0:  Always 0x01 (signature version or algorithm ID)
 Block 1 byte 1 → last data byte:  Cryptographic data (unique per brick)
```

### Payload sizes

| # | Brick ID | Payload (blocks 1–N) | Payload bytes |
| --- | --- | --- | --- |
| 1 | `0x6B` (107) | Blocks 1–26 | 103 bytes |
| 2 | `0x3B` (59) | Blocks 1–14 | 55 bytes |
| 3 | `0x9E` (158) | Blocks 1–39 | 154 bytes |
| 4 | `0xA9` (169) | Blocks 1–42 | 165 bytes |

> **Reading tip:** The **first byte of block 1 is always `0x01`** across all bricks. This likely indicates a payload version or signature algorithm. Everything after it — up to the data length boundary — is the cryptographic body, unique per brick.

### The 4 examples — First and last data blocks

| # | Block 1 | Block 2 | Block 3 | ... | Last full block | Last partial block |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `01-99-F4-93` | `76-76-DC-5C` | `CC-C0-2D-6B` | | Blk 25: `2A-47-C6-1D` | Blk 26: `3A-3E-B8`-~~`00`~~ |
| 2 | `01-9C-D5-2D` | `72-F5-50-61` | `AB-8F-73-D0` | | Blk 13: `80-6E-CF-4E` | Blk 14: `90-68-E7`-~~`00`~~ |
| 3 | `01-0B-61-49` | `1A-35-D3-D0` | `D0-16-B3-B4` | | Blk 38: `17-74-EE-93` | Blk 39: `FD-C4`-~~`00-00`~~ |
| 4 | `01-2A-72-06` | `94-F4-E5-26` | `64-D6-CA-C9` | | Blk 41: `9A-65-84-C3` | Blk 42: `3F`-~~`00-00-00`~~ |

*(Strikethrough bytes are zero-padding, not payload data.)*

---

## Full Memory Dumps

### Card 1 — Brick ID `0x6B` (107)

UID: `A9-08-78-27-01-5C-16-E0`

| Block | Hex data | ASCII | Region |
| --- | --- | --- | --- |
| 000 | `00-6B-01-0C` | `.k..` | **Header** |
| 001 | `01-99-F4-93` | `....` | Payload |
| 002 | `76-76-DC-5C` | `vv.\` | Payload |
| 003 | `CC-C0-2D-6B` | `..-k` | Payload |
| 004 | `FA-BA-0A-F0` | `....` | Payload |
| 005 | `36-19-74-FD` | `6.t.` | Payload |
| 006 | `2C-AD-33-A8` | `,.3.` | Payload |
| 007 | `40-2F-19-04` | `@/..` | Payload |
| 008 | `E4-F4-75-56` | `..uV` | Payload |
| 009 | `56-AA-E2-FF` | `V...` | Payload |
| 010 | `A6-19-B6-4E` | `...N` | Payload |
| 011 | `28-07-A1-D2` | `(...` | Payload |
| 012 | `AC-8A-43-86` | `..C.` | Payload |
| 013 | `50-55-E5-8C` | `PU..` | Payload |
| 014 | `C5-53-48-C6` | `.SH.` | Payload |
| 015 | `F4-8C-D7-73` | `...s` | Payload |
| 016 | `84-2C-BF-3C` | `.,.<` | Payload |
| 017 | `93-5C-DE-60` | `.\.`` | Payload |
| 018 | `9B-3D-A1-DB` | `.=..` | Payload |
| 019 | `68-10-23-6D` | `h.#m` | Payload |
| 020 | `CC-F0-C4-35` | `...5` | Payload |
| 021 | `1B-B0-BC-6F` | `...o` | Payload |
| 022 | `0D-4B-B4-E3` | `.K..` | Payload |
| 023 | `EA-81-84-20` | `...` | Payload |
| 024 | `B1-ED-C6-C7` | `....` | Payload |
| 025 | `2A-47-C6-1D` | `*G..` | Payload |
| 026 | `3A-3E-B8-00` | `:>..` | Payload (last 3 bytes) |
| 027–065 | `00-00-00-00` | `....` | Empty |

**Data bytes: 107** (matches Brick ID `0x6B`)

### Card 2 — TIE Fighter (set 75421) — Brick ID `0x3B` (59)

UID: `4A-E7-0C-22-01-5C-16-E0`

| Block | Hex data | ASCII | Region |
| --- | --- | --- | --- |
| 000 | `00-3B-01-0C` | `.;..` | **Header** |
| 001 | `01-9C-D5-2D` | `...-` | Payload |
| 002 | `72-F5-50-61` | `r.Pa` | Payload |
| 003 | `AB-8F-73-D0` | `..s.` | Payload |
| 004 | `D3-57-FA-07` | `.W..` | Payload |
| 005 | `13-9C-3F-44` | `..?D` | Payload |
| 006 | `0F-55-ED-88` | `.U..` | Payload |
| 007 | `D9-D4-8F-8B` | `....` | Payload |
| 008 | `63-5D-CE-9E` | `c]..` | Payload |
| 009 | `A9-35-21-B6` | `.5!.` | Payload |
| 010 | `49-10-DA-E1` | `I...` | Payload |
| 011 | `5A-17-F0-D5` | `Z...` | Payload |
| 012 | `E6-9E-A1-4C` | `...L` | Payload |
| 013 | `80-6E-CF-4E` | `.n.N` | Payload |
| 014 | `90-68-E7-00` | `.h..` | Payload (last 3 bytes) |
| 015–065 | `00-00-00-00` | `....` | Empty |

**Data bytes: 59** (matches Brick ID `0x3B`)

### Card 3 — Brick ID `0x9E` (158)

UID: `6D-EA-D4-26-01-5C-16-E0`

| Block | Hex data | ASCII | Region |
| --- | --- | --- | --- |
| 000 | `00-9E-01-0C` | `....` | **Header** |
| 001 | `01-0B-61-49` | `..aI` | Payload |
| 002 | `1A-35-D3-D0` | `.5..` | Payload |
| 003 | `D0-16-B3-B4` | `....` | Payload |
| 004 | `54-2A-5E-43` | `T*^C` | Payload |
| 005 | `88-D4-FE-B3` | `....` | Payload |
| 006 | `AA-22-BE-60` | `.".`` | Payload |
| 007 | `1A-1E-1F-98` | `....` | Payload |
| 008 | `34-D4-3F-FA` | `4.?.` | Payload |
| 009 | `6F-BA-65-52` | `o.eR` | Payload |
| 010 | `FF-1F-5B-FE` | `..[.` | Payload |
| 011 | `5B-2D-2D-8A` | `[--.` | Payload |
| 012 | `F9-B8-43-0E` | `..C.` | Payload |
| 013 | `05-32-1F-7F` | `.2..` | Payload |
| 014 | `7C-27-0C-5F` | `\|'._` | Payload |
| 015 | `5E-7F-83-9C` | `^...` | Payload |
| 016 | `FB-D5-81-C2` | `....` | Payload |
| 017 | `43-0D-16-F9` | `C...` | Payload |
| 018 | `E5-6B-58-21` | `.kX!` | Payload |
| 019 | `0B-D3-74-81` | `..t.` | Payload |
| 020 | `A2-5B-77-92` | `.[w.` | Payload |
| 021 | `DD-63-4D-AB` | `.cM.` | Payload |
| 022 | `38-EC-92-9C` | `8...` | Payload |
| 023 | `77-13-43-D6` | `w.C.` | Payload |
| 024 | `B6-87-D4-BE` | `....` | Payload |
| 025 | `DA-5C-79-67` | `.\yg` | Payload |
| 026 | `B0-6E-0A-0D` | `.n..` | Payload |
| 027 | `FE-7D-59-FB` | `.}Y.` | Payload |
| 028 | `69-9A-43-63` | `i.Cc` | Payload |
| 029 | `2C-05-0C-27` | `,..'` | Payload |
| 030 | `9C-E4-BD-5D` | `...]` | Payload |
| 031 | `A2-DE-9C-A2` | `....` | Payload |
| 032 | `90-D4-5A-D3` | `..Z.` | Payload |
| 033 | `28-74-4B-D6` | `(tK.` | Payload |
| 034 | `06-C7-09-94` | `....` | Payload |
| 035 | `12-D5-7E-99` | `..~.` | Payload |
| 036 | `94-C8-8C-85` | `....` | Payload |
| 037 | `1F-5B-E3-EA` | `.[..` | Payload |
| 038 | `17-74-EE-93` | `.t..` | Payload |
| 039 | `FD-C4-00-00` | `....` | Payload (last 2 bytes) |
| 040–065 | `00-00-00-00` | `....` | Empty |

**Data bytes: 158** (matches Brick ID `0x9E`)

### Card 4 — Darth Vader — Brick ID `0xA9` (169)

UID: `FC-F1-6F-2A-01-5C-16-E0`

| Block | Hex data | ASCII | Region |
| --- | --- | --- | --- |
| 000 | `00-A9-01-0C` | `....` | **Header** |
| 001 | `01-2A-72-06` | `.*r.` | Payload |
| 002 | `94-F4-E5-26` | `...&` | Payload |
| 003 | `64-D6-CA-C9` | `d...` | Payload |
| 004 | `21-D9-96-98` | `!...` | Payload |
| 005 | `19-C2-F2-53` | `...S` | Payload |
| 006 | `7B-9A-87-CB` | `{...` | Payload |
| 007 | `D0-48-9F-30` | `.H.0` | Payload |
| 008 | `60-2F-81-8A` | ``/..` | Payload |
| 009 | `63-DA-E3-9F` | `c...` | Payload |
| 010 | `71-4F-6A-7E` | `qOj~` | Payload |
| 011 | `77-E4-33-2E` | `w.3.` | Payload |
| 012 | `62-09-F8-DE` | `b...` | Payload |
| 013 | `49-89-1D-D7` | `I...` | Payload |
| 014 | `2C-57-29-4D` | `,W)M` | Payload |
| 015 | `BA-E3-E9-A0` | `....` | Payload |
| 016 | `88-5F-47-57` | `._GW` | Payload |
| 017 | `30-80-09-64` | `0..d` | Payload |
| 018 | `0D-C5-9A-51` | `...Q` | Payload |
| 019 | `88-F9-DD-30` | `...0` | Payload |
| 020 | `88-A9-E6-03` | `....` | Payload |
| 021 | `99-A3-EC-E7` | `....` | Payload |
| 022 | `46-87-3A-B6` | `F.:.` | Payload |
| 023 | `EE-87-53-73` | `..Ss` | Payload |
| 024 | `FE-22-30-F7` | `."0.` | Payload |
| 025 | `6C-DA-7A-90` | `l.z.` | Payload |
| 026 | `54-AE-2A-2F` | `T.*/` | Payload |
| 027 | `11-55-59-15` | `.UY.` | Payload |
| 028 | `2E-4B-A3-69` | `.K.i` | Payload |
| 029 | `E3-01-25-55` | `..%U` | Payload |
| 030 | `21-4F-DB-4F` | `!O.O` | Payload |
| 031 | `A5-F0-B7-95` | `....` | Payload |
| 032 | `90-B8-88-B6` | `....` | Payload |
| 033 | `08-57-3C-D2` | `.W<.` | Payload |
| 034 | `6E-29-A8-1A` | `n)..` | Payload |
| 035 | `E2-E3-5E-A0` | `..^.` | Payload |
| 036 | `30-11-7D-F4` | `0.}.` | Payload |
| 037 | `48-80-E8-03` | `H...` | Payload |
| 038 | `53-13-DF-2D` | `S..-` | Payload |
| 039 | `46-79-2F-C3` | `Fy/.` | Payload |
| 040 | `76-24-8C-DE` | `v$..` | Payload |
| 041 | `9A-65-84-C3` | `.e..` | Payload |
| 042 | `3F-00-00-00` | `?...` | Payload (last 1 byte) |
| 043–065 | `00-00-00-00` | `....` | Empty |

**Data bytes: 169** (matches Brick ID `0xA9`)

---

## Summary — What to Read

To identify a LEGO Smart Brick, you only need to:

1. **Detect** an ISO 15693 card (inventory command)
2. **Read block 0** — extract **byte 2** (the second byte) to get the **Brick ID**
3. *(Optional)* Read the UID for per-chip uniqueness
4. *(Optional)* Read blocks 1–N (where N is derived from the Brick ID) for the full cryptographic payload

The Brick ID (a single byte, 0–255) maps to a specific LEGO character or object in the Smart Play system. Its value also tells you exactly how many bytes of data are stored on the card.

### Quick Reference

| What you need | Where to find it | Size |
| --- | --- | --- |
| Brick identity (which character) | Block 0, **byte 2** | 1 byte |
| Total data length | Block 0, byte 2 (same value) | = Brick ID value in bytes |
| Unique chip serial | UID bytes 1–4 | 4 bytes |
| Cryptographic payload | Blocks 1 through N (first byte always `0x01`) | Variable (55–165 bytes observed) |
| Card type marker | Block 0, byte 4 (`0x0C`) | 1 byte |
| Memory capacity | System info response | 66 blocks × 4 bytes |

### Key findings from the full dump

1. **Brick ID = data length.** Byte 2 of block 0 precisely equals the total number of meaningful data bytes on the card (verified across all 4 cards).
2. **Variable-length payload.** The cryptographic data extends well beyond block 3, ranging from 14 to 42 blocks depending on the brick.
3. **All payloads start with `0x01`.** Block 1 byte 0 is `0x01` on every card — likely a payload format version.
4. **Zero-padded.** All unused blocks (after the data region) are filled with `00-00-00-00`.
5. **Constant header.** Block 0 is always `00-[BrickID]-01-0C` — only the Brick ID byte varies.
6. **Cloning works.** Fully cloning a card (UID + all memory) onto a blank EM4237 chip produces a working clone — the official SMART Brick recognises it. This proves the SMART Brick does **not** perform `EM_AUTH` challenge-response authentication during play; it only reads and verifies the memory content. The payload is not bound to the chip's UID.
7. **Brick ID as identifier is a compromise.** We currently use byte 2 of block 0 as the "Brick ID" (called **Content ID** in node-smartplay). Since this byte also represents the payload length, different bricks with identical payload sizes would share the same value. A proper tag identification would require decrypting the payload content, which is not currently feasible.
8. **Encryption is in the ASIC, not the tag.** The custom LEGO ASIC (DA000001-01) reads raw EEPROM data from the tag, decrypts it internally, and passes the decrypted content to the EM9305 processor via SPI. The encryption algorithm is **unknown** — it is baked into the ASIC silicon and inaccessible from the EM9305 firmware or via JTAG. This is a stronger protection than the Grain-128A hypothesis from the Coffee & Fun blog.
9. **Tags carry content selectors, not play logic.** A tag's decrypted payload contains content identifiers that select three independent assets from the ROFS: a play script, an audio bank, and an animation bank. All actual play logic lives in `play.bin` on the brick.

### Card protection assessment

LEGO did an excellent job at protecting the content of the NFC cards. Although full card cloning is possible (the SMART Brick does not use hardware authentication), the payload itself is encrypted within the custom ASIC (DA000001-01) and factory-locked, making it extremely difficult to understand, modify, or forge the data. The encryption algorithm is unknown — it is embedded in the ASIC's internal silicon logic, not accessible from the EM9305 firmware, the BLE interface, or any external debug port.

Getting past this protection would require one of the following approaches:

- **Reverse-engineer the ASIC silicon** — The decryption key and algorithm are inside the custom LEGO ASIC (DA000001-01). Decapping the chip and die-level analysis is the most direct path, but extremely difficult. The EM9305 firmware contains no decryption logic — it receives already-decrypted content via SPI.
- **Side-channel attack on the ASIC** — Use power analysis or electromagnetic probing during tag reads to recover the decryption key from the ASIC's analog operations.
- **Exploit the EM9305 ↔ ASIC SPI bus** — The ASIC sends decrypted tag content to the EM9305 via memory-mapped registers at `0xF04000`–`0xF04BFF`. Intercepting or probing this SPI bus during tag reads could reveal plain-text tag content, bypassing the encryption entirely.
- **Compare payloads across identical bricks** — Tags with the same Content ID have byte-for-byte identical EEPROM data (confirmed by node-smartplay). Obtaining tags with similar Content IDs and diffing their encrypted payloads could reveal structural patterns.
- **Exploit the writable blocks (64–65)** — Investigate whether writing specific data to the two unlocked blocks triggers observable behaviour changes that leak information about the encryption.

---

## Cross-Reference with Coffee & Fun Blog Dumps

The blog [Reverse Engineering LEGO Smart Play (Part 1)](https://www.coffeeandfun.com/blog/reverse-engineering-lego-smart-play-nfc-part-1/) from Coffee & Fun LLC (Feb 28, 2026) documents the LEGO Star Wars 75423 X-Wing set. They scanned 8 tags with a Flipper Zero and published full `.nfc` dump files. Only Part 1 has been published; there is no Part 2 or Part 3 yet.

### Blog tag inventory (LEGO Star Wars 75423 X-Wing set)

| Tag name | Piece | UID | Brick ID | Data bytes |
| --- | --- | --- | --- | --- |
| Car | X-Wing body | `E0:16:5C:01:22:14:09:B4` | `0x69` (105) | 105 |
| Xwing | Wing tag | `E0:16:5C:01:1B:F4:E6:04` | `0x6B` (107) | 107 |
| Luke | Luke Skywalker minifig | `E0:16:5C:01:1B:C5:76:6A` | `0x9D` (157) | 157 |
| Layla | Princess Leia minifig | `E0:16:5C:01:27:38:FF:A4` | `0x9E` (158) | 158 |
| Rd | R2-D2 small tag | `E0:16:5C:01:26:19:A8:ED` | `0x4A` (74) | 74 |
| Shooter | Weapon tag | `E0:16:5C:01:22:14:09:C7` | — | — |
| Vacrum | Accessory tag | `E0:16:5C:01:22:1A:B3:F1` | — | — |
| Unnameable_corridor | Additional tag | `E0:16:5C:01:27:6D:02:BA` | — | — |

> **Note:** Only the Car and Luke dump files on the blog contain valid `00-[len]-01-0C` headers. The other 6 files (Xwing, Layla, Rd, Shooter, Vacrum, Unnameable_corridor) appear to have corrupted or scrambled block data, making direct byte comparison impossible for those.

### Matching Brick IDs — Our cards vs. the blog

| Our card | Our Brick ID | Blog match | Blog piece | Same tag type? |
| --- | --- | --- | --- | --- |
| **Card 1** | `0x6B` (107) | **Xwing** | Wing tag | **Yes** — same Brick ID, same data length |
| Card 2 | `0x3B` (59) | — | TIE Fighter vehicle, set 75421 (*not in blog set*) | No match |
| **Card 3** | `0x9E` (158) | **Layla** | Princess Leia minifig | **Yes** — same Brick ID, same data length |
| Card 4 | `0xA9` (169) | — | Darth Vader minifig, set 75421 (*not in blog set*) | No match |

### What this tells us

- **Our Card 1 is a wing tag** (`0x6B`), the same type as the X-Wing wing piece in the 75423 set.
- **Our Card 3 is a Princess Leia minifigure tag** (`0x9E`), the same type as the Leia in the 75423 set.
- **Our Card 2 is a TIE Fighter vehicle tag** (`0x3B` = 59) from set **75421** (Darth Vader's TIE Fighter), not present in the 75423 X-Wing blog set.
- **Our Card 4 is a Darth Vader minifigure tag** (`0xA9` = 169) from set **75421**, not present in the 75423 X-Wing blog set.
- **UIDs are different** between our cards and the blog's. This is expected since UIDs are factory-unique per physical chip.
- **Payload data is identical for matching Content IDs.** The node-smartplay project confirmed that tags with the same Content ID have byte-for-byte identical EEPROM data, regardless of UID. This means the encryption is NOT UID-diversified — the payload is a fixed blob per Content ID, programmed identically into all physical tags of that type at the factory.

### Known Brick ID catalog (combined)

| Brick ID | Decimal | Data bytes | Tag type | Source |
| --- | --- | --- | --- | --- |
| `0x3B` | 59 | 59 | TIE Fighter vehicle | Our set (75421) |
| `0x4A` | 74 | 74 | R2-D2 accessory tag | Blog (75423) + node-smartplay |
| `0x69` | 105 | 105 | X-Wing body (vehicle) | Blog (75423) |
| `0x6B` | 107 | 107 | X-Wing wing tag | Blog (75423) + Our set + node-smartplay |
| `0x7E` | 126 | 126 | Lightsaber tile | node-smartplay |
| `0x9D` | 157 | 157 | Luke Skywalker minifig | Blog (75423) + node-smartplay |
| `0x9E` | 158 | 158 | Princess Leia minifig | Blog (75423) + Our set + node-smartplay |
| `0xA9` | 169 | 169 | Darth Vader minifig | Our set (75421) + node-smartplay |
| `0xAB` | 171 | 171 | Emperor Palpatine minifig | node-smartplay |

### Blog chip identification

The blog identifies the IC as likely an **EM4237** (or close variant) based on IC reference `0x17`. Research from the [node-smartplay](https://github.com/nathankellenicki/node-smartplay) project has since determined this is a **custom LEGO die** fabricated by EM Microelectronic — IC ref `0x17` does not match any off-the-shelf EM product. The blog hypothesises LEGO does challenge-response authentication (Grain-128A `EM_AUTH`) during play.

Our own cloning test disproves this hypothesis: a full clone (UID + all memory) onto a blank EM4237 chip **is recognised by the official SMART Brick**. This means the SMART Brick does not perform `EM_AUTH` challenge-response during play — it only reads the memory content. The node-smartplay project further confirms this: the EM9305 firmware sends no `EM_AUTH` or Login commands — only standard ISO 15693 reads. The ASIC handles all tag reading autonomously via hardware registers. See [this video showing the fully cloned NFC](https://www.reddit.com/r/lego/comments/1rmpvwv/hacking_the_smart_brick/).

### Block security confirmation

The blog's Flipper Zero dumps show block security status for all tags:

- **Blocks 0–63:** Status `0x01` (locked)
- **Blocks 64–65:** Status `0x00` (unlocked, writable)

The 2 unlocked blocks at the end (8 bytes) are all zeros on factory-fresh tags. The blog speculates these are reserved for write-back during play (e.g., character progression, session data, achievements).

### Payload size patterns

Combining both sources, tag types correlate with data size ranges:

| Tag category | Brick ID range | Data bytes | Examples |
| --- | --- | --- | --- |
| Vehicle (small) | `0x3B` | 59 | TIE Fighter (set 75421) |
| Small accessories | `0x4A` | 74 | R2-D2 |
| Vehicle / body tags | `0x69`–`0x6B` | 105–107 | X-Wing body, wing tag |
| Minifigures | `0x9D`–`0xA9` | 157–169 | Luke, Leia, Darth Vader |

Minifigure tags carry roughly 2–3× more data than small accessories, which the blog suggests may encode character attributes, identity, or progression state in the encrypted payload.

---

## node-smartplay Reverse Engineering Findings

The [node-smartplay](https://github.com/nathankellenicki/node-smartplay) project by Nathan Kellenicki provides an extensive reverse engineering of the LEGO Smart Play system, based on firmware binary analysis (ARC disassembly of the EM9305), Android APK decompilation (Il2CppDumper), BLE traffic captures (btsnoop), ROFS content extraction, and backend API probing. The findings below significantly advance our understanding of the tag format and system architecture.

### SMART Brick Chipset

The SMART Brick contains two main ICs:

| Chip | Marking | Role |
| --- | --- | --- |
| **Custom LEGO ASIC** | `DA000001-01` / `DNP6G-010` | 4.1mm mixed-signal ASIC — coil control (tag reading, wireless charging, brick-to-brick positioning), LED array, sensors, analog synthesizer |
| **EM9305** | EM Microelectronic BLE 5.4 SoC | 32-bit ARC CPU, 512KB flash. Handles BLE (app GATT, PAwR play communication), runs play engine |

The EM9305 is the main processor — firmware is raw ARC machine code (~499KB) with the full Cordio BLE stack. The ASIC handles all analog functions (coils, sensors, audio output). `P11_audiobrick_EM` in firmware strings refers to EM Microelectronic.

### Tag IC — Custom EM Microelectronic Die

The tag IC with reference `0x17` is a **custom LEGO die** fabricated by EM Microelectronic. It does not match any off-the-shelf EM product:

| Property | Custom LEGO Die | EM4233SLIC | EM4233 |
| --- | --- | --- | --- |
| IC Reference | `0x17` | `0x02` | `0x09` |
| Memory | **66 blocks** (264 bytes) | 32 blocks (128 bytes) | 64 blocks (256 bytes) |
| Manufacturer | EM Microelectronic (`0x16`) | EM Microelectronic (`0x16`) | EM Microelectronic (`0x16`) |

Standard ISO 15693 reads work without any Login or authentication. No custom commands (`0xA0`–`0xDF`) or Login (`0xE4`) are needed. The ASIC internally generates all ISO 15693 RF frames.

### Tag Types

| Type | Description | Data Size |
| --- | --- | --- |
| **Identity** | Smart Minifigs — character identity | ~74–171 bytes |
| **Item** | Smart Tiles — content selector that tells the brick which play script to run | ~107–126 bytes |

R2-D2 is physically a tile but acts as an Identity tag (the classification is in the encrypted payload, not visible in cleartext).

### Tag Data Format (after ASIC decryption)

The ASIC reads raw EEPROM, decrypts it, and sends a restructured response to the EM9305 over SPI. The EM9305 **never sees the cleartext header** (`00 XX 01 0C`) — it receives already-parsed content. The tag event struct delivered to the play engine contains:

| Offset | Size | Purpose |
| --- | --- | --- |
| 4 | 4 bytes | **Event type** (determines dispatch path) |
| 8 | 4 bytes | **Content reference** (identity) or **audio bank + class + variant** (item) |
| 9 | 1 byte | Content class (item tags) |
| 10 | 1–2 bytes | Content variant (item tags) |
| 12 | 4 bytes | Extra parameter |
| 16 | 4 bytes | Extended data (item tags, content types 3/4) |

Identity tags carry a simple 32-bit content reference. Item tags carry significantly more data, specifying audio bank, content class, variant, and type.

### Tag Event Types

| Type | Magic ID | Description |
| --- | --- | --- |
| 1 | `0xA7E24ED1` | Identity tag placed (minifig) |
| 2 | `0x0BBDA113` | Item tag placed (tile) |
| 3 | `0x812312DC` | Play command |
| 4 | `0x814A0D84` | Distributed play (triggers PAwR multi-brick session) |
| 5 | `0xA7E24ED1` | Identity alias |
| 6 | `0xE3B77171` | Status/position event |

### Security Model

LEGO’s tag security operates at **two layers**:

1. **EM9305 ↔ ASIC authentication** (AES-128 + ECC): The EM9305 derives a 16-byte key from ROFS config data + hardware OTP, written to ASIC register `0xF04084`. This authenticates the EM9305 to the ASIC — prevents rogue processors from reading decrypted tag data. Per-brick unique (hardware OTP dependent).

2. **Tag data encryption** (algorithm unknown, in ASIC silicon): Raw EEPROM content is encrypted. The ASIC decrypts it after reading, before passing to the EM9305. The decryption key is in the ASIC's internal logic — not accessible from the EM9305 firmware or via JTAG. All bricks share the same decryption capability (tags work on any brick).

Tag access itself is **completely open** — no password, no page protection, no privacy mode. Anyone with an NFC reader can dump the raw EEPROM. But the data is meaningless without the ASIC's decryption key.

### Tag Reading Pipeline

The copper coils are time-multiplexed between tag reading, wireless charging, brick-to-brick positioning, and audio:

```text
ASIC Coils (13.56 MHz ISO 15693 RF)
  ↓ Inventory, anti-collision, tag read — all in hardware
SPI Driver (memory-mapped registers 0xF01800–0xF04BFF)
  ↓ Status, data, DMA transfers (512-byte, 14 × 32-bit words)
Interrupt Handler (coil interrupt vector 0xAA)
  ↓ 600ms timer-based coil scheduling
ASIC-to-RAM Copy
  ↓ 4 × 20-byte blocks to RAM mirror
Tag Scan Loop (polls for tag presence)
  ↓ Dual-read + complement validation
UID Extraction (parses ISO 15693 response)
  ↓ Compares against stored UID
TLV Content Parser (block type 0x2000 = new, 0x1000 = continuation)
  ↓ Reassembles multi-packet data
Manufacturer Dispatch (EM=0x16 / TI=0x07 / ST=0x0D)
  ↓ 2D handler table at ROM
Play Engine
```

Tag polling runs on a **600ms timer**. The ASIC periodically scans for tag presence. Tag removal has a **20-tick grace period** (~320ms at 62.5 Hz) — if the tag returns within that window, playback continues uninterrupted.

### Multi-Manufacturer Tag Support

The firmware supports tags from **multiple IC manufacturers** via a dispatch at `0x5C96C`:

| Code | Manufacturer | Notes |
| --- | --- | --- |
| `0x07` | Texas Instruments | Secondary IC ref check |
| `0x0D` | STMicroelectronics | IC ref must be `0x0F` |
| **`0x16`** | **EM Microelectronic** | **Production Smart Tags** |
| `0x17` | Texas Instruments (new) | Direct accept |

For custom tags, the safest approach is to use the EM path (manufacturer code `0x16`) with properly formatted data.

### Play Engine Architecture

The SMART Brick firmware is a **generic play engine** — it doesn't know what an X-Wing, a castle, or a spaceship is. It provides:

- A **144-opcode software synthesizer** for procedural audio generation (not PCM playback)
- A **25-opcode semantic tree executor** for reactive scripting
- A **PPL (Play Preset Language) state machine** for content sequencing
- **Sensor reading** (accelerometer, light/colour, sound)
- **PAwR messaging** for inter-brick communication (BLE 5.4)

All specific behaviour lives in **ROFS content files** (`play.bin`, `audio.bin`, `animation.bin`), not in the firmware. The tag is a **parameterized content selector**. Different tags can share the same script but produce completely different experiences by referencing different audio and animation banks.

### On-Device Content (ROFS)

The ROFS (Read-Only File System) is baked into the firmware binary (zlib-compressed, ~95 KB → ~165 KB):

| File | Magic | Size (v0.72.1) | Contents |
| --- | --- | --- | --- |
| `play.bin` | `7F PPL` | 22,690 bytes | Play Preset Library — 5 presets, 58 script blocks |
| `audio.bin` | `7F AAP` | 132,569 bytes | Audio Assets Pack — 3 banks, 154 clips of **synthesizer instructions** (not PCM) |
| `animation.bin` | `7F ANI` | 9,407 bytes | Animation data — 9 banks, 135 LED clips |
| `version.txt` | (text) | 17 bytes | Version string `"0.72.1"` currently `0.72.33` |

New content (scripts, sounds, animations) **requires a firmware update** — there is no separate content update channel. However, LEGO can produce new tags that reference any combination of the existing 58 scripts × 3 audio banks × 9 animation banks without a firmware update.

### BLE Protocol and Authentication

When docked on a charging base, the SMART Brick advertises as `"Smart Brick"` over BLE with WDX service UUID `0xFEF6`. Communication uses a register-based protocol over the Control Point characteristic.

**ECDSA P-256** challenge-response authentication is required for privileged operations:

| Requires Auth | No Auth Needed |
| --- | --- |
| Firmware updates | Read battery, volume |
| Factory reset | Set volume, name |
| Telemetry consent | Read MAC address |
| Unlock | Read firmware version |

The ECDSA private key is held exclusively on LEGO’s servers at `p11.bilbo.lego.com`. The companion app proxies signing via the backend API. Analysis of 6 captured signatures confirmed proper random `k` values, ruling out nonce-reuse recovery.

**Smart Tag events are NOT exposed over BLE.** The tag pipeline is a closed loop: physical RFID → ASIC → EM9305 firmware → play engine. There is no BLE path to inject or observe tag events.

### Multi-Brick Communication (PAwR / BrickNet)

Undocked bricks use **BLE 5.4 PAwR (Periodic Advertising with Responses)** for device-to-device play communication. They don't advertise via standard BLE — invisible to scanners. One brick is the **coordinator**, others are **responders**.

PAwR is triggered by Smart Tags whose play content requires multi-brick communication (content types `0x02` and `0x04`). Inter-brick messages are **25-byte packets** with XOR-encrypted payloads (lightweight obfuscation, not AES).

### Case Study: X-Wing vs Imperial Turret

Both the X-Wing and Imperial Turret tags select the same play script (Script 42 — the only type-0x0E script with PAwR capability, 1,564 bytes, 6 branches). They share identical gameplay logic (motion detection, colour sensor trigger, PAwR combat, damage progression) but produce different experiences through different audio and animation bank selections. The firmware has no concept of "X-Wing" or "turret" — it only knows sensor thresholds, audio bank IDs, and animation bank IDs.

### Content ID Field Analysis

The cleartext Content ID (byte 1 of header) is consumed solely by the ASIC silicon — the EM9305 firmware contains zero references to any known Content ID value. The ASIC likely uses it for decryption key/route selection. The actual content identity used by the play engine comes from the **decrypted payload** as a 32-bit content reference.

The cleartext header may exist for future use cases (e.g., phone-based tag identification without decryption) or may be purely an ASIC-internal routing mechanism.

### Additional References

- [node-smartplay GitHub](https://github.com/nathankellenicki/node-smartplay) — Full reverse engineering notes covering hardware, BLE protocol, firmware analysis, backend API, and file transfer
- [Packetcraft Cordio BLE stack](https://github.com/packetcraft-inc/stacks) — Open-source Cordio with WDX protocol definitions
- [r/LegoSmartBrick](https://reddit.com/r/LegoSmartBrick) — Community teardown photos and discussion
