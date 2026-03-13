// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using nanoFramework.Device.Bluetooth;

namespace LegoSmartBrick.Ble
{
    /// <summary>
    /// Implements the WDX Device Configuration register map.
    /// Handles GET and SET operations for DC register IDs, mirroring
    /// the Packetcraft Cordio wdxs_dc.c behaviour.
    /// Phase 3 adds unauthenticated status registers: volume (R/W),
    /// device name, MAC address, battery level, and firmware version.
    /// </summary>
    public static class WdxRegisters
    {
        // ---------------------------------------------------------------
        //  External callbacks
        // ---------------------------------------------------------------

        /// <summary>
        /// Delegate invoked when the volume register is written.
        /// The parameter is the new volume (0–15, LEGO protocol range).
        /// Subscribe from the main project to forward volume changes to hardware.
        /// </summary>
        public delegate void VolumeChangedHandler(byte newVolume);

        /// <summary>
        /// Event raised after a successful DC SET Volume operation.
        /// </summary>
        public static event VolumeChangedHandler VolumeChanged;

        /// <summary>
        /// Delegate invoked when the hub local name register (0x80) is written.
        /// </summary>
        public delegate void NameChangedHandler(string newName);

        /// <summary>
        /// Event raised after a successful DC SET HubLocalName operation.
        /// </summary>
        public static event NameChangedHandler NameChanged;

        // ---------------------------------------------------------------
        //  Mutable register values
        // ---------------------------------------------------------------

        /// <summary>Current battery level (0–100).</summary>
        public static byte BatteryLevel { get; set; } = 100;

        /// <summary>Current volume (0–15, LEGO protocol range).</summary>
        public static byte Volume { get; set; } = 10;

        /// <summary>Current volume percentage (0–100) as last written by the app.</summary>
        public static byte VolumePct { get; set; } = 100;

        /// <summary>
        /// Hub local name (writable via register 0x80).
        /// Initialised to <see cref="BleConstants.DeviceName"/>.
        /// </summary>
        public static string HubLocalName { get; set; } = BleConstants.DeviceName;

        /// <summary>
        /// BLE MAC address (6 bytes, big-endian / network byte order).
        /// Derived from the ESP32 base MAC (WiFi MAC + 2 for BLE).
        /// </summary>
        public static byte[] MacAddress { get; set; } = new byte[] { 0x40, 0x91, 0x51, 0x8B, 0xA9, 0xD2 };

        // ---------------------------------------------------------------
        //  Ownership state
        // ---------------------------------------------------------------

        /// <summary>Whether the connected client has claimed ownership.</summary>
        public static bool OwnershipClaimed { get; set; } = false;

        /// <summary>Travel mode state. 0 = off, 1 = on.</summary>
        public static byte TravelMode { get; set; } = 0;

        /// <summary>
        /// Last generated 16-byte nonce for signed command / ownership proof.
        /// </summary>
        private static byte[] _commandNonce = null;

        // ---------------------------------------------------------------
        //  Authentication state (Phase 4)
        // ---------------------------------------------------------------

        /// <summary>
        /// When true, any AU reply is accepted and the requested auth level
        /// is granted — developer / debug mode. When false, all authentication
        /// attempts are rejected with <see cref="BleConstants.AuErrAuthFailed"/>.
        /// Set from <c>Program.BypassEcdsaAuth</c>.
        /// </summary>
        public static bool BypassEcdsaAuth { get; set; } = false;

        /// <summary>
        /// Current authentication level granted to the connected client.
        /// Defaults to <see cref="BleConstants.AuLvlNone"/>.
        /// Reset on disconnect (if connection tracking is added later).
        /// </summary>
        public static byte AuthLevel { get; set; } = BleConstants.AuLvlNone;

        /// <summary>
        /// The 16-byte challenge nonce generated during the current
        /// authentication exchange. Null when no exchange is active.
        /// </summary>
        public static byte[] PendingChallenge { get; set; } = null;

        /// <summary>
        /// The auth level that was requested in the most recent AU_OP_START.
        /// </summary>
        public static byte PendingAuthLevel { get; set; } = BleConstants.AuLvlNone;

        // ---------------------------------------------------------------
        //  DC GET handler
        // ---------------------------------------------------------------

        /// <summary>
        /// Processes a DC GET request and writes the response into the supplied
        /// <see cref="DataWriter"/>. The caller is responsible for sending the
        /// response via notification.
        /// </summary>
        /// <param name="registerId">The register ID being queried.</param>
        /// <param name="writer">DataWriter to receive the response payload
        /// (OP_UPDATE + id + value).</param>
        /// <returns>True if the register ID was handled; false if unknown.</returns>
        public static bool HandleGet(byte registerId, DataWriter writer)
        {
            // Response header: OP_UPDATE + register ID
            writer.WriteByte(BleConstants.DcOpUpdate);
            writer.WriteByte(registerId);

            switch (registerId)
            {
                case BleConstants.DcIdBatteryLevel:
                    writer.WriteByte(BatteryLevel);
                    Debug.WriteLine($"  DC GET BatteryLevel → {BatteryLevel}");
                    return true;

                case BleConstants.DcIdModelNumber:
                    WriteFixedString(writer, BleConstants.DeviceModel, BleConstants.DcLenDeviceModel);
                    Debug.WriteLine($"  DC GET ModelNumber → \"{BleConstants.DeviceModel}\"");
                    return true;

                case BleConstants.DcIdFirmwareRev:
                    WriteNullTermString(writer, BleConstants.FirmwareVersion);
                    Debug.WriteLine($"  DC GET FirmwareRev → \"{BleConstants.FirmwareVersion}\"");
                    return true;

                case BleConstants.DcIdConnSecLevel:
                    // Report security level 0 (no security)
                    writer.WriteByte(0x00);
                    Debug.WriteLine("  DC GET ConnSecLevel → 0");
                    return true;

                case BleConstants.DcIdAttMtu:
                    // Report default ATT MTU (23 bytes)
                    writer.WriteUInt16(23);
                    Debug.WriteLine("  DC GET AttMtu → 23");
                    return true;

                case BleConstants.DcIdConnParam:
                    // Connection parameters matching the real LEGO Smart Brick.
                    // Observed unsolicited UPDATE: 0030000000c003
                    //   connInterval (2B): 0x0030 = 48  (60 ms)
                    //   slaveLatency (2B): 0x0000 = 0
                    //   supTimeout   (2B): 0x00C0 = 192 (1.92 s)
                    //   dataFormat   (1B): 0x03
                    writer.WriteUInt16(0x0030);
                    writer.WriteUInt16(0x0000);
                    writer.WriteUInt16(0x00C0);
                    writer.WriteByte(0x03);
                    Debug.WriteLine("  DC GET ConnParam → interval=0x30, latency=0, timeout=0xC0, format=3");
                    return true;

                case BleConstants.DcIdPhy:
                    // Report 1M PHY for TX, RX, and options
                    writer.WriteByte(0x01); // TX PHY: 1M
                    writer.WriteByte(0x01); // RX PHY: 1M
                    writer.WriteByte(0x00); // options
                    Debug.WriteLine("  DC GET Phy → 1M/1M");
                    return true;

                // --- Phase 3: unauthenticated status registers ----------

                case BleConstants.DcIdVolume:
                    writer.WriteByte(Volume);
                    Debug.WriteLine($"  DC GET Volume → {Volume}");
                    return true;

                case BleConstants.DcIdDeviceName:
                    WriteFixedString(writer, HubLocalName, BleConstants.DcLenDeviceName);
                    Debug.WriteLine($"  DC GET DeviceName → \"{HubLocalName}\"");
                    return true;

                case BleConstants.DcIdMacAddress:
                    writer.WriteBytes(MacAddress);
                    Debug.WriteLine($"  DC GET MacAddress → {FormatMac(MacAddress)}");
                    return true;

                // --- LEGO vendor-specific registers (0x80+) -----

                case BleConstants.DcIdHubLocalName:
                    // Null-terminated UTF-8 device name
                    WriteNullTermString(writer, HubLocalName);
                    Debug.WriteLine($"  DC GET HubLocalName → \"{HubLocalName}\"");
                    return true;

                case BleConstants.DcIdUserVolume:
                    writer.WriteByte(VolumePct);
                    Debug.WriteLine($"  DC GET UserVolume → {VolumePct}%");
                    return true;

                case BleConstants.DcIdPrimaryMacAddress:
                    writer.WriteBytes(MacAddress);
                    Debug.WriteLine($"  DC GET PrimaryMacAddress → {FormatMac(MacAddress)}");
                    return true;

                case BleConstants.DcIdChargingState:
                    writer.WriteByte(0x00); // 0x00 = not charging
                    Debug.WriteLine("  DC GET ChargingState → 0x00");
                    return true;

                case BleConstants.DcIdUpdateState:
                    writer.WriteByte(0x00);
                    Debug.WriteLine("  DC GET UpdateState → 0x00");
                    return true;

                case BleConstants.DcIdSignedCommandNonce:
                    // Generate a fresh 16-byte random nonce each time the app reads
                    _commandNonce = new byte[16];
                    new System.Random().NextBytes(_commandNonce);
                    writer.WriteBytes(_commandNonce);
                    Debug.WriteLine($"  DC GET SignedCommandNonce → [{FormatHex(_commandNonce, 0, 16)}]");
                    return true;

                case BleConstants.DcIdUpgradeState:
                    writer.WriteByte(0x00); // 0x00 = Ready
                    Debug.WriteLine("  DC GET UpgradeState → 0x00 (Ready)");
                    return true;

                case BleConstants.DcIdHardwareRev:
                    WriteNullTermString(writer, BleConstants.HardwareVersion);
                    Debug.WriteLine($"  DC GET HardwareRev → \"{BleConstants.HardwareVersion}\"");
                    return true;

                case BleConstants.DcIdCurrentWriteOffset:
                    writer.WriteUInt32(0x00000000);
                    Debug.WriteLine("  DC GET CurrentWriteOffset → 0");
                    return true;

                case BleConstants.DcIdPipelineStage:
                    writer.WriteByte(0x00); // 0x00 = Idle
                    Debug.WriteLine("  DC GET PipelineStage → 0x00 (Idle)");
                    return true;

                case BleConstants.DcIdManufacturerName:
                    WriteNullTermString(writer, BleConstants.ManufacturerName);
                    Debug.WriteLine($"  DC GET ManufacturerName → \"{BleConstants.ManufacturerName}\"");
                    return true;

                case BleConstants.DcIdBatteryType:
                    writer.WriteByte(0x01); // 0x01 = Rechargeable
                    Debug.WriteLine("  DC GET BatteryType → 0x01 (Rechargeable)");
                    return true;

                case BleConstants.DcIdChargingVoltagePresent:
                    writer.WriteByte(0x00); // 0x00 = no charging voltage detected
                    Debug.WriteLine("  DC GET ChargingVoltagePresent → 0x00");
                    return true;

                case BleConstants.DcIdTravelMode:
                    writer.WriteByte(TravelMode);
                    Debug.WriteLine($"  DC GET TravelMode → 0x{TravelMode:X2}");
                    return true;

                default:
                    Debug.WriteLine($"  DC GET unknown register 0x{registerId:X2}");
                    return false;
            }
        }

        // ---------------------------------------------------------------
        //  DC SET handler
        // ---------------------------------------------------------------

        /// <summary>
        /// Processes a DC SET request. The data bytes (after op + id) are
        /// supplied in <paramref name="data"/> starting at <paramref name="offset"/>.
        /// </summary>
        /// <param name="registerId">The register ID being set.</param>
        /// <param name="data">Raw write payload.</param>
        /// <param name="offset">Offset of the first value byte in <paramref name="data"/>.</param>
        /// <param name="length">Number of value bytes after the header.</param>
        /// <returns>True if successfully handled.</returns>
        public static bool HandleSet(byte registerId, byte[] data, int offset, int length)
        {
            switch (registerId)
            {
                case BleConstants.DcIdDisconnectReq:
                    Debug.WriteLine("  DC SET DisconnectReq — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdDeleteBonds:
                    Debug.WriteLine("  DC SET DeleteBonds — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdEnterDiagnostics:
                    Debug.WriteLine("  DC SET EnterDiagnostics — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdDisconnectAndReset:
                    Debug.WriteLine("  DC SET DisconnectAndReset — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdConnUpdateReq:
                    Debug.WriteLine("  DC SET ConnUpdateReq — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdSecurityReq:
                    Debug.WriteLine("  DC SET SecurityReq — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdServiceChanged:
                    Debug.WriteLine("  DC SET ServiceChanged — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdPhyUpdateReq:
                    Debug.WriteLine("  DC SET PhyUpdateReq — acknowledged (no-op)");
                    return true;

                // --- Phase 3: volume write handler ----------------------

                case BleConstants.DcIdVolume:
                    if (length >= 1)
                    {
                        byte newVolume = data[offset];
                        if (newVolume > BleConstants.VolumeMax)
                            newVolume = BleConstants.VolumeMax;

                        Volume = newVolume;
                        Debug.WriteLine($"  DC SET Volume → {Volume}");

                        // Notify subscribers (e.g. MP3 player in main project)
                        VolumeChanged?.Invoke(newVolume);
                    }
                    return true;

                case BleConstants.DcIdUXSignal:
                    {
                        string hex = FormatHex(data, offset, length);
                        Debug.WriteLine($"  DC SET UXSignal (keepalive) → [{hex}]");
                    }
                    return true;

                case BleConstants.DcIdUserVolume:
                    {
                        // The LEGO app writes volume percentage (0–100) via register 0x81
                        if (length >= 1)
                        {
                            byte pct = data[offset];
                            VolumePct = pct;
                            // Map 0–100 percentage to 0–15 LEGO range
                            byte mapped = (byte)(pct * BleConstants.VolumeMax / 100);
                            if (mapped > BleConstants.VolumeMax) mapped = BleConstants.VolumeMax;
                            Volume = mapped;
                            Debug.WriteLine($"  DC SET UserVolume (0x81) → {pct}% (mapped {Volume}/15)");
                            VolumeChanged?.Invoke(Volume);
                            BrickSettings.SaveVolume(pct);
                        }
                    }
                    return true;

                case BleConstants.DcIdHubLocalName:
                    {
                        // Extract the name from the written bytes, trimming any null terminator
                        int nameLen = length;
                        // Strip trailing null bytes
                        while (nameLen > 0 && data[offset + nameLen - 1] == 0x00)
                            nameLen--;

                        if (nameLen > 0)
                        {
                            byte[] nameBuf = new byte[nameLen];
                            Array.Copy(data, offset, nameBuf, 0, nameLen);
                            string newName = System.Text.Encoding.UTF8.GetString(nameBuf, 0, nameLen);
                            HubLocalName = newName;
                            Debug.WriteLine($"  DC SET HubLocalName → \"{newName}\"");
                            NameChanged?.Invoke(newName);
                            BrickSettings.SaveName(newName);
                        }
                        else
                        {
                            Debug.WriteLine("  DC SET HubLocalName — empty name, ignored");
                        }
                    }
                    return true;

                case BleConstants.DcIdClearConfig:
                    Debug.WriteLine("  DC SET ClearConfig — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdSignedCommand:
                    {
                        Debug.WriteLine($"  DC SET SignedCommand ({length} bytes) — acknowledged (no backend key)");
                    }
                    return true;

                case BleConstants.DcIdFactoryReset:
                    Debug.WriteLine("  DC SET FactoryReset — acknowledged (no-op)");
                    return true;

                case BleConstants.DcIdTravelMode:
                    if (length >= 1)
                    {
                        TravelMode = data[offset];
                        Debug.WriteLine($"  DC SET TravelMode → 0x{TravelMode:X2}");
                    }
                    return true;

                case BleConstants.DcIdOwnershipProof:
                    {
                        OwnershipClaimed = true;
                        Debug.WriteLine($"  DC SET OwnershipProof ({length} bytes) (accepted)");
                    }
                    return true;

                default:
                    {
                        string hex = FormatHex(data, offset, length);
                        Debug.WriteLine($"  DC SET unknown register 0x{registerId:X2} data=[{hex}]");
                    }
                    return false;
            }
        }

        // ---------------------------------------------------------------
        //  Helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Writes a string into the DataWriter, padded or truncated to
        /// <paramref name="fixedLength"/> bytes (zero-padded).
        /// </summary>
        private static void WriteFixedString(DataWriter writer, string value, int fixedLength)
        {
            byte[] buf = new byte[fixedLength];
            byte[] src = System.Text.Encoding.UTF8.GetBytes(value);
            int copyLen = src.Length < fixedLength ? src.Length : fixedLength;
            Array.Copy(src, 0, buf, 0, copyLen);
            writer.WriteBytes(buf);
        }

        /// <summary>
        /// Writes a string as UTF-8 followed by a null terminator.
        /// node-smartplay parses name/model/firmware as: read until first 0x00 byte.
        /// </summary>
        private static void WriteNullTermString(DataWriter writer, string value)
        {
            byte[] src = System.Text.Encoding.UTF8.GetBytes(value);
            writer.WriteBytes(src);
            writer.WriteByte(0x00);
        }

        /// <summary>
        /// Formats a byte range as a space-separated hex string for logging.
        /// </summary>
        private static string FormatHex(byte[] data, int offset, int length)
        {
            string hex = "";
            int end = offset + length;
            if (end > data.Length) end = data.Length;
            for (int i = offset; i < end; i++)
            {
                if (hex.Length > 0) hex += " ";
                hex += $"{data[i]:X2}";
            }
            return hex;
        }

        /// <summary>
        /// Formats a 6-byte MAC address as a colon-separated hex string.
        /// </summary>
        private static string FormatMac(byte[] mac)
        {
            if (mac == null || mac.Length < 6)
                return "(unknown)";

            return $"{mac[0]:X2}:{mac[1]:X2}:{mac[2]:X2}:{mac[3]:X2}:{mac[4]:X2}:{mac[5]:X2}";
        }
    }
}
