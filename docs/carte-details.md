# LEGO Smart Play NFC Card Dumps — Annotated

All the payload has been dumped using .NET nanoFramework and a [PN5180](https://github.com/nanoframework/nanoFramework.IoT.Device/tree/main/devices/Pn5180).

> Known elements are marked with `◄` annotations.
> See [lego-smart-brick-nfc.md](lego-smart-brick-nfc.md) for the full analysis.

## UID structure (all cards)

```text
  UID: XX-XX-XX-XX-01-5C-16-E0
       ├─────────┘ │  │  │  └── E0 = ISO 15693 family identifier
       │           │  │  └───── 16 = EM Microelectronic (manufacturer code)
       │           │  └──────── 5C ┐ LEGO product code (constant across all cards)
       │           └─────────── 01 ┘
       └─────────────────────── Unique serial number (4 bytes, varies per chip)
```

## System info structure (all cards)

```text
  System info: 00-0F-[UID x8]-00-00-41-03-17
               │  │            │  │  │  │  └── 17 = IC reference (EM4237 chip)
               │  │            │  │  │  └───── 03 = block size (4 bytes)
               │  │            │  │  └──────── 41 = block count (66 blocks, 0-65)
               │  │            │  └─────────── 00 = AFI (unused)
               │  │            └──────────────  00 = DSFID (unused)
               │  └──────────────────────────── 0F = info flags (all fields present)
               └─────────────────────────────── 00 = response flags (OK)
```

## Block 0 header structure (all cards)

```text
  Block 000: 00-XX-01-0C
             │  │  │  └── 0C = LEGO Smart Play card type marker (constant)
             │  │  └───── 01 = format version (constant)
             │  └──────── XX = Brick ID (= total data length in bytes)
             └─────────── 00 = flags (reserved)
```

---

## Card 1 — Wing Tag (X-Wing wing piece, Brick ID 0x6B = 107 bytes)

```text
ISO 15693 card detected:                                          ◄ NFC-V / ISO 15693 protocol
  UID: A9-08-78-27-01-5C-16-E0                                    ◄ Serial: A9-08-78-27
  DSFID: 0x00
  Slot: 9
Getting system information...
  DSFID: 0x00
  AFI: 0x00
  System info data: 00-0F-A9-08-78-27-01-5C-16-E0-00-00-41-03-17  ◄ EM4237, 66 blocks x 4 bytes
  Memory: 66 blocks x 4 bytes = 264 bytes (2112 bits)
--- Full memory dump (66 blocks) ---
  Block 000: 00-6B-01-0C  |.k..|                                 ◄ HEADER — Brick ID 0x6B=107 (Wing tag), v1, type 0x0C
  Block 001: 01-99-F4-93  |....|                                 ◄ PAYLOAD START — 01=payload version, then encrypted
  Block 002: 76-76-DC-5C  |vv.\|                                 ◄ Encrypted payload (Grain-128A)
  Block 003: CC-C0-2D-6B  |..-k|                                 ◄ Encrypted payload
  Block 004: FA-BA-0A-F0  |....|                                 ◄ Encrypted payload
  Block 005: 36-19-74-FD  |6.t.|                                 ◄ Encrypted payload
  Block 006: 2C-AD-33-A8  |,.3.|                                 ◄ Encrypted payload
  Block 007: 40-2F-19-04  |@/..|                                 ◄ Encrypted payload
  Block 008: E4-F4-75-56  |..uV|                                 ◄ Encrypted payload
  Block 009: 56-AA-E2-FF  |V...|                                 ◄ Encrypted payload
  Block 010: A6-19-B6-4E  |...N|                                 ◄ Encrypted payload
  Block 011: 28-07-A1-D2  |(...|                                 ◄ Encrypted payload
  Block 012: AC-8A-43-86  |..C.|                                 ◄ Encrypted payload
  Block 013: 50-55-E5-8C  |PU..|                                 ◄ Encrypted payload
  Block 014: C5-53-48-C6  |.SH.|                                 ◄ Encrypted payload
  Block 015: F4-8C-D7-73  |...s|                                 ◄ Encrypted payload
  Block 016: 84-2C-BF-3C  |.,.<|                                 ◄ Encrypted payload
  Block 017: 93-5C-DE-60  |.\.`|                                 ◄ Encrypted payload
  Block 018: 9B-3D-A1-DB  |.=..|                                 ◄ Encrypted payload
  Block 019: 68-10-23-6D  |h.#m|                                 ◄ Encrypted payload
  Block 020: CC-F0-C4-35  |...5|                                 ◄ Encrypted payload
  Block 021: 1B-B0-BC-6F  |...o|                                 ◄ Encrypted payload
  Block 022: 0D-4B-B4-E3  |.K..|                                 ◄ Encrypted payload
  Block 023: EA-81-84-20  |... |                                 ◄ Encrypted payload
  Block 024: B1-ED-C6-C7  |....|                                 ◄ Encrypted payload
  Block 025: 2A-47-C6-1D  |*G..|                                 ◄ Encrypted payload
  Block 026: 3A-3E-B8-00  |:>..|                                 ◄ PAYLOAD END — 3 data bytes + 1 zero pad (107 total ✓)
  Block 027: 00-00-00-00  |....|                                 ◄ Empty — zero-padded, locked
  Block 028: 00-00-00-00  |....|                                 ◄ ┆
  Block 029: 00-00-00-00  |....|                                 ◄ ┆
  Block 030: 00-00-00-00  |....|                                 ◄ ┆
  Block 031: 00-00-00-00  |....|                                 ◄ ┆
  Block 032: 00-00-00-00  |....|                                 ◄ ┆
  Block 033: 00-00-00-00  |....|                                 ◄ ┆
  Block 034: 00-00-00-00  |....|                                 ◄ ┆
  Block 035: 00-00-00-00  |....|                                 ◄ ┆
  Block 036: 00-00-00-00  |....|                                 ◄ ┆
  Block 037: 00-00-00-00  |....|                                 ◄ ┆
  Block 038: 00-00-00-00  |....|                                 ◄ ┆
  Block 039: 00-00-00-00  |....|                                 ◄ ┆
  Block 040: 00-00-00-00  |....|                                 ◄ ┆
  Block 041: 00-00-00-00  |....|                                 ◄ ┆
  Block 042: 00-00-00-00  |....|                                 ◄ ┆
  Block 043: 00-00-00-00  |....|                                 ◄ ┆
  Block 044: 00-00-00-00  |....|                                 ◄ ┆
  Block 045: 00-00-00-00  |....|                                 ◄ ┆
  Block 046: 00-00-00-00  |....|                                 ◄ ┆
  Block 047: 00-00-00-00  |....|                                 ◄ ┆
  Block 048: 00-00-00-00  |....|                                 ◄ ┆
  Block 049: 00-00-00-00  |....|                                 ◄ ┆
  Block 050: 00-00-00-00  |....|                                 ◄ ┆
  Block 051: 00-00-00-00  |....|                                 ◄ ┆
  Block 052: 00-00-00-00  |....|                                 ◄ ┆
  Block 053: 00-00-00-00  |....|                                 ◄ ┆
  Block 054: 00-00-00-00  |....|                                 ◄ ┆
  Block 055: 00-00-00-00  |....|                                 ◄ ┆
  Block 056: 00-00-00-00  |....|                                 ◄ ┆
  Block 057: 00-00-00-00  |....|                                 ◄ ┆
  Block 058: 00-00-00-00  |....|                                 ◄ ┆
  Block 059: 00-00-00-00  |....|                                 ◄ ┆
  Block 060: 00-00-00-00  |....|                                 ◄ ┆
  Block 061: 00-00-00-00  |....|                                 ◄ ┆
  Block 062: 00-00-00-00  |....|                                 ◄ ┆
  Block 063: 00-00-00-00  |....|                                 ◄ Empty end — blocks 0-63 are LOCKED (read-only)
  Block 064: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
  Block 065: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
--- End of memory dump ---
```

---

## Card 2 — TIE Fighter Vehicle, set 75421 (Brick ID 0x3B = 59 bytes)

```text
ISO 15693 card detected:
  UID: 4A-E7-0C-22-01-5C-16-E0                                    ◄ Serial: 4A-E7-0C-22
  DSFID: 0x00
  Slot: 10
  *** Card changed! Previous UID: A9-08-78-27-01-5C-16-E0 ***
Getting system information...
  DSFID: 0x00
  AFI: 0x00
  System info data: 00-0F-4A-E7-0C-22-01-5C-16-E0-00-00-41-03-17  ◄ EM4237, 66 blocks x 4 bytes
  Memory: 66 blocks x 4 bytes = 264 bytes (2112 bits)
--- Full memory dump (66 blocks) ---
  Block 000: 00-3B-01-0C  |.;..|                                 ◄ HEADER — Brick ID 0x3B=59 (TIE Fighter, set 75421), v1, type 0x0C
  Block 001: 01-9C-D5-2D  |...-|                                 ◄ PAYLOAD START — 01=payload version, then encrypted
  Block 002: 72-F5-50-61  |r.Pa|                                 ◄ Encrypted payload
  Block 003: AB-8F-73-D0  |..s.|                                 ◄ Encrypted payload
  Block 004: D3-57-FA-07  |.W..|                                 ◄ Encrypted payload
  Block 005: 13-9C-3F-44  |..?D|                                 ◄ Encrypted payload
  Block 006: 0F-55-ED-88  |.U..|                                 ◄ Encrypted payload
  Block 007: D9-D4-8F-8B  |....|                                 ◄ Encrypted payload
  Block 008: 63-5D-CE-9E  |c]..|                                 ◄ Encrypted payload
  Block 009: A9-35-21-B6  |.5!.|                                 ◄ Encrypted payload
  Block 010: 49-10-DA-E1  |I...|                                 ◄ Encrypted payload
  Block 011: 5A-17-F0-D5  |Z...|                                 ◄ Encrypted payload
  Block 012: E6-9E-A1-4C  |...L|                                 ◄ Encrypted payload
  Block 013: 80-6E-CF-4E  |.n.N|                                 ◄ Encrypted payload
  Block 014: 90-68-E7-00  |.h..|                                 ◄ PAYLOAD END — 3 data bytes + 1 zero pad (59 total ✓)
  Block 015: 00-00-00-00  |....|                                 ◄ Empty — zero-padded, locked
  Block 016: 00-00-00-00  |....|                                 ◄ ┆
  Block 017: 00-00-00-00  |....|                                 ◄ ┆
  Block 018: 00-00-00-00  |....|                                 ◄ ┆
  Block 019: 00-00-00-00  |....|                                 ◄ ┆
  Block 020: 00-00-00-00  |....|                                 ◄ ┆
  Block 021: 00-00-00-00  |....|                                 ◄ ┆
  Block 022: 00-00-00-00  |....|                                 ◄ ┆
  Block 023: 00-00-00-00  |....|                                 ◄ ┆
  Block 024: 00-00-00-00  |....|                                 ◄ ┆
  Block 025: 00-00-00-00  |....|                                 ◄ ┆
  Block 026: 00-00-00-00  |....|                                 ◄ ┆
  Block 027: 00-00-00-00  |....|                                 ◄ ┆
  Block 028: 00-00-00-00  |....|                                 ◄ ┆
  Block 029: 00-00-00-00  |....|                                 ◄ ┆
  Block 030: 00-00-00-00  |....|                                 ◄ ┆
  Block 031: 00-00-00-00  |....|                                 ◄ ┆
  Block 032: 00-00-00-00  |....|                                 ◄ ┆
  Block 033: 00-00-00-00  |....|                                 ◄ ┆
  Block 034: 00-00-00-00  |....|                                 ◄ ┆
  Block 035: 00-00-00-00  |....|                                 ◄ ┆
  Block 036: 00-00-00-00  |....|                                 ◄ ┆
  Block 037: 00-00-00-00  |....|                                 ◄ ┆
  Block 038: 00-00-00-00  |....|                                 ◄ ┆
  Block 039: 00-00-00-00  |....|                                 ◄ ┆
  Block 040: 00-00-00-00  |....|                                 ◄ ┆
  Block 041: 00-00-00-00  |....|                                 ◄ ┆
  Block 042: 00-00-00-00  |....|                                 ◄ ┆
  Block 043: 00-00-00-00  |....|                                 ◄ ┆
  Block 044: 00-00-00-00  |....|                                 ◄ ┆
  Block 045: 00-00-00-00  |....|                                 ◄ ┆
  Block 046: 00-00-00-00  |....|                                 ◄ ┆
  Block 047: 00-00-00-00  |....|                                 ◄ ┆
  Block 048: 00-00-00-00  |....|                                 ◄ ┆
  Block 049: 00-00-00-00  |....|                                 ◄ ┆
  Block 050: 00-00-00-00  |....|                                 ◄ ┆
  Block 051: 00-00-00-00  |....|                                 ◄ ┆
  Block 052: 00-00-00-00  |....|                                 ◄ ┆
  Block 053: 00-00-00-00  |....|                                 ◄ ┆
  Block 054: 00-00-00-00  |....|                                 ◄ ┆
  Block 055: 00-00-00-00  |....|                                 ◄ ┆
  Block 056: 00-00-00-00  |....|                                 ◄ ┆
  Block 057: 00-00-00-00  |....|                                 ◄ ┆
  Block 058: 00-00-00-00  |....|                                 ◄ ┆
  Block 059: 00-00-00-00  |....|                                 ◄ ┆
  Block 060: 00-00-00-00  |....|                                 ◄ ┆
  Block 061: 00-00-00-00  |....|                                 ◄ ┆
  Block 062: 00-00-00-00  |....|                                 ◄ ┆
  Block 063: 00-00-00-00  |....|                                 ◄ Empty end — blocks 0-63 LOCKED
  Block 064: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
  Block 065: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
--- End of memory dump ---
```

---

## Card 3 — Princess Leia Minifigure (Brick ID 0x9E = 158 bytes)

```text
ISO 15693 card detected:
  UID: 6D-EA-D4-26-01-5C-16-E0                                    ◄ Serial: 6D-EA-D4-26
  DSFID: 0x00
  Slot: 13
Getting system information...
  DSFID: 0x00
  AFI: 0x00
  System info data: 00-0F-6D-EA-D4-26-01-5C-16-E0-00-00-41-03-17  ◄ EM4237, 66 blocks x 4 bytes
  Memory: 66 blocks x 4 bytes = 264 bytes (2112 bits)
--- Full memory dump (66 blocks) ---
  Block 000: 00-9E-01-0C  |....|                                 ◄ HEADER — Brick ID 0x9E=158 (Princess Leia), v1, type 0x0C
  Block 001: 01-0B-61-49  |..aI|                                 ◄ PAYLOAD START — 01=payload version, then encrypted
  Block 002: 1A-35-D3-D0  |.5..|                                 ◄ Encrypted payload
  Block 003: D0-16-B3-B4  |....|                                 ◄ Encrypted payload
  Block 004: 54-2A-5E-43  |T*^C|                                 ◄ Encrypted payload
  Block 005: 88-D4-FE-B3  |....|                                 ◄ Encrypted payload
  Block 006: AA-22-BE-60  |.".`|                                 ◄ Encrypted payload
  Block 007: 1A-1E-1F-98  |....|                                 ◄ Encrypted payload
  Block 008: 34-D4-3F-FA  |4.?.|                                 ◄ Encrypted payload
  Block 009: 6F-BA-65-52  |o.eR|                                 ◄ Encrypted payload
  Block 010: FF-1F-5B-FE  |..[.|                                 ◄ Encrypted payload
  Block 011: 5B-2D-2D-8A  |[--.|                                 ◄ Encrypted payload
  Block 012: F9-B8-43-0E  |..C.|                                 ◄ Encrypted payload
  Block 013: 05-32-1F-7F  |.2..|                                 ◄ Encrypted payload
  Block 014: 7C-27-0C-5F  ||'._|                                 ◄ Encrypted payload
  Block 015: 5E-7F-83-9C  |^...|                                 ◄ Encrypted payload
  Block 016: FB-D5-81-C2  |....|                                 ◄ Encrypted payload
  Block 017: 43-0D-16-F9  |C...|                                 ◄ Encrypted payload
  Block 018: E5-6B-58-21  |.kX!|                                 ◄ Encrypted payload
  Block 019: 0B-D3-74-81  |..t.|                                 ◄ Encrypted payload
  Block 020: A2-5B-77-92  |.[w.|                                 ◄ Encrypted payload
  Block 021: DD-63-4D-AB  |.cM.|                                 ◄ Encrypted payload
  Block 022: 38-EC-92-9C  |8...|                                 ◄ Encrypted payload
  Block 023: 77-13-43-D6  |w.C.|                                 ◄ Encrypted payload
  Block 024: B6-87-D4-BE  |....|                                 ◄ Encrypted payload
  Block 025: DA-5C-79-67  |.\yg|                                 ◄ Encrypted payload
  Block 026: B0-6E-0A-0D  |.n..|                                 ◄ Encrypted payload
  Block 027: FE-7D-59-FB  |.}Y.|                                 ◄ Encrypted payload
  Block 028: 69-9A-43-63  |i.Cc|                                 ◄ Encrypted payload
  Block 029: 2C-05-0C-27  |,..'|                                 ◄ Encrypted payload
  Block 030: 9C-E4-BD-5D  |...]|                                 ◄ Encrypted payload
  Block 031: A2-DE-9C-A2  |....|                                 ◄ Encrypted payload
  Block 032: 90-D4-5A-D3  |..Z.|                                 ◄ Encrypted payload
  Block 033: 28-74-4B-D6  |(tK.|                                 ◄ Encrypted payload
  Block 034: 06-C7-09-94  |....|                                 ◄ Encrypted payload
  Block 035: 12-D5-7E-99  |..~.|                                 ◄ Encrypted payload
  Block 036: 94-C8-8C-85  |....|                                 ◄ Encrypted payload
  Block 037: 1F-5B-E3-EA  |.[..|                                 ◄ Encrypted payload
  Block 038: 17-74-EE-93  |.t..|                                 ◄ Encrypted payload
  Block 039: FD-C4-00-00  |....|                                 ◄ PAYLOAD END — 2 data bytes + 2 zero pad (158 total ✓)
  Block 040: 00-00-00-00  |....|                                 ◄ Empty — zero-padded, locked
  Block 041: 00-00-00-00  |....|                                 ◄ ┆
  Block 042: 00-00-00-00  |....|                                 ◄ ┆
  Block 043: 00-00-00-00  |....|                                 ◄ ┆
  Block 044: 00-00-00-00  |....|                                 ◄ ┆
  Block 045: 00-00-00-00  |....|                                 ◄ ┆
  Block 046: 00-00-00-00  |....|                                 ◄ ┆
  Block 047: 00-00-00-00  |....|                                 ◄ ┆
  Block 048: 00-00-00-00  |....|                                 ◄ ┆
  Block 049: 00-00-00-00  |....|                                 ◄ ┆
  Block 050: 00-00-00-00  |....|                                 ◄ ┆
  Block 051: 00-00-00-00  |....|                                 ◄ ┆
  Block 052: 00-00-00-00  |....|                                 ◄ ┆
  Block 053: 00-00-00-00  |....|                                 ◄ ┆
  Block 054: 00-00-00-00  |....|                                 ◄ ┆
  Block 055: 00-00-00-00  |....|                                 ◄ ┆
  Block 056: 00-00-00-00  |....|                                 ◄ ┆
  Block 057: 00-00-00-00  |....|                                 ◄ ┆
  Block 058: 00-00-00-00  |....|                                 ◄ ┆
  Block 059: 00-00-00-00  |....|                                 ◄ ┆
  Block 060: 00-00-00-00  |....|                                 ◄ ┆
  Block 061: 00-00-00-00  |....|                                 ◄ ┆
  Block 062: 00-00-00-00  |....|                                 ◄ ┆
  Block 063: 00-00-00-00  |....|                                 ◄ Empty end — blocks 0-63 LOCKED
  Block 064: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
  Block 065: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
--- End of memory dump ---
```

---

## Card 4 — Darth Vader Minifigure, set 75421 (Brick ID 0xA9 = 169 bytes)

```text
ISO 15693 card detected:
  UID: FC-F1-6F-2A-01-5C-16-E0                                    ◄ Serial: FC-F1-6F-2A
  DSFID: 0x00
  Slot: 12
Getting system information...
  DSFID: 0x00
  AFI: 0x00
  System info data: 00-0F-FC-F1-6F-2A-01-5C-16-E0-00-00-41-03-17  ◄ EM4237, 66 blocks x 4 bytes
  Memory: 66 blocks x 4 bytes = 264 bytes (2112 bits)
--- Full memory dump (66 blocks) ---
  Block 000: 00-A9-01-0C  |....|                                 ◄ HEADER — Brick ID 0xA9=169 (Darth Vader minifig, set 75421), v1, type 0x0C
  Block 001: 01-2A-72-06  |.*r.|                                 ◄ PAYLOAD START — 01=payload version, then encrypted
  Block 002: 94-F4-E5-26  |...&|                                 ◄ Encrypted payload
  Block 003: 64-D6-CA-C9  |d...|                                 ◄ Encrypted payload
  Block 004: 21-D9-96-98  |!...|                                 ◄ Encrypted payload
  Block 005: 19-C2-F2-53  |...S|                                 ◄ Encrypted payload
  Block 006: 7B-9A-87-CB  |{...|                                 ◄ Encrypted payload
  Block 007: D0-48-9F-30  |.H.0|                                 ◄ Encrypted payload
  Block 008: 60-2F-81-8A  |`/..|                                 ◄ Encrypted payload
  Block 009: 63-DA-E3-9F  |c...|                                 ◄ Encrypted payload
  Block 010: 71-4F-6A-7E  |qOj~|                                 ◄ Encrypted payload
  Block 011: 77-E4-33-2E  |w.3.|                                 ◄ Encrypted payload
  Block 012: 62-09-F8-DE  |b...|                                 ◄ Encrypted payload
  Block 013: 49-89-1D-D7  |I...|                                 ◄ Encrypted payload
  Block 014: 2C-57-29-4D  |,W)M|                                 ◄ Encrypted payload
  Block 015: BA-E3-E9-A0  |....|                                 ◄ Encrypted payload
  Block 016: 88-5F-47-57  |._GW|                                 ◄ Encrypted payload
  Block 017: 30-80-09-64  |0..d|                                 ◄ Encrypted payload
  Block 018: 0D-C5-9A-51  |...Q|                                 ◄ Encrypted payload
  Block 019: 88-F9-DD-30  |...0|                                 ◄ Encrypted payload
  Block 020: 88-A9-E6-03  |....|                                 ◄ Encrypted payload
  Block 021: 99-A3-EC-E7  |....|                                 ◄ Encrypted payload
  Block 022: 46-87-3A-B6  |F.:.|                                 ◄ Encrypted payload
  Block 023: EE-87-53-73  |..Ss|                                 ◄ Encrypted payload
  Block 024: FE-22-30-F7  |."0.|                                 ◄ Encrypted payload
  Block 025: 6C-DA-7A-90  |l.z.|                                 ◄ Encrypted payload
  Block 026: 54-AE-2A-2F  |T.*/|                                 ◄ Encrypted payload
  Block 027: 11-55-59-15  |.UY.|                                 ◄ Encrypted payload
  Block 028: 2E-4B-A3-69  |.K.i|                                 ◄ Encrypted payload
  Block 029: E3-01-25-55  |..%U|                                 ◄ Encrypted payload
  Block 030: 21-4F-DB-4F  |!O.O|                                 ◄ Encrypted payload
  Block 031: A5-F0-B7-95  |....|                                 ◄ Encrypted payload
  Block 032: 90-B8-88-B6  |....|                                 ◄ Encrypted payload
  Block 033: 08-57-3C-D2  |.W<.|                                 ◄ Encrypted payload
  Block 034: 6E-29-A8-1A  |n)..|                                 ◄ Encrypted payload
  Block 035: E2-E3-5E-A0  |..^.|                                 ◄ Encrypted payload
  Block 036: 30-11-7D-F4  |0.}.|                                 ◄ Encrypted payload
  Block 037: 48-80-E8-03  |H...|                                 ◄ Encrypted payload
  Block 038: 53-13-DF-2D  |S..-|                                 ◄ Encrypted payload
  Block 039: 46-79-2F-C3  |Fy/.|                                 ◄ Encrypted payload
  Block 040: 76-24-8C-DE  |v$..|                                 ◄ Encrypted payload
  Block 041: 9A-65-84-C3  |.e..|                                 ◄ Encrypted payload
  Block 042: 3F-00-00-00  |?...|                                 ◄ PAYLOAD END — 1 data byte + 3 zero pad (169 total ✓)
  Block 043: 00-00-00-00  |....|                                 ◄ Empty — zero-padded, locked
  Block 044: 00-00-00-00  |....|                                 ◄ ┆
  Block 045: 00-00-00-00  |....|                                 ◄ ┆
  Block 046: 00-00-00-00  |....|                                 ◄ ┆
  Block 047: 00-00-00-00  |....|                                 ◄ ┆
  Block 048: 00-00-00-00  |....|                                 ◄ ┆
  Block 049: 00-00-00-00  |....|                                 ◄ ┆
  Block 050: 00-00-00-00  |....|                                 ◄ ┆
  Block 051: 00-00-00-00  |....|                                 ◄ ┆
  Block 052: 00-00-00-00  |....|                                 ◄ ┆
  Block 053: 00-00-00-00  |....|                                 ◄ ┆
  Block 054: 00-00-00-00  |....|                                 ◄ ┆
  Block 055: 00-00-00-00  |....|                                 ◄ ┆
  Block 056: 00-00-00-00  |....|                                 ◄ ┆
  Block 057: 00-00-00-00  |....|                                 ◄ ┆
  Block 058: 00-00-00-00  |....|                                 ◄ ┆
  Block 059: 00-00-00-00  |....|                                 ◄ ┆
  Block 060: 00-00-00-00  |....|                                 ◄ ┆
  Block 061: 00-00-00-00  |....|                                 ◄ ┆
  Block 062: 00-00-00-00  |....|                                 ◄ ┆
  Block 063: 00-00-00-00  |....|                                 ◄ Empty end — blocks 0-63 LOCKED
  Block 064: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
  Block 065: 00-00-00-00  |....|                                 ◄ UNLOCKED — writable, reserved for play write-back
--- End of memory dump ---
```
