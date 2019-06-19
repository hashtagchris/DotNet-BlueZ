// This program is the equivalent of the sample code posted to https://stackoverflow.com/questions/53933345/utilizing-bluetooth-le-on-raspberry-pi-using-net-core/56623587#56623587
// This uses HashtagChris.DotNetBlueZ instead of Tmds.DBus directly.
//
// Use the `bluetoothctl` command-line tool or the Bluetooth Manager GUI to scan for devices and possibly pair.
// Then you can use this program to connect and print "Device Information" GATT service values.
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

class Program
{
  static string defaultAdapterName = "hci0";
  static TimeSpan timeout = TimeSpan.FromSeconds(15);

  static async Task Main(string[] args)
  {
    if (args.Length < 1)
    {
      Console.WriteLine("Usage: PrintDeviceInfo <deviceAddress> [adapterName]");
      Console.WriteLine("Example: PrintDeviceInfo AA:BB:CC:11:22:33 hci1");
      return;
    }

    var deviceAddress = args[0];
    var adapterName = args.Length > 1 ? args[1] : defaultAdapterName;

    // Get the Bluetooth adapter.
    var adapter = BlueZManager.GetAdapter(adapterName);
    if (adapter == null)
    {
      Console.WriteLine($"Bluetooth adapter '{adapterName}' not found.");
    }

    // Find the Bluetooth peripheral.
    var device = await adapter.GetDeviceAsync(deviceAddress);
    if (device == null)
    {
      Console.WriteLine($"Bluetooth peripheral with address '{deviceAddress}' not found. Use `bluetoothctl` or Bluetooth Manager to scan and possibly pair first.");
      return;
    }

    Console.WriteLine("Connecting...");
    await device.ConnectAsync();
    await device.WaitForPropertyValueAsync("Connected", value: true, timeout);
    Console.WriteLine("Connected.");

    Console.WriteLine("Waiting for services to resolve...");
    await device.WaitForPropertyValueAsync("ServicesResolved", value: true, timeout);

    var servicesUUID = await device.GetUUIDsAsync();
    Console.WriteLine($"Device offers {servicesUUID.Length} service(s).");

    var deviceInfoServiceFound = servicesUUID.Any(uuid => String.Equals(uuid, GattConstants.DeviceInformationServiceUUID, StringComparison.OrdinalIgnoreCase));
    if (!deviceInfoServiceFound)
    {
      Console.WriteLine("Device doesn't have the Device Information Service. Try pairing first?");
      return;
    }

    // Console.WriteLine("Retrieving Device Information service...");
    var service = await device.GetServiceAsync(GattConstants.DeviceInformationServiceUUID);
    var modelNameCharacteristic = await service.GetCharacteristicAsync(GattConstants.ModelNameCharacteristicUUID);
    var manufacturerCharacteristic = await service.GetCharacteristicAsync(GattConstants.ManufacturerNameCharacteristicUUID);

    int characteristicsFound = 0;
    if (modelNameCharacteristic != null)
    {
        characteristicsFound++;
        Console.WriteLine("Reading model name characteristic...");
        var modelNameBytes = await modelNameCharacteristic.ReadValueAsync(timeout);
        Console.WriteLine($"Model name: {Encoding.UTF8.GetString(modelNameBytes)}");
    }

    if (manufacturerCharacteristic != null)
    {
        characteristicsFound++;
        Console.WriteLine("Reading manufacturer characteristic...");
        var manufacturerBytes = await manufacturerCharacteristic.ReadValueAsync(timeout);
        Console.WriteLine($"Manufacturer: {Encoding.UTF8.GetString(manufacturerBytes)}");
    }

    if (characteristicsFound == 0)
    {
        Console.WriteLine("Model name and manufacturer characteristics not found.");
    }

    await device.DisconnectAsync();
    Console.WriteLine("Disconnected.");
  }
}