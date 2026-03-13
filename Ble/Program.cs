// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace LegoSmartBrick.Ble
{
    public class Program
    {
        // Set to true to accept any ECDSA signature during BLE authentication
        // (developer mode). When false, all auth attempts are rejected (secure default).
        private const bool BypassEcdsaAuth = true;

        // Battery simulation: starts at stored level (or 20% if none),
        // charges 1% every ChargeIntervalMs until 100%.
        private const int ChargeIntervalMs = 30_000; // 30 seconds per 1%
        private const byte InitialBatteryIfNew = 20;

        public static void Main()
        {
            Debug.WriteLine("=== LEGO Smart Brick — BLE-only firmware ===");
            Debug.WriteLine("");

            // Load persisted settings (name, volume, battery)
            BrickSettings.Load();

            // If battery was never stored, start low to simulate a fresh charge
            if (WdxRegisters.BatteryLevel == 100)
            {
                WdxRegisters.BatteryLevel = InitialBatteryIfNew;
            }

            Debug.WriteLine($"Battery: starting at {WdxRegisters.BatteryLevel}%");

            WdxRegisters.BypassEcdsaAuth = BypassEcdsaAuth;

            if (BluetoothService.Init())
            {
                // Force GC and compact heap after all BLE init allocations
                uint freeBytes = nanoFramework.Runtime.Native.GC.Run(true);
                Debug.WriteLine($"BLE advertising started. Free heap: {freeBytes} bytes");
            }
            else
            {
                Debug.WriteLine("BLE advertising failed to start.");
            }

            Debug.WriteLine("");

            // Start battery charging simulation in background
            new Thread(BatteryChargeLoop).Start();

            // Keep the application alive.
            Thread.Sleep(Timeout.Infinite);
        }

        private static void BatteryChargeLoop()
        {
            while (true)
            {
                Thread.Sleep(ChargeIntervalMs);

                byte level = WdxRegisters.BatteryLevel;
                if (level < 100)
                {
                    level++;
                    WdxRegisters.BatteryLevel = level;
                    BrickSettings.SaveBattery(level);
                    Debug.WriteLine($"Battery: charging → {level}%");
                }
            }
        }
    }
}
