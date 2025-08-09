using System.Diagnostics;
using System.Text.Json;
using Serilog;

namespace MacSystemDetector
{
    public class Program
    {
        public static readonly Dictionary<string, bool> Results = new();

        static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("system-detector.log",
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Console.Clear();
                WriteHeader();

                var detector = new MacSystemDetector();

                // Run all detections
                await detector.RunAllDetectionsAsync();

                // Display summary
                WriteSummary();

                // Interactive mode
                await RunInteractiveMode(detector);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application crashed");
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
            }
            finally
            {
                Log.CloseAndFlush();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        static void WriteHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   macOS System Status Detector              ║");
            Console.WriteLine("║                      Native Console App                     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
            Console.WriteLine($"Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine($"Machine: {Environment.MachineName}");
            Console.WriteLine($"User: {Environment.UserName}");
            Console.WriteLine();
        }

        static void WriteSummary()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         SUMMARY                             ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            foreach (var result in Results)
            {
                var status = result.Value ? "✅ ENABLED" : "❌ DISABLED";
                var color = result.Value ? ConsoleColor.Green : ConsoleColor.Red;

                Console.ForegroundColor = color;
                Console.WriteLine($"{result.Key.PadRight(20)}: {status}");
                Console.ResetColor();
            }
        }

        static async Task RunInteractiveMode(MacSystemDetector detector)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    INTERACTIVE MODE                         ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Commands:");
                Console.WriteLine("  1 - Test Bluetooth");
                Console.WriteLine("  2 - Test USB");
                Console.WriteLine("  3 - Test Firewall");
                Console.WriteLine("  4 - Test Network");
                Console.WriteLine("  5 - Test All");
                Console.WriteLine("  6 - Run Command");
                Console.WriteLine("  7 - System Info");
                Console.WriteLine("  q - Quit");
                Console.Write("\nEnter command: ");

                var input = Console.ReadLine()?.Trim().ToLower();

                switch (input)
                {
                    case "1":
                        await detector.TestBluetoothAsync();
                        break;
                    case "2":
                        await detector.TestUsbAsync();
                        break;
                    case "3":
                        await detector.TestFirewallAsync();
                        break;
                    case "4":
                        await detector.TestNetworkAsync();
                        break;
                    case "5":
                        await detector.RunAllDetectionsAsync();
                        break;
                    case "6":
                        await RunCustomCommand();
                        break;
                    case "7":
                        await detector.ShowSystemInfoAsync();
                        break;
                    case "q":
                    case "quit":
                    case "exit":
                        return;
                    default:
                        Console.WriteLine("Invalid command. Try again.");
                        break;
                }
            }
        }

        static async Task RunCustomCommand()
        {
            Console.Write("Enter command: ");
            var command = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(command)) return;

            Console.Write("Enter arguments (optional): ");
            var args = Console.ReadLine()?.Trim() ?? "";

            Console.WriteLine($"\nExecuting: {command} {args}");
            Console.WriteLine("─".PadRight(60, '─'));

            var result = await ExecuteCommandAsync(command, args);
            Console.WriteLine($"Exit Code: {result.ExitCode}");
            if (!string.IsNullOrEmpty(result.Output))
            {
                Console.WriteLine($"Output:\n{result.Output}");
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error:\n{result.Error}");
                Console.ResetColor();
            }
        }

        static async Task<(int ExitCode, string Output, string Error)> ExecuteCommandAsync(string command, string arguments)
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

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode, output, error);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.Message);
            }
        }
    }

    public class MacSystemDetector
    {
        private readonly ILogger _logger;

        public MacSystemDetector()
        {
            _logger = Log.ForContext<MacSystemDetector>();
        }

        public async Task RunAllDetectionsAsync()
        {
            Console.WriteLine("Running comprehensive system detection...\n");

            await TestBluetoothAsync();
            await TestUsbAsync();
            await TestFirewallAsync();
            await TestNetworkAsync();
        }

        public async Task TestBluetoothAsync()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("🔵 BLUETOOTH DETECTION");
            Console.WriteLine("─".PadRight(40, '─'));
            Console.ResetColor();

            bool isEnabled = false;
            var methods = new List<(string Name, Func<Task<bool?>> Test)>
            {
                ("blueutil", TestBlueUtilAsync),
                ("ioreg", TestIoregBluetoothAsync),
                ("system_profiler", TestSystemProfilerBluetoothAsync),
                ("defaults", TestDefaultsBluetoothAsync)
            };

            foreach (var (name, test) in methods)
            {
                try
                {
                    Console.Write($"  Testing {name}... ");
                    var result = await test();

                    if (result.HasValue)
                    {
                        var status = result.Value ? "✅ ENABLED" : "❌ DISABLED";
                        var color = result.Value ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine(status);
                        Console.ResetColor();

                        if (result.Value) isEnabled = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  UNKNOWN");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: {ex.Message}");
                    Console.ResetColor();
                    _logger.Error(ex, "Bluetooth test {Method} failed", name);
                }
            }

            Program.Results["Bluetooth"] = isEnabled;
            Console.WriteLine();
        }

        private async Task<bool?> TestBlueUtilAsync()
        {
            var paths = new[] { "/opt/homebrew/bin/blueutil", "/usr/local/bin/blueutil" };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var result = await ExecuteCommandAsync(path, "-p");
                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
                    {
                        var output = result.Output.Trim();
                        _logger.Debug("blueutil output: '{Output}'", output);
                        return output == "1";
                    }
                }
            }
            return null;
        }

        private async Task<bool?> TestIoregBluetoothAsync()
        {
            var result = await ExecuteCommandAsync("/usr/sbin/ioreg", "-r -k State -n IOBluetoothHCIController");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("ioreg bluetooth output length: {Length}", result.Output.Length);

                if (result.Output.Contains("\"State\" = 1") || result.Output.Contains("State = 1"))
                    return true;
                if (result.Output.Contains("\"State\" = 0") || result.Output.Contains("State = 0"))
                    return false;
            }
            return null;
        }

        private async Task<bool?> TestSystemProfilerBluetoothAsync()
        {
            var result = await ExecuteCommandAsync("/usr/sbin/system_profiler", "SPBluetoothDataType");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("system_profiler bluetooth output length: {Length}", result.Output.Length);

                return result.Output.Contains("State: On", StringComparison.OrdinalIgnoreCase) ||
                       result.Output.Contains("Bluetooth Power: On", StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        private async Task<bool?> TestDefaultsBluetoothAsync()
        {
            var result = await ExecuteCommandAsync("/usr/bin/defaults", "read /Library/Preferences/com.apple.Bluetooth ControllerPowerState");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                var output = result.Output.Trim();
                _logger.Debug("defaults bluetooth output: '{Output}'", output);
                return output == "1";
            }
            return null;
        }

        public async Task TestUsbAsync()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🟢 USB DETECTION");
            Console.WriteLine("─".PadRight(40, '─'));
            Console.ResetColor();

            bool isEnabled = false;
            var methods = new List<(string Name, Func<Task<bool?>> Test)>
            {
                ("system_profiler", TestSystemProfilerUsbAsync),
                ("ioreg", TestIoregUsbAsync),
                ("ls /dev", TestDevUsbAsync)
            };

            foreach (var (name, test) in methods)
            {
                try
                {
                    Console.Write($"  Testing {name}... ");
                    var result = await test();

                    if (result.HasValue)
                    {
                        var status = result.Value ? "✅ AVAILABLE" : "❌ NOT AVAILABLE";
                        var color = result.Value ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine(status);
                        Console.ResetColor();

                        if (result.Value) isEnabled = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  UNKNOWN");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: {ex.Message}");
                    Console.ResetColor();
                    _logger.Error(ex, "USB test {Method} failed", name);
                }
            }

            Program.Results["USB"] = isEnabled;
            Console.WriteLine();
        }

        private async Task<bool?> TestSystemProfilerUsbAsync()
        {
            var result = await ExecuteCommandAsync("/usr/sbin/system_profiler", "SPUSBDataType");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("system_profiler USB output length: {Length}", result.Output.Length);

                return result.Output.Contains("USB Bus", StringComparison.OrdinalIgnoreCase) ||
                       result.Output.Contains("USB Controller", StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        private async Task<bool?> TestIoregUsbAsync()
        {
            var result = await ExecuteCommandAsync("/usr/sbin/ioreg", "-p IOUSB");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("ioreg USB output length: {Length}", result.Output.Length);
                return result.Output.Contains("+-o");
            }
            return null;
        }

        private async Task<bool?> TestDevUsbAsync()
        {
            var result = await ExecuteCommandAsync("/bin/ls", "/dev/cu.*");
            if (result.ExitCode == 0)
            {
                _logger.Debug("ls /dev USB devices: {Output}", result.Output);
                return !string.IsNullOrEmpty(result.Output);
            }
            return null;
        }

        public async Task TestFirewallAsync()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("🔴 FIREWALL DETECTION");
            Console.WriteLine("─".PadRight(40, '─'));
            Console.ResetColor();

            bool isEnabled = false;
            var methods = new List<(string Name, Func<Task<bool?>> Test)>
            {
                ("socketfilterfw", TestSocketFilterfwAsync),
                ("defaults alf", TestDefaultsAlfAsync),
                ("pfctl", TestPfctlAsync)
            };

            foreach (var (name, test) in methods)
            {
                try
                {
                    Console.Write($"  Testing {name}... ");
                    var result = await test();

                    if (result.HasValue)
                    {
                        var status = result.Value ? "✅ ENABLED" : "❌ DISABLED";
                        var color = result.Value ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine(status);
                        Console.ResetColor();

                        if (result.Value) isEnabled = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  UNKNOWN");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: {ex.Message}");
                    Console.ResetColor();
                    _logger.Error(ex, "Firewall test {Method} failed", name);
                }
            }

            Program.Results["Firewall"] = isEnabled;
            Console.WriteLine();
        }

        private async Task<bool?> TestSocketFilterfwAsync()
        {
            var result = await ExecuteCommandAsync("/usr/libexec/ApplicationFirewall/socketfilterfw", "--getglobalstate");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("socketfilterfw output: '{Output}'", result.Output.Trim());
                return result.Output.Contains("enabled", StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        private async Task<bool?> TestDefaultsAlfAsync()
        {
            var result = await ExecuteCommandAsync("/usr/bin/defaults", "read /Library/Preferences/com.apple.alf globalstate");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                var state = result.Output.Trim();
                _logger.Debug("defaults alf output: '{Output}'", state);
                // 0 = off, 1 = on for specific services, 2 = on for essential services
                return state == "1" || state == "2";
            }
            return null;
        }

        private async Task<bool?> TestPfctlAsync()
        {
            var result = await ExecuteCommandAsync("/sbin/pfctl", "-s info");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("pfctl output length: {Length}", result.Output.Length);
                return result.Output.Contains("Status: Enabled", StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        public async Task TestNetworkAsync()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("🟣 NETWORK DETECTION");
            Console.WriteLine("─".PadRight(40, '─'));
            Console.ResetColor();

            bool isEnabled = false;
            var methods = new List<(string Name, Func<Task<bool?>> Test)>
            {
                ("networksetup", TestNetworkSetupAsync),
                ("ifconfig", TestIfconfigAsync),
                ("ping", TestPingAsync)
            };

            foreach (var (name, test) in methods)
            {
                try
                {
                    Console.Write($"  Testing {name}... ");
                    var result = await test();

                    if (result.HasValue)
                    {
                        var status = result.Value ? "✅ CONNECTED" : "❌ DISCONNECTED";
                        var color = result.Value ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine(status);
                        Console.ResetColor();

                        if (result.Value) isEnabled = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  UNKNOWN");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: {ex.Message}");
                    Console.ResetColor();
                    _logger.Error(ex, "Network test {Method} failed", name);
                }
            }

            Program.Results["Network"] = isEnabled;
            Console.WriteLine();
        }

        private async Task<bool?> TestNetworkSetupAsync()
        {
            var result = await ExecuteCommandAsync("/usr/sbin/networksetup", "-getnetworkserviceenabled \"Wi-Fi\"");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("networksetup wifi output: '{Output}'", result.Output.Trim());
                return result.Output.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        private async Task<bool?> TestIfconfigAsync()
        {
            var result = await ExecuteCommandAsync("/sbin/ifconfig", "");
            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                _logger.Debug("ifconfig output length: {Length}", result.Output.Length);
                return result.Output.Contains("status: active", StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        private async Task<bool?> TestPingAsync()
        {
            var result = await ExecuteCommandAsync("/sbin/ping", "-c 1 -t 2 8.8.8.8");
            _logger.Debug("ping result: exit={ExitCode}, output length={Length}", result.ExitCode, result.Output?.Length ?? 0);
            return result.ExitCode == 0;
        }

        public async Task ShowSystemInfoAsync()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ℹ️  SYSTEM INFORMATION");
            Console.WriteLine("─".PadRight(40, '─'));
            Console.ResetColor();

            var commands = new[]
            {
                ("macOS Version", "/usr/bin/sw_vers", ""),
                ("Hardware", "/usr/sbin/system_profiler", "SPHardwareDataType"),
                ("CPU Info", "/usr/sbin/sysctl", "-n machdep.cpu.brand_string"),
                ("Memory", "/usr/sbin/sysctl", "-n hw.memsize"),
                ("Uptime", "/usr/bin/uptime", ""),
                ("Disk Usage", "/bin/df", "-h /")
            };

            foreach (var (name, command, args) in commands)
            {
                try
                {
                    var result = await ExecuteCommandAsync(command, args);
                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
                    {
                        var output = result.Output.Trim();
                        if (output.Length > 200)
                            output = output.Substring(0, 200) + "...";

                        Console.WriteLine($"  {name}: {output}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {name}: Error - {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        private async Task<(int ExitCode, string Output, string Error)> ExecuteCommandAsync(string command, string arguments)
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

                var sw = Stopwatch.StartNew();
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();
                sw.Stop();

                var output = await outputTask;
                var error = await errorTask;

                _logger.Debug("Command {Command} {Args} completed in {Duration}ms with exit code {ExitCode}",
                    command, arguments, sw.ElapsedMilliseconds, process.ExitCode);

                return (process.ExitCode, output, error);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to execute command {Command} {Args}", command, arguments);
                return (-1, "", ex.Message);
            }
        }
    }
}