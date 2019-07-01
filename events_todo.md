* Should I fire `DeviceFound` events for already present devices so consumers don't have to check the current state after adding an event handler? I could indicate in the eventArgs if the device is new so existing devices could be ignored or processed differently. Should I fire these events on the event_add thread before returning, or queue a task? Same idea for device `Connected` and adapter state changes (e.g. `PoweredOn` or `Ready`).
* What's the harm in not disposing of the handler that WatchPropertiesAsync returns?
* Should I use a finalizer instead of making consumers deal with disposing every device they get? Would a finalizer be good enough?

* In C# are you supposed to unsubscribe from every event to prevent leaks?

Answer: Generally yes, so the garbage collection can collect the subscribing object.

* Should I call WatchPropertiesAsync only when the first event is subscribed to? I'll have to call it synchronously, and add mutexs to do refcounting reliably.

Update: Tried this, resulted in a hang on `m_proxy.WatchPropertiesAsync(OnPropertyChanges).GetAwaiter().GetResult()`.

```C#
  public event EventHandler Disconnected
  {
    add
    {
      lock (this)
      {
        if (m_watcher == null)
        {
          m_watcher = m_proxy.WatchPropertiesAsync(OnPropertyChanges).GetAwaiter().GetResult();
        }

        m_disconnected += value;
        m_refcount++;
      }
    }
    remove
    {
      lock (this)
      {
        m_disconnected -= value;
        m_refcount--;

        // Should we instead check if m_disconnected and other events are all null/empty?
        if (m_refcount == 0)
        {
          m_watcher.Dispose();
          m_watcher = null;
        }
      }
    }
  }
```