using System;
using System.Linq;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace printDeviceInfoEventDriven
{
  class Program
  {
    static async Task Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Usage: PrintDeviceInfo <deviceAddress> [adapterName]");
        Console.WriteLine("Example: PrintDeviceInfo AA:BB:CC:11:22:33 hci1");
        return;
      }

      var deviceAddress = args[0];

      IAdapter1 adapter;
      if (args.Length > 1)
      {
        adapter = await BlueZManager.GetAdapterAsync(args[1]);
      }
      else
      {
        var adapters = await BlueZManager.GetAdaptersAsync();
        if (adapters.Length == 0)
        {
          throw new Exception("No Bluetooth adapters found.");
        }

        adapter = adapters.First();
      }

      var adapterPath = adapter.ObjectPath.ToString();
      var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
      Console.WriteLine($"Using Bluetooth adapter {adapterName}");

      // Find the Bluetooth peripheral.
      Device device = await adapter.GetDeviceAsync(deviceAddress);
      if (device == null)
      {
        Console.WriteLine($"Bluetooth peripheral with address '{deviceAddress}' not found. Use `bluetoothctl` or Bluetooth Manager to scan and possibly pair first.");
        return;
      }

      device.Connected += device_Connected;
      device.ServicesResolved += device_ServicesResolved;

      Console.WriteLine("Connecting...");
      await device.ConnectAsync();
    }

    static async void device_Connected(Object sender, EventArgs e)
    {
      var dev = (Device)sender;
      Console.WriteLine($"Connected to {await dev.GetAddressAsync()}");
    }

    static async void device_Disonnected(Object sender, EventArgs e)
    {
      var dev = (Device)sender;
      Console.WriteLine($"Disconnected from {await dev.GetAddressAsync()}");
    }

    static async void device_ServicesResolved(Object sender, EventArgs e)
    {
      var dev = (Device)sender;
      Console.WriteLine($"Services resolved for {await dev.GetAddressAsync()}");
    }
  }
}
