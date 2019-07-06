using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  public static class BlueZManager
  {
    public static async Task<Adapter> GetAdapterAsync(string adapterName)
    {
      var adapterObjectPath = $"/org/bluez/{adapterName}";
      var adapter = Connection.System.CreateProxy<IAdapter1>(BluezConstants.DbusService, adapterObjectPath);

      try
      {
        await adapter.GetAliasAsync();
      }
      catch (Exception)
      {
        throw new Exception($"Bluetooth adapter {adapterName} not found.");
      }

      return await Adapter.CreateAsync(adapter);
    }

    public static async Task<IReadOnlyList<Adapter>> GetAdaptersAsync()
    {
      var adapters = await GetProxiesAsync<IAdapter1>(BluezConstants.AdapterInterface, rootObject: null);

      return await Task.WhenAll(adapters.Select(Adapter.CreateAsync));
    }

    // Normalize a 16, 32 or 128 bit UUID.
    public static string NormalizeUUID(string uuid)
    {
      // TODO: Improve this validation.
      if (uuid.Length == 4) {
        return $"0000{uuid}-0000-1000-8000-00805f9b34fb".ToLowerInvariant();
      }
      else if (uuid.Length == 8) {
        return $"{uuid}-0000-1000-8000-00805f9b34fb".ToLowerInvariant();
      }
      else if (uuid.Length == 36) {
        return uuid.ToLowerInvariant();
      }
      else {
        throw new ArgumentException($"'{uuid}' isn't a valid 16, 32 or 128 bit UUID.");
      }
    }

    /// <param name="interfaceName">The interface to search for</param>
    /// <param name="rootObject">The DBus object to search under. Can be null</param>
    internal static async Task<IReadOnlyList<T>> GetProxiesAsync<T>(string interfaceName, IDBusObject rootObject)
    {
      // Console.WriteLine("GetProxiesAsync called.");
      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      var objects = await objectManager.GetManagedObjectsAsync();

      var matchingObjectPaths = objects
          .Where(obj => IsMatch(interfaceName, obj.Key, obj.Value, rootObject))
          .Select(obj => obj.Key);

      var proxies = matchingObjectPaths
          .Select(objectPath => Connection.System.CreateProxy<T>(BluezConstants.DbusService, objectPath))
          .ToList();

      // Console.WriteLine($"GetProxiesAsync returning {proxies.Count} proxies of type {typeof(T)}.");
      return proxies;
    }

    internal static bool IsMatch(string interfaceName, ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces, IDBusObject rootObject)
    {
      return IsMatch(interfaceName, objectPath, interfaces.Keys, rootObject);
    }

    internal static bool IsMatch(string interfaceName, ObjectPath objectPath, ICollection<string> interfaces, IDBusObject rootObject)
    {
      if (rootObject != null && !objectPath.ToString().StartsWith($"{rootObject.ObjectPath}/"))
      {
        return false;
      }

      return interfaces.Contains(interfaceName);
    }
  }
}
