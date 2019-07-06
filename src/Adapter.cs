using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HashtagChris.DotNetBlueZ.Extensions;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  public delegate Task DeviceChangeEventHandlerAsync(Adapter sender, DeviceFoundEventArgs eventArgs);

  public delegate Task AdapterEventHandlerAsync(Adapter sender, BlueZEventArgs eventArgs);

  /// <summary>
  /// Add events to IAdapter1.
  /// </summary>
  public class Adapter : IAdapter1, IDisposable
  {
    ~Adapter()
    {
      Dispose();
    }

    internal static async Task<Adapter> CreateAsync(IAdapter1 proxy)
    {
      var adapter = new Adapter
      {
        m_proxy = proxy,
      };

      var objectManager = Connection.System.CreateProxy<IObjectManager>(BluezConstants.DbusService, "/");
      adapter.m_interfacesWatcher = await objectManager.WatchInterfacesAddedAsync(adapter.OnDeviceAdded);
      adapter.m_propertyWatcher = await proxy.WatchPropertiesAsync(adapter.OnPropertyChanges);

      return adapter;
    }

    public void Dispose()
    {
      m_interfacesWatcher?.Dispose();
      m_interfacesWatcher = null;

      GC.SuppressFinalize(this);
    }

    public event DeviceChangeEventHandlerAsync DeviceFound
    {
      add
      {
        m_deviceFound += value;
        FireEventForExistingDevicesAsync();
      }
      remove
      {
        m_deviceFound -= value;
      }
    }

    public event AdapterEventHandlerAsync PoweredOn
    {
      add
      {
        m_poweredOn += value;
        FireEventIfPropertyAlreadyTrueAsync(m_poweredOn, "Powered");
      }
      remove
      {
        m_poweredOn -= value;
      }
    }

    public event AdapterEventHandlerAsync PoweredOff;

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

    private async void FireEventForExistingDevicesAsync()
    {
      var devices = await this.GetDevicesAsync();
      foreach (var device in devices)
      {
        m_deviceFound?.Invoke(this, new DeviceFoundEventArgs(device, isStateChange: false));
      }
    }

    private async void OnDeviceAdded((ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces) args)
    {
      if (BlueZManager.IsMatch(BluezConstants.DeviceInterface, args.objectPath, args.interfaces, this))
      {
        var device = Connection.System.CreateProxy<IDevice1>(BluezConstants.DbusService, args.objectPath);

        var dev = await Device.CreateAsync(device);
        m_deviceFound?.Invoke(this, new DeviceFoundEventArgs(dev));
      }
    }

    private async void FireEventIfPropertyAlreadyTrueAsync(AdapterEventHandlerAsync handler, string prop)
    {
      try
      {
        var value = await m_proxy.GetAsync<bool>(prop);
        if (value)
        {
          // TODO: Suppress duplicate event from OnPropertyChanges.
          handler?.Invoke(this, new BlueZEventArgs(isStateChange: false));
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error checking if '{prop}' is already true: {ex}");
      }
    }

    private void OnPropertyChanges(PropertyChanges changes)
    {
      foreach (var pair in changes.Changed)
      {
        switch (pair.Key)
        {
          case "Powered":
            if (true.Equals(pair.Value))
            {
              m_poweredOn?.Invoke(this, new BlueZEventArgs());
            }
            else
            {
              PoweredOff?.Invoke(this, new BlueZEventArgs());
            }
            break;
        }
      }
    }

    private IAdapter1 m_proxy;
    private IDisposable m_interfacesWatcher;
    private IDisposable m_propertyWatcher;
    private DeviceChangeEventHandlerAsync m_deviceFound;
    private AdapterEventHandlerAsync m_poweredOn;
  }
}
