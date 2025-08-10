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

        private string? FindBlueutilPath()
        {
            string[] possiblePaths =
            {
                "/usr/local/bin/blueutil",
                "/opt/homebrew/bin/blueutil",
                "/usr/bin/blueutil"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        public async Task<bool> IsBluetoothEnabledAsync()
        {
            try
            {
                _logger.Debug("Checking Bluetooth status on macOS");

                // Try using blueutil if available
                var blueutilPath = FindBlueutilPath();
                if (!string.IsNullOrEmpty(blueutilPath))
                {
                    var blueutilOutput = await RunCommandAsync("/bin/bash", $"-c \"'{blueutilPath}' -p\"");
                    if (!string.IsNullOrEmpty(blueutilOutput))
                    {
                        var isEnabled = blueutilOutput.Trim() == "1";
                        _logger.Information("Bluetooth is {Status} (via blueutil)", isEnabled ? "enabled" : "disabled");
                        return isEnabled;
                    }
                }

                // Fallback to ioreg
                var ioregResult = await RunCommandAsync("/bin/bash", "-c \"/usr/sbin/ioreg -r -k State -n IOBluetoothHCIController\"");
                if (!string.IsNullOrEmpty(ioregResult))
                {
                    if (ioregResult.Contains("\"State\" = 1") || ioregResult.Contains("State = 1"))
                    {
                        _logger.Information("Bluetooth is enabled (via ioreg)");
                        return true;
                    }
                    else if (ioregResult.Contains("\"State\" = 0") || ioregResult.Contains("State = 0"))
                    {
                        _logger.Information("Bluetooth is disabled (via ioreg)");
                        return false;
                    }
                }

                // Final fallback to system_profiler
                var result = await RunCommandAsync("/bin/bash", "-c \"/usr/sbin/system_profiler SPBluetoothDataType\"");
                _logger.Debug("system_profiler output: {Output}", result);

                if (result.Contains("State: On", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("Bluetooth Power: On", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("Bluetooth is enabled (via system_profiler)");
                    return true;
                }

                _logger.Information("Bluetooth appears to be disabled");
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

                var result = await RunCommandAsync("/bin/bash", "-c \"/usr/sbin/system_profiler SPUSBDataType\"");
                if (result.Contains("USB Bus", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("USB Controller", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("USB is enabled");
                    return true;
                }

                var ioregResult = await RunCommandAsync("/bin/bash", "-c \"/usr/sbin/ioreg -p IOUSB\"");
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
                return true; // Assume enabled if uncertain
            }
        }

        public async Task<bool> IsFirewallEnabledAsync()
        {
            try
            {
                _logger.Debug("Checking Firewall status on macOS");

                var result = await RunCommandAsync("/bin/bash", "-c \"/usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate\"");
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

                var defaultsResult = await RunCommandAsync("/bin/bash", "-c \"/usr/bin/defaults read /Library/Preferences/com.apple.alf globalstate\"");
                if (!string.IsNullOrEmpty(defaultsResult))
                {
                    var state = defaultsResult.Trim();
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
                _logger.Error(ex, "Failed to run command {Command} {Arguments}", command, arguments);
                return string.Empty;
            }
        }
    }
}
