using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace Scan
{
    class Program
    {
        const string DefaultAdapterName = "hci0";
        static TimeSpan timeout = TimeSpan.FromSeconds(15);

        static async Task Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2 || args[0].ToLowerInvariant() == "-h" || !int.TryParse(args[0], out int scanSeconds))
            {
                Console.WriteLine("Usage: scan <SecondsToScan> [adapterName]");
                Console.WriteLine("Example: scan 15 hci0");
                return;
            }

            var adapterName = args.Length > 1 ? args[1] : DefaultAdapterName;
            var adapter = BlueZManager.GetAdapter(adapterName);

            // Print out the devices we already know about.
            var devices = await adapter.GetDevicesAsync();
            Console.WriteLine($"{devices.Count} device(s) found ahead of scan.");
            foreach (var device in devices)
            {
                string deviceDescription = await GetDeviceDescriptionAsync(device);
                Console.WriteLine(deviceDescription);
            }

            Console.WriteLine();

            // Scan for more devices.
            Console.WriteLine($"Scanning for {scanSeconds} seconds...");

            int newDevices = 0;
            using (await adapter.WatchDevicesAddedAsync(async device => {
                newDevices++;
                // Write a message when we detect new devices during the scan.
                string deviceDescription = await GetDeviceDescriptionAsync(device);
                Console.WriteLine($"[NEW] {deviceDescription}");
            }))
            {
                await adapter.StartDiscoveryAsync();
                await Task.Delay(TimeSpan.FromSeconds(scanSeconds));
                await adapter.StopDiscoveryAsync();
            }
            Console.WriteLine($"Scan complete. {newDevices} new device(s) found.");
        }

        private static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
        {
            var deviceProperties = await device.GetAllAsync();
            return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI})";
        }
    }
}
