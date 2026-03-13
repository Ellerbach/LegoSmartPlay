// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Diagnostics;
using System.Threading;
using Iot.Device.Card.Icode;
using Iot.Device.Pn5180;
using Iot.Device.Rfid;
using Iot.Device.Tcs3472x;
using Iot.Device.Yx5300;
using nanoFramework.Hardware.Esp32;

namespace LegoSmartBrick.Brick
{
    public class Program
    {
        // Set to false to skip all PN5180 / NFC reader code (useful for testing other peripherals).
        private const bool EnableNfc = false;

        // Set to false to skip YX5300 MP3 player initialisation.
        private const bool EnableMp3 = false;

        // ---------------------------------------------------------------
        //  Board selection — change this line to switch hardware config.
        // ---------------------------------------------------------------
        //private static readonly HardwareConfig Board = HardwareConfig.Esp32C3SuperMini;
        private static readonly HardwareConfig Board = HardwareConfig.Esp32Wroom32;

        // LEGO Smart Play card header constants.
        private const byte LegoHeaderFlags = 0x00;
        private const byte LegoFormatVersion = 0x01;
        private const byte LegoCardTypeMarker = 0x0C;

        // Colour-triggered sound track numbers (folder 99 on the SD card).
        private const byte ColorTrackBlueWater = 255;
        private const byte ColorTrackGreenTools = 254;
        private const byte ColorTrackRedLaser = 253;

        // Tracks the last colour that triggered a sound, to avoid repeating.
        private static string _lastColorSound = string.Empty;

        /// <summary>
        /// Returns the known tag name for a given Brick ID, or null if unknown.
        /// Brick IDs are sourced from our own card dumps and the Coffee &amp; Fun blog
        /// (LEGO Star Wars sets 75421 and 75423).
        /// </summary>
        private static string GetBrickName(byte brickId)
        {
            switch (brickId)
            {
                // Set 75421 — Darth Vader's TIE Fighter
                case 0x3B: return "TIE Fighter (vehicle, set 75421)";
                case 0xA9: return "Darth Vader (minifig, set 75421)";

                // Set 75423 — X-Wing Starfighter
                case 0x4A: return "R2-D2 (accessory, set 75423)";
                case 0x69: return "X-Wing body (vehicle, set 75423)";
                case 0x6B: return "X-Wing wing (vehicle, set 75423)";
                case 0x7E: return "Lightsaber (tile)";
                case 0x9D: return "Luke Skywalker (minifig, set 75423)";
                case 0x9E: return "Princess Leia (minifig, set 75423)";
                case 0xAB: return "Emperor Palpatine (minifig)";

                default: return null;
            }
        }

        public static void Main()
        {
            Debug.WriteLine("=== LegoSmartBrick - LEGO Smart Play Tag Reader ===");
            Debug.WriteLine($"Board: {Board.BoardName}");
            Debug.WriteLine("");

            Pn5180 pn5180 = null;

            if (IsNfcEnabled())
            {
                // Configure ESP32 SPI pin muxing.
                // This is required on ESP32 before using SPI — map physical GPIOs to SPI1 functions.
                Configuration.SetPinFunction(Board.SpiMosi, DeviceFunction.SPI1_MOSI);
                Configuration.SetPinFunction(Board.SpiMiso, DeviceFunction.SPI1_MISO);
                Configuration.SetPinFunction(Board.SpiClock, DeviceFunction.SPI1_CLOCK);

                SpiDevice spi = SpiDevice.Create(new SpiConnectionSettings(1, Board.SpiChipSelect)
                {
                    ClockFrequency = Pn5180.MaximumSpiClockFrequency,
                    Mode = Pn5180.DefaultSpiMode,
                    DataFlow = DataFlow.MsbFirst,
                });

                // Hardware-reset the PN5180 before driver initialisation.
                GpioController gpio = new();
                gpio.OpenPin(Board.NfcReset, PinMode.Output);
                gpio.Write(Board.NfcReset, PinValue.Low);
                Thread.Sleep(10);
                gpio.Write(Board.NfcReset, PinValue.High);
                Thread.Sleep(10);

                // Create the PN5180 driver instance.
                // Share the same GpioController — disposing a separate one
                // can invalidate BUSY/NSS pins on nanoFramework.
                pn5180 = new Pn5180(spi, Board.NfcBusy, Board.NfcNss, gpio, false);

                // Read and display PN5180 firmware version.
                var versions = pn5180.GetVersions();
                Debug.WriteLine($"PN5180 Product: {versions.Product}");
                Debug.WriteLine($"PN5180 Firmware: {versions.Firmware}");
                Debug.WriteLine($"PN5180 EEPROM: {versions.Eeprom}");
                Debug.WriteLine("");

                // Load ISO 15693 RF configuration and verify the antenna / RF field.
                pn5180.LoadRadioFrequencyConfiguration(
                    TransmitterRadioFrequencyConfiguration.Iso15693_ASK100_26,
                    ReceiverRadioFrequencyConfiguration.Iso15693_26);
                pn5180.RadioFrequencyField = true;
                Thread.Sleep(50);
                Debug.WriteLine($"RF field on: {pn5180.RadioFrequencyField}, Status: {pn5180.GetRadioFrequencyStatus()}, External: {pn5180.IsRadioFrequencyFieldExternal()}");
                pn5180.RadioFrequencyField = false;
                Debug.WriteLine("");
            }
            else
            {
                Debug.WriteLine("NFC reader disabled. Skipping PN5180 init.");
                Debug.WriteLine("");
            }

            // --- TCS3472x colour sensor (I2C) ---
            Configuration.SetPinFunction(Board.I2cSda, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(Board.I2cScl, DeviceFunction.I2C1_CLOCK);

            Tcs3472x colorSensor = null;

            try
            {
                // Use Standard mode (100 kHz) — more reliable on ESP32-C3 with longer wires.
                var i2cSettings = new I2cConnectionSettings(1, Tcs3472x.DefaultI2cAddress, I2cBusSpeed.StandardMode);
                I2cDevice i2c = I2cDevice.Create(i2cSettings);
                Debug.WriteLine($"I2C bus speed: {i2cSettings.BusSpeed}");

                colorSensor = new Tcs3472x(i2c, gain: Gain.Gain04X);
                Debug.WriteLine($"TCS3472x chip ID: 0x{(int)colorSensor.ChipId:X2}  Gain: {(int)colorSensor.Gain}  Integration time: {colorSensor.IntegrationTime}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TCS3472x init failed: {ex.Message}");
                Debug.WriteLine("Colour sensor disabled.");
                Debug.WriteLine("");
            }

            // --- YX5300 serial MP3 player (UART) ---
            Yx5300 mp3Player = null;

            if (EnableMp3)
            {
                // Sound files are stored on the MP3 module's SD card.
                // Expected layout: /01/001.mp3, /01/002.mp3, ... mapped to Brick IDs.
                Configuration.SetPinFunction(Board.UartTx, DeviceFunction.COM2_TX);
                Configuration.SetPinFunction(Board.UartRx, DeviceFunction.COM2_RX);

                try
                {
                    mp3Player = new Yx5300("COM2");

                    // The YX5300 module needs time to boot after power-on.
                    // Increase delay on classic ESP32 — module may take longer to become ready.
                    Thread.Sleep(1000);

                    mp3Player.Volume(Yx5300.MaxVolume / 2);
                    Debug.WriteLine("YX5300 MP3 player initialised on COM2.");
                    Debug.WriteLine("");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"YX5300 init failed: {ex.Message}");
                    Debug.WriteLine("MP3 player disabled.");
                    Debug.WriteLine("");
                    mp3Player = null;
                }
            }
            else
            {
                Debug.WriteLine("MP3 player disabled. Skipping YX5300 init.");
                Debug.WriteLine("");
            }

            // Start the colour sensor on its own thread.
            if (colorSensor != null)
            {
                Tcs3472x sensor = colorSensor;
                Yx5300 mp3 = mp3Player;
                new Thread(() => ColorSensorLoop(sensor, mp3)).Start();
            }

            // Start the NFC polling loop on the main thread.
            PollForLegoTags(pn5180, mp3Player);

            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// Returns true if NFC polling is enabled.
        /// </summary>
        private static bool IsNfcEnabled()
        {
            return EnableNfc;
        }

        /// <summary>
        /// Continuously reads the TCS3472x colour sensor every 500 ms
        /// on a dedicated background thread.
        /// </summary>
        private static void ColorSensorLoop(Tcs3472x colorSensor, Yx5300 mp3Player)
        {
            Debug.WriteLine("Colour sensor thread started.");

            while (true)
            {
                try
                {
                    ReadColor(colorSensor, mp3Player);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Color sensor error: {ex.Message}");
                }

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Continuously polls for ISO 15693 cards and identifies LEGO Smart Play tags.
        /// </summary>
        private static void PollForLegoTags(Pn5180 pn5180, Yx5300 mp3Player)
        {
            Debug.WriteLine("Waiting for LEGO Smart Play tags (ISO 15693 / NFC-V)...");
            Debug.WriteLine("");

            string lastUid = string.Empty;
            int nfcErrors = 0;

            while (true)
            {
                // --- NFC tag polling ---
                if (IsNfcEnabled() && pn5180 != null)
                {
                    try
                    {
                        if (pn5180.ListenToCardIso15693(
                            TransmitterRadioFrequencyConfiguration.Iso15693_ASK100_26,
                            ReceiverRadioFrequencyConfiguration.Iso15693_26,
                            out ArrayList detectedCards,
                            2000))
                        {
                            nfcErrors = 0;
                            Data26_53kbps card = (Data26_53kbps)detectedCards[0];
                            string currentUid = BitConverter.ToString(card.NfcId);

                            if (currentUid != lastUid)
                            {
                                if (lastUid != string.Empty)
                                {
                                    Debug.WriteLine($"  *** Card changed! Previous UID: {lastUid} ***");
                                }

                                lastUid = currentUid;
                                ProcessLegoTag(pn5180, card, mp3Player);
                            }
                        }
                        else
                        {
                            nfcErrors = 0;

                            // Card removed — reset so re-placing the same card triggers a new read.
                            if (lastUid != string.Empty)
                            {
                                Debug.WriteLine("Tag removed.");
                                Debug.WriteLine("");
                                lastUid = string.Empty;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        nfcErrors++;
                        Debug.WriteLine($"NFC error ({nfcErrors}): {ex.Message}");

                        // After repeated failures, attempt a PN5180 soft recovery.
                        if (nfcErrors % 5 == 0)
                        {
                            Debug.WriteLine("Attempting PN5180 recovery (RF off, reload config)...");
                            try
                            {
                                pn5180.RadioFrequencyField = false;
                                Thread.Sleep(200);
                                pn5180.LoadRadioFrequencyConfiguration(
                                    TransmitterRadioFrequencyConfiguration.Iso15693_ASK100_26,
                                    ReceiverRadioFrequencyConfiguration.Iso15693_26);
                            }
                            catch
                            {
                                Debug.WriteLine("Recovery failed — check power supply.");
                            }
                        }

                        lastUid = string.Empty;
                        Thread.Sleep(1000);
                        continue;
                    }
                }

                // Turn off the RF field to power-reset the card before the next inventory.
                if (IsNfcEnabled() && pn5180 != null)
                {
                    try
                    {
                        pn5180.RadioFrequencyField = false;
                    }
                    catch
                    {
                        // Ignore — will retry next cycle.
                    }
                }

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Reads the TCS3472x colour sensor, converts to HSL, classifies the
        /// colour by hue/saturation/lightness, prints the result, and plays
        /// a sound effect for Blue (water) or Green (tools).
        /// </summary>
        private static void ReadColor(Tcs3472x colorSensor, Yx5300 mp3Player)
        {
            if (colorSensor == null)
            {
                return;
            }

            var color = colorSensor.GetColor(true);
            if (color.A < 100)
            {
                return;
            }

            string name = ClassifyColor(color.R, color.G, color.B);
            Debug.WriteLine($"  Color => R: {color.R}  G: {color.G}  B: {color.B}  Clear: {color.A}  => {name}");

            // Trigger sound effects for specific colours (only on change).
            if (name != _lastColorSound)
            {
                _lastColorSound = name;

                if (name == "Blue" && mp3Player != null)
                {
                    Debug.WriteLine($"  Playing water sound (track {ColorTrackBlueWater})...");
                    mp3Player.PlaySpecific(99, ColorTrackBlueWater);
                }
                else if (name == "Green" && mp3Player != null)
                {
                    Debug.WriteLine($"  Playing tools sound (track {ColorTrackGreenTools})...");
                    mp3Player.PlaySpecific(99, ColorTrackGreenTools);
                }
                else if (name == "Red" && mp3Player != null)
                {
                    Debug.WriteLine($"  Playing laser blast (track {ColorTrackRedLaser})...");
                    mp3Player.PlaySpecific(99, ColorTrackRedLaser);
                }
            }
        }

        /// <summary>
        /// Classifies an RGB reading into a common colour name using
        /// hue, saturation and lightness (HSL).
        /// </summary>
        private static string ClassifyColor(int r, int g, int b)
        {
            // Find min/max channel values.
            int max = r;
            if (g > max) max = g;
            if (b > max) max = b;

            int min = r;
            if (g < min) min = g;
            if (b < min) min = b;

            int delta = max - min;

            // Lightness (0-255 scale).
            int lightness = (max + min) / 2;

            // Very dark = black.
            if (lightness < 20)
            {
                return "Black";
            }

            // Very bright + low saturation = white.
            // Saturation: delta relative to max.
            if (max > 0 && (delta * 100 / max) < 15)
            {
                return lightness > 180 ? "White" : "Grey";
            }

            // Compute hue (0-360 degrees).
            int hue;
            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue = 60 * (g - b) / delta;
                if (hue < 0) hue += 360;
            }
            else if (max == g)
            {
                hue = 120 + 60 * (b - r) / delta;
            }
            else
            {
                hue = 240 + 60 * (r - g) / delta;
            }

            if (hue < 0) hue += 360;

            // Saturation percentage (0-100).
            int satPct = max > 0 ? (delta * 100 / max) : 0;

            // Low saturation = grey/white/black.
            if (satPct < 15)
            {
                if (lightness > 180) return "White";
                if (lightness < 40) return "Black";
                return "Grey";
            }

            // Classify by hue angle.
            if (hue < 15 || hue >= 345) return "Red";
            if (hue < 40)  return "Orange";
            if (hue < 75)  return "Yellow";
            if (hue < 160) return "Green";
            if (hue < 195) return "Cyan";
            if (hue < 260) return "Blue";
            if (hue < 290) return "Purple";
            if (hue < 345) return "Pink";

            return "Unknown";
        }

        /// <summary>
        /// Reads block 0 of the detected card, validates the LEGO header,
        /// extracts the Brick ID, and prints the tag identification.
        /// </summary>
        private static void ProcessLegoTag(Pn5180 pn5180, Data26_53kbps detectedCard, Yx5300 mp3Player)
        {
            // Reset RF configuration for addressed-mode communication.
            pn5180.ResetPN5180Configuration(
                TransmitterRadioFrequencyConfiguration.Iso15693_ASK100_26,
                ReceiverRadioFrequencyConfiguration.Iso15693_26);

            var icodeCard = new IcodeCard(pn5180, detectedCard.TargetNumber)
            {
                Uid = detectedCard.NfcId,
                Capacity = IcodeCardCapacity.Unknown
            };

            string uid = BitConverter.ToString(detectedCard.NfcId);
            Debug.WriteLine($"ISO 15693 card detected:");
            Debug.WriteLine($"  UID: {uid}");
            Debug.WriteLine($"  DSFID: 0x{detectedCard.Dsfid:X2}");
            Debug.WriteLine($"  Slot: {detectedCard.TargetNumber}");

            // Read block 0 (4 bytes): [flags] [brick-id] [version] [type]
            if (!icodeCard.ReadSingleBlock(0))
            {
                Debug.WriteLine("  Failed to read block 0.");
                Debug.WriteLine("");
                return;
            }

            if (icodeCard.Data == null || icodeCard.Data.Length < 5)
            {
                Debug.WriteLine("  Block 0 data too short.");
                Debug.WriteLine("");
                return;
            }

            // The first byte is the ISO 15693 block security status — skip it.
            // Actual block data: [flags] [brick-id] [version] [type]
            byte flags = icodeCard.Data[1];
            byte brickId = icodeCard.Data[2];
            byte version = icodeCard.Data[3];
            byte cardType = icodeCard.Data[4];

            Debug.WriteLine($"  Block 0: {BitConverter.ToString(icodeCard.Data)}");

            // Validate LEGO Smart Play header.
            if (flags != LegoHeaderFlags || version != LegoFormatVersion || cardType != LegoCardTypeMarker)
            {
                Debug.WriteLine($"  Not a LEGO Smart Play tag (expected 00-xx-01-0C, got {flags:X2}-{brickId:X2}-{version:X2}-{cardType:X2}).");
                Debug.WriteLine("");
                return;
            }

            // Look up the Brick ID.
            string name = GetBrickName(brickId);
            if (name != null)
            {
                Debug.WriteLine($"  Brick ID: 0x{brickId:X2} ({brickId}) => {name}");
            }
            else
            {
                Debug.WriteLine($"  Brick ID: 0x{brickId:X2} ({brickId}) => Unknown tag");
            }

            Debug.WriteLine($"  Payload size: {brickId} bytes (blocks 1-{(brickId + 3) / 4})");
            Debug.WriteLine("");

            // Play the MP3 sound associated with this Brick ID (if one exists).
            PlayBrickSound(mp3Player, brickId);
        }

        /// <summary>
        /// Plays the MP3 file whose track number matches the Brick ID byte value.
        /// The Brick ID is used directly as the track number in folder 01 on the SD card.
        /// For example, Brick ID 0x3B (59) plays file /01/059.mp3.
        /// If no matching file exists on the SD card, the module silently does nothing.
        /// </summary>
        private static void PlayBrickSound(Yx5300 mp3Player, byte brickId)
        {
            if (mp3Player == null)
            {
                return;
            }

            Debug.WriteLine($"  Playing track {brickId:D3} for Brick ID 0x{brickId:X2}...");
            mp3Player.PlaySpecific(1, brickId);
        }
    }
}
