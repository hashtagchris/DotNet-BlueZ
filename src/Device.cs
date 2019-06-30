using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  /// <summary>
  /// Adds events to IDevice1.
  /// </summary>
  public class Device : IDevice1, IDisposable
  {
    internal static async Task<Device> CreateAsync(IDevice1 proxy)
    {
      var device = new Device
      {
        m_proxy = proxy,
      };
      device.m_watcher = await proxy.WatchPropertiesAsync(device.OnPropertyChanges);

      return device;
    }

    public void Dispose()
    {
      m_watcher.Dispose();
    }

    public event EventHandler Connected;
    public event EventHandler Disconnected;
    public event EventHandler ServicesResolved;

    public ObjectPath ObjectPath => m_proxy.ObjectPath;

    public Task CancelPairingAsync()
    {
      return m_proxy.CancelPairingAsync();
    }

    public Task ConnectAsync()
    {
      return m_proxy.ConnectAsync();
    }

    public Task ConnectProfileAsync(string UUID)
    {
      return m_proxy.ConnectProfileAsync(UUID);
    }

    public Task DisconnectAsync()
    {
      return m_proxy.DisconnectAsync();
    }

    public Task DisconnectProfileAsync(string UUID)
    {
      return m_proxy.DisconnectProfileAsync(UUID);
    }

    public Task<Device1Properties> GetAllAsync()
    {
      return m_proxy.GetAllAsync();
    }

    public Task<T> GetAsync<T>(string prop)
    {
      return m_proxy.GetAsync<T>(prop);
    }

    public Task PairAsync()
    {
      return m_proxy.PairAsync();
    }

    public Task SetAsync(string prop, object val)
    {
      return m_proxy.SetAsync(prop, val);
    }

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
      return m_proxy.WatchPropertiesAsync(handler);
    }

    private void OnPropertyChanges(PropertyChanges changes)
    {
      foreach (var pair in changes.Changed)
      {
        switch (pair.Key)
        {
          case "Connected":
            if (true.Equals(pair.Value))
            {
              Connected?.Invoke(this, new EventArgs());
            }
            else
            {
              Disconnected?.Invoke(this, new EventArgs());
            }
            break;

          case "ServicesResolved":
            if (true.Equals(pair.Value))
            {
              ServicesResolved?.Invoke(this, new EventArgs());
            }
            break;
        }
      }
    }

    private IDevice1 m_proxy;
    private IDisposable m_watcher;
  }
}