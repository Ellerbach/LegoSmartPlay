// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.Advertisement;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;

namespace LegoSmartBrick.Ble
{
    /// <summary>
    /// Manages BLE advertising and the WDX GATT service, making the ESP32
    /// appear as a LEGO SMART Brick to BLE scanners and the companion app.
    ///
    /// Phase 2 — Exposes the four WDX characteristics:
    ///   DC  (Device Configuration)  — Write + Notify
    ///   FTC (File Transfer Control)  — WriteNoResponse + Notify
    ///   FTD (File Transfer Data)     — WriteNoResponse + Notify
    ///   AU  (Authentication)         — Write + Notify
    /// </summary>
    public static class BluetoothService
    {
        private static GattServiceProvider _serviceProvider;

        // Characteristic references — needed for sending notifications
        private static GattLocalCharacteristic _dcCharacteristic;
        private static GattLocalCharacteristic _ftcCharacteristic;
        private static GattLocalCharacteristic _ftdCharacteristic;
        private static GattLocalCharacteristic _auCharacteristic;

        /// <summary>
        /// Initialises the BLE stack, creates the WDX GATT service with all
        /// four characteristics, hooks up write handlers, and starts advertising
        /// as "Smart Brick" with the WDX service UUID (0xFEF6).
        /// </summary>
        /// <returns>True if BLE advertising started successfully.</returns>
        public static bool Init()
        {
            Debug.WriteLine("BLE: Initialising Bluetooth...");

            try
            {
                // --- BLE Server ---
                BluetoothLEServer server = BluetoothLEServer.Instance;
                server.DeviceName = BleConstants.DeviceName;

                Debug.WriteLine($"BLE: Device name set to \"{BleConstants.DeviceName}\"");

                // --- WDX Service (FEF6) ---
                // Create the GATT service with the FEF6 UUID so it appears in the
                // advertisement as "Wicentric, Inc. (FEF6)", matching the real brick.
                GattServiceProviderResult serviceResult = GattServiceProvider.Create(BleConstants.WdxAdvertisedUuid);

                if (serviceResult.Error != BluetoothError.Success)
                {
                    Debug.WriteLine($"BLE: Failed to create WDX service — {serviceResult.Error}");
                    return false;
                }

                _serviceProvider = serviceResult.ServiceProvider;
                GattLocalService service = _serviceProvider.Service;

                // --- DC — Device Configuration (Write + Notify) ---
                _dcCharacteristic = CreateCharacteristic(
                    service,
                    BleConstants.DcUuid,
                    "Device Configuration",
                    GattCharacteristicProperties.Write | GattCharacteristicProperties.Notify);

                if (_dcCharacteristic == null) return false;

                // --- FTC — File Transfer Control (WriteWithoutResponse + Notify) ---
                _ftcCharacteristic = CreateCharacteristic(
                    service,
                    BleConstants.FtcUuid,
                    "File Transfer Control",
                    GattCharacteristicProperties.WriteWithoutResponse | GattCharacteristicProperties.Notify);

                if (_ftcCharacteristic == null) return false;

                // --- FTD — File Transfer Data (WriteWithoutResponse + Notify) ---
                _ftdCharacteristic = CreateCharacteristic(
                    service,
                    BleConstants.FtdUuid,
                    "File Transfer Data",
                    GattCharacteristicProperties.WriteWithoutResponse | GattCharacteristicProperties.Notify);

                if (_ftdCharacteristic == null) return false;

                // --- AU — Authentication (Write + Notify) ---
                _auCharacteristic = CreateCharacteristic(
                    service,
                    BleConstants.AuUuid,
                    "Authentication",
                    GattCharacteristicProperties.Write | GattCharacteristicProperties.Notify);

                if (_auCharacteristic == null) return false;

                Debug.WriteLine("BLE: WDX service created with DC, FTC, FTD, AU characteristics.");

                // NOTE: Device Information Service (0x180A) removed — adding a
                // second GattServiceProvider with 4 characteristics exceeds the
                // ESP32 BLE heap and causes OOM / invisible advertisement.
                // The nanoFramework default DIS ("nanoFramework"/"ESP32") remains.
                // The LEGO app reads model/manufacturer/firmware from DC registers.

                // --- Start advertising (before hooking write events) ---
                Debug.WriteLine("BLE: Starting advertising...");

                // Build advertising parameters matching the real LEGO Smart Brick.
                var advParams = new GattServiceProviderAdvertisingParameters()
                {
                    IsConnectable = true,
                    IsDiscoverable = true
                };

                // LEGO advertising manufacturer data (6 bytes):
                //   [0] ButtonState      = 0x00 (not pressed)
                //   [1] SystemType+DevNo = 0x60 (Smart Brick identifier)
                //   [2] DeviceCapabilities = 0x00
                //   [3] LastNetwork       = 0x00
                //   [4] Status            = 0x00
                //   [5] Option            = 0x00
                DataWriter advData = new();
                advData.WriteByte(0x00); // ButtonState
                advData.WriteByte(0x60); // SystemTypeAndDeviceNumber
                advData.WriteByte(0x00); // DeviceCapabilities
                advData.WriteByte(0x00); // LastNetwork
                advData.WriteByte(0x00); // Status
                advData.WriteByte(0x00); // Option
                advParams.Advertisement.ManufacturerData.Add(
                    new BluetoothLEManufacturerData(BleConstants.LegoCompanyId, advData.DetachBuffer()));

                // Set the local name in the advertisement data so scanners
                // show "Smart Brick" as the model (matches the real brick).
                advParams.Advertisement.LocalName = BleConstants.DeviceName;

                _serviceProvider.StartAdvertising(advParams);

                Debug.WriteLine("BLE: Advertising as \"Smart Brick\" with WDX service UUID 0xFEF6.");

                // --- Start the BLE server ---
                server.Start();
                Debug.WriteLine("BLE: Server started.");

                // --- Hook write handlers AFTER advertising/start ---
                _dcCharacteristic.WriteRequested += OnDcWriteRequested;
                _dcCharacteristic.SubscribedClientsChanged += OnDcSubscribedClientsChanged;
                _ftcCharacteristic.WriteRequested += OnFtcWriteRequested;
                _ftdCharacteristic.WriteRequested += OnFtdWriteRequested;
                _auCharacteristic.WriteRequested += OnAuWriteRequested;

                Debug.WriteLine("BLE: Write handlers registered.");
                Debug.WriteLine("");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BLE: Init failed — {ex.Message}");
                Debug.WriteLine("");
                return false;
            }
        }

        // ---------------------------------------------------------------
        //  Characteristic factory
        // ---------------------------------------------------------------

        /// <summary>
        /// Creates a GATT characteristic on the given service and logs success or failure.
        /// </summary>
        private static GattLocalCharacteristic CreateCharacteristic(
            GattLocalService service,
            Guid uuid,
            string description,
            GattCharacteristicProperties properties)
        {
            // Provide an empty StaticValue — the working LegoBluetooth repo
            // always sets one; without it the native BLE stack may crash.
            DataWriter sw = new();
            sw.WriteByte(0);

            GattLocalCharacteristicParameters parms = new()
            {
                CharacteristicProperties = properties,
                UserDescription = description,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                StaticValue = sw.DetachBuffer()
            };

            GattLocalCharacteristicResult result = service.CreateCharacteristic(uuid, parms);

            if (result.Error != BluetoothError.Success)
            {
                Debug.WriteLine($"BLE: Failed to create {description} characteristic — {result.Error}");
                return null;
            }

            Debug.WriteLine($"BLE: Created {description} characteristic ({uuid}).");
            return result.Characteristic;
        }

        /// <summary>
        /// Creates a read-only GATT characteristic with a static string value
        /// for the Device Information Service.
        /// </summary>
        private static void CreateReadOnlyCharacteristic(GattLocalService service, Guid uuid, string description, string value)
        {
            DataWriter dw = new();
            byte[] valueBytes = System.Text.Encoding.UTF8.GetBytes(value);
            dw.WriteBytes(valueBytes);

            GattLocalCharacteristicParameters parms = new()
            {
                CharacteristicProperties = GattCharacteristicProperties.Read,
                UserDescription = description,
                StaticValue = dw.DetachBuffer()
            };

            GattLocalCharacteristicResult result = service.CreateCharacteristic(uuid, parms);
            if (result.Error != BluetoothError.Success)
            {
                Debug.WriteLine($"BLE: Failed to create DIS {description} — {result.Error}");
            }
        }

        // ---------------------------------------------------------------
        //  DC subscription handler — unsolicited notifications
        // ---------------------------------------------------------------

        /// <summary>
        /// When a client subscribes to DC notifications, send the connection
        /// parameters as an unsolicited UPDATE — matching the real LEGO Smart
        /// Brick behaviour (observed: register 0x02, data 0030000000c003).
        /// </summary>
        private static void OnDcSubscribedClientsChanged(GattLocalCharacteristic sender, object args)
        {
            if (sender.SubscribedClients.Length > 0)
            {
                Debug.WriteLine("BLE DC: Client subscribed — sending unsolicited ConnParam UPDATE");

                DataWriter writer = new();
                writer.WriteByte(BleConstants.DcOpUpdate);
                writer.WriteByte(BleConstants.DcIdConnParam);
                writer.WriteUInt16(0x0030); // connInterval = 48 (60 ms)
                writer.WriteUInt16(0x0000); // connLatency  = 0
                writer.WriteUInt16(0x00C0); // supTimeout   = 192 (1.92 s)
                writer.WriteByte(0x03);     // format

                _dcCharacteristic.NotifyValue(writer.DetachBuffer());
            }
        }

        // ---------------------------------------------------------------
        //  DC — Device Configuration write handler
        // ---------------------------------------------------------------

        /// <summary>
        /// Handles incoming DC writes. The WDX DC protocol uses a simple
        /// op + id + data PDU format:
        ///   [op: 1B] [id: 1B] [value: 0..N bytes]
        ///
        /// OP_GET → look up register, respond via notification (OP_UPDATE).
        /// OP_SET → apply the value to the register.
        /// </summary>
        private static void OnDcWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            GattWriteRequest request = args.GetRequest();
            Buffer writeBuffer = request.Value;

            if (writeBuffer.Length < BleConstants.DcHdrLen)
            {
                Debug.WriteLine("BLE DC: Write too short, ignoring.");
                request.Respond();
                return;
            }

            DataReader reader = DataReader.FromBuffer(writeBuffer);
            byte op = reader.ReadByte();
            byte id = reader.ReadByte();

            Debug.WriteLine($"BLE DC: op=0x{op:X2} id=0x{id:X2} len={writeBuffer.Length}");

            // Reclaim heap before processing to avoid OOM on constrained ESP32
            nanoFramework.Runtime.Native.GC.Run(false);

            switch (op)
            {
                case BleConstants.DcOpGet:
                    HandleDcGet(id);
                    break;

                case BleConstants.DcOpSet:
                    int valueLen = (int)writeBuffer.Length - BleConstants.DcHdrLen;
                    byte[] raw = new byte[writeBuffer.Length];
                    {
                        // Read remaining bytes from the already-positioned reader
                        byte[] valueBytes = new byte[valueLen];
                        if (valueLen > 0)
                            reader.ReadBytes(valueBytes);

                        // Reconstruct raw = [op, id, value...]
                        raw[0] = op;
                        raw[1] = id;
                        if (valueLen > 0)
                            Array.Copy(valueBytes, 0, raw, BleConstants.DcHdrLen, valueLen);
                    }
                    WdxRegisters.HandleSet(id, raw, BleConstants.DcHdrLen, valueLen);

                    // Cordio WDX requires an OP_UPDATE notification after every SET
                    // to confirm the value was applied.
                    {
                        DataWriter setAck = new();
                        setAck.WriteByte(BleConstants.DcOpUpdate);
                        setAck.WriteByte(id);

                        if (id == BleConstants.DcIdOwnershipProof)
                        {
                            // Ownership proof: respond with status=0x00 (success).
                            // Echoing the full value would start with 0x01 (claim type),
                            // which the app may interpret as an error status.
                            setAck.WriteByte(0x00);
                        }
                        else
                        {
                            // Standard WDX: echo back the value bytes
                            for (int i = BleConstants.DcHdrLen; i < raw.Length; i++)
                                setAck.WriteByte(raw[i]);
                        }

                        _dcCharacteristic.NotifyValue(setAck.DetachBuffer());
                    }
                    break;

                default:
                    Debug.WriteLine($"BLE DC: Unknown op 0x{op:X2}");
                    break;
            }

            // Write-with-response: acknowledge the write
            request.Respond();
        }

        /// <summary>
        /// Performs a DC GET: builds the OP_UPDATE response via
        /// <see cref="WdxRegisters.HandleGet"/> and sends it as a notification.
        /// </summary>
        private static void HandleDcGet(byte registerId)
        {
            DataWriter writer = new();
            bool handled = WdxRegisters.HandleGet(registerId, writer);

            if (!handled)
            {
                // Unknown register — send an empty UPDATE so the client
                // doesn't hang waiting for a response.
                writer = new DataWriter();
                writer.WriteByte(BleConstants.DcOpUpdate);
                writer.WriteByte(registerId);
            }

            Buffer response = writer.DetachBuffer();
            _dcCharacteristic.NotifyValue(response);
        }

        // ---------------------------------------------------------------
        //  FTC — File Transfer Control write handler
        // ---------------------------------------------------------------

        private static void OnFtcWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            GattWriteRequest request = args.GetRequest();
            Buffer writeBuffer = request.Value;

            if (writeBuffer.Length < 1)
            {
                Debug.WriteLine("BLE FTC: Write too short, ignoring.");
                return;
            }

            DataReader reader = DataReader.FromBuffer(writeBuffer);
            byte op = reader.ReadByte();

            Debug.WriteLine($"BLE FTC: op=0x{op:X2} len={writeBuffer.Length}");

            // Phase 2 stub: log and ignore — full implementation in later phases.
            // WriteWithoutResponse has no Respond() call.
        }

        // ---------------------------------------------------------------
        //  FTD — File Transfer Data write handler
        // ---------------------------------------------------------------

        private static void OnFtdWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            GattWriteRequest request = args.GetRequest();
            Buffer writeBuffer = request.Value;

            Debug.WriteLine($"BLE FTD: Received {writeBuffer.Length} bytes (stub — ignored).");

            // Phase 2 stub: log and ignore — full implementation in later phases.
        }

        // ---------------------------------------------------------------
        //  AU — Authentication write handler (Phase 4)
        // ---------------------------------------------------------------

        /// <summary>
        /// Handles incoming AU writes implementing the WDX authentication
        /// challenge/response protocol:
        ///
        ///   AU_OP_START   (0x01): Client requests an auth level.
        ///                         Server responds with AU_OP_CHALLENGE + 16-byte nonce.
        ///   AU_OP_REPLY   (0x03): Client sends 8-byte hash.
        ///                         Server verifies (or bypasses) and notifies result.
        ///
        /// Controlled by <see cref="WdxRegisters.BypassEcdsaAuth"/>:
        ///   true  → accept any reply, grant the requested level (dev mode).
        ///   false → reject all replies with AU_ERR_AUTH_FAILED (secure default).
        /// </summary>
        private static void OnAuWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            GattWriteRequest request = args.GetRequest();
            Buffer writeBuffer = request.Value;

            if (writeBuffer.Length < BleConstants.AuHdrLen)
            {
                Debug.WriteLine("BLE AU: Write too short, ignoring.");
                request.Respond();
                return;
            }

            DataReader reader = DataReader.FromBuffer(writeBuffer);
            byte op = reader.ReadByte();

            Debug.WriteLine($"BLE AU: op=0x{op:X2} len={writeBuffer.Length} bypass={WdxRegisters.BypassEcdsaAuth}");

            switch (op)
            {
                case BleConstants.AuOpStart:
                    HandleAuStart(reader, writeBuffer);
                    break;

                case BleConstants.AuOpReply:
                    HandleAuReply(reader, writeBuffer);
                    break;

                default:
                    Debug.WriteLine($"BLE AU: Unknown op 0x{op:X2}");
                    break;
            }

            // Write-with-response: acknowledge the write
            request.Respond();
        }

        /// <summary>
        /// Handles AU_OP_START — the client requests a specific authentication level.
        /// We generate a 16-byte random challenge and send it back via notification.
        /// </summary>
        private static void HandleAuStart(DataReader reader, Buffer writeBuffer)
        {
            // Payload: [level_lo: 1B] [level_hi: 1B] (little-endian uint16)
            if (writeBuffer.Length < BleConstants.AuHdrLen + BleConstants.AuParamLenStart)
            {
                Debug.WriteLine("BLE AU: START payload too short.");
                return;
            }

            byte requestedLevel = reader.ReadByte(); // low byte is the level
            // ignore high byte — LEGO only uses levels 0–3

            Debug.WriteLine($"BLE AU: START — client requests auth level {requestedLevel}");

            // Store pending state
            WdxRegisters.PendingAuthLevel = requestedLevel;

            // Generate 16-byte random challenge using Guid.NewGuid() as RNG
            byte[] challenge = Guid.NewGuid().ToByteArray();
            WdxRegisters.PendingChallenge = challenge;

            Debug.WriteLine($"BLE AU: Sending CHALLENGE ({challenge.Length} bytes)");

            // Build notification: [AU_OP_CHALLENGE] [challenge: 16B]
            DataWriter writer = new();
            writer.WriteByte(BleConstants.AuOpChallenge);
            writer.WriteBytes(challenge);

            _auCharacteristic.NotifyValue(writer.DetachBuffer());
        }

        /// <summary>
        /// Handles AU_OP_REPLY — the client sends an 8-byte hash in response
        /// to our challenge. In bypass mode we accept it; otherwise we reject.
        /// </summary>
        private static void HandleAuReply(DataReader reader, Buffer writeBuffer)
        {
            if (WdxRegisters.PendingChallenge == null)
            {
                Debug.WriteLine("BLE AU: REPLY received but no challenge is pending — ignoring.");
                return;
            }

            // Read the 8-byte hash (if present)
            int hashLen = (int)writeBuffer.Length - BleConstants.AuHdrLen;
            byte[] hash = null;
            if (hashLen >= BleConstants.AuParamLenReply)
            {
                hash = new byte[BleConstants.AuParamLenReply];
                reader.ReadBytes(hash);
            }

            Debug.WriteLine($"BLE AU: REPLY — hash length={hashLen} pending level={WdxRegisters.PendingAuthLevel}");

            // Log the hash bytes for protocol research
            if (hash != null)
            {
                Debug.WriteLine($"BLE AU: REPLY hash = {BitConverter.ToString(hash)}");
            }

            if (WdxRegisters.BypassEcdsaAuth)
            {
                // --- Developer / debug mode: accept any reply ---
                WdxRegisters.AuthLevel = WdxRegisters.PendingAuthLevel;
                Debug.WriteLine($"BLE AU: BYPASS — granted auth level {WdxRegisters.AuthLevel}");

                // Notify success: [AU_OP_REPLY] [granted_level]
                DataWriter writer = new();
                writer.WriteByte(BleConstants.AuOpReply);
                writer.WriteByte(WdxRegisters.AuthLevel);
                _auCharacteristic.NotifyValue(writer.DetachBuffer());
            }
            else
            {
                // --- Secure default: reject ---
                Debug.WriteLine("BLE AU: REJECTED — ECDSA verification not available.");

                WdxRegisters.AuthLevel = BleConstants.AuLvlNone;

                // Notify failure: [AU_OP_REPLY] [AU_LVL_NONE]
                DataWriter writer = new();
                writer.WriteByte(BleConstants.AuOpReply);
                writer.WriteByte(BleConstants.AuLvlNone);
                _auCharacteristic.NotifyValue(writer.DetachBuffer());
            }

            // Clear pending state
            WdxRegisters.PendingChallenge = null;
            WdxRegisters.PendingAuthLevel = BleConstants.AuLvlNone;
        }
    }
}
