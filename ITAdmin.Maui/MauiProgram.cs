using ITAdmin.Maui.Services;
using ITAdmin.Maui.ViewModels;
using ITAdmin.Maui.Views;
using ITAdmin.Shared.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace ITAdmin.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Configure Serilog
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".itadminutility",
            "logs",
            "maui-log-.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Add Serilog
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Register Services
        builder.Services.AddSingleton<ISystemChecker, MacSystemChecker>();

        // Register ViewModels
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<MainViewModel>();

        // Register Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();

        // Initialize database
        DatabaseService.InitializeDatabase();

        // Log application startup
        Log.Information("IT Admin Utility starting up...");
        Log.Information("Log file location: {LogPath}", logPath);

        return builder.Build();
    }
}