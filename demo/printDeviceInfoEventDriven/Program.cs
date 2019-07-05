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
    private const string ServiceUUID = GattConstants.CurrentTimeServiceUUID;
    private const string CharacteristicUUID = GattConstants.CurrentTimeCharacteristicUUID;

    private static string s_deviceFilter;

    private static TimeSpan timeout = TimeSpan.FromSeconds(15);

    private static async Task Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Usage: PrintDeviceInfo <deviceAddress>|<deviceNameSubstring> [adapterName]");
        Console.WriteLine("Example: PrintDeviceInfo phone hci1");
        return;
      }

      s_deviceFilter = args[0];

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
      try
      {
        if (e.IsStateChange)
        {
          Console.WriteLine("Bluetooth adapter powered on.");
        }
        else
        {
          Console.WriteLine("Bluetooth adapter already powered on.");
        }

        Console.WriteLine("Starting scan...");
        await adapter.StartDiscoveryAsync();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static async Task adapter_DeviceFoundAsync(Adapter adapter, DeviceFoundEventArgs e)
    {
      try
      {
        var device = e.Device;

        var deviceDescription = await GetDeviceDescriptionAsync(device);
        if (e.IsStateChange)
        {
          Console.WriteLine($"Found: [NEW] {deviceDescription}");
        }
        else
        {
          Console.WriteLine($"Found: {deviceDescription}");
        }

        var deviceAddress = await device.GetAddressAsync();
        var deviceName = await device.GetAliasAsync();
        if (deviceAddress.Equals(s_deviceFilter, StringComparison.OrdinalIgnoreCase)
            || deviceName.Contains(s_deviceFilter, StringComparison.OrdinalIgnoreCase))
        {
          Console.WriteLine("Stopping scan....");
          try
          {
            await adapter.StopDiscoveryAsync();
            Console.WriteLine("Stopped.");
          }
          catch (Exception ex)
          {
            // Best effort. Sometimes BlueZ gets in a state where you can't stop the scan.
            Console.Error.WriteLine($"Error stopping scan: {ex.Message}");
          }

          device.Connected += device_ConnectedAsync;
          device.Disconnected += device_DisconnectedAsync;
          device.ServicesResolved += device_ServicesResolvedAsync;
          Console.WriteLine($"Connecting to {await device.GetAddressAsync()}...");
          await device.ConnectAsync();
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static async Task device_ConnectedAsync(Device device, BlueZEventArgs e)
    {
      try
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
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static async Task device_DisconnectedAsync(Device device, BlueZEventArgs e)
    {
      try
      {
        Console.WriteLine($"Disconnected from {await device.GetAddressAsync()}");

        await Task.Delay(TimeSpan.FromSeconds(15));

        Console.WriteLine($"Attempting to reconnect to {await device.GetAddressAsync()}...");
        await device.ConnectAsync();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static async Task device_ServicesResolvedAsync(Device device, BlueZEventArgs e)
    {
      try
      {
        if (e.IsStateChange)
        {
          Console.WriteLine($"Services resolved for {await device.GetAddressAsync()}");
        }
        else
        {
          Console.WriteLine($"Services already resolved for {await device.GetAddressAsync()}");
        }

        var servicesUUIDs = await device.GetUUIDsAsync();
        Console.WriteLine($"Device offers {servicesUUIDs.Length} service(s).");
        // foreach (var uuid in servicesUUIDs)
        // {
        //   Console.WriteLine(uuid);
        // }

        var service = await device.GetServiceAsync(ServiceUUID);
        if (service == null)
        {
          Console.WriteLine($"Service UUID {ServiceUUID} notfound. Do you need to pair first?");
          return;
        }

        var characteristic = await service.GetCharacteristicAsync(CharacteristicUUID);
        if (characteristic == null)
        {
          Console.WriteLine($"Characteristic UUID {CharacteristicUUID} not found within service {ServiceUUID}.");
          return;
        }

        Console.WriteLine();
        characteristic.Value += characteristic_Value;

        // Console.WriteLine("Reading GATT characteristic...");
        // var valueBytes = await characteristic.ReadValueAsync(timeout);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static async Task characteristic_Value(GattCharacteristic characteristic, GattCharacteristicValueEventArgs e)
    {
      try
      {
        var uuid = await characteristic.GetUUIDAsync();
        Console.WriteLine($"UUID: {uuid}; Status change: {e.IsStateChange}");
        if (String.Equals(uuid, GattConstants.CurrentTimeCharacteristicUUID, StringComparison.OrdinalIgnoreCase))
        {
          var currentTime = ReadCurrentTime(e.Value);
          Console.WriteLine($"Current time: {currentTime}");
        }
        else
        {
          // Default
          Console.WriteLine($"Characteristic value (hex): {BitConverter.ToString(e.Value)}");
          try
          {
            var stringValue = Encoding.UTF8.GetString(e.Value);
            Console.WriteLine($"Characteristic value (UTF-8): \"{stringValue}\"");
          }
          catch (Exception) {}
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
    {
      var deviceProperties = await device.GetAllAsync();
      return $"{deviceProperties.Address} (Alias: {deviceProperties.Alias}, RSSI: {deviceProperties.RSSI})";
    }

    private static DateTime ReadCurrentTime(byte[] value)
    {
      if (value.Length < 7)
      {
        throw new Exception("7+ bytes are required for the current date time.");
      }

      // https://github.com/sputnikdev/bluetooth-gatt-parser/blob/master/src/main/resources/gatt/characteristic/org.bluetooth.characteristic.date_time.xml
      var year = value[0] + 256 * value[1];
      var month = value[2];
      var day = value[3];
      var hour = value[4];
      var minute = value[5];
      var second = value[6];

      return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
    }
  }
}
