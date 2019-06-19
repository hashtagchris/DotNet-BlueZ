using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ.Extensions
{
  public static class Extensions
  {
    public static Task<IReadOnlyList<IDevice1>> GetDevicesAsync(this IAdapter1 adapter)
    {
      return GetProxiesAsync<IDevice1>(adapter, BluezConstants.Device1Interface);
    }

    public static async Task<IDevice1> GetDeviceAsync(this IAdapter1 adapter, string deviceAddress)
    {
      var devices = await GetProxiesAsync<IDevice1>(adapter, BluezConstants.Device1Interface);

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

      return matches.FirstOrDefault();
    }


    public static Task<IDisposable> WatchDevicesAddedAsync(this IAdapter1 adapter, Action<IDevice1> handler)
    {
      void OnDeviceAdded((ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces) args)
      {
        if (IsMatch(adapter, BluezConstants.Device1Interface, args.objectPath, args.interfaces))
        {
          var device = Connection.System.CreateProxy<IDevice1>(BluezConstants.DbusService, args.objectPath);
          handler(device);
        }
      }

      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      return objectManager.WatchInterfacesAddedAsync(OnDeviceAdded);
    }

    public static Task<IDisposable> WatchDevicesRemovedAsync(this IAdapter1 adapter, Action<IDevice1> handler)
    {
      void OnDeviceAdded((ObjectPath objectPath, String[] interfaces) args)
      {
        if (IsMatch(adapter, BluezConstants.Device1Interface, args.objectPath, args.interfaces))
        {
          var device = Connection.System.CreateProxy<IDevice1>(BluezConstants.DbusService, args.objectPath);
          handler(device);
        }
      }

      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      return objectManager.WatchInterfacesRemovedAsync(OnDeviceAdded);
    }

    public static async Task<IGattService1> GetServiceAsync(this IDevice1 device, string serviceUUID)
    {
      var services = await GetProxiesAsync<IGattService1>(device, BluezConstants.GattServiceInterface);

      foreach (var service in services)
      {
        if (await service.GetUUIDAsync() == serviceUUID)
        {
          return service;
        }
      }

      return null;
    }

    public static async Task<IGattCharacteristic1> GetCharacteristicAsync(this IGattService1 service, string characteristicUUID)
    {
      var characteristics = await GetProxiesAsync<IGattCharacteristic1>(service, BluezConstants.GattCharacteristicInterface);

      foreach (var characteristic in characteristics)
      {
        if (await characteristic.GetUUIDAsync() == characteristicUUID)
        {
          return characteristic;
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

    private static async Task<IReadOnlyList<T>> GetProxiesAsync<T>(IDBusObject rootObject, string interfaceName)
    {
      // Console.WriteLine("GetProxiesAsync called.");
      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      var objects = await objectManager.GetManagedObjectsAsync();

      var matchingObjectPaths = objects
          .Where(obj => IsMatch(rootObject, interfaceName, obj.Key, obj.Value))
          .Select(obj => obj.Key);

      var proxies = matchingObjectPaths
          .Select(objectPath => Connection.System.CreateProxy<T>(BluezConstants.DbusService, objectPath))
          .ToList();

      // Console.WriteLine($"GetProxiesAsync returning {proxies.Count} proxies of type {typeof(T)}.");
      return proxies;
    }

    private static bool IsMatch(IDBusObject rootObject, string interfaceName, ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces)
    {
      return IsMatch(rootObject, interfaceName, objectPath, interfaces.Keys);
    }

    private static bool IsMatch(IDBusObject rootObject, string interfaceName, ObjectPath objectPath, ICollection<string> interfaces)
    {
      if (rootObject != null && !objectPath.ToString().StartsWith($"{rootObject.ObjectPath}/"))
      {
        return false;
      }

      return interfaces.Contains(interfaceName);
    }
  }
}
