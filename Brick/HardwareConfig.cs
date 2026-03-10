// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LegoSmartBrick.Brick
{
    /// <summary>
    /// Holds the GPIO pin assignments for a specific ESP32 board variant.
    /// Create a static instance for each supported board and select the
    /// active one via <see cref="Program.ActiveBoard"/>.
    /// </summary>
    public class HardwareConfig
    {
        // --- PN5180 NFC Reader (SPI) ---
        public int SpiMosi { get; }
        public int SpiMiso { get; }
        public int SpiClock { get; }
        public int SpiChipSelect { get; }
        public int NfcReset { get; }
        public int NfcBusy { get; }
        public int NfcNss { get; }

        // --- TCS3472x Colour Sensor (I2C) ---
        public int I2cSda { get; }
        public int I2cScl { get; }

        // --- YX5300 MP3 Player (UART) ---
        public int UartTx { get; }
        public int UartRx { get; }

        /// <summary>Human-readable board name for debug output.</summary>
        public string BoardName { get; }

        public HardwareConfig(
            string boardName,
            int spiMosi, int spiMiso, int spiClock, int spiCs,
            int nfcReset, int nfcBusy, int nfcNss,
            int i2cSda, int i2cScl,
            int uartTx, int uartRx)
        {
            BoardName = boardName;
            SpiMosi = spiMosi;
            SpiMiso = spiMiso;
            SpiClock = spiClock;
            SpiChipSelect = spiCs;
            NfcReset = nfcReset;
            NfcBusy = nfcBusy;
            NfcNss = nfcNss;
            I2cSda = i2cSda;
            I2cScl = i2cScl;
            UartTx = uartTx;
            UartRx = uartRx;
        }

        // ---------------------------------------------------------------
        //  Pre-defined board configurations
        // ---------------------------------------------------------------

        /// <summary>
        /// ESP32-C3 Super Mini — limited GPIOs, uses SPI1 on non-standard pins.
        /// </summary>
        public static HardwareConfig Esp32C3SuperMini => new(
            boardName: "ESP32-C3 Super Mini",
            spiMosi: 6, spiMiso: 5, spiClock: 4, spiCs: -1,
            nfcReset: 3, nfcBusy: 1, nfcNss: 2,
            i2cSda: 7, i2cScl: 9,
            uartTx: 21, uartRx: 20);

        /// <summary>
        /// Classic ESP32 (WROOM-32 / DevKitC) — uses VSPI defaults, plenty of GPIOs.
        /// GPIO 35 is input-only, ideal for PN5180 BUSY.
        /// </summary>
        public static HardwareConfig Esp32Wroom32 => new(
            boardName: "ESP32 WROOM-32",
            spiMosi: 23, spiMiso: 19, spiClock: 18, spiCs: 5,
            nfcReset: 27, nfcBusy: 35, nfcNss: 26,
            i2cSda: 21, i2cScl: 22,
            uartTx: 17, uartRx: 16);
    }
}
