using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

// An event-driven example that subscribes to one GATT characteristic and prints the value on updates.
namespace subscribeToCharacteristic
{
  class Program
  {
    // TODO: Is there a good characteristic that works for a wide variety of people?
    // Battery level doesn't work because BlueZ gives that a separate interface.
    // Current time seems promising, but the iPhone doesn't seem to notify current time subscribers.
    private const string DefaultServiceUUID = GattConstants.ANCServiceUUID;
    private const string DefaultCharacteristicUUID = GattConstants.ANCSNotificationSourceUUID;

    private static string s_deviceFilter;
    private static string s_serviceUUID;
    private static string s_characteristicUUID;

    private static TimeSpan timeout = TimeSpan.FromSeconds(15);

    private static async Task Main(string[] args)
    {
      if (args.Length < 1 || args.Length == 3)
      {
        Console.WriteLine("Usage: subscribeToCharacteristic <deviceAddress>|<deviceNameSubstring> [adapterName] [serviceUUID characteristicUUID]");
        Console.WriteLine(@"Examples:
  subscribeToCharacteristic phone
  subscribeToCharacteristic 8C:8E:F2:AB:73:76 hci0 CAFE CFFE (see https://github.com/hashtagchris/early-iOS-BluetoothLowEnergy-tests/tree/master/myFirstPeripheral)");
        Console.WriteLine();
        Console.WriteLine($"Default service:        {DefaultServiceUUID}");
        Console.WriteLine($"Default characteristic: {DefaultCharacteristicUUID}");
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

      s_serviceUUID = BlueZManager.NormalizeUUID(args.Length > 3
        ? args[2]
        : DefaultServiceUUID);

      s_characteristicUUID = BlueZManager.NormalizeUUID(args.Length > 3
        ? args[3]
        : DefaultCharacteristicUUID);

      var adapterPath = adapter.ObjectPath.ToString();
      var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
      Console.WriteLine($"Using Bluetooth adapter {adapterName}");

      adapter.PoweredOn += adapter_PoweredOnAsync;
      adapter.DeviceFound += adapter_DeviceFoundAsync;

      Console.WriteLine("Waiting for events. Use Control-C to quit.");
      Console.WriteLine();
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

        var service = await device.GetServiceAsync(s_serviceUUID);
        if (service == null)
        {
          Console.WriteLine($"Service UUID {s_serviceUUID} not found. Do you need to pair first?");
          return;
        }

        var characteristic = await service.GetCharacteristicAsync(s_characteristicUUID);
        if (characteristic == null)
        {
          Console.WriteLine($"Characteristic UUID {s_characteristicUUID} not found within service {s_serviceUUID}.");
          return;
        }

        Console.WriteLine();

        // Subscribe to the characteristic's value. Be notified of updates.
        characteristic.Value += characteristic_Value;

        // Attempt to read the current value. Some characteristics only support Notify.
        byte[] value;
        try
        {
          Console.WriteLine("Reading current characteristic value...");
          value = await characteristic.GetValueAsync();
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine($"Error reading characteristic value: {ex.Message}");
          return;
        }

        PrintCharacteristicValue(s_characteristicUUID, value);
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
        PrintCharacteristicValue(uuid, e.Value);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    private static void PrintCharacteristicValue(string uuid, byte[] value)
    {
      if (String.Equals(uuid, GattConstants.CurrentTimeCharacteristicUUID, StringComparison.OrdinalIgnoreCase))
      {
        var currentTime = ReadCurrentTime(value);
        Console.WriteLine($"Current time: {currentTime}");
      }
      else if (String.Equals(uuid, GattConstants.ANCSNotificationSourceUUID, StringComparison.OrdinalIgnoreCase))
      {
        PrintAncsDescription(value);
      }
      else
      {
        // Default
        Console.WriteLine($"[{DateTime.Now}] Characteristic value (hex): {BitConverter.ToString(value)}");
        try
        {
          var stringValue = Encoding.UTF8.GetString(value);
          Console.WriteLine($"[{DateTime.Now}] Characteristic value (UTF-8): \"{stringValue}\"");
        }
        catch (Exception) {}
      }
    }

    private static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
    {
      var deviceProperties = await device.GetAllAsync();
      return $"{deviceProperties.Address} (Alias: {deviceProperties.Alias}, RSSI: {deviceProperties.RSSI})";
    }

    private static void PrintAncsDescription(byte[] value)
    {
      if (value.Length < 8)
      {
        throw new ArgumentException("8 bytes are required for ANCS notifications.");
      }

      var eventIds = new string[] { "added", "modified", "removed" };
      var categoryIds = new string[] { "Other", "IncomingCall", "MissedCall", "Voicemail", "Social", "Schedule", "Email", "News", "Health & Fitness", "Business & Finance", "Location", "Entertainment" };

      byte[] notificationUid = new byte[4];
      Array.Copy(value, 4, notificationUid, 0, 4);

      Console.WriteLine($"{categoryIds[value[2]]} notification {eventIds[value[0]]} (Count: {value[3]}) (UID: {BitConverter.ToString(notificationUid)})");
    }

    private static DateTime ReadCurrentTime(byte[] value)
    {
      if (value.Length < 7)
      {
        throw new ArgumentException("7+ bytes are required for the current date time.");
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
