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
  }
}
