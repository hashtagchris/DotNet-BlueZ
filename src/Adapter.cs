using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  public delegate void DeviceEventHandler(Adapter sender, Device device);

  /// <summary>
  /// Add events to IAdapter1.
  /// </summary>
  public class Adapter : IAdapter1, IDisposable
  {
    internal static async Task<Adapter> CreateAsync(IAdapter1 proxy)
    {
      var adapter = new Adapter
      {
        m_proxy = proxy,
      };

      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      adapter.m_watcher = await objectManager.WatchInterfacesAddedAsync(adapter.OnDeviceAdded);

      return adapter;
    }

    public void Dispose()
    {
      m_watcher.Dispose();
    }

    public event DeviceEventHandler DeviceAdded;

    public ObjectPath ObjectPath => m_proxy.ObjectPath;

    public Task<Adapter1Properties> GetAllAsync()
    {
      return m_proxy.GetAllAsync();
    }

    public Task<T> GetAsync<T>(string prop)
    {
      return m_proxy.GetAsync<T>(prop);
    }

    public Task<string[]> GetDiscoveryFiltersAsync()
    {
      return m_proxy.GetDiscoveryFiltersAsync();
    }

    public Task RemoveDeviceAsync(ObjectPath Device)
    {
      return m_proxy.RemoveDeviceAsync(Device);
    }

    public Task SetAsync(string prop, object val)
    {
      return m_proxy.SetAsync(prop, val);
    }

    public Task SetDiscoveryFilterAsync(IDictionary<string, object> Properties)
    {
      return m_proxy.SetDiscoveryFilterAsync(Properties);
    }

    public Task StartDiscoveryAsync()
    {
      return m_proxy.StartDiscoveryAsync();
    }

    public Task StopDiscoveryAsync()
    {
      return m_proxy.StopDiscoveryAsync();
    }

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
      return m_proxy.WatchPropertiesAsync(handler);
    }

    void OnDeviceAdded((ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces) args)
    {
      if (BlueZManager.IsMatch(BluezConstants.DeviceInterface, args.objectPath, args.interfaces, this))
      {
        var device = Connection.System.CreateProxy<IDevice1>(BluezConstants.DbusService, args.objectPath);

        var dev = Device.Create(device);
        DeviceAdded?.Invoke(this, dev);
      }
    }

    private IAdapter1 m_proxy;
    private IDisposable m_watcher;
  }
}
