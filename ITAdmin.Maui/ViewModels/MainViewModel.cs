using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITAdmin.Maui.Services;
using ITAdmin.Shared.Data;
using ITAdmin.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.ObjectModel;

namespace ITAdmin.Maui.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly ISystemChecker _systemChecker;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string welcomeMessage = "Welcome!";

    [ObservableProperty]
    private string bluetoothStatus = "Checking...";

    [ObservableProperty]
    private Color bluetoothColor = Colors.Gray;

    [ObservableProperty]
    private string usbStatus = "Checking...";

    [ObservableProperty]
    private Color usbColor = Colors.Gray;

    [ObservableProperty]
    private string firewallStatus = "Checking...";

    [ObservableProperty]
    private Color firewallColor = Colors.Gray;

    [ObservableProperty]
    private string lastCheckTime = "Last check: Never";

    [ObservableProperty]
    private bool isChecking;

    [ObservableProperty]
    private ObservableCollection<SystemCheckViewModel> checkHistory = new();

    partial void OnUsernameChanged(string value)
    {
        WelcomeMessage = $"Welcome, {value}!";
    }

    public MainViewModel(ISystemChecker systemChecker)
    {
        _systemChecker = systemChecker;
        _logger = Log.ForContext<MainViewModel>();
    }

    [RelayCommand]
    private async Task StartMonitoringAsync()
    {
        _logger.Information("Starting system monitoring for user: {Username}", Username);

        // Load initial data
        await LoadHistoryAsync();
        await PerformSystemCheckAsync();

        // Start periodic monitoring
        _cancellationTokenSource = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await PerformSystemCheckAsync();
                    });
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Monitoring stopped");
            }
        });
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        _logger.Information("Stopping system monitoring");
        _cancellationTokenSource?.Cancel();
        _timer?.Dispose();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _logger.Information("Manual refresh requested");
        await PerformSystemCheckAsync();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            _logger.Information("Logout requested for user: {Username}", Username);

            StopMonitoring();

            using var context = new AppDbContext();
            var user = await context.UserLogins
                .FirstOrDefaultAsync(u => u.Username == Username);

            if (user != null)
            {
                user.IsLoggedIn = false;
                await context.SaveChangesAsync();
            }

            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during logout");
            await Application.Current!.MainPage!.DisplayAlert("Error",
                $"Logout failed: {ex.Message}", "OK");
        }
    }

    private async Task PerformSystemCheckAsync()
    {
        IsChecking = true;

        try
        {
            _logger.Debug("Performing system check");

            // Check system status
            var bluetoothEnabled = await _systemChecker.IsBluetoothEnabledAsync();
            var usbEnabled = await _systemChecker.IsUsbEnabledAsync();
            var firewallEnabled = await _systemChecker.IsFirewallEnabledAsync();

            // Update UI
            BluetoothStatus = bluetoothEnabled ? "ON" : "OFF";
            BluetoothColor = bluetoothEnabled ? Colors.Green : Colors.Red;

            UsbStatus = usbEnabled ? "Enabled" : "Disabled";
            UsbColor = usbEnabled ? Colors.Green : Colors.Red;

            FirewallStatus = firewallEnabled ? "ON" : "OFF";
            FirewallColor = firewallEnabled ? Colors.Green : Colors.Red;

            LastCheckTime = $"Last check: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            // Save to database
            using var context = new AppDbContext();
            var checkResult = new SystemCheckResult
            {
                CheckTime = DateTime.Now,
                BluetoothEnabled = bluetoothEnabled,
                UsbEnabled = usbEnabled,
                FirewallEnabled = firewallEnabled,
                Notes = $"Check performed for user: {Username}"
            };

            context.SystemCheckResults.Add(checkResult);
            await context.SaveChangesAsync();

            // Add to history
            CheckHistory.Insert(0, new SystemCheckViewModel(checkResult));

            // Keep only last 50 items
            while (CheckHistory.Count > 50)
            {
                CheckHistory.RemoveAt(CheckHistory.Count - 1);
            }

            _logger.Information(
                "System check completed - Bluetooth: {Bluetooth}, USB: {USB}, Firewall: {Firewall}",
                bluetoothEnabled ? "ON" : "OFF",
                usbEnabled ? "Enabled" : "Disabled",
                firewallEnabled ? "ON" : "OFF");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during system check");
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            using var context = new AppDbContext();
            var history = await context.SystemCheckResults
                .OrderByDescending(s => s.CheckTime)
                .Take(50)
                .ToListAsync();

            CheckHistory.Clear();
            foreach (var item in history)
            {
                CheckHistory.Add(new SystemCheckViewModel(item));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading history");
        }
    }
}