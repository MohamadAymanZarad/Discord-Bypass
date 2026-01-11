using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DiscordBypass.Services;
using DiscordBypass.Helpers;

namespace DiscordBypass
{
    public partial class MainWindow : Window
    {
        private readonly BypassManager _bypassManager;
        private readonly StartupHelper _startupHelper;
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            
            _bypassManager = new BypassManager();
            _startupHelper = new StartupHelper();
            
            // Subscribe to bypass manager events
            _bypassManager.OnLogMessage += AddLogMessage;
            _bypassManager.OnStatusChanged += UpdateConnectionStatus;
            
            // Load saved settings
            LoadSettings();
            
            // Initial log
            AddLogMessage("Application started. Ready to bypass Discord blocking.");
        }

        private void LoadSettings()
        {
            try
            {
                AutoStartToggle.IsChecked = _startupHelper.IsStartupEnabled();
            }
            catch
            {
                AutoStartToggle.IsChecked = false;
            }
        }

        #region Window Chrome Events
        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (MinimizeToTrayToggle.IsChecked == true)
            {
                Hide();
                TrayIcon.ShowBalloonTip("Discord Bypass", "Application minimized to system tray", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (MinimizeToTrayToggle.IsChecked == true)
            {
                Hide();
                TrayIcon.ShowBalloonTip("Discord Bypass", "Running in background. Right-click tray icon to exit.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
            else
            {
                ExitApplication();
            }
        }

        #endregion

        #region Main Toggle Events

        private async void MainToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (MainToggle.IsChecked == true)
            {
                await EnableBypass();
            }
            else
            {
                await DisableBypass();
            }
        }

        private async System.Threading.Tasks.Task EnableBypass()
        {
            try
            {
                MainToggle.IsEnabled = false;
                ApplyButton.IsEnabled = false;
                
                AddLogMessage("Enabling bypass...");
                
                var options = new BypassOptions
                {
                    EnableDpiBypass = DpiBypassToggle.IsChecked == true,
                    EnableDiscord = DiscordToggle.IsChecked == true,
                    EnableFiveM = FiveMToggle.IsChecked == true,
                    EnableValorant = ValorantToggle.IsChecked == true,
                    EnableLeague = LeagueToggle.IsChecked == true
                };
                
                bool success = await _bypassManager.EnableBypassAsync(options);
                
                if (success)
                {
                    _isConnected = true;
                    UpdateUI(true);
                    AddLogMessage("✓ Bypass enabled successfully!");
                }
                else
                {
                    MainToggle.IsChecked = false;
                    AddLogMessage("✗ Failed to enable bypass. Check logs for details.");
                }
            }
            catch (Exception ex)
            {
                MainToggle.IsChecked = false;
                AddLogMessage($"✗ Error: {ex.Message}");
            }
            finally
            {
                MainToggle.IsEnabled = true;
                ApplyButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task DisableBypass()
        {
            try
            {
                MainToggle.IsEnabled = false;
                ApplyButton.IsEnabled = false;
                
                AddLogMessage("Disabling bypass...");
                
                bool success = await _bypassManager.DisableBypassAsync();
                
                if (success)
                {
                    _isConnected = false;
                    UpdateUI(false);
                    AddLogMessage("✓ Bypass disabled. Hosts file restored.");
                }
                else
                {
                    AddLogMessage("⚠ Warning: Could not fully restore hosts file.");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"✗ Error: {ex.Message}");
            }
            finally
            {
                MainToggle.IsEnabled = true;
                ApplyButton.IsEnabled = true;
            }
        }

        #endregion

        #region Button Events

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                // Re-apply with new settings
                await DisableBypass();
                MainToggle.IsChecked = true;
            }
            else
            {
                MainToggle.IsChecked = true;
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                MainToggle.IsChecked = false;
            }
            
            // Reset all toggles to default
            DpiBypassToggle.IsChecked = true;
            DiscordToggle.IsChecked = true;
            FiveMToggle.IsChecked = true;
            ValorantToggle.IsChecked = true;
            LeagueToggle.IsChecked = true;
            MinimizeToTrayToggle.IsChecked = true;
            
            AddLogMessage("Settings reset to defaults.");
        }

        private void AutoStartToggle_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AutoStartToggle.IsChecked == true)
                {
                    _startupHelper.EnableStartup();
                    AddLogMessage("Auto-start enabled.");
                }
                else
                {
                    _startupHelper.DisableStartup();
                    AddLogMessage("Auto-start disabled.");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"⚠ Could not change startup setting: {ex.Message}");
                AutoStartToggle.IsChecked = !AutoStartToggle.IsChecked;
            }
        }

        #endregion

        #region System Tray Events

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ToggleBypass_Click(object sender, RoutedEventArgs e)
        {
            MainToggle.IsChecked = !MainToggle.IsChecked;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private async void ExitApplication()
        {
            // Disable bypass before exiting
            if (_isConnected)
            {
                AddLogMessage("Cleaning up before exit...");
                await _bypassManager.DisableBypassAsync();
            }
            
            TrayIcon.Dispose();
            Application.Current.Shutdown();
        }

        #endregion

        #region UI Updates

        private void UpdateUI(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                if (connected)
                {
                    StatusText.Text = "CONNECTED";
                    StatusText.Foreground = FindResource("SuccessBrush") as Brush;
                    StatusSubtext.Text = "Discord bypass is active";
                    StatusIndicator.Background = FindResource("SuccessBrush") as Brush;
                }
                else
                {
                    StatusText.Text = "DISCONNECTED";
                    StatusText.Foreground = FindResource("TextMuted") as Brush;
                    StatusSubtext.Text = "Discord bypass is disabled";
                    StatusIndicator.Background = FindResource("BackgroundTertiary") as Brush;
                }
            });
        }

        private void UpdateConnectionStatus(bool connected)
        {
            Dispatcher.Invoke(() => UpdateUI(connected));
        }

        private void AddLogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogListBox.Items.Add($"[{timestamp}] {message}");
                
                // Auto-scroll to bottom
                if (LogListBox.Items.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
                
                // Limit log entries
                while (LogListBox.Items.Count > 100)
                {
                    LogListBox.Items.RemoveAt(0);
                }
            });
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            TrayIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
