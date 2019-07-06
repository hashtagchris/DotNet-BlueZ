using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  public delegate Task GattCharacteristicEventHandlerAsync(GattCharacteristic sender, GattCharacteristicValueEventArgs eventArgs);

  /// <summary>
  /// Adds events to IGattCharacteristic1.
  /// </summary>
  public class GattCharacteristic : IGattCharacteristic1, IDisposable
  {
    ~GattCharacteristic()
    {
      Dispose();
    }

    internal static async Task<GattCharacteristic> CreateAsync(IGattCharacteristic1 proxy)
    {
      var characteristic = new GattCharacteristic
      {
        m_proxy = proxy,
      };

      characteristic.m_propertyWatcher = await proxy.WatchPropertiesAsync(characteristic.OnPropertyChanges);

      return characteristic;
    }

    public void Dispose()
    {
      Console.WriteLine("GattCharacteristic disposing.");
      m_propertyWatcher?.Dispose();
      m_propertyWatcher = null;

      GC.SuppressFinalize(this);
    }

    public event GattCharacteristicEventHandlerAsync Value
    {
      add
      {
        m_value += value;

        // Subscribe here instead of CreateAsync, because not all GATT characteristics are notifable.
        Subscribe();
      }
      remove
      {
        m_value -= value;
      }
    }

    public ObjectPath ObjectPath => m_proxy.ObjectPath;

    public Task<byte[]> ReadValueAsync(IDictionary<string, object> Options)
    {
      return m_proxy.ReadValueAsync(Options);
    }

    public Task WriteValueAsync(byte[] Value, IDictionary<string, object> Options)
    {
      return m_proxy.WriteValueAsync(Value, Options);
    }

    public Task<(CloseSafeHandle fd, ushort mtu)> AcquireWriteAsync(IDictionary<string, object> Options)
    {
      return m_proxy.AcquireWriteAsync(Options);
    }

    public Task<(CloseSafeHandle fd, ushort mtu)> AcquireNotifyAsync(IDictionary<string, object> Options)
    {
      return m_proxy.AcquireNotifyAsync(Options);
    }

    public Task StartNotifyAsync()
    {
      return m_proxy.StartNotifyAsync();
    }

    public Task StopNotifyAsync()
    {
      return m_proxy.StopNotifyAsync();
    }

    public Task<T> GetAsync<T>(string prop)
    {
      return m_proxy.GetAsync<T>(prop);
    }

    public Task<GattCharacteristic1Properties> GetAllAsync()
    {
      return m_proxy.GetAllAsync();
    }

    public Task SetAsync(string prop, object val)
    {
      return m_proxy.SetAsync(prop, val);
    }

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
      return m_proxy.WatchPropertiesAsync(handler);
    }

    private async void Subscribe()
    {
      try
      {
        await m_proxy.StartNotifyAsync();

        // Is there a way to check if a characteristic supports Read?
        // // Reading the current value will trigger OnPropertyChanges.
        // var options = new Dictionary<string, object>();
        // var value = await m_proxy.ReadValueAsync(options);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error subscribing to characteristic value: {ex}");
      }
    }

    private void OnPropertyChanges(PropertyChanges changes)
    {
      // Console.WriteLine("OnPropertyChanges called.");
      foreach (var pair in changes.Changed)
      {
        switch (pair.Key)
        {
          case "Value":
            m_value?.Invoke(this, new GattCharacteristicValueEventArgs((byte[])pair.Value));
            break;
        }
      }
    }

    private IGattCharacteristic1 m_proxy;
    private IDisposable m_propertyWatcher;
    private event GattCharacteristicEventHandlerAsync m_value;
  }
}