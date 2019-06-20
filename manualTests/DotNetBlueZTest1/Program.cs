using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace DotNetBlueZTest1
{
    class Program
    {
        const string DefaultAdapterName = "hci0";
        static TimeSpan timeout = TimeSpan.FromSeconds(15);

        static async Task Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2 || args[0].ToLowerInvariant() == "-h" || !int.TryParse(args[0], out int scanSeconds))
            {
                Console.WriteLine("Usage: DotNetBlueZTest1 <SecondsToScan> [adapterName]");
                Console.WriteLine("Example: DotNetBlueZTest1 15 hci0");
                return;
            }

            var adapterName = args.Length > 1 ? args[1] : DefaultAdapterName;
            var adapter = await BlueZManager.GetAdapterAsync(adapterName);

            // Scan briefly for devices.
            Console.WriteLine($"Scanning for {scanSeconds} seconds...");

            using (await adapter.WatchDevicesAddedAsync(async device => {
                // Write a message when we detect new devices during the scan.
                string deviceDescription = await GetDeviceDescriptionAsync(device);
                Console.WriteLine($"[NEW] {deviceDescription}");
            }))
            {
                await adapter.StartDiscoveryAsync();
                await Task.Delay(TimeSpan.FromSeconds(scanSeconds));
                await adapter.StopDiscoveryAsync();
            }

            var devices = await adapter.GetDevicesAsync();
            Console.WriteLine($"{devices.Count} device(s) found.");

            foreach (var device in devices)
            {
                await OnDeviceFoundAsync(device);
            }
        }

        static async Task OnDeviceFoundAsync(IDevice1 device)
        {
            string deviceDescription = await GetDeviceDescriptionAsync(device);
            while (true)
            {
                Console.WriteLine($"Connect to {deviceDescription}? yes/[no]?");
                string response = Console.ReadLine();
                if (response.Length == 0 || response.ToLowerInvariant().StartsWith("n"))
                {
                    return;
                }

                if (response.ToLowerInvariant().StartsWith("y"))
                {
                    break;
                }
            }

            try
            {
                Console.WriteLine("Connecting...");
                await device.ConnectAsync();
                await device.WaitForPropertyValueAsync("Connected", value: true, timeout);
                Console.WriteLine("Connected.");

                Console.WriteLine("Waiting for services to resolve...");
                await device.WaitForPropertyValueAsync("ServicesResolved", value: true, timeout);

                var servicesUUIDs = await device.GetUUIDsAsync();
                Console.WriteLine($"Device offers {servicesUUIDs.Length} service(s).");

                var deviceInfoServiceFound = servicesUUIDs.Any(uuid => uuid == GattConstants.DeviceInformationServiceUUID);
                if (!deviceInfoServiceFound)
                {
                    Console.WriteLine("Device doesn't have the Device Information Service. Try pairing first?");
                    return;
                }

                // Console.WriteLine("Retrieving Device Information service...");
                var service = await device.GetServiceAsync(GattConstants.DeviceInformationServiceUUID);
                var modelNameCharacteristic = await service.GetCharacteristicAsync(GattConstants.ModelNameCharacteristicUUID);
                var manufacturerCharacteristic = await service.GetCharacteristicAsync(GattConstants.ManufacturerNameCharacteristicUUID);

                Console.WriteLine("Reading Device Info characteristic values...");
                var modelNameBytes = await modelNameCharacteristic.ReadValueAsync(timeout);
                var manufacturerBytes = await manufacturerCharacteristic.ReadValueAsync(timeout);

                Console.WriteLine($"Model name: {Encoding.UTF8.GetString(modelNameBytes)}");
                Console.WriteLine($"Manufacturer: {Encoding.UTF8.GetString(manufacturerBytes)}");

                // Test walking back up to the adapter...
                var adapterName = await (await (await (await modelNameCharacteristic.GetServiceAsync()).GetDeviceAsync()).GetAdapterAsync()).GetAliasAsync();

                Console.WriteLine($"Adapter name: {adapterName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine();
            }
        }

        private static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
        {
            var deviceProperties = await device.GetAllAsync();
            return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI})";
        }
    }
}
