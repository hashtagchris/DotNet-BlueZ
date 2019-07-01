using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace printDeviceInfoEventDriven
{
  class Program
  {
    private static async Task Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Usage: PrintDeviceInfo <deviceNameSubstring> [adapterName]");
        Console.WriteLine("Example: PrintDeviceInfo phone hci1");
        return;
      }

      s_deviceNameSubstring = args[0];

      Adapter adapter;
      if (args.Length > 1)
      {
        adapter = await BlueZManager.GetAdapterAsync(args[1]);
      }
      else
      {
        var adapters = await BlueZManager.GetAdaptersAsync();
        if (adapters.Count == 0)
        {
          throw new Exception("No Bluetooth adapters found.");
        }

        adapter = adapters.First();
      }

      var adapterPath = adapter.ObjectPath.ToString();
      var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
      Console.WriteLine($"Using Bluetooth adapter {adapterName}");

      adapter.PoweredOn += adapter_PoweredOnAsync;
      adapter.DeviceFound += adapter_DeviceFoundAsync;
      await Task.Delay(-1);
    }

    private static async Task adapter_PoweredOnAsync(Adapter adapter, BlueZEventArgs e)
    {
      if (e.IsStateChange)
      {
        Console.WriteLine("Powered on.");
      }
      else
      {
        Console.WriteLine("Already powered on.");
      }

      Console.WriteLine("Starting scan...");
      await adapter.StartDiscoveryAsync();
    }

    private static async Task adapter_DeviceFoundAsync(Adapter adapter, DeviceFoundEventArgs e)
    {
      var device = e.Device;

      string deviceDescription = await GetDeviceDescriptionAsync(device);
      if (e.IsStateChange)
      {
        Console.WriteLine($"Found: [NEW] {deviceDescription}");
      }
      else
      {
        Console.WriteLine($"Found: {deviceDescription}");
      }

      var deviceName = await device.GetAliasAsync();
      if (deviceName.Contains(s_deviceNameSubstring, StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine("Stopping scan....");
        await adapter.StopDiscoveryAsync();
        Console.WriteLine("Stopped.");

        device.Connected += device_ConnectedAsync;
        device.Disconnected += device_DisconnectedAsync;
        device.ServicesResolved += device_ServicesResolvedAsync;
        Console.WriteLine("Connecting...");
        await device.ConnectAsync();
      }
    }

    private static async Task device_ConnectedAsync(Device device, BlueZEventArgs e)
    {
      if (e.IsStateChange)
      {
        Console.WriteLine($"Connected to {await device.GetAddressAsync()}");
      }
      else
      {
        Console.WriteLine($"Already connected to {await device.GetAddressAsync()}");
      }
    }

    private static async Task device_DisconnectedAsync(Device device, BlueZEventArgs e)
    {
      Console.WriteLine($"Disconnected from {await device.GetAddressAsync()}");

      await Task.Delay(TimeSpan.FromSeconds(15));

      Console.WriteLine("Attempting to reconnect...");
      await device.ConnectAsync();
    }

    private static async Task device_ServicesResolvedAsync(Device device, BlueZEventArgs e)
    {
      if (e.IsStateChange)
      {
        Console.WriteLine($"Services resolved for {await device.GetAddressAsync()}");
      }
      else
      {
        Console.WriteLine($"Services already resolved for {await device.GetAddressAsync()}");
      }

      var servicesUUID = await device.GetUUIDsAsync();
      Console.WriteLine($"Device offers {servicesUUID.Length} service(s).");

      var deviceInfoServiceFound = servicesUUID.Any(uuid => String.Equals(uuid, GattConstants.BatteryServiceUUID, StringComparison.OrdinalIgnoreCase));
      if (!deviceInfoServiceFound)
      {
        Console.WriteLine("Device doesn't have the Device Information Service. Try pairing first?");
        return;
      }

      var service = await device.GetServiceAsync(GattConstants.BatteryServiceUUID);
      var characteristic = await service.GetCharacteristicAsync(GattConstants.BatteryLevelCharacteristicUUID);

      Console.WriteLine("Reading current battery level...");
      var valueBytes = await characteristic.ReadValueAsync(timeout);
      Console.WriteLine($"Battery level: {valueBytes[0]}%");
    }

    private static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
    {
      var deviceProperties = await device.GetAllAsync();
      return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI})";
    }

    private static string s_deviceNameSubstring;

    private static TimeSpan timeout = TimeSpan.FromSeconds(15);
  }
}
