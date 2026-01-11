using System;
using Microsoft.Win32;

namespace DiscordBypass.Helpers
{
    /// <summary>
    /// Helper for managing Windows startup registration
    /// </summary>
    public class StartupHelper
    {
        private const string AppName = "DiscordBypass";
        private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Enables auto-start on Windows boot
        /// </summary>
        public void EnableStartup()
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName 
                    ?? throw new InvalidOperationException("Could not determine executable path");
                
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enable startup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disables auto-start on Windows boot
        /// </summary>
        public void DisableStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                key?.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to disable startup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if auto-start is enabled
        /// </summary>
        public bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
