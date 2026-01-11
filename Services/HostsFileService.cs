using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBypass.Services
{
    /// <summary>
    /// Service for managing Windows hosts file
    /// </summary>
    public class HostsFileService
    {
        private const string HostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
        private const string BackupPath = @"C:\Windows\System32\drivers\etc\hosts.discord-bypass.bak";
        private const string MarkerStart = "# === DISCORD BYPASS START ===";
        private const string MarkerEnd = "# === DISCORD BYPASS END ===";
        
        public event Action<string>? OnLogMessage;

        /// <summary>
        /// Adds domain-to-IP mappings to the hosts file
        /// </summary>
        public async Task<bool> AddEntriesAsync(Dictionary<string, string> domainIpMappings)
        {
            try
            {
                // Read current hosts file
                string currentContent = await File.ReadAllTextAsync(HostsFilePath);
                
                // Create backup if it doesn't exist
                if (!File.Exists(BackupPath))
                {
                    await File.WriteAllTextAsync(BackupPath, currentContent);
                    Log("Created hosts file backup");
                }
                
                // Remove any existing bypass entries
                currentContent = RemoveBypassEntries(currentContent);
                
                // Build new entries
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine(MarkerStart);
                sb.AppendLine("# Added by Discord Bypass application");
                sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                foreach (var mapping in domainIpMappings)
                {
                    sb.AppendLine($"{mapping.Value}\t{mapping.Key}");
                }
                
                sb.AppendLine(MarkerEnd);
                
                // Append new entries
                string newContent = currentContent.TrimEnd() + sb.ToString();
                
                // Write back to hosts file
                await File.WriteAllTextAsync(HostsFilePath, newContent);
                
                Log($"Added {domainIpMappings.Count} entries to hosts file");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Log("ERROR: Access denied. Please run as Administrator.");
                return false;
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to modify hosts file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes all bypass entries from the hosts file
        /// </summary>
        public async Task<bool> RemoveEntriesAsync()
        {
            try
            {
                string currentContent = await File.ReadAllTextAsync(HostsFilePath);
                string cleanedContent = RemoveBypassEntries(currentContent);
                
                await File.WriteAllTextAsync(HostsFilePath, cleanedContent);
                
                Log("Removed bypass entries from hosts file");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Log("ERROR: Access denied. Please run as Administrator.");
                return false;
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to clean hosts file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores the original hosts file from backup
        /// </summary>
        public async Task<bool> RestoreBackupAsync()
        {
            try
            {
                if (File.Exists(BackupPath))
                {
                    string backupContent = await File.ReadAllTextAsync(BackupPath);
                    await File.WriteAllTextAsync(HostsFilePath, backupContent);
                    Log("Restored hosts file from backup");
                    return true;
                }
                else
                {
                    Log("No backup file found");
                    return await RemoveEntriesAsync();
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to restore backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if bypass entries exist in hosts file
        /// </summary>
        public async Task<bool> HasBypassEntriesAsync()
        {
            try
            {
                string content = await File.ReadAllTextAsync(HostsFilePath);
                return content.Contains(MarkerStart);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes DNS cache after modifying hosts file
        /// </summary>
        public async Task FlushDnsAsync()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync();
                
                Log("DNS cache flushed");
            }
            catch (Exception ex)
            {
                Log($"Warning: Could not flush DNS cache: {ex.Message}");
            }
        }

        private string RemoveBypassEntries(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();
            bool inBypassSection = false;
            
            foreach (var line in lines)
            {
                if (line.Trim() == MarkerStart)
                {
                    inBypassSection = true;
                    continue;
                }
                
                if (line.Trim() == MarkerEnd)
                {
                    inBypassSection = false;
                    continue;
                }
                
                if (!inBypassSection)
                {
                    result.Add(line);
                }
            }
            
            // Remove trailing empty lines and add one
            while (result.Count > 0 && string.IsNullOrWhiteSpace(result[result.Count - 1]))
            {
                result.RemoveAt(result.Count - 1);
            }
            
            return string.Join(Environment.NewLine, result);
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
