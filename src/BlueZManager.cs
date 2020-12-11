using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  public static class BlueZManager
  {

    //Persistent instance of the object manager, creating the manager consumes resources
    private static IObjectManager _objectManager;
    
    //Keeping a list of cached items from the 
    private static IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> _proxyCache;

    //Locker for accessing cached proxy data
    public static object _proxyCacheLocker = new object();


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
      //Upon first run do make sure the proxy is set in the initial cache
      if (_proxyCache == null) { _proxyCache = await RebuildProxyCache(); }

      // Consume the object from the proxycache
      var matchingObjectPaths = _proxyCache
        .Where(obj => IsMatch(interfaceName, obj.Key, obj.Value, rootObject))
        .Select(obj => obj.Key).ToList();

      var proxies = matchingObjectPaths
        .Select(objectPath => Connection.System.CreateProxy<T>(BluezConstants.DbusService, objectPath))
        .ToList();

      return proxies;
    }
    
    private static async Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> RebuildProxyCache()
    {
      // Create the object manager and attach the add/remove handlers to update our cache
      if (_objectManager == null)
      {
        _objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
        await _objectManager.WatchInterfacesAddedAsync(AddInstanceToProxyCache, null);
        await _objectManager.WatchInterfacesRemovedAsync(DropInstanceFromProxyCache, null);
      }
      var objectManager = _objectManager;
      var objects = await objectManager.GetManagedObjectsAsync();
      return objects;
    }

    private static void AddInstanceToProxyCache((ObjectPath @object, IDictionary<string, IDictionary<string, object>> interfaces) obj)
    {
      lock (_proxyCacheLocker)
      {
        if (!_proxyCache.ContainsKey(obj.@object))
        {
            _proxyCache.Add(obj.@object, obj.interfaces);
        }
      }
    }

    private static void DropInstanceFromProxyCache((ObjectPath @object, string[] interfaces) obj)
    {
      lock (_proxyCacheLocker)
      {
        if (_proxyCache.ContainsKey(obj.@object))
        {
          //Remove all the indicated interfaces from the cache
          foreach (var i in obj.interfaces)
          {
            if (_proxyCache[obj.@object].ContainsKey(i))
            {
              _proxyCache[obj.@object].Remove(i);
            }
          }

          //If there are no objects left in the cached element remove the element alltogether
          if (_proxyCache[obj.@object].Count == 0)
          {
            _proxyCache.Remove(obj.@object);
          }
        }
      }
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
