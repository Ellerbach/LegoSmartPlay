// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LegoSmartBrick.Ble
{
    /// <summary>
    /// BLE constants for the LEGO Smart Play brick protocol.
    /// UUIDs, opcodes, and register IDs for the Packetcraft Cordio WDX profile.
    /// </summary>
    public static class BleConstants
    {
        // ---------------------------------------------------------------
        //  Device identity
        // ---------------------------------------------------------------

        /// <summary>
        /// BLE device name — matches the official SMART Brick advertisement.
        /// </summary>
        public const string DeviceName = "Smart Brick";

        /// <summary>
        /// Simulated firmware version string — matches the latest known official version.
        /// </summary>
        public const string FirmwareVersion = "2.29.2";

        /// <summary>
        /// Simulated device model string.
        /// </summary>
        public const string DeviceModel = "Smart Brick";

        /// <summary>
        /// Simulated hardware revision string.
        /// </summary>
        public const string HardwareVersion = "0.72.33";

        // ---------------------------------------------------------------
        //  WDX Service UUID (16-bit short code)
        // ---------------------------------------------------------------

        /// <summary>
        /// WDX (Wireless Data Exchange) advertised service UUID — 16-bit short code 0xFEF6.
        /// This is included in the BLE advertisement so scanners can find the device.
        /// </summary>
        public static readonly Guid WdxAdvertisedUuid = new Guid("0000FEF6-0000-1000-8000-00805F9B34FB");

        /// <summary>
        /// WDX base service UUID (part = 0x0001).
        /// The GATT service containing the DC, FTC, FTD, and AU characteristics.
        /// The real LEGO Smart Brick exposes this as a separate 128-bit service.
        /// </summary>
        public static readonly Guid WdxServiceUuid = new Guid("005f0001-2ff2-4ed5-b045-4c7463617865");

        // ---------------------------------------------------------------
        //  LEGO Bluetooth company ID and advertising data
        // ---------------------------------------------------------------

        /// <summary>LEGO Systems A/S Bluetooth SIG company ID.</summary>
        public const ushort LegoCompanyId = 0x0397;

        /// <summary>Manufacturer name for Device Information Service.</summary>
        public const string ManufacturerName = "LEGO";

        // ---------------------------------------------------------------
        //  Device Information Service (0x180A) — standard BLE SIG
        // ---------------------------------------------------------------

        /// <summary>Device Information Service UUID (16-bit: 0x180A).</summary>
        public static readonly Guid DeviceInfoServiceUuid = new Guid("0000180A-0000-1000-8000-00805F9B34FB");

        /// <summary>Manufacturer Name String characteristic (0x2A29).</summary>
        public static readonly Guid ManufacturerNameUuid = new Guid("00002A29-0000-1000-8000-00805F9B34FB");

        /// <summary>Model Number String characteristic (0x2A24).</summary>
        public static readonly Guid ModelNumberUuid = new Guid("00002A24-0000-1000-8000-00805F9B34FB");

        /// <summary>Firmware Revision String characteristic (0x2A26).</summary>
        public static readonly Guid FirmwareRevisionUuid = new Guid("00002A26-0000-1000-8000-00805F9B34FB");

        /// <summary>Software Revision String characteristic (0x2A28).</summary>
        public static readonly Guid SoftwareRevisionUuid = new Guid("00002A28-0000-1000-8000-00805F9B34FB");

        // ---------------------------------------------------------------
        //  WDX Characteristic UUIDs (128-bit)
        //  Base: 005fXXXX-2ff2-4ed5-b045-4C7463617865
        //  Source: Packetcraft Cordio wdx_defs.h
        // ---------------------------------------------------------------

        /// <summary>Device Configuration characteristic UUID (part = 0x0002).</summary>
        public static readonly Guid DcUuid = new Guid("005f0002-2ff2-4ed5-b045-4c7463617865");

        /// <summary>File Transfer Control characteristic UUID (part = 0x0003).</summary>
        public static readonly Guid FtcUuid = new Guid("005f0003-2ff2-4ed5-b045-4c7463617865");

        /// <summary>File Transfer Data characteristic UUID (part = 0x0004).</summary>
        public static readonly Guid FtdUuid = new Guid("005f0004-2ff2-4ed5-b045-4c7463617865");

        /// <summary>Authentication characteristic UUID (part = 0x0005).</summary>
        public static readonly Guid AuUuid = new Guid("005f0005-2ff2-4ed5-b045-4c7463617865");

        // ---------------------------------------------------------------
        //  Secondary LEGO custom service (base: 3ff2)
        //  node-smartplay subscribes to this for bidirectional comms.
        // ---------------------------------------------------------------

        /// <summary>
        /// Secondary LEGO custom service UUID (part = 0x0001, base 3ff2).
        /// </summary>
        public static readonly Guid LegoSecondaryServiceUuid = new Guid("005f0001-3ff2-4ed5-b045-4c7463617865");

        /// <summary>
        /// Bidirectional characteristic on the secondary service (part = 0x000a, base 3ff2).
        /// Supports WriteWithoutResponse + Notify for command/data exchange.
        /// </summary>
        public static readonly Guid LegoBidirectionalUuid = new Guid("005f000a-3ff2-4ed5-b045-4c7463617865");

        // ---------------------------------------------------------------
        //  DC (Device Configuration) opcodes
        // ---------------------------------------------------------------

        /// <summary>Get a parameter value.</summary>
        public const byte DcOpGet = 0x01;

        /// <summary>Set a parameter value.</summary>
        public const byte DcOpSet = 0x02;

        /// <summary>Send an update of a parameter value (notification response).</summary>
        public const byte DcOpUpdate = 0x03;

        /// <summary>DC message header length (op + id).</summary>
        public const int DcHdrLen = 2;

        // ---------------------------------------------------------------
        //  DC register IDs (GET-able)
        // ---------------------------------------------------------------

        /// <summary>Connection parameter update request (SET only).</summary>
        public const byte DcIdConnUpdateReq = 0x01;

        /// <summary>Current connection parameters (GET).</summary>
        public const byte DcIdConnParam = 0x02;

        /// <summary>Disconnect request (SET only).</summary>
        public const byte DcIdDisconnectReq = 0x03;

        /// <summary>Connection security level (GET).</summary>
        public const byte DcIdConnSecLevel = 0x04;

        /// <summary>Security request (SET only).</summary>
        public const byte DcIdSecurityReq = 0x05;

        /// <summary>Service changed (SET only).</summary>
        public const byte DcIdServiceChanged = 0x06;

        /// <summary>Delete bonds (SET only).</summary>
        public const byte DcIdDeleteBonds = 0x07;

        /// <summary>Current ATT MTU (GET).</summary>
        public const byte DcIdAttMtu = 0x08;

        /// <summary>PHY update request (SET only).</summary>
        public const byte DcIdPhyUpdateReq = 0x09;

        /// <summary>Current PHY (GET).</summary>
        public const byte DcIdPhy = 0x0A;

        /// <summary>Battery level (GET).</summary>
        public const byte DcIdBatteryLevel = 0x20;

        /// <summary>Device model (GET).</summary>
        public const byte DcIdModelNumber = 0x21;

        /// <summary>Firmware revision (GET).</summary>
        public const byte DcIdFirmwareRev = 0x22;

        /// <summary>Enter diagnostic mode (SET only).</summary>
        public const byte DcIdEnterDiagnostics = 0x23;

        /// <summary>Diagnostic complete.</summary>
        public const byte DcIdDiagnosticsComplete = 0x24;

        /// <summary>Disconnect and reset (SET only).</summary>
        public const byte DcIdDisconnectAndReset = 0x25;

        /// <summary>Clear / reset device configuration (SET only, no data).</summary>
        public const byte DcIdClearConfig = 0x26;

        // ---------------------------------------------------------------
        //  Custom LEGO-specific DC register IDs (0x30+)
        //  These are not part of the Cordio WDX standard but are used
        //  by the LEGO SMART Brick for app-facing status/control.
        // ---------------------------------------------------------------

        /// <summary>Volume level 0–15 (GET / SET).</summary>
        public const byte DcIdVolume = 0x30;

        /// <summary>BLE device name string (GET).</summary>
        public const byte DcIdDeviceName = 0x31;

        /// <summary>BLE MAC address — 6 bytes (GET).</summary>
        public const byte DcIdMacAddress = 0x32;

        // ---------------------------------------------------------------
        //  LEGO vendor-specific DC register IDs (0x80+)
        //  Corrected names from node-smartplay reverse engineering.
        // ---------------------------------------------------------------

        /// <summary>
        /// Hub local name (GET/SET). Null-terminated UTF-8 device name
        /// (max 12 bytes). The LEGO app reads this to display the device name.
        /// </summary>
        public const byte DcIdHubLocalName = 0x80;

        /// <summary>
        /// User volume (GET/SET). Single byte 0–100.
        /// The LEGO app reads/writes volume percentage via this register.
        /// </summary>
        public const byte DcIdUserVolume = 0x81;

        /// <summary>
        /// Current write offset (GET). 4 bytes (uint32).
        /// Used during WDX file transfers to track progress. Returns 0 when idle.
        /// </summary>
        public const byte DcIdCurrentWriteOffset = 0x82;

        /// <summary>
        /// Hardware revision (GET). Null-terminated string.
        /// Exposes the hardware version (e.g. "0.72.33").
        /// </summary>
        public const byte DcIdHardwareRev = 0x83;

        /// <summary>
        /// Primary MAC address (GET). 6 bytes.
        /// The LEGO app reads the BLE MAC address from this register.
        /// </summary>
        public const byte DcIdPrimaryMacAddress = 0x84;

        /// <summary>
        /// Upgrade state (GET). Single byte: 0 = Ready, 1 = InProgress, 2 = LowBattery.
        /// The app checks this before writing volume.
        /// </summary>
        public const byte DcIdUpgradeState = 0x85;

        /// <summary>
        /// Signed command nonce (GET). Triggers BLE pairing — avoid reading
        /// without auth backend. Part of ECDSA P-256 authentication flow.
        /// </summary>
        public const byte DcIdSignedCommandNonce = 0x86;

        /// <summary>
        /// Signed command (SET). Variable-length signed command blob.
        /// Requires LEGO backend private key — stub accepts and logs.
        /// </summary>
        public const byte DcIdSignedCommand = 0x87;

        /// <summary>
        /// Update state (GET). Single byte.
        /// </summary>
        public const byte DcIdUpdateState = 0x88;

        /// <summary>
        /// Pipeline stage (GET). Single byte OTA progress indicator.
        /// 0x00 = Idle.
        /// </summary>
        public const byte DcIdPipelineStage = 0x89;

        /// <summary>
        /// Manufacturer name (GET). Null-terminated UTF-8 string.
        /// Returns "LEGO" — supplements ModelNumber (0x21) since DIS is removed.
        /// </summary>
        public const byte DcIdManufacturerName = 0x8A;

        /// <summary>
        /// UX signal / keepalive (SET). The app writes [0xEA, 0x00] periodically
        /// as a keepalive signal during the polling loop.
        /// </summary>
        public const byte DcIdUXSignal = 0x90;

        /// <summary>
        /// Ownership proof (SET). The app writes [0x01, 0x00, 0x00] during
        /// connection setup.
        /// </summary>
        public const byte DcIdOwnershipProof = 0x91;

        /// <summary>
        /// Battery type (GET). Single byte.
        /// 0x00 = Normal, 0x01 = Rechargeable.
        /// </summary>
        public const byte DcIdBatteryType = 0x92;

        /// <summary>
        /// Charging state (GET). Single byte, polled continuously.
        /// 0x00 = not charging.
        /// </summary>
        public const byte DcIdChargingState = 0x93;

        /// <summary>
        /// Battery charging voltage present (GET). Single byte.
        /// 0x00 = no charging voltage, 0x01 = charging voltage detected.
        /// </summary>
        public const byte DcIdChargingVoltagePresent = 0x94;

        /// <summary>
        /// Factory reset (SET only). Writing triggers a factory reset (no-op stub).
        /// </summary>
        public const byte DcIdFactoryReset = 0x95;

        /// <summary>
        /// Travel mode (GET/SET). Single byte.
        /// 0x00 = off, 0x01 = on (low-power shipping mode).
        /// </summary>
        public const byte DcIdTravelMode = 0x96;

        // ---------------------------------------------------------------
        //  DC register lengths
        // ---------------------------------------------------------------

        public const int DcLenSecLevel = 1;
        public const int DcLenAttMtu = 2;
        public const int DcLenBatteryLevel = 1;
        public const int DcLenConnParam = 7;
        public const int DcLenPhy = 3;
        public const int DcLenDeviceModel = 18;
        public const int DcLenFirmwareRev = 16;
        public const int DcLenVolume = 1;
        public const int DcLenDeviceName = 18;
        public const int DcLenMacAddress = 6;
        public const int DcLenHubLocalName = 18;
        public const int DcLenUserVolume = 1;
        public const int DcLenPrimaryMacAddress = 6;

        /// <summary>Maximum LEGO volume level.</summary>
        public const byte VolumeMax = 15;

        // ---------------------------------------------------------------
        //  FTC (File Transfer Control) opcodes
        // ---------------------------------------------------------------

        public const byte FtcOpNone = 0x00;
        public const byte FtcOpGetReq = 0x01;
        public const byte FtcOpGetRsp = 0x02;
        public const byte FtcOpPutReq = 0x03;
        public const byte FtcOpPutRsp = 0x04;
        public const byte FtcOpEraseReq = 0x05;
        public const byte FtcOpEraseRsp = 0x06;
        public const byte FtcOpVerifyReq = 0x07;
        public const byte FtcOpVerifyRsp = 0x08;
        public const byte FtcOpAbort = 0x09;
        public const byte FtcOpEof = 0x0A;

        // ---------------------------------------------------------------
        //  AU (Authentication) opcodes
        // ---------------------------------------------------------------

        /// <summary>Authentication start — client sends requested auth level.</summary>
        public const byte AuOpStart = 0x01;

        /// <summary>Authentication challenge — server sends 16-byte random nonce.</summary>
        public const byte AuOpChallenge = 0x02;

        /// <summary>Authentication reply — client sends 8-byte hash/signature.</summary>
        public const byte AuOpReply = 0x03;

        // ---------------------------------------------------------------
        //  AU message / parameter lengths
        // ---------------------------------------------------------------

        /// <summary>AU message header length (op byte).</summary>
        public const int AuHdrLen = 1;

        /// <summary>Start parameter length — requested auth level (2 bytes).</summary>
        public const int AuParamLenStart = 2;

        /// <summary>Challenge parameter length — random nonce (16 bytes).</summary>
        public const int AuParamLenChallenge = 16;

        /// <summary>Reply parameter length — hash (8 bytes).</summary>
        public const int AuParamLenReply = 8;

        // ---------------------------------------------------------------
        //  AU authentication levels
        // ---------------------------------------------------------------

        /// <summary>No authentication.</summary>
        public const byte AuLvlNone = 0x00;

        /// <summary>User-level authentication.</summary>
        public const byte AuLvlUser = 0x01;

        /// <summary>Maintenance-level authentication.</summary>
        public const byte AuLvlMaint = 0x02;

        /// <summary>Debug-level authentication.</summary>
        public const byte AuLvlDebug = 0x03;

        // ---------------------------------------------------------------
        //  AU / WDX proprietary ATT error codes
        // ---------------------------------------------------------------

        /// <summary>Application-level authentication required.</summary>
        public const byte AuErrAuthRequired = 0x80;

        /// <summary>Invalid authentication message.</summary>
        public const byte AuErrInvalidMessage = 0x81;

        /// <summary>Invalid authentication state.</summary>
        public const byte AuErrInvalidState = 0x82;

        /// <summary>Authentication failed.</summary>
        public const byte AuErrAuthFailed = 0x83;
    }
}
