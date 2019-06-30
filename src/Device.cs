using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  /// <summary>
  /// Adds events to IDevice1.
  /// </summary>
  public class Device : IDevice1
  {
    // Factory method in case we want to add async code later.
    internal static Device Create(IDevice1 proxy)
    {
      return new Device
      {
        m_proxy = proxy,
      };
    }

    public event EventHandler Connected
    {
      add
      {
        Console.WriteLine("Subscribing to Connected.");
        AddEventHandler(m_connected, value);
        Console.WriteLine("Subscribed!");
      }
      remove
      {
        RemoveEventHandler(m_connected, value);
      }
    }

    public event EventHandler Disconnected
    {
      add
      {
        AddEventHandler(m_disconnected, value);
      }
      remove
      {
        RemoveEventHandler(m_disconnected, value);
      }
    }

    public event EventHandler ServicesResolved
    {
      add
      {
        AddEventHandler(m_servicesResolved, value);
      }
      remove
      {
        RemoveEventHandler(m_servicesResolved, value);
      }
    }

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

    private void AddEventHandler(EventHandler handler, EventHandler callback)
    {
      lock (m_eventLock)
      {
        if (m_watcher == null)
        {
          Console.WriteLine("Creating property watcher (could hang here)...");
          m_watcher = m_proxy.WatchPropertiesAsync(OnPropertyChanges).GetAwaiter().GetResult();
          Console.WriteLine("Property watcher created.");
        }

        Console.WriteLine("Adding callback.");
        handler += callback;
        Console.WriteLine("Callback added.");
      }
    }

    private void RemoveEventHandler(EventHandler handler, EventHandler callback)
    {
      lock (m_eventLock)
      {
        handler -= callback;

        if (!EventListenersRegistered())
        {
          m_watcher.Dispose();
          m_watcher = null;
        }
      }
    }

    private bool EventListenersRegistered()
    {
      return m_connected != null;
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
              m_connected?.Invoke(this, new EventArgs());
            }
            else
            {
              m_disconnected?.Invoke(this, new EventArgs());
            }
            break;

          case "ServicesResolved":
            if (true.Equals(pair.Value))
            {
              m_servicesResolved?.Invoke(this, new EventArgs());
            }
            break;
        }
      }
    }

    private IDevice1 m_proxy;
    private IDisposable m_watcher;

    private event EventHandler m_connected;

    private event EventHandler m_disconnected;

    private event EventHandler m_servicesResolved;

    private Object m_eventLock = new Object();
  }
}