using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBypass.Services
{
    /// <summary>
    /// DNS-over-HTTPS service to bypass ISP DNS blocking
    /// </summary>
    public class DnsService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, List<string>> _dnsCache;
        
        // DNS-over-HTTPS endpoints
        private const string CloudflareDoh = "https://cloudflare-dns.com/dns-query";
        private const string GoogleDoh = "https://dns.google/resolve";
        
        public event Action<string>? OnLogMessage;

        public DnsService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/dns-json");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _dnsCache = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Resolves a domain to IP addresses using DNS-over-HTTPS
        /// </summary>
        public async Task<List<string>> ResolveAsync(string domain)
        {
            // Check cache first
            if (_dnsCache.TryGetValue(domain, out var cachedIps))
            {
                Log($"Using cached IPs for {domain}");
                return cachedIps;
            }

            List<string> ips = new();
            
            // Try Cloudflare first
            try
            {
                ips = await ResolveWithCloudflareAsync(domain);
                if (ips.Count > 0)
                {
                    _dnsCache[domain] = ips;
                    return ips;
                }
            }
            catch (Exception ex)
            {
                Log($"Cloudflare DNS failed for {domain}: {ex.Message}");
            }

            // Fallback to Google
            try
            {
                ips = await ResolveWithGoogleAsync(domain);
                if (ips.Count > 0)
                {
                    _dnsCache[domain] = ips;
                    return ips;
                }
            }
            catch (Exception ex)
            {
                Log($"Google DNS failed for {domain}: {ex.Message}");
            }

            return ips;
        }

        private async Task<List<string>> ResolveWithCloudflareAsync(string domain)
        {
            var url = $"{CloudflareDoh}?name={domain}&type=A";
            var response = await _httpClient.GetStringAsync(url);
            return ParseDnsResponse(response);
        }

        private async Task<List<string>> ResolveWithGoogleAsync(string domain)
        {
            var url = $"{GoogleDoh}?name={domain}&type=A";
            var response = await _httpClient.GetStringAsync(url);
            return ParseDnsResponse(response);
        }

        private List<string> ParseDnsResponse(string jsonResponse)
        {
            var ips = new List<string>();
            
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("Answer", out var answers))
                {
                    foreach (var answer in answers.EnumerateArray())
                    {
                        // Type 1 = A record (IPv4)
                        if (answer.TryGetProperty("type", out var type) && type.GetInt32() == 1)
                        {
                            if (answer.TryGetProperty("data", out var data))
                            {
                                ips.Add(data.GetString() ?? "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to parse DNS response: {ex.Message}");
            }
            
            return ips;
        }

        /// <summary>
        /// Get all Discord-related domains that need to be resolved
        /// </summary>
        public static List<string> GetDiscordDomains()
        {
            return new List<string>
            {
                // Core Discord domains
                "discord.com",
                "www.discord.com",
                "discordapp.com",
                "www.discordapp.com",
                "discord.gg",
                
                // Discord CDN and media
                "cdn.discordapp.com",
                "media.discordapp.net",
                "images-ext-1.discordapp.net",
                "images-ext-2.discordapp.net",
                
                // Discord Gateway (voice/chat)
                "gateway.discord.gg",
                "discord.media",
                
                // Discord API
                "discordapp.net",
                "discord.co",
                "discordstatus.com",
                
                // Discord PTB and Canary
                "ptb.discord.com",
                "canary.discord.com",
                "ptb.discordapp.com",
                "canary.discordapp.com"
            };
        }

        /// <summary>
        /// Get FiveM OAuth domains
        /// </summary>
        public static List<string> GetFiveMDomains()
        {
            return new List<string>
            {
                // FiveM uses Discord OAuth
                "discord.com",
                "discordapp.com",
                "cdn.discordapp.com",
                
                // Additional FiveM-specific (if any custom auth)
                "keymaster.fivem.net",
                "runtime.fivem.net",
                "cfx.re"
            };
        }

        /// <summary>
        /// Get Valorant VOIP domains (uses Discord-like infrastructure)
        /// </summary>
        public static List<string> GetValorantDomains()
        {
            return new List<string>
            {
                // Riot voice chat servers
                "voice.riotgames.com",
                "vcs.si.riotgames.com"
            };
        }

        /// <summary>
        /// Get League of Legends VOIP domains
        /// </summary>
        public static List<string> GetLeagueDomains()
        {
            return new List<string>
            {
                // League voice chat
                "lolvcs.riotgames.com"
            };
        }

        public void ClearCache()
        {
            _dnsCache.Clear();
            Log("DNS cache cleared");
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
