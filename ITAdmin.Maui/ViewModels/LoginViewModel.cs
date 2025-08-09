using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITAdmin.Maui.Views;
using ITAdmin.Shared.Data;
using ITAdmin.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;

namespace ITAdmin.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ILogger _logger;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private Color statusColor = Colors.Blue;

    [ObservableProperty]
    private bool isStatusVisible;

    [ObservableProperty]
    private bool isBusy;

    public LoginViewModel()
    {
        _logger = Log.ForContext<LoginViewModel>();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowStatusAsync("Please enter username and password.", Colors.Red);
            return;
        }

        IsBusy = true;

        try
        {
            _logger.Information("Login attempt for user: {Username}", Username);

            using var context = new AppDbContext();
            var user = await context.UserLogins
                .FirstOrDefaultAsync(u => u.Username == Username.Trim());

            if (user == null)
            {
                await ShowStatusAsync("User not found.", Colors.Red);
                _logger.Warning("Login failed - user not found: {Username}", Username);
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                await ShowStatusAsync("Invalid password.", Colors.Red);
                _logger.Warning("Login failed - invalid password for user: {Username}", Username);
                return;
            }

            // Update login status
            user.IsLoggedIn = true;
            user.LastLoginTime = DateTime.Now;
            await context.SaveChangesAsync();

            await ShowStatusAsync("Login successful!", Colors.Green);
            _logger.Information("User logged in successfully: {Username}", Username);

            // Navigate to main page
            await Shell.Current.GoToAsync($"MainPage?username={Uri.EscapeDataString(Username)}");

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Login error for user: {Username}", Username);
            await ShowStatusAsync($"Error: {ex.Message}", Colors.Red);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowStatusAsync("Please enter username and password.", Colors.Red);
            return;
        }

        if (Password.Length < 6)
        {
            await ShowStatusAsync("Password must be at least 6 characters.", Colors.Red);
            return;
        }

        IsBusy = true;

        try
        {
            _logger.Information("Registration attempt for user: {Username}", Username);

            using var context = new AppDbContext();

            if (await context.UserLogins.AnyAsync(u => u.Username == Username.Trim()))
            {
                await ShowStatusAsync("Username already exists.", Colors.Red);
                _logger.Warning("Registration failed - username exists: {Username}", Username);
                return;
            }

            var newUser = new UserLogin
            {
                Username = Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                IsLoggedIn = false,
                CreatedAt = DateTime.Now
            };

            context.UserLogins.Add(newUser);
            await context.SaveChangesAsync();

            await ShowStatusAsync("Registration successful! You can now login.", Colors.Green);
            _logger.Information("User registered successfully: {Username}", Username);

            Password = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Registration error for user: {Username}", Username);
            await ShowStatusAsync($"Error: {ex.Message}", Colors.Red);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowStatusAsync(string message, Color color)
    {
        StatusMessage = message;
        StatusColor = color;
        IsStatusVisible = true;

        // Hide status after 3 seconds
        await Task.Delay(3000);
        IsStatusVisible = false;
    }
}