// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace LegoSmartBrick.Ble
{
    /// <summary>
    /// Persists brick settings (name, volume, battery) to internal flash storage.
    /// Uses simple line-based text files on the I: drive.
    /// </summary>
    public static class BrickSettings
    {
        private const string SettingsDir = @"I:\brick";
        private const string NameFile = @"I:\brick\name.txt";
        private const string VolumeFile = @"I:\brick\vol.txt";
        private const string BatteryFile = @"I:\brick\bat.txt";

        /// <summary>
        /// Loads all persisted settings into <see cref="WdxRegisters"/>.
        /// Missing files use defaults silently.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                {
                    Directory.CreateDirectory(SettingsDir);
                    Debug.WriteLine("Settings: Created directory " + SettingsDir);
                }

                // Name
                if (File.Exists(NameFile))
                {
                    string name = ReadText(NameFile);
                    if (name.Length > 0)
                    {
                        WdxRegisters.HubLocalName = name;
                        Debug.WriteLine($"Settings: Loaded name \"{name}\"");
                    }
                }

                // Volume (percentage 0-100)
                if (File.Exists(VolumeFile))
                {
                    string volStr = ReadText(VolumeFile);
                    if (volStr.Length > 0)
                    {
                        int vol = int.Parse(volStr);
                        if (vol < 0) vol = 0;
                        if (vol > 100) vol = 100;
                        WdxRegisters.VolumePct = (byte)vol;
                        byte mapped = (byte)(vol * BleConstants.VolumeMax / 100);
                        if (mapped > BleConstants.VolumeMax) mapped = BleConstants.VolumeMax;
                        WdxRegisters.Volume = mapped;
                        Debug.WriteLine($"Settings: Loaded volume {vol}%");
                    }
                }

                // Battery level (0-100)
                if (File.Exists(BatteryFile))
                {
                    string batStr = ReadText(BatteryFile);
                    if (batStr.Length > 0)
                    {
                        int bat = int.Parse(batStr);
                        if (bat < 0) bat = 0;
                        if (bat > 100) bat = 100;
                        WdxRegisters.BatteryLevel = (byte)bat;
                        Debug.WriteLine($"Settings: Loaded battery {bat}%");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Settings: Load failed — {ex.Message}");
            }
        }

        /// <summary>Saves the brick name to internal storage.</summary>
        public static void SaveName(string name)
        {
            try
            {
                EnsureDir();
                WriteText(NameFile, name);
                Debug.WriteLine($"Settings: Saved name \"{name}\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Settings: SaveName failed — {ex.Message}");
            }
        }

        /// <summary>Saves the volume percentage to internal storage.</summary>
        public static void SaveVolume(byte volumePct)
        {
            try
            {
                EnsureDir();
                WriteText(VolumeFile, volumePct.ToString());
                Debug.WriteLine($"Settings: Saved volume {volumePct}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Settings: SaveVolume failed — {ex.Message}");
            }
        }

        /// <summary>Saves the battery level to internal storage.</summary>
        public static void SaveBattery(byte level)
        {
            try
            {
                EnsureDir();
                WriteText(BatteryFile, level.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Settings: SaveBattery failed — {ex.Message}");
            }
        }

        private static void EnsureDir()
        {
            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);
        }

        private static string ReadText(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            return System.Text.Encoding.UTF8.GetString(data, 0, data.Length).Trim();
        }

        private static void WriteText(string path, string content)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
            File.WriteAllBytes(path, data);
        }
    }
}
