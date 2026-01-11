using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBypass.Services
{
    /// <summary>
    /// Service for managing Cloudflare WARP - more reliable bypass for Egypt
    /// </summary>
    public class WarpService
    {
        private const string WarpCliPath = @"C:\Program Files\Cloudflare\Cloudflare WARP\warp-cli.exe";
        private const string WarpInstallerUrl = "https://1111-releases.cloudflareclient.com/windows/Cloudflare_WARP_Release-x64.msi";
        
        public event Action<string>? OnLogMessage;
        
        public bool IsWarpInstalled => File.Exists(WarpCliPath);

        /// <summary>
        /// Checks if WARP is currently connected
        /// </summary>
        public async Task<bool> IsConnectedAsync()
        {
            if (!IsWarpInstalled) return false;
            
            try
            {
                var result = await RunWarpCliAsync("status");
                return result.Contains("Connected") || result.Contains("Status: Connected");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Registers WARP if not already registered
        /// </summary>
        public async Task<bool> RegisterAsync()
        {
            if (!IsWarpInstalled)
            {
                Log("⚠ WARP is not installed. Please install it first.");
                return false;
            }

            try
            {
                Log("Registering WARP account...");
                var result = await RunWarpCliAsync("register");
                
                if (result.Contains("Success") || result.Contains("already registered"))
                {
                    Log("✓ WARP registration complete");
                    return true;
                }
                
                Log($"Registration result: {result}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Registration failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Connects to WARP
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (!IsWarpInstalled)
            {
                Log("ERROR: Cloudflare WARP is not installed.");
                Log("Please download and install WARP from:");
                Log("https://1.1.1.1/");
                return false;
            }

            try
            {
                // First check if already connected
                if (await IsConnectedAsync())
                {
                    Log("✓ WARP is already connected");
                    return true;
                }

                // Register if needed
                await RegisterAsync();

                // Set mode to WARP (full tunnel)
                Log("Setting WARP mode...");
                await RunWarpCliAsync("set-mode warp");

                // Connect
                Log("Connecting to WARP...");
                var result = await RunWarpCliAsync("connect");
                
                // Wait and verify connection
                await Task.Delay(3000);
                
                if (await IsConnectedAsync())
                {
                    Log("══════════════════════════════════════");
                    Log("✓ CLOUDFLARE WARP CONNECTED!");
                    Log("✓ All traffic is now encrypted");
                    Log("✓ Discord should work now!");
                    Log("══════════════════════════════════════");
                    return true;
                }
                else
                {
                    Log("⚠ Connection may still be establishing...");
                    Log("Check the WARP icon in system tray");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnects from WARP
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            if (!IsWarpInstalled) return true;

            try
            {
                Log("Disconnecting from WARP...");
                await RunWarpCliAsync("disconnect");
                Log("✓ WARP disconnected");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error disconnecting: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads WARP installer
        /// </summary>
        public async Task<string?> DownloadInstallerAsync()
        {
            try
            {
                string downloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "Cloudflare_WARP_Installer.msi");

                Log("Downloading Cloudflare WARP installer...");
                
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                
                var response = await httpClient.GetAsync(WarpInstallerUrl);
                response.EnsureSuccessStatusCode();
                
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(downloadPath, bytes);
                
                Log($"✓ Downloaded to: {downloadPath}");
                return downloadPath;
            }
            catch (Exception ex)
            {
                Log($"Download failed: {ex.Message}");
                return null;
            }
        }

        private async Task<string> RunWarpCliAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = WarpCliPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            return string.IsNullOrEmpty(output) ? error : output;
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
