using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBypass.Services
{
    /// <summary>
    /// Options for bypass configuration
    /// </summary>
    public class BypassOptions
    {
        public bool EnableDiscord { get; set; } = true;
        public bool EnableFiveM { get; set; } = true;
        public bool EnableValorant { get; set; } = true;
        public bool EnableLeague { get; set; } = true;
        public bool EnableDpiBypass { get; set; } = true;
        public bool LowPingMode { get; set; } = false;
    }

    /// <summary>
    /// Main orchestrator for bypass methods (WARP, DNS, Hosts file)
    /// </summary>
    public class BypassManager
    {
        private readonly DnsService _dnsService;
        private readonly HostsFileService _hostsService;
        private readonly WarpService _warpService;
        private readonly DpiBypassService _dpiService;
        private bool _isEnabled = false;
        
        public event Action<string>? OnLogMessage;
        public event Action<bool>? OnStatusChanged;
        
        public bool IsEnabled => _isEnabled;

        public BypassManager()
        {
            _dnsService = new DnsService();
            _hostsService = new HostsFileService();
            _warpService = new WarpService();
            _dpiService = new DpiBypassService();
            
            // Forward log messages
            _dnsService.OnLogMessage += msg => OnLogMessage?.Invoke(msg);
            _hostsService.OnLogMessage += msg => OnLogMessage?.Invoke(msg);
            _warpService.OnLogMessage += msg => OnLogMessage?.Invoke(msg);
            _dpiService.OnLogMessage += msg => OnLogMessage?.Invoke(msg);
        }

        /// <summary>
        /// Enables the bypass using multiple methods
        /// </summary>
        public async Task<bool> EnableBypassAsync(BypassOptions options)
        {
            try
            {
                Log("Starting bypass configuration...");
                Log("");
                
                bool anySuccess = false;
                
                // STEP 1: Reliability Mode (WARP) vs Low Ping Mode (GoodbyeDPI)
                if (options.EnableDpiBypass)
                {
                    Log("══════════════════════════════════════");
                    Log(options.LowPingMode ? "   STEP 1: LOW PING MODE (DPI BYPASS)" : "   STEP 1: CLOUDFLARE WARP");
                    Log("══════════════════════════════════════");
                    
                    if (options.LowPingMode)
                    {
                        Log("Low Ping Mode active - skipping WARP to maintain game ping.");
                        Log("Starting GoodbyeDPI (Stealth Mode)...");
                        _dpiService.AggressiveMode = true; // Use aggressive settings for low ping mode
                        bool dpiStarted = await _dpiService.StartAsync();
                        if (dpiStarted)
                        {
                            anySuccess = true;
                        }
                    }
                    else if (_warpService.IsWarpInstalled)
                    {
                        Log("WARP is installed, connecting...");
                        bool warpConnected = await _warpService.ConnectAsync();
                        if (warpConnected)
                        {
                            anySuccess = true;
                        }
                    }
                    else
                    {
                        Log("⚠ Cloudflare WARP is NOT installed.");
                        Log("");
                        Log("To install WARP:");
                        Log("1. Go to: https://1.1.1.1/");
                        Log("2. Download 'WARP for Windows'");
                        Log("3. Install and restart this app");
                        Log("");

                        if (options.EnableFiveM)
                        {
                            Log("⚠ [IMPORTANT] FiveM REQUIRES WARP to connect to certain servers in Egypt.");
                            Log("For a full connection, WARP is more reliable.");
                            Log("Switch to 'Low Ping Mode' if you only need Auth/Discord.");
                            Log("");
                        }

                        Log("Trying alternative method (GoodbyeDPI)...");
                        
                        // Try GoodbyeDPI as fallback
                        bool dpiStarted = await _dpiService.StartAsync();
                        if (dpiStarted)
                        {
                            anySuccess = true;
                            Log("✓ GoodbyeDPI started as fallback");
                        }
                    }
                }
                
                // STEP 2: DNS Resolution and Hosts file (supplementary)
                Log("");
                Log("══════════════════════════════════════");
                Log("   STEP 2: DNS BYPASS");
                Log("══════════════════════════════════════");
                
                var domains = new HashSet<string>();
                
                if (options.EnableDiscord)
                {
                    foreach (var d in DnsService.GetDiscordDomains())
                        domains.Add(d);
                }
                
                if (options.EnableFiveM)
                {
                    foreach (var d in DnsService.GetFiveMDomains())
                        domains.Add(d);
                }
                
                if (options.EnableValorant)
                {
                    foreach (var d in DnsService.GetValorantDomains())
                        domains.Add(d);
                }
                
                if (options.EnableLeague)
                {
                    foreach (var d in DnsService.GetLeagueDomains())
                        domains.Add(d);
                }
                
                Log($"Resolving {domains.Count} domains via DoH...");
                
                var domainIpMappings = new Dictionary<string, string>();
                foreach (var domain in domains)
                {
                    try
                    {
                        var ips = await _dnsService.ResolveAsync(domain);
                        if (ips.Count > 0)
                        {
                            domainIpMappings[domain] = ips[0];
                        }
                    }
                    catch { }
                }
                
                Log($"✓ Resolved {domainIpMappings.Count} domains");
                
                // Update hosts file
                if (domainIpMappings.Count > 0)
                {
                    bool hostsSuccess = await _hostsService.AddEntriesAsync(domainIpMappings);
                    if (hostsSuccess)
                    {
                        await _hostsService.FlushDnsAsync();
                        anySuccess = true;
                    }
                }
                
                _isEnabled = true;
                OnStatusChanged?.Invoke(true);
                
                Log("");
                Log("══════════════════════════════════════");
                Log("   BYPASS STATUS");
                Log("══════════════════════════════════════");
                
                if (_warpService.IsWarpInstalled && await _warpService.IsConnectedAsync())
                {
                    Log("✓ WARP: CONNECTED");
                    if (options.EnableFiveM)
                    {
                        Log("✓ FiveM: Connection fix active via WARP");
                    }
                }
                else if (_dpiService.IsRunning)
                {
                    Log("✓ DPI Bypass: ACTIVE");
                    if (options.EnableFiveM)
                    {
                        Log("⚠ FiveM: Connection may fail without WARP (UDP block)");
                    }
                }
                else
                {
                    Log("⚠ No VPN/DPI bypass active");
                    Log("  Discord may not work!");
                    if (options.EnableFiveM)
                    {
                        Log("  FiveM will NOT work!");
                    }
                    Log("  Please install Cloudflare WARP");
                }
                
                Log("✓ DNS Bypass: ACTIVE");
                Log("══════════════════════════════════════");
                Log("");
                Log("Try opening Discord now!");
                
                return anySuccess;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disables the bypass and restores original settings
        /// </summary>
        public async Task<bool> DisableBypassAsync()
        {
            try
            {
                Log("Disabling bypass...");
                
                // Disconnect WARP if connected
                if (_warpService.IsWarpInstalled)
                {
                    await _warpService.DisconnectAsync();
                }
                
                // Stop DPI bypass
                _dpiService.Stop();
                
                // Remove hosts entries
                await _hostsService.RemoveEntriesAsync();
                await _hostsService.FlushDnsAsync();
                _dnsService.ClearCache();
                
                _isEnabled = false;
                OnStatusChanged?.Invoke(false);
                Log("✓ Bypass disabled successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if bypass is currently active
        /// </summary>
        public async Task<bool> CheckStatusAsync()
        {
            bool warpConnected = _warpService.IsWarpInstalled && await _warpService.IsConnectedAsync();
            bool hostsActive = await _hostsService.HasBypassEntriesAsync();
            return warpConnected || hostsActive || _dpiService.IsRunning;
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
