using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBypass.Services
{
    /// <summary>
    /// Service for managing GoodbyeDPI - Deep Packet Inspection bypass tool
    /// </summary>
    public class DpiBypassService
    {
        private readonly string _appDataPath;
        private readonly string _goodbyeDpiPath;
        private Process? _goodbyeDpiProcess;
        
        public event Action<string>? OnLogMessage;
        
        public bool IsRunning => _goodbyeDpiProcess != null && !_goodbyeDpiProcess.HasExited;

        public DpiBypassService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiscordBypass");
            _goodbyeDpiPath = Path.Combine(_appDataPath, "goodbyedpi");
            
            // Ensure directory exists
            Directory.CreateDirectory(_appDataPath);
        }

        /// <summary>
        /// Downloads and extracts GoodbyeDPI if not already present
        /// </summary>
        public async Task<bool> EnsureGoodbyeDpiAsync()
        {
            string exePath = Path.Combine(_goodbyeDpiPath, "x86_64", "goodbyedpi.exe");
            
            if (File.Exists(exePath))
            {
                Log("GoodbyeDPI is already installed");
                return true;
            }

            try
            {
                Log("Downloading GoodbyeDPI...");
                
                // Download GoodbyeDPI from GitHub
                string downloadUrl = "https://github.com/ValdikSS/GoodbyeDPI/releases/download/0.2.3rc3/goodbyedpi-0.2.3rc3.zip";
                string zipPath = Path.Combine(_appDataPath, "goodbyedpi.zip");

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    var response = await httpClient.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(zipPath, bytes);
                }
                
                Log("Extracting GoodbyeDPI...");
                
                // Extract the zip
                if (Directory.Exists(_goodbyeDpiPath))
                    Directory.Delete(_goodbyeDpiPath, true);
                    
                ZipFile.ExtractToDirectory(zipPath, _appDataPath);
                
                // Rename extracted folder
                string extractedFolder = Path.Combine(_appDataPath, "goodbyedpi-0.2.3rc3");
                if (Directory.Exists(extractedFolder))
                {
                    Directory.Move(extractedFolder, _goodbyeDpiPath);
                }
                
                // Clean up zip file
                File.Delete(zipPath);
                
                Log("GoodbyeDPI installed successfully");
                return File.Exists(exePath);
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to download GoodbyeDPI: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts GoodbyeDPI with optimal settings for Egypt
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (IsRunning)
            {
                Log("GoodbyeDPI is already running");
                return true;
            }

            string exePath = Path.Combine(_goodbyeDpiPath, "x86_64", "goodbyedpi.exe");
            
            if (!File.Exists(exePath))
            {
                bool downloaded = await EnsureGoodbyeDpiAsync();
                if (!downloaded)
                {
                    Log("ERROR: GoodbyeDPI executable not found");
                    return false;
                }
            }

            try
            {
                Log("Starting GoodbyeDPI with Egypt-optimized settings...");
                
                // Optimal settings for bypassing Egyptian DPI
                // -5 = Russia preset (aggressive, works for most DPI)
                // -e = Additional SNI bypass
                // -q = Quiet mode
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-5 -e 1 -q",
                    WorkingDirectory = Path.Combine(_goodbyeDpiPath, "x86_64"),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _goodbyeDpiProcess = new Process { StartInfo = startInfo };
                _goodbyeDpiProcess.Start();
                
                // Give it a moment to start
                await Task.Delay(1000);
                
                if (_goodbyeDpiProcess.HasExited)
                {
                    Log("ERROR: GoodbyeDPI exited immediately. Try running as Administrator.");
                    return false;
                }
                
                Log("✓ GoodbyeDPI started successfully (DPI bypass active)");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to start GoodbyeDPI: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops GoodbyeDPI
        /// </summary>
        public void Stop()
        {
            if (_goodbyeDpiProcess != null && !_goodbyeDpiProcess.HasExited)
            {
                try
                {
                    _goodbyeDpiProcess.Kill(true);
                    _goodbyeDpiProcess.WaitForExit(3000);
                    Log("GoodbyeDPI stopped");
                }
                catch (Exception ex)
                {
                    Log($"Warning: Error stopping GoodbyeDPI: {ex.Message}");
                }
            }
            _goodbyeDpiProcess = null;
        }

        /// <summary>
        /// Installs WinDivert driver (required for GoodbyeDPI)
        /// </summary>
        public async Task<bool> InstallDriverAsync()
        {
            string driverPath = Path.Combine(_goodbyeDpiPath, "x86_64", "WinDivert64.sys");
            
            if (!File.Exists(driverPath))
            {
                Log("WinDivert driver not found - downloading GoodbyeDPI first");
                return await EnsureGoodbyeDpiAsync();
            }
            
            return true;
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
