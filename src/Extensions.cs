using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ.Extensions
{
  public static class Extensions
  {
    public static async Task<IReadOnlyList<Device>> GetDevicesAsync(this IAdapter1 adapter)
    {
      var devices = await BlueZManager.GetProxiesAsync<IDevice1>(BluezConstants.DeviceInterface, adapter);

      return await Task.WhenAll(devices.Select(Device.CreateAsync));
    }

    public static async Task<Device> GetDeviceAsync(this IAdapter1 adapter, string deviceAddress)
    {
      var devices = await BlueZManager.GetProxiesAsync<IDevice1>(BluezConstants.DeviceInterface, adapter);

      var matches = new List<IDevice1>();
      foreach (var device in devices)
      {
        if (String.Equals(await device.GetAddressAsync(), deviceAddress, StringComparison.OrdinalIgnoreCase))
        {
          matches.Add(device);
        }
      }

      // BlueZ can get in a weird state, probably due to random public BLE addresses.
      if (matches.Count > 1)
      {
        throw new Exception($"{matches.Count} devices found with the address {deviceAddress}!");
      }

      var dev = matches.FirstOrDefault();
      if (dev != null)
      {
        return await Device.CreateAsync(dev);
      }
      return null;
    }

    public static Task<IDisposable> WatchDevicesAddedAsync(this IAdapter1 adapter, Action<Device> handler)
    {
      async void OnDeviceAdded((ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces) args)
      {
        if (BlueZManager.IsMatch(BluezConstants.DeviceInterface, args.objectPath, args.interfaces, adapter))
        {
          var device = Connection.System.CreateProxy<IDevice1>(BluezConstants.DbusService, args.objectPath);

          var dev = await Device.CreateAsync(device);
          handler(dev);
        }
      }

      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      return objectManager.WatchInterfacesAddedAsync(OnDeviceAdded);
    }

    public static async Task<IGattService1> GetServiceAsync(this IDevice1 device, string serviceUUID)
    {
      var services = await BlueZManager.GetProxiesAsync<IGattService1>(BluezConstants.GattServiceInterface, device);

      foreach (var service in services)
      {
        var uuid = await service.GetUUIDAsync();
        // Console.WriteLine($"Checking {uuid}");
        if (String.Equals(uuid, serviceUUID, StringComparison.OrdinalIgnoreCase))
        {
          return service;
        }
      }

      return null;
    }

    public static async Task<GattCharacteristic> GetCharacteristicAsync(this IGattService1 service, string characteristicUUID)
    {
      var characteristics = await BlueZManager.GetProxiesAsync<IGattCharacteristic1>(BluezConstants.GattCharacteristicInterface, service);

      foreach (var characteristic in characteristics)
      {
        var uuid = await characteristic.GetUUIDAsync();
        // Console.WriteLine($"Checking {uuid}");
        if (String.Equals(uuid, characteristicUUID, StringComparison.OrdinalIgnoreCase))
        {
          var ch = await GattCharacteristic.CreateAsync(characteristic);
          return ch;
        }
      }

      return null;
    }

    public static async Task<byte[]> ReadValueAsync(this IGattCharacteristic1 characteristic, TimeSpan timeout)
    {
      var options = new Dictionary<string, object>();
      var readTask = characteristic.ReadValueAsync(options);
      var timeoutTask = Task.Delay(timeout);

      await Task.WhenAny(new Task[] { readTask, timeoutTask });
      if (!readTask.IsCompleted)
      {
        throw new TimeoutException("Timed out waiting to read characteristic value.");
      }

      return await readTask;
    }

    // TODO: Make this available to other generated interfaces too, not just IDevice1.
    // `dynamic obj` works, but it requires a Microsoft.* NuGet package and isn't type safe.
    public static async Task WaitForPropertyValueAsync<T>(this IDevice1 obj, string propertyName, T value, TimeSpan timeout)
    {
      var (watchTask, watcher) = WaitForPropertyValueInternal<T>(obj, propertyName, value);
      var currentValue = await obj.GetAsync<T>(propertyName);
      // Console.WriteLine($"{propertyName}: {currentValue}");

      // https://stackoverflow.com/questions/390900/cant-operator-be-applied-to-generic-types-in-c
      if (EqualityComparer<T>.Default.Equals(currentValue, value))
      {
        watcher.Dispose();
        return;
      }

      await Task.WhenAny(new Task[] { watchTask, Task.Delay(timeout) });
      if (!watchTask.IsCompleted)
      {
        throw new TimeoutException($"Timed out waiting for '{propertyName}' to change to '{value}'.");
      }

      // propogate any exceptions.
      await watchTask;
    }

    private static (Task, IDisposable) WaitForPropertyValueInternal<T>(IDevice1 obj, string propertyName, T value)
    {
      var taskSource = new TaskCompletionSource<bool>();

      IDisposable watcher = null;
      watcher = obj.WatchPropertiesAsync(propertyChanges => {
        try
        {
          if (propertyChanges.Changed.Any(kvp => kvp.Key == propertyName))
          {
            var pair = propertyChanges.Changed.Single(kvp => kvp.Key == propertyName);
            if (pair.Value.Equals(value))
            {
              // Console.WriteLine($"[CHG] {propertyName}: {pair.Value}.");
              taskSource.SetResult(true);
              watcher.Dispose();
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception: {ex}");
          taskSource.SetException(ex);
          watcher.Dispose();
        }
      });

      return (taskSource.Task, watcher);
    }
  }
}
