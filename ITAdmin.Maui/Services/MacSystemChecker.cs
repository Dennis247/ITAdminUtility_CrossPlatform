using System.Diagnostics;
using Serilog;

namespace ITAdmin.Maui.Services
{
    public class MacSystemChecker : ISystemChecker
    {
        private readonly ILogger _logger;

        public MacSystemChecker()
        {
            _logger = Log.ForContext<MacSystemChecker>();
        }

        public async Task<bool> IsBluetoothEnabledAsync()
        {
            try
            {
                _logger.Debug("Checking Bluetooth status on macOS");

                // Use blueutil if available, otherwise use system_profiler
                var blueutil = await RunCommandAsync("/usr/local/bin/blueutil", "-p");
                if (!string.IsNullOrEmpty(blueutil) && blueutil.Trim() == "1")
                {
                    _logger.Information("Bluetooth is enabled (via blueutil)");
                    return true;
                }

                // Fallback to system_profiler
                var result = await RunCommandAsync("/usr/sbin/system_profiler", "SPBluetoothDataType");

                if (result.Contains("Bluetooth Power: On", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("Bluetooth is enabled");
                    return true;
                }

                _logger.Information("Bluetooth is disabled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking Bluetooth status");
                return false;
            }
        }

        public async Task<bool> IsUsbEnabledAsync()
        {
            try
            {
                _logger.Debug("Checking USB status on macOS");

                // Check USB devices using system_profiler
                var result = await RunCommandAsync("/usr/sbin/system_profiler", "SPUSBDataType");

                // If we can enumerate USB devices, USB is enabled
                // Look for USB controller information
                if (result.Contains("USB Bus", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("USB Controller", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("USB is enabled");
                    return true;
                }

                // Additional check: see if any USB devices are connected
                var ioregResult = await RunCommandAsync("/usr/sbin/ioreg", "-p IOUSB");
                if (!string.IsNullOrEmpty(ioregResult) && ioregResult.Contains("+-o"))
                {
                    _logger.Information("USB is enabled (devices detected)");
                    return true;
                }

                _logger.Warning("USB appears to be disabled or no devices found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking USB status");
                // Assume USB is enabled if we can't check
                return true;
            }
        }

        public async Task<bool> IsFirewallEnabledAsync()
        {
            try
            {
                _logger.Debug("Checking Firewall status on macOS");

                // Check application firewall status
                var result = await RunCommandAsync("/usr/libexec/ApplicationFirewall/socketfilterfw", "--getglobalstate");

                if (result.Contains("enabled", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("Firewall is enabled");
                    return true;
                }
                else if (result.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("Firewall is disabled");
                    return false;
                }

                // Alternative check using defaults
                var defaultsResult = await RunCommandAsync("/usr/bin/defaults", "read /Library/Preferences/com.apple.alf globalstate");
                if (!string.IsNullOrEmpty(defaultsResult))
                {
                    var state = defaultsResult.Trim();
                    // 0 = off, 1 = on for specific services, 2 = on for essential services
                    var isEnabled = state == "1" || state == "2";
                    _logger.Information("Firewall is {Status} (state: {State})",
                        isEnabled ? "enabled" : "disabled", state);
                    return isEnabled;
                }

                _logger.Warning("Could not determine firewall status");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking Firewall status");
                return false;
            }
        }

        private async Task<string> RunCommandAsync(string command, string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.Warning("Command {Command} {Arguments} returned error: {Error}",
                        command, arguments, error);
                }

                _logger.Debug("Command {Command} {Arguments} output: {Output}",
                    command, arguments, output.Trim());

                return output;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to run command {Command} {Arguments}",
                    command, arguments);
                return string.Empty;
            }
        }
    }
}