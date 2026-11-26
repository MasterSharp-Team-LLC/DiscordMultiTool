using Microsoft.Win32;
using System;

namespace DiscordMultiTool.Services;

public static class AutoStartService
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DiscordMultiTool";

    public static void SetEnabled(bool enable)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null)
                return;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(ValueName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registry AutoStart error: {ex.Message}");
        }
    }
}
