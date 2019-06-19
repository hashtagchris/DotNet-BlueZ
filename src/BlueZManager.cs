using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

namespace HashtagChris.DotNetBlueZ
{
  public static class BlueZManager
  {
    public static IAdapter1 GetAdapter(string adapterName)
    {
      var adapterObjectPath = $"/org/bluez/{adapterName}";
      return Connection.System.CreateProxy<IAdapter1>(BluezConstants.DbusService, adapterObjectPath);
    }

    // IDBusObject doesn't include GetAsync<T> and WatchPropertiesAsync, so we use dynamic here.
    public static async Task WaitForPropertyValueAsync<T>(dynamic obj, string propertyName, T value, TimeSpan timeout)
    {
      var waitTask = WaitForPropertyValueAsyncInternal(obj, propertyName, value);
      var currentValue = await obj.GetAsync<T>(propertyName);
      Console.WriteLine($"{propertyName}: {currentValue}");

      // https://stackoverflow.com/questions/390900/cant-operator-be-applied-to-generic-types-in-c
      if (EqualityComparer<T>.Default.Equals(currentValue, value))
      {
        return;
      }

      var timeoutTask = Task.Delay(timeout);

      await Task.WhenAny(new Task[] { waitTask, timeoutTask });
      if (!waitTask.IsCompleted)
      {
        throw new TimeoutException("Timed out waiting to read characteristic value.");
      }

      // propogate any exceptions.
      await waitTask;
    }

    private static Task WaitForPropertyValueAsyncInternal<T>(dynamic obj, string propertyName, T value)
    {
      var taskSource = new TaskCompletionSource<bool>();

      IDisposable watcher = null;
      Action<PropertyChanges> onPropertiesChanged = propertyChanges => {
        try
        {
          if (propertyChanges.Changed.Any(kvp => kvp.Key == propertyName))
          {
            var pair = propertyChanges.Changed.Single(kvp => kvp.Key == propertyName);
            if (pair.Value.Equals(value))
            {
              Console.WriteLine($"[CHG] {propertyName}: {pair.Value}.");
              taskSource.TrySetResult(true);
              watcher.Dispose();
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception: {ex}");
          taskSource.TrySetException(ex);
          watcher.Dispose();
        }
      };

      watcher = obj.WatchPropertiesAsync(onPropertiesChanged);

      return taskSource.Task;
    }
  }
}
