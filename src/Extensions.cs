using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public static async Task WaitForPropertyValueAsync<T>(this IDBusObject obj, string propertyName, Func<Task<T>> readValueFunc, T value, TimeSpan timeout)
    {
      // Ideally we'd wait for D-Bus PropertyChanged events to fire, but for now we'll poll.
      // Also ideally we'd be able to read property values for any D-Bus object, but for now we take a function.
      var watch = Stopwatch.StartNew();
      while (watch.Elapsed <= timeout)
      {
        await Task.Delay(50);

        if ((await readValueFunc()).Equals(value))
        {
          return;
        }
      }

      throw new TimeoutException($"Timed out waiting for {obj.GetType().Name} ({obj.ObjectPath}) \"{propertyName}\" property to equal '{value}'.");
    }

    private static async Task<IReadOnlyList<T>> GetProxiesAsync<T>(IDBusObject rootObject, string interfaceName)
    {
      // Console.WriteLine("GetProxiesAsync called.");
      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      var objects = await objectManager.GetManagedObjectsAsync();

      var matchingObjects = objects
          .Where(obj => obj.Value.Keys.Contains(interfaceName))
          .Select(obj => obj.Key)
          .Where(objectPath => objectPath.ToString().StartsWith($"{rootObject.ObjectPath}/"));

      var proxies = matchingObjects
          .Select(objectPath => Connection.System.CreateProxy<T>(BluezConstants.DbusService, objectPath))
          .ToList();

      // Console.WriteLine($"GetProxiesAsync returning {proxies.Count} proxies of type {typeof(T)}.");
      return proxies;
    }
  }
}
